using UnityEngine;
using UnityEngine.EventSystems;

namespace UrbanWildlifeRooms.UI
{
    public sealed class EcologicalIndicatorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private GameObject tooltip;

        public void Initialize(GameObject tooltipObject)
        {
            tooltip = tooltipObject;
            tooltip?.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            tooltip?.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip?.SetActive(false);
        }
    }
}
