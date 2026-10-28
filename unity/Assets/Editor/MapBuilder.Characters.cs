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

            var visual = BuildCharacterVisual(player.transform, PlayerLook, false, 0f, true);
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

            BuildCharacterVisual(npc.transform, look, true, 2.6f);
            return npc.transform;
        }

        /// <summary>A pedestrian that walks between two points. It has no collider, so it never blocks the way.</summary>
        private static Transform BuildPedestrian(Transform parent, string name, Vector2 from, Vector2 to, Look look, float speed)
        {
            var walker = new GameObject(name);
            walker.transform.SetParent(parent, false);
            walker.transform.position = from;

            MakeYSorted(walker, -0.45f);
            BuildCharacterVisual(walker.transform, look, true);
            walker.AddComponent<Patrol>().Configure(from, to, speed, 1.6f);
            BuildThing(walker.transform, "Talk", "Saludar", Vector2.zero, new Vector2(0.6f, 0.6f), PasserbyLines(name));
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
        /// Shadow, then Bob (the walk cycle) holding Visual (the left and right flip) with the person:
        /// head, torso, arms and legs. Returns Visual. See MapBuilder.People.cs.
        /// </summary>
        private static Transform BuildCharacterVisual(Transform root, Look look, bool social = false, float talkRadius = 0f, bool isPlayer = false)
        {
            return BuildPersonVisual(root, look, social, talkRadius, isPlayer);
        }
    }
}
