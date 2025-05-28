namespace LethalSaboteurs.src.Utils
{
    /*[HarmonyPatch(typeof(StartOfRound))]
    internal class WeatherAlertPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnShipLandedMiscEvents")]
        public static void OnShipLandedMiscEventsPatch()
        {
            string title = "Weather alert!";
            if (Effects.IsWeatherEffectPresent("majoramoon"))
            {
                Effects.MessageOneTime(title, MajoraMoonWeather.weatherAlert, true, "LW_MajoraTip");
            }
        }
    }*/
}
