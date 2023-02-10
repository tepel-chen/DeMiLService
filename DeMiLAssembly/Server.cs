using Assets.Scripts.Missions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using static KMGameInfo;

namespace DeMiLService
{
    internal class Server
    {
        string Prefix => $"http://*:{config.Port}/";
        private readonly HttpListener listener;
        private readonly KMGameCommands gameCommands;
        private readonly KMGameInfo inf;
        private readonly DeMiLConfig config;
        private State state;

        private readonly Queue<IEnumerator<object>> coroutineQueue;

        internal bool IsRunning => listener != null && listener.IsListening;

        internal Server(KMGameCommands gameCommands, KMGameInfo inf)
        {
            config = DeMiLConfig.Read();
            listener = new HttpListener();
            listener.Prefixes.Add(Prefix);
            this.gameCommands = gameCommands;
            this.inf = inf;
            inf.OnStateChange += OnStateChange;
            coroutineQueue = new Queue<IEnumerator<object>>();
        }

        internal IEnumerator<object> StartCoroutine()
        {
            listener.Start();
            Debug.Log($"[DeMiLService] Server ready on ${listener.Prefixes.ElementAt(0)}");
            while (true)
            {
                var result = listener.BeginGetContext(new AsyncCallback(Handler), listener);

                while (!result.IsCompleted)
                {
                    if (coroutineQueue.Count > 0)
                    {
                        IEnumerator<object> e = coroutineQueue.Dequeue();
                        while (e.MoveNext())
                        {
                            yield return e.Current;
                        }
                    }
                    yield return null;
                }

                result.AsyncWaitHandle.WaitOne();
                yield return null;
            }
        }

        internal void Close()
        {
            if (!IsRunning) return;
            listener.Stop();
        }

        private void Handler(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);

            Logger.Log($"Recieved request {context.Request.Url.OriginalString}");
            coroutineQueue.Enqueue(Send(SwitchURL(context), context));
        }

        internal void OnStateChange(State _state) => state = _state;

