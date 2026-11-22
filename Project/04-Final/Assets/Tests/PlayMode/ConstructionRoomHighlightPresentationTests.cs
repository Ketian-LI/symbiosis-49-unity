using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionRoomHighlightPresentationTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [UnityTest] public IEnumerator WasteCoverageAndWarningsCoexistAndSelectionSurvivesPointerExit()
        {
            var run=new ConstructionRunModel(ConstructionGameMode.Endless);
            run.ConfigureEndlessLossRules(20,20);run.SetOpeningTourCompleted(true);run.TrySimulateDay(out _);
            foreach(var item in new[]{(ConstructionCategory.Residence,4,3),(ConstructionCategory.Waste,4,4),(ConstructionCategory.Green,5,4),(ConstructionCategory.Residence,5,5)})
            {Assert.That(run.TryBuild(item.Item2,item.Item3,item.Item1,item.Item1==ConstructionCategory.Green?GreenPlanting.Meadow:GreenPlanting.None,out _,out _,out _),Is.True);Assert.That(run.TrySimulateConstructionDay(out _),Is.True);}
            var root=new GameObject("Persistent room selection fixture");
            try
            {
                var controller=root.AddComponent<ConstructionPrototypeController>();controller.UseRunForTesting(run);
                yield return null;var until=Time.realtimeSinceStartup+12f;
                while(!controller.IsBoardCaseOpen && Time.realtimeSinceStartup<until)yield return null;
                Assert.That(controller.IsBoardCaseOpen,Is.True);
                var before=JsonUtility.ToJson(run.Export());
                Assert.That(controller.SelectRoomForReview(4,4),Is.True);yield return null;
                var minX=(int)typeof(ConstructionPrototypeController).GetField("viewMinColumn",Private).GetValue(controller);
                var minY=(int)typeof(ConstructionPrototypeController).GetField("viewMinRow",Private).GetValue(controller);
                var px=4-minX;var py=3-minY;var dx=5-minX;var dy=5-minY;
                var frames=(RectTransform[,,])typeof(ConstructionPrototypeController).GetField("selectedRoomFrames",Private).GetValue(controller);
                var coverage=(Image[,])typeof(ConstructionPrototypeController).GetField("selectedRoomCoverage",Private).GetValue(controller);
                Assert.That(coverage[px,py].gameObject.activeInHierarchy,Is.True);
                Assert.That(coverage[dx,dy].gameObject.activeInHierarchy,Is.True);
                Assert.That(frames[px,py,1].gameObject.activeInHierarchy,Is.True);
                Assert.That(frames[dx,dy,1]==null || !frames[dx,dy,1].gameObject.activeInHierarchy,Is.True);
                var cells=(Image[,])typeof(ConstructionPrototypeController).GetField("cellBackgrounds",Private).GetValue(controller);
                ExecuteEvents.Execute(cells[4-minX,4-minY].gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);
                yield return null;
                Assert.That(controller.SelectedRoomReviewId,Is.EqualTo(run.At(4,4).id));
                foreach(var language in new[]{InterfaceLanguage.English,InterfaceLanguage.Chinese})
                {
                    controller.UseLanguageForTesting(language);controller.ToggleLanguage();controller.ToggleLanguage();
                    yield return null;yield return null;
                    var detail=root.GetComponentsInChildren<Text>().Single(t=>t.name=="Selected room relationship explanation");
                    Assert.That(detail.text,Does.Contain("-80"));
                    var sdf=detail.GetComponentInChildren<TMPro.TextMeshProUGUI>();sdf.ForceMeshUpdate();
                    Assert.That(sdf.isTextTruncated,Is.False,detail.text);
                    Assert.That(sdf.preferredHeight,Is.LessThanOrEqualTo(detail.rectTransform.rect.height+1));
                    var path=Environment.GetEnvironmentVariable("SYMBIOSIS49_STORY_CAPTURE_PATH");
                    if(!string.IsNullOrEmpty(path))
                        yield return (IEnumerator)typeof(ConstructionNeighborhoodStoryPresentationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic)
                            .Invoke(null,new object[]{root,Path.Combine(Path.GetDirectoryName(path),language==InterfaceLanguage.Chinese?"room-selection-zh.png":"room-selection-en.png")});
                }
                typeof(ConstructionPrototypeController).GetMethod("ToggleQuickGuide",Private).Invoke(controller,null);yield return null;
                Assert.That(frames[px,py,1].gameObject.activeInHierarchy,Is.False);
                Assert.That(controller.TryCancelRoomHighlights(),Is.False,"The hidden selection must not take a modal's Escape key.");
                Assert.That(controller.SelectedRoomReviewId,Is.EqualTo(run.At(4,4).id));
                typeof(ConstructionPrototypeController).GetMethod("ToggleQuickGuide",Private).Invoke(controller,null);yield return null;
                Assert.That(frames[px,py,1].gameObject.activeInHierarchy,Is.True);
                Assert.That(controller.SelectRoomForReview(4,3),Is.True);yield return null;
                Assert.That(root.GetComponentsInChildren<RectTransform>().Any(t=>t.name=="Selected room benefit"),Is.True);
                Assert.That(controller.ClearRoomHighlightSelection(),Is.True);yield return null;
                Assert.That(root.GetComponentsInChildren<RectTransform>().Any(t=>t.name=="Selected room penalty"),Is.False);
                Assert.That(JsonUtility.ToJson(run.Export()),Is.EqualTo(before));
            }
            finally{UnityEngine.Object.Destroy(root);}yield return null;
        }
    }
}
