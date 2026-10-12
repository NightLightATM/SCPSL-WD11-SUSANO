using HarmonyLib;
using InventorySystem.Items.ThrowableProjectiles;
using WD11Susano.Handlers;

namespace WD11Susano.Patches {
    [HarmonyPatch(typeof(FlashbangGrenade), "ProcessPlayer")]
    internal static class NoBlindPatch {
        [HarmonyPrefix]
        private static bool SkipBlind(FlashbangGrenade __instance) {
            if (SusHandler.NoBlindFlashes.Contains(__instance.Info.Serial)) {
                return false;
            }
            return true;
        }
    }
}
