using System.Collections.Generic;
using System.Linq;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;

#pragma warning disable IDE0130
namespace PodracingTracker
#pragma warning restore IDE0130
{
    public partial class LocationManager
    {
        public static void GatherLocationTransforms()
        {
            relevantLocations = [];
            relevantLandings = [];
            anyLandings = [];
            mazeLandings = [];

            ModHelper.Console.WriteLine("Starting to gather location transforms.", MessageType.Info);

            var astroObjects = UnityEngine.Object.FindObjectsOfType<AstroObject>();
            var shipLogEntries = UnityEngine.Object.FindObjectsOfType<ShipLogEntryLocation>();
            var anyLandingsIds = GetAnyLandingsIds();

            ModHelper.Console.WriteLine($"Found {astroObjects.Length} AstroObjects and {shipLogEntries.Length} ShipLogEntryLocations.", MessageType.Info);

            var combinedMap = astroObjects
                .GroupBy(UtilityTools.IdFromAstro)
                .ToDictionary(group => group.Key, group => group.First().transform)
                .Concat(
                    shipLogEntries
                    .GroupBy(shipLog => shipLog.GetEntryID())
                    .ToDictionary(group => group.Key, group => group.First().transform)
                )
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            ModHelper.Console.WriteLine($"Combined map contains {combinedMap.Count} entries.", MessageType.Info);

            var locations = GetLocations();
            if (locations == null)
            {
                ModHelper.Console.WriteLine("LocationHandler.GetLocations() returned null.", MessageType.Error);
                return;
            }

            foreach (ShipLogEntryLocation shipLogEntry in shipLogEntries)
            {
                var outerFogWarpVolume = shipLogEntry.GetOuterFogWarpVolume();
                if (outerFogWarpVolume != null)
                {
                    string entryFogWarpVolume = outerFogWarpVolume.GetName().ToString();

                    mazeLandings.Add(shipLogEntry.GetEntryID(), entryFogWarpVolume);
                    ModHelper.Console.WriteLine($"Added maze landing {shipLogEntry.GetEntryID()} with outerFogWarpVolume: {entryFogWarpVolume}", MessageType.Success);
                }
            }

            foreach (Location location in locations)
            {
                ModHelper.Console.WriteLine($"Processing location: {location.Name}", MessageType.Info);

                if (combinedMap.TryGetValue(location.Id, out Transform locationTransform))
                {
                    relevantLocations[location.Id] = locationTransform;
                    ModHelper.Console.WriteLine($"Added location transform for {location.Name} ({location.Id})", MessageType.Success);
                }
                else
                {
                    ModHelper.Console.WriteLine($"No transform found for location: {location.Name} ({location.Id})", MessageType.Warning);
                }

                foreach (Landing landing in location.Landings)
                {
                    foreach (Requirement requirement in landing.Requirements)
                    {
                        if (requirement.Type == "Any")
                        {
                            requirement.Id = "Any";
                            ModHelper.Console.WriteLine($"Ignoring requirement: {requirement.Id}", MessageType.Info);
                            continue;
                        }

                        if (combinedMap.TryGetValue(requirement.Id, out Transform requirementTransform))
                        {
                            relevantLandings[requirement.Id] = requirementTransform;
                            ModHelper.Console.WriteLine($"Added requirement transform for {requirement.Id}.", MessageType.Success);
                        }
                        else if (combinedMap.TryGetValue(location.Name, out requirementTransform))
                        {
                            relevantLandings[requirement.Id] = requirementTransform;
                            ModHelper.Console.WriteLine($"Added location transform for requirement {requirement.Id} using location {location.Name}.", MessageType.Success);
                        }
                        else
                        {
                            ModHelper.Console.WriteLine($"No transform found for requirement: {requirement.Id}", MessageType.Warning);
                        }
                    }
                }
            }

            ModHelper.Console.WriteLine($"Combined map finished with {relevantLocations.Count + relevantLandings.Count} ({relevantLocations.Count}/{relevantLandings.Count}) relevant entries.", MessageType.Info);

            ModHelper.Console.WriteLine("Gathering 'Any' landing transforms.", MessageType.Info);

            foreach (string anyLandingId in anyLandingsIds)
            {
                if (combinedMap.TryGetValue(anyLandingId, out Transform anyLandingTransform))
                {
                    anyLandings.Add(anyLandingId, anyLandingTransform);
                    ModHelper.Console.WriteLine($"Added 'Any' landing transform for {anyLandingId}.", MessageType.Success);
                }
                else
                {
                    ModHelper.Console.WriteLine($"No transform found for 'Any' landing: {anyLandingId}", MessageType.Warning);
                }
            }

            ModHelper.Console.WriteLine("Finished gathering location transforms.", MessageType.Info);
        }
    }
}
