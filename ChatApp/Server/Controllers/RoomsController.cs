using ChatApp.Server.Services;
using ChatApp.Shared;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        [HttpGet("rooms")]
        public List<Tuple<string, string>> GetRooms()
        {
            List<Tuple<string, string>> res = new();
            var roomCollection = rooms.Find(s => s.state).ToEnumerable();
            foreach (var room in roomCollection)
            {
                res.Add(new Tuple<string, string>(room.Id, room.name));
            }
            return res;
        }

        [HttpGet("messages/{roomid}/{username}")]
        public async Task<List<Message>> GetMessages(string roomid, string username)
        {
            var filter = Builders<Message>.Filter.Empty;
            var task = memoryService.ChatRooms[roomid].GetCollection<Message>(username).DeleteManyAsync(filter);
            var res = memoryService.RoomMessages[roomid].Find(filter).ToList();
            res.Sort();
            foreach (Message m in res)
            {
                m.content = encryptService.Decrypt(m.content);
            }
            await task;
            if (res is not null)
            {
                return res;
            }
            return res;
        }

        [HttpGet("new-message/{roomid}/{username}")]
        public Message GetNewMessage(string roomid, string username)
        {
            var update = Builders<Message>.Update.Set("received", true);
            var myinbox = memoryService.ChatRooms[roomid].GetCollection<Message>(username);
            var res = myinbox.Find(s => !s.received).FirstOrDefault();
            if (res is not null)
            {
                res.content = encryptService.Decrypt(res.content);
                var filterupdate = Builders<Message>.Filter.Eq("_id", res.Id);
                myinbox.UpdateOneAsync(filterupdate, update);
            }
            return res;
        }

        [HttpPost("load-messages/{username}/{roomid}")]
        public async Task<Tuple<List<Message>, DateTime>> LoadMessages(string roomid, string username, [FromBody] DateTime checkpoint)
        {
            var filter = Builders<Message>.Filter.Empty;
            var task = memoryService.ChatRooms[roomid].GetCollection<Message>(username).DeleteManyAsync(filter);
            var res = memoryService.RoomMessages[roomid].Find(s => s.time < checkpoint).ToList();
            res.Sort();
            foreach (Message m in res)
            {
                m.content = encryptService.Decrypt(m.content);
            }
            await task;
            if (res is not null)
            {
                res = res.GetRange(0, 51);
                return new Tuple<List<Message>, DateTime>(res, res[0].time);
            }
            return new Tuple<List<Message>, DateTime>(res, DateTime.Now);
        }

        [HttpPost("inject-memory")]
        public void InjectRoomToMemory([FromBody] string roomid)
        {
            if (!memoryService.ChatRooms.ContainsKey(roomid))
            {
                memoryService.ChatRooms[roomid] = client.GetDatabase(roomid);
                memoryService.RoomAccounts[roomid] = memoryService.ChatRooms[roomid].GetCollection<Account>("accounts");
                memoryService.RoomMessages[roomid] = memoryService.ChatRooms[roomid].GetCollection<Message>("messages");
            }
        }

        [HttpPost("join-room/{username}/{roomid}")]
        public async Task<SignResult> JoinRoom(string roomid, string username, [FromBody] string password)
        {
            var filter = Builders<Room>.Filter.Eq("_id", roomid);
            var room = rooms.Find(filter).FirstOrDefault();
            if (room is not null)
            {
                if (!memoryService.ChatRooms.ContainsKey(room.Id))
                {
                    memoryService.ChatRooms[room.Id] = client.GetDatabase(room.Id);
                    memoryService.RoomAccounts[room.Id] = memoryService.ChatRooms[room.Id].GetCollection<Account>("accounts");
                    memoryService.RoomMessages[room.Id] = memoryService.ChatRooms[room.Id].GetCollection<Message>("messages");
                }
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
                    room.password = string.Empty;
                    var userfilter = Builders<Account>.Filter.Eq("_id", username);
                    var userupdate = Builders<Account>.Update.AddToSet<string>("rooms", roomid);
                    var update = Builders<Room>.Update.Set("users", room.users);
                    if (!room.state)
                    {
                        if (room.password == encryptService.Encrypt(password))
                        {
                            var usermesstask = memoryService.ChatRooms[roomid].CreateCollectionAsync(username);
                            var usertask = accounts.UpdateOneAsync(userfilter, userupdate);
                            room.users.Add(username);
                            var task = memoryService.RoomAccounts[roomid].InsertOneAsync(new Account() { username = username, connected = false });
                            var task1 = rooms.UpdateOneAsync(filter, update);
                            memoryService.ChatRooms[roomid].CreateCollection(username);
                            await usermesstask;
                            await usertask;
                            await task;
                            await task1;
                            return new SignResult() { success = true };
                        }
                        return new SignResult() { success = false, err = "Incorrect password" };
                    }
                    var usertask1 = accounts.UpdateOneAsync(userfilter, userupdate);
                    room.users.Add(username);
                    var task2 = memoryService.RoomAccounts[roomid].InsertOneAsync(new Account() { username = username, connected = false });
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
                if (!room.state)
                {
                    room.password = encryptService.Encrypt(room.password);
                }
                var task = rooms.InsertOneAsync(room);
                var roomdb = client.GetDatabase(room.Id);
                var task1 = roomdb.CreateCollectionAsync("messages");
                var task2 = roomdb.CreateCollectionAsync(username);
                roomdb.CreateCollection("accounts");
                memoryService.ChatRooms[room.Id] = roomdb;
                await task1;
                var task3 = Task.Factory.StartNew(() => { return roomdb.GetCollection<Message>("messages"); });
                var roomacc = roomdb.GetCollection<Account>("accounts");
                var roommess = await task3;
                memoryService.RoomAccounts[room.Id] = roomacc;
                memoryService.RoomMessages[room.Id] = roommess;
                roomacc.InsertOne(new Account() { username = username, connected = false });
                await usertask;
                await task;
                await task2;
                return new SignResult() { success = true };
            }
        }

        [HttpPost("send-message/{username}/{roomid}")]
        public async Task SendMessage(string username, string roomid, [FromBody] Message message)
        {
            message.Id = ObjectId.GenerateNewId().ToString();
            message.content = encryptService.Encrypt(message.content);
            var task = memoryService.RoomMessages[roomid].InsertOneAsync(message);
            var room = rooms.Find(s => s.Id == roomid).FirstOrDefault();
            var accs = room.users;
            accs.Remove(username);
            List<Task> tasks = new();
            Parallel.ForEach(accs, acc =>
            {
                var t = memoryService.ChatRooms[roomid].GetCollection<Message>(acc).InsertOneAsync(message);
                tasks.Add(t);
            });
            await task;
            Task.WaitAll(tasks.ToArray());
        }

        [HttpPut("typing/{username}/{roomid}")]
        public void UpdateTyping(string username, string roomid, [FromBody] bool typing)
        {
            var filter = Builders<Account>.Filter.Eq("_id", username);
            var update = Builders<Account>.Update.Set("typing", typing);
            memoryService.RoomAccounts[roomid].UpdateOne(filter, update);
        }
    }
}
