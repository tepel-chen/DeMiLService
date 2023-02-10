using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Assets.Scripts.Mods;

namespace DeMiLService
{
    public class DeMiLService : MonoBehaviour
    {

        private KMGameCommands gameCommands;
        private KMGameInfo inf;

        private Server server;
        private Coroutine serverCoroutine;

        void Awake()
        {
            if (!Application.isEditor)
            {
                ModInfo info = ModManager.Instance.InstalledModInfos.Values.FirstOrDefault(info => info.ID == "DeMiLService");
                if (info != null)
                {
                    Logger.Log($"Using version: {(info.Version)}");
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
            server = new Server(gameCommands, inf);

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
