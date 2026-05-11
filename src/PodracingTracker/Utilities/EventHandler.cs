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

[HarmonyPatch]
public class OnStartShipIgnition {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipThrusterAudio), nameof(ShipThrusterAudio.OnStartShipIgnition))]
    public static void OnStartShipIgnition_Prefix() {
        RuleManager.IsTakeoff.ignitionStart = true;
        RuleManager.IsTakeoff.ignitionCancel = false;
        RuleManager.IsTakeoff.ignitionComplete = false;
    }
}

[HarmonyPatch]
public class OnCancelShipIgnition {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipThrusterAudio), nameof(ShipThrusterAudio.OnCancelShipIgnition))]
    public static void OnCancelShipIgnition_Prefix() {
        RuleManager.IsTakeoff.ignitionCancel = true;
        RuleManager.IsTakeoff.ignitionStart = false;
        RuleManager.IsTakeoff.ignitionComplete = false;
    }
}

[HarmonyPatch]
public class OnCompleteShipIgnition {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipThrusterAudio), nameof(ShipThrusterAudio.OnCompleteShipIgnition))]
    public static void OnCompleteShipIgnition_Prefix() {
        RuleManager.IsTakeoff.ignitionComplete = true;
    }
}


[HarmonyPatch]
public class OnLockOnChanged {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerLockOnTargeting), nameof(PlayerLockOnTargeting.LockOn), new Type[] { typeof(Transform), typeof(float), typeof(bool), typeof(float) })]
    public static void OnStartShipIgnition_Prefix1() {
        PodracingTracker.isLockedOn = true;
        PodracingTracker.modHelper.Console.WriteLine("Locked On (Overload 1)");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerLockOnTargeting), nameof(PlayerLockOnTargeting.LockOn), new Type[] { typeof(Transform), typeof(Vector3), typeof(float), typeof(bool), typeof(float) })]
    public static void OnStartShipIgnition_Prefix2() {
        PodracingTracker.isLockedOn = true;
        PodracingTracker.modHelper.Console.WriteLine("Locked On (Overload 2)");
    }
}