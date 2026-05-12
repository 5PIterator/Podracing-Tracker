namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsNewExpedition : Rule
    {
        public override string Name => "New Expedition";
        public override string Description => "Runs are disqualified if the player is in a new Expedition mode (at least two loops).";
        public override bool PreTracking => false;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

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
}
