using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;
namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private RectTransform referenceRoot,referenceDiagrams;
        private Text referenceHeading,referenceSubtitle,referenceCloseLabel,referencePageLabel,referenceExamplesLabel,referenceJournalLabel;
        private Button referencePrevious,referenceNext;
        private ScrollRect referenceScroll;
        private TextMeshProUGUI referenceJournal;
        private bool referenceIsJournal;
        private int referencePage;
        public bool ReferenceOpen=>referenceRoot!=null && referenceRoot.gameObject.activeSelf;
        private void BuildReferences(Transform canvas)
        {
            var examples=ButtonAt("Open building examples",canvas,new Vector2(-650,409),new Vector2(120,36)," ",16,Ink,Paper,()=>ToggleReference(false),false);
            var journal=ButtonAt("Open independent journal",canvas,new Vector2(-516,409),new Vector2(120,36)," ",16,Ink,Paper,()=>ToggleReference(true),false);
            referenceExamplesLabel=examples.GetComponentInChildren<Text>();referenceJournalLabel=journal.GetComponentInChildren<Text>();
            caseHeaderGroups.Add(examples.gameObject.AddComponent<CanvasGroup>());caseHeaderGroups.Add(journal.gameObject.AddComponent<CanvasGroup>());
            referenceRoot=NewRect("Rules and journal reference",canvas,Vector2.zero,new Vector2(1600,900));Stretch(referenceRoot);
            var shield=Panel("Reference input shield",referenceRoot,Vector2.zero,new Vector2(1600,900),new Color(0,0,0,.35f));Stretch(shield.rectTransform);
            var frame=Panel("Reference frame",referenceRoot,Vector2.zero,new Vector2(858,662),Ink);
            Panel("Reference brass",frame.transform,Vector2.zero,new Vector2(848,652),HandQuietBrass).raycastTarget=false;
            var face=Panel("Reference paper",frame.transform,Vector2.zero,new Vector2(840,644),Color.white);
            face.sprite=ConstructionHandArtCatalog.GetCardPaper();face.type=Image.Type.Simple;face.raycastTarget=false;
            referenceHeading=TextAt("Reference heading",frame.transform,new Vector2(0,278),new Vector2(780,42),"",26,true,Ink,TextAnchor.MiddleLeft);
            referenceSubtitle=TextAt("Reference subtitle",frame.transform,new Vector2(0,238),new Vector2(780,34),"",17,false,Ink,TextAnchor.MiddleLeft);
            Panel("Reference heading rule",frame.transform,new Vector2(0,214),new Vector2(780,1),HandQuietBrass).raycastTarget=false;
            referenceDiagrams=NewRect("Building diagrams",frame.transform,new Vector2(0,-14),new Vector2(790,440));
            var viewport=NewRect("Journal viewport",frame.transform,new Vector2(0,-20),new Vector2(780,458));
            viewport.gameObject.AddComponent<Image>().color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
            var content=NewRect("Independent journal entries",viewport,Vector2.zero,new Vector2(780,458));
            content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,458);
            referenceJournal=content.gameObject.AddComponent<TextMeshProUGUI>();referenceJournal.fontSize=18;referenceJournal.color=Ink;referenceJournal.lineSpacing=6;
            referenceJournal.richText=true;referenceJournal.textWrappingMode=TextWrappingModes.Normal;referenceJournal.overflowMode=TextOverflowModes.Overflow;
            referenceJournal.gameObject.AddComponent<ConstructionTermLinks>().Owner=this;
            referenceScroll=viewport.gameObject.AddComponent<ScrollRect>();referenceScroll.viewport=viewport;referenceScroll.content=content;referenceScroll.horizontal=false;referenceScroll.inertia=false;referenceScroll.scrollSensitivity=30;
            referencePrevious=ButtonAt("Previous building example",frame.transform,new Vector2(-274,-286),new Vector2(180,36),"<",20,Paper,Ink,()=>ChangeReferencePage(-1),false);
            referenceNext=ButtonAt("Next building example",frame.transform,new Vector2(274,-286),new Vector2(180,36),">",20,Paper,Ink,()=>ChangeReferencePage(1),false);
            referencePageLabel=TextAt("Example page",frame.transform,new Vector2(0,-248),new Vector2(160,28),"",17,false,Ink,TextAnchor.MiddleCenter);
            referenceCloseLabel=ButtonAt("Close reference",frame.transform,new Vector2(0,-286),new Vector2(210,36)," ",17,Ink,Paper,CloseReference,false).GetComponentInChildren<Text>();
            referenceRoot.gameObject.SetActive(false);RefreshReferenceLabels();
        }
        private void RefreshReferenceLabels()
        {
            if(referenceRoot==null)return;
            referenceExamplesLabel.text=L("Examples · P","图例 · P");referenceJournalLabel.text=L("Journal · I","日志 · I");
            referenceCloseLabel.text=L("Close · Esc","关闭 · Esc");
        }
        public bool ToggleReference(bool journal)
        {
            if(ReferenceOpen && referenceIsJournal==journal){CloseReference();return true;}
            if(run==null || !IsBoardCaseOpen || PauseOpen || hasPendingPlacement || scoreMotionAnimation!=null ||
                finalResultPanel!=null && finalResultPanel.activeSelf ||
                specialTaskSelectionRoot!=null && specialTaskSelectionRoot.gameObject.activeSelf)return false;
            if(QuickGuideOpen)ToggleQuickGuide();CloseDeckInventory();CloseScoreIndex();HideTooltip();
            referenceIsJournal=journal;referenceRoot.gameObject.SetActive(true);referenceRoot.SetAsLastSibling();RefreshReference();return true;
        }
        public void CloseReference(){if(referenceRoot!=null)referenceRoot.gameObject.SetActive(false);HideTooltip();}
        private void ChangeReferencePage(int direction){referencePage=(referencePage+direction+3)%3;RefreshReference();}
        private void RefreshReference()
        {
            RefreshReferenceLabels();referenceDiagrams.gameObject.SetActive(!referenceIsJournal);referenceScroll.gameObject.SetActive(referenceIsJournal);
            referencePrevious.gameObject.SetActive(!referenceIsJournal);referenceNext.gameObject.SetActive(!referenceIsJournal);referencePageLabel.gameObject.SetActive(!referenceIsJournal);
            referenceHeading.text=referenceIsJournal?L("Neighbourhood journal","街区日志"):L("Rules and building examples","规则与建造图例");
            referenceSubtitle.text=referenceIsJournal?L("Recorded settlements · newest first · I to close","已结算记录 · 最新在前 · I 收起"):L("P to close · arrows below change the example","P 收起 · 用下方箭头切换图例");
            if(referenceIsJournal)
            {
                referenceJournal.font=Resources.Load<TMP_FontAsset>(Chinese?"Fonts/TMP/NotoSansSC-Regular SDF":"Fonts/TMP/Nunito-Regular SDF");
                referenceJournal.text=ConstructionGlossary.Linkify(JournalText());
                referenceJournal.rectTransform.sizeDelta=new Vector2(0,Mathf.Max(458,referenceJournal.GetPreferredValues(referenceJournal.text,780,0).y+20));
                Canvas.ForceUpdateCanvases();referenceScroll.verticalNormalizedPosition=1;return;
            }
            foreach(Transform child in referenceDiagrams){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            referencePageLabel.text=$"{referencePage+1} / 3";
            if(referencePage==0)DrawCommuteExample();else if(referencePage==1)DrawWasteExample();else DrawUpgradeExamples();
        }
        private Text ReferenceCopy(string name,string en,string zh,Vector2 position,Vector2 size,int font=18,bool bold=false)
        {var text=TextAt(name,referenceDiagrams,position,size,L(en,zh),font,bold,Ink,TextAnchor.MiddleLeft);text.raycastTarget=false;return text;}
        private void ExampleTile(ConstructionCategory category,Vector2 position,string en,string zh,Color? frame=null)
        {
            var rim=Panel("Example tile "+category,referenceDiagrams,position,new Vector2(74,74),frame??HandQuietBrass);rim.raycastTarget=false;
            var art=Panel("Example room art",rim.transform,Vector2.zero,new Vector2(68,68),Color.white);art.sprite=ConstructionHandArtCatalog.GetRoom(category,GreenPlanting.Meadow);art.preserveAspect=true;art.raycastTarget=false;
            if(string.IsNullOrWhiteSpace(en) && string.IsNullOrWhiteSpace(zh))return;
            var label=TextAt("Example room label",rim.transform,new Vector2(0,-53),new Vector2(135,32),L(en,zh),17,true,Ink,TextAnchor.MiddleCenter);label.raycastTarget=false;
        }
        private void DrawCommuteExample()
        {
            ReferenceCopy("Example explanation","1. Connect reachable jobs and meal seats; residents are assigned automatically.","1. 让住宅接通岗位与餐位；系统自动规划通勤。",new Vector2(0,172),new Vector2(760,64),20,true);
            var types=new[]{ConstructionCategory.Residence,ConstructionCategory.Workshop,ConstructionCategory.Restaurant,ConstructionCategory.Residence};
            var en=new[]{"Home","Work","Meal","Return home"};var zh=new[]{"住宅出发","上班","用餐","回到住宅"};
            for(var i=0;i<4;i++){ExampleTile(types[i],new Vector2(-285+190*i,85),en[i],zh[i]);if(i<3)ReferenceCopy("Route arrow","→","→",new Vector2(-211+190*i,85),new Vector2(45,40),25,true);}
            ReferenceCopy("Example explanation","2. Shared edges connect. Diagonal contact and empty gaps do not.","2. 上下左右共边才连通；斜角或空格会断路。",new Vector2(0,-20),new Vector2(760,64),20,true);
            ExampleTile(ConstructionCategory.Residence,new Vector2(-285,-110),"Home","住宅");ExampleTile(ConstructionCategory.Street,new Vector2(-190,-110),"Street","街道");ExampleTile(ConstructionCategory.Workshop,new Vector2(-95,-110),"Job","岗位");
            ReferenceCopy("Good route","✓ connected","✓ 共边连通",new Vector2(-200,-194),new Vector2(300,30));
            ExampleTile(ConstructionCategory.Residence,new Vector2(150,-85),"Home","住宅");ExampleTile(ConstructionCategory.Workshop,new Vector2(265,-130),"Job","岗位",ReadingWarning);
            ReferenceCopy("Broken route","× diagonal is disconnected","× 斜角不连通",new Vector2(230,-216),new Vector2(300,32));
        }
        private void DrawWasteExample()
        {
            ReferenceCopy("Example explanation","Cleanup coverage and disturbance are different rules.","清运覆盖与邻接干扰是两条不同规则。",new Vector2(0,174),new Vector2(760,46),21,true);
            for(var y=0;y<3;y++)for(var x=0;x<3;x++)
            {var cell=Panel("Coverage diagram cell",referenceDiagrams,new Vector2(-280+x*82,85-y*82),new Vector2(77,77),new Color(.15f,.42f,.72f,.48f));cell.raycastTarget=false;}
            ExampleTile(ConstructionCategory.Waste,new Vector2(-198,3),"","");ExampleTile(ConstructionCategory.Residence,new Vector2(-116,3),"","",ReadingWarning);
            var points=-ConstructionPlacementScore.PairPoints(ConstructionCategory.Waste,ConstructionCategory.Residence,run.PlacementRulesVersion);
            ReferenceCopy("Example explanation","Blue: the eight surrounding cells can be cleared.\nRed: the shared-edge home loses "+points+" points at every confirmed settlement.\nDiagonal homes are covered but do not pay this penalty.","蓝格：周围八格在清运范围内。\n红框：共边住宅每次建造结算扣 "+points+" 分。\n斜角住宅可被清运，但不扣这项分。",new Vector2(165,24),new Vector2(400,204),19);
            ReferenceCopy("Example explanation","Connect waste via streets to serve homes without a bad shared edge. Viewing the board never deducts points.","清运可沿连续共边街道延伸；用街道接通垃圾间与住宅，避免直接共边。查看、选牌不扣分。",new Vector2(0,-173),new Vector2(760,76),18);
        }
        private void DrawUpgradeExamples()
        {
            referenceSubtitle.text=L("Habitat level is not maturity; new green grows for two settlements.","连片等级与成熟不同；新绿地生长 2 次结算后成熟。");
            var names=new[]{("2 linked plots → L2; 3+ plots and 2 plant types → L3","2 格共边绿地 → 2 级；至少 3 格、2 种植物 → 3 级",ConstructionCategory.Green,ConstructionCategory.Green),
                ("Home beside two connected green plots → garden","住宅旁接两格连片绿地 → 花园住宅",ConstructionCategory.Residence,ConstructionCategory.Green),
                ("Restaurant beside a square → three meal seats","餐厅贴广场 → 露台餐厅，3 个餐位",ConstructionCategory.Restaurant,ConstructionCategory.Square),
                ("Green on opposite sides of a street → half traffic risk","街道相对两侧有绿地 → 慢行街，风险减半",ConstructionCategory.Street,ConstructionCategory.Green)};
            for(var i=0;i<names.Length;i++)
            {var y=150-i*110;ExampleTile(names[i].Item3,new Vector2(-310,y),""," ");ExampleTile(names[i].Item4,new Vector2(-215,y),""," ");ReferenceCopy("Example explanation",names[i].Item1,names[i].Item2,new Vector2(110,y),new Vector2(490,83),19,true);}
        }
        private string JournalText()
        {
            var save=run.Export();var days=save.score?.days;
            if(days==null || days.Count==0)return L("No settled changes yet. Confirm a build to begin the journal.","还没有结算记录。确认一次建造后，变化会记录在这里。");
            var text=new StringBuilder();
            foreach(var day in days.OrderByDescending(d=>d.day))
            {
                text.Append("<b>").Append(L("Settlement ","第 ")).Append(day.day).Append(L(""," 次结算")).Append("</b>\n");
                var placed=run.BuiltTiles.FirstOrDefault(t=>t.builtDay==day.day);
                if(placed!=null)text.Append(L("Built: ","建造：")).Append(CategoryLabel(placed.category)).Append($" ({placed.column+1},{placed.row+1})\n");
                if(placed!=null)
                    foreach(var other in run.BuiltTiles.Where(t=>t.buildIndex<placed.buildIndex && ConstructionBoardModel.AreCardinalNeighbours(t.column,t.row,placed.column,placed.row)))
                    {
                        var points=ConstructionPlacementScore.PairPoints(placed.category,other.category,run.PlacementRulesVersion);
                        if(points<0)text.Append("<color=#984A35>").Append(L("New bad edge: ","新增不良共边：")).Append(CategoryLabel(placed.category)).Append(" + ").Append(CategoryLabel(other.category)).Append($" {points}\n</color>");
                    }
                foreach(var penalty in run.RecurringPenalties.Where(p=>p.day==day.day))
                {
                    var a=run.BuiltTiles.First(t=>t.id==penalty.firstTileId);var b=run.BuiltTiles.First(t=>t.id==penalty.secondTileId);
                    text.Append("<color=#984A35>").Append(L("Recurring: ","持续扣分：")).Append(CategoryLabel(a.category)).Append($" ({a.column+1},{a.row+1}) + ").Append(CategoryLabel(b.category)).Append($" ({b.column+1},{b.row+1})  {penalty.points}\n</color>");
                }
                if(placed!=null && run.UsesNeighborUpgrades)
                    foreach(var upgrade in ConstructionPlacementScore.NewlyUpgradedTiles(placed,run.BuiltTiles.Where(t=>t.buildIndex<=placed.buildIndex).ToArray()))
                        text.Append(L("Upgraded: ","升级：")).Append(CategoryLabel(upgrade.category)).Append('\n');
                text.Append(L($"Wildlife meals {day.animalMeals} · completed commutes {day.completedWorkCycles}",$"动物进食 {day.animalMeals} 份 · 完成通勤 {day.completedWorkCycles} 次")).Append("\n\n");
            }
            return text.ToString();
        }
    }
}
