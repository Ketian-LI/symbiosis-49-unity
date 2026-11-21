using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionScoreIndexPresentationTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [UnityTest] public IEnumerator IndexHighlightsRealParticipantsAndHonoursShortcutsAndModals()
        {
            var run=Fill();var root=new GameObject("Score index fixture");
            try
            {
                var controller=root.AddComponent<ConstructionPrototypeController>();controller.UseRunForTesting(run);
                yield return Ready(controller);
                root.GetComponentsInChildren<Button>().First(button=>button.name=="Fill hand card 1").onClick.Invoke();
                var saved=JsonUtility.ToJson(run.Export());
                var entries=ConstructionScoreTrace.For(run);
                var residentIndex=entries.ToList().FindIndex(entry=>entry.Kind==ScoreTraceKind.Resident);
                Assert.That(residentIndex,Is.GreaterThanOrEqualTo(0));
                Assert.That(controller.JumpToScoreIndex(residentIndex),Is.True);
                yield return null;
                var world=root.GetComponentInChildren<ConstructionWorldView>();
                var actor=world.ScoreTraceActor(entries[residentIndex].Actors[0]);
                Assert.That(actor,Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<RectTransform>().Count(rect=>rect.name=="Scoring actor bracket"),Is.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<RectTransform>().Count(rect=>rect.name=="Scoring room highlight"),
                    Is.EqualTo(entries[residentIndex].Tiles.Count));
                var start=controller.CurrentScoreIndex;
                Assert.That(controller.HandleScoreIndexKey(KeyCode.Tab),Is.True);
                Assert.That(controller.CurrentScoreIndex,Is.EqualTo((start+1)%controller.ScoreIndexCount));
                Assert.That(controller.HandleScoreIndexKey(KeyCode.Tab,true),Is.True);
                Assert.That(controller.CurrentScoreIndex,Is.EqualTo(start));
                Assert.That(controller.JumpToScoreIndex(-1),Is.True);
                Assert.That(controller.CurrentScoreIndex,Is.EqualTo(controller.ScoreIndexCount-1));
                var input=new GameObject("Nickname field",typeof(RectTransform),typeof(InputField));input.transform.SetParent(root.transform);
                EventSystem.current.SetSelectedGameObject(input);
                var last=controller.CurrentScoreIndex;
                Assert.That(controller.HandleScoreIndexKey(KeyCode.H),Is.False);
                Assert.That(controller.HandleScoreIndexKey(KeyCode.Tab),Is.False);
                Assert.That(controller.CurrentScoreIndex,Is.EqualTo(last));
                EventSystem.current.SetSelectedGameObject(null);UnityEngine.Object.Destroy(input);
                controller.JumpToScoreIndex(residentIndex);
                foreach(var language in new[]{InterfaceLanguage.English,InterfaceLanguage.Chinese})
                {
                    controller.UseLanguageForTesting(language);controller.ToggleLanguage();controller.ToggleLanguage();
                    yield return null;yield return null;
                    foreach(var text in root.GetComponentsInChildren<Text>().Where(text=>text.name.StartsWith("Score index")||text.name=="Label"&&text.transform.parent.name.StartsWith("Score index control")))
                    {
                        var sdf=text.GetComponentInChildren<TextMeshProUGUI>();if(sdf==null)continue;sdf.ForceMeshUpdate();
                        Assert.That(sdf.isTextTruncated,Is.False,text.name+": "+text.text);
                    }
                    var capture=Environment.GetEnvironmentVariable("SYMBIOSIS49_STORY_CAPTURE_PATH");
                    if(!string.IsNullOrEmpty(capture))
                        yield return (IEnumerator)typeof(ConstructionNeighborhoodStoryPresentationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic)
                            .Invoke(null,new object[]{root,Path.Combine(Path.GetDirectoryName(capture),language==InterfaceLanguage.Chinese?"score-index-zh.png":"score-index-en.png")});
                }
                var next=root.GetComponentsInChildren<Button>().First(button=>button.name=="Score index control 1");
                Canvas.ForceUpdateCanvases();
                var point=RectTransformUtility.WorldToScreenPoint(null,next.transform.position);
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                Assert.That(hits.First().gameObject.GetComponentInParent<Button>(),Is.EqualTo(next),"Index controls must be the top pointer target.");
                typeof(ConstructionPrototypeController).GetMethod("ToggleQuickGuide",Private).Invoke(controller,null);
                yield return null;
                Assert.That(controller.HandleScoreIndexKey(KeyCode.Tab),Is.False);
                Assert.That(root.GetComponentsInChildren<RectTransform>().Any(rect=>rect.name=="Scoring actor bracket"),Is.False);
                typeof(ConstructionPrototypeController).GetMethod("ToggleQuickGuide",Private).Invoke(controller,null);
                yield return null;
                Assert.That(controller.HandleScoreIndexKey(KeyCode.Escape),Is.True);
                Assert.That(controller.IsScoreIndexOpen,Is.False);
                var day=run.LastScoreDay;
                typeof(ConstructionPrototypeController).GetMethod("StartFillHandScoreMotion",Private).Invoke(controller,new object[]{run.LastEcologyDay,
                    run.AnimalScore-day.animalPoints+day.trafficPenaltyPoints,run.HumanScore-day.humanPoints,run.BalanceScore-day.balancePoints});
                var seen=new HashSet<ScoreTraceKind>();var end=Time.realtimeSinceStartup+12f;
                while(typeof(ConstructionPrototypeController).GetField("scoreMotionAnimation",Private).GetValue(controller)!=null && Time.realtimeSinceStartup<end)
                {
                    var active=(ScoreTraceEntry[])typeof(ConstructionPrototypeController).GetField("scoreTraceActive",Private).GetValue(controller);
                    foreach(var entry in active)seen.Add(entry.Kind);
                    yield return null;
                }
                Assert.That(seen,Does.Contain(ScoreTraceKind.Animal));Assert.That(seen,Does.Contain(ScoreTraceKind.Resident));Assert.That(seen,Does.Contain(ScoreTraceKind.Shared));
                Assert.That(root.GetComponentsInChildren<RectTransform>().Any(rect=>rect.name=="Scoring room highlight"),Is.False);
                Assert.That(JsonUtility.ToJson(run.Export()),Is.EqualTo(saved));
            }
            finally{UnityEngine.Object.Destroy(root);}
            yield return null;
        }
        [UnityTest] public IEnumerator EndlessIndexJumpsToAnOffscreenContributor()
        {
            var run=new ConstructionRunModel(ConstructionGameMode.Endless);run.ConfigureEndlessLossRules(20,20);run.SetOpeningTourCompleted(true);run.TrySimulateDay(out _);
            foreach(var choice in new[]{(ConstructionCategory.Residence,4,3),(ConstructionCategory.Restaurant,5,3)})
            {Assert.That(run.TryBuild(choice.Item2,choice.Item3,choice.Item1,GreenPlanting.None,out _,out _,out _),Is.True);Assert.That(run.TrySimulateConstructionDay(out _),Is.True);}
            var root=new GameObject("Endless score index fixture");
            try
            {
                var controller=root.AddComponent<ConstructionPrototypeController>();controller.UseRunForTesting(run);yield return Ready(controller);
                typeof(ConstructionPrototypeController).GetField("viewMinColumn",Private).SetValue(controller,100);
                typeof(ConstructionPrototypeController).GetField("viewMinRow",Private).SetValue(controller,100);
                typeof(ConstructionPrototypeController).GetMethod("Refresh",Private).Invoke(controller,null);
                var index=ConstructionScoreTrace.For(run).ToList().FindIndex(entry=>entry.Kind==ScoreTraceKind.Resident);
                Assert.That(controller.JumpToScoreIndex(index),Is.True);
                Assert.That((int)typeof(ConstructionPrototypeController).GetField("viewMinColumn",Private).GetValue(controller),Is.LessThan(10));
                Assert.That(controller.HandleScoreIndexKey(KeyCode.F),Is.True);
            }
            finally{UnityEngine.Object.Destroy(root);}yield return null;
        }
        private static IEnumerator Ready(ConstructionPrototypeController controller)
        {yield return null;var end=Time.realtimeSinceStartup+12f;while(!controller.IsBoardCaseOpen&&Time.realtimeSinceStartup<end)yield return null;Assert.That(controller.IsBoardCaseOpen,Is.True);}
        private static ConstructionRunModel Fill()
        {
            var run=new ConstructionRunModel(ConstructionGameMode.Fill,true);
            var initial=run.Export();initial.runId="00000000-0000-0000-0000-000000000001";initial.fillHandRandomState=73856093;initial.specialTasks=null;
            Assert.That(ConstructionRunModel.TryRestore(initial,out run),Is.True);
            run.TryConfirmSpecialTasks();run.SetOpeningTourCompleted(true);
            foreach(var c in new[]{(ConstructionCategory.Green,3,3),(ConstructionCategory.Residence,4,3),(ConstructionCategory.Restaurant,5,3),(ConstructionCategory.Workshop,4,4),(ConstructionCategory.Street,4,2),(ConstructionCategory.Waste,6,3),(ConstructionCategory.Supermarket,3,2),(ConstructionCategory.Square,4,1)})
            {Assert.That(run.TryBuild(c.Item2,c.Item3,c.Item1,c.Item1==ConstructionCategory.Green?GreenPlanting.Oak:GreenPlanting.None,out _,out _,out _),Is.True);Assert.That(run.TrySimulateFillDay(out _),Is.True);}
            return run;
        }
    }
}
