using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;

namespace ColonQuest.UI
{
    /// <summary>
    /// The windows of the places where the player spends big: the real estate agency, the car agency and the
    /// farm. They only show the true figures and ask the session to buy; the money moves in the finance engine.
    /// Every figure here comes from <see cref="MarketAssumptions"/> and is shown with the warning that it is an example.
    /// </summary>
    public sealed partial class GameUi
    {
        private const int MarketFont = 28;
        private const float MarketBodyHeight = 540f;

        private static string Hex(UnityEngine.Color color)
        {
            return GameUiColors.Hex(color);
        }

        private static string Red(string text)
        {
            return $"<color={Hex(GameUiColors.Loss)}>{text}</color>";
        }

        private static string Pct(int value)
        {
            return value > 0 ? "+" + value + "%" : value + "%";
        }

        private static string Rate(decimal value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static ModalOption Back(string label, System.Action then = null)
        {
            return new ModalOption { Label = label, Color = GameUiColors.Pink, OnPress = then };
        }

        // ---------------------------------------------------------------- real estate

        /// <summary>The agency lists every house; a sign in front of one house opens just that house.</summary>
        private void ShowRealEstate(Station station)
        {
            string id = station.Lines != null && station.Lines.Length > 0 ? station.Lines[0] : null;
            var listing = id != null ? MarketAssumptions.FindHome(id) : null;
            if (listing != null)
            {
                ShowHomeDetail(listing, false);
            }
            else
            {
                ShowHomeCatalog();
            }
        }

        private static string HomeTag(HomeListing home)
        {
            return home.ForSale
                ? Loc.F("se vende {0}", Money.Format(home.Price))
                : Loc.F("se alquila {0} al mes", Money.Format(home.MonthlyRent));
        }

        private void ShowHomeCatalog()
        {
            var text = new StringBuilder();
            text.AppendLine(Loc.T("Elige una casa para ver lo que cuesta de verdad, no solo el precio del letrero."));
            text.AppendLine();
            text.AppendLine(Loc.F("Tus gastos de vida ({0} al mes) ya cubren donde vives hoy. Una casa nueva se suma a eso.", Money.Format(GameRules.LivingExpenses)));
            text.AppendLine();
            text.Append(Loc.T(MarketAssumptions.ShortDisclaimer));

            var options = new List<ModalOption>();
            int index = 0;
            foreach (var home in MarketAssumptions.Homes)
            {
                var listing = home;
                options.Add(new ModalOption
                {
                    Label = Loc.F("{0}.  {1}  ·  {2}", index + 1, Loc.T(listing.Name), HomeTag(listing)),
                    Color = GameUiColors.Cycle(index),
                    OnPress = () => ShowHomeDetail(listing, true)
                });
                index++;
            }

            options.Add(Back(Loc.F("{0}.  Salir", index + 1)));
            modal.Show(Loc.T("Inmobiliaria"), Loc.T("Casas en venta y en alquiler"), text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        private void ShowHomeDetail(HomeListing home, bool fromCatalog)
        {
            var state = session.State;
            var text = new StringBuilder();
            var options = new List<ModalOption>();
            text.AppendLine(Loc.T(home.Description));
            text.AppendLine();

            if (home.ForSale)
            {
                var quote = MarketAssumptions.QuoteMortgage(home);
                long share = FinanceEngine.ShareOfSalaryPct(quote.MonthlyPayment, state.SalaryNet);
                text.AppendLine(Loc.F("Precio  {0}", Money.Format(quote.Price)));
                text.AppendLine(Loc.F("Prima ({0}%)  {1}", Rate(MarketAssumptions.HomeDownPaymentPct), Money.Format(quote.DownPayment)));
                text.AppendLine(Loc.F("Gastos de formalización ({0}%)  {1}", Rate(MarketAssumptions.HomeClosingCostsPct), Money.Format(quote.ClosingCosts)));
                text.AppendLine(Loc.F("Para entrar necesitas  {0}  y tienes {1}", Red(Money.Format(quote.UpfrontCost)), Money.Format(state.Cash)));
                text.AppendLine(Loc.F("Hipoteca de {0} a {1} meses ({2} años, el plazo más largo del juego) con tasa de ejemplo de {3}% al mes", Money.Format(quote.Financed), quote.TermMonths, quote.TermMonths / 12, Rate(quote.MonthlyRatePct)));
                text.AppendLine(Loc.F("Cuota  {0} al mes, que es el {1}% de tu salario", Red(Money.Format(quote.MonthlyPayment)), share));
                text.AppendLine(Loc.F("En total pagarías  {0}: {1} más que el precio", Money.Format(quote.TotalPaid), Red(Money.Format(quote.TotalPaid - quote.Price))));
                text.AppendLine(Loc.F("Solo de intereses para el banco  {0}", Money.Format(quote.TotalInterest)));

                var refusal = session.Loop.CheckBuyHome(home);
                if (refusal != PurchaseFailure.None)
                {
                    text.AppendLine();
                    text.AppendLine(Red(PurchaseMessages.For(refusal)));
                }
                else
                {
                    options.Add(new ModalOption { Label = Loc.T("1.  Comprar con hipoteca"), Color = GameUiColors.Green, OnPress = () => session.BuyHome(home) });
                }
            }
            else
            {
                var quote = MarketAssumptions.QuoteRent(home);
                long share = FinanceEngine.ShareOfSalaryPct(quote.MonthlyRent, state.SalaryNet);
                text.AppendLine(Loc.F("Alquiler  {0} al mes, que es el {1}% de tu salario", Money.Format(quote.MonthlyRent), share));
                text.AppendLine(Loc.F("Depósito  {0} ({1} mes), y tienes {2}", Money.Format(quote.Deposit), MarketAssumptions.RentDepositMonths, Money.Format(state.Cash)));
                text.AppendLine(Loc.F("Contrato de {0} meses: {1} en alquiler", quote.LeaseMonths, Red(Money.Format(quote.LeaseCost))));
                text.AppendLine(Loc.F("La partida dura {0} meses, pero el contrato sigue después y la casa nunca es tuya", GameRules.MonthsToPlay));

                var refusal = session.Loop.CheckRent(home);
                if (refusal != PurchaseFailure.None)
                {
                    text.AppendLine();
                    text.AppendLine(Red(PurchaseMessages.For(refusal)));
                }
                else
                {
                    options.Add(new ModalOption { Label = Loc.T("1.  Alquilar"), Color = GameUiColors.Green, OnPress = () => session.RentHome(home) });
                }
            }

            text.AppendLine();
            text.Append(Loc.T(MarketAssumptions.ShortDisclaimer));

            string backLabel = $"{options.Count + 1}.  " + Loc.T(fromCatalog ? "Volver a las casas" : "Salir");
            options.Add(Back(backLabel, fromCatalog ? (System.Action) ShowHomeCatalog : null));
            modal.Show(Loc.T(home.Name), HomeTag(home), text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        // ---------------------------------------------------------------- car agency

        private void ShowCarCatalog()
        {
            var text = new StringBuilder();
            var options = new List<ModalOption>();
            var owned = session.Holdings.Car;
            if (owned != null)
            {
                text.Append(Loc.F("Ya tienes {0}. En esta partida cabe un solo carro.", Loc.T(owned.Name)));
                options.Add(Back(Loc.T("1.  Salir")));
                modal.Show(Loc.T("Agencia de carros"), "Don Chepe", text.ToString(), options, null, MarketFont, MarketBodyHeight);
                return;
            }

            text.AppendLine(Loc.T("Elige un carro y negocia con Don Chepe. Antes de firmar pregunta por la prima, el seguro, la comisión y el costo total."));
            text.AppendLine();
            text.AppendLine(Loc.T("El carro trae gastos que no salen en la cuota: el marchamo cada año, la gasolina y el mantenimiento."));
            text.AppendLine();
            text.Append(Loc.T(MarketAssumptions.ShortDisclaimer));

            int index = 0;
            foreach (var car in MarketAssumptions.Cars)
            {
                var model = car;
                options.Add(new ModalOption
                {
                    Label = $"{index + 1}.  {Loc.T(model.Name)}  ·  {Money.Format(model.Price)}",
                    Color = GameUiColors.Cycle(index),
                    OnPress = () => session.BeginCarNegotiation(model)
                });
                index++;
            }

            options.Add(Back(Loc.F("{0}.  Salir", index + 1)));
            modal.Show(Loc.T("Agencia de carros"), "Don Chepe", text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        // ---------------------------------------------------------------- farm

        private void ShowFarm()
        {
            var text = new StringBuilder();
            text.AppendLine(Loc.T("Una finca no es una cuenta de ahorros. La plata entra hoy y no sale hasta la cosecha o la venta, y puede salir menos de lo que entró."));
            text.AppendLine();
            text.AppendLine(Loc.F("Tienes {0} en efectivo. Nunca se invierte con la tarjeta.", Money.Format(session.State.Cash)));
            text.AppendLine();
            text.Append(Loc.T(MarketAssumptions.ShortDisclaimer));

            var options = new List<ModalOption>();
            int index = 0;
            foreach (var farm in MarketAssumptions.Farm)
            {
                var option = farm;
                options.Add(new ModalOption
                {
                    Label = $"{index + 1}.  {Loc.T(option.Name)}  ·  {WaitText(option)}",
                    Color = GameUiColors.Cycle(index),
                    OnPress = () => ShowFarmOption(option)
                });
                index++;
            }

            options.Add(Back(Loc.F("{0}.  Salir", index + 1)));
            modal.Show(Loc.T("Finca San Isidro"), Loc.T("Café y ganado"), text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        private static string WaitText(FarmOption option)
        {
            return option.MonthsMin == option.MonthsMax
                ? Loc.F("unos {0} meses", option.MonthsMax)
                : Loc.F("{0} a {1} meses", option.MonthsMin, option.MonthsMax);
        }

        private void ShowFarmOption(FarmOption option)
        {
            long cash = session.State.Cash;
            var text = new StringBuilder();
            text.AppendLine(Loc.T(option.Description));
            text.AppendLine();
            text.AppendLine(Loc.F("Tarda {0} en pagar. La partida dura {1} meses: no vas a ver esa plata de vuelta mientras juegas, la proyección sale al final.", WaitText(option), GameRules.MonthsToPlay));
            text.AppendLine(Loc.F("Escenarios de ejemplo cuando se cobre: {0} si sale mal, {1} si sale normal, {2} si sale bien.", Pct(option.LowPct), Pct(option.BasePct), Pct(option.HighPct)));

            var options = new List<ModalOption>();
            foreach (long stake in MarketAssumptions.FarmStakes)
            {
                if (stake > cash)
                {
                    continue;
                }

                long amount = stake;
                var example = option.Project(amount);
                options.Add(new ModalOption
                {
                    Label = Loc.F("{0}.  Invertir {1}  ·  normal: {2}", options.Count + 1, Money.Format(amount), Money.Format(example.BaseValue)),
                    Color = GameUiColors.Cycle(options.Count),
                    OnPress = () => session.InvestInFarm(option, amount)
                });
            }

            if (options.Count == 0)
            {
                text.AppendLine();
                text.AppendLine(Red(Loc.F("No te alcanza ni para lo mínimo ({0}).", Money.Format(MarketAssumptions.FarmStakes[0]))));
            }

            text.AppendLine();
            text.Append(Loc.T(MarketAssumptions.ShortDisclaimer));
            options.Add(Back(Loc.F("{0}.  Volver", options.Count + 1), ShowFarm));
            modal.Show(Loc.T(option.Name), Loc.T("Finca San Isidro"), text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        // ---------------------------------------------------------------- end of the run

        private void ShowAssetsReport(IReadOnlyList<string> lines)
        {
            var text = new StringBuilder();
            foreach (string line in lines)
            {
                text.AppendLine(line);
                text.AppendLine();
            }

            modal.Show(Loc.T("Lo que compraste"), Loc.T("A tiempo real, no a 3 meses"), text.ToString().TrimEnd(), new[]
            {
                new ModalOption { Label = Loc.T("Jugar de nuevo"), Color = GameUiColors.Green, OnPress = Restart }
            }, null, 24, 680f);
        }
    }
}
