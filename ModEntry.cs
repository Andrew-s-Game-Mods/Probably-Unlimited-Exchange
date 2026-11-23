using System.Collections.Generic;

namespace unlimited_exchange
{
    /// <summary>
    /// Entry point of the mod. The loader finds the one class implementing IMod, creates it,
    /// applies the [HarmonyPatch] classes of this assembly, then calls Init and OnEnable.
    /// Settings are defined here; the patch reads them through GetBarterLimit at night.
    /// </summary>
    public class ModEntry : IMod
    {
        public static ModEntry Instance { get; private set; }
        public ModManifest Manifest { get; private set; }
        public ModLog Log { get; private set; }

        private static readonly Dictionary<string, BarterSetting> Settings = new Dictionary<string, BarterSetting>();

        private sealed class BarterSetting
        {
            public ConfigEntry<bool> Unlimited;
            public ConfigEntry<int> Limit;
        }

        // One toggle + slider can govern several barter identifiers (e.g. all keycard -> crate trades).
        private sealed class BarterGroup
        {
            public readonly string Key;
            public readonly string NameKey;
            public readonly string[] Ids;

            public BarterGroup(string key, string nameKey, params string[] ids)
            {
                Key = key;
                NameKey = nameKey;
                Ids = ids;
            }
        }

        // Identifiers match ExchangeList. Order follows the in-game exchange list.
        private static readonly BarterGroup[] PermanentGroups =
        {
            new BarterGroup("filter_barter", "name_filter_barter", "filter_barter"),
            new BarterGroup("module_barter", "name_module_barter", "module_barter"),
            new BarterGroup("dream_dust_barter", "name_dream_dust_barter", "dream_dust_barter"),
            new BarterGroup("commandKeycardBarter", "name_commandKeycardBarter", "commandKeycardBarter"),
            new BarterGroup("poison_barter", "name_poison_barter", "poison_barter"),
            new BarterGroup("bullet_barter", "name_bullet_barter", "bullet_barter"),
            new BarterGroup("smuggler_barter", "name_smuggler_barter", "smuggler_barter"),
        };

        private static readonly BarterGroup[] PromotionalGroups =
        {
            new BarterGroup("crate_barter", "name_crate_barter", "sec_crate_barter", "med_crate_barter", "eng_crate_barter"),
            new BarterGroup("seed_barter", "name_seed_barter", "seed_barter"),
            new BarterGroup("improved_dream_dust_barter", "name_improved_dream_dust_barter", "improved_dream_dust_barter"),
            new BarterGroup("bm_rep_barter", "name_bm_rep_barter", "bm_rep_barter"),
            new BarterGroup("immunivax_barter", "name_immunivax_barter", "immunivax_barter"),
        };

        public void Init(ModManifest manifest)
        {
            Instance = this;
            Manifest = manifest;
            Log = new ModLog(manifest);
            Log.Log($"{manifest.Name} v{manifest.ModVersion} loaded.");

            ModConfig config = ModHelper.GetConfig(manifest.ID);
            DefineGroups(config, PermanentGroups, "常驻交易 · Permanent Trades");
            DefineGroups(config, PromotionalGroups, "促销交易 · Promotional Trades");
        }

        private void DefineGroups(ModConfig config, BarterGroup[] groups, string section)
        {
            string modId = Manifest.ID;
            foreach (BarterGroup group in groups)
            {
                string nameKey = group.NameKey;
                ConfigEntry<bool> unlimited = config.Define(
                    group.Key + "_unlimited", true, section,
                    () => ModHelper.GetLocalized(modId, nameKey) + " · " + ModHelper.GetLocalized(modId, "unlimited_suffix"),
                    () => ModHelper.GetLocalized(modId, "unlimited_tooltip"));
                ConfigEntry<int> limit = config.Define(
                    group.Key + "_limit", 1, section,
                    () => ModHelper.GetLocalized(modId, nameKey) + " · " + ModHelper.GetLocalized(modId, "limit_suffix"),
                    min: 1, max: 20,
                    tooltip: () => ModHelper.GetLocalized(modId, "limit_tooltip"));

                BarterSetting setting = new BarterSetting { Unlimited = unlimited, Limit = limit };
                foreach (string id in group.Ids)
                {
                    Settings[id] = setting;
                }
            }
        }

        /// <summary>
        /// Per-night accept budget for a barter: int.MaxValue when unlimited, otherwise the
        /// configured 1-20 limit. Unknown barters (e.g. added by another mod) stay unlimited,
        /// preserving the mod's core behaviour.
        /// </summary>
        public static int GetBarterLimit(string identifier)
        {
            if (identifier != null && Settings.TryGetValue(identifier, out BarterSetting setting))
            {
                return setting.Unlimited.Value ? int.MaxValue : setting.Limit.Value;
            }
            return int.MaxValue;
        }

        public void OnEnable()
        {
            Log.Log("Enabled. Underground Exchange limits are configurable per trade.");
        }

        public void OnDisable()
        {
            Log.Log("Disabled.");
        }
    }
}