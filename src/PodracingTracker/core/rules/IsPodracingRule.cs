using System;
using UnityEngine;

namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsPodracing : Rule
    {
        public override string Name => "Podracing";
        public override string Description => "Tracks whether podracing is active, errors countdown to exit, and the time of the run along with number of landings.";
        public override bool PreTracking => true;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

        public static event Action OnPodracingStart;
        public static event Action OnPodracingCompleted;
        public static event Action OnPodracingFailed;

        public static bool isPodracing = false;
        public static bool hasPodracingExited = true;
        public static bool isDisqualified = false;
        public static string score = "L:00, T:00:00.000";
        public static float podracingTime = 0f;
        public static float startPodracingTime = 0f;

        public override void Initialize()
        {
            isDisqualified = false;
        }

        public override void Update()
        {
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
                podracingTime = Time.realtimeSinceStartup - startPodracingTime;
            }

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

            if (IsPodracingExit.isPodracingExit && IsPodracingExit.exitCountdown <= 0f)
            {
                isPodracing = false;
                hasPodracingExited = true;
                IsPodracingExit.isPodracingExit = false;
                IsPodracingExit.loopCountDown = false;
                OnPodracingCompleted?.Invoke();
            }
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
            string statusText;
            string statusColor;
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
                    $"{Name} -> <b><color={statusColor}>{statusText}</color></b>",
                    true,
                    TextCorner
                );
            }

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
}
