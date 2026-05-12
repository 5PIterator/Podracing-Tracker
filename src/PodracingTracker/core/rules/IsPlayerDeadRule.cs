namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsPlayerDead : Rule
    {
        public override string Name => "Dead";
        public override string Description => "Tracks whether the player is dead.";
        public override bool PreTracking => false;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

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
}
