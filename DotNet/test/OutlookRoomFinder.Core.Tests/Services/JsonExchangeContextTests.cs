using FluentAssertions;
using NSubstitute;
using OutlookRoomFinder.Core.Models.FileModels;
using OutlookRoomFinder.Core.Services;
using Serilog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xunit;

namespace OutlookRoomFinder.Core.Tests.Services
{
    public class JsonExchangeContextTests
    {
        private readonly ILogger ilogger;
        private readonly IAppSettings appSettings;
        private readonly int cacheLifetime = 1400;

        public JsonExchangeContextTests()
        {
            ilogger = Substitute.For<ILogger>();
            appSettings = Substitute.For<IAppSettings>();
            appSettings.WebRootPath = "c:\\test";
            appSettings.Exchange = new AppSettingsExchange
            {
                JsonFilename = "resources-objects.json",
                CacheLifetime = cacheLifetime,
                DefaultRoomCapacity = 20
            };
        }

        private LocalJsonModel SetupContext()
        {
            var fileContext = Substitute.For<LocalJsonModel>();
            fileContext.Equipment = new List<string>
            {
                "LCD TV",
                "Projector",
                "Custom Table Config",
                "Wireless Microphone",
            };
            fileContext.EquipmentList = new List<string>
            {
                "Microphone",
                "PA",
                "VTC",
                "Laptop",
            };
            fileContext.Locations = new List<ResourceJsonObject>
            {
                new ResourceJsonObject { DisplayName = "AWBERC", EmailAddress = "awerc@test.com" },
                new ResourceJsonObject { DisplayName = "Erlanger", EmailAddress = "Erlanger@test.com" },
                new ResourceJsonObject { DisplayName = "Annex", EmailAddress = "Annex@test.com" }
            };
            fileContext.Mailboxes = new List<MailboxJsonObject> {
                new MailboxJsonObject {
                    EmailAddress = "mailbox1@email.com",
                    DisplayName = "mailbox 1",
                    SamAccountName = "samaccount-mailbox1",
                    PrimarySmtpAddress = "mailbox1@email.com",
                    UserPrincipalName = "mailbox1@email.com",
                    ResourceCapacity = 25,
                    RestrictionType = "ApprovalRequired",
                    BookingProcessing = "AutoAccept",
                    Office = "AWberc",
                    Equipment = new Collection<string> {"LCD TV", "VTC", "Laptop" },
                    EquipmentDependencies = new Collection<string>
                    {
                        "equipment1@email.com", "equipment2@email.com", "laptop1@email.com"
                    }
                },
                new MailboxJsonObject {
                    EmailAddress = "mailbox2@email.com",
                    DisplayName = "mailbox 2",
                    SamAccountName = "samaccount-mailbox2",
                    PrimarySmtpAddress = "mailbox2@email.com",
                    UserPrincipalName = "mailbox2@email.com",
                    ResourceCapacity = 25,
                    RestrictionType = "None",
                    BookingProcessing = "AutoAccept",
                    Office = "AWBERC",
                    Equipment = new Collection<string> {"LCD TV", "Laptop" },
                    EquipmentDependencies = new Collection<string>
                    {
                        "equipment1@email.com", "laptop2@email.com"
                    }
                },
                new MailboxJsonObject {
                    EmailAddress = "mailbox3@email.com",
                    DisplayName = "mailbox 3",
                    SamAccountName = "samaccount-mailbox3",
                    PrimarySmtpAddress = "mailbox3@email.com",
                    UserPrincipalName = "mailbox3@email.com",
                    ResourceCapacity = 125,
                    RestrictionType = "unknown",
                    BookingProcessing = "AutoAccept",
                    Office = "Erlanger",
                    Equipment = new Collection<string> {"LCD TV", "Laptop" },
                    EquipmentDependencies = new Collection<string>
                    {
                        "equipment2@email.com", "laptop2@email.com"
                    }
                }
            };
            fileContext.Equipments = new List<EquipmentJsonObject> {
                    new EquipmentJsonObject { EmailAddress = "equipment1@email.com", DisplayName = "Equip 1", EquipmentType = "VTC" },
                    new EquipmentJsonObject { EmailAddress = "equipment2@email.com", DisplayName = "Equip 2", EquipmentType = "VTC" },
                    new EquipmentJsonObject { EmailAddress = "laptop1@email.com", DisplayName = "Laptop 1", EquipmentType = "Laptop" },
                    new EquipmentJsonObject { EmailAddress = "laptop2@email.com", DisplayName = "Laptop 2", EquipmentType = "Laptop" }
                };
            return fileContext;
        }

        [Fact]
        public void It_can_UseInMemory_context_EquipmentLists()
        {
            //Arrange
            var FileContext = SetupContext();
            var context = new JsonExchangeContext(ilogger, appSettings, FileContext);

            // Act
            var equipmentListing = context.GetEquipmentListing();

            // Assert
            equipmentListing.Count().Should().Be(2);
        }

        [Fact]
        public void It_can_UseInMemory_context_EquipmentLists_CheckItems()
        {
            //Arrange
            var FileContext = SetupContext();
            var context = new JsonExchangeContext(ilogger, appSettings, FileContext);

            // Act
            var equipmentListing = context.GetEquipmentListing();

            // Assert
            equipmentListing.FirstOrDefault(fn => fn.DisplayName == "VTC").Resources.Count().Should().Be(2);
            equipmentListing.FirstOrDefault(fn => fn.DisplayName == "Laptop").Resources.Count().Should().Be(2);
        }

        [Fact]
        public void It_can_UseInMemory_context_EquipmentTypes_CheckItems()
        {
            //Arrange
            var FileContext = SetupContext();
            var context = new JsonExchangeContext(ilogger, appSettings, FileContext);

            // Act
            var equipmentTypes = context.GetEquipmentTypes();

            // Assert
            equipmentTypes.Count().Should().Be(4);
        }

        [Fact]
        public void It_can_UseInMemory_context_Rooms_CheckItems()
        {
            //Arrange
            var FileContext = SetupContext();
            var context = new JsonExchangeContext(ilogger, appSettings, FileContext);

            // Act
            var rooms = context.GetRooms();
            var roomListings = context.GetRoomsListing();

            // Assert
            Assert.NotNull(rooms);
            Assert.NotNull(roomListings);
            roomListings.Count().Should().Be(3);
            roomListings.FirstOrDefault(fn => fn.DisplayName == "Annex").Resources.Count().Should().Be(0);
            roomListings.FirstOrDefault(fn => fn.DisplayName == "AWBERC").Resources.Count().Should().Be(2);
            roomListings.FirstOrDefault(fn => fn.DisplayName == "Erlanger").Resources.Count().Should().Be(1);
        }

        [Fact]
        public void It_can_Deserialize_json_file_context()
        {
            //Arrange
            appSettings.WebRootPath = Directory.GetCurrentDirectory();
            var context = new JsonExchangeContext(ilogger, appSettings);
            context.ClearCache();

            // Act
            var rooms = context.GetRooms();
            var roomListings = context.GetRoomsListing();
            var roomlocations = context.GetRoomLocations();

            // Assert
            rooms.Count().Should().Be(22);
            roomListings.Count().Should().Be(3);
            roomlocations.Count().Should().Be(1);
            Assert.NotNull(roomlocations["OH"]);
            NodeSortedList sortedListing = Assert.IsType<NodeSortedList>(roomlocations["OH"]);
            sortedListing.Keys.Count().Should().Be(1);
        }
    }
}
