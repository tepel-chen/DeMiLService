using Assets.Scripts.Missions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DeMiLService
{
    public class MultipleBombsMissionDetails
    {
        public int BombCount { get; set; } = 1;
        public Dictionary<int, GeneratorSetting> GeneratorSettings { get; set; } = new Dictionary<int, GeneratorSetting>();

        public MultipleBombsMissionDetails()
        {

        }

        public MultipleBombsMissionDetails(int bombCount, GeneratorSetting firstBombGeneratorSetting)
        {
            BombCount = bombCount;
            GeneratorSettings.Add(0, firstBombGeneratorSetting);
        }

        public static MultipleBombsMissionDetails ReadMission(Mission mission)
        {
            return ReadMission(mission, false, out _);
        }

        public static MultipleBombsMissionDetails ReadMission(Mission mission, bool removeComponentPools, out List<ComponentPool> multipleBombsComponentPools)
        {
            MultipleBombsMissionDetails missionDetails = new MultipleBombsMissionDetails();
            multipleBombsComponentPools = new List<ComponentPool>();
            if (mission.GeneratorSetting != null)
            {
                GeneratorSetting generatorSetting = UnityEngine.Object.Instantiate(mission).GeneratorSetting;
                missionDetails.GeneratorSettings.Add(0, generatorSetting);
                if (generatorSetting.ComponentPools != null)
                {
                    for (int i = generatorSetting.ComponentPools.Count - 1; i >= 0; i--)
                    {
                        ComponentPool pool = generatorSetting.ComponentPools[i];
                        if (pool.ModTypes != null && pool.ModTypes.Count == 1)
                        {
                            if (pool.ModTypes[0] == "Multiple Bombs")
                            {
                                missionDetails.BombCount += pool.Count;
                                generatorSetting.ComponentPools.RemoveAt(i);
                                multipleBombsComponentPools.Add(mission.GeneratorSetting.ComponentPools[i]);
                                if (removeComponentPools)
                                    mission.GeneratorSetting.ComponentPools.RemoveAt(i);
                            }
                            else if (pool.ModTypes[0].StartsWith("Multiple Bombs:"))
                            {
                                string[] strings = pool.ModTypes[0].Split(new char[] { ':' }, 3);
                                if (strings.Length != 3)
                                    continue;
                                int bombIndex;
                                if (!int.TryParse(strings[1], out bombIndex))
                                    continue;
                                if (missionDetails.GeneratorSettings.ContainsKey(bombIndex))
                                    continue;
                                GeneratorSetting bombGeneratorSetting;
                                try
                                {
                                    bombGeneratorSetting = ModMission.CreateGeneratorSettingsFromMod(JsonConvert.DeserializeObject<KMGeneratorSetting>(strings[2]));
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                                missionDetails.GeneratorSettings.Add(bombIndex, bombGeneratorSetting);
                                generatorSetting.ComponentPools.RemoveAt(i);
                                multipleBombsComponentPools.Add(mission.GeneratorSetting.ComponentPools[i]);
                                if (removeComponentPools)
                                    mission.GeneratorSetting.ComponentPools.RemoveAt(i);
                            }
                        }
                    }
                }
            }
            else
            {
                missionDetails.GeneratorSettings.Add(0, null);
            }
            return missionDetails;
        }

        public void GetMissionInfo(out float maxTime, out int totalModules, out int totalStrikes)
        {
            maxTime = 0;
            totalModules = 0;
            totalStrikes = 0;
            for (int i = 0; i < BombCount; i++)
            {
                GeneratorSetting generatorSetting;
                if (GeneratorSettings.TryGetValue(i, out generatorSetting))
                {
                    if (generatorSetting.TimeLimit > maxTime)
                        maxTime = generatorSetting.TimeLimit;
                    totalModules += generatorSetting.GetComponentCount();
                    totalStrikes += generatorSetting.NumStrikes;
                }
                else
                {
                    totalModules += GeneratorSettings[0].GetComponentCount();
                    totalStrikes += GeneratorSettings[0].NumStrikes;
                }
            }
        }
    }
}
