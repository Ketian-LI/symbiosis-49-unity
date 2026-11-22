using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class TmpTextRenderingTests
    {
        [UnityTest]
        public IEnumerator ChineseAndEnglishLabelKeepsGlyphsAndHoverTarget()
        {
            var root = new GameObject("Text rendering QA", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var labelObject = new GameObject("Bilingual score label",
                typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(root.transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(520f, 72f);
            var label = labelObject.GetComponent<Text>();
            label.font = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = true;
            label.text = "动物得分 26 · 居民得分 6 / Animal 26 · Resident 6";

            yield return new WaitForSecondsRealtime(0.7f);

            var sdf = labelObject.GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.That(sdf, Is.Not.Null, "The visible label should use TMP SDF.");
            Assert.That(label.enabled, Is.False);
            Assert.That(sdf.raycastTarget, Is.True,
                "The score label must remain a pointer hover target.");
            Assert.That(sdf.text, Is.EqualTo(label.text));
            Assert.That(sdf.font.HasCharacters(label.text,
                out uint[] missing, true, true), Is.True,
                "The bilingual label must have every glyph.");
            Assert.That(missing == null || missing.Length == 0, Is.True);
            sdf.ForceMeshUpdate();
            Assert.That(sdf.isTextTruncated, Is.False);
            Object.Destroy(root);
        }
    }
}
