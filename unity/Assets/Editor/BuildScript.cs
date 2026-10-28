using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// Command line builds for the web and the desktop. Pass -buildPath to choose the output folder;
    /// it should live outside the repository, since a build is large and never committed.
    /// </summary>
    public static class BuildScript
    {
        private static string[] Scenes()
        {
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            return scenes.ToArray();
        }

        [MenuItem("Colon Quest/Build/WebGL")]
        public static void WebGL()
        {
            // Gzip with the fallback loader works on any static host, even one that cannot set headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            Run(BuildTarget.WebGL, "webgl", string.Empty);
        }

        [MenuItem("Colon Quest/Build/Windows")]
        public static void Windows()
        {
            Run(BuildTarget.StandaloneWindows64, "windows", "ColonQuest.exe");
        }

        private static void Run(BuildTarget target, string defaultFolder, string fileName)
        {
            string folder = Argument("-buildPath") ?? Path.Combine(Path.GetTempPath(), "ColonQuestBuild", defaultFolder);
            Directory.CreateDirectory(folder);

            var options = new BuildPlayerOptions
            {
                scenes = Scenes(),
                locationPathName = string.IsNullOrEmpty(fileName) ? folder : Path.Combine(folder, fileName),
                target = target,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"Build {target}: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors, {report.summary.totalTime}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
