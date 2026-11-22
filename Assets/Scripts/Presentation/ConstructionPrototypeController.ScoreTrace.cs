using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private RectTransform scoreTracePanel, scoreTraceActorLayer;
        private Text scoreTraceHeading, scoreTraceTitle, scoreTraceDetail, scoreTraceLaunchLabel;
        private Text[] scoreTraceButtonLabels;
        private readonly RectTransform[,] scoreTraceRooms = new RectTransform[7,7];
        private readonly List<RectTransform> scoreTraceActorMarkers = new();
        private readonly Dictionary<RectTransform,Image[]> scoreTraceEdges = new();
        private readonly Dictionary<string,Color> scoreTraceRoomColors = new();
        private readonly Dictionary<Transform,Color> scoreTraceActorColors = new();
        private IReadOnlyList<ScoreTraceEntry> scoreTraceEntries = Array.Empty<ScoreTraceEntry>();
        private ScoreTraceEntry[] scoreTraceActive = Array.Empty<ScoreTraceEntry>();
        private string scoreTraceRunId;
        private int scoreTraceDay = -1, scoreTraceIndex;
        private bool scoreTraceReviewOpen, scoreTraceAutomatic;
        private Button scoreTraceLaunch;
        private RectTransform scoreBatchPanel;
        private Text scoreBatchLabel;
        public bool IsScoreIndexOpen => scoreTraceReviewOpen;
        public int ScoreIndexCount => scoreTraceEntries.Count;
        public int CurrentScoreIndex => scoreTraceIndex;

        private void BuildScoreTrace(Transform canvas)
        {
            var batch=Panel("Score batch",canvas,new Vector2(0,338),new Vector2(278,32),Ink);
            batch.raycastTarget=false;scoreBatchPanel=batch.rectTransform;
            scoreBatchLabel=TextAt("Score batch label",batch.transform,Vector2.zero,new Vector2(266,30),"",17,true,Paper,TextAnchor.MiddleCenter);
            scoreBatchLabel.raycastTarget=false;scoreBatchPanel.gameObject.SetActive(false);
            scoreTraceActorLayer = NewRect("Scoring actor highlights", canvas, Vector2.zero, Vector2.zero);
            Stretch(scoreTraceActorLayer);
            var group = scoreTraceActorLayer.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false;
            for (var y=0;y<7;y++) for(var x=0;x<7;x++)
            {
                scoreTraceRooms[x,y] = TraceBracket("Scoring room highlight", cellBackgrounds[x,y].transform, 92f);
                scoreTraceRooms[x,y].gameObject.SetActive(false);
            }
            var paper = Panel("Score index", guidePanel.transform.parent,
                new Vector2(CaseLeafCenter - 10f, -220f), new Vector2(288f,112f), ReadingPaper);
            scoreTracePanel = paper.rectTransform;
            paper.raycastTarget = false;
            scoreTraceHeading = TextAt("Score index heading", paper.transform, new Vector2(0,40), new Vector2(264,26), "",16,true,Ink,TextAnchor.MiddleLeft);
            scoreTraceTitle = TextAt("Score index entry", paper.transform, new Vector2(0,12), new Vector2(264,26), "",17,true,Ink,TextAnchor.MiddleLeft);
            scoreTraceDetail = TextAt("Score index targets", paper.transform,new Vector2(0,-14),new Vector2(264,26),"",16,false,Ink,TextAnchor.MiddleLeft);
            scoreTraceButtonLabels = new Text[3];
            for(var index=0;index<3;index++)
            {
                var action = index;
                var button = ButtonAt("Score index control " + index,paper.transform,
                    new Vector2(-91+index*91,-42),new Vector2(86,26)," ",16,Ink,Paper,
                    ()=> { if(action==2) CloseScoreIndex(); else StepScoreIndex(action==0 ? -1 : 1); },false);
                scoreTraceButtonLabels[index] = button.GetComponentInChildren<Text>();
            }
            scoreTraceLaunch = ButtonAt("Open score index", progressPanel.transform,
                run.UsesFillHand ? new Vector2(-12,-158) : new Vector2(104,88),
                run.UsesFillHand ? new Vector2(264,32) : new Vector2(84,28)," ",16,Paper,Ink,
                ()=>ToggleScoreIndex(),false);
            scoreTraceLaunchLabel = scoreTraceLaunch.GetComponentInChildren<Text>();
            var launchOutline=scoreTraceLaunch.gameObject.AddComponent<Outline>();
            launchOutline.effectColor=Ink;launchOutline.effectDistance=new Vector2(1,-1);
            scoreTracePanel.gameObject.SetActive(false);
        }

        private RectTransform TraceBracket(string name,Transform parent,float size)
        {
            var root = NewRect(name,parent,Vector2.zero,new Vector2(size,size));
            foreach(var x in new[]{-1f,1f}) foreach(var y in new[]{-1f,1f})
            {
                Panel("Bracket horizontal",root,new Vector2(x*(size/2-7),y*size/2),new Vector2(16,3),Color.white).raycastTarget=false;
                Panel("Bracket vertical",root,new Vector2(x*size/2,y*(size/2-7)),new Vector2(3,16),Color.white).raycastTarget=false;
            }
            scoreTraceEdges[root]=root.GetComponentsInChildren<Image>();
            foreach(var image in scoreTraceEdges[root])
            {
                var shadow=image.gameObject.AddComponent<Shadow>();
                shadow.effectColor=Ink;shadow.effectDistance=new Vector2(1.2f,-1.2f);
            }
            return root;
        }

        private bool ScoreTraceBlocked => run == null || !IsBoardCaseOpen || PauseOpen || QuickGuideOpen || ReferenceOpen ||
            ShouldShowOpeningTour() || buildPaletteOpen || hasPendingPlacement || DeckInventoryOpen ||
            specialTaskSelectionRoot != null && specialTaskSelectionRoot.gameObject.activeSelf ||
            finalResultPanel != null && finalResultPanel.activeSelf ||
            localRankingsOverlay != null && localRankingsOverlay.activeSelf ||
            networkRankingsOverlay != null && networkRankingsOverlay.activeSelf ||
            endlessRulesOverlay != null && endlessRulesOverlay.activeSelf;

        public bool HandleScoreIndexKey(KeyCode key, bool shift = false)
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (ScoreTraceBlocked || scoreMotionAnimation!=null || selected != null &&
                (selected.GetComponentInParent<InputField>() != null || selected.GetComponentInParent<TMP_InputField>() != null)) return false;
            if(key==KeyCode.H) return ToggleScoreIndex();
            if(key==KeyCode.Tab) return StepScoreIndex(shift ? -1 : 1);
            if(key==KeyCode.F && scoreTraceReviewOpen) return FocusScoreIndex();
            if(key==KeyCode.Escape && scoreTraceReviewOpen) {CloseScoreIndex();return true;}
            return false;
        }

        private void UpdateScoreTrace()
        {
            if(scoreTracePanel == null) return;
            if(Input.GetKeyDown(KeyCode.H)) HandleScoreIndexKey(KeyCode.H);
            if(Input.GetKeyDown(KeyCode.Tab)) HandleScoreIndexKey(KeyCode.Tab,Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
            if(Input.GetKeyDown(KeyCode.F)) HandleScoreIndexKey(KeyCode.F);
            DrawScoreTrace();
        }

        private void RefreshScoreTrace()
        {
            if(scoreTracePanel == null || run == null) return;
            var day = run.LastScoreDay?.day ?? 0;
            if(scoreTraceRunId != run.RunId || scoreTraceDay != day)
            {
                scoreTraceReviewOpen=false; scoreTraceAutomatic=false; scoreTraceIndex=0;
                scoreTraceRunId=run.RunId; scoreTraceDay=day;
                scoreTraceEntries=ConstructionScoreTrace.For(run);
                scoreTraceActive=Array.Empty<ScoreTraceEntry>();
            }
            scoreTraceLaunch.interactable=scoreTraceEntries.Count>0;
            scoreTraceLaunchLabel.text=L("Score sources · H","得分来源 · H");
            for(var i=0;i<3;i++) scoreTraceButtonLabels[i].text=i==0?L("Previous","上一项"):i==1?L("Next","下一项"):L("Close","关闭");
            if(scoreTraceReviewOpen) SelectScoreTrace();
            DrawScoreTrace();
        }

        public bool ToggleScoreIndex()
        {
            if(scoreTraceReviewOpen) { CloseScoreIndex(); return true; }
            return JumpToScoreIndex(0);
        }

        public bool StepScoreIndex(int direction)
        {
            RefreshScoreTrace();
            if(ScoreTraceBlocked || scoreTraceEntries.Count==0) return false;
            return JumpToScoreIndex(scoreTraceReviewOpen ? scoreTraceIndex+direction : direction<0 ? scoreTraceEntries.Count-1 : 0);
        }

        public bool JumpToScoreIndex(int index)
        {
            RefreshScoreTrace();
            if(ScoreTraceBlocked || scoreMotionAnimation!=null || scoreTraceEntries.Count==0) return false;
            scoreTraceReviewOpen=true;
            scoreTraceIndex=((index%scoreTraceEntries.Count)+scoreTraceEntries.Count)%scoreTraceEntries.Count;
            SelectScoreTrace();
            var visible=scoreTraceActive[0].Tiles.Any(id=>VisibleTile(id)!=null);
            if(IsEndless && !visible) FocusScoreIndex();
            DrawScoreTrace(); return true;
        }

        private void SelectScoreTrace()
        {
            var entry=scoreTraceEntries[scoreTraceIndex]; scoreTraceActive=new[]{entry};
            scoreTraceHeading.text=L($"Score sources {scoreTraceIndex+1}/{scoreTraceEntries.Count}",$"得分来源 {scoreTraceIndex+1}/{scoreTraceEntries.Count}");
            scoreTraceTitle.text=L(entry.English,entry.Chinese)+(entry.Points.HasValue ? $"  {entry.Points.Value:+0;-0;0}" : "");
            scoreTraceTitle.color=entry.Points<0 ? ReadingWarning : Ink;
            var count=entry.Actors.Count(actor=>worldView?.ScoreTraceActor(actor)!=null);
            scoreTraceDetail.text=L($"Day {scoreTraceDay} · {entry.Tiles.Count} plots · {count} actors",$"第 {scoreTraceDay} 天 · {entry.Tiles.Count} 个房间 · {count} 个角色");
        }

        public bool FocusScoreIndex()
        {
            if(ScoreTraceBlocked || !scoreTraceReviewOpen) return false;
            var tile=run.BuiltTiles.FirstOrDefault(tile=>tile.id==scoreTraceEntries[scoreTraceIndex].Tiles.FirstOrDefault());
            if(tile==null) return false;
            if(IsEndless)
            {
                StopViewportUiAnimations(); viewMinColumn=tile.column-3; viewMinRow=tile.row-3; Refresh();
            }
            return true;
        }

        public void CloseScoreIndex()
        {
            scoreTraceReviewOpen=false; scoreTraceAutomatic=false;
            if(run?.UsesFillHand==true) RefreshFillRightHandPreview();
            scoreTraceActive=Array.Empty<ScoreTraceEntry>(); DrawScoreTrace();
        }

        private void ShowScoreTracePhase(ScoreSide side)
        {
            RefreshScoreTrace();
            if(side is not (ScoreSide.Resident or ScoreSide.Animal or ScoreSide.Shared))
            {EndScoreTracePhase();return;}
            scoreTraceReviewOpen=false;
            var kind=side==ScoreSide.Animal?ScoreTraceKind.Animal:side==ScoreSide.Resident?ScoreTraceKind.Resident:
                side==ScoreSide.Build?ScoreTraceKind.Build:ScoreTraceKind.Shared;
            scoreTraceActive=scoreTraceEntries.Where(entry=>entry.Kind==kind).ToArray();
            scoreBatchLabel.text=side switch
            {
                ScoreSide.Resident=>L("Residents · 1/3","居民结算 · 1/3"),
                ScoreSide.Animal=>L("Animals · 2/3","动物结算 · 2/3"),
                _=>L("Shared · 3/3","共同得分 · 3/3")
            };
            scoreBatchLabel.color=side==ScoreSide.Animal?Color.Lerp(ScoreAnimalGreen,Paper,0.35f):side==ScoreSide.Shared?Color.Lerp(ScoreSharedBlue,Paper,0.25f):ScoreGold;
            scoreTraceAutomatic=true; DrawScoreTrace();
        }

        private void EndScoreTracePhase()
        {
            scoreTraceAutomatic=false;
            if(!scoreTraceReviewOpen) scoreTraceActive=Array.Empty<ScoreTraceEntry>();
            DrawScoreTrace();
        }

        private Color TraceColor(ScoreTraceEntry entry) => entry.Points<0 ? ReadingWarning : entry.Kind switch
        {
            ScoreTraceKind.Animal => ScoreAnimalGreen,
            ScoreTraceKind.Resident => FillScoreColor(ScoreSide.Resident,entry.Points??0),
            ScoreTraceKind.Shared => ScoreSharedBlue,
            _ => HandBrass
        };

        private void DrawScoreTrace()
        {
            if(scoreTracePanel==null) return;
            var visible=!ScoreTraceBlocked;
            scoreBatchPanel.gameObject.SetActive(visible && scoreTraceAutomatic);
            scoreTracePanel.gameObject.SetActive(scoreTraceReviewOpen && visible);
            if(fillRightPlacementInstruction!=null && scoreTraceReviewOpen)fillRightPlacementInstruction.gameObject.SetActive(false);
            if(fillRightHandTagline!=null && scoreTraceReviewOpen)fillRightHandTagline.gameObject.SetActive(false);
            var active=visible && (scoreTraceReviewOpen || scoreTraceAutomatic) ? scoreTraceActive : Array.Empty<ScoreTraceEntry>();
            var rooms=scoreTraceRoomColors; var actors=scoreTraceActorColors; rooms.Clear(); actors.Clear();
            foreach(var entry in active.OrderBy(entry=>entry.Points<0 ? 1 : 0))
            {
                var color=TraceColor(entry);
                foreach(var id in entry.Tiles) rooms[id]=color;
                foreach(var reference in entry.Actors)
                {
                    var actor=worldView?.ScoreTraceActor(reference); if(actor!=null) actors[actor]=color;
                }
            }
            var alpha=0.78f+0.22f*Mathf.Sin(Time.unscaledTime*3f);
            void Paint(RectTransform rect,Color color)
            {
                color.a=alpha;
                foreach(var image in scoreTraceEdges[rect]) image.color=color;
            }
            for(var y=0;y<7;y++) for(var x=0;x<7;x++)
            {
                var tile=run?.At(viewMinColumn+x,viewMinRow+y); var rect=scoreTraceRooms[x,y];
                var shown=tile!=null && rooms.ContainsKey(tile.id); rect.gameObject.SetActive(shown);
                if(shown) {rect.SetAsLastSibling();Paint(rect,rooms[tile.id]);}
            }
            var actorIndex=0;
            foreach(var pair in actors)
            {
                if(actorIndex==scoreTraceActorMarkers.Count) scoreTraceActorMarkers.Add(TraceBracket("Scoring actor bracket",scoreTraceActorLayer,30f));
                var marker=scoreTraceActorMarkers[actorIndex++];
                var screen=worldView.BoardCamera.WorldToScreenPoint(pair.Key.position);
                var canvas=scoreTraceActorLayer.GetComponentInParent<Canvas>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(scoreTraceActorLayer,screen,
                    canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var point);
                marker.anchoredPosition=point; marker.gameObject.SetActive(screen.z>0 && worldView.BoardCamera.pixelRect.Contains(screen));
                Paint(marker,pair.Value);
            }
            for(var i=actorIndex;i<scoreTraceActorMarkers.Count;i++) scoreTraceActorMarkers[i].gameObject.SetActive(false);
        }
    }
}
