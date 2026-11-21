using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionPigeonSaveRegressionTests
    {
        private static ConstructionRunSaveData Sample() =>
            JsonUtility.FromJson<ConstructionRunSaveData>(File.ReadAllText(Path.Combine(
                Application.dataPath,"Tests/Fixtures/filled-board-pigeon-relocated.json")));

        [TestCase(0)]
        [TestCase(2)]
        public void LaterGreenFoodReferenceDoesNotInvalidateEarlierArrival(int awardVersion)
        {
            var saved=Sample();saved.awardRulesVersion=awardVersion;
            var reference=saved.board.tiles.Single(t=>t.id==saved.pigeonTileId);
            Assert.That(reference.builtDay,Is.GreaterThan(saved.pigeonArrivalDay));
            var original=JsonUtility.ToJson(saved);
            Assert.That(ConstructionRunModel.TryRestore(saved,out var run),Is.True);
            Assert.That(run.IsFinished,Is.True);
            Assert.That(run.BuiltCount,Is.EqualTo(49));
            Assert.That(run.PigeonArrivalDay,Is.EqualTo(saved.pigeonArrivalDay));
            Assert.That(JsonUtility.ToJson(saved),Is.EqualTo(original),"Loading must not rewrite the source snapshot.");
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),out var again),Is.True);
            Assert.That(again.OverallScore,Is.EqualTo(run.OverallScore));
        }

        [Test]
        public void InvalidHistoricArrivalAndNonGreenReferencesAreStillRejected()
        {
            var saved=Sample();saved.pigeonArrivalDay=2;
            Assert.That(ConstructionRunModel.TryRestore(saved,out _),Is.False);
            saved=Sample();saved.pigeonTileId=saved.board.tiles.First(t=>t.category==ConstructionCategory.Residence).id;
            Assert.That(ConstructionRunModel.TryRestore(saved,out _),Is.False);
        }
    }
}
