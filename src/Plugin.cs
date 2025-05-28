using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalSaboteurs.src.Utils;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LethalSaboteurs.src
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        const string GUID = "Zeldahu.LethalSaboteurs";
        const string NAME = "Lethal Saboteurs";
        const string VERSION = "0.1.0";

        public static Plugin instance;
        public static ManualLogSource logger;
        private readonly Harmony harmony = new Harmony(GUID);
        internal static Config config { get; private set; } = null!;

        void HarmonyPatchAll()
        {
            harmony.PatchAll();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        void Awake()
        {
            instance = this;
            logger = Logger;

            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lethalsaboteurs");
            AssetBundle bundle = AssetBundle.LoadFromFile(assetDir);
            //string directory = "Assets/Data/_Misc/LegendWeathers/";

            config = new Config(Config);
            config.SetupCustomConfigs();
            Effects.SetupNetwork();

            HarmonyPatchAll();
            logger.LogInfo(NAME + " is loaded !");
        }
    }
}
