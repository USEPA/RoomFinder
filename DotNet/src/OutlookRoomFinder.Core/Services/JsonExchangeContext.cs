using Newtonsoft.Json;
using OutlookRoomFinder.Core.Models.FileModels;
using OutlookRoomFinder.Core.Models;
using OutlookRoomFinder.Core.Extensions;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Events;

namespace OutlookRoomFinder.Core.Services
{
    /// <summary>
    /// Provides implementation for reading JSON file into memory for queries
    /// </summary>
    public class JsonExchangeContext : IExchangeContext
    {
        protected ILogger Logger { get; }
        private readonly IAppSettings AppSettings;
        private readonly int CacheLifetime;
        private readonly int DefaultRoomCapacity;
        private static DateTime DataLoadedTimestamp { get; set; }
        private static bool FileContextLoaded { get; set; }

        public JsonExchangeContext(ILogger logger, IAppSettings appSettings)
        {
            Logger = logger;
            AppSettings = appSettings;
            CacheLifetime = AppSettings.Exchange.CacheLifetime;
            DefaultRoomCapacity = AppSettings.Exchange.DefaultRoomCapacity;
        }

        public JsonExchangeContext(ILogger logger, IAppSettings appSettings, LocalJsonModel jsonModel)
            : this(logger, appSettings)
        {
            FileContext = jsonModel;
            FileContextLoaded = true;
            DataLoadedTimestamp = DateTime.Now;
        }


        public void ClearCache()
        {
            FileContextLoaded = false;
            roomLocations.Clear();
            roomEquipmentTypes.Clear();
            roomLists.Clear();
            rooms.Clear();
            equipmentTypes.Clear();
            equipmentLocations.Clear();
            equipLists.Clear();
            equipments.Clear();
            DataLoadedTimestamp = DateTime.UtcNow.Add(new TimeSpan(-24, 0, 0));
        }

        private static readonly object JsonLoadFileSyncLock = new object();
        private LocalJsonModel FileContext { get; set; }
        private void LoadJsonFile()
        {
            lock (JsonLoadFileSyncLock)
            {
                if (DataLoadedTimestamp < DateTime.Now || FileContext == null)
                {
                    var fileName = Path.Combine(new[] { AppSettings.WebRootPath, "assets", "config", AppSettings.Exchange.JsonFilename });
                    if (File.Exists(fileName))
                    {
                        using var file = File.OpenText(fileName);
                        JsonSerializer serializer = new JsonSerializer();
                        FileContext = (LocalJsonModel)serializer.Deserialize(file, typeof(LocalJsonModel));
                        FileContextLoaded = true;
                    }
                    Logger.Logging(LogEventLevel.Debug, $"Debugging jsonfile with filename {fileName}");
                    Logger.Logging(LogEventLevel.Debug, $"Debugging FileContextLoaded {FileContextLoaded}");
                    Logger.Logging(LogEventLevel.Debug, $"Debugging Rooms #{FileContext?.Mailboxes.Count ?? 0}");
                }
            }
        }

        private static readonly object JsonLoadSyncLock = new object();
        private void LoadContext()
        {
            LoadJsonFile();
            lock (JsonLoadSyncLock)
            {
                Logger.Logging(LogEventLevel.Debug, $"Debugging FileContextLoaded => {FileContextLoaded}");
                Logger.Logging(LogEventLevel.Debug, $"Debugging DataLoadedTimestamp < DateTime.Now => {(DataLoadedTimestamp < DateTime.Now)}");
                if (FileContextLoaded && DataLoadedTimestamp < DateTime.Now)
                {
                    ClearCache();

                    LoadEquipmentListing();
                    LoadEquipments();
                    LoadEquipmentLocations();
                    LoadEquipmentTypes();

                    LoadRoomListingData();
                    LoadRoomsData();
                    LoadRoomLocations();
                    LoadRoomEquipmentTypes();

                    DataLoadedTimestamp = DateTime.Now.AddMinutes(CacheLifetime);
                }
            }
        }


        private readonly ConcurrentBag<ResourceListing<ResourceItemEquipment>> equipLists = new ConcurrentBag<ResourceListing<ResourceItemEquipment>>();
        public ConcurrentBag<ResourceListing<ResourceItemEquipment>> GetEquipmentListing()
        {
            LoadContext();
            return equipLists;
        }
        private static readonly object EquipmentListingSyncLock = new object();
        private void LoadEquipmentListing()
        {
            lock (EquipmentListingSyncLock)
            {
                if (equipLists?.Count == 0)
                {
                    var fileEquipmentTypes = FileContext.Equipments.Select(sw => sw.EquipmentType).Distinct();
                    fileEquipmentTypes.ToList().ForEach(equipTypeName =>
                    {
                        var equipmentListing = new ResourceListing<ResourceItemEquipment>
                        {
                            DisplayName = equipTypeName,
                            Resources = new List<ResourceItemEquipment>()
                        };

                        equipLists.Add(equipmentListing);
                    });
                }
            }
        }

