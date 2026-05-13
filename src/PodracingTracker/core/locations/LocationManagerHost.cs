using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;

#pragma warning disable IDE0130
namespace PodracingTracker
#pragma warning restore IDE0130
{
    public partial class LocationManager
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
            showLandingDetails = ModHelper.Config.GetSettingsValue<bool>("Show Landing Details");
            LoadJson();
        }

        private static void LoadJson()
        {
            try
            {
                string pathLandings = ModContentPaths.RuleFile("PodracingLandings.json");
                string pathAny = ModContentPaths.RuleFile("PodracingAnyLandings.json");

                using (StreamReader reader = File.OpenText(pathLandings))
                {
                    var podRacingLandings = JsonConvert.DeserializeObject<PodracingLandings>(reader.ReadToEnd());
                    locations = podRacingLandings.Locations;
                    ModHelper.Console.WriteLine("PodracingLandings.json loaded successfully");
                }

                using (StreamReader reader = File.OpenText(pathAny))
                {
                    var podracingAnyLandings = JsonConvert.DeserializeObject<PodracingAnyLandings>(reader.ReadToEnd());
                    anyLandingsIds = podracingAnyLandings.AnyLandings;
                    ModHelper.Console.WriteLine("PodracingAnyLandings.json loaded successfully");
                }
            }
            catch (Exception ex)
            {
                ModHelper.Console.WriteLine($"Error loading JSON file: {ex.Message}");
            }
        }

        public static List<Location> GetLocations()
        {
            return locations;
        }

        public static List<string> GetAnyLandingsIds()
        {
            return anyLandingsIds;
        }
    }
}
