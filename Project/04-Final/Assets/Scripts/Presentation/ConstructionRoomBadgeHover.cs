using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UrbanWildlifeRooms.Presentation
{
    // Hover-only handler: clicks keep bubbling to the room button underneath.
    public sealed class ConstructionRoomBadgeHover : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        private Action onEnter;
        private Action onExit;

        public void Initialize(Action enter, Action exit)
        {
            onEnter = enter;
            onExit = exit;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            onEnter?.Invoke();

        public void OnPointerExit(PointerEventData eventData) =>
            onExit?.Invoke();
    }
}
