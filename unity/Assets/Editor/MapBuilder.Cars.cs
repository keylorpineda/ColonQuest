using ColonQuest.World;
using UnityEditor;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The cars of the town: four real body styles in the inked style of the buildings, with wheels that turn and somebody at
    /// the wheel, plus what you see on a Costa Rican street: the red taxi with its yellow triangle and the pick-up with sacks.
    /// </summary>
    public static partial class MapBuilder
    {
        private static readonly string[] DriverSkins = { "#F2C9A0", "#C68E5F", "#8D5A3A", "#E8B88A", "#6F4529" };
        private const string TaxiRed = "#D6322E";
        private const string TaxiYellow = "#FFD23F";
        private const string SackCloth = "#C9A66B";

        private static Sprite CarSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>The style of a car that was not given one: it depends on where it stands, so rebuilds give the same street.</summary>
        private static CarShape CarShapeAt(float x, float y)
        {
            return (CarShape)(Mathf.Abs(Mathf.RoundToInt(x * 3f + y * 5f)) % 4);
        }

        private static Transform Car(Transform parent, Transform shadows, float x, float y, string hex, float speed = 0f, bool tinted = false, CarShape? style = null)
        {
            SpriteFactory.EnsureCarsGenerated();
            var shape = style ?? CarShapeAt(x, y);
            var root = Root(parent, "Car", new Vector2(x, y), y - 0.55f);
            bool driving = speed != 0f;
            if (driving)
            {
                var shadowColor = GamePalette.Outline;
                shadowColor.a = 0.28f;
                OnGround(root, "Shadow", softCircle, new Vector2(0.06f, -0.48f), new Vector2(2.95f, 0.8f), shadowColor, TownShadow);
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
                GroundShadowRect(shadows, new Vector2(x + 0.08f, y - 0.44f), new Vector2(2.95f, 0.85f), 0.34f);
            }

            var size = new Vector2(SpriteFactory.CarWidth, SpriteFactory.CarHeight) / SpriteFactory.CarPixelsPerUnit;
            var middle = new Vector2(0f, SpriteFactory.CarCenterY);

            if (shape == CarShape.Pickup && !tinted)
            {
                BuildPickupLoad(root);
            }

            // Who sits behind the glass. Tinted glass hides them, and nobody sits in a parked car.
            if (!tinted)
            {
                float seatX = shape == CarShape.Sedan ? 0.14f : 0.18f;
                float seatY = shape == CarShape.Suv ? 0.36f : 0.26f;
                Shape(root, "Seat", roundedSmall, new Vector2(seatX - 0.14f, seatY), new Vector2(0.16f, 0.42f), GamePalette.Hex("#2A3345"), 1);
                if (driving)
                {
                    var skin = GamePalette.Hex(DriverSkins[Mathf.Abs(Mathf.RoundToInt(x * 7f + y * 3f)) % DriverSkins.Length]);
                    Shape(root, "DriverBody", roundedSmall, new Vector2(seatX, seatY - 0.06f), new Vector2(0.26f, 0.26f), GamePalette.Hex("#35405A"), 1);
                    Shape(root, "DriverHead", circle, new Vector2(seatX + 0.02f, seatY + 0.12f), Vector2.one * 0.2f, skin, 2);
                    Shape(root, "DriverHair", circle, new Vector2(seatX, seatY + 0.17f), new Vector2(0.2f, 0.13f), GamePalette.Hex("#2B2118"), 2);
                }
            }

            var glassColor = tinted ? GamePalette.Hex("#2A2E3A") : Color.white;
            Shape(root, "Glass", CarSprite(SpriteFactory.CarGlassPath(shape)), middle, size, glassColor, 3);
            Shape(root, "Body", CarSprite(SpriteFactory.CarBodyPath(shape)), middle, size, GamePalette.Hex(hex), 4);

            // The mirror at the foot of the windshield, the lights at the corners.
            float mirrorX = shape == CarShape.Sedan ? 0.54f : shape == CarShape.Suv ? 0.64f : 0.6f;
            float belt = shape == CarShape.Suv ? 0.3f : 0.22f;
            Shape(root, "Mirror_Out", roundedSmall, new Vector2(mirrorX, belt), new Vector2(0.15f, 0.12f), GamePalette.Outline, 5);
            Shape(root, "Mirror", roundedSmall, new Vector2(mirrorX, belt), new Vector2(0.1f, 0.07f), GamePalette.Shade(GamePalette.Hex(hex), 0.8f), 6);
            float nose = shape == CarShape.Suv ? 1.29f : 1.3f;
            float tail = shape == CarShape.Pickup ? -1.35f : -1.31f;
            Shape(root, "Headlight_Out", roundedSmall, new Vector2(nose, 0f), new Vector2(0.19f, 0.17f), GamePalette.Outline, 5);
            Shape(root, "Headlight", roundedSmall, new Vector2(nose, 0f), new Vector2(0.12f, 0.1f), GamePalette.Hex("#FFF0A8"), 6);
            Shape(root, "Taillight_Out", roundedSmall, new Vector2(tail, 0.03f), new Vector2(0.15f, 0.2f), GamePalette.Outline, 5);
            Shape(root, "Taillight", roundedSmall, new Vector2(tail, 0.03f), new Vector2(0.08f, 0.13f), GamePalette.Hex("#E8323A"), 6);

            var wheel = CarSprite(SpriteFactory.CarWheelPath);
            foreach (float wx in new[] { -SpriteFactory.WheelX, SpriteFactory.WheelX })
            {
                var tire = Shape(root, "Wheel", wheel, new Vector2(wx, SpriteFactory.WheelY), Vector2.one * SpriteFactory.WheelSize, Color.white, 7);
                if (driving)
                {
                    tire.gameObject.AddComponent<WheelSpin>().Configure(SpriteFactory.WheelSize * 0.5f);
                }
            }

            if (!driving)
            {
                var collider = root.gameObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(2.5f, 1.1f);
            }

            return root;
        }

        /// <summary>Coffee sacks in the bed of a pick-up, tied and stacked over the rail.</summary>
        private static void BuildPickupLoad(Transform root)
        {
            var cloth = GamePalette.Hex(SackCloth);
            foreach (var sack in new[] { new Vector3(-1.08f, 0.32f, 0.44f), new Vector3(-0.68f, 0.33f, 0.46f), new Vector3(-0.88f, 0.54f, 0.42f) })
            {
                var at = new Vector2(sack.x, sack.y);
                Shape(root, "Sack_Out", roundedSmall, at, new Vector2(sack.z + 0.07f, 0.31f), GamePalette.Outline, 0);
                Shape(root, "Sack", roundedSmall, at, new Vector2(sack.z, 0.24f), cloth, 1);
                Shape(root, "Sack_Tie", square, at, new Vector2(0.03f, 0.24f), GamePalette.Shade(cloth, 0.68f), 2);
            }
        }

        /// <summary>A red taxi, as in Costa Rica: the yellow triangle on the door and a light on the roof.</summary>
        private static Transform Taxi(Transform parent, Transform shadows, float x, float y, float speed)
        {
            var root = Car(parent, shadows, x, y, TaxiRed, speed, false, CarShape.Sedan);
            var yellow = GamePalette.Hex(TaxiYellow);
            if (triangle != null)
            {
                Shape(root, "TaxiTriangle", triangle, new Vector2(0.18f, -0.07f), new Vector2(0.3f, 0.26f), yellow, 5);
            }

            Shape(root, "TaxiLight_Out", roundedSmall, new Vector2(-0.08f, 0.68f), new Vector2(0.42f, 0.2f), GamePalette.Outline, 2);
            Shape(root, "TaxiLight", roundedSmall, new Vector2(-0.08f, 0.68f), new Vector2(0.34f, 0.12f), yellow, 3);
            return root;
        }
    }
}
