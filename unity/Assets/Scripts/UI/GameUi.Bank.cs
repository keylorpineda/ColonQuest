using System.Collections.Generic;
using System.Text;
using ColonQuest.Game;
using ColonQuest.World;

namespace ColonQuest.UI
{
    /// <summary>
    /// The cash machines and the deposit window of the bank. The savings account is the money in the bank: the machine
    /// takes it out to cash and the window puts cash back in. The finance engine moves the money.
    /// </summary>
    public sealed partial class GameUi
    {
        /// <summary>The amounts the machine and the window offer, in colones.</summary>
        public static readonly long[] BankAmounts = { 10_000L, 25_000L, 50_000L };

        /// <summary>The name of the deposit window in the bank scene.</summary>
        public const string DepositWindowName = "Thing_Teller3";

        private void ShowAtm()
        {
            var state = session.State;
            var text = new StringBuilder();
            text.AppendLine(Loc.F("En tu cuenta de ahorro hay {0}.", Money.Format(state.Savings)));
            text.AppendLine(Loc.F("En la bolsa traés {0}.", Money.Format(state.Cash)));
            text.AppendLine();
            text.Append(Loc.T("Sacá solo lo que vas a usar: la plata en la bolsa se gasta más fácil."));

            var options = new List<ModalOption>();
            foreach (long amount in BankAmounts)
            {
                if (state.Savings < amount)
                {
                    continue;
                }

                long chosen = amount;
                options.Add(new ModalOption
                {
                    Label = Loc.F("Sacar {0}", Money.Format(chosen)),
                    Color = GameUiColors.Cycle(options.Count),
                    OnPress = () => session.WithdrawSavings(chosen)
                });
            }

            if (options.Count == 0 && state.Savings > 0L)
            {
                long all = state.Savings;
                options.Add(new ModalOption { Label = Loc.F("Sacar {0}", Money.Format(all)), Color = GameUiColors.Green, OnPress = () => session.WithdrawSavings(all) });
            }

            if (options.Count == 0)
            {
                text.Insert(0, Loc.T("Tu cuenta de ahorro está vacía: no hay nada que sacar.") + "\n\n");
            }

            options.Add(new ModalOption { Label = Loc.T("Ver estado de cuenta"), Color = GameUiColors.Blue, OnPress = ShowStatement });
            modal.Show(Loc.T("Cajero automático"), Loc.T("Retiro de efectivo"), text.ToString(), options.Count < 4 ? AddLeave(options) : options);
        }

        private void ShowDeposit()
        {
            var state = session.State;
            var text = new StringBuilder();
            text.AppendLine(Loc.T("\"Buenas. En esta caja recibimos depósitos. ¿Cuánto quiere poner en su cuenta?\""));
            text.AppendLine();
            text.AppendLine(Loc.F("En la bolsa traés {0}.", Money.Format(state.Cash)));
            text.Append(Loc.F("En tu cuenta de ahorro hay {0}.", Money.Format(state.Savings)));

            var options = new List<ModalOption>();
            foreach (long amount in BankAmounts)
            {
                if (state.Cash < amount)
                {
                    continue;
                }

                long chosen = amount;
                options.Add(new ModalOption
                {
                    Label = Loc.F("Depositar {0}", Money.Format(chosen)),
                    Color = GameUiColors.Cycle(options.Count),
                    OnPress = () => session.DepositCash(chosen)
                });
            }

            if (options.Count == 0 && state.Cash > 0L)
            {
                long all = state.Cash;
                options.Add(new ModalOption { Label = Loc.F("Depositar {0}", Money.Format(all)), Color = GameUiColors.Green, OnPress = () => session.DepositCash(all) });
            }

            if (options.Count == 0)
            {
                text.Insert(0, Loc.T("No traés efectivo para depositar.") + "\n\n");
            }

            modal.Show(Loc.T("Caja 4: depósitos"), Loc.T("Depósito a tu cuenta"), text.ToString(), AddLeave(options), Loc.T("Cajero"));
        }

        private static List<ModalOption> AddLeave(List<ModalOption> options)
        {
            options.Add(new ModalOption { Label = Loc.T("Cerrar"), Color = GameUiColors.Muted, OnPress = null });
            return options;
        }
    }
}
