namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsTitleScreen : Rule
    {
        public override string Name => "Title Screen";
        public override string Description => "Leaving to the title screen automatically disqualifies the run.";
        public override bool PreTracking => true;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

        public static bool isTitleScreen = false;

        public override void Initialize()
        {
            isTitleScreen = false;
        }

        public override void Update()
        {
            isTitleScreen = LoadManager.GetCurrentScene() == OWScene.TitleScreen;
        }

        public override void Display()
        {
            if (isTitleScreen)
            {
                GUILineManager.SetLine("titleScreen",
                    $"Title Screen:\n <color={(IsPodracing.isDisqualified ? "red" : "yellow")}>{Description}</color>",
                    true,
                    Corner.CenterRight
                    );
            }
            else
            {
                GUILineManager.RemoveLine("titleScreen");
            }
        }
    }
}
