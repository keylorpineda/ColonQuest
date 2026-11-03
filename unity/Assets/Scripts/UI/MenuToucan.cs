using ColonQuest.Game;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The toucan of the main menu, cut from the key art into three pieces (body, head with the bill and the banknote,
    /// and the wing) that sit on the picture without the bird (<c>KeyArt_Plate</c>). When the menu opens it flies in from
    /// the top right beating its wings, brakes and lands on its branch; then it keeps looking around with quick turns of
    /// the head and pauses, as birds do, and now and then ruffles its wing, hops or blinks. The head always moves on a
    /// spring, so it lags, nods when the bird lands and never jumps. Every piece starts where the drawing had it, so the
    /// perched toucan looks like the original picture. There is nothing drawn over the picture: the bird is part of it.
    /// </summary>
    public sealed class MenuToucan : MonoBehaviour
    {
        // Where the pieces were in the 2048 x 1152 key art (see the sprites in Resources/Menu), in its pixels.
        private const float SourceWidth = 2048f;
        private const float SourceHeight = 1152f;
        private static readonly Rect BodyPx = new Rect(1571f, 614f, 416f, 529f);
        private static readonly Rect HeadPx = new Rect(1505f, 536f, 357f, 235f);
        private static readonly Rect WingPx = new Rect(1712f, 700f, 255f, 269f);
        private static readonly Vector2 PerchPx = new Vector2(1765f, 990f);
        private static readonly Vector2 NeckPx = new Vector2(1790f, 668f);
        private static readonly Vector2 ShoulderPx = new Vector2(1800f, 725f);
        private static readonly Vector2 EyePx = new Vector2(1761f, 590f);
        private const float EyeDiameterPx = 30f;
        private static readonly Color EyeSkin = new Color(0.65f, 0.82f, 0.45f, 1f);

        private const float FlightSeconds = 1.9f;

        // Positive turns the bill down and negative lifts it, in degrees. Past these the cut of the neck would show.
        private const float HeadLookUp = -7f;
        private const float HeadLookDown = 6f;

        private RectTransform root;
        private RectTransform headPivot;
        private RectTransform wingPivot;
        private RectTransform wingImage;
        private RectTransform lid;
        private float scale;
        private Vector2 perch;

        private bool arriving;
        private bool perched;
        private float arrivalStarted;
        private float landedAt = -10f;
        private float nextAction;
        private int action;
        private float actionStarted = -10f;
        private float nextBlink;
        private float blinkStarted = -10f;
        private Vector2 neckRest;
        private float headAngle;
        private float headVelocity;
        private float lookTarget;
        private float nextLook;
        private float lastUpdate;

        /// <summary>True once the bird is on its branch.</summary>
        public bool Perched => perched;

        /// <summary>Builds the bird inside the art rectangle, hidden until <see cref="Arrive"/> is called.</summary>
        public static MenuToucan Create(RectTransform art, float artWidth, float artHeight)
        {
            var body = Resources.Load<Sprite>("Menu/Toucan_Body");
            var head = Resources.Load<Sprite>("Menu/Toucan_Head");
            var wing = Resources.Load<Sprite>("Menu/Toucan_Wing");
            if (body == null || head == null || wing == null)
            {
                return null;
            }

            var go = new GameObject("Toucan", typeof(RectTransform));
            go.transform.SetParent(art, false);
            var toucan = go.AddComponent<MenuToucan>();
            toucan.Build(art, artWidth / SourceWidth, body, head, wing);
            return toucan;
        }

        /// <summary>Starts the flight in. Calling it again while it flies or after it landed does nothing.</summary>
        public void Arrive()
        {
            if (arriving || perched)
            {
                return;
            }

            arriving = true;
            arrivalStarted = Time.unscaledTime;
            lastUpdate = arrivalStarted;
            root.gameObject.SetActive(true);
        }

        /// <summary>Puts the bird straight on its branch, for code (and tests) that do not want to wait for the flight.</summary>
        public void LandNow()
        {
            arriving = false;
            perched = true;
            landedAt = Time.unscaledTime - 1f;
            lastUpdate = Time.unscaledTime;
            root.gameObject.SetActive(true);
            ResetPose();
            ScheduleNext(Time.unscaledTime);
        }

        /// <summary>A pixel of the key art (top left origin) in the art rectangle (center origin, y up).</summary>
        private Vector2 ArtPoint(Vector2 px)
        {
            return new Vector2((px.x - SourceWidth * 0.5f) * scale, (SourceHeight * 0.5f - px.y) * scale);
        }

        private void Build(RectTransform art, float pixelScale, Sprite body, Sprite head, Sprite wing)
        {
            scale = pixelScale;
            root = (RectTransform)transform;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Vector2.zero;
            perch = ArtPoint(PerchPx);
            root.anchoredPosition = perch;

            Piece("Body", body, BodyPx, root, PerchPx);

            wingPivot = Pivot("WingPivot", root, ShoulderPx, PerchPx);
            wingImage = Piece("Wing", wing, WingPx, wingPivot, ShoulderPx);

            headPivot = Pivot("HeadPivot", root, NeckPx, PerchPx);
            neckRest = headPivot.anchoredPosition;
            Piece("Head", head, HeadPx, headPivot, NeckPx);

            // The eyelid: a disc of the skin around the eye that closes from the top for a blink.
            var eyeLocal = (ArtPoint(EyePx) - ArtPoint(NeckPx));
            lid = UiKit.Rect(headPivot, "Lid", Vector2.one * EyeDiameterPx * scale, new Vector2(0.5f, 0.5f), eyeLocal + new Vector2(0f, EyeDiameterPx * scale * 0.5f));
            lid.pivot = new Vector2(0.5f, 1f);
            var lidImage = lid.gameObject.AddComponent<Image>();
            lidImage.sprite = UiKit.Disc;
            lidImage.color = EyeSkin;
            lidImage.raycastTarget = false;
            lid.localScale = new Vector3(1f, 0f, 1f);

            root.gameObject.SetActive(false);
        }

        private RectTransform Pivot(string name, RectTransform parent, Vector2 atPx, Vector2 parentPx)
        {
            var pivot = UiKit.Rect(parent, name, Vector2.zero, new Vector2(0.5f, 0.5f), ArtPoint(atPx) - ArtPoint(parentPx));
            return pivot;
        }

        private RectTransform Piece(string name, Sprite sprite, Rect px, RectTransform parent, Vector2 parentPx)
        {
            var center = new Vector2(px.x + px.width * 0.5f, px.y + px.height * 0.5f);
            var rect = UiKit.Rect(parent, name, new Vector2(px.width, px.height) * scale, new Vector2(0.5f, 0.5f), ArtPoint(center) - ArtPoint(parentPx));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return rect;
        }

        private void Update()
        {
            if (root == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (arriving)
            {
                Fly(now);
                return;
            }

            if (perched)
            {
                Idle(now);
            }
        }

        /// <summary>From off the top right of the picture to the branch, beating the wings and braking at the end.</summary>
        private void Fly(float now)
        {
            float t = (now - arrivalStarted) / FlightSeconds;
            if (t >= 1f)
            {
                arriving = false;
                perched = true;
                landedAt = now;

                // The body stops on the branch and the head carries on: it nods and comes back on its spring.
                float carried = headAngle;
                float carriedVelocity = headVelocity;
                ResetPose();
                headAngle = carried;
                headVelocity = carriedVelocity + 70f;
                headPivot.localRotation = Quaternion.Euler(0f, 0f, headAngle);
                nextLook = now + 1.1f;
                ScheduleNext(now);
                return;
            }

            float eased = MenuMotion.GlideEase(t);
            var start = perch + new Vector2(760f, 640f) * scale;
            var bend1 = start + new Vector2(-380f, -60f) * scale;
            var bend2 = perch + new Vector2(260f, 260f) * scale;
            root.anchoredPosition = MenuMotion.Bezier(start, bend1, bend2, perch, eased);

            // Smaller far away, the body nose down while it glides and level as it brakes.
            float size = Mathf.Lerp(0.72f, 1f, eased);
            root.localScale = new Vector3(size, size, 1f);
            float pitch = Mathf.Lerp(16f, 0f, eased);
            root.localRotation = Quaternion.Euler(0f, 0f, pitch);

            // Birds keep the head steady while the body pitches and beats. Halfway it looks up at where it goes, then
            // down at the branch, and each beat of the wings bobs it a little.
            float seconds = now - arrivalStarted;
            float look = -10f * MenuMotion.Pulse(Mathf.InverseLerp(0.22f, 0.58f, t)) + 8f * MenuMotion.Pulse(Mathf.InverseLerp(0.58f, 1f, t));
            float bob = Mathf.Sin(seconds * 5.5f * Mathf.PI * 2f + 2f) * 2.5f * (1f - Mathf.InverseLerp(0.8f, 1f, t));
            MoveHead(-pitch * 0.8f + look + bob, 110f, 12f, now);

            // Fast beats in the air; in the last fifth the wing opens wide to brake and then folds.
            float flap = MenuMotion.FlapAngle(now - arrivalStarted, 5.5f, 38f, 42f);
            float brake = Mathf.InverseLerp(0.8f, 1f, t);
            float angle = Mathf.Lerp(flap, 70f * (1f - brake), brake);
            wingPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            float beat = Mathf.Cos((now - arrivalStarted) * 5.5f * Mathf.PI * 2f);
            wingImage.localScale = new Vector3(1f, Mathf.Lerp(0.75f + 0.25f * beat, 1f, brake), 1f);
        }

        /// <summary>Takes the head toward an angle on its spring; the neck stretches a little as the head turns.</summary>
        private void MoveHead(float target, float stiffness, float damping, float now)
        {
            // Long frames are cut so that a hiccup never throws the spring.
            float delta = Mathf.Min(now - lastUpdate, 0.05f);
            lastUpdate = now;
            MenuMotion.SpringStep(ref headAngle, ref headVelocity, target, stiffness, damping, delta);
            headPivot.localRotation = Quaternion.Euler(0f, 0f, headAngle);
            headPivot.anchoredPosition = neckRest + new Vector2(-headAngle * 0.35f, Mathf.Sin(now * 1.8f + 0.8f) * 1.6f) * scale;
        }

        /// <summary>A quick turn to somewhere new and then a pause; sometimes it blinks as it turns, as birds do.</summary>
        private void LookAround(float now)
        {
            if (now < nextLook)
            {
                return;
            }

            // A clear turn up or down, or back to the pose of the drawing: small turns would not read at this size.
            float pick = Random.value;
            lookTarget = pick < 0.25f ? 0f : pick < 0.65f ? Random.Range(HeadLookUp, -3f) : Random.Range(2.5f, HeadLookDown);
            nextLook = now + Random.Range(0.7f, 2.6f);
            if (Random.value < 0.35f)
            {
                nextBlink = now;
            }
        }

        /// <summary>On the branch: it breathes, looks around and, every few seconds, does one small thing.</summary>
        private void Idle(float now)
        {
            float squash = MenuMotion.LandingSquash(now - landedAt);
            float breath = 1f + Mathf.Sin(now * 1.8f) * 0.006f;
            float hop = 0f;
            float wingAngle = 0f;

            float since = now - actionStarted;
            switch (action)
            {
                case 2:
                    // Ruffles the wing: two small beats.
                    wingAngle = since < 0.7f ? Mathf.Abs(Mathf.Sin(since * Mathf.PI * 2f / 0.35f)) * 24f : 0f;
                    break;
                case 3:
                    // A little hop on the branch, landing with a squash.
                    hop = since < 0.32f ? MenuMotion.Pulse(since / 0.32f) * 22f * scale : 0f;
                    if (since >= 0.32f)
                    {
                        squash *= MenuMotion.LandingSquash(since - 0.32f);
                    }

                    break;
            }

            if (now >= nextAction)
            {
                actionStarted = now;
                action = 2 + Random.Range(0, 2);
                if (action == 3)
                {
                    // The hop throws the head up, and it comes back on its spring.
                    headVelocity -= 45f;
                }

                ScheduleNext(now);
            }

            LookAround(now);

            root.anchoredPosition = perch + new Vector2(0f, hop);
            root.localScale = new Vector3(1f / squash, squash * breath, 1f);
            root.localRotation = Quaternion.identity;
            MoveHead(lookTarget + MenuMotion.HeadDrift(now), 140f, 15f, now);
            wingPivot.localRotation = Quaternion.Euler(0f, 0f, wingAngle);
            wingImage.localScale = Vector3.one;
            Blink(now);
        }

        private void Blink(float now)
        {
            if (now >= nextBlink)
            {
                blinkStarted = now;
                nextBlink = now + Random.Range(2.5f, 5.5f);
            }

            float closed = MenuMotion.Pulse((now - blinkStarted) / 0.18f);
            lid.localScale = new Vector3(1f, closed, 1f);
        }

        private void ScheduleNext(float now)
        {
            nextAction = now + Random.Range(3.5f, 7f);
            if (nextBlink < now)
            {
                nextBlink = now + Random.Range(1.5f, 3f);
            }
        }

        private void ResetPose()
        {
            root.anchoredPosition = perch;
            root.localScale = Vector3.one;
            root.localRotation = Quaternion.identity;
            headPivot.localRotation = Quaternion.identity;
            headPivot.anchoredPosition = neckRest;
            headAngle = 0f;
            headVelocity = 0f;
            lookTarget = 0f;
            wingPivot.localRotation = Quaternion.identity;
            wingImage.localScale = Vector3.one;
            lid.localScale = new Vector3(1f, 0f, 1f);
        }
    }
}
