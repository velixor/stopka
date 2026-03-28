using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

namespace Stopka.Editor
{
    public static class SceneSetup
    {
        // --- Design tokens ---
        private static readonly Color PanelBg = new Color(0, 0, 0, 0.55f);
        private static readonly Color PanelBorder = new Color(1, 1, 1, 0.1f);
        private static readonly Color TextPrimary = new Color(1, 1, 1, 0.7f);
        private static readonly Color TextSecondary = new Color(1, 1, 1, 0.5f);
        private static readonly Color TextTertiary = new Color(1, 1, 1, 0.4f);
        private static readonly Color TextRestart = new Color(1, 1, 1, 0.55f);
        private static readonly Color GearDim = new Color(1, 1, 1, 0.35f);
        private static readonly Color DimBg = new Color(0, 0, 0, 0.4f);
        private static readonly Color ToggleOnTrack = new Color(1, 1, 1, 0.3f);
        private static readonly Color ToggleOffTrack = new Color(1, 1, 1, 0.15f);
        private static readonly Color ToggleOnKnob = Color.white;
        private static readonly Color ToggleOffKnob = new Color(1, 1, 1, 0.5f);

        private const float CharSpacingTitle = 30f;
        private const float CharSpacingSmall = 12f;
        private const float CharSpacingMedium = 18f;

        [MenuItem("Stopka/Setup Game Scene")]
        public static void SetupGameScene()
        {
            // --- Clean up existing scene objects ---
            foreach (var oldGm in Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None))
                Object.DestroyImmediate(oldGm.gameObject);
            foreach (var oldEs in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                Object.DestroyImmediate(oldEs.gameObject);
            foreach (var oldCanvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                Object.DestroyImmediate(oldCanvas.gameObject);
            if (Camera.main != null)
            {
                var camTransform = Camera.main.transform;
                for (int i = camTransform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(camTransform.GetChild(i).gameObject);
                foreach (var comp in Camera.main.GetComponents<MonoBehaviour>())
                    Object.DestroyImmediate(comp);
            }

            // --- GameConfig ---
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/GameConfig.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, "Assets/Resources/GameConfig.asset");
                AssetDatabase.SaveAssets();
            }

            // --- BlockBase material shader ---
            var blockBaseMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/BlockBase.mat");
            var blockWaveShader = Shader.Find("Stopka/BlockWave");
            if (blockBaseMat != null && blockWaveShader != null)
            {
                blockBaseMat.shader = blockWaveShader;
                EditorUtility.SetDirty(blockBaseMat);
            }

            // --- Font ---
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter-Light SDF.asset");
            if (font == null)
                Debug.LogWarning("SceneSetup: Inter-Light SDF font not found at Assets/Fonts/Inter-Light SDF.asset. Using default TMP font.");

            // --- GameManager object ---
            var gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<GameManager>();
            var spawner = gmObj.AddComponent<BlockSpawner>();
            var tower = gmObj.AddComponent<TowerManager>();
            var colorMgr = gmObj.AddComponent<BlockColorManager>();
            var audioMgr = gmObj.AddComponent<AudioManager>();

            WireField(spawner, "config", config);
            WireField(tower, "config", config);

            // --- Camera ---
            var camObj = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.GetComponent<Camera>() ?? camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;

            // --- Gradient Skybox ---
            var skyboxMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/GradientSkybox.mat");
            if (skyboxMat == null)
            {
                skyboxMat = new Material(Shader.Find("Stopka/GradientSkybox"));
                AssetDatabase.CreateAsset(skyboxMat, "Assets/Resources/GradientSkybox.mat");
            }
            RenderSettings.skybox = skyboxMat;
            camObj.transform.position = config.cameraOffset;
            camObj.transform.LookAt(Vector3.zero);

            var camCtrl = camObj.AddComponent<CameraController>();
            WireField(camCtrl, "config", config);

            var skyboxCtrl = camObj.AddComponent<SkyboxController>();
            WireField(skyboxCtrl, "skyboxMaterial", skyboxMat);

            // --- Clean up old renderer features ---
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/Mobile_Renderer.asset");
            if (rendererData != null)
            {
                var so = new SerializedObject(rendererData);
                var featuresProp = so.FindProperty("m_RendererFeatures");
                bool changed = false;
                for (int i = featuresProp.arraySize - 1; i >= 0; i--)
                {
                    var element = featuresProp.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue == null)
                    {
                        featuresProp.DeleteArrayElementAtIndex(i);
                        changed = true;
                    }
                }
                var intermediateTexProp = so.FindProperty("m_IntermediateTextureMode");
                if (intermediateTexProp != null && intermediateTexProp.intValue != 0)
                {
                    intermediateTexProp.intValue = 0;
                    changed = true;
                }
                if (changed)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(rendererData);
                    AssetDatabase.SaveAssets();
                }
            }

