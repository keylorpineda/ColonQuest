using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The people of the game, version 2: a cartoon person built from separate vector shapes (head with hair,
    /// eyes, eyelids and mouth, torso, arms with an elbow and legs), each on its own pivot so PlayerAnimator can
    /// move them. The measures follow docs/characters-v2.md; the look of each person comes from MapBuilder.Cast.cs.
    /// </summary>
    public static partial class MapBuilder
    {
        private static readonly string[] SkinTones = { "#F6D8BC", "#EBBE96", "#D29B6E", "#B07A52", "#80553A", "#5E3B26" };

        /// <summary>From this index on, a skin tone goes with dark hair more often than not.</summary>
        private const int FirstDarkSkinTone = 3;
        private static readonly string[] HairTones = { "#2A1F1A", "#4A2F20", "#8A5A2B", "#D9A441", "#B0452D", "#555A66" };
        private static readonly string[] PantsTones = { "#2F3E66", "#3A3F52", "#6B5B50", "#1F6B58", "#8E4A5A" };

        private const float TorsoY = -0.02f;
        private const float ArmRestDegrees = 22f;
        private const float ElbowRestDegrees = 14f;

        /// <summary>The same look always gives the same person, but different looks give different people.</summary>
        private static int LookSeed(Look look)
        {
            int seed = 17;
            foreach (char c in (look.Skin ?? string.Empty) + look.Seed)
            {
                seed = seed * 31 + c;
            }

            // The mask, not Abs: the hash can overflow to the one value Abs cannot make positive.
            return seed & int.MaxValue;
        }

        private static bool Has(Look look, Extras extra)
        {
            return (look.Wear & extra) != 0;
        }

        private static Transform BuildPersonVisual(Transform root, Look look, bool social, float talkRadius, bool isPlayer)
        {
            look = Resolve(look, isPlayer);
            float width = look.Width;
            float height = look.Height;

            var shadowColor = GamePalette.Outline;
            shadowColor.a = 0.45f;
            var shadow = Shape(root, "Shadow", softCircle, new Vector2(0f, -0.52f), new Vector2(0.9f * width, 0.3f), shadowColor, OrderPlayer - 1);

            var bob = new GameObject("Bob").transform;
            bob.SetParent(root, false);
            var animator = bob.gameObject.AddComponent<PlayerAnimator>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(bob, false);

            var skin = GamePalette.Hex(look.SkinTone);
            var hair = GamePalette.Hex(look.HairColor);
            var pants = GamePalette.Hex(look.Pants);
            var shirt = GamePalette.Hex(look.Skin);

            var rig = new PersonRig();
            rig.Shadow = shadow != null ? shadow.transform : null;

            rig.LegL = Leg(visual, "LegL", -0.1f * width, look, pants, skin);
            rig.LegR = Leg(visual, "LegR", 0.1f * width, look, pants, skin);
            if (look.Skirt)
            {
                Part2(visual, "Skirt", rounded, new Vector2(0f, -0.26f * height), new Vector2(0.42f * width, 0.26f), pants, OrderPlayer + 2);
            }
            else
            {
                // The hips: they join the two legs under the round bottom of the shirt, so no gap opens at the waist.
                Part2(visual, "Hips", rounded, new Vector2(0f, -0.24f * height), new Vector2(0.42f * width, 0.12f), pants, OrderPlayer + 2);
            }

            var torso = new GameObject("Torso").transform;
            torso.SetParent(visual, false);
            torso.localPosition = new Vector3(0f, TorsoY, 0f);
            Part2(torso, "Torso", rounded, Vector2.zero, new Vector2(0.5f * width, 0.5f * height), shirt, OrderPlayer + 6);

            // Light from the upper left: a lit strip on that side and a darker band at the waist.
            Shape(torso, "TorsoShade", rounded, new Vector2(0.03f, -0.15f * height), new Vector2(0.5f * width - 0.08f, 0.16f), GamePalette.Shade(shirt, 0.84f), OrderPlayer + 8);
            Shape(torso, "TorsoLight", roundedSmall, new Vector2(-0.16f * width, 0.04f), new Vector2(0.07f, 0.3f * height), GamePalette.Shade(shirt, 1.15f), OrderPlayer + 8);
            Shape(torso, "Collar", roundedSmall, new Vector2(0f, 0.215f * height), new Vector2(0.2f, 0.07f), GamePalette.Shade(shirt, 0.78f), OrderPlayer + 9);
            // The neck belongs to the torso, so it leans with it; the animator moves the head along (see PlayerAnimator).
            Shape(torso, "Neck", roundedSmall, new Vector2(0f, 0.25f * height), new Vector2(0.12f, 0.09f), GamePalette.Shade(skin, 0.84f), OrderPlayer + 8);
            rig.Torso = torso;

            // The arms hang from the torso, so they follow its breathing and its lean.
            rig.ArmL = Arm(torso, "ArmL", -1, look, shirt, skin, out rig.ElbowL);
            rig.ArmR = Arm(torso, "ArmR", 1, look, shirt, skin, out rig.ElbowR);
            DrawExtras(torso, rig, look, shirt, skin);
            GroupTorsoFront(torso, rig);

            var head = new GameObject("Head").transform;
            head.SetParent(visual, false);
            head.localPosition = new Vector3(0f, 0.27f * height, 0f);
            rig.Head = head;
            BuildHead(head, look, skin, hair, rig);

            animator.Bind(rig, social, talkRadius);
            return visual;
        }

        /// <summary>A leg hangs from a pivot at the hip, so rotating it swings the whole leg.</summary>
        private static Transform Leg(Transform parent, string name, float x, Look look, Color pants, Color skin)
        {
            float height = look.Height;
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(x, -0.17f * height, 0f);

            // With a skirt the leg is a thin leg of skin.
            Part2(pivot, "Leg", roundedSmall, new Vector2(0f, -0.17f * height), new Vector2(look.Skirt ? 0.1f : 0.16f, 0.34f * height), look.Skirt ? skin : pants, OrderPlayer + 1);
            if (!look.Skirt)
            {
                Shape(pivot, "Crease", roundedSmall, new Vector2(0f, -0.17f * height), new Vector2(0.025f, 0.26f * height), GamePalette.Shade(pants, 0.78f), OrderPlayer + 3);
            }

            Part2(pivot, "Shoe", roundedSmall, new Vector2(0.02f, -0.35f * height), new Vector2(0.21f, 0.1f), GamePalette.Hex(look.Shoes), OrderPlayer + 3);
            return pivot;
        }

        /// <summary>
        /// An arm has two pivots: the shoulder, inside the torso, and the elbow at the end of the sleeve. The sleeve sits
        /// behind the torso so the joint stays hidden. side is -1 for the left arm and 1 for the right one.
        /// </summary>
        private static Transform Arm(Transform torso, string name, int side, Look look, Color shirt, Color skin, out Transform elbow)
        {
            float length = 0.2f * look.Height;

            var shoulder = new GameObject(name).transform;
            shoulder.SetParent(torso, false);
            shoulder.localPosition = new Vector3(side * 0.235f * look.Width, 0.15f, 0f);
            shoulder.localRotation = Quaternion.Euler(0f, 0f, side * ArmRestDegrees);
            Part2(shoulder, "Sleeve", roundedSmall, new Vector2(0f, -length * 0.5f), new Vector2(0.13f, length + 0.08f), shirt, OrderPlayer + 4);

            elbow = new GameObject(name + "Elbow").transform;
            elbow.SetParent(shoulder, false);
            elbow.localPosition = new Vector3(0f, -length, 0f);
            elbow.localRotation = Quaternion.Euler(0f, 0f, -side * ElbowRestDegrees);

            // The forearm starts right at the elbow and a round cap closes it there: turning around the elbow keeps it
            // inside the sleeve instead of a corner poking out of its side.
            var forearm = look.LongSleeve ? shirt : skin;
            Part2(elbow, "Forearm", roundedSmall, new Vector2(0f, -length * 0.5f), new Vector2(0.1f, length), forearm, OrderPlayer + 3);
            Part2(elbow, "Joint", circle, Vector2.zero, Vector2.one * 0.1f, forearm, OrderPlayer + 3);
            Part2(elbow, "Hand", circle, new Vector2(0f, -(length + 0.03f)), Vector2.one * 0.12f, skin, OrderPlayer + 5);
            if (!look.LongSleeve)
            {
                Shape(elbow, "SleeveEdge", circle, Vector2.zero, new Vector2(0.13f, 0.06f), GamePalette.Shade(shirt, 0.82f), OrderPlayer + 5);
            }

            return shoulder;
        }

        private static void BuildHead(Transform head, Look look, Color skin, Color hair, PersonRig rig)
        {
            float h = look.Head;
            bool hat = look.Hat != Hat.None;
            var ink = GamePalette.Hex("#2A2F45");

            DrawHairBehind(head, look, hair, hat, rig);
            if (look.Hat == Hat.Hood)
            {
                Part2(head, "Hood", rounded, new Vector2(0f, 0.2f), new Vector2(0.62f * h, 0.56f), GamePalette.Hex(look.HatColor), OrderPlayer + 8);
            }

            Part2(head, "EarL", circle, new Vector2(-0.235f * h, 0.17f), new Vector2(0.09f, 0.12f), skin, OrderPlayer + 10);
            Part2(head, "EarR", circle, new Vector2(0.235f * h, 0.17f), new Vector2(0.09f, 0.12f), skin, OrderPlayer + 10);

            if (look.SquareJaw)
            {
                // A wider chin. The cover hides the outline of the round face that would cross the jaw.
                Part2(head, "Jaw", rounded, new Vector2(0f, 0.06f), new Vector2(0.46f * h, 0.22f), skin, OrderPlayer + 10);
            }

            Part2(head, "Face", circle, new Vector2(0f, 0.2f), new Vector2(0.5f * h, 0.46f), skin, OrderPlayer + 11);
            if (look.SquareJaw)
            {
                // As wide as the jaw, so the outline of the round face does not cross it; the jaw keeps its own outline.
                Shape(head, "JawCover", rounded, new Vector2(0f, 0.06f), new Vector2(0.44f * h, 0.2f), skin, OrderPlayer + 13);
            }

            // Everything from here to the beard is the face: it is hidden while the person walks away (see BuildBackView).
            int faceFirst = head.childCount;

            // A soft shade over the chin, kept inside the face so it never covers the dark edge of the chin.
            Shape(head, "JawShade", circle, new Vector2(0f, 0.045f), new Vector2(0.26f * h, 0.05f), GamePalette.Shade(skin, 0.9f), OrderPlayer + 13);

            rig.PupilL = Eye(head, -0.1f * h, look, skin, ink, out rig.LidL);
            rig.PupilR = Eye(head, 0.1f * h, look, skin, ink, out rig.LidR);

            var brow = GamePalette.Shade(GamePalette.Hex(look.HairColor), 0.8f);
            float browLeft;
            float browRight;
            float browRaise = 0f;
            switch (look.Face)
            {
                case Expression.Warm:
                    browLeft = -8f;
                    browRight = 8f;
                    break;
                case Expression.Smirk:
                    browLeft = -12f;
                    browRight = 14f;
                    browRaise = 0.015f;
                    break;
                case Expression.Frown:
                    browLeft = 14f;
                    browRight = -14f;
                    break;
                default:
                    browLeft = 0f;
                    browRight = 0f;
                    break;
            }

            ShapeRot(head, "BrowL", roundedSmall, new Vector2(-0.1f * h, 0.305f), new Vector2(0.12f, 0.035f), brow, OrderPlayer + 21, browLeft);
            ShapeRot(head, "BrowR", roundedSmall, new Vector2(0.1f * h, 0.305f + browRaise), new Vector2(0.12f, 0.035f), brow, OrderPlayer + 21, browRight);

            Shape(head, "Nose", circle, new Vector2(0f, 0.13f), new Vector2(0.065f, 0.05f), GamePalette.Shade(skin, 0.82f), OrderPlayer + 18);

            if (look.Blush)
            {
                var cheek = Color.Lerp(skin, GamePalette.Hex("#FF828C"), 0.35f);
                Shape(head, "BlushL", circle, new Vector2(-0.15f * h, 0.1f), new Vector2(0.08f, 0.045f), cheek, OrderPlayer + 18);
                Shape(head, "BlushR", circle, new Vector2(0.15f * h, 0.1f), new Vector2(0.08f, 0.045f), cheek, OrderPlayer + 18);
            }

            rig.Mouth = BuildMouth(head, look, skin, rig);

            if (look.Mustache)
            {
                Shape(head, "Mustache", roundedSmall, new Vector2(0f, 0.095f), new Vector2(0.16f, 0.05f), brow, OrderPlayer + 21);
            }

            if (look.Goatee)
            {
                Part2(head, "Goatee", roundedSmall, new Vector2(look.MouthX, 0f), new Vector2(0.16f, 0.08f), GamePalette.Hex(look.HairColor), OrderPlayer + 22);
            }

            int faceEnd = head.childCount;
            DrawHairFront(head, look, hair, hat);
            int hairFrontEnd = head.childCount;
            DrawHat(head, look);
            BuildBackView(head, look, skin, hair, rig, faceFirst, faceEnd, hairFrontEnd);
        }

        /// <summary>
        /// What the animator swaps while the person walks up the screen, away from the camera: the face (and the fringe on
        /// the forehead) goes into a "Face" group that is hidden, and a "BackHead" of hair (or of the hood, or of skin for
        /// a bald head) covers the head. Ears, hats, spikes and curls stay, since they show from behind too.
        /// </summary>
        private static void BuildBackView(Transform head, Look look, Color skin, Color hair, PersonRig rig, int faceFirst, int faceEnd, int hairFrontEnd)
        {
            var move = new System.Collections.Generic.List<Transform>();
            for (int i = faceFirst; i < faceEnd && i < head.childCount; i++)
            {
                move.Add(head.GetChild(i));
            }

            for (int i = faceEnd; i < hairFrontEnd && i < head.childCount; i++)
            {
                var child = head.GetChild(i);
                if (child.name.StartsWith("Fringe") || child.name.StartsWith("Lock"))
                {
                    move.Add(child);
                }
            }

            var face = new GameObject("Face").transform;
            face.SetParent(head, false);
            foreach (var part in move)
            {
                part.SetParent(face, false);
            }

            float h = look.Head;
            Color back = look.Hat == Hat.Hood ? GamePalette.Hex(look.HatColor) : (look.Hair == HairStyle.Bald ? GamePalette.Shade(skin, 0.95f) : hair);
            var backHead = new GameObject("BackHead").transform;
            backHead.SetParent(head, false);
            // As big as the face with its outline, so no edge of the face peeks out; under the hats (+26) and the curls.
            Part2(backHead, "BackHair", circle, new Vector2(0f, 0.2f), new Vector2(0.5f * h + 0.05f, 0.51f), back, OrderPlayer + 22);
            backHead.gameObject.SetActive(false);

            rig.Face = face.gameObject;
            rig.BackHead = backHead.gameObject;
        }

        /// <summary>The things worn on the chest (tie, badge, apron...): hidden from behind with the face.</summary>
        private static readonly System.Collections.Generic.HashSet<string> TorsoFrontParts = new System.Collections.Generic.HashSet<string>
        {
            "Tie", "Tie_Outline", "Knot", "LanyardL", "LanyardR", "Card", "Card_Outline", "Badge", "Badge_Outline", "Pen",
            "PoliceBadge", "Chain", "ChainCut", "TubeL", "TubeR", "Chestpiece", "Chestpiece_Outline", "Apron", "Apron_Outline",
            "ApronPocket", "Bib", "Bib_Outline", "Pocket", "RobeCollar"
        };

        private static void GroupTorsoFront(Transform torso, PersonRig rig)
        {
            var move = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in torso)
            {
                if (TorsoFrontParts.Contains(child.name))
                {
                    move.Add(child);
                }
            }

            if (move.Count == 0)
            {
                return;
            }

            var front = new GameObject("TorsoFront").transform;
            front.SetParent(torso, false);
            foreach (var part in move)
            {
                part.SetParent(front, false);
            }

            rig.TorsoFront = front.gameObject;
        }

        /// <summary>The mouth is a pivot that PlayerAnimator squashes to talk. A smile is a dark disc cut by a disc of skin.</summary>
        private static Transform BuildMouth(Transform head, Look look, Color skin, PersonRig rig)
        {
            var mouth = new GameObject("Mouth").transform;
            mouth.SetParent(head, false);
            mouth.localPosition = new Vector3(look.MouthX, 0.065f, 0f);

            var dark = GamePalette.Hex("#7A2E3A");
            if (look.Face == Expression.Neutral)
            {
                Shape(mouth, "Mouth", roundedSmall, Vector2.zero, new Vector2(0.1f, 0.025f), dark, OrderPlayer + 19);
                rig.MouthRest = 0.5f;
                return mouth;
            }

            var inner = new GameObject("Curve").transform;
            inner.SetParent(mouth, false);
            if (look.Face == Expression.Smirk)
            {
                inner.localPosition = new Vector3(0.03f, 0f, 0f);
                inner.localRotation = Quaternion.Euler(0f, 0f, 12f);
            }

            float cut = look.Face == Expression.Frown ? -0.025f : 0.025f;
            Shape(inner, "Lip", circle, Vector2.zero, new Vector2(0.13f, 0.08f), dark, OrderPlayer + 19);
            Shape(inner, "Cut", circle, new Vector2(0f, cut), new Vector2(0.14f, 0.07f), skin, OrderPlayer + 20);
            rig.MouthRest = 0.55f;
            return mouth;
        }

        /// <summary>An eye is a white, a pupil that moves to look at things, and an eyelid that closes to blink.</summary>
        private static Transform Eye(Transform head, float x, Look look, Color skin, Color ink, out Transform lid)
        {
            const float y = 0.21f;
            var white = new Vector2(0.095f, 0.115f);

            if (look.Glasses)
            {
                Shape(head, "Rim", circle, new Vector2(x, y), new Vector2(0.15f, 0.14f), ink, OrderPlayer + 13);
                Shape(head, "Lens", circle, new Vector2(x, y), new Vector2(0.12f, 0.11f), Color.Lerp(skin, Color.white, 0.25f), OrderPlayer + 14);
            }

            Shape(head, "Eye_Outline", circle, new Vector2(x, y), white + Vector2.one * 0.04f, GamePalette.Outline, OrderPlayer + 15);
            Shape(head, "Eye", circle, new Vector2(x, y), white, GamePalette.White, OrderPlayer + 16);

            var pupil = new GameObject("Pupil").transform;
            pupil.SetParent(head, false);
            pupil.localPosition = new Vector3(x + 0.012f + look.LookAside, y - 0.005f, 0f);
            Shape(pupil, "Pupil", circle, Vector2.zero, new Vector2(0.065f, 0.08f), GamePalette.Outline, OrderPlayer + 17);
            Shape(pupil, "Shine", circle, new Vector2(0.012f, 0.017f), Vector2.one * 0.022f, GamePalette.White, OrderPlayer + 18);

            lid = new GameObject("Lid").transform;
            lid.SetParent(head, false);
            lid.localPosition = new Vector3(x, y, 0f);
            lid.localScale = new Vector3(1f, 0f, 1f);
            Shape(lid, "Lid", circle, Vector2.zero, new Vector2(0.115f, 0.135f), skin, OrderPlayer + 19);

            if (look.HeavyLids)
            {
                // A fixed drooping eyelid, apart from the one that blinks.
                Shape(head, "Droop", circle, new Vector2(x, y + 0.045f), new Vector2(0.12f, 0.05f), skin, OrderPlayer + 20);
            }

            if (look.Glasses)
            {
                Shape(head, "Bridge", roundedSmall, new Vector2(0f, 0.215f), new Vector2(0.06f, 0.02f), ink, OrderPlayer + 20);
            }

            return pupil;
        }

        /// <summary>The hair that sits behind the head. With a hat only what falls at the back or the sides stays.</summary>
        private static void DrawHairBehind(Transform head, Look look, Color hair, bool hat, PersonRig rig)
        {
            float h = look.Head;
            Transform hairParent = head;
            Vector2 swayOffset = Vector2.zero;
            if (look.Hair == HairStyle.Long || look.Hair == HairStyle.Ponytail || look.Hair == HairStyle.Bun || look.Hair == HairStyle.Bob)
            {
                var hairSway = new GameObject("HairSway").transform;
                hairSway.SetParent(head, false);
                Vector2 pivotPos = look.Hair == HairStyle.Ponytail ? new Vector2(0.27f * h, 0.18f) : new Vector2(0f, 0.38f);
                hairSway.localPosition = (Vector3)pivotPos;
                rig.HairSway = hairSway;
                hairParent = hairSway;
                swayOffset = -pivotPos;
            }

            switch (look.Hair)
            {
                case HairStyle.Long:
                    Part2(hairParent, "HairLong", rounded, new Vector2(0f, 0.17f) + swayOffset, new Vector2(0.6f * h, 0.52f), hair, OrderPlayer + 9);
                    break;
                case HairStyle.Afro:
                    Part2(head, "HairAfro", circle, new Vector2(0f, 0.36f), new Vector2(0.68f * h, 0.42f), hair, OrderPlayer + 8);
                    break;
                case HairStyle.Bob:
                    Part2(hairParent, "HairBob", rounded, new Vector2(0f, 0.2f) + swayOffset, new Vector2(0.58f * h, 0.42f), hair, OrderPlayer + 8);
                    break;
                case HairStyle.Ponytail:
                    Part2Rot(hairParent, "Tail", rounded, new Vector2(0.27f * h, 0.18f) + swayOffset, new Vector2(0.11f, 0.3f), hair, OrderPlayer + 8, -12f);
                    if (!hat)
                    {
                        Part2(hairParent, "HairBack", circle, new Vector2(0f, 0.27f) + swayOffset, new Vector2(0.54f * h, 0.4f), hair, OrderPlayer + 9);
                    }

                    break;
                case HairStyle.Bald:
                    Part2(head, "SideL", circle, new Vector2(-0.2f * h, 0.27f), new Vector2(0.08f, 0.14f), hair, OrderPlayer + 9);
                    Part2(head, "SideR", circle, new Vector2(0.2f * h, 0.27f), new Vector2(0.08f, 0.14f), hair, OrderPlayer + 9);
                    break;
                case HairStyle.Bun:
                    if (!hat)
                    {
                        Part2(hairParent, "Bun", circle, new Vector2(0f, 0.55f) + swayOffset, Vector2.one * 0.17f, hair, OrderPlayer + 7);
                        Part2(hairParent, "HairBack", circle, new Vector2(0f, 0.27f) + swayOffset, new Vector2(0.54f * h, 0.4f), hair, OrderPlayer + 9);
                    }

                    break;
                default:
                    if (!hat)
                    {
                        Part2(head, "HairBack", circle, new Vector2(0f, 0.27f), new Vector2(0.54f * h, 0.4f), hair, OrderPlayer + 9);
                    }

                    break;
            }
        }

        /// <summary>The fringe on the forehead. A hat covers it, so nothing is drawn.</summary>
        private static void DrawHairFront(Transform head, Look look, Color hair, bool hat)
        {
            if (hat)
            {
                return;
            }

            float h = look.Head;
            int order = OrderPlayer + 24;
            switch (look.Hair)
            {
                case HairStyle.Side:
                    Part2(head, "Fringe", circle, new Vector2(0f, 0.38f), new Vector2(0.5f * h, 0.2f), hair, order);
                    Part2Rot(head, "Lock", rounded, new Vector2(-0.07f, 0.36f), new Vector2(0.3f, 0.11f), hair, order, -18f);
                    break;
                case HairStyle.Long:
                    Part2Rot(head, "FringeL", circle, new Vector2(-0.1f * h, 0.37f), new Vector2(0.3f, 0.17f), hair, order, 14f);
                    Part2Rot(head, "FringeR", circle, new Vector2(0.1f * h, 0.37f), new Vector2(0.3f, 0.17f), hair, order, -14f);
                    break;
                case HairStyle.Spiky:
                    float[] xs = { -0.15f, -0.05f, 0.06f, 0.16f };
                    float[] angles = { 25f, 8f, -8f, -25f };
                    for (int i = 0; i < xs.Length; i++)
                    {
                        Part2Rot(head, "Spike" + i, rounded, new Vector2(xs[i], 0.48f), new Vector2(0.1f, 0.17f), hair, order - 1, angles[i]);
                    }

                    Part2(head, "Fringe", circle, new Vector2(0f, 0.39f), new Vector2(0.48f * h, 0.16f), hair, order);
                    break;
                case HairStyle.Bun:
                case HairStyle.Ponytail:
                    Part2(head, "Fringe", circle, new Vector2(0f, 0.39f), new Vector2(0.5f * h, 0.18f), hair, order);
                    break;
                case HairStyle.Curly:
                    Vector2[] curls =
                    {
                        new Vector2(-0.18f, 0.33f), new Vector2(-0.1f, 0.42f), new Vector2(0f, 0.45f),
                        new Vector2(0.1f, 0.42f), new Vector2(0.18f, 0.33f)
                    };
                    for (int i = 0; i < curls.Length; i++)
                    {
                        Part2(head, "Curl" + i, circle, new Vector2(curls[i].x * h, curls[i].y), new Vector2(0.16f, 0.15f), hair, order);
                    }

                    break;
                case HairStyle.Afro:
                    Part2(head, "Fringe", circle, new Vector2(0f, 0.41f), new Vector2(0.5f * h, 0.16f), hair, order);
                    break;
                case HairStyle.Bob:
                    Part2(head, "Fringe", circle, new Vector2(0.04f, 0.39f), new Vector2(0.52f * h, 0.18f), hair, order);
                    break;
                case HairStyle.Buzz:
                    Part2(head, "Fringe", circle, new Vector2(0f, 0.4f), new Vector2(0.48f * h, 0.13f), hair, order);
                    break;
            }
        }

        private static void DrawHat(Transform head, Look look)
        {
            if (look.Hat == Hat.None || look.Hat == Hat.Hood)
            {
                return;
            }

            float h = look.Head;
            var color = GamePalette.Hex(look.HatColor);
            int order = OrderPlayer + 26;
            switch (look.Hat)
            {
                case Hat.Cap:
                    Part2(head, "Cap", circle, new Vector2(0f, 0.4f), new Vector2(0.54f * h, 0.24f), color, order);
                    Part2(head, "Brim", circle, new Vector2(look.BrimSide * 0.12f, 0.31f), new Vector2(0.34f, 0.08f), GamePalette.Shade(color, 0.75f), order + 2);
                    break;
                case Hat.FlatCap:
                    Part2(head, "Cap", rounded, new Vector2(0f, 0.39f), new Vector2(0.56f * h, 0.18f), color, order);
                    Part2(head, "Brim", rounded, new Vector2(0f, 0.31f), new Vector2(0.5f, 0.06f), GamePalette.Shade(color, 0.75f), order + 2);
                    break;
                case Hat.Helmet:
                    Part2(head, "Helmet", circle, new Vector2(0f, 0.38f), new Vector2(0.58f * h, 0.3f), color, order);
                    Part2(head, "Rim", rounded, new Vector2(0f, 0.28f), new Vector2(0.62f * h, 0.06f), GamePalette.Shade(color, 0.8f), order + 2);
                    break;
                case Hat.Police:
                    Part2(head, "Cap", rounded, new Vector2(0f, 0.42f), new Vector2(0.56f * h, 0.2f), color, order);
                    Part2(head, "Visor", rounded, new Vector2(0f, 0.31f), new Vector2(0.5f, 0.07f), GamePalette.Hex("#0B0E1A"), order + 2);
                    Shape(head, "Shield", circle, new Vector2(0f, 0.43f), Vector2.one * 0.08f, GamePalette.Hex("#FFD23F"), order + 5);
                    break;
                case Hat.Nurse:
                    Part2(head, "Cap", rounded, new Vector2(0f, 0.45f), new Vector2(0.3f, 0.1f), GamePalette.White, order);
                    Shape(head, "Band", roundedSmall, new Vector2(0f, 0.45f), new Vector2(0.18f, 0.03f), GamePalette.Hex("#2E86C1"), order + 3);
                    break;
                case Hat.Beanie:
                    Part2(head, "Beanie", circle, new Vector2(0f, 0.38f), new Vector2(0.56f * h, 0.3f), color, order);
                    Part2(head, "Fold", rounded, new Vector2(0f, 0.3f), new Vector2(0.6f * h, 0.09f), GamePalette.Shade(color, 0.8f), order + 2);
                    break;
                case Hat.Chonete:
                    Part2(head, "Brim", circle, new Vector2(0f, 0.31f), new Vector2(0.78f, 0.13f), color, order);
                    Part2(head, "Crown", circle, new Vector2(0f, 0.4f), new Vector2(0.44f, 0.2f), color, order + 2);
                    Shape(head, "Band", roundedSmall, new Vector2(0f, 0.33f), new Vector2(0.44f, 0.035f), GamePalette.Shade(color, 0.7f), order + 4);
                    break;
            }
        }

        /// <summary>The details of the clothes: ties, aprons, coats, straps. They sit on the torso, below the face.</summary>
        private static void DrawExtras(Transform torso, PersonRig rig, Look look, Color shirt, Color skin)
        {
            float w = look.Width;
            float hh = look.Height;
            int order = OrderPlayer + 10;
            var wear = GamePalette.Hex(look.WearColor);
            var ink = GamePalette.Hex("#1D2238");

            if (Has(look, Extras.Backpack))
            {
                Shape(torso, "StrapL", roundedSmall, new Vector2(-0.13f * w, 0f), new Vector2(0.045f, 0.42f * hh), ink, order + 1);
                Shape(torso, "StrapR", roundedSmall, new Vector2(0.13f * w, 0f), new Vector2(0.045f, 0.42f * hh), ink, order + 1);
            }

            if (Has(look, Extras.Stripe))
            {
                Shape(torso, "Stripe", roundedSmall, new Vector2(0f, 0.02f), new Vector2(0.46f * w, 0.06f), wear, order + 2);
            }

            if (Has(look, Extras.Overall))
            {
                Part2(torso, "Overall", rounded, new Vector2(0f, -0.07f * hh), new Vector2(0.42f * w, 0.34f * hh), wear, order);
                Part2(torso, "Bib", roundedSmall, new Vector2(0f, 0.07f * hh), new Vector2(0.22f * w, 0.16f * hh), wear, order);
                Shape(torso, "Pocket", roundedSmall, new Vector2(0.12f * w, -0.04f), new Vector2(0.07f, 0.06f), GamePalette.Shade(wear, 0.7f), order + 3);
            }

            if (Has(look, Extras.Apron))
            {
                Part2(torso, "Apron", rounded, new Vector2(0f, -0.07f * hh), new Vector2(0.3f * w, 0.34f * hh), GamePalette.White, order);
                Shape(torso, "ApronPocket", roundedSmall, new Vector2(0f, -0.16f * hh), new Vector2(0.14f * w, 0.07f), GamePalette.Shade(GamePalette.White, 0.88f), order + 3);
            }

            if (Has(look, Extras.Cardigan))
            {
                var cardigan = GamePalette.Hex(look.WearColor);
                Part2(torso, "CardiganL", roundedSmall, new Vector2(-0.15f * w, -0.03f * hh), new Vector2(0.17f * w, 0.42f * hh), cardigan, order);
                Part2(torso, "CardiganR", roundedSmall, new Vector2(0.15f * w, -0.03f * hh), new Vector2(0.17f * w, 0.42f * hh), cardigan, order);
            }

            if (Has(look, Extras.Coat))
            {
                var coat = GamePalette.Hex("#F4F7FB");
                Part2(torso, "CoatL", roundedSmall, new Vector2(-0.15f * w, -0.03f * hh), new Vector2(0.2f * w, 0.46f * hh), coat, order);
                Part2(torso, "CoatR", roundedSmall, new Vector2(0.15f * w, -0.03f * hh), new Vector2(0.2f * w, 0.46f * hh), coat, order);
            }

            if (Has(look, Extras.Robe))
            {
                Shape(torso, "RobeCollar", roundedSmall, new Vector2(0f, 0.19f * hh), new Vector2(0.14f, 0.07f), GamePalette.White, order + 3);
            }

            if (Has(look, Extras.PoliceBadge))
            {
                Shape(torso, "Belt", roundedSmall, new Vector2(0f, -0.2f * hh), new Vector2(0.5f * w, 0.05f), GamePalette.Hex("#0B0E1A"), order + 1);
                Shape(torso, "PoliceBadge", circle, new Vector2(-0.11f * w, 0.08f), Vector2.one * 0.07f, GamePalette.Hex("#FFD23F"), order + 3);
            }

            if (Has(look, Extras.Scarf))
            {
                var tieSway = new GameObject("TieSway").transform;
                tieSway.SetParent(torso, false);
                tieSway.localPosition = new Vector3(0f, 0.2f * hh, 0f);
                rig.TieSway = tieSway;
                Shape(tieSway, "Scarf", roundedSmall, Vector2.zero, new Vector2(0.34f * w, 0.09f), wear, order + 2);
                Shape(tieSway, "ScarfTail", roundedSmall, new Vector2(-0.06f, 0.1f - 0.2f * hh), new Vector2(0.07f, 0.2f), wear, order + 2);
            }

            if (Has(look, Extras.Tie))
            {
                var tieSway = new GameObject("TieSway").transform;
                tieSway.SetParent(torso, false);
                tieSway.localPosition = new Vector3(0f, 0.19f * hh, 0f);
                rig.TieSway = tieSway;
                Part2(tieSway, "Tie", roundedSmall, new Vector2(0f, 0.04f - 0.19f * hh), new Vector2(0.09f, 0.3f), wear, order + 2);
                Shape(tieSway, "Knot", roundedSmall, Vector2.zero, new Vector2(0.07f, 0.05f), GamePalette.Shade(wear, 0.8f), order + 4);
            }

            if (Has(look, Extras.Lanyard))
            {
                var strap = GamePalette.Hex(look.WearColor2);
                ShapeRot(torso, "LanyardL", roundedSmall, new Vector2(-0.05f, 0.1f), new Vector2(0.03f, 0.22f), strap, order + 4, 18f);
                ShapeRot(torso, "LanyardR", roundedSmall, new Vector2(0.05f, 0.1f), new Vector2(0.03f, 0.22f), strap, order + 4, -18f);
                Part2(torso, "Card", roundedSmall, new Vector2(0f, -0.04f), new Vector2(0.07f, 0.08f), GamePalette.White, order + 4);
            }

            if (Has(look, Extras.Badge))
            {
                Part2(torso, "Badge", roundedSmall, new Vector2(-0.14f * w, 0.07f), new Vector2(0.09f, 0.07f), GamePalette.White, order + 3);
            }

            if (Has(look, Extras.Pen))
            {
                Shape(torso, "Pen", roundedSmall, new Vector2(0.14f * w, 0.08f), new Vector2(0.02f, 0.08f), GamePalette.Hex("#2E86C1"), order + 3);
            }

            if (Has(look, Extras.Chain))
            {
                Shape(torso, "Chain", circle, new Vector2(0f, 0.12f), new Vector2(0.2f, 0.13f), GamePalette.Hex("#FFD23F"), order + 1);
                Shape(torso, "ChainCut", circle, new Vector2(0f, 0.135f), new Vector2(0.17f, 0.09f), shirt, order + 2);
            }

            if (Has(look, Extras.Pouch))
            {
                Shape(torso, "PouchStrap", roundedSmall, new Vector2(0f, -0.16f * hh), new Vector2(0.48f * w, 0.03f), GamePalette.Hex("#0B0E1A"), order + 1);
                Part2(torso, "Pouch", roundedSmall, new Vector2(0f, -0.21f * hh), new Vector2(0.26f * w, 0.1f), GamePalette.Hex("#4A4440"), order + 1);
            }

            if (Has(look, Extras.Stethoscope))
            {
                ShapeRot(torso, "TubeL", roundedSmall, new Vector2(-0.07f, 0.12f), new Vector2(0.025f, 0.2f), ink, order + 3, 12f);
                ShapeRot(torso, "TubeR", roundedSmall, new Vector2(0.07f, 0.12f), new Vector2(0.025f, 0.2f), ink, order + 3, -12f);
                Part2(torso, "Chestpiece", circle, new Vector2(0.05f, -0.04f), Vector2.one * 0.06f, GamePalette.Hex("#A9B0C4"), order + 3);
            }

            // The phone, the watch and the bag go on the hand, so they move with the arm.
            float length = 0.2f * hh;
            if (Has(look, Extras.Phone))
            {
                Part2(rig.ElbowR, "Phone", roundedSmall, new Vector2(0.05f, -(length + 0.02f)), new Vector2(0.07f, 0.12f), ink, OrderPlayer + 8);
            }

            if (Has(look, Extras.Watch))
            {
                Shape(rig.ElbowL, "Watch", roundedSmall, new Vector2(0f, -(length - 0.01f)), new Vector2(0.12f, 0.04f), GamePalette.Hex("#555A66"), OrderPlayer + 8);
            }

            if (Has(look, Extras.Bag))
            {
                Part2(rig.ElbowL, "Bag", rounded, new Vector2(-0.02f, -(length + 0.12f)), new Vector2(0.14f, 0.12f), GamePalette.Hex("#8B5A3C"), OrderPlayer + 8);
            }
        }

        /// <summary>A shape rotated around its center, in degrees.</summary>
        private static void ShapeRot(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order, float degrees)
        {
            Shape(parent, name, sprite, center, size, color, order).transform.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        /// <summary>A shape with a dark outline behind it. The outline takes the order, the color the next one.</summary>
        private static void Part2(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order)
        {
            Shape(parent, name + "_Outline", sprite, center, size + Vector2.one * 0.05f, GamePalette.Outline, order);
            Shape(parent, name, sprite, center, size, color, order + 1);
        }

        /// <summary>Part2 with both of its shapes rotated.</summary>
        private static void Part2Rot(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order, float degrees)
        {
            ShapeRot(parent, name + "_Outline", sprite, center, size + Vector2.one * 0.05f, GamePalette.Outline, order, degrees);
            ShapeRot(parent, name, sprite, center, size, color, order + 1, degrees);
        }
    }
}
