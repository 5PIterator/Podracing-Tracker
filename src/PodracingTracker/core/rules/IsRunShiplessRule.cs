using UnityEngine;

namespace PodracingTracker;

public partial class RuleManager
{
    public partial class IsRunShipless : Rule
    {
        public override string Name => "Shipless";
        public override string Description => "Tracks whether the player is more than 100m away from the ship.";
        public override bool PreTracking => false;
        public override bool AllowUpdate { get; set; }
        public override bool AllowDisplay { get; set; }
        public override Corner TextCorner { get; set; }

        public static bool isRunShipless = false;

        public override void Initialize()
        {
            isRunShipless = false;
        }

        public override void Update()
        {
            if (shipDamageController.IsDestroyed())
                isRunShipless = true;
            else
                isRunShipless = Vector3.Distance(player.transform.position, ship.transform.position) > 100f;
        }

        public override void Display()
        {
            if (IsPodracing.isPodracing && isRunShipless)
            {
                GUILineManager.SetLine("shipless",
                    $"Shipless: <color={(isRunShipless ? "green" : "red")}>{isRunShipless}</color>",
                    true,
                    TextCorner
                );
            }
        }
    }
}
