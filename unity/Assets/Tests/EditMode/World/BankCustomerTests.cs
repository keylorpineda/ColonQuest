using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    public sealed class BankCustomerTests
    {
        /// <summary>The front of the teller counter: everything above it is the staff side.</summary>
        private const float CounterFront = 3.7f;

        /// <summary>The two ropes of the queue, from post to post (see BuildQueue in the map builder).</summary>
        private static readonly float[] Ropes = { 2.45f, 1.25f };
        private const float RopeHalfWidth = 3.6f;

        [Test]
        public void Errands_NeverGoBehindTheCounter()
        {
            foreach (var errand in BankCustomer.Errands())
            {
                foreach (var stop in errand)
                {
                    Assert.Less(stop.Point.y, CounterFront, stop.Point.ToString());
                }
            }
        }

        [Test]
        public void Errands_EnterAndLeaveTheQueueByItsOpenEnds()
        {
            foreach (var errand in BankCustomer.Errands())
            {
                Vector2 from = BankCustomer.Door;
                foreach (var stop in errand)
                {
                    Assert.IsFalse(CrossesARope(from, stop.Point), from + " to " + stop.Point);
                    from = stop.Point;
                }

                Assert.IsFalse(CrossesARope(from, BankCustomer.Door), from + " back to the door");
            }
        }

        [Test]
        public void Errands_ToAWindowWaitInFrontOfIt()
        {
            int windows = 0;
            foreach (var errand in BankCustomer.Errands())
            {
                foreach (var stop in errand)
                {
                    if (stop.Point == BankCustomer.Window2 || stop.Point == BankCustomer.Window4)
                    {
                        windows++;
                        Assert.Greater(stop.MaxSeconds, 0f, "a customer stays a while at the window");
                    }
                }
            }

            Assert.GreaterOrEqual(windows, 3);
        }

        private static bool CrossesARope(Vector2 a, Vector2 b)
        {
            foreach (float y in Ropes)
            {
                if ((a.y - y) * (b.y - y) >= 0f)
                {
                    continue;
                }

                float x = Mathf.Lerp(a.x, b.x, (y - a.y) / (b.y - a.y));
                if (Mathf.Abs(x) <= RopeHalfWidth + 0.3f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
