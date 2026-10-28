namespace ColonQuest.Game
{
    /// <summary>
    /// The SINPE notices that show up on the player's phone. They only warn and teach: nothing here
    /// moves money, that is the finance engine's job.
    /// </summary>
    public static class PhoneMessages
    {
        private static readonly string[] SinpeNotices =
        {
            "SINPE Móvil: un número que no conoces te pide plata \"por error\". Verifica el nombre antes de mandar nada.",
            "SINPE Móvil: te llegó un mensaje de \"tu banco\" con un link. Los bancos no piden claves por mensaje.",
            "SINPE Móvil: un amigo te pide prestado y te manda el número. Confirma por llamada que sea él.",
            "SINPE Móvil: antes de pagar, revisa el nombre que aparece, no solo el número."
        };

        /// <summary>The notice for the n-th time the phone is opened; they rotate and never run out.</summary>
        public static string SinpeNotice(int openings)
        {
            int count = SinpeNotices.Length;
            return SinpeNotices[((openings % count) + count) % count];
        }
    }
}
