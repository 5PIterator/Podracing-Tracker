using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.InputSystem;
using OWML.ModHelper.Menus;
using OWML.ModHelper.Menus.NewMenuSystem;
using System.Runtime.InteropServices;
using System.Collections;
using ShapeCollision;
using UnityEngine.InputSystem.EnhancedTouch;
using System.IO;

namespace PodracingTracker;

//This project is a mod for Outer Wilds that tracks the rules of Podracing.
// The goal is to create a tracker that lets the player know the rules of podracing in real time.


//The rules of podracing are as follows:
/*A, Basic Rules:
1. For a qualifying run you are required to end the run with the most Qualifying Takeoffs performed at any of the Takeoff Locations.
2. Only one Qualifying Takeoff can be performed per a Takeoff Location.
3. The order in which these takeoffs are performed doesn't matter.
4. Score is tallied by the amount of takeoffs performed as priority, and time of the run as secondary. (L03, T15:00 > L02, T04:00)
5. The rules of Podracing are vague, but are to be taken literally. It is half the fun to figure out the Takeoff itself, and find loop holes to get the most optimal route.
6. Resume Expedition only.

B, Start and End:
1. The run starts the instant you interact with the cockpit of your ship while wearing a spacesuit.
2. The run ends one second after you become grounded* while wearing a spacesuit. Leaving this state cancels this rule until next time.
3. The run ends one second after waking up in another loop. (The animation time is added to the run)

C, Takeoff Location is defined either as:
1. A specified radius around a markable/lock-on location**. (Hollow's Lantern is a Lock-On, Village is a markable ship-log)
2. Any*** markable main ship-log location. (Village - main, Zero-G Cave - child)
3. If the criteria for two or more takeoffs are met at once, they all count as their own takeoffs. (2 in 1)

D, Qualifying Takeoff is defined either as:
1. Any ship takeoff, so far your takeoff makes a charging sound. (This includes the first takeoff)
2. Any shipless**** takeoff, so far you takeoff least 0.8 seconds after you become grounded*.
3. Any Death.

E, Disqualifying actions during a run:
1. Lock-On
2. Mark-On HUD
3. Exit to Title Screen
4. Any game modifications, except the Podracing Tracker mod

F, Permitted use of tools during a run:
1: External notes
2: External timers
3: Podracing Tracker mod

*Grounded is defined as being in gravity. The value of gravity on your hud is visible and is higher than zero. (Gravity 0.1x-inf)
**By 'location' it is meant literally a 'ship-log location'. Some Takeoff Locations are then either markable ship-logs, or objects that can be Locked-On in-game.
***If ship-log location isn't specified in criteria, any markable location can be counted as a Takeoff Location, so far all other criteria are met. (Any 0-50m)
****You can count any takeoff as shipless, so far your ship is more than 100m away. (Ship 100-inf m)*/

//The following is a list of all the Takeoff Locations in the game, and their specific criteria for a takeoff.
/*Ordered by the difficulty of a ship takeoff.
 - Basic description of the takeoff location (Specific tracking-ready parameters)

Timber Hearth:
 - Anywhere in the Village. (Village 0-50m)
 - Nomai Mines, the inside of the largest geyser. (Timber Hearth 215-220m & Nomai Mines 110-130m)

Quantum Moon:
 - Anywhere on the Quantum Moon. (Quantum Moon 0-300m)

Giant's Deep:
 - Anywhere on the Giant's Deep's orbit. (Giant's Deep 750-2000m & Any 0-70m)

White Hole:
 - Anywhere around the White Hole. (White Hole Station 0-1000m & Any 0-50m)

Ash Twins:
 - Inside the sand pillar anywhere on Ember Twin's equator. (Ember Twin 150-200m & Ash Twin 370-375m & Any 0-80)
 - Inside the sand pillar on Ash Twin's equator. (Ash Twin 135-150m & Ember Twin 365-370m)

Hollow's Lantern:
 - Anywhere on the Hollow's Lantern. (Hollow's Lantern 0-300m)

Brittle Hollow:
 - Above the Black Hole Forge. (Brittle Hollow 250-300m & Black Hole Forge 180-200m)

The Interloper:
 - Next to the Shuttle. (Frozen Nomai Shuttle 0-50m)
 - On the North Pole. (Comet 0-100m & Sun 0-3000m)

Dark Bramble:
 - Next to Feldspar's Camp (Feldspar's Camp 0-50m)
 - On the The Vessel (The Vessel 0-300m)
 - Next to Frozen Jellyfish (Frozen Jellyfish 0-50m)

Sun Station:
 - On the Sun Station. (Sun Station 0-2500m)

The Stranger:
 - Anywhere on The Stranger. (The Stranger 0-500m & Any 0-100m)

The Attlerock:
 - On top of the Lunar Lookout. (The Attlerock 80-85m & Lunar Lookout 0-10m)
*/

