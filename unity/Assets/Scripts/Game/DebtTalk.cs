using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.World;
using EventType = ColonQuest.Finance.EventType;

namespace ColonQuest.Game
{
    /// <summary>Who the player is talking with in their own words.</summary>
    public enum TalkPartner
    {
        Collectors,
        Lender,
        Pyramid,

        /// <summary>Someone in town who gives advice about money.</summary>
        Neighbor,

        /// <summary>Someone in town who tries the SINPE scam.</summary>
        Stranger
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
    /// "Invierte Ya 300%" stand. A model writes the answers from <see cref="ToRequest"/>, which holds the true figures; when
    /// the backend cannot answer, <see cref="Say"/> follows fixed rules. Either way the other one never lies about a figure,
    /// only says it when asked, and every figure comes from the game's numbers. Pure, so it can be tested.
    /// </summary>
    public sealed class DebtTalk
    {
        /// <summary>The example loan the lender talks about, and the months of the short loan.</summary>
        public const long ExampleLoan = 100_000L;
        public const int ExampleMonths = 3;
        public const int LenderRatePct = (int)Finance.FinanceEngine.ShortInformalLoanMonthlyRatePct;
        public const int LenderLongRatePct = (int)Finance.FinanceEngine.DefaultInformalLoanMonthlyRatePct;

        /// <summary>The short loan's rate over a year, without counting interest on interest.</summary>
        public const int LenderYearlyPct = LenderRatePct * 12;

        /// <summary>
        /// The legal yearly cap of Law 9859 against usury: 36.48% in colones for the second half of 2026 (BCCR),
        /// said as "about 36%" because the talk only allows whole percents.
        /// </summary>
        public const int UsuryCapYearlyPct = 36;
        public const int PyramidPromisePct = 300;

        /// <summary>What the stranger of the SINPE scam asks to have sent back.</summary>
        public const long StrangerAsk = 30_000L;

        /// <summary>The monthly rate of an informal loan, which every neighbor knows to fear.</summary>
        private const int NeighborLoanRatePct = LenderRatePct;

        /// <summary>What the lender is willing to lend. The contract of the game allows up to the last one.</summary>
        public static readonly long[] LenderLoans = { ExampleLoan, 200_000L, 300_000L };

        public const string TakeOffer = "take";
        public const string SkipOffer = "skip";

        private readonly List<TalkTurn> turns = new List<TalkTurn>();
        private readonly List<TalkFact> asked = new List<TalkFact>();

        public TalkPartner Partner { get; }

        /// <summary>What the collectors came for today. Zero for the others.</summary>
        public long Demand { get; }

        public IReadOnlyList<TalkTurn> Turns => turns;
        public IReadOnlyList<TalkFact> Asked => asked;

        /// <summary>The monthly rate of the player's card, for the neighbor's advice.</summary>
        public decimal CardRatePct { get; }

        public DebtTalk(TalkPartner partner, long demand = 0L, decimal cardRatePct = CreditCard.TypicalMonthlyRatePct)
        {
            Partner = partner;
            Demand = demand;
            CardRatePct = cardRatePct;
        }

        /// <summary>The same picks for the same person: a quarter of the people of the town try the scam.</summary>
        public static TalkPartner PartnerFor(string personName)
        {
            int sum = 0;
            foreach (char letter in personName ?? string.Empty)
            {
                sum += letter;
            }

            return sum % 4 == 0 ? TalkPartner.Stranger : TalkPartner.Neighbor;
        }

        private string WirePartner
        {
            get
            {
                switch (Partner)
                {
                    case TalkPartner.Collectors: return "collectors";
                    case TalkPartner.Lender: return "lender";
                    case TalkPartner.Neighbor: return "neighbor";
                    case TalkPartner.Stranger: return "stranger";
                    default: return "pyramid";
                }
            }
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
                    case TalkPartner.Neighbor:
                        return "Vecino";
                    case TalkPartner.Stranger:
                        return "Desconocido";
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
                    case TalkPartner.Neighbor:
                        return "¿Todo bien? Si anda con dudas de plata, pregunte con confianza.";
                    case TalkPartner.Stranger:
                        return Loc.F("Disculpe, le mandé {0} por SINPE por error. ¿Me los devuelve ya? Es urgente.", Money.Format(StrangerAsk));
                    default:
                        return Loc.F("¡Duplicá, triplicá tu plata! Te pagamos {0}% en un mes. ¿Entrás o te lo vas a perder?", PyramidPromisePct);
                }
            }
        }

