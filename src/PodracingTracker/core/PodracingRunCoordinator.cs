using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;

namespace PodracingTracker;

/// <summary>
/// Owns per-run landing progress, nearest-location tracking, and score display/output for a podracing session.
/// </summary>
public sealed class PodracingRunCoordinator
{
    private readonly IModHelper _modHelper;

    private AstroObject _lastClosestBody;
    private readonly List<string> _completedLandings = [];
    private readonly List<string> _completedAnyLandings = [];
    private Location _nearestLocation;
    private Dictionary<Landing, bool> _landingResults = [];

    public PodracingRunCoordinator(IModHelper modHelper)
    {
        _modHelper = modHelper;
    }

    public Location NearestLocation => _nearestLocation;

    public bool IsTrainingOverlay() =>
        _modHelper.Config.GetSettingsValue<bool>("Training overlay");

    public void UpdateNearestLocationAndLandings(PlayerBody player)
    {
        UtilityTools.UpdatePlayerPosition();

        var closestBody = UtilityTools.GetClosestAstroObject(player.transform, LocationManager.GetRelevantLocationsTransforms()) ?? _lastClosestBody;
        if (closestBody == null)
        {
            _nearestLocation = null;
            return;
        }

        if (closestBody != _lastClosestBody)
        {
            bool isRingWorld = UtilityTools.IdFromAstro(closestBody) == "RingWorld";
            bool isWithinDistance = Vector3.Distance(player.transform.position, closestBody.transform.position) <= 500;

            if (!isRingWorld || isWithinDistance)
            {
                _modHelper.Console.WriteLine($"Closest AstroObject: {UtilityTools.NameFromAstro(closestBody) ?? "Unknown"}", MessageType.Info);
                GUILineManager.ClearCorner(Corner.CenterLeft);
                _lastClosestBody = closestBody;
            }
            else
            {
                closestBody = _lastClosestBody;
            }
        }

        string bodyId = UtilityTools.playerInMaze == null ? UtilityTools.IdFromAstro(closestBody) : "DarkBramble";
        _nearestLocation = LocationManager.GetLocationById(bodyId);
        LocationManager.GatherDistances(_nearestLocation);
        if (_nearestLocation == null)
            return;

        _landingResults = _nearestLocation.DisplayLocation();

        RuleManager.IsPodracing.score = $"L{_completedLandings.Count:00}, T{RuleManager.IsPodracing.podracingTime.ToString("00:00.000", CultureInfo.InvariantCulture)}";
        GUILineManager.SetLine("completedLandings", $"<b><color=green>{string.Join("\n", _completedLandings)}</color></b>", true, Corner.CenterRight);
    }

    public void OnTakeoff()
    {
        foreach (KeyValuePair<Landing, bool> pair in _landingResults)
        {
            Landing landing = pair.Key;
            bool requirementsMet = pair.Value;

            if (!requirementsMet)
                continue;

            var anyRequirement = landing.Requirements.FirstOrDefault(req => req.Type == "Any");
            if (anyRequirement != null && !_completedAnyLandings.Contains(anyRequirement.Id))
            {
                while (landing.RequirementsMet)
                {
                    _completedLandings.Add($"{_nearestLocation.Name}/{landing.Name}/{anyRequirement.Id}");
                    _completedAnyLandings.Add(anyRequirement.Id);
                    _modHelper.Console.WriteLine($"Completed landing: {_completedLandings[_completedLandings.Count - 1]}", MessageType.Info);
                    LocationManager.RemoveAnyLanding(anyRequirement.Id);
                    LocationManager.GatherDistances(_nearestLocation);
                }
            }
            else if (anyRequirement == null && !_completedLandings.Contains($"{_nearestLocation.Name}/{landing.Name}"))
            {
                _completedLandings.Add($"{_nearestLocation.Name}/{landing.Name}");
                _modHelper.Console.WriteLine($"Completed landing: {_completedLandings[_completedLandings.Count - 1]}", MessageType.Info);
                landing.IsLanded = true;
            }
        }
    }

    public void OnPodracingStarted()
    {
        _modHelper.Console.WriteLine("Podracing Started", MessageType.Info);
        GUILineManager.ClearLines();
        TrainingSphereOverlay.Clear();
        LocationManager.ClearLandingState();
        _completedLandings.Clear();
        _completedAnyLandings.Clear();
    }

    public void OnPodracingCompleted()
    {
        _modHelper.Console.WriteLine("Podracing Completed", MessageType.Info);
        GUILineManager.ClearLines();
        TrainingSphereOverlay.Clear();
        GUILineManager.SetLine("score", $"Final score: {RuleManager.IsPodracing.score}", true, Corner.CenterRight);
        foreach (string landing in _completedLandings)
        {
            GUILineManager.SetLine(landing, $"<color=green>{landing}</color>", true, Corner.CenterRight);
        }

        string path = _modHelper.Config.GetSettingsValue<string>("Score Output Directory");
        path = Environment.ExpandEnvironmentVariables(path);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        path = Path.Combine(path, $"PTScore_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
        using StreamWriter sw = new(path);
        sw.WriteLine($"Final score: {RuleManager.IsPodracing.score}");
        foreach (string landing in _completedLandings)
        {
            sw.WriteLine(landing);
        }
        _modHelper.Console.WriteLine($"Score saved to {path}", MessageType.Info);
    }

    public void OnPodracingFailed()
    {
        _modHelper.Console.WriteLine("Podracing Failed", MessageType.Info);
        GUILineManager.ClearLines();
        TrainingSphereOverlay.Clear();
        LocationManager.ClearLandingState();
        _completedLandings.Clear();
        _completedAnyLandings.Clear();
    }
}
