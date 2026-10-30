using ColonQuest.Finance;
using ColonQuest.World;

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
                    return Loc.T("No te alcanza el efectivo para entrar. Lo que debes poner al firmar sale de tu efectivo, no de la tarjeta.");
                case PurchaseFailure.AlreadyHasHome:
                    return Loc.T("Ya tienes una casa en esta partida.");
                case PurchaseFailure.AlreadyHasCar:
                    return Loc.T("Ya tienes un carro en esta partida.");
                case PurchaseFailure.AlreadyHasFurniture:
                    return Loc.T("Ese mueble ya está en tu cuarto.");
                case PurchaseFailure.TooManyInstallments:
                    return Loc.T("Ya tienes demasiadas cuotas abiertas. Termina de pagar alguna antes de firmar otra.");
                default:
                    return string.Empty;
            }
        }
    }
}
