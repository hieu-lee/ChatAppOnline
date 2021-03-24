using Newtonsoft.Json;
using System.Collections.Generic;

namespace ChatApp.Shared
{
    public class SignResult
    {
        [JsonProperty("success")]
        public bool success { get; set; }
        [JsonProperty("err")]
        public string err { get; set; }
        [JsonProperty("avatar")]
        public byte[] avatar { get; set; }
        [JsonProperty("rooms")]
        public HashSet<string> rooms { get; set; }
    }
}
