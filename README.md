![image](https://github.com/user-attachments/assets/f53e603d-9515-40d9-ad89-600bb25abca0)

## Now This Is Podracing!!

Podracing is a challenge in Outer Wilds revolving around flying around the Solar System and landing on specific locations. The more locations you land on, the higher your score.
The Podracing Tracker modification is then a mod specifically designed to track the score and rules of Podracing.

You can find the specific rules of the challenge here: [Now This Is Podracing!](https://docs.google.com/spreadsheets/d/1Bg4JSZbgrWFhUh_O9f2fur0on2GWKHVNhJipSkRYD-I/edit?gid=0#gid=0)

## Features

Currently, the mod is in a very early stage of development, issues and bugs are expected. If you find any, please report them in the issues tab of GitHub repository.
You may toggle some of the rules and where they are displayed on the screen, though the functionality hasn't been fully tested or implemented yet.
Final score is printed into the path: `Documents\PodracingTracker\PTScore_<date>.txt`. Which can be changed in the settings of the mod.

Code is now much more cleaner and should be more stable. If issues arise, please report them in the issues tab of GitHub repository.

### LiveSplit

The mod can drive [LiveSplit](https://livesplit.org/) over **LiveSplit Server** (TCP): timer **reset** and **start** when a podracing run begins, **split** (with segment names from the tracker) when you complete a qualifying landing, **split** and **pause** when the run ends cleanly, and **reset** on disqualification. See [CHANGELOG.md](CHANGELOG.md) for a shorter checklist.

**Setup:**

1. Install LiveSplit and add the **Server** component if needed: **Edit Layout → add Other → LiveSplit Server** (or install it via **right‑click → Control → Layout Settings** depending on your version).
2. **Open splits** in LiveSplit: use **`Content/Outer Wilds - Podracing.lss`** from the installed mod folder (`Outer Wilds Mods\<this mod>\` next to `PodracingTracker.dll`, same place as the **`Content`** folder). In the repo, it lives at `src/PodracingTracker/Content/Outer Wilds - Podracing.lss`. The bundled file has **25 segments**; the mod assumes that count when renaming segments at run start.
3. In LiveSplit, **start the server**: **right‑click → Control → Start Server**. The default TCP port is **16834** (change only if you change it in LiveSplit).
4. In **Outer Wilds** / OWML mod settings for Podracing Tracker, turn **LiveSplit integration** on. Set **LiveSplit host** to `127.0.0.1` if LiveSplit runs on the same PC, and **LiveSplit port** to match LiveSplit (usually `16834`). If LiveSplit runs on another machine, use that computer’s address and ensure firewalls allow the port.
5. Optional: **LiveSplit verbose logs** logs each TCP command to the OWML console (useful for troubleshooting).
6. Optional: **Wipe splits at start** — after each reset, the mod renames segments **0–24** before starting the timer. Leave this **empty** to clear every segment name to blank; set a non‑empty string to use one placeholder name for all segments. This only matches runs with **25 splits** like the bundled `.lss`.

### Custom landing data

You can create your own Podracing landing definitions by editing **`Content/PodracingLandings.json`** in the installed mod directory (or **`src/PodracingTracker/Content/PodracingLandings.json`** in this repository). A list of compatible ship log entry ids is in **`src/PodracingTracker/misc/ship_log_entries.txt`**. It is technically possible to add modded locations if you know the id of the location, though that has not been tested yet.

The format is as follows:

```json
{
    "id": "WhiteHole", // In-Game identifier for the location
    "name": "White Hole", // Name of the location to be displayed
    "landings": [
    {
        "name": "White Hole", // Name of the landing to be displayed
        "description": "Anywhere around the White Hole.", // Description of the landing to be displayed
        "requirements": [
        {
            "id": "WHITE_HOLE_STATION", // id of the transform to be checked
            "type": "Entry", // type of the transform (Entry-Ship Log Entry, Body-Planet/Moon, Any-All entries will be checked)
            "min": 0, // Minimum distance from the transform
            "max": 1000 // Maximum distance from the transform
        },
        {
            "id": "Any", // Will change dynamically to the id of the transform that the player is closest to
            "type": "Any",
            "min": 0,
            "max": 50
        }
        ]
    }
    ]
},
```

Other files shipped under **`Content/`** (copied next to the DLL on build) include **`PodracingAnyLandings.json`**, **`RuleGuiCorners.json`**, and the LiveSplit preset above. Keep the whole **`Content`** folder when installing or zipping the mod.

## Planned Features

- [ ] Add more settings to the mod.
- [ ] Use native game UI for displaying the score.
- [ ] Use native game UI to track distances.
- [ ] Anti-cheat measures.

## Known Issues

- No mod compatibility has been tested yet.
- The mod is not very efficient, and may cause performance issues.
- If you are on unstable ground, the mod may not be able to detect a proper takeoff even if the sound is played. (This is a game issue, and for the sake of simplicity I consider it a feature. Just try to take off again.)

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Credits

- I, for coming up with the challenge. Money here: [$$$Ko-fi$$$](https://ko-fi.com/theiterator)
- Me, for coding the mod. Money here: [$$$Ko-fi$$$](https://ko-fi.com/theiterator)
- The OW Modding Discord for helping out: [The Outer Wilds Modding](https://discord.gg/9vE5aHxcF9)

## Installation

Use the [Outer Wilds Mod Manager](https://outerwildsmods.com/mods/podracingtracker) to install the mod. If you install manually, copy the **`PodracingTracker.dll`** plus the entire **`Content`** folder and **`default-config.json`** / **`manifest.json`** as produced by the build output.
