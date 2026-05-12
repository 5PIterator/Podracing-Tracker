using UnityEngine;

namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsPodracingExit : Rule
    {
        public override string Name => "Podracing Exit";
        public override string Description => "Tracks whether the podracing has properly exited and another can be started.";
        public override bool PreTracking => true;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

        public static bool isPodracingExit = false;
        public static bool loopCountDown = false;
        public static float exitCountdown = 1f;
        public static float startExitCountdown = 1f;

        public override void Initialize()
        {
        }

        public override void Update()
        {
            if (isPodracingExit)
            {
                if (loopCountDown)
                {
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
                startExitCountdown = 0f;
                exitCountdown = 1f;
            }
        }

        public override void Display()
        {
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
}
