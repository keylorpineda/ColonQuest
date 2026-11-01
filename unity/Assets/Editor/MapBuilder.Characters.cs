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

            /// <summary>What sets apart people who were given the same shirt color: the name of the person.</summary>
            public string Seed;
        }

        /// <summary>
        /// Fills what a look leaves empty: proportions of 1, the old accessory as a hat or a torso detail, and
        /// skin, hair and pants taken from the color hash, so a pedestrian without a sheet keeps today's colors.
        /// </summary>
        private static Look Resolve(Look look, bool isPlayer)
        {
            int seed = LookSeed(look);
            bool extra = !isPlayer && look.SkinTone == null;
            int tone = isPlayer ? 1 : seed % SkinTones.Length;
            if (look.SkinTone == null)
            {
                look.SkinTone = SkinTones[tone];
            }

            if (look.HairColor == null)
            {
                int hairTones = tone >= FirstDarkSkinTone ? 2 : HairTones.Length;
                look.HairColor = HairTones[isPlayer ? 1 : (seed / 7) % hairTones];
            }

            if (look.Pants == null)
            {
                look.Pants = PantsTones[isPlayer ? 0 : (seed / 13) % PantsTones.Length];
            }

            if (look.Hair == HairStyle.Auto)
            {
                HairStyle[] automatic =
                {
                    HairStyle.Side, HairStyle.Long, HairStyle.Bun, HairStyle.Buzz, HairStyle.Curly,
                    HairStyle.Afro, HairStyle.Bob, HairStyle.Ponytail, HairStyle.Spiky, HairStyle.Bald
                };
                look.Hair = isPlayer ? HairStyle.Side : automatic[(seed / 3) % automatic.Length];
            }

            if (extra)
            {
                // The people of the town differ in build and in small things, each one always the same.
                look.Width = look.Width > 0f ? look.Width : 0.92f + (seed / 11) % 5 * 0.04f;
                look.Height = look.Height > 0f ? look.Height : 0.94f + (seed / 17) % 4 * 0.04f;
                look.Glasses |= (seed / 19) % 5 == 0;
                bool longHair = look.Hair == HairStyle.Long || look.Hair == HairStyle.Bun || look.Hair == HairStyle.Ponytail || look.Hair == HairStyle.Bob;
                look.Skirt |= longHair && (seed / 23) % 3 == 0;
                look.Mustache |= !longHair && (seed / 29) % 5 == 0;
                if ((seed / 31) % 3 == 0)
                {
                    look.Face = Expression.Warm;
                }
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
            BuildAlternateLook(player.transform);
            player.AddComponent<PlayerController>().SetVisual(visual);
            player.AddComponent<PlayerLookSwitch>();
            if (ySorted)
            {
                var bob = player.transform.Find("Bob");
                if (bob != null)
                {
                    bob.gameObject.AddComponent<IdleActions>();
                }

                // Out in the town the steps kick up a little sand or dust (indoors the floors stay clean).
                var footDust = player.AddComponent<FootDust>();
                footDust.Configure(softCircle, LayerFloor, 200, -0.45f);
                footDust.ConfigurePrints(circle, LayerFloor, 199);

                // At night a soft light goes with the player, so the way ahead never gets lost in the dark.
                AddNightLight(player.transform, new Vector2(0f, -0.1f), 3f, "#FFE8C0", 0.45f, 0f);
            }

            return player.transform;
        }

        /// <summary>The second look of the player, switched off: <see cref="PlayerLookSwitch"/> keeps the one of the run. One shadow is enough.</summary>
        private static void BuildAlternateLook(Transform player)
        {
            int before = player.childCount;
            BuildCharacterVisual(player, PlayerLookF, false, 0f, true);
            for (int i = player.childCount - 1; i >= before; i--)
            {
                var child = player.GetChild(i);
                if (child.name == "Shadow")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
                else if (child.name == "Bob")
                {
                    child.name = PlayerLookSwitch.AlternateBob;
                    child.gameObject.SetActive(false);
                }
            }
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
            if (NightShiftExits.TryGetValue(name, out var exit))
            {
                npc.AddComponent<NightShift>().Configure(exit);
            }

            AddNpcWorkLoop(npc, name);

            return npc.transform;
        }

        private static void AddNpcWorkLoop(GameObject npc, string name)
        {
            var bob = npc.transform.Find("Bob");
            if (bob == null) return;

            if (name == "Cajera" || name == "Cajero" || name == "Cajero2" || name == "AsesoraCoop" || name == "Financiera")
            {
                bob.gameObject.AddComponent<WorkLoop>().Configure(PersonPose.Type);
            }
            else if (name == "DonaLucia")
            {
                bob.gameObject.AddComponent<WorkLoop>().Configure(PersonPose.Cook, PersonPose.Wash);
            }
            else if (name == "DonRafa" || name == "Mecanico")
            {
                bob.gameObject.AddComponent<WorkLoop>().Configure(PersonPose.Reach);
            }
            else if (name.StartsWith("Vendedor"))
            {
                bob.gameObject.AddComponent<WorkLoop>().Configure(PersonPose.Wave, PersonPose.None, 5f, 9f);
            }
        }

        /// <summary>The people who close their place at night, and which way they walk off: toward the back of it.</summary>
        private static readonly System.Collections.Generic.Dictionary<string, Vector2> NightShiftExits = new System.Collections.Generic.Dictionary<string, Vector2>
        {
            { "DonRafa", new Vector2(0f, 1.4f) },
            { "DonaMarta", new Vector2(0f, 1.4f) },
            { "Promotor", new Vector2(1.4f, 0f) },
            { "Estafador", new Vector2(1.4f, 0f) },
            { "DonaLucia", new Vector2(0f, 1.6f) },
            { "Vendedor", new Vector2(0f, 1.2f) },
            { "Cajero", new Vector2(0f, 1.2f) }
        };

        /// <summary>
        /// The two men of the collectors' car: an agency's shirt and tie, serious faces, and nothing else in common. They wait
        /// hidden off the map, without a collider, and the overlay shows them and walks them to the player when the car stops.
        /// </summary>
        private static void BuildCollectorMen(Transform parent)
        {
            var looks = new[]
            {
                new Look
                {
                    Skin = "#5F7692", SkinTone = "#F0CDB0", HairColor = "#8A5A2B", Hair = HairStyle.Side, Pants = "#3A3F52",
                    Face = Expression.Frown, Width = 1.06f, Height = 1.04f, Glasses = true, Wear = Extras.Tie, WearColor = "#2A3345"
                },
                new Look
                {
                    Skin = "#5F7692", SkinTone = "#B07A52", HairColor = "#7A7A7A", Hair = HairStyle.Bald, Pants = "#3A3F52",
                    Face = Expression.Neutral, Width = 1.12f, Height = 0.98f, Mustache = true, Wear = Extras.Tie, WearColor = "#2A3345"
                }
            };
            for (int i = 0; i < CollectorCar.MenNames.Length; i++)
            {
                var look = looks[i];
                var man = new GameObject(CollectorCar.MenNames[i]);
                man.transform.SetParent(parent, false);
                man.transform.position = new Vector2(MapLayout.PlayWestX - 8f - i, 3.2f);
                MakeYSorted(man, -0.45f);
                BuildCharacterVisual(man.transform, look);
                foreach (var renderer in man.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    renderer.enabled = false;
                }
            }
        }

        /// <summary>A pedestrian that walks between two points. It has no collider, so it never blocks the way.</summary>
        private static Transform BuildPedestrian(Transform parent, string name, Vector2 from, Vector2 to, Look look, float speed, Vector2? home = null)
        {
            look = ApplyCast(name, look);
            var walker = new GameObject(name);
            walker.transform.SetParent(parent, false);
            walker.transform.position = from;

            MakeYSorted(walker, -0.45f);
            BuildCharacterVisual(walker.transform, look, true);
            var patrol = walker.AddComponent<Patrol>();
            if (home.HasValue)
            {
                patrol.Configure(from, to, speed, 1.6f, home.Value);
            }
            else
            {
                patrol.Configure(from, to, speed, 1.6f);
            }
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
