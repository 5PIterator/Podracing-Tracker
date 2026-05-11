using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.InputSystem;
using OWML.ModHelper.Menus;
using OWML.ModHelper.Menus.NewMenuSystem;
using System.Runtime.InteropServices;
using System.Collections;
using ShapeCollision;
using UnityEngine.InputSystem.EnhancedTouch;
using System.IO;

namespace PodracingTracker;

public class UtilityTools : ModBehaviour
{
    private static PlayerBody playerBody;
    private static PlayerFogWarpDetector playerFogWarpDetector;
    public static void Initialize(IModHelper ModHelper)
    {
        playerBody = FindObjectOfType<PlayerBody>();
        playerFogWarpDetector = FindObjectOfType<PlayerFogWarpDetector>();
        ModHelper.Console.WriteLine("Initializing UtilityTools.", MessageType.Info);
    }

    public static AstroObject GetClosestAstroObject(Transform targetTransform, List<Transform> transforms)
    {
        //ModHelper.Console.WriteLine("Getting closest AstroObject.", MessageType.Info);
        float minDistance = float.MaxValue;
        AstroObject closestBody = null;

        foreach (Transform astroObject in transforms)
        {
            float distance = Vector3.Distance(targetTransform.position, astroObject.position);
            //ModHelper.Console.WriteLine($"Distance to {astroObject.name}: {distance}", MessageType.Info);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestBody = astroObject.GetComponentInParent<AstroObject>();
                //ModHelper.Console.WriteLine($"New closest AstroObject: {closestBody?.name} at {minDistance} units.", MessageType.Info);
            }
        }
        //ModHelper.Console.WriteLine($"Closest AstroObject: {closestBody?.name} at {minDistance} units.", MessageType.Info);
        return closestBody;
    }



    public static string NameFromAstro(AstroObject astroObject)
    {
        return AstroObject.AstroObjectNameToString(astroObject.GetAstroObjectName());
    }

    public static string IdFromAstro(AstroObject astroObject)
    {
        return astroObject.GetAstroObjectName().ToString();
    }

    public static bool IsPlayerMoving()
    {
        // Check if the velocity magnitude is above a small threshold to account for floating-point imprecision
        return playerBody.GetVelocity().sqrMagnitude > 0.01f;
    }



    public static Dictionary<Transform, float> distanceCache = [];
    public static Vector3 lastPlayerPosition;
    private static bool hasPlayerMoved;
    public static string playerInMaze;

    public static void UpdatePlayerPosition()
    {
        // Check if the player has moved
        Vector3 currentPosition = playerBody.transform.position;
        hasPlayerMoved = currentPosition != lastPlayerPosition;

        if (hasPlayerMoved)
        {
            lastPlayerPosition = currentPosition;
            playerInMaze = playerFogWarpDetector._outerWarpVolume?.GetName().ToString();
            distanceCache.Clear(); // Clear the cache only if the player has moved
        }
    }
}