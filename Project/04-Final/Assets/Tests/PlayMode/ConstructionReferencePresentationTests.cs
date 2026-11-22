using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionReferencePresentationTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static IEnumerator Capture(GameObject root,string name)
        {var path=Environment.GetEnvironmentVariable("SYMBIOSIS49_STORY_CAPTURE_PATH");if(string.IsNullOrEmpty(path))yield break;
         yield return (IEnumerator)typeof(ConstructionNeighborhoodStoryPresentationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,Path.Combine(Path.GetDirectoryName(path),name)});}
        [UnityTest] public IEnumerator ExamplesJournalAndRoadMarkersStayReadableAndDoNotMutateTheRun()
        {
            var run=(ConstructionRunModel)typeof(ConstructionApprovedFillUiTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{true});
            var root=new GameObject("Rules examples and journal fixture");
            try
            {
                var controller=root.AddComponent<ConstructionPrototypeController>();controller.UseRunForTesting(run);yield return null;
                var end=Time.realtimeSinceStartup+12;while(!controller.IsBoardCaseOpen && Time.realtimeSinceStartup<end)yield return null;
                var before=JsonUtility.ToJson(run.Export());
                var death=(Text[,])typeof(ConstructionPrototypeController).GetField("streetDeathBadges",Private).GetValue(controller);
                Assert.That(death[4,2].transform.parent.gameObject.activeSelf,Is.False,"No death warning at zero incidents.");
                foreach(var language in new[]{InterfaceLanguage.English,InterfaceLanguage.Chinese})
                {
                    controller.UseLanguageForTesting(language);controller.ToggleLanguage();controller.ToggleLanguage();
                    Assert.That(controller.ToggleReference(false),Is.True);
                    for(var page=0;page<3;page++)
                    {
                        typeof(ConstructionPrototypeController).GetField("referencePage",Private).SetValue(controller,page);
                        typeof(ConstructionPrototypeController).GetMethod("RefreshReference",Private).Invoke(controller,null);yield return null;yield return null;
                        Assert.That(controller.SelectRoomForReview(2,4),Is.False,"The reference owns input.");
                        var diagram=root.GetComponentsInChildren<RectTransform>().Single(t=>t.name=="Building diagrams");
                        foreach(var label in diagram.GetComponentsInChildren<Text>().Where(t=>!string.IsNullOrWhiteSpace(t.text)))
                        {var sdf=label.GetComponentInChildren<TMPro.TextMeshProUGUI>();sdf.ForceMeshUpdate();Assert.That(sdf.isTextTruncated,Is.False,label.text);}
                        yield return Capture(root,$"building-example-{page+1}-"+(language==InterfaceLanguage.Chinese?"zh.png":"en.png"));
                    }
                    Assert.That(controller.ToggleReference(true),Is.True);yield return null;
                    var journal=root.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Single(t=>t.name=="Independent journal entries");
                    Assert.That(journal.text,Does.Contain("-80"));
                    Assert.That(journal.text,Does.Contain(language==InterfaceLanguage.Chinese?"持续扣分":"Recurring"));
                    yield return Capture(root,language==InterfaceLanguage.Chinese?"independent-journal-zh.png":"independent-journal-en.png");
                    controller.CloseReference();Assert.That(controller.ReferenceOpen,Is.False);
                }
                Assert.That(JsonUtility.ToJson(run.Export()),Is.EqualTo(before));
            }
            finally{UnityEngine.Object.Destroy(root);}yield return null;
        }
    }
}
