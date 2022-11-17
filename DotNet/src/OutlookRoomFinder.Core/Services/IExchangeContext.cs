using OutlookRoomFinder.Core.Models;
using System.Collections.Concurrent;

namespace OutlookRoomFinder.Core.Services
{
    public interface IExchangeContext
    {
        /// <summary>
        /// Retreive EquipmentTypes filtered by Room
        /// </summary>
        /// <returns></returns>
        ConcurrentBag<string> GetRoomEquipmentTypes();

        /// <summary>
        /// Retreive EquipementTypes
        /// </summary>
        /// <returns></returns>
        ConcurrentBag<string> GetEquipmentTypes();

        /// <summary>
        /// Retreive Equipment listing
        /// </summary>
        /// <returns></returns>
        ConcurrentBag<ResourceListing<ResourceItemEquipment>> GetEquipmentListing();

        /// <summary>
        /// Retreive Room listings
        /// </summary>
        /// <returns></returns>
        ConcurrentBag<ResourceListing<ResourceItemMailbox>> GetRoomsListing();

        /// <summary>
        /// Retreive Rooms
        /// </summary>
        /// <returns></returns>
        ConcurrentBag<ResourceItemMailbox> GetRooms();

        /// <summary>
        /// Retreive Conference room locations
        /// </summary>
        /// <returns></returns>
        NodeSortedList GetRoomLocations();

        /// <summary>
        /// Retreive equipment locations
        /// </summary>
        /// <returns></returns>
        NodeSortedList GetEquipmentLocations();

        /// <summary>
        /// clear the cached data
        /// </summary>
        void ClearCache();
    }
}
