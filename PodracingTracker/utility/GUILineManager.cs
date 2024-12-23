using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using OWML.ModHelper;
using OWML.Common;


namespace PodracingTracker
{
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

    public static class GUILineManager
    {
        public static IModHelper ModHelper;
        public static void Initialize(IModHelper ModHelper)
        {
            GUILineManager.ModHelper = ModHelper;
        }

        public class GUILine
        {
            public string Text { get; set; }
            public bool IsRichText { get; set; }
            public Corner TextCorner { get; set; } // Add this property

            public GUILine(string text, bool isRichText, Corner corner = Corner.TopLeft)
            {
                Text = text;
                IsRichText = isRichText;
                TextCorner = corner;
            }
        }

        private static readonly Dictionary<string, GUILine> GUILines = [];
        private static readonly List<string> GUILineOrder = [];
        private static readonly System.Random random = new();

        public static string GenerateId() => new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 10).Select(s => s[random.Next(s.Length)]).ToArray());

        public static string NewLine(string id, string text, bool isRichText = false, Corner corner = Corner.TopLeft, int index = -1)
        {
            while (guiBusy) {}
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
            {
                MoveLine(id, index);
            }

            return id;
        }

        public static void RemoveLine(string id)
        {
            while (guiBusy) {}
            if (GUILines.ContainsKey(id))
            {
                GUILines.Remove(id);
                GUILineOrder.Remove(id);
            }
        }

        public static void ClearLines()
        {
            while (guiBusy) {}
            GUILines.Clear();
            GUILineOrder.Clear();
        }

        public static void ClearCorner(Corner corner)
        {
            var idsToRemove = new List<string>();

            foreach (var id in GUILineOrder)
            {
                if (GUILines[id].TextCorner == corner)
                {
                    idsToRemove.Add(id);
                }
            }

            foreach (var id in idsToRemove)
            {
                RemoveLine(id);
            }
        }

        public static void SetLine(string id, string newLine, bool? isRichText = null, Corner? corner = null, int index = -1)
        {
            while (guiBusy) {}
            if (GUILines.ContainsKey(id))
            {
                GUILines[id].Text = newLine;
                if (isRichText.HasValue)
                {
                    GUILines[id].IsRichText = isRichText.Value;
                }
                if (corner.HasValue)
                {
                    GUILines[id].TextCorner = corner.Value;
                }
            }
            else
            {
                NewLine(id, newLine, isRichText ?? false, corner ?? Corner.TopLeft, index);
            }
        }
        public static void MoveLine(string id, int newIndex)
        {
            while (guiBusy) {} // Wait for GUI to finish rendering
            if (GUILines.ContainsKey(id) && newIndex >= 0 && newIndex < GUILineOrder.Count)
            {
                GUILineOrder.Remove(id);
                GUILineOrder.Insert(newIndex, id);
            }
        }

        private static bool guiBusy = false;

        public static void OnGUI()
        {
            guiBusy = true;
            var style = new GUIStyle
            {
                fontSize = 25,
                normal = { textColor = Color.white },
                richText = true // Enable rich text
            };

            // Dictionary to store combined text for each corner
            var cornerTextBlocks = new Dictionary<Corner, string>
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

            // Iterate over GUILines dictionary
            foreach (var id in GUILineOrder)
            {
                var line = GUILines[id];
                cornerTextBlocks[line.TextCorner] += line.Text + "\n";
            }
            // Create a label for each corner with the combined text
            foreach (var corner in cornerTextBlocks.Keys)
            {
                if (!string.IsNullOrEmpty(cornerTextBlocks[corner]))
                {
                    Vector2 position = GetPosition(corner);
                    float width = style.CalcSize(new GUIContent(cornerTextBlocks[corner])).x;
                    float height = style.CalcHeight(new GUIContent(cornerTextBlocks[corner]), width);

                    switch (corner)
                    {
                        case Corner.TopRight:
                            style.alignment = TextAnchor.UpperLeft;
                            position.x -= width;
                            break;
                        case Corner.CenterRight:
                            style.alignment = TextAnchor.UpperLeft;
                            position.x -= width;
                            position.y -= height / 2;
                            break;
                        case Corner.BottomRight:
                            style.alignment = TextAnchor.UpperLeft;
                            position.x -= width;
                            break;
                        case Corner.CenterTop:
                            style.alignment = TextAnchor.UpperLeft;
                            position.x -= width / 2;
                            break;
                        case Corner.Center:
                            style.alignment = TextAnchor.UpperLeft;
                            position.x -= width / 2;
                            position.y -= height / 2;
                            break;
                        case Corner.CenterBottom:
                            style.alignment = TextAnchor.UpperLeft;
                            position.x -= width / 2;
                            position.y -= height;
                            break;
                        case Corner.TopLeft:
                            style.alignment = TextAnchor.UpperLeft;
                            break;
                        case Corner.CenterLeft:
                            style.alignment = TextAnchor.UpperLeft;
                            position.y -= height / 2;
                            break;
                        case Corner.BottomLeft:
                            style.alignment = TextAnchor.UpperLeft;
                            position.y -= height;
                            break;
                    }
                    Rect labelRect = new Rect(position.x, position.y, width, height);
                    //GUI.Box(labelRect, GUIContent.none); // Draw a box around the label for debugging
                    GUI.Label(labelRect, cornerTextBlocks[corner], style);
                }
            }
            guiBusy = false;
        }

        private static Vector2 GetPosition(Corner corner)
        {
            float x = 10;
            float y = 10;

            return corner switch
            {
                Corner.TopLeft => new Vector2(x, y),
                Corner.TopRight => new Vector2(Screen.width - x, y),
                Corner.BottomLeft => new Vector2(x, Screen.height - y),
                Corner.BottomRight => new Vector2(Screen.width - x, Screen.height - y),
                Corner.Center => new Vector2(Screen.width / 2, Screen.height / 2),
                Corner.CenterTop => new Vector2(Screen.width / 2, y),
                Corner.CenterBottom => new Vector2(Screen.width / 2, Screen.height - y),
                Corner.CenterLeft => new Vector2(x, Screen.height / 2),
                Corner.CenterRight => new Vector2(Screen.width - x, Screen.height / 2),
                _ => new Vector2(x, y),
            };
        }

    }
}
