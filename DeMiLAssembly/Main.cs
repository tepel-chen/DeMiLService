using System.Linq;
using UnityEngine;
using Assets.Scripts.Mods;

namespace DeMiLService
{
    public class DeMiLService : MonoBehaviour
    {

        private KMGameCommands gameCommands;
        private KMGameInfo inf;
        private KMAudio audio;

        private Server server;
        private Coroutine serverCoroutine;

        void Awake()
        {
            if (!Application.isEditor)
            {
                var info = ModManager
                .Instance
                .InstalledModInfos
                .Values
                .Cast<object>()
                .FirstOrDefault(x => ((ModInfo)x).ID is "AdvancedMissionGenerator");
                if (info != null)
                {
                    Logger.Log($"Using version: {(((ModInfo)info).Version)}");
                }
                else
                    Logger.Log("Cannot get Version.");
            }
            else
            {
                Logger.Log("Cannot get Version.");
            }
            gameCommands = GetComponent<KMGameCommands>();
            inf = GetComponent<KMGameInfo>();
            audio = GetComponent<KMAudio>();
            server = new Server(gameCommands, inf, audio, gameObject.transform);

        }
        void OnEnable()
        {
            Logger.Log("Enabled");
            if (server.IsRunning) return;
            serverCoroutine = StartCoroutine(server.StartCoroutine());
            StartCoroutine(MultipleBombs.Refresh());
        }

        void OnDisable()
        {
            Logger.Log("Disabled");
            if (serverCoroutine != null)
            {
                StopCoroutine(serverCoroutine);
            }
            server.Close();

        }
    }
}
