using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// Generates the anti-aliased base shapes and high-contrast 2D cel-shaded textures and sprites
    /// used across the overworld and interiors.
    /// </summary>
    public static class SpriteFactory
    {
        public const string Folder = "Assets/Art/Sprites";
        public const string RoundedRectPath = Folder + "/RoundedRect.png";
        public const string RoundedRectSmallPath = Folder + "/RoundedRectSmall.png";
        public const string CirclePath = Folder + "/Circle.png";
        public const string RingPath = Folder + "/Ring.png";
        public const string SoftCirclePath = Folder + "/SoftCircle.png";
        public const string VignettePath = Folder + "/Vignette.png";
        public const string SquarePath = Folder + "/Square.png";
        public const string NoisePath = Folder + "/Noise.png";
        public const string TrianglePath = Folder + "/Triangle.png";

        // 2D Cel-Shaded Texture & Prop Sprites
        public const string RoofTileTexturePath = Folder + "/RoofTileTexture.png";
        public const string RoofEaveTexturePath = Folder + "/RoofEaveTexture.png";
        public const string CobblestoneTexturePath = Folder + "/CobblestoneTexture.png";
        public const string SidewalkSlabTexturePath = Folder + "/SidewalkSlabTexture.png";
        public const string GrassEdgeTexturePath = Folder + "/GrassEdgeTexture.png";
        public const string PlazaFlagstoneTexturePath = Folder + "/PlazaFlagstoneTexture.png";
        public const string BrickWainscotTexturePath = Folder + "/BrickWainscotTexture.png";
        public const string WoodPlankTexturePath = Folder + "/WoodPlankTexture.png";
        public const string TreeCanopyPath = Folder + "/TreeCanopy.png";
        public const string PalmFrondPath = Folder + "/PalmFrond.png";
        public const string BeachUmbrellaPath = Folder + "/BeachUmbrella.png";
        public const string FacadeBrickTexturePath = Folder + "/FacadeBrickTexture.png";
        public const string GrassDetailTexturePath = Folder + "/GrassDetailTexture.png";
        public const string WaterCausticTexturePath = Folder + "/WaterCausticTexture.png";

        // Phase 4 - Building Materials & Weathering Textures
        public const string StuccoTexturePath = Folder + "/StuccoTexture.png";
        public const string ConcreteBlockTexturePath = Folder + "/ConcreteBlockTexture.png";
        public const string WoodSidingTexturePath = Folder + "/WoodSidingTexture.png";
        public const string ZincWallTexturePath = Folder + "/ZincWallTexture.png";
        public const string TileWhiteTexturePath = Folder + "/TileWhiteTexture.png";
        public const string ZincRoofTexturePath = Folder + "/ZincRoofTexture.png";
        public const string ClayRoofTexturePath = Folder + "/ClayRoofTexture.png";
        public const string ConcreteRoofTexturePath = Folder + "/ConcreteRoofTexture.png";
        public const string GrimeTexturePath = Folder + "/GrimeTexture.png";

        // Phase 5 - Interior Floor & Wall Textures
        public const string FloorCeramicTexturePath = Folder + "/FloorCeramicTexture.png";
        public const string FloorTerrazzoTexturePath = Folder + "/FloorTerrazzoTexture.png";
        public const string FloorMosaicTexturePath = Folder + "/FloorMosaicTexture.png";
        public const string FloorWoodTexturePath = Folder + "/FloorWoodTexture.png";
        public const string FloorCementTexturePath = Folder + "/FloorCementTexture.png";
        public const string WallPaintTexturePath = Folder + "/WallPaintTexture.png";

        [MenuItem("Colon Quest/Art/Generate Sprites")]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);

            // Rounded rectangles are 9-sliced: the corner radius stays fixed at any size.
            Write(RoundedRectPath, 64, 64, 20, (x, y) => RoundedRectAlpha(x, y, 64, 20));
            Write(RoundedRectSmallPath, 32, 64, 8, (x, y) => RoundedRectAlpha(x, y, 32, 8));
            Write(CirclePath, 128, 128, 0, (x, y) => CircleAlpha(x, y, 128));
            Write(RingPath, 128, 128, 0, (x, y) => RingAlpha(x, y, 128, 12f));
            WriteColor(TrianglePath, 256, 128, 128, 0, TrianglePixel, false);

            // Soft shapes for glows, nebulae and the screen vignette: alpha fades smoothly.
            Write(SoftCirclePath, 256, 256, 0, (x, y) => SoftCircleAlpha(x, y, 256));
            Write(VignettePath, 256, 256, 0, (x, y) => VignetteAlpha(x, y, 256));

            // A plain 1 by 1 unit square for thin lines such as floor tile joints.
            Write(SquarePath, 4, 4, 0, (x, y) => 1f);

            // Fine grain that tiles seamlessly, to give flat colors a little texture.
            Write(NoisePath, 128, 32, 0, (x, y) => GrainAlpha((int)x, (int)y), true);

            // 2D Hand-Inked Cel-Shaded Sprites & Seamless Textures
            WriteColor(RoofTileTexturePath, 256, 256, 64, 0, RoofTilePixel, true);
            WriteColor(RoofEaveTexturePath, 256, 64, 64, 0, RoofEavePixel, true);
            WriteColor(CobblestoneTexturePath, 256, 256, 64, 0, CobblestonePixel, true);
            WriteColor(SidewalkSlabTexturePath, 256, 128, 64, 0, SidewalkSlabPixel, true);
            WriteColor(GrassEdgeTexturePath, 256, 64, 64, 0, GrassEdgePixel, true);
            WriteColor(PlazaFlagstoneTexturePath, 256, 256, 64, 0, PlazaFlagstonePixel, true);
            WriteColor(BrickWainscotTexturePath, 256, 128, 64, 0, BrickWainscotPixel, true);
            WriteColor(WoodPlankTexturePath, 256, 256, 64, 0, WoodPlankPixel, true);
            WriteColor(FacadeBrickTexturePath, 128, 128, 64, 0, FacadeBrickPixel, true);
            WriteColor(GrassDetailTexturePath, 256, 256, 32, 0, GrassDetailPixel, true);
            WriteColor(WaterCausticTexturePath, 128, 128, 64, 0, WaterCausticPixel, true);
            WriteColor(TreeCanopyPath, 256, 256, 192, 0, TreeCanopyPixel, false);
            WriteColor(PalmFrondPath, 256, 128, 192, 0, PalmFrondPixel, false);
            WriteColor(BeachUmbrellaPath, 256, 256, 192, 0, BeachUmbrellaPixel, false);

            // Phase 4 - Building Materials & Weathering Textures
            WriteColor(StuccoTexturePath, 128, 128, 64, 0, StuccoPixel, true);
            WriteColor(ConcreteBlockTexturePath, 128, 128, 64, 0, ConcreteBlockPixel, true);
            WriteColor(WoodSidingTexturePath, 128, 128, 64, 0, WoodSidingPixel, true);
            WriteColor(ZincWallTexturePath, 128, 128, 64, 0, ZincWallPixel, true);
            WriteColor(TileWhiteTexturePath, 128, 128, 64, 0, TileWhitePixel, true);
            WriteColor(ZincRoofTexturePath, 256, 256, 64, 0, ZincRoofPixel, true);
            WriteColor(ClayRoofTexturePath, 256, 256, 64, 0, ClayRoofPixel, true);
            WriteColor(ConcreteRoofTexturePath, 128, 128, 64, 0, ConcreteRoofPixel, true);
            WriteColor(GrimeTexturePath, 128, 128, 64, 0, GrimePixel, true);

            // Phase 5 - Interior Floor & Wall Textures
            WriteColor(FloorCeramicTexturePath, 128, 128, 64, 0, FloorCeramicPixel, true);
            WriteColor(FloorTerrazzoTexturePath, 128, 128, 64, 0, FloorTerrazzoPixel, true);
            WriteColor(FloorMosaicTexturePath, 128, 128, 64, 0, FloorMosaicPixel, true);
            WriteColor(FloorWoodTexturePath, 128, 128, 64, 0, FloorWoodPixel, true);
            WriteColor(FloorCementTexturePath, 128, 128, 64, 0, FloorCementPixel, true);
            WriteColor(WallPaintTexturePath, 128, 128, 64, 0, WallPaintPixel, true);

            AssetDatabase.Refresh();
        }

        public static void EnsureGenerated()
        {
            if (!File.Exists(RoundedRectPath) || !File.Exists(RoundedRectSmallPath)
                || !File.Exists(CirclePath) || !File.Exists(RingPath) || !File.Exists(TrianglePath)
                || !File.Exists(SoftCirclePath) || !File.Exists(VignettePath) || !File.Exists(SquarePath)
                || !File.Exists(NoisePath)
                || !File.Exists(RoofTileTexturePath) || !File.Exists(RoofEaveTexturePath)
                || !File.Exists(CobblestoneTexturePath) || !File.Exists(SidewalkSlabTexturePath)
                || !File.Exists(GrassEdgeTexturePath) || !File.Exists(PlazaFlagstoneTexturePath)
                || !File.Exists(BrickWainscotTexturePath) || !File.Exists(WoodPlankTexturePath)
                || !File.Exists(TreeCanopyPath) || !File.Exists(PalmFrondPath)
                || !File.Exists(BeachUmbrellaPath) || !File.Exists(FacadeBrickTexturePath)
                || !File.Exists(GrassDetailTexturePath) || !File.Exists(WaterCausticTexturePath)
                || !File.Exists(StuccoTexturePath) || !File.Exists(ConcreteBlockTexturePath)
                || !File.Exists(WoodSidingTexturePath) || !File.Exists(ZincWallTexturePath)
                || !File.Exists(TileWhiteTexturePath) || !File.Exists(ZincRoofTexturePath)
                || !File.Exists(ClayRoofTexturePath) || !File.Exists(ConcreteRoofTexturePath)
                || !File.Exists(GrimeTexturePath) || !File.Exists(FloorCeramicTexturePath)
                || !File.Exists(FloorTerrazzoTexturePath) || !File.Exists(FloorMosaicTexturePath)
                || !File.Exists(FloorWoodTexturePath) || !File.Exists(FloorCementTexturePath)
                || !File.Exists(WallPaintTexturePath))
            {
                Generate();
            }
        }

        /// <summary>Anti-aliased isosceles triangle (256x128) for building gables and classical pediments.</summary>
        private static Color TrianglePixel(float x, float y)
        {
            float w = 256f;
            float h = 128f;
            float nx = Mathf.Abs((x - w * 0.5f) / (w * 0.5f)); // 0 at center, 1 at left/right base corners
            float ny = y / h;                                  // 0 at base, 1 at apex

            if (ny < 0.02f)
            {
                float bottomFade = Mathf.Clamp01(ny / 0.02f);
                return new Color(1f, 1f, 1f, bottomFade);
            }

            // Slope line: nx + ny = 0.96
            float edgeDist = (0.96f - (nx + ny * 0.92f)) * 96f;
            float alpha = Mathf.Clamp01(edgeDist);
            return new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>
        /// Bold hand-inked colonial clay barrel roof tiles (256x256).
        /// Uses dark ink outlines (#1C1412), deep trough shadows, and bright ridge highlights so tinting produces vivid 3-tone cel shading.
        /// </summary>
        private static Color RoofTilePixel(float x, float y)
        {
            float tileW = 32f;
            float tileH = 42.6667f; // 6 rows in 256px
            int row = Mathf.FloorToInt(y / tileH);
            int col = Mathf.FloorToInt(x / tileW);
            uint hash = (uint)(col * 73856093 ^ row * 19349663);

            float lx = x % tileW;
            float ly = y % tileH;

            float nx = (lx / tileW) * 2f - 1f; // -1..1 across one barrel tile
            float ny = ly / tileH;             // 0..1 from bottom overlap to top of tile

            // Curved scalloped bottom lip of each barrel tile
            float arch = (1f - nx * nx) * 0.24f;
            float lipDist = ny - arch;

            // Bold dark ink line at the curved bottom overlap of each tile row
            if (lipDist >= -0.05f && lipDist < 0.09f)
            {
                return new Color(0.14f, 0.10f, 0.10f, 1f);
            }

            // Bold dark ink line along the vertical channels between barrel tiles
            if (Mathf.Abs(nx) > 0.87f)
            {
                return new Color(0.15f, 0.11f, 0.11f, 1f);
            }

            // Subtle per-tile clay variation (0.76..0.90)
            float tileVar = 0.78f + ((hash & 15u) / 15f) * 0.12f;

            // Deep cast shadow directly under the tile row above (ny > 0.82) or below the arch lip
            if (lipDist < -0.05f || ny > 0.86f)
            {
                float s = tileVar * 0.68f;
                return new Color(s, s * 0.94f, s * 0.90f, 1f);
            }

            // Concave valley shadow on the right side of each barrel tile
            if (nx > 0.42f)
            {
                float s = tileVar * 0.78f;
                return new Color(s, s * 0.96f, s * 0.92f, 1f);
            }

            // Bright cel-shaded sunlit highlight strip along the left-center crown of each barrel tile
            if (nx > -0.52f && nx < 0.05f && lipDist > 0.14f && ny < 0.80f)
            {
                float hi = Mathf.Min(1f, tileVar + 0.20f);
                return new Color(hi, hi, hi * 0.96f, 1f);
            }

            // Occasional hand-drawn crack line on a tile
            if ((hash & 31u) == 0u && Mathf.Abs(nx - (ny - 0.5f) * 0.7f) < 0.05f && ny > 0.3f && ny < 0.75f)
            {
                return new Color(0.22f, 0.16f, 0.15f, 1f);
            }

            return new Color(tileVar, tileVar, tileVar, 1f);
        }

        /// <summary>Scalloped bottom roof eave trim (256x64) with curved tile arches and dark under-eave shadow.</summary>
        private static Color RoofEavePixel(float x, float y)
        {
            float tileW = 32f;
            float lx = x % tileW;
            float nx = (lx / tileW) * 2f - 1f;
            float ny = y / 64f; // 0 = bottom of cast shadow, 1 = top of eave

            float archY = 0.34f + (1f - nx * nx) * 0.28f;
            if (ny < archY - 0.22f)
            {
                return Color.clear;
            }

            if (ny < archY - 0.06f)
            {
                // Soft dark cast shadow on the wall directly under the scalloped tiles
                return new Color(0.08f, 0.06f, 0.08f, 0.42f);
            }

            if (ny < archY + 0.08f || Mathf.Abs(nx) > 0.87f || ny > 0.90f)
            {
                // Crisp dark ink outline of the eave arch
                return new Color(0.14f, 0.10f, 0.10f, 1f);
            }

            // Dark hollow inside the barrel arch vs lit clay rim
            if (ny < archY + 0.22f)
            {
                return new Color(0.55f, 0.50f, 0.48f, 1f);
            }

            float shade = nx < 0.15f ? 0.96f : 0.76f;
            return new Color(shade, shade, shade, 1f);
        }

        /// <summary>
        /// Weathered warm cream/sandstone concrete sidewalk slabs (256x128, full-color).
        /// Matches the concept art: large cream slabs, dark ink expansion joints, diagonal corner cracks,
        /// stone chips, and green grass tufts sprouting in the joints.
        /// </summary>
        private static Color SidewalkSlabPixel(float x, float y)
        {
            float slabW = 128f; // 2 large slabs per 256px (each 2 world units wide)
            int slabIdx = Mathf.FloorToInt(x / slabW);
            float lx = x % slabW;
            float ly = y; // 0..128 (2 world units tall)

            // Green grass tufts sprouting from the bottom of the expansion joints
            float distToJoint = Mathf.Min(lx, slabW - lx);
            if (ly < 28f && distToJoint < 14f)
            {
                float gx = (lx > slabW * 0.5f) ? (lx - slabW) : lx;
                float blade1 = Mathf.Abs(gx - (ly - 4f) * 0.32f);
                float blade2 = Mathf.Abs(gx + (ly - 4f) * 0.28f);
                float blade3 = Mathf.Abs(gx * 1.2f);
                float maxH = 22f - Mathf.Abs(gx) * 1.1f;
                if (ly > 3f && ly < maxH && (blade1 < 3.2f || blade2 < 3.2f || blade3 < 2.8f))
                {
                    if (blade1 > 1.8f && blade2 > 1.8f && blade3 > 1.6f)
                    {
                        return new Color(0.12f, 0.16f, 0.10f, 1f); // Ink outline of grass tuft
                    }
                    return ly > 12f ? new Color(0.42f, 0.78f, 0.28f, 1f) : new Color(0.26f, 0.58f, 0.20f, 1f);
                }
            }

            // Small grass tuft along the bottom curb seam around x = 58
            if (slabIdx == 0 && ly < 20f && Mathf.Abs(lx - 58f) < 10f)
            {
                float gx = lx - 58f;
                float d = new Vector2(gx * 0.8f, ly - 5f).magnitude;
                if (d < 8.5f && ly > 2f)
                {
                    return d > 6.5f
                        ? new Color(0.12f, 0.16f, 0.10f, 1f)
                        : new Color(0.38f, 0.74f, 0.26f, 1f);
                }
            }

            // Dark ink vertical expansion joint between concrete slabs
            if (distToJoint < 2.2f || ly < 2.5f || ly > 125.5f)
            {
                return new Color(0.20f, 0.18f, 0.16f, 1f);
            }

            // Diagonal hand-drawn cracks inside each concrete slab
            if (slabIdx == 0)
            {
                // Top-left diagonal crack
                if (lx < 42f && ly > 78f)
                {
                    float crackY = 92f + lx * 0.65f + Mathf.Sin(lx * 0.35f) * 2.2f;
                    if (Mathf.Abs(ly - crackY) < 1.3f)
                    {
                        return new Color(0.28f, 0.25f, 0.22f, 1f);
                    }
                }
                // Bottom-right branched crack
                if (lx > 86f && ly < 52f)
                {
                    float crackY = 48f - (lx - 86f) * 0.85f + Mathf.Sin(lx * 0.4f) * 1.8f;
                    if (Mathf.Abs(ly - crackY) < 1.2f)
                    {
                        return new Color(0.30f, 0.27f, 0.23f, 1f);
                    }
                }
            }
            else
            {
                // Crack from top joint downward
                if (lx > 48f && lx < 82f && ly > 82f)
                {
                    float crackX = 64f + (128f - ly) * 0.28f + Mathf.Sin(ly * 0.35f) * 2.0f;
                    if (Mathf.Abs(lx - crackX) < 1.2f)
                    {
                        return new Color(0.28f, 0.25f, 0.22f, 1f);
                    }
                }
            }

            // Base warm sandstone/cream concrete color (#DBD1BD) with subtle grain
            float grain = (GrainAlpha((int)x, (int)y) - 0.5f) * 0.035f;
            Color baseCream = new Color(0.86f + grain, 0.82f + grain, 0.74f + grain, 1f);

            // Sunlit bevel highlight along the top and left edges of each slab
            if (ly > 118f || (lx >= 2.2f && lx < 6.5f))
            {
                return new Color(0.93f, 0.90f, 0.83f, 1f);
            }

            // Shaded bevel along the bottom and right edges of each slab
            if (ly < 10f || lx > slabW - 6.5f)
            {
                return new Color(0.76f, 0.71f, 0.63f, 1f);
            }

            // Weathered stone pits / small pebbles on the concrete surface
            for (int p = 0; p < 5; p++)
            {
                uint ph = (uint)((slabIdx * 13 + p * 37) * 73856093);
                float px = 16f + (ph & 95u);
                float py = 18f + ((ph >> 8) & 88u);
                float d = new Vector2((lx - px) * 0.8f, (ly - py) * 1.2f).magnitude;
                if (d < 4.2f)
                {
                    return new Color(0.74f, 0.69f, 0.60f, 1f);
                }
            }

            return baseCream;
        }

        /// <summary>
        /// Hand-inked scalloped green grass border (256x64, full-color) that overlaps the top edge of sidewalks.
        /// </summary>
        private static Color GrassEdgePixel(float x, float y)
        {
            // y = 64 is top (into the lawn), y = 0 is bottom (overlapping onto the sidewalk)
            float wave1 = Mathf.Sin(x * (Mathf.PI * 2f / 32f)) * 7f;
            float wave2 = Mathf.Sin(x * (Mathf.PI * 2f / 16f) + 0.9f) * 4.5f;
            float bladeTooth = Mathf.Abs(((x * 0.35f) % 2f) - 1f) * 8f;
            float edgeY = 22f + wave1 + wave2 + bladeTooth;

            if (y < edgeY - 4f)
            {
                return Color.clear;
            }

            // Soft drop shadow on the concrete just under the grass blades
            if (y < edgeY)
            {
                return new Color(0.15f, 0.14f, 0.12f, 0.35f);
            }

            // Crisp dark ink outline along the scalloped grass tips
            if (y < edgeY + 3.8f)
            {
                return new Color(0.11f, 0.16f, 0.09f, 1f);
            }

            // Darker green shade near the tips, vibrant sunny lawn green (#67C242) above
            if (y < edgeY + 11f)
            {
                return new Color(0.28f, 0.62f, 0.21f, 1f);
            }

            // Occasional bright blade highlight
            if (y < edgeY + 24f && Mathf.Sin(x * 0.49f) > 0.72f)
            {
                return new Color(0.52f, 0.85f, 0.34f, 1f);
            }

            return new Color(0.404f, 0.761f, 0.259f, 1f); // Matches sunny GrassColor #67C242
        }

        /// <summary>
        /// Colonial irregular terracotta & sandstone flagstones (256x256, full-color).
        /// Multi-toned warm terracotta stones with dark ink mortar lines, bevel highlights, and tiny green moss tufts.
        /// </summary>
        private static Color PlazaFlagstonePixel(float x, float y)
        {
            float rowH = 32f; // 8 rows in 256px
            int row = Mathf.FloorToInt(y / rowH);
            float ly = y % rowH;

            // Vary stone widths per row using a deterministic pattern that tiles cleanly across 256px
            float shift = ((row * 53) % 8) * 32f;
            float sx = (x + shift) % 256f;

            // Partition 256px into 6 irregular flagstones: [0..44, 44..82, 82..130, 130..168, 168..214, 214..256]
            float[] bounds = { 0f, 44f, 82f, 130f, 168f, 214f, 256f };
            int col = 0;
            for (int i = 0; i < 6; i++)
            {
                if (sx >= bounds[i] && sx <= bounds[i + 1])
                {
                    col = i;
                    break;
                }
            }

            float stoneW = bounds[col + 1] - bounds[col];
            float lx = sx - bounds[col];
            uint hash = (uint)(col * 73856093 ^ row * 19349663);

            // Slight organic wobble on the mortar lines
            float wobbleX = Mathf.Sin(y * 0.24f + row) * 1.2f;
            float wobbleY = Mathf.Sin(x * 0.24f + col) * 1.2f;
            float dlx = lx + wobbleX;
            float dly = ly + wobbleY;

            // Tiny green moss tufts at some stone corner intersections
            if ((hash & 7u) == 0u && dlx < 9f && dly < 9f)
            {
                float md = new Vector2(dlx - 2f, dly - 2f).magnitude;
                if (md < 5.5f)
                {
                    return md > 4.0f
                        ? new Color(0.14f, 0.18f, 0.10f, 1f)
                        : new Color(0.36f, 0.72f, 0.26f, 1f);
                }
            }

            // Rounded corner distance to mortar gap
            float halfW = stoneW * 0.5f;
            float halfH = rowH * 0.5f;
            float radius = 4.2f;
            float qx = Mathf.Abs(dlx - halfW) - (halfW - 1.8f - radius);
            float qy = Mathf.Abs(dly - halfH) - (halfH - 1.8f - radius);
            float dist = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;

            if (dist > -1.4f)
            {
                // Crisp dark espresso mortar joint (#2D1B14)
                return new Color(0.18f, 0.11f, 0.08f, 1f);
            }

            // Warm multi-tone colonial terracotta palette
            Color[] palette =
            {
                new Color(0.86f, 0.53f, 0.34f, 1f), // #DC8757 warm terracotta
                new Color(0.79f, 0.44f, 0.26f, 1f), // #C97042 deep clay
                new Color(0.90f, 0.60f, 0.41f, 1f), // #E69969 sunlit ochre clay
                new Color(0.74f, 0.39f, 0.23f, 1f), // #BD633B roasted sienna
                new Color(0.83f, 0.49f, 0.31f, 1f)  // #D47D4F classic adobe
            };
            Color stoneCol = palette[hash % (uint)palette.Length];

            // Cel-shaded top-left highlight bevel vs bottom-right shadow bevel
            if (dly > rowH - 6.5f || dlx < 5.5f)
            {
                stoneCol = Color.Lerp(stoneCol, new Color(0.98f, 0.78f, 0.58f, 1f), 0.32f);
            }
            else if (dly < 6.0f || dlx > stoneW - 5.5f)
            {
                stoneCol = Color.Lerp(stoneCol, new Color(0.45f, 0.22f, 0.14f, 1f), 0.28f);
            }

            return stoneCol;
        }

        /// <summary>
        /// Exposed colonial red-brick wainscot texture (256x128, full-color) with cream mortar and cel-shaded bricks.
        /// </summary>
        private static Color BrickWainscotPixel(float x, float y)
        {
            float brickW = 42.6667f; // 6 bricks across 256px
            float brickH = 21.3333f; // 6 rows across 128px
            int row = Mathf.FloorToInt(y / brickH);
            float ox = (row % 2) * (brickW * 0.5f);
            float lx = (x + ox) % brickW;
            float ly = y % brickH;
            int col = Mathf.FloorToInt((x + ox) / brickW);
            uint hash = (uint)(col * 73856093 ^ row * 19349663);

            // Warm cream-tan mortar joint (#D8C7B0) with thin dark ink shadow under each brick
            if (ly < 2.6f || lx < 2.6f)
            {
                return new Color(0.84f, 0.77f, 0.68f, 1f);
            }

            if (ly < 4.2f || lx > brickW - 2.0f)
            {
                return new Color(0.24f, 0.13f, 0.10f, 1f);
            }

            Color[] bricks =
            {
                new Color(0.76f, 0.34f, 0.24f, 1f), // #C2573D
                new Color(0.68f, 0.28f, 0.19f, 1f), // #AD4730
                new Color(0.82f, 0.41f, 0.29f, 1f), // #D1694A
                new Color(0.62f, 0.25f, 0.17f, 1f)  // #9E402B
            };
            Color c = bricks[hash % (uint)bricks.Length];
            if (ly > brickH - 4.5f)
            {
                c = Color.Lerp(c, new Color(0.95f, 0.65f, 0.50f, 1f), 0.3f);
            }

            return c;
        }

        /// <summary>Hand-laid rounded cobblestone pavers with dark mortar lines and cel-shaded stone bevels.</summary>
        private static Color CobblestonePixel(float x, float y)
        {
            float stoneW = 42.6667f;
            float stoneH = 32f;
            int row = Mathf.FloorToInt(y / stoneH);
            float ox = (row % 2) * (stoneW * 0.5f);
            float lx = (x + ox) % stoneW;
            if (lx < 0f) lx += stoneW;
            float ly = y % stoneH;

            int col = Mathf.FloorToInt((x + ox) / stoneW);
            uint hash = (uint)(col * 73856093 ^ row * 19349663);

            float halfW = stoneW * 0.5f;
            float halfH = stoneH * 0.5f;
            float radius = 6.5f;
            float qx = Mathf.Abs(lx - halfW) - (halfW - 2.4f - radius);
            float qy = Mathf.Abs(ly - halfH) - (halfH - 2.4f - radius);
            float dist = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;

            if (dist > 0f)
            {
                return new Color(0.45f, 0.40f, 0.36f, 0.96f);
            }

            if (dist > -2.4f)
            {
                return new Color(0.18f, 0.15f, 0.16f, 1f);
            }

            float tone = 0.80f + ((hash & 7u) / 7f) * 0.18f;
            if (ly > stoneH * 0.68f && lx < stoneW * 0.72f)
            {
                tone = Mathf.Min(1f, tone + 0.14f);
            }
            else if (ly < stoneH * 0.28f)
            {
                tone -= 0.16f;
            }

            return new Color(tone, tone, tone, 1f);
        }

        /// <summary>Warm wood planks with bold dark ink seam lines, wood grain rings, and nail dots.</summary>
        private static Color WoodPlankPixel(float x, float y)
        {
            float plankH = 32f;
            int row = Mathf.FloorToInt(y / plankH);
            float ly = y % plankH;
            float jointX = ((row * 73 + 31) % 256);
            float dxJoint = Mathf.Min(Mathf.Abs(x - jointX), 256f - Mathf.Abs(x - jointX));

            if (ly < 2.6f || ly > plankH - 1.4f || dxJoint < 1.8f)
            {
                return new Color(0.16f, 0.11f, 0.09f, 0.92f);
            }

            if (dxJoint > 5f && dxJoint < 9f && (Mathf.Abs(ly - 8f) < 2.0f || Mathf.Abs(ly - 24f) < 2.0f))
            {
                return new Color(0.18f, 0.14f, 0.12f, 0.95f);
            }

            float wave = Mathf.Sin((x * 0.09f) + row * 1.7f + Mathf.Sin(y * 0.28f) * 1.6f);
            float grain = wave > 0.60f ? 0.84f : (wave < -0.60f ? 0.98f : 0.91f);
            if (ly > plankH - 5.5f)
            {
                grain = 1.0f;
            }
            else if (ly < 6f)
            {
                grain -= 0.08f;
            }

            return new Color(grain, grain, grain, 1f);
        }

        /// <summary>Warm stucco wall texture with subtle plaster variation and faint crack details.</summary>
        private static Color FacadeBrickPixel(float x, float y)
        {
            float g = 0.97f + (GrainAlpha((int)x, (int)y) - 0.5f) * 0.04f;
            return new Color(g, g, g, 1f);
        }

        /// <summary>
        /// Seamless caustic light for the water (128x128): white wavy lines with low alpha, transparent elsewhere. Every
        /// term repeats a whole number of times across the tile, so the edges meet.
        /// </summary>
        private static Color WaterCausticPixel(float x, float y)
        {
            float u = x / 128f * Mathf.PI * 2f;
            float v = y / 128f * Mathf.PI * 2f;
            float w1 = Mathf.Sin(2f * u + 1.3f * Mathf.Sin(2f * v));
            float w2 = Mathf.Sin(2f * v + 1.3f * Mathf.Sin(3f * u + 1f));
            float ridge = Mathf.Clamp01(1f - Mathf.Abs((w1 + w2) * 0.5f) * 6f);
            return new Color(1f, 1f, 1f, ridge * ridge * 0.35f);
        }

        /// <summary>Seamless lush grass texture (256x256) with hand-inked grass blade tufts and sunny clover patches.</summary>
        private static Color GrassDetailPixel(float x, float y)
        {
            // Sunny chartreuse grass patches
            float patchWave = Mathf.Sin(x * (Mathf.PI * 2f / 256f) * 2f) * Mathf.Cos(y * (Mathf.PI * 2f / 256f) * 2f);

            // Hand-drawn grass blade clusters ("vvv" dark green strokes with bright tips)
            for (int i = 0; i < 18; i++)
            {
                uint h = (uint)(i * 1597334677u + 1013904223u);
                float cx = 16f + (h % 224u);
                float cy = 16f + ((h >> 8) % 224u);
                float dx = x - cx;
                float dy = y - cy;
                if (dy >= -1f && dy <= 11f && Mathf.Abs(dx) <= 9f)
                {
                    float b1 = Mathf.Abs(dx + 4.5f + dy * 0.28f);
                    float b2 = Mathf.Abs(dx - dy * 0.05f);
                    float b3 = Mathf.Abs(dx - 4.5f - dy * 0.28f);
                    float maxH = 10f - Mathf.Abs(dx) * 0.4f;
                    if (dy < maxH && (b1 < 1.35f || b2 < 1.35f || b3 < 1.35f))
                    {
                        return dy > 5.5f
                            ? new Color(0.18f, 0.44f, 0.14f, 0.75f)
                            : new Color(0.12f, 0.32f, 0.10f, 0.85f);
                    }
                }
            }

            if (patchWave > 0.35f)
            {
                return new Color(0.55f, 0.88f, 0.34f, (patchWave - 0.35f) * 0.28f);
            }

            return Color.clear;
        }

        /// <summary>
        /// Hand-inked, multi-layered cel-shaded tree canopy cluster (256x256) with internal scalloped leaf arcs,
        /// dark ink outline, deep shadow pockets, and sunny leaf highlights.
        /// </summary>
        private static Color TreeCanopyPixel(float x, float y)
        {
            float size = 256f;
            Vector2 p = new Vector2((x - size * 0.5f) / (size * 0.5f), (y - size * 0.5f) / (size * 0.5f));

            // 9 puffy leaf lobes forming a rich organic crown silhouette
            Vector2[] centers =
            {
                new Vector2(0f, 0.04f),
                new Vector2(-0.38f, -0.05f),
                new Vector2(0.38f, -0.04f),
                new Vector2(-0.26f, 0.30f),
                new Vector2(0.26f, 0.28f),
                new Vector2(0f, 0.36f),
                new Vector2(-0.22f, -0.28f),
                new Vector2(0.22f, -0.27f),
                new Vector2(0f, -0.32f)
            };
            float[] radii = { 0.56f, 0.46f, 0.46f, 0.44f, 0.44f, 0.42f, 0.42f, 0.42f, 0.40f };

            float minDist = 10f;
            for (int i = 0; i < centers.Length; i++)
            {
                // Add small leaf-edge scallops around each lobe
                Vector2 delta = p - centers[i];
                float ang = Mathf.Atan2(delta.y, delta.x);
                float leafScallop = Mathf.Sin(ang * 7f + i * 1.3f) * 0.022f;
                float d = delta.magnitude - (radii[i] + leafScallop);
                if (d < minDist) minDist = d;
            }

            float pixelScale = size * 0.5f;
            float sd = minDist * pixelScale;
            if (sd > 0.8f)
            {
                return Color.clear;
            }

            float edgeAlpha = Mathf.Clamp01(0.8f - sd);
            if (sd > -6.5f)
            {
                // Bold dark ink outline (#151C14)
                return new Color(0.08f, 0.11f, 0.08f, edgeAlpha);
            }

            // Internal hand-drawn leaf cluster arcs
            Vector2[] innerArcs =
            {
                new Vector2(-0.18f, 0.14f),
                new Vector2(0.18f, 0.10f),
                new Vector2(-0.04f, -0.08f),
                new Vector2(-0.26f, -0.12f),
                new Vector2(0.24f, -0.14f)
            };
            for (int a = 0; a < innerArcs.Length; a++)
            {
                Vector2 dp = p - innerArcs[a];
                float r = dp.magnitude;
                if (Mathf.Abs(r - 0.22f) < 0.018f && dp.y < -0.04f)
                {
                    return new Color(0.10f, 0.18f, 0.10f, edgeAlpha);
                }
            }

            // 4-tone cel-shaded foliage (designed to be tinted with a bright sunny green)
            float lightDot = Vector2.Dot(p, new Vector2(-0.55f, 0.83f));
            float wave = Mathf.Sin(p.x * 14f) * Mathf.Cos(p.y * 14f) * 0.08f;
            float lum = lightDot + wave;

            float shade;
            if (lum > 0.24f)
            {
                shade = 1.0f; // Bright sunlit top-left leaf clusters
            }
            else if (lum > -0.06f)
            {
                shade = 0.86f; // Mid-tone lush green
            }
            else if (lum > -0.28f)
            {
                shade = 0.70f; // Under-cluster cel shadow
            }
            else
            {
                shade = 0.54f; // Deep lower canopy shadow
            }

            return new Color(shade, shade, shade, edgeAlpha);
        }

        /// <summary>Feathered tropical palm/banana frond (256x128) with central rib, serrated leaflet notches, and bold ink outline.</summary>
        private static Color PalmFrondPixel(float x, float y)
        {
            float w = 256f;
            float h = 128f;
            float nx = (x / w) * 2f - 1f;
            float ny = (y / h) * 2f - 1f;

            float arch = (1f - nx * nx) * 0.15f;
            float cy = ny - arch;
            float halfWidth = Mathf.Sin(Mathf.Clamp01((nx + 0.94f) / 1.88f) * Mathf.PI) * 0.70f;

            float notchWave = Mathf.Sin((nx + 0.8f) * 18f);
            float notchCut = notchWave > 0.52f ? (notchWave - 0.52f) * 0.34f : 0f;
            float effectiveHalf = Mathf.Max(0.04f, halfWidth - notchCut);

            float dist = (Mathf.Abs(cy) - effectiveHalf) * (h * 0.5f);
            if (Mathf.Abs(nx) > 0.93f)
            {
                dist = Mathf.Max(dist, (Mathf.Abs(nx) - 0.93f) * (w * 0.5f));
            }

            if (dist > 0.8f)
            {
                return Color.clear;
            }

            float alpha = Mathf.Clamp01(0.8f - dist);
            if (dist > -5.0f)
            {
                return new Color(0.08f, 0.11f, 0.08f, alpha);
            }

            // Bright central spine (rachis)
            if (Mathf.Abs(cy) < 0.065f && nx < 0.84f)
            {
                return new Color(0.95f, 1.0f, 0.75f, alpha);
            }

            float shade = cy > 0f ? 0.98f : 0.78f;
            return new Color(shade, shade, shade, alpha);
        }

        /// <summary>8-panel octagonal beach umbrella (256x256) with rib seams, bold ink outline, and center finial.</summary>
        private static Color BeachUmbrellaPixel(float x, float y)
        {
            float size = 256f;
            Vector2 p = new Vector2((x - size * 0.5f) / (size * 0.5f), (y - size * 0.5f) / (size * 0.5f));
            p.y *= 1.18f;

            float r = p.magnitude;
            float angle = Mathf.Atan2(p.y, p.x);
            float scallop = Mathf.Cos(angle * 8f) * 0.038f;
            float outerRadius = 0.86f + scallop;

            float sd = (r - outerRadius) * (size * 0.5f);
            if (sd > 0.8f)
            {
                return Color.clear;
            }

            float alpha = Mathf.Clamp01(0.8f - sd);
            if (r < 0.09f)
            {
                return r > 0.06f ? new Color(0.08f, 0.09f, 0.15f, alpha) : new Color(0.98f, 0.95f, 0.84f, alpha);
            }

            if (sd > -6.0f)
            {
                return new Color(0.08f, 0.09f, 0.15f, alpha);
            }

            float sectorPos = ((angle + Mathf.PI) / (Mathf.PI * 2f)) * 8f;
            float frac = sectorPos - Mathf.Floor(sectorPos);
            if (frac < 0.045f || frac > 0.955f)
            {
                return new Color(0.08f, 0.09f, 0.15f, alpha * 0.90f);
            }

            float sun = Vector2.Dot(p.normalized, new Vector2(-0.6f, 0.8f));
            float shade = sun > 0.15f ? 1.0f : (sun < -0.25f ? 0.78f : 0.90f);
            return new Color(shade, shade, shade, alpha);
        }

        // ---------------------------------------------------------------- Phase 4: Building Exterior Pixels

        /// <summary>Fine irregular plaster/stucco with two-scale grain and soft weathering stains (128x128, grayscale).</summary>
        private static Color StuccoPixel(float x, float y)
        {
            float n1 = (GrainAlpha((int)x, (int)y) - 0.5f) * 0.08f;
            float n2 = (GrainAlpha((int)(x * 0.25f), (int)(y * 0.25f)) - 0.5f) * 0.05f;
            float u = x * (Mathf.PI * 2f / 128f);
            float v = y * (Mathf.PI * 2f / 128f);
            float patch = Mathf.Sin(2f * u + 1f) * Mathf.Cos(2f * v + 0.5f) * 0.035f;
            float g = Mathf.Clamp01(0.95f + n1 + n2 + patch);
            return new Color(g, g, g, 1f);
        }

        /// <summary>Concrete masonry unit blocks in running bond with recessed mortar joints (128x128, grayscale).</summary>
        private static Color ConcreteBlockPixel(float x, float y)
        {
            float blockW = 128f / 3f;  // ~42.67px wide (3 blocks across 128px)
            float blockH = 128f / 6f;  // ~21.33px tall (6 courses down 128px)
            int row = Mathf.FloorToInt(y / blockH);
            float ox = (row % 2) * (blockW * 0.5f);
            float lx = (x + ox) % blockW;
            if (lx < 0f) lx += blockW;
            float ly = y % blockH;
            int col = Mathf.FloorToInt((x + ox) / blockW);
            uint hash = (uint)(col * 73856093 ^ row * 19349663);

            // 2px recessed mortar joint (25% darker)
            if (lx < 2.0f || ly < 2.0f)
            {
                return new Color(0.66f, 0.66f, 0.68f, 1f);
            }

            float blockVar = 0.86f + ((hash & 7u) / 7f) * 0.07f;
            float grain = (GrainAlpha((int)x, (int)y) - 0.5f) * 0.04f;
            float b = Mathf.Clamp01(blockVar + grain);
            return new Color(b, b, b, 1f);
        }

        /// <summary>Horizontal bevelled wood siding planks with cast seam shadow and fine grain (128x128, grayscale).</summary>
        private static Color WoodSidingPixel(float x, float y)
        {
            float plankH = 16f; // 8 planks across 128px
            int row = Mathf.FloorToInt(y / plankH);
            float ly = y % plankH;

            // Seam shadow under each board (3px)
            if (ly < 1.2f)
            {
                return new Color(0.42f, 0.40f, 0.38f, 1f);
            }
            if (ly < 3.0f)
            {
                return new Color(0.66f, 0.64f, 0.62f, 1f);
            }
            // Top bevel highlight
            if (ly > 14.5f)
            {
                return new Color(0.98f, 0.98f, 0.98f, 1f);
            }

            float wave = Mathf.Sin(x * (Mathf.PI * 2f / 64f) + row * 1.7f);
            float g = 0.89f + wave * 0.035f + (GrainAlpha((int)x, (int)y) - 0.5f) * 0.03f;
            return new Color(g, g, g, 1f);
        }

        /// <summary>Vertical corrugated zinc siding with sinusoidal sheen and occasional rivets (128x128, grayscale).</summary>
        private static Color ZincWallPixel(float x, float y)
        {
            float wave = Mathf.Sin(x * (Mathf.PI * 2f / 16f));
            float tone = 0.85f + wave * 0.12f;

            // Rivet dots on corrugated peaks
            float ly = y % 64f;
            if (wave > 0.92f && Mathf.Abs(ly - 32f) < 2.5f)
            {
                float peakX = (Mathf.Floor(x / 16f) * 16f) + 4f;
                float d = Mathf.Sqrt((x - peakX) * (x - peakX) + (ly - 32f) * (ly - 32f));
                if (d < 1.2f)
                {
                    return new Color(0.98f, 0.98f, 0.98f, 1f);
                }
                if (d < 2.4f)
                {
                    return new Color(0.44f, 0.44f, 0.48f, 1f);
                }
            }

            return new Color(tone, tone, tone, 1f);
        }

        /// <summary>Square ceramic tiles with crisp mortar joints (128x128, grayscale).</summary>
        private static Color TileWhitePixel(float x, float y)
        {
            float lx = x % 32f;
            float ly = y % 32f;
            int col = Mathf.FloorToInt(x / 32f);
            int row = Mathf.FloorToInt(y / 32f);

            if (lx < 2.0f || ly < 2.0f)
            {
                return new Color(0.72f, 0.74f, 0.76f, 1f);
            }
            if (lx < 3.5f || ly > 30.5f)
            {
                return new Color(0.99f, 0.99f, 1.0f, 1f);
            }

            uint hash = (uint)(col * 31 + row * 17);
            float varTile = 0.94f + ((hash & 7u) / 7f) * 0.04f;
            return new Color(varTile, varTile, varTile + 0.01f, 1f);
        }

        /// <summary>Corrugated zinc roofing with diagonal ridges, metallic sheen, and subtle rust stains (256x256).</summary>
        private static Color ZincRoofPixel(float x, float y)
        {
            float u = (x + y * 0.25f) * (Mathf.PI * 2f / 16f);
            float wave = Mathf.Sin(u);
            float baseTone = 0.85f + wave * 0.13f;
            Color col = new Color(baseTone, baseTone, baseTone, 1f);

            // Two subtle rust stain patches
            float dx1 = Mathf.Abs(x - 64f); if (dx1 > 128f) dx1 = 256f - dx1;
            float dy1 = Mathf.Abs(y - 80f); if (dy1 > 128f) dy1 = 256f - dy1;
            float d1 = Mathf.Sqrt(dx1 * dx1 + dy1 * dy1);
            if (d1 < 24f)
            {
                float rust = (1f - d1 / 24f) * 0.32f;
                col = Color.Lerp(col, new Color(0.72f, 0.40f, 0.20f, 1f), rust);
            }

            float dx2 = Mathf.Abs(x - 180f); if (dx2 > 128f) dx2 = 256f - dx2;
            float dy2 = Mathf.Abs(y - 200f); if (dy2 > 128f) dy2 = 256f - dy2;
            float d2 = Mathf.Sqrt(dx2 * dx2 + dy2 * dy2);
            if (d2 < 20f)
            {
                float rust = (1f - d2 / 20f) * 0.28f;
                col = Color.Lerp(col, new Color(0.68f, 0.36f, 0.18f, 1f), rust);
            }

            return col;
        }

        /// <summary>Broad clay roof tiles with organic undulating edges and bevel shadows (256x256, grayscale).</summary>
        private static Color ClayRoofPixel(float x, float y)
        {
            float tileW = 256f / 6f; // ~42.67px wide
            float tileH = 256f / 6f; // ~42.67px tall
            int row = Mathf.FloorToInt(y / tileH);
            float ox = (row % 2) * (tileW * 0.5f);
            float lx = (x + ox) % tileW;
            if (lx < 0f) lx += tileW;
            float ly = y % tileH;
            int col = Mathf.FloorToInt((x + ox) / tileW);
            uint hash = (uint)(col * 73856093 ^ row * 19349663);

            float wobble = Mathf.Sin(y * 0.25f) * 1.5f;
            if (ly < 3.2f || (lx + wobble) < 2.4f)
            {
                return new Color(0.50f, 0.48f, 0.46f, 1f);
            }

            float nx = (lx / tileW) * 2f - 1f;
            float barrel = 1f - nx * nx;
            float tone = 0.78f + barrel * 0.16f + ((hash & 7u) / 7f) * 0.06f;
            return new Color(tone, tone, tone, 1f);
        }

        /// <summary>Monolithic concrete roof slab with expansion joint and faint weathering (128x128, grayscale).</summary>
        private static Color ConcreteRoofPixel(float x, float y)
        {
            float baseTone = 0.83f + (GrainAlpha((int)x, (int)y) - 0.5f) * 0.04f;
            float u = x * (Mathf.PI * 2f / 128f);
            float v = y * (Mathf.PI * 2f / 128f);
            float damp = Mathf.Sin(u * 2f) * Mathf.Cos(v * 2f);
            if (damp > 0.35f)
            {
                baseTone -= (damp - 0.35f) * 0.22f;
            }
            if (Mathf.Abs(x - 64f) < 1.4f || Mathf.Abs(y - 64f) < 1.4f)
            {
                baseTone = 0.56f;
            }
            return new Color(baseTone, baseTone, baseTone, 1f);
        }

        /// <summary>Vertical water and grime streak stains that fade downward (128x128, alpha only).</summary>
        private static Color GrimePixel(float x, float y)
        {
            float ny = y / 128f; // 0 at bottom, 1 at top
            float u = x * (Mathf.PI * 2f / 32f);
            float streak = Mathf.Max(0f, Mathf.Sin(u) * 0.7f + Mathf.Sin(x * (Mathf.PI * 2f / 16f)) * 0.3f);
            float fade = Mathf.Pow(ny, 1.6f);
            float alpha = Mathf.Clamp01(streak * fade * 0.65f + (ny > 0.88f ? (ny - 0.88f) * 2.5f : 0f) * 0.25f);
            return new Color(0.14f, 0.10f, 0.08f, alpha);
        }

        // ---------------------------------------------------------------- Phase 5: Interior Pixels

        /// <summary>Interior residential ceramic tile with mortar joints and sheen variation (128x128, grayscale).</summary>
        private static Color FloorCeramicPixel(float x, float y)
        {
            float lx = x % 32f;
            float ly = y % 32f;
            int col = Mathf.FloorToInt(x / 32f);
            int row = Mathf.FloorToInt(y / 32f);
            uint hash = (uint)(col * 31 + row * 73);

            if (lx < 1.8f || ly < 1.8f)
            {
                return new Color(0.68f, 0.65f, 0.62f, 1f);
            }
            float tone = 0.92f + ((hash & 7u) / 7f) * 0.08f;
            if (lx < 3.2f || ly > 30.2f)
            {
                tone = Mathf.Min(1f, tone + 0.04f);
            }
            return new Color(tone, tone, tone, 1f);
        }

        /// <summary>Commercial terrazzo flooring with fine polished stone aggregate chips (128x128, grayscale).</summary>
        private static Color FloorTerrazzoPixel(float x, float y)
        {
            float baseT = 0.92f + (GrainAlpha((int)x, (int)y) - 0.5f) * 0.03f;
            uint h = (uint)((int)x * 374761393 ^ (int)y * 668265263);
            if ((h % 47u) == 0u)
            {
                return new Color(0.42f, 0.40f, 0.38f, 1f);
            }
            if ((h % 31u) == 0u)
            {
                return new Color(0.68f, 0.65f, 0.60f, 1f);
            }
            if ((h % 59u) == 0u)
            {
                return new Color(0.78f, 0.55f, 0.45f, 1f);
            }
            return new Color(baseT, baseT, baseT, 1f);
        }

        /// <summary>Two-tone clinical checkerboard mosaic tile (128x128, grayscale).</summary>
        private static Color FloorMosaicPixel(float x, float y)
        {
            int col = Mathf.FloorToInt(x / 16f);
            int row = Mathf.FloorToInt(y / 16f);
            float lx = x % 16f;
            float ly = y % 16f;

            if (lx < 1.4f || ly < 1.4f)
            {
                return new Color(0.60f, 0.60f, 0.62f, 1f);
            }
            bool dark = ((col + row) % 2) == 0;
            float t = dark ? 0.82f : 0.95f;
            return new Color(t, t, t, 1f);
        }

        /// <summary>Interior tongue-and-groove hardwood parquet floorboards (128x128, grayscale).</summary>
        private static Color FloorWoodPixel(float x, float y)
        {
            float plankH = 16f;
            float plankW = 64f;
            int row = Mathf.FloorToInt(y / plankH);
            float ox = (row % 2) * (plankW * 0.5f);
            float lx = (x + ox) % plankW;
            if (lx < 0f) lx += plankW;
            float ly = y % plankH;

            if (ly < 1.4f || lx < 1.4f)
            {
                return new Color(0.50f, 0.42f, 0.36f, 1f);
            }
            float grain = 0.88f + Mathf.Sin(x * 0.12f + row * 1.5f) * 0.05f;
            return new Color(grain, grain, grain, 1f);
        }

        /// <summary>Polished smooth cement with subtle burnishing and fine hairline crack (128x128, grayscale).</summary>
        private static Color FloorCementPixel(float x, float y)
        {
            float u = x * (Mathf.PI * 2f / 128f);
            float v = y * (Mathf.PI * 2f / 128f);
            float patch = Mathf.Sin(u * 2f + 0.5f) * Mathf.Cos(v * 2f);
            float tone = 0.85f + patch * 0.06f + (GrainAlpha((int)x, (int)y) - 0.5f) * 0.03f;

            // Hairline floor crack
            if (x > 22f && x < 88f)
            {
                float crackY = 32f + Mathf.Sin(x * 0.14f) * 7f;
                if (Mathf.Abs(y - crackY) < 0.8f)
                {
                    tone = 0.45f;
                }
            }

            return new Color(tone, tone, tone, 1f);
        }

        /// <summary>Fine-grained painted interior drywall / plaster wall (128x128, grayscale).</summary>
        private static Color WallPaintPixel(float x, float y)
        {
            float g = 0.96f + (GrainAlpha((int)x, (int)y) - 0.5f) * 0.03f;
            return new Color(g, g, g, 1f);
        }

        private static float GrainAlpha(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFu) / 65535f;
        }

        private static float SoftCircleAlpha(float x, float y, int size)
        {
            float half = size * 0.5f;
            float fade = Mathf.Clamp01(1f - new Vector2(x - half, y - half).magnitude / half);
            return fade * fade * (3f - 2f * fade);
        }

        private static float VignetteAlpha(float x, float y, int size)
        {
            float nx = x / size * 2f - 1f;
            float ny = y / size * 2f - 1f;
            float distance = Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f;
            float t = Mathf.Clamp01((distance - 0.35f) / 0.65f);
            return t * t * (3f - 2f * t);
        }

        private static float RoundedRectAlpha(float x, float y, int size, float radius)
        {
            float half = size * 0.5f;
            float qx = Mathf.Abs(x - half) - (half - 1f - radius);
            float qy = Mathf.Abs(y - half) - (half - 1f - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            float distance = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            return Mathf.Clamp01(0.5f - distance);
        }

        private static float CircleAlpha(float x, float y, int size)
        {
            float half = size * 0.5f;
            float distance = new Vector2(x - half, y - half).magnitude - (half - 1f);
            return Mathf.Clamp01(0.5f - distance);
        }

        private static float RingAlpha(float x, float y, int size, float thickness)
        {
            float half = size * 0.5f;
            float middle = half - 1f - thickness * 0.5f;
            float distance = Mathf.Abs(new Vector2(x - half, y - half).magnitude - middle) - thickness * 0.5f;
            return Mathf.Clamp01(0.5f - distance);
        }

        private static void Write(string path, int size, int pixelsPerUnit, int border, Func<float, float, float> alphaAt, bool repeat = false)
        {
            WriteColor(path, size, size, pixelsPerUnit, border, (x, y) => new Color(1f, 1f, 1f, alphaAt(x, y)), repeat);
        }

        private static void WriteColor(string path, int width, int height, int pixelsPerUnit, int border, Func<float, float, Color> colorAt, bool repeat = false)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, colorAt(x + 0.5f, y + 0.5f));
                }
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
