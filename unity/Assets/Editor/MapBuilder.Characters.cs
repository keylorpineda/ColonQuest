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

        private enum HairStyle { Auto, Side, Long, Spiky, Bun, Ponytail, Curly, Afro, Bob, Buzz, Bald }

        private enum Hat { None, Cap, FlatCap, Helmet, Police, Nurse, Beanie, Chonete, Hood }

        private enum Expression { Smile, Warm, Smirk, Frown, Neutral }

        [System.Flags]
        private enum Extras
        {
            None = 0,
            Tie = 1,
            Badge = 2,
            Lanyard = 4,
            Phone = 8,
            Watch = 16,
            Pen = 32,
            Chain = 64,
            Pouch = 128,
            Overall = 256,
            Apron = 512,
            Coat = 1024,
            Stethoscope = 2048,
            Robe = 4096,
            Scarf = 8192,
            Stripe = 16384,
            Backpack = 32768,
            Cardigan = 65536,
            Bag = 131072,
            PoliceBadge = 262144
        }

        /// <summary>
        /// The look of one cartoon person. Skin (the name is kept so the old calls compile) is the shirt color and
        /// every new field is optional: an empty one is filled by Resolve from the old accessory and the color hash.
        /// </summary>
        private struct Look
        {
            public string Skin;
            public Accessory Extra;
            public string ExtraColor;

            public string SkinTone;
            public string HairColor;
            public HairStyle Hair;
            public string Pants;
            public string Shoes;
            public Hat Hat;
            public string HatColor;
            public Expression Face;
            public Extras Wear;
            public string WearColor;
            public string WearColor2;
            public float Width, Height, Head;
            public bool Glasses, Mustache, Goatee, HeavyLids, SquareJaw, Blush, LongSleeve, Skirt;
            public float LookAside;
            public float MouthX;
            public int BrimSide;
        }

        /// <summary>
        /// Fills what a look leaves empty: proportions of 1, the old accessory as a hat or a torso detail, and
        /// skin, hair and pants taken from the color hash, so a pedestrian without a sheet keeps today's colors.
        /// </summary>
        private static Look Resolve(Look look, bool isPlayer)
        {
            int seed = LookSeed(look);
            if (look.SkinTone == null)
            {
                look.SkinTone = SkinTones[isPlayer ? 1 : seed % SkinTones.Length];
            }

            if (look.HairColor == null)
            {
                look.HairColor = HairTones[isPlayer ? 1 : (seed / 7) % HairTones.Length];
            }

            if (look.Pants == null)
            {
                look.Pants = PantsTones[isPlayer ? 0 : (seed / 13) % PantsTones.Length];
            }

            if (look.Hair == HairStyle.Auto)
            {
                HairStyle[] automatic = { HairStyle.Side, HairStyle.Long, HairStyle.Bun, HairStyle.Buzz };
                look.Hair = isPlayer ? HairStyle.Side : automatic[(seed / 3) % automatic.Length];
            }

            if (look.Hat == Hat.None && look.Wear == Extras.None)
            {
                switch (look.Extra)
                {
                    case Accessory.Cap:
                        look.Hat = Hat.Cap;
                        break;
                    case Accessory.Helmet:
                        look.Hat = Hat.Helmet;
                        break;
                    case Accessory.Hood:
                        look.Hat = Hat.Hood;
                        break;
                    case Accessory.Tie:
                        look.Wear = Extras.Tie;
                        break;
                    case Accessory.Vest:
                        look.Wear = Extras.Cardigan;
                        break;
                }
            }

            look.HatColor = look.HatColor ?? look.ExtraColor ?? "#2A2F45";
            look.WearColor = look.WearColor ?? look.ExtraColor ?? "#C0392B";
            look.WearColor2 = look.WearColor2 ?? look.WearColor;
            look.Shoes = look.Shoes ?? "#2A2F45";
            look.Width = look.Width > 0f ? look.Width : 1f;
            look.Height = look.Height > 0f ? look.Height : 1f;
            look.Head = look.Head > 0f ? look.Head : 1f;
            look.BrimSide = look.BrimSide == 0 ? 1 : look.BrimSide;
            return look;
        }

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
            look = ApplyCast(name, look);
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
            look = ApplyCast(name, look);
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
