using System;
using System.Collections.Generic;
using ColonQuest.Finance;

namespace ColonQuest.Game
{
    public enum GoalKind
    {
        EmergencyFund,
        PayDownCard,
        FurnishRoom,
        Savings
    }

    /// <summary>One goal of the run: what it asks for and whether it was already reached. A goal reached stays reached.</summary>
    public sealed class Goal
    {
        public GoalKind Kind { get; }

        /// <summary>The number to reach. For the card, the balance to get down to.</summary>
        public long Target { get; }

        /// <summary>Where it started, so the card's progress can be measured.</summary>
        public long Start { get; }

        public bool Done { get; set; }

        public Goal(GoalKind kind, long target, long start, bool done = false)
        {
            Kind = kind;
            Target = target;
            Start = start;
            Done = done;
        }
    }

    /// <summary>
    /// The three goals the phone proposes at the start, chosen from the player's situation. Pure, so it can be tested. It only
    /// reads the numbers: the money moves in the finance engine.
    /// </summary>
    public sealed class GoalBook
    {
        public const long FundTarget = 100_000L;
        public const long SavingsTarget = 150_000L;
        public const int RoomPieces = 4;

        private readonly List<Goal> goals = new List<Goal>();

        public IReadOnlyList<Goal> Goals => goals;

        public static GoalBook Propose(PlayerState state)
        {
            var book = new GoalBook();
            book.goals.Add(new Goal(GoalKind.EmergencyFund, FundTarget, state.EmergencyFund));

            // Half the card is the goal for a short run: all of it would take more months than the game has.
            long card = state.CreditCard.Balance;
            book.goals.Add(card > 0L
                ? new Goal(GoalKind.PayDownCard, card / 2L, card)
                : new Goal(GoalKind.Savings, SavingsTarget, state.Savings));

            book.goals.Add(new Goal(GoalKind.FurnishRoom, RoomPieces, 0L));
            return book;
        }

        /// <summary>Puts a goal back where a saved game left it.</summary>
        public void Restore(Goal goal)
        {
            goals.Add(goal);
        }

        /// <summary>How far a goal is, from 0 to 1.</summary>
        public static float Progress(Goal goal, PlayerState state, Holdings holdings)
        {
            if (goal.Done)
            {
                return 1f;
            }

            switch (goal.Kind)
            {
                case GoalKind.EmergencyFund:
                    return Share(state.EmergencyFund, goal.Target);
                case GoalKind.Savings:
                    return Share(state.Savings, goal.Target);
                case GoalKind.FurnishRoom:
                    return Share(Pieces(holdings), goal.Target);
                default:
                    return Share(goal.Start - state.CreditCard.Balance, goal.Start - goal.Target);
            }
        }

        /// <summary>The goals reached since the last look: they are marked done and returned, to celebrate them once.</summary>
        public List<Goal> TakeNewlyDone(PlayerState state, Holdings holdings)
        {
            var reached = new List<Goal>();
            foreach (var goal in goals)
            {
                if (!goal.Done && Progress(goal, state, holdings) >= 1f)
                {
                    goal.Done = true;
                    reached.Add(goal);
                }
            }

            return reached;
        }

        private static long Pieces(Holdings holdings)
        {
            long count = 0L;
            foreach (var unused in holdings.Furniture)
            {
                count++;
            }

            return count;
        }

        private static float Share(long value, long target)
        {
            return target <= 0L ? 1f : Math.Max(0f, Math.Min(1f, (float)value / target));
        }
    }
}
