using System.Collections.Generic;

namespace ColonQuest.World
{
    /// <summary>
    /// The English text, keyed by the Spanish one. Written as a Costa Rican would say it to a friend in English:
    /// "plata" is money or cash, "mae" is dude, and the words that have no English twin keep their name with a short
    /// explanation the first time (marchamo, SINPE, aguinaldo, pulpería, soda). Placeholders such as {0} must stay
    /// exactly as in the Spanish text; a test checks it.
    /// </summary>
    public static class Translations
    {
        public static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            // ---- main menu
            { "Modo historia", "Story mode" },
            { "Jugar online", "Play online" },
            { "Ajustes", "Settings" },
            { "Créditos", "Credits" },
            { "Salir", "Quit" },
            { "Disponible en la versión completa", "Available in the full version" },
            { "Entendido", "Got it" },
            { "Volver", "Back" },
            { "Aprendé a manejar tu plata", "Learn to handle your money" },
            { "alfa", "alpha" },
            { "Versión alfa. Si algo se rompe, avisanos y lo arreglamos.", "Alpha version. If something breaks, let us know and we'll fix it." },
            { "Hecho por DoWhile Team", "Made by DoWhile Team" },
            { "Keylor y Jefferson", "Keylor and Jefferson" },
            { "Herramientas de Meta", "Meta tools" },
            { "El game master que cuenta cada evento y da voz a los personajes", "The game master that tells every event and gives the characters a voice" },
            { "El resumen final de tu partida", "The final summary of your run" },
            { "Entiende lo que le decís al vendedor", "Understands what you say to the seller" },

            // ---- settings and pause
            { "Sonido", "Sound" },
            { "Sonido: activado", "Sound: on" },
            { "Sonido: desactivado", "Sound: off" },
            { "Volumen", "Volume" },
            { "Pausa", "Paused" },
            { "Reanudar", "Resume" },
            { "Volver al menú", "Back to the menu" },
            { "¿Seguro? Se pierde la partida", "Sure? This run will be lost" },

            // ---- the HUD
            { "Efectivo", "Cash" },
            { "Ahorros", "Savings" },
            { "Tarjeta", "Card" },
            { "Cuotas al mes", "Installments / month" },
            { "Ánimo", "Mood" },
            { "[P]  Celular", "[P]  Phone" },
            { "Mes {0} de {1}", "Month {0} of {1}" },
            { "Tareas {0}/{1}", "Tasks {0}/{1}" },
            { "El game master está pensando...", "The game master is thinking..." },

            // ---- prompts: what the player can do where they stand
            { "Cobrar el salario y cerrar el mes", "Collect your salary and close the month" },
            { "Revisar tu estado de cuenta", "Check your account statement" },
            { "Consultar tu saldo en el cajero", "Check your balance at the ATM" },
            { "Ya hiciste tus tareas del mes. Ve al banco a cobrar.", "You finished this month's tasks. Go to the bank to get paid." },
            { "Ya pasaste por {0} este mes.", "You already stopped by {0} this month." },

            // ---- places (the names of the stations)
            { "Taller de Don Rafa", "Don Rafa's garage" },
            { "Pulperia", "Corner store (pulpería)" },
            { "Prestamos Rapidos", "Quick Loans" },
            { "Inversiones Seguras", "Safe Investments" },
            { "Cooperativa de ahorro", "Savings co-op" },
            { "Soda El Muelle", "El Muelle diner (soda)" },
            { "Hablar con el vendedor", "Talk to the seller" },
            { "Hablar con el cajero", "Talk to the teller" },
            { "Hablar con Don Chepe y ver los carros", "Talk to Don Chepe and look at the cars" },
            { "Ver los carros de la agencia", "See the agency's cars" },
            { "Ver las casas de la inmobiliaria", "See the houses at the real estate agency" },
            { "Ver la casa en venta", "See the house for sale" },
            { "Ver la casa en alquiler", "See the house for rent" },
            { "Hablar de invertir en la finca", "Talk about investing in the farm" },
            { "Hablar de un seguro de salud", "Talk about health insurance" },
            { "Tomar un café", "Grab a coffee" },
            { "Mirar el taller", "Look at the workshop" },
            { "Leer el contrato", "Read the contract" },
            { "Hablar con el oficial", "Talk to the officer" },
            { "Hablar con el juez", "Talk to the judge" },
            { "Hablar con el doctor", "Talk to the doctor" },
            { "Mirar las camas", "Look at the beds" },
            { "Ver el carro", "Look at the car" },
            { "Cajero automático", "ATM" },
            { "Caja 2", "Window 2" },
            { "Casa", "Home" },
            { "Abrir el ropero", "Open the wardrobe" },
            { "Abrir la refri", "Open the fridge" },
            { "Acostarte un rato", "Lie down for a bit" },
            { "Bañarte", "Take a shower" },
            { "Cocinar", "Cook" },
            { "Lavarte las manos", "Wash your hands" },
            { "Preguntar en información", "Ask at the information desk" },
            { "Sacar un tiquete", "Take a ticket" },
            { "Saludar", "Say hi" },
            { "Saludar al guarda", "Say hi to the guard" },
            { "Sentarte a la mesa", "Sit at the table" },
            { "Sentarte en el sillón", "Sit on the couch" },
            { "Sentarte en la banca", "Sit on the bench" },
            { "Ver el tipo de cambio", "Check the exchange rate" },
            { "Ver la fuente", "Look at the fountain" },
            { "Ver la lavadora", "Look at the washer" },
            { "Ver la moto", "Look at the motorbike" },
            { "Ver la refri nueva", "Look at the new fridge" },
            { "Ver los accesorios", "Look at the accessories" },
            { "Ver los televisores", "Look at the TVs" },
            { "Ver tele", "Watch TV" },

