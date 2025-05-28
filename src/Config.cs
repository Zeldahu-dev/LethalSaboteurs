using BepInEx.Configuration;

namespace LethalSaboteurs.src
{
    class Config
    {
        public readonly ConfigEntry<bool> examplebool;

        public Config(ConfigFile cfg)
        {
            cfg.SaveOnConfigSet = false;
            examplebool = cfg.Bind("Example", "ExampleBool", true, "This is an example boolean config entry.");
            cfg.Save();
            cfg.SaveOnConfigSet = true;
        }

        public void SetupCustomConfigs()
        {
            //WeatherRegisteryInstalled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("mrov.WeatherRegistry");
        }
    }
}
