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
using Epic.OnlineServices.Platform;
using UnityEngine.Events;
using OWML.Utils;

namespace PodracingTracker
{
    public class RuleManager
    {
        public abstract class Rule
        {
            /// <summary>
            /// The name of the rule.
            /// </summary>
            public abstract string Name { get; }
            /// <summary>
            /// The description of the rule.
            /// </summary>
            public abstract string Description { get; }
            /// <summary>
            /// Whether the rule can be tracked before the tracking is ready.
            /// </summary>
            public abstract bool PreTracking { get; }
            public abstract bool AllowUpdate { get; set; }
            public abstract bool AllowDisplay { get; set; }
            public abstract Corner TextCorner { get; set; }
            /// <summary>
            /// The method to initialize the rule.
            /// </summary>
            public abstract void Initialize();
            /// <summary>
            /// The method to update the rule.
            /// </summary>
            public abstract void Update();
            /// <summary>
            /// The method to display the rule.
            /// </summary>
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

        public static void Initialize(IModHelper ModHelper, List<Rule> rules)
        {
            Rules = rules.Where(rule => rule != null).ToList();
            RuleDictionary = Rules.ToDictionary(rule => rule.Name, rule => rule);

            RuleManager.ModHelper = ModHelper;
            var allComponents = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();

            player = allComponents.OfType<PlayerBody>().FirstOrDefault();
            ship = allComponents.OfType<ShipBody>().FirstOrDefault();
            playerController = allComponents.OfType<PlayerCharacterController>().FirstOrDefault();
            landingPadManager = allComponents.OfType<LandingPadManager>().FirstOrDefault();
            shipThrusterController = allComponents.OfType<ShipThrusterController>().FirstOrDefault();
            shipDamageController = allComponents.OfType<ShipDamageController>().FirstOrDefault();

            // Reset the Podracing rules
            InitializeRules();
        }

        /// <summary>
        /// Whether the podracing run is active.
        /// </summary>
        public class IsPodracing : Rule
        {
            // default properties
            public override string Name => "Podracing";
            public override string Description => "Tracks whether podracing is active, errors countdown to exit, and the time of the run along with number of landings.";
            public override bool PreTracking => true;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // events
            public static event Action OnPodracingStart;
            public static event Action OnPodracingCompleted;
            public static event Action OnPodracingFailed;

            // properties
            /// <summary>
            /// Whether the podracing run is active.
            /// </summary>
            public static bool isPodracing = false;
            /// <summary>
            /// Whether the podracing run has exited and another run can start.
            /// </summary>
            public static bool hasPodracingExited = true;
            /// <summary>
            /// Whether the player triggers a disqualifying event.
            /// </summary>
            public static bool isDisqualified = false;
            /// <summary>
            /// The podracing score. L: Landings, T: Time.
            /// </summary>
            public static string score = "L:00 T:00:00.000";
            /// <summary>
            /// The time in seconds the podracing run has been active.
            /// </summary>
            public static float podracingTime = 0f;
            public static float startPodracingTime = 0f;


            public override void Initialize()
            {
                isDisqualified = false;
            }

            public override void Update()
            {
                //B, Start and End:
                if (isPodracing)
                {
                    if (
                        IsNewExpedition.isNewExpedition ||
                        IsLockedOn.isLockedOn ||
                        IsTitleScreen.isTitleScreen ||
                        IsMarked.isMarked ||
                        IsModified.isModified
                        )
                    {
                        isDisqualified = true;
                        isPodracing = false;
                        hasPodracingExited = true;
                        OnPodracingFailed?.Invoke();
                        return;
                    }
                    // run is disqualified by disqualifying rule
                    podracingTime = Time.realtimeSinceStartup - startPodracingTime;
                }

                // run is completed if the player is in gravity for more than 1 second
                if (
                    isPodracing &&
                    IsPlayerWearingSuit.isPlayerWearingSuit &&
                    IsPlayerGrounded.isPlayerGrounded &&
                    IsPlayerGrounded.timeGrounded >= 1f
                    )
                {
                    isPodracing = false;
                    IsPodracingExit.isPodracingExit = true;
                    IsPodracingExit.loopCountDown = true;
                    OnPodracingCompleted?.Invoke();
                }

                // run is completed if the player enters a new loop or the main menu
                if (IsPodracingExit.isPodracingExit && IsPodracingExit.exitCountdown <= 0f)
                {
                    isPodracing = false;
                    hasPodracingExited = true;
                    IsPodracingExit.isPodracingExit = false;
                    IsPodracingExit.loopCountDown = false;
                    OnPodracingCompleted?.Invoke();
                }
                //B1. The run starts the instant you interact with the cockpit of your ship while wearing a spacesuit.
                if (
                    !isDisqualified &&
                    !isPodracing &&
                    IsPlayerWearingSuit.isPlayerWearingSuit &&
                    IsPlayerPilotingShip.isPlayerPilotingShip &&
                    hasPodracingExited
                    )
                {
                    isPodracing = true;
                    hasPodracingExited = false;
                    podracingTime = 0f;
                    startPodracingTime = Time.realtimeSinceStartup;
                    OnPodracingStart?.Invoke();
                }
            }

