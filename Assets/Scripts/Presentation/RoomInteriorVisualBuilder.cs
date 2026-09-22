using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>
    /// Creates the replaceable low-poly furnishing pass used by the prototype.
    /// Every furnishing cluster stays in a room corner so the central cross and
    /// all authored doorway landings remain available to people and wildlife.
    /// </summary>
    public static class RoomInteriorVisualBuilder
    {
        private static readonly Color WarmWood = new(0.54f, 0.34f, 0.20f);
        private static readonly Color DarkWood = new(0.31f, 0.22f, 0.17f);
        private static readonly Color Cream = new(0.91f, 0.84f, 0.70f);
        private static readonly Color Linen = new(0.91f, 0.82f, 0.78f);
        private static readonly Color Graphite = new(0.18f, 0.20f, 0.22f);
        private static readonly Color SoftGrey = new(0.68f, 0.70f, 0.68f);
        private static readonly Color Leaf = new(0.31f, 0.55f, 0.29f);
        private static readonly Color DeepLeaf = new(0.20f, 0.39f, 0.23f);
        private static readonly Color Terracotta = new(0.67f, 0.28f, 0.18f);
        private static readonly Color Ochre = new(0.83f, 0.57f, 0.22f);
        private static readonly Color Teal = new(0.27f, 0.57f, 0.59f);

        public static bool Supports(RoomType type)
        {
            return type is RoomType.Residence or
                RoomType.Office or
                RoomType.Canteen or
                RoomType.Supermarket or
                RoomType.Garage or
                RoomType.PigeonHabitat or
                RoomType.ShrubHabitat or
                RoomType.FoxDen;
        }

        public static Transform Build(
            Transform parent,
            RoomSpec spec,
            float width,
            float depth,
            Material material,
            HideFlags hideFlags)
        {
            if (parent == null || spec == null || material == null)
            {
                return null;
            }

            if (!Supports(spec.Type))
            {
                return null;
            }

            var root = NewGroup($"{spec.Type} Furnishings", parent, hideFlags);
            var context = new BuildContext(root, material, hideFlags, AccentFor(spec.Id));
            var primary = PrimaryCorner(width, depth);
            var secondary = SecondaryCorner(width, depth);

            switch (spec.Type)
            {
                case RoomType.Residence:
                    BuildResidence(context, primary, secondary);
                    break;
                case RoomType.Office:
                    BuildOffice(context, primary, secondary);
                    break;
                case RoomType.Canteen:
                    BuildFoodShop(context, width, depth);
                    break;
                case RoomType.Supermarket:
                    BuildSupermarket(context, width, depth);
                    break;
                case RoomType.Garage:
                    BuildGarage(context, spec, depth);
                    break;
                case RoomType.PigeonHabitat:
                    BuildPigeonHabitat(context, primary, secondary, HasSecondCluster(width, depth));
                    break;
                case RoomType.ShrubHabitat:
                    BuildShrubHabitat(context, primary);
                    break;
                case RoomType.FoxDen:
                    BuildFoxDen(context, primary);
                    break;
            }

            return root;
        }

        private static void BuildResidence(
            BuildContext context,
            Vector3 anchor,
            Vector3 secondary)
        {
            context.Box("Bed Frame", anchor + new Vector3(0f, 0.34f, 0f), new Vector3(0.74f, 0.16f, 0.90f), WarmWood);
            context.Box("Mattress", anchor + new Vector3(0f, 0.47f, -0.01f), new Vector3(0.68f, 0.15f, 0.80f), Cream);
            context.Box("Headboard", anchor + new Vector3(0f, 0.55f, 0.43f), new Vector3(0.76f, 0.46f, 0.08f), DarkWood);
            context.Box("Pillow", anchor + new Vector3(0f, 0.59f, 0.25f), new Vector3(0.42f, 0.10f, 0.20f), Color.Lerp(Cream, Color.white, 0.34f));
            context.Box("Folded Blanket", anchor + new Vector3(0f, 0.58f, -0.25f), new Vector3(0.64f, 0.05f, 0.25f), context.Accent);

            context.Box("Wardrobe", secondary + new Vector3(0f, 0.63f, 0f), new Vector3(0.55f, 0.72f, 0.36f), WarmWood);
            context.Box("Wardrobe Door A", secondary + new Vector3(-0.14f, 0.64f, -0.19f), new Vector3(0.24f, 0.62f, 0.035f), Color.Lerp(WarmWood, Cream, 0.14f));
            context.Box("Wardrobe Door B", secondary + new Vector3(0.14f, 0.64f, -0.19f), new Vector3(0.24f, 0.62f, 0.035f), Color.Lerp(WarmWood, Cream, 0.14f));
            context.Sphere("Wardrobe Handle A", secondary + new Vector3(-0.035f, 0.64f, -0.22f), Vector3.one * 0.055f, Ochre);
            context.Sphere("Wardrobe Handle B", secondary + new Vector3(0.035f, 0.64f, -0.22f), Vector3.one * 0.055f, Ochre);

            context.Box("Bedside Cabinet", anchor + new Vector3(-0.40f, 0.49f, 0.17f), new Vector3(0.18f, 0.30f, 0.20f), DarkWood);
            context.Cylinder("Bedside Lamp", anchor + new Vector3(-0.40f, 0.74f, 0.17f), new Vector3(0.08f, 0.08f, 0.08f), Cream);

            var southWest = new Vector3(anchor.x, 0f, -anchor.z);
            var southEast = new Vector3(secondary.x, 0f, -secondary.z);
            context.Cylinder("Small Oval Rug", southWest + new Vector3(0f, 0.252f, 0f), new Vector3(0.68f, 0.006f, 0.44f), Linen);
            context.Cylinder("One Person Round Table", southWest + new Vector3(-0.08f, 0.47f, 0f), new Vector3(0.32f, 0.045f, 0.32f), WarmWood);
            context.Cylinder("Round Table Pedestal", southWest + new Vector3(-0.08f, 0.35f, 0f), new Vector3(0.055f, 0.10f, 0.055f), DarkWood);
            context.Box("Single Chair Seat", southWest + new Vector3(0.24f, 0.39f, 0f), new Vector3(0.17f, 0.11f, 0.17f), context.Accent);
            context.Box("Single Chair Back", southWest + new Vector3(0.32f, 0.53f, 0f), new Vector3(0.05f, 0.31f, 0.18f), WarmWood);
            BuildPottedPlant(context, southEast, "Residence Plant");
        }

        private static void BuildOffice(BuildContext context, Vector3 anchor, Vector3 secondary)
        {
            BuildWorkstation(context, anchor, "Workstation A");
            BuildWorkstation(context, secondary, "Workstation B");

            var southWest = new Vector3(anchor.x, 0f, -anchor.z);
            var southEast = new Vector3(secondary.x, 0f, -secondary.z);
            context.Box("Shared Filing Cabinet", southWest + new Vector3(0f, 0.45f, 0f), new Vector3(0.56f, 0.42f, 0.30f), Cream);
            context.Box("Filing Drawer A", southWest + new Vector3(0f, 0.41f, -0.16f), new Vector3(0.47f, 0.12f, 0.025f), SoftGrey);
            context.Box("Filing Drawer B", southWest + new Vector3(0f, 0.56f, -0.16f), new Vector3(0.47f, 0.12f, 0.025f), SoftGrey);
            context.Box("Compact Printer", southWest + new Vector3(0f, 0.74f, 0f), new Vector3(0.27f, 0.16f, 0.22f), Graphite);
            context.Box("Printer Paper", southWest + new Vector3(0f, 0.83f, 0.04f), new Vector3(0.18f, 0.025f, 0.13f), Color.white);
            context.Cylinder("Waste Paper Basket", southWest + new Vector3(-0.28f, 0.35f, 0.30f), new Vector3(0.12f, 0.10f, 0.12f), Graphite);
            BuildPottedPlant(context, southEast, "Office Plant");
        }

        private static void BuildWorkstation(BuildContext context, Vector3 anchor, string name)
        {
            context.Box($"{name} Desk Top", anchor + new Vector3(0f, 0.55f, 0f), new Vector3(0.78f, 0.10f, 0.38f), Cream);
            foreach (var offset in new[]
                     {
                         new Vector3(-0.31f, 0.38f, -0.13f), new Vector3(0.31f, 0.38f, -0.13f),
                         new Vector3(-0.31f, 0.38f, 0.13f), new Vector3(0.31f, 0.38f, 0.13f)
                     })
            {
                context.Box($"{name} Desk Leg", anchor + offset, new Vector3(0.065f, 0.30f, 0.065f), DarkWood);
            }

            context.Box($"{name} Monitor", anchor + new Vector3(0f, 0.76f, 0.06f), new Vector3(0.34f, 0.28f, 0.075f), Graphite);
            context.Box($"{name} Screen", anchor + new Vector3(0f, 0.76f, 0.018f), new Vector3(0.27f, 0.20f, 0.025f), Teal);
            context.Box($"{name} Keyboard", anchor + new Vector3(0f, 0.62f, -0.12f), new Vector3(0.32f, 0.035f, 0.12f), SoftGrey);
            context.Box($"{name} Chair", anchor + new Vector3(0f, 0.44f, -0.23f), new Vector3(0.34f, 0.20f, 0.20f), context.Accent);
        }

        private static void BuildPottedPlant(BuildContext context, Vector3 anchor, string name)
        {
            context.Cylinder($"{name} Pot", anchor + new Vector3(0f, 0.36f, 0f), new Vector3(0.15f, 0.12f, 0.15f), Terracotta);
            context.Box($"{name} Stem", anchor + new Vector3(0f, 0.57f, 0f), new Vector3(0.04f, 0.24f, 0.04f), DeepLeaf);
            context.Sphere($"{name} Leaf A", anchor + new Vector3(-0.10f, 0.65f, 0.02f), new Vector3(0.18f, 0.11f, 0.13f), Leaf);
            context.Sphere($"{name} Leaf B", anchor + new Vector3(0.10f, 0.68f, 0.01f), new Vector3(0.18f, 0.11f, 0.13f), DeepLeaf);
            context.Sphere($"{name} Leaf C", anchor + new Vector3(0f, 0.72f, -0.09f), new Vector3(0.13f, 0.11f, 0.17f), Leaf);
        }

        private static void BuildFoodShop(BuildContext context, float width, float depth)
        {
            var northWest = Corner(width, depth, -1f, 1f);
            var northEast = Corner(width, depth, 1f, 1f);
            var southWest = Corner(width, depth, -1f, -1f);
            var southEast = Corner(width, depth, 1f, -1f);

            var anchor = northWest;
            context.Box("Kitchen Counter", anchor + new Vector3(0f, 0.49f, 0f), new Vector3(0.84f, 0.40f, 0.42f), WarmWood);
            context.Box("Counter Top", anchor + new Vector3(0f, 0.72f, 0f), new Vector3(0.90f, 0.08f, 0.46f), Cream);
            context.Box("Cooker", anchor + new Vector3(-0.22f, 0.79f, 0f), new Vector3(0.27f, 0.055f, 0.23f), Graphite);
            context.Cylinder("Cooker Ring A", anchor + new Vector3(-0.29f, 0.84f, 0f), new Vector3(0.07f, 0.015f, 0.07f), Terracotta);
            context.Cylinder("Cooker Ring B", anchor + new Vector3(-0.15f, 0.84f, 0f), new Vector3(0.07f, 0.015f, 0.07f), Terracotta);
            context.Box("Sink", anchor + new Vector3(0.22f, 0.79f, 0f), new Vector3(0.27f, 0.055f, 0.23f), Graphite);
            context.Box("Sink Basin", anchor + new Vector3(0.22f, 0.83f, 0f), new Vector3(0.20f, 0.025f, 0.16f), Teal);
            context.Box("Extraction Hood", anchor + new Vector3(0f, 0.96f, 0.17f), new Vector3(0.46f, 0.27f, 0.045f), SoftGrey);
            context.Box("Low Kitchen Divider", anchor + new Vector3(0f, 0.46f, -0.34f), new Vector3(0.75f, 0.36f, 0.07f), WarmWood);
            context.Box("Under-counter Refrigerator", anchor + new Vector3(0.22f, 0.43f, -0.18f), new Vector3(0.22f, 0.27f, 0.06f), SoftGrey);
            context.Box("Refrigerator Handle", anchor + new Vector3(0.29f, 0.48f, -0.22f), new Vector3(0.025f, 0.13f, 0.025f), Graphite);
            context.Box("Host Order Station", anchor + new Vector3(0.25f, 0.79f, -0.24f), new Vector3(0.20f, 0.13f, 0.08f), Graphite);
            context.Box("Host Order Screen", anchor + new Vector3(0.25f, 0.80f, -0.29f), new Vector3(0.15f, 0.08f, 0.02f), Teal);
            context.Box("Closed Food Waste Bin", anchor + new Vector3(-0.34f, 0.39f, 0.29f), new Vector3(0.17f, 0.29f, 0.18f), DeepLeaf);
            context.Box("Food Waste Bin Lid", anchor + new Vector3(-0.34f, 0.55f, 0.29f), new Vector3(0.20f, 0.045f, 0.20f), Graphite);

            BuildDiningTable(context, northEast, "Dining Table A");
            BuildDiningTable(context, southWest, "Dining Table B");
            context.Box("Wall Booth Seat", southEast + new Vector3(0f, 0.40f, 0.08f), new Vector3(0.70f, 0.22f, 0.28f), context.Accent);
            context.Box("Wall Booth Back", southEast + new Vector3(0f, 0.61f, 0.19f), new Vector3(0.70f, 0.42f, 0.09f), context.Accent);
            context.Box("Wall Booth Table", southEast + new Vector3(0f, 0.51f, -0.17f), new Vector3(0.62f, 0.08f, 0.25f), Cream);
        }

        private static void BuildDiningTable(BuildContext context, Vector3 anchor, string name)
        {
            context.Cylinder(name, anchor + new Vector3(0f, 0.48f, 0f), new Vector3(0.29f, 0.09f, 0.29f), Cream);
            context.Cylinder($"{name} Pedestal", anchor + new Vector3(0f, 0.35f, 0f), new Vector3(0.075f, 0.18f, 0.075f), DarkWood);
            context.Box($"{name} Seat A", anchor + new Vector3(-0.25f, 0.38f, 0f), new Vector3(0.17f, 0.16f, 0.23f), context.Accent);
            context.Box($"{name} Seat B", anchor + new Vector3(0.25f, 0.38f, 0f), new Vector3(0.17f, 0.16f, 0.23f), context.Accent);
            context.Cylinder($"{name} Plate A", anchor + new Vector3(-0.10f, 0.59f, 0f), new Vector3(0.08f, 0.012f, 0.08f), Color.white);
            context.Cylinder($"{name} Plate B", anchor + new Vector3(0.10f, 0.59f, 0f), new Vector3(0.08f, 0.012f, 0.08f), Color.white);
            context.Cylinder($"{name} Cup A", anchor + new Vector3(-0.10f, 0.61f, 0.10f), new Vector3(0.04f, 0.05f, 0.04f), Cream);
            context.Cylinder($"{name} Cup B", anchor + new Vector3(0.10f, 0.61f, 0.10f), new Vector3(0.04f, 0.05f, 0.04f), Cream);
            context.Box($"{name} Napkin", anchor + new Vector3(0f, 0.59f, -0.12f), new Vector3(0.12f, 0.01f, 0.07f), Linen);
        }

        private static void BuildSupermarket(BuildContext context, float width, float depth)
        {
            var northWest = Corner(width, depth, -1f, 1f);
            var northEast = Corner(width, depth, 1f, 1f);
            var southWest = Corner(width, depth, -1f, -1f);
            var southEast = Corner(width, depth, 1f, -1f);

            // Two central gondolas leave routes around both ends and keep all six doors clear.
            BuildStockedShelf(context, new Vector3(-0.82f, 0f, 0f), "Central Gondola A", Leaf, Ochre);
            BuildStockedShelf(context, new Vector3(0.82f, 0f, 0f), "Central Gondola B", Terracotta, Cream);

            context.Box("Wall Chilled Case", northWest + new Vector3(0f, 0.55f, 0f), new Vector3(0.64f, 0.62f, 0.32f), SoftGrey);
            context.Box("Chilled Case Glass", northWest + new Vector3(0f, 0.58f, -0.17f), new Vector3(0.55f, 0.48f, 0.025f), Teal);
            context.Box("Chilled Case Handle", northWest + new Vector3(0.20f, 0.58f, -0.19f), new Vector3(0.025f, 0.22f, 0.025f), Cream);
            context.Box("Carton Stack Bottom", northWest + new Vector3(-0.32f, 0.34f, -0.25f), new Vector3(0.18f, 0.18f, 0.18f), WarmWood);
            context.Box("Carton Stack Top", northWest + new Vector3(-0.32f, 0.52f, -0.25f), new Vector3(0.16f, 0.17f, 0.16f), Ochre);

            context.Box("Produce Crate", northEast + new Vector3(0f, 0.42f, 0f), new Vector3(0.68f, 0.28f, 0.42f), WarmWood);
            context.Sphere("Produce Greens", northEast + new Vector3(-0.18f, 0.62f, 0f), Vector3.one * 0.17f, Leaf);
            context.Sphere("Produce Fruit", northEast + new Vector3(0.17f, 0.61f, 0f), Vector3.one * 0.15f, Terracotta);

            context.Box("Shopping Basket Stack", southWest + new Vector3(0.04f, 0.37f, 0f), new Vector3(0.33f, 0.22f, 0.25f), Ochre);
            context.Box("Basket Rim", southWest + new Vector3(0.04f, 0.50f, 0f), new Vector3(0.37f, 0.035f, 0.29f), DarkWood);
            BuildPottedPlant(context, southWest + new Vector3(-0.27f, 0f, 0f), "Supermarket Plant");

            context.Box("Checkout Counter", southEast + new Vector3(0f, 0.48f, 0f), new Vector3(0.72f, 0.38f, 0.40f), Cream);
            context.Box("Checkout Register", southEast + new Vector3(0.16f, 0.75f, 0f), new Vector3(0.25f, 0.20f, 0.20f), Graphite);
            context.Box("Card Terminal", southEast + new Vector3(-0.19f, 0.72f, -0.08f), new Vector3(0.12f, 0.10f, 0.16f), Teal);
            context.Box("Closed Waste Container", southEast + new Vector3(0.36f, 0.39f, 0f), new Vector3(0.16f, 0.26f, 0.17f), DeepLeaf);
            context.Box("Waste Container Lid", southEast + new Vector3(0.36f, 0.54f, 0f), new Vector3(0.19f, 0.04f, 0.20f), Graphite);
        }

        private static void BuildStockedShelf(
            BuildContext context,
            Vector3 anchor,
            string name,
            Color productA,
            Color productB)
        {
            context.Box(name, anchor + new Vector3(0f, 0.60f, 0f), new Vector3(0.72f, 0.72f, 0.34f), WarmWood);
            for (var row = 0; row < 3; row++)
            {
                var y = 0.37f + row * 0.22f;
                context.Box($"{name} Shelf {row + 1}", anchor + new Vector3(0f, y, -0.19f), new Vector3(0.68f, 0.04f, 0.38f), Cream);
                for (var column = -1; column <= 1; column++)
                {
                    context.Box(
                        $"{name} Product {row + 1}-{column + 2}",
                        anchor + new Vector3(column * 0.20f, y + 0.08f, -0.22f),
                        new Vector3(0.14f, 0.12f, 0.09f),
                        (row + column) % 2 == 0 ? productA : productB);
                }
            }
        }

        private static void BuildGarage(BuildContext context, RoomSpec spec, float depth)
        {
            // Painted markings only: traffic is spawned separately, and no workshop clutter
            // should occupy the pedestrian doors or imply that cars are permanently parked.
            var laneX = GarageVisualLayout.LaneCenterX(spec.Width);
            var markZ = Mathf.Min(0.70f, depth * 0.5f - 1.0f);
            for (var side = -1; side <= 1; side += 2)
            {
                for (var end = -1; end <= 1; end += 2)
                {
                    context.Box(
                        $"Lane Edge {side} {end}",
                        new Vector3(laneX + side * 0.48f, 0.254f, end * markZ),
                        new Vector3(0.045f, 0.012f, 0.28f),
                        SoftGrey);
                }
            }
        }

        private static void BuildPigeonHabitat(
            BuildContext context,
            Vector3 anchor,
            Vector3 secondary,
            bool extended)
        {
            BuildPigeonRoost(context, anchor, "Primary Roost");
            context.Cylinder(
                "Landing Perch",
                secondary + new Vector3(0f, 0.55f, 0.10f),
                new Vector3(0.045f, extended ? 0.34f : 0.28f, 0.045f),
                Cream,
                new Vector3(0f, 0f, 90f));
            context.Cylinder("Water Dish", secondary + new Vector3(-0.19f, 0.31f, -0.12f), new Vector3(0.12f, 0.025f, 0.12f), Teal);
            context.Box("Seed Tray", secondary + new Vector3(0.18f, 0.31f, -0.12f), new Vector3(0.24f, 0.04f, 0.17f), Ochre);
        }

        private static void BuildPigeonRoost(BuildContext context, Vector3 anchor, string name)
        {
            context.Box(name, anchor + new Vector3(0f, 0.51f, 0f), new Vector3(0.76f, 0.48f, 0.38f), WarmWood);
            for (var column = 0; column < 4; column++)
            {
                var x = -0.27f + column * 0.18f;
                context.Box(
                    $"{name} Nest Box {column + 1}",
                    anchor + new Vector3(x, 0.56f, -0.21f),
                    new Vector3(0.145f, 0.25f, 0.08f),
                    DarkWood);
                context.Cylinder(
                    $"{name} Nest Bowl {column + 1}",
                    anchor + new Vector3(x, 0.45f, -0.27f),
                    new Vector3(0.060f, 0.025f, 0.060f),
                    Ochre);
            }

            context.Cylinder(
                $"{name} Perch",
                anchor + new Vector3(0f, 0.78f, -0.23f),
                new Vector3(0.045f, 0.34f, 0.045f),
                Cream,
                new Vector3(0f, 0f, 90f));
        }

        private static void BuildShrubHabitat(BuildContext context, Vector3 anchor)
        {
            var leafOffsets = new[]
            {
                new Vector3(-0.22f, 0.48f, 0.04f),
                new Vector3(0.22f, 0.46f, 0.10f),
                new Vector3(0f, 0.58f, -0.18f),
                new Vector3(0f, 0.40f, 0.23f)
            };
            for (var index = 0; index < leafOffsets.Length; index++)
            {
                context.Sphere(
                    $"Shrub Crown {index + 1}",
                    anchor + leafOffsets[index],
                    new Vector3(0.44f, 0.38f, 0.40f),
                    index % 2 == 0 ? Leaf : DeepLeaf);
            }

            context.Cylinder(
                "Shelter Log",
                anchor + new Vector3(0.05f, 0.34f, -0.03f),
                new Vector3(0.11f, 0.30f, 0.11f),
                DarkWood,
                new Vector3(0f, 0f, 90f));
            context.Sphere("White Flower", anchor + new Vector3(-0.27f, 0.69f, -0.06f), Vector3.one * 0.085f, Cream);
            context.Sphere("Ochre Flower", anchor + new Vector3(0.25f, 0.65f, -0.10f), Vector3.one * 0.075f, Ochre);
        }

        private static void BuildFoxDen(BuildContext context, Vector3 anchor)
        {
            // A small urban retaining wall and open drainage culvert replace the
            // previous woodland-like dirt mound. All pieces remain in one corner.
            var brick = new Color(0.47f, 0.33f, 0.27f);
            var stone = new Color(0.59f, 0.58f, 0.50f);
            var hollow = new Color(0.075f, 0.075f, 0.07f);
            context.Box("Retaining Wall Stone Cap", anchor + new Vector3(0f, 0.64f, 0.20f), new Vector3(0.80f, 0.13f, 0.26f), stone);
            context.Box("Retaining Wall Brick", anchor + new Vector3(0f, 0.40f, 0.20f), new Vector3(0.78f, 0.38f, 0.23f), brick);
            context.Box("Retaining Wall Left Stone", anchor + new Vector3(-0.30f, 0.47f, 0.06f), new Vector3(0.16f, 0.42f, 0.36f), stone);
            context.Box("Retaining Wall Right Stone", anchor + new Vector3(0.30f, 0.45f, 0.07f), new Vector3(0.16f, 0.38f, 0.34f), stone);
            context.Cylinder("Open Drainage Culvert", anchor + new Vector3(-0.09f, 0.27f, -0.12f), new Vector3(0.23f, 0.014f, 0.23f), hollow);
            context.Cylinder("Culvert Concrete Ring", anchor + new Vector3(-0.09f, 0.267f, -0.12f), new Vector3(0.30f, 0.008f, 0.30f), stone);
            context.Cylinder("Culvert Dark Opening", anchor + new Vector3(-0.09f, 0.283f, -0.12f), new Vector3(0.21f, 0.006f, 0.21f), hollow);
            context.Box("Sheltered Side Recess", anchor + new Vector3(0.25f, 0.27f, -0.12f), new Vector3(0.16f, 0.014f, 0.19f), hollow);
            context.Box("Resting Cardboard", anchor + new Vector3(-0.19f, 0.29f, -0.31f), new Vector3(0.28f, 0.018f, 0.12f), Ochre, 12f);
            context.Box("Resting Leaves", anchor + new Vector3(0.18f, 0.29f, -0.30f), new Vector3(0.20f, 0.018f, 0.12f), WarmWood, -18f);
            context.Box("Ivy at Culvert", anchor + new Vector3(-0.29f, 0.69f, 0.12f), new Vector3(0.17f, 0.08f, 0.14f), DeepLeaf, 23f);
        }

        private static Transform NewGroup(string name, Transform parent, HideFlags hideFlags)
        {
            var group = new GameObject(name) { hideFlags = hideFlags };
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static Vector3 PrimaryCorner(float width, float depth)
        {
            return Corner(width, depth, -1f, 1f);
        }

        private static Vector3 Corner(float width, float depth, float xSign, float zSign)
        {
            return new Vector3(
                xSign * (width * 0.5f - 0.56f),
                0f,
                zSign * (depth * 0.5f - 0.56f));
        }

        private static Vector3 SecondaryCorner(float width, float depth)
        {
            return width >= depth
                ? new Vector3(width * 0.5f - 0.56f, 0f, depth * 0.5f - 0.56f)
                : new Vector3(-width * 0.5f + 0.56f, 0f, -depth * 0.5f + 0.56f);
        }

        private static bool HasSecondCluster(float width, float depth)
        {
            return Mathf.Max(width, depth) > 4f;
        }

        private static Color AccentFor(string id)
        {
            var sum = 0;
            foreach (var character in id ?? string.Empty)
            {
                sum += character;
            }

            return (sum % 4) switch
            {
                0 => new Color(0.45f, 0.65f, 0.67f),
                1 => new Color(0.70f, 0.49f, 0.61f),
                2 => new Color(0.63f, 0.69f, 0.40f),
                _ => new Color(0.82f, 0.55f, 0.28f)
            };
        }

        private sealed class BuildContext
        {
            private readonly Transform parent;
            private readonly Material material;
            private readonly HideFlags hideFlags;

            public BuildContext(Transform parent, Material material, HideFlags hideFlags, Color accent)
            {
                this.parent = parent;
                this.material = material;
                this.hideFlags = hideFlags;
                Accent = accent;
            }

            public Color Accent { get; }

            public GameObject Box(
                string name,
                Vector3 position,
                Vector3 scale,
                Color color,
                float yRotation = 0f)
            {
                var instance = Primitive(PrimitiveType.Cube, name, position, scale, color);
                instance.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
                return instance;
            }

            public GameObject Cylinder(
                string name,
                Vector3 position,
                Vector3 scale,
                Color color,
                Vector3? rotation = null)
            {
                var instance = Primitive(PrimitiveType.Cylinder, name, position, scale, color);
                instance.transform.localRotation = Quaternion.Euler(rotation ?? Vector3.zero);
                return instance;
            }

            public GameObject Sphere(string name, Vector3 position, Vector3 scale, Color color)
            {
                return Primitive(PrimitiveType.Sphere, name, position, scale, color);
            }

            private GameObject Primitive(
                PrimitiveType primitiveType,
                string name,
                Vector3 position,
                Vector3 scale,
                Color color)
            {
                return UrbanVisualFactory.CreatePrimitive(
                    primitiveType,
                    name,
                    parent,
                    position,
                    scale,
                    color,
                    material,
                    true,
                    hideFlags);
            }
        }
    }
}
