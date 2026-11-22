using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    // Opt-in, presentation-only emphasis. Plain source copy remains available
    // to accessibility, tests and localisation; only its SDF rendering is styled.
    public sealed class InstructionEmphasis : MonoBehaviour
    {
        private string previous, formatted;
        private static readonly string[] Phrases = {
            "点击高亮空格", "高亮的正中格", "高亮空格", "确认建造", "安排工作与用餐",
            "上下左右贴边", "上下左右", "至少 2 格", "2 格连片绿地", "选一张牌", "选中手牌",
            "点击住宅", "跳过教程", "下一天", "查看余牌", "建造", "换牌", "悬停", "成熟日期",
            "高亮格", "贴住宅", "贴广场", "连通街道", "连续街道", "每轮先放置一格",
            "click a lit plot", "highlighted centre plot", "Select a card", "select a hand card",
            "Set work & meals", "shared edges", "edge to edge", "Skip tutorial", "Next day",
            "click the home", "confirm", "Swap", "Hover", "maturity date", "arrow keys"
        };
        private static readonly Regex KeyPhrases = new(string.Join("|", Phrases
            .OrderByDescending(p => p.Length).Select(Regex.Escape)) +
            @"|(?<![\w])(?:O|H|Tab|Esc|Shift\+Tab)(?![\w])|\d+\s*(?:格|处|份|个岗位|个餐位|座广场)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool AppliesTo(string name) => name is
            "Story instruction" or "Room function" or "Placement guide" or "Hand instruction" or
            "Selected hand instruction" or "Selected hand valid plots" or "Selected room relationship explanation" or "Story trigger" or "Lesson room summary" or
            "Tour body" or "Tooltip details" or "Dispatch note" or "Rules summary" or
            "Special task selection note" or "Special task rule";

        public string Render(string copy)
        {
            if (previous != copy || formatted == null)
            { previous = copy; formatted = Format(copy); }
            return formatted;
        }

        public static string Format(string copy)
        {
            if (string.IsNullOrEmpty(copy)) return copy;
            var result = new StringBuilder(copy.Length + 64);
            var boldDepth = 0;
            foreach (var part in Regex.Split(copy, "(<[^>]+>)"))
            {
                if (part.StartsWith("<", StringComparison.Ordinal))
                {
                    if (part.Equals("<b>", StringComparison.OrdinalIgnoreCase)) boldDepth++;
                    if (part.Equals("</b>", StringComparison.OrdinalIgnoreCase)) boldDepth = Math.Max(0, boldDepth - 1);
                    result.Append(part);
                }
                else result.Append(boldDepth > 0 ? part : KeyPhrases.Replace(part, m => "<b>" + m.Value + "</b>"));
            }
            return result.ToString();
        }
    }
}
