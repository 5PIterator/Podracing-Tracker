namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsMarked : Rule
    {
        public override string Name => "Marked";
        public override string Description => "Any use of Marked-On HUD automatically disqualifies the run.";
        public override bool PreTracking => false;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

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
}
