using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;


#if CLIENT
[assembly: IgnoresAccessChecksTo("Barotrauma")]
#endif
#if SERVER
[assembly: IgnoresAccessChecksTo("DedicatedServer")]
#endif
[assembly: IgnoresAccessChecksTo("BarotraumaCore")]

namespace BetterFabricatorUI
{
    public partial class Plugin : IAssemblyPlugin
    {
        private Harmony harmony;

        public void Initialize()
        {
            LuaCsLogger.LogMessage($"[{nameof(BetterFabricatorUI)}] Start patching {nameof(Fabricator)}");
            harmony = new Harmony("com.whosyourdaddy.betterfabricatorui");
            harmony.PatchAll();
        }

        public void OnLoadCompleted()
        {

        }

        public void PreInitPatching()
        {
            // Not yet supported: Called during the Barotrauma startup phase before vanilla content is loaded.
        }

        public void Dispose()
        {
            
            harmony?.UnpatchAll();
            harmony = null;
        }
    }
}
