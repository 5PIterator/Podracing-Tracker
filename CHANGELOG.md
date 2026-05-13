# Changelog

## 0.1.0

- Initial release.

## 0.2.0

- Added support for LiveSplit.
- Added overlay destination selector: Main HUD, Landing monitor, Space suit HUD (WIP, not working yet).
- Exposed **Content** folder next to the DLL so users can edit data files without rebuilding the mod:
  - **PodracingAnyLandings.json** — list of landing ids treated as “Any” landings.
  - **PodracingLandings.json** — landing locations and distance rules.
  - **RuleGuiCorners.json** — which corner each rule uses in the overlay.
  - **Outer Wilds - Podracing.lss** — suggested LiveSplit splits (25 segments).
- Removed in-menu options for per-rule overlay corners; corners are configured in **RuleGuiCorners.json** (may add UI again if requested).
- Refactored code to be more modular and easier to maintain.

### LiveSplit — how to configure

1. **Splits file:** In LiveSplit, open **`Outer Wilds - Podracing.lss`** from the mod’s **`Content`** folder (same folder as `PodracingTracker.dll` after install). It must stay a **25-segment** run for the mod’s start-of-run rename commands to match.
2. **Server:** In LiveSplit, start **LiveSplit TCP Server** (default port **16834**). Ensure the **Server** component is in your layout if LiveSplit asks for it.
3. **Mod settings (OWML):** Enable **LiveSplit integration**. Set **host** to `127.0.0.1` when LiveSplit is local; set **port** to the same value as in LiveSplit.
4. **Optional:** Turn on **LiveSplit verbose logs** to print every command to the OWML console. Use **Wipe splits at start** only if you understand it renames segments 0–24 after each reset (`""` = blank names; any other text = that name for all segments).
5. **Order of operations:** Podracing start → LiveSplit **reset** → wipe (if configured) → **start**; qualifying landing → **setcurrentsplitname** + **split**; run finished → final **split** + **pause**; failed run → **reset**.

## 0.3.0 (planned)

- Add more settings to the mod.
- Use native game UI for displaying the score.
- Use native game UI to track distances.
- Anti-cheat measures.
- In-game dialogs with NPCs for immersion.