        private readonly ConcurrentBag<ResourceItemEquipment> equipments = new ConcurrentBag<ResourceItemEquipment>();
        public ConcurrentBag<ResourceItemEquipment> GetEquipments()
        {
            LoadContext();
            return equipments;
        }
        private static readonly object EquipmentsSyncLock = new object();
        private void LoadEquipments()
        {
            lock (EquipmentsSyncLock)
            {
                if (equipments?.Count == 0)
                {
                    foreach (var equipment in FileContext?.Equipments.ConvertIntoSureEnumerable())
                    {
                        var equipItem = new ResourceItemEquipment
                        {
                            DisplayName = equipment.DisplayName,
                            EmailAddress = equipment.EmailAddress,
                            SamAccountName = equipment.SamAccountName
                        };

                        equipItem.Location = new LocationAddress
                        {
                            Country = equipment.CountryOrRegion,
                            State = equipment.StateOrProvince,
                            City = equipment.City,
                            Office = equipment.Office,
                            Floor = equipment.Floor,
                            PostalCode = equipment.PostalCode
                        };
                        equipItem.RestrictionType = equipment.GetResourceRestriction();
                        equipItem.EquipmentType = equipment.EquipmentType;

                        // equipments should exists based on GetEquipmentListing()
                        equipLists.Where(equip => equip.DisplayName.Equals(equipment.EquipmentType, StringComparison.CurrentCultureIgnoreCase))
                        .ForEach((equipmentList) =>
                        {
                            equipmentList.Resources.Add(equipItem);
                            equipItem.EquipmentList = equipmentList;
                        });


                        equipments.Add(equipItem);
                    }
                }
            }
        }

        private readonly NodeSortedList equipmentLocations = new NodeSortedList();
        public NodeSortedList GetEquipmentLocations()
        {
            LoadContext();
            return equipmentLocations;
        }
        private static readonly object EquipmentLocationsSyncLock = new object();
        private void LoadEquipmentLocations()
        {
            lock (EquipmentLocationsSyncLock)
            {
                if (equipmentLocations?.Count == 0)
                {
                    equipments.ConvertIntoSureEnumerable()
                        .Where(w => w.Location != null && !string.IsNullOrEmpty(w.Location.State) && !string.IsNullOrEmpty(w.Location.City))
                        .ToList()
                        .ForEach((resource) =>
                        {
                            var l = resource.Location;
                            equipmentLocations.Add(new[] { l.State, l.City, l.Office, l.Floor ?? "unknown" }, resource);
                        });
                }
            }
        }

        private readonly ConcurrentBag<string> equipmentTypes = new ConcurrentBag<string>();
        public ConcurrentBag<string> GetEquipmentTypes()
        {
            LoadContext();
            return equipmentTypes;
        }
        private static readonly object EquipmentTypesSyncLock = new object();
        private void LoadEquipmentTypes()
        {
            lock (EquipmentTypesSyncLock)
            {
                if (equipmentTypes?.Count == 0)
                {
                    FileContext?.EquipmentList.ForEach((equipmentName) => equipmentTypes.Add(equipmentName));
                }
            }
        }

        private readonly NodeSortedList roomLocations = new NodeSortedList();
        public NodeSortedList GetRoomLocations()
        {
            LoadContext();
            return roomLocations;
        }
        private static readonly object RoomLocationsSyncLock = new object();
        private void LoadRoomLocations()
        {
            lock (RoomLocationsSyncLock)
            {
                Logger.Logging(LogEventLevel.Debug, $"Debugging roomLocations?.Count => {roomLocations?.Count}");
                if (roomLocations?.Count == 0)
                {
                    rooms.ConvertIntoSureEnumerable()
                        .Where(w => w.Location != null && !string.IsNullOrEmpty(w.Location.State) && !string.IsNullOrEmpty(w.Location.City))
                        .ToList()
                        .ForEach((room) =>
                        {
                            var l = room.Location;
                            roomLocations.Add(new[] { l.State, l.City, l.Office, l.Floor ?? "unknown" }, room);
                        });
                }
            }
        }

