using UnityEditor;
using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    public static partial class MapBuilder
    {
        /// <summary>Placeholder town: only the ground, the spawn points and the player. The real layout comes next.</summary>
        [MenuItem("Colon Quest/Build Town Scene")]
        public static void BuildTown()
        {
            var scene = BeginScene();
            var map = new GameObject("Map").transform;

            currentLayer = LayerFloor;
            Shape(Group(map, "Ground"), "Grass", rounded, new Vector2(0f, -1.5f), new Vector2(56f, 36f), GamePalette.Hex("#6DBE7A"), 0);
            currentLayer = LayerWorld;

            BuildSpawnPoint(map, SpawnIds.Start, new Vector2(0f, 2f));
            BuildSpawnPoint(map, SpawnIds.TownFromCasa, new Vector2(-19.5f, 4.3f));
            BuildSpawnPoint(map, SpawnIds.TownFromBanco, new Vector2(-8.5f, 4.3f));
            BuildSpawnPoint(map, SpawnIds.TownFromTienda, new Vector2(8.5f, 4.3f));

            var player = BuildPlayer(new Vector2(0f, 2f), true);
            BuildCamera(MapLayout.TownCameraSize, GamePalette.Hex("#6DBE7A"), new Vector2(0f, 2f))
                .AddComponent<CameraFollow>().Configure(player, MapLayout.TownBounds);

            SaveScene(scene, SceneNames.Town);
        }
    }
}
