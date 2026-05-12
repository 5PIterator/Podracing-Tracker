namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsPlayerPilotingShip : Rule
    {
        public override string Name => "Piloting";
        public override string Description => "Tracks whether the player is piloting the ship.";
        public override bool PreTracking => false;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

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
}
