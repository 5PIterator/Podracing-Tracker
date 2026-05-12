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
        private static string closestAnyLanding = "";

        public static void GatherDistances(Location location)
        {
            if (location == null || location.Landings == null)
            {
                ModHelper.Console.WriteLine("Location or Landings is null", MessageType.Error);
                return;
            }

            float closestAnyDistance = float.MaxValue;
            foreach (KeyValuePair<string, Transform> anyLanding in anyLandings)
            {
                string anyLandingId = anyLanding.Key;
                Transform anyLandingTransform = anyLanding.Value;
                float distance = Vector3.Distance(UtilityTools.lastPlayerPosition, anyLandingTransform.position);
                if (distance < closestAnyDistance)
                {
                    closestAnyDistance = distance;
                    closestAnyLanding = anyLandingId;
                }
            }

            foreach (Landing landing in location.Landings)
            {
                if (landing.Requirements == null)
                {
                    ModHelper.Console.WriteLine("Landing Requirements is null", MessageType.Error);
                    continue;
                }

                foreach (Requirement requirement in landing.Requirements)
                {
                    if (requirement == null)
                    {
                        ModHelper.Console.WriteLine("Requirement is null", MessageType.Error);
                        continue;
                    }

                    if (requirement.Type == "Any")
                    {
                        if (anyLandings == null)
                        {
                            ModHelper.Console.WriteLine("AnyLandings is null", MessageType.Error);
                            continue;
                        }

                        requirement.Distance = closestAnyDistance;
                        requirement.Id = closestAnyLanding;
                    }
                    else if (landing.IsLanded)
                    {
                        continue;
                    }
                    else if (relevantLandings != null && relevantLandings.TryGetValue(requirement.Id, out Transform requirementTransform))
                    {
                        if (!UtilityTools.distanceCache.TryGetValue(requirementTransform, out float distance))
                        {
                            distance = Vector3.Distance(UtilityTools.lastPlayerPosition, requirementTransform.position);
                            UtilityTools.distanceCache[requirementTransform] = distance;
                        }
                        requirement.Distance = distance;
                    }
                }
            }
        }

        public static void RemoveAnyLanding(string id)
        {
            anyLandings.Remove(id);
        }

        public static void ClearLandingState()
        {
            foreach (Location location in GetLocations())
            {
                foreach (Landing landing in location.Landings)
                {
                    landing.IsLanded = false;
                }
            }
        }

        public static Location GetLocationById(string id)
        {
            var result = GetLocations()?.Find(location => location.Id == id);
            return result;
        }

        public static List<Landing> GetLandingsByLocationId(string id)
        {
            var location = GetLocationById(id);
            return location?.Landings;
        }

        public static Landing GetLandingByName(string locationId, string landingName)
        {
            var landings = GetLandingsByLocationId(locationId);
            return landings?.Find(landing => landing.Name == landingName);
        }

        public static List<string> GetRelevantLocationsIds()
        {
            return relevantLocations.Keys.ToList();
        }

        public static List<Transform> GetRelevantLocationsTransforms()
        {
            return relevantLocations.Values.ToList();
        }

        public static List<string> GetRelevantLandingsIds()
        {
            return relevantLandings.Keys.ToList();
        }

        public static List<Transform> GetRelevantLandingsTransforms()
        {
            return relevantLandings.Values.ToList();
        }

        public static List<Transform> GetAnyLandingsTransforms()
        {
            return anyLandings.Values.ToList();
        }

        public static Dictionary<string, string> GetMazeLandings()
        {
            return mazeLandings;
        }

        public static bool TryGetRequirementTransform(string requirementId, out Transform transform)
        {
            transform = null;
            if (string.IsNullOrEmpty(requirementId))
                return false;
            if (relevantLandings != null && relevantLandings.TryGetValue(requirementId, out transform))
                return true;
            if (anyLandings != null && anyLandings.TryGetValue(requirementId, out transform))
                return true;
            return false;
        }
    }
}
