using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using HotelData;
using HotelNetworking;

namespace HotelServer
{
    internal sealed class RoomRepository
    {
        private readonly string connectionString;
        private readonly SemaphoreSlim initializeLock = new SemaphoreSlim(1, 1);
        private bool initialized;

        public RoomRepository(string connectionString) { this.connectionString = connectionString; }

        private async Task EnsureInitializedAsync()
        {
            await initializeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (initialized) return;
                await HotelDatabase.InitializeAsync(connectionString).ConfigureAwait(false);
                initialized = true;
            }
            finally { initializeLock.Release(); }
        }

        public async Task<List<Room>> GetRoomsAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var rooms = new List<Room>();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = new SqlCommand("SELECT roomid, roomNo, roomType, bed, price, booked FROM dbo.rooms ORDER BY roomid", connection))
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                        rooms.Add(new Room
                        {
                            roomid = reader.GetInt32(0), roomNo = reader.GetString(1), roomType = reader.GetString(2),
                            bed = reader.GetString(3), price = reader.GetInt64(4), booked = reader.IsDBNull(5) ? null : reader.GetString(5)
                        });
                }
            }
            return rooms;
        }
    }
}
