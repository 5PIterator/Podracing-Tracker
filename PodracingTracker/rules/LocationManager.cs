using System.Collections.Generic;
using System;
using System.IO;
using OWML.ModHelper;
using OWML.Common;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.LowLevel;
using Epic.OnlineServices.UserInfo;
using System.Linq.Expressions;
using UnityEngine.SceneManagement;
using System.Linq;
using Newtonsoft.Json;
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace PodracingTracker
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    public class LocationManager
    {
        public class PodracingLandings
        {
            public List<Location> Locations { get; set; }
        }
        public class PodracingAnyLandings
        {
            public List<string> AnyLandings { get; set; }
        }

        private static IModHelper ModHelper;
        private static List<Location> locations;
        private static List<string> anyLandingsIds;
        public static Dictionary<string, string> mazeLandings;
        public static Dictionary<string, Transform> relevantLocations;
        public static Dictionary<string, Transform> relevantLandings;
        public static Dictionary<string, Transform> anyLandings;
        public static bool showLandingDetails;

        public static void Initialize(IModHelper ModHelper)
        {
            LocationManager.ModHelper = ModHelper;
            showLandingDetails = ModHelper.Config.GetSettingsValue<bool>("Hide Any Requirement");
            LoadJson();
        }

        private static void LoadJson()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var jsonLandings = "PodracingTracker.rules.PodracingLandings.json";
                var jsonAnyLandings = "PodracingTracker.rules.PodracingAnyLandings.json";

                using (Stream stream = assembly.GetManifestResourceStream(jsonLandings))
                using (StreamReader reader = new StreamReader(stream))
                {
                    var jsonData = reader.ReadToEnd();
                    var podRacingLandings = JsonConvert.DeserializeObject<PodracingLandings>(jsonData);
                    locations = podRacingLandings.Locations;
                    ModHelper.Console.WriteLine("JSON file loaded successfully");
                }

                using (Stream stream = assembly.GetManifestResourceStream(jsonAnyLandings))
                using (StreamReader reader = new StreamReader(stream))
                {
                    var jsonData = reader.ReadToEnd();
                    var podracingAnyLandings = JsonConvert.DeserializeObject<PodracingAnyLandings>(jsonData);
                    anyLandingsIds = podracingAnyLandings.AnyLandings;
                    ModHelper.Console.WriteLine("JSON file loaded successfully");
                }
            }
            catch (Exception ex)
            {
                ModHelper.Console.WriteLine($"Error loading JSON file: {ex.Message}");
            }
        }

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

            //debug
            //print all markers into a file
            //using (StreamWriter sw = new StreamWriter(markerpath))
            //{
            //    foreach (ShipLogEntryLocation marker in entryLocations)
            //    {
            //        sw.WriteLine($"{marker._entryID}");
            //    }
            //}
            //string astropath = @"C:\Users\kryst\Desktop\GitProjects\Outer Wilds\astroObjects.txt";
            //string shiplogpath = @"C:\Users\kryst\Desktop\GitProjects\Outer Wilds\shipLogEntries.txt";
            //using (StreamWriter sw = new StreamWriter(astropath))
            //{
            //    foreach (var astroObject in astroObjects)
            //    {
            //        sw.WriteLine($"{astroObject.name}");
            //    }
            //}
            //using (StreamWriter sw = new StreamWriter(shiplogpath))
            //{
            //    foreach (var shipLogEntry in shipLogEntries)
            //    {
            //        sw.WriteLine($"{shipLogEntry.GetEntryID()}");
            //    }
            //}

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

            // gather maze landings
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
            //string markerpath = @"C:\Users\kryst\Desktop\GitProjects\Outer Wilds\markers.txt";
            //using (StreamWriter sw = new StreamWriter(markerpath))
            //{
            //    foreach (var location in combinedMap.Keys)
            //    {
            //        var transform = combinedMap[location];
            //        sw.WriteLine($"{location}, Transform: {(transform == null ? "null" : transform.name)}");
            //    }
            //}

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

            //string markerOutput = @"C:\Users\kryst\Desktop\GitProjects\Outer Wilds\markerOutput.txt";
            //using (StreamWriter sw = new StreamWriter(markerOutput))
            //{
            //    foreach (string location in relevantLocations.Keys)
            //    {
            //        sw.WriteLine($"{location}");
            //    }
            //}

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

            return;
        }

        private static string closestAnyLanding = "";
        /// <summary>
        /// Gather distances for all relevant landings of the given location
        /// </summary>
        /// <param name="location"></param>
        public static void GatherDistances(Location location)
        {
            if (location == null || location.Landings == null)
            {
                ModHelper.Console.WriteLine("Location or Landings is null", MessageType.Error);
                return;
            }

            // Gather the closest Any landing
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
            //debug
            //GUILineManager.SetLine("Any", $"Closest Any: {closestAnyLanding} ({closestAnyDistance})", true, Corner.TopLeft);

            // Gather the closest location and all its landings
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
                        // Skip landings that have already been landed
                        continue;
                    }
                    else if (relevantLandings != null && relevantLandings.TryGetValue(requirement.Id, out Transform requirementTransform))
                    {
                        // Check or calculate distance
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

        /// <summary>
        /// Clears the IsLanded state of all landings
        /// </summary>
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
        public static List<Location> GetLocations()
        {
            return locations;
        }

        public static Location GetLocationById(string id)
        {
            var result = locations?.Find(location => location.Id == id);
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
        public static List<string> GetAnyLandingsIds()
        {
            return anyLandingsIds;
        }
        public static List<Transform> GetAnyLandingsTransforms()
        {
            return anyLandings.Values.ToList();
        }
        public static Dictionary<string, string> GetMazeLandings()
        {
            return mazeLandings;
        }
    }

    public class Location(string id, string name, List<Landing> landings)
    {
        public string UIid { get; set; } = GUILineManager.GenerateId();
        public string Id { get; set; } = id;
        public string Name { get; set; } = name;
        public List<Landing> Landings { get; set; } = landings;
        public bool LandingsMet => Landings.All(landing => landing.IsLanded);
        public Corner InfoCorner { get; set; } = Corner.CenterLeft;

        /// <summary>
        /// Display the location and all landings with their requirements
        /// </summary>
        /// <returns>
        /// Dictionary with landing names and if the requirements are met
        /// </returns>
        public Dictionary<Landing, bool> DisplayLocation()
        {
            GUILineManager.SetLine($"{UIid}", $"<b>{(LandingsMet ? $"<color=green>{Name}</color>" : Name)}</b>", true, InfoCorner, 0);
            Dictionary<Landing, bool> landingResults = [];

            foreach (Landing landing in Landings)
            {
                landingResults.Add(landing, landing.DisplayLanding());
            }
            return landingResults;
        }
    }
    public class Landing(string name, string description, List<Requirement> requirements, bool isLanded)
    {
        public string UIid { get; set; } = GUILineManager.GenerateId();
        public string Name { get; set; } = name;
        public string Description { get; set; } = description;
        public List<Requirement> Requirements { get; set; } = requirements;
        public bool IsLanded { get; set; } = isLanded;
        public Corner InfoCorner { get; set; } = Corner.CenterLeft;
        public Corner LandedCorner { get; set; } = Corner.CenterRight;

        public bool RequirementsMet => Requirements.All(
            requirement => requirement.RequirementsMet.Item1 && requirement.RequirementsMet.Item2
        );

        public bool DisplayLanding()
        {
            var anyRequirement = Requirements.FirstOrDefault(req => req.Type == "Any");
            if (anyRequirement != null) // If the landing has an "Any" requirement
            {
                if (IsLanded) // and If the landing has been landed, display the landing name and the id of the "Any" requirement
                {
                    GUILineManager.SetLine($"{UIid}", $"  <b><color=yellow>{Name}</color></b>\n  <i>{Description}</i>", true, InfoCorner);
                }
                else // else display just the landing name
                {
                    GUILineManager.SetLine($"{UIid}", $"  <b><color=white>{Name}</color></b>\n  <i>{Description}</i>", true, InfoCorner);
                }

                // Then process and display all requirements
                foreach (Requirement requirement in Requirements)
                {
                    //Hide requirement if the id matches the id of maze landing, and the player is not in the maze
                    requirement.Hidden = LocationManager.mazeLandings.ContainsKey(requirement.Id) && LocationManager.mazeLandings[requirement.Id] != UtilityTools.playerInMaze;
                    //GUILineManager.SetLine($"{parentLocation.Name}/{Name}/{requirement.Name}", $"    {requirement.Name} ({requirement.Id})", true, corner);
                    requirement.DisplayRequirement();
                }
            }
            else // If the landing has no "Any" requirement
            {
                if (IsLanded) // and If the landing has been landed, display the landing name in both corners
                {
                    GUILineManager.SetLine($"{UIid}", $"  <b><color=green>{Name}</color></b>", true, InfoCorner);
                }
                else
                {
                    GUILineManager.SetLine($"{UIid}", $"  <b><color=white>{Name}</color></b>\n  <i>{Description}</i>", true, InfoCorner);

                }
                foreach (Requirement requirement in Requirements)
                {
                    //Hide requirement if the id matches the id of maze landing, and the player is not in the same level of the maze
                    requirement.Hidden =
                        LocationManager.mazeLandings.ContainsKey(requirement.Id) &&
                        LocationManager.mazeLandings[requirement.Id] != UtilityTools.playerInMaze
                    ;
                    //GUILineManager.SetLine($"{parentLocation.Name}/{Name}/{requirement.Name}", $"    {requirement.Name} ({requirement.Id})", true, corner);
                    requirement.DisplayRequirement();
                }
            }
            return RequirementsMet;
        }
    }
    public class Requirement(string id, int min, int max, float distance)
    {
        public string UIid { get; set; } = GUILineManager.GenerateId();
        public string Id { get; set; } = id;
        public string Type { get; set; }
        public int Min { get; set; } = min;
        public int Max { get; set; } = max;
        public float Distance { get; set; } = distance;
        public bool Hidden { get; set; } = false;
        public Tuple<bool, bool> RequirementsMet => new(Distance >= Min, Distance <= Max);
        public Corner corner { get; set; } = Corner.CenterLeft;

        public void DisplayRequirement()
        {
            var (minMet, maxMet) = RequirementsMet;

            string minText = minMet ? $"<color=green>{Min}</color>" : $"<color=red>{Min}</color>";
            string maxText = maxMet ? $"<color=green>{Max}</color>" : $"<color=red>{Max}</color>";

            if (minMet && maxMet)
            {
                minText = $"<color=green>{Min}</color>";
                maxText = $"<color=green>{Max}</color>";
            }
            else if (minMet || maxMet)
            {
                minText = minMet ? $"<color=yellow>{Min}</color>" : $"<color=red>{Min}</color>";
                maxText = maxMet ? $"<color=yellow>{Max}</color>" : $"<color=red>{Max}</color>";
            }

            string idText;
            string distanceText;
            string detailsText;
            if (Type == "Any")
            {
                idText = $"{Type}";
                detailsText = $"({Id})";
            }
            else
            {
                idText = $"{Id}";
                detailsText = $"({Type})";
            }
            detailsText = LocationManager.showLandingDetails ? detailsText : "";
            distanceText = Distance < 10000 && !Hidden ? Distance.ToString("0.00") : "####.##";

            //string statusText = string.Format("    {0,-10}<{1,8:0.00}<{2,-10} - {3}", minText, Distance, maxText, idText);
            string statusText = $"    {minText}<{distanceText}<{maxText}\t - {idText} {detailsText}";
            //GUILineManager.SetLine($"{Name}", string.Format("    {3}: {0,-10}<{1,8:0.00}<{2,-10}", minText, Distance, maxText, idText), true, corner);
            GUILineManager.SetLine($"{UIid}", statusText, true, corner);
        }
    }
}