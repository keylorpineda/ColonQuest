using System;
using System.Collections.Generic;
using ColonQuest.Finance;

namespace ColonQuest.Game
{
    /// <summary>
    /// The lines for the end of the run about what the player bought: the home, the car and the farm. The farm
    /// is shown as a projection at real time, since the run is only a few months long. It builds text only and
    /// moves no money.
    /// </summary>
    public static class AssetsReport
    {
        /// <summary>One line per thing owned, then the warning about the figures. Empty when the player bought nothing.</summary>
        public static IReadOnlyList<string> Build(Holdings holdings, int monthsPlayed)
        {
            var lines = new List<string>();
            if (holdings == null || holdings.IsEmpty)
            {
                return lines;
            }

            AddHome(lines, holdings.Home, monthsPlayed);
            AddCar(lines, holdings.Car);
            foreach (var stake in holdings.Farm)
            {
                AddFarm(lines, stake, monthsPlayed);
            }

            lines.Add(MarketAssumptions.Disclaimer);
            return lines;
        }

        private static void AddHome(List<string> lines, HomeHolding home, int monthsPlayed)
        {
            if (home == null)
            {
                return;
            }

            if (home.Owned)
            {
                int left = Math.Max(0, home.Months - monthsPlayed);
                lines.Add($"Casa: compraste {home.Name} con hipoteca. Pagas {Money.Format(home.Monthly)} al mes y todavía faltan {left} cuotas. Valor de ejemplo de la casa: {Money.Format(home.Value)}.");
            }
            else
            {
                int paid = Math.Min(monthsPlayed, home.Months);
                lines.Add($"Casa: alquilas {home.Name} por {Money.Format(home.Monthly)} al mes. El contrato es de {home.Months} meses y en la partida pagaste {paid}; el resto lo seguirías debiendo.");
            }
        }

        private static void AddCar(List<string> lines, CarHolding car)
        {
            if (car == null)
            {
                return;
            }

            lines.Add($"Carro: {car.Name}. Además de la cuota gasta {Money.Format(car.MonthlyRunningCosts)} al mes en gasolina y mantenimiento. Valor de reventa de ejemplo: {Money.Format(car.ResaleValue)}, porque un carro pierde valor apenas sale de la agencia.");
        }

        private static void AddFarm(List<string> lines, FarmStake stake, int monthsPlayed)
        {
            var option = MarketAssumptions.FindFarm(stake.Kind);
            var projection = option.Project(stake.Invested);
            int held = Math.Max(0, monthsPlayed - stake.MonthsPlayedWhenBought);
            int left = projection.MonthsLeftAfter(held);
            string wait = option.MonthsMin == option.MonthsMax
                ? $"{option.MonthsMax} meses"
                : $"{option.MonthsMin} a {option.MonthsMax} meses";

            lines.Add(
                $"Finca ({option.Name.ToLowerInvariant()}): metiste {Money.Format(stake.Invested)} y esa plata no vuelve pronto. " +
                $"A tiempo real tarda unos {wait} desde la compra; en la partida pasaron {held}, así que todavía faltan {left}. " +
                $"Proyección de ejemplo cuando se cobre: en un mal escenario {Money.Format(projection.LowValue)}, " +
                $"en uno normal {Money.Format(projection.BaseValue)} y en uno bueno {Money.Format(projection.HighValue)}.");
        }
    }
}
