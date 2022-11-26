using HarmonyLib;
using System;
using UnityEngine;

namespace OutsideTrafficBalancer
{
    //
    // This patch is called whenever the game is about to issue new transfer manager offers for an outside connection.
    // Typically every few seconds (when unpaused).
    //
    // It performs two functions:
    // 1. Applies the capacity multiplier, which affects how many new offers are spawned.
    // 2. If the outside connection has a lower-than-default capacity, prevents any offers from being spawned for that connection
    // at regular intervals, in line with capacity.
    //
    [HarmonyPatch(typeof(OutsideConnectionAI), nameof(OutsideConnectionAI.AddConnectionOffers))]
    class AddConnectionOffersPatch
    {
        [HarmonyPrefix]
        static bool Prefix(ushort buildingID, ref Building data, ref int productionRate,
            int cargoCapacity, int residentCapacity, int touristFactor0, int touristFactor1, int touristFactor2,
            TransferManager.TransferReason dummyTrafficReason, int dummyTrafficFactor)
        {
            if (!OutsideConnectionsInfo.Capacities.ContainsKey(buildingID))
            {
                // This is a Space Evelator (or some other "fake" connection), ignore.
                return true;
            }

            double capacity = OutsideConnectionsInfo.Capacities[buildingID];

            if (capacity < 1.0)
            {
                // Less-than-default-capacity, we skip some cycles.

                OutsideConnectionsInfo.Counters[buildingID] += capacity;

                if (OutsideConnectionsInfo.Counters[buildingID] >= 1.0)
                {
                    OutsideConnectionsInfo.Counters[buildingID] -= 1.0;
                }
                else
                {
                    // Skip this cycle (main function not executed).
                    return false;
                }
            }

            // Apply capacity multiplier to productionRate.
            // ProductionRate is normally based on the budget multiplier (100% budget -> production rate of 100)
            // and affects all capacities (cargo, resident, tourist).

            productionRate = (int)Math.Ceiling(productionRate * capacity);
            return true;

        }
    }
}
