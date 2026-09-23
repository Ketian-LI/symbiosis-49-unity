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
                    BuildPigeonHabitat(context, width, depth);
                    break;
                case RoomType.ShrubHabitat:
                    BuildShrubHabitat(context, spec.Id, width, depth);
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

        private static void BuildPigeonHabitat(BuildContext context, float width, float depth)
        {
            var northWest = Corner(width, depth, -1f, 1f);
            var northEast = Corner(width, depth, 1f, 1f);
            var southWest = Corner(width, depth, -1f, -1f);
            var southEast = Corner(width, depth, 1f, -1f);

            context.Box("Ventilation Service Core", northWest + new Vector3(0f, 0.53f, 0f), new Vector3(0.70f, 0.54f, 0.40f), UrbanPalette.PigeonVentMetal);
            context.Box("Ventilation Top Panel", northWest + new Vector3(0f, 0.82f, 0f), new Vector3(0.64f, 0.035f, 0.35f), UrbanPalette.PigeonVentHighlight);
            context.Box("Ventilation Dark Grille", northWest + new Vector3(-0.08f, 0.85f, -0.02f), new Vector3(0.32f, 0.025f, 0.22f), Graphite);
            for (var index = 0; index < 3; index++)
            {
                context.Box($"Ventilation Grille Slat {index + 1}",
                    northWest + new Vector3(-0.19f + index * 0.11f, 0.87f, -0.02f),
                    new Vector3(0.025f, 0.012f, 0.18f), UrbanPalette.PigeonVentMetal);
            }
            context.Cylinder("Ventilation Turbine Base", northWest + new Vector3(0.21f, 0.87f, 0.06f), new Vector3(0.15f, 0.03f, 0.15f), UrbanPalette.PigeonVentMetal);
            context.Sphere("Ventilation Turbine Dome", northWest + new Vector3(0.21f, 0.96f, 0.06f), new Vector3(0.19f, 0.17f, 0.19f), UrbanPalette.PigeonVentHighlight);
            context.Box("Service Conduit", northWest + new Vector3(0.15f, 0.85f, -0.13f), new Vector3(0.20f, 0.035f, 0.035f), Graphite);

            BuildPigeonRoost(context, northEast, "Open Pigeon Loft");
            context.Cylinder(
                "Loft Perch Rail",
                northEast + new Vector3(0f, 0.43f, -0.31f),
                new Vector3(0.035f, 0.31f, 0.035f),
                Cream,
                new Vector3(0f, 0f, 90f));
            context.Cylinder(
                "Landing Perch Rail",
                southEast + new Vector3(0f, 0.48f, 0f),
                new Vector3(0.035f, 0.31f, 0.035f),
                UrbanPalette.PigeonLoftWood,
                new Vector3(0f, 0f, 90f));
            foreach (var corner in new[] { northEast + new Vector3(0f, 0f, -0.31f), southEast })
            {
                context.Box("Perch Left Support", corner + new Vector3(-0.26f, 0.36f, 0f), new Vector3(0.045f, 0.23f, 0.045f), DarkWood);
                context.Box("Perch Right Support", corner + new Vector3(0.26f, 0.36f, 0f), new Vector3(0.045f, 0.23f, 0.045f), DarkWood);
            }
            context.Cylinder("Water Dish Rim", southWest + new Vector3(-0.15f, 0.29f, 0f), new Vector3(0.29f, 0.028f, 0.29f), Cream);
            context.Cylinder("Water Dish", southWest + new Vector3(-0.15f, 0.32f, 0f), new Vector3(0.23f, 0.008f, 0.23f), UrbanPalette.PigeonWater);
            context.Box("Separate Seed Tray", southWest + new Vector3(0.22f, 0.29f, 0f), new Vector3(0.22f, 0.035f, 0.17f), UrbanPalette.PigeonLoftWood);
            for (var index = 0; index < 3; index++)
            {
                context.Sphere($"Tray Seed {index + 1}", southWest + new Vector3(0.15f + index * 0.07f, 0.32f, 0f),
                    Vector3.one * 0.025f, Ochre);
            }
        }

        private static void BuildPigeonRoost(BuildContext context, Vector3 anchor, string name)
        {
            context.Box(name, anchor + new Vector3(0f, 0.51f, 0f), new Vector3(0.76f, 0.48f, 0.38f), UrbanPalette.PigeonLoftWood);
            context.Box($"{name} Rear Roof", anchor + new Vector3(0f, 0.79f, 0.11f), new Vector3(0.80f, 0.10f, 0.20f), UrbanPalette.PigeonLoftRoof);
            context.Box($"{name} Front Sill", anchor + new Vector3(0f, 0.38f, -0.27f), new Vector3(0.78f, 0.055f, 0.08f), UrbanPalette.PigeonLoftWood);
            context.Box($"{name} Left Post", anchor + new Vector3(-0.36f, 0.56f, -0.20f), new Vector3(0.055f, 0.39f, 0.07f), Cream);
            context.Box($"{name} Right Post", anchor + new Vector3(0.36f, 0.56f, -0.20f), new Vector3(0.055f, 0.39f, 0.07f), Cream);
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
                context.Box($"{name} Straw {column + 1}",
                    anchor + new Vector3(x, 0.42f, -0.29f),
                    new Vector3(0.11f, 0.018f, 0.05f), Cream, column % 2 == 0 ? 14f : -12f);
                // The camera becomes fully top-down in play; a shallow roof
                // recess keeps all four cubbies legible from that angle.
                context.Box($"{name} Roof Nest {column + 1}",
                    anchor + new Vector3(x, 0.848f, 0.11f),
                    new Vector3(0.135f, 0.012f, 0.14f), DarkWood);
                context.Box($"{name} Roof Straw {column + 1}",
                    anchor + new Vector3(x, 0.858f, 0.11f),
                    new Vector3(0.092f, 0.012f, 0.070f), UrbanPalette.PigeonLoftRoof,
                    column % 2 == 0 ? 12f : -12f);
            }

            context.Cylinder(
                $"{name} Perch",
                anchor + new Vector3(0f, 0.78f, -0.23f),
                new Vector3(0.045f, 0.34f, 0.045f),
                Cream,
                new Vector3(0f, 0f, 90f));
        }

        private static void BuildShrubHabitat(BuildContext context, string roomId, float width, float depth)
        {
            // The three fixed kits have equal cover; only the silhouette and open route vary.
            var northWest = Corner(width, depth, -1f, 1f);
            var northEast = Corner(width, depth, 1f, 1f);
            var southWest = Corner(width, depth, -1f, -1f);
            var southEast = Corner(width, depth, 1f, -1f);
            var first = roomId switch
            {
                "shrub-c" => northEast,
                _ => northWest
            };
            var second = roomId switch
            {
                "shrub-b" => southEast,
                "shrub-c" => southEast,
                _ => southWest
            };
            BuildShrubIsland(context, first, "Cover Island A", roomId == "shrub-b" ? 1.05f : 1f,
                roomId == "shrub-b" ? 5 : 4);
            BuildShrubIsland(context, second, "Cover Island B", roomId == "shrub-b" ? 0.95f : 1f,
                roomId == "shrub-b" ? 3 : 4);
            // Major cover remains asymmetrical, but small edge plants keep the
            // other two corners from reading as empty painted floor.
            var edgeCorners = roomId switch
            {
                "shrub-b" => new[] { northEast, southWest },
                "shrub-c" => new[] { northWest, southWest },
                _ => new[] { northEast, southEast }
            };
            for (var index = 0; index < edgeCorners.Length; index++)
            {
                BuildShrubEdgeGrowth(context, edgeCorners[index], $"Edge Growth {index + 1}");
            }

            var towardCentre = new Vector3(-Mathf.Sign(first.x) * 0.27f, 0f, -Mathf.Sign(first.z) * 0.25f);
            context.Box("Dry Leaf Resting Patch", first + towardCentre + new Vector3(0f, 0.27f, 0f), new Vector3(0.30f, 0.018f, 0.20f), UrbanPalette.ShrubPathDark, 18f);
            context.Box("Dry Resting Leaves", first + towardCentre + new Vector3(0.04f, 0.29f, 0.02f), new Vector3(0.20f, 0.012f, 0.11f), UrbanPalette.ShrubPathLight, -17f);
            context.Sphere("Smooth Stone A", first + new Vector3(-Mathf.Sign(first.x) * 0.23f, 0.30f, -Mathf.Sign(first.z) * 0.31f), new Vector3(0.17f, 0.12f, 0.14f), UrbanPalette.ShrubStone);
            context.Sphere("Smooth Stone B", second + new Vector3(-Mathf.Sign(second.x) * 0.23f, 0.30f, -Mathf.Sign(second.z) * 0.31f), new Vector3(0.16f, 0.10f, 0.13f), UrbanPalette.ShrubStone);
            context.Sphere("Smooth Stone C", second + new Vector3(0.15f * -Mathf.Sign(second.x), 0.29f, 0f), new Vector3(0.11f, 0.08f, 0.10f), Cream);
            context.Box("Leaf Litter Insect Point", second + new Vector3(-Mathf.Sign(second.x) * 0.27f, 0.27f, -Mathf.Sign(second.z) * 0.25f), new Vector3(0.16f, 0.014f, 0.13f), DarkWood, -22f);
            BuildShrubFlower(context, first, "White Flower A");
            BuildShrubFlower(context, second, "White Flower B");
        }

        private static void BuildShrubIsland(BuildContext context, Vector3 anchor, string name, float size, int crownCount)
        {
            var offsets = new[]
            {
                new Vector3(-0.19f, 0.48f, 0.02f),
                new Vector3(0.19f, 0.45f, 0.06f),
                new Vector3(0f, 0.56f, -0.15f),
                new Vector3(0f, 0.41f, 0.18f),
                new Vector3(0.02f, 0.61f, 0.03f)
            };
            for (var index = 0; index < crownCount; index++)
            {
                context.FacetedFoliage($"{name} Crown {index + 1}", anchor + offsets[index] * size,
                    new Vector3(0.54f, 0.43f, 0.50f) * size,
                    index % 3 == 0 ? UrbanPalette.ShrubLeafHighlight :
                    index % 2 == 0 ? UrbanPalette.ShrubLeaf : UrbanPalette.ShrubDeepLeaf,
                    index * 27f - 19f);
            }
            var undergrowth = new[]
            {
                new Vector3(-0.27f, 0.34f, -0.20f), new Vector3(0.27f, 0.33f, -0.19f),
                new Vector3(-0.25f, 0.34f, 0.20f), new Vector3(0.25f, 0.33f, 0.21f)
            };
            for (var index = 0; index < undergrowth.Length; index++)
            {
                context.FacetedFoliage($"{name} Ground Foliage {index + 1}", anchor + undergrowth[index],
                    new Vector3(0.23f, 0.17f, 0.22f),
                    index % 2 == 0 ? UrbanPalette.ShrubDeepLeaf : UrbanPalette.ShrubLeaf,
                    index * 41f);
            }
            // Two smaller, low clumps pull the corner island toward the path
            // without filling the centre or any of the four door landings.
            var inwardX = -Mathf.Sign(anchor.x);
            var inwardZ = -Mathf.Sign(anchor.z);
            context.FacetedFoliage($"{name} Ground Foliage 5",
                anchor + new Vector3(inwardX * 0.35f, 0.32f, inwardZ * 0.19f),
                new Vector3(0.24f, 0.16f, 0.21f), UrbanPalette.ShrubLeaf, 19f);
            context.FacetedFoliage($"{name} Ground Foliage 6",
                anchor + new Vector3(inwardX * 0.20f, 0.32f, inwardZ * 0.35f),
                new Vector3(0.22f, 0.15f, 0.20f), UrbanPalette.ShrubDeepLeaf, -23f);
        }

        private static void BuildShrubFlower(BuildContext context, Vector3 anchor, string name)
        {
            context.Cylinder($"{name} Centre", anchor + new Vector3(0f, 0.82f, 0f),
                new Vector3(0.055f, 0.009f, 0.055f), Ochre);
            for (var index = 0; index < 5; index++)
            {
                var angle = index * Mathf.PI * 2f / 5f;
                context.Box($"{name} Petal {index + 1}",
                    anchor + new Vector3(Mathf.Sin(angle) * 0.07f, 0.81f, Mathf.Cos(angle) * 0.07f),
                    new Vector3(0.055f, 0.014f, 0.085f), Cream, index * 72f);
            }
        }

        private static void BuildShrubEdgeGrowth(BuildContext context, Vector3 anchor, string name)
        {
            var inwardX = -Mathf.Sign(anchor.x);
            var inwardZ = -Mathf.Sign(anchor.z);
            context.FacetedFoliage($"{name} Low Tuft A",
                anchor + new Vector3(inwardX * 0.12f, 0.34f, inwardZ * 0.06f),
                new Vector3(0.32f, 0.23f, 0.30f), UrbanPalette.ShrubLeaf, 18f);
            context.FacetedFoliage($"{name} Low Tuft B",
                anchor + new Vector3(-inwardX * 0.17f, 0.33f, inwardZ * 0.15f),
                new Vector3(0.25f, 0.18f, 0.23f), UrbanPalette.ShrubDeepLeaf, -31f);
            context.Box($"{name} Dry Leaf",
                anchor + new Vector3(inwardX * 0.28f, 0.27f, inwardZ * 0.26f),
                new Vector3(0.12f, 0.014f, 0.065f), UrbanPalette.ShrubPathDark, 35f);
        }

        private static void BuildFoxDen(BuildContext context, Vector3 anchor)
        {
            // Two rear wall fragments imply one retaining wall while leaving the
            // mandatory north door open. The pipe and side recess stay in corners.
            var brick = UrbanPalette.FoxBrick;
            var stone = UrbanPalette.FoxStone;
            var hollow = UrbanPalette.FoxHollow;
            var darkEarth = UrbanPalette.FoxRestEarth;
            var northEast = new Vector3(-anchor.x, 0f, anchor.z);
            var southWest = new Vector3(anchor.x, 0f, -anchor.z);
            var restingCorner = new Vector3(-anchor.x, 0f, -anchor.z);
            context.Box("Retaining Wall Stone Cap", anchor + new Vector3(0f, 0.64f, 0.20f), new Vector3(0.80f, 0.13f, 0.26f), stone);
            context.Box("Retaining Wall Brick", anchor + new Vector3(0f, 0.40f, 0.20f), new Vector3(0.78f, 0.38f, 0.23f), brick);
            context.Box("Retaining Wall Left Stone", anchor + new Vector3(-0.30f, 0.47f, 0.06f), new Vector3(0.16f, 0.42f, 0.36f), stone);
            context.Box("Retaining Wall Right Stone", anchor + new Vector3(0.30f, 0.45f, 0.07f), new Vector3(0.16f, 0.38f, 0.34f), stone);
            context.Box("Right Retaining Wall Cap", northEast + new Vector3(0f, 0.64f, 0.20f), new Vector3(0.78f, 0.13f, 0.26f), stone);
            context.Box("Right Retaining Wall Brick", northEast + new Vector3(0f, 0.40f, 0.20f), new Vector3(0.76f, 0.38f, 0.23f), brick);
            context.Box("Right Retaining Wall Front Course", northEast + new Vector3(0f, 0.48f, -0.01f), new Vector3(0.68f, 0.08f, 0.07f), UrbanPalette.FoxStoneShade);
            for (var index = 0; index < 4; index++)
            {
                var x = -0.26f + index * 0.17f;
                context.Box($"Left Wall Top Brick {index + 1}", anchor + new Vector3(x, 0.713f, 0.20f),
                    new Vector3(0.13f, 0.012f, 0.15f), index % 2 == 0 ? stone : UrbanPalette.FoxStoneShade);
                context.Box($"Right Wall Top Brick {index + 1}", northEast + new Vector3(x, 0.713f, 0.20f),
                    new Vector3(0.13f, 0.012f, 0.15f), index % 2 == 0 ? stone : UrbanPalette.FoxStoneShade);
            }

            var culvert = anchor + new Vector3(-0.05f, 0f, -0.11f);
            context.Cylinder("Open Drainage Culvert", culvert + new Vector3(0f, 0.264f, 0f), new Vector3(0.38f, 0.010f, 0.38f), hollow);
            context.Cylinder("Culvert Concrete Ring", culvert + new Vector3(0f, 0.268f, 0f), new Vector3(0.49f, 0.008f, 0.49f), stone);
            context.Cylinder("Culvert Dark Opening", culvert + new Vector3(0f, 0.284f, 0f), new Vector3(0.32f, 0.006f, 0.32f), hollow);
            for (var index = 0; index < 10; index++)
            {
                var angle = index * Mathf.PI * 2f / 10f;
                context.Box($"Culvert Ring Stone {index + 1}",
                    culvert + new Vector3(Mathf.Sin(angle) * 0.21f, 0.287f, Mathf.Cos(angle) * 0.21f),
                    new Vector3(0.10f, 0.025f, 0.075f), index % 2 == 0 ? stone : SoftGrey,
                    index * 36f);
            }
            context.Box("Sheltered Side Recess", northEast + new Vector3(-0.04f, 0.274f, -0.14f), new Vector3(0.28f, 0.014f, 0.20f), hollow);
            context.Box("Side Recess Stone Header", northEast + new Vector3(-0.04f, 0.292f, -0.02f), new Vector3(0.36f, 0.035f, 0.055f), stone);
            context.Box("Side Recess Left Jamb", northEast + new Vector3(-0.20f, 0.289f, -0.14f), new Vector3(0.05f, 0.035f, 0.20f), stone);
            context.Box("Side Recess Right Jamb", northEast + new Vector3(0.12f, 0.289f, -0.14f), new Vector3(0.05f, 0.035f, 0.20f), stone);

            context.Cylinder("Sheltered Resting Hollow", restingCorner + new Vector3(0f, 0.258f, 0f), new Vector3(0.50f, 0.008f, 0.42f), darkEarth);
            context.Box("Resting Cardboard", restingCorner + new Vector3(-0.10f, 0.275f, 0f), new Vector3(0.25f, 0.018f, 0.17f), UrbanPalette.FoxCardboard, 12f);
            context.Box("Resting Leaves", restingCorner + new Vector3(0.10f, 0.288f, 0.05f), new Vector3(0.18f, 0.018f, 0.13f), UrbanPalette.FoxDryLeaf, -18f);
            context.Cylinder("Southwest Resting Hollow", southWest + new Vector3(0f, 0.258f, 0f),
                new Vector3(0.42f, 0.008f, 0.36f), darkEarth);
            context.Box("Southwest Resting Cardboard", southWest + new Vector3(0.02f, 0.275f, -0.02f),
                new Vector3(0.24f, 0.018f, 0.15f), UrbanPalette.FoxCardboard, -17f);
            var restCorners = new[] { restingCorner, southWest };
            for (var cornerIndex = 0; cornerIndex < restCorners.Length; cornerIndex++)
            {
                var corner = restCorners[cornerIndex];
                for (var index = 0; index < 3; index++)
                {
                    context.Box($"Resting Hollow Fallen Leaf {cornerIndex + 1}-{index + 1}",
                        corner + new Vector3(-0.12f + index * 0.11f, 0.294f, 0.13f - index * 0.06f),
                        new Vector3(0.075f, 0.012f, 0.050f), UrbanPalette.FoxDryLeaf,
                        -28f + index * 25f);
                }
            }
            context.Box("Ivy at Culvert", anchor + new Vector3(-0.29f, 0.69f, 0.12f), new Vector3(0.17f, 0.08f, 0.14f), UrbanPalette.FoxLeaf, 23f);
            context.FacetedFoliage("Ivy on Right Wall", northEast + new Vector3(0.22f, 0.71f, 0.05f), new Vector3(0.19f, 0.16f, 0.19f), UrbanPalette.FoxLeaf, -15f);
            context.FacetedFoliage("Culvert Edge Weed A", anchor + new Vector3(-0.27f, 0.36f, -0.29f), new Vector3(0.20f, 0.20f, 0.16f), UrbanPalette.FoxLeaf, 18f);
            context.FacetedFoliage("Culvert Edge Weed B", anchor + new Vector3(0.27f, 0.35f, -0.26f), new Vector3(0.18f, 0.18f, 0.15f), UrbanPalette.ShrubDeepLeaf, -12f);
            context.FacetedFoliage("Resting Edge Low Shrub", restingCorner + new Vector3(-0.18f, 0.40f, -0.16f), new Vector3(0.25f, 0.27f, 0.22f), UrbanPalette.FoxLeaf, 27f);
            context.Sphere("Weathered Corner Stone", restingCorner + new Vector3(0.20f, 0.32f, 0.17f), new Vector3(0.18f, 0.13f, 0.16f), stone);
            context.FacetedFoliage("Southwest Weeds", southWest + new Vector3(-0.12f, 0.38f, -0.08f), new Vector3(0.24f, 0.23f, 0.21f), UrbanPalette.FoxLeaf, 11f);
            context.FacetedFoliage("Southwest Stone", southWest + new Vector3(0.18f, 0.34f, -0.11f), new Vector3(0.22f, 0.16f, 0.19f), stone, -24f);
            context.Box("Southwest Leaf Litter", southWest + new Vector3(0.17f, 0.27f, 0.17f), new Vector3(0.20f, 0.012f, 0.12f), UrbanPalette.FoxDryLeaf, 19f);
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

            public void FacetedFoliage(string name, Vector3 position, Vector3 diameter, Color color, float yaw)
            {
                LowPolyTreeVisualBuilder.BuildFacetedFoliage(
                    name, parent, position, diameter, yaw, color, material, hideFlags);
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
