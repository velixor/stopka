using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Stopka.Editor
{
    public static class SceneSetup
    {
        [MenuItem("Stopka/Setup Game Scene")]
        public static void SetupGameScene()
        {
            // --- Create GameConfig asset ---
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, "Assets/Resources/GameConfig.asset");
            AssetDatabase.SaveAssets();

            // --- GameManager object (holds multiple components) ---
            var gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<GameManager>();
            var spawner = gmObj.AddComponent<BlockSpawner>();
            var tower = gmObj.AddComponent<TowerManager>();
            var colorMgr = gmObj.AddComponent<BlockColorManager>();
            var audioMgr = gmObj.AddComponent<AudioManager>();

            // Wire config via SerializedObject
            WireField(spawner, "config", config);
            WireField(tower, "config", config);

            // --- Camera ---
            var camObj = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.GetComponent<Camera>() ?? camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = HexColor("1a1a2e");
            cam.fieldOfView = 60f;
            camObj.transform.position = new Vector3(5f, 5f, 5f);
            camObj.transform.LookAt(Vector3.zero);

            var camCtrl = camObj.AddComponent<CameraController>();

            // ShakeTarget child
            var shakeObj = new GameObject("ShakeTarget");
            shakeObj.transform.SetParent(camObj.transform, false);
            var shake = shakeObj.AddComponent<CameraShake>();

            // --- Canvas ---
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // --- Start Panel ---
            var startPanel = CreatePanel(canvasObj.transform, "StartPanel");
            var titleText = CreateTMP(startPanel.transform, "TitleText", "STOPKA", 72, TextAlignmentOptions.Center);
            SetAnchors(titleText, new Vector2(0, 0.6f), new Vector2(1, 0.8f));
            var tapText = CreateTMP(startPanel.transform, "TapText", "Tap to Start", 36, TextAlignmentOptions.Center);
            SetAnchors(tapText, new Vector2(0, 0.45f), new Vector2(1, 0.55f));
            var hsStartText = CreateTMP(startPanel.transform, "HighScoreText", "Best: 0", 28, TextAlignmentOptions.Center);
            SetAnchors(hsStartText, new Vector2(0, 0.35f), new Vector2(1, 0.45f));

            // --- Playing Panel ---
            var playingPanel = CreatePanel(canvasObj.transform, "PlayingPanel", true);
            var scoreTextObj = CreateTMP(playingPanel.transform, "ScoreText", "0", 64, TextAlignmentOptions.Top);
            SetAnchors(scoreTextObj, new Vector2(0.3f, 0.85f), new Vector2(0.7f, 0.98f));
            var comboTextObj = CreateTMP(playingPanel.transform, "ComboText", "PERFECT!", 48, TextAlignmentOptions.Center);
            SetAnchors(comboTextObj, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.55f));
            comboTextObj.SetActive(false);

            // --- Game Over Panel ---
            var gameOverPanel = CreatePanel(canvasObj.transform, "GameOverPanel");
            var goText = CreateTMP(gameOverPanel.transform, "GameOverText", "GAME OVER", 56, TextAlignmentOptions.Center);
            SetAnchors(goText, new Vector2(0, 0.6f), new Vector2(1, 0.75f));
            var finalScoreObj = CreateTMP(gameOverPanel.transform, "FinalScore", "0", 72, TextAlignmentOptions.Center);
            SetAnchors(finalScoreObj, new Vector2(0, 0.45f), new Vector2(1, 0.6f));
            var hsEndText = CreateTMP(gameOverPanel.transform, "HighScoreText", "Best: 0", 28, TextAlignmentOptions.Center);
            SetAnchors(hsEndText, new Vector2(0, 0.35f), new Vector2(1, 0.45f));
            var newBadge = CreateTMP(gameOverPanel.transform, "NewBadge", "NEW!", 32, TextAlignmentOptions.Center);
            SetAnchors(newBadge, new Vector2(0.6f, 0.42f), new Vector2(0.8f, 0.48f));
            newBadge.GetComponent<TextMeshProUGUI>().color = Color.yellow;
            newBadge.SetActive(false);
            var restartText = CreateTMP(gameOverPanel.transform, "RestartText", "Tap to Restart", 36, TextAlignmentOptions.Center);
            SetAnchors(restartText, new Vector2(0, 0.2f), new Vector2(1, 0.3f));

            gameOverPanel.SetActive(false);

            // --- GameUI component ---
            var gameUI = gmObj.AddComponent<GameUI>();
            WireField(gameUI, "startPanel", startPanel);
            WireField(gameUI, "highScoreStartText", hsStartText.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "playingPanel", playingPanel);
            WireField(gameUI, "scoreText", scoreTextObj.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "comboText", comboTextObj.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "gameOverPanel", gameOverPanel);
            WireField(gameUI, "finalScoreText", finalScoreObj.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "highScoreEndText", hsEndText.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "newHighScoreBadge", newBadge);

            // --- Wire GameManager ---
            WireField(gm, "config", config);
            WireField(gm, "spawner", spawner);
            WireField(gm, "tower", tower);
            WireField(gm, "cameraController", camCtrl);
            WireField(gm, "colorManager", colorMgr);
            WireField(gm, "gameUI", gameUI);
            WireField(gm, "audioManager", audioMgr);
            WireField(gm, "cameraShake", shake);

            // --- EventSystem ---
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // --- Directional Light ---
            if (Object.FindAnyObjectByType<Light>() == null)
            {
                var lightObj = new GameObject("Directional Light");
                var light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // --- Save scene ---
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Stopka", "Game scene setup complete!\n\nSave the scene as Assets/Scenes/GameScene.unity.", "OK");
        }

        private static GameObject CreatePanel(Transform parent, string name, bool transparent = false)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            if (!transparent)
            {
                var img = panel.AddComponent<UnityEngine.UI.Image>();
                img.color = new Color(0, 0, 0, 0.5f);
            }

            return panel;
        }

        private static GameObject CreateTMP(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return obj;
        }

        private static void SetAnchors(GameObject obj, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void WireField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"SceneSetup: Could not find field '{fieldName}' on {target.GetType().Name}");
            }
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
