using Assets.Scripts.Missions;
using Assets.Scripts.Mods.Mission;
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
        public ToCData[] ToCs;
        public string LoadURL => $"http://localhost:{Port}/loadMission?steamID={SteamID}";

        public static MissionPackData GetMissionData(string steamID, Mod mod, int port, bool getToCData = false)
        {
             
            var missions = mod.GetValue<List<ModMission>>("missions");
            var tocs = mod.GetValue<List<ModTableOfContentsMetaData>>("tocs");
            var missionData = missions
                .Select(mission =>
                {
                    var multipleMission = MultipleBombsMissionDetails.ReadMission(mission);
                    return new MissionData
                    {
                        SteamID = steamID,
                        MissionID = mission.ID,
                        Title = Localization.GetLocalizedString(mission.DisplayNameTerm),
                        Description = Localization.GetLocalizedString(mission.DescriptionTerm),
                        BombCount = multipleMission.BombCount,
                        BombData = Enumerable.Range(0, multipleMission.BombCount).Select(i =>
                        {
                            if (multipleMission.GeneratorSettings.TryGetValue(i, out GeneratorSetting setting))
                            {
                                return BombData.GetBombData(setting);
                            }
                            return null;
                        }).ToArray(),
                        FactoryMode = FactoryMission.GetGameMode(mission),
                        Port = port
                    };
                });


            return new MissionPackData
            {
                Port = port,
                SteamID = steamID,
                ModID = mod.ModID,
                Title = mod.Title,
                ToCs = getToCData ? ToCData.GetToCData(missionData, tocs) : null,
                Missions = !getToCData ? missionData.ToArray() : null 
            };
        }
    }

    [JsonObject]
    class ToCData
    {
        public string Title;
        public SectionData[] Sections;

        public static ToCData[] GetToCData(IEnumerable<MissionData> missionData, List<ModTableOfContentsMetaData> tocs)
        {
            return tocs.Select(toc =>
            {
                return new ToCData()
                {
                    Title = Localization.GetLocalizedString(toc.DisplayNameTerm ?? ""),
                    Sections = toc.Sections
                        .Select(sectionMetaData =>
                        {
                            return new SectionData()
                            {
                                Title = Localization.GetLocalizedString(sectionMetaData.TitleTerm ?? ""),
                                SectionNum = sectionMetaData.SectionNum,
                                Missions = sectionMetaData.MissionIDs
                                    .Select(id => missionData.FirstOrDefault(data => data.MissionID == id))
                                    .Where(data => data is MissionData)
                                    .ToArray()
                            };
                        })
                        .Where(section => section.Missions.Length > 0)
                        .ToArray()
                };
            }).ToArray();
        }
    }

    [JsonObject]
    class SectionData
    {

        public string Title;
        public int SectionNum;
        public MissionData[] Missions;
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
        public string FactoryMode;
        public BombData[] BombData;
        public int BombCount;
        public string StartURL => $"http://localhost:{Port}/startMission?steamID={SteamID}&missionID={MissionID}";
    }

    [JsonObject]
    class BombData
    {
        public float TimeLimit;
        public int NumStrikes;
        public int TimeBeforeNeedyActivation;
        public bool FrontFaceOnly;
        public int OptionalWidgetCount;
        public string[][] ComponentPools;
        public static BombData GetBombData(GeneratorSetting generator)
        {
            return new BombData
            {
                TimeLimit = generator.TimeLimit,
                NumStrikes = generator.NumStrikes,
                TimeBeforeNeedyActivation = generator.TimeBeforeNeedyActivation,
                FrontFaceOnly = generator.FrontFaceOnly,
                OptionalWidgetCount = generator.OptionalWidgetCount,
                ComponentPools = generator.ComponentPools.SelectMany(pool =>
                {
                    string specialComponent = GetSpecialComponent(pool.SpecialComponentType, pool.AllowedSources);
                    if (specialComponent != null)
                    {
                        return Enumerable.Repeat(new string[] { specialComponent }, pool.Count);
                    }
                    List<string> component = pool.ComponentTypes.Select(c => c.ToString()).ToList();
                    component.AddRange(pool.ModTypes);
                    return Enumerable.Repeat(component.ToArray(), pool.Count);

                }).ToArray()
            };
        }

        static string GetSpecialComponent(SpecialComponentTypeEnum type, ComponentPool.ComponentSource source)
        {
            if (type == SpecialComponentTypeEnum.ALL_NEEDY && source == (ComponentPool.ComponentSource.Base | ComponentPool.ComponentSource.Mods))
            {
                return "ALL_NEEDY";
            }
            if (type == SpecialComponentTypeEnum.ALL_SOLVABLE && source == (ComponentPool.ComponentSource.Base | ComponentPool.ComponentSource.Mods))
            {
                return "ALL_SOLVABLE";
            }
            if (type == SpecialComponentTypeEnum.ALL_NEEDY && source == ComponentPool.ComponentSource.Base)
            {
                return "ALL_VANILLA_NEEDY";
            }
            if (type == SpecialComponentTypeEnum.ALL_SOLVABLE && source == ComponentPool.ComponentSource.Base)
            {
                return "ALL_VANILLA";
            }
            if (type == SpecialComponentTypeEnum.ALL_NEEDY && source == ComponentPool.ComponentSource.Mods)
            {
                return "ALL_MODS_NEEDY";
            }
            if (type == SpecialComponentTypeEnum.ALL_SOLVABLE && source == ComponentPool.ComponentSource.Mods)
            {
                return "ALL_MODS";
            }
            return null;
        }
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
