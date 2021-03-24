using ChatApp.Server.Services;
using ChatApp.Shared;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        EncryptionAndCompressService encryptService;
        MemoryDataAndEmailService memoryService;
        IMongoCollection<Room> rooms;
        IMongoCollection<Account> accounts;
        IMongoClient client;

        public RoomsController(IMongoClient client, MemoryDataAndEmailService data, EncryptionAndCompressService encrypt)
        {
            this.client = client;
            var database = client.GetDatabase("ChatAppManagement");
            memoryService = data;
            encryptService = encrypt;
            rooms = database.GetCollection<Room>("rooms");
            accounts = database.GetCollection<Account>("accounts");
        }

        [HttpPost("join-room/{username}/{roomid}")]
        public async Task<SignResult> JoinRoom(string roomid, string username, [FromBody] string password)
        {
            var filter = Builders<Room>.Filter.Eq("_id", roomid);
            var room = rooms.Find(filter).FirstOrDefault();
            if (room is not null)
            {
                if (room.users.Contains(username))
                {
                    if (!room.state)
                    {
                        if (room.password == encryptService.Encrypt(password))
                        {
                            return new SignResult() { success = true };
                        }
                        return new SignResult() { success = false, err = "Incorrect password" };
                    }
                    return new SignResult() { success = true };
                }
                else
                {
                    var userfilter = Builders<Account>.Filter.Eq("_id", username);
                    var userupdate = Builders<Account>.Update.AddToSet<string>("rooms", roomid);
                    var update = Builders<Room>.Update.Set("users", room.users);
                    if (!room.state)
                    {
                        if (room.password == encryptService.Encrypt(password))
                        {
                            var usertask = accounts.UpdateOneAsync(userfilter, userupdate);
                            room.users.Add(username);
                            var task = memoryService.RoomAccounts[roomid].InsertOneAsync(new Account() { username = username, connected = true });
                            var task1 = rooms.UpdateOneAsync(filter, update);
                            memoryService.ChatRooms[roomid].CreateCollection(username);
                            await usertask;
                            await task;
                            await task1;
                            return new SignResult() { success = true };
                        }
                        return new SignResult() { success = false, err = "Incorrect password" };
                    }
                    var usertask1 = accounts.UpdateOneAsync(userfilter, userupdate);
                    room.users.Add(username);
                    var task2 = memoryService.RoomAccounts[roomid].InsertOneAsync(new Account() { username = username, connected = true });
                    var task3 = rooms.UpdateOneAsync(filter, update);
                    memoryService.ChatRooms[roomid].CreateCollection(username);
                    await usertask1;
                    await task2;
                    await task3;
                    return new SignResult() { success = true };
                }
            }
            return new SignResult() { success = false, err = "RoomId does not exist" };
        }

        [HttpPost("create-room/{username}")]
        public async Task<SignResult> CreateRoom(string username, [FromBody] Room room)
        {
            if (memoryService.ChatRooms.ContainsKey(room.Id))
            {
                return new SignResult() { success = false, err = "RoomId has been taken" };
            }
            else
            {
                var userfilter = Builders<Account>.Filter.Eq("_id", username);
                var userupdate = Builders<Account>.Update.AddToSet<string>("rooms", room.Id);
                var usertask = accounts.UpdateOneAsync(userfilter, userupdate);
                room.users.Add(username);
                var task = rooms.InsertOneAsync(room);
                var roomdb = client.GetDatabase(room.Id);
                roomdb.CreateCollection("accounts");
                memoryService.ChatRooms[room.Id] = roomdb;
                var roomacc = roomdb.GetCollection<Account>("accounts");
                memoryService.RoomAccounts[room.Id] = roomacc;
                roomacc.InsertOne(new Account() { username = username, connected = true });
                await usertask;
                await task;
                return new SignResult() { success = true };
            }
        }
    }
}
