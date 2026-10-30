using System;
using UnityEditor;
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
        }

        private static void OnUpdate()
        {
            EditorApplication.update -= OnUpdate;
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
