using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PodracingTracker;

public partial class RuleManager
{
    public abstract class Rule
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract bool PreTracking { get; }
        public abstract bool AllowUpdate { get; set; }
        public abstract bool AllowDisplay { get; set; }
        public abstract Corner TextCorner { get; set; }
        public abstract void Initialize();
        public abstract void Update();
        public abstract void Display();
    }

    public static List<Rule> Rules { get; private set; }
    public static Dictionary<string, Rule> RuleDictionary { get; private set; }

    private static IModHelper ModHelper;
    private static PlayerBody player;
    private static PlayerCharacterController playerController;
    private static ShipBody ship;
    private static ShipDamageController shipDamageController;
    private static LandingPadManager landingPadManager;
    private static ShipThrusterController shipThrusterController;
    private static Dictionary<string, string> ruleGuiCorners;

    public static void Initialize(IModHelper ModHelper, List<Rule> rules)
    {
        Rules = rules.Where(rule => rule != null).ToList();
        RuleDictionary = Rules.ToDictionary(rule => rule.Name, rule => rule);

        RuleManager.ModHelper = ModHelper;
        ruleGuiCorners = LoadRuleGuiCorners();
        var allComponents = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();

        player = allComponents.OfType<PlayerBody>().FirstOrDefault();
        ship = allComponents.OfType<ShipBody>().FirstOrDefault();
        playerController = allComponents.OfType<PlayerCharacterController>().FirstOrDefault();
        landingPadManager = allComponents.OfType<LandingPadManager>().FirstOrDefault();
        shipThrusterController = allComponents.OfType<ShipThrusterController>().FirstOrDefault();
        shipDamageController = allComponents.OfType<ShipDamageController>().FirstOrDefault();

        InitializeRules();
    }

    private static Dictionary<string, string> LoadRuleGuiCorners()
    {
        const string resourceName = "PodracingTracker.rules.RuleGuiCorners.json";
        var assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        var map = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd())
            ?? throw new InvalidOperationException("RuleGuiCorners.json deserialized to null.");
        return map;
    }

    public static void InitializeRules()
    {
        foreach (Rule rule in Rules)
        {
            rule.AllowUpdate = ModHelper.Config.GetSettingsValue<string>(rule.GetType().Name).Contains("Track");
            rule.AllowDisplay = ModHelper.Config.GetSettingsValue<string>(rule.GetType().Name).Contains("Display");
            string ruleName = rule.GetType().Name;
            if (!ruleGuiCorners.TryGetValue(ruleName, out string cornerName))
                throw new InvalidOperationException($"RuleGuiCorners.json has no entry for rule '{ruleName}'.");
            rule.TextCorner = (Corner)Enum.Parse(typeof(Corner), cornerName);
            ModHelper.Console.WriteLine($"Rule: {rule.GetType().Name} AllowUpdate: {rule.AllowUpdate} AllowDisplay: {rule.AllowDisplay} TextCorner: {rule.TextCorner}", MessageType.Info);
            rule.Initialize();
        }
    }

    public static void UpdateRules(bool readyToTrack)
    {
        if (Rules == null)
        {
            ModHelper.Console.WriteLine("Rules object is null.");
        }

        foreach (Rule rule in Rules)
        {
            if (rule == null)
            {
                ModHelper.Console.WriteLine("Rule object is null.");
                continue;
            }

            if (rule.AllowUpdate && (readyToTrack || rule.PreTracking))
                { rule.Update(); }

            if (rule.AllowDisplay)
                { rule.Display(); }
        }
    }
}
