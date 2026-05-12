using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using OWML.Common;
using OWML.ModHelper;

namespace PodracingTracker;

public enum Corner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center,
    CenterTop,
    CenterBottom,
    CenterLeft,
    CenterRight
}

/// <summary>
/// Run overlay lines keyed by id, rendered as uGUI on one or more canvases (see mod toggles).
/// Each active host is just a <see cref="Canvas"/> plus normalized anchor bounds and a corner layout profile.
/// </summary>
public static class GUILineManager
{
    public static IModHelper ModHelper { get; private set; }

    /// <summary>Stable id for <see cref="SetHostAnchorBounds"/> / <see cref="GetHostAnchorBounds"/> across sessions.</summary>
    public enum OverlayHostKind
    {
        Screen,
        Landing,
        SpaceSuit
    }

    public class GUILine
    {
        public string Text { get; set; }
        public bool IsRichText { get; set; }
        public Corner TextCorner { get; set; }

        public GUILine(string text, bool isRichText, Corner corner = Corner.TopLeft)
        {
            Text = text;
            IsRichText = isRichText;
            TextCorner = corner;
        }
    }

    private sealed class OverlayHostRuntime
    {
        public readonly OverlayHostKind Kind;
        public GameObject Root;
        public readonly Text[] CornerTexts;

        public OverlayHostRuntime(OverlayHostKind kind)
        {
            Kind = kind;
            CornerTexts = new Text[Enum.GetValues(typeof(Corner)).Length];
        }
    }

    private static readonly Dictionary<string, GUILine> GUILines = [];
    private static readonly List<string> GUILineOrder = [];
    private static readonly System.Random Random = new();

    private static readonly Dictionary<OverlayHostKind, Rect> HostAnchorBounds = new()
    {
        [OverlayHostKind.Screen] = new Rect(0f, 0f, 1f, 1f),
        [OverlayHostKind.Landing] = new Rect(0f, 0f, 0.8f, 0.8f),
        [OverlayHostKind.SpaceSuit] = new Rect(0f, 0f, 0.8f, 0.8f)
    };

    private static readonly List<OverlayHostRuntime> Hosts = [];

    /// <summary>
    /// Normalized rectangle (0–1 along each parent canvas axis) that limits where corner anchors are placed for this host kind.
    /// x,y is lower-left; width/height span toward upper-right in uGUI space.
    /// </summary>
    public static void SetHostAnchorBounds(OverlayHostKind host, Rect normalizedBounds)
    {
        normalizedBounds = Clamp01Rect(normalizedBounds);
        HostAnchorBounds[host] = normalizedBounds;

        foreach (OverlayHostRuntime h in Hosts)
        {
            if (h.Kind == host)
                ApplyAnchorBounds(h.Root, normalizedBounds);
        }
    }

    public static Rect GetHostAnchorBounds(OverlayHostKind host) => HostAnchorBounds[host];

    public static void Initialize(IModHelper modHelper, CanvasMarkerManager canvasMarkerManager, ShipCockpitUI shipCockpitUi = null, HUDCanvas hudCanvas = null)
    {
        ModHelper = modHelper;
        TeardownAllHosts();

        if (IsScreenOverlayEnabled())
        {
            Canvas hud = canvasMarkerManager?._canvas;
            if (hud != null)
                TryAddCanvasHost(OverlayHostKind.Screen, hud, "PodracingTracker_OverlayScreen", fontSize: 24, largeCornerPanels: true);
        }

        if (IsLandingOverlayEnabled() && shipCockpitUi != null)
        {
            Canvas landing = shipCockpitUi._landingCamOffScreenIndicatorGui;
            if (landing != null)
                TryAddCanvasHost(OverlayHostKind.Landing, landing, "PodracingTracker_OverlayLanding", fontSize: 16, largeCornerPanels: false);
        }

        if (IsSpaceSuitOverlayEnabled())
        {
            Canvas suit = ResolveSpaceSuitHostCanvas(hudCanvas);
            if (suit != null)
                TryAddCanvasHost(OverlayHostKind.SpaceSuit, suit, "PodracingTracker_OverlaySpaceSuit", fontSize: 24, largeCornerPanels: true);
        }

        SyncOverlayFromLines();
    }

    public static bool IsScreenOverlayEnabled() =>
        ModHelper == null || ModHelper.Config.GetSettingsValue<bool>("Overlay: Screen HUD");

    public static bool IsLandingOverlayEnabled() =>
        ModHelper != null && ModHelper.Config.GetSettingsValue<bool>("Overlay: Landing monitor");

    public static bool IsSpaceSuitOverlayEnabled() =>
        ModHelper != null && ModHelper.Config.GetSettingsValue<bool>("Overlay: Space suit HUD");

