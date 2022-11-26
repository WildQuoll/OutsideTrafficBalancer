using HarmonyLib;
using ColossalFramework;

namespace OutisdeTrafficBalancer
{
    //
    // This patch records each outside connection (both road and non-road), which is from then on tracked by OutsideConnectionsInfo.
    // It is called when:
    // - A map/savegame is loaded.
    // - A new outside connection is created.
    // - An existing outside connection is upgraded from one road type to another.
    //
    [HarmonyPatch(typeof(BuildingManager), nameof(BuildingManager.AddOutsideConnection))]
    class AddOutsideConnectionPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ushort buildingID)
        {
            var buildingManager = Singleton<BuildingManager>.instance;

            var outsideConnectionAI = buildingManager.m_buildings.m_buffer[buildingID].Info.m_buildingAI as OutsideConnectionAI;
            if (!outsideConnectionAI)
            {
                // Unexpected?
                return;
            }

            OutsideConnectionsInfo.UpdateCapacity(buildingID);
        }
    }
}
