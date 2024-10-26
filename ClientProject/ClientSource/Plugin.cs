using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Barotrauma.Items.Components;
using Barotrauma.Networking;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;

namespace BetterFabricatorUI
{
    public partial class Plugin : IAssemblyPlugin
    {
        [HarmonyPatch(declaringType: typeof(Fabricator))]
        [HarmonyPatch(methodName: nameof(Fabricator.CreateGUI))]
        class Patch_Fabricator_CreateGUI
        {
            [HarmonyPostfix]
            static void Inject(Fabricator __instance)
            {
                var fabricator = __instance;

            }
        }
    }
}
