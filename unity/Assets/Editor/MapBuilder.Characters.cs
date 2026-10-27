using UnityEngine;
using UnityEngine.Rendering;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    public static partial class MapBuilder
    {
        private enum Accessory
        {
            None,
            Cap,
            Hood,
            Helmet,
            Tie,
            Vest
        }

        /// <summary>The colors that tell one cartoon character from another.</summary>
        private struct Look
        {
            public string Skin;
            public Accessory Extra;
            public string ExtraColor;
        }

        private static readonly Look PlayerLook = new Look { Skin = "#FF8A3D", Extra = Accessory.None };

        /// <summary>
        /// The player: a Rigidbody2D with a round collider, the keyboard controller, and the
        /// bean-shaped visual. In the town it is Y sorted so it can walk behind trees and buildings.
        /// </summary>
        private static Transform BuildPlayer(Vector3 position, bool ySorted)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = position;

            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;
            collider.offset = new Vector2(0f, -0.1f);

            player.AddComponent<PlayerSpawner>();
            if (ySorted)
            {
                MakeYSorted(player, -0.45f);
            }

            // A warm pool of light that travels with the player.
            Glow(player.transform, "Glow", Vector2.zero, 4.2f, "#FFE2A8", 0.16f, OrderPlayer - 3);

            var visual = BuildCharacterVisual(player.transform, PlayerLook);
            player.AddComponent<PlayerController>().SetVisual(visual);
            return player.transform;
        }

        /// <summary>
        /// A character that stands in place, such as a vendor. It blocks the player and
        /// breathes. Pass ySorted in the town so the player can pass in front of or behind it.
        /// </summary>
        private static Transform BuildNpc(Transform parent, string name, Vector2 position, Look look, bool ySorted)
        {
            var npc = new GameObject(name);
            npc.transform.SetParent(parent, false);
            npc.transform.position = position;

            var collider = npc.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;
            collider.offset = new Vector2(0f, -0.1f);

            if (ySorted)
            {
                MakeYSorted(npc, -0.45f, false);
            }

            BuildCharacterVisual(npc.transform, look);
            return npc.transform;
        }

        /// <summary>A pedestrian that walks between two points. It has no collider, so it never blocks the way.</summary>
        private static Transform BuildPedestrian(Transform parent, string name, Vector2 from, Vector2 to, Look look, float speed)
        {
            var walker = new GameObject(name);
            walker.transform.SetParent(parent, false);
            walker.transform.position = from;

            MakeYSorted(walker, -0.45f);
            BuildCharacterVisual(walker.transform, look);
            walker.AddComponent<Patrol>().Configure(from, to, speed, 1.6f);
            return walker.transform;
        }

        /// <summary>Groups the character's renderers and sorts them by the height of their feet.</summary>
        private static void MakeYSorted(GameObject go, float footOffset, bool moving = true)
        {
            var group = go.AddComponent<SortingGroup>();
            group.sortingLayerName = LayerWorld;
            group.sortingOrder = YSort.OrderFor(go.transform.position.y + footOffset);

            if (moving)
            {
                go.AddComponent<YSort>().SetFootOffset(footOffset);
            }
        }

        /// <summary>
        /// Shadow, then Bob (the walk cycle) holding Visual (the left and right flip) with the
        /// body, accessory and eyes. Returns Visual.
        /// </summary>
        private static Transform BuildCharacterVisual(Transform root, Look look)
        {
            var shadowColor = GamePalette.Outline;
            shadowColor.a = 0.45f;
            Shape(root, "Shadow", softCircle, new Vector2(0f, -0.5f), new Vector2(1.1f, 0.4f), shadowColor, OrderPlayer - 1);

            var bob = new GameObject("Bob").transform;
            bob.SetParent(root, false);
            bob.gameObject.AddComponent<PlayerAnimator>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(bob, false);

            var skin = GamePalette.Hex(look.Skin);
            Shape(visual, "Body_Outline", rounded, Vector2.zero, new Vector2(0.96f, 1.16f), GamePalette.Outline, OrderPlayer);
            Shape(visual, "Body", rounded, Vector2.zero, new Vector2(0.8f, 1.0f), skin, OrderPlayer + 1);
            Shape(visual, "Belly", roundedSmall, new Vector2(0f, -0.28f), new Vector2(0.5f, 0.28f), GamePalette.Shade(skin, 1.15f), OrderPlayer + 2);

            DrawAccessory(visual, look);

            // Two big cartoon eyes looking toward the walking direction (right when unflipped).
            Eye(visual, new Vector2(0.0f, 0.22f));
            Eye(visual, new Vector2(0.3f, 0.22f));
            return visual;
        }

        private static void DrawAccessory(Transform visual, Look look)
        {
            var color = look.Extra == Accessory.None ? Color.white : GamePalette.Hex(look.ExtraColor);
            var dark = GamePalette.Shade(color, 0.6f);

            switch (look.Extra)
            {
                case Accessory.Cap:
                    Shape(visual, "Cap_Outline", roundedSmall, new Vector2(0.02f, 0.52f), new Vector2(0.9f, 0.34f), GamePalette.Outline, OrderPlayer + 3);
                    Shape(visual, "Cap", roundedSmall, new Vector2(0.02f, 0.52f), new Vector2(0.78f, 0.24f), color, OrderPlayer + 4);
                    Shape(visual, "Cap_Brim", roundedSmall, new Vector2(0.38f, 0.45f), new Vector2(0.34f, 0.12f), dark, OrderPlayer + 4);
                    break;
                case Accessory.Hood:
                    Shape(visual, "Hood_Outline", rounded, new Vector2(0f, 0.28f), new Vector2(1.0f, 0.62f), GamePalette.Outline, OrderPlayer + 3);
                    Shape(visual, "Hood", rounded, new Vector2(0f, 0.28f), new Vector2(0.88f, 0.5f), color, OrderPlayer + 4);
                    break;
                case Accessory.Helmet:
                    Shape(visual, "Helmet_Outline", roundedSmall, new Vector2(0f, 0.5f), new Vector2(0.9f, 0.4f), GamePalette.Outline, OrderPlayer + 3);
                    Shape(visual, "Helmet", roundedSmall, new Vector2(0f, 0.5f), new Vector2(0.78f, 0.28f), color, OrderPlayer + 4);
                    break;
                case Accessory.Tie:
                    Shape(visual, "Tie_Outline", roundedSmall, new Vector2(-0.22f, -0.22f), new Vector2(0.22f, 0.34f), GamePalette.Outline, OrderPlayer + 3);
                    Shape(visual, "Tie", roundedSmall, new Vector2(-0.22f, -0.22f), new Vector2(0.14f, 0.26f), color, OrderPlayer + 4);
                    break;
                case Accessory.Vest:
                    Shape(visual, "Vest", roundedSmall, new Vector2(0f, -0.2f), new Vector2(0.8f, 0.5f), color, OrderPlayer + 3);
                    break;
            }
        }

        private static void Eye(Transform parent, Vector2 center)
        {
            Shape(parent, "Eye_Outline", circle, center, Vector2.one * 0.3f, GamePalette.Outline, OrderPlayer + 5);
            Shape(parent, "Eye", circle, center, Vector2.one * 0.24f, GamePalette.White, OrderPlayer + 6);
            Shape(parent, "Pupil", circle, center + new Vector2(0.04f, 0f), Vector2.one * 0.11f, GamePalette.Outline, OrderPlayer + 7);
        }
    }
}
