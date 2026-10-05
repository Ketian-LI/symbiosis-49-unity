using System;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public readonly struct GameplayGuidePage
    {
        public GameplayGuidePage(string title, string body, RoomType? roomType = null,
            EcologicalMetricKind? metricKind = null, bool deathLimit = false)
        {
            Title = title;
            Body = body;
            RoomType = roomType;
            MetricKind = metricKind;
            DeathLimit = deathLimit;
        }

        public string Title { get; }
        public string Body { get; }
        public RoomType? RoomType { get; }
        public EcologicalMetricKind? MetricKind { get; }
        public bool DeathLimit { get; }
    }

    // The area pages are built from the actual board data, so newly added rooms
    // cannot silently disappear from the tutorial's room list.
    public static class GameplayGuideCatalog
    {
        private static readonly RoomType[] AreaOrder =
        {
            RoomType.CentralPark, RoomType.Residence, RoomType.Office,
            RoomType.Canteen, RoomType.Supermarket, RoomType.Garage,
            RoomType.Trash, RoomType.PigeonHabitat, RoomType.OakHabitat,
            RoomType.ShrubHabitat, RoomType.FoxDen, RoomType.SharedSpace,
            RoomType.EcologicalBuffer, RoomType.CommunitySquare
        };

        private const int AreaPageStart = 9;
        public static int PageCount => AreaPageStart + AreaOrder.Length;

        public static GameplayGuidePage GetPage(int index, bool chinese)
        {
            if (index < 0 || index >= PageCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (index == 0)
            {
                return chinese
                    ? new GameplayGuidePage("你在经营什么？",
                        "这是一张 7×7、由 49 间单格房组成的城市生态地图。移动房间，连接动物栖息地与自然食物，同时保住居民的上班、吃饭、回家路线。连续 3 天无法完成循环的居民会离开；总人数低于 3 人，或动物死亡达到上限，本局结束。投喂只能暂时救急。")
                    : new GameplayGuidePage("What are you managing?",
                        "This 7×7 city ecosystem has 49 single-cell rooms. Move rooms so wildlife can reach natural food while residents retain a home–work–meal–home route. A resident leaves after three failed days; the run ends below three residents or at the animal death limit. Placed food is temporary emergency aid.");
            }
            if (index == 1)
            {
                return chinese
                    ? new GameplayGuidePage("时间怎样推动游戏？",
                        "顶部圆盘显示天数与时段。每天须选择一次有效行动：调整房间或成功投喂，才能跨天或跳日；两者当天不可兼做。投喂仍每 3 天可用一次。日终检查居民上班→吃饭→回家的循环；连续 3 天失败会离开，少于 3 人则结束。垃圾隔日清运；商铺从第 4 天轮值，第 7 天起每 4 天有大型集市。教程与布局编辑暂停时间。")
                    : new GameplayGuidePage("How does the simulation run?",
                        "Each day needs one completed action before dawn or skip: rearrange rooms or place food, never both that day. Feeding still has a three-day cooldown. Residents need a full work–meal–home trip; three failed days make them leave, and fewer than three ends the run. Waste is collected every other night. Shops rotate from day 4; large markets recur every four days from day 7. Tutorial and layout edit pause time.");
            }
            if (index == 2)
            {
                return chinese
                    ? new GameplayGuidePage("界面从哪里读起？",
                        "顶部圆盘看天数与速度；左上看可完成通勤的人数。居民头顶三格依次为上班、吃饭、回家：绿表示当前可达（亮绿已走完），红表示路线或名额有缺口。动物三格随饥饿天数减少：有足够且可达的现存食物为绿，否则为红。左侧预警框指出需关注的房间。绿色不保证及时进食，也不排除车祸或捕食。")
                    : new GameplayGuidePage("How do you read the screen?",
                        "The dial shows day and speed; the upper-left count shows viable commutes. Resident cells mean work, meal, home: green is reachable (bright green when completed), red flags a route or capacity gap. Animal cells shrink with hungry days; green means enough current food is within range, red means an access gap. The left warning card names a room to check. Green never guarantees arrival or safety from traffic and predation.");
            }
            if (index == 3)
            {
                return chinese
                    ? new GameplayGuidePage("居民人数",
                        "第一个圆环表示居民人数：4/8 即当前 4 人、上限 8 人。居民需完成住宅→办公室→餐饮→住宅的路线；连续 3 天未完成会离开，总人数低于 3 人才结束。",
                        metricKind: EcologicalMetricKind.HumanFunction)
                    : new GameplayGuidePage("Resident count",
                        "The first ring shows residents: 4/8 means four people out of eight. Each needs a home–office–meal–home route. Three consecutive failures cause departure; the run ends only when the total resident count falls below three.",
                        metricKind: EcologicalMetricKind.HumanFunction);
            }
            if (index == 4)
            {
                return chinese
                    ? new GameplayGuidePage("今日进食",
                        "第二个圆环显示今日已进食／存活动物；悬停查看剩余食物份数。布局预览的鸽餐上限不保证进食，次日回顾显示实际结果。米色是人行路，青色是动物通道；极饿时可借人行门，封闭口仍不能走。鸽群房须连到绿地才补种子，4 格连通绿地才有额外增产。投喂只供救急。",
                        metricKind: EcologicalMetricKind.FoodAccessibility)
                    : new GameplayGuidePage("Meals today",
                        "Ring two shows animals fed today / living animals; hover for food left. The layout's pigeon meal ceiling is not a guaranteed meal; next-day review shows what happened. Beige is pedestrian, teal wildlife-only. On the third hungry day animals may borrow pedestrian doors, never blocked exits. Pigeon plazas need a green link for seeds; four linked green cells add a bonus. Feeding is emergency aid.",
                        metricKind: EcologicalMetricKind.FoodAccessibility);
            }
            if (index == 5)
            {
                return chinese
                    ? new GameplayGuidePage("庇护指数",
                        "第三个圆环是规则设定的庇护指数，不是可容纳动物的数量：鸽群广场、公园、灌木和狐狸洞提供基础 75%；四处成熟橡树最多补足 25%。鸽群广场可在布局编辑中整体移动，幼树或受损橡树不算成熟。",
                        metricKind: EcologicalMetricKind.HabitatProvision)
                    : new GameplayGuidePage("Shelter index",
                        "The third ring is a rule-based shelter index, not animal capacity. Pigeon plazas, park cover, shrubs and the fox den provide a 75% base; four mature oaks can add the final 25%. Pigeon plazas can move as whole rooms in layout edit. Young or damaged oaks do not count as mature.",
                        metricKind: EcologicalMetricKind.HabitatProvision);
            }
            if (index == 6)
            {
                return chinese
                    ? new GameplayGuidePage("存活动物",
                        "第四个圆环直接表示当前存活动物数/动物个体总数，左下头像分别显示鸽子、松鼠、刺猬和狐狸的存活数。动物会因饥饿、交通或捕食死亡，也可能在之后重生；累计死亡不会因此减少，另见最下方圆环。",
                        metricKind: EcologicalMetricKind.AnimalSafety)
                    : new GameplayGuidePage("Living animals",
                        "The fourth ring directly shows living animals out of all animal slots. The portraits at bottom left show pigeons, squirrels, hedgehogs and foxes separately. Animals can die from hunger, traffic or predation and later respawn; cumulative deaths do not fall, as shown in the bottom ring.",
                        metricKind: EcologicalMetricKind.AnimalSafety);
            }
            if (index == 7)
            {
                return chinese
                    ? new GameplayGuidePage("动物死亡上限",
                        "最下方心电图圆环表示本局累计死亡数相对死亡上限。无尽模式累计 3 次死亡就结束，越满越危险。死亡可能来自饥饿、交通或狐狸捕食；动物重生也不会抹去记录。悬停心电图圆环或左下动物头像可查看各类动物的死亡原因。",
                        deathLimit: true)
                    : new GameplayGuidePage("Animal death limit",
                        "The bottom heartbeat ring tracks cumulative deaths. Endless Mode ends at three deaths; a fuller ring means greater danger. Starvation, traffic and fox predation are risks, and respawns do not erase deaths. Hover over the ring or an animal portrait at bottom left to see deaths by species and cause.",
                        deathLimit: true);
            }
            if (index == 8)
            {
                return chinese
                    ? new GameplayGuidePage("你可以怎样干预？",
                        "从第 2 天起，每天有一处提前预告的动物通道事件；花园养护可能暂时关闭整间房的动物出口。每天只选一种行动：免费调整最多 3 间房（含移动或旋转），或成功投喂一次；投喂每 3 天才恢复。布局编辑可看青色通道与橙色受扰端点，按 R 可旋转通道。只查看或取消投喂不算行动。")
                    : new GameplayGuidePage("How can you intervene?",
                        "From day 2, an animal-route event is announced ahead; maintenance may close a room's animal exits. Choose just one action each day: a free move or rotation affecting up to three rooms, or one successful feeding. Feeding returns every three days. Layout edit shows teal routes and amber blocked doors; hold a room and press R to rotate. Reviewing or cancelling feed does not count.");
            }

            var type = AreaOrder[index - AreaPageStart];
            var rooms = RoomLayoutData.All.Where(room => room.Type == type).ToArray();
            if (type == RoomType.SharedSpace)
            {
                return chinese
                    ? new GameplayGuidePage("公共空间",
                        $"地图上有 {rooms.Length} 间单格公共空间。动物专用通道有直路、转角、丁字路、十字路和独头路；人只能走米色人行口，不能走青色动物口。其中 4 间是可移动绿地：连接中央公园达到 4 格后，松鼠林地与刺猬花园可供食。", type)
                    : new GameplayGuidePage("Shared spaces",
                        $"The map has {rooms.Length} one-cell shared spaces with straight, corner, T, cross and dead-end animal paths. People use beige pedestrian entrances, never teal wildlife-only ones. Four are movable green cells; link at least four cells to the park to activate the squirrel grove and hedgehog garden food sources.", type);
            }
            var names = chinese
                ? string.Join("、", rooms.Select(room => room.DisplayName))
                : string.Join(", ", rooms.Select(room => EnglishName(type, room.Id)));
            var count = rooms.Length;
            var title = chinese ? ChineseTitle(type) : EnglishTitle(type);
            var function = chinese ? ChineseFunction(type) : EnglishFunction(type);
            var movement = rooms.All(room => !room.Movable)
                ? chinese ? "这些区域固定，不能放进布局托盘。" : "These areas are fixed and cannot go into the layout tray."
                : chinese ? "其中可移动的房间可在布局编辑中调整。" : "Movable rooms can be rearranged in layout edit.";
            var body = chinese
                ? $"地图上的{title}（{count}）：{names}。\n{function} {movement}"
                : $"{title} on the map ({count}): {names}.\n{function} {movement}";
            return new GameplayGuidePage(title, body, type);
        }

        private static string ChineseTitle(RoomType type) => type switch
        {
            RoomType.CentralPark => "中央公园",
            RoomType.Residence => "住宅",
            RoomType.Office => "办公室",
            RoomType.Canteen => "餐饮商铺",
            RoomType.Supermarket => "小型超市",
            RoomType.Garage => "车库",
            RoomType.Trash => "垃圾房",
            RoomType.PigeonHabitat => "鸽群空间",
            RoomType.OakHabitat => "橡树",
            RoomType.ShrubHabitat => "灌木",
            RoomType.FoxDen => "狐狸洞",
            RoomType.SharedSpace => "公共空间",
            RoomType.EcologicalBuffer => "生态缓冲庭",
            RoomType.CommunitySquare => "社区活动场",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        private static string EnglishTitle(RoomType type) => type switch
        {
            RoomType.CentralPark => "Central park",
            RoomType.Residence => "Residences",
            RoomType.Office => "Offices",
            RoomType.Canteen => "Food shops",
            RoomType.Supermarket => "Supermarket",
            RoomType.Garage => "Garages",
            RoomType.Trash => "Waste rooms",
            RoomType.PigeonHabitat => "Pigeon spaces",
            RoomType.OakHabitat => "Oak habitats",
            RoomType.ShrubHabitat => "Shrub habitats",
            RoomType.FoxDen => "Fox den",
            RoomType.SharedSpace => "Shared spaces",
            RoomType.EcologicalBuffer => "Ecological buffer",
            RoomType.CommunitySquare => "Community square",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        private static string ChineseFunction(RoomType type) => type switch
        {
            RoomType.CentralPark => "固定公园是绿地网络起点与鸽群落脚处；园内免于捕食，边缘鸽群房轮休。",
            RoomType.Residence => "居民的住处与日常活动起点；他们会从这里前往工作和食物地点。",
            RoomType.Office => "居民的工作目的地；需连通餐饮地点和住宅，三段循环连续 3 天失败的居民会离开。",
            RoomType.Canteen => "为居民提供餐饮，也可能吸引觅食动物；居民完成上班路线仍需能到达餐饮点。",
            RoomType.Supermarket => "居民购买食物的目的地，包装食物也可能影响动物觅食。",
            RoomType.Garage => "车辆出入口；动物穿越附近路线时要留意碰撞风险。",
            RoomType.Trash => "垃圾在这里积累，有容量上限且定期清运；也会成为动物的食物热点。",
            RoomType.PigeonHabitat => "鸽群休息和聚集的区域；观察它们往返食物地点。",
            RoomType.OakHabitat => "给松鼠提供庇护和自然食物；幼树与受损树木会呈现不同恢复状态。",
            RoomType.ShrubHabitat => "给刺猬提供庇护与相对安全的夜间路径。",
            RoomType.FoxDen => "保留的固定狐狸隐蔽点；狐狸目前从可移动的狐狸边缘栖地出发。",
            RoomType.SharedSpace => "可移动的通行空间；不会自行产出食物，也不提供新的动物栖息位，但能拼接动物通道。",
            RoomType.EcologicalBuffer => "与车库相邻时，动物穿越该车库的致命风险从 50% 降至 25%；不消除风险，也不产食物。",
            RoomType.CommunitySquare => "同时邻接同一办公室与餐饮商铺时，可把通往该办公室的可行路程延长一步，并改善较长路线的通勤效率；活动日东侧动物通道会受扰。",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        private static string EnglishFunction(RoomType type) => type switch
        {
            RoomType.CentralPark => "The fixed park anchors the green network and hosts pigeons. No hunting inside; edge pigeon plots take turns resting.",
            RoomType.Residence => "Residents' homes and starting points for work and food trips.",
            RoomType.Office => "Work destinations; each resident needs a route onward to a meal and back home. Three failed daily loops cause departure.",
            RoomType.Canteen => "Food service for residents and a possible wildlife food source; workers also need a route here.",
            RoomType.Supermarket => "A resident food destination; packaged food may affect wildlife foraging.",
            RoomType.Garage => "Vehicle entrances: watch collision risk where animals cross.",
            RoomType.Trash => "Waste accumulates to a capacity limit and is collected periodically; animals may forage here.",
            RoomType.PigeonHabitat => "Resting and gathering spaces for pigeons travelling to food.",
            RoomType.OakHabitat => "Shelter and natural food for squirrels; young or damaged trees have recovery states.",
            RoomType.ShrubHabitat => "Shelter and lower-risk night routes for hedgehogs.",
            RoomType.FoxDen => "A retained fixed fox shelter. The fox currently starts from the movable edge refuge.",
            RoomType.SharedSpace => "Movable passage spaces. They produce no food and add no animal homes, but can join animal routes.",
            RoomType.EcologicalBuffer => "Beside a garage, reduces the fatal chance for animals crossing that garage from 50% to 25%. Risk remains, and it produces no food.",
            RoomType.CommunitySquare => "When the same square borders an office and food shop, it extends the viable trip to work by one step and improves long commutes. Its east animal passage is disturbed on event days.",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        private static string EnglishName(RoomType type, string id)
        {
            var baseName = type switch
            {
                RoomType.CentralPark => "Central Park",
                RoomType.Residence => "Residence",
                RoomType.Office => "Office",
                RoomType.Canteen => "Food Shop",
                RoomType.Supermarket => "Supermarket",
                RoomType.Garage => "Garage",
                RoomType.Trash => "Waste",
                RoomType.PigeonHabitat => "Pigeon",
                RoomType.OakHabitat => "Oak",
                RoomType.ShrubHabitat => "Shrub",
                RoomType.FoxDen => "Fox Den",
                RoomType.SharedSpace => "Shared Space",
                RoomType.EcologicalBuffer => "Ecological Buffer",
                RoomType.CommunitySquare => "Community Square",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
            var suffix = id.Length > 1 && id[id.Length - 2] == '-'
                ? $" {char.ToUpperInvariant(id[id.Length - 1])}"
                : string.Empty;
            return baseName + suffix;
        }
    }
}
