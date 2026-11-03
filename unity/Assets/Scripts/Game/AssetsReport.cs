using System;
using System.Collections.Generic;
using ColonQuest.Finance;
using ColonQuest.World;

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

            lines.Add(Loc.T(MarketAssumptions.Disclaimer));
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
                lines.Add(Loc.F("Casa: compraste {0} con hipoteca. Pagas {1} al mes y todavía faltan {2} cuotas. Valor de ejemplo de la casa: {3}.", Loc.T(home.Name), Money.Format(home.Monthly), left, Money.Format(home.Value)));
            }
            else
            {
                int paid = Math.Min(monthsPlayed, home.Months);
                lines.Add(Loc.F("Casa: alquilas {0} por {1} al mes. El contrato es de {2} meses y en la partida pagaste {3}; el resto lo seguirías debiendo.", Loc.T(home.Name), Money.Format(home.Monthly), home.Months, paid));
            }
        }

        private static void AddCar(List<string> lines, CarHolding car)
        {
            if (car == null)
            {
                return;
            }

            lines.Add(Loc.F("Carro: {0}. Además de la cuota gasta {1} al mes en gasolina y mantenimiento. Valor de reventa de ejemplo: {2}, porque un carro pierde valor apenas sale de la agencia.", Loc.T(car.Name), Money.Format(car.MonthlyRunningCosts), Money.Format(car.ResaleValue)));
        }

        private static void AddFarm(List<string> lines, FarmStake stake, int monthsPlayed)
        {
            var option = MarketAssumptions.FindFarm(stake.Kind);
            var projection = option.Project(stake.Invested);
            int held = Math.Max(0, monthsPlayed - stake.MonthsPlayedWhenBought);
            int left = projection.MonthsLeftAfter(held);
            string wait = option.MonthsMin == option.MonthsMax
                ? Loc.F("{0} meses", option.MonthsMax)
                : Loc.F("{0} a {1} meses", option.MonthsMin, option.MonthsMax);

            lines.Add(
                Loc.F("Finca ({0}): metiste {1} y esa plata no vuelve pronto. ", Loc.T(option.Name).ToLowerInvariant(), Money.Format(stake.Invested)) +
                Loc.F("A tiempo real tarda unos {0} desde la compra; en la partida pasaron {1}, así que todavía faltan {2}. ", wait, held, left) +
                Loc.F("Proyección de ejemplo cuando se cobre: en un mal escenario {0}, ", Money.Format(projection.LowValue)) +
                Loc.F("en uno normal {0} y en uno bueno {1}.", Money.Format(projection.BaseValue), Money.Format(projection.HighValue)));
        }
    }
}
