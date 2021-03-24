using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Shared
{
    public class SignResult
    {
        [JsonProperty("success")]
        public bool success { get; set; }
        [JsonProperty("err")]
        public string err { get; set; } = string.Empty;
    }
}
