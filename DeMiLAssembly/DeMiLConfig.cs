using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DeMiLService
{
    [JsonObject]
    class DeMiLConfig
    {
        public int Port = 8095;
        public MissionPackAbstractData[] SteamIDs = { };

        public string[] IgnoredSteamIDs = { };

        [JsonIgnore]
        public static ModConfig<DeMiLConfig> loader = new ModConfig<DeMiLConfig>("DeMiLService");
        public static DeMiLConfig Read()
        {
            var original = loader.Read();
            foreach(var ids in original.SteamIDs)
            {
                ids.Port = original.Port;
            }
            return original;
        }
        public static void Write(DeMiLConfig config) => loader.Write(config);
    }
}
