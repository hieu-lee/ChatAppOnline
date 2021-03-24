using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Client.Services
{
    public class StateService
    {
        public bool logged { get; set; } = false;
        public string username { get; set; } = string.Empty;
        public byte[] avatar { get; set; }
        public HashSet<string> rooms { get; set; } = new();
    }
}
