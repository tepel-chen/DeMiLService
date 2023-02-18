using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeMiLService
{
    static class BinderRefresher
    {
        private static BombBinder binder;
        public static void Refresh()
        {
            if(binder == null)
            {
                binder = UnityEngine.Object.FindObjectOfType<SetupRoom>().BombBinder;
            }
            binder.MissionTableOfContentsPageManager.ForceFullRefresh();
            KTInputManager.Instance.Select(binder.Selectable);
        }
    }
}
