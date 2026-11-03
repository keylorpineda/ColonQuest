using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ColonQuest.Api
{
    /// <summary>
    /// The seller when the backend cannot answer. It follows the scripted fallback of
    /// <c>server/app/negotiation/questions.py</c>: it classifies the key questions with fixed patterns and
    /// only reveals what the player asked about. The figures come from <see cref="SellerOffer"/>.
    /// </summary>
    public static class LocalSeller
    {
        /// <summary>
        /// Writes a text in the player's language. The API layer does not know the language, so the game sets
        /// this once (to <c>Loc.T</c>); until then the Spanish text is used as it is.
        /// </summary>
        public static System.Func<string, string> Translate = text => text;

        public static string T(string spanish)
        {
            return Translate(spanish);
        }

        public static string F(string spanishTemplate, params object[] values)
        {
            return string.Format(Translate(spanishTemplate), values);
        }

        private static readonly Dictionary<KeyQuestion, string[]> Patterns = new Dictionary<KeyQuestion, string[]>
        {
            { KeyQuestion.TotalCost, new[] { @"\btotal\b", "al final", @"cuanto (?:pago|sale|cuesta|termino pagando|seria) (?:todo|en todo)", @"\bcosto (?:real|completo)", @"how much (?:do i pay|does it cost) (?:in total|overall)", "in the end", @"\bfull cost" } },
            { KeyQuestion.HiddenFees, new[] { "comision", @"cargos?\b", @"cobros? (?:extra|adicional)", @"costos? (?:extra|adicional|oculto|escondido)", @"letra (?:pequena|chiquita)", @"algo mas que (?:pagar|cobren)", "escondid", "commission", @"\bfees?\b", @"extra (?:charge|cost)s?", "hidden", "fine print" } },
            { KeyQuestion.InterestRate, new[] { "interes", @"\btasa\b" } },
            { KeyQuestion.Term, new[] { @"\bplazo\b", "cuantos meses", "cuantas cuotas", "cuanto tiempo", "por cuantos", @"\bterm\b", "how many months", "how many installments", "how long" } },
            { KeyQuestion.CashPrice, new[] { "contado", "precio real", "sin cuotas", "en efectivo", "de una vez", "cash price", "real price", "in cash", "no installments", "all at once" } },
            { KeyQuestion.Insurance, new[] { "seguro", "poliza", "insurance", "policy" } }
        };

        private static readonly Regex Accept = new Regex(@"\b(acepto|me lo llevo|trato hecho|de una|deme uno|i accept|i'll take it|deal)\b", RegexOptions.IgnoreCase);
        private static readonly Regex Leave = new Regex(@"\b(no gracias|me voy|mejor no|paso|no me interesa|no thanks|i'm leaving|better not|not interested)\b", RegexOptions.IgnoreCase);

        /// <summary>Lowercase without accents, so "interés" and "INTERES" match the same pattern.</summary>
        public static string Normalize(string text)
        {
            var builder = new StringBuilder(text.Length);
            foreach (char c in text.ToLowerInvariant())
            {
                switch (c)
                {
                    case 'á': case 'à': case 'ä': builder.Append('a'); break;
                    case 'é': case 'è': case 'ë': builder.Append('e'); break;
                    case 'í': case 'ì': case 'ï': builder.Append('i'); break;
                    case 'ó': case 'ò': case 'ö': builder.Append('o'); break;
                    case 'ú': case 'ù': case 'ü': builder.Append('u'); break;
                    case 'ñ': builder.Append('n'); break;
                    default: builder.Append(c); break;
                }
            }

            return builder.ToString();
        }

        public static List<KeyQuestion> Classify(string text)
        {
            var asked = new List<KeyQuestion>();
            string normalized = Normalize(text ?? string.Empty);
            foreach (KeyQuestion question in System.Enum.GetValues(typeof(KeyQuestion)))
            {
                foreach (string pattern in Patterns[question])
                {
                    if (Regex.IsMatch(normalized, pattern))
                    {
                        asked.Add(question);
                        break;
                    }
                }
            }

            return asked;
        }

        public static DealStatus StatusOf(string text)
        {
            string normalized = Normalize(text ?? string.Empty);
            if (Accept.IsMatch(normalized))
            {
                return DealStatus.Accepted;
            }

            return Leave.IsMatch(normalized) ? DealStatus.WalkedAway : DealStatus.Open;
        }

        public static NegotiationReply Reply(SellerOffer offer, string playerText)
        {
            var questions = Classify(playerText);
            var facts = FactsAnswering(questions);
            var reply = new NegotiationReply
            {
                Transcript = playerText ?? string.Empty,
                KeyQuestionsAsked = questions,
                DealStatus = StatusOf(playerText)
            };

            if (facts.Count == 0)
            {
                string down = offer.DownPayment > 0L ? F(", más la prima de {0}", SellerOffer.FormatColones(offer.DownPayment)) : string.Empty;
                reply.NpcReply = F("Mae, este {0} sale a solo {1} al mes{2}. ¡Se lo lleva hoy mismo!", T(offer.Item), offer.Describe(SellerFact.MonthlyPayment), down);
                reply.RevealedFacts = new List<SellerFact> { SellerFact.MonthlyPayment };
                return reply;
            }

            var parts = new List<string>();
            foreach (var fact in facts)
            {
                parts.Add(T(Label(fact)) + " " + offer.Describe(fact));
            }

            reply.NpcReply = T("Bueno, diay... ") + string.Join("; ", parts) + ".";
            if (offer.DownPayment > 0L && AsksAboutDownPayment(playerText))
            {
                reply.NpcReply += F(" La prima es de {0}.", SellerOffer.FormatColones(offer.DownPayment));
            }

            reply.RevealedFacts = facts;
            return reply;
        }

        public static string Greeting(SellerOffer offer)
        {
            string terms = offer.DownPayment > 0L
                ? F("con una prima de {0} se lo dejo en cuotas bien cómodas", SellerOffer.FormatColones(offer.DownPayment))
                : T("se lo dejo en cuotas bien cómodas");
            return F("¡Pura vida! Soy {0}. Mire este {1}: {2}. ¿Qué le interesa saber?", offer.SellerName, T(offer.Item), terms);
        }

        /// <summary>The down payment is not one of the key questions of the backend, so it is answered on the side.</summary>
        private static bool AsksAboutDownPayment(string text)
        {
            string normalized = Normalize(text ?? string.Empty);
            return normalized.Contains("prima") || normalized.Contains("entrada") || normalized.Contains("down payment");
        }

        private static List<SellerFact> FactsAnswering(List<KeyQuestion> questions)
        {
            var facts = new List<SellerFact>();
            foreach (var question in questions)
            {
                foreach (var fact in FactsFor(question))
                {
                    if (!facts.Contains(fact))
                    {
                        facts.Add(fact);
                    }
                }
            }

            return facts;
        }

        private static SellerFact[] FactsFor(KeyQuestion question)
        {
            switch (question)
            {
                case KeyQuestion.TotalCost: return new[] { SellerFact.TotalCost };
                case KeyQuestion.HiddenFees: return new[] { SellerFact.Commission, SellerFact.InsuranceFee };
                case KeyQuestion.InterestRate: return new[] { SellerFact.InterestRate };
                case KeyQuestion.Term: return new[] { SellerFact.TermMonths };
                case KeyQuestion.CashPrice: return new[] { SellerFact.CashPrice };
                default: return new[] { SellerFact.InsuranceFee };
            }
        }

        private static string Label(SellerFact fact)
        {
            switch (fact)
            {
                case SellerFact.CashPrice: return "de contado sale en";
                case SellerFact.MonthlyPayment: return "la cuota es de";
                case SellerFact.TermMonths: return "el plazo es de";
                case SellerFact.InterestRate: return "la tasa es de";
                case SellerFact.InsuranceFee: return "el seguro es de";
                case SellerFact.Commission: return "la comisión de apertura es de";
                default: return "en total terminás pagando";
            }
        }
    }
}
