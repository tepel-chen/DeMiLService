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
    }
}
