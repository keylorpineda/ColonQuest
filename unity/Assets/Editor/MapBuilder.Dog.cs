using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The house dog (the zaguatico) lying on its cushion, built as a small rig so <see cref="DogAnimator"/> can move
    /// its tail, head, ear, eye and tongue. It faces right, as the old flat drawing did, and keeps the same spot.
    /// </summary>
    public static partial class MapBuilder
    {
        private static readonly Color DogFur = GamePalette.Hex("#C68B59");
        private static readonly Color DogFurDark = GamePalette.Hex("#A66D3B");
        private static readonly Color DogFurLight = GamePalette.Hex("#E2B98A");
        private static readonly Color DogEar = GamePalette.Hex("#7D4E27");

        /// <summary>
        /// Builds the dog under <paramref name="pet"/> with its body centred on <paramref name="at"/>. The root is called
        /// "Dog": the petting scene shakes that object, and the animator reads the shake as the cue to cheer.
        /// </summary>
        private static Transform BuildDog(Transform pet, Vector2 at)
        {
            var dog = Pivot(pet, "Dog", at, 0f);
            var rig = Pivot(dog, "Rig", Vector2.zero, 0f);

            // The tail hangs from the rump and rests raised; the animator wags it around this pivot.
            var tail = Pivot(rig, "TailPivot", new Vector2(-0.26f, 0.04f), 28f);
            DogPart(tail, "Tail", roundedSmall, new Vector2(-0.13f, 0f), new Vector2(0.27f, 0.1f), DogFur, 9);
            Shape(tail, "TailTip", circle, new Vector2(-0.25f, 0f), Vector2.one * 0.085f, DogFurLight, 11);

            // The body breathes: it scales up and down from its own centre.
            var body = Pivot(rig, "Body", Vector2.zero, 0f);
            DogPart(body, "Body", roundedSmall, Vector2.zero, new Vector2(0.58f, 0.36f), DogFur, 9);
            Shape(body, "Belly", roundedSmall, new Vector2(0.04f, -0.09f), new Vector2(0.4f, 0.13f), DogFurLight, 11);
            Shape(body, "Spot", circle, new Vector2(-0.08f, 0.07f), new Vector2(0.22f, 0.15f), DogFurDark, 11);
            Shape(body, "Collar", roundedSmall, new Vector2(0.15f, 0.02f), new Vector2(0.06f, 0.2f), GamePalette.Hex("#E8505B"), 12);
            Shape(body, "Tag", circle, new Vector2(0.17f, -0.08f), Vector2.one * 0.08f, GamePalette.Hex("#FFD23F"), 13);

            // The paws stay on the cushion.
            DogPart(rig, "PawBack", roundedSmall, new Vector2(0.3f, -0.15f), new Vector2(0.24f, 0.09f), DogFurDark, 11);
            DogPart(rig, "PawFront", roundedSmall, new Vector2(0.37f, -0.1f), new Vector2(0.26f, 0.1f), DogFur, 12);
            Shape(rig, "PawToe", circle, new Vector2(0.48f, -0.1f), Vector2.one * 0.08f, DogFurLight, 14);

            // The head turns at the neck: down on the paws to sleep, up when the player comes.
            var head = Pivot(rig, "HeadPivot", new Vector2(0.17f, 0.05f), 0f);
            DogPart(head, "Head", circle, new Vector2(0.08f, 0.05f), Vector2.one * 0.32f, DogFur, 14);
            Shape(head, "Muzzle", roundedSmall, new Vector2(0.2f, 0.02f), new Vector2(0.2f, 0.14f), DogFurLight, 16);
            Shape(head, "Nose", circle, new Vector2(0.29f, 0.04f), new Vector2(0.07f, 0.055f), GamePalette.Hex("#1F1F1F"), 17);
            var tongue = Pivot(head, "Tongue", new Vector2(0.22f, -0.04f), 0f);
            Shape(tongue, "Tongue", roundedSmall, new Vector2(0f, -0.035f), new Vector2(0.065f, 0.08f), GamePalette.Hex("#FF7A8A"), 15);
            var eye = Pivot(head, "Eye", new Vector2(0.11f, 0.1f), 0f);
            Shape(eye, "Eye", circle, Vector2.zero, Vector2.one * 0.055f, GamePalette.Hex("#1F1F1F"), 17);
            Shape(eye, "Shine", circle, new Vector2(0.01f, 0.012f), Vector2.one * 0.018f, GamePalette.White, 18);

            // The floppy ear hangs from the top of the head and flicks now and then.
            var ear = Pivot(head, "EarPivot", new Vector2(0.01f, 0.17f), -18f);
            Shape(ear, "Ear_Outline", roundedSmall, new Vector2(-0.01f, -0.08f), new Vector2(0.17f, 0.26f), GamePalette.Outline, 18);
            Shape(ear, "Ear", roundedSmall, new Vector2(-0.01f, -0.08f), new Vector2(0.13f, 0.22f), DogEar, 19);

            // A heart that floats up while the dog is petted. Hidden until then.
            var heart = Pivot(dog, "DogHeart", new Vector2(0.28f, 0.42f), 0f);
            var pink = GamePalette.Hex("#FF5C7A");
            Shape(heart, "LobeL", circle, new Vector2(-0.045f, 0.03f), Vector2.one * 0.1f, pink, 20);
            Shape(heart, "LobeR", circle, new Vector2(0.045f, 0.03f), Vector2.one * 0.1f, pink, 20);
            Shape(heart, "Point", square, new Vector2(0f, -0.02f), Vector2.one * 0.095f, pink, 20).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            dog.gameObject.AddComponent<DogAnimator>().Bind(rig, body, head, tail, ear, eye, tongue, heart);
            return dog;
        }

        /// <summary>An empty transform that parts hang from, turned to its resting angle.</summary>
        private static Transform Pivot(Transform parent, string name, Vector2 position, float degrees)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            pivot.localRotation = Quaternion.Euler(0f, 0f, degrees);
            return pivot;
        }

        /// <summary>A dog part with the dark outline of the style behind it.</summary>
        private static void DogPart(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order)
        {
            Shape(parent, name + "_Outline", sprite, center, size + Vector2.one * 0.05f, GamePalette.Outline, order);
            Shape(parent, name, sprite, center, size, color, order + 1);
        }
    }
}