            // ---- the result window
            { "Lo que pasó", "What happened" },
            { "Continuar", "Continue" },
            { "Fondo de emergencia", "Emergency fund" },
            { "Deuda con prestamista", "Loan-shark debt" },
            { "Tus cuentas quedaron igual.", "Your accounts stayed the same." },
            { "Hiciste todas tus tareas. Ve al banco a cobrar el salario.", "You finished all your tasks. Go to the bank to get your salary." },
            { "Tareas del mes: {0} de {1}.", "Tasks this month: {0} of {1}." },

            // ---- things that happen to the player
            { "Te arrestaron por entrar a una casa ajena", "You were arrested for going into someone else's house" },
            { "Te atropelló un carro (con seguro de salud)", "A car ran you over (with health insurance)" },
            { "Te atropelló un carro (sin seguro de salud)", "A car ran you over (no health insurance)" },
            { "Pagar a los cobradores", "Pay the collectors" },
            { "Negociar con los cobradores", "Negotiate with the collectors" },
            { "Ignorar a los cobradores", "Ignore the collectors" },
            { "{0}: ¡Salga de mi casa o llamo a la policía!", "{0}: Get out of my house or I'm calling the police!" },

            // ---- health insurance desk
            { "Seguro de salud", "Health insurance" },
            { "Recepción del hospital", "Hospital reception" },
            { "Ya tienes seguro de salud. Pagas {0} al mes y, si te internan, tu parte de la cuenta es de {1} %.", "You already have health insurance. You pay {0} a month and, if you are admitted, your share of the bill is {1} %." },
            { "Un accidente no avisa. Una noche en el hospital, sin seguro, se paga completa.", "An accident doesn't give you a heads-up. A night in the hospital, with no insurance, is paid in full." },
            { "Sin seguro, una internada como la de un atropello cuesta {0}.", "With no insurance, a hospital stay like the one after being run over costs {0}." },
            { "Con seguro, tú pones {0} y pagas {1} cada mes, tengas o no tengas un accidente.", "With insurance, you put in {0} and you pay {1} every month, accident or no accident." },
            { "El fondo de emergencia también sirve para esto: es la plata que ya está lista cuando llega el golpe.", "An emergency fund works for this too: it's the cash that is already set aside when the hit comes." },
            { "Cifras de ejemplo del juego. Es un seguro de salud genérico; no es el de ninguna institución.", "Example figures from the game. It is a generic health insurance, not any institution's." },
            { "1.  Contratar el seguro  ·  {0} al mes", "1.  Get the insurance  ·  {0} a month" },
            { "1.  Salir", "1.  Leave" },
            { "2.  Por ahora no", "2.  Not for now" },

            // ---- the collectors
            { "Los cobradores", "The collectors" },
            { "Cobro por atraso", "Collection for late payment" },
            { "Un carro negro, con los vidrios polarizados y la música a todo volumen, frena frente a ti. Se bajan dos hombres.", "A black car with tinted windows and the music blasting stops right in front of you. Two men get out." },
            { "«Mae, usted debe plata. Hoy venimos por {0}.»", "\"Dude, you owe money. Today we're here for {0}.\"" },
            { "Pagar baja la deuda. Negociar paga la mitad hoy y deja el resto como un préstamo nuevo con recargo. Ignorarlos cuesta una multa por atraso y mucha tensión.", "Paying lowers the debt. Negotiating pays half today and leaves the rest as a new loan with a surcharge. Ignoring them costs a late fee and a lot of stress." },
            { "Cifras de ejemplo del juego.", "Example figures from the game." },
            { "1.  Pagar  ·  {0}", "1.  Pay  ·  {0}" },
            { "2.  Negociar  ·  pagar {0} hoy", "2.  Negotiate  ·  pay {0} today" },
            { "3.  Ignorarlos", "3.  Ignore them" }
        };
    }
}
