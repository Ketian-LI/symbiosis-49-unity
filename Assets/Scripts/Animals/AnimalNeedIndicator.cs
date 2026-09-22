using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Animals
{
    public sealed class AnimalNeedIndicator : MonoBehaviour
    {
        private const float IconWorldSize = 0.22f;

        private readonly List<GameObject> activeIcons = new(3);

        private GameObject indicatorRoot;
        private GameObject hungerIcon;
        private GameObject habitatIcon;
        private GameObject safetyIcon;
        private bool selected;
        private bool alive = true;
        private bool hungry;
        private bool habitatWarning;
        private bool danger;

        public bool IsVisible => indicatorRoot != null && indicatorRoot.activeSelf;
        public int VisibleCount => IsVisible ? activeIcons.Count : 0;

        public void Initialize(HideFlags hideFlags = HideFlags.None)
        {
            if (indicatorRoot != null)
            {
                return;
            }

            indicatorRoot = new GameObject("Animal Need Indicators")
            {
                hideFlags = hideFlags
            };
            indicatorRoot.transform.SetParent(transform, false);
            indicatorRoot.transform.localPosition = new Vector3(0f, 0.78f, 0f);

            hungerIcon = CreateIcon(
                "Hunger Warning",
                AnimalNeedVisualCatalog.GetSprite(AnimalNeedVisual.HungerWarning),
                61,
                hideFlags);
            habitatIcon = CreateIcon(
                "Habitat Warning",
                AnimalNeedVisualCatalog.GetSprite(AnimalNeedVisual.HabitatWarning),
                61,
                hideFlags);
            safetyIcon = CreateIcon(
                "Safety Danger",
                AnimalNeedVisualCatalog.GetSprite(AnimalNeedVisual.SafetyDanger),
                62,
                hideFlags);

            Refresh();
        }

        public void SetSelected(bool value)
        {
            selected = value;
            Refresh();
        }

        public void SetAlive(bool value)
        {
            alive = value;
            Refresh();
        }

        public void SetNeeds(bool hungerNeedsAttention, bool habitatNeedsAttention, bool safetyDanger)
        {
            hungry = hungerNeedsAttention;
            habitatWarning = habitatNeedsAttention;
            danger = safetyDanger;
            Refresh();
        }

        private void LateUpdate()
        {
            if (!IsVisible)
            {
                return;
            }

            var camera = Camera.main;
            if (camera != null)
            {
                indicatorRoot.transform.rotation = camera.transform.rotation;
            }
        }

        private void Refresh()
        {
            if (indicatorRoot == null)
            {
                return;
            }

            activeIcons.Clear();
            SetIconActive(hungerIcon, hungry);
            SetIconActive(habitatIcon, habitatWarning);
            SetIconActive(safetyIcon, danger);

            var visible = selected && alive && activeIcons.Count > 0;
            indicatorRoot.SetActive(visible);
            if (!visible)
            {
                return;
            }

            LayoutActiveIcons();
        }

        private void SetIconActive(GameObject icon, bool isActive)
        {
            if (icon == null)
            {
                return;
            }

            icon.SetActive(isActive);
            if (isActive)
            {
                activeIcons.Add(icon);
            }
        }

        private void LayoutActiveIcons()
        {
            if (danger)
            {
                safetyIcon.transform.localPosition = new Vector3(0f, 0.055f, 0f);

                var secondaryCount = (hungry ? 1 : 0) + (habitatWarning ? 1 : 0);
                if (secondaryCount == 1)
                {
                    var secondary = hungry ? hungerIcon : habitatIcon;
                    secondary.transform.localPosition = new Vector3(-0.18f, -0.035f, 0.012f);
                }
                else if (secondaryCount == 2)
                {
                    hungerIcon.transform.localPosition = new Vector3(-0.20f, -0.045f, 0.012f);
                    habitatIcon.transform.localPosition = new Vector3(0.20f, -0.045f, 0.012f);
                }

                return;
            }

            if (activeIcons.Count == 1)
            {
                activeIcons[0].transform.localPosition = Vector3.zero;
                return;
            }

            hungerIcon.transform.localPosition = new Vector3(-0.12f, 0f, 0f);
            habitatIcon.transform.localPosition = new Vector3(0.12f, 0f, 0f);
        }

        private GameObject CreateIcon(string objectName, Sprite sprite, int sortingOrder, HideFlags hideFlags)
        {
            var instance = new GameObject(objectName)
            {
                hideFlags = hideFlags
            };
            instance.transform.SetParent(indicatorRoot.transform, false);

            var renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            if (sprite != null)
            {
                var longestSide = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                instance.transform.localScale = Vector3.one * (IconWorldSize / Mathf.Max(0.001f, longestSide));
            }

            return instance;
        }
    }
}
