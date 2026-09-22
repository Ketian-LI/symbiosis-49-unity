using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class LowPolyTreeVisualBuilderTests
    {
        [Test]
        public void GrowthStagesKeepFacetedCrownAndTaperedTrunk()
        {
            var parent = new GameObject("Tree Test Room");
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                var sapling = LowPolyTreeVisualBuilder.Build(parent.transform, Vector3.zero,
                    0.30f, true, material, HideFlags.None);
                var mature = LowPolyTreeVisualBuilder.Build(parent.transform, Vector3.zero,
                    0.82f, false, material, HideFlags.None);

                Assert.That(sapling.localPosition.y, Is.EqualTo(0.24f));
                Assert.That(sapling.GetComponentsInChildren<MeshFilter>()
                    .Count(item => item.name.Contains("Crown")), Is.EqualTo(2));
                Assert.That(mature.GetComponentsInChildren<MeshFilter>()
                    .Count(item => item.name.Contains("Canopy")), Is.EqualTo(5));
                Assert.That(mature.GetComponentsInChildren<MeshFilter>()
                    .Any(item => item.name.Contains("Faceted Tapered Trunk")), Is.True);

                foreach (var crown in mature.GetComponentsInChildren<MeshFilter>()
                    .Where(item => item.name.Contains("Canopy")))
                {
                    Assert.That(crown.sharedMesh.triangles.Length / 3, Is.EqualTo(20));
                    Assert.That(crown.sharedMesh.vertexCount, Is.EqualTo(60));
                    Assert.That(crown.GetComponent<Collider>(), Is.Null);
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void DestroyingPreviewTreeAlsoReleasesProceduralMesh()
        {
            var parent = new GameObject("Disposable Tree");
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            var tree = LowPolyTreeVisualBuilder.Build(parent.transform, Vector3.zero,
                0.82f, false, material, HideFlags.None);
            var mesh = tree.GetComponentInChildren<MeshFilter>().sharedMesh;
            Assert.That(mesh, Is.Not.Null);

            Object.DestroyImmediate(parent);
            Assert.That(mesh == null, Is.True);
            Object.DestroyImmediate(material);
        }
    }
}