            public override void Display()
            {
                // before run, show general podracing status
                // red- if not podracing, yellow- if near ship and equipped with suit, green- if podracing
                // "Idle/Primed/Running"
                string statusText;
                string statusColor;
                // Display disqualifying rule
                if (isDisqualified)
                {
                    statusColor = "red";
                    GUILineManager.SetLine($"{Name}",
                        $"<b><color={statusColor}>DISQUALIFIED</color></b>",
                        true,
                        TextCorner
                    );
                    return;
                }

                if (!isPodracing)
                {
                    // show warning if in new expedition
                    if (IsPodracingExit.isPodracingExit)
                    {
                        statusColor = "red";
                        TimeSpan timeSpan = TimeSpan.FromSeconds(IsPodracingExit.exitCountdown);
                        statusText = $"Exit in: ({string.Format("{0:D2}:{1:D2}.{2:D3}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds)})";
                    }
                    else if (!IsRunShipless.isRunShipless && IsPlayerWearingSuit.isPlayerWearingSuit)
                    {
                        statusColor = "yellow";
                        statusText = "Primed";
                    }
                    else
                    {
                        statusColor = "red";
                        statusText = "Ready";
                    }

                    GUILineManager.SetLine($"{Name}",
                        //$"<color={statusColor}>{statusText}</color>\t - Podracing",
                        $"{Name} -> <b><color={statusColor}>{statusText}</color></b>",
                        true,
                        TextCorner
                    );
                }

                // during run, show time and exit status
                // white- while running, yellow- while grounded, red- while exiting or grounded for more than 1 second

