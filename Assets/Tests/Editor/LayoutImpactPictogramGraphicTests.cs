using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class LayoutImpactPictogramGraphicTests
    {
        [Test]
        public void EveryLayoutMetricDrawsWithinItsSquareIconSlot()
        {
            var method = typeof(LayoutImpactPictogramGraphic).GetMethod(
                "OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null);
            Assert.That(method, Is.Not.Null);

            foreach (LayoutImpactMetricKind kind in Enum.GetValues(typeof(LayoutImpactMetricKind)))
            {
                var holder = new GameObject($"Icon {kind}", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(LayoutImpactPictogramGraphic));
                try
                {
                    holder.GetComponent<RectTransform>().sizeDelta = new Vector2(46f, 46f);
                    var graphic = holder.GetComponent<LayoutImpactPictogramGraphic>();
                    graphic.Kind = kind;
                    using var vertices = new VertexHelper();
                    method.Invoke(graphic, new object[] { vertices });
                    Assert.That(vertices.currentVertCount, Is.GreaterThan(0), $"{kind} is empty");
                    var mesh = new Mesh();
                    try
                    {
                        vertices.FillMesh(mesh);
                        foreach (var vertex in mesh.vertices)
                        {
                            Assert.That(Mathf.Abs(vertex.x), Is.LessThan(23f), $"{kind} exceeds icon width");
                            Assert.That(Mathf.Abs(vertex.y), Is.LessThan(23f), $"{kind} exceeds icon height");
                        }
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(mesh);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(holder);
                }
            }
        }
    }
}
