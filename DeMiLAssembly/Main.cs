using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

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
                Logger.Log($"Using version: {(ModManager.Instance.InstalledModInfos.Values.First(info => info.ID == "DeMiLService").Version)}");
            } else
            {
                Logger.Log("Not in game, cannot get Version.");
            }
            gameCommands = GetComponent<KMGameCommands>();
            inf = GetComponent<KMGameInfo>();
            server = new Server(gameCommands, inf);
            serverCoroutine = StartCoroutine(server.StartCoroutine());
            StartCoroutine(MultipleBombs.Refresh());
        }

        void OnEnable()
        {
            Logger.Log("Enabled");
            if (serverCoroutine != null) return;
            serverCoroutine = StartCoroutine(server.StartCoroutine());
            StartCoroutine(MultipleBombs.Refresh());
        }

        void OnDisable()
        {
            Logger.Log("Disabled");
            StopCoroutine(serverCoroutine);
        }
    }
}