    /// <summary>Hides or shows every overlay root (e.g. when the pause menu is open).</summary>
    public static void SetOverlaysVisible(bool visible)
    {
        foreach (OverlayHostRuntime h in Hosts)
        {
            if (h.Root != null)
                h.Root.SetActive(visible);
        }
    }

    public static string GenerateId() =>
        new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 10).Select(s => s[Random.Next(s.Length)]).ToArray());

    public static string NewLine(string id, string text, bool isRichText = false, Corner corner = Corner.TopLeft, int index = -1)
    {
        if (!GUILines.ContainsKey(id))
        {
            GUILines[id] = new GUILine(text, isRichText, corner);
            GUILineOrder.Add(id);
        }
        else
        {
            SetLine(id, text, isRichText, corner);
        }

        if (index >= 0 && index < GUILineOrder.Count)
            MoveLine(id, index);
        else
            SyncOverlayFromLines();

        return id;
    }

    public static void RemoveLine(string id)
    {
        if (!GUILines.ContainsKey(id))
            return;

        GUILines.Remove(id);
        GUILineOrder.Remove(id);
        SyncOverlayFromLines();
    }

    public static void ClearLines()
    {
        GUILines.Clear();
        GUILineOrder.Clear();
        SyncOverlayFromLines();
    }

    public static void ClearCorner(Corner corner)
    {
        var idsToRemove = new List<string>();
        foreach (var id in GUILineOrder)
        {
            if (GUILines[id].TextCorner == corner)
                idsToRemove.Add(id);
        }

        foreach (var id in idsToRemove)
            RemoveLine(id);
    }

    public static void SetLine(string id, string newLine, bool? isRichText = null, Corner? corner = null, int index = -1)
    {
        if (GUILines.ContainsKey(id))
        {
            GUILines[id].Text = newLine;
            if (isRichText.HasValue)
                GUILines[id].IsRichText = isRichText.Value;
            if (corner.HasValue)
                GUILines[id].TextCorner = corner.Value;

            if (index >= 0 && index < GUILineOrder.Count)
                MoveLine(id, index);
            else
                SyncOverlayFromLines();
        }
        else
        {
            NewLine(id, newLine, isRichText ?? false, corner ?? Corner.TopLeft, index);
        }
    }

    public static void MoveLine(string id, int newIndex)
    {
        if (GUILines.ContainsKey(id) && newIndex >= 0 && newIndex < GUILineOrder.Count)
        {
            GUILineOrder.Remove(id);
            GUILineOrder.Insert(newIndex, id);
        }

        SyncOverlayFromLines();
    }

    private static Dictionary<Corner, string> AggregateCornerBlocks()
    {
        var blocks = new Dictionary<Corner, string>
        {
            { Corner.TopLeft, "" },
            { Corner.TopRight, "" },
            { Corner.BottomLeft, "" },
            { Corner.BottomRight, "" },
            { Corner.Center, "" },
            { Corner.CenterTop, "" },
            { Corner.CenterBottom, "" },
            { Corner.CenterLeft, "" },
            { Corner.CenterRight, "" }
        };

        foreach (var id in GUILineOrder)
        {
            GUILine line = GUILines[id];
            blocks[line.TextCorner] += line.Text + "\n";
        }

        return blocks;
    }

    private static void SyncOverlayFromLines()
    {
        Dictionary<Corner, string> blocks = AggregateCornerBlocks();

        foreach (OverlayHostRuntime h in Hosts)
            PushBlocksToCornerTexts(blocks, h.CornerTexts);
    }

    private static void PushBlocksToCornerTexts(Dictionary<Corner, string> blocks, Text[] cornerTexts)
    {
        foreach (Corner corner in Enum.GetValues(typeof(Corner)))
        {
            Text t = cornerTexts[(int)corner];
            if (t == null)
                continue;

            string block = blocks[corner].TrimEnd('\n');
            t.gameObject.SetActive(!string.IsNullOrEmpty(block));
            t.text = block;
        }
    }

    private static Font ResolveFont() =>
        Resources.GetBuiltinResource<Font>("Arial.ttf")
        ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    private static Canvas ResolveSpaceSuitHostCanvas(HUDCanvas hudCanvas)
    {
        if (hudCanvas == null)
            return null;

        Canvas onSame = hudCanvas.GetComponent<Canvas>();
        if (onSame != null)
            return onSame;

        return hudCanvas.GetComponentInParent<Canvas>();
    }

    private static void TeardownAllHosts()
    {
        foreach (OverlayHostRuntime h in Hosts)
        {
            if (h.Root != null)
            {
                UnityEngine.Object.Destroy(h.Root);
                h.Root = null;
            }

            for (int i = 0; i < h.CornerTexts.Length; i++)
                h.CornerTexts[i] = null;
        }

        Hosts.Clear();
    }

    private static void TryAddCanvasHost(
        OverlayHostKind kind,
        Canvas host,
        string rootObjectName,
        int fontSize,
        bool largeCornerPanels)
    {
        Font font = ResolveFont();
        if (font == null)
            return;

        Rect anchorBounds = HostAnchorBounds[kind];

        var runtime = new OverlayHostRuntime(kind);
        GameObject root = new(rootObjectName);
        root.transform.SetParent(host.transform, false);
        root.transform.SetAsLastSibling();

        RectTransform rootRect = root.AddComponent<RectTransform>();
        ApplyAnchorBoundsToRect(rootRect, anchorBounds);

        foreach (Corner corner in Enum.GetValues(typeof(Corner)))
        {
            var go = new GameObject($"Corner_{corner}");
            go.transform.SetParent(root.transform, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;

            RectTransform rt = text.rectTransform;
            ApplyHudCornerLayout(corner, rt, text, largeCornerPanels);
            runtime.CornerTexts[(int)corner] = text;
        }

        runtime.Root = root;
        Hosts.Add(runtime);
    }

    private static void ApplyAnchorBounds(GameObject root, Rect n)
    {
        if (root == null)
            return;

        RectTransform rt = root.GetComponent<RectTransform>();
        if (rt == null)
            return;

        ApplyAnchorBoundsToRect(rt, n);
    }

    private static void ApplyAnchorBoundsToRect(RectTransform rt, Rect n)
    {
        n = Clamp01Rect(n);
        rt.anchorMin = new Vector2(n.xMin, n.yMin);
        rt.anchorMax = new Vector2(n.xMax, n.yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Rect Clamp01Rect(Rect r)
    {
        float xMin = Mathf.Clamp01(Mathf.Min(r.xMin, r.xMax));
        float xMax = Mathf.Clamp01(Mathf.Max(r.xMin, r.xMax));
        float yMin = Mathf.Clamp01(Mathf.Min(r.yMin, r.yMax));
        float yMax = Mathf.Clamp01(Mathf.Max(r.yMin, r.yMax));
        if (xMax - xMin < 0.01f)
            xMax = Mathf.Clamp01(xMin + 0.01f);
        if (yMax - yMin < 0.01f)
            yMax = Mathf.Clamp01(yMin + 0.01f);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    /// <summary>Same corner semantics as the old IMGUI overlay; <paramref name="largeCornerPanels"/> picks panel sizes for full HUD vs landing monitor.</summary>
    private static void ApplyHudCornerLayout(Corner corner, RectTransform rt, Text text, bool largeCornerPanels)
    {
        float inset = largeCornerPanels ? 14f : 10f;
        Vector2 box = largeCornerPanels ? new Vector2(560f, 400f) : new Vector2(360f, 260f);
        Vector2 centerBox = largeCornerPanels ? new Vector2(720f, 480f) : new Vector2(520f, 360f);
        Vector2 band = largeCornerPanels ? new Vector2(720f, 240f) : new Vector2(520f, 200f);
        Vector2 sideVertical = largeCornerPanels ? new Vector2(360f, 480f) : new Vector2(280f, 360f);

        switch (corner)
        {
            case Corner.TopLeft:
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(inset, -inset);
                rt.sizeDelta = box;
                text.alignment = TextAnchor.UpperLeft;
                break;
            case Corner.TopRight:
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-inset, -inset);
                rt.sizeDelta = box;
                text.alignment = TextAnchor.UpperRight;
                break;
            case Corner.BottomLeft:
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(inset, inset);
                rt.sizeDelta = box;
                text.alignment = TextAnchor.LowerLeft;
                break;
            case Corner.BottomRight:
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-inset, inset);
                rt.sizeDelta = box;
                text.alignment = TextAnchor.LowerRight;
                break;
            case Corner.Center:
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = centerBox;
                text.alignment = TextAnchor.MiddleCenter;
                break;
            case Corner.CenterTop:
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -inset);
                rt.sizeDelta = band;
                text.alignment = TextAnchor.UpperCenter;
                break;
            case Corner.CenterBottom:
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, inset);
                rt.sizeDelta = band;
                text.alignment = TextAnchor.LowerCenter;
                break;
            case Corner.CenterLeft:
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(inset, 0f);
                rt.sizeDelta = sideVertical;
                text.alignment = TextAnchor.MiddleLeft;
                break;
            case Corner.CenterRight:
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-inset, 0f);
                rt.sizeDelta = sideVertical;
                text.alignment = TextAnchor.MiddleRight;
                break;
        }
    }
}
