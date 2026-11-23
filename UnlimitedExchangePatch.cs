using System.Collections.Generic;
using HarmonyLib;

namespace unlimited_exchange
{
    // Vanilla HandleExchange accepts each barter once per night (barterCounterPerNight = 1).
    // We replace that pass so every barter follows its own configured budget: unlimited, or a
    // fixed number of accepts per night. Reset() still runs beforehand and restores counters.
    [HarmonyPatch(typeof(ExchangeManager), nameof(ExchangeManager.HandleExchange))]
    public static class UnlimitedExchangePatch
    {
        private static bool Prefix(ExchangeManager __instance)
        {
            List<ExchangeBarter> barters = new List<ExchangeBarter>(__instance.activeBarter);
            foreach (ExchangeBarter barter in __instance.promotionalBarter)
            {
                if (__instance.activePromotionalBarterIds.Contains(barter.identifier))
                {
                    barters.Add(barter);
                }
            }

            barters.Sort((x, y) => y.priority.CompareTo(x.priority));

            int accepted = 0;
            foreach (ExchangeBarter barter in barters)
            {
                barter.currentBarterCounter = ModEntry.GetBarterLimit(barter.identifier);
                while (barter.CanAccept())
                {
                    barter.Accept();
                    StoreReputation.GetBMFaction().ModProgressReputation(75);
                    accepted++;
                }
            }

            if (accepted > 0)
            {
                ModEntry.Instance.Log.Log($"{accepted} barter(s) accepted.");
            }

            return false;
        }
    }
}