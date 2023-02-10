using Assets.Scripts.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DeMiLService
{
    class FactoryMission
    {
        private static Type _FactoryGameModePickerType;
        private static MethodInfo _UpdateCompatibleMissionsMethod;
        private static MethodInfo _GetGameModeForMissionMethod;
        private static MethodInfo _GetFriendlyNameMethod;

        public static bool IsInstalled => _FactoryGameModePickerType != null;

        public static void Reload()
        {
            _FactoryGameModePickerType = ReflectionHelper.FindType("FactoryAssembly.FactoryGameModePicker");
            _UpdateCompatibleMissionsMethod = 
                _FactoryGameModePickerType.GetMethod(
                    "UpdateCompatibleMissions", 
                    BindingFlags.Static | BindingFlags.NonPublic
                );
            _GetGameModeForMissionMethod =
                _FactoryGameModePickerType.GetMethod(
                    "GetGameModeForMission",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(Mission) },
                    null
                );
            _GetFriendlyNameMethod =
                _FactoryGameModePickerType.GetMethod(
                    "GetFriendlyName",
                    BindingFlags.Static | BindingFlags.NonPublic
                );
        }

        public static void UpdateCompatibleMissions()
        {
            if (!IsInstalled) return ;
            _UpdateCompatibleMissionsMethod.Invoke(null, null);
        }

        public static string GetGameMode(Mission mission)
        {
            if (!IsInstalled) return null;
            object gamemode = _GetGameModeForMissionMethod.Invoke(null, new object[] { mission });
            object result = _GetFriendlyNameMethod.Invoke(null, new object[] { gamemode });
            if (result == null) return null;
            return (string)result;
        }
    }
}
