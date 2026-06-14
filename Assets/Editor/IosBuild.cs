#if UNITY_EDITOR_OSX
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SevenWondersDuel.Editor
{
    public static class IosBuild
    {
        [MenuItem("Seven Wonders Duel/Build iOS Xcode Project")]
        public static void BuildXcodeProject()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var outputDirectory = Path.Combine(projectRoot, "Builds", "iOS");
            Directory.CreateDirectory(outputDirectory);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

            PlayerSettings.companyName = "Khaled";
            PlayerSettings.productName = "Seven Wonders Duel";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.khaled.sevenwondersduel");
            PlayerSettings.iOS.buildNumber = "1";

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" };
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputDirectory,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("iOS build failed: " + report.summary.result);
            }

            Debug.Log("iOS Xcode project built at " + outputDirectory);
        }
    }
}
#endif
