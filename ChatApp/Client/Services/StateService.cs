using System.Collections.Generic;
using System.Net.Http;
using System.Timers;

namespace ChatApp.Client.Services
{
    public class StateService
    {
        public bool logged { get; set; } = false;
        public string username { get; set; } = string.Empty;
        public byte[] avatar { get; set; }
        public HashSet<string> rooms { get; set; } = new();
        public string roomid { get; set; }
        public Timer timer = new(100000);
        public int chatAreaHeight { get; set; }
        public HttpClient Http { get; set; }
        public Timer updateTimer { get; set; } = new(250);
        public StateService(HttpClient Http)
        {
            this.Http = Http;
            timer.Elapsed += async (s, e) =>
            {
                if (logged)
                {
                    await Http.PutAsJsonAsync<bool>($"Accounts/connection/{username}", true);
                    return;
                }
                else
                {
                    timer.Enabled = false;
                    timer.Close();
                }
            };
        }
    }
}
