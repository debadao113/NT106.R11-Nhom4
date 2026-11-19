using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
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
                        rooms.Add(ReadRoom(reader));
                }
            }
            return rooms;
        }

        public async Task<Room> AddRoomAsync(Room room)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                const string sql = @"INSERT INTO dbo.rooms (roomNo, roomType, bed, price)
OUTPUT INSERTED.roomid, INSERTED.roomNo, INSERTED.roomType, INSERTED.bed, INSERTED.price, INSERTED.booked
VALUES (@roomNo, @roomType, @bed, @price)";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@roomNo", SqlDbType.VarChar, 50).Value = room.roomNo;
                    command.Parameters.Add("@roomType", SqlDbType.VarChar, 100).Value = room.roomType;
                    command.Parameters.Add("@bed", SqlDbType.VarChar, 50).Value = room.bed;
                    command.Parameters.Add("@price", SqlDbType.BigInt).Value = room.price;
                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        await reader.ReadAsync().ConfigureAwait(false);
                        return ReadRoom(reader);
                    }
                }
            }
        }

        private static Room ReadRoom(SqlDataReader reader)
        {
            return new Room
            {
                roomid = reader.GetInt32(0), roomNo = reader.GetString(1), roomType = reader.GetString(2),
                bed = reader.GetString(3), price = reader.GetInt64(4), booked = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }
    }
}
