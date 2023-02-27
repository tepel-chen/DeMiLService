
using Assets.Scripts.Services;
using Assets.Scripts.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using static Assets.Scripts.Mods.ModInfo;

namespace DeMiLService
{
	static class MissionLoader
	{
		public static string SteamDirectory;
		public static string SteamWorkshopDirectory
			=> SteamDirectory == null ?
				null :
				Path.GetFullPath(new[] { SteamDirectory, "steamapps", "workshop", "content", "341800" }.Aggregate(Path.Combine));
		public static readonly Dictionary<string, Mod> LoadedMods = ModManager.Instance.GetValue<Dictionary<string, Mod>>("loadedMods");
		private static readonly Regex matchSteamID = new Regex(@"^\d+$");
		private static MethodInfo dblEnterAndLeaveModManagerMethod;
		static MissionLoader()
		{
			SteamDirectory = FindSteamDirectory();
		}

		private static string FindSteamDirectory()
		{
			// Mod folders
			var folders = AbstractServices.Instance.GetModFolders();
			if (folders.Count != 0)
			{
				return folders[0] + "/../../../../..";
			}

			// Relative to the game
			var relativePath = Path.GetFullPath("./../..");
			if (new DirectoryInfo(relativePath).Name == "steamapps")
			{
				return Path.GetFullPath("./../../..");
			}

			// Registry key
			using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
			{
				if (key?.GetValueNames().Contains("SteamPath") == true)
				{
					return key.GetValue("SteamPath")?.ToString().Replace('/', '\\') ?? string.Empty;
				}
			}

			// Guess common paths
			foreach (var path in new[]
			{
				@"Program Files (x86)\Steam",
				@"Program Files\Steam",
			})
			{
				foreach (var drive in Directory.GetLogicalDrives())
				{
					if (Directory.Exists(drive + path))
					{
						return drive + path;
					}
				}
			}

			foreach (var path in new[]
			{
				"/Library/Application Support/Steam",
				"/.steam/steam",
			})
			{
				var combinedPath = Environment.GetEnvironmentVariable("HOME") + path;
				if (Directory.Exists(combinedPath))
				{
					try
					{
						Process process = new Process
						{
							StartInfo = new ProcessStartInfo
							{
								WindowStyle = ProcessWindowStyle.Hidden,
								FileName = "readlink",
								Arguments = $"\"{combinedPath}\"",
								RedirectStandardOutput = true,
								UseShellExecute = false
							}
						};
						process.Start();

						var linkTarget = process.StandardOutput.ReadToEnd();
						if (!string.IsNullOrEmpty(linkTarget))
							return linkTarget;

						return combinedPath;
					}
					catch
					{
						return combinedPath;
					}
				}
			}

			return null;
		}
		public static string GetModPath(string steamID)
		{
			return Path.Combine(SteamWorkshopDirectory, steamID);
		}

		public static IEnumerator<object> LoadMission(string steamID)
		{

			if (!matchSteamID.IsMatch(steamID))
			{
				throw new Exception($"SteamID must be numbers");
			}

			Logger.Log($"Trying to load Mod {steamID}");
			string modPath = GetModPath(steamID);
			if (!Directory.Exists(modPath))
			{
				throw new Exception($"Mod with steamID {steamID} not found");
			}

			if (LoadedMods.TryGetValue(modPath, out Mod mod))
			{
				Logger.Log($"{steamID} is already loaded.");
				yield return mod;
				yield break;
			}

			mod = Mod.LoadMod(modPath, ModSourceEnum.Local);

			if (mod == null)
			{
				throw new Exception($"Skipping \"{steamID}\": No valid modInfo.json");
			}


			foreach (string fileName in mod.GetAssetBundlePaths())
			{
				var bundleRequest = AssetBundle.LoadFromFileAsync(fileName);
				yield return bundleRequest;

				var mainBundle = bundleRequest.assetBundle;

				if (mainBundle == null)
				{
					throw new Exception($"\"{steamID}\" have no asset bundle");
				}

				try
				{
					mod.LoadBundle(mainBundle);
				} catch (Exception ex)
				{
					UnityEngine.Debug.LogErrorFormat("Load of mod \"{0}\" failed: \n{1}\n{2}", mod.ModID, ex.Message, ex.StackTrace);
				}

				mainBundle.Unload(false);
				LoadedMods[modPath] = mod;
				Logger.Log($"Loaded Mod {steamID}");

				FactoryMission.Reload();
				FactoryMission.UpdateCompatibleMissions();
			}

			mod.RemoveServiceObjects();
			mod.CallMethod("RemoveSoundGroups");
			mod.CallMethod("RemoveSoundOverrides");

			Toasts.Make($"Loaded mod: {mod.ModID}({steamID})");
		}

		public static bool IsMissionMod(Mod mod)
		{
			return mod.GetModObjects<KMBombModule>().Count == 0 &&
				mod.GetModObjects<KMNeedyModule>().Count == 0 &&
				mod.GetValue<List<ModMission>>("missions").Count > 0 &&
				mod.GetValue<List<KMSoundOverride>>("soundOverrides").Count == 0;
		}

		public static void DisableMod(string steamID) {
			Utilities.DisableMod(steamID);
		}

		public static IEnumerator EnterAndLeaveModManager()
        {
			if (dblEnterAndLeaveModManagerMethod == null) {
				dblEnterAndLeaveModManagerMethod = ReflectionHelper.FindType("DemandBasedLoading", "TweaksAssembly").GetMethod("EnterAndLeaveModManager");
			}
			return (IEnumerator)dblEnterAndLeaveModManagerMethod.Invoke(null, null);
		}
	}
}
