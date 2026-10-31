using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ColonQuest.World;

namespace ColonQuest.Game
{
    /// <summary>Who the player is talking with in their own words.</summary>
    public enum TalkPartner
    {
        Collectors,
        Lender,
        Pyramid
    }

    /// <summary>What the player can find out by asking. Each one is a question worth asking before saying yes.</summary>
    public enum TalkFact
    {
        TotalCost,
        Rate,
        Arrangement,
        WhatIfNotPaid,
        WhereMoneyComes,
        Guarantee,
        Legal
    }

    /// <summary>One exchange of a talk: what the player said and what the other one answered.</summary>
    public sealed class TalkTurn
    {
        public string Player = string.Empty;
        public string Reply = string.Empty;
    }

    /// <summary>
    /// A conversation in the player's own words, typed or spoken, with the collectors, the lender of quick money or the
    /// "Invierte Ya 300%" stand. The answers follow fixed rules: the other one never lies about a figure, but only says it
    /// when asked. Every figure comes from the game's numbers, never from the text. Pure, so it can be tested.
    /// </summary>
    public sealed class DebtTalk
    {
        /// <summary>The example loan the lender talks about, and the months of the short loan.</summary>
        public const long ExampleLoan = 100_000L;
        public const int ExampleMonths = 3;
        public const int LenderRatePct = 20;
        public const int PyramidPromisePct = 300;

        private readonly List<TalkTurn> turns = new List<TalkTurn>();
        private readonly List<TalkFact> asked = new List<TalkFact>();

        public TalkPartner Partner { get; }

        /// <summary>What the collectors came for today. Zero for the others.</summary>
        public long Demand { get; }

        public IReadOnlyList<TalkTurn> Turns => turns;
        public IReadOnlyList<TalkFact> Asked => asked;

        public DebtTalk(TalkPartner partner, long demand = 0L)
        {
            Partner = partner;
            Demand = demand;
        }

        public string Name
        {
            get
            {
                switch (Partner)
                {
                    case TalkPartner.Collectors:
                        return "Cobrador";
                    case TalkPartner.Lender:
                        return "Prestamista";
                    default:
                        return "Invierte Ya";
                }
            }
        }

        public string Opening
        {
            get
            {
                switch (Partner)
                {
                    case TalkPartner.Collectors:
                        return Loc.F("Mae, usted debe plata. Hoy venimos por {0}. ¿Cómo hacemos?", Money.Format(Demand));
                    case TalkPartner.Lender:
                        return "¿Ocupa platica rápido? Sin papeles, sin fiador, hoy mismo. Usted dice cuánto.";
                    default:
                        return Loc.F("¡Duplicá, triplicá tu plata! Te pagamos {0}% en un mes. ¿Entrás o te lo vas a perder?", PyramidPromisePct);
                }
            }
        }

        /// <summary>The collectors offer an arrangement only to the one who asks for it.</summary>
        public bool ArrangementOffered => asked.Contains(TalkFact.Arrangement);

        /// <summary>Half of the demand today, the rest as a new debt with the surcharge (see FinanceEngine.NegotiateCollectors).</summary>
        public long ArrangementToday => Demand / 2L;

        public long ArrangementLater => (Demand - ArrangementToday) + (Demand - ArrangementToday) * MarketAssumptions.CollectionNegotiateSurchargePct / 100L;

        /// <summary>What the example loan costs at the end of the short term, month on month at the lender's rate.</summary>
        public static long LenderTotal()
        {
            long total = ExampleLoan;
            for (int month = 0; month < ExampleMonths; month++)
            {
                total += total * LenderRatePct / 100L;
            }

            return total;
        }

        /// <summary>The player says something; the answer is added to the turns and returned.</summary>
        public string Say(string playerText)
        {
            string text = playerText == null ? string.Empty : playerText.Trim();
            if (text.Length == 0)
            {
                return string.Empty;
            }

            var fact = Classify(text);
            if (fact.HasValue && !asked.Contains(fact.Value))
            {
                asked.Add(fact.Value);
            }

            string reply = Answer(fact);
            turns.Add(new TalkTurn { Player = text, Reply = reply });
            return reply;
        }

        /// <summary>The history tags of the questions asked, for the metrics of the pilot.</summary>
        public IEnumerable<string> HistoryTags()
        {
            string who = Partner == TalkPartner.Collectors ? "collectors" : (Partner == TalkPartner.Lender ? "lender" : "pyramid");
            foreach (var fact in asked)
            {
                yield return "asked_" + who + "_" + fact.ToString().ToLowerInvariant();
            }
        }

