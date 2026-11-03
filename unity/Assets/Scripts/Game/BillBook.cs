using System;
using System.Collections.Generic;
using ColonQuest.Finance;

namespace ColonQuest.Game
{
    /// <summary>The services of the home: each one sends one bill a month.</summary>
    public enum BillKind
    {
        Electricity,
        Water,
        Internet,
        Phone
    }

    /// <summary>What is owed for one service: the amount and how many month closes it went through unpaid.</summary>
    public sealed class BillDue
    {
        public BillKind Kind { get; }
        public long Amount { get; set; }
        public int MonthsLate { get; set; }

        public BillDue(BillKind kind, long amount, int monthsLate)
        {
            Kind = kind;
            Amount = amount;
            MonthsLate = monthsLate;
        }
    }

    /// <summary>
    /// The bills of the month. They arrive in the last week; whatever is unpaid when the month closes gets a late fee, and after
    /// two closes unpaid the service is cut until the bill is paid. Pure, so it can be tested; the fee is the finance engine's.
    /// </summary>
    public sealed class BillBook
    {
        /// <summary>How many month closes a bill can go through unpaid before the service is cut.</summary>
        public const int MonthsUntilCut = 2;

        private readonly List<BillDue> due = new List<BillDue>();

        public IReadOnlyList<BillDue> Due => due;

        /// <summary>True when this month's bills have arrived.</summary>
        public bool ArrivedThisMonth { get; private set; }

        /// <summary>The late fees that went on with the last closed month, to show in its report.</summary>
        public long LastLateFees { get; private set; }

        public long TotalDue
        {
            get
            {
                long total = 0L;
                foreach (var bill in due)
                {
                    total += bill.Amount;
                }

                return total;
            }
        }

        public bool IsCut(BillKind kind)
        {
            var bill = Find(kind);
            return bill != null && bill.MonthsLate >= MonthsUntilCut;
        }

        /// <summary>This month's bills arrive, once. A service still unpaid adds the new amount to what it owes.</summary>
        public void Arrive(Func<BillKind, long> amountOf)
        {
            if (ArrivedThisMonth)
            {
                return;
            }

            ArrivedThisMonth = true;
            foreach (BillKind kind in Enum.GetValues(typeof(BillKind)))
            {
                var bill = Find(kind);
                if (bill != null)
                {
                    bill.Amount += amountOf(kind);
                }
                else
                {
                    due.Add(new BillDue(kind, amountOf(kind), 0));
                }
            }
        }

        /// <summary>
        /// The month closes: every bill that arrived before now goes late. Bills that had not arrived come now, on time, because
        /// the player never had the chance to pay them. Returns the late fees added.
        /// </summary>
        public long CloseMonth(Func<BillKind, long> amountOf)
        {
            long fees = 0L;
            foreach (var bill in due)
            {
                long fee = FinanceEngine.CalculateBillLateFee(bill.Amount);
                bill.Amount += fee;
                bill.MonthsLate++;
                fees += fee;
            }

            Arrive(amountOf);
            ArrivedThisMonth = false;
            LastLateFees = fees;
            return fees;
        }

        /// <summary>Takes a paid bill out of the book.</summary>
        public void Settle(BillKind kind)
        {
            due.RemoveAll(bill => bill.Kind == kind);
        }

        public BillDue Find(BillKind kind)
        {
            return due.Find(bill => bill.Kind == kind);
        }

        /// <summary>Puts a bill back where a saved game left it.</summary>
        public void Restore(BillKind kind, long amount, int monthsLate)
        {
            due.Add(new BillDue(kind, amount, monthsLate));
        }

        public void RestoreArrived(bool arrived)
        {
            ArrivedThisMonth = arrived;
        }
    }
}
