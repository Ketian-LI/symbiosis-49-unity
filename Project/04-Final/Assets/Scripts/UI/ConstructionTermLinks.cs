using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.UI
{
    // Only linked glyphs take pointer input. Whitespace and ordinary copy let
    // the underlying card/scroll view keep their normal click/drag behaviour.
    public sealed class ConstructionTermLinks : MonoBehaviour, IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ICanvasRaycastFilter
    {
        private TMP_Text label;
        private string hovered;
        public ConstructionPrototypeController Owner;
        private void Awake()=>label=GetComponent<TMP_Text>();
        public string TermAt(Vector2 point,Camera camera=null)
        {
            if(label==null || !label.gameObject.activeInHierarchy)return null;
            var index=TMP_TextUtilities.FindIntersectingLink(label,point,camera);
            if(index<0)return null;
            var id=label.textInfo.linkInfo[index].GetLinkID();
            return id.StartsWith("term:")?id.Substring(5):null;
        }
        public bool IsRaycastLocationValid(Vector2 point,Camera camera)=>TermAt(point,camera)!=null;
        public void OnPointerEnter(PointerEventData e)=>OnPointerMove(e);
        public void OnPointerMove(PointerEventData e)
        {
            var term=TermAt(e.position,e.enterEventCamera);
            if(term==hovered)return;
            if(hovered!=null)Owner?.HideTermExplanation();
            hovered=term;if(term!=null)Owner?.ShowTermExplanation(term,e.position);
        }
        public void OnPointerExit(PointerEventData e){if(hovered!=null)Owner?.HideTermExplanation();hovered=null;}
        public void OnPointerClick(PointerEventData e)
        {if(e.button==PointerEventData.InputButton.Left){var id=TermAt(e.position,e.pressEventCamera);if(id!=null)Owner?.OpenManualTerm(id);}}
        private void OnDisable(){if(hovered!=null)Owner?.HideTermExplanation();hovered=null;}
    }
}
