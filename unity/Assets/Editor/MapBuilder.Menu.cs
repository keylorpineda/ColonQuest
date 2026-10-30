using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ColonQuest.UI;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    /// <summary>The main menu scene: a camera and the object that builds the whole screen when it starts.</summary>
    public static partial class MapBuilder
    {
        [MenuItem("Colon Quest/Build Menu Scene")]
        public static void BuildMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Main Camera") { tag = "MainCamera" };
            camera.transform.position = new Vector3(0f, 0f, -10f);
            var view = camera.AddComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = 5f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = GamePalette.Hex("#8FD3FF");
            camera.AddComponent<AudioListener>();

            new GameObject("MainMenu").AddComponent<MainMenu>();

            SaveScene(scene, SceneNames.Menu);
        }
    }
}
