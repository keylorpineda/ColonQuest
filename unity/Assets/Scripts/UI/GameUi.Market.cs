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
                ? "se vende " + Money.Format(home.Price)
                : "se alquila " + Money.Format(home.MonthlyRent) + " al mes";
        }

        private void ShowHomeCatalog()
        {
            var text = new StringBuilder();
            text.AppendLine("Elige una casa para ver lo que cuesta de verdad, no solo el precio del letrero.");
            text.AppendLine();
            text.AppendLine($"Tus gastos de vida ({Money.Format(GameRules.LivingExpenses)} al mes) ya cubren donde vives hoy. Una casa nueva se suma a eso.");
            text.AppendLine();
            text.Append(MarketAssumptions.ShortDisclaimer);

            var options = new List<ModalOption>();
            int index = 0;
            foreach (var home in MarketAssumptions.Homes)
            {
                var listing = home;
                options.Add(new ModalOption
                {
                    Label = $"{index + 1}.  {listing.Name}  ·  {HomeTag(listing)}",
                    Color = GameUiColors.Cycle(index),
                    OnPress = () => ShowHomeDetail(listing, true)
                });
                index++;
            }

            options.Add(Back($"{index + 1}.  Salir"));
            modal.Show("Inmobiliaria", "Casas en venta y en alquiler", text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        private void ShowHomeDetail(HomeListing home, bool fromCatalog)
        {
            var state = session.State;
            var text = new StringBuilder();
            var options = new List<ModalOption>();
            text.AppendLine(home.Description);
            text.AppendLine();

            if (home.ForSale)
            {
                var quote = MarketAssumptions.QuoteMortgage(home);
                long share = FinanceEngine.ShareOfSalaryPct(quote.MonthlyPayment, state.SalaryNet);
                text.AppendLine($"Precio  {Money.Format(quote.Price)}");
                text.AppendLine($"Prima ({Rate(MarketAssumptions.HomeDownPaymentPct)}%)  {Money.Format(quote.DownPayment)}");
                text.AppendLine($"Gastos de formalización ({Rate(MarketAssumptions.HomeClosingCostsPct)}%)  {Money.Format(quote.ClosingCosts)}");
                text.AppendLine($"Para entrar necesitas  {Red(Money.Format(quote.UpfrontCost))}  y tienes {Money.Format(state.Cash)}");
                text.AppendLine($"Hipoteca de {Money.Format(quote.Financed)} a {quote.TermMonths} meses ({quote.TermMonths / 12} años, el plazo más largo del juego) con tasa de ejemplo de {Rate(quote.MonthlyRatePct)}% al mes");
                text.AppendLine($"Cuota  {Red(Money.Format(quote.MonthlyPayment))} al mes, que es el {share}% de tu salario");
                text.AppendLine($"En total pagarías  {Money.Format(quote.TotalPaid)}: {Red(Money.Format(quote.TotalPaid - quote.Price))} más que el precio");
                text.AppendLine($"Solo de intereses para el banco  {Money.Format(quote.TotalInterest)}");

                var refusal = session.Loop.CheckBuyHome(home);
                if (refusal != PurchaseFailure.None)
                {
                    text.AppendLine();
                    text.AppendLine(Red(PurchaseMessages.For(refusal)));
                }
                else
                {
                    options.Add(new ModalOption { Label = "1.  Comprar con hipoteca", Color = GameUiColors.Green, OnPress = () => session.BuyHome(home) });
                }
            }
            else
            {
                var quote = MarketAssumptions.QuoteRent(home);
                long share = FinanceEngine.ShareOfSalaryPct(quote.MonthlyRent, state.SalaryNet);
                text.AppendLine($"Alquiler  {Money.Format(quote.MonthlyRent)} al mes, que es el {share}% de tu salario");
                text.AppendLine($"Depósito  {Money.Format(quote.Deposit)} ({MarketAssumptions.RentDepositMonths} mes), y tienes {Money.Format(state.Cash)}");
                text.AppendLine($"Contrato de {quote.LeaseMonths} meses: {Red(Money.Format(quote.LeaseCost))} en alquiler");
                text.AppendLine($"La partida dura {GameRules.MonthsToPlay} meses, pero el contrato sigue después y la casa nunca es tuya");

                var refusal = session.Loop.CheckRent(home);
                if (refusal != PurchaseFailure.None)
                {
                    text.AppendLine();
                    text.AppendLine(Red(PurchaseMessages.For(refusal)));
                }
                else
                {
                    options.Add(new ModalOption { Label = "1.  Alquilar", Color = GameUiColors.Green, OnPress = () => session.RentHome(home) });
                }
            }

            text.AppendLine();
            text.Append(MarketAssumptions.ShortDisclaimer);

            string backLabel = $"{options.Count + 1}.  " + (fromCatalog ? "Volver a las casas" : "Salir");
            options.Add(Back(backLabel, fromCatalog ? (System.Action) ShowHomeCatalog : null));
            modal.Show(home.Name, HomeTag(home), text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        // ---------------------------------------------------------------- car agency

        private void ShowCarCatalog()
        {
            var text = new StringBuilder();
            var options = new List<ModalOption>();
            var owned = session.Holdings.Car;
            if (owned != null)
            {
                text.Append($"Ya tienes {owned.Name}. En esta partida cabe un solo carro.");
                options.Add(Back("1.  Salir"));
                modal.Show("Agencia de carros", "Don Chepe", text.ToString(), options, null, MarketFont, MarketBodyHeight);
                return;
            }

            text.AppendLine("Elige un carro y negocia con Don Chepe. Antes de firmar pregunta por la prima, el seguro, la comisión y el costo total.");
            text.AppendLine();
            text.AppendLine("El carro trae gastos que no salen en la cuota: el marchamo cada año, la gasolina y el mantenimiento.");
            text.AppendLine();
            text.Append(MarketAssumptions.ShortDisclaimer);

            int index = 0;
            foreach (var car in MarketAssumptions.Cars)
            {
                var model = car;
                options.Add(new ModalOption
                {
                    Label = $"{index + 1}.  {model.Name}  ·  {Money.Format(model.Price)}",
                    Color = GameUiColors.Cycle(index),
                    OnPress = () => session.BeginCarNegotiation(model)
                });
                index++;
            }

            options.Add(Back($"{index + 1}.  Salir"));
            modal.Show("Agencia de carros", "Don Chepe", text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        // ---------------------------------------------------------------- farm

        private void ShowFarm()
        {
            var text = new StringBuilder();
            text.AppendLine("Una finca no es una cuenta de ahorros. La plata entra hoy y no sale hasta la cosecha o la venta, y puede salir menos de lo que entró.");
            text.AppendLine();
            text.AppendLine($"Tienes {Money.Format(session.State.Cash)} en efectivo. Nunca se invierte con la tarjeta.");
            text.AppendLine();
            text.Append(MarketAssumptions.ShortDisclaimer);

            var options = new List<ModalOption>();
            int index = 0;
            foreach (var farm in MarketAssumptions.Farm)
            {
                var option = farm;
                options.Add(new ModalOption
                {
                    Label = $"{index + 1}.  {option.Name}  ·  {WaitText(option)}",
                    Color = GameUiColors.Cycle(index),
                    OnPress = () => ShowFarmOption(option)
                });
                index++;
            }

            options.Add(Back($"{index + 1}.  Salir"));
            modal.Show("Finca San Isidro", "Café y ganado", text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        private static string WaitText(FarmOption option)
        {
            return option.MonthsMin == option.MonthsMax
                ? $"unos {option.MonthsMax} meses"
                : $"{option.MonthsMin} a {option.MonthsMax} meses";
        }

        private void ShowFarmOption(FarmOption option)
        {
            long cash = session.State.Cash;
            var text = new StringBuilder();
            text.AppendLine(option.Description);
            text.AppendLine();
            text.AppendLine($"Tarda {WaitText(option)} en pagar. La partida dura {GameRules.MonthsToPlay} meses: no vas a ver esa plata de vuelta mientras juegas, la proyección sale al final.");
            text.AppendLine($"Escenarios de ejemplo cuando se cobre: {Pct(option.LowPct)} si sale mal, {Pct(option.BasePct)} si sale normal, {Pct(option.HighPct)} si sale bien.");

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
                    Label = $"{options.Count + 1}.  Invertir {Money.Format(amount)}  ·  normal: {Money.Format(example.BaseValue)}",
                    Color = GameUiColors.Cycle(options.Count),
                    OnPress = () => session.InvestInFarm(option, amount)
                });
            }

            if (options.Count == 0)
            {
                text.AppendLine();
                text.AppendLine(Red($"No te alcanza ni para lo mínimo ({Money.Format(MarketAssumptions.FarmStakes[0])})."));
            }

            text.AppendLine();
            text.Append(MarketAssumptions.ShortDisclaimer);
            options.Add(Back($"{options.Count + 1}.  Volver", ShowFarm));
            modal.Show(option.Name, "Finca San Isidro", text.ToString(), options, null, MarketFont, MarketBodyHeight);
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

            modal.Show("Lo que compraste", "A tiempo real, no a 3 meses", text.ToString().TrimEnd(), new[]
            {
                new ModalOption { Label = "Jugar de nuevo", Color = GameUiColors.Green, OnPress = Restart }
            }, null, 24, 680f);
        }
    }
}
