using UnityEngine;
using UnityEngine.Rendering;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    public static partial class MapBuilder
    {
        // Floor layer orders used by the town, back to front.
        private const int TownGrass = 0;
        private const int TownGrassPatch = 1;
        private const int TownGroundDetail = 2;
        private const int TownPark = 5;
        private const int TownWalkOutline = 8;
        private const int TownWalk = 9;
        private const int TownWalkPlate = 10;
        private const int TownPlaza = 14;
        private const int TownRoadOutline = 22;
        private const int TownRoad = 23;
        private const int TownAlley = 24;
        private const int TownRoadMark = 25;
        private const int TownShadow = 50;

        private enum Entrance
        {
            Door,
            Garage,
            Window,
            None
        }

        private struct BuildingSpec
        {
            public string Name;
            public string Sign;
            public string Roof;
            public string Wall;
            public float XMin;
            public float XMax;
            public float YBottom;
            public float YTop;
            public float DoorX;
            public float FacadeHeight;
            public Entrance Entrance;
            public string AwningA;
            public string AwningB;
        }

        /// <summary>Ground taken by the buildings, so the ground cover is not planted under them.</summary>
        private static readonly System.Collections.Generic.List<Rect> footprints = new System.Collections.Generic.List<Rect>();

        private static Transform Root(Transform parent, string name, Vector2 position, float footY)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var group = go.AddComponent<SortingGroup>();
            group.sortingLayerName = LayerWorld;
            group.sortingOrder = YSort.OrderFor(footY);
            return go.transform;
        }

        /// <summary>Draws something flat on the ground layer (shadows, paths, water) and returns to the world layer.</summary>
        private static SpriteRenderer OnGround(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order)
        {
            string previous = currentLayer;
            currentLayer = LayerFloor;
            var renderer = Shape(parent, name, sprite, center, size, color, order);
            currentLayer = previous;
            return renderer;
        }

        private static void GroundShadow(Transform shadows, Vector2 center, Vector2 size, float alpha = 0.28f)
        {
            var color = GamePalette.Outline;
            color.a = alpha;
            OnGround(shadows, "Shadow", softCircle, center, size, color, TownShadow);
        }

        private static void GroundShadowRect(Transform shadows, Vector2 center, Vector2 size, float alpha = 0.25f)
        {
            var color = GamePalette.Outline;
            color.a = alpha * 0.55f;
            OnGround(shadows, "Shadow", softCircle, center, size, color, TownShadow);
        }

        private static Transform Building(Transform parent, Transform shadows, BuildingSpec b)
        {
            buildingSpecs.Add(b);
            footprints.Add(new Rect(b.XMin - 1f, b.YBottom - 1.5f, b.XMax - b.XMin + 2f, b.YTop - b.YBottom + b.FacadeHeight + 3f));
            float cx = (b.XMin + b.XMax) * 0.5f;
            float w = b.XMax - b.XMin;
            float h = b.YTop - b.YBottom;
            float fh = b.FacadeHeight;
            float dx = b.DoorX - cx;
            float roofH = h - fh + 0.15f;

            var root = Root(parent, b.Name, new Vector2(cx, b.YBottom), b.YBottom);
            var wall = GamePalette.Hex(b.Wall);
            var roof = GamePalette.Hex(b.Roof);

            GroundShadowRect(shadows, new Vector2(cx + 0.35f, b.YBottom - 0.12f), new Vector2(w + 1.4f, 2.0f));

            // Main building silhouette & warm plaster facade
            Shape(root, "Outline", rounded, new Vector2(0f, h * 0.5f), new Vector2(w + 0.18f, h + 0.18f), GamePalette.Outline, 0);
            Shape(root, "Facade", rounded, new Vector2(0f, (fh + 0.4f) * 0.5f), new Vector2(w, fh + 0.4f), wall, 1);
            if (facadeBrickTex != null)
            {
                TiledShape(root, "Facade_Texture", facadeBrickTex, new Vector2(0f, (fh + 0.4f) * 0.5f), new Vector2(w - 0.12f, fh + 0.24f), wall, 1);
            }

            // Under-eave cast shadow on the upper wall
            Shape(root, "Facade_EaveShade", square, new Vector2(0f, fh - 0.20f), new Vector2(w - 0.06f, 0.38f), GamePalette.Shade(wall, 0.82f), 2);

            // Tall exposed colonial red-brick wainscot (zocalo) or neoclassical marble plinth
            if (b.Name == "Banco")
            {
                Shape(root, "MarblePlinth_Out", roundedSmall, new Vector2(0f, 0.28f), new Vector2(w + 0.24f, 0.60f), GamePalette.Outline, 2);
                Shape(root, "MarblePlinth", roundedSmall, new Vector2(0f, 0.28f), new Vector2(w + 0.10f, 0.48f), GamePalette.Hex("#CFD7E6"), 3);
                Shape(root, "MarbleStep_Out", roundedSmall, new Vector2(0f, 0.08f), new Vector2(w + 0.46f, 0.28f), GamePalette.Outline, 3);
                Shape(root, "MarbleStep", roundedSmall, new Vector2(0f, 0.08f), new Vector2(w + 0.34f, 0.18f), GamePalette.Hex("#E2E8F4"), 4);
            }
            else if (b.Name == "Comisaria")
            {
                float wainscotH = Mathf.Min(1.02f, fh * 0.36f);
                Shape(root, "Wainscot_Outline", roundedSmall, new Vector2(0f, wainscotH * 0.5f + 0.02f), new Vector2(w + 0.08f, wainscotH + 0.10f), GamePalette.Outline, 2);
                Shape(root, "Wainscot_Base", roundedSmall, new Vector2(0f, wainscotH * 0.5f), new Vector2(w - 0.04f, wainscotH), GamePalette.Hex("#475569"), 3);
                for (float bx = -w * 0.45f; bx < w * 0.45f; bx += 1.8f)
                {
                    Shape(root, "StoneJoint", square, new Vector2(bx, wainscotH * 0.5f), new Vector2(0.04f, wainscotH - 0.08f), GamePalette.Hex("#334155"), 4);
                }
                Shape(root, "Wainscot_LedgeOut", roundedSmall, new Vector2(0f, wainscotH + 0.04f), new Vector2(w + 0.14f, 0.18f), GamePalette.Outline, 3);
                Shape(root, "Wainscot_Ledge", roundedSmall, new Vector2(0f, wainscotH + 0.04f), new Vector2(w + 0.04f, 0.10f), GamePalette.Hex("#64748B"), 4);
            }
            else if (b.Name == "Hospital")
            {
                float wainscotH = Mathf.Min(1.02f, fh * 0.36f);
                Shape(root, "Wainscot_Outline", roundedSmall, new Vector2(0f, wainscotH * 0.5f + 0.02f), new Vector2(w + 0.08f, wainscotH + 0.10f), GamePalette.Outline, 2);
                Shape(root, "Wainscot_Base", roundedSmall, new Vector2(0f, wainscotH * 0.5f), new Vector2(w - 0.04f, wainscotH), GamePalette.Hex("#CBD5E1"), 3);
                Shape(root, "Wainscot_LedgeOut", roundedSmall, new Vector2(0f, wainscotH + 0.04f), new Vector2(w + 0.14f, 0.18f), GamePalette.Outline, 3);
                Shape(root, "Wainscot_Ledge", roundedSmall, new Vector2(0f, wainscotH + 0.04f), new Vector2(w + 0.04f, 0.10f), GamePalette.Hex("#E2E8F0"), 4);
            }
            else
            {
                float wainscotH = Mathf.Min(1.02f, fh * 0.36f);
                Shape(root, "Wainscot_Outline", roundedSmall, new Vector2(0f, wainscotH * 0.5f + 0.02f), new Vector2(w + 0.08f, wainscotH + 0.10f), GamePalette.Outline, 2);
                if (brickWainscotTex != null)
                {
                    TiledShape(root, "Wainscot_Bricks", brickWainscotTex, new Vector2(0f, wainscotH * 0.5f + 0.02f), new Vector2(w - 0.04f, wainscotH), GamePalette.White, 3);
                }
                else
                {
                    Shape(root, "Wainscot_Fallback", roundedSmall, new Vector2(0f, wainscotH * 0.5f), new Vector2(w - 0.04f, wainscotH), GamePalette.Hex("#B85238"), 3);
                }

                // Carved stone trim ledge capping the brick wainscot
                Shape(root, "Wainscot_LedgeOut", roundedSmall, new Vector2(0f, wainscotH + 0.04f), new Vector2(w + 0.14f, 0.18f), GamePalette.Outline, 3);
                Shape(root, "Wainscot_Ledge", roundedSmall, new Vector2(0f, wainscotH + 0.04f), new Vector2(w + 0.04f, 0.10f), GamePalette.Hex("#D8CBB8"), 4);
            }

            // Colonial barrel-tiled roof with high-contrast 3-tone cel shading, scalloped eave trim, and ridge cap
            float roofY = fh - 0.15f + roofH * 0.5f;
            Shape(root, "Roof_Outline", rounded, new Vector2(0f, roofY), new Vector2(w + 0.72f, roofH + 0.20f), GamePalette.Outline, 3);
            Shape(root, "Roof", rounded, new Vector2(0f, roofY), new Vector2(w + 0.54f, roofH), roof, 4);
            if (roofTileTex != null)
            {
                TiledShape(root, "Roof_TilePattern", roofTileTex, new Vector2(0f, roofY), new Vector2(w + 0.44f, Mathf.Max(0.4f, roofH - 0.12f)), GamePalette.Shade(roof, 1.18f), 5);
            }

            if (roofEaveTex != null)
            {
                TiledShape(root, "Roof_EaveScallops", roofEaveTex, new Vector2(0f, fh - 0.06f), new Vector2(w + 0.52f, 0.54f), GamePalette.Shade(roof, 1.14f), 6);
            }
            else
            {
                Shape(root, "Eave_Outline", roundedSmall, new Vector2(0f, fh - 0.02f), new Vector2(w + 0.56f, 0.36f), GamePalette.Outline, 5);
                Shape(root, "Eave", roundedSmall, new Vector2(0f, fh - 0.02f), new Vector2(w + 0.44f, 0.24f), GamePalette.Shade(roof, 0.74f), 6);
            }

            Shape(root, "Ridge_Outline", roundedSmall, new Vector2(0f, h - 0.24f), new Vector2(w + 0.10f, 0.34f), GamePalette.Outline, 5);
            Shape(root, "Ridge", roundedSmall, new Vector2(0f, h - 0.24f), new Vector2(w - 0.04f, 0.22f), GamePalette.Shade(roof, 1.24f), 6);

            // Lush green moss tufts on terracotta/colonial roofs
            if (b.Name == "Casa" || b.Name == "Pulperia" || b.Name.StartsWith("Casa_"))
            {
                RoofMossPatch(root, new Vector2(-w * 0.32f, fh + 0.55f), 0.9f, 5);
                RoofMossPatch(root, new Vector2(-w * 0.12f, fh + 1.45f), 0.75f, 5);
                RoofMossPatch(root, new Vector2(w * 0.28f, fh + 0.85f), 0.8f, 5);
            }

            DrawEntrance(root, b, dx, wall);
            DrawFacadeWindows(root, b, w, dx, wall);

            if (b.AwningA != null)
            {
                DrawAwning(root, w, fh, b.AwningA, b.AwningB);
            }

            // Custom landmark architecture for Casa, Banco, Tienda, Inmobiliaria, Agencia, Hospital, and Comisaria
            if (b.Name == "Casa")
            {
                DecorateCasaArchitecture(root, w, fh, h, dx, wall, roof);
            }
            else if (b.Name == "Banco")
            {
                DecorateBancoArchitecture(root, w, fh, h, dx, wall, roof);
            }
            else if (b.Name == "Tienda")
            {
                DecorateTiendaArchitecture(root, w, fh, h, dx, wall, roof);
            }
            else if (b.Name == "Inmobiliaria")
            {
                DecorateInmobiliariaArchitecture(root, w, fh, h, dx, wall, roof);
            }
            else if (b.Name == "AgenciaDeCarros")
            {
                DecorateAgenciaArchitecture(root, w, fh, h, dx, wall, roof);
            }
            else if (b.Name == "Hospital")
            {
                DecorateHospitalArchitecture(root, w, fh, h, dx, wall, roof);
            }
            else if (b.Name == "Comisaria")
            {
                DecorateComisariaArchitecture(root, w, fh, h, dx, wall, roof);
            }
            else if (!string.IsNullOrEmpty(b.Sign))
            {
                DrawSign(root, b.Sign, dx, fh + 0.62f, wall);
            }

            var collider = root.gameObject.AddComponent<BoxCollider2D>();
            collider.offset = new Vector2(0f, h * 0.5f);
            collider.size = new Vector2(w, h);
            return root;
        }

        private static void RoofMossPatch(Transform root, Vector2 pos, float scale, int order)
        {
            Shape(root, "Moss_Out", circle, pos, new Vector2(0.76f, 0.34f) * scale, GamePalette.Outline, order);
            Shape(root, "Moss", circle, pos, new Vector2(0.64f, 0.24f) * scale, GamePalette.Hex("#4EA836"), order);
            Shape(root, "Moss_Light", circle, pos + new Vector2(-0.08f, 0.03f) * scale, new Vector2(0.36f, 0.14f) * scale, GamePalette.Hex("#72CC4E"), order);
        }

        /// <summary>
        /// Transforms "CASA DE MAMÁ" into the L-shaped colonial Costa Rican home from the concept art:
        /// protruding right gable roof with diagonal terracotta ridge tiles, wooden plank sign "CASA DE MAMÁ",
        /// and a recessed left garden alcove with potted Monstera and Parlour Palm plants.
        /// </summary>
        private static void DecorateCasaArchitecture(Transform root, float w, float fh, float h, float dx, Color wall, Color roof)
        {
            // Protruding right wing gable (Frontón Triangular) centered over the door & right window
            float gableX = 1.15f;
            float gableW = 5.4f;
            float gableH = 2.35f;
            float gableY = fh + gableH * 0.5f - 0.08f;

            if (triangle != null)
            {
                // Diagonal roof shadow & dark outline behind the triangular gable
                Shape(root, "Gable_Shadow", triangle, new Vector2(gableX, gableY - 0.10f), new Vector2(gableW + 0.55f, gableH + 0.34f), new Color(0.08f, 0.06f, 0.06f, 0.38f), 6);
                Shape(root, "Gable_Outline", triangle, new Vector2(gableX, gableY), new Vector2(gableW + 0.44f, gableH + 0.26f), GamePalette.Outline, 6);
                Shape(root, "Gable_RoofRake", triangle, new Vector2(gableX, gableY), new Vector2(gableW + 0.24f, gableH + 0.12f), GamePalette.Shade(roof, 1.15f), 7);
                Shape(root, "Gable_WallOut", triangle, new Vector2(gableX, gableY - 0.12f), new Vector2(gableW - 0.55f, gableH - 0.32f), GamePalette.Outline, 7);
                Shape(root, "Gable_Wall", triangle, new Vector2(gableX, gableY - 0.14f), new Vector2(gableW - 0.72f, gableH - 0.44f), GamePalette.Shade(wall, 0.96f), 8);
            }

            // Diagonal stepped terracotta ridge caps along the left and right slopes of the gable
            for (int step = 0; step < 6; step++)
            {
                float t = step / 5f;
                float lx = gableX - (gableW * 0.48f) * (1f - t);
                float rx = gableX + (gableW * 0.48f) * (1f - t);
                float sy = (fh - 0.02f) + t * (gableH - 0.12f);
                var tileL = Shape(root, "RakeTileL_Out", roundedSmall, new Vector2(lx, sy), new Vector2(0.68f, 0.32f), GamePalette.Outline, 8);
                var tileLB = Shape(root, "RakeTileL", roundedSmall, new Vector2(lx, sy), new Vector2(0.56f, 0.22f), GamePalette.Shade(roof, 1.22f), 9);
                Tilt(tileL, 38f);
                Tilt(tileLB, 38f);

                var tileR = Shape(root, "RakeTileR_Out", roundedSmall, new Vector2(rx, sy), new Vector2(0.68f, 0.32f), GamePalette.Outline, 8);
                var tileRB = Shape(root, "RakeTileR", roundedSmall, new Vector2(rx, sy), new Vector2(0.56f, 0.22f), GamePalette.Shade(roof, 0.95f), 9);
                Tilt(tileR, -38f);
                Tilt(tileRB, -38f);
            }

            // Large weathered wooden plank sign "CASA DE MAMÁ" mounted on the gable
            DrawWoodenPlankSign(root, "CASA DE MAMÁ", new Vector2(gableX, fh + 0.68f), 4.4f, 0.88f, 10);

            // Potted Monstera deliciosa & Parlour Palms in terracotta pots flanking the house and garden
            BuildMonsteraPot(root, new Vector2(-3.55f, -0.15f), 1.05f, 12);
            BuildTropicalPalmPot(root, new Vector2(-1.45f, 0.05f), 0.95f, 12);
            BuildTropicalPalmPot(root, new Vector2(-0.38f, 0.08f), 1.05f, 12);
            BuildSmallFernPot(root, new Vector2(1.38f, 0.02f), 0.88f, 12);
            BuildTropicalPalmPot(root, new Vector2(3.65f, -0.05f), 0.95f, 12);
        }

        /// <summary>
        /// Transforms "BANCO" into the Neoclassical Bank from the concept art:
        /// grand white marble triangular pediment above the 4 fluted columns, double glass doors with brass handles,
        /// and "BANCO" mounted inside the pediment tympanum.
        /// </summary>
        private static void DecorateBancoArchitecture(Transform root, float w, float fh, float h, float dx, Color wall, Color roof)
        {
            float pedY = fh + 1.05f;
            float pedW = w + 0.65f;
            float pedH = 2.25f;

            // Classical architrave beam directly atop the columns
            Shape(root, "Architrave_Out", roundedSmall, new Vector2(0f, fh - 0.02f), new Vector2(pedW + 0.16f, 0.42f), GamePalette.Outline, 9);
            Shape(root, "Architrave", roundedSmall, new Vector2(0f, fh - 0.02f), new Vector2(pedW + 0.04f, 0.30f), GamePalette.Hex("#E3E9F4"), 10);

            if (triangle != null)
            {
                Shape(root, "Pediment_Shadow", triangle, new Vector2(0f, pedY - 0.08f), new Vector2(pedW + 0.32f, pedH + 0.24f), new Color(0.06f, 0.08f, 0.14f, 0.35f), 9);
                Shape(root, "Pediment_Outline", triangle, new Vector2(0f, pedY), new Vector2(pedW + 0.24f, pedH + 0.18f), GamePalette.Outline, 10);
                Shape(root, "Pediment_Cornice", triangle, new Vector2(0f, pedY), new Vector2(pedW + 0.06f, pedH + 0.04f), GamePalette.Hex("#F2F5FA"), 11);
                Shape(root, "Pediment_InnerOut", triangle, new Vector2(0f, pedY - 0.10f), new Vector2(pedW - 0.95f, pedH - 0.42f), GamePalette.Outline, 11);
                Shape(root, "Pediment_Tympanum", triangle, new Vector2(0f, pedY - 0.12f), new Vector2(pedW - 1.12f, pedH - 0.54f), GamePalette.Hex("#DCE3F0"), 12);
            }

            // Gold-framed neoclassical "BANCO" plaque inside the pediment
            Shape(root, "BankSign_Out", roundedSmall, new Vector2(0f, fh + 0.72f), new Vector2(3.36f, 0.82f), GamePalette.Outline, 12);
            Shape(root, "BankSign_Brass", roundedSmall, new Vector2(0f, fh + 0.72f), new Vector2(3.22f, 0.68f), GamePalette.Hex("#C89B3C"), 13);
            Shape(root, "BankSign_Plate", roundedSmall, new Vector2(0f, fh + 0.72f), new Vector2(3.02f, 0.52f), GamePalette.Hex("#FBF6E8"), 14);
            Label(root, "BANCO", new Vector2(0f, fh + 0.72f), GamePalette.Hex("#5C3D1E"), 0.042f, TextAnchor.MiddleCenter, 15);

            // Double blue-glass entrance doors with gold frame and vertical brass handles
            Shape(root, "GlassDoor_Out", roundedSmall, new Vector2(dx, 1.18f), new Vector2(1.86f, 2.18f), GamePalette.Outline, 7);
            Shape(root, "GlassDoor_Frame", roundedSmall, new Vector2(dx, 1.18f), new Vector2(1.72f, 2.04f), GamePalette.Hex("#C89B3C"), 8);
            foreach (float side in new[] { -0.40f, 0.40f })
            {
                Shape(root, "GlassLeaf_Out", roundedSmall, new Vector2(dx + side, 1.18f), new Vector2(0.74f, 1.84f), GamePalette.Outline, 8);
                Shape(root, "GlassLeaf", roundedSmall, new Vector2(dx + side, 1.18f), new Vector2(0.64f, 1.74f), GamePalette.Hex("#88C8E6"), 9);
                var shine = Shape(root, "GlassReflection", roundedSmall, new Vector2(dx + side - 0.06f, 1.26f), new Vector2(0.16f, 1.15f), new Color(1f, 1f, 1f, 0.55f), 10);
                Tilt(shine, -18f);
                Shape(root, "BrassHandle", roundedSmall, new Vector2(dx + side * 0.32f, 1.15f), new Vector2(0.08f, 0.36f), GamePalette.Hex("#FFD23F"), 10);
            }

            BuildTropicalPalmPot(root, new Vector2(-w * 0.46f, 0.05f), 0.95f, 12);
        }

        /// <summary>
        /// Transforms "TIENDA" into the Warm Colonial Market Storefront from the concept art:
        /// warm yellow triangular gable front crowned with purple roof rakes, "TIENDA" header,
        /// and wooden produce crates overflowing with fresh fruit and vegetables out front.
        /// </summary>
        private static void DecorateTiendaArchitecture(Transform root, float w, float fh, float h, float dx, Color wall, Color roof)
        {
            float gableY = fh + 1.35f;
            float gableW = w + 0.30f;
            float gableH = 2.75f;

            if (triangle != null)
            {
                Shape(root, "TiendaGable_Out", triangle, new Vector2(0f, gableY), new Vector2(gableW + 0.36f, gableH + 0.24f), GamePalette.Outline, 9);
                Shape(root, "TiendaGable_Rake", triangle, new Vector2(0f, gableY), new Vector2(gableW + 0.16f, gableH + 0.10f), GamePalette.Shade(roof, 1.12f), 10);
                Shape(root, "TiendaGable_WallOut", triangle, new Vector2(0f, gableY - 0.12f), new Vector2(gableW - 0.55f, gableH - 0.32f), GamePalette.Outline, 10);
                Shape(root, "TiendaGable_Wall", triangle, new Vector2(0f, gableY - 0.14f), new Vector2(gableW - 0.72f, gableH - 0.44f), wall, 11);
            }

            // "TIENDA" painted boldly near the apex of the yellow gable, above the red "CRÉDITO FÁCIL" banner
            Label(root, "TIENDA", new Vector2(dx, fh + 2.48f), GamePalette.Hex("#4A2C18"), 0.040f, TextAnchor.MiddleCenter, 15);

            // Wooden produce display crates flanking the storefront entrance (non-blocking visual props)
            BuildProduceCrate(root, new Vector2(dx - 2.35f, -0.05f), new Vector2(1.75f, 0.95f), true, 12);
            BuildProduceCrate(root, new Vector2(dx + 2.15f, -0.05f), new Vector2(1.85f, 1.05f), false, 12);
        }

        /// <summary>
        /// Transforms "INMOBILIARIA" into a contemporary real estate office:
        /// corporate teal and gold signplate with house emblems, commercial glass entrance door,
        /// large storefront showcase windows displaying architectural house floor plans and property listing cards,
        /// and manicured spherical topiary shrub planters flanking the facade.
        /// </summary>
        private static void DecorateInmobiliariaArchitecture(Transform root, float w, float fh, float h, float dx, Color wall, Color roof)
        {
            // Corporate Teal & Gold Signplate
            float signY = fh + 0.62f;
            Shape(root, "Sign_Shadow", roundedSmall, new Vector2(dx, signY - 0.05f), new Vector2(4.8f, 0.90f), new Color(0f, 0f, 0f, 0.32f), 9);
            Shape(root, "Sign_Outline", roundedSmall, new Vector2(dx, signY), new Vector2(4.8f, 0.86f), GamePalette.Outline, 10);
            Shape(root, "Sign_Back", roundedSmall, new Vector2(dx, signY), new Vector2(4.64f, 0.70f), GamePalette.Hex("#1A5A65"), 11);
            Shape(root, "Sign_GoldBezel", roundedSmall, new Vector2(dx, signY), new Vector2(4.44f, 0.52f), GamePalette.Hex("#D4AF37"), 12);
            Shape(root, "Sign_Face", roundedSmall, new Vector2(dx, signY), new Vector2(4.30f, 0.40f), GamePalette.Hex("#226E7C"), 13);
            Label(root, "INMOBILIARIA", new Vector2(dx, signY), GamePalette.Hex("#FFFBF0"), 0.038f, TextAnchor.MiddleCenter, 15);

            // Mini gold house emblems flanking the text
            foreach (float side in new[] { -1.65f, 1.65f })
            {
                if (triangle != null)
                {
                    Shape(root, "HouseIcon_Roof", triangle, new Vector2(dx + side, signY + 0.07f), new Vector2(0.36f, 0.22f), GamePalette.Hex("#FFD23F"), 14);
                }
                Shape(root, "HouseIcon_Body", square, new Vector2(dx + side, signY - 0.06f), new Vector2(0.24f, 0.16f), GamePalette.Hex("#FFD23F"), 14);
                Shape(root, "HouseIcon_Door", square, new Vector2(dx + side, signY - 0.09f), new Vector2(0.08f, 0.10f), GamePalette.Hex("#1A5A65"), 15);
            }

            // Commercial Glass Entrance Door at dx
            Shape(root, "Step_Outline", roundedSmall, new Vector2(dx, 0.10f), new Vector2(2.10f, 0.26f), GamePalette.Outline, 5);
            Shape(root, "Step", roundedSmall, new Vector2(dx, 0.10f), new Vector2(1.98f, 0.18f), GamePalette.Hex("#CBD5E1"), 5);
            Shape(root, "WelcomeMat_Out", roundedSmall, new Vector2(dx, -0.08f), new Vector2(1.36f, 0.32f), GamePalette.Outline, 5);
            Shape(root, "WelcomeMat", roundedSmall, new Vector2(dx, -0.08f), new Vector2(1.22f, 0.22f), GamePalette.Hex("#1A5A65"), 5);

            Shape(root, "DoorFrame_Out", roundedSmall, new Vector2(dx, 1.10f), new Vector2(1.64f, 2.16f), GamePalette.Outline, 5);
            Shape(root, "DoorFrame", roundedSmall, new Vector2(dx, 1.10f), new Vector2(1.50f, 2.02f), GamePalette.Hex("#2C3E50"), 6);
            Shape(root, "DoorGlass_Out", roundedSmall, new Vector2(dx, 1.10f), new Vector2(1.30f, 1.84f), GamePalette.Outline, 6);
            Shape(root, "DoorGlass", roundedSmall, new Vector2(dx, 1.10f), new Vector2(1.20f, 1.74f), GamePalette.Hex("#8EC5DF"), 7);
            Shape(root, "DoorKickplate", square, new Vector2(dx, 0.38f), new Vector2(1.20f, 0.36f), GamePalette.Hex("#2A7F8C"), 8);
            Shape(root, "DoorTransom", square, new Vector2(dx, 1.68f), new Vector2(1.20f, 0.06f), GamePalette.Hex("#2C3E50"), 8);
            Shape(root, "DoorHandle_Out", roundedSmall, new Vector2(dx + 0.42f, 1.05f), new Vector2(0.12f, 0.58f), GamePalette.Outline, 8);
            Shape(root, "DoorHandle", roundedSmall, new Vector2(dx + 0.42f, 1.05f), new Vector2(0.06f, 0.52f), GamePalette.Hex("#E2E8F0"), 9);
            var dShine = Shape(root, "DoorShine", roundedSmall, new Vector2(dx - 0.16f, 1.25f), new Vector2(0.18f, 1.10f), new Color(1f, 1f, 1f, 0.42f), 8);
            Tilt(dShine, -20f);

            // Large Vitrinas / Showcase Windows flanking the door
            foreach (float side in new[] { -2.75f, 2.75f })
            {
                float wx = dx + side;
                Shape(root, "Vitrina_Out", roundedSmall, new Vector2(wx, 1.58f), new Vector2(2.54f, 1.54f), GamePalette.Outline, 5);
                Shape(root, "Vitrina_Frame", roundedSmall, new Vector2(wx, 1.58f), new Vector2(2.40f, 1.40f), GamePalette.Hex("#2C3E50"), 6);
                Shape(root, "Vitrina_Glass", roundedSmall, new Vector2(wx, 1.58f), new Vector2(2.24f, 1.24f), GamePalette.Hex("#96CCE3"), 7);

                var vitShine = Shape(root, "Vitrina_Shine", roundedSmall, new Vector2(wx - 0.45f, 1.75f), new Vector2(0.24f, 0.85f), new Color(1f, 1f, 1f, 0.45f), 9);
                Tilt(vitShine, -20f);
            }

            // Left Window (dx - 2.75f): Architectural House Blueprint Floor Plan + Listing
            float leftWx = dx - 2.75f;
            Shape(root, "Blueprint_Sheet", roundedSmall, new Vector2(leftWx - 0.42f, 1.58f), new Vector2(1.05f, 0.90f), GamePalette.Hex("#1E4A84"), 8);
            Shape(root, "BP_RoomA", square, new Vector2(leftWx - 0.62f, 1.72f), new Vector2(0.40f, 0.36f), new Color(0.85f, 0.95f, 1f, 0.75f), 9);
            Shape(root, "BP_RoomB", square, new Vector2(leftWx - 0.22f, 1.72f), new Vector2(0.32f, 0.36f), new Color(0.85f, 0.95f, 1f, 0.75f), 9);
            Shape(root, "BP_RoomC", square, new Vector2(leftWx - 0.42f, 1.42f), new Vector2(0.72f, 0.28f), new Color(0.85f, 0.95f, 1f, 0.75f), 9);
            Shape(root, "BP_Pin", circle, new Vector2(leftWx - 0.42f, 1.98f), Vector2.one * 0.10f, GamePalette.Hex("#E8453C"), 10);

            Shape(root, "PropCard1_Out", roundedSmall, new Vector2(leftWx + 0.52f, 1.58f), new Vector2(0.68f, 0.84f), GamePalette.Outline, 8);
            Shape(root, "PropCard1", roundedSmall, new Vector2(leftWx + 0.52f, 1.58f), new Vector2(0.62f, 0.78f), Color.white, 9);
            Shape(root, "PropCard1_Header", square, new Vector2(leftWx + 0.52f, 1.84f), new Vector2(0.56f, 0.16f), GamePalette.Hex("#2E7D32"), 10);
            Shape(root, "PropCard1_Photo", square, new Vector2(leftWx + 0.52f, 1.56f), new Vector2(0.52f, 0.28f), GamePalette.Hex("#8AB8D0"), 10);
            Shape(root, "PropCard1_Line", square, new Vector2(leftWx + 0.52f, 1.32f), new Vector2(0.46f, 0.05f), GamePalette.Hex("#475569"), 10);
            Shape(root, "PropCard1_Pin", circle, new Vector2(leftWx + 0.52f, 1.94f), Vector2.one * 0.08f, GamePalette.Hex("#FFD23F"), 11);

            // Right Window (dx + 2.75f): Property Listings
            float rightWx = dx + 2.75f;
            Shape(root, "PropCard2_Out", roundedSmall, new Vector2(rightWx - 0.46f, 1.58f), new Vector2(0.72f, 0.88f), GamePalette.Outline, 8);
            Shape(root, "PropCard2", roundedSmall, new Vector2(rightWx - 0.46f, 1.58f), new Vector2(0.66f, 0.82f), Color.white, 9);
            Shape(root, "PropCard2_Header", square, new Vector2(rightWx - 0.46f, 1.86f), new Vector2(0.58f, 0.16f), GamePalette.Hex("#DC2626"), 10);
            Shape(root, "PropCard2_Photo", square, new Vector2(rightWx - 0.46f, 1.56f), new Vector2(0.54f, 0.30f), GamePalette.Hex("#F4B266"), 10);
            Shape(root, "PropCard2_Line", square, new Vector2(rightWx - 0.46f, 1.30f), new Vector2(0.48f, 0.05f), GamePalette.Hex("#475569"), 10);
            Shape(root, "PropCard2_Pin", circle, new Vector2(rightWx - 0.46f, 1.95f), Vector2.one * 0.08f, GamePalette.Hex("#E8453C"), 11);

            Shape(root, "PropCard3_Out", roundedSmall, new Vector2(rightWx + 0.46f, 1.58f), new Vector2(0.72f, 0.88f), GamePalette.Outline, 8);
            Shape(root, "PropCard3", roundedSmall, new Vector2(rightWx + 0.46f, 1.58f), new Vector2(0.66f, 0.82f), Color.white, 9);
            Shape(root, "PropCard3_Header", square, new Vector2(rightWx + 0.46f, 1.86f), new Vector2(0.58f, 0.16f), GamePalette.Hex("#2563EB"), 10);
            Shape(root, "PropCard3_Photo", square, new Vector2(rightWx + 0.46f, 1.56f), new Vector2(0.54f, 0.30f), GamePalette.Hex("#8AB8D0"), 10);
            Shape(root, "PropCard3_Line", square, new Vector2(rightWx + 0.46f, 1.30f), new Vector2(0.48f, 0.05f), GamePalette.Hex("#475569"), 10);
            Shape(root, "PropCard3_Pin", circle, new Vector2(rightWx + 0.46f, 1.95f), Vector2.one * 0.08f, GamePalette.Hex("#FFD23F"), 11);

            // Modern Topiary Shrub Planters
            foreach (float side in new[] { -4.35f, 4.35f })
            {
                float px = dx + side;
                Shape(root, "Topiary_PlanterOut", roundedSmall, new Vector2(px, 0.28f), new Vector2(0.82f, 0.54f), GamePalette.Outline, 8);
                Shape(root, "Topiary_Planter", roundedSmall, new Vector2(px, 0.28f), new Vector2(0.72f, 0.44f), GamePalette.Hex("#CBD5E1"), 9);
                Shape(root, "Topiary_Soil", roundedSmall, new Vector2(px, 0.46f), new Vector2(0.64f, 0.14f), GamePalette.Hex("#473223"), 10);
                Shape(root, "Topiary_BushOut", circle, new Vector2(px, 0.82f), Vector2.one * 0.78f, GamePalette.Outline, 11);
                Shape(root, "Topiary_Bush", circle, new Vector2(px, 0.82f), Vector2.one * 0.70f, GamePalette.Hex("#2E7D32"), 12);
                Shape(root, "Topiary_BushHi", circle, new Vector2(px - 0.08f, 0.90f), Vector2.one * 0.40f, GamePalette.Hex("#4CAF50"), 13);
            }
        }

        /// <summary>
        /// Transforms "AGENCIA DE CARROS" (Autos Don Chepe) into an energetic automotive dealership:
        /// chrome marquee signplate with bold red inlay, festive pennant bunting strung along the eave,
        /// industrial garage door with yellow/black hazard warning stripes, wide showroom plate-glass windows
        /// showcasing promotional deal badges and vehicle silhouettes, and stacked auto tires.
        /// </summary>
        private static void DecorateAgenciaArchitecture(Transform root, float w, float fh, float h, float dx, Color wall, Color roof)
        {
            // Dealership Chrome Marquee Signplate for "AUTOS DON CHEPE"
            float signY = fh + 0.62f;
            Shape(root, "AgencySign_Shadow", roundedSmall, new Vector2(dx, signY - 0.05f), new Vector2(5.8f, 0.96f), new Color(0f, 0f, 0f, 0.35f), 9);
            Shape(root, "AgencySign_Out", roundedSmall, new Vector2(dx, signY), new Vector2(5.8f, 0.92f), GamePalette.Outline, 10);
            Shape(root, "AgencySign_Chrome", roundedSmall, new Vector2(dx, signY), new Vector2(5.64f, 0.76f), GamePalette.Hex("#E5E7EB"), 11);
            Shape(root, "AgencySign_RedInlay", roundedSmall, new Vector2(dx, signY), new Vector2(5.38f, 0.56f), GamePalette.Hex("#B91C1C"), 12);
            Shape(root, "AgencySign_Face", roundedSmall, new Vector2(dx, signY), new Vector2(5.24f, 0.44f), GamePalette.Hex("#DC2626"), 13);
            Label(root, "AUTOS DON CHEPE", new Vector2(dx, signY), Color.white, 0.040f, TextAnchor.MiddleCenter, 15);

            // Metallic bolt studs on sign
            foreach (float bx in new[] { dx - 2.58f, dx + 2.58f })
            {
                Shape(root, "AgencySign_Bolt", circle, new Vector2(bx, signY), Vector2.one * 0.16f, GamePalette.Hex("#CBD5E1"), 14);
                Shape(root, "AgencySign_BoltDot", circle, new Vector2(bx, signY), Vector2.one * 0.08f, GamePalette.Hex("#374151"), 15);
            }

            // Dealership Festive Pennant Bunting (Banderines Triangulares de Concesionario)
            if (triangle != null)
            {
                string[] pennantColors = { "#DC2626", "#FFFFFF", "#FBBF24", "#2563EB", "#1F2937" };
                float pennantY = fh - 0.06f;
                Shape(root, "PennantCable", square, new Vector2(0f, pennantY + 0.08f), new Vector2(w + 0.4f, 0.03f), GamePalette.Outline, 9);
                int pCount = 13;
                float pSpan = (w - 1.2f) / (pCount - 1);
                for (int i = 0; i < pCount; i++)
                {
                    float px = -w * 0.5f + 0.6f + i * pSpan;
                    string col = pennantColors[i % pennantColors.Length];
                    var pennantOut = Shape(root, "Pennant_Out", triangle, new Vector2(px, pennantY - 0.06f), new Vector2(0.42f, 0.36f), GamePalette.Outline, 9);
                    var pennant = Shape(root, "Pennant", triangle, new Vector2(px, pennantY - 0.06f), new Vector2(0.34f, 0.28f), GamePalette.Hex(col), 10);
                    Tilt(pennantOut, 180f);
                    Tilt(pennant, 180f);
                }
            }

            // Enhanced Industrial Roller Garage Door
            float hazardY = 0.08f;
            Shape(root, "HazardBase_Out", roundedSmall, new Vector2(dx, hazardY), new Vector2(3.18f, 0.24f), GamePalette.Outline, 8);
            Shape(root, "HazardBase", roundedSmall, new Vector2(dx, hazardY), new Vector2(3.08f, 0.18f), GamePalette.Hex("#1F2937"), 9);
            for (float hx = dx - 1.35f; hx <= dx + 1.35f; hx += 0.38f)
            {
                var stripe = Shape(root, "HazardStripe", square, new Vector2(hx, hazardY), new Vector2(0.18f, 0.18f), GamePalette.Hex("#FBBF24"), 10);
                Tilt(stripe, 30f);
            }
            Shape(root, "GarageLintel_Out", roundedSmall, new Vector2(dx, 2.15f), new Vector2(3.26f, 0.22f), GamePalette.Outline, 8);
            Shape(root, "GarageLintel", roundedSmall, new Vector2(dx, 2.15f), new Vector2(3.16f, 0.14f), GamePalette.Hex("#475569"), 9);
            Shape(root, "GarageHandle_Out", roundedSmall, new Vector2(dx, 0.32f), new Vector2(0.52f, 0.14f), GamePalette.Outline, 8);
            Shape(root, "GarageHandle", roundedSmall, new Vector2(dx, 0.32f), new Vector2(0.44f, 0.08f), GamePalette.Hex("#E2E8F0"), 9);

            // Showroom Panoramic Plate Glass Windows flanking the garage
            foreach (float side in new[] { -4.5f, 4.5f })
            {
                float wx = dx + side;
                Shape(root, "Showroom_Out", roundedSmall, new Vector2(wx, 1.58f), new Vector2(2.74f, 1.54f), GamePalette.Outline, 5);
                Shape(root, "Showroom_Frame", roundedSmall, new Vector2(wx, 1.58f), new Vector2(2.60f, 1.40f), GamePalette.Hex("#374151"), 6);
                Shape(root, "Showroom_Glass", roundedSmall, new Vector2(wx, 1.58f), new Vector2(2.44f, 1.24f), GamePalette.Hex("#90C4DC"), 7);

                var sShine = Shape(root, "Showroom_Shine", roundedSmall, new Vector2(wx - 0.5f, 1.76f), new Vector2(0.26f, 0.82f), new Color(1f, 1f, 1f, 0.45f), 9);
                Tilt(sShine, -20f);
            }

            // Left Window: Dealership Starburst Sticker & Car Silhouette
            float lWx = dx - 4.5f;
            Shape(root, "DealBadge_Out", circle, new Vector2(lWx - 0.45f, 1.58f), Vector2.one * 0.72f, GamePalette.Outline, 8);
            Shape(root, "DealBadge_Gold", circle, new Vector2(lWx - 0.45f, 1.58f), Vector2.one * 0.66f, GamePalette.Hex("#FBBF24"), 9);
            Shape(root, "DealBadge_Red", circle, new Vector2(lWx - 0.45f, 1.58f), Vector2.one * 0.54f, GamePalette.Hex("#DC2626"), 10);
            Label(root, "0%", new Vector2(lWx - 0.45f, 1.68f), Color.white, 0.034f, TextAnchor.MiddleCenter, 11);
            Label(root, "PRIMA", new Vector2(lWx - 0.45f, 1.48f), GamePalette.Hex("#FDE68A"), 0.024f, TextAnchor.MiddleCenter, 11);

            Shape(root, "ShowroomCar_Cabin", roundedSmall, new Vector2(lWx + 0.48f, 1.62f), new Vector2(0.65f, 0.38f), GamePalette.Hex("#1E293B"), 8);
            Shape(root, "ShowroomCar_Body", roundedSmall, new Vector2(lWx + 0.48f, 1.44f), new Vector2(1.05f, 0.32f), GamePalette.Hex("#2563EB"), 8);
            Shape(root, "ShowroomCar_WheelL", circle, new Vector2(lWx + 0.22f, 1.30f), Vector2.one * 0.20f, GamePalette.Hex("#0F172A"), 9);
            Shape(root, "ShowroomCar_WheelR", circle, new Vector2(lWx + 0.74f, 1.30f), Vector2.one * 0.20f, GamePalette.Hex("#0F172A"), 9);

            // Right Window: Dealership Warranty Poster & Spec Sheet
            float rWx = dx + 4.5f;
            Shape(root, "SpecPoster_Out", roundedSmall, new Vector2(rWx, 1.58f), new Vector2(1.25f, 0.95f), GamePalette.Outline, 8);
            Shape(root, "SpecPoster_Card", roundedSmall, new Vector2(rWx, 1.58f), new Vector2(1.18f, 0.88f), Color.white, 9);
            Shape(root, "SpecPoster_Header", square, new Vector2(rWx, 1.88f), new Vector2(1.08f, 0.18f), GamePalette.Hex("#DC2626"), 10);
            Label(root, "GARANTIA 100%", new Vector2(rWx, 1.88f), Color.white, 0.022f, TextAnchor.MiddleCenter, 11);
            for (int li = 0; li < 3; li++)
            {
                Shape(root, "SpecLine", square, new Vector2(rWx, 1.68f - li * 0.14f), new Vector2(0.96f, 0.04f), GamePalette.Hex("#64748B"), 10);
            }

            // Stack of 3 racing/auto tires on far left
            float tireX = -w * 0.46f;
            for (int ti = 0; ti < 3; ti++)
            {
                float ty = 0.22f + ti * 0.28f;
                Shape(root, "Tire_Out", roundedSmall, new Vector2(tireX, ty), new Vector2(0.82f, 0.28f), GamePalette.Outline, 11);
                Shape(root, "Tire_Rubber", roundedSmall, new Vector2(tireX, ty), new Vector2(0.74f, 0.22f), GamePalette.Hex("#1F2937"), 12);
                Shape(root, "Tire_Rim", roundedSmall, new Vector2(tireX, ty), new Vector2(0.42f, 0.10f), GamePalette.Hex("#9CA3AF"), 13);
            }
        }

        /// <summary>
        /// Transforms "HOSPITAL" into a modern public clinical facility:
        /// backlit medical lightbox sign with red cross emblems, horizontal architectural belt molding dividing floors,
        /// upper floor clinical ribbon windows flanking a prominent central raised medical emblem disc,
        /// ground floor observation windows with privacy frosted cross decals, automatic double sliding glass doors,
        /// pulsing red emergency beacon lamp over the entrance, and rooftop industrial HVAC ventilation equipment.
        /// </summary>
        private static void DecorateHospitalArchitecture(Transform root, float w, float fh, float h, float dx, Color wall, Color roof)
        {
            // Backlit Medical Lightbox Sign for "HOSPITAL"
            float signY = fh + 0.62f;
            Shape(root, "HospSign_Shadow", roundedSmall, new Vector2(dx, signY - 0.05f), new Vector2(4.6f, 0.88f), new Color(0f, 0f, 0f, 0.32f), 9);
            Shape(root, "HospSign_Out", roundedSmall, new Vector2(dx, signY), new Vector2(4.6f, 0.82f), GamePalette.Outline, 10);
            Shape(root, "HospSign_Box", roundedSmall, new Vector2(dx, signY), new Vector2(4.44f, 0.68f), GamePalette.Hex("#1E293B"), 11);
            Shape(root, "HospSign_Face", roundedSmall, new Vector2(dx, signY), new Vector2(4.28f, 0.54f), Color.white, 12);
            Label(root, "HOSPITAL", new Vector2(dx, signY), GamePalette.Hex("#0F172A"), 0.040f, TextAnchor.MiddleCenter, 15);

            foreach (float side in new[] { -1.68f, 1.68f })
            {
                Shape(root, "SignCrossH", square, new Vector2(dx + side, signY), new Vector2(0.36f, 0.12f), GamePalette.Hex("#DC2626"), 14);
                Shape(root, "SignCrossV", square, new Vector2(dx + side, signY), new Vector2(0.12f, 0.36f), GamePalette.Hex("#DC2626"), 14);
            }

            // Horizontal belt cornice dividing floor 1 and floor 2
            float beltY = 2.70f;
            Shape(root, "Belt_Shadow", square, new Vector2(0f, beltY - 0.10f), new Vector2(w + 0.08f, 0.08f), new Color(0f, 0f, 0f, 0.25f), 4);
            Shape(root, "Belt_Out", roundedSmall, new Vector2(0f, beltY), new Vector2(w + 0.12f, 0.18f), GamePalette.Outline, 4);
            Shape(root, "Belt_Molding", roundedSmall, new Vector2(0f, beltY), new Vector2(w + 0.04f, 0.12f), GamePalette.Hex("#CBD5E1"), 5);

            // UPPER FLOOR (2nd Story) - Clinical Ribbon Windows & Raised Hospital Emblem
            float emblemY = 4.25f;
            Shape(root, "Emblem_Shadow", circle, new Vector2(0f, emblemY - 0.06f), Vector2.one * 1.76f, new Color(0f, 0f, 0f, 0.30f), 5);
            Shape(root, "Emblem_Out", circle, new Vector2(0f, emblemY), Vector2.one * 1.72f, GamePalette.Outline, 5);
            Shape(root, "Emblem_SilverRim", circle, new Vector2(0f, emblemY), Vector2.one * 1.58f, GamePalette.Hex("#CBD5E1"), 6);
            Shape(root, "Emblem_Porcelain", circle, new Vector2(0f, emblemY), Vector2.one * 1.38f, Color.white, 7);

            Shape(root, "MedCrossH_Out", roundedSmall, new Vector2(0f, emblemY), new Vector2(1.06f, 0.40f), GamePalette.Outline, 8);
            Shape(root, "MedCrossH", roundedSmall, new Vector2(0f, emblemY), new Vector2(0.96f, 0.32f), GamePalette.Hex("#DC2626"), 9);
            Shape(root, "MedCrossV_Out", roundedSmall, new Vector2(0f, emblemY), new Vector2(0.40f, 1.06f), GamePalette.Outline, 8);
            Shape(root, "MedCrossV", roundedSmall, new Vector2(0f, emblemY), new Vector2(0.32f, 0.96f), GamePalette.Hex("#DC2626"), 9);

            foreach (float side in new[] { -3.2f, 3.2f })
            {
                float wx = dx + side;
                Shape(root, "RibbonWin_Out", roundedSmall, new Vector2(wx, 4.25f), new Vector2(2.15f, 1.35f), GamePalette.Outline, 5);
                Shape(root, "RibbonWin_Frame", roundedSmall, new Vector2(wx, 4.25f), new Vector2(2.02f, 1.22f), GamePalette.Hex("#CBD5E1"), 6);
                Shape(root, "RibbonWin_Glass", roundedSmall, new Vector2(wx, 4.25f), new Vector2(1.88f, 1.08f), GamePalette.Hex("#82BAD8"), 7);

                for (int li = -1; li <= 1; li++)
                {
                    Shape(root, "RibbonWin_Louver", square, new Vector2(wx, 4.25f + li * 0.28f), new Vector2(1.84f, 0.04f), GamePalette.Hex("#E2E8F0"), 8);
                }

                var rShine = Shape(root, "RibbonWin_Shine", roundedSmall, new Vector2(wx - 0.40f, 4.45f), new Vector2(0.20f, 0.70f), new Color(1f, 1f, 1f, 0.45f), 9);
                Tilt(rShine, -20f);
            }

            // GROUND FLOOR - Clinical Observation Windows & Emergency Entrance
            foreach (float side in new[] { -2.85f, 2.85f })
            {
                float wx = dx + side;
                Shape(root, "HospWin_Out", roundedSmall, new Vector2(wx, 1.52f), new Vector2(2.15f, 1.45f), GamePalette.Outline, 5);
                Shape(root, "HospWin_Frame", roundedSmall, new Vector2(wx, 1.52f), new Vector2(2.02f, 1.32f), GamePalette.Hex("#CBD5E1"), 6);
                Shape(root, "HospWin_Glass", roundedSmall, new Vector2(wx, 1.52f), new Vector2(1.88f, 1.18f), GamePalette.Hex("#99D5E8"), 7);

                Shape(root, "HospWin_Frost", square, new Vector2(wx, 1.45f), new Vector2(1.88f, 0.50f), new Color(1f, 1f, 1f, 0.35f), 8);
                Shape(root, "HospWin_CrossH", square, new Vector2(wx, 1.45f), new Vector2(0.42f, 0.12f), GamePalette.Hex("#DC2626"), 9);
                Shape(root, "HospWin_CrossV", square, new Vector2(wx, 1.45f), new Vector2(0.12f, 0.42f), GamePalette.Hex("#DC2626"), 9);

                var hwShine = Shape(root, "HospWin_Shine", roundedSmall, new Vector2(wx - 0.38f, 1.72f), new Vector2(0.20f, 0.75f), new Color(1f, 1f, 1f, 0.50f), 9);
                Tilt(hwShine, -20f);
            }

            // Automatic Double Sliding Glass Entrance Doors
            Shape(root, "Ramp_Out", roundedSmall, new Vector2(dx, 0.10f), new Vector2(2.35f, 0.28f), GamePalette.Outline, 5);
            Shape(root, "Ramp", roundedSmall, new Vector2(dx, 0.10f), new Vector2(2.22f, 0.20f), GamePalette.Hex("#D1D5DB"), 5);

            Shape(root, "AutoDoor_Out", roundedSmall, new Vector2(dx, 1.12f), new Vector2(2.00f, 2.18f), GamePalette.Outline, 5);
            Shape(root, "AutoDoor_Frame", roundedSmall, new Vector2(dx, 1.12f), new Vector2(1.86f, 2.04f), GamePalette.Hex("#475569"), 6);

            foreach (float side in new[] { -0.44f, 0.44f })
            {
                Shape(root, "DoorLeaf_Out", roundedSmall, new Vector2(dx + side, 1.10f), new Vector2(0.86f, 1.84f), GamePalette.Outline, 6);
                Shape(root, "DoorLeaf_Glass", roundedSmall, new Vector2(dx + side, 1.10f), new Vector2(0.76f, 1.74f), GamePalette.Hex("#99D5E8"), 7);
                Shape(root, "DoorCrossH", square, new Vector2(dx + side, 1.25f), new Vector2(0.32f, 0.10f), Color.white, 8);
                Shape(root, "DoorCrossV", square, new Vector2(dx + side, 1.25f), new Vector2(0.10f, 0.32f), Color.white, 8);
                Shape(root, "DoorRail", roundedSmall, new Vector2(dx + side * 0.28f, 1.10f), new Vector2(0.06f, 0.65f), GamePalette.Hex("#E2E8F0"), 8);
            }
            Shape(root, "DoorSensor_Out", roundedSmall, new Vector2(dx, 2.08f), new Vector2(1.84f, 0.18f), GamePalette.Outline, 8);
            Shape(root, "DoorSensor", roundedSmall, new Vector2(dx, 2.08f), new Vector2(1.74f, 0.12f), GamePalette.Hex("#334155"), 9);

            // Red Emergency Beacon Lamp (Luz de Emergencia)
            float beaconY = 2.36f;
            Shape(root, "Beacon_Bracket", square, new Vector2(dx, beaconY - 0.08f), new Vector2(0.18f, 0.12f), GamePalette.Hex("#1E293B"), 9);
            Shape(root, "Beacon_Out", roundedSmall, new Vector2(dx, beaconY + 0.08f), new Vector2(0.36f, 0.38f), GamePalette.Outline, 10);
            Shape(root, "Beacon_Dome", roundedSmall, new Vector2(dx, beaconY + 0.08f), new Vector2(0.28f, 0.30f), GamePalette.Hex("#DC2626"), 11);
            Shape(root, "Beacon_Core", circle, new Vector2(dx - 0.04f, beaconY + 0.12f), Vector2.one * 0.12f, Color.white, 12);
            Flickering(Glow(root, "BeaconGlow", new Vector2(dx, beaconY + 0.08f), 2.2f, "#EF4444", 0.35f, 13), 0.15f, 1.6f, dx + 1.7f);

            // Rooftop Industrial HVAC Ventilation Unit
            float hvacX = 3.2f;
            float hvacY = h - 0.38f;
            Shape(root, "HVAC_Shadow", roundedSmall, new Vector2(hvacX, hvacY - 0.06f), new Vector2(1.68f, 0.88f), new Color(0f, 0f, 0f, 0.35f), 6);
            Shape(root, "HVAC_Out", roundedSmall, new Vector2(hvacX, hvacY), new Vector2(1.68f, 0.84f), GamePalette.Outline, 7);
            Shape(root, "HVAC_Body", roundedSmall, new Vector2(hvacX, hvacY), new Vector2(1.56f, 0.72f), GamePalette.Hex("#94A3B8"), 8);
            Shape(root, "HVAC_FanOut", circle, new Vector2(hvacX - 0.35f, hvacY), Vector2.one * 0.54f, GamePalette.Outline, 9);
            Shape(root, "HVAC_Fan", circle, new Vector2(hvacX - 0.35f, hvacY), Vector2.one * 0.46f, GamePalette.Hex("#475569"), 10);
            Shape(root, "HVAC_GrillH", square, new Vector2(hvacX - 0.35f, hvacY), new Vector2(0.42f, 0.06f), GamePalette.Hex("#1E293B"), 11);
            Shape(root, "HVAC_GrillV", square, new Vector2(hvacX - 0.35f, hvacY), new Vector2(0.06f, 0.42f), GamePalette.Hex("#1E293B"), 11);
            for (int vi = -1; vi <= 1; vi++)
            {
                Shape(root, "HVAC_Louver", square, new Vector2(hvacX + 0.38f, hvacY + vi * 0.16f), new Vector2(0.50f, 0.05f), GamePalette.Hex("#334155"), 9);
            }
        }

        /// <summary>
        /// Transforms "COMISARIA" (Policía) into a reinforced law enforcement precinct:
        /// official navy and gold police signplate with law enforcement star badges,
        /// heavy reinforced steel door with perimeter steel rivets and armored barred peep slit,
        /// pulsing blue police beacon siren mounted over the entrance, gold star badge shield emblem,
        /// reinforced security barred windows with heavy stone sills, and a CCTV surveillance camera under the eave.
        /// </summary>
        private static void DecorateComisariaArchitecture(Transform root, float w, float fh, float h, float dx, Color wall, Color roof)
        {
            // Official Navy & Gold Police Precinct Signplate
            float signY = fh + 0.62f;
            Shape(root, "PoliceSign_Shadow", roundedSmall, new Vector2(dx, signY - 0.05f), new Vector2(4.8f, 0.90f), new Color(0f, 0f, 0f, 0.35f), 9);
            Shape(root, "PoliceSign_Out", roundedSmall, new Vector2(dx, signY), new Vector2(4.8f, 0.86f), GamePalette.Outline, 10);
            Shape(root, "PoliceSign_Navy", roundedSmall, new Vector2(dx, signY), new Vector2(4.64f, 0.70f), GamePalette.Hex("#0F2942"), 11);
            Shape(root, "PoliceSign_GoldBezel", roundedSmall, new Vector2(dx, signY), new Vector2(4.40f, 0.52f), GamePalette.Hex("#D4AF37"), 12);
            Shape(root, "PoliceSign_Face", roundedSmall, new Vector2(dx, signY), new Vector2(4.26f, 0.40f), GamePalette.Hex("#1A3A5C"), 13);
            Label(root, "POLICÍA", new Vector2(dx, signY), GamePalette.Hex("#FFFDF0"), 0.040f, TextAnchor.MiddleCenter, 15);

            foreach (float side in new[] { -1.55f, 1.55f })
            {
                Shape(root, "BadgeStar_Disc", circle, new Vector2(dx + side, signY), Vector2.one * 0.32f, GamePalette.Hex("#D4AF37"), 14);
                Shape(root, "BadgeStar_Dot", circle, new Vector2(dx + side, signY), Vector2.one * 0.14f, GamePalette.Hex("#0F2942"), 15);
            }

            // Heavy Reinforced Steel Police Door at dx
            Shape(root, "Step_Out", roundedSmall, new Vector2(dx, 0.10f), new Vector2(1.95f, 0.24f), GamePalette.Outline, 5);
            Shape(root, "Step", roundedSmall, new Vector2(dx, 0.10f), new Vector2(1.85f, 0.16f), GamePalette.Hex("#64748B"), 5);

            Shape(root, "PDoor_Out", roundedSmall, new Vector2(dx, 1.08f), new Vector2(1.58f, 2.14f), GamePalette.Outline, 5);
            Shape(root, "PDoor_Frame", roundedSmall, new Vector2(dx, 1.08f), new Vector2(1.44f, 2.00f), GamePalette.Hex("#334155"), 6);
            Shape(root, "PDoor_Leaf", roundedSmall, new Vector2(dx, 1.08f), new Vector2(1.28f, 1.86f), GamePalette.Hex("#1E293B"), 7);

            for (int ri = 0; ri < 6; ri++)
            {
                float ry = 0.35f + ri * 0.28f;
                Shape(root, "RivetL", circle, new Vector2(dx - 0.52f, ry), Vector2.one * 0.08f, GamePalette.Hex("#94A3B8"), 8);
                Shape(root, "RivetR", circle, new Vector2(dx + 0.52f, ry), Vector2.one * 0.08f, GamePalette.Hex("#94A3B8"), 8);
            }

            // Armored View Slit
            Shape(root, "Slit_Out", roundedSmall, new Vector2(dx, 1.52f), new Vector2(0.56f, 0.38f), GamePalette.Outline, 8);
            Shape(root, "Slit_Frame", roundedSmall, new Vector2(dx, 1.52f), new Vector2(0.48f, 0.30f), GamePalette.Hex("#475569"), 9);
            Shape(root, "Slit_Glass", roundedSmall, new Vector2(dx, 1.52f), new Vector2(0.38f, 0.22f), GamePalette.Hex("#0F172A"), 10);
            Shape(root, "Slit_Bar", square, new Vector2(dx, 1.52f), new Vector2(0.04f, 0.22f), GamePalette.Hex("#94A3B8"), 11);

            Shape(root, "PDoor_HandleOut", roundedSmall, new Vector2(dx + 0.40f, 1.02f), new Vector2(0.12f, 0.44f), GamePalette.Outline, 8);
            Shape(root, "PDoor_Handle", roundedSmall, new Vector2(dx + 0.40f, 1.02f), new Vector2(0.06f, 0.38f), GamePalette.Hex("#E2E8F0"), 9);

            // Blue Police Beacon Siren (Baliza Policial) directly over the entrance
            float beaconY = 2.36f;
            Shape(root, "Siren_Bracket", square, new Vector2(dx, beaconY - 0.08f), new Vector2(0.20f, 0.12f), GamePalette.Hex("#0F172A"), 11);
            Shape(root, "Siren_Out", roundedSmall, new Vector2(dx, beaconY + 0.08f), new Vector2(0.38f, 0.42f), GamePalette.Outline, 12);
            Shape(root, "Siren_Dome", roundedSmall, new Vector2(dx, beaconY + 0.08f), new Vector2(0.28f, 0.34f), GamePalette.Hex("#2563EB"), 13);
            Shape(root, "Siren_Core", circle, new Vector2(dx - 0.04f, beaconY + 0.12f), Vector2.one * 0.14f, GamePalette.Hex("#BAE6FD"), 14);
            Flickering(Glow(root, "SirenGlow", new Vector2(dx, beaconY + 0.08f), 2.4f, "#3B82F6", 0.35f, 15), 0.12f, 1.5f, dx + 2.5f);

            // Official Police Shield Badge Emblem mounted above the door
            float shieldY = 2.82f;
            Shape(root, "Shield_Out", roundedSmall, new Vector2(dx, shieldY), new Vector2(0.92f, 1.08f), GamePalette.Outline, 11);
            Shape(root, "Shield_Gold", roundedSmall, new Vector2(dx, shieldY), new Vector2(0.82f, 0.98f), GamePalette.Hex("#D4AF37"), 12);
            Shape(root, "Shield_Navy", roundedSmall, new Vector2(dx, shieldY), new Vector2(0.68f, 0.84f), GamePalette.Hex("#0F2942"), 13);
            Shape(root, "Shield_Star", circle, new Vector2(dx, shieldY), Vector2.one * 0.38f, GamePalette.Hex("#FBBF24"), 14);
            Shape(root, "Shield_Dot", circle, new Vector2(dx, shieldY), Vector2.one * 0.14f, GamePalette.Hex("#0F2942"), 15);

            // Barred Security Windows flanking the door
            foreach (float side in new[] { -2.75f, 2.75f })
            {
                float wx = dx + side;
                float wy = 1.62f;

                Shape(root, "SecSill_Out", roundedSmall, new Vector2(wx, 0.90f), new Vector2(1.92f, 0.24f), GamePalette.Outline, 6);
                Shape(root, "SecSill", roundedSmall, new Vector2(wx, 0.90f), new Vector2(1.82f, 0.16f), GamePalette.Hex("#94A3B8"), 7);

                Shape(root, "SecWin_Out", roundedSmall, new Vector2(wx, wy), new Vector2(1.78f, 1.44f), GamePalette.Outline, 5);
                Shape(root, "SecWin_Frame", roundedSmall, new Vector2(wx, wy), new Vector2(1.66f, 1.32f), GamePalette.Hex("#1E293B"), 6);
                Shape(root, "SecWin_Glass", roundedSmall, new Vector2(wx, wy), new Vector2(1.50f, 1.18f), GamePalette.Hex("#64748B"), 7);

                var secShine = Shape(root, "SecWin_Shine", roundedSmall, new Vector2(wx - 0.32f, wy + 0.18f), new Vector2(0.18f, 0.65f), new Color(1f, 1f, 1f, 0.30f), 8);
                Tilt(secShine, -20f);

                for (int bi = -2; bi <= 2; bi++)
                {
                    float bx = wx + bi * 0.28f;
                    Shape(root, "SecBar_Out", square, new Vector2(bx, wy), new Vector2(0.09f, 1.28f), GamePalette.Outline, 8);
                    Shape(root, "SecBar", square, new Vector2(bx, wy), new Vector2(0.05f, 1.26f), GamePalette.Hex("#1E293B"), 9);
                    Shape(root, "SecBar_Hi", square, new Vector2(bx - 0.015f, wy), new Vector2(0.02f, 1.24f), GamePalette.Hex("#94A3B8"), 10);
                }

                Shape(root, "SecTie_Out", square, new Vector2(wx, wy), new Vector2(1.54f, 0.08f), GamePalette.Outline, 10);
                Shape(root, "SecTie", square, new Vector2(wx, wy), new Vector2(1.52f, 0.04f), GamePalette.Hex("#334155"), 11);
            }

            // Security CCTV Camera mounted under the left roof eave
            float camX = -w * 0.42f;
            float camY = fh - 0.12f;
            Shape(root, "CCTV_Mount", square, new Vector2(camX, camY + 0.08f), new Vector2(0.10f, 0.14f), GamePalette.Hex("#334155"), 12);
            var cam = Shape(root, "CCTV_Body", roundedSmall, new Vector2(camX + 0.10f, camY), new Vector2(0.32f, 0.18f), GamePalette.Hex("#E2E8F0"), 13);
            Tilt(cam, -25f);
            Shape(root, "CCTV_LED", circle, new Vector2(camX + 0.22f, camY - 0.06f), Vector2.one * 0.06f, GamePalette.Hex("#DC2626"), 14);
        }

        private static void BuildProduceCrate(Transform root, Vector2 pos, Vector2 size, bool leftStyle, int order)
        {
            var crate = new GameObject("ProduceCrate").transform;
            crate.SetParent(root, false);
            crate.localPosition = pos;

            // Slatted wooden crate body
            Shape(crate, "Crate_Shadow", softCircle, new Vector2(0.05f, -0.08f), size + new Vector2(0.25f, 0.15f), new Color(0f, 0f, 0f, 0.28f), order);
            Shape(crate, "Crate_Out", roundedSmall, new Vector2(0f, 0.35f), size + new Vector2(0.14f, 0.14f), GamePalette.Outline, order);
            Shape(crate, "Crate_Wood", roundedSmall, new Vector2(0f, 0.35f), size, GamePalette.Hex("#B87D44"), order + 1);
            Shape(crate, "Crate_SlatA", square, new Vector2(0f, 0.18f), new Vector2(size.x - 0.08f, 0.06f), GamePalette.Outline, order + 2);
            Shape(crate, "Crate_SlatB", square, new Vector2(0f, 0.34f), new Vector2(size.x - 0.08f, 0.06f), GamePalette.Outline, order + 2);
            Shape(crate, "Crate_Interior", roundedSmall, new Vector2(0f, 0.56f), new Vector2(size.x - 0.14f, 0.42f), GamePalette.Hex("#5C3A1E"), order + 2);

            // Divider board in the crate
            Shape(crate, "Crate_Divider", square, new Vector2(0f, 0.56f), new Vector2(0.08f, 0.42f), GamePalette.Hex("#8B5A3C"), order + 3);

            // Overflowing colorful fruits & vegetables (tomatoes, oranges, bananas, avocados)
            string[] leftColors = leftStyle ? new[] { "#E8453C", "#FF8A2B" } : new[] { "#E8453C", "#FFD23F" };
            string[] rightColors = leftStyle ? new[] { "#68C242", "#4BA334" } : new[] { "#4BA334", "#FF9F2E" };

            for (int i = 0; i < 6; i++)
            {
                float fx = -size.x * 0.25f + ((i % 3) - 1) * 0.22f;
                float fy = 0.50f + (i / 3) * 0.14f;
                string c = leftColors[i % leftColors.Length];
                Shape(crate, "FruitL_Out", circle, new Vector2(fx, fy), Vector2.one * 0.24f, GamePalette.Outline, order + 3);
                Shape(crate, "FruitL", circle, new Vector2(fx, fy), Vector2.one * 0.17f, GamePalette.Hex(c), order + 4);
            }

            for (int i = 0; i < 6; i++)
            {
                float fx = size.x * 0.25f + ((i % 3) - 1) * 0.22f;
                float fy = 0.50f + (i / 3) * 0.14f;
                string c = rightColors[i % rightColors.Length];
                Shape(crate, "FruitR_Out", circle, new Vector2(fx, fy), Vector2.one * 0.24f, GamePalette.Outline, order + 3);
                Shape(crate, "FruitR", circle, new Vector2(fx, fy), Vector2.one * 0.17f, GamePalette.Hex(c), order + 4);
            }
        }

        private static void BuildMonsteraPot(Transform parent, Vector2 pos, float scale, int order)
        {
            var pot = new GameObject("MonsteraPot").transform;
            pot.SetParent(parent, false);
            pot.localPosition = pos;

            Shape(pot, "Shadow", softCircle, new Vector2(0.05f, -0.04f), new Vector2(1.0f, 0.42f) * scale, new Color(0f, 0f, 0f, 0.26f), order);
            Shape(pot, "Pot_Out", roundedSmall, new Vector2(0f, 0.26f) * scale, new Vector2(0.76f, 0.52f) * scale, GamePalette.Outline, order);
            Shape(pot, "Pot", roundedSmall, new Vector2(0f, 0.26f) * scale, new Vector2(0.64f, 0.40f) * scale, GamePalette.Hex("#B85D3B"), order + 1);
            Shape(pot, "PotRim_Out", roundedSmall, new Vector2(0f, 0.46f) * scale, new Vector2(0.86f, 0.18f) * scale, GamePalette.Outline, order + 1);
            Shape(pot, "PotRim", roundedSmall, new Vector2(0f, 0.46f) * scale, new Vector2(0.74f, 0.10f) * scale, GamePalette.Hex("#CF6E4A"), order + 2);

            var foliage = new GameObject("MonsteraLeaves").transform;
            foliage.SetParent(pot, false);
            foliage.localPosition = new Vector2(0f, 0.52f) * scale;
            foliage.gameObject.AddComponent<FoliagePhysics>().Configure(3.6f, 1.5f, pos.x * 1.3f, 0.025f, true, 1.4f, 10f);

            var frond = palmFrondSprite != null ? palmFrondSprite : circle;
            foreach (var leaf in new[] { (-0.28f, 0.28f, 135f, "#2E8B46"), (0.28f, 0.26f, 45f, "#3FA84E"), (-0.12f, 0.48f, 108f, "#54C258"), (0.14f, 0.46f, 72f, "#48B550") })
            {
                var l = Shape(foliage, "Leaf", frond, new Vector2(leaf.Item1, leaf.Item2) * scale, new Vector2(0.95f, 0.56f) * scale, GamePalette.Hex(leaf.Item4), order + 3);
                Tilt(l, leaf.Item3);
            }
        }

        private static void BuildTropicalPalmPot(Transform parent, Vector2 pos, float scale, int order)
        {
            var pot = new GameObject("PalmPot").transform;
            pot.SetParent(parent, false);
            pot.localPosition = pos;

            Shape(pot, "Shadow", softCircle, new Vector2(0.04f, -0.04f), new Vector2(0.88f, 0.36f) * scale, new Color(0f, 0f, 0f, 0.25f), order);
            Shape(pot, "Pot_Out", roundedSmall, new Vector2(0f, 0.24f) * scale, new Vector2(0.68f, 0.48f) * scale, GamePalette.Outline, order);
            Shape(pot, "Pot", roundedSmall, new Vector2(0f, 0.24f) * scale, new Vector2(0.56f, 0.36f) * scale, GamePalette.Hex("#BD603E"), order + 1);
            Shape(pot, "PotRim_Out", roundedSmall, new Vector2(0f, 0.42f) * scale, new Vector2(0.76f, 0.16f) * scale, GamePalette.Outline, order + 1);
            Shape(pot, "PotRim", roundedSmall, new Vector2(0f, 0.42f) * scale, new Vector2(0.64f, 0.09f) * scale, GamePalette.Hex("#D4724E"), order + 2);

            var foliage = new GameObject("PalmLeaves").transform;
            foliage.SetParent(pot, false);
            foliage.localPosition = new Vector2(0f, 0.46f) * scale;
            foliage.gameObject.AddComponent<FoliagePhysics>().Configure(4.0f, 1.65f, pos.x * 1.7f, 0.025f, true, 1.4f, 11f);

            var frond = palmFrondSprite != null ? palmFrondSprite : circle;
            foreach (var leaf in new[] { (-0.32f, 0.25f, 148f, "#2E8B46"), (0.32f, 0.25f, 32f, "#2E8B46"), (-0.16f, 0.44f, 115f, "#48B852"), (0.16f, 0.44f, 65f, "#48B852"), (0f, 0.54f, 90f, "#66CF60") })
            {
                var l = Shape(foliage, "Frond", frond, new Vector2(leaf.Item1, leaf.Item2) * scale, new Vector2(0.92f, 0.44f) * scale, GamePalette.Hex(leaf.Item4), order + 3);
                Tilt(l, leaf.Item3);
            }
        }

        private static void BuildSmallFernPot(Transform parent, Vector2 pos, float scale, int order)
        {
            BuildTropicalPalmPot(parent, pos, scale * 0.82f, order);
        }

        private static void DrawWoodenPlankSign(Transform root, string text, Vector2 center, float width, float height, int order)
        {
            var board = new GameObject("WoodenSign_" + text).transform;
            board.SetParent(root, false);
            board.localPosition = center;
            board.gameObject.AddComponent<FoliagePhysics>().Configure(1.2f, 1.2f, center.x + center.y, 0f, false);

            Shape(board, "Shadow", roundedSmall, new Vector2(0.05f, -0.08f), new Vector2(width + 0.22f, height + 0.18f), new Color(0f, 0f, 0f, 0.36f), order);
            Shape(board, "Outline", roundedSmall, Vector2.zero, new Vector2(width + 0.16f, height + 0.14f), GamePalette.Outline, order + 1);
            Shape(board, "WoodBase", roundedSmall, Vector2.zero, new Vector2(width, height), GamePalette.Hex("#C8965A"), order + 1);
            if (woodPlankTex != null)
            {
                TiledShape(board, "WoodPlanks", woodPlankTex, Vector2.zero, new Vector2(width - 0.08f, height - 0.08f), GamePalette.Hex("#D4A366"), order + 2);
            }

            // Corner nail heads
            float nx = width * 0.5f - 0.18f;
            float ny = height * 0.5f - 0.16f;
            foreach (float sx in new[] { -nx, nx })
            {
                foreach (float sy in new[] { -ny, ny })
                {
                    Shape(board, "Nail_Out", circle, new Vector2(sx, sy), Vector2.one * 0.11f, GamePalette.Outline, order + 3);
                    Shape(board, "Nail", circle, new Vector2(sx, sy), Vector2.one * 0.06f, GamePalette.Hex("#5C4028"), order + 3);
                }
            }

            Label(board, text, Vector2.zero, GamePalette.Hex("#3E2212"), 0.042f, TextAnchor.MiddleCenter, order + 4);
        }

        private static void DrawEntrance(Transform root, BuildingSpec b, float dx, Color wall)
        {
            if (b.Name == "Banco" || b.Name == "Hospital" || b.Name == "Inmobiliaria" || b.Name == "Comisaria")
            {
                return;
            }

            switch (b.Entrance)
            {
                case Entrance.Door:
                    Shape(root, "Step_Outline", roundedSmall, new Vector2(dx, 0.11f), new Vector2(2.06f, 0.3f), GamePalette.Outline, 5);
                    Shape(root, "Step", roundedSmall, new Vector2(dx, 0.11f), new Vector2(1.94f, 0.2f), GamePalette.Shade(wall, 0.78f), 5);
                    Shape(root, "WelcomeMat_Outline", roundedSmall, new Vector2(dx, -0.08f), new Vector2(1.36f, 0.32f), GamePalette.Outline, 5);
                    Shape(root, "WelcomeMat", roundedSmall, new Vector2(dx, -0.08f), new Vector2(1.22f, 0.22f), GamePalette.Hex("#C8423B"), 5);
                    Shape(root, "DoorFrame_Outline", roundedSmall, new Vector2(dx, 1.08f), new Vector2(1.56f, 2.12f), GamePalette.Outline, 5);
                    Shape(root, "DoorFrame", roundedSmall, new Vector2(dx, 1.06f), new Vector2(1.42f, 1.98f), GamePalette.Hex("#7A4B2A"), 5);
                    Shape(root, "Door_Outline", roundedSmall, new Vector2(dx, 1.04f), new Vector2(1.30f, 1.90f), GamePalette.Outline, 6);
                    Shape(root, "Door", roundedSmall, new Vector2(dx, 1.0f), new Vector2(1.18f, 1.80f), GamePalette.Hex("#9B643D"), 6);
                    if (woodPlankTex != null)
                    {
                        TiledShape(root, "Door_WoodGrain", woodPlankTex, new Vector2(dx, 1.0f), new Vector2(1.12f, 1.74f), GamePalette.Hex("#9B643D"), 6);
                    }
                    Shape(root, "Door_PanelA_Out", roundedSmall, new Vector2(dx, 1.45f), new Vector2(0.86f, 0.64f), GamePalette.Outline, 7);
                    Shape(root, "Door_PanelA", roundedSmall, new Vector2(dx, 1.45f), new Vector2(0.76f, 0.54f), GamePalette.Hex("#7D4E2D"), 7);
                    Shape(root, "Door_PanelB_Out", roundedSmall, new Vector2(dx, 0.62f), new Vector2(0.86f, 0.64f), GamePalette.Outline, 7);
                    Shape(root, "Door_PanelB", roundedSmall, new Vector2(dx, 0.62f), new Vector2(0.76f, 0.54f), GamePalette.Hex("#7D4E2D"), 7);
                    Shape(root, "Door_Knob_Out", circle, new Vector2(dx + 0.38f, 0.96f), Vector2.one * 0.20f, GamePalette.Outline, 8);
                    Shape(root, "Door_Knob", circle, new Vector2(dx + 0.38f, 0.96f), Vector2.one * 0.13f, GamePalette.Hex("#FFD23F"), 8);
                    Shape(root, "PorchLamp_Outline", roundedSmall, new Vector2(dx + 0.94f, 1.72f), new Vector2(0.28f, 0.36f), GamePalette.Outline, 7);
                    Shape(root, "PorchLamp", roundedSmall, new Vector2(dx + 0.94f, 1.72f), new Vector2(0.18f, 0.26f), GamePalette.Hex("#FFE082"), 8);
                    Flickering(Glow(root, "PorchGlow", new Vector2(dx + 0.94f, 1.72f), 2.2f, "#FFE082", 0.26f, 9), 0.1f, 1.5f, dx + 2.1f);
                    break;
                case Entrance.Garage:
                    Shape(root, "Garage_Outline", roundedSmall, new Vector2(dx, 1.1f), new Vector2(3.1f, 2.1f), GamePalette.Outline, 5);
                    Shape(root, "Garage", roundedSmall, new Vector2(dx, 1.05f), new Vector2(2.94f, 1.94f), GamePalette.Hex("#9AA3B8"), 6);
                    for (int i = 0; i < 4; i++)
                    {
                        Shape(root, "Garage_Rib", square, new Vector2(dx, 0.35f + i * 0.45f), new Vector2(2.8f, 0.05f), GamePalette.Hex("#6F7890"), 7);
                    }
                    break;
                case Entrance.Window:
                    Shape(root, "Counter_Outline", roundedSmall, new Vector2(dx, 1.5f), new Vector2(2.3f, 1.3f), GamePalette.Outline, 5);
                    Shape(root, "Counter_Inside", roundedSmall, new Vector2(dx, 1.5f), new Vector2(2.14f, 1.14f), GamePalette.Hex("#3A2E2A"), 6);
                    Shape(root, "Counter_Ledge", roundedSmall, new Vector2(dx, 0.92f), new Vector2(2.5f, 0.26f), GamePalette.Hex("#A9743F"), 8);
                    break;
            }
        }

        private static void DrawFacadeWindows(Transform root, BuildingSpec b, float w, float dx, Color wall)
        {
            if (b.Name == "Banco" || b.Name == "Hospital" || b.Name == "Comisaria" || b.Name == "Inmobiliaria" || b.Name == "AgenciaDeCarros")
            {
                return; // Custom storefronts and public landmark architecture
            }

            float offset = FacadeWindowOffset(b);
            if (offset <= 0f)
            {
                return;
            }

            Color shutterColor = GamePalette.Hex("#2E8B52");
            Color louverDark = GamePalette.Hex("#1B5E34");
            foreach (float side in new[] { -offset, offset })
            {
                float wx = dx + side;
                if (Mathf.Abs(wx) > w * 0.5f - 0.6f)
                {
                    continue;
                }

                // Louvered wooden window shutters (postigos con tablillas) on each side
                foreach (float sx in new[] { -0.78f, 0.78f })
                {
                    Shape(root, "Shutter_Outline", roundedSmall, new Vector2(wx + sx, 1.58f), new Vector2(0.42f, 1.22f), GamePalette.Outline, 5);
                    Shape(root, "Shutter", roundedSmall, new Vector2(wx + sx, 1.58f), new Vector2(0.30f, 1.10f), shutterColor, 6);
                    for (int li = -2; li <= 2; li++)
                    {
                        Shape(root, "Shutter_Louver", square, new Vector2(wx + sx, 1.58f + li * 0.18f), new Vector2(0.22f, 0.04f), louverDark, 7);
                    }
                }

                // Wooden window frame & sky-reflective glass panes
                Shape(root, "Window_Outline", roundedSmall, new Vector2(wx, 1.58f), new Vector2(1.26f, 1.18f), GamePalette.Outline, 5);
                Shape(root, "Window_WoodFrame", roundedSmall, new Vector2(wx, 1.58f), new Vector2(1.14f, 1.06f), GamePalette.Hex("#8B5A3C"), 6);
                Shape(root, "Window_Glass", roundedSmall, new Vector2(wx, 1.58f), new Vector2(0.98f, 0.90f), GamePalette.Hex("#88C5E2"), 7);
                Shape(root, "Window_FrameV", square, new Vector2(wx, 1.58f), new Vector2(0.07f, 0.94f), GamePalette.Hex("#8B5A3C"), 8);
                Shape(root, "Window_FrameH", square, new Vector2(wx, 1.58f), new Vector2(0.98f, 0.07f), GamePalette.Hex("#8B5A3C"), 8);
                var shine = GamePalette.White;
                shine.a = 0.55f;
                Shape(root, "Window_Shine", roundedSmall, new Vector2(wx - 0.25f, 1.78f), new Vector2(0.22f, 0.28f), shine, 8);
                WindowNightGlow(root, wx, 1.58f, b.YBottom);

                // Terracotta window planter box overflowing with colorful blooms and foliage
                Shape(root, "FlowerBox_Outline", roundedSmall, new Vector2(wx, 0.92f), new Vector2(1.48f, 0.34f), GamePalette.Outline, 8);
                Shape(root, "FlowerBox", roundedSmall, new Vector2(wx, 0.92f), new Vector2(1.34f, 0.22f), GamePalette.Hex("#9B5A36"), 9);
                var boxFlowers = new GameObject("WindowFlowers").transform;
                boxFlowers.SetParent(root, false);
                boxFlowers.localPosition = new Vector2(wx, 1.06f);

                string[] bloomColors = { "#A855F7", "#FF5E7E", "#FFD23F", "#FF7847", "#EC4899" };
                for (int fi = -2; fi <= 2; fi++)
                {
                    float fx = fi * 0.26f;
                    float fy = (fi % 2 == 0) ? 0.02f : 0.08f;
                    Shape(boxFlowers, "Leaf_Out", circle, new Vector2(fx, fy - 0.04f), new Vector2(0.36f, 0.28f), GamePalette.Outline, 9);
                    Shape(boxFlowers, "Leaf", circle, new Vector2(fx, fy - 0.04f), new Vector2(0.28f, 0.20f), GamePalette.Hex("#389E48"), 10);
                    Shape(boxFlowers, "Bloom_Out", circle, new Vector2(fx, fy + 0.04f), Vector2.one * 0.22f, GamePalette.Outline, 10);
                    Shape(boxFlowers, "Bloom", circle, new Vector2(fx, fy + 0.04f), Vector2.one * 0.15f, GamePalette.Hex(bloomColors[fi + 2]), 11);
                }

                boxFlowers.gameObject.AddComponent<FoliagePhysics>().Configure(3.2f, 1.8f, wx * 1.3f, 0.025f, false);
            }
        }

        /// <summary>
        /// The warm light of a room behind a window, only at night: four panes between the frame bars, so the bars still
        /// show. Not every home has the light on: about one window in four stays dark.
        /// </summary>
        private static void WindowNightGlow(Transform root, float wx, float wy, float seed)
        {
            if (Mathf.Abs(Mathf.RoundToInt(wx * 7f + seed * 3f)) % 4 == 0)
            {
                return;
            }

            var warm = GamePalette.Hex("#FFD27A");
            warm.a = 0.85f;
            foreach (var pane in new[] { new Vector2(-0.2525f, 0.2325f), new Vector2(0.2525f, 0.2325f), new Vector2(-0.2525f, -0.2325f), new Vector2(0.2525f, -0.2325f) })
            {
                var glow = Shape(root, "Window_NightGlow", roundedSmall, new Vector2(wx, wy) + pane, new Vector2(0.42f, 0.38f), warm, 8);
                Flickering(glow, 0.06f, 0.25f, wx + pane.x * 10f + seed, true);
            }
        }

        private static float FacadeWindowOffset(BuildingSpec b)
        {
            if (b.Entrance != Entrance.Door && b.Entrance != Entrance.Garage)
            {
                return 0f;
            }

            float w = b.XMax - b.XMin;
            float offset = Mathf.Min(2.7f, w * 0.5f - 1.25f);
            float doorHalf = b.Entrance == Entrance.Garage ? 1.55f : 0.7f;
            return offset >= doorHalf + 0.75f ? offset : 0f;
        }

        private static void DrawAwning(Transform root, float w, float fh, string a, string b)
        {
            var awningShadow = GamePalette.Outline;
            awningShadow.a = 0.34f;
            Shape(root, "Awning_Shadow", roundedSmall, new Vector2(0f, fh - 0.92f), new Vector2(w + 0.16f, 0.34f), awningShadow, 7);
            Shape(root, "Awning_Outline", roundedSmall, new Vector2(0f, fh - 0.42f), new Vector2(w + 0.24f, 0.94f), GamePalette.Outline, 8);
            int stripes = Mathf.RoundToInt(w / 0.82f);
            float width = (w + 0.08f) / stripes;
            for (int i = 0; i < stripes; i++)
            {
                float x = -(w + 0.08f) * 0.5f + (i + 0.5f) * width;
                Color stripeCol = GamePalette.Hex(i % 2 == 0 ? a : b);
                Shape(root, "Awning", square, new Vector2(x, fh - 0.40f), new Vector2(width, 0.76f), stripeCol, 9);
                Shape(root, "Awning_TopSun", square, new Vector2(x, fh - 0.16f), new Vector2(width, 0.18f), GamePalette.Shade(stripeCol, 1.08f), 9);
                Shape(root, "Awning_ValanceOutline", circle, new Vector2(x, fh - 0.80f), new Vector2(width + 0.10f, 0.32f), GamePalette.Outline, 8);
                Shape(root, "Awning_Valance", circle, new Vector2(x, fh - 0.79f), new Vector2(width - 0.02f, 0.24f), GamePalette.Shade(stripeCol, 0.88f), 9);
            }
        }

        private static void DrawSign(Transform root, string text, float dx, float y, Color wall)
        {
            float length = Mathf.Max(1.9f, text.Length * 0.34f + 0.85f);
            DrawWoodenPlankSign(root, text, new Vector2(dx, y), length, 0.68f, 10);
        }

        private static void Tree(Transform parent, Transform shadows, float x, float y, float scale, string leaf, bool fruit)
        {
            if (OnPavement(x, y, 0.3f) || InsideBuilding(x, y))
            {
                return;
            }

            var root = Root(parent, "Tree", new Vector2(x, y), y);
            GroundShadow(shadows, new Vector2(x + 0.32f, y - 0.06f), new Vector2(2.6f * scale, 0.95f * scale), 0.32f);

            // Gnarled wooden trunk with root flare and bark lines
            Shape(root, "RootFlare_Out", roundedSmall, new Vector2(0f, 0.14f * scale), new Vector2(0.78f * scale, 0.32f * scale), GamePalette.Outline, 0);
            Shape(root, "RootFlare", roundedSmall, new Vector2(0f, 0.14f * scale), new Vector2(0.64f * scale, 0.20f * scale), GamePalette.Hex("#784B2A"), 1);
            Shape(root, "Trunk_Outline", roundedSmall, new Vector2(0f, 0.62f * scale), new Vector2(0.62f * scale, 1.24f * scale), GamePalette.Outline, 0);
            Shape(root, "Trunk", roundedSmall, new Vector2(0f, 0.58f * scale), new Vector2(0.46f * scale, 1.12f * scale), GamePalette.Hex("#8B5A3C"), 1);
            Shape(root, "Trunk_BarkLight", roundedSmall, new Vector2(-0.09f * scale, 0.58f * scale), new Vector2(0.14f * scale, 0.92f * scale), GamePalette.Hex("#A9743F"), 1);
            Shape(root, "Trunk_BarkLine", square, new Vector2(0.06f * scale, 0.56f * scale), new Vector2(0.04f * scale, 0.78f * scale), GamePalette.Hex("#52321B"), 1);

            var canopyRig = new GameObject("CanopyRig").transform;
            canopyRig.SetParent(root, false);
            canopyRig.localPosition = new Vector2(0f, 1.58f * scale);
            // The crown bends from the foot of the trunk with the breeze that crosses the town, instead of spinning in place.
            canopyRig.gameObject.AddComponent<FoliagePhysics>().Configure(3.2f, 1.45f, x * 0.53f + y * 0.71f, 0.006f, true, 2.2f, 9f, true, 1.58f * scale);

            // Tropical banana/plantain fronds peeking out behind the tree crown
            if (palmFrondSprite != null)
            {
                var frondL = Shape(canopyRig, "BananaFrond_L", palmFrondSprite, new Vector2(-0.88f, 0.18f) * scale, new Vector2(1.65f, 0.78f) * scale, GamePalette.Hex("#4CB848"), 1);
                Tilt(frondL, 148f);
                var frondR = Shape(canopyRig, "BananaFrond_R", palmFrondSprite, new Vector2(0.86f, 0.16f) * scale, new Vector2(1.65f, 0.78f) * scale, GamePalette.Hex("#3FA642"), 1);
                Tilt(frondR, 32f);
            }

            // Multi-layered volumetric hand-inked tree canopy clusters
            var canopySpr = treeCanopySprite != null ? treeCanopySprite : circle;
            var deepLeaf = GamePalette.Hex("#247339");
            var mainLeaf = GamePalette.Hex("#389E48");
            var sunnyLeaf = GamePalette.Hex("#5CC44E");
            var crownHighlight = GamePalette.Hex("#82DE5E");

            Shape(canopyRig, "Canopy_Base", canopySpr, new Vector2(0.06f, -0.08f) * scale, new Vector2(2.65f, 2.32f) * scale, deepLeaf, 2);
            Shape(canopyRig, "Canopy_R", canopySpr, new Vector2(0.48f, 0.10f) * scale, Vector2.one * 1.72f * scale, mainLeaf, 3);
            Shape(canopyRig, "Canopy_L", canopySpr, new Vector2(-0.48f, 0.12f) * scale, Vector2.one * 1.74f * scale, sunnyLeaf, 3);
            Shape(canopyRig, "Canopy_Center", canopySpr, new Vector2(-0.04f, 0.32f) * scale, new Vector2(1.92f, 1.66f) * scale, sunnyLeaf, 4);
            Shape(canopyRig, "Canopy_TopSun", canopySpr, new Vector2(-0.22f, 0.48f) * scale, new Vector2(1.22f, 1.04f) * scale, crownHighlight, 5);

            if (fruit)
            {
                foreach (var spot in new[] { new Vector2(0.52f, 0.18f), new Vector2(-0.28f, -0.22f), new Vector2(0.16f, 0.54f) })
                {
                    Shape(canopyRig, "Fruit_Outline", circle, spot * scale, Vector2.one * 0.32f * scale, GamePalette.Outline, 6);
                    Shape(canopyRig, "Fruit", circle, spot * scale, Vector2.one * 0.23f * scale, GamePalette.Hex("#FF9F2E"), 7);
                }
            }

            var collider = root.gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.25f;
            collider.offset = new Vector2(0f, 0.2f);
        }

        private static void Lamp(Transform parent, float x, float y)
        {
            var root = Root(parent, "Lamp", new Vector2(x, y), y);
            // Tapered metallic streetlamp with base collar and curved arm
            Shape(root, "Base_Outline", roundedSmall, new Vector2(0f, 0.18f), new Vector2(0.42f, 0.38f), GamePalette.Outline, 0);
            Shape(root, "Base", roundedSmall, new Vector2(0f, 0.18f), new Vector2(0.30f, 0.26f), GamePalette.Hex("#5E687E"), 1);
            Shape(root, "Post_Outline", roundedSmall, new Vector2(0f, 1.15f), new Vector2(0.24f, 2.25f), GamePalette.Outline, 0);
            Shape(root, "Post", roundedSmall, new Vector2(0f, 1.15f), new Vector2(0.13f, 2.15f), GamePalette.Hex("#788299"), 1);

            Shape(root, "Head_Outline", circle, new Vector2(0f, 2.30f), new Vector2(0.56f, 0.44f), GamePalette.Outline, 2);
            Shape(root, "Head_Cap", circle, new Vector2(0f, 2.34f), new Vector2(0.46f, 0.30f), GamePalette.Hex("#5E687E"), 2);
            Shape(root, "Head_Bulb", circle, new Vector2(0f, 2.24f), new Vector2(0.36f, 0.24f), GamePalette.Hex("#FFE688"), 3);
            Flickering(Glow(root, "Glow", new Vector2(0f, 2.1f), 4.0f, "#FFE082", 0.24f, 20), 0.12f, 1.3f, x * 3.1f, false);
            // Seen from above at an angle, the head shines on the ground around the foot of the post: the light sits between them.
            AddNightLight(root, new Vector2(0f, 0.8f), 3.6f, "#FFD98A", 0.95f, x * 3.1f);

            var collider = root.gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.15f;
            collider.offset = new Vector2(0f, 0.1f);
        }

        /// <summary>Ornate black wrought-iron colonial streetlamp in the Central Plaza.</summary>
        private static void ColonialLamp(Transform parent, float x, float y)
        {
            var root = Root(parent, "ColonialLamp", new Vector2(x, y), y);
            var iron = GamePalette.Hex("#282C38");
            Shape(root, "Base_Out", roundedSmall, new Vector2(0f, 0.26f), new Vector2(0.50f, 0.54f), GamePalette.Outline, 0);
            Shape(root, "Base", roundedSmall, new Vector2(0f, 0.26f), new Vector2(0.38f, 0.42f), iron, 1);
            Shape(root, "Post_Out", roundedSmall, new Vector2(0f, 1.65f), new Vector2(0.26f, 3.10f), GamePalette.Outline, 0);
            Shape(root, "Post", roundedSmall, new Vector2(0f, 1.65f), new Vector2(0.14f, 2.98f), iron, 1);

            // Decorative wrought-iron crossbars & scrolls near top
            Shape(root, "Crossbar", roundedSmall, new Vector2(0f, 2.75f), new Vector2(0.86f, 0.12f), GamePalette.Outline, 2);
            Shape(root, "ScrollL", ring, new Vector2(-0.24f, 2.62f), Vector2.one * 0.26f, GamePalette.Outline, 2);
            Shape(root, "ScrollR", ring, new Vector2(0.24f, 2.62f), Vector2.one * 0.26f, GamePalette.Outline, 2);

            // Hexagonal glowing colonial lantern head
            Shape(root, "Lantern_Out", roundedSmall, new Vector2(0f, 3.25f), new Vector2(0.68f, 0.78f), GamePalette.Outline, 3);
            Shape(root, "Lantern_Glass", roundedSmall, new Vector2(0f, 3.25f), new Vector2(0.52f, 0.62f), GamePalette.Hex("#FFE680"), 4);
            Shape(root, "Lantern_Core", circle, new Vector2(0f, 3.25f), Vector2.one * 0.28f, GamePalette.White, 5);
            if (triangle != null)
            {
                Shape(root, "Lantern_RoofOut", triangle, new Vector2(0f, 3.70f), new Vector2(0.86f, 0.38f), GamePalette.Outline, 5);
                Shape(root, "Lantern_Roof", triangle, new Vector2(0f, 3.68f), new Vector2(0.72f, 0.28f), iron, 6);
            }

            Flickering(Glow(root, "LanternGlow", new Vector2(0f, 3.2f), 4.6f, "#FFE074", 0.32f, 20), 0.12f, 1.4f, x * 2.7f, false);
            AddNightLight(root, new Vector2(0f, 1.2f), 4.2f, "#FFE08A", 1f, x * 2.7f);

            var collider = root.gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.16f;
            collider.offset = new Vector2(0f, 0.1f);
        }

        private static void Bench(Transform parent, Transform shadows, float x, float y)
        {
            PlazaBench(parent, shadows, x, y, 0f);
        }

        /// <summary>Wooden-slat park bench with black wrought-iron armrests and legs, optionally angled toward the fountain.</summary>
        private static void PlazaBench(Transform parent, Transform shadows, float x, float y, float tiltDegrees)
        {
            var root = Root(parent, "Bench", new Vector2(x, y), y);
            BuildThing(root, "Bench", "Sentarte en la banca", Vector2.zero, new Vector2(2.0f, 0.7f),
                "Te sentás un rato a ver pasar la gente. Pasear por el parque no cuesta nada.");
            GroundShadowRect(shadows, new Vector2(x + 0.08f, y - 0.02f), new Vector2(2.1f, 0.75f), 0.26f);

            var rig = new GameObject("BenchRig").transform;
            rig.SetParent(root, false);
            rig.localRotation = Quaternion.Euler(0f, 0f, tiltDegrees);

            // Black wrought-iron legs & curved armrests
            foreach (float lx in new[] { -0.86f, 0.86f })
            {
                Shape(rig, "IronLeg", roundedSmall, new Vector2(lx, 0.32f), new Vector2(0.16f, 0.68f), GamePalette.Outline, 0);
                Shape(rig, "IronArm", roundedSmall, new Vector2(lx, 0.48f), new Vector2(0.18f, 0.42f), GamePalette.Hex("#282C38"), 4);
            }

            // Wooden slat backrest
            Shape(rig, "Back_Outline", roundedSmall, new Vector2(0f, 0.64f), new Vector2(2.06f, 0.40f), GamePalette.Outline, 1);
            Shape(rig, "Back", roundedSmall, new Vector2(0f, 0.64f), new Vector2(1.92f, 0.28f), GamePalette.Hex("#9C6334"), 2);
            Shape(rig, "Back_Slat", square, new Vector2(0f, 0.64f), new Vector2(1.88f, 0.04f), GamePalette.Outline, 3);

            // Wooden slat seat
            Shape(rig, "Seat_Outline", roundedSmall, new Vector2(0f, 0.28f), new Vector2(2.06f, 0.56f), GamePalette.Outline, 2);
            Shape(rig, "Seat", roundedSmall, new Vector2(0f, 0.28f), new Vector2(1.92f, 0.42f), GamePalette.Hex("#C2834B"), 3);
            Shape(rig, "Seat_Slat", square, new Vector2(0f, 0.28f), new Vector2(1.88f, 0.04f), GamePalette.Outline, 3);

            var collider = root.gameObject.AddComponent<BoxCollider2D>();
            collider.offset = new Vector2(0f, 0.3f);
            collider.size = new Vector2(2.0f, 0.6f);
        }

        private static void Bush(Transform parent, Transform shadows, float x, float y, float scale)
        {
            if (OnPavement(x, y, 0.5f) || InsideBuilding(x, y))
            {
                return;
            }

            var root = Root(parent, "Bush", new Vector2(x, y), y);
            GroundShadow(shadows, new Vector2(x + 0.15f, y), new Vector2(1.4f * scale, 0.55f * scale), 0.24f);

            var bushRig = new GameObject("BushRig").transform;
            bushRig.SetParent(root, false);
            bushRig.localPosition = new Vector2(0f, 0.42f * scale);
            bushRig.gameObject.AddComponent<FoliagePhysics>().Configure(4.6f, 1.75f, x * 0.83f + y * 0.47f, 0.032f, true, 1.6f, 13f);

            var bushSpr = treeCanopySprite != null ? treeCanopySprite : circle;
            Shape(bushRig, "Bush_Dark", bushSpr, new Vector2(0.05f, -0.04f) * scale, new Vector2(1.38f, 1.18f) * scale, GamePalette.Hex("#26753B"), 1);
            Shape(bushRig, "Bush", bushSpr, new Vector2(-0.04f, 0.05f) * scale, new Vector2(1.20f, 1.04f) * scale, GamePalette.Hex("#42A84C"), 2);
            Shape(bushRig, "Light", circle, new Vector2(-0.2f, 0.22f) * scale, new Vector2(0.48f, 0.36f) * scale, GamePalette.Hex("#78D962"), 3);
            Shape(bushRig, "Bloom_Out", circle, new Vector2(0.24f, 0.16f) * scale, Vector2.one * 0.24f * scale, GamePalette.Outline, 4);
            Shape(bushRig, "Bloom", circle, new Vector2(0.24f, 0.16f) * scale, Vector2.one * 0.17f * scale, GamePalette.Hex("#FF5E7E"), 4);
        }

        /// <summary>
        /// 3/4 Side-Top Perspective Cel-Shaded Car (matching the red hatchback in the concept art):
        /// sculpted chassis, raised cabin roof, side windows, angled windshield, bumpers, headlights,
        /// and round rubber tires with silver hubcaps along the bottom edge.
        /// </summary>
        private static Transform Car(Transform parent, Transform shadows, float x, float y, string hex, float speed = 0f, bool tinted = false)
        {
            var root = Root(parent, "Car", new Vector2(x, y), y - 0.55f);
            bool driving = speed != 0f;
            if (driving)
            {
                var shadowColor = GamePalette.Outline;
                shadowColor.a = 0.28f;
                OnGround(root, "Shadow", softCircle, new Vector2(0.06f, -0.42f), new Vector2(2.85f, 0.95f), shadowColor, TownShadow);
                root.gameObject.AddComponent<TrafficCar>();
                var body2d = root.gameObject.AddComponent<Rigidbody2D>();
                body2d.bodyType = RigidbodyType2D.Kinematic;
                body2d.useFullKinematicContacts = true;
                root.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(2.5f, 1.1f);
                root.gameObject.AddComponent<Drift>().ConfigureLane(speed, MapLayout.PlayWestX - 12f, MapLayout.PlayEastX + 12f);
                if (speed < 0f)
                {
                    root.localScale = new Vector3(-1f, 1f, 1f);
                }
            }
            else
            {
                BuildThing(root, "Car", "Ver el carro", Vector2.zero, new Vector2(2.4f, 1.1f),
                    "Un carro no es solo la cuota: marchamo, RTV, seguro, gasolina y mantenimiento, todos los años.");
                GroundShadowRect(shadows, new Vector2(x + 0.08f, y - 0.38f), new Vector2(2.85f, 0.95f), 0.34f);
            }

            var body = GamePalette.Hex(hex);
            var darkBody = GamePalette.Shade(body, 0.78f);
            var roofCol = GamePalette.Shade(body, 1.12f);

            // Far-side tires peeking slightly under the chassis
            foreach (float wx in new[] { -0.78f, 0.78f })
            {
                Shape(root, "FarWheel", circle, new Vector2(wx + 0.08f, -0.32f), new Vector2(0.56f, 0.48f), GamePalette.Outline, 0);
            }

            // Sculpted lower car body (hood, doors, fenders)
            Shape(root, "LowerBody_Out", rounded, new Vector2(0f, -0.06f), new Vector2(2.78f, 0.86f), GamePalette.Outline, 1);
            Shape(root, "LowerBody", rounded, new Vector2(0f, -0.06f), new Vector2(2.62f, 0.70f), body, 2);
            Shape(root, "LowerBody_SkirtShade", roundedSmall, new Vector2(0f, -0.28f), new Vector2(2.50f, 0.22f), darkBody, 2);

            // Raised 3/4 passenger cabin & roof
            Shape(root, "Cabin_Out", rounded, new Vector2(-0.14f, 0.34f), new Vector2(1.82f, 0.78f), GamePalette.Outline, 2);
            Shape(root, "Cabin", rounded, new Vector2(-0.14f, 0.34f), new Vector2(1.66f, 0.64f), body, 3);
            Shape(root, "Roof_Top", rounded, new Vector2(-0.16f, 0.48f), new Vector2(1.44f, 0.34f), roofCol, 3);

            // Side windows + angled front windshield
            var glass = tinted ? GamePalette.Hex("#161922") : GamePalette.Hex("#78B8DC");
            Shape(root, "RearSideWindow_Out", roundedSmall, new Vector2(-0.56f, 0.30f), new Vector2(0.58f, 0.40f), GamePalette.Outline, 4);
            Shape(root, "RearSideWindow", roundedSmall, new Vector2(-0.56f, 0.30f), new Vector2(0.48f, 0.30f), glass, 5);

            Shape(root, "FrontSideWindow_Out", roundedSmall, new Vector2(0.10f, 0.30f), new Vector2(0.62f, 0.40f), GamePalette.Outline, 4);
            Shape(root, "FrontSideWindow", roundedSmall, new Vector2(0.10f, 0.30f), new Vector2(0.52f, 0.30f), glass, 5);

            Shape(root, "Windshield_Out", roundedSmall, new Vector2(0.58f, 0.30f), new Vector2(0.36f, 0.44f), GamePalette.Outline, 4);
            Shape(root, "Windshield", roundedSmall, new Vector2(0.58f, 0.30f), new Vector2(0.26f, 0.34f), glass, 5);

            if (!tinted)
            {
                var shine = new Color(1f, 1f, 1f, 0.55f);
                Tilt(Shape(root, "WinShineA", roundedSmall, new Vector2(0.08f, 0.32f), new Vector2(0.10f, 0.24f), shine, 6), -20f);
                Tilt(Shape(root, "WinShineB", roundedSmall, new Vector2(0.58f, 0.32f), new Vector2(0.09f, 0.26f), shine, 6), -20f);
            }

            // Side door seams, handle, bumpers, headlights & taillights
            Shape(root, "DoorSeam", square, new Vector2(-0.22f, -0.06f), new Vector2(0.04f, 0.54f), GamePalette.Outline, 4);
            Shape(root, "DoorHandle", roundedSmall, new Vector2(-0.08f, 0.06f), new Vector2(0.14f, 0.06f), GamePalette.Outline, 4);
            Shape(root, "FrontBumper_Out", roundedSmall, new Vector2(1.30f, -0.22f), new Vector2(0.26f, 0.22f), GamePalette.Outline, 4);
            Shape(root, "FrontBumper", roundedSmall, new Vector2(1.30f, -0.22f), new Vector2(0.18f, 0.14f), GamePalette.Hex("#5A6275"), 5);
            Shape(root, "RearBumper_Out", roundedSmall, new Vector2(-1.30f, -0.22f), new Vector2(0.24f, 0.22f), GamePalette.Outline, 4);
            Shape(root, "RearBumper", roundedSmall, new Vector2(-1.30f, -0.22f), new Vector2(0.16f, 0.14f), GamePalette.Hex("#5A6275"), 5);

            Shape(root, "Headlight_Out", circle, new Vector2(1.28f, 0.02f), new Vector2(0.22f, 0.26f), GamePalette.Outline, 4);
            Shape(root, "Headlight", circle, new Vector2(1.28f, 0.02f), new Vector2(0.14f, 0.18f), GamePalette.Hex("#FFE875"), 5);
            Shape(root, "Taillight_Out", roundedSmall, new Vector2(-1.28f, 0.04f), new Vector2(0.16f, 0.24f), GamePalette.Outline, 4);
            Shape(root, "Taillight", roundedSmall, new Vector2(-1.28f, 0.04f), new Vector2(0.10f, 0.18f), GamePalette.Hex("#FF4242"), 5);

            // Round 3/4 front and rear wheels with dark rubber tires and silver hubcaps
            foreach (float wx in new[] { -0.76f, 0.76f })
            {
                Shape(root, "WheelWell", circle, new Vector2(wx, -0.34f), new Vector2(0.68f, 0.60f), GamePalette.Outline, 5);
                Shape(root, "Tire", circle, new Vector2(wx, -0.36f), new Vector2(0.58f, 0.54f), GamePalette.Hex("#22252E"), 6);
                Shape(root, "Rim_Out", circle, new Vector2(wx, -0.36f), new Vector2(0.36f, 0.33f), GamePalette.Outline, 7);
                Shape(root, "Rim", circle, new Vector2(wx, -0.36f), new Vector2(0.28f, 0.25f), GamePalette.Hex("#B8C2D4"), 8);
                Shape(root, "Hub", circle, new Vector2(wx, -0.36f), Vector2.one * 0.10f, GamePalette.Hex("#5A6275"), 9);
            }

            if (!driving)
            {
                var collider = root.gameObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(2.5f, 1.1f);
            }

            return root;
        }

        private static void CollectorCarBuild(Transform parent, Transform shadows)
        {
            var root = Car(parent, shadows, MapLayout.PlayWestX - 12f, 3.2f, "#15161C", 3.4f, true);
            root.name = "CollectorCar";
            var drift = root.GetComponent<Drift>();
            if (drift != null)
            {
                Object.DestroyImmediate(drift);
            }

            Shape(root, "GoldStripe", roundedSmall, new Vector2(0f, -0.04f), new Vector2(2.4f, 0.09f), GamePalette.Hex("#FFD23F"), 5);
            Shape(root, "Speaker", circle, new Vector2(-0.15f, 0.68f), Vector2.one * 0.26f, GamePalette.Hex("#FFD23F"), 5);
            root.gameObject.AddComponent<CollectorCar>().Configure(3.4f, MapLayout.PlayWestX - 12f, MapLayout.PlayEastX + 12f);
            BuildCollectorMen(parent);
        }

        private static void Hedge(Transform parent, Vector2 center, Vector2 size)
        {
            var root = Root(parent, "Hedge", center, center.y - size.y * 0.5f);
            Shape(root, "Outline", rounded, Vector2.zero, size + Vector2.one * 0.16f, GamePalette.Outline, 0);
            Shape(root, "Hedge", rounded, Vector2.zero, size, GamePalette.Hex("#389E48"), 1);

            var light = GamePalette.Hex("#68CF50");
            light.a = 0.7f;
            float length = Mathf.Max(size.x, size.y);
            bool horizontal = size.x >= size.y;
            for (float t = -length * 0.5f + 1f; t < length * 0.5f - 0.5f; t += 2.2f)
            {
                Vector2 spot = horizontal ? new Vector2(t, 0.2f) : new Vector2(0.2f, t);
                Shape(root, "Leaf", circle, spot, Vector2.one * 0.55f, light, 2);
            }

            root.gameObject.AddComponent<BoxCollider2D>().size = size;
        }

        /// <summary>
        /// 3-Tier Carved Stone Colonial Fountain with ornamental relief panels, turquoise water,
        /// animated white foam rings, floating pink petals, and cascading water streams.
        /// </summary>
        /// <summary>How far below the middle of the fountain the player stands to use it: just past the stone rim.</summary>
        private const float FountainFront = 1.85f;

        private static void Fountain(Transform parent, Transform shadows, Vector2 center)
        {
            GroundShadow(shadows, center + new Vector2(0.22f, -0.48f), new Vector2(4.7f, 2.3f), 0.28f);
            var root = Root(parent, "Fountain", center, center.y - 1.3f);
            // Only from the front: a strip below the basin, so walking along its sides does not offer it.
            BuildThing(root, "Fountain", "Ver la fuente", new Vector2(0f, -FountainFront), new Vector2(1.6f, 0.4f),
                "Hay monedas en el fondo. Pedir un deseo es gratis; cumplirlo casi siempre empieza por ahorrar.");

            var stone = GamePalette.Hex("#E3D9C6");
            var stoneShade = GamePalette.Hex("#BFB29B");
            var stoneDeep = GamePalette.Hex("#9E9078");
            var waterDeep = GamePalette.Hex("#269CD6");
            var waterTurquoise = GamePalette.Hex("#42C6EB");
            var waterBright = GamePalette.Hex("#88EBFF");

            // Outer carved stone basin with ornamental relief segments along the front wall
            Shape(root, "Basin_Outline", circle, new Vector2(0f, -0.04f), new Vector2(4.36f, 2.94f), GamePalette.Outline, 0);
            Shape(root, "Basin_FrontWall", circle, new Vector2(0f, -0.10f), new Vector2(4.20f, 2.76f), stoneShade, 1);
            for (int seg = -3; seg <= 3; seg++)
            {
                float ang = seg * 22f * Mathf.Deg2Rad;
                Vector2 rp = new Vector2(Mathf.Sin(ang) * 1.72f, -Mathf.Cos(ang) * 1.12f - 0.08f);
                Shape(root, "Relief_Out", roundedSmall, rp, new Vector2(0.42f, 0.22f), GamePalette.Outline, 1);
                Shape(root, "Relief", roundedSmall, rp, new Vector2(0.32f, 0.14f), stoneDeep, 2);
            }

            Shape(root, "Basin_RimOut", circle, new Vector2(0f, 0.06f), new Vector2(4.22f, 2.66f), GamePalette.Outline, 2);
            Shape(root, "Basin_Rim", circle, new Vector2(0f, 0.06f), new Vector2(4.06f, 2.52f), stone, 3);
            Shape(root, "Basin_InnerOut", circle, new Vector2(0f, 0.08f), new Vector2(3.56f, 2.14f), GamePalette.Outline, 3);
            Shape(root, "Basin_InnerWall", circle, new Vector2(0f, 0.08f), new Vector2(3.44f, 2.02f), stoneDeep, 4);

            // Vibrant multi-toned turquoise water in the main basin
            Shape(root, "Water_Deep", circle, new Vector2(0f, 0.04f), new Vector2(3.38f, 1.96f), waterDeep, 4);
            Shape(root, "Water_Mid", circle, new Vector2(0f, 0.06f), new Vector2(3.05f, 1.74f), waterTurquoise, 5);
            Shape(root, "Water_Glow", softCircle, new Vector2(-0.35f, 0.16f), new Vector2(2.2f, 1.2f), waterBright, 5);

            // Animated concentric white foam rings
            var ripple = new Color(1f, 1f, 1f, 0.65f);
            Flickering(Shape(root, "RippleOuter", ring, new Vector2(0f, 0.05f), new Vector2(2.85f, 1.58f), ripple, 5), 0.55f, 1.6f, 3f);
            Flickering(Shape(root, "RippleInner", ring, new Vector2(0f, 0.06f), new Vector2(1.98f, 1.08f), ripple, 5), 0.55f, 2.2f, 9f);

            // Floating pink/red flower petals & green leaves on the water surface
            foreach (var petal in new[] { new Vector2(-1.15f, 0.12f), new Vector2(-0.72f, -0.46f), new Vector2(0.88f, -0.38f), new Vector2(1.18f, 0.18f), new Vector2(-0.18f, -0.62f) })
            {
                Shape(root, "Petal_Out", circle, petal, new Vector2(0.22f, 0.16f), GamePalette.Outline, 5);
                Shape(root, "Petal", circle, petal, new Vector2(0.15f, 0.10f), GamePalette.Hex("#FF5E7E"), 6);
            }

            // Tier 2: Carved middle stone pedestal & bowl
            Shape(root, "Pedestal1_Out", roundedSmall, new Vector2(0f, 0.52f), new Vector2(0.86f, 0.96f), GamePalette.Outline, 6);
            Shape(root, "Pedestal1", roundedSmall, new Vector2(0f, 0.52f), new Vector2(0.72f, 0.84f), stoneShade, 7);
            Shape(root, "MidBowl_Out", circle, new Vector2(0f, 1.05f), new Vector2(2.42f, 1.24f), GamePalette.Outline, 8);
            Shape(root, "MidBowl_Under", circle, new Vector2(0f, 0.98f), new Vector2(2.26f, 1.12f), stoneShade, 9);
            Shape(root, "MidBowl_Rim", circle, new Vector2(0f, 1.08f), new Vector2(2.26f, 1.06f), stone, 9);
            Shape(root, "MidBowl_Water", circle, new Vector2(0f, 1.10f), new Vector2(1.92f, 0.84f), waterTurquoise, 10);

            // Cascading water streams falling from the middle bowl into the main basin
            var streamCol = new Color(0.88f, 0.98f, 1f, 0.82f);
            for (int s = -3; s <= 3; s++)
            {
                float sx = s * 0.28f;
                float len = 0.68f - Mathf.Abs(s) * 0.04f;
                var stream = Shape(root, "CascadeStream", roundedSmall, new Vector2(sx, 0.62f), new Vector2(0.09f, len), streamCol, 10);
                Flickering(stream, 0.45f, 2.8f + Mathf.Abs(s) * 0.3f, s * 1.9f);
                var foam = Shape(root, "SplashFoam", circle, new Vector2(sx, 0.24f - Mathf.Abs(s) * 0.02f), new Vector2(0.22f, 0.12f), Color.white, 6);
                Flickering(foam, 0.5f, 3.2f, s * 2.3f);
            }

            // Tier 3: Upper crown pedestal, small bowl, and bubbling water plume
            Shape(root, "Pedestal2_Out", roundedSmall, new Vector2(0f, 1.48f), new Vector2(0.54f, 0.78f), GamePalette.Outline, 11);
            Shape(root, "Pedestal2", roundedSmall, new Vector2(0f, 1.48f), new Vector2(0.40f, 0.68f), stone, 12);
            Shape(root, "TopBowl_Out", circle, new Vector2(0f, 1.88f), new Vector2(1.32f, 0.68f), GamePalette.Outline, 12);
            Shape(root, "TopBowl", circle, new Vector2(0f, 1.88f), new Vector2(1.18f, 0.54f), stone, 13);
            Shape(root, "TopBowl_Water", circle, new Vector2(0f, 1.90f), new Vector2(0.96f, 0.40f), waterBright, 14);

            // Arching fountain crown water plume
            var spray = new Color(0.92f, 0.99f, 1f, 0.90f);
            for (int k = -2; k <= 2; k++)
            {
                float angle = k * 22f;
                float length = 0.78f - Mathf.Abs(k) * 0.10f;
                var jet = Shape(root, "PlumeJet", roundedSmall,
                    new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad) * length * 0.5f, 2.08f + Mathf.Cos(angle * Mathf.Deg2Rad) * length * 0.45f),
                    new Vector2(0.11f, length), spray, 14);
                Tilt(jet, -angle);
                Flickering(jet, 0.50f, 2.6f + Mathf.Abs(k) * 0.3f, k * 1.7f);
            }

            var collider = root.gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 1.45f;
            collider.offset = new Vector2(0f, 0.1f);
        }

        private static void PlazaPlanter(Transform parent, Transform shadows, Vector2 pos)
        {
            var root = Root(parent, "PlazaPlanter", pos, pos.y);
            GroundShadow(shadows, pos + new Vector2(0.10f, -0.05f), new Vector2(1.35f, 0.58f), 0.28f);
            Shape(root, "Pot_Outline", roundedSmall, new Vector2(0f, 0.30f), new Vector2(1.12f, 0.60f), GamePalette.Outline, 0);
            Shape(root, "Pot", roundedSmall, new Vector2(0f, 0.30f), new Vector2(0.98f, 0.46f), GamePalette.Hex("#B85A38"), 1);
            Shape(root, "PotRim_Out", roundedSmall, new Vector2(0f, 0.54f), new Vector2(1.24f, 0.20f), GamePalette.Outline, 1);
            Shape(root, "PotRim", roundedSmall, new Vector2(0f, 0.54f), new Vector2(1.10f, 0.12f), GamePalette.Hex("#CF6E4A"), 2);

            var foliage = new GameObject("PlanterFoliage").transform;
            foliage.SetParent(root, false);
            foliage.localPosition = new Vector2(0f, 0.62f);
            foliage.gameObject.AddComponent<FoliagePhysics>().Configure(4.2f, 1.65f, pos.x * 0.9f, 0.028f, true, 1.5f, 12f);

            var frond = palmFrondSprite != null ? palmFrondSprite : circle;
            foreach (var l in new[] { (-0.38f, 0.22f, 145f, "#26783C"), (0.38f, 0.22f, 35f, "#26783C"), (-0.22f, 0.44f, 118f, "#3FA84C"), (0.22f, 0.44f, 62f, "#3FA84C"), (0f, 0.52f, 90f, "#5EC958") })
            {
                var leaf = Shape(foliage, "Frond", frond, new Vector2(l.Item1, l.Item2), new Vector2(1.08f, 0.50f), GamePalette.Hex(l.Item4), 3);
                Tilt(leaf, l.Item3);
            }

            // Bright red and orange Heliconia flower spikes
            foreach (var h in new[] { new Vector2(-0.18f, 0.48f), new Vector2(0.18f, 0.44f), new Vector2(0f, 0.64f) })
            {
                Shape(foliage, "Heliconia_Out", roundedSmall, h, new Vector2(0.26f, 0.44f), GamePalette.Outline, 4);
                Shape(foliage, "Heliconia", roundedSmall, h, new Vector2(0.18f, 0.34f), GamePalette.Hex("#FF4838"), 5);
                Shape(foliage, "Heliconia_Tip", circle, h + new Vector2(0f, 0.10f), Vector2.one * 0.12f, GamePalette.Hex("#FFD23F"), 6);
            }
        }

        private static void FestoonGarland(Transform parent, Vector2 lampA, Vector2 lampB, float heightOffset)
        {
            Vector2 mid = (lampA + lampB) * 0.5f;
            var root = Root(parent, "FestoonGarland", mid, lampA.y - 0.1f);
            var rig = new GameObject("GarlandRig").transform;
            rig.SetParent(root, false);
            rig.localPosition = new Vector2(0f, heightOffset);
            rig.gameObject.AddComponent<FoliagePhysics>().Configure(2.4f, 1.35f, mid.x + mid.y, 0.012f, false);

            float span = lampB.x - lampA.x;
            int bulbs = 9;
            Vector2 prevPos = Vector2.zero;
            for (int i = 0; i <= bulbs; i++)
            {
                float t = i / (float)bulbs;
                float x = (t - 0.5f) * span;
                float sag = -0.64f * (4f * t * (1f - t));
                Vector2 curPos = new Vector2(x, sag);
                if (i > 0)
                {
                    Vector2 segMid = (prevPos + curPos) * 0.5f;
                    Vector2 delta = curPos - prevPos;
                    float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    var seg = Shape(rig, "WireSegment", square, segMid, new Vector2(delta.magnitude + 0.04f, 0.06f), GamePalette.Outline, 14);
                    Tilt(seg, angle);
                }
                prevPos = curPos;

                Shape(rig, "WireNode", circle, curPos, Vector2.one * 0.10f, GamePalette.Outline, 14);
                if (i > 0 && i < bulbs)
                {
                    string bulbHex = i % 2 == 0 ? "#FFE252" : "#FFAA3B";
                    Shape(rig, "Bulb_Outline", circle, new Vector2(x, sag - 0.15f), Vector2.one * 0.30f, GamePalette.Outline, 15);
                    Shape(rig, "Bulb", circle, new Vector2(x, sag - 0.15f), Vector2.one * 0.20f, GamePalette.Hex(bulbHex), 16);
                    Flickering(Glow(rig, "BulbGlow", new Vector2(x, sag - 0.15f), 1.65f, bulbHex, 0.36f, 17), 0.15f, 1.8f, i * 1.9f);
                }
            }
        }

        /// <summary>
        /// Complete pointed wooden picket fence enclosing Tu Casa's front garden (with open gate at the stone path).
        /// </summary>
        private static void CasaFrontFence(Transform parent, Transform shadows)
        {
            var root = Root(parent, "CasaFence", new Vector2(-20f, 5.42f), 5.42f);
            var wood = GamePalette.Hex("#C28A52");
            var woodDark = GamePalette.Hex("#966334");

            // Left fence span (-4.2 to -0.35) and Right fence span (1.25 to 4.25)
            DrawPicketSpan(root, -4.25f, -0.35f, wood, woodDark);
            DrawPicketSpan(root, 1.25f, 4.25f, wood, woodDark);
        }

        private static void DrawPicketSpan(Transform root, float xMin, float xMax, Color wood, Color woodDark)
        {
            float width = xMax - xMin;
            float cx = (xMin + xMax) * 0.5f;

            // Two horizontal wooden rails
            foreach (float ry in new[] { 0.24f, 0.52f })
            {
                Shape(root, "Rail_Out", roundedSmall, new Vector2(cx, ry), new Vector2(width + 0.12f, 0.18f), GamePalette.Outline, 0);
                Shape(root, "Rail", roundedSmall, new Vector2(cx, ry), new Vector2(width + 0.02f, 0.10f), woodDark, 1);
            }

            // Vertical pointed pickets
            for (float px = xMin + 0.12f; px <= xMax - 0.08f; px += 0.34f)
            {
                Shape(root, "Picket_Out", roundedSmall, new Vector2(px, 0.42f), new Vector2(0.26f, 0.86f), GamePalette.Outline, 2);
                Shape(root, "Picket", roundedSmall, new Vector2(px, 0.42f), new Vector2(0.18f, 0.76f), wood, 3);
                Shape(root, "Picket_Grain", square, new Vector2(px + 0.04f, 0.40f), new Vector2(0.03f, 0.64f), woodDark, 3);
                if (triangle != null)
                {
                    Shape(root, "Tip_Out", triangle, new Vector2(px, 0.86f), new Vector2(0.28f, 0.20f), GamePalette.Outline, 2);
                    Shape(root, "Tip", triangle, new Vector2(px, 0.84f), new Vector2(0.20f, 0.14f), wood, 3);
                }
            }
        }

        /// <summary>
        /// Builds the wooden utility poles with transformers and swaying catenary overhead power lines
        /// across the residential street, plus the curved metallic streetlamp next to Tu Casa.
        /// </summary>
        private static void BuildOverheadPowerNetwork(Transform parent, Transform shadows)
        {
            Vector2 poleWest = new Vector2(-34.3f, 5.45f);
            Vector2 poleCasa = new Vector2(-21.25f, 5.45f);
            Vector2 poleEast = new Vector2(-13.85f, 5.45f);

            UtilityPole(parent, shadows, poleWest);
            UtilityPole(parent, shadows, poleCasa);
            CurvedStreetLamp(parent, shadows, new Vector2(-14.85f, 5.65f));

            // Swaying catenary overhead power lines connecting the poles across the upper screen
            OverheadPowerSpan(parent, poleWest, poleCasa, 4.45f);
            OverheadPowerSpan(parent, poleCasa, new Vector2(-14.85f, 5.65f), 4.45f);
        }

        private static void UtilityPole(Transform parent, Transform shadows, Vector2 pos)
        {
            var root = Root(parent, "UtilityPole", pos, pos.y);
            GroundShadow(shadows, pos + new Vector2(0.15f, -0.04f), new Vector2(0.95f, 0.42f), 0.28f);

            var wood = GamePalette.Hex("#8E623C");
            var woodLight = GamePalette.Hex("#AC7B4F");

            // Tall wooden utility pole shaft
            Shape(root, "Pole_Out", roundedSmall, new Vector2(0f, 2.35f), new Vector2(0.36f, 4.75f), GamePalette.Outline, 10);
            Shape(root, "Pole", roundedSmall, new Vector2(0f, 2.35f), new Vector2(0.24f, 4.62f), wood, 11);
            Shape(root, "Pole_Highlight", roundedSmall, new Vector2(-0.05f, 2.35f), new Vector2(0.08f, 4.50f), woodLight, 11);

            // Banding / poster wraps near the bottom of the pole
            Shape(root, "PosterBand_Out", roundedSmall, new Vector2(0f, 1.05f), new Vector2(0.38f, 0.44f), GamePalette.Outline, 11);
            Shape(root, "PosterBand", roundedSmall, new Vector2(0f, 1.05f), new Vector2(0.26f, 0.34f), GamePalette.Hex("#D8D2C5"), 12);

            // Cylindrical grey electrical transformer bolted to the right side of the pole
            Shape(root, "Transformer_Out", roundedSmall, new Vector2(0.26f, 3.35f), new Vector2(0.46f, 0.78f), GamePalette.Outline, 12);
            Shape(root, "Transformer", roundedSmall, new Vector2(0.26f, 3.35f), new Vector2(0.34f, 0.66f), GamePalette.Hex("#7E889B"), 13);
            Shape(root, "Transformer_Shine", roundedSmall, new Vector2(0.20f, 3.35f), new Vector2(0.10f, 0.54f), GamePalette.Hex("#A3AEBF"), 13);
            Shape(root, "Transformer_Lid", roundedSmall, new Vector2(0.26f, 3.68f), new Vector2(0.40f, 0.14f), GamePalette.Hex("#5E6778"), 14);

            // Horizontal wooden crossarm & white ceramic insulators at the top
            Shape(root, "Crossarm_Out", roundedSmall, new Vector2(0f, 4.25f), new Vector2(1.68f, 0.24f), GamePalette.Outline, 13);
            Shape(root, "Crossarm", roundedSmall, new Vector2(0f, 4.25f), new Vector2(1.56f, 0.14f), woodLight, 14);
            foreach (float ix in new[] { -0.65f, -0.28f, 0.28f, 0.65f })
            {
                Shape(root, "Insulator_Out", roundedSmall, new Vector2(ix, 4.40f), new Vector2(0.18f, 0.22f), GamePalette.Outline, 14);
                Shape(root, "Insulator", roundedSmall, new Vector2(ix, 4.40f), new Vector2(0.10f, 0.14f), GamePalette.Hex("#E8EEF7"), 15);
            }
        }

        private static void CurvedStreetLamp(Transform parent, Transform shadows, Vector2 pos)
        {
            var root = Root(parent, "CurvedStreetLamp", pos, pos.y);
            GroundShadow(shadows, pos + new Vector2(0.12f, -0.04f), new Vector2(0.85f, 0.36f), 0.26f);

            var metal = GamePalette.Hex("#7A8499");
            var metalDark = GamePalette.Hex("#555E70");

            Shape(root, "Base_Out", roundedSmall, new Vector2(0f, 0.24f), new Vector2(0.48f, 0.48f), GamePalette.Outline, 10);
            Shape(root, "Base", roundedSmall, new Vector2(0f, 0.24f), new Vector2(0.36f, 0.36f), metalDark, 11);
            Shape(root, "Post_Out", roundedSmall, new Vector2(0f, 2.20f), new Vector2(0.26f, 4.10f), GamePalette.Outline, 10);
            Shape(root, "Post", roundedSmall, new Vector2(0f, 2.20f), new Vector2(0.15f, 3.98f), metal, 11);

            // Angled gooseneck arm & cobra-head lamp fixture
            var armOut = Shape(root, "Arm_Out", roundedSmall, new Vector2(-0.34f, 3.85f), new Vector2(0.78f, 0.20f), GamePalette.Outline, 11);
            var arm = Shape(root, "Arm", roundedSmall, new Vector2(-0.34f, 3.85f), new Vector2(0.68f, 0.11f), metal, 12);
            Tilt(armOut, 28f);
            Tilt(arm, 28f);

            Shape(root, "Fixture_Out", circle, new Vector2(-0.62f, 3.62f), new Vector2(0.56f, 0.36f), GamePalette.Outline, 12);
            Shape(root, "Fixture", circle, new Vector2(-0.62f, 3.64f), new Vector2(0.44f, 0.24f), metalDark, 13);
            Shape(root, "Fixture_Lens", circle, new Vector2(-0.62f, 3.58f), new Vector2(0.34f, 0.18f), GamePalette.Hex("#FFE688"), 13);
            Flickering(Glow(root, "StreetLampGlow", new Vector2(-0.62f, 3.50f), 4.2f, "#FFE082", 0.28f, 20), 0.10f, 1.4f, pos.x, false);
        }

        private static void OverheadPowerSpan(Transform parent, Vector2 poleA, Vector2 poleB, float heightOffset)
        {
            Vector2 mid = (poleA + poleB) * 0.5f;
            var root = Root(parent, "OverheadPowerSpan", mid, Mathf.Min(poleA.y, poleB.y) - 0.2f);
            var rig = new GameObject("PowerCablesRig").transform;
            rig.SetParent(root, false);
            rig.localPosition = new Vector2(0f, heightOffset);
            rig.gameObject.AddComponent<FoliagePhysics>().Configure(1.8f, 1.15f, mid.x * 0.5f, 0.012f, false);

            float span = poleB.x - poleA.x;
            float[] wireOffsetsY = { 0.0f, -0.38f, -0.74f };
            float[] wireSags = { 0.42f, 0.52f, 0.60f };

            for (int w = 0; w < wireOffsetsY.Length; w++)
            {
                int segments = 10;
                Vector2 prev = Vector2.zero;
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float x = (t - 0.5f) * span;
                    float y = wireOffsetsY[w] - wireSags[w] * (4f * t * (1f - t));
                    Vector2 cur = new Vector2(x, y);
                    if (i > 0)
                    {
                        Vector2 segMid = (prev + cur) * 0.5f;
                        Vector2 delta = cur - prev;
                        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                        var seg = Shape(rig, "PowerWire", square, segMid, new Vector2(delta.magnitude + 0.04f, 0.055f), GamePalette.Outline, 18);
                        Tilt(seg, angle);
                    }
                    prev = cur;
                }
            }
        }

        private static void BuildAlleyAndLot(Transform parent, Transform shadows)
        {
            // Trash cans and crates in the alley.
            foreach (var can in new[] { new Vector2(-15.2f, -4.1f), new Vector2(-12.9f, -6.9f) })
            {
                var root = Root(parent, "TrashCan", can, can.y - 0.4f);
                GroundShadow(shadows, can + new Vector2(0.1f, -0.35f), new Vector2(0.9f, 0.4f), 0.3f);
                Shape(root, "Outline", roundedSmall, new Vector2(0f, 0.3f), new Vector2(0.78f, 0.9f), GamePalette.Outline, 0);
                Shape(root, "Can", roundedSmall, new Vector2(0f, 0.28f), new Vector2(0.64f, 0.76f), GamePalette.Hex("#7E869C"), 1);
                Shape(root, "Lid", roundedSmall, new Vector2(0f, 0.68f), new Vector2(0.76f, 0.2f), GamePalette.Hex("#9AA3B8"), 2);
                root.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 0.8f);
            }

            var crates = Root(parent, "Crates", new Vector2(-15.1f, -5.6f), -6.0f);
            Shape(crates, "Crate_Outline", rounded, new Vector2(0f, 0.4f), new Vector2(1.14f, 1.14f), GamePalette.Outline, 0);
            Shape(crates, "Crate", rounded, new Vector2(0f, 0.4f), new Vector2(1.0f, 1.0f), GamePalette.Hex("#A9743F"), 1);
            Shape(crates, "Crate_Plank", square, new Vector2(0f, 0.4f), new Vector2(1.0f, 0.06f), GamePalette.Hex("#7A4F2A"), 2);
            Shape(crates, "Crate_Plank", square, new Vector2(0f, 0.4f), new Vector2(0.06f, 1.0f), GamePalette.Hex("#7A4F2A"), 2);
            crates.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(1.0f, 0.9f);

            // Neon sign over the alley mouth. It hangs high, so the player always walks under it.
            var sign = Root(parent, "AlleySign", new Vector2(-14f, -2.55f), -9f);
            Shape(sign, "Outline", roundedSmall, Vector2.zero, new Vector2(3.5f, 0.74f), GamePalette.Outline, 0);
            var neon = Shape(sign, "Neon", roundedSmall, Vector2.zero, new Vector2(3.36f, 0.6f), GamePalette.Hex("#8E1F2B"), 1);
            Flickering(neon, 0.45f, 7f, 4f);
            Label(sign, "PRESTAMOS RAPIDOS", Vector2.zero, GamePalette.Hex("#FFD23F"), 0.026f, TextAnchor.MiddleCenter);

            // The stand: striped canopy, a banner and a table with piles of cash.
            var tent = Root(parent, "Stand", new Vector2(7f, -5.2f), -5.2f);
            GroundShadowRect(shadows, new Vector2(7.3f, -4.4f), new Vector2(4.6f, 2.4f), 0.25f);
            Shape(tent, "Pole", square, new Vector2(-2.0f, 1.0f), new Vector2(0.14f, 2.0f), GamePalette.Hex("#6F7890"), 0);
            Shape(tent, "Pole", square, new Vector2(2.0f, 1.0f), new Vector2(0.14f, 2.0f), GamePalette.Hex("#6F7890"), 0);
            Shape(tent, "Canopy_Outline", rounded, new Vector2(0f, 2.25f), new Vector2(4.7f, 1.3f), GamePalette.Outline, 1);
            for (int i = 0; i < 6; i++)
            {
                Shape(tent, "Canopy", square, new Vector2(-2.0f + 0.4f + i * 0.72f, 2.25f), new Vector2(0.72f, 1.14f), GamePalette.Hex(i % 2 == 0 ? "#E8505B" : "#FFD23F"), 2);
            }

            Shape(tent, "Banner_Outline", roundedSmall, new Vector2(0f, 1.2f), new Vector2(4.1f, 0.74f), GamePalette.Outline, 3);
            Shape(tent, "Banner", roundedSmall, new Vector2(0f, 1.2f), new Vector2(3.96f, 0.6f), GamePalette.Hex("#2E7D4F"), 4);
            Label(tent, "INVIERTE YA 300%", new Vector2(0f, 1.2f), GamePalette.White, 0.027f, TextAnchor.MiddleCenter);

            var table = Root(parent, "StandTable", new Vector2(7f, -5.7f), -5.95f);
            Shape(table, "Table_Outline", rounded, new Vector2(0f, 0.3f), new Vector2(2.7f, 0.84f), GamePalette.Outline, 0);
            Shape(table, "Table", rounded, new Vector2(0f, 0.3f), new Vector2(2.54f, 0.68f), GamePalette.Hex("#A9743F"), 1);
            for (int i = 0; i < 3; i++)
            {
                Shape(table, "Cash", roundedSmall, new Vector2(-0.8f + i * 0.8f, 0.4f), new Vector2(0.56f, 0.34f), GamePalette.MoneyIn, 2);
                Shape(table, "Cash_Band", square, new Vector2(-0.8f + i * 0.8f, 0.4f), new Vector2(0.06f, 0.34f), GamePalette.Hex("#2E7D4F"), 3);
            }

            table.gameObject.AddComponent<BoxCollider2D>().offset = new Vector2(0f, 0.3f);
            table.GetComponent<BoxCollider2D>().size = new Vector2(2.5f, 0.6f);

            var seller = BuildNpc(parent, "Estafador", new Vector2(7f, -4.7f), new Look { Skin = "#C98B5C", Extra = Accessory.Tie, ExtraColor = "#2E7D4F" }, true);
            BuildThing(seller, "Talk", "Hablar con el del puesto", Vector2.zero, new Vector2(0.8f, 0.8f),
                "Triplicá tu plata en un mes. Solo traé efectivo y no le contés a nadie.",
                "Es seguro, es seguro. Papeles no tengo, pero confiá en mí.",
                "Con los {cash} que tenés hoy, mañana tendrías el triple. Eso me dijeron mis clientes.",
                "Nadie puede garantizar 300% en un mes. Un negocio así se llama esquema piramidal: gana el que entra primero.");
        }

        private static void BuildDoorsAndStations(Transform parent)
        {
            Doorway(parent, new Vector2(-19.5f, 5.7f), SceneNames.Casa);
            Doorway(parent, new Vector2(-8.5f, 5.7f), SceneNames.Banco, true);
            Doorway(parent, new Vector2(8.5f, 5.7f), SceneNames.Tienda, true);
            Doorway(parent, new Vector2(12.5f, -24.3f), SceneNames.Soda, true);

            currentLayer = LayerFloor;
            BuildStation(parent, "Station_Garage", StationType.Garage, "Taller de Don Rafa", new Vector2(19.5f, 4.9f));
            BuildStation(parent, "Station_Grocery", StationType.Grocery, "Pulperia", new Vector2(-19.5f, -9.0f));
            BuildStation(parent, "Station_Lender", StationType.Lender, "Prestamos Rapidos", new Vector2(-14f, -3.9f));
            BuildStation(parent, "Station_Pyramid", StationType.Pyramid, "Inversiones Seguras", new Vector2(7f, -6.9f));
            Doorway(parent, new Vector2(-31.5f, 5.7f), SceneNames.Cooperativa, true);
            BuildSpawnPoint(parent, SpawnIds.TownFromCooperativa, new Vector2(-31.5f, 4.3f));
            BuildStation(parent, "Station_Courses", StationType.Courses, "Ver los cursos", new Vector2(-27.0f, -9.0f));
            currentLayer = LayerWorld;
        }

        private static void Doorway(Transform parent, Vector2 position, string targetScene, bool closesAtNight = false)
        {
            var go = new GameObject("Door_" + targetScene);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(1.3f, 1.0f);
            trigger.isTrigger = true;
            var door = go.AddComponent<Door>();
            door.Configure(targetScene, SpawnIds.FromTown, Vector2.up);
            door.SetClosesAtNight(closesAtNight);

            if (closesAtNight)
            {
                var sign = new GameObject("ClosedSign");
                sign.transform.SetParent(go.transform, false);
                sign.transform.localPosition = new Vector2(0f, 1.3f);
                Shape(sign.transform, "SignPlate", roundedSmall, Vector2.zero, new Vector2(1.1f, 0.45f), GamePalette.Hex("#B22222"), 15);
                Shape(sign.transform, "SignBorder", roundedSmall, Vector2.zero, new Vector2(1.18f, 0.53f), GamePalette.Outline, 14);
                Label(sign.transform, "CERRADO", Vector2.zero, GamePalette.White, 0.026f, TextAnchor.MiddleCenter);
                door.AttachClosedSign(sign);
                sign.SetActive(false);
            }
        }

        /// <summary>Colored dots on the ground for flower beds and wildflowers.</summary>
        private static void Flowers(Transform parent, System.Random random, Rect area, int count)
        {
            var colors = new[] { "#FFD23F", "#FF8FB1", "#F1F4FF", "#FF7A5C", "#B28DFF" };
            for (int i = 0; i < count; i++)
            {
                float x = area.xMin + (float)random.NextDouble() * area.width;
                float y = area.yMin + (float)random.NextDouble() * area.height;
                if (OnPavement(x, y, 0.3f) || InsideBuilding(x, y))
                {
                    continue;
                }

                OnGround(parent, "Flower", circle, new Vector2(x, y), Vector2.one * (0.16f + (float)random.NextDouble() * 0.08f),
                    GamePalette.Hex(colors[random.Next(colors.Length)]), TownGroundDetail + 1);
            }
        }

        private static void DashedLine(Transform parent, float y, float xMin, float xMax)
        {
            var yellow = GamePalette.Hex("#F2C12E");
            for (float x = xMin + 1f; x < xMax - 1f; x += 2.6f)
            {
                OnGround(parent, "Dash_Out", roundedSmall, new Vector2(x, y), new Vector2(1.42f, 0.22f), GamePalette.Outline, TownRoadMark);
                OnGround(parent, "Dash", roundedSmall, new Vector2(x, y), new Vector2(1.30f, 0.14f), yellow, TownRoadMark + 1);
            }
        }

        private static void Crosswalk(Transform parent, float x, float yMin, float yMax)
        {
            var white = GamePalette.Hex("#F4F1E8");
            for (float y = yMin + 0.38f; y < yMax - 0.25f; y += 0.72f)
            {
                OnGround(parent, "Stripe_Out", roundedSmall, new Vector2(x, y), new Vector2(1.66f, 0.44f), GamePalette.Outline, TownRoadMark);
                OnGround(parent, "Stripe", roundedSmall, new Vector2(x, y), new Vector2(1.54f, 0.34f), white, TownRoadMark + 1);
            }
        }
    }
}