            // --- Tower Wave Controller ---
            var towerWave = gmObj.AddComponent<TowerWaveController>();
            WireField(towerWave, "config", config);

            // --- CameraShake ---
            var shakeObj = new GameObject("ShakeTarget");
            shakeObj.transform.SetParent(camObj.transform, false);
            var shake = shakeObj.AddComponent<CameraShake>();

            // ============================
            // --- Canvas ---
            // ============================
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // --- Safe Area wrapper (for top elements: score, gear) ---
            var safeArea = new GameObject("SafeArea", typeof(RectTransform));
            safeArea.transform.SetParent(canvasObj.transform, false);
            var safeAreaRT = safeArea.GetComponent<RectTransform>();
            safeAreaRT.anchorMin = Vector2.zero;
            safeAreaRT.anchorMax = Vector2.one;
            safeAreaRT.sizeDelta = Vector2.zero;
            safeArea.AddComponent<SafeAreaFitter>();

            // ============================
            // --- Start Panel ---
            // ============================
            var startPanel = CreateFrostedPanel(canvasObj.transform, "StartPanel");
            var startFader = startPanel.GetComponent<UIFader>();

            // Inner content container (centered)
            var startContent = CreateCenteredContainer(startPanel.transform, "StartContent", 0.25f, 0.75f);

            var titleText = CreateTMP(startContent.transform, "TitleText", "STOPKA", 72, TextAlignmentOptions.Center, font);
            titleText.GetComponent<TextMeshProUGUI>().color = Color.white;
            titleText.GetComponent<TextMeshProUGUI>().characterSpacing = CharSpacingTitle;
            SetAnchors(titleText, new Vector2(0, 0.6f), new Vector2(1, 0.85f));

            var tapText = CreateTMP(startContent.transform, "TapText", "TAP TO START", 36, TextAlignmentOptions.Center, font);
            tapText.GetComponent<TextMeshProUGUI>().color = TextPrimary;
            tapText.GetComponent<TextMeshProUGUI>().characterSpacing = CharSpacingSmall;
            SetAnchors(tapText, new Vector2(0, 0.4f), new Vector2(1, 0.55f));

            // Divider
            var divider1 = CreateDivider(startContent.transform, "Divider");
            SetAnchors(divider1, new Vector2(0.4f, 0.32f), new Vector2(0.6f, 0.325f));

            var hsStartText = CreateTMP(startContent.transform, "HighScoreText", "BEST: 0", 28, TextAlignmentOptions.Center, font);
            hsStartText.GetComponent<TextMeshProUGUI>().color = TextSecondary;
            hsStartText.GetComponent<TextMeshProUGUI>().characterSpacing = CharSpacingSmall;
            SetAnchors(hsStartText, new Vector2(0, 0.18f), new Vector2(1, 0.3f));

            // ============================
            // --- Playing Panel (inside SafeArea) ---
            // ============================
            var playingPanel = new GameObject("PlayingPanel", typeof(RectTransform));
            playingPanel.transform.SetParent(safeArea.transform, false);
            var playingRT = playingPanel.GetComponent<RectTransform>();
            playingRT.anchorMin = Vector2.zero;
            playingRT.anchorMax = Vector2.one;
            playingRT.sizeDelta = Vector2.zero;
            var playingCG = playingPanel.AddComponent<CanvasGroup>();
            var playingFader = playingPanel.AddComponent<UIFader>();

