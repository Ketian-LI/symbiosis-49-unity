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
    public sealed class ConstructionApprovedFillUiTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static ConstructionRunModel Fixture(bool waste=false)
        {
            var run=new ConstructionRunModel(ConstructionGameMode.Fill,useFillHand:true);
            var save=run.Export();save.runId="00000000-0000-0000-0000-000000000001";
            save.fillHandRandomState=73856093;save.specialTasks=null;
            Assert.That(ConstructionRunModel.TryRestore(save,out run),Is.True);
            Assert.That(run.TryConfirmSpecialTasks(),Is.True);run.SetOpeningTourCompleted(true);
            var opening=new[]{(ConstructionCategory.Green,3,3),(ConstructionCategory.Residence,4,3),
                (ConstructionCategory.Restaurant,5,3),(ConstructionCategory.Workshop,4,4),
                (ConstructionCategory.Street,4,2),(ConstructionCategory.Waste,6,3),
                (ConstructionCategory.Supermarket,3,2),(ConstructionCategory.Square,4,1)};
            foreach(var tile in opening)Build(ref run,tile.Item1,tile.Item2,tile.Item3);
            Build(ref run,ConstructionCategory.Green,2,3);
            if(waste)
            {
                Build(ref run,ConstructionCategory.Residence,3,4);
                Build(ref run,ConstructionCategory.Waste,2,4);
                Build(ref run,ConstructionCategory.Residence,1,3);
            }
            return run;
        }
        private static void Build(ref ConstructionRunModel run,ConstructionCategory category,int x,int y)
        {
            // Fixed UI fixture, separate from randomized draw/probability checks.
            if(!run.IsIntroPhase && !run.FillHandCards.Contains(category))
            {var save=run.Export();save.fillHandCards[0]=category;Assert.That(ConstructionRunModel.TryRestore(save,out run),Is.True);}
            Assert.That(run.TryBuild(x,y,category,category==ConstructionCategory.Green?
                run.BuiltCount==0?GreenPlanting.Oak:GreenPlanting.Meadow:GreenPlanting.None,
                out _,out var failure,out var placement),Is.True,failure+" / "+placement);
            Assert.That(run.TrySimulateFillDay(out _),Is.True);
        }
        private static IEnumerator Ready(ConstructionPrototypeController controller)
        {yield return null;var end=Time.realtimeSinceStartup+12f;while(!controller.IsBoardCaseOpen && Time.realtimeSinceStartup<end)yield return null;Assert.That(controller.IsBoardCaseOpen,Is.True);yield return null;}
        private static IEnumerator Capture(GameObject root,string filename)
        {
            var path=Environment.GetEnvironmentVariable("SYMBIOSIS49_STORY_CAPTURE_PATH");
            if(string.IsNullOrEmpty(path))yield break;
            yield return (IEnumerator)typeof(ConstructionNeighborhoodStoryPresentationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{root,Path.Combine(Path.GetDirectoryName(path),filename)});
        }
        private static void Fits(Text label)
        {
            var sdf=label.GetComponentInChildren<TMPro.TextMeshProUGUI>();Assert.That(sdf,Is.Not.Null,label.name);
            sdf.ForceMeshUpdate();Assert.That(sdf.isTextTruncated,Is.False,label.name+": "+label.text);
            Assert.That(sdf.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+1),label.name);
        }
        [UnityTest] public IEnumerator SkippedTutorialEmptyFillBoardKeepsThreeGoalsAndManualShortcut()
        {
            var run=new ConstructionRunModel(ConstructionGameMode.Fill,useFillHand:true);
            Assert.That(run.TryConfirmSpecialTasks(),Is.True);run.SetOpeningTourCompleted(true);
            Assert.That(run.SkipFillTutorial(),Is.True);
            var root=new GameObject("Approved Fill opening fixture");
            try
            {
                var controller=root.AddComponent<ConstructionPrototypeController>();controller.UseRunForTesting(run);yield return Ready(controller);
                controller.UseLanguageForTesting(InterfaceLanguage.Chinese);controller.ToggleLanguage();controller.ToggleLanguage();yield return null;yield return null;
                Assert.That(run.BuiltCount,Is.Zero);
                Assert.That(root.GetComponentsInChildren<Text>().Count(t=>t.name=="Goal count"),Is.EqualTo(3));
                Assert.That(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("手册") && t.text.Contains("O")),Is.True);
                yield return Capture(root,"fill-opening-zh.png");
            }
            finally{UnityEngine.Object.Destroy(root);}yield return null;
        }
        [UnityTest] public IEnumerator RemainingCardsAreRealBilingualModalAndReadOnly()
        {
            var run=Fixture();var root=new GameObject("Approved Fill deck fixture");
            try
            {
                var controller=root.AddComponent<ConstructionPrototypeController>();controller.UseRunForTesting(run);yield return Ready(controller);
                var before=JsonUtility.ToJson(run.Export());
                foreach(var language in new[]{InterfaceLanguage.English,InterfaceLanguage.Chinese})
                {
                    controller.UseLanguageForTesting(language);controller.ToggleLanguage();controller.ToggleLanguage();
                    Assert.That(controller.ToggleDeckInventory(),Is.True);yield return null;yield return null;
                    Assert.That(controller.DeckInventoryOpen,Is.True);
                    Assert.That(controller.SelectRoomForReview(3,3),Is.False,"Inventory owns input.");
                    var panel=root.GetComponentsInChildren<RectTransform>().Single(t=>t.name=="Deck inventory frame");
                    foreach(var label in panel.GetComponentsInChildren<Text>())if(label.text.Length>0)Fits(label);
                    var summary=panel.Find("Deck inventory summary").GetComponent<Text>().text;
                    Assert.That(summary,Does.Contain(run.FillDeckCardsRemaining.ToString()));
                    foreach(var label in root.GetComponentsInChildren<Text>().Where(t=>t.name is "Remaining cards on draw pile" or "View remaining cards"))Fits(label);
                    yield return Capture(root,language==InterfaceLanguage.Chinese?"fill-deck-zh.png":"fill-deck-en.png");
                    root.GetComponentsInChildren<Button>().Single(b=>b.name=="Deck contents outside click").onClick.Invoke();
                    Assert.That(controller.DeckInventoryOpen,Is.False);
                }
                Assert.That(controller.ToggleDeckInventory(),Is.True);
                typeof(ConstructionPrototypeController).GetMethod("ToggleQuickGuide",Private).Invoke(controller,null);yield return null;
                Assert.That(controller.DeckInventoryOpen,Is.False,"Opening the manual closes the stock modal.");
                Assert.That(JsonUtility.ToJson(run.Export()),Is.EqualTo(before));
            }
            finally{UnityEngine.Object.Destroy(root);}yield return null;
        }
        [UnityTest] public IEnumerator FillSelectionShowsCoverageAndPenaltyTogetherWithoutHidingGoals()
        {
            var run=Fixture(true);var root=new GameObject("Approved Fill relationship fixture");
            try
            {
                var controller=root.AddComponent<ConstructionPrototypeController>();controller.UseRunForTesting(run);yield return Ready(controller);
                var before=JsonUtility.ToJson(run.Export());
                Assert.That(controller.SelectRoomForReview(2,4),Is.True);yield return null;
                var frames=(RectTransform[,,])typeof(ConstructionPrototypeController).GetField("selectedRoomFrames",Private).GetValue(controller);
                var coverage=(Image[,])typeof(ConstructionPrototypeController).GetField("selectedRoomCoverage",Private).GetValue(controller);
                Assert.That(coverage[3,4].gameObject.activeInHierarchy,Is.True);
                Assert.That(coverage[1,3].gameObject.activeInHierarchy,Is.True);
                Assert.That(frames[3,4,1].gameObject.activeInHierarchy,Is.True);
                Assert.That(frames[1,3,1]==null || !frames[1,3,1].gameObject.activeInHierarchy,Is.True,"Diagonal coverage is not a shared-edge penalty.");
                foreach(var language in new[]{InterfaceLanguage.English,InterfaceLanguage.Chinese})
                {
                    controller.UseLanguageForTesting(language);controller.ToggleLanguage();controller.ToggleLanguage();yield return null;yield return null;
                    var detail=root.GetComponentsInChildren<Text>().Single(t=>t.name=="Selected room relationship explanation");
                    Assert.That(detail.text,Does.Contain("-80"));Fits(detail);
                    var selected=detail.transform.parent.GetComponent<Image>();Assert.That(selected.color.a,Is.Zero,"Use the approved continuous paper.");
                    Assert.That(root.GetComponentsInChildren<Text>().Any(t=>t.name=="Goal count"),Is.True);
                    yield return Capture(root,language==InterfaceLanguage.Chinese?"fill-room-selection-zh.png":"fill-room-selection-en.png");
                }
                Assert.That(controller.ClearRoomHighlightSelection(),Is.True);yield return null;
                Assert.That(JsonUtility.ToJson(run.Export()),Is.EqualTo(before));
            }
            finally{UnityEngine.Object.Destroy(root);}yield return null;
        }
    }
}