        private readonly ConcurrentBag<string> roomEquipmentTypes = new ConcurrentBag<string>();
        public ConcurrentBag<string> GetRoomEquipmentTypes()
        {
            LoadContext();
            return roomEquipmentTypes;
        }
        private static readonly object RoomEquipmentTypesSyncLock = new object();
        private void LoadRoomEquipmentTypes()
        {
            lock (RoomEquipmentTypesSyncLock)
            {
                if (roomEquipmentTypes.Count == 0)
                {
                    rooms
                        .SelectMany(room => room.Equipment)
                        .Distinct()
                        .ForEach((equipment) => roomEquipmentTypes.Add(equipment));
                    FileContext?.Equipment.ForEach((foundItem) => roomEquipmentTypes.Add(foundItem));
                }
            }
        }

        private readonly ConcurrentBag<ResourceListing<ResourceItemMailbox>> roomLists = new ConcurrentBag<ResourceListing<ResourceItemMailbox>>();
        public ConcurrentBag<ResourceListing<ResourceItemMailbox>> GetRoomsListing()
        {
            LoadContext();
            return roomLists;
        }
        private static readonly object RoomListingDataSyncLock = new object();
        private void LoadRoomListingData()
        {
            lock (RoomListingDataSyncLock)
            {
                if (roomLists?.Count == 0)
                {
                    var locations = FileContext.Locations;

                    foreach (var location in locations)
                    {
                        roomLists.Add(new ResourceListing<ResourceItemMailbox>
                        {
                            DisplayName = location.DisplayName,
                            EmailAddress = location.EmailAddress,
                            Resources = new List<ResourceItemMailbox>()
                        });
                    }
                }
            }
        }

        private readonly ConcurrentBag<ResourceItemMailbox> rooms = new ConcurrentBag<ResourceItemMailbox>();
        public ConcurrentBag<ResourceItemMailbox> GetRooms()
        {
            LoadContext();
            return rooms;
        }
        private static readonly object ConfRoomSyncLock = new object();
        private void LoadRoomsData()
        {
            lock (ConfRoomSyncLock)
            {
                if (rooms?.Count == 0)
                {
                    FileContext?.Mailboxes.ConvertIntoSureEnumerable()
                        .ForEach((room) =>
                        {
                            var cf = new ResourceItemMailbox
                            {
                                Capacity = room.ResourceCapacity ?? DefaultRoomCapacity,
                                DisplayName = room.DisplayName,
                                EmailAddress = room.EmailAddress,
                                RestrictionImage = "blank.ico",
                                RestrictionTooltip = "Unrestricted",
                                Dependencies = room.Dependencies,
                                SamAccountName = room.SamAccountName,
                            };

                            room.RestrictionType ??= "None";
                            cf.RestrictionType = room.RestrictionType.ToUpperInvariant() switch
                            {
                                "APPROVALREQUIRED" => RestrictionType.ApprovalRequired,
                                "RESTRICTED" => RestrictionType.Restricted,
                                _ => RestrictionType.None
                            };

                            cf.Location = new LocationAddress
                            {
                                Country = room.CountryOrRegion,
                                State = room.StateOrProvince,
                                City = room.City,
                                Office = room.Office ?? "unknown",
                                Floor = room.Floor,
                                PostalCode = room.PostalCode
                            };

                            if (!string.IsNullOrEmpty(room.Office))
                            {
                                roomLists.Where(rl => rl.DisplayName.Equals(room.Office, StringComparison.CurrentCultureIgnoreCase))
                                    .ForEach((roomList) =>
                                    {
                                        cf.RoomList = roomList;
                                        roomList.Resources.Add(cf);
                                    });
                            }

                            var EquipmentDependencies = room.EquipmentDependencies.ConvertIntoSureEnumerable();
                            if (EquipmentDependencies.Any())
                            {
                                cf.EquipmentDependencies = new List<EquipmentModel>();
                            }
                            EquipmentDependencies.Join(equipments,
                                equipmentEmail => equipmentEmail,
                                equipmentItem => equipmentItem.EmailAddress,
                                (equipmentEmail, equipment) => new EquipmentModel
                                {
                                    DisplayName = equipment.DisplayName,
                                    EmailAddress = equipmentEmail,
                                    EquipmentType = equipment.EquipmentType
                                },
                                StringComparer.InvariantCultureIgnoreCase).ForEach((eqp) =>
                                {
                                    cf.EquipmentDependencies.Add(eqp);
                                });

                            room.Equipment.Intersect(roomEquipmentTypes).ForEach(
                                (equipmentSearch) =>
                                {
                                    cf.Equipment.Add(equipmentSearch);
                                });

                            rooms.Add(cf);
                        });
                }
            }
        }
    }
}
