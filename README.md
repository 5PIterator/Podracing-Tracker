
## Now This Is Podracing!
Podracing is a challenge in Outer Wilds revolving around flying around the Solar System and landing on specific locations. The more locations you land on, the higher your score.
The Podracing Tracker modification is then a mod specifically designed to track the score and rules of Podracing.

You can find the specific rules of the challenge here: [Now This Is Podracing!](https://docs.google.com/spreadsheets/d/1Bg4JSZbgrWFhUh_O9f2fur0on2GWKHVNhJipSkRYD-I/edit?gid=0#gid=0)

## Features
Currently, the mod is in a very early stage of development, issues and bugs are expected. If you find any, please report them in the issues tab of this repository.
You may toggle each some of the rules and where they are displayed on the screen, though the functionality hasn't been fully tested or implemented yet.
Final score is printed into the path: `Documents\PodracingTracker\PTScore_<date>.txt`. Which can be changed in the settings of the mod.

Code is a bit of a mess, and this version is not the most efficient, but it works for now. If enough people are interested in the mod, I will work on it more.

You can create your own Podracing Landing Locations by modifying the `PodracingTracker\rules\PodracingLandings.json` file. List of compatible entry ids is in the `PodracingTracker\misc\ship_log_entries.txt` file. It is technically possible to add modded locations if you know the id of the location, though hasn't been tested yet.
The format is as follows:
```json
{
    "id": "WhiteHole", // Unique identifier for the location
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
### 0.1.0
- Initial release.

## Credits
- Me, for making the mod. Money here:

## Installation
Use the [Outer Wilds Mod Manager](https://outerwildsmods.com/mod-manager) to install the mod.