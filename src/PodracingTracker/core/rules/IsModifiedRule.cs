using OWML.Common;
using OWML.ModHelper;

namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsModified : Rule
    {
        public override string Name => "Modified";
        public override string Description => "Use of other modifications automatically disqualifies the run.";
        public override bool PreTracking => true;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

        public static bool isModified = false;

        public override void Initialize()
        {
            isModified = false;
        }

        public override void Update()
        {
            isModified = ModHelper.Interaction.GetMods().Count > 1;
        }

        public override void Display()
        {
            if (isModified)
            {
                GUILineManager.SetLine("modified",
                    $"Modified:\n <color={(IsPodracing.isDisqualified ? "red" : "yellow")}>{Description}</color>",
                    true,
                    Corner.CenterRight
                    );
            }
            else
            {
                GUILineManager.RemoveLine("modified");
            }
        }
    }
}
