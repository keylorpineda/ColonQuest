using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A small scene made of steps, like the actions of The Sims: the player walks to the place, turns, takes a pose, a sound
    /// plays, an object or a person reacts, a line is said, and only at the end <see cref="Apply"/> runs the action that
    /// changes the game. The money is always moved by the finance engine inside that action, never by the animation.
    /// Space skips the rest of the scene: every step turns instant, but the apply steps still run.
    /// </summary>
    public sealed class Interaction
    {
        private enum Kind
        {
            WalkTo,
            Face,
            Pose,
            Say,
            Sound,
            Fly,
            Steam,
            Zzz,
            Flicker,
            Fade,
            Wait,
            NpcPose,
            Wobble,
            Apply
        }

        private struct Step
        {
            public Kind Kind;
            public Vector2 Point;
            public Vector2 To;
            public float Seconds;
            public PersonPose Pose;
            public string Name;
            public string Text;
            public SoundEffectKind Sound;
            public Color Color;
            public bool Through;
            public Action Action;
        }

        private readonly List<Step> steps = new List<Step>();

        /// <summary>Raised with the speaker and the line. The speaker is empty when it is the player's own thought.</summary>
        public static event Action<string, string> Spoke;

        /// <summary>True while a scene plays. Nothing else should start meanwhile.</summary>
        public static bool Playing { get; private set; }

        public int Count => steps.Count;

        /// <summary>How many seconds the steps that wait add up to, without counting the walk.</summary>
        public float WaitSeconds
        {
            get
            {
                float total = 0f;
                foreach (var step in steps)
                {
                    if (step.Kind == Kind.Pose || step.Kind == Kind.Wait || step.Kind == Kind.Fade)
                    {
                        total += step.Seconds;
                    }
                }

                return total;
            }
        }

        /// <summary>How many steps change the game (the apply steps).</summary>
        public int ApplyCount
        {
            get
            {
                int count = 0;
                foreach (var step in steps)
                {
                    count += step.Kind == Kind.Apply ? 1 : 0;
                }

                return count;
            }
        }

        /// <summary>Walks the player to a point and waits until there.</summary>
        public Interaction WalkTo(Vector2 point)
        {
            return Add(new Step { Kind = Kind.WalkTo, Point = point });
        }

        /// <summary>Like <see cref="WalkTo"/> but the player can walk over furniture, to lie in a bed or sit on a sofa.</summary>
        public Interaction WalkInto(Vector2 point)
        {
            return Add(new Step { Kind = Kind.WalkTo, Point = point, Through = true });
        }

        /// <summary>Turns the player toward a point.</summary>
        public Interaction Face(Vector2 point)
        {
            return Add(new Step { Kind = Kind.Face, Point = point });
        }

        /// <summary>Takes a pose and holds it for some seconds. The pose stays until the next pose step or the end of the scene.</summary>
        public Interaction Pose(PersonPose pose, float seconds)
        {
            return Add(new Step { Kind = Kind.Pose, Pose = pose, Seconds = seconds });
        }

        /// <summary>A line of a person (the speaker) or of the player's own thoughts (an empty speaker). It does not wait.</summary>
        public Interaction Say(string speaker, string text)
        {
            return Add(new Step { Kind = Kind.Say, Name = speaker ?? string.Empty, Text = text });
        }

        public Interaction Sound(SoundEffectKind kind)
        {
            return Add(new Step { Kind = Kind.Sound, Sound = kind });
        }

        /// <summary>An object that flies from a point to another: bills, a card, a receipt, a plate. It does not wait.</summary>
        public Interaction Fly(string prop, Vector2 from, Vector2 to, float seconds)
        {
            return Add(new Step { Kind = Kind.Fly, Name = prop, Point = from, To = to, Seconds = seconds });
        }

        /// <summary>Puffs of steam that rise from a point. It does not wait.</summary>
        public Interaction Steam(Vector2 at, float seconds)
        {
            return Add(new Step { Kind = Kind.Steam, Point = at, Seconds = seconds });
        }

        /// <summary>Z letters that float up from a point. It does not wait.</summary>
        public Interaction Zzz(Vector2 at, float seconds)
        {
            return Add(new Step { Kind = Kind.Zzz, Point = at, Seconds = seconds });
        }

        /// <summary>A screen that flickers with a color, such as a television. It does not wait.</summary>
        public Interaction Flicker(Vector2 at, Vector2 size, Color color, float seconds)
        {
            return Add(new Step { Kind = Kind.Flicker, Point = at, To = size, Color = color, Seconds = seconds });
        }

        /// <summary>The whole screen goes to a color with an alpha (0 clear, 1 solid) in some seconds, and waits.</summary>
        public Interaction Fade(Color color, float seconds)
        {
            return Add(new Step { Kind = Kind.Fade, Color = color, Seconds = seconds });
        }

        public Interaction Wait(float seconds)
        {
            return Add(new Step { Kind = Kind.Wait, Seconds = seconds });
        }

        /// <summary>Makes another person (found by the name of its object) take a pose for some seconds. It does not wait.</summary>
        public Interaction NpcPose(string person, PersonPose pose, float seconds)
        {
            return Add(new Step { Kind = Kind.NpcPose, Name = person, Pose = pose, Seconds = seconds });
        }

        /// <summary>Makes an object (found by name) shake for some seconds: a counter, a door. It does not wait.</summary>
        public Interaction Wobble(string objectName, float seconds)
        {
            return Add(new Step { Kind = Kind.Wobble, Name = objectName, Seconds = seconds });
        }

        /// <summary>The moment the game changes. It runs even if the player skips the scene.</summary>
        public Interaction Apply(Action action)
        {
            return Add(new Step { Kind = Kind.Apply, Action = action });
        }

        private Interaction Add(Step step)
        {
            steps.Add(step);
            return this;
        }

        /// <summary>
        /// Plays the scene. <paramref name="finished"/> runs after the last step, when the player is free again. Nothing plays
        /// if another scene is already playing; the action of the apply steps then never runs, so the caller falls back to it.
        /// </summary>
        public bool Play(Action finished = null)
        {
            if (Playing || steps.Count == 0)
            {
                return false;
            }

            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                return false;
            }

            var host = new GameObject("Interaction").AddComponent<InteractionRunner>();
            host.StartCoroutine(host.Run(steps, player, finished));
            return true;
        }

        internal static void SetPlaying(bool value)
        {
            Playing = value;
        }

        internal static void Speak(string speaker, string text)
        {
            Spoke?.Invoke(speaker, text);
        }

        private sealed class InteractionRunner : MonoBehaviour
        {
            private const float WalkSpeed = 3.4f;
            private const float WalkTimeout = 4f;

            private bool skip;

            public IEnumerator Run(List<Step> script, GameObject player, Action finished)
            {
                SetPlaying(true);
                PlayerController.Frozen = true;

                var body = player.GetComponent<Rigidbody2D>();
                var collider = player.GetComponent<Collider2D>();
                var controller = player.GetComponent<PlayerController>();
                var bob = player.transform.Find("Bob");
                var animator = bob != null ? bob.GetComponent<PlayerAnimator>() : null;
                bool through = false;

                try
                {
                    foreach (var step in script)
                    {
                        CheckSkip();
                        switch (step.Kind)
                        {
                            case Kind.WalkTo:
                                if (step.Through && collider != null)
                                {
                                    collider.enabled = false;
                                    through = true;
                                }

                                yield return Walk(body, controller, step.Point);
                                break;

                            case Kind.Face:
                                if (controller != null)
                                {
                                    controller.Face(step.Point.x >= player.transform.position.x ? 1 : -1);
                                }

                                break;

                            case Kind.Pose:
                                if (animator != null)
                                {
                                    animator.SetPose(step.Pose);
                                }

                                yield return Hold(step.Seconds);
                                break;

                            case Kind.Say:
                                Speak(step.Name, step.Text);
                                break;

                            case Kind.Sound:
                                AudioDirector.Effect(step.Sound);
                                break;

                            case Kind.Fly:
                                InteractionProps.Fly(step.Name, step.Point, step.To, step.Seconds);
                                break;

                            case Kind.Steam:
                                InteractionProps.Steam(step.Point, step.Seconds);
                                break;

                            case Kind.Zzz:
                                InteractionProps.Zzz(step.Point, step.Seconds);
                                break;

                            case Kind.Flicker:
                                InteractionProps.Flicker(step.Point, step.To, step.Color, step.Seconds);
                                break;

                            case Kind.Fade:
                                yield return Fade(step.Color, step.Seconds);
                                break;

                            case Kind.Wait:
                                yield return Hold(step.Seconds);
                                break;

                            case Kind.NpcPose:
                                SetActorPose(step.Name, step.Pose, step.Seconds);
                                break;

                            case Kind.Wobble:
                                StartCoroutine(Wobble(step.Name, step.Seconds));
                                break;

                            case Kind.Apply:
                                step.Action?.Invoke();
                                break;
                        }
                    }
                }
                finally
                {
                    if (body != null)
                    {
                        body.linearVelocity = Vector2.zero;
                    }

                    if (through && collider != null)
                    {
                        collider.enabled = true;
                    }

                    if (animator != null)
                    {
                        animator.SetPose(PersonPose.None);
                    }

                    InteractionProps.ClearOverlay();
                    PlayerController.Frozen = false;
                    SetPlaying(false);
                }

                finished?.Invoke();
                Destroy(gameObject);
            }

            private void CheckSkip()
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    skip = true;
                }
            }

            private IEnumerator Walk(Rigidbody2D body, PlayerController controller, Vector2 target)
            {
                if (body == null)
                {
                    yield break;
                }

                for (float t = 0f; t < WalkTimeout && !skip; t += Time.deltaTime)
                {
                    Vector2 toTarget = target - body.position;
                    if (toTarget.magnitude < 0.12f)
                    {
                        break;
                    }

                    body.linearVelocity = toTarget.normalized * WalkSpeed;
                    if (controller != null && Mathf.Abs(toTarget.x) > 0.05f)
                    {
                        controller.Face(toTarget.x >= 0f ? 1 : -1);
                    }

                    CheckSkip();
                    yield return null;
                }

                body.linearVelocity = Vector2.zero;
                if (skip || (target - body.position).magnitude >= 0.12f)
                {
                    // Skipped, or something blocked the way: the player ends where the scene meant.
                    body.position = target;
                    body.transform.position = target;
                }
            }

            private IEnumerator Hold(float seconds)
            {
                for (float t = 0f; t < seconds && !skip; t += Time.deltaTime)
                {
                    CheckSkip();
                    yield return null;
                }
            }

            private IEnumerator Fade(Color color, float seconds)
            {
                if (skip || seconds <= 0f)
                {
                    InteractionProps.SetOverlay(color);
                    yield break;
                }

                Color start = InteractionProps.OverlayColor;
                for (float t = 0f; t < seconds && !skip; t += Time.deltaTime)
                {
                    InteractionProps.SetOverlay(Color.Lerp(start, color, t / seconds));
                    CheckSkip();
                    yield return null;
                }

                InteractionProps.SetOverlay(color);
            }

            private static void SetActorPose(string person, PersonPose pose, float seconds)
            {
                var actor = GameObject.Find(person);
                var bob = actor != null ? actor.transform.Find("Bob") : null;
                var animator = bob != null ? bob.GetComponent<PlayerAnimator>() : null;
                if (animator != null)
                {
                    animator.SetPose(pose, seconds);
                }
            }

            private IEnumerator Wobble(string objectName, float seconds)
            {
                var target = GameObject.Find(objectName);
                if (target == null)
                {
                    yield break;
                }

                Vector3 rest = target.transform.localPosition;
                for (float t = 0f; t < seconds; t += Time.deltaTime)
                {
                    if (target == null)
                    {
                        yield break;
                    }

                    target.transform.localPosition = rest + new Vector3(Mathf.Sin(t * 60f) * 0.035f, 0f, 0f);
                    yield return null;
                }

                if (target != null)
                {
                    target.transform.localPosition = rest;
                }
            }
        }
    }
}
