using System;
using UnityEngine;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private string manualFocusedTerm;
        private bool manualTermContext;
        public string ManualFocusedTerm => manualFocusedTerm;
        public void ShowTermExplanation(string id,Vector2 pointer)
        {
            if(!ConstructionGlossary.TryGet(id,out var term))return;
            ShowTooltip(null,L(term.English,term.Chinese),
                L(term.EnglishMeaning,term.ChineseMeaning)+L("\nClick to open this manual entry.","\n点击定位手册条目。"),pointer,TooltipLocation.FollowPointer);
            if(QuickGuideOpen && tooltipRect!=null)
            {
                var point=tooltipRect.anchoredPosition;
                point.x=568f-191f-TooltipWidth*0.5f-16f;
                tooltipRect.anchoredPosition=point;
            }
        }
        public void HideTermExplanation()=>HideTooltip();
        public bool OpenManualTerm(string id)
        {
            if(!ConstructionGlossary.TryGet(id,out _) || run==null || run.IsFinished || !IsBoardCaseOpen || PauseOpen)return false;
            manualTermContext=true;manualFocusedTerm=id;quickGuideBodyReady=false;
            if(!QuickGuideOpen)ToggleQuickGuide();else RefreshQuickGuide();
            if(!QuickGuideOpen)return false;
            HideTooltip();return ScrollManualToTerm(id);
        }
        private bool ScrollManualToTerm(string id)
        {
            Canvas.ForceUpdateCanvases();quickGuideBody.ForceMeshUpdate(true);
            var info=quickGuideBody.textInfo;
            for(var index=0;index<info.linkCount;index++)
            {
                var link=info.linkInfo[index];if(link.GetLinkID()!= "entry:"+id)continue;
                var glyph=info.characterInfo[link.linkTextfirstCharacterIndex];
                var overflow=Mathf.Max(0,quickGuideBody.rectTransform.rect.height-quickGuideScroll.viewport.rect.height);
                var offset=Mathf.Clamp(quickGuideBody.rectTransform.rect.yMax-glyph.ascender-12f,0,overflow);
                quickGuideScroll.StopMovement();quickGuideScroll.verticalNormalizedPosition=overflow>0?1f-offset/overflow:1f;
                Canvas.ForceUpdateCanvases();return true;
            }
            return false;
        }
    }
}