        private IEnumerable<object> SwitchURL(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;

            if (request.Url.Segments.Length < 2)
            {
                return new object[] { RunDefault() };
            }
            if (request.Url.Segments[1].Contains("startMission"))
            {
                return RunStartMission(context);
            }
            if (request.Url.Segments[1].Contains("loadMission"))
            {
                return RunLoadMission(context);
            }
            if (request.Url.Segments[1].Contains("missionDetail"))
            {
                return RunGetMissionInfo(context);
            }
            if (request.Url.Segments[1].Contains("missions"))
            {
                return RunGetMissions(context);
            }
            if (request.Url.Segments[1].Contains("saveAndDisable"))
            {
                return RunSaveAndDisableMissions(context);
            }
            return new object[] { Run404() };

        }
        private string RunDefault()
        {
            var data = new Dictionary<string, object>() {
                    { "TODO", "Build main page" }
                };
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        private string Run404()
        {
            var data = new Dictionary<string, object>() {
                    { "ERROR", "Not found." }
                };
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        private IEnumerable<object> RunStartMission(HttpListenerContext context)
        {
            string steamId = context.Request.QueryString.Get("steamId");
            string missionId = context.Request.QueryString.Get("missionId");
            string seed = context.Request.QueryString.Get("seed");
            bool force = (context.Request.QueryString.Get("force")?.ToLower() ?? "false") == "true";

            if (steamId != null)
            {
                yield return MissionLoader.LoadMission(steamId);
            }

            yield return StartMission(missionId, seed, force);
        }

        private IEnumerable<object> RunLoadMission(HttpListenerContext context)
        {
            string steamId = context.Request.QueryString.Get("steamId");

            if (steamId != null)
            {
                yield return MissionLoader.LoadMission(steamId);
                var pageManager = UnityEngine.Object.FindObjectOfType<SetupRoom>().BombBinder.MissionTableOfContentsPageManager;
                pageManager.ForceFullRefresh();
                var data = new Dictionary<string, string>() {
                    { "Loaded mission", steamId }
                };
                yield return JsonConvert.SerializeObject(data, Formatting.Indented);
            } else
            {
                var data = new Dictionary<string, object>() {
                    { "ERROR", "You must specify steamID" }
                };
                yield return JsonConvert.SerializeObject(data, Formatting.Indented);
            }
        }

        private IEnumerable<object> RunGetMissionInfo(HttpListenerContext context)
        {
            string steamId = context.Request.QueryString.Get("steamId");

            if (steamId != null)
            {
                yield return MissionLoader.LoadMission(steamId);
                if (MissionLoader.loadedMods.TryGetValue(MissionLoader.GetModPath(steamId), out Mod mod)) {
                    var data = MissionPackData.GetMissionData(steamId, mod, config.Port);
                    yield return JsonConvert.SerializeObject(data, Formatting.Indented);
                } else
                {
                    var data = new Dictionary<string, object>() {
                        { "ERROR", $"Mod ${steamId} not found" }
                    };
                    yield return JsonConvert.SerializeObject(data, Formatting.Indented);
                }
            }
            else
            {
                var data = new Dictionary<string, object>() {
                    { "ERROR", "You must specify steamID" }
                };
                yield return JsonConvert.SerializeObject(data, Formatting.Indented);
            }
        }
        private IEnumerable<object> RunGetMissions(HttpListenerContext context)
        {
            var missionAbstract = MissionLoader.loadedMods.AsEnumerable()
                .Where(v => MissionLoader.IsMissionMod(v.Value))
                .Select(v => MissionPackAbstractData.GetMissionData(Path.GetFileName(v.Key), v.Value, config.Port));
            var data = config.SteamIDs.Concat(missionAbstract).Distinct();

            yield return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        private IEnumerable<object> RunSaveAndDisableMissions(HttpListenerContext context)
        {
            var missionAbstract = MissionLoader.loadedMods.AsEnumerable()
                .Where(v => MissionLoader.IsMissionMod(v.Value))
                .Select(v => MissionPackAbstractData.GetMissionData(Path.GetFileName(v.Key), v.Value, config.Port));

            foreach (var d in missionAbstract)
            {
                MissionLoader.DisableMod(d.SteamID);
            }
            MissionLoader.FlushDisabledMods();
            config.SteamIDs = config.SteamIDs.Concat(missionAbstract).Distinct().ToArray();

            DeMiLConfig.Write(config);

            var data = new Dictionary<string, object>() {
                { "Saved missions", missionAbstract }
            };

            yield return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        private IEnumerator<object> Send(IEnumerable<object> s, HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;
            Stream outputStream = response.OutputStream;

            response.AddHeader("Access-Control-Allow-Origin", "*");

            IEnumerator<object> e = Utils.FlattenThrowableIEnumerator(s.GetEnumerator());
            string last = "";
            while (e.MoveNext())
            {
                if(e.Current is Exception ex)
                {
                    last = ex.ToString();
                    yield return e.Current;
                    break;
                }
                if (e.Current.GetType() == typeof(string)) last = (string)e.Current;
                yield return e.Current;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(last);


            outputStream.Write(bytes, 0, bytes.Length);
            outputStream.Close();
        }

        protected string StartMission(string missionId, string _seed, bool force = false)
        {
            string seed = _seed == null ? "-1" : _seed;
            Dictionary<string, object> data;

            if (state != State.Setup)
            {
                data = new Dictionary<string, object>() {
                    { "ERROR", "You must be in the setup state to start a mission." }
                };
                return JsonConvert.SerializeObject(data, Formatting.Indented);

            }

            Mission mission = MissionManager.Instance.GetMission(missionId);
            if (mission == null)
            {
                data = new Dictionary<string, object>() {
                    { "ERROR", $"Mission not found: {missionId}" }
                };
                return JsonConvert.SerializeObject(data, Formatting.Indented);
            }

            if(!force)
            {
                var isMultipleBombsInstalled = MultipleBombs.Installed();
                var availableMods = inf.GetAvailableModuleInfo().Where(x => x.IsMod).Select(y => y.ModuleId).ToList();
                var missingMods = new HashSet<string>();

                List<ComponentPool> componentPools;
                if(isMultipleBombsInstalled)
                {
                    var missionDetail = MultipleBombsMissionDetails.ReadMission(mission);
                    componentPools = missionDetail.GeneratorSettings.SelectMany(setting => setting.Value.ComponentPools).ToList();

                    if (missionDetail.BombCount > MultipleBombs.GetMaximumBombCount())
                    {
                        data = new Dictionary<string, object>() {
                            { "MissionID", missionId },
                            { "Maximum supported bombs count", MultipleBombs.GetMaximumBombCount() },
                            { "Mission modules count", missionDetail.BombCount },
                        };
                        return JsonConvert.SerializeObject(data, Formatting.Indented);

                    }
                } else
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
                    data = new Dictionary<string, object>() {
                        { "MissionID", missionId },
                        { "Missing modules", missingMods }
                    };
                    return JsonConvert.SerializeObject(data, Formatting.Indented);
                }
                if (moduleCount > inf.GetMaximumBombModules())
                {
                    data = new Dictionary<string, object>() {
                        { "MissionID", missionId },
                        { "Maximum supported modules count", inf.GetMaximumBombModules() },
                        { "Mission modules count", moduleCount },
                    };
                    return JsonConvert.SerializeObject(data, Formatting.Indented);
                }
                if (moduleCount > inf.GetMaximumModulesFrontFace() && mission.GeneratorSetting.FrontFaceOnly)
                {
                    data = new Dictionary<string, object>() {
                        { "MissionID", missionId },
                        { "Maximum supported frontface modules count", inf.GetMaximumModulesFrontFace() },
                        { "Mission modules count", moduleCount },
                    };
                    return JsonConvert.SerializeObject(data, Formatting.Indented);

                }
            }

            gameCommands.StartMission(missionId, seed);

            data = new Dictionary<string, object>() {
                { "MissionID", missionId },
                { "Seed", seed }
            };
            return JsonConvert.SerializeObject(data, Formatting.Indented);

        }
    }
}
