using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    // Lives on the Main scene root, not in the legacy HUD implementation.
    // The two modes keep their own scenes and their own save files.
    public sealed class ConstructionMenuEntry : MonoBehaviour
    {
        private const string ConstructionScene = "ConstructionPrototype";
        private GameRuntimeController runtime;
        private Transform menu;
        private Button entrance;
        private Text caption;
        private InterfaceLanguage shownLanguage;

        private IEnumerator Start()
        {
            // The legacy bootstrap creates its HUD at runtime. Wait for that
            // hierarchy instead of depending on component Awake order.
            for (var frame = 0; frame < 120; frame++)
            {
                runtime = GetComponentInChildren<GameRuntimeController>(true);
                var hud = GetComponentInChildren<UrbanWildlifeHud>(true);
                menu = hud?.transform.Find("Main Menu");
                if (runtime != null && menu != null)
                {
                    CreateEntrance();
                    yield break;
                }
                yield return null;
            }
            Debug.LogError("Construction entrance could not find the main menu.", this);
        }

        private void LateUpdate()
        {
            if (entrance == null || runtime == null || menu == null) return;
            if (shownLanguage != runtime.Language) UpdateCaption();
            var endlessSetup = menu.Find("Endless Difficulty Overlay");
            var researchSetup = menu.Find("Research Setup Overlay");
            var visible = runtime.AtDesktop && !runtime.SettingsOpen &&
                menu.gameObject.activeInHierarchy &&
                (endlessSetup == null || !endlessSetup.gameObject.activeSelf) &&
                (researchSetup == null || !researchSetup.gameObject.activeSelf);
            if (entrance.gameObject.activeSelf != visible)
                entrance.gameObject.SetActive(visible);
        }

        private void CreateEntrance()
        {
            var root = new GameObject("Construction Mode Entrance",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RoundedPanelGraphic), typeof(Button));
            root.transform.SetParent(menu, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.345f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(440f, 66f);

            var backing = root.GetComponent<RoundedPanelGraphic>();
            backing.color = new Color(0.12f, 0.21f, 0.27f, 0.96f);
            backing.BorderColor = new Color(0.91f, 0.79f, 0.53f);
            backing.BorderWidth = 2f;
            backing.CornerRadius = 23f;
            entrance = root.GetComponent<Button>();
            entrance.targetGraphic = backing;
            entrance.onClick.AddListener(EnterConstruction);

            var labelObject = new GameObject("Construction Entry Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(root.transform, false);
            caption = labelObject.GetComponent<Text>();
            caption.alignment = TextAnchor.MiddleCenter;
            caption.fontSize = 25;
            caption.color = new Color(0.98f, 0.94f, 0.84f);
            caption.raycastTarget = false;
            var labelRect = caption.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 2f);
            labelRect.offsetMax = new Vector2(-10f, -2f);

            // Keep the new entry below existing setup overlays in raycast order.
            var setup = menu.Find("Endless Difficulty Overlay");
            if (setup != null) root.transform.SetSiblingIndex(setup.GetSiblingIndex());
            UpdateCaption();
        }

        private void UpdateCaption()
        {
            shownLanguage = runtime.Language;
            var chinese = shownLanguage == InterfaceLanguage.Chinese;
            caption.font = Resources.Load<Font>(chinese
                ? "Fonts/NotoSansSC-Regular" : "Fonts/Nunito-Bold");
            caption.text = chinese
                ? "建造 49 格生态城  →" : "Build a 49-cell ecosystem  →";
        }

        public void EnterConstruction()
        {
            if (!Application.CanStreamedLevelBeLoaded(ConstructionScene))
            {
                Debug.LogError("The construction scene is not in build settings.", this);
                return;
            }
            SceneManager.LoadScene(ConstructionScene, LoadSceneMode.Single);
        }
    }
}
