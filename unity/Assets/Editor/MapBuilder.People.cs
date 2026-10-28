using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The people of the game: a cartoon person built from separate vector shapes (head with hair, eyes and
    /// eyelids, mouth, torso, arms and legs), each on its own pivot so PlayerAnimator can move them.
    /// </summary>
    public static partial class MapBuilder
    {
        private static readonly string[] SkinTones = { "#F6D8BC", "#EBBE96", "#D29B6E", "#B07A52", "#80553A" };
        private static readonly string[] HairTones = { "#2A1F1A", "#4A2F20", "#8A5A2B", "#D9A441", "#B0452D", "#555A66" };
        private static readonly string[] PantsTones = { "#2F3E66", "#3A3F52", "#6B5B50", "#1F6B58", "#8E4A5A" };

        /// <summary>The same look always gives the same person, but different looks give different people.</summary>
        private static int LookSeed(Look look)
        {
            int seed = 17;
            foreach (char c in look.Skin ?? string.Empty)
            {
                seed = seed * 31 + c;
            }

            return Mathf.Abs(seed);
        }

        private static Transform BuildPersonVisual(Transform root, Look look, bool social, float talkRadius, bool isPlayer)
        {
            var shadowColor = GamePalette.Outline;
            shadowColor.a = 0.45f;
            Shape(root, "Shadow", softCircle, new Vector2(0f, -0.52f), new Vector2(0.95f, 0.34f), shadowColor, OrderPlayer - 1);

            var bob = new GameObject("Bob").transform;
            bob.SetParent(root, false);
            var animator = bob.gameObject.AddComponent<PlayerAnimator>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(bob, false);

            int seed = LookSeed(look);
            var skin = GamePalette.Hex(SkinTones[isPlayer ? 1 : seed % SkinTones.Length]);
            var hair = GamePalette.Hex(HairTones[isPlayer ? 1 : (seed / 7) % HairTones.Length]);
            var pants = GamePalette.Hex(PantsTones[isPlayer ? 0 : (seed / 13) % PantsTones.Length]);
            var shirt = GamePalette.Hex(look.Skin);
            int hairStyle = isPlayer ? 0 : (seed / 3) % 4;

            var rig = new PersonRig();

            if (hairStyle == 1)
            {
                // Long hair falls behind the head and shoulders.
                Part2(visual, "HairLong", rounded, new Vector2(0f, 0.36f), new Vector2(0.6f, 0.66f), hair, OrderPlayer);
            }

            rig.LegL = Leg(visual, "LegL", -0.1f, pants);
            rig.LegR = Leg(visual, "LegR", 0.1f, pants);

            rig.ArmL = Arm(visual, "ArmL", -0.3f, shirt, skin);
            rig.ArmR = Arm(visual, "ArmR", 0.3f, shirt, skin);

            var torso = new GameObject("Torso").transform;
            torso.SetParent(visual, false);
            torso.localPosition = new Vector3(0f, -0.02f, 0f);
            Part2(torso, "Torso", rounded, Vector2.zero, new Vector2(0.52f, 0.52f), shirt, OrderPlayer + 6);
            Shape(torso, "Collar", roundedSmall, new Vector2(0f, 0.22f), new Vector2(0.2f, 0.07f), GamePalette.Shade(shirt, 0.8f), OrderPlayer + 9);
            DrawTorsoAccessory(torso, look);
            rig.Torso = torso;

            var head = new GameObject("Head").transform;
            head.SetParent(visual, false);
            head.localPosition = new Vector3(0f, 0.27f, 0f);
            rig.Head = head;
            BuildHead(head, look, skin, hair, hairStyle, rig);

            animator.Bind(rig, social, talkRadius);
            return visual;
        }

        /// <summary>A leg hangs from a pivot at the hip, so rotating it swings the whole leg.</summary>
        private static Transform Leg(Transform parent, string name, float x, Color pants)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(x, -0.17f, 0f);
            Part2(pivot, "Leg", roundedSmall, new Vector2(0f, -0.17f), new Vector2(0.17f, 0.36f), pants, OrderPlayer + 1);
            Part2(pivot, "Shoe", roundedSmall, new Vector2(0.02f, -0.36f), new Vector2(0.23f, 0.11f), GamePalette.Hex("#2A2F45"), OrderPlayer + 3);
            return pivot;
        }

        /// <summary>An arm hangs from a pivot at the shoulder: a sleeve, then the hand.</summary>
        private static Transform Arm(Transform parent, string name, float x, Color shirt, Color skin)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(x, 0.17f, 0f);
            Part2(pivot, "Sleeve", roundedSmall, new Vector2(0f, -0.15f), new Vector2(0.14f, 0.34f), shirt, OrderPlayer + 4);
            Part2(pivot, "Hand", circle, new Vector2(0f, -0.35f), new Vector2(0.15f, 0.15f), skin, OrderPlayer + 4);
            return pivot;
        }

        private static void BuildHead(Transform head, Look look, Color skin, Color hair, int hairStyle, PersonRig rig)
        {
            // Hood goes behind the head.
            if (look.Extra == Accessory.Hood)
            {
                Part2(head, "Hood", rounded, new Vector2(0f, 0.2f), new Vector2(0.62f, 0.56f), GamePalette.Hex(look.ExtraColor), OrderPlayer + 10);
            }

            Part2(head, "EarL", circle, new Vector2(-0.23f, 0.18f), new Vector2(0.1f, 0.13f), skin, OrderPlayer + 11);
            Part2(head, "EarR", circle, new Vector2(0.23f, 0.18f), new Vector2(0.1f, 0.13f), skin, OrderPlayer + 11);
            Part2(head, "Face", circle, new Vector2(0f, 0.2f), new Vector2(0.47f, 0.47f), skin, OrderPlayer + 13);

            var blush = GamePalette.Hex("#FF7A8A");
            blush.a = 0.4f;
            Shape(head, "BlushL", circle, new Vector2(-0.14f, 0.1f), new Vector2(0.1f, 0.06f), blush, OrderPlayer + 16);
            Shape(head, "BlushR", circle, new Vector2(0.14f, 0.1f), new Vector2(0.1f, 0.06f), blush, OrderPlayer + 16);
            Shape(head, "Nose", circle, new Vector2(0f, 0.13f), new Vector2(0.06f, 0.05f), GamePalette.Shade(skin, 0.86f), OrderPlayer + 16);

            rig.PupilL = Eye(head, -0.1f, skin, out rig.LidL);
            rig.PupilR = Eye(head, 0.1f, skin, out rig.LidR);

            var brow = GamePalette.Shade(hair, 0.8f);
            Shape(head, "BrowL", roundedSmall, new Vector2(-0.1f, 0.315f), new Vector2(0.12f, 0.03f), brow, OrderPlayer + 24);
            Shape(head, "BrowR", roundedSmall, new Vector2(0.1f, 0.315f), new Vector2(0.12f, 0.03f), brow, OrderPlayer + 24);

            var mouth = new GameObject("Mouth").transform;
            mouth.SetParent(head, false);
            mouth.localPosition = new Vector3(0f, 0.045f, 0f);
            Shape(mouth, "Mouth", roundedSmall, Vector2.zero, new Vector2(0.13f, 0.09f), GamePalette.Hex("#7A2E3A"), OrderPlayer + 24);
            Shape(mouth, "Tongue", circle, new Vector2(0f, -0.02f), new Vector2(0.07f, 0.03f), GamePalette.Hex("#FF7A8A"), OrderPlayer + 25);
            rig.Mouth = mouth;

            DrawHair(head, hair, hairStyle);
            DrawHat(head, look);
        }

        /// <summary>An eye is a white, a pupil that moves to look at things, and an eyelid that closes to blink.</summary>
        private static Transform Eye(Transform head, float x, Color skin, out Transform lid)
        {
            Shape(head, "Eye_Outline", circle, new Vector2(x, 0.22f), Vector2.one * 0.17f, GamePalette.Outline, OrderPlayer + 17);
            Shape(head, "Eye", circle, new Vector2(x, 0.22f), Vector2.one * 0.135f, GamePalette.White, OrderPlayer + 18);
            var pupil = new GameObject("Pupil").transform;
            pupil.SetParent(head, false);
            pupil.localPosition = new Vector3(x, 0.215f, 0f);
            Shape(pupil, "Pupil", circle, Vector2.zero, Vector2.one * 0.075f, GamePalette.Outline, OrderPlayer + 19);
            Shape(pupil, "Shine", circle, new Vector2(0.015f, 0.015f), Vector2.one * 0.025f, GamePalette.White, OrderPlayer + 20);

            lid = new GameObject("Lid").transform;
            lid.SetParent(head, false);
            lid.localPosition = new Vector3(x, 0.22f, 0f);
            lid.localScale = new Vector3(1f, 0f, 1f);
            Shape(lid, "Lid", circle, Vector2.zero, Vector2.one * 0.15f, skin, OrderPlayer + 21);
            return pupil;
        }

        private static void DrawHair(Transform head, Color hair, int style)
        {
            switch (style)
            {
                case 1:
                    Part2(head, "Fringe", rounded, new Vector2(0f, 0.44f), new Vector2(0.5f, 0.18f), hair, OrderPlayer + 28);
                    break;
                case 2:
                    Part2(head, "Hair", rounded, new Vector2(0f, 0.44f), new Vector2(0.5f, 0.18f), hair, OrderPlayer + 28);
                    Part2(head, "Bun", circle, new Vector2(0f, 0.62f), new Vector2(0.2f, 0.2f), hair, OrderPlayer + 27);
                    break;
                case 3:
                    Part2(head, "Tuft", rounded, new Vector2(0f, 0.45f), new Vector2(0.3f, 0.08f), hair, OrderPlayer + 28);
                    Part2(head, "SideL", roundedSmall, new Vector2(-0.22f, 0.26f), new Vector2(0.06f, 0.16f), hair, OrderPlayer + 28);
                    Part2(head, "SideR", roundedSmall, new Vector2(0.22f, 0.26f), new Vector2(0.06f, 0.16f), hair, OrderPlayer + 28);
                    break;
                default:
                    Part2(head, "Hair", rounded, new Vector2(0f, 0.44f), new Vector2(0.5f, 0.18f), hair, OrderPlayer + 28);
                    Part2(head, "SideL", roundedSmall, new Vector2(-0.22f, 0.3f), new Vector2(0.07f, 0.2f), hair, OrderPlayer + 28);
                    Part2(head, "SideR", roundedSmall, new Vector2(0.22f, 0.3f), new Vector2(0.07f, 0.2f), hair, OrderPlayer + 28);
                    break;
            }
        }

        private static void DrawHat(Transform head, Look look)
        {
            if (look.Extra != Accessory.Cap && look.Extra != Accessory.Helmet)
            {
                return;
            }

            var color = GamePalette.Hex(look.ExtraColor);
            if (look.Extra == Accessory.Cap)
            {
                Part2(head, "Cap", rounded, new Vector2(0f, 0.45f), new Vector2(0.52f, 0.2f), color, OrderPlayer + 32);
                Part2(head, "Brim", roundedSmall, new Vector2(0.1f, 0.35f), new Vector2(0.36f, 0.07f), GamePalette.Shade(color, 0.7f), OrderPlayer + 33);
            }
            else
            {
                Part2(head, "Helmet", rounded, new Vector2(0f, 0.46f), new Vector2(0.54f, 0.22f), color, OrderPlayer + 32);
                Shape(head, "Stripe", square, new Vector2(0f, 0.49f), new Vector2(0.07f, 0.16f), GamePalette.Shade(color, 1.2f), OrderPlayer + 35);
            }
        }

        private static void DrawTorsoAccessory(Transform torso, Look look)
        {
            var color = look.Extra == Accessory.None ? Color.white : GamePalette.Hex(look.ExtraColor);
            switch (look.Extra)
            {
                case Accessory.Tie:
                    Part2(torso, "Tie", roundedSmall, new Vector2(0f, 0.04f), new Vector2(0.09f, 0.3f), color, OrderPlayer + 10);
                    break;
                case Accessory.Vest:
                    Part2(torso, "Vest", roundedSmall, new Vector2(0f, -0.04f), new Vector2(0.5f, 0.4f), color, OrderPlayer + 10);
                    Shape(torso, "Zip", square, new Vector2(0f, -0.04f), new Vector2(0.03f, 0.38f), GamePalette.Shade(color, 0.6f), OrderPlayer + 12);
                    break;
            }
        }

        /// <summary>A shape with a dark outline behind it. The outline takes the order, the color the next one.</summary>
        private static void Part2(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order)
        {
            Shape(parent, name + "_Outline", sprite, center, size + Vector2.one * 0.05f, GamePalette.Outline, order);
            Shape(parent, name, sprite, center, size, color, order + 1);
        }
    }
}
