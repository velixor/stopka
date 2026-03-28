using UnityEditor;
using UnityEngine;

namespace Stopka.Editor
{
    public static class PlayerSettingsSetup
    {
        [MenuItem("Stopka/Apply Player Settings")]
        public static void Apply()
        {
            // Bundle ID
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "dev.velixor.stopka");

            // Version
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.iOS.buildNumber = "1";

            // Orientation — portrait only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Company name
            PlayerSettings.companyName = "velixor";
            PlayerSettings.productName = "Stopka";

            Debug.Log("Player Settings applied:\n" +
                      $"  Bundle ID: dev.velixor.stopka\n" +
                      $"  Version: 1.0.0 (build 1)\n" +
                      $"  Orientation: Portrait only\n" +
                      $"  Company: velixor");

            EditorUtility.DisplayDialog("Stopka", "Player Settings applied!", "OK");
        }
    }
}
