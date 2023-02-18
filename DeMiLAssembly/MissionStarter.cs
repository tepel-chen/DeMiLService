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

        private State state;
        private void OnStateChange(State _state) => state = _state;
        private KMGameInfo inf;
        private KMGameCommands gameCommands;

        public MissionStarter(KMGameInfo inf, KMGameCommands gameCommands)
        {
            this.inf = inf;
            this.gameCommands = gameCommands;
            inf.OnStateChange += this.OnStateChange;
        }

        public Dictionary<string, object> StartMission(string missionId, string _seed, bool force = false)
        {
            string seed = _seed == null ? "-1" : _seed;

            if (state != State.Setup)
            {
                throw new Exception("You must be in the setup state to start a mission.");
            }

            Mission mission = MissionManager.Instance.GetMission(missionId);
            if (mission == null)
            {
                throw new Exception($"Mission not found: {missionId}");
            }

            Dictionary<string, object> detail;
            if (!force && !CanStartMission(mission, out detail))
            {
                return detail;
            } else
            {
                detail = new Dictionary<string, object>();
            }

            gameCommands.StartMission(missionId, seed);
            detail.Add("Seed", seed);
            return detail;
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
                            { "Maximum supported bombs count", MultipleBombs.GetMaximumBombCount() },
                            { "Mission modules count", missionDetail.BombCount },
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
                        { "Missing modules", missingMods }
                    };
            }
            if (moduleCount > inf.GetMaximumBombModules())
            {
                detail = new Dictionary<string, object>() {
                        { "MissionID", missionId },
                        { "Maximum supported modules count", inf.GetMaximumBombModules() },
                        { "Mission modules count", moduleCount },
                    };
                return false;
            }
            if (moduleCount > inf.GetMaximumModulesFrontFace() && mission.GeneratorSetting.FrontFaceOnly)
            {
                detail = new Dictionary<string, object>() {
                        { "MissionID", missionId },
                        { "Maximum supported frontface modules count", inf.GetMaximumModulesFrontFace() },
                        { "Mission modules count", moduleCount },
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
