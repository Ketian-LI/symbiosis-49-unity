using System.Collections.Generic;
using System.Linq;

namespace UrbanWildlifeRooms.Data
{
    public static class RoomLayoutData
    {
        public const int GridSize = 7;

        private static readonly RoomSpec[] Rooms =
        {
            new("garage-a", "车库 A", RoomType.Garage, 0, 0, 1, 1, true, "车辆出入口与动物碰撞风险"),
            new("shared-a", "车库前庭", RoomType.SharedSpace, 1, 0, 1, 1, true, "可移动的公共通行空间；通过动物通道连接相邻房间"),
            new("residence-a", "住宅 A", RoomType.Residence, 2, 0, 1, 1, true, "居民休息与日常活动"),
            new("shared-b", "住宅庭院", RoomType.SharedSpace, 3, 0, 1, 1, true, "可移动的公共通行空间；通过动物通道连接相邻房间"),
            new("oak-a", "橡树 A", RoomType.OakHabitat, 4, 0, 1, 1, true, "松鼠庇护与自然食物"),
            new("shared-c", "林荫空地", RoomType.SharedSpace, 5, 0, 1, 1, true, "可移动的公共通行空间；通过动物通道连接相邻房间"),
            new("trash-a", "垃圾 A", RoomType.Trash, 6, 0, 1, 1, true, "垃圾收集与动物食物热点"),

            new("pigeon-a", "鸽群 A", RoomType.PigeonHabitat, 0, 1, 1, 1, true, "鸽群休息与聚集空间"),
            new("supermarket", "小型超市", RoomType.Supermarket, 1, 1, 1, 1, true, "人类目的地与包装食物来源"),
            new("shared-d", "超市外广场", RoomType.SharedSpace, 2, 1, 1, 1, true, "可移动的公共通行空间；通过动物通道连接相邻房间"),
            new("office-a", "办公室 A", RoomType.Office, 3, 1, 1, 1, true, "工作人流目的地"),
            new("trash-b", "垃圾 B", RoomType.Trash, 4, 1, 1, 1, true, "垃圾收集与动物食物热点"),
            new("residence-c", "住宅 C", RoomType.Residence, 5, 1, 1, 1, true, "居民休息与日常活动"),
            new("garage-b", "车库 B", RoomType.Garage, 6, 1, 1, 1, true, "车辆出入口与动物碰撞风险"),

            new("shared-e", "鸽群前广场", RoomType.SharedSpace, 0, 2, 1, 1, true, "可移动的公共通行空间；通过动物通道连接相邻房间"),
            new("pigeon-b", "鸽群 B", RoomType.PigeonHabitat, 1, 2, 1, 1, true, "鸽群休息与聚集空间"),
            new("central-park", "中央公园", RoomType.CentralPark, 2, 2, 1, 1, false, "固定公共空间、投喂与多物种避难"),
            new("shared-f", "松鼠林地", RoomType.SharedSpace, 3, 2, 1, 1, true, "可移动绿地；松鼠坚果点与动物通道", greenRole: ParkGreenRole.SquirrelGrove),
            new("pigeon-c", "鸽群 C", RoomType.PigeonHabitat, 4, 2, 1, 1, true, "鸽群休息与聚集空间"),
            new("office-b", "办公室 B", RoomType.Office, 5, 2, 1, 1, true, "工作人流目的地"),
            new("shared-g", "生态缓冲庭", RoomType.EcologicalBuffer, 6, 2, 1, 1, true, "紧邻车库时降低动物穿越该车库的致命风险；自身不产食物"),

            new("residence-g", "住宅 G", RoomType.Residence, 0, 3, 1, 1, true, "居民休息与日常活动"),
            new("oak-c", "橡树 C", RoomType.OakHabitat, 1, 3, 1, 1, true, "开局幼树，用于展示生态恢复", true),
            new("shared-h", "刺猬花园", RoomType.SharedSpace, 2, 3, 1, 1, true, "可移动绿地；刺猬昆虫点与动物通道", greenRole: ParkGreenRole.HedgehogGarden),
            new("shared-i", "绿地连接庭", RoomType.SharedSpace, 3, 3, 1, 1, true, "可移动的绿地连接空间；自身不产食物", greenRole: ParkGreenRole.Connector),
            new("residence-d", "住宅 D", RoomType.Residence, 4, 3, 1, 1, true, "居民休息与日常活动"),
            new("trash-c", "垃圾 C", RoomType.Trash, 5, 3, 1, 1, true, "垃圾收集与动物食物热点"),
            new("oak-b", "橡树 B", RoomType.OakHabitat, 6, 3, 1, 1, true, "松鼠庇护与自然食物"),

            new("residence-h", "住宅 H", RoomType.Residence, 0, 4, 1, 1, true, "居民休息与日常活动"),
            new("residence-b", "住宅 B", RoomType.Residence, 1, 4, 1, 1, true, "居民休息与日常活动"),
            new("shared-j", "狐狸边缘栖地", RoomType.SharedSpace, 2, 4, 1, 1, true, "可移动绿地；狐狸的外缘庇护，不直接产食物", greenRole: ParkGreenRole.FoxEdge),
            new("canteen-b", "餐饮商铺 B", RoomType.Canteen, 3, 4, 1, 1, true, "人类用餐服务与动物食物来源"),
            new("shared-k", "社区活动场", RoomType.CommunitySquare, 4, 4, 1, 1, true, "同时邻接办公室与餐饮商铺时延长一步可行上班路程；活动日东侧动物通道受扰"),
            new("shrub-a", "灌木 A", RoomType.ShrubHabitat, 5, 4, 1, 1, true, "刺猬庇护与夜间低风险路径"),
            new("shared-l", "橡树下空地", RoomType.SharedSpace, 6, 4, 1, 1, true, "可移动的公共通行空间；通过动物通道连接相邻房间"),

            new("residence-e", "住宅 E", RoomType.Residence, 0, 5, 1, 1, true, "居民休息与日常活动"),
            new("residence-f", "住宅 F", RoomType.Residence, 1, 5, 1, 1, true, "居民休息与日常活动"),
            new("canteen-a", "餐饮商铺 A", RoomType.Canteen, 2, 5, 1, 1, true, "人类用餐服务与动物食物来源"),
            new("office-c", "办公室 C", RoomType.Office, 3, 5, 1, 1, true, "工作人流目的地"),
            new("trash-d", "垃圾 D", RoomType.Trash, 4, 5, 1, 1, true, "垃圾收集与动物食物热点"),
            new("pigeon-d", "鸽群 D", RoomType.PigeonHabitat, 5, 5, 1, 1, true, "鸽群休息与聚集空间"),
            new("shrub-b", "灌木 B", RoomType.ShrubHabitat, 6, 5, 1, 1, true, "刺猬庇护与夜间低风险路径"),

            new("garage-c", "车库 C", RoomType.Garage, 0, 6, 1, 1, true, "车辆出入口与动物碰撞风险"),
            new("shared-m", "车库侧广场", RoomType.SharedSpace, 1, 6, 1, 1, true, "可移动的公共通行空间；通过动物通道连接相邻房间"),
            new("shared-n", "餐饮露台 A", RoomType.SharedSpace, 2, 6, 1, 1, true, "可移动的公共通行空间；通过动物通道连接相邻房间"),
            new("office-d", "办公室 D", RoomType.Office, 3, 6, 1, 1, true, "居民工作目的地与通勤路线"),
            new("oak-d", "橡树 D", RoomType.OakHabitat, 4, 6, 1, 1, true, "松鼠庇护与自然食物"),
            new("shrub-c", "灌木 C", RoomType.ShrubHabitat, 5, 6, 1, 1, true, "刺猬庇护与夜间低风险路径"),
            new("fox-den", "狐狸洞", RoomType.FoxDen, 6, 6, 1, 1, false, "保留的固定狐狸隐蔽点；狐狸初始位于可移动的边缘栖地")
        };

