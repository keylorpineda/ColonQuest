using System.Collections.Generic;
using System.Text;

namespace ColonQuest.Game
{
    public sealed class GlossaryEntry
    {
        public string Title;
        public string Text;
        public string[] Keywords;
    }

    /// <summary>
    /// The phone's search engine: fixed, short explanations of the money ideas the game teaches. It never
    /// invents figures; it points to the habit that protects the player. Matching ignores accents and case.
    /// </summary>
    public static class FinanceGlossary
    {
        public static readonly IReadOnlyList<GlossaryEntry> Entries = new[]
        {
            new GlossaryEntry
            {
                Title = "Tarjeta de crédito",
                Keywords = new[] { "tarjeta", "credito", "minimo", "pago minimo", "card", "credit", "minimum" },
                Text = "Pagar solo el mínimo mantiene la deuda viva y los intereses siguen corriendo cada mes sobre lo que debés. Si podés, pagá el saldo completo antes de la fecha de corte."
            },
            new GlossaryEntry
            {
                Title = "Intereses",
                Keywords = new[] { "interes", "intereses", "tasa", "interes compuesto", "interest", "rate", "compound" },
                Text = "El interés es lo que cuesta usar plata ajena. En una deuda, los intereses se suman al saldo y el mes siguiente se cobran intereses también sobre ellos: eso es el interés compuesto."
            },
            new GlossaryEntry
            {
                Title = "Compras a cuotas",
                Keywords = new[] { "cuota", "cuotas", "financiamiento", "plazo", "costo total", "installment", "installments", "financing", "term", "total cost" },
                Text = "Antes de aceptar una cuota preguntá el costo total: cuota por meses más seguros y comisiones. Compará ese total con el precio de contado y decidí con ese número, no con la cuota."
            },
            new GlossaryEntry
            {
                Title = "Ahorro y fondo de emergencia",
                Keywords = new[] { "ahorro", "ahorrar", "emergencia", "fondo", "savings", "save", "emergency", "fund" },
                Text = "Un fondo de emergencia te evita endeudarte cuando algo se arruina. Una guía común es juntar entre tres y seis meses de gastos, empezando por un monto pequeño cada mes."
            },
            new GlossaryEntry
            {
                Title = "SINPE Móvil",
                Keywords = new[] { "sinpe", "transferencia", "transferir", "numero", "transfer", "number" },
                Text = "Antes de enviar, revisá el nombre que aparece junto al número. Una transferencia por SINPE es inmediata y no se devuelve sola: si te equivocás o te estafan, recuperarla es muy difícil."
            },
            new GlossaryEntry
            {
                Title = "Estafas",
                Keywords = new[] { "estafa", "estafas", "fraude", "engano", "phishing", "link", "scam", "scams", "fraud" },
                Text = "Desconfiá de lo urgente: premios que no pediste, familiares que piden plata por mensaje, links de tu banco. Nadie legítimo te pide claves ni códigos. Si dudás, llamá vos al número oficial."
            },
            new GlossaryEntry
            {
                Title = "Gota a gota",
                Keywords = new[] { "gota", "gota a gota", "prestamista", "prestamo informal", "prestamos", "loan shark", "informal loan", "loans" },
                Text = "El préstamo gota a gota da plata rápido sin papeles, pero cobra intereses altísimos y a veces amenaza. Un préstamo formal en un banco o cooperativa cuesta mucho menos."
            },
            new GlossaryEntry
            {
                Title = "Esquemas piramidales",
                Keywords = new[] { "piramide", "piramidal", "inversion segura", "ganancia garantizada", "300", "pyramid", "guaranteed profit" },
                Text = "Si te prometen una ganancia enorme y segura, es una señal de alarma. Las inversiones reales tienen riesgo y las ganancias de una pirámide salen de lo que ponen los que entran después."
            },
            new GlossaryEntry
            {
                Title = "Aguinaldo",
                Keywords = new[] { "aguinaldo", "diciembre", "bonus", "december" },
                Text = "El aguinaldo es un pago extra que recibe quien trabaja con planilla en diciembre. Es buena idea decidir de antemano en qué lo vas a usar: saldar deudas y ahorrar antes de gastar."
            },
            new GlossaryEntry
            {
                Title = "Marchamo",
                Keywords = new[] { "marchamo", "derechos de circulacion", "rtv", "registration", "inspection" },
                Text = "Tener carro cuesta más que la cuota: marchamo cada año, revisión técnica, seguro, gasolina y mantenimiento. Sumalo todo antes de comprar."
            },
            new GlossaryEntry
            {
                Title = "Inflación",
                Keywords = new[] { "inflacion", "precios", "subida", "inflation", "prices" },
                Text = "La inflación hace que la misma plata compre menos con el tiempo. Por eso ahorrar debajo del colchón pierde valor, y por eso conviene comparar rendimientos."
            },
            new GlossaryEntry
            {
                Title = "Alquilar o comprar casa",
                Keywords = new[] { "casa", "alquiler", "alquilar", "hipoteca", "vivienda", "bienes raices", "house", "rent", "mortgage", "home" },
                Text = "Alquilar da flexibilidad y no te deja deuda. Comprar con hipoteca crea un patrimonio, pero pagás intereses por años, más prima, seguros y mantenimiento. Compará la cuota total con lo que podés pagar."
            },
            new GlossaryEntry
            {
                Title = "Comprar un carro",
                Keywords = new[] { "carro", "vehiculo", "auto", "agencia", "prima", "car", "vehicle", "dealership", "down payment" },
                Text = "Un carro pierde valor cada año. Pedí el costo total del financiamiento, la prima que te piden y los gastos fijos mensuales antes de decidir. Comprar usado suele costar menos."
            },
            new GlossaryEntry
            {
                Title = "Invertir en café o ganado",
                Keywords = new[] { "cafe", "ganado", "finca", "inversion", "invertir", "cosecha", "coffee", "cattle", "farm", "invest", "harvest" },
                Text = "Una finca no da ganancia de un mes a otro: el café depende de la cosecha y del precio, y el ganado tarda más de un año. Los precios cambian y no hay ganancia garantizada. No pongás ahí plata que necesitás."
            },
            new GlossaryEntry
            {
                Title = "Casino y apuestas",
                Keywords = new[] { "casino", "apuesta", "apuestas", "azar", "loteria", "bet", "betting", "gambling", "lottery" },
                Text = "En un casino la casa siempre gana a largo plazo: los premios chicos te animan a seguir y las pérdidas grandes llegan solas. Jugar es un gasto, no una forma de ganar plata."
            }
        };

        /// <summary>The entry whose keywords best match the question, or null when nothing matches.</summary>
        public static GlossaryEntry Search(string query)
        {
            string text = Normalize(query);
            if (text.Length < 2)
            {
                return null;
            }

            GlossaryEntry best = null;
            int bestScore = 0;
            foreach (var entry in Entries)
            {
                int score = 0;
                foreach (string keyword in entry.Keywords)
                {
                    if (text.Contains(Normalize(keyword)))
                    {
                        score += Normalize(keyword).Length;
                    }
                }

                if (score > bestScore)
                {
                    best = entry;
                    bestScore = score;
                }
            }

            return best;
        }

        private const string Accented = "áéíóúüñàèìòù";
        private const string Plain = "aeiouunaeiou";

        /// <summary>Lowercase and without accents, so "Tarjeta de Crédito" and "tarjeta de credito" are the same.</summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var result = new StringBuilder(text.Length);
            foreach (char c in text.Trim().ToLowerInvariant())
            {
                int index = Accented.IndexOf(c);
                result.Append(index >= 0 ? Plain[index] : c);
            }

            return result.ToString();
        }
    }
}