        /// <summary>The questions worth asking this one, in Spanish: the buttons of the talk and of the demo lab.</summary>
        public string[] SuggestedQuestions
        {
            get
            {
                switch (Partner)
                {
                    case TalkPartner.Collectors:
                        return new[] { "No tengo toda esa plata.", "¿Podemos hacer un arreglo?", "¿Qué pasa si no pago?", "¿Esto es legal?" };
                    case TalkPartner.Lender:
                        return new[] { "¿Cuál es el interés?", "¿Cuánto tendría que devolver en total?", "¿Qué pasa si me atraso?", "¿Firmo algún contrato?" };
                    case TalkPartner.Neighbor:
                        return new[] { "¿Qué tan caro sale la tarjeta?", "¿Conviene comprar a cuotas?", "¿Cómo logro ahorrar algo?", "¿Y si me ofrecen plata rápida?" };
                    case TalkPartner.Stranger:
                        return new[] { "¿De dónde sacó mi número?", "¿Por qué tanta prisa?", "¿Me manda el comprobante?", "¿Puedo llamar al banco primero?" };
                    default:
                        return new[] { "¿Cuánto me van a pagar?", "¿De dónde sale la plata?", "¿Hay algún contrato?", "¿Están inscritos en SUGEF?" };
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
            return LenderTotal(ExampleLoan);
        }

        /// <summary>What a loan costs at the end of the short term, month on month at the lender's rate.</summary>
        public static long LenderTotal(long loan)
        {
            long total = loan;
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

        /// <summary>Adds a turn the model wrote. What it says was asked adds to what the local rules recognize in the words.</summary>
        public void Record(string playerText, string reply, IEnumerable<TalkFact> askedFacts)
        {
            string text = playerText == null ? string.Empty : playerText.Trim();
            var recognized = Classify(text);
            if (recognized.HasValue && !asked.Contains(recognized.Value))
            {
                asked.Add(recognized.Value);
            }

            if (askedFacts != null)
            {
                foreach (var fact in askedFacts)
                {
                    if (!asked.Contains(fact))
                    {
                        asked.Add(fact);
                    }
                }
            }

            turns.Add(new TalkTurn { Player = text, Reply = reply });
        }

        /// <summary>
        /// What goes to the model for one turn: the true sheet, in the player's language, and the only amounts and
        /// percentages the character may say. The model writes the words and never computes a figure.
        /// </summary>
        public TalkRequestDto ToRequest(string playerText, string language)
        {
            var request = new TalkRequestDto
            {
                partner = WirePartner,
                text = playerText,
                language = language
            };

            switch (Partner)
            {
                case TalkPartner.Collectors:
                    FillCollectors(request);
                    break;
                case TalkPartner.Lender:
                    FillLender(request);
                    break;
                case TalkPartner.Neighbor:
                    FillNeighbor(request);
                    break;
                case TalkPartner.Stranger:
                    FillStranger(request);
                    break;
                default:
                    FillPyramid(request);
                    break;
            }

            foreach (var turn in turns)
            {
                request.history.Add(new TalkExchangeDto { player = turn.Player, reply = turn.Reply });
            }

            return request;
        }

        private void FillCollectors(TalkRequestDto request)
        {
            Line(request, TalkFact.TotalCost, Loc.F("Hoy vienes a cobrar {0}: lo atrasado, nada más.", Money.Format(Demand)));
            Line(request, TalkFact.Arrangement, Loc.F("Si pide un arreglo: {0} hoy y lo demás queda como deuda nueva de {1}, con {2}% de recargo.",
                Money.Format(ArrangementToday), Money.Format(ArrangementLater), MarketAssumptions.CollectionNegotiateSurchargePct));
            Line(request, TalkFact.WhatIfNotPaid, Loc.F("Si no paga hoy: una multa del {0}% sobre la deuda, y vuelves otro día.", MarketAssumptions.CollectionIgnoreFeePct));
            Line(request, TalkFact.Legal, Loc.T("Cobrar es legal; amenazar no. Si alguien lo amenaza, eso se denuncia."));
            request.amounts.AddRange(new[] { Demand, ArrangementToday, ArrangementLater });
            request.percents.AddRange(new[] { MarketAssumptions.CollectionNegotiateSurchargePct, MarketAssumptions.CollectionIgnoreFeePct });
        }

        private static void FillLender(TalkRequestDto request)
        {
            Line(request, TalkFact.Rate, Loc.F("Cobras el {0}% al mes a {1} meses o menos, y el {2}% a más plazo.", LenderRatePct, ExampleMonths, LenderLongRatePct));
            Line(request, TalkFact.TotalCost, LenderTerms("Prestás {0}, {1} o {2}, nada más. A {3} meses te devuelven {4}, {5} o {6}."));
            Line(request, TalkFact.Arrangement, Loc.T("No hay rebajas: pasas todos los días a cobrar la cuota, gota a gota."));
            Line(request, TalkFact.WhatIfNotPaid, Loc.T("Si se atrasa, los intereses siguen corriendo y mandas a los muchachos del carro negro."));
            Line(request, TalkFact.Legal, Loc.F("Sin papeles y sin SUGEF: esto es informal, entre los dos. Tu {0}% al mes son {1}% al año, y la ley contra la usura topa cualquier préstamo en unos {2}% al año.",
                LenderRatePct, LenderYearlyPct, UsuryCapYearlyPct));
            Line(request, TalkFact.Guarantee, Loc.T("Ningún contrato: solo una firma en tu libreta."));
            foreach (long loan in LenderLoans)
            {
                request.amounts.AddRange(new[] { loan, LenderTotal(loan) });
            }

            request.percents.AddRange(new[] { LenderRatePct, LenderLongRatePct, LenderYearlyPct, UsuryCapYearlyPct });
        }

        private void FillNeighbor(TalkRequestDto request)
        {
            Line(request, TalkFact.Rate, Loc.F("Una tarjeta como la del jugador cobra {0}% al mes sobre lo que queda debiendo: pagar solo el mínimo hace crecer la deuda.", CardRatePct.ToString("0.#", CultureInfo.InvariantCulture)));
            Line(request, TalkFact.TotalCost, Loc.T("Comprar a cuotas casi siempre sale más caro que de contado: hay que preguntar el total, el seguro y la comisión."));
            Line(request, TalkFact.Arrangement, Loc.T("Lo que funciona es apartar algo cada quincena, aunque sea poco, antes de gastar."));
            Line(request, TalkFact.WhatIfNotPaid, Loc.F("Un prestamista gota a gota cobra hasta {0}% al mes y manda a cobrar a la casa: mejor evitarlo.", NeighborLoanRatePct));
            Line(request, TalkFact.Legal, Loc.T("Si alguien promete ganancias enormes, hay que buscarlo en SUGEF antes de darle un colón."));
            Line(request, TalkFact.Guarantee, Loc.T("Un SINPE no se devuelve: hay que revisar el nombre antes de enviar."));
            request.percents.AddRange(new[] { (int)CardRatePct, NeighborLoanRatePct });
        }

        private static void FillStranger(TalkRequestDto request)
        {
            Line(request, TalkFact.TotalCost, Loc.F("Pedís que te devuelvan {0} por SINPE, diciendo que los mandaste por error.", Money.Format(StrangerAsk)));
            Line(request, TalkFact.WhereMoneyComes, Loc.T("No hay ningún depósito: es mentira, pero solo lo admitís si te presionan mucho."));
            Line(request, TalkFact.Guarantee, Loc.T("No tenés comprobante, y das excusas si te lo piden."));
            Line(request, TalkFact.Legal, Loc.T("Si mencionan al banco o a la policía, te ponés nervioso y querés terminar la conversación."));
            request.amounts.Add(StrangerAsk);
        }

        private static void FillPyramid(TalkRequestDto request)
        {
            long promised = ExampleLoan * (100L + PyramidPromisePct) / 100L;
            Line(request, TalkFact.TotalCost, Loc.F("Prometes que {0} se vuelven {1} en un mes: {2}% de ganancia.", Money.Format(ExampleLoan), Money.Format(promised), PyramidPromisePct));
            Line(request, TalkFact.WhereMoneyComes, Loc.T("La plata sale de los nuevos que entran: por eso cada uno tiene que traer a tres amigos."));
            Line(request, TalkFact.Guarantee, Loc.T("No hay contrato ni garantía, solo confianza."));
            Line(request, TalkFact.Legal, Loc.T("Ustedes no están inscritos en SUGEF, y lo sabes."));
            Line(request, TalkFact.WhatIfNotPaid, Loc.T("Solo les pagas a los primeros que entraron."));
            Line(request, TalkFact.Arrangement, Loc.T("Si mete más plata hoy, lo suben al nivel oro."));
            request.amounts.AddRange(new[] { ExampleLoan, promised });
            request.percents.Add(PyramidPromisePct);
        }

        private static string LenderTerms(string spanish)
        {
            return Loc.F(spanish, Money.Format(LenderLoans[0]), Money.Format(LenderLoans[1]), Money.Format(LenderLoans[2]), ExampleMonths,
                Money.Format(LenderTotal(LenderLoans[0])), Money.Format(LenderTotal(LenderLoans[1])), Money.Format(LenderTotal(LenderLoans[2])));
        }

        private static void Line(TalkRequestDto request, TalkFact fact, string text)
        {
            request.sheet.Add(new TalkSheetLineDto { fact = WireEnum<TalkFact>.ToWire(fact), text = text });
        }

        /// <summary>
        /// The offer of the lender or of the pyramid as an event: what the player signs, with the figures of the sheet. It is not
        /// asked of a model, because the game master does not know where the player is standing.
        /// </summary>
        public static GameEvent OfferEvent(TalkPartner partner)
        {
            bool lender = partner == TalkPartner.Lender;
            string narrative = lender
                ? Loc.F("Hoy mismo, sin papeles. A {0} meses, con el {1}% al mes, cada {2} se vuelven {3}.", ExampleMonths, LenderRatePct, Money.Format(ExampleLoan), Money.Format(LenderTotal()))
                : Loc.F("Ponés {0} y en un mes te prometen {1}. No hay contrato: es de confianza.", Money.Format(ExampleLoan), Money.Format(ExampleLoan * (100L + PyramidPromisePct) / 100L));
            return new GameEvent
            {
                EventId = lender ? "evt_offer_lender" : "evt_offer_pyramid",
                Type = lender ? EventType.Offer : EventType.Scam,
                Title = lender ? Loc.T("Plata rápida") : Loc.F("Tu plata, {0}% en un mes", PyramidPromisePct),
                Narrative = narrative,
                Npc = lender ? new Npc("prestamista_gota_a_gota", Loc.T("Prestamista"), "lender") : new Npc("amigo_piramide", "Invierte Ya", "promoter"),
                ImageKey = "scene_esquina_barrio",
                TeachingConcept = lender ? TeachingConcept.InformalLending : TeachingConcept.PyramidScheme,
                Choices = OfferChoices(lender)
            };
        }

        private static List<Choice> OfferChoices(bool lender)
        {
            var choices = new List<Choice>();
            if (lender)
            {
                for (int i = 0; i < LenderLoans.Length; i++)
                {
                    long loan = LenderLoans[i];
                    var effects = new List<Effect> { new Effect(EffectKind.NewInformalLoan, loan, null, ExampleMonths) };
                    choices.Add(new Choice(i == 0 ? TakeOffer : TakeOffer + "_" + loan, Loc.F("Agarrar {0} y devolver {1}", Money.Format(loan), Money.Format(LenderTotal(loan))), effects));
                }
            }
            else
            {
                choices.Add(new Choice(TakeOffer, Loc.F("Meter {0}", Money.Format(ExampleLoan)), new List<Effect> { new Effect(EffectKind.Expense, ExampleLoan, MoneySource.Savings) }));
            }

            choices.Add(new Choice(SkipOffer, Loc.T("Mejor no"), new List<Effect>()));
            return choices;
        }

        /// <summary>The history tags of the questions asked, for the metrics of the pilot.</summary>
        public IEnumerable<string> HistoryTags()
        {
            string who = WirePartner;
            foreach (var fact in asked)
            {
                yield return "asked_" + who + "_" + fact.ToString().ToLowerInvariant();
            }
        }

        public static TalkFact? Classify(string text)
        {
            string t = Normalize(text);
            if (Has(t, "de donde sale", "de donde viene", "como ganan", "como funciona", "referid", "meter gente", "nuevos", "come from", "comes from", "how does it work", "how do you make", "recruit", "new people", "new members"))
            {
                return TalkFact.WhereMoneyComes;
            }

            if (Has(t, "garantia", "contrato", "papel", "recibo", "comprobante", "por escrito", "seguro que", "guarantee", "contract", "paper", "receipt", "proof", "in writing"))
            {
                return TalkFact.Guarantee;
            }

            if (Has(t, "legal", "sugef", "permiso", "inscrit", "policia", "banco", "denunc", "ley", "amenaz", "permit", "licen", "police", "bank", "report you", "threat", "the law"))
            {
                return TalkFact.Legal;
            }

            if (Has(t, "arreglo", "negoci", "mitad", "plazo", "cuotas", "partes", "rebaj", "descuento", "abono", "despues", "otro dia", "arrangement", "work something out", "half", "installment", "payment plan", "discount", "lower", "later", "another day", "tomorrow", "next week"))
            {
                return TalkFact.Arrangement;
            }

            if (Has(t, "no pago", "no tengo", "no puedo", "si no", "que pasa", "plata rapida", "don't pay", "dont pay", "can't pay", "cant pay", "cannot pay", "don't have", "dont have", "what happens", "fall behind", "if i don't", "quick money", "fast money"))
            {
                return TalkFact.WhatIfNotPaid;
            }

            if (Has(t, "interes", "tasa", "porcentaje", "%", "por ciento", "al mes", "cobran", "tarjeta", "interest", "rate", "percent", "a month", "per month", "card"))
            {
                return TalkFact.Rate;
            }

            if (Has(t, "total", "cuanto", "en total", "al final", "devolver", "pagaria", "cuotas", "how much", "in the end", "pay back", "give back", "owe", "installment"))
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
                case TalkPartner.Neighbor:
                    return NeighborAnswer(fact);
                case TalkPartner.Stranger:
                    return StrangerAnswer(fact);
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
                    return Loc.F("Si no paga hoy, le cae una multa del {0}% sobre la deuda y volvemos. Usted verá.", MarketAssumptions.CollectionIgnoreFeePct);
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
                    return Loc.F("Es el {0}% al mes a {1} meses o menos, y el {2}% a más plazo, mae. Poquito, ¿verdad?", LenderRatePct, ExampleMonths, LenderLongRatePct);
                case TalkFact.TotalCost:
                    return LenderTerms("Le presto {0}, {1} o {2}. A {3} meses me devuelve {4}, {5} o {6}.");
                case TalkFact.Arrangement:
                    return "Aquí no hay rebajas. Yo paso todos los días a cobrar la cuota, gota a gota.";
                case TalkFact.WhatIfNotPaid:
                    return "Si se atrasa, los intereses siguen corriendo y le mando a los muchachos del carro negro.";
                case TalkFact.Legal:
                    return Loc.F("¿Papeles, SUGEF? No, mae, esto es entre usted y yo. ¿La ley de usura? Topa en unos {0}% al año y lo mío son {1}%, pero por eso es tan rápido.",
                        UsuryCapYearlyPct, LenderYearlyPct);
                case TalkFact.Guarantee:
                    return "Ningún papel. Usted me firma aquí en la libreta y listo.";
                default:
                    return "Usted pregunte lo que quiera, pero la plata vuela. ¿Cuánto ocupa?";
            }
        }

        private string NeighborAnswer(TalkFact? fact)
        {
            switch (fact)
            {
                case TalkFact.Rate:
                    return Loc.F("La tarjeta cobra {0}% al mes sobre lo que queda. Pagando solo el mínimo no sale nunca.", CardRatePct.ToString("0.#", CultureInfo.InvariantCulture));
                case TalkFact.TotalCost:
                case TalkFact.Arrangement:
                    return "A cuotas casi siempre sale más caro. Pregunte el total, y aparte algo cada quincena, aunque sea poco.";
                case TalkFact.WhatIfNotPaid:
                    return Loc.F("Un gota a gota cobra hasta {0}% al mes y le manda gente a la casa. Mejor no.", NeighborLoanRatePct);
                case TalkFact.Legal:
                case TalkFact.WhereMoneyComes:
                    return "Si le prometen ganancias enormes, búsquelos en SUGEF antes de darles un colón.";
                case TalkFact.Guarantee:
                    return "Un SINPE no se devuelve. Revise bien el nombre antes de enviar.";
                default:
                    return "Yo aprendí a golpes: antes de firmar, pregunte cuánto paga en total.";
            }
        }

        private static string StrangerAnswer(TalkFact? fact)
        {
            switch (fact)
            {
                case TalkFact.Guarantee:
                    return "¿Comprobante? Ahorita no lo tengo a mano, pero créame, por favor.";
                case TalkFact.Legal:
                    return "No, no, no hace falta llamar a nadie. Es rapidito, mándelo y ya.";
                case TalkFact.WhereMoneyComes:
                    return "Se lo juro que le cayó. Revise después, ahora ocupo la plata.";
                default:
                    return Loc.F("Es urgente, de verdad. Devuélvame los {0} y no le quito más tiempo.", Money.Format(StrangerAsk));
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
