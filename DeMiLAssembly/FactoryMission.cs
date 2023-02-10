using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DeMiLService
{
    class FactoryMission
    {
        private const string MULTIPLE_BOMBS_TYPE_NAME = "FactoryAssembly.MultipleBombsInterface";
        private static Type _FactoryGameModePickerType;
        private static MethodInfo _UpdateCompatibleMissionsMethod;

        public static bool IsInstalled => _FactoryGameModePickerType != null;

        public static void Reload()
        {
            _FactoryGameModePickerType = ReflectionHelper.FindType("FactoryAssembly.FactoryGameModePicker");
            _UpdateCompatibleMissionsMethod = 
                _FactoryGameModePickerType.GetMethod(
                    "UpdateCompatibleMissions", 
                    BindingFlags.Static | BindingFlags.NonPublic
                );
        }

        public static void UpdateCompatibleMissions()
        {
            if (!IsInstalled) return ;
            _UpdateCompatibleMissionsMethod.Invoke(null, null);
        }
    }
}
