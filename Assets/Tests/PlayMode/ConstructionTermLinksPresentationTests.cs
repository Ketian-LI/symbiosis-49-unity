using System;
using System.Collections;
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
using UrbanWildlifeRooms.UI;
namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionTermLinksPresentationTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static ConstructionRunModel Fixture()=>(ConstructionRunModel)typeof(ConstructionApprovedFillUiTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{false});
        private static IEnumerator Ready(ConstructionPrototypeController controller)
        {yield return null;var end=Time.realtimeSinceStartup+12f;while(!controller.IsBoardCaseOpen && Time.realtimeSinceStartup<end)yield return null;Assert.That(controller.IsBoardCaseOpen,Is.True);yield return null;}
        private static IEnumerator Capture(GameObject root,string name)
        {
            var path=Environment.GetEnvironmentVariable("SYMBIOSIS49_STORY_CAPTURE_PATH");if(string.IsNullOrEmpty(path))yield break;
            yield return (IEnumerator)typeof(ConstructionNeighborhoodStoryPresentationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,Path.Combine(Path.GetDirectoryName(path),name)});
        }
        private static Vector2 WordPoint(TMP_Text label,string id)
        {
            label.ForceMeshUpdate();var link=label.textInfo.linkInfo.Take(label.textInfo.linkCount).First(l=>l.GetLinkID()==id);
            var ch=label.textInfo.characterInfo[link.linkTextfirstCharacterIndex];
            return RectTransformUtility.WorldToScreenPoint(null,label.transform.TransformPoint((ch.bottomLeft+ch.topRight)*0.5f));
        }
        private static void AnchorVisible(TMP_Text text,RectTransform viewport,string id)
        {
            text.ForceMeshUpdate();var link=text.textInfo.linkInfo.Take(text.textInfo.linkCount).Single(l=>l.GetLinkID()=="entry:"+id);
            var ch=text.textInfo.characterInfo[link.linkTextfirstCharacterIndex];
            var point=viewport.InverseTransformPoint(text.transform.TransformPoint(ch.topLeft));
            Assert.That(point.y,Is.LessThanOrEqualTo(viewport.rect.yMax+2));
            Assert.That(point.y,Is.GreaterThan(viewport.rect.yMin+20));
        }
        [UnityTest] public IEnumerator ManualTermsHoverAndJumpToTheirActualBilingualLines()
        {
            var run=Fixture();var root=new GameObject("Term navigation fixture");
            try
            {
                var controller=root.AddComponent<ConstructionPrototypeController>();controller.UseRunForTesting(run);yield return Ready(controller);
                var before=JsonUtility.ToJson(run.Export());
                foreach(var language in new[]{InterfaceLanguage.English,InterfaceLanguage.Chinese})
                {
                    controller.UseLanguageForTesting(language);controller.ToggleLanguage();controller.ToggleLanguage();
                    typeof(ConstructionPrototypeController).GetMethod("ToggleQuickGuide",Private).Invoke(controller,null);yield return null;yield return null;
                    var body=root.GetComponentsInChildren<TextMeshProUGUI>().Single(t=>t.name=="Manual entries");
                    var scroll=body.GetComponentInParent<ScrollRect>();var links=body.GetComponent<ConstructionTermLinks>();
                    scroll.verticalNormalizedPosition=1f;Canvas.ForceUpdateCanvases();yield return null;
                    var point=WordPoint(body,"term:restaurant");
                    Assert.That(links.TermAt(point),Is.EqualTo("restaurant"),
                        $"active={body.gameObject.activeInHierarchy}; mode={body.canvas.renderMode}; point={point}; hit={TMP_TextUtilities.FindIntersectingLink(body,point,null)}; links={body.textInfo.linkCount}");
                    var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
                    ExecuteEvents.Execute(body.gameObject,pointer,ExecuteEvents.pointerEnterHandler);yield return null;
                    Assert.That(root.GetComponentsInChildren<Text>().Any(t=>t.name=="Tooltip details" && t.text.Contains(language==InterfaceLanguage.Chinese?"点击":"Click")),Is.True);
                    yield return Capture(root,language==InterfaceLanguage.Chinese?"term-hover-zh.png":"term-hover-en.png");
                    // The offscreen capture temporarily changes canvas/camera geometry.
                    yield return null;Canvas.ForceUpdateCanvases();pointer.position=WordPoint(body,"term:restaurant");
                    pointer.position=WordPoint(body,"term:restaurant");
                    ExecuteEvents.Execute(body.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
                    Assert.That(controller.ManualFocusedTerm,Is.EqualTo("restaurant"));AnchorVisible(body,scroll.viewport,"restaurant");
                    Assert.That(body.text,Does.Contain("<mark="));
                    yield return Capture(root,language==InterfaceLanguage.Chinese?"manual-restaurant-zh.png":"manual-restaurant-en.png");
                    foreach(var term in ConstructionGlossary.Terms)
                    {Assert.That(controller.OpenManualTerm(term.Id),Is.True,term.Id);AnchorVisible(body,scroll.viewport,term.Id);}
                    Assert.That(controller.OpenManualTerm("unknown"),Is.False);
                    // ScrollRect keeps normal drag/page ownership outside linked glyphs.
                    Assert.That(links.IsRaycastLocationValid(new Vector2(-10000,-10000),null),Is.False);
                    typeof(ConstructionPrototypeController).GetMethod("ToggleQuickGuide",Private).Invoke(controller,null);yield return null;
                }
                Assert.That(JsonUtility.ToJson(run.Export()),Is.EqualTo(before));
            }
            finally{UnityEngine.Object.Destroy(root);}yield return null;
        }
        [UnityTest] public IEnumerator DefinitionOverGoalReviewReturnsWithoutChangingGoals()
        {
            var run=Fixture();var root=new GameObject("Definition modal ownership fixture");
            try
            {
                var controller=root.AddComponent<ConstructionPrototypeController>();controller.UseRunForTesting(run);yield return Ready(controller);
                var before=JsonUtility.ToJson(run.Export());
                typeof(ConstructionPrototypeController).GetMethod("OpenSpecialTaskReview",Private).Invoke(controller,null);yield return null;
                var goalReview=root.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="Special task selection");
                Assert.That(goalReview.gameObject.activeInHierarchy,Is.True);
                Assert.That(controller.OpenManualTerm("home"),Is.True);yield return null;
                Assert.That(controller.SelectRoomForReview(3,3),Is.False);
                typeof(ConstructionPrototypeController).GetMethod("ToggleQuickGuide",Private).Invoke(controller,null);yield return null;
                Assert.That(goalReview.gameObject.activeInHierarchy,Is.True);
                Assert.That(JsonUtility.ToJson(run.Export()),Is.EqualTo(before));
            }
            finally{UnityEngine.Object.Destroy(root);}yield return null;
        }
        [UnityTest] public IEnumerator RoomHintsLinkToManualAndNoDispatchControlCanCoverGoals()
        {
            var run=Fixture();var root=new GameObject("Automatic resident UI fixture");
            try
            {
                var controller=root.AddComponent<ConstructionPrototypeController>();controller.UseRunForTesting(run);yield return Ready(controller);
                Assert.That(controller.TryPlace(4,3),Is.False);yield return null;
                Assert.That(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name is "Open resident dispatch" or "Resident dispatch"),Is.False);
                controller.OpenResidentDispatch();Assert.That(controller.CycleResidentDispatch(true,1),Is.False);
                TextMeshProUGUI text=null;
                foreach(var language in new[]{InterfaceLanguage.English,InterfaceLanguage.Chinese})
                {
                    controller.UseLanguageForTesting(language);controller.ToggleLanguage();controller.ToggleLanguage();yield return null;yield return null;
                    var detail=root.GetComponentsInChildren<Text>().Single(t=>t.name=="Selected room relationship explanation");
                    Assert.That(detail.text,Does.Contain(language==InterfaceLanguage.Chinese?"自动通勤":"Automatic commute"));
                    text=detail.GetComponentInChildren<TextMeshProUGUI>();text.ForceMeshUpdate();Assert.That(text.isTextTruncated,Is.False,detail.text);
                    Assert.That(text.text,Does.Contain("term:workshop"));
                    yield return Capture(root,language==InterfaceLanguage.Chinese?"automatic-commute-zh.png":"automatic-commute-en.png");
                    yield return null;Canvas.ForceUpdateCanvases();
                }
                var point=WordPoint(text,"term:workshop");var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(text.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
                Assert.That(controller.ManualFocusedTerm,Is.EqualTo("workshop"));
                var body=root.GetComponentsInChildren<TextMeshProUGUI>().Single(t=>t.name=="Manual entries");AnchorVisible(body,body.GetComponentInParent<ScrollRect>().viewport,"workshop");
            }
            finally{UnityEngine.Object.Destroy(root);}yield return null;
        }
    }
}
