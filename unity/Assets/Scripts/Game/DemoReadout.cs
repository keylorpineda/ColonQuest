using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.World;

namespace ColonQuest.Game
{
    /// <summary>
    /// Turns what the backend reports into what a person can read in the demo lab. Nothing is made up here: a call that
    /// did not happen does not appear, and a reply from the fallback says so.
    /// </summary>
    public static class DemoReadout
    {
        /// <summary>The twenty pictures of the backend (server/app/images/keys.py), all pregenerated with Muse Image.</summary>
        public static readonly string[] Portraits =
        {
            "npc_don_rafa_mecanico", "npc_kevin_vendedor", "npc_dona_marta_vendedora", "npc_prestamista_gota_a_gota", "npc_amigo_piramide"
        };

        public static readonly string[] Scenes =
        {
            "scene_consultorio", "scene_cooperativa", "scene_esquina_barrio", "scene_estado_cuenta", "scene_gasolinera", "scene_ins",
            "scene_navidad", "scene_oficina", "scene_soda", "scene_taller_mecanico", "scene_tienda_celulares", "scene_tienda_electro"
        };

        private static readonly Dictionary<string, string> ConceptNames = new Dictionary<string, string>
        {
            { "budget", "Presupuesto" },
            { "emergency_fund", "Fondo de emergencia" },
            { "compound_interest", "Interés compuesto" },
            { "minimum_payment", "Pago mínimo" },
            { "installments_total_cost", "Costo real de las cuotas" },
            { "informal_lending", "Préstamos gota a gota" },
            { "pyramid_scheme", "Esquemas piramidales" },
            { "inflation", "Inflación" },
            { "exchange_rate", "Tipo de cambio" },
            { "payroll_deductions", "Rebajos del salario" },
            { "invest_in_yourself", "Invertir en vos" }
        };

        public static string ConceptName(string wire)
        {
            return ConceptNames.TryGetValue(wire ?? string.Empty, out var name) ? Loc.T(name) : wire ?? string.Empty;
        }

        public static string ConceptName(TeachingConcept concept)
        {
            return ConceptName(WireEnum<TeachingConcept>.ToWire(concept));
        }

        /// <summary>The name of the model as the competition knows it. The open one comes first: its id contains the other.</summary>
        public static string ModelName(string model)
        {
            string id = (model ?? string.Empty).ToLowerInvariant();
            if (id.Contains("spark-open"))
            {
                return "Muse Spark Open";
            }

            if (id.Contains("spark"))
            {
                return "Muse Spark";
            }

            if (id.Contains("wonder"))
            {
                return "Muse Wonder";
            }

            if (id.Contains("voice") || id.Contains("transcribe"))
            {
                return "Muse Voice Transcribe";
            }

            return id.Contains("image") ? "Muse Image" : model ?? string.Empty;
        }

        public static string ProviderName(string provider)
        {
            return (provider ?? string.Empty).Contains("open") ? "Baseten" : Loc.T("API de Meta");
        }

        public static string JobOf(string endpoint)
        {
            switch (endpoint)
            {
                case "event_agent": return Loc.T("Game master con herramientas");
                case "event": return Loc.T("Evento del mes");
                case "negotiation_agent": return Loc.T("Vendedor con herramientas");
                case "negotiation": return Loc.T("Respuesta del vendedor");
                case "transcribe": return Loc.T("Voz a texto");
                case "season_plan": return Loc.T("Plan del año");
                case "summary": return Loc.T("Resumen del año");
                case "class_dashboard": return Loc.T("Informe de la clase");
                default: return endpoint ?? string.Empty;
            }
        }

        /// <summary>Who wrote a text, in the words of the badge next to it.</summary>
        public static string OriginOf(string origin)
        {
            switch (origin)
            {
                case "agent": return Loc.T("Lo escribió el agente, con sus herramientas");
                case "model": return Loc.T("Lo escribió el modelo");
                case "cache": return Loc.T("Salió de la caché: no gastó nada");
                case "fallback": return Loc.T("Respaldo del servidor: el modelo no respondió a tiempo");
                case "local": return Loc.T("Respaldo local: el servidor no respondió");
                default: return Loc.T("Respondió el servidor");
            }
        }

        public static bool WrittenByAModel(string origin)
        {
            return origin == "agent" || origin == "model";
        }

        /// <summary>True once the plan of the year stored in the session is the one the model wrote, not the base plan.</summary>
        public static bool PlannedByTheModel(SessionTraceDto trace)
        {
            if (trace == null || trace.seasonPlan == null || trace.seasonPlan.Count == 0)
            {
                return false;
            }

            foreach (var call in trace.calls)
            {
                if (call.endpoint == "season_plan" && call.ok)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the planner already answered, well or badly, so there is nothing left to wait for.</summary>
        public static bool PlannerAnswered(SessionTraceDto trace)
        {
            if (trace == null)
            {
                return false;
            }

            foreach (var call in trace.calls)
            {
                if (call.endpoint == "season_plan")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>What a proposed effect does, in words: what the player reads next to each choice.</summary>
        public static string Describe(Effect effect)
        {
            switch (effect.Kind)
            {
                case EffectKind.Expense:
                    return Loc.F("Gasto de {0}", Money.Format(effect.Amount)) + " (" + SourceName(effect.Source) + ")";
                case EffectKind.Income:
                    return Loc.F("Ingreso de {0}", Money.Format(effect.Amount));
                case EffectKind.NewInstallment:
                    return Loc.F("Cuota de {0} por {1} meses", Money.Format(effect.Amount), effect.Months ?? 0);
                case EffectKind.NewInformalLoan:
                    return Loc.F("Préstamo informal de {0}", Money.Format(effect.Amount));
                case EffectKind.SavingsChange:
                    return Loc.F("Ahorro {0}", Signed(effect.Amount));
                case EffectKind.WellbeingChange:
                    return Loc.F("Bienestar {0}", (effect.Amount >= 0L ? "+" : string.Empty) + effect.Amount.ToString(CultureInfo.InvariantCulture));
                default:
                    return Loc.F("Inflación del {0}%", effect.Amount);
            }
        }

        public static string DescribeAll(Choice choice)
        {
            var text = new StringBuilder();
            foreach (var effect in choice.Effects)
            {
                if (text.Length > 0)
                {
                    text.Append("  ·  ");
                }

                text.Append(Describe(effect));
            }

            return text.Length > 0 ? text.ToString() : Loc.T("Sin cambios en la plata");
        }

        public static string Dollars(float amount)
        {
            return "$" + amount.ToString(amount >= 1f ? "0.00" : "0.0000", CultureInfo.InvariantCulture);
        }

        /// <summary>A thousands-separated whole number: 1 200.</summary>
        public static string Whole(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ");
        }

        private static string Signed(long amount)
        {
            return (amount >= 0L ? "+" : "-") + Money.Format(amount >= 0L ? amount : -amount);
        }

        private static string SourceName(MoneySource? source)
        {
            switch (source)
            {
                case MoneySource.Savings: return Loc.T("de los ahorros");
                case MoneySource.EmergencyFund: return Loc.T("del fondo de emergencia");
                case MoneySource.CreditCard: return Loc.T("a la tarjeta");
                default: return Loc.T("en efectivo");
            }
        }
    }
}
