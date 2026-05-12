namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsPlayerWearingSuit : Rule
    {
        public override string Name => "Suit";
        public override string Description => "Tracks whether the player is wearing a spacesuit.";
        public override bool PreTracking => false;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

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
}
