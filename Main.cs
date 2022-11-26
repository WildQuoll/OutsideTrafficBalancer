using ICities;
using CitiesHarmony.API;

namespace OutisdeTrafficBalancer
{
    public class Mod : IUserMod
    {
        public string Name => "Outisde Traffic Balancer";
        public string Description => "Adjusts the amount of road traffic using each outside connection, based on road capacity.";

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }

        public static string Identifier = "WQ.OTB/";
    }
}
