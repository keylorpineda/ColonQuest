using ColonQuest.Finance;

namespace ColonQuest.Game
{
    /// <summary>What the game says when a purchase cannot be made. Text only: the finance engine already refused it.</summary>
    public static class PurchaseMessages
    {
        public static string For(PurchaseFailure failure)
        {
            switch (failure)
            {
                case PurchaseFailure.NotEnoughCash:
                    return "No te alcanza el efectivo para entrar. Lo que debes poner al firmar sale de tu efectivo, no de la tarjeta.";
                case PurchaseFailure.AlreadyHasHome:
                    return "Ya tienes una casa en esta partida.";
                case PurchaseFailure.AlreadyHasCar:
                    return "Ya tienes un carro en esta partida.";
                case PurchaseFailure.TooManyInstallments:
                    return "Ya tienes demasiadas cuotas abiertas. Termina de pagar alguna antes de firmar otra.";
                default:
                    return string.Empty;
            }
        }
    }
}
