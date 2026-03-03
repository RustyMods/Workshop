# Workshop

Dream, Plan, Build

--------------------

- Inspired by Plan Build: https://thunderstore.io/c/valheim/p/MathiasDecrock/PlanBuild/
- Inspired by Infinity Hammer: https://thunderstore.io/c/valheim/p/JereKuusela/Infinity_Hammer/

I've taken my old blueprint mods and repacked into a single entity with new features.

## Features

- Build ghost pieces
- Save blueprints
- Publish blueprints
- Edit terrain
- Move pieces

## Ghost Pieces

Use a new tool `Ghost Hammer` to plan, design and build ghost pieces. 

1. No placement cost
2. No collision

The hammer comes with a set of terrain tools

1. Edit biome terrain (i.e Meadows terrain --> Plains terrain) which also influences the grass
2. Paint lava 
3. Select objects within a radius
4. Select connected pieces
5. Remove objects within a radius
6. Remove connected pieces

Some of these tools are admin only

## Save Blueprints

1. Select objects using the ghost hammer
2. Use the `Save Key` (default: `F3`) to prompt save window
3. Set Name
4. Set Description
5. Blueprints will be saved on client config folder
6. Use the `Blueprint Hammer` to place blueprints

## Publish blueprints

Use a new crafting station `Blueprint Table` to view published blueprints to purchase and publish your local blueprints

1. Publishing blueprints will convert the local blueprint (which has no cost requirements) into a recipe and shared on the server
2. Recipes will allow users to craft blueprints to use to build blueprints
3. Check the `Revenue` tab to collect your fees
4. Use the `Publish` tab to view local blueprints
5. Set Price
6. Publish

## Build blueprints

Use a new ward `Construction Ward` to view connected ghost pieces and fill it with required resources to convert ghost pieces into reality

1. Search within radius for ghost pieces
2. Remove/Disable pieces from construction ward
3. Fill with required resources
4. Make sure required stations are connected to ward
5. Build ghosts pieces

Processing ghost pieces can be configured using `Build Interval`, this controls the interval between placement of each ghost pieces.
Pieces built are ordered by lowest to highest.

During construction, piece support is ignored, but once done, the pieces will begin they individual check for support. If a piece does not have support, they
will break, and convert back into a ghost piece.

## Edit Terrain

1. Paint biome color (i.e Ashlands Grass, Ashlands Lava)

## Notes

- When clients publish blueprints, they are sent to the server. The server manages all published blueprints and syncs it to all clients.
- Snapshots of blueprints will be generated if it does not exist. New icons will be saved in config folder to be used next time.
- Player built pieces, when destroyed, will become ghosts