using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable IDE0130
namespace PodracingTracker
#pragma warning restore IDE0130
{
    public class Location(string id, string name, List<Landing> landings)
    {
        public string UIid { get; set; } = GUILineManager.GenerateId();
        public string Id { get; set; } = id;
        public string Name { get; set; } = name;
        public List<Landing> Landings { get; set; } = landings;
        public bool LandingsMet => Landings.All(landing => landing.IsLanded);
        public Corner InfoCorner { get; set; } = Corner.CenterLeft;

        public Dictionary<Landing, bool> DisplayLocation() => LandingHudPresenter.DisplayLocation(this);
    }

    public class Landing(string name, string description, List<Requirement> requirements, bool isLanded)
    {
        public string UIid { get; set; } = GUILineManager.GenerateId();
        public string Name { get; set; } = name;
        public string Description { get; set; } = description;
        public List<Requirement> Requirements { get; set; } = requirements;
        public bool IsLanded { get; set; } = isLanded;
        public Corner InfoCorner { get; set; } = Corner.CenterLeft;
        public Corner LandedCorner { get; set; } = Corner.CenterRight;

        public bool RequirementsMet => Requirements.All(
            requirement => requirement.RequirementsMet.Item1 && requirement.RequirementsMet.Item2
        );

        public bool DisplayLanding() => LandingHudPresenter.DisplayLanding(this);
    }

    public class Requirement(string id, int min, int max, float distance)
    {
        public string UIid { get; set; } = GUILineManager.GenerateId();
        public string Id { get; set; } = id;
        public string Type { get; set; }
        public int Min { get; set; } = min;
        public int Max { get; set; } = max;
        public float Distance { get; set; } = distance;
        public bool Hidden { get; set; } = false;
        public Tuple<bool, bool> RequirementsMet => new(Distance >= Min, Distance <= Max);
        public Corner corner { get; set; } = Corner.CenterLeft;

        public void DisplayRequirement() => LandingHudPresenter.DisplayRequirement(this);
    }
}
