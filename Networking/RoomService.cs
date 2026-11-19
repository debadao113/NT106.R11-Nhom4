using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace HotelNetworking
{
    internal static class RoomService
    {
        public static async Task<List<Room>> GetRoomsAsync()
        {
            await HotelConnection.ConnectAsync().ConfigureAwait(false);
            var response = await HotelConnection.Client.SendAsync("GET_ROOMS", null).ConfigureAwait(false);
            if (response.success != true) throw new IOException(response.error?.message ?? "Không thể tải danh sách phòng.");
            return new JavaScriptSerializer().ConvertToType<List<Room>>(response.data);
        }

        public static async Task<Room> AddRoomAsync(Room room)
        {
            await HotelConnection.ConnectAsync().ConfigureAwait(false);
            var response = await HotelConnection.Client.SendAsync("ADD_ROOM", room).ConfigureAwait(false);
            if (response.success != true) throw new IOException(response.error?.message ?? "Không thể thêm phòng.");
            return new JavaScriptSerializer().ConvertToType<Room>(response.data);
        }
    }
}
