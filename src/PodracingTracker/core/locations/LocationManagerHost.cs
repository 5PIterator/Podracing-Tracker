using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
