using System.IO;
using ColonQuest.Lighting;
using ColonQuest.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The town is lit by real 2D lights (URP 2D Renderer): one global light for the sun that changes through the day,
    /// and point lights in the street lamps, the porch lights and the player's surroundings that come on at night.
    /// </summary>
    /// <remarks>
    /// Only the town has lights. The interiors and the menu have none, and the 2D Renderer draws a sprite with no light
    /// on its layer exactly as an unlit one, so they look as they always did whatever material their sprites get.
    /// Everything here falls back to the old look (the full colored day wash, unlit sprites, no lights) when the project
    /// does not use the URP 2D asset this file creates, so a scene can always be built.
    /// </remarks>
    public static partial class MapBuilder
    {
        private const string PipelineFolder = "Assets/Settings";
        private const string PipelineAssetPath = PipelineFolder + "/ColonQuest_URP2D.asset";
        private const string RendererAssetPath = PipelineFolder + "/ColonQuest_URP2D_Renderer.asset";
        private const string LitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        private const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        /// <summary>True while a lit scene (the town) is being built: its sprites get the lit material.</summary>
        private static bool litScene;

        private static Material litMaterial;
        private static Material unlitMaterial;

        /// <summary>
        /// Makes sure the project renders with the 2D Renderer of URP: creates the pipeline asset the first time and sets
        /// it as the default. Another pipeline someone chose on purpose is left alone. Returns true when the town can be lit.
        /// </summary>
        [MenuItem("Colon Quest/Setup URP 2D Lighting")]
        public static void SetupUrp2D()
        {
            EnsureUrp2D();
        }

        public static bool EnsureUrp2D()
        {
            var current = GraphicsSettings.defaultRenderPipeline;
            var ours = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (current != null && current != ours)
            {
                Debug.LogWarning("[MapBuilder] The project already uses another render pipeline (" + current.name + "). The town is built without 2D lights.");
                return false;
            }

            if (ours == null)
            {
                Directory.CreateDirectory(PipelineFolder);
                var rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
                ours = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(ours, PipelineAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[MapBuilder] Created the URP 2D pipeline at " + PipelineAssetPath + ".");
            }

            if (current != ours)
            {
                // The quality levels keep no pipeline of their own, so they all use this default one.
                GraphicsSettings.defaultRenderPipeline = ours;
                AssetDatabase.SaveAssets();
                Debug.Log("[MapBuilder] The project now renders with the URP 2D Renderer.");
            }

            return true;
        }

        /// <summary>Loads the sprite materials of URP. Called for every scene; lighting stays off until a lit scene asks for it.</summary>
        private static void LoadLighting()
        {
            litScene = false;
            litMaterial = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);
            unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
        }

        /// <summary>Turns lighting on for the scene being built, when the pipeline and both materials are there.</summary>
        private static bool BeginLitScene()
        {
            litScene = EnsureUrp2D() && litMaterial != null && unlitMaterial != null;
            if (!litScene)
            {
                Debug.LogWarning("[MapBuilder] Building the town without 2D lights (URP 2D or its sprite materials are missing).");
            }

            return litScene;
        }

        private static void EndLitScene()
        {
            litScene = false;
        }

        /// <summary>
        /// Gives a sprite of a lit scene the lit material, so the sun and the lamps light it. What hangs from the camera
        /// (the vignette, the fade to black) stays unlit: it is a screen effect, not a thing in the town.
        /// </summary>
        private static void ApplyLighting(SpriteRenderer renderer)
        {
            if (!litScene || renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = renderer.GetComponentInParent<Camera>() != null ? unlitMaterial : litMaterial;
        }

        /// <summary>A glow sprite shines by itself: it must not be darkened by the night like the things around it.</summary>
        private static SpriteRenderer KeepUnlit(SpriteRenderer renderer)
        {
            if (litScene && renderer != null && unlitMaterial != null)
            {
                renderer.sharedMaterial = unlitMaterial;
            }

            return renderer;
        }

        /// <summary>The sun: the only global light of the town, driven by the hour (see <see cref="DayLight"/>).</summary>
        private static void BuildSunLight(Transform parent)
        {
            if (!litScene)
            {
                return;
            }

            var go = new GameObject("SunLight");
            go.transform.SetParent(parent, false);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.shadowsEnabled = false;
            DayCycle.LightFor(0f, out Color color, out float intensity);
            light.color = color;
            light.intensity = intensity;
            go.AddComponent<DayLight>();
        }

        /// <summary>
        /// A point light that comes on at night (see <see cref="NightLight"/>), at <paramref name="position"/> under
        /// <paramref name="parent"/>. Nothing is added when the scene is not lit.
        /// </summary>
        private static void AddNightLight(Transform parent, Vector2 position, float radius, string hex, float intensity, float seed)
        {
            if (!litScene)
            {
                return;
            }

            var go = new GameObject("NightLight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.shadowsEnabled = false;
            light.color = GamePalette.Hex(hex);
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.15f;
            light.falloffIntensity = 0.65f;
            go.AddComponent<NightLight>().Configure(intensity, 0.05f, seed);
        }
    }
}
