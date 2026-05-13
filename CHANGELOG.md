- 0.1.0
    - Initial release.

- 0.2.0
    - Added support for LiveSplit.
    - Added Overlay destination selector. Main HUD, Landing monitor, Space suit HUD (WIP not working yet).
    - Exposed Content folder within the mod files so users can modify some aspects of the mod.
        - PodracingAnyLandings.json: List of landing wildcards that can be interpreted as an Any Landing.
        - PodracingLandings.json: List of landing locations with their requirements.
        - RuleGuiCorners.json: Mapping of rule display layout.
        - Outer Wilds - Podracing.lss: LiveSplit preset for Podracing.
    - Removed Settings options for changing the layout of the overlay to make it more readable. Moved to RuleGuiCorners.json instead. (Might add it back later if requested.)
    - Refactored code to be more modular and easier to maintain.


- 0.3.0 (Planned Features)
    - Add more settings to the mod.
    - Use native game UI for displaying the score.
    - Use native game UI to track distances.
    - Anti-cheat measures.
    - In-game dialogs with npcs for immersion.