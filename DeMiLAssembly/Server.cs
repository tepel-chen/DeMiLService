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
using Assets.Scripts.Mods;

namespace DeMiLService
{
    internal class Server
    {
        string Prefix => $"http://*:{config.Port}/";
        private readonly HttpListener listener;
        private readonly DeMiLConfig config;
        private readonly MissionStarter missionStarter;

        private readonly Queue<IEnumerator<object>> coroutineQueue;

        internal bool IsRunning => listener != null && listener.IsListening;

        internal Server(KMGameCommands gameCommands, KMGameInfo inf)
        {
            config = DeMiLConfig.Read();
            listener = new HttpListener();
            listener.Prefixes.Add(Prefix);
            missionStarter = new MissionStarter(inf, gameCommands);
            coroutineQueue = new Queue<IEnumerator<object>>();
        }

        internal IEnumerator<object> StartCoroutine()
        {
            listener.Start();
            Logger.Log($"Server ready on ${listener.Prefixes.ElementAt(0)}");
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

            if(context.Request.Url.Host != "localhost")
            {
                coroutineQueue.Enqueue(Send(new string[] { "This url is only accessable from localhost"}, context));
                return;
            }

            Logger.Log($"Recieved request {context.Request.Url.OriginalString}");
            coroutineQueue.Enqueue(Send(SwitchURL(context), context));
        }

        private IEnumerable<object> SwitchURL(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;

            if (request.Url.Segments.Length < 2)
            {
                return RunDefault();
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
            if (request.Url.Segments[1].Contains("tocDetail"))
            {
                return RunGetToCInfo(context);
            }
            if (request.Url.Segments[1].Contains("missions"))
            {
                return RunGetMissions(context);
            }
            if (request.Url.Segments[1].Contains("saveAndDisable"))
            {
                return RunSaveAndDisableMissions(context);
            }
            if (request.Url.Segments[1].Contains("version"))
            {
                return RunVersion();
            }
            return Run404();

        }
        private IEnumerable<object> RunDefault()
        {
            yield return new Dictionary<string, object>() {
                    { "TODO", "Build main page" }
                };
        }
        private IEnumerable<object> Run404()
        {
            throw new Exception("Not found.");
        }

        private IEnumerable<object> RunVersion()
        {
            if (!Application.isEditor)
            {
                Logger.Log(JsonConvert.SerializeObject(ModManager.Instance.InstalledModInfos.Values));
                ModInfo info = ModManager.Instance.InstalledModInfos.Values.FirstOrDefault(info => info.ID == "DeMiLService");
                if (info != null)
                {
                    yield return new Dictionary<string, object>() {
                        { "Version", info.Version }
                    };
                    yield break;
                }
            }
            throw new Exception("Cannot get Version");
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

            yield return missionStarter.StartMission(missionId, seed, force);
        }

        private IEnumerable<object> RunLoadMission(HttpListenerContext context)
        {
            string steamId = context.Request.QueryString.Get("steamId");
            bool refreshBinder = (context.Request.QueryString.Get("refreshBinder")?.ToLower() ?? "true") != "false";

            if (steamId != null)
            {
                yield return MissionLoader.LoadMission(steamId);
                if (refreshBinder) BinderRefresher.Refresh();
                yield return new Dictionary<string, string>() {
                    { "Loaded mission", steamId }
                };
            } else
            {
                throw new Exception("You must specify steamID");
            }
        }

        private IEnumerable<object> RunGetMissionInfo(HttpListenerContext context)
        {
            string steamId = context.Request.QueryString.Get("steamId");
            bool refreshBinder = (context.Request.QueryString.Get("refreshBinder")?.ToLower() ?? "true") != "false";

            if (steamId != null)
            {
                yield return MissionLoader.LoadMission(steamId);

                if (refreshBinder) BinderRefresher.Refresh();

                if (MissionLoader.LoadedMods.TryGetValue(MissionLoader.GetModPath(steamId), out Mod mod)) {
                    var data = MissionPackData.GetMissionData(steamId, mod, config.Port);
                    yield return data;
                } else
                {
                    throw new Exception($"Mod ${steamId} not found");
                }
            }
            else
            {
                throw new Exception("You must specify steamID");
            }
        }
        private IEnumerable<object> RunGetToCInfo(HttpListenerContext context)
        {
            string steamId = context.Request.QueryString.Get("steamId");
            bool refreshBinder = (context.Request.QueryString.Get("refreshBinder")?.ToLower() ?? "true") != "false";

            if (steamId != null)
            {
                yield return MissionLoader.LoadMission(steamId);

                if (refreshBinder) BinderRefresher.Refresh();

                if (MissionLoader.LoadedMods.TryGetValue(MissionLoader.GetModPath(steamId), out Mod mod))
                {
                    var data = MissionPackData.GetMissionData(steamId, mod, config.Port, true);
                    yield return data;
                }
                else
                {
                    throw new Exception($"Mod ${steamId} not found");
                }
            }
            else
            {
                throw new Exception("You must specify steamID");
            }
        }
        private IEnumerable<object> RunGetMissions(HttpListenerContext context)
        {
            var missionAbstract = MissionLoader.LoadedMods.AsEnumerable()
                .Where(v => MissionLoader.IsMissionMod(v.Value))
                .Select(v => MissionPackAbstractData.GetMissionData(Path.GetFileName(v.Key), v.Value, config.Port));
            var data = config.SteamIDs.Concat(missionAbstract).Distinct();

            yield return data;
        }

        private IEnumerable<object> RunSaveAndDisableMissions(HttpListenerContext context)
        {

            if (missionStarter.State != State.Setup)
            {
                throw new Exception("You must be in the setup state to save and disable mods.");
            }

            var missionAbstract = MissionLoader.LoadedMods.AsEnumerable()
                .Where(v => MissionLoader.IsMissionMod(v.Value))
                .Select(v => MissionPackAbstractData.GetMissionData(Path.GetFileName(v.Key), v.Value, config.Port));

            foreach (var d in missionAbstract)
            {
                if (config.IgnoredSteamIDs.Contains(d.SteamID)) continue;
                MissionLoader.DisableMod(d.SteamID);
            }
            yield return MissionLoader.EnterAndLeaveModManager();
            config.SteamIDs = config.SteamIDs.Concat(missionAbstract).Distinct().ToArray();

            DeMiLConfig.Write(config);

            yield return new Dictionary<string, object>() {
                { "Saved missions", missionAbstract }
            };
        }

        private IEnumerator<object> Send(IEnumerable<object> s, HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;
            Stream outputStream = response.OutputStream;

            response.AddHeader("Access-Control-Allow-Origin", "*");

            IEnumerator<object> e = Utils.FlattenThrowableIEnumerator(s.GetEnumerator());
            object last = null;
            while (e.MoveNext())
            {
                if(e.Current is Exception ex)
                {
                    last = new Dictionary<string, object>()
                    {
                        {"ERROR", ex.Message },
                        {"Stacktrace", ex.StackTrace }
                    };
                    yield return e.Current;
                    break;
                }
                last = e.Current;
                yield return e.Current;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(last));
            try
            {

                outputStream.Write(bytes, 0, bytes.Length);
                outputStream.Close();
            } catch(Exception error)
            {
                Logger.Log("Something wrong happened...");
                Debug.LogError(error);
            }
        }
    }
}
