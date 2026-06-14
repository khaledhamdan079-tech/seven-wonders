using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SevenWondersDuel.Editor
{
    public static class AndroidBuild
    {
        private const string PreferredNdkVersion = "27.2.12479018";

        [MenuItem("Seven Wonders Duel/Build Android APK")]
        public static void BuildApk()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var outputDirectory = Path.Combine(projectRoot, "Builds", "Android");
            var outputPath = Path.Combine(outputDirectory, "seven-wonders-duel.apk");
            Directory.CreateDirectory(outputDirectory);

            ConfigureAndroidExternalTools();

            PlayerSettings.companyName = "Khaled";
            PlayerSettings.productName = "Seven Wonders Duel";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.khaled.sevenwondersduel");
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.forceInternetPermission = true;

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = false;

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
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Android build failed: " + report.summary.result);
            }

            Debug.Log("Android APK built at " + outputPath);
        }

        private static void ConfigureAndroidExternalTools()
        {
            var sdkRoot = FindAndroidSdkRoot();
            var ndkRoot = FindAndroidNdkRoot(sdkRoot);
            var jdkRoot = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "OpenJDK");

            if (!string.IsNullOrEmpty(sdkRoot))
            {
                AndroidExternalToolsSettings.sdkRootPath = sdkRoot;
                Debug.Log("Android SDK path set to " + sdkRoot);
            }

            if (!string.IsNullOrEmpty(ndkRoot))
            {
                AndroidExternalToolsSettings.ndkRootPath = ndkRoot;
                Debug.Log("Android NDK path set to " + ndkRoot);
            }

            if (Directory.Exists(jdkRoot))
            {
                AndroidExternalToolsSettings.jdkRootPath = jdkRoot;
                Debug.Log("Android JDK path set to " + jdkRoot);
            }

            AndroidExternalToolsSettings.maxJvmHeapSize = Math.Max(AndroidExternalToolsSettings.maxJvmHeapSize, 4096);
        }

        private static string FindAndroidSdkRoot()
        {
            var candidates = new[]
            {
                Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
                Environment.GetEnvironmentVariable("ANDROID_HOME"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "sdk")
            };

            return candidates
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .FirstOrDefault(path => Directory.Exists(Path.Combine(path, "platform-tools")));
        }

        private static string FindAndroidNdkRoot(string sdkRoot)
        {
            var envCandidates = new[]
            {
                Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT"),
                Environment.GetEnvironmentVariable("ANDROID_NDK_HOME")
            };

            var envRoot = envCandidates
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .FirstOrDefault(IsValidNdkRoot);

            if (!string.IsNullOrEmpty(envRoot))
            {
                return envRoot;
            }

            if (string.IsNullOrEmpty(sdkRoot))
            {
                return null;
            }

            var ndkDirectory = Path.Combine(sdkRoot, "ndk");
            if (Directory.Exists(ndkDirectory))
            {
                var preferredRoot = Path.Combine(ndkDirectory, PreferredNdkVersion);
                if (IsValidNdkRoot(preferredRoot))
                {
                    return preferredRoot;
                }

                var ndkRoot = Directory.GetDirectories(ndkDirectory)
                    .Where(IsValidNdkRoot)
                    .OrderByDescending(path => new DirectoryInfo(path).Name, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(ndkRoot))
                {
                    return ndkRoot;
                }
            }

            var legacyRoot = Path.Combine(sdkRoot, "ndk-bundle");
            return IsValidNdkRoot(legacyRoot) ? legacyRoot : null;
        }

        private static bool IsValidNdkRoot(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   Directory.Exists(path) &&
                   File.Exists(Path.Combine(path, "source.properties"));
        }
    }
}
