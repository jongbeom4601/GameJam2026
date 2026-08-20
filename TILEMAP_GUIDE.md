# Tilemap Example Guide

## Included example

- Tile atlas: `Assets/Images/Sprites/Tiles/ExampleTileAtlas.png`
- Generated Tile assets and palette: `Assets/Tilemaps/Example`
- Example scene: `Assets/Scenes/Examples/TilemapExample.unity`
- Generator menu: `Tools > Game Jam > Generate Tilemap Example`

The example uses separate `Ground` and `Obstacles` Tilemaps. Only the obstacle tiles have colliders, so gameplay code can distinguish walkable floor from blocked cells without per-object setup.

## Painting a map

1. Open `Window > 2D > Tile Palette`.
2. Select `ExampleTilePalette`.
3. Open `TilemapExample.unity` or add a `Grid` with child Tilemaps to another scene.
4. Select the target Tilemap in the Tile Palette window.
5. Choose a tile and paint with the brush tool.

Use `Ground` for grass, dirt, stone, water, and floors. Use `Obstacles` for walls, bushes, and rocks that should block the player.

## Replacing the temporary art

Keep the same 4-by-4 cell order when replacing `ExampleTileAtlas.png`, then run `Tools > Game Jam > Generate Tilemap Example` again. The generator updates the existing Tile assets, palette, and example scene while preserving their asset paths.
