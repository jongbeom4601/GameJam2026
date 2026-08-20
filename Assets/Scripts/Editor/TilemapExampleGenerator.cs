using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace GameJam.Editor
{
    public static class TilemapExampleGenerator
    {
        private const string AtlasPath = "Assets/Images/Sprites/Tiles/ExampleTileAtlas.png";
        private const string GeneratedFolder = "Assets/Tilemaps/Example";
        private const string PalettePath = GeneratedFolder + "/ExampleTilePalette.prefab";
        private const string SceneFolder = "Assets/Scenes/Examples";
        private const string ExampleScenePath = SceneFolder + "/TilemapExample.unity";
        private const int AtlasColumns = 4;
        private const int AtlasRows = 4;

        private static readonly string[] TileNames =
        {
            "Grass",
            "FlowerGrass",
            "Dirt",
            "PebbleDirt",
            "Stone",
            "CrackedStone",
            "DeepWater",
            "ShallowWater",
            "GrassDirtEdge",
            "DirtStoneEdge",
            "StoneWallTop",
            "StoneWallFace",
            "Bush",
            "Rock",
            "WoodFloor",
            "Void"
        };

        private static readonly HashSet<string> CollidableTileNames = new HashSet<string>
        {
            "StoneWallTop",
            "StoneWallFace",
            "Bush",
            "Rock"
        };

        [MenuItem("Tools/Game Jam/Generate Tilemap Example")]
        public static void Generate()
        {
            GenerateInternal();
        }

        private static void GenerateInternal()
        {
            EnsureFolder(GeneratedFolder);
            EnsureFolder(SceneFolder);

            ConfigureAndSliceAtlas();
            Dictionary<string, Tile> tiles = CreateOrUpdateTiles();
            CreateOrUpdatePalette(tiles);
            CreateOrUpdateExampleScene(tiles);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TilemapExampleGenerator] Generated palette and example scene at {ExampleScenePath}");
        }

        private static void ConfigureAndSliceAtlas()
        {
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);

            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            TextureImporter importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (atlas == null || importer == null)
            {
                throw new InvalidOperationException($"Tile atlas was not found at {AtlasPath}.");
            }

            if (atlas.width != atlas.height)
            {
                throw new InvalidOperationException("The example tile atlas must be square.");
            }

            int cellSize = atlas.width / AtlasColumns;
            if (cellSize <= 0)
            {
                throw new InvalidOperationException("The example tile atlas is too small to slice.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = cellSize;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
            {
                throw new InvalidOperationException("Unity Sprite Editor data provider is unavailable.");
            }

            dataProvider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> existingIds = dataProvider.GetSpriteRects()
                .GroupBy(spriteRect => spriteRect.name)
                .ToDictionary(group => group.Key, group => group.First().spriteID);

            SpriteRect[] spriteRects = new SpriteRect[TileNames.Length];
            for (int row = 0; row < AtlasRows; row++)
            {
                for (int column = 0; column < AtlasColumns; column++)
                {
                    int index = row * AtlasColumns + column;
                    string tileName = TileNames[index];
                    int x = Mathf.CeilToInt(column * atlas.width / (float)AtlasColumns);
                    int y = Mathf.CeilToInt((AtlasRows - 1 - row) * atlas.height / (float)AtlasRows);

                    spriteRects[index] = new SpriteRect
                    {
                        name = tileName,
                        spriteID = existingIds.TryGetValue(tileName, out GUID spriteId)
                            ? spriteId
                            : GUID.Generate(),
                        rect = new Rect(x, y, cellSize, cellSize),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        border = Vector4.zero
                    };
                }
            }

            dataProvider.SetSpriteRects(spriteRects);
            ISpriteNameFileIdDataProvider nameProvider =
                dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameProvider?.SetNameFileIdPairs(
                spriteRects.Select(spriteRect =>
                    new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID)));
            dataProvider.Apply();

            if (dataProvider.targetObject is AssetImporter spriteImporter)
            {
                spriteImporter.SaveAndReimport();
            }
        }

        private static Dictionary<string, Tile> CreateOrUpdateTiles()
        {
            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name, sprite => sprite);

            Dictionary<string, Tile> tiles = new Dictionary<string, Tile>();
            foreach (string tileName in TileNames)
            {
                if (!sprites.TryGetValue(tileName, out Sprite sprite))
                {
                    throw new InvalidOperationException($"Sprite slice '{tileName}' was not imported.");
                }

                string tilePath = $"{GeneratedFolder}/{tileName}.asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }

                tile.name = tileName;
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.transform = Matrix4x4.identity;
                tile.flags = TileFlags.LockAll;
                tile.colliderType = CollidableTileNames.Contains(tileName)
                    ? Tile.ColliderType.Grid
                    : Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                tiles.Add(tileName, tile);
            }

            AssetDatabase.SaveAssets();
            return tiles;
        }

        private static void CreateOrUpdatePalette(IReadOnlyDictionary<string, Tile> tiles)
        {
            GameObject paletteAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
            if (paletteAsset == null)
            {
                paletteAsset = GridPaletteUtility.CreateNewPalette(
                    GeneratedFolder,
                    "ExampleTilePalette",
                    GridLayout.CellLayout.Rectangle,
                    GridPalette.CellSizing.Manual,
                    Vector3.one,
                    GridLayout.CellSwizzle.XYZ);
            }

            string paletteAssetPath = AssetDatabase.GetAssetPath(paletteAsset);
            GameObject paletteContents = PrefabUtility.LoadPrefabContents(paletteAssetPath);
            try
            {
                Tilemap paletteTilemap = paletteContents.GetComponentInChildren<Tilemap>();
                paletteTilemap.ClearAllTiles();

                for (int row = 0; row < AtlasRows; row++)
                {
                    for (int column = 0; column < AtlasColumns; column++)
                    {
                        string tileName = TileNames[row * AtlasColumns + column];
                        paletteTilemap.SetTile(new Vector3Int(column, -row, 0), tiles[tileName]);
                    }
                }

                paletteTilemap.CompressBounds();
                EditorUtility.SetDirty(paletteTilemap);
                PrefabUtility.SaveAsPrefabAsset(paletteContents, paletteAssetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(paletteContents);
            }
        }

        private static void CreateOrUpdateExampleScene(IReadOnlyDictionary<string, Tile> tiles)
        {
            Scene exampleScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            GameObject gridObject = new GameObject("Grid");
            SceneManager.MoveGameObjectToScene(gridObject, exampleScene);
            gridObject.AddComponent<Grid>();

            Tilemap ground = CreateTilemap("Ground", gridObject.transform, 0, false);
            Tilemap obstacles = CreateTilemap("Obstacles", gridObject.transform, 1, true);
            PaintExampleMap(ground, obstacles, tiles);

            GameObject cameraObject = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, exampleScene);
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 6.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject lightObject = new GameObject("Global Light 2D");
            SceneManager.MoveGameObjectToScene(lightObject, exampleScene);
            Light2D light2D = lightObject.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.intensity = 1f;

            EditorSceneManager.SaveScene(exampleScene, ExampleScenePath);
            EditorSceneManager.CloseScene(exampleScene, true);
        }

        private static Tilemap CreateTilemap(
            string name,
            Transform parent,
            int sortingOrder,
            bool addCollider)
        {
            GameObject tilemapObject = new GameObject(name);
            tilemapObject.transform.SetParent(parent, false);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;

            if (addCollider)
            {
                tilemapObject.AddComponent<TilemapCollider2D>();
            }

            return tilemap;
        }

        private static void PaintExampleMap(
            Tilemap ground,
            Tilemap obstacles,
            IReadOnlyDictionary<string, Tile> tiles)
        {
            const int minX = -7;
            const int maxX = 7;
            const int minY = -4;
            const int maxY = 4;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    bool border = x == minX || x == maxX || y == minY || y == maxY;
                    string tileName = border ? "DeepWater" : "Grass";

                    if (!border && (x == 0 || x == 1))
                    {
                        tileName = (x + y) % 3 == 0 ? "PebbleDirt" : "Dirt";
                    }
                    else if (!border && x >= 3 && x <= 6 && y >= 0 && y <= 3)
                    {
                        tileName = (x + y) % 4 == 0 ? "CrackedStone" : "Stone";
                    }
                    else if (!border && (x + y * 2) % 11 == 0)
                    {
                        tileName = "FlowerGrass";
                    }

                    ground.SetTile(new Vector3Int(x, y, 0), tiles[tileName]);
                }
            }

            for (int x = 3; x <= 6; x++)
            {
                obstacles.SetTile(new Vector3Int(x, 3, 0), tiles["StoneWallTop"]);
                obstacles.SetTile(new Vector3Int(x, 2, 0), tiles["StoneWallFace"]);
            }

            obstacles.SetTile(new Vector3Int(-5, 1, 0), tiles["Bush"]);
            obstacles.SetTile(new Vector3Int(-4, -2, 0), tiles["Bush"]);
            obstacles.SetTile(new Vector3Int(-3, 2, 0), tiles["Rock"]);
            obstacles.SetTile(new Vector3Int(4, -2, 0), tiles["Rock"]);

            ground.CompressBounds();
            obstacles.CompressBounds();
        }

        private static void EnsureFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string currentPath = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string nextPath = currentPath + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[index]);
                }

                currentPath = nextPath;
            }
        }
    }
}
