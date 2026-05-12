using System;
using UnityEngine;

namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsTakeoff : Rule
    {
        public override string Name => "Takeoff";
        public override string Description => "Tracks whether the requirements for a takeoff are met.";
        public override bool PreTracking => false;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

        public static event Action OnTakeoff;

        public static bool isTakeoff = false;
        public static bool touchdown = false;
        public static int contactCount = 0;
        public static bool takeoffPrimed = false;
        public static bool ignitionStart = false;
        public static bool ignitionCancel = false;
        public static bool ignitionComplete = false;

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

            if (!IsRunShipless.isRunShipless)
            {
                contactCount = landingPadManager.GetContactCount();
                touchdown = landingPadManager.IsLanded();
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
            if (IsRunShipless.isRunShipless)
            {

                touchdown = IsPlayerGrounded.isPlayerGrounded;
                if (!touchdown && takeoffPrimed && IsPlayerGrounded.lastTimeGrounded >= 0.8f)
                {
                    isTakeoff = true;
                }

                if (touchdown)
                {
                    takeoffPrimed = true;
                }
                else
                {
                    takeoffPrimed = false;
                }
            }

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
}
