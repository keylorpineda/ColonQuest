using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>What the player walks on, which decides how the footsteps sound.</summary>
    public enum SurfaceKind
    {
        Grass,
        Sidewalk,
        Road,
        Sand,
        Wood,
        Tile
    }

    /// <summary>Pure rules for the ground of every scene, so the footsteps can be tested.</summary>
    public static class Surfaces
    {
        private const float BeachStartY = -19f;

        // The streets of the town: the two avenues of the first town, the short street between them and the northern avenue.
        private static readonly Rect[] Roads =
        {
            new Rect(-50f, 0f, 120f, 4f),
            new Rect(-50f, -14f, 120f, 4f),
            new Rect(-2f, -10f, 4f, 10f),
            new Rect(-50f, 26f, 120f, 4f)
        };

        /// <summary>The decks of the four bridges: wood over the river.</summary>
        private static readonly Rect[] Decks =
        {
            new Rect(MapLayout.RiverXMin, 0f, MapLayout.RiverXMax - MapLayout.RiverXMin, 4f),
            new Rect(MapLayout.RiverXMin, -14f, MapLayout.RiverXMax - MapLayout.RiverXMin, 4f),
            new Rect(MapLayout.RiverXMin, 26f, MapLayout.RiverXMax - MapLayout.RiverXMin, 4f),
            new Rect(MapLayout.RiverXMin, -19.15f, MapLayout.RiverXMax - MapLayout.RiverXMin, 1.7f)
        };

        public static SurfaceKind At(string scene, Vector2 position)
        {
            switch (scene)
            {
                case SceneNames.Casa:
                case SceneNames.Cuarto:
                case SceneNames.Vecino:
                case SceneNames.Finca:
                    return SurfaceKind.Wood;
                case SceneNames.Banco:
                case SceneNames.Tienda:
                case SceneNames.Agencia:
                case SceneNames.Carcel:
                case SceneNames.Hospital:
                case SceneNames.HospitalArriba:
                case SceneNames.Inmobiliaria:
                case SceneNames.Soda:
                case SceneNames.Cooperativa:
                    return SurfaceKind.Tile;
                case SceneNames.Town:
                    return Town(position);
                default:
                    return SurfaceKind.Grass;
            }
        }

        private static SurfaceKind Town(Vector2 p)
        {
            foreach (var deck in Decks)
            {
                if (deck.Contains(p))
                {
                    return SurfaceKind.Wood;
                }
            }

            if (p.x >= MapLayout.PierXMin && p.x <= MapLayout.PierXMax && p.y < MapLayout.ShoreY + 1f)
            {
                return SurfaceKind.Wood;
            }

            foreach (var road in Roads)
            {
                if (road.Contains(p))
                {
                    return SurfaceKind.Road;
                }
            }

            if (p.y < BeachStartY)
            {
                return SurfaceKind.Sand;
            }

            foreach (var road in Roads)
            {
                var margin = new Rect(road.xMin, road.yMin - 1.2f, road.width, road.height + 2.4f);
                if (margin.Contains(p))
                {
                    return SurfaceKind.Sidewalk;
                }
            }

            return SurfaceKind.Grass;
        }
    }

    /// <summary>What the town tells the player when they walk where they should not: a thought, and a few steps back.</summary>
    public enum HintKind
    {
        None,
        MapEdge,
        Water
    }

    /// <summary>Which water the player walked into: only the sea is for swimming.</summary>
    public enum WaterBody
    {
        None,
        Sea,
        River,
        Pond
    }

    public struct WorldHint
    {
        public HintKind Kind;
        public WaterBody Water;
        public Vector2 Away;
    }

    public static class WorldHints
    {
        private const float EdgeMargin = 1.1f;
        private const float WaterMargin = 0.9f;

        /// <summary>
        /// The river and the pond have walls the size of the water, so only a player pressed against them hears the
        /// thought: the feet (radius 0.3) plus a little. A wider margin covered the sidewalks along the river.
        /// </summary>
        public const float TouchMargin = 0.45f;
        private static readonly Rect Pond = new Rect(19.5f - 2.7f, -5.9f - 1.2f, 5.4f, 2.4f);

        public static WorldHint Check(string scene, Vector2 p)
        {
            if (scene != SceneNames.Town)
            {
                return new WorldHint();
            }

            if (p.x < MapLayout.PlayWestX + EdgeMargin)
            {
                return new WorldHint { Kind = HintKind.MapEdge, Away = Vector2.right };
            }

            if (p.x > MapLayout.PlayEastX - EdgeMargin)
            {
                return new WorldHint { Kind = HintKind.MapEdge, Away = Vector2.left };
            }

            if (p.y > MapLayout.PlayNorthY - EdgeMargin)
            {
                return new WorldHint { Kind = HintKind.MapEdge, Away = Vector2.down };
            }

            bool onPier = p.x >= MapLayout.PierXMin - 0.3f && p.x <= MapLayout.PierXMax + 0.3f;
            if (p.y < MapLayout.ShoreY + WaterMargin && p.y > MapLayout.ShoreY - 2f && !onPier)
            {
                return new WorldHint { Kind = HintKind.Water, Water = WaterBody.Sea, Away = Vector2.up };
            }

            if (p.x > MapLayout.RiverXMin - TouchMargin && p.x < MapLayout.RiverXMax + TouchMargin && p.y > MapLayout.ShoreY && !OnDeck(p))
            {
                float middle = (MapLayout.RiverXMin + MapLayout.RiverXMax) * 0.5f;
                return new WorldHint { Kind = HintKind.Water, Water = WaterBody.River, Away = p.x < middle ? Vector2.left : Vector2.right };
            }

            var pond = new Rect(Pond.xMin - TouchMargin, Pond.yMin - TouchMargin, Pond.width + TouchMargin * 2f, Pond.height + TouchMargin * 2f);
            if (pond.Contains(p))
            {
                return new WorldHint { Kind = HintKind.Water, Water = WaterBody.Pond, Away = (p - Pond.center).normalized };
            }

            return new WorldHint();
        }

        private static bool OnDeck(Vector2 p)
        {
            // From railing to railing, like the walls of the river: a player on the sidewalk of a bridge is not in the water.
            return Between(p.y, MapLayout.BridgeFirstStreet) || Between(p.y, MapLayout.BridgeSouthStreet) || Between(p.y, MapLayout.BridgeAvenue)
                || (p.y > -19.6f && p.y < -17.0f);
        }

        private static bool Between(float y, Rect deck)
        {
            return y > deck.yMin - TouchMargin && y < deck.yMax + TouchMargin;
        }
    }

    /// <summary>The player's own thoughts: short lines shown as a subtitle, never a window and never an error.</summary>
    public static class Thoughts
    {
        public static event System.Action<string> Said;

        public static void Say(string spanish)
        {
            Said?.Invoke(spanish);
        }
    }
}
