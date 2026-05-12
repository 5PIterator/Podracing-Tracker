using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable IDE0130
namespace PodracingTracker
#pragma warning restore IDE0130
{
    public static class LandingHudPresenter
    {
        public static Dictionary<Landing, bool> DisplayLocation(Location location)
        {
            GUILineManager.SetLine($"{location.UIid}", $"<b>{(location.LandingsMet ? $"<color=green>{location.Name}</color>" : location.Name)}</b>", true, location.InfoCorner, 0);
            Dictionary<Landing, bool> landingResults = [];

            foreach (Landing landing in location.Landings)
            {
                landingResults.Add(landing, DisplayLanding(landing));
            }
            return landingResults;
        }

        public static bool DisplayLanding(Landing landing)
        {
            var anyRequirement = landing.Requirements.FirstOrDefault(req => req.Type == "Any");
            if (anyRequirement != null)
            {
                if (landing.IsLanded)
                {
                    GUILineManager.SetLine($"{landing.UIid}", $"  <b><color=yellow>{landing.Name}</color></b>\n  <i>{landing.Description}</i>", true, landing.InfoCorner);
                }
                else
                {
                    GUILineManager.SetLine($"{landing.UIid}", $"  <b><color=white>{landing.Name}</color></b>\n  <i>{landing.Description}</i>", true, landing.InfoCorner);
                }

                foreach (Requirement requirement in landing.Requirements)
                {
                    requirement.Hidden = LocationManager.mazeLandings.ContainsKey(requirement.Id) && LocationManager.mazeLandings[requirement.Id] != UtilityTools.playerInMaze;
                    DisplayRequirement(requirement);
                }
            }
            else
            {
                if (landing.IsLanded)
                {
                    GUILineManager.SetLine($"{landing.UIid}", $"  <b><color=green>{landing.Name}</color></b>", true, landing.InfoCorner);
                }
                else
                {
                    GUILineManager.SetLine($"{landing.UIid}", $"  <b><color=white>{landing.Name}</color></b>\n  <i>{landing.Description}</i>", true, landing.InfoCorner);
                }
                foreach (Requirement requirement in landing.Requirements)
                {
                    requirement.Hidden =
                        LocationManager.mazeLandings.ContainsKey(requirement.Id) &&
                        LocationManager.mazeLandings[requirement.Id] != UtilityTools.playerInMaze;
                    DisplayRequirement(requirement);
                }
            }
            return landing.RequirementsMet;
        }

        public static void DisplayRequirement(Requirement requirement)
        {
            var (minMet, maxMet) = requirement.RequirementsMet;

            string minText = minMet ? $"<color=green>{requirement.Min}</color>" : $"<color=red>{requirement.Min}</color>";
            string maxText = maxMet ? $"<color=green>{requirement.Max}</color>" : $"<color=red>{requirement.Max}</color>";

            if (minMet && maxMet)
            {
                minText = $"<color=green>{requirement.Min}</color>";
                maxText = $"<color=green>{requirement.Max}</color>";
            }
            else if (minMet || maxMet)
            {
                minText = minMet ? $"<color=yellow>{requirement.Min}</color>" : $"<color=red>{requirement.Min}</color>";
                maxText = maxMet ? $"<color=yellow>{requirement.Max}</color>" : $"<color=red>{requirement.Max}</color>";
            }

            string idText;
            string distanceText;
            string detailsText;
            if (requirement.Type == "Any")
            {
                idText = $"{requirement.Type}";
                detailsText = $"({requirement.Id})";
            }
            else
            {
                idText = $"{requirement.Id}";
                detailsText = $"({requirement.Type})";
            }
            detailsText = LocationManager.showLandingDetails ? detailsText : "";
            distanceText = requirement.Distance < 10000 && !requirement.Hidden ? requirement.Distance.ToString("0.00") : "####.##";

            string statusText = $"    {minText}<{distanceText}<{maxText}\t - {idText} {detailsText}";
            GUILineManager.SetLine($"{requirement.UIid}", statusText, true, requirement.corner);
        }
    }
}