        public static TalkFact? Classify(string text)
        {
            string t = Normalize(text);
            if (Has(t, "de donde sale", "de donde viene", "como ganan", "como funciona", "referid", "meter gente", "nuevos"))
            {
                return TalkFact.WhereMoneyComes;
            }

            if (Has(t, "garantia", "contrato", "papel", "recibo", "por escrito", "seguro que"))
            {
                return TalkFact.Guarantee;
            }

            if (Has(t, "legal", "sugef", "permiso", "inscrit", "policia", "denunc", "ley", "amenaz"))
            {
                return TalkFact.Legal;
            }

            if (Has(t, "arreglo", "negoci", "mitad", "plazo", "cuotas", "partes", "rebaj", "descuento", "abono", "despues", "otro dia"))
            {
                return TalkFact.Arrangement;
            }

            if (Has(t, "no pago", "no tengo", "no puedo", "si no", "que pasa"))
            {
                return TalkFact.WhatIfNotPaid;
            }

            if (Has(t, "interes", "tasa", "porcentaje", "%", "por ciento", "al mes", "cobran"))
            {
                return TalkFact.Rate;
            }

            if (Has(t, "total", "cuanto", "en total", "al final", "devolver", "pagaria"))
            {
                return TalkFact.TotalCost;
            }

            return null;
        }

        private string Answer(TalkFact? fact)
        {
            switch (Partner)
            {
                case TalkPartner.Collectors:
                    return CollectorsAnswer(fact);
                case TalkPartner.Lender:
                    return LenderAnswer(fact);
                default:
                    return PyramidAnswer(fact);
            }
        }

        private string CollectorsAnswer(TalkFact? fact)
        {
            switch (fact)
            {
                case TalkFact.Arrangement:
                    return Loc.F("Bueno, mae. Deme {0} hoy y lo otro queda como deuda nueva: {1}, con el {2}% de recargo.",
                        Money.Format(ArrangementToday), Money.Format(ArrangementLater), MarketAssumptions.CollectionNegotiateSurchargePct);
                case TalkFact.WhatIfNotPaid:
                    return Loc.F("Si no paga hoy, le cae una multa del {0}% a la tarjeta y volvemos. Usted verá.", MarketAssumptions.CollectionIgnoreFeePct);
                case TalkFact.Legal:
                    return "Tranquilo, nadie lo va a tocar. Cobrar es legal; amenazar no. Si alguien lo amenaza, eso se denuncia.";
                case TalkFact.TotalCost:
                case TalkFact.Rate:
                    return Loc.F("Hoy son {0}. Eso es lo atrasado, nada más.", Money.Format(Demand));
                default:
                    return "Mae, al grano: ¿paga, hacemos un arreglo o qué?";
            }
        }

        private static string LenderAnswer(TalkFact? fact)
        {
            switch (fact)
            {
                case TalkFact.Rate:
                    return Loc.F("Es el {0}% al mes, mae. Poquito, ¿verdad?", LenderRatePct);
                case TalkFact.TotalCost:
                    return Loc.F("Si le presto {0}, en {1} meses me devuelve {2}.", Money.Format(ExampleLoan), ExampleMonths, Money.Format(LenderTotal()));
                case TalkFact.Arrangement:
                    return "Aquí no hay rebajas. Yo paso todos los días a cobrar la cuota, gota a gota.";
                case TalkFact.WhatIfNotPaid:
                    return "Si se atrasa, los intereses siguen corriendo y le mando a los muchachos del carro negro.";
                case TalkFact.Legal:
                    return "¿Papeles, SUGEF? No, mae, esto es entre usted y yo. Por eso es tan rápido.";
                case TalkFact.Guarantee:
                    return "Ningún papel. Usted me firma aquí en la libreta y listo.";
                default:
                    return "Usted pregunte lo que quiera, pero la plata vuela. ¿Cuánto ocupa?";
            }
        }

        private static string PyramidAnswer(TalkFact? fact)
        {
            switch (fact)
            {
                case TalkFact.WhereMoneyComes:
                    return "Bueno... de los nuevos que entran. Por eso tenés que traer a tres amigos.";
                case TalkFact.Guarantee:
                    return "¿Contrato? No, esto es de confianza. Garantizado, te lo juro.";
                case TalkFact.Legal:
                    return "¿SUGEF? No, eso es para los bancos. Nosotros somos un club de amigos.";
                case TalkFact.Rate:
                case TalkFact.TotalCost:
                    return Loc.F("Ponés {0} y en un mes te damos {1}. Así de fácil.", Money.Format(ExampleLoan), Money.Format(ExampleLoan * (100L + PyramidPromisePct) / 100L));
                case TalkFact.WhatIfNotPaid:
                    return "¿Que si no pagamos? Siempre pagamos... a los primeros.";
                case TalkFact.Arrangement:
                    return "Si metés más plata hoy, te subimos al nivel oro. Pero solo hoy.";
                default:
                    return "Pensalo rápido, que los cupos se acaban.";
            }
        }

        private static bool Has(string text, params string[] words)
        {
            foreach (string word in words)
            {
                if (text.Contains(word))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Lower case and without accents, so "interés" and "interes" are the same word.</summary>
        public static string Normalize(string text)
        {
            string decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var plain = new StringBuilder(decomposed.Length);
            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    plain.Append(c);
                }
            }

            return plain.ToString();
        }
    }
}
