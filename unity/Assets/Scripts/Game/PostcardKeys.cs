namespace ColonQuest.Game
{
    /// <summary>
    /// The three illustrations of the postcard at the end of the run, named as the backend pregenerates them
    /// (<c>scripts/pregenerate_images.py</c>). Which one is shown depends only on how much the player owes at the end.
    /// The pictures carry no text or numbers: the game writes the figures over them.
    /// </summary>
    public static class PostcardKeys
    {
        public const string DebtFree = "card_year_debt_free";
        public const string SomeDebt = "card_year_some_debt";
        public const string HeavyDebt = "card_year_heavy_debt";

        /// <summary>Debts from this many colones up (about two salaries) are a heavy load, not a small one.</summary>
        public const long HeavyDebtColones = 900_000L;

        public static readonly string[] All = { DebtFree, SomeDebt, HeavyDebt };

        public static string For(long totalDebt)
        {
            if (totalDebt <= 0L)
            {
                return DebtFree;
            }

            return totalDebt >= HeavyDebtColones ? HeavyDebt : SomeDebt;
        }
    }
}
