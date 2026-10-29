using ColonQuest.Finance;
using ColonQuest.World;

namespace ColonQuest.Game
{
    /// <summary>
    /// Picks what an everyday object says when the player uses it. Lines rotate with each use and may
    /// mention the player's money with {cash}, {savings}, {card} or {salary}; the amounts come from the
    /// state, never from the text. Pure C#, tested in EditMode.
    /// </summary>
    public static class ThingLines
    {
        public static string Pick(string[] lines, int use, PlayerState state)
        {
            if (lines == null || lines.Length == 0)
            {
                return string.Empty;
            }

            int index = ((use % lines.Length) + lines.Length) % lines.Length;
            string line = Loc.T(lines[index] ?? string.Empty);
            if (state == null)
            {
                return line;
            }

            return line
                .Replace("{cash}", Money.Format(state.Cash))
                .Replace("{savings}", Money.Format(state.Savings + state.EmergencyFund))
                .Replace("{card}", Money.Format(state.CreditCard.Balance))
                .Replace("{salary}", Money.Format(state.SalaryNet));
        }
    }
}
