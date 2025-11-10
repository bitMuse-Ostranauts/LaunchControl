LaunchControl
=============

LaunchControl is a shared toolkit for Ostranauts mods. It gives mod authors common building blocks so they do not have to rewrite the same support code over and over. If you install LaunchControl by itself, nothing in the game will change. You only notice it when a mod depends on it.

Why it exists
-------------
Modders often need the same helpers: commands for testing, UI tweaks, data storage, and more. LaunchControl bundles those helpers into one library so different mods can use the same reliable features and reduces conflicts by giving them one unified way to reach those systems. When you combine LaunchControl with a mod that was built for it, the mod can plug into the game with less hassle.

What it provides
----------------
These are the main systems that mods can tap into:

- Command System: add in-game console commands for debugging or mod features.
- MegaTooltip System: insert extra panels into the game tooltips for items or characters.
- Pledge System: define new AI jobs or behaviors without patching game code by hand.
- Task System: attach custom data to ongoing tasks and keep it safely through saves.
- PersistentData System: save and load mod data alongside the game world.
- Item Category System: group related items and match them by ID or rules.
- Sprite Utility: look up and reuse existing game sprites by keyword.
- DumpUI Command: click on interface elements and see how they are built.

Have an idea
------------
LaunchControl keeps expanding as modders find new gaps to fill. If you need another shared helper, reach out to bitMuse on Discord or open a pull request so we can grow the toolkit together.