using ColossalFramework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutisdeTrafficBalancer
{
    public static class OutsideConnectionsInfo
    {
        // Capacity of each outside connection, as a fraction (or multiple) of the standard 3-lane highway (which has a value of 1).
        // Non-road outside connections are also included with a value of 1.
        // Deleted outside connections are kept until the user leaves the game.
        // (Key) Outside connection "building ID" -> (Value) Capacity
        public static Dictionary<ushort, double> Capacities = new Dictionary<ushort, double>();

        // Counters indicating the current progress of each outside connection towards the next issuance of transfer manager offers.
        // Values start at >=0, and gradually grow towards and past 1.
        // The progress increases with each "cycle" (each call to OutsideConnectionAI.AddConnectionOffers, which occurs every few seconds or so),
        // by the amount equal to the capacity of the outside connection.
        // Once a counter surpasses 1 (and only then) the outside connection will issue new transfer manager offers,
        // while the counter will be set back by 1, with the new value back in the [0, 1) range.
        // Outside connections with capacity of >= 1 have their counters initialised, but not increased/tracked,
        // as they issue new offers in each cycle anyway (since each increment is >= 1). Likewise for non-road outside connections.
        // Deleted outside connections are kept until the user leaves the game.
        // (Key) Outside connection "building ID" -> (Value) Current counter value
        public static Dictionary<ushort, double> Counters = new Dictionary<ushort, double>();

        private static bool IsCarLane(NetInfo.Lane lane)
        {
            var laneType = NetInfo.LaneType.Vehicle;
            var vehicleType = VehicleInfo.VehicleType.Car;
            var vehicleCategory = VehicleInfo.VehicleCategoryPart1.PassengerCar;

            return (lane.m_laneType & laneType) != 0 &&
                   (lane.m_vehicleType & vehicleType) != 0 &&
                   (lane.m_vehicleCategoryPart1 & vehicleCategory) != 0;
        }

        private static uint GetCarLaneCount(NetInfo info)
        {
            uint count = 0;

            foreach (var lane in info.m_lanes)
            {
                if (IsCarLane(lane))
                {
                    count += 1;
                }
            }

            return count;
        }

        // Calculates outside connection capacity multiplier, where 1 is the standard 3-lane highway capacity
        private static double CalculateCapacity(ushort outsideConnectionBuildingID)
        {
            var netManager = Singleton<NetManager>.instance;
            foreach (NetNode node in netManager.m_nodes.m_buffer)
            {
                if (node.m_building == outsideConnectionBuildingID)
                {
                    if (node.m_segment0 == 0)
                    {
                        // Probably shouldn't happen:
                        // The outside connection node is not connected to a segment, or the connected segment is not at index 0.
                        // Use default capacity.
                        return 1.0;
                    }

                    var netInfo = netManager.m_segments.m_buffer[node.m_segment0].Info;

                    if (netInfo == null || !netInfo.m_netAI.GetType().IsSubclassOf(typeof(RoadBaseAI)))
                    {
                        // This is not a road. Could be rail/ship/plane. Use default capacity.
                        return 1.0;
                    }

                    // Note: We only count actual car lanes (not bus lanes, not emergency lanes)
                    // Also note: Vanilla highways appear to have a special "outside connection" variant, which automatically appears on map boundaries.
                    // It differs from the standard segment by having an additional, hidden bus lane.
                    // Why that is, who knows - but all the more important we only count car lanes.
                    double laneCount = GetCarLaneCount(netInfo);

                    // If this is a one way road, we assume that all lanes match the direction (incoming/outgoing) of the outside connection.
                    // If this is a two way road, then we assume we are dealing with a bi-directional outside connection,
                    // with 1/2 capacity assigned to each direction.
                    // Note: Capacity of asymmetrical roads is intentionally equalised (e.g. 1+2 road -> 1.5 capacity in each direction).
                    if (netInfo.m_hasBackwardVehicleLanes && netInfo.m_hasForwardVehicleLanes)
                    {
                        laneCount /= 2.0;
                    }

                    // Note: 6 is the capacity of the standard 3-lane highway (3 lanes x 2 speed "units"; 1 speed unit = 50 km/h)
                    double capacity = (laneCount * netInfo.m_averageVehicleLaneSpeed) / 6.0;

                    // We in fact use a square root of the capacity, as:
                    // CAPACITY = FREQUENCY OF NEW TRANSFER OFFERS x MAX NUM OF NEW TRANSFER OFFERS, and the multiplier is applied to both
                    // FREQUENCY OF NEW TRANSFER OFFERS and MAX NUM OF NEW TRANSFER OFFERS.
                    return Math.Sqrt(capacity);
                }
            }

            // Probably shouldn't happen: No node connected to the outside connection.
            // Use default capacity.
            return 1.0;
        }

        public static void UpdateCapacity(ushort outsideConnectionBuildingID)
        {
            Capacities[outsideConnectionBuildingID] = CalculateCapacity(outsideConnectionBuildingID);

            // Initialize each connection with a random value between [0, 1).
            // Otherwise all outside connection with the same road type would always issue new transfer manager offers at the same time.
            Counters[outsideConnectionBuildingID] = (double)Singleton<SimulationManager>.instance.m_randomizer.Int32(0, 255) / 256.0;
        }
    }
}
