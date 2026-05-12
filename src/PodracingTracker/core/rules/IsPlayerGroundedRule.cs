using UnityEngine;

namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsPlayerGrounded : Rule
    {
        public override string Name => "Grounded";
        public override string Description => "Tracks whether the player is in gravity.";
        public override bool PreTracking => false;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

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
}
