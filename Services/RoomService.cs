using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace HotelData
{
    internal static class RoomService
    {
        public static Task<List<Room>> GetRoomsAsync()
        {
            return HotelSession.SendAsync<List<Room>>(HttpMethod.Post, "rest/v1/rpc/hotel_get_rooms", new { });
        }

        public static async Task<Room> AddRoomAsync(Room room)
        {
            var rooms = await HotelSession.SendAsync<List<Room>>(HttpMethod.Post, "rest/v1/rooms?select=Id:id,Number:number,Type:room_type,Bed:bed,Price:price", new { number = room.Number.Trim(), room_type = room.Type, bed = room.Bed, price = room.Price }).ConfigureAwait(false);
            HotelSession.NotifyChanged("ROOMS_CHANGED");
            return rooms[0];
        }
    }
}
