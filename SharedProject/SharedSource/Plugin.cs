using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using Microsoft.Xna.Framework;


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
            harmony = new Harmony("whosyourdaddy.betterfabricatorui");
            harmony.PatchAll();
            LuaCsLogger.LogMessage($"Start patching {nameof(Fabricator)}");
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
