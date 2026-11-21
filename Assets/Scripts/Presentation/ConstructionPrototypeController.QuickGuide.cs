using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private RectTransform quickGuideRoot;
        private Button quickGuideButton;
        private Text quickGuideButtonLabel;
        private Text quickGuideHeading;
        private Text quickGuideSubtitle;
        private TextMeshProUGUI quickGuideBody;
        private Text quickGuidePreviousLabel;
        private Text quickGuideNextLabel;
        private Text quickGuideCloseLabel;
        private ScrollRect quickGuideScroll;
        private const float ManualBodyWidth = 318f;
        private const float ManualBodyHeight = 506f;
        private const float ManualPageOverlap = 52f;
        private bool quickGuideBodyChinese;
        private ConstructionGameMode quickGuideBodyMode;
        private bool quickGuideBodyUsesHand;
        private string quickGuideBodyRunId;
        private bool quickGuideBodyReady;

        private bool QuickGuideOpen => quickGuideRoot != null &&
            quickGuideRoot.gameObject.activeSelf;

        private void BuildQuickGuide(Transform canvas)
        {
            quickGuideButton = ButtonAt("Open game manual", canvas,
                new Vector2(520f, 409f), new Vector2(102f, 36f),
                "O  MANUAL", 14, Ink, Paper, ToggleQuickGuide,
                themed: false);
            quickGuideButtonLabel = quickGuideButton.GetComponentInChildren<Text>();
            caseHeaderGroups.Add(quickGuideButton.gameObject.AddComponent<CanvasGroup>());

            quickGuideRoot = NewRect("Game manual", canvas,
                Vector2.zero, new Vector2(1600f, 900f));
            Stretch(quickGuideRoot);
            // Leave the city visible while preventing a placement behind the page.
            var shield = Panel("Manual input shield", quickGuideRoot,
                Vector2.zero, new Vector2(1600f, 900f), Color.clear);
            Stretch(shield.rectTransform);
            shield.raycastTarget = true;

            var frame = Panel("Manual right leaf", quickGuideRoot,
                new Vector2(568f, -2f), new Vector2(382f, 760f), Ink);
            Panel("Manual brass edge", frame.transform, Vector2.zero,
                new Vector2(369f, 747f), Gold).raycastTarget = false;
            var paper = Panel("Manual textured paper", frame.transform,
                Vector2.zero, new Vector2(360f, 738f), Paper);
            paper.sprite = LoadSprite("UI/FillHandArt/card_paper_v2");
            paper.type = Image.Type.Simple;
            paper.raycastTarget = false;

            quickGuideHeading = TextAt("Manual heading", frame.transform,
                new Vector2(0f, 331f), new Vector2(308f, 42f), "", 25,
                true, Ink, TextAnchor.MiddleCenter);
            quickGuideHeading.raycastTarget = false;
            quickGuideSubtitle = TextAt("Manual subtitle", frame.transform,
                new Vector2(0f, 296f), new Vector2(310f, 30f), "", 16,
                false, HeadingInk, TextAnchor.MiddleCenter);
            quickGuideSubtitle.raycastTarget = false;
            Panel("Manual heading rule", frame.transform,
                new Vector2(0f, 274f), new Vector2(306f, 2f), Gold)
                .raycastTarget = false;

            var viewport = NewRect("Manual page viewport", frame.transform,
                new Vector2(0f, 6f), new Vector2(ManualBodyWidth, ManualBodyHeight));
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var bodyRect = NewRect("Manual entries", viewport, Vector2.zero,
                new Vector2(ManualBodyWidth, ManualBodyHeight));
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = new Vector2(0f, ManualBodyHeight);
            quickGuideBody = bodyRect.gameObject.AddComponent<TextMeshProUGUI>();
            quickGuideBody.fontSize = 19f;
            quickGuideBody.color = Ink;
            quickGuideBody.alignment = TextAlignmentOptions.TopLeft;
            quickGuideBody.lineSpacing = 5f;
            quickGuideBody.richText = true;
            quickGuideBody.textWrappingMode = TextWrappingModes.Normal;
            quickGuideBody.overflowMode = TextOverflowModes.Overflow;
            quickGuideBody.raycastTarget = false;
            quickGuideScroll = viewport.gameObject.AddComponent<ScrollRect>();
            quickGuideScroll.viewport = viewport;
            quickGuideScroll.content = bodyRect;
            quickGuideScroll.horizontal = false;
            quickGuideScroll.vertical = true;
            quickGuideScroll.movementType = ScrollRect.MovementType.Clamped;
            quickGuideScroll.inertia = false;
            quickGuideScroll.scrollSensitivity = 30f;

            var previous = ButtonAt("Previous manual page", frame.transform,
                new Vector2(-78f, -285f), new Vector2(148f, 35f),
                "UP", 14, Ink, Paper,
                () => MoveQuickGuidePage(1f), themed: false);
            quickGuidePreviousLabel = previous.GetComponentInChildren<Text>();
            var next = ButtonAt("Next manual page", frame.transform,
                new Vector2(78f, -285f), new Vector2(148f, 35f),
                "DOWN", 14, Ink, Paper,
                () => MoveQuickGuidePage(-1f), themed: false);
            quickGuideNextLabel = next.GetComponentInChildren<Text>();

            var close = ButtonAt("Close game manual", frame.transform,
                new Vector2(0f, -338f), new Vector2(302f, 43f),
                "CLOSE  ·  O", 17, Ink, Paper, ToggleQuickGuide,
                themed: false);
            var outline = close.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(1f, -1f);
            quickGuideCloseLabel = close.GetComponentInChildren<Text>();
            quickGuideRoot.gameObject.SetActive(false);
            RefreshQuickGuide();
        }

        private static void ManualEntry(StringBuilder page, bool chinese,
            string englishName, string englishExplanation,
            string chineseName, string chineseExplanation)
        {
            page.Append("<b>").Append(chinese ? chineseName : englishName)
                .Append(chinese ? "：</b>" : ":</b> ")
                .Append(chinese ? chineseExplanation : englishExplanation)
                .Append("\n\n");
        }

        private static void ManualSection(StringBuilder page, string title)
        {
            page.Append("<size=20><b>").Append(title)
                .Append("</b></size>\n\n");
        }

        private string GameManualText()
        {
            var page = new StringBuilder(6600);
            var zh = Chinese;
            var hand = run.UsesFillHand;
            var balanced = run.ScoringVersion >= 12;
            ManualSection(page, L("PLAY A TURN", "怎样玩一回合"));
            ManualEntry(page, zh, "Your aim",
                "Help wildlife find food while residents can work, eat and return home.",
                "游戏目标", "让动物能找到食物，也让居民能上班、用餐并回家。");

            if (hand)
            {
                ManualEntry(page, zh, "Place a card",
                    "Select a card, click a lit plot to preview, then confirm. Each confirmed placement settles wildlife, residents and scores. Fill all 49 plots, including the central oak, to finish.",
                    "放牌", "选一张牌，点亮格预览，再确认建造。每放一张结算一次动物、居民和分数。填满 49 格（含橡树）后结束。");
                ManualEntry(page, zh, "Eight room lessons",
                    "The guided opening introduces oak, home, restaurant, workshop, street, waste room, supermarket and square. Skip tutorial ends the fixed order immediately and deals a free hand.",
                    "认识八种地块", "引导依次介绍橡树、住宅、餐厅、工作坊、街道、垃圾间、超市和广场。点“跳过教程”会立即结束固定顺序，改为自由手牌。");
                ManualEntry(page, zh, "Hand after the lessons",
                    "Start with three cards. The deck replaces each one you play." +
                    (run.UsesFiniteFillDeck
                        ? " Your third and sixth built squares each add a slot, up to five cards."
                        : ""),
                    "自由手牌", "教程结束后从三张牌中选，打出一张补一张。" +
                    (run.UsesFiniteFillDeck
                        ? "建成第 3 座和第 6 座广场时各多一个手牌位，最多五张。"
                        : ""));
                ManualEntry(page, zh, "One shared deck",
                    "Green plots, streets and waste rooms are cards in the same deck as buildings. Select them from the hand below; playing one draws a replacement.",
                    "统一牌组", "绿地、街道、垃圾间与建筑都从底部手牌选择，打出一张后从同一牌堆补牌。");
                if (run.UsesFiniteFillDeck)
                    ManualEntry(page, zh, "Card swaps",
                        (run.FillHandStartingRerolls > 0
                            ? $"Start with {run.FillHandStartingRerolls} swaps. " : "") +
                        "The first meal for a squirrel, a resident pigeon and a hedgehog each earns one more. In free play, select a hand card and use Swap to draw a different type with a legal plot.",
                        "换牌", (run.FillHandStartingRerolls > 0
                            ? $"开局有 {run.FillHandStartingRerolls} 次换牌。" : "") +
                        "松鼠、常驻鸽子和刺猬首次进食各增加 1 次。自由建造时，选中手牌并点“换牌”，可换成有合法落点的另一类牌。");
            }
            else if (run.Mode == ConstructionGameMode.BoardGame)
                ManualEntry(page, zh, "Physical board",
                    "Start the camera with the four corner marks and central marker 34 in view. Place the physical tile, confirm its recognition, then use Next Day to settle the turn.",
                    "实体棋盘", "开启摄像头，让棋盘四角标记和中央 34 号标记同时入镜。放好实体方块，识别后确认建造，再点“下一天”结算。");
            else if (run.Mode == ConstructionGameMode.Endless)
            {
                ManualEntry(page, zh, "Build one room per day",
                    "Start at the central oak. Build one room before Next day becomes available. Use the four direction buttons or arrow keys to move the viewport; Latest room returns to your newest plot.",
                    "每轮建一格", "开局已有中央橡树。每轮先放置一格，才能点“下一天”。用棋盘四周的方向按钮或方向键移动视野；“回到最新房间”可回到最近建造处。");
                ManualEntry(page, zh, "Building advice",
                    "Suggestions use today's forecast for waste blockages, work, meals and wildlife food. Check the proposed plot before confirming.",
                    "建造建议", "界面根据今日预估的垃圾阻断、上班、用餐和动物食物推荐一张可放的牌。放置前仍可预览具体落点。");
                ManualEntry(page, zh, "Coexistence streak",
                    "A settled day counts when at least one animal eats and at least one resident works, eats and returns home. Aim for 1, 3 and 7 days in a row. After that the next target rises by 7 days. The best streak is kept if a day breaks the current one.",
                    "连续共栖", "动物实际进食、且至少一名居民完成上班、用餐、回家，当天才计入连日。先挑战连续 1、3、7 天，之后每次提高 7 天。中断只重置当前连日，最长纪录保留。");
                ManualEntry(page, zh, "Ending conditions",
                    $"The run ends if deaths in {ConstructionRunModel.AnimalDeathWindowDays} days reach your limit, or if enough residents miss work for {ConstructionRunModel.UnemployedConsecutiveDays} days in a row. It also ends if all residents leave, or squirrels or an arrived pigeon flock stay absent for five days.",
                    "结束条件", $"最近 {ConstructionRunModel.AnimalDeathWindowDays} 天动物死亡达到本局上限，或未上班人数连续 {ConstructionRunModel.UnemployedConsecutiveDays} 天达到本局阈值，都会结束本局。居民全部离开，或松鼠、已到来的鸽群连续 5 天消失，也会结束。");
            }
            else
            {
                ManualEntry(page, zh, "Build one room per day",
                    "Choose a room in Build, click a lit plot and confirm. Use Next Day to settle food, residents and scores.",
                    "每天建一格", "从建造栏选房间，点亮格预览并确认。点“下一天”结算动物进食、居民活动和得分。");
            }
            ManualEntry(page, zh, "Inspect and pause",
                "Hover over a built plot for its status. Select a home to set work and meals. Esc opens the pause menu.",
                "查看与暂停", "悬停已建格可看状态。点击住宅可安排工作与用餐。按 Esc 打开暂停菜单。");
            ManualEntry(page, zh, "Score index shortcuts",
                "H opens the last settled day's score index. Tab moves forward; Shift+Tab moves back. F centres an indexed plot in Endless. Esc closes the index. Coloured brackets identify the contributing rooms and visible actors during settlement and review.",
                "得分索引快捷键", "H 打开最近一次结算的得分索引。Tab 下一项，Shift+Tab 上一项；无尽模式下按 F 居中定位。Esc 关闭索引。结算和查看索引时，彩色边框会标出相关房间与仍在场的居民、动物。");

            ManualSection(page, L("ROOMS AND UPGRADES", "房间与升级"));
            ManualEntry(page, zh, "Shared edges and nearby plots",
                "Shared edges are above, below, left and right. Placement and resident routes use these connections. 'Surrounding plots' means all eight neighbours, including diagonals.",
                "共边和周围", "共边指上下左右，建造相连和居民路线都按共边判断。周围八格包含四个斜角。");
            if (hand || run.Mode == ConstructionGameMode.Endless)
                ManualEntry(page, zh, "Where to build",
                    "Jobs, meal services and streets need a shared edge with a home or its connected street. Waste rooms may also touch a restaurant." +
                    (hand ? " The lesson plaza needs a route from a home." : ""),
                    "合法落点", "工作坊、餐厅、超市和街道要贴住宅或与住宅接通的街道。垃圾间还可贴餐厅。" +
                    (hand ? "教程里的广场须接通住宅。" : ""));
            ManualEntry(page, zh, "Oak and green",
                run.UsesGreenUpgrades
                    ? "Edge-connected green plots reach level 2 at two plots and level 3 at three or more. Level is separate from maturity: each extra level adds one storage space per plot and one food on mature producing turns."
                    : "The oak starts the city. Green plots provide habitat and food for wildlife.",
                "橡树与绿地", run.UsesGreenUpgrades
                    ? "共边绿地连成两格升到 2 级，三格及以上升到 3 级。等级与成熟分开计算；每升一级，每格多存 1 份食物，成熟后的产食回合多产 1 份。"
                    : "橡树是城市起点。绿地为动物提供栖息和食物。");
            ManualEntry(page, zh, "When green matures",
                "New plots are planted on the build day, growing the next day and mature two days after planting (day 5 build → day 7 mature). The central oak is already mature. Meadows seed from the next day on alternate days; oaks and shrubs produce only when mature." +
                (run.HasVariableGreenYield
                    ? " From day 10, mature plots have an 80% harvest chance; check today's forecast."
                    : ""),
                "什么时候成熟", "建造当天为新植，次日生长，建造日加 2 天成熟（第 5 天建造→第 7 天成熟）。中央橡树直接成熟。草地次日起隔天产籽；橡树和灌木须成熟才产食。" +
                (run.HasVariableGreenYield ? "第 10 天起成熟绿地有 80% 收成机会，以今日预估为准。" : ""));
            ManualEntry(page, zh, "Green types follow the landing plot",
                "Types are not unlocked by waiting. After the opening oak, a new green is meadow until a meadow is reachable from the central oak. Then check shared-edge neighbours in order: any non-green building → meadow; central oak → oak; at least two greens → shrub; otherwise a meadow → oak; all other plots → meadow. The planted type stays the same as it matures.",
                "落点决定绿地类型", "类型不是随天数解锁。首格中央橡树之后，尚无可从中央橡树到达的草地时，新绿地形成草地。之后依次检查共边邻格：有非绿地建筑→草地；贴中央橡树→橡树；贴至少两格绿地→灌木；其余贴草地→橡树，否则→草地。建成后，成熟不会改变类型。");
            if (run.UsesGreenPatches)
                ManualEntry(page, zh, "Who lives in a green patch",
                    "A mature oak shelters squirrels. One meadow brings visiting pigeons. Two mature meadows in the same patch make room for resident pigeons. One mature shrub offers hedgehog habitat and two offer fox habitat.",
                    "动物住在哪里", "成熟橡树可供松鼠栖息。一块草地可供鸽子暂歇，同一片里有两块成熟草地才有常驻鸽子的住处。一块成熟灌木能给刺猬留出栖息地，两块可形成狐狸栖息地。");
            ManualEntry(page, zh, "Home",
                "Houses one resident. Click a built home, then Set work & meals to choose reachable destinations. Their daily loop is work, meal, then home.",
                "住宅", "每格容纳一名居民。点击已建住宅，再点左侧“安排工作与用餐”，选择可达的地点；居民的循环是上班、用餐、回家。");
            ManualEntry(page, zh, "Workshops, restaurants and supermarkets",
                "A workshop offers two jobs. A restaurant or supermarket offers one job and two meal seats.",
                "工作坊、餐厅、超市", "工作坊提供两个岗位；餐厅和超市各有一个岗位、两个餐位。");
            ManualEntry(page, zh, "What supermarket food does",
                "Each supermarket with a resident meal produces one temporary wildlife portion that day, regardless of diner count. Wildlife uses natural food first, then this food if still hungry and able to reach it. Each portion eaten removes one portion of supermarket waste. Unused food is treated as waste, not stored as tomorrow's food; food alone does not attract new animals.",
                "超市食物有什么用", "每家超市当天有人实际用餐，就留下 1 份临时动物食物，与用餐人数无关。动物先吃自然食物，仍饥饿且能到达时再取食。每吃掉 1 份，超市少留 1 份餐厨垃圾。未吃的按垃圾处理，不能留作隔日食物；留下食物不会自动引来新动物。");
            ManualEntry(page, zh, "Reaching temporary food",
                "Routes follow shared edges through green, streets, squares, supermarkets and waste rooms. Homes, workshops and restaurants block animal passage." +
                (run.ScoringVersion >= 7
                    ? run.UsesGreenPatches
                        ? " Squirrels and hedgehogs also need food within the eight plots around their habitat; pigeons can follow the route farther."
                        : " Squirrels also need food within the eight plots around their habitat; pigeons and hedgehogs can follow the route farther."
                    : ""),
                "动物怎样取食", "动物沿共边穿过绿地、街道、广场、超市和垃圾间；住宅、工作坊和餐厅不能通行。" +
                (run.ScoringVersion >= 7
                    ? run.UsesGreenPatches
                        ? "松鼠和刺猬还要求食源在其栖息格周围八格内，鸽子可沿通路到更远处。"
                        : "松鼠还要求食源在其栖息格周围八格内，鸽子和刺猬可沿通路到更远处。"
                    : ""));
            ManualEntry(page, zh, "Street and square",
                "Connected streets extend work, meal and waste collection routes. A visit to a square after a completed resident loop can leave food." +
                (run.ScoringVersion >= 8
                    ? " It also halves traffic risk on streets in the surrounding eight plots."
                    : ""),
                "街道和广场", "连通街道可延伸工作、用餐和清运路线。居民完成循环后到访广场，可留下动物食物。" +
                (run.ScoringVersion >= 8 ? "到访也会让广场周围八格内街道的车祸风险减半。" : ""));
            ManualEntry(page, zh, "Waste room", run.UsesUncappedWaste
                    ? "Clears all waste in the surrounding eight plots and in homes or food services reached by continuous shared-edge streets."
                    : "Clears waste up to its capacity in the surrounding eight plots and along continuous shared-edge streets.",
                "垃圾间", run.UsesUncappedWaste
                    ? "清运周围八格及连续共边街道接通的住宅与餐饮建筑里的全部垃圾。"
                    : "按容量清运周围八格及连续共边街道接通的住宅与餐饮建筑里的垃圾。");
            if (run.UsesNeighborUpgrades)
            {
                var garden = ConstructionPlacementScore.GardenResidencePoints;
                ManualEntry(page, zh, "Upgrade preview",
                    "The preview outlines existing plots in gold when they will upgrade. Confirm to activate them and update their art.",
                    "升级预览", "预放置时，用金边标出将升级的旧地块。确认建造后，升级生效并换成新插画。");
                ManualEntry(page, zh, "Garden home",
                    "A home sharing an edge with level 2 or 3 green becomes a garden home." +
                    (run.UsesPlacementScore ? $" The first upgrade gives +{garden} building points." : ""),
                    "花园住宅", "住宅与 2 级或 3 级绿地共边时换成花园住宅插画。" +
                    (run.UsesPlacementScore ? $"每栋首次升级加 {garden} 建造分。" : ""));
                ManualEntry(page, zh, "Terrace restaurant",
                    "A restaurant sharing an edge with a square offers three meal seats and one job.",
                    "露台餐厅", "餐厅与广场共边时，提供三个餐位和一个岗位。");
                ManualEntry(page, zh, "Slow street",
                    $"Green plots on opposite shared edges halve the risk (1/{ConstructionTrafficModel.ChanceDenominator} → 1/{ConstructionTrafficModel.ProtectedChanceDenominator}). If a visited square also protects it, use the same reduced chance.",
                    "慢行街", $"街道相对两侧都与绿地共边时，动物遇险概率从 1/{ConstructionTrafficModel.ChanceDenominator} 降到 1/{ConstructionTrafficModel.ProtectedChanceDenominator}。同时受广场保护时仍用这一个概率。");
            }

            ManualSection(page, L("SCORING AND GOALS", "结算、目标与奖杯"));
            ManualEntry(page, zh, "Residents",
                "Work, eat and return home to earn base points plus job, meal and route bonuses. Visiting a square adds one point.",
                "居民得分", "完成上班、用餐、回家的循环可得基础分，工作、用餐和路线还可加分；到访广场再加 1 分。");
            if (run.HasFedAnimalScore)
            {
                var meal = balanced ? ConstructionFoodAreaScorer.BalancedFedAnimalPoints :
                    ConstructionFoodAreaScorer.FedAnimalPoints;
                var green = ConstructionFoodAreaScorer.PointsForFedSource(
                    ConstructionCategory.Green, run.ScoringVersion >= 11, balanced);
                var urban = ConstructionFoodAreaScorer.FedUrbanSourcePoints;
                ManualEntry(page, zh, "Wildlife",
                    $"A squirrel, resident pigeon or hedgehog that eats scores {meal}. Each used green source scores +{green} and each used square or supermarket +{urban}, once per turn. Waste food adds 0 source points.",
                    "动物得分", $"松鼠、常驻鸽子和刺猬实际进食时，每只得 {meal} 分。每回合实际供食的绿地每格另加 {green} 分，广场或超市每格另加 {urban} 分。垃圾食源本身加 0 分。");
            }
            else
                ManualEntry(page, zh, "Wildlife",
                    "Wildlife needs a usable route to reach food and eat. Check the score details for the current scoring rules.",
                    "动物与食物", "动物须能到达食源才能进食。本局计分方式可在得分详情查看。");
            if (run.HasLinkageScore)
                ManualEntry(page, zh, "Coexistence",
                    "Successful resident loops and wildlife feeding can earn linkage bonuses when they share a square, supermarket or nearby green source.",
                    "共栖联动", "居民完成循环、动物实际进食时，共用的广场、超市或附近绿地有机会带来联动分。");

            if (run.UsesPlacementScore)
            {
                if (run.ScoringVersion >= 13)
                    ManualEntry(page, zh, "Layout quality",
                        "Each shared edge scores once: home–workshop or home–restaurant +24; home–green, home–market or home–square +16; green–green, green–market or green–square +12. Negative build points remain in the final total. These rules also apply in new Endless games; older saves keep their original scoring version.",
                        "布局质量", "每条共边只计一次：住宅配工作坊/餐厅 +24；住宅配绿地/超市/广场 +16；绿地配绿地/超市/广场 +12。建造扣分直接计入最终总分，允许负分；新无尽对局也采用此规则，旧存档保留原计分版本。");
                var two = ConstructionPlacementScore.TwoGreenMilestonePoints;
                var diverse = ConstructionPlacementScore.DiverseGreenMilestonePoints;
                ManualEntry(page, zh, "Building points", balanced
                        ? $"Pairing points score once per shared edge. A green patch earns {two} when it first reaches two plots and {diverse} when it first has at least three plots and two plant types."
                        : "A new plot scores once for pairings in the eight surrounding plots; poor pairings deduct points.",
                    "建造得分", balanced
                        ? $"每条共边只结算一次搭配分。每片绿地首次达到两格加 {two}，首次达到至少三格且有两种植物再加 {diverse}。"
                        : "新地块与周围八格结算一次搭配分；不合适的搭配也会扣分。");
                if (run.UsesCompleteNeighborhoods)
                {
                    var award = balanced ? ConstructionPlacementScore.CommunityPoints(run.PlacementRulesVersion) :
                        ConstructionPlacementScore.CompleteNeighborhoodPoints;
                    ManualEntry(page, zh, "Complete neighbourhood bonus",
                        "A home needs a workshop, restaurant, supermarket, square and two green plant types in its surrounding eight plots. " +
                        (balanced ? "Once its resident completes the daily loop, " : "") +
                        $"that home earns {award} once.",
                        "配套街坊加成", "住宅周围八格有工作坊、餐厅、超市、广场和两种植物的绿地。" +
                        (balanced ? "居民实际完成循环后，" : "") + $"每栋只得一次 +{award}。");
                }
                if (balanced)
                {
                    var street = -ConstructionPlacementScore.PairPoints(ConstructionCategory.Street,
                        ConstructionCategory.Green, run.PlacementRulesVersion);
                    var waste = -ConstructionPlacementScore.PairPoints(ConstructionCategory.Waste,
                        ConstructionCategory.Residence, run.PlacementRulesVersion);
                    ManualEntry(page, zh, "Bad pairings",
                        $"Street beside green: −{street} per shared edge, including slow streets. Waste room beside a home: −{waste} per shared edge.",
                        "不良搭配", $"街道与绿地每条共边扣 {street} 分，慢行街同样计入。垃圾间与住宅每条共边扣 {waste} 分。");
                }
            }
            if (run.HasTrafficPenaltyScore)
                ManualEntry(page, zh, "Traffic",
                    $"A collision deducts {run.CollisionPenalty} points. There can be at most one fatal collision per settlement.",
                    "交通事故", $"每次车祸扣 {run.CollisionPenalty} 分；每次结算最多发生一起致死车祸。");
            if (run.UsesSpecialTasks)
            {
                ManualEntry(page, zh, "Goal cards",
                    "Three goal cards are dealt at the start. You can replace each once before confirming. " +
                    (run.UsesPostTutorialTaskRules ? "Goal progress starts in free play, after the eight lessons. " : "") +
                    "Completion grants the printed reward and activates the effect. Some effects score at the end.",
                    "目标牌", "开局发三张，每张可换一次，确认后锁定。" +
                    (run.UsesPostTutorialTaskRules ? "八张教程牌结算完、进入自由建造才开始推进目标。" : "") +
                    "达成时获得卡面奖励并激活效果，部分效果在终局加分。");
                var effectLimits = run.SpecialTasks;
                if (effectLimits.Count > 0 && effectLimits[0].EffectLimit > 0)
                {
                    var easy = ConstructionRunModel.BalancedTaskEffectLimit(ConstructionSpecialTaskDifficulty.Easy);
                    var medium = ConstructionRunModel.BalancedTaskEffectLimit(ConstructionSpecialTaskDifficulty.Medium);
                    var hard = ConstructionRunModel.BalancedTaskEffectLimit(ConstructionSpecialTaskDifficulty.Hard);
                    ManualEntry(page, zh, "Goal effect limits",
                        $"A completed easy, medium or hard card can earn up to {easy}, {medium} or {hard} extra points from its effect.",
                        "目标效果上限", $"达成后，简单／中等／困难牌通过效果还可分别得最多 {easy}／{medium}／{hard} 分。");
                }
                ManualEntry(page, zh, "Nine-plot goal",
                    "Keep the resident's completed route inside the 3 × 3 area centred on home. Green plots may repeat there. Each other room type appears at most once.",
                    "九格街坊目标", "让居民完成的整条路线留在以住宅为中心的九格内。绿地可以重复，其它房间每类最多一格。");
            }
            if (run.Mode == ConstructionGameMode.Fill &&
                ConstructionAwardModel.SupportsScoringVersion(run.ScoringVersion))
            {
                var iron = AwardMinimum(ConstructionAwardTier.Iron);
                var bronze = AwardMinimum(ConstructionAwardTier.Bronze);
                var silver = AwardMinimum(ConstructionAwardTier.Silver);
                var gold = AwardMinimum(ConstructionAwardTier.Gold);
                ManualEntry(page, zh, "Trophy and ranking",
                    run.UsesSpecialTasks
                        ? "After completing the board, the trophy and ranking use your final total: coexistence, construction and goal points, including end effects."
                        : "After completing the board, the trophy and ranking use your final coexistence score.",
                    "奖杯与排行", run.UsesSpecialTasks
                        ? "填满棋盘后，用总分评奖杯和排行。总分包含共栖、建造、目标，以及目标的终局加分。"
                        : "填满棋盘后按最终共栖分评定奖杯与排行。");
                ManualEntry(page, zh, "Trophy bands",
                    $"Plastic below {iron}; iron {iron}–{bronze - 1}; bronze {bronze}–{silver - 1}; silver {silver}–{gold - 1}; gold {gold}+.",
                    "奖杯分段", $"塑料低于 {iron}；铁 {iron}–{bronze - 1}；铜 {bronze}–{silver - 1}；银 {silver}–{gold - 1}；金 {gold} 及以上。");
            }
            return page.ToString();
        }

        private void RefreshQuickGuide()
        {
            if (quickGuideRoot == null || run == null) return;
            var blocked = run.IsFinished || !IsBoardCaseOpen ||
                specialTaskSelectionRoot != null &&
                    specialTaskSelectionRoot.gameObject.activeSelf ||
                endlessRulesOverlay != null && endlessRulesOverlay.activeSelf;
            if (blocked && QuickGuideOpen)
                quickGuideRoot.gameObject.SetActive(false);
            if (quickGuideButton != null)
            {
                quickGuideButton.gameObject.SetActive(!blocked);
                quickGuideButtonLabel.text = L("Manual", "手册");
            }
            quickGuideHeading.text = L("GAME MANUAL", "游戏手册");
            quickGuideSubtitle.text = L("Scroll to read · O to close", "滚轮翻阅　O 收起");
            quickGuidePreviousLabel.text = L("UP", "向上");
            quickGuideNextLabel.text = L("DOWN", "向下");
            quickGuideCloseLabel.text = L("CLOSE  ·  O", "收起手册  ·  O");
            if (!quickGuideBodyReady || quickGuideBodyChinese != Chinese ||
                quickGuideBodyMode != run.Mode ||
                quickGuideBodyUsesHand != run.UsesFillHand ||
                quickGuideBodyRunId != run.RunId)
            {
                var font = Resources.Load<TMP_FontAsset>(Chinese
                    ? "Fonts/TMP/NotoSansSC-Regular SDF"
                    : "Fonts/TMP/Nunito-Regular SDF");
                quickGuideBody.font = font != null ? font : TMP_Settings.defaultFontAsset;
                quickGuideBody.text = GameManualText();
                quickGuideBodyChinese = Chinese;
                quickGuideBodyMode = run.Mode;
                quickGuideBodyUsesHand = run.UsesFillHand;
                quickGuideBodyRunId = run.RunId;
                quickGuideBodyReady = true;
                var preferredHeight = quickGuideBody.GetPreferredValues(
                    quickGuideBody.text, ManualBodyWidth, 0f).y;
                quickGuideBody.rectTransform.sizeDelta = new Vector2(0f,
                    Mathf.Max(ManualBodyHeight, Mathf.Ceil(preferredHeight + 24f)));
                Canvas.ForceUpdateCanvases();
                quickGuideScroll.StopMovement();
                quickGuideScroll.verticalNormalizedPosition = 1f;
            }
            if (QuickGuideOpen) quickGuideRoot.SetAsLastSibling();
        }

        private void MoveQuickGuidePage(float direction)
        {
            if (quickGuideScroll == null) return;
            Canvas.ForceUpdateCanvases();
            var overflow = quickGuideScroll.content.rect.height -
                quickGuideScroll.viewport.rect.height;
            if (overflow <= 0f) return;
            var visible = quickGuideScroll.viewport.rect.height;
            var step = (visible - ManualPageOverlap) / overflow;
            quickGuideScroll.StopMovement();
            quickGuideScroll.verticalNormalizedPosition = Mathf.Clamp01(
                quickGuideScroll.verticalNormalizedPosition + direction * step);
        }

        private void ToggleQuickGuide()
        {
            if (quickGuideRoot == null || run == null || run.IsFinished ||
                !IsBoardCaseOpen || PauseOpen ||
                specialTaskSelectionRoot != null &&
                    specialTaskSelectionRoot.gameObject.activeSelf ||
                endlessRulesOverlay != null && endlessRulesOverlay.activeSelf)
                return;
            quickGuideRoot.gameObject.SetActive(!QuickGuideOpen);
            HideTooltip();
            RefreshQuickGuide();
            RefreshOpeningTour();
            RefreshTutorialFocus();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
        }

        // The opening lesson already points at the first action. The manual is
        // an on-demand reference and does not open over that lesson.
        private void TryShowOpeningQuickGuide() { }
    }
}
