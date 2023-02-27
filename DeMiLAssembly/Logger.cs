using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DeMiLService
{
    static class Logger
    {
        public static void Log(string str)
        {
            Debug.Log($"[DeMiLService] {str}");
        }

        public static void LogError(Exception e)
        {
            Debug.Log($"[DeMiLService] Exception occered");
            Debug.LogError(e);
        }
        public static void LogError(string str, Exception e)
        {
            Debug.Log($"[DeMiLService] {str}");
            Debug.LogError(e);
        }
    }
}
