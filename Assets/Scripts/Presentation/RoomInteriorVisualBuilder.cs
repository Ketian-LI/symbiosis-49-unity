using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>
    /// Creates the replaceable low-poly furnishing pass used by the prototype.
    /// Most furnishing clusters stay in room corners to preserve the authored
    /// doorway landings. The residence bed is a visual-only, pass-through kit.
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
        private static readonly Color RestaurantUpholstery = new(0.70f, 0.38f, 0.25f);
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

        public static bool IsPassThroughFurnishing(Renderer renderer)
        {
            return renderer != null && renderer.GetComponentInParent<RoomPassThroughVisual>() != null;
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
            var accent = spec.Type == RoomType.Residence
                ? ResidenceFabricFor(spec.Id)
                : AccentFor(spec.Id);
            var context = new BuildContext(root, material, hideFlags, accent);
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
            // A larger single bed fills about half the depth of a 1x1 room.
            // Its visible footprint can cross a doorway landing: inhabitants and
            // wildlife use room-level routes and must be able to pass through it.
            const float bedWidth = 1.02f;
            const float bedLength = 1.48f;
            var halfWidth = bedWidth * 0.5f;
            var halfLength = bedLength * 0.5f;
            var bed = anchor + new Vector3(-0.035f, 0f, -0.22f);
            var bedContext = context.PassThrough("Residence Bed And Nightstand");
            bedContext.Box("Bed Frame", bed + new Vector3(0f, 0.34f, 0f),
                new Vector3(bedWidth, 0.16f, bedLength), WarmWood);
            for (var side = -1; side <= 1; side += 2)
            {
                bedContext.Box($"Bed Side Rail {side}", bed + new Vector3(side * (halfWidth - 0.02f), 0.43f, 0f),
                    new Vector3(0.045f, 0.18f, bedLength - 0.02f), DarkWood);
                bedContext.Box($"Headboard Post {side}", bed + new Vector3(side * (halfWidth - 0.03f), 0.59f, halfLength - 0.02f),
                    new Vector3(0.065f, 0.39f, 0.07f), WarmWood);
                bedContext.Box($"Footboard Post {side}", bed + new Vector3(side * (halfWidth - 0.03f), 0.49f, -halfLength + 0.02f),
                    new Vector3(0.065f, 0.28f, 0.07f), WarmWood);
            }
            bedContext.Box("Mattress", bed + new Vector3(0f, 0.47f, -0.01f),
                new Vector3(bedWidth - 0.06f, 0.15f, bedLength - 0.10f), Cream);
            bedContext.Box("Headboard", bed + new Vector3(0f, 0.55f, halfLength - 0.02f),
                new Vector3(bedWidth + 0.02f, 0.46f, 0.08f), DarkWood);
            bedContext.Box("Pillow", bed + new Vector3(0f, 0.59f, halfLength - 0.22f),
                new Vector3(bedWidth * 0.64f, 0.10f, 0.24f), Color.Lerp(Cream, Color.white, 0.34f));
            bedContext.Box("Bed Cover", bed + new Vector3(0f, 0.58f, -0.20f),
                new Vector3(bedWidth - 0.10f, 0.055f, 0.84f), context.Accent);
            bedContext.Box("Duvet Foot Stitch", bed + new Vector3(0f, 0.612f, -0.58f),
                new Vector3(bedWidth - 0.17f, 0.007f, 0.018f), Color.Lerp(context.Accent, Cream, 0.20f));
            bedContext.Box("Folded Blanket", bed + new Vector3(0f, 0.61f, 0.29f),
                new Vector3(bedWidth - 0.10f, 0.025f, 0.10f), Linen);
            bedContext.Box("Bed Footboard", bed + new Vector3(0f, 0.42f, -halfLength + 0.02f),
                new Vector3(bedWidth + 0.02f, 0.22f, 0.06f), WarmWood);

            context.Box("Wardrobe", secondary + new Vector3(0f, 0.63f, 0f), new Vector3(0.55f, 0.72f, 0.36f), WarmWood);
            context.Box("Wardrobe Door A", secondary + new Vector3(-0.14f, 0.64f, -0.19f), new Vector3(0.24f, 0.62f, 0.035f), Color.Lerp(WarmWood, Cream, 0.14f));
            context.Box("Wardrobe Door B", secondary + new Vector3(0.14f, 0.64f, -0.19f), new Vector3(0.24f, 0.62f, 0.035f), Color.Lerp(WarmWood, Cream, 0.14f));
            context.Sphere("Wardrobe Handle A", secondary + new Vector3(-0.035f, 0.64f, -0.22f), Vector3.one * 0.055f, Ochre);
            context.Sphere("Wardrobe Handle B", secondary + new Vector3(0.035f, 0.64f, -0.22f), Vector3.one * 0.055f, Ochre);
            context.Box("Wardrobe Wood Top Rim", secondary + new Vector3(0f, 1.008f, 0f),
                new Vector3(0.58f, 0.025f, 0.39f), DarkWood);
            context.Box("Wardrobe Ivory Top", secondary + new Vector3(0f, 1.027f, 0f),
                new Vector3(0.47f, 0.019f, 0.29f), Cream);
            context.Box("Wardrobe Top Centre Seam", secondary + new Vector3(0f, 1.041f, 0f),
                new Vector3(0.018f, 0.008f, 0.27f), WarmWood);

            var cabinet = bed + new Vector3(halfWidth + 0.16f, 0f, halfLength - 0.26f);
            bedContext.Box("Bedside Cabinet", cabinet + new Vector3(0f, 0.49f, 0f), new Vector3(0.23f, 0.30f, 0.25f), WarmWood);
            bedContext.Box("Bedside Cabinet Top", cabinet + new Vector3(0f, 0.65f, 0f),
                new Vector3(0.27f, 0.035f, 0.29f), DarkWood);
            bedContext.Box("Bedside Cabinet Drawer", cabinet + new Vector3(0f, 0.52f, -0.13f),
                new Vector3(0.19f, 0.09f, 0.016f), DarkWood);
            bedContext.Cylinder("Bedside Lamp", cabinet + new Vector3(0f, 0.77f, 0f), new Vector3(0.10f, 0.07f, 0.10f), Cream);
            bedContext.Cylinder("Bedside Lamp Base", cabinet + new Vector3(0f, 0.68f, 0f), new Vector3(0.05f, 0.018f, 0.05f), Ochre);

            var southEast = new Vector3(secondary.x, 0f, -secondary.z);
            context.Cylinder("One Person Round Table", southEast + new Vector3(-0.08f, 0.47f, 0f), new Vector3(0.49f, 0.045f, 0.49f), WarmWood);
            context.Cylinder("Round Table Surface Grain", southEast + new Vector3(-0.08f, 0.517f, 0f),
                new Vector3(0.44f, 0.002f, 0.44f), Color.Lerp(WarmWood, Cream, 0.16f));
            context.Cylinder("Round Table Pedestal", southEast + new Vector3(-0.08f, 0.35f, 0f), new Vector3(0.055f, 0.10f, 0.055f), DarkWood);
            context.Cylinder("Round Table Cup", southEast + new Vector3(-0.18f, 0.54f, 0.09f),
                new Vector3(0.055f, 0.025f, 0.055f), Cream);
            context.Box("Round Table Book", southEast + new Vector3(0.04f, 0.54f, -0.07f),
                new Vector3(0.16f, 0.018f, 0.11f), Color.Lerp(context.Accent, DarkWood, 0.35f), 18f);
            context.Box("Single Chair Seat", southEast + new Vector3(-0.34f, 0.39f, -0.20f), new Vector3(0.18f, 0.11f, 0.18f), context.Accent);
            context.Box("Single Chair Back", southEast + new Vector3(-0.34f, 0.53f, -0.31f), new Vector3(0.19f, 0.31f, 0.05f), WarmWood);
            BuildPottedPlant(context, secondary + new Vector3(0.30f, 0f, -0.23f), "Residence Plant");
        }

        private static void BuildOffice(BuildContext context, Vector3 anchor, Vector3 secondary)
        {
            BuildWorkstation(context, anchor, "Workstation A", -1f);
            BuildWorkstation(context, secondary, "Workstation B", 1f);

            var southWest = new Vector3(anchor.x, 0f, -anchor.z);
            var southEast = new Vector3(secondary.x, 0f, -secondary.z);
            context.Box("Shared Filing Cabinet", southWest + new Vector3(0f, 0.45f, 0f),
                new Vector3(0.68f, 0.42f, 0.34f), Cream);
            context.Box("Filing Countertop", southWest + new Vector3(0f, 0.67f, 0f),
                new Vector3(0.72f, 0.035f, 0.38f), Color.Lerp(WarmWood, Cream, 0.35f));
            context.Box("Filing Drawer A", southWest + new Vector3(-0.16f, 0.43f, -0.18f),
                new Vector3(0.27f, 0.11f, 0.025f), SoftGrey);
            context.Box("Filing Drawer B", southWest + new Vector3(0.16f, 0.43f, -0.18f),
                new Vector3(0.27f, 0.11f, 0.025f), SoftGrey);
            context.Box("Compact Printer", southWest + new Vector3(0.09f, 0.76f, 0f),
                new Vector3(0.34f, 0.14f, 0.25f), Graphite);
            context.Box("Printer Paper", southWest + new Vector3(0.09f, 0.84f, 0.03f),
                new Vector3(0.23f, 0.014f, 0.15f), Color.white);
            context.Box("Printer Output Tray", southWest + new Vector3(0.09f, 0.72f, -0.16f),
                new Vector3(0.24f, 0.018f, 0.10f), Graphite);
            for (var index = 0; index < 3; index++)
            {
                context.Box($"Filing Binder {index + 1}", southWest +
                    new Vector3(-0.27f + index * 0.085f, 0.76f, 0.03f),
                    new Vector3(0.065f, 0.15f, 0.13f),
                    index == 1 ? Color.Lerp(Leaf, Cream, 0.25f) : Color.Lerp(Teal, Graphite, 0.45f));
            }
            context.Cylinder("Waste Paper Basket", southWest + new Vector3(-0.28f, 0.35f, 0.31f),
                new Vector3(0.12f, 0.10f, 0.12f), Graphite);
            var plant = southEast + new Vector3(0.10f, 0f, -0.04f);
            BuildPottedPlant(context, plant, "Office Plant");
            context.Sphere("Office Plant Broad Leaf A", plant + new Vector3(-0.17f, 0.67f, 0.08f),
                new Vector3(0.24f, 0.08f, 0.17f), Leaf);
            context.Sphere("Office Plant Broad Leaf B", plant + new Vector3(0.16f, 0.66f, -0.08f),
                new Vector3(0.25f, 0.08f, 0.18f), DeepLeaf);
        }

        private static void BuildWorkstation(BuildContext context, Vector3 anchor, string name, float outerSide)
        {
            var deskWood = Color.Lerp(WarmWood, Cream, 0.30f);
            context.Box($"{name} Desk Top", anchor + new Vector3(0f, 0.56f, 0f),
                new Vector3(0.94f, 0.085f, 0.46f), deskWood);
            context.Box($"{name} Desk Front Lip", anchor + new Vector3(0f, 0.525f, -0.23f),
                new Vector3(0.94f, 0.08f, 0.035f), WarmWood);
            foreach (var offset in new[]
                     {
                         new Vector3(-0.40f, 0.38f, -0.18f), new Vector3(0.40f, 0.38f, -0.18f),
                         new Vector3(-0.40f, 0.38f, 0.18f), new Vector3(0.40f, 0.38f, 0.18f)
                     })
            {
                context.Box($"{name} Desk Leg", anchor + offset, new Vector3(0.065f, 0.30f, 0.065f), DarkWood);
            }

            var pedestal = anchor + new Vector3(outerSide * 0.34f, 0f, 0.02f);
            context.Box($"{name} Drawer Pedestal", pedestal + new Vector3(0f, 0.39f, 0f),
                new Vector3(0.22f, 0.39f, 0.33f), Cream);
            for (var index = 0; index < 2; index++)
            {
                context.Box($"{name} Drawer {index + 1}", pedestal +
                    new Vector3(0f, 0.35f + index * 0.13f, -0.175f),
                    new Vector3(0.17f, 0.09f, 0.018f), SoftGrey);
            }

            // Low, upward-facing screens stay legible in the board's overhead view.
            context.Box($"{name} Monitor", anchor + new Vector3(0f, 0.66f, 0.09f),
                new Vector3(0.45f, 0.04f, 0.22f), Graphite);
            context.Box($"{name} Screen", anchor + new Vector3(0f, 0.685f, 0.09f),
                new Vector3(0.38f, 0.008f, 0.16f), Teal);
            context.Box($"{name} Monitor Stand", anchor + new Vector3(0f, 0.63f, -0.015f),
                new Vector3(0.10f, 0.025f, 0.06f), Graphite);
            context.Box($"{name} Keyboard", anchor + new Vector3(0f, 0.625f, -0.13f),
                new Vector3(0.39f, 0.025f, 0.11f), Graphite);
            for (var row = 0; row < 2; row++)
            {
                for (var column = 0; column < 6; column++)
                {
                    context.Box($"{name} Key {row + 1}-{column + 1}",
                        anchor + new Vector3(-0.15f + column * 0.06f, 0.641f,
                            -0.16f + row * 0.05f),
                        new Vector3(0.045f, 0.007f, 0.025f), SoftGrey);
                }
            }
            context.Box($"{name} Notebook", anchor + new Vector3(outerSide * 0.33f, 0.617f, -0.11f),
                new Vector3(0.13f, 0.018f, 0.17f), Color.Lerp(Teal, Cream, 0.32f));
            context.Cylinder($"{name} Pen Cup", anchor + new Vector3(outerSide * 0.34f, 0.66f, 0.14f),
                new Vector3(0.065f, 0.06f, 0.065f), Graphite);
            context.Sphere($"{name} Mouse", anchor + new Vector3(-outerSide * 0.29f, 0.625f, -0.12f),
                new Vector3(0.065f, 0.025f, 0.085f), Graphite);

            var chairUpholstery = Color.Lerp(Graphite, UrbanPalette.Boundary, 0.55f);
            context.Cylinder($"{name} Chair", anchor + new Vector3(0f, 0.43f, -0.31f),
                new Vector3(0.38f, 0.08f, 0.31f), chairUpholstery);
            context.Box($"{name} Chair Back", anchor + new Vector3(0f, 0.56f, -0.40f),
                new Vector3(0.40f, 0.22f, 0.09f), chairUpholstery);
            context.Cylinder($"{name} Chair Stem", anchor + new Vector3(0f, 0.31f, -0.31f),
                new Vector3(0.035f, 0.10f, 0.035f), Graphite);
            context.Box($"{name} Chair Feet", anchor + new Vector3(0f, 0.28f, -0.31f),
                new Vector3(0.30f, 0.025f, 0.12f), Graphite);
            context.Box($"{name} Chair Feet Cross", anchor + new Vector3(0f, 0.277f, -0.31f),
                new Vector3(0.12f, 0.025f, 0.30f), Graphite);
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
            if (width < 4f && depth < 4f)
            {
                BuildCompactFoodShop(context);
                return;
            }

            if (depth > width)
            {
                // Both footprints are the same room kit rotated as a whole, not two unrelated
                // arrangements. This also keeps the dining zone centred in the long dimension.
                context = context.Rotated("Food Shop Rotated Kit", 90f);
                var horizontalWidth = depth;
                depth = width;
                width = horizontalWidth;
            }

            var northWest = Corner(width, depth, -1f, 1f);
            var northEast = Corner(width, depth, 1f, 1f);
            var southWest = Corner(width, depth, -1f, -1f);
            var southEast = Corner(width, depth, 1f, -1f);

            var anchor = northWest;
            context.Box("Kitchen Counter", anchor + new Vector3(0f, 0.49f, 0.04f), new Vector3(0.84f, 0.40f, 0.63f), SoftGrey);
            context.Box("Counter Top", anchor + new Vector3(0f, 0.72f, 0.04f), new Vector3(0.90f, 0.08f, 0.65f), Cream);
            context.Box("Cooker", anchor + new Vector3(-0.22f, 0.79f, 0f), new Vector3(0.27f, 0.055f, 0.23f), Graphite);
            for (var burner = 0; burner < 4; burner++)
            {
                var x = -0.29f + burner % 2 * 0.13f;
                var z = -0.055f + burner / 2 * 0.11f;
                context.Cylinder($"Cooker Ring {burner + 1}", anchor + new Vector3(x, 0.84f, z),
                    new Vector3(0.052f, 0.015f, 0.052f), Graphite);
            }
            context.Box("Sink", anchor + new Vector3(0.22f, 0.79f, 0f), new Vector3(0.27f, 0.055f, 0.23f), Graphite);
            context.Box("Sink Basin", anchor + new Vector3(0.22f, 0.83f, 0f), new Vector3(0.20f, 0.025f, 0.16f), Teal);
            context.Box("Extraction Hood", anchor + new Vector3(0f, 0.96f, 0.17f), new Vector3(0.46f, 0.27f, 0.045f), SoftGrey);
            context.Box("Low Kitchen Divider", anchor + new Vector3(0f, 0.46f, -0.34f), new Vector3(0.75f, 0.36f, 0.07f), WarmWood);
            context.Box("Kitchen Return Divider", anchor + new Vector3(-0.45f, 0.46f, 0.02f),
                new Vector3(0.07f, 0.36f, 0.68f), WarmWood);
            context.Box("Under-counter Refrigerator", anchor + new Vector3(0.22f, 0.43f, -0.18f), new Vector3(0.22f, 0.27f, 0.06f), SoftGrey);
            context.Box("Refrigerator Handle", anchor + new Vector3(0.29f, 0.48f, -0.22f), new Vector3(0.025f, 0.13f, 0.025f), Graphite);
            context.Box("Oven Front", anchor + new Vector3(-0.22f, 0.48f, -0.225f), new Vector3(0.27f, 0.20f, 0.025f), Graphite);
            context.Box("Oven Window", anchor + new Vector3(-0.22f, 0.48f, -0.243f), new Vector3(0.19f, 0.11f, 0.012f), SoftGrey);
            context.Box("Kitchen Divider Cap", anchor + new Vector3(0f, 0.67f, -0.34f), new Vector3(0.79f, 0.05f, 0.12f), Ochre);
            context.Cylinder("Kitchen Herb Pot", anchor + new Vector3(0.34f, 0.81f, 0.25f),
                new Vector3(0.07f, 0.07f, 0.07f), Terracotta);
            context.Sphere("Kitchen Herb Leaves", anchor + new Vector3(0.34f, 0.94f, 0.25f),
                new Vector3(0.16f, 0.11f, 0.15f), Leaf);

            // The little order pedestal is separate from the working kitchen, as in the reference.
            var host = new Vector3(-1.78f, 0f, 0.34f);
            context.Box("Host Order Station", host + new Vector3(0f, 0.46f, 0f), new Vector3(0.34f, 0.36f, 0.26f), WarmWood);
            context.Box("Host Order Countertop", host + new Vector3(0f, 0.66f, 0f), new Vector3(0.40f, 0.045f, 0.30f), Cream);
            context.Box("Host Order Screen", host + new Vector3(-0.055f, 0.73f, 0f), new Vector3(0.13f, 0.10f, 0.09f), Graphite);
            context.Box("Host Order Display", host + new Vector3(-0.055f, 0.74f, -0.05f), new Vector3(0.10f, 0.065f, 0.012f), Teal);

            BuildPottedPlant(context, southWest + new Vector3(0.05f, 0f, 0.02f), "Restaurant Corner Plant");
            context.Box("Closed Food Waste Bin", southEast + new Vector3(0f, 0.39f, 0f), new Vector3(0.20f, 0.29f, 0.20f), DeepLeaf);
            context.Box("Food Waste Bin Lid", southEast + new Vector3(0f, 0.55f, 0f), new Vector3(0.23f, 0.045f, 0.23f), Graphite);
            context.Box("Food Waste Bin Handle", southEast + new Vector3(0f, 0.582f, 0f),
                new Vector3(0.07f, 0.016f, 0.035f), SoftGrey);

            // Keep the compact kitchen and booth against the wall while the two set tables
            // define the central dining area, outside all six door landings.
            BuildDiningTable(context, new Vector3(0.82f, 0f, -0.12f), "Dining Table A");
            BuildDiningTable(context, new Vector3(-0.82f, 0f, -0.12f), "Dining Table B");
            context.Box("Wall Booth Seat", northEast + new Vector3(0f, 0.40f, 0.11f), new Vector3(0.88f, 0.22f, 0.31f), RestaurantUpholstery);
            context.Box("Wall Booth Back", northEast + new Vector3(0f, 0.61f, 0.28f), new Vector3(0.88f, 0.42f, 0.09f), RestaurantUpholstery);
            context.Box("Wall Booth Cushion", northEast + new Vector3(0f, 0.525f, 0.11f),
                new Vector3(0.80f, 0.035f, 0.27f), Color.Lerp(RestaurantUpholstery, Cream, 0.22f));
            context.Box("Wall Booth Table", northEast + new Vector3(0f, 0.51f, -0.18f), new Vector3(0.70f, 0.08f, 0.32f), WarmWood);
            context.Cylinder("Wall Booth Vase", northEast + new Vector3(0f, 0.58f, -0.18f),
                new Vector3(0.038f, 0.045f, 0.038f), Terracotta);
            context.Sphere("Wall Booth Flowers", northEast + new Vector3(0f, 0.66f, -0.18f),
                new Vector3(0.10f, 0.075f, 0.10f), Leaf);
            for (var side = -1; side <= 1; side += 2)
            {
                context.Cylinder($"Wall Booth Plate {side}", northEast + new Vector3(side * 0.17f, 0.57f, -0.17f),
                    new Vector3(0.07f, 0.012f, 0.07f), Color.white);
                context.Cylinder($"Wall Booth Cup {side}", northEast + new Vector3(side * 0.17f, 0.59f, -0.26f),
                    new Vector3(0.035f, 0.045f, 0.035f), Cream);
            }
        }

        private static void BuildCompactFoodShop(BuildContext context)
        {
            // The former two-cell restaurant kit cannot fit a single module.
            // Small, visual-only furniture leaves every door landing usable.
            var furniture = context.PassThrough("Compact Food Shop Furniture");
            var counter = new Vector3(-0.82f, 0f, 0.82f);
            furniture.Box("Compact Kitchen Counter", counter + new Vector3(0f, 0.48f, 0f),
                new Vector3(0.82f, 0.34f, 0.58f), SoftGrey);
            furniture.Box("Compact Counter Top", counter + new Vector3(0f, 0.67f, 0f),
                new Vector3(0.88f, 0.06f, 0.62f), Cream);
            furniture.Box("Compact Cooker", counter + new Vector3(-0.18f, 0.72f, 0f),
                new Vector3(0.22f, 0.04f, 0.22f), Graphite);
            furniture.Box("Compact Sink", counter + new Vector3(0.18f, 0.72f, 0f),
                new Vector3(0.22f, 0.04f, 0.22f), Teal);
            var table = new Vector3(0.72f, 0f, -0.72f);
            furniture.Cylinder("Compact Dining Table", table + new Vector3(0f, 0.47f, 0f),
                new Vector3(0.56f, 0.06f, 0.56f), WarmWood);
            furniture.Cylinder("Compact Table Leg", table + new Vector3(0f, 0.32f, 0f),
                new Vector3(0.08f, 0.23f, 0.08f), DarkWood);
            furniture.Box("Compact Dining Chair", table + new Vector3(-0.48f, 0.37f, 0f),
                new Vector3(0.21f, 0.12f, 0.26f), RestaurantUpholstery);
            furniture.Cylinder("Compact Plate", table + new Vector3(0f, 0.51f, 0f),
                new Vector3(0.11f, 0.012f, 0.11f), Cream);
            BuildPottedPlant(furniture, new Vector3(0.94f, 0f, 0.94f), "Compact Shop Plant");
        }

        private static void BuildDiningTable(BuildContext context, Vector3 anchor, string name)
        {
            context.Box(name, anchor + new Vector3(0f, 0.48f, 0f), new Vector3(0.56f, 0.09f, 0.52f), WarmWood);
            context.Box($"{name} Runner", anchor + new Vector3(0f, 0.536f, 0f),
                new Vector3(0.13f, 0.012f, 0.43f), Linen);
            for (var side = -1; side <= 1; side += 2)
            {
                context.Box($"{name} Placemat {side}", anchor + new Vector3(side * 0.13f, 0.537f, 0f),
                    new Vector3(0.17f, 0.008f, 0.22f), Color.Lerp(Linen, RestaurantUpholstery, 0.25f));
            }
            for (var x = -1; x <= 1; x += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                {
                    context.Box($"{name} Leg {x} {z}", anchor + new Vector3(x * 0.21f, 0.35f, z * 0.18f),
                        new Vector3(0.045f, 0.22f, 0.045f), DarkWood);
                }
            }
            context.Box($"{name} Seat A", anchor + new Vector3(-0.37f, 0.38f, 0f), new Vector3(0.20f, 0.16f, 0.23f), RestaurantUpholstery);
            context.Box($"{name} Seat B", anchor + new Vector3(0.37f, 0.38f, 0f), new Vector3(0.20f, 0.16f, 0.23f), RestaurantUpholstery);
            context.Box($"{name} Seat Cushion A", anchor + new Vector3(-0.37f, 0.472f, 0f),
                new Vector3(0.18f, 0.018f, 0.21f), Color.Lerp(RestaurantUpholstery, Cream, 0.18f));
            context.Box($"{name} Seat Cushion B", anchor + new Vector3(0.37f, 0.472f, 0f),
                new Vector3(0.18f, 0.018f, 0.21f), Color.Lerp(RestaurantUpholstery, Cream, 0.18f));
            context.Box($"{name} Chair Back A", anchor + new Vector3(-0.51f, 0.55f, 0f),
                new Vector3(0.055f, 0.34f, 0.24f), WarmWood);
            context.Box($"{name} Chair Back B", anchor + new Vector3(0.51f, 0.55f, 0f),
                new Vector3(0.055f, 0.34f, 0.24f), WarmWood);
            context.Cylinder($"{name} Plate A", anchor + new Vector3(-0.10f, 0.548f, 0f), new Vector3(0.08f, 0.012f, 0.08f), Color.white);
            context.Cylinder($"{name} Plate B", anchor + new Vector3(0.10f, 0.548f, 0f), new Vector3(0.08f, 0.012f, 0.08f), Color.white);
            context.Cylinder($"{name} Cup A", anchor + new Vector3(-0.10f, 0.58f, 0.10f), new Vector3(0.04f, 0.05f, 0.04f), Cream);
            context.Cylinder($"{name} Cup B", anchor + new Vector3(0.10f, 0.58f, 0.10f), new Vector3(0.04f, 0.05f, 0.04f), Cream);
            context.Box($"{name} Napkin", anchor + new Vector3(0f, 0.545f, -0.12f), new Vector3(0.12f, 0.01f, 0.07f), Cream);
            context.Cylinder($"{name} Centre Vase", anchor + new Vector3(0f, 0.565f, 0.015f),
                new Vector3(0.042f, 0.045f, 0.042f), Terracotta);
            context.Sphere($"{name} Centre Leaves", anchor + new Vector3(0f, 0.64f, 0.015f),
                new Vector3(0.11f, 0.075f, 0.10f), DeepLeaf);
        }

        private static void BuildSupermarket(BuildContext context, float width, float depth)
        {
            if (width < 4f && depth < 4f)
            {
                BuildCompactSupermarket(context);
                return;
            }

            var northWest = Corner(width, depth, -1f, 1f);
            var northEast = Corner(width, depth, 1f, 1f);
            var southWest = Corner(width, depth, -1f, -1f);
            var southEast = Corner(width, depth, 1f, -1f);

            // Four compact gondolas echo the reference's stocked aisles while
            // retaining the central and side routes between the six doors.
            BuildStockedShelf(context, new Vector3(-0.82f, 0f, 0f), "Central Gondola A", Leaf, Ochre);
            BuildStockedShelf(context, new Vector3(0.82f, 0f, 0f), "Central Gondola B", Terracotta, Cream);
            BuildStockedShelf(context, new Vector3(-0.55f, 0f, 0.74f), "Rear Gondola A", Teal, Cream);
            BuildStockedShelf(context, new Vector3(0.55f, 0f, 0.74f), "Rear Gondola B", Ochre, Leaf);

            context.Box("Wall Chilled Case", northWest + new Vector3(0f, 0.55f, 0f), new Vector3(0.64f, 0.62f, 0.32f), SoftGrey);
            context.Box("Chilled Case Glass", northWest + new Vector3(0f, 0.58f, -0.17f), new Vector3(0.55f, 0.48f, 0.025f), Teal);
            context.Box("Chilled Case Handle", northWest + new Vector3(0.20f, 0.58f, -0.19f), new Vector3(0.025f, 0.22f, 0.025f), Cream);
            context.Box("Chilled Case Header", northWest + new Vector3(0f, 0.87f, -0.02f),
                new Vector3(0.70f, 0.055f, 0.35f), Graphite);
            for (var product = -1; product <= 1; product++)
            {
                context.Box($"Chilled Case Bottle {product + 2}",
                    northWest + new Vector3(product * 0.17f, 0.91f, 0f),
                    new Vector3(0.09f, 0.06f, 0.11f), product == 0 ? Cream : Teal);
            }
            context.Box("Carton Stack Bottom", northWest + new Vector3(-0.32f, 0.34f, -0.25f), new Vector3(0.18f, 0.18f, 0.18f), WarmWood);
            context.Box("Carton Stack Top", northWest + new Vector3(-0.32f, 0.52f, -0.25f), new Vector3(0.16f, 0.17f, 0.16f), Ochre);

            context.Box("Produce Crate", northEast + new Vector3(0f, 0.42f, 0f), new Vector3(0.68f, 0.28f, 0.42f), WarmWood);
            context.Box("Produce Crate Divider", northEast + new Vector3(0f, 0.58f, 0f),
                new Vector3(0.028f, 0.06f, 0.38f), DarkWood);
            for (var item = 0; item < 6; item++)
            {
                var x = item < 3 ? -0.20f : 0.20f;
                var z = -0.12f + item % 3 * 0.12f;
                context.Sphere($"Produce Item {item + 1}", northEast + new Vector3(x, 0.61f, z),
                    new Vector3(0.11f, 0.10f, 0.10f), item < 3 ? Leaf : item % 2 == 0 ? Ochre : Terracotta);
            }
            // Open produce compartments, visible from the gameplay camera.
            for (var item = 0; item < 4; item++)
            {
                context.Sphere($"Produce Front Item {item + 1}",
                    northEast + new Vector3(-0.24f + item * 0.16f, 0.60f, -0.23f),
                    new Vector3(0.075f, 0.065f, 0.075f), item % 2 == 0 ? Leaf : Ochre);
            }

            context.Box("Shopping Basket Stack", southWest + new Vector3(0.04f, 0.37f, 0f), new Vector3(0.33f, 0.22f, 0.25f), Ochre);
            context.Box("Basket Rim", southWest + new Vector3(0.04f, 0.50f, 0f), new Vector3(0.37f, 0.035f, 0.29f), DarkWood);
            BuildPottedPlant(context, southWest + new Vector3(-0.27f, 0f, 0f), "Supermarket Plant");

            context.Box("Checkout Counter", southEast + new Vector3(0f, 0.48f, 0f), new Vector3(0.72f, 0.38f, 0.40f), Cream);
            context.Box("Checkout Conveyor", southEast + new Vector3(-0.14f, 0.69f, 0f),
                new Vector3(0.34f, 0.025f, 0.31f), Graphite);
            context.Box("Checkout Register", southEast + new Vector3(0.16f, 0.75f, 0f), new Vector3(0.25f, 0.20f, 0.20f), Graphite);
            context.Box("Checkout Register Display", southEast + new Vector3(0.16f, 0.86f, -0.03f),
                new Vector3(0.14f, 0.02f, 0.10f), Teal);
            context.Box("Card Terminal", southEast + new Vector3(-0.19f, 0.72f, -0.08f), new Vector3(0.12f, 0.10f, 0.16f), Teal);
            context.Box("Closed Waste Container", southEast + new Vector3(0.36f, 0.39f, 0f), new Vector3(0.16f, 0.26f, 0.17f), DeepLeaf);
            context.Box("Waste Container Lid", southEast + new Vector3(0.36f, 0.54f, 0f), new Vector3(0.19f, 0.04f, 0.20f), Graphite);
        }

        private static void BuildCompactSupermarket(BuildContext context)
        {
            // A one-cell shop keeps a visible centre aisle. Fixtures are visual-only
            // because the four door approaches must remain traversable after swaps.
            var fixtures = context.PassThrough("Compact Supermarket Fixtures");
            BuildStockedShelf(fixtures, new Vector3(-0.72f, 0f, 0.62f),
                "Compact Gondola A", Leaf, Ochre);
            BuildStockedShelf(fixtures, new Vector3(0.72f, 0f, 0.62f),
                "Compact Gondola B", Terracotta, Cream);
            fixtures.Box("Compact Checkout Counter", new Vector3(0.74f, 0.46f, -0.78f),
                new Vector3(0.64f, 0.34f, 0.42f), Cream);
            fixtures.Box("Compact Checkout Register", new Vector3(0.74f, 0.68f, -0.78f),
                new Vector3(0.22f, 0.12f, 0.18f), Graphite);
            fixtures.Box("Compact Produce Crate", new Vector3(-0.74f, 0.36f, -0.78f),
                new Vector3(0.55f, 0.17f, 0.40f), WarmWood);
            for (var item = 0; item < 3; item++)
            {
                fixtures.Sphere($"Compact Produce {item + 1}",
                    new Vector3(-0.91f + item * 0.17f, 0.49f, -0.78f),
                    new Vector3(0.10f, 0.08f, 0.10f), item == 1 ? Ochre : Leaf);
            }
        }

        private static void BuildStockedShelf(
            BuildContext context,
            Vector3 anchor,
            string name,
            Color productA,
            Color productB)
        {
            // An open-backed gondola: a solid cabinet obscured every stocked tier
            // from the straight-down gameplay camera.
            context.Box(name, anchor + new Vector3(0f, 0.60f, 0.14f),
                new Vector3(0.72f, 0.72f, 0.055f), WarmWood);
            for (var side = -1; side <= 1; side += 2)
            {
                context.Box($"{name} Side Post {side}",
                    anchor + new Vector3(side * 0.35f, 0.60f, -0.07f),
                    new Vector3(0.045f, 0.72f, 0.40f), Ochre);
            }
            for (var row = 0; row < 3; row++)
            {
                var y = 0.37f + row * 0.22f;
                context.Box($"{name} Shelf {row + 1}", anchor + new Vector3(0f, y, -0.07f),
                    new Vector3(0.68f, 0.04f, 0.39f), Cream);
                for (var column = -2; column <= 2; column++)
                {
                    context.Box(
                        $"{name} Product {row + 1}-{column + 3}",
                        anchor + new Vector3(column * 0.13f, y + 0.09f, -0.09f),
                        new Vector3(0.105f, 0.14f, 0.13f),
                        (row + column) % 3 == 0 ? Ochre :
                        (row + column) % 2 == 0 ? productA : productB);
                }
            }
            // Product labels on the upper display face keep the three stocked
            // tiers legible when the camera becomes completely top-down.
            var topColours = new[] { productA, Cream, productB, Ochre };
            for (var column = 0; column < 4; column++)
            {
                context.Box($"{name} Top Display {column + 1}",
                    anchor + new Vector3(-0.23f + column * 0.15f, 0.986f, -0.11f),
                    new Vector3(0.115f, 0.018f, 0.17f), topColours[column]);
            }
        }

        private static void BuildGarage(BuildContext context, RoomSpec spec, float depth)
        {
            // Painted markings only: traffic is spawned separately, and no workshop clutter
            // should occupy the pedestrian doors or imply that cars are permanently parked.
            var paint = context.PassThrough("Garage Lane Paint");
            var laneX = GarageVisualLayout.LaneCenterX(spec.Width);
            var markZ = Mathf.Min(0.70f, depth * 0.5f - 1.0f);
            for (var side = -1; side <= 1; side += 2)
            {
                for (var end = -1; end <= 1; end += 2)
                {
                    paint.Box(
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

            // Plaza furniture stays in the four corners. The centre remains a flat,
            // unobstructed gathering area for pigeons and the room's door routes.
            context.Box("Plaza Planter Base", northWest + new Vector3(0f, 0.31f, 0f),
                new Vector3(0.64f, 0.12f, 0.55f), UrbanPalette.PigeonVentHighlight);
            context.Box("Plaza Planter Soil", northWest + new Vector3(0f, 0.377f, 0f),
                new Vector3(0.53f, 0.014f, 0.44f), DarkWood);
            context.FacetedFoliage("Plaza Planter Greenery A", northWest + new Vector3(-0.13f, 0.47f, 0.02f),
                new Vector3(0.29f, 0.23f, 0.28f), UrbanPalette.ShrubLeaf, 19f);
            context.FacetedFoliage("Plaza Planter Greenery B", northWest + new Vector3(0.14f, 0.44f, -0.04f),
                new Vector3(0.25f, 0.18f, 0.24f), UrbanPalette.ShrubLeafHighlight, -21f);

            context.Box("Plaza Stone Bench Seat", northEast + new Vector3(0f, 0.46f, 0f),
                new Vector3(0.78f, 0.07f, 0.25f), UrbanPalette.PigeonVentHighlight);
            context.Box("Plaza Stone Bench Back", northEast + new Vector3(0f, 0.58f, 0.12f),
                new Vector3(0.78f, 0.22f, 0.055f), UrbanPalette.PigeonVentMetal);
            for (var side = -1; side <= 1; side += 2)
            {
                context.Box(side < 0 ? "Plaza Bench Left Support" : "Plaza Bench Right Support",
                    northEast + new Vector3(side * 0.29f, 0.34f, 0f),
                    new Vector3(0.08f, 0.19f, 0.22f), SoftGrey);
            }

            context.Cylinder("Plaza Bird Bath Pedestal", southWest + new Vector3(0f, 0.30f, 0f),
                new Vector3(0.37f, 0.045f, 0.37f), UrbanPalette.PigeonVentMetal);
            context.Cylinder("Plaza Bird Bath Rim", southWest + new Vector3(0f, 0.35f, 0f),
                new Vector3(0.55f, 0.025f, 0.55f), Cream);
            context.Cylinder("Plaza Bird Bath Water", southWest + new Vector3(0f, 0.38f, 0f),
                new Vector3(0.43f, 0.007f, 0.43f), UrbanPalette.PigeonWater);

            context.Box("Plaza Feeding Stone", southEast + new Vector3(0f, 0.275f, 0f),
                new Vector3(0.45f, 0.025f, 0.32f), UrbanPalette.PigeonVentHighlight);
            for (var index = 0; index < 4; index++)
            {
                context.Sphere($"Plaza Scattered Seed {index + 1}",
                    southEast + new Vector3(-0.13f + index * 0.08f, 0.31f, index % 2 == 0 ? -0.04f : 0.05f),
                    Vector3.one * 0.025f, Ochre);
            }
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
                new Vector3(0.42f, 0.28f, 0.38f), UrbanPalette.ShrubLeaf, 18f);
            context.FacetedFoliage($"{name} Low Tuft B",
                anchor + new Vector3(-inwardX * 0.17f, 0.33f, inwardZ * 0.15f),
                new Vector3(0.34f, 0.22f, 0.30f), UrbanPalette.ShrubDeepLeaf, -31f);
            context.FacetedFoliage($"{name} Low Tuft C",
                anchor + new Vector3(inwardX * 0.29f, 0.35f, inwardZ * 0.24f),
                new Vector3(0.30f, 0.21f, 0.31f), UrbanPalette.ShrubLeafHighlight, 9f);
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

        private static Color ResidenceFabricFor(string id)
        {
            // The same room kit retains the four bedding variations from the
            // residence reference sheet, with the sage variant used by residence-c.
            var variant = 0;
            foreach (var character in id ?? string.Empty)
            {
                variant += character;
            }
            return (variant % 4) switch
            {
                0 => new Color(0.48f, 0.61f, 0.67f),
                1 => new Color(0.66f, 0.49f, 0.54f),
                2 => new Color(0.45f, 0.55f, 0.40f),
                _ => new Color(0.74f, 0.57f, 0.32f)
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

            public BuildContext Rotated(string name, float degrees)
            {
                var group = NewGroup(name, parent, hideFlags);
                group.localRotation = Quaternion.Euler(0f, degrees, 0f);
                return new BuildContext(group, material, hideFlags, Accent);
            }

            public BuildContext PassThrough(string name)
            {
                var group = NewGroup(name, parent, hideFlags);
                group.gameObject.AddComponent<RoomPassThroughVisual>();
                return new BuildContext(group, material, hideFlags, Accent);
            }

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
