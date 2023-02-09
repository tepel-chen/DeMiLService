using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeMiLService
{
    [JsonObject]
    class MissionPackData
    {
        [JsonIgnore]
        public int Port;

        public string SteamID;
        public string ModID;
        public string Title;
        public MissionData[] Missions;
        public string LoadURL => $"http://localhost:{Port}/loadMission?steamID={SteamID}";

        public static MissionPackData GetMissionData(string steamID, Mod mod, int port)
        {
            return new MissionPackData
            {
                Port = port,
                SteamID = steamID,
                ModID = mod.ModID,
                Title = mod.Title,
                Missions = mod.GetValue<List<ModMission>>("missions")
                .Select(mission =>
                {
                    var m = new MissionData
                    {
                        SteamID = steamID,
                        MissionID = mission.ID,
                        Title = Localization.GetLocalizedString(mission.DisplayNameTerm),
                        Description = Localization.GetLocalizedString(mission.DescriptionTerm),
                        Port = port
                    };
                    return m;
                }).ToArray()
            };
        }
    }

    [JsonObject]
    class MissionData
    {
        [JsonIgnore]
        public string SteamID;
        [JsonIgnore]
        public int Port;

        public string Title;
        public string MissionID;
        public string Description;
        public string StartURL => $"http://localhost:{Port}/startMission?steamID={SteamID}&missionID={MissionID}";
    }


    [JsonObject]
    class MissionPackAbstractData : IEquatable<MissionPackAbstractData>
    {
        [JsonIgnore]
        public int Port;

        public string SteamID;
        public string ModID;
        public string Title;
        public string LoadURL => $"http://localhost:{Port}/loadMission?steamID={SteamID}";
        public string DetailURL => $"http://localhost:{Port}/missionDetail?steamID={SteamID}";

        public static MissionPackAbstractData GetMissionData(string steamID, Mod mod, int port)
        {
            return new MissionPackAbstractData
            {
                Port = port,
                SteamID = steamID,
                ModID = mod.ModID,
                Title = mod.Title
            };
        }

        public bool Equals(MissionPackAbstractData other) => SteamID == other.SteamID;
        public override int GetHashCode() => SteamID.GetHashCode();
    }
}
