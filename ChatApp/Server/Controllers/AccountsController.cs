using ChatApp.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ChatApp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        EncryptionAndCompressService encryptService;
        MemoryDataAndEmailService memoryService;
        IMongoCollection<Account> accounts;

        public AccountsController(IMongoClient client, MemoryDataAndEmailService data, EncryptionAndCompressService encrypt)
        {
            var database = client.GetDatabase("ChatAppManagement");
            memoryService = data;
            encryptService = encrypt;
            accounts = database.GetCollection<Account>("accounts");
        }

        [HttpPost("sendcode/{username}")]
        public async Task SendVerificationEmailAsync(string username, [FromBody] string email)
        {
            if (memoryService.TimerReset.ContainsKey(username))
            {
                memoryService.TimerReset[username].Enabled = false;
                memoryService.TimerReset.Remove(username);
            }
            if (memoryService.ResetAccounts.ContainsKey(username))
            {
                memoryService.ResetAccounts.Remove(username);
            }
            await memoryService.SendEmailAsync(username, email);
        }

        [HttpPost("verify/{username}")]
        public SignResult VerifyCode(string username, [FromBody] int code)
        {
            if (memoryService.ResetAccounts.ContainsKey(username))
            {
                if (code == memoryService.ResetAccounts[username])
                {
                    memoryService.ResetAccounts.Remove(username);
                    memoryService.TimerReset[username].Enabled = false;
                    memoryService.TimerReset[username].Close();
                    memoryService.TimerReset.Remove(username);
                    return new SignResult() { success = true };
                }
                return new SignResult() { success = false, err = "Your verification code is incorrect" };
            }
            return new SignResult() { success = false, err = "Your verification code has expired" };
        }

        [HttpPost("verify-signup")]
        public async Task SignUpAfterVerify([FromBody] Account acc)
        {
            var filter = Builders<Account>.Filter.Eq("_id", acc.username);
            var update = Builders<Account>.Update.Set("connected", true);
            acc.password = encryptService.Encrypt(acc.password);
            var task2 = accounts.UpdateOneAsync(filter, update);
            var task3 = accounts.InsertOneAsync(acc);
            memoryService.OnlineTrack[acc.username] = new(600000);
            memoryService.OnlineTrack[acc.username].Elapsed += (s, e) =>
            {
                var filter = Builders<Account>.Filter.Eq("_id", acc.username);
                var update = Builders<Account>.Update.Set("connected", false);
                accounts.UpdateOne(filter, update);
                memoryService.OnlineTrack[acc.username].Stop();
                memoryService.OnlineTrack.Remove(acc.username);
                memoryService.OnlineTrack[acc.username].Close();
            };
            await task2;
            await task3;
        }

        [HttpPost("signin")]
        public async Task<SignResult> SignIn([FromBody] Account acc)
        {
            var filter = Builders<Account>.Filter.Eq("_id", acc.username);
            var update = Builders<Account>.Update.Set("connected", true);
            var myacc = accounts.Find(filter).FirstOrDefault();
            if (myacc is not null)
            {
                if (encryptService.Decrypt(myacc.password) == acc.password)
                {
                    if (myacc.connected)
                    {
                        return new SignResult() { success = false, err = "Your account has already been online" };
                    }
                    var task = accounts.UpdateOneAsync(filter, update);
                    memoryService.OnlineTrack[acc.username] = new(600000);
                    memoryService.OnlineTrack[acc.username].Elapsed += (s, e) =>
                    {
                        var filter = Builders<Account>.Filter.Eq("_id", acc.username);
                        var update = Builders<Account>.Update.Set("connected", false);
                        accounts.UpdateOne(filter, update);
                        memoryService.OnlineTrack[acc.username].Stop();
                        memoryService.OnlineTrack.Remove(acc.username);
                        memoryService.OnlineTrack[acc.username].Close();
                    };
                    await task;
                    return new SignResult() { success = true, avatar = myacc.avatar, rooms = myacc.rooms };
                }
                return new SignResult() { success = false, err = "Incorrect password" };
            }
            return new SignResult() { success = false, err = "Incorrect username" };
        }

        [HttpPost("signup")]
        public async Task<SignResult> SignUp([FromBody] Account acc)
        {
            var filter = Builders<Account>.Filter.Eq("_id", acc.username);
            var myacc = accounts.Find(filter).FirstOrDefault();
            if (myacc is not null)
            {
                return new SignResult() { success = false, err = "Your username has been taken" };
            }
            if (memoryService.TimerReset.ContainsKey(acc.username))
            {
                memoryService.TimerReset[acc.username].Enabled = false;
                memoryService.TimerReset.Remove(acc.username);
            }
            if (memoryService.ResetAccounts.ContainsKey(acc.username))
            {
                memoryService.ResetAccounts.Remove(acc.username);
            }
            await memoryService.SendEmailAsync(acc.username, acc.email);
            return new SignResult() { success = true };
        }

        [HttpPut("connection/{username}")]
        public async Task UpdateConnection(string username, [FromBody] bool connected)
        {
            if (connected && !memoryService.OnlineTrack.ContainsKey(username))
            {
                var filter = Builders<Account>.Filter.Eq("_id", username);
                var update = Builders<Account>.Update.Set("connected", true);
                accounts.UpdateOne(filter, update);
                memoryService.OnlineTrack[username] = new(600000);
                memoryService.OnlineTrack[username].Elapsed += async (s, e) =>
                {
                    var filter = Builders<Account>.Filter.Eq("_id", username);
                    var update = Builders<Account>.Update.Set("connected", false);
                    var update1 = Builders<Account>.Update.Set("typing", false);
                    var task = accounts.UpdateOneAsync(filter, update1);
                    accounts.UpdateOne(filter, update);
                    memoryService.OnlineTrack[username].Stop();
                    memoryService.OnlineTrack.Remove(username);
                    memoryService.OnlineTrack[username].Close();
                    await task;
                };
                return;
            }
            if (connected)
            {
                memoryService.OnlineTrack[username].Stop();
                memoryService.OnlineTrack[username].Close();
                memoryService.OnlineTrack[username] = new(600000);
                memoryService.OnlineTrack[username].Elapsed += async (s, e) =>
                {
                    var filter = Builders<Account>.Filter.Eq("_id", username);
                    var update = Builders<Account>.Update.Set("connected", false);
                    var update1 = Builders<Account>.Update.Set("typing", false);
                    var task = accounts.UpdateOneAsync(filter, update1);
                    accounts.UpdateOne(filter, update);
                    memoryService.OnlineTrack[username].Stop();
                    memoryService.OnlineTrack.Remove(username);
                    memoryService.OnlineTrack[username].Close();
                    await task;
                };
                return;
            }
            else
            {
                var filter = Builders<Account>.Filter.Eq("_id", username);
                var update = Builders<Account>.Update.Set("connected", false);
                var update1 = Builders<Account>.Update.Set("typing", false);
                var task = accounts.UpdateOneAsync(filter, update1);
                accounts.UpdateOne(filter, update);
                memoryService.OnlineTrack[username].Stop();
                memoryService.OnlineTrack[username].Close();
                memoryService.OnlineTrack.Remove(username);
                await task;
                return;
            }
        }
    }
}