            var scoreTextObj = CreateTMP(playingPanel.transform, "ScoreText", "0", 64, TextAlignmentOptions.Top, font);
            scoreTextObj.GetComponent<TextMeshProUGUI>().color = Color.white;
            scoreTextObj.GetComponent<TextMeshProUGUI>().characterSpacing = CharSpacingSmall;
            // TMP underlay for drop shadow
            var scoreTMP = scoreTextObj.GetComponent<TextMeshProUGUI>();
            scoreTMP.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            scoreTMP.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -1f);
            scoreTMP.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.3f);
            scoreTMP.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.5f);
            scoreTMP.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0.3f));
            scoreTMP.fontMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            SetAnchors(scoreTextObj, new Vector2(0.3f, 0.85f), new Vector2(0.7f, 0.98f));

            // NEW BEST label (hidden by default)
            var newBestObj = CreateTMP(playingPanel.transform, "NewBestLabel", "NEW BEST", 20, TextAlignmentOptions.Top, font);
            var newBestTMP = newBestObj.GetComponent<TextMeshProUGUI>();
            newBestTMP.color = new Color(1f, 0.843f, 0f, 0.6f);
            newBestTMP.characterSpacing = CharSpacingMedium;
            SetAnchors(newBestObj, new Vector2(0.3f, 0.82f), new Vector2(0.7f, 0.86f));
            newBestObj.SetActive(false);

            // ============================
            // --- Game Over Panel ---
            // ============================
            var gameOverPanel = CreateFrostedPanel(canvasObj.transform, "GameOverPanel");
            var gameOverFader = gameOverPanel.GetComponent<UIFader>();

            var goContent = CreateCenteredContainer(gameOverPanel.transform, "GameOverContent", 0.2f, 0.8f);

            var goText = CreateTMP(goContent.transform, "GameOverText", "GAME OVER", 22, TextAlignmentOptions.Center, font);
            goText.GetComponent<TextMeshProUGUI>().color = TextSecondary;
            goText.GetComponent<TextMeshProUGUI>().characterSpacing = CharSpacingMedium;
            SetAnchors(goText, new Vector2(0, 0.72f), new Vector2(1, 0.85f));

            var finalScoreObj = CreateTMP(goContent.transform, "FinalScore", "0", 72, TextAlignmentOptions.Center, font);
            finalScoreObj.GetComponent<TextMeshProUGUI>().color = Color.white;
            SetAnchors(finalScoreObj, new Vector2(0, 0.48f), new Vector2(1, 0.7f));

            var divider2 = CreateDivider(goContent.transform, "Divider");
            SetAnchors(divider2, new Vector2(0.4f, 0.42f), new Vector2(0.6f, 0.425f));

            var hsEndText = CreateTMP(goContent.transform, "HighScoreText", "BEST: 0", 22, TextAlignmentOptions.Center, font);
            hsEndText.GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.45f);
            hsEndText.GetComponent<TextMeshProUGUI>().characterSpacing = CharSpacingSmall;
            SetAnchors(hsEndText, new Vector2(0, 0.3f), new Vector2(1, 0.4f));

            var restartText = CreateTMP(goContent.transform, "RestartText", "TAP TO RESTART", 22, TextAlignmentOptions.Center, font);
            restartText.GetComponent<TextMeshProUGUI>().color = TextRestart;
            restartText.GetComponent<TextMeshProUGUI>().characterSpacing = CharSpacingMedium;
            SetAnchors(restartText, new Vector2(0, 0.12f), new Vector2(1, 0.25f));

            gameOverPanel.SetActive(false);

            // ============================
            // --- Settings Panel ---
            // ============================
            // Dim background (fullscreen, behind settings)
            var dimBg = new GameObject("SettingsDimBg", typeof(RectTransform));
            dimBg.transform.SetParent(canvasObj.transform, false);
            var dimBgRT = dimBg.GetComponent<RectTransform>();
            dimBgRT.anchorMin = Vector2.zero;
            dimBgRT.anchorMax = Vector2.one;
            dimBgRT.sizeDelta = Vector2.zero;
            var dimBgImg = dimBg.AddComponent<Image>();
            dimBgImg.color = DimBg;
            var dimBgBtn = dimBg.AddComponent<Button>();
            dimBgBtn.transition = Selectable.Transition.None;
            dimBg.SetActive(false);

            var settingsPanel = CreateFrostedPanel(canvasObj.transform, "SettingsPanel", 0.1f, 0.35f, 0.9f, 0.65f);
            var settingsFader = settingsPanel.GetComponent<UIFader>();

            var settingsContent = CreateCenteredContainer(settingsPanel.transform, "SettingsContent", 0.05f, 0.95f);
            var settingsContentRT = settingsContent.GetComponent<RectTransform>();
            settingsContentRT.anchorMin = new Vector2(0.1f, 0.05f);
            settingsContentRT.anchorMax = new Vector2(0.9f, 0.95f);

            var settingsTitle = CreateTMP(settingsContent.transform, "SettingsTitle", "SETTINGS", 22, TextAlignmentOptions.Center, font);
            settingsTitle.GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.6f);
            settingsTitle.GetComponent<TextMeshProUGUI>().characterSpacing = CharSpacingMedium;
            SetAnchors(settingsTitle, new Vector2(0, 0.78f), new Vector2(1, 0.95f));

            // Sound toggle row
            var soundRow = CreateToggleRow(settingsContent.transform, "SoundRow", "SOUND", font, 0.5f, 0.72f);
            var soundTrack = soundRow.transform.Find("ToggleTrack").GetComponent<Image>();
            var soundKnob = soundRow.transform.Find("ToggleTrack/Knob").GetComponent<RectTransform>();

            // Vibration toggle row
            var vibrationRow = CreateToggleRow(settingsContent.transform, "VibrationRow", "VIBRATION", font, 0.25f, 0.47f);
            var vibrationTrack = vibrationRow.transform.Find("ToggleTrack").GetComponent<Image>();
            var vibrationKnob = vibrationRow.transform.Find("ToggleTrack/Knob").GetComponent<RectTransform>();

            // Divider
            var divider3 = CreateDivider(settingsContent.transform, "Divider");
            SetAnchors(divider3, new Vector2(0.35f, 0.18f), new Vector2(0.65f, 0.185f));

            // Close text
            var closeText = CreateTMP(settingsContent.transform, "CloseText", "TAP TO CLOSE", 18, TextAlignmentOptions.Center, font);
            closeText.GetComponent<TextMeshProUGUI>().color = TextTertiary;
            closeText.GetComponent<TextMeshProUGUI>().characterSpacing = CharSpacingSmall;
            SetAnchors(closeText, new Vector2(0, 0.02f), new Vector2(1, 0.15f));

            settingsPanel.SetActive(false);

            // ============================
            // --- Gear Icons (inside SafeArea) ---
            // ============================
            var gearBtn = CreateGearButton(safeArea.transform, "GearButton", font);

            // ============================
            // --- GameUI component ---
            // ============================
            var gameUI = gmObj.AddComponent<GameUI>();
            WireField(gameUI, "startFader", startFader);
            WireField(gameUI, "playingFader", playingFader);
            WireField(gameUI, "gameOverFader", gameOverFader);
            WireField(gameUI, "settingsFader", settingsFader);
            WireField(gameUI, "settingsDimBackground", dimBg);
            WireField(gameUI, "highScoreStartText", hsStartText.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "tapToStartText", tapText.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "scoreText", scoreTextObj.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "newBestLabel", newBestTMP);
            WireField(gameUI, "gameOverTitleText", goText.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "finalScoreText", finalScoreObj.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "highScoreEndText", hsEndText.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "restartText", restartText.GetComponent<TextMeshProUGUI>());
            WireField(gameUI, "soundToggleTrack", soundTrack);
            WireField(gameUI, "soundToggleKnob", soundKnob);
            WireField(gameUI, "vibrationToggleTrack", vibrationTrack);
            WireField(gameUI, "vibrationToggleKnob", vibrationKnob);
            WireField(gameUI, "audioManager", audioMgr);

            // Wire button events
            // Gear -> OpenSettings
            var gearBtnComp = gearBtn.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(gearBtnComp.onClick, gameUI.OpenSettings);

            // Dim background -> CloseSettings
            UnityEditor.Events.UnityEventTools.AddPersistentListener(dimBgBtn.onClick, gameUI.CloseSettings);

            // Sound toggle button
            var soundBtn = soundRow.transform.Find("ToggleTrack").gameObject.AddComponent<Button>();
            soundBtn.transition = Selectable.Transition.None;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(soundBtn.onClick, gameUI.ToggleSound);

            // Vibration toggle button
            var vibBtn = vibrationRow.transform.Find("ToggleTrack").gameObject.AddComponent<Button>();
            vibBtn.transition = Selectable.Transition.None;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(vibBtn.onClick, gameUI.ToggleVibration);

            // --- Wire GameManager ---
            WireField(gm, "config", config);
            WireField(gm, "spawner", spawner);
            WireField(gm, "tower", tower);
            WireField(gm, "cameraController", camCtrl);
            WireField(gm, "colorManager", colorMgr);
            WireField(gm, "gameUI", gameUI);
            WireField(gm, "audioManager", audioMgr);
            WireField(gm, "cameraShake", shake);
            WireField(gm, "towerWave", towerWave);
            WireField(gm, "skyboxController", skyboxCtrl);
            WireField(colorMgr, "config", config);

            // --- EventSystem ---
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
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

        // ============================
        // Helper methods
        // ============================

        private static GameObject CreateFrostedPanel(Transform parent, string name,
            float anchorMinX = 0f, float anchorMinY = 0f, float anchorMaxX = 1f, float anchorMaxY = 1f)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = panel.AddComponent<Image>();
            img.color = PanelBg;
            img.raycastTarget = false;

            // Add outline for subtle border
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = PanelBorder;
            outline.effectDistance = new Vector2(1, -1);

            panel.AddComponent<CanvasGroup>();
            panel.AddComponent<UIFader>();

            return panel;
        }

        private static GameObject CreateCenteredContainer(Transform parent, string name,
            float anchorMinY, float anchorMaxY)
        {
            var container = new GameObject(name, typeof(RectTransform));
            container.transform.SetParent(parent, false);
            var rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, anchorMinY);
            rt.anchorMax = new Vector2(0.9f, anchorMaxY);
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return container;
        }

        private static GameObject CreateDivider(Transform parent, string name)
        {
            var divider = new GameObject(name, typeof(RectTransform));
            divider.transform.SetParent(parent, false);
            var img = divider.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.25f);
            img.raycastTarget = false;
            return divider;
        }

        private static GameObject CreateTMP(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            if (font != null)
                tmp.font = font;
            return obj;
        }

        private static GameObject CreateToggleRow(Transform parent, string name, string label, TMP_FontAsset font,
            float anchorMinY, float anchorMaxY)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, anchorMinY);
            rowRT.anchorMax = new Vector2(1, anchorMaxY);
            rowRT.sizeDelta = Vector2.zero;
            rowRT.offsetMin = Vector2.zero;
            rowRT.offsetMax = Vector2.zero;

            // Label
            var labelObj = CreateTMP(row.transform, "Label", label, 22, TextAlignmentOptions.MidlineLeft, font);
            labelObj.GetComponent<TextMeshProUGUI>().color = TextPrimary;
            labelObj.GetComponent<TextMeshProUGUI>().characterSpacing = 8f;
            SetAnchors(labelObj, new Vector2(0, 0), new Vector2(0.6f, 1));

            // Toggle track
            var track = new GameObject("ToggleTrack", typeof(RectTransform));
            track.transform.SetParent(row.transform, false);
            var trackRT = track.GetComponent<RectTransform>();
            trackRT.anchorMin = new Vector2(0.7f, 0.2f);
            trackRT.anchorMax = new Vector2(1f, 0.8f);
            trackRT.sizeDelta = Vector2.zero;
            trackRT.offsetMin = Vector2.zero;
            trackRT.offsetMax = Vector2.zero;
            var trackImg = track.AddComponent<Image>();
            trackImg.color = ToggleOnTrack;

            // Make track look like a pill (rounded)
            // We'll rely on the default white sprite; for true rounded corners a custom sprite would be needed

            // Knob
            var knob = new GameObject("Knob", typeof(RectTransform));
            knob.transform.SetParent(track.transform, false);
            var knobRT = knob.GetComponent<RectTransform>();
            knobRT.anchorMin = new Vector2(0.5f, 0.5f);
            knobRT.anchorMax = new Vector2(0.5f, 0.5f);
            knobRT.sizeDelta = new Vector2(32, 32);
            var knobImg = knob.AddComponent<Image>();
            knobImg.color = ToggleOnKnob;

            return row;
        }

        private static GameObject CreateGearButton(Transform parent, string name, TMP_FontAsset font)
        {
            var btn = new GameObject(name, typeof(RectTransform));
            btn.transform.SetParent(parent, false);
            var btnRT = btn.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(1, 1);
            btnRT.anchorMax = new Vector2(1, 1);
            btnRT.pivot = new Vector2(1, 1);
            btnRT.sizeDelta = new Vector2(60, 60);
            btnRT.anchoredPosition = new Vector2(-16, -16);

            // Background circle
            var bgImg = btn.AddComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0.08f);

            // Gear text (unicode gear character)
            var gearText = CreateTMP(btn.transform, "GearIcon", "...", 32, TextAlignmentOptions.Center, font);
            gearText.GetComponent<TextMeshProUGUI>().color = GearDim;
            var gearRT = gearText.GetComponent<RectTransform>();
            gearRT.anchorMin = Vector2.zero;
            gearRT.anchorMax = Vector2.one;
            gearRT.sizeDelta = Vector2.zero;

            btn.AddComponent<Button>().transition = Selectable.Transition.None;

            return btn;
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
    }
}
