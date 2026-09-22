using System.Collections.Generic;
using System.Linq;

namespace UrbanWildlifeRooms.Data
{
    public static class RoomLayoutData
    {
        public const int GridSize = 7;

        private static readonly RoomSpec[] Rooms =
        {
            new("garage-a", "车库 A", RoomType.Garage, 0, 0, 2, 1, true, "车辆出入口与动物碰撞风险"),
            new("residence-a", "住宅 A", RoomType.Residence, 2, 0, 2, 1, true, "居民休息与日常活动"),
            new("oak-a", "橡树 A", RoomType.OakHabitat, 4, 0, 2, 1, true, "松鼠庇护与自然食物"),
            new("trash-a", "垃圾 A", RoomType.Trash, 6, 0, 1, 1, true, "垃圾收集与动物食物热点"),

            new("pigeon-a", "鸽群 A", RoomType.PigeonHabitat, 0, 1, 1, 2, false, "鸽群休息与聚集空间"),
            new("supermarket", "小型超市", RoomType.Supermarket, 1, 1, 2, 1, true, "人类目的地与包装食物来源"),
            new("office-a", "办公室 A", RoomType.Office, 3, 1, 1, 1, true, "工作人流目的地"),
            new("trash-b", "垃圾 B", RoomType.Trash, 4, 1, 1, 1, true, "垃圾收集与动物食物热点"),
            new("residence-c", "住宅 C", RoomType.Residence, 5, 1, 1, 1, true, "居民休息与日常活动"),
            new("garage-b", "车库 B", RoomType.Garage, 6, 1, 1, 2, true, "车辆出入口与动物碰撞风险"),

            new("pigeon-b", "鸽群 B", RoomType.PigeonHabitat, 1, 2, 1, 1, false, "鸽群休息与聚集空间"),
            new("central-park", "中央公园", RoomType.CentralPark, 2, 2, 2, 2, false, "固定公共空间、投喂与多物种避难"),
            new("pigeon-c", "鸽群 C", RoomType.PigeonHabitat, 4, 2, 1, 1, false, "鸽群休息与聚集空间"),
            new("office-b", "办公室 B", RoomType.Office, 5, 2, 1, 1, true, "工作人流目的地"),

            new("residence-g", "住宅 G", RoomType.Residence, 0, 3, 1, 1, true, "居民休息与日常活动"),
            new("oak-c", "橡树 C", RoomType.OakHabitat, 1, 3, 1, 1, true, "开局幼树，用于展示生态恢复", true),
            new("residence-d", "住宅 D", RoomType.Residence, 4, 3, 1, 1, true, "居民休息与日常活动"),
            new("trash-c", "垃圾 C", RoomType.Trash, 5, 3, 1, 1, true, "垃圾收集与动物食物热点"),
            new("oak-b", "橡树 B", RoomType.OakHabitat, 6, 3, 1, 2, true, "松鼠庇护与自然食物"),

            new("residence-h", "住宅 H", RoomType.Residence, 0, 4, 1, 1, true, "居民休息与日常活动"),
            new("residence-b", "住宅 B", RoomType.Residence, 1, 4, 2, 1, true, "居民休息与日常活动"),
            new("canteen-b", "餐饮商铺 B", RoomType.Canteen, 3, 4, 2, 1, true, "人类用餐服务与动物食物来源"),
            new("shrub-a", "灌木 A", RoomType.ShrubHabitat, 5, 4, 1, 1, true, "刺猬庇护与夜间低风险路径"),

            new("residence-e", "住宅 E", RoomType.Residence, 0, 5, 1, 1, true, "居民休息与日常活动"),
            new("residence-f", "住宅 F", RoomType.Residence, 1, 5, 1, 1, true, "居民休息与日常活动"),
            new("canteen-a", "餐饮商铺 A", RoomType.Canteen, 2, 5, 1, 2, true, "人类用餐服务与动物食物来源"),
            new("office-c", "办公室 C", RoomType.Office, 3, 5, 1, 1, true, "工作人流目的地"),
            new("trash-d", "垃圾 D", RoomType.Trash, 4, 5, 1, 1, true, "垃圾收集与动物食物热点"),
            new("pigeon-d", "鸽群 D", RoomType.PigeonHabitat, 5, 5, 1, 1, false, "鸽群休息与聚集空间"),
            new("shrub-b", "灌木 B", RoomType.ShrubHabitat, 6, 5, 1, 1, true, "刺猬庇护与夜间低风险路径"),

            new("garage-c", "车库 C", RoomType.Garage, 0, 6, 2, 1, true, "车辆出入口与动物碰撞风险"),
            new("office-d", "办公室 D", RoomType.Office, 3, 6, 1, 1, true, "工作人流与资源点来源"),
            new("oak-d", "橡树 D", RoomType.OakHabitat, 4, 6, 1, 1, true, "松鼠庇护与自然食物"),
            new("shrub-c", "灌木 C", RoomType.ShrubHabitat, 5, 6, 1, 1, true, "刺猬庇护与夜间低风险路径"),
            new("fox-den", "狐狸洞", RoomType.FoxDen, 6, 6, 1, 1, false, "一只狐狸的固定核心庇护")
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

            if (Rooms.Length != 35)
            {
                errors.Add($"逻辑房间数应为 35，当前为 {Rooms.Length}。");
            }

            if (Rooms.Sum(room => room.CellCount) != GridSize * GridSize)
            {
                errors.Add("房间总占格数不是 49。 ");
            }

            var park = Rooms.SingleOrDefault(room => room.Type == RoomType.CentralPark);
            if (park == null || park.Column != 2 || park.Row != 2 || park.Width != 2 || park.Height != 2 || park.Movable)
            {
                errors.Add("中央公园必须固定在 R3C3 起始的 2×2 区域。 ");
            }

            return errors;
        }
    }
}
