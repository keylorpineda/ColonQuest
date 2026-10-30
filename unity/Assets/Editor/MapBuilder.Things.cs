using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// Everyday objects and people the player can use by walking up and pressing E. They only talk:
    /// money moves through the finance engine, never through these lines. Amounts in the lines come
    /// from the player's state through the {cash}, {savings}, {card} and {salary} tokens.
    /// </summary>
    public static partial class MapBuilder
    {
        private static void BuildHouseThings(InteriorParts parts)
        {
            var things = parts.Stations;
            BuildThing(things, "Bed", "Acostarte un rato", new Vector2(-5.1f, 3.85f), new Vector2(2.3f, 2.4f),
                "Te tirás un rato en la cama. Dormir bien también es plata: cansado se compra por impulso.",
                "Mañana hay que trabajar. Ponés la alarma y pensás en lo que te queda: {cash} en efectivo.");
            BuildStation(things, "Station_Wardrobe", StationType.Wardrobe, "Abrir el ropero", new Vector2(-7.6f, 1.0f));
            BuildThing(things, "Shower", "Bañarte", new Vector2(0.8f, 4.35f), new Vector2(1.35f, 1.45f),
                "Agua caliente con la ducha eléctrica. Baños cortos, recibo de luz más bajito.");
            BuildThing(things, "Sink", "Lavarte las manos", new Vector2(-1.5f, 2.4f), new Vector2(0.6f, 0.9f),
                "Cerrás bien el tubo. Una gota que cae todo el día se nota en el recibo del agua.");
            BuildThing(things, "Stove", "Cocinar", new Vector2(5.5f, 4.7f), new Vector2(1.1f, 0.8f),
                "Gallo pinto, huevo y café. Cocinar en casa sale mucho más barato que comer afuera todos los días.",
                "Hacés almuerzo para llevar mañana. Eso son unos ₡3 000 que no salen de la bolsa.");
            BuildThing(things, "Fridge", "Abrir la refri", new Vector2(7.5f, 4.45f), new Vector2(1.0f, 1.4f),
                "Queda medio litro de leche y un tomate. Hacer la lista antes de ir a la pulpería ayuda a no comprar de más.",
                "La refri está llena de lo que compraste en la feria. Más barato que el súper y más fresco.");
            BuildThing(things, "Tv", "Ver tele", new Vector2(-7.4f, -1.4f), new Vector2(0.4f, 1.0f),
                "Anuncio: \"¡Llévelo hoy con solo ₡9 900 al mes!\". No dicen cuántos meses ni cuánto suma.",
                "Noticias: el colón se movió frente al dólar. Si tus deudas son en dólares, eso te afecta.");
            BuildThing(things, "Sofa", "Sentarte en el sillón", new Vector2(-3.6f, -1.8f), new Vector2(0.4f, 1.2f),
                "Revisás el celular: tenés {cash} en efectivo y {card} en la tarjeta.");
            BuildThing(things, "DiningTable", "Sentarte a la mesa", new Vector2(5.0f, -2.6f), new Vector2(2.4f, 1.3f),
                "Sobre la mesa hay recibos: luz, agua, internet. Pagarlos a tiempo evita recargos.",
                "Anotás el presupuesto del mes: salario de {salary}, primero lo fijo, luego el ahorro, luego lo demás.");
        }

        private static void BuildBankThings(InteriorParts parts)
        {
            var things = parts.Stations;
            foreach (float y in new[] { -1.0f, -2.6f, -4.2f })
            {
                BuildAtm(things, "Atm" + Mathf.RoundToInt(-y * 10f), new Vector2(10.6f, y));
            }

            BuildThing(things, "TicketMachine", "Sacar un tiquete", new Vector2(-2.6f, -6.0f), new Vector2(0.8f, 0.9f),
                "Te sale el número 47. Van por el 32. Mucho trámite se puede hacer desde la app del banco.");
            BuildThing(things, "RatesBoard", "Ver el tipo de cambio", new Vector2(-10.6f, -6.4f), new Vector2(2.2f, 1.1f),
                "El banco compra el dólar más barato de lo que lo vende. Cambiar plata ida y vuelta siempre cuesta.");
            BuildThing(things, "Guard", "Saludar al guarda", new Vector2(2.4f, -6.6f), new Vector2(0.6f, 0.6f),
                "\"Buenas. Ojo: el banco nunca le pide la clave por teléfono ni por WhatsApp.\"");
            BuildThing(things, "Receptionist", "Preguntar en información", new Vector2(5.6f, -5.3f), new Vector2(2.4f, 0.6f),
                "\"Para abrir una cuenta de ahorro solo ocupa la cédula. Puede programar que se le rebaje algo cada quincena.\"",
                "\"Un certificado a plazo le paga más interés que la cuenta, pero no lo puede sacar antes.\"");
            BuildThing(things, "Teller3", "Hablar con el cajero", new Vector2(TellerWindows[3], TellerCounterY - 1.2f), new Vector2(1.4f, 1.0f),
                "\"Esta caja es solo para depósitos. Para cobrar el salario vaya a la caja 2.\"");
        }

        private static void BuildStoreThings(InteriorParts parts)
        {
            var things = parts.Stations;
            BuildThing(things, "Screens", "Ver los televisores", new Vector2(-4.6f, 3.9f), new Vector2(7.6f, 0.4f),
                "Una pantalla de 55 pulgadas: ₡399 000 de contado o 36 cuotas de ₡17 900. Sumalo: son ₡644 400.",
                "La etiqueta grande dice la cuota. La chiquita dice el plazo. Siempre leé la chiquita.");
            BuildThing(things, "Fridge", "Ver la refri nueva", new Vector2(2.2f, 3.8f), new Vector2(1.0f, 0.4f),
                "Una refri inverter gasta menos luz. Sale más cara hoy, pero se paga sola en el recibo.");
            BuildThing(things, "Washer", "Ver la lavadora", new Vector2(5.2f, 3.9f), new Vector2(0.9f, 0.4f),
                "\"Sin prima\", dice el rótulo. Sin prima no significa sin intereses.");
            BuildThing(things, "Motorbike", "Ver la moto", new Vector2(7.4f, 1.6f), new Vector2(2.4f, 1.4f),
                "La moto sale en cuotas, pero también hay marchamo, seguro, RTV y gasolina todos los meses.");
            BuildThing(things, "Accessories", "Ver los accesorios", new Vector2(-7.7f, -1.8f), new Vector2(0.4f, 2.6f),
                "Audífonos, cargadores, fundas. Lo pequeño que se compra cada semana también suma al mes.");
            BuildThing(things, "Cashier", "Hablar con el cajero", new Vector2(-5.0f, -5.2f), new Vector2(2.4f, 0.6f),
                "\"¿Con tarjeta? ¿Le pongo tasa cero a 12 meses?\" Tasa cero solo es cero si pagás cada cuota a tiempo.");
        }

        /// <summary>What a passerby says. Chosen by name so each one keeps the same voice.</summary>
        private static string[] PasserbyLines(string name)
        {
            string[][] voices =
            {
                new[] { "\"¡Pura vida! Diay, ¿ya cobró? Yo apenas cobro y aparto lo del ahorro, antes de que se me vaya.\"" },
                new[] { "\"Mae, me ofrecieron una plata rápida sin papeleo... después me cobraban el doble. Mejor no.\"" },
                new[] { "\"Voy para la feria del agricultor. Sale más barato y uno apoya a los productores.\"" },
                new[] { "\"Un compa me quiere meter en un negocio que paga 300%. Eso no existe, ¿verdad?\"" },
                new[] { "\"Este mes me cae el marchamo y ni me acordaba. Hay que ir guardando desde antes.\"" },
                new[] { "\"Por SINPE Móvil es facilito, pero fíjese bien el número antes de mandar.\"" }
            };

            int index = 0;
            foreach (char c in name)
            {
                index = (index * 31 + c) & 0x7fffffff;
            }

            return voices[index % voices.Length];
        }
    }
}
