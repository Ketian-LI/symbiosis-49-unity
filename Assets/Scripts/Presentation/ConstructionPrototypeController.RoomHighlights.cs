using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private string selectedRoomReviewId, selectedRoomReviewStamp, selectedRoomReviewRunId;
        private ConstructionRoomRelations selectedRoomRelations;
        private readonly Image[,] selectedRoomCoverage = new Image[7,7];
        private readonly RectTransform[,,] selectedRoomFrames = new RectTransform[7,7,4];
        private readonly List<RectTransform> selectedRoomActors = new();
        private RectTransform selectedRoomPanel, selectedRoomActorLayer;
        private Text selectedRoomHeading, selectedRoomDetails, selectedRoomCloseLabel;
        private bool selectedRoomOwnsGuide;
        private bool selectedRoomWasVisible;
        private static readonly int[] RoomReviewDrawOrder={3,2,0,1};
        public string SelectedRoomReviewId => selectedRoomReviewId;

        private void BuildRoomHighlights(Transform canvas)
        {
            selectedRoomActorLayer=NewRect("Selected room actors",canvas,Vector2.zero,Vector2.zero);
            Stretch(selectedRoomActorLayer);
            var group=selectedRoomActorLayer.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts=false;group.interactable=false;
            var paper=Panel("Selected room relationships",guidePanel.transform.parent,
                new Vector2(CaseLeafCenter-10f,-58f),new Vector2(288f,310f),ReadingPaper);
            paper.raycastTarget=false;selectedRoomPanel=paper.rectTransform;
            Panel("Selection heading rule",paper.transform,new Vector2(0,151),new Vector2(272,1),HandQuietBrass).raycastTarget=false;
            selectedRoomHeading=TextAt("Selected room relationship heading",paper.transform,new Vector2(0,126),new Vector2(264,38),"",22,true,Ink,TextAnchor.MiddleLeft);
            selectedRoomDetails=TextAt("Selected room relationship explanation",paper.transform,new Vector2(0,3),new Vector2(264,204),"",16,false,Ink,TextAnchor.UpperLeft);
            selectedRoomDetails.lineSpacing=1.2f;selectedRoomDetails.raycastTarget=false;
            var close=ButtonAt("Clear selected room highlights",paper.transform,new Vector2(0,-131),new Vector2(264,30)," ",16,Ink,Paper,
                ()=>ClearRoomHighlightSelection(),false);
            selectedRoomCloseLabel=close.GetComponentInChildren<Text>();
            selectedRoomPanel.gameObject.SetActive(false);
        }

        public bool SelectRoomForReview(int column,int row)
        {
            var tile=run?.At(column,row);
            if(tile==null || !IsBoardCaseOpen || ScoreTraceBlocked || scoreMotionAnimation!=null) return false;
            CloseScoreIndex();
            selectedRoomReviewId=selectedRoomReviewId==tile.id?null:tile.id;
            selectedRoomReviewRunId=run.RunId;
            selectedRoomReviewStamp=null;
            DrawSelectedRoomHighlights();
            return true;
        }

        public bool ClearRoomHighlightSelection()
        {
            if(selectedRoomReviewId==null) return false;
            selectedRoomReviewId=null;selectedRoomReviewStamp=null;
            DrawSelectedRoomHighlights();return true;
        }

        public bool TryCancelRoomHighlights() => !ScoreTraceBlocked && !scoreTraceReviewOpen &&
            scoreMotionAnimation==null && ClearRoomHighlightSelection();

        private RectTransform SelectedRoomFrame(int x,int y,int layer)
        {
            if(selectedRoomFrames[x,y,layer]!=null)return selectedRoomFrames[x,y,layer];
            var size=layer==0?98f:layer==1?92f:layer==2?84f:88f;
            var frame=TraceBracket(layer switch {0=>"Selected room anchor",1=>"Selected room penalty",2=>"Selected room benefit",_=>"Selected room settled relation"},cellBackgrounds[x,y].transform,size);
            selectedRoomFrames[x,y,layer]=frame;return frame;
        }

        private void DrawSelectedRoomHighlights()
        {
            if(selectedRoomPanel==null || run==null)return;
            if(selectedRoomReviewId!=null && selectedRoomReviewRunId!=run.RunId)
                selectedRoomReviewId=null;
            var stamp=$"{run.RunId}:{run.CurrentDay}:{run.BuiltCount}:{run.LastScoreDay?.day}:{selectedRoomReviewId}:{Chinese}";
            if(selectedRoomReviewStamp!=stamp)
            {
                selectedRoomRelations=ConstructionRoomRelations.For(run,selectedRoomReviewId);
                if(selectedRoomRelations.Selected==null)selectedRoomReviewId=null;
                selectedRoomReviewStamp=stamp;
                if(selectedRoomRelations.Selected!=null) WriteSelectedRoomExplanation();
            }
            var visible=selectedRoomReviewId!=null && !ScoreTraceBlocked && !scoreTraceReviewOpen && !scoreTraceAutomatic && scoreMotionAnimation==null;
            selectedRoomPanel.gameObject.SetActive(visible);
            if(!visible && !selectedRoomWasVisible && !selectedRoomOwnsGuide)return;
            if(visible)
            {
                if(guidePanel.gameObject.activeSelf){guidePanel.gameObject.SetActive(false);selectedRoomOwnsGuide=true;}
                if(readingChangeRoot!=null)readingChangeRoot.gameObject.SetActive(false);
            }
            else if(selectedRoomOwnsGuide)
            {
                guidePanel.gameObject.SetActive(true);selectedRoomOwnsGuide=false;
                if(run.UsesFillHand && !scoreTraceReviewOpen)RefreshReadingSummary();
            }
            void Paint(RectTransform frame,Color color)
            {foreach(var edge in scoreTraceEdges[frame])edge.color=color;frame.SetAsLastSibling();}
            for(var y=0;y<7;y++)for(var x=0;x<7;x++)
            {
                var tile=run.At(viewMinColumn+x,viewMinRow+y);
                var covered=visible && selectedRoomRelations.Coverage.Contains((viewMinColumn+x,viewMinRow+y));
                if(covered && selectedRoomCoverage[x,y]==null)
                {
                    var wash=Panel("Selected waste coverage",cellBackgrounds[x,y].transform,Vector2.zero,new Vector2(94,94),new Color(0.20f,0.52f,0.70f,0.24f));
                    wash.raycastTarget=false;selectedRoomCoverage[x,y]=wash;
                    var blue=new Color(0.30f,0.70f,0.88f,0.90f);
                    foreach(var side in new[]{-1f,1f})
                    {
                        Panel("Cleanup boundary",wash.transform,new Vector2(0,side*46),new Vector2(92,1.5f),blue).raycastTarget=false;
                        Panel("Cleanup boundary",wash.transform,new Vector2(side*46,0),new Vector2(1.5f,92),blue).raycastTarget=false;
                    }
                }
                if(selectedRoomCoverage[x,y]!=null)
                {selectedRoomCoverage[x,y].gameObject.SetActive(covered);if(covered)selectedRoomCoverage[x,y].transform.SetAsLastSibling();}
                // Draw the warning last: it stays visible on top of cleanup tint
                // and any settled positive relation involving the same room.
                foreach(var layer in RoomReviewDrawOrder)
                {
                    var shown=visible && tile!=null && (layer switch
                    {
                        0=>tile.id==selectedRoomReviewId,
                        1=>selectedRoomRelations.Penalties.ContainsKey(tile.id),
                        2=>selectedRoomRelations.Benefits.ContainsKey(tile.id),
                        _=>tile.id!=selectedRoomReviewId && selectedRoomRelations.SettledRooms.Contains(tile.id)
                    });
                    var frame=shown?SelectedRoomFrame(x,y,layer):selectedRoomFrames[x,y,layer];
                    if(frame==null)continue;frame.gameObject.SetActive(shown);
                    if(shown)Paint(frame,layer switch {0=>HandBrass,1=>new Color(0.81f,0.25f,0.16f),2=>ReadingGreen,_=>ReadingBlue});
                }
            }
            var count=0;
            if(visible)
                foreach(var actor in selectedRoomRelations.SettledActors.Select(a=>worldView?.ScoreTraceActor(a)).Where(a=>a!=null).Distinct())
                {
                    if(count==selectedRoomActors.Count)selectedRoomActors.Add(TraceBracket("Selected room related actor",selectedRoomActorLayer,30));
                    var marker=selectedRoomActors[count++];var screen=worldView.BoardCamera.WorldToScreenPoint(actor.position);
                    var canvas=selectedRoomActorLayer.GetComponentInParent<Canvas>();
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(selectedRoomActorLayer,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var point);
                    marker.anchoredPosition=point;marker.gameObject.SetActive(screen.z>0 && worldView.BoardCamera.pixelRect.Contains(screen));Paint(marker,ReadingBlue);
                }
            for(var i=count;i<selectedRoomActors.Count;i++)selectedRoomActors[i].gameObject.SetActive(false);
            selectedRoomWasVisible=visible;
        }

        private void WriteSelectedRoomExplanation()
        {
            var relation=selectedRoomRelations;
            selectedRoomHeading.text=CategoryLabel(relation.Selected.category)+L(" · selected"," · 已选中");
            var rows=new List<string>();
            if(relation.Coverage.Count>0)rows.Add(L("<color=#24506B>Blue tiles: cleanup range, including connected streets.</color>","<color=#24506B>蓝格：清运范围，含连续街道接通的位置。</color>"));
            if(relation.Penalties.Count>0)
            {
                var cause=relation.Selected.category==UrbanWildlifeRooms.Data.ConstructionCategory.Waste
                    ? L("homes beside waste",run.PlacementRulesVersion>=4?"共边住宅":"邻近住宅")
                    : L("poor pairings","不良搭配");
                rows.Add(L($"<color=#984A35>Red frames: {cause} ×{relation.Penalties.Count}; {relation.Penalties.Values.Sum()} build points.</color>",$"<color=#984A35>红框：{cause} ×{relation.Penalties.Count}，建造搭配 {relation.Penalties.Values.Sum()} 分。</color>"));
            }
            if(relation.Benefits.Count>0)rows.Add(L($"Green frames: favourable pairings ×{relation.Benefits.Count}.",$"绿框：正向搭配 ×{relation.Benefits.Count}。"));
            if(relation.SettledRooms.Count>0)rows.Add(L($"Blue frames: related rooms/actors in day {relation.SettledDay}'s settled record.",$"蓝框：第 {relation.SettledDay} 天已结算的相关房间与角色。"));
            if(rows.Count==0)rows.Add(L("No scored relation recorded for this room yet.","该房间暂未记录得分关联。"));
            if(relation.Penalties.Count>0)rows.Add(L("Pairing points currently apply when built; this view does not deduct again.","当前搭配分在建造时结算；查看不会再次扣分。"));
            selectedRoomDetails.text=string.Join("\n",rows);
            selectedRoomCloseLabel.text=L("Cancel selection · Esc","取消选中 · Esc");
        }
    }
}