//DONE: Test with tracking the distance between the player and Village
//  - The tracking will be displayed using the OnGUI method
//DONE: Find a more reliable way to track the distance to locations
//  - markers are a bust for now, Settled with tracking mazes
//DONE: Handle exit conditions <-
//DONE: Handle Disqualify conditions
//DONE: Any locations can be landed multiple times

//TODO: Create configs for which rules to track, and the output directory for runs
//TODO: Allow multiple landings per takeoff

public class PodracingTracker : ModBehaviour
{
    public static PodracingTracker Instance;
    private static PlayerBody player;
    private static PauseMenuManager PauseMenuManager;
    private static CanvasMarkerManager canvasMarkerManager;

    // Experimental Debugging
    private float timer = 0f;
    private float timerStart = 0f;
    private bool debug_triggered = false;
    private ShipLogEntryHUDMarker shipLogEntryHUDMarker;
    private LockOnReticule lockOnReticule; //LockOnReticule
    private PlayerLockOnTargeting playerLockOnTargeting;
    private PlayerCameraController playerCameraController;
    private ShipCockpitUI shipCockpitUI;
    public static bool isLockedOn = false;
    public static IModHelper modHelper;
    public void Debug()
    {
        // check every second
        timer += Time.realtimeSinceStartup - timerStart;
        if (Time.realtimeSinceStartup - timerStart >= 1f)
        {
            timer = 0f;
            timerStart = Time.realtimeSinceStartup;
        }
        else
        {
            return;
        }

        //debug
        //IsModified



        //ModHelper.Console.WriteLine($"Hit: {RuleManager.IsPodracing.hasPodracingExited}");
        /*GUILineManager.SetLine("isPodracing",
            $"<color={(RuleManager.IsPodracing.isPodracing ? "green" : "red")}>{RuleManager.IsPodracing.isPodracing}</color>\t - isPodracing",
            true,
            Corner.TopLeft
        );
        GUILineManager.SetLine("hasPodracingExited",
            $"<color={(RuleManager.IsPodracing.hasPodracingExited ? "green" : "red")}>{RuleManager.IsPodracing.hasPodracingExited}</color>\t - hasPodracingExited",
            true,
            Corner.TopLeft
        );
        //debug
        GUILineManager.SetLine("countFromLoop",
            $"<color={(RuleManager.IsPodracingExit.loopCountDown ? "green" : "red")}>{RuleManager.IsPodracingExit.loopCountDown}</color>\t - countFromLoop",
            true,
            Corner.TopLeft
        );
        GUILineManager.SetLine("isPodracingExit",
            $"<color={(RuleManager.IsPodracingExit.isPodracingExit ? "green" : "red")}>{RuleManager.IsPodracingExit.isPodracingExit}</color>\t - isPodracingExit",
            true,
            Corner.TopLeft
        );
        GUILineManager.SetLine("exitCountdown",
            $"<color={(RuleManager.IsPodracingExit.exitCountdown > 0f ? "yellow" : "red")}>{RuleManager.IsPodracingExit.exitCountdown:0.00}</color>\t - exitCountdown",
            true,
            Corner.TopLeft
        );
        GUILineManager.SetLine("isDisqualified",
            $"<color={(RuleManager.IsPodracing.isDisqualified ? "green" : "red")}>{RuleManager.IsPodracing.isDisqualified}</color>\t - isDisqualified",
            true,
            Corner.TopLeft
        );*/

        /*if (!debug_triggered)
        {
            playerFogWarpDetector = FindObjectOfType<PlayerFogWarpDetector>();
        }
        debug_triggered = true;

        if (playerFogWarpDetector == null)
        {
            return;
        }
        ModHelper.Console.WriteLine("Debugging", MessageType.Info);
        // _outerWarpVolume
        GUILineManager.SetLine("Debug1", $"_outerWarpVolume: {playerFogWarpDetector._outerWarpVolume?.GetName()}", true);
        */

        // print all fog volumes into a file
        /*string path = @"C:\Users\kryst\Desktop\GitProjects\Outer Wilds\misc\FogVolumes.txt";
        using (StreamWriter writer = new StreamWriter(path))
        {
            foreach (ShipLogEntryLocation location in allEntryLocations)
            {
                if (location == null)
                {
                    ModHelper.Console.WriteLine("Location is null", MessageType.Warning);
                    continue;
                }
                var entryID = location.GetEntryID();
                var fogWarpVolume = location.GetOuterFogWarpVolume();
                var fogWarpVolumeName = fogWarpVolume?.GetName();
                ModHelper.Console.WriteLine($"Writing: {entryID} - {fogWarpVolumeName}", MessageType.Info);
                writer.WriteLine($"{entryID} - {fogWarpVolumeName}");
            }
        }*/

       /* try
        {
            // debug
            if (marker == null)
            {
                // find the DB_VESSEL/TH_VILLAGE
                string locationId = "TH_NOMAI_MINE";
                ModHelper.Console.WriteLine($"Debugging: {locationId}", MessageType.Info);
                ShipLogEntryLocation location = allEntryLocations.FirstOrDefault(entry => entry.GetEntryID() == locationId);
                if (location == null)
                {
                    ModHelper.Console.WriteLine($"Location: {locationId} not found", MessageType.Warning);
                    return;
                }
                else
                {
                    ModHelper.Console.WriteLine($"Location: {location.GetEntryID()}", MessageType.Info);
                }
                marker = canvasMarkerManager.InstantiateNewMarker(); // Use a method to create or get a CanvasMarker
                Transform landingTransform = location?.transform;
                ModHelper.Console.WriteLine($"Marker: {locationId} at {landingTransform?.position}", MessageType.Info);

                // Initialize the marker
                marker.Init(canvasMarkerManager._canvas);
                marker.SetMarkerTarget(landingTransform);
                marker.SetLabel(location.GetEntryID());
                marker.SetOuterFogWarpVolume(location.GetOuterFogWarpVolume());
                marker.SetVisibility(true);

                ModHelper.Console.WriteLine($"Marker: {marker.GetMarkerLabelName()} at {marker.GetArrowAnchoredPosition()}", MessageType.Info);
            }
            else
            {
                // update the marker
                // GetMarkerLabelName and GetArrowAnchoredPosition
                GUILineManager.SetLine("Marker1", $"Marker: {marker.GetMarkerLabelName()} at {marker.GetArrowAnchoredPosition()}", true);
                // GetOuterFogWarpVolume
                GUILineManager.SetLine("Marker2", $"FogWarpVolume: {marker.GetOuterFogWarpVolume()?.GetName()}", true);
                // IsVisible
                GUILineManager.SetLine("Marker3", $"IsVisible: {marker.IsVisible()}", true);
                // IsVisibleIgnoreFog
                GUILineManager.SetLine("Marker4", $"IsIgnoreFog: {marker.IsVisibleIgnoreFog()}", true);
                // GetMarkerDistance
                GUILineManager.SetLine("Marker5", $"Distance: {marker.GetMarkerDistance()}", true);
                //.GetWarpDistance
                GUILineManager.SetLine("Marker6", $"WarpDistance: {marker.GetWarpDistance()}", true);
                //.GetRawFogMarkerCount
                GUILineManager.SetLine("Marker7", $"RawFogMarkerCount: {marker.GetRawFogMarkerCount()}", true);
            }
        }
        catch (Exception e)
        {
            ModHelper.Console.WriteLine($"Error: {e}", MessageType.Error);
        }
        finally
        {
            ModHelper.Console.WriteLine("Debugging complete", MessageType.Info);
        }*/
}

