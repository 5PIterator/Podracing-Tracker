![image](https://github.com/user-attachments/assets/f53e603d-9515-40d9-ad89-600bb25abca0)

## Now This Is Podracing!!

Podracing is a challenge in Outer Wilds revolving around flying around the Solar System and landing on specific locations. The more locations you land on, the higher your score.
The Podracing Tracker modification is then a mod specifically designed to track the score and rules of Podracing.

You can find the specific rules of the challenge here: [Now This Is Podracing!](https://docs.google.com/spreadsheets/d/1Bg4JSZbgrWFhUh_O9f2fur0on2GWKHVNhJipSkRYD-I/edit?gid=0#gid=0)
There are a lot of rules to keep track of, which is why there is a need for this modification to keep things manageable. Still, I recommend reading the rules so that you don't wonder why is your run disqualified. For first training runs, I recommend turning on all the overlays in the settings of the mod. This should help you understand how all of this works in-game.

Currently, the mod is in a very early stage of development, issues and bugs are expected. If you find any, please report them in the issues tab of GitHub repository.

## Features

You may toggle some of the rules and where they are displayed on the screen, though the functionality hasn't been fully tested or implemented yet.
Final score is printed into the path: `Documents\PodracingTracker\PTScore_<date>.txt`. Which can be changed in the settings of the mod.

Code is now much more cleaner and should be more stable, or at least something I am more comfortable with. If issues arise, please report them in the issues tab of GitHub repository.

### LiveSplit

The mod is fully compatible with [LiveSplit](https://livesplit.org/) and is capable of driving it over **LiveSplit Server** (TCP). New splits are created automatically when you complete a qualifying landing, including dynamically generated names. That said, I did not find a way to create the split slots themselves in LiveSplit, all this mod does is rename them when doing a split, meaning I recommend that you import the splits file into LiveSplit which is packaged with the mod. Although, realistically all you need is insert a bunch of lines in the Split Editor. Just make sure to have enough splits, otherwise your run will end prematurely (WIP).

- On start: Tracker resets the timer and wipes the split names.
- On qualifying landing: Tracker creates a new split and names it after the landing.
- On completion: Tracker creates a new split and names it after the final score.
- On disqualification: Tracker resets the timer, leaving only the split names.

**Setup:**

1. Install [LiveSplit](https://livesplit.org/downloads/), and open it.
2. Import my splits preset: **right‑click → Open splits → From File...**: Find the file **`Content/Outer Wilds - Podracing.lss`** from the installed mod folder (`Outer Wilds Mods\<this mod>\`). In the repo, it lives at `src/PodracingTracker/Content/Outer Wilds - Podracing.lss`. The bundled file has **25 segments**.
3. Now start the server: **right‑click → Control → Start Server**. The default TCP port is **16834** (change only if you change it in LiveSplit).
4. Enable the LiveSplit integration in **Outer Wilds**: **OPTIONS → MODS → PodracingTracker → LiveSplit** and turn **LiveSplit integration** on. Set **LiveSplit host** to `127.0.0.1` if LiveSplit runs on the same PC, and **LiveSplit port** to match LiveSplit (usually `16834`). If LiveSplit runs on another machine, use that computer’s address and ensure firewalls allow the port.
5. Optional: **LiveSplit verbose logs** logs each TCP command to the OWML console (useful for troubleshooting).
6. Optional: **Wipe splits at start** — after each reset, the mod renames the split names to blank. Leave this **empty** to clear every segment name to blank; set a non‑empty string to use one placeholder name for all segments.

### Custom landing data

You can create your own Podracing landing definitions by editing **`Content/PodracingLandings.json`** in the installed mod directory (or **`src/PodracingTracker/Content/PodracingLandings.json`** in this repository). A list of compatible ship log entry ids is in **`src/PodracingTracker/misc/ship_log_entries.txt`**. It is technically possible to add modded locations if you know the id of the location, though note that this has not been tested yet.

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

- [#] Add more settings to the mod.
- [#] Use native game UI for displaying the score. (WIP)
- [ ] Use native game UI to track distances.
- [ ] Anti-cheat measures.
- [ ] Immersion features: In-game dialogs with NPCs for the story of the challenge.

## Known Issues

- No mod compatibility has been tested yet.
- The mod is not very efficient, and may cause performance issues.
- If you are on unstable ground, the mod may not be able to detect a proper takeoff even if the sound is played. (This is a game issue, and for the sake of simplicity I consider it a feature. Just try to take off again.)
- Possible conflicts between the Tracker and the challenge rules. As a rule, this mod has precedence over the rules of the challenge. For example, if the mod says you have completed a qualifying landing, though it is questionable in the rules, be free to trust the Tracker. Please do still put these edge cases into issue tracker because as said this mod is early in development and so are the rules, and I don't want to leave in anything too outrageous.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Credits

- I, for coming up with the challenge. Money here: [$$$Ko-fi$$$](https://ko-fi.com/theiterator)
- Me, for coding the mod. Money here: [$$$Ko-fi$$$](https://ko-fi.com/theiterator)
- The OW Modding Discord for helping out. Come visit here: [The Outer Wilds Modding](https://discord.gg/9vE5aHxcF9)

## Installation

Use the [Outer Wilds Mod Manager](https://outerwildsmods.com/mods/podracingtracker) to install the mod. If you install manually, copy the **`PodracingTracker.dll`** plus the entire **`Content`** folder and **`default-config.json`** / **`manifest.json`** as produced by the build output.
