using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private RectTransform deckInventoryRoot;
        private Text deckInventoryHeading, deckInventorySummary, deckInventoryColumns, deckInventoryNote, deckInventoryHint;
        private readonly Text[] deckInventoryNames=new Text[8], deckInventoryCounts=new Text[8];
        private string deckInventoryRunId;
        private static readonly ConstructionCategory[] DeckInventoryCategories={
            ConstructionCategory.Residence,ConstructionCategory.Workshop,ConstructionCategory.Restaurant,ConstructionCategory.Supermarket,
            ConstructionCategory.Street,ConstructionCategory.Square,ConstructionCategory.Waste,ConstructionCategory.Green};
        public bool DeckInventoryOpen=>deckInventoryRoot!=null && deckInventoryRoot.gameObject.activeSelf;

        private void BuildDeckInventory(Transform canvas)
        {
            if(run?.UsesFillHand!=true)return;
            var deckImage=fillHandDeckRect.GetComponent<Image>();deckImage.raycastTarget=true;
            var open=fillHandDeckRect.gameObject.AddComponent<Button>();open.targetGraphic=deckImage;
            open.onClick.AddListener(()=>ToggleDeckInventory());
            deckInventoryHint=TextAt("View remaining cards",fillHandDeckRect, new Vector2(0,-41),new Vector2(126,30),"",18,false,HandInk,TextAnchor.MiddleCenter);
            deckInventoryHint.raycastTarget=false;
            deckInventoryRoot=NewRect("Deck contents",canvas,Vector2.zero,new Vector2(1600,900));Stretch(deckInventoryRoot);
            var shield=Panel("Deck contents outside click",deckInventoryRoot,Vector2.zero,new Vector2(1600,900),Color.clear);Stretch(shield.rectTransform);
            var outside=shield.gameObject.AddComponent<Button>();outside.targetGraphic=shield;outside.onClick.AddListener(CloseDeckInventory);
            var frame=Panel("Deck inventory frame",deckInventoryRoot,new Vector2(0,-144),new Vector2(600,254),Ink);
            var rim=Panel("Deck inventory brass",frame.transform,Vector2.zero,new Vector2(597,251),HandQuietBrass);
            rim.raycastTarget=false;
            var face=Panel("Deck inventory paper",frame.transform,Vector2.zero,new Vector2(594,248),Color.white);
            face.sprite=ConstructionHandArtCatalog.GetCardPaper();face.type=Image.Type.Sliced;
            face.pixelsPerUnitMultiplier=face.sprite.rect.height/248f;
            face.raycastTarget=true;
            deckInventoryHeading=TextAt("Deck inventory heading",frame.transform,new Vector2(-180,103),new Vector2(190,34),"",23,true,Ink,TextAnchor.MiddleLeft);
            deckInventorySummary=TextAt("Deck inventory summary",frame.transform,new Vector2(87,103),new Vector2(320,30),"",16,false,Ink,TextAnchor.MiddleRight);
            var close=ButtonAt("Close deck contents",frame.transform,new Vector2(276,103),new Vector2(28,28),"×",18,Paper,Ink,CloseDeckInventory,false);
            Panel("Deck inventory rule",frame.transform,new Vector2(0,81),new Vector2(556,1),HandQuietBrass).raycastTarget=false;
            deckInventoryColumns=TextAt("Deck inventory columns",frame.transform,new Vector2(184,64),new Vector2(182,26),"",16,false,Ink,TextAnchor.MiddleRight);
            for(var index=0;index<8;index++)
            {
                var row=NewRect("Deck inventory category "+index,frame.transform,new Vector2(-210+(index%4)*140,20-(index/4)*70),new Vector2(140,60));
                var art=Panel("Deck category art",row,new Vector2(-50,0),new Vector2(38,42),Color.white);
                art.sprite=ConstructionHandArtCatalog.GetRoom(DeckInventoryCategories[index],GreenPlanting.Meadow);
                art.preserveAspect=true;art.raycastTarget=false;
                deckInventoryNames[index]=TextAt("Deck category name",row,new Vector2(23,14),new Vector2(96,26),"",16,true,Ink,TextAnchor.MiddleLeft);
                deckInventoryCounts[index]=TextAt("Deck category remaining",row,new Vector2(23,-13),new Vector2(96,28),"",19,false,Ink,TextAnchor.MiddleLeft);
            }
            Panel("Deck inventory foot rule",frame.transform,new Vector2(0,-84),new Vector2(556,1),HandQuietBrass).raycastTarget=false;
            deckInventoryNote=TextAt("Deck inventory note",frame.transform,new Vector2(0,-105),new Vector2(556,38),"",16,false,Ink,TextAnchor.MiddleCenter);
            foreach(var label in frame.GetComponentsInChildren<Text>())label.raycastTarget=false;
            deckInventoryRoot.gameObject.SetActive(false);RefreshDeckInventory();
        }

        public bool ToggleDeckInventory()
        {
            if(DeckInventoryOpen){CloseDeckInventory();return true;}
            if(run?.UsesFillHand!=true || run.IsFinished || ScoreTraceBlocked || scoreMotionAnimation!=null)return false;
            CloseScoreIndex();ClearRoomHighlightSelection();
            deckInventoryRunId=run.RunId;
            deckInventoryRoot.SetAsLastSibling();deckInventoryRoot.gameObject.SetActive(true);
            RefreshDeckInventory();RefreshFillRightHandPreview();return true;
        }

        public void CloseDeckInventory()
        {
            if(!DeckInventoryOpen)return;
            deckInventoryRoot.gameObject.SetActive(false);
            if(run?.UsesFillHand==true)RefreshFillRightHandPreview();
        }

        private void RefreshDeckInventory()
        {
            if(deckInventoryRoot==null || run==null)return;
            if(DeckInventoryOpen && (deckInventoryRunId!=run.RunId || run.IsFinished))
            {CloseDeckInventory();return;}
            deckInventoryHint.text=L("View cards","查看余牌");
            deckInventoryHeading.text=L("Remaining cards","余牌一览");
            deckInventoryColumns.text=L("Left / initial","剩余 / 初始");
            deckInventorySummary.text=run.UsesFiniteFillDeck
                ? L($"Deck {run.FillDeckCardsRemaining} · Held {run.FillDeckHeldCards} · Built {run.BuiltCount}",
                    $"牌堆 {run.FillDeckCardsRemaining} · 手中 {run.FillDeckHeldCards} · 已建 {run.BuiltCount}")
                : L("Random refills · no fixed supply","随机补牌 · 无固定总量");
            for(var index=0;index<8;index++)
            {
                var category=DeckInventoryCategories[index];
                deckInventoryNames[index].text=category==ConstructionCategory.Supermarket
                    ? L("Market","超市") : CategoryLabel(category);
                deckInventoryCounts[index].text=run.UsesFiniteFillDeck
                    ? $"{run.FillDeckRemainingFor(category)} / {ConstructionRunModel.DigitalFillCardSupply(category)}"
                    : L("Random","随机");
            }
            deckInventoryNote.text=run.IsIntroPhase
                ? L("Tutorial queue is separate; green types share one supply.","教程按固定顺序发牌；绿地类型共用数量。")
                : L("Green types share one supply. Swapped cards return to the deck.","绿地类型共用数量；换下的牌回到牌堆。");
        }
    }
}
