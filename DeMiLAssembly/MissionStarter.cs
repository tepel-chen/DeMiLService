using Assets.Scripts.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static KMGameInfo;

namespace DeMiLService
{
    public class MissionStarter
    {

        public State State;
        private void OnStateChange(State _state) => State = _state;
        private readonly KMGameInfo inf;
        private readonly KMGameCommands gameCommands;

        public MissionStarter(KMGameInfo inf, KMGameCommands gameCommands)
        {
            this.inf = inf;
            this.gameCommands = gameCommands;
            inf.OnStateChange += this.OnStateChange;
        }

        public Dictionary<string, object> StartMission(string missionId, string _seed, bool force = false)
        {
            string seed = _seed == null ? "-1" : _seed;
            Logger.Log($"Starting mission ID:{missionId}, seed:{seed}, force:{force}");

            if (State != State.Setup)
            {
                throw new Exception("You must be in the setup state to start a mission.");
            }

            Mission mission = MissionManager.Instance.GetMission(missionId);
            if (mission == null)
            {
                throw new Exception($"Mission not found: {missionId}");
            }

            if (!force && !CanStartMission(mission, out Dictionary<string, object> detail))
            {
                return detail;
            }
            else
            {
                detail = new Dictionary<string, object>
                {
                    { "MissionID", missionId }
                };
            }

            gameCommands.StartMission(missionId, seed);
            detail.Add("Seed", seed);
            return detail;
        }
        public Dictionary<string, object> StartMissionByName(string name, Mod mod, string _seed, bool force = false)
        {
            var searchStr = name.Trim();
            var missions = mod.GetValue<List<ModMission>>("missions").Select(mission => Tuple.Create(Localization.GetLocalizedString(mission.DisplayNameTerm).Trim(), mission));
            var fullMatch = missions.Where(missionTuple => missionTuple.Item1 == searchStr).Take(2).ToList();
            if(fullMatch.Count > 1)
            {
                throw new Exception($"Multiple mission with exact name {name} found!");
            }
            if(fullMatch.Count == 1)
            {
                return StartMission(fullMatch[0].Item2.ID, _seed, force);
            }
            var partialMatch = missions.Where(missionTuple => missionTuple.Item1.Contains(searchStr)).Take(2).ToList();
            if (partialMatch.Count > 1)
            {
                throw new Exception($"No mission with exact name found, but multiple mission patially matched {name} ({partialMatch.Select(m => m.Item1).Join(", ")})!");
            }
            if (partialMatch.Count == 1)
            {
                return StartMission(partialMatch[0].Item2.ID, _seed, force);
            }
            throw new Exception($"No mission with name {name} found!");
        }

        public bool CanStartMission(Mission mission, out Dictionary<string, object> detail)
        {
            var missionId = mission.ID;
            var isMultipleBombsInstalled = MultipleBombs.Installed();
            var availableMods = inf.GetAvailableModuleInfo().Where(x => x.IsMod).Select(y => y.ModuleId).ToList();
            var missingMods = new HashSet<string>();

            List<ComponentPool> componentPools;
            if (isMultipleBombsInstalled)
            {
                var missionDetail = MultipleBombsMissionDetails.ReadMission(mission);
                componentPools = missionDetail.GeneratorSettings.SelectMany(setting => setting.Value.ComponentPools).ToList();

                if (missionDetail.BombCount > MultipleBombs.GetMaximumBombCount())
                {
                    detail = new Dictionary<string, object>() {
                            { "MissionID", missionId },
                            { "MaximumSupportedBombsCount", MultipleBombs.GetMaximumBombCount() },
                            { "MissionModulesCount", missionDetail.BombCount },
                        };
                    return false;
                }
            }
            else
            {
                componentPools = mission.GeneratorSetting.ComponentPools;
            }

            int moduleCount = 0;
            foreach (var componentPool in componentPools)
            {
                moduleCount += componentPool.Count;
                var modTypes = componentPool.ModTypes;
                if (modTypes == null || modTypes.Count == 0) continue;
                foreach (string mod in modTypes.Where(x => !availableMods.Contains(x)))
                {
                    missingMods.Add(mod);
                }
            }
            if (missingMods.Count > 0)
            {
                detail = new Dictionary<string, object>() {
                        { "MissionID", missionId },
                        { "MissingModules", missingMods }
                    };
                return false;
            }
            if (moduleCount > inf.GetMaximumBombModules())
            {
                detail = new Dictionary<string, object>() {
                        { "MissionID", missionId },
                        { "MaximumSupportedModulesCount", inf.GetMaximumBombModules() },
                        { "MissionModulesCount", moduleCount },
                    };
                return false;
            }
            if (moduleCount > inf.GetMaximumModulesFrontFace() && mission.GeneratorSetting.FrontFaceOnly)
            {
                detail = new Dictionary<string, object>() {
                        { "MissionID", missionId },
                        { "MaximumSupportedFrontfaceModulesCount", inf.GetMaximumModulesFrontFace() },
                        { "MissionModulesCount", moduleCount },
                    };
                return false;
            }
            detail = new Dictionary<string, object>() {
                { "MissionID", missionId }
            };
            return true;
        }
    }
}