                if (isPodracing)
                {

                    if (IsPodracingExit.isPodracingExit || IsPlayerGrounded.timeGrounded >= 1f)
                    {
                        statusColor = "red";
                    }
                    else if (IsPlayerGrounded.isPlayerGrounded)
                    {
                        statusColor = "yellow";
                    }
                    else
                    {
                        statusColor = "white";
                    }

                    GUILineManager.SetLine("podracing",
                        $"<b><color={statusColor}>{score}</color></b>",
                        true,
                        TextCorner
                    );
                }
            }
        }
        /// <summary>
        /// Whether the podracing run is exiting.
        /// </summary>
        public class IsPodracingExit : Rule
        {
            // override properties
            public override string Name => "Podracing Exit";
            public override string Description => "Tracks whether the podracing has properly exited and another can be started.";
            public override bool PreTracking => true;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // events
            public static event Action OnPodracingExit;

            // properties
            /// <summary>
            /// Whether the podracing run is exiting.
            /// </summary>
            public static bool isPodracingExit = false;

            /// <summary>
            /// Whether the podracing run is counting down to exit using the timeLoop.
            /// </summary>
            public static bool loopCountDown = false;
            /// <summary>
            /// The time in seconds the podracing run will take to exit.
            /// </summary>
            public static float exitCountdown = 1f;
            public static float startExitCountdown = 1f;

            // methods
            public override void Initialize()
            {
                // isPodracingExit = false;
                // loopCountDown = false;
                // exitCountdown = 1f;
                // startExitCountdown = 1f; // disabled for cross loop functionality
            }

            /// <summary>
            /// Updates the exit countdown when the player is in a podracing exit.
            /// </summary>
            /// <remarks>
            /// If the player is in a podracing exit, this method counts down the time until the exit ends.
            /// The countdown is reset to 1 second whenever the player leaves the exit.
            /// </remarks>
            public override void Update()
            {

                //ModHelper.Console.WriteLine($"Hit IsPodracingExit.isPodracingExit: {isPodracingExit}", MessageType.Info);

                if (isPodracingExit)
                {
                    if (loopCountDown)
                    {
                        //ModHelper.Console.WriteLine($"Hit IsPodracingExit.countFromLoop: {countFromLoop}", MessageType.Info);
                        exitCountdown = TimeLoop.GetSecondsRemaining();
                    }
                    else if (startExitCountdown == 0f)
                    {
                        startExitCountdown = Time.realtimeSinceStartup;
                    }
                    else if (exitCountdown > 0f)
                    {
                        exitCountdown = startExitCountdown + 1f - Time.realtimeSinceStartup;
                    }
                }
                else
                {
                    //ModHelper.Console.WriteLine($"Hit IsPodracingExit.isPodracingExit: {isPodracingExit}", MessageType.Info);
                    startExitCountdown = 0f;
                    exitCountdown = 1f;
                }
            }

            public override void Display()
            {
                // display exit status only when active
                if (isPodracingExit)
                {
                    GUILineManager.SetLine("exit",
                        $"Podracing Exit: <color={(exitCountdown > 0f ? "yellow" : "red")}>{exitCountdown:0.00}</color>",
                        true,
                        TextCorner
                    );
                }
            }
        }
        /// <summary>
        /// Whether the player performs a qualifying takeoff.
        /// </summary>
        public class IsTakeoff : Rule
        {
            // override properties
            public override string Name => "Takeoff";
            public override string Description => "Tracks whether the requirements for a takeoff are met.";
            public override bool PreTracking => false;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // events
            public static event Action OnTakeoff;

            // properties
            /// <summary>
            /// Whether the takeoff requirements are met.
            /// </summary>
            public static bool isTakeoff = false;
            /// <summary>
            /// Whether the ship is in a touchdown state.
            /// </summary>
            public static bool touchdown = false;
            /// <summary>
            /// The number of landing pad contacts.
            /// </summary>
            public static int contactCount = 0;
            /// <summary>
            /// Whether the ship is primed for a qualifying takeoff.
            /// </summary>
            public static bool takeoffPrimed = false;
            /// <summary>
            /// Whether the ship is taking off.
            /// </summary>
            public static bool ignitionStart = false;
            /// <summary>
            /// Whether the ship has cancelled ignition.
            /// </summary>
            public static bool ignitionCancel = false;
            /// <summary>
            /// Whether the ship has completed ignition.
            /// </summary>
            public static bool ignitionComplete = false;

            /// <summary>
            /// The time in seconds after a qualifying takeoff.
            /// </summary>
            public static float takeoffTime = 0f;
            public static float startTakeoffTime = 0f;

            public override void Initialize()
            {
                isTakeoff = false;
                touchdown = false;
                contactCount = 0;
                takeoffPrimed = false;
                ignitionStart = false;
                ignitionCancel = false;
                ignitionComplete = false;
                takeoffTime = 0f;
                startTakeoffTime = 0f;
            }
            public override void Update()
            {
                // debug
                /*GUILineManager.SetLine("isTakeoff",
                    $"<color={(isTakeoff ? "green" : "red")}>{isTakeoff}</color>\t - Takeoff",
                    true,
                    Corner.CenterRight
                );
                GUILineManager.SetLine("touchdown",
                    $"<color={(touchdown ? "green" : "red")}>{touchdown}</color>\t - Touchdown",
                    true,
                    Corner.CenterRight
                );
                GUILineManager.SetLine("takeoffPrimed",
                    $"<color={(takeoffPrimed ? "green" : "red")}>{takeoffPrimed}</color>\t - takeoffPrimed",
                    true,
                    Corner.CenterRight
                );
                GUILineManager.SetLine("RequiresIgnition", // is set to true when the ship requires ignition to take off, Does not reset when ship is pushed out of the state
                    $"<color={(shipThrusterController.RequiresIgnition() ? "green" : "red")}>{shipThrusterController.RequiresIgnition()}</color>\t - RequiresIgnition",
                    true,
                    Corner.CenterRight
                );
                GUILineManager.SetLine("ignitionStart",
                    $"<color={(ignitionStart ? "green" : "red")}>{ignitionStart}</color>\t - Ignition Start",
                    true,
                    Corner.CenterRight
                );
                GUILineManager.SetLine("ignitionCancel",
                    $"<color={(ignitionCancel ? "green" : "red")}>{ignitionCancel}</color>\t - Ignition Cancel",
                    true,
                    Corner.CenterRight
                );
                GUILineManager.SetLine("ignitionComplete",
                    $"<color={(ignitionComplete ? "green" : "red")}>{ignitionComplete}</color>\t - Ignition Complete",
                    true,
                    Corner.CenterRight
                );*/

                // is takeoff is set to true for the duration of the ignition
                if (takeoffTime >= 1f)
                {
                    isTakeoff = false;
                }
                else if (isTakeoff)
                {
                    takeoffTime = Time.realtimeSinceStartup - startTakeoffTime;
                }
                else
                {
                    startTakeoffTime = Time.realtimeSinceStartup;
                    takeoffTime = 0f;
                }

                // if piloting, isTakeoff is determined by whether the ship takes off from the primed state
                if (!IsRunShipless.isRunShipless)
                {
                    contactCount = landingPadManager.GetContactCount();
                    touchdown = landingPadManager.IsLanded();
                    //takeoffPrimed = shipThrusterController.RequiresIgnition() && !ignitionCancel && touchdown;
                    //isTakeoff = takeoffPrimed && ignitionComplete;
                    if (ignitionCancel)
                    {
                        takeoffPrimed = false;
                    }
                    else if (
                        takeoffPrimed &&
                        ignitionComplete
                        )
                    {
                        isTakeoff = true;
                        takeoffPrimed = false;
                    }
                    else if (touchdown && shipThrusterController.RequiresIgnition())
                    {
                        takeoffPrimed = true;
                    }

                }
                //debug
                //GUILineManager.SetLine("isTakeoff",
                //    $"<color={(isTakeoff ? "green" : "red")}>{isTakeoff}</color>\t - isTakeoff",
                //    true,
                //    Corner.TopLeft
                //);
                //GUILineManager.SetLine("touchdown",
                //    $"<color={(touchdown ? "green" : "red")}>{touchdown}</color>\t - Touchdown",
                //    true,
                //    Corner.TopLeft
                //);
                //GUILineManager.SetLine("isRunShipless",
                //    $"<color={(IsRunShipless.isRunShipless ? "green" : "red")}>{IsRunShipless.isRunShipless}</color>\t - IsRunShipless",
                //    true,
                //    Corner.TopLeft
                //);
                //GUILineManager.SetLine("isPlayerGrounded",
                //    $"<color={(IsPlayerGrounded.isPlayerGrounded ? "green" : "red")}>{IsPlayerGrounded.isPlayerGrounded}</color>\t - IsPlayerGrounded",
                //    true,
                //    Corner.TopLeft
                //);
                //GUILineManager.SetLine("takeoffPrimed",
                //    $"<color={(takeoffPrimed ? "green" : "red")}>{takeoffPrimed}</color>\t - takeoffPrimed",
                //    true,
                //    Corner.TopLeft
                //);
                // if shipless, isTakeoff is determined by the player's isGrounded status
                if (IsRunShipless.isRunShipless)
                {

                    touchdown = IsPlayerGrounded.isPlayerGrounded;
                    // if player is no longer grounded, takeoff is primed, and grounded for more than 0.8 seconds, takeoff
                    if (!touchdown && takeoffPrimed && IsPlayerGrounded.lastTimeGrounded >= 0.8f)
                    {
                        isTakeoff = true;
                    }

                    //prime takeoff if player is grounded
                    if (touchdown)
                    {
                        takeoffPrimed = true;
                    }
                    else
                    {
                        takeoffPrimed = false;
                    }
                }

                // if dead, isTakeoff is determined by the player's death
                if (IsPlayerDead.isPlayerDead)
                {
                    isTakeoff = true;
                }

                if (isTakeoff)
                {
                    ignitionStart = false;
                    ignitionCancel = false;
                    ignitionComplete = false;
                    OnTakeoff?.Invoke();
                }
            }

            public override void Display()
            {
                if (IsRunShipless.isRunShipless)
                {
                    string statusText = isTakeoff ? "Takeoff" : IsPlayerGrounded.lastTimeGrounded >= 0.8f ? "Primed" : takeoffPrimed ? "Ready" : touchdown ? "Grounded" : "Airborne";
                    string statusColor = isTakeoff ? "green" : IsPlayerGrounded.lastTimeGrounded >= 0.8f ? "yellow" : "red";
                    GUILineManager.SetLine("takeoff",
                        $"{(IsPodracing.isPodracing ? "" : "Takeoff -> ")}<color={statusColor}>{statusText}</color>",
                        true,
                        TextCorner
                    );
                }
                else
                {
                    string statusText = isTakeoff ? "Takeoff" : takeoffPrimed ? "Primed" : contactCount == 3 ? "Ready" : contactCount > 0 ? "Contact" : "Airborne";
                    string statusColor = ignitionStart || isTakeoff ? "green" : takeoffPrimed ? "yellow" : "red";
                    GUILineManager.SetLine("takeoff",
                        $"{(IsPodracing.isPodracing ? "" : "Takeoff -> ")}<color={statusColor}>{statusText} ({contactCount}/3)</color>",
                        true,
                        TextCorner
                    );
                }
            }
        }
        /// <summary>
        /// Whether the player is more than 100m away from the ship.
        /// </summary>
        public class IsRunShipless : Rule
        {
            // override properties
            public override string Name => "Shipless";
            public override string Description => "Tracks whether the player is more than 100m away from the ship.";
            public override bool PreTracking => false;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            public static bool isRunShipless = false;

            public override void Initialize()
            {
                isRunShipless = false;
            }

            public override void Update()
            {
                //debug
                //GUILineManager.SetLine("IsDestroyed",
                //    $"<color={(shipDamageController.IsDestroyed() ? "red" : "green")}>{shipDamageController.IsDestroyed()}</color>\t - IsDestroyed",
                //    true,
                //    Corner.CenterRight
                //);
                //GUILineManager.SetLine("IsHullBreached",
                //    $"<color={(shipDamageController.IsHullBreached() ? "red" : "green")}>{shipDamageController.IsHullBreached()}</color>\t - IsHullBreached",
                //    true,
                //    Corner.CenterRight
                //);
                if (shipDamageController.IsDestroyed())
                isRunShipless = true;
                else
                isRunShipless = Vector3.Distance(player.transform.position, ship.transform.position) > 100f;
            }

            public override void Display()
            {
                if (IsPodracing.isPodracing && isRunShipless)
                {
                    GUILineManager.SetLine("shipless",
                        $"Shipless: <color={(isRunShipless ? "green" : "red")}>{isRunShipless}</color>",
                        true,
                        TextCorner
                    );
                }
            }
        }

        /// <summary>
        /// Whether the player is wearing a spacesuit.
        /// </summary>
        public class IsPlayerWearingSuit : Rule
        {
            // override properties
            public override string Name => "Suit";
            public override string Description => "Tracks whether the player is wearing a spacesuit.";
            public override bool PreTracking => false;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // properties
            public static bool isPlayerWearingSuit = false;

            public override void Initialize()
            {
                isPlayerWearingSuit = false;
            }

            public override void Update()
            {
                isPlayerWearingSuit = PlayerState.IsWearingSuit();
            }

            public override void Display()
            {
                GUILineManager.SetLine("suit",
                    $"Suit: <color={(isPlayerWearingSuit ? "green" : "red")}>{isPlayerWearingSuit}</color>",
                    true,
                    Corner.CenterRight
                );
            }
        }

        /// <summary>
        /// Whether the player is in gravity.
        /// </summary>
        public class IsPlayerGrounded : Rule
        {
            // override properties
            public override string Name => "Grounded";
            public override string Description => "Tracks whether the player is in gravity.";
            public override bool PreTracking => false;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // properties
            public static bool isPlayerGrounded = false;
            public static float lastTimeGrounded = 0f;
            public static float timeGrounded = 0f;
            public static float startTimeGrounded = 0f;

            public override void Initialize()
            {
                lastTimeGrounded = 0f;
                isPlayerGrounded = false;
                timeGrounded = 0f;
                startTimeGrounded = 0f;
            }
            public override void Update()
            {
                isPlayerGrounded = playerController.IsGrounded();

                if (isPlayerGrounded)
                {
                    if (startTimeGrounded == 0f)
                    {
                        startTimeGrounded = Time.realtimeSinceStartup;
                    }

                    if (timeGrounded < 1f)
                    {
                        timeGrounded = Time.realtimeSinceStartup - startTimeGrounded;
                        lastTimeGrounded = IsPodracing.isPodracing ? timeGrounded : 0f;
                    }
                    else
                    {
                        timeGrounded = 1f;
                    }
                }
                else
                {
                    startTimeGrounded = 0f;
                    timeGrounded = 0f;
                }
            }

            public override void Display()
            {
                if (timeGrounded >= 1f)
                {
                    GUILineManager.SetLine("grounded",
                        $"{(IsPodracing.isPodracing ? "" : "Grounded -> ")}<color=red>{timeGrounded:0.00}</color>",
                        true,
                        TextCorner
                    );
                }
                else
                {
                    GUILineManager.SetLine("grounded",
                        $"{(IsPodracing.isPodracing ? "" : "Grounded -> ")}<color={(isPlayerGrounded ? "yellow" : "green")}>{timeGrounded:0.00}</color>",
                        true,
                        TextCorner
                    );
                }
            }
        }

        /// <summary>
        /// Whether the player is piloting the ship.
        /// </summary>
        public class IsPlayerPilotingShip : Rule
        {
            // override properties
            public override string Name => "Piloting";
            public override string Description => "Tracks whether the player is piloting the ship.";
            public override bool PreTracking => false;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // properties
            public static bool isPlayerPilotingShip = false;

            public override void Initialize()
            {
                isPlayerPilotingShip = false;
            }

            public override void Update()
            {
                isPlayerPilotingShip = PlayerState.AtFlightConsole();
            }

            public override void Display()
            {
                GUILineManager.SetLine("piloting",
                    $"Piloting: <color={(isPlayerPilotingShip ? "green" : "red")}>{isPlayerPilotingShip}</color>",
                    true,
                    Corner.CenterRight
                );
            }
        }

        /// <summary>
        /// Whether the player is dead.
        /// </summary>
        public class IsPlayerDead : Rule
        {
            // override properties
            public override string Name => "Dead";
            public override string Description => "Tracks whether the player is dead.";
            public override bool PreTracking => false;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // properties
            public static bool isPlayerDead = false;

            public override void Initialize()
            {
                isPlayerDead = false;
            }
            public override void Update()
            {
                isPlayerDead = PlayerState.IsDead();
            }

            public override void Display()
            {
                GUILineManager.SetLine("dead",
                    $"Dead: <color={(isPlayerDead ? "red" : "green")}>{isPlayerDead}</color>",
                    true,
                    Corner.CenterRight
                );
            }
        }

        //// Disqualifying Rules

        /// <summary>
        /// Whether the player is in a new Expedition mode or Resume Expedition mode.
        /// </summary>
        public class IsNewExpedition : Rule
        {
            // override properties
            public override string Name => "New Expedition";
            public override string Description => "Runs are disqualified if the player is in a new Expedition mode (at least two loops).";
            public override bool PreTracking => false;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // properties
            public static bool isNewExpedition = false;

            public override void Initialize()
            {
                isNewExpedition = false;
            }

            public override void Update()
            {
                isNewExpedition = PlayerData.LoadLoopCount() < 2;
            }

            public override void Display()
            {
                if (isNewExpedition)
                {
                    GUILineManager.SetLine("expedition",
                        $"New Expedition:\n <color={(IsPodracing.isDisqualified ? "red" : "yellow")}>{Description}</color>",
                        true,
                        Corner.CenterRight
                    );
                }
                else
                {
                    GUILineManager.RemoveLine("expedition");
                }
            }
        }

        public class IsLockedOn : Rule
        {
            // override properties
            public override string Name => "Locked On";
            public override string Description => "Any use of Lock-On automatically disqualifies the run.";
            public override bool PreTracking => false;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // properties
            public static bool isLockedOn = false;

            public override void Initialize()
            {
                isLockedOn = false;
            }

            public override void Update()
            {
                isLockedOn = Locator.GetReferenceFrame() != null;
            }
            public override void Display()
            {
                if (isLockedOn)
                {
                    GUILineManager.SetLine("lockedOn",
                    $"Locked-On:\n <color={(IsPodracing.isDisqualified ? "red" : "yellow")}>{Description}</color>",
                    true,
                    Corner.CenterRight
                    );
                }
                else
                {
                    GUILineManager.RemoveLine("lockedOn");
                }
            }
        }

        public class IsMarked : Rule
        {
            // override properties
            public override string Name => "Marked";
            public override string Description => "Any use of Marked-On HUD automatically disqualifies the run.";
            public override bool PreTracking => false;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // properties
            public static bool isMarked = false;

            public override void Initialize()
            {
                isMarked = false;
            }

            public override void Update()
            {
                isMarked = ShipLogEntryHUDMarker.s_entryLocation != null;
            }

            public override void Display()
            {
                if (isMarked)
                {
                    GUILineManager.SetLine("marked",
                    $"Marked-On HUD:\n <color={(IsPodracing.isDisqualified ? "red" : "yellow")}>{Description}</color>",
                    true,
                    Corner.CenterRight
                    );
                }
                else
                {
                    GUILineManager.RemoveLine("marked");
                }
            }
        }

        public class IsTitleScreen : Rule
        {
            // override properties
            public override string Name => "Title Screen";
            public override string Description => "Leaving to the title screen automatically disqualifies the run.";
            public override bool PreTracking => true;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // properties
            public static bool isTitleScreen = false;

            public override void Initialize()
            {
                isTitleScreen = false;
            }

            public override void Update()
            {
                isTitleScreen = LoadManager.GetCurrentScene() == OWScene.TitleScreen;
            }

            public override void Display()
            {
                if (isTitleScreen)
                {
                    GUILineManager.SetLine("titleScreen",
                    $"Title Screen:\n <color={(IsPodracing.isDisqualified ? "red" : "yellow")}>{Description}</color>",
                    true,
                    Corner.CenterRight
                    );
                }
                else
                {
                    GUILineManager.RemoveLine("titleScreen");
                }
            }
        }

        public class IsModified : Rule
        {
            // override properties
            public override string Name => "Modified";
            public override string Description => "Use of other modifications automatically disqualifies the run.";
            public override bool PreTracking => true;
            public override bool AllowUpdate { get; set; }
            public override bool AllowDisplay { get; set; }
            public override Corner TextCorner { get; set; }

            // properties
            public static bool isModified = false;

            public override void Initialize()
            {
                isModified = false;
            }

            public override void Update()
            {
                isModified = ModHelper.Interaction.GetMods().Count > 1;
            }

            public override void Display()
            {
                if (isModified)
                {
                    GUILineManager.SetLine("modified",
                    $"Modified:\n <color={(IsPodracing.isDisqualified ? "red" : "yellow")}>{Description}</color>",
                    true,
                    Corner.CenterRight
                    );
                }
                else
                {
                    GUILineManager.RemoveLine("modified");
                }
            }
        }

        public static void InitializeRules()
        {
            foreach (Rule rule in Rules)
            {
                rule.AllowUpdate = ModHelper.Config.GetSettingsValue<string>(rule.GetType().Name).Contains("Track");
                rule.AllowDisplay = ModHelper.Config.GetSettingsValue<string>(rule.GetType().Name).Contains("Display");
                rule.TextCorner = (Corner)Enum.Parse(typeof(Corner), ModHelper.Config.GetSettingsValue<string>($"{rule.GetType().Name} Gui"));
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
                    {rule.Update();}

                if (rule.AllowDisplay)
                    {rule.Display();}
            }
        }
    }
}