    // Startup
    public static bool readyToTrack = false;
    public static bool isInitialized = false;
    public void Awake()
    {
        Instance = this;
        // You won't be able to access OWML's mod helper in Awake.
        // So you probably don't want to do anything here.
        // Use Start() instead.
    }
    public void Start()
    {
        ModHelper.Console.WriteLine($"{nameof(PodracingTracker)} is loaded!", MessageType.Success);
        modHelper = ModHelper;

        new Harmony("Ernesto.PodracingTracker").PatchAll(Assembly.GetExecutingAssembly());

        OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen);

        LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        // connect RuleManager Events
        RuleManager.IsTakeoff.OnTakeoff += OnTakeoff;
        RuleManager.IsPodracing.OnPodracingStart += OnPodracingStarted;
        RuleManager.IsPodracing.OnPodracingCompleted += OnPodracingCompleted;
        RuleManager.IsPodracing.OnPodracingFailed += OnPodracingFailed;

    }
    public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
    {
        readyToTrack = false;
        GUILineManager.ClearLines();
        // Rule B. 3. The run ends one second after waking up in another loop or entering main menu. (The animation time is added to the run)

        if (newScene == OWScene.SolarSystem) {
            // if entering a new loop, the run is deactivated
            ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
        }
        else if (newScene == OWScene.TitleScreen)
        {
            // if entering the main menu, the run is disqualified
            ModHelper.Console.WriteLine("Loaded into title screen!", MessageType.Success);
            return;
        }
        else
        {
            ModHelper.Console.WriteLine("Loaded into unknown scene!", MessageType.Warning);
            return;
        }

        if (RuleManager.IsPodracingExit.isPodracingExit)
        {
            RuleManager.IsPodracing.hasPodracingExited = true;
            RuleManager.IsPodracingExit.isPodracingExit = false;
        }
        else if (RuleManager.IsPodracing.isPodracing)
        {
            RuleManager.IsPodracingExit.loopCountDown = false;
            RuleManager.IsPodracingExit.isPodracingExit = true;
        }


        // Load player
        player = FindObjectOfType<PlayerBody>();

        // Load the other managers
        PauseMenuManager = FindObjectOfType<PauseMenuManager>();
        canvasMarkerManager = FindObjectOfType<CanvasMarkerManager>();
        shipLogEntryHUDMarker = FindObjectOfType<ShipLogEntryHUDMarker>();
        lockOnReticule = FindObjectOfType<LockOnReticule>();
        playerCameraController = FindObjectOfType<PlayerCameraController>();
        shipCockpitUI = FindObjectOfType<ShipCockpitUI>();

        // Initialize the Managers
        GUILineManager.Initialize(ModHelper);
        UtilityTools.Initialize(ModHelper);
        LocationManager.Initialize(ModHelper);
        RuleManager.Initialize(ModHelper,
        [
            // Primary Rules (Podracing)
            new RuleManager.IsPodracing(),
            new RuleManager.IsPodracingExit(),
            new RuleManager.IsTakeoff(),

            // Secondary Rules (Player info)
            new RuleManager.IsPlayerDead(),
            new RuleManager.IsPlayerGrounded(),
            new RuleManager.IsPlayerPilotingShip(),
            new RuleManager.IsPlayerWearingSuit(),
            new RuleManager.IsRunShipless(),

            // Tertiary Rules (Disqualifying tools)
            new RuleManager.IsLockedOn(),
            new RuleManager.IsMarked(),
            new RuleManager.IsNewExpedition(),
            new RuleManager.IsTitleScreen(),
            new RuleManager.IsModified(),
        ]);
        // Load the relevant locations
        LocationManager.GatherLocationTransforms();

        readyToTrack = true;
        isInitialized = true;
    }

    // Main Update Loop
    private AstroObject lastClosestBody = null;
    private readonly List<string> completedLandings = [];
    private readonly List<string> completedAnyLandings = [];
    private Location nearestLocation = null;
    public void Update()
    {
        if (!isInitialized)
            return;

        // process rule related data
        RuleManager.UpdateRules(readyToTrack);

        if (!readyToTrack)
            return;
        //Process location related data
        if (UtilityTools.IsPlayerMoving() && RuleManager.IsPodracing.isPodracing) // Only update the closest body if the player is moving
        {
            UtilityTools.UpdatePlayerPosition();

            var closestBody = UtilityTools.GetClosestAstroObject(player.transform, LocationManager.GetRelevantLocationsTransforms()) ?? lastClosestBody;
            if (closestBody == null)
                {return;}

            if (closestBody != lastClosestBody)
                ModHelper.Console.WriteLine($"Closest AstroObject: {UtilityTools.NameFromAstro(closestBody)??"Unknown"}", MessageType.Info);
                GUILineManager.ClearCorner(Corner.CenterLeft); // Clear the previous data
            lastClosestBody = closestBody;

            // Get all distances for the closest AstroObject
            string bodyId = UtilityTools.playerInMaze == null ? UtilityTools.IdFromAstro(closestBody) : "DarkBramble";
            // Get all landings for the closest AstroObject
            nearestLocation = LocationManager.GetLocationById(bodyId);
            LocationManager.GatherDistances(nearestLocation);
            if (nearestLocation == null)
                {return;}

            //TodoLandings: Displayed on the right, DoneLandings: Displayed on the left
            landingResults = nearestLocation.DisplayLocation();

            // Track landed locations
            RuleManager.IsPodracing.score = $"L{completedLandings.Count:00} T{RuleManager.IsPodracing.podracingTime:00:00.000}";
            GUILineManager.SetLine("completedLandings", $"<b><color=green>{string.Join("\n", completedLandings)}</color></b>", true, Corner.CenterRight);
        }
        //Debug();
    }

    Dictionary<Landing, bool> landingResults = [];

    // Events
    public void OnTakeoff()
    {
        // Check if the player has landed in a qualifying location Using landingResults
        foreach (KeyValuePair<Landing, bool> pair in landingResults)
        {
            Landing landing = pair.Key;
            bool requirementsMet = pair.Value;

            if (requirementsMet) // If the player has landed in a qualifying location, add it to completedLandings and mark it as completed
            {
                var anyRequirement = landing.Requirements.FirstOrDefault(req => req.Type == "Any");
                //ModHelper.Console.WriteLine($"Requirements: {landing.Requirements.Count} Any: {anyRequirement != null}", MessageType.Info);
                if (anyRequirement != null && !completedAnyLandings.Contains(anyRequirement.Id))
                {
                    // if any of the requirements has the type "Any", add the id of the requirement to the completedLandings
                    while (landing.RequirementsMet) // while the any requirement is met, add the id of the requirement to the completedLandings
                    {
                        completedLandings.Add($"{nearestLocation.Name}/{landing.Name}/{anyRequirement.Id}");
                        completedAnyLandings.Add(anyRequirement.Id);
                        ModHelper.Console.WriteLine($"Completed landing: {completedLandings.Last()}", MessageType.Info);
                        landing.IsLanded = true;
                        LocationManager.RemoveAnyLanding(anyRequirement.Id);
                        LocationManager.GatherDistances(nearestLocation);
                    }
                }
                else if (anyRequirement == null && !completedLandings.Contains($"{nearestLocation.Name}/{landing.Name}"))
                {
                    // otherwise, add the name of the landing to the completedLandings
                    completedLandings.Add($"{nearestLocation.Name}/{landing.Name}");
                    ModHelper.Console.WriteLine($"Completed landing: {completedLandings.Last()}", MessageType.Info);
                    landing.IsLanded = true;
                }
            }
        }

        //ModHelper.Console.WriteLine("Takeoff", MessageType.Info);
        //RuleManager.IsTakeoff.isTakeoff = true;
    }
    /// <summary>
    /// OnPodracingStarted is called when the player starts a podracing run.
    /// It clears the completedLandings list.
    /// </summary>
    public void OnPodracingStarted() {
        ModHelper.Console.WriteLine("Podracing Started", MessageType.Info);
        GUILineManager.ClearLines();
        LocationManager.ClearLandingState();
        completedLandings.Clear();
        completedAnyLandings.Clear();
    }
    /// <summary>
    /// OnPodracingCompleted is called when the player completes a podracing run.
    /// It displays the final score and the completed landings.
    /// </summary>
    public void OnPodracingCompleted() {
        ModHelper.Console.WriteLine("Podracing Completed", MessageType.Info);
        GUILineManager.ClearLines();
        GUILineManager.SetLine("score", $"Final score: {RuleManager.IsPodracing.score}", true, Corner.CenterRight);
        foreach (string landing in completedLandings)
        {
            GUILineManager.SetLine(landing, $"<color=green>{landing}</color>", true, Corner.CenterRight);
        }

        // print the completed landings into a document
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "PodracingTracker",
            $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using StreamWriter sw = new(path);
        sw.WriteLine($"Final score: {RuleManager.IsPodracing.score}");
        foreach (string landing in completedLandings)
        {
            sw.WriteLine(landing);
        }
    }
    /// <summary>
    /// OnPodracingFailed is called when the player fails a podracing run.
    /// It displays all the violated rules.
    /// </summary>
    public void OnPodracingFailed() {
        ModHelper.Console.WriteLine("Podracing Failed", MessageType.Info);
        GUILineManager.ClearLines();
        LocationManager.ClearLandingState();
        completedLandings.Clear();
        completedAnyLandings.Clear();
    }
    public void OnGUI()
    {
        if (PauseMenuManager != null && PauseMenuManager.IsOpen())
        {
            return;
        }
        GUILineManager.OnGUI();
    }
}
// List of ShipLogEntryLocation IDs
/*
Any = Any location on the planet
Village = TH_VILLAGE
Timber Hearth = TimberHearth / Nomai Mines = TH_NOMAI_MINE
Quantum Moon = QuantumMoon
Ember Twin = CaveTwin / TowerTwin = TowerTwin / Any = (CT_ANGLERFISH_FOSSIL, CT_CHERT, CT_ESCAPE_POD, CT_GRAVITY_CANNON, CT_HIGH_ENERGY_LAB, CT_LAKEBED_CAVERN, CT_QUANTUM_CAVES, CT_QUANTUM_MOON_LOCATOR, CT_SUNLESS_CITY, CT_WARP_TOWER_MAP)
Giant's Deep = GiantsDeep / Any = (GD_BRAMBLE_ISLAND, GD_CONSTRUCTION_YARD, GD_GABBRO_ISLAND, GD_QUANTUM_TOWER, GD_STATUE_ISLAND, GD_STATUE_WORKSHOP, ORBITAL_PROBE_CANNON)
White Hole Station = WHITE_HOLE_STATION / Any = (BH_QUANTUM_RESEARCH_TOWER)
Frozen Nomai Shuttle = COMET_SHUTTLE
Ash Twin = TowerTwin / CaveTwin
Feldspar's Camp = DB_FELDSPAR
The Vessel = DB_VESSEL
Frozen Jellyfish = DB_FROZEN_JELLYFISH
Hollow's Lantern = VolcanicMoon
Brittle Hollow = BrittleHollow / Black Hole Forge = BH_BLACK_HOLE_FORGE
Sun Station = S_SUNSTATION
The Stranger = IP_RING_WORLD
The Attlerock = TimberMoon / Lunar Lookout = TM_NORTH_POLE

Village = TH_VILLAGE
Timber Hearth = TimberHearth
Quantum Moon = QuantumMoon
Ember Twin / Ash Twin = CaveTwin / TowerTwin = TowerTwin / Any = (CT_ANGLERFISH_FOSSIL, CT_CHERT, CT_ESCAPE_POD, CT_GRAVITY_CANNON, CT_HIGH_ENERGY_LAB, CT_LAKEBED_CAVERN, CT_QUANTUM_CAVES, CT_QUANTUM_MOON_LOCATOR, CT_SUNLESS_CITY, CT_WARP_TOWER_MAP)
Giant's Deep = GiantsDeep / Any = (GD_BRAMBLE_ISLAND, GD_CONSTRUCTION_YARD, GD_GABBRO_ISLAND, GD_QUANTUM_TOWER, GD_STATUE_ISLAND, GD_STATUE_WORKSHOP, ORBITAL_PROBE_CANNON)
White Hole Station = WHITE_HOLE_STATION / Any = (BH_QUANTUM_RESEARCH_TOWER)
Frozen Nomai Shuttle = COMET_SHUTTLE
Ash Twin = TowerTwin / CaveTwin
Feldspar's Camp = DB_FELDSPAR
The Vessel = DB_VESSEL
Frozen Jellyfish = DB_FROZEN_JELLYFISH
Hollow's Lantern = VolcanicMoon
Brittle Hollow = BrittleHollow / Black Hole Forge = BH_BLACK_HOLE_FORGE
Sun Station = S_SUNSTATION
The Stranger = IP_RING_WORLD
The Attlerock = TimberMoon / Lunar Lookout = TM_NORTH_POLE
*/