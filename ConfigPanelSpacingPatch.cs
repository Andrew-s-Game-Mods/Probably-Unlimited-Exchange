using HarmonyLib;

namespace unlimited_exchange
{
    // The native ModConfig panel draws entries with fixed row spacing and exposes no per-entry
    // gap. Insert a vertical spacer after each trade group (its "_limit" slider) so consecutive
    // groups are visually separated. Gated to this mod, leaving other mods' panels untouched.
    [HarmonyPatch(typeof(ModConfigWindow), "BuildEntry")]
    public static class ConfigPanelSpacingPatch
    {
        private static void Postfix(CustomUIBuilder b, ModConfig config, ConfigEntry entry)
        {
            if (ModEntry.Instance != null
                && config.ModId == ModEntry.Instance.Manifest.ID
                && entry.Key.EndsWith("_limit"))
            {
                b.AddSpace(10f);
            }
        }
    }
}