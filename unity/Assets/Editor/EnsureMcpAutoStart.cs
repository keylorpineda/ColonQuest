using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// Guarantees that the MCP for Unity HTTP bridge connects on editor startup and persists
    /// across domain reloads so agentic tooling remains available without manual clicks.
    /// </summary>
    [InitializeOnLoad]
    public static class EnsureMcpAutoStart
    {
        private const string AutoStartOnLoadKey = "MCPForUnity.AutoStartOnLoad";
        private const string UseHttpTransportKey = "MCPForUnity.UseHttpTransport";

        static EnsureMcpAutoStart()
        {
            EditorPrefs.SetBool(AutoStartOnLoadKey, true);
            EditorPrefs.SetBool(UseHttpTransportKey, true);
            EditorApplication.update += OnUpdate;
            EditorApplication.delayCall += EnsureScenesUpToDate;
        }

        private static void EnsureScenesUpToDate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                const string townPath = "Assets/Scenes/Town.unity";
                if (File.Exists(townPath))
                {
                    string content = File.ReadAllText(townPath);
                    if (!content.Contains("Roof_TilePattern") || !content.Contains("Plaza_CobblePattern"))
                    {
                        Debug.Log("[EnsureMcpAutoStart] Rebuilding scenes with SCRUM-94 cel-shaded textures & crossings fix...");
                        AssetDatabase.Refresh();
                        SpriteFactory.Generate();
                        MapBuilder.BuildAll();
                        EditorSceneManager.OpenScene(townPath, OpenSceneMode.Single);
                        Debug.Log("[EnsureMcpAutoStart] All scenes rebuilt and Town.unity opened.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[EnsureMcpAutoStart] Auto scene rebuild skipped: " + ex.Message);
            }
        }

        private static double _lastCheckTime;

        private static void OnUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastCheckTime < 5.0) return;
            _lastCheckTime = now;

            try
            {
                var transport = MCPServiceLocator.TransportManager;
                if (transport != null && !transport.IsRunning(TransportMode.Http))
                {
                    _ = transport.StartAsync(TransportMode.Http);
                    Debug.Log("[EnsureMcpAutoStart] Started MCP for Unity HTTP Bridge on main thread.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[EnsureMcpAutoStart] Failed to start HTTP bridge: " + ex.Message);
            }
        }
    }
}
