namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsLockedOn : Rule
    {
        public override string Name => "Locked On";
        public override string Description => "Any use of Lock-On automatically disqualifies the run.";
        public override bool PreTracking => false;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

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
}