        public static IReadOnlyList<RoomSpec> All => Rooms;

        public static IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            var occupancy = new string[GridSize, GridSize];

            foreach (var room in Rooms)
            {
                if (room.Width < 1 || room.Height < 1)
                {
                    errors.Add($"{room.Id}: 房间尺寸必须为正数。");
                    continue;
                }

                if (room.Column < 0 || room.Row < 0 ||
                    room.Column + room.Width > GridSize || room.Row + room.Height > GridSize)
                {
                    errors.Add($"{room.Id}: 房间超出 7×7 网格。");
                    continue;
                }

                for (var row = room.Row; row < room.Row + room.Height; row++)
                {
                    for (var column = room.Column; column < room.Column + room.Width; column++)
                    {
                        if (!string.IsNullOrEmpty(occupancy[column, row]))
                        {
                            errors.Add($"{room.Id}: 单元格 C{column + 1} R{row + 1} 与 {occupancy[column, row]} 重叠。");
                        }

                        occupancy[column, row] = room.Id;
                    }
                }
            }

            for (var row = 0; row < GridSize; row++)
            {
                for (var column = 0; column < GridSize; column++)
                {
                    if (string.IsNullOrEmpty(occupancy[column, row]))
                    {
                        errors.Add($"单元格 C{column + 1} R{row + 1} 未被任何房间占用。");
                    }
                }
            }

            if (Rooms.Length != GridSize * GridSize)
            {
                errors.Add($"逻辑房间数应为 49，当前为 {Rooms.Length}。");
            }

            if (Rooms.Any(room => room.Width != 1 || room.Height != 1))
            {
                errors.Add("每间房必须恰好占用一个格子。");
            }

            if (Rooms.Select(room => room.Id).Distinct().Count() != Rooms.Length)
            {
                errors.Add("房间 ID 必须唯一。");
            }

            if (Rooms.Sum(room => room.CellCount) != GridSize * GridSize)
            {
                errors.Add("房间总占格数不是 49。 ");
            }

            var park = Rooms.SingleOrDefault(room => room.Type == RoomType.CentralPark);
            if (park == null || park.Column != 2 || park.Row != 2 || park.Width != 1 || park.Height != 1 || park.Movable)
            {
                errors.Add("中央公园必须固定在 R3C3 的单格区域。");
            }

            return errors;
        }
    }
}
