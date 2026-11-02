"""Generate the cards of the README and the docs: each page drawn as one tall pixel-art SVG per language.

Usage:
    python scripts/build_readme_art.py

Writes readme-*.svg, costa-rica-*.svg and run-*.svg in docs/assets. They share one palette (paper
and ink, Costa Rican blue and red, colón ochre), hard offset shadows and stepped, sprite-like
animation. Each card carries a title and description for screen readers and stops moving under
prefers-reduced-motion; the README repeats the full content as text below the card.
Standard library only.
"""

# ruff: noqa: E501 - SVG markup reads better on one line per element

import textwrap
from collections.abc import Sequence
from html import escape
from pathlib import Path
from typing import Any

OUT = Path(__file__).resolve().parents[1] / "docs" / "assets"

INK = "#1d1a16"
MUTED = "#4f4536"
PAPER = "#f3ead7"
CARD = "#fbf6ea"
OCHRE = "#e2a92b"
OCHRE_DARK = "#8a5a12"
OCHRE_LIGHT = "#f6d47a"
BLUE = "#1f4e8c"
RED = "#b3261e"

MONO = 'ui-monospace, "Cascadia Mono", Consolas, Menlo, "DejaVu Sans Mono", monospace'
SERIF = 'Georgia, "Iowan Old Style", "Times New Roman", serif'
WIDTH = 800
LEFT, RIGHT = 48, 744  # content column

# Sizes are in viewBox units; on a phone the card shrinks to about 45 %, so body text stays >= 22.
STYLE = f"""
    .title {{ font: 800 56px {MONO}; fill: {INK}; letter-spacing: 2px; }}
    .tagline {{ font: italic 500 26px {SERIF}; fill: {MUTED}; }}
    .kicker {{ font: 700 16px {MONO}; fill: {INK}; letter-spacing: 2px; }}
    .event {{ font: 700 16px {MONO}; fill: {CARD}; letter-spacing: 1px; }}
    .h {{ font: 800 32px {MONO}; fill: {INK}; letter-spacing: 1px; }}
    .num {{ font: 700 20px {MONO}; fill: {RED}; }}
    .label {{ font: 800 24px {MONO}; fill: {INK}; }}
    .label-sm {{ font: 800 22px {MONO}; fill: {INK}; }}
    .body {{ font: 500 24px {SERIF}; fill: {INK}; }}
    .detail {{ font: 500 22px {SERIF}; fill: {MUTED}; }}
    .detail-sm {{ font: 500 19px {SERIF}; fill: {MUTED}; }}
    .step {{ font: 700 15px {MONO}; fill: {RED}; }}
    .pct {{ font: 800 26px {MONO}; fill: {RED}; }}
    .stat {{ font: 800 38px {MONO}; fill: {RED}; }}
    .stamp {{ font: 800 18px {MONO}; fill: {RED}; letter-spacing: 2px; }}
    .chip {{ font: 800 15px {MONO}; letter-spacing: 1px; }}
    .code {{ font: 600 20px {MONO}; fill: {CARD}; }}
    .prompt {{ font: 800 20px {MONO}; fill: {OCHRE}; }}
    .rule {{ font: 500 21px {SERIF}; fill: {INK}; }}
    .big {{ font: 800 30px {MONO}; fill: {INK}; letter-spacing: 2px; }}
    .strong {{ font-weight: 700; }}
    .blink {{ animation: blink 1s steps(1) infinite; }}
    .bob {{ animation: bob 1.2s steps(1) infinite; }}
    .node {{ animation: active 8s steps(1) infinite; }}
    .n2 {{ animation-delay: 2s; }}
    .n3 {{ animation-delay: 4s; }}
    .n4 {{ animation-delay: 6s; }}
    .runner {{ animation: run 8s steps(48) -1s infinite; }}
    .seg {{ animation: flash 0.25s steps(1) 1; }}
    .p1 {{ animation: p1 3s steps(30) infinite; }}
    .p2 {{ animation: p2 4s steps(40) infinite; }}
    .p3 {{ animation: p3 4s steps(40) 1.3s infinite; }}
    .p4 {{ animation: p4 4s steps(40) 2.6s infinite; }}
    @keyframes blink {{ 50% {{ opacity: 0; }} }}
    @keyframes bob {{ 50% {{ transform: translateY(-6px); }} }}
    @keyframes active {{ 0% {{ fill: {OCHRE}; }} 25% {{ fill: {CARD}; }} }}
    @keyframes run {{ 0% {{ transform: translateX(0); }} 100% {{ transform: translateX(720px); }} }}
    @keyframes flash {{ 0% {{ fill: {OCHRE_LIGHT}; }} 100% {{ fill: {OCHRE}; }} }}
    @keyframes p1 {{ 0% {{ transform: translate(0, 0); }} 50% {{ transform: translate(56px, 0); }} 100% {{ transform: translate(0, 0); }} }}
    @keyframes p2 {{ 0% {{ transform: translate(0, 0); }} 30% {{ transform: translate(0, 52px); }} 100% {{ transform: translate(-456px, 52px); }} }}
    @keyframes p3 {{ 0% {{ transform: translate(0, 0); }} 60% {{ transform: translate(0, 52px); }} 100% {{ transform: translate(-200px, 52px); }} }}
    @keyframes p4 {{ 0% {{ transform: translate(0, 0); }} 40% {{ transform: translate(0, 52px); }} 100% {{ transform: translate(56px, 52px); }} }}"""

REDUCED = """
    @media (prefers-reduced-motion: reduce) {
      * { animation: none !important; }
      .hide-still { opacity: 0; }
    }"""

ICONS: dict[str, list[str]] = {
    "coin": [
        "..oooo..",
        ".oOOOOo.",
        "oOo#o#Oo",
        "oO#####o",
        "oO#o#oOo",
        "oO#####o",
        ".oOo#oO.",
        "..oooo..",
    ],
    "card": [
        "........",
        "########",
        "########",
        "#wwwwww#",
        "#woowww#",
        "#wwwwww#",
        "########",
        "........",
    ],
    "clock": [
        "..####..",
        ".#wwww#.",
        "#www#ww#",
        "#www#ww#",
        "#www##w#",
        "#wwwwww#",
        ".#wwww#.",
        "..####..",
    ],
    "bag": [
        "...##...",
        "..#..#..",
        "...##...",
        "..####..",
        ".#oooo#.",
        "#oo#ooo#",
        "#oooooo#",
        ".######.",
    ],
    "pyramid": [
        "...rr...",
        "...rr...",
        "..rrrr..",
        "..rrrr..",
        ".rrrrrr.",
        ".rrrrrr.",
        "rrrrrrrr",
        "########",
    ],
    "spark": [
        "...o....",
        "...o....",
        "..ooo...",
        "ooooooo.",
        "..ooo...",
        "...o....",
        "...o....",
        "........",
    ],
    "bolt": [
        "....##..",
        "...#o#..",
        "..#o#...",
        ".######.",
        "...#o#..",
        "..#o#...",
        ".#o#....",
        ".##.....",
    ],
    "mic": [
        "..###...",
        "..#o#...",
        "..#o#...",
        "#.#o#.#.",
        "#.###.#.",
        ".#...#..",
        "..###...",
        "...#....",
    ],
    "picture": [
        "########",
        "#wwwwww#",
        "#woowww#",
        "#woowbw#",
        "#wwwbbb#",
        "#wwbbbb#",
        "########",
        "........",
    ],
    "chart": [
        "......r.",
        "......r.",
        "....o.r.",
        "....o.r.",
        "..b.o.r.",
        "..b.o.r.",
        "#.b.o.r.",
        "########",
    ],
    "person": [
        "...##...",
        "...##...",
        "........",
        ".######.",
        "...##...",
        "...##...",
        "..#..#..",
        ".#....#.",
    ],
    "stack": [
        "########",
        "#wwwwww#",
        "########",
        "........",
        "########",
        "#wbbbbw#",
        "########",
        "........",
    ],
    "terminal": [
        "########",
        "#wwwwww#",
        "#w#wwww#",
        "#ww#www#",
        "#w#w##w#",
        "#wwwwww#",
        "########",
        "........",
    ],
    "folder": [
        "###.....",
        "#ww#####",
        "#wwwwww#",
        "#wwwwww#",
        "#wwwwww#",
        "#wwwwww#",
        "########",
        "........",
    ],
    "team": [
        ".##..##.",
        ".##..##.",
        "........",
        "###..###",
        ".#....#.",
        ".#....#.",
        "#.#..#.#",
        "........",
    ],
    "warning": [
        "...##...",
        "..#rr#..",
        "..#rr#..",
        ".#rrrr#.",
        ".#r##r#.",
        "#rrrrrr#",
        "#rr##rr#",
        "########",
    ],
}
PIXEL_COLORS = {
    "#": INK,
    "o": OCHRE,
    "O": OCHRE_LIGHT,
    "r": RED,
    "b": BLUE,
    "w": CARD,
}


def icon(name: str, x: float, y: float, px: float) -> str:
    cells = []
    for row, line in enumerate(ICONS[name]):
        for col, char in enumerate(line):
            if char in PIXEL_COLORS:
                cells.append(
                    f'<rect x="{x + col * px:g}" y="{y + row * px:g}" width="{px:g}" '
                    f'height="{px:g}" fill="{PIXEL_COLORS[char]}"/>'
                )
    return "".join(cells)


def big_coin(x0: float, y0: float, px: float) -> str:
    """14x14 pixel colón coin with the ₡ glyph."""
    n, center = 14, 6.5
    glyph = [
        "...#.#..",
        ".######.",
        "##.#.#..",
        "##.#.#..",
        "##.#.#..",
        "##.#.#..",
        ".######.",
        "...#.#..",
    ]
    cells = []
    for r in range(n):
        for k in range(n):
            distance = ((r - center) ** 2 + (k - center) ** 2) ** 0.5
            if distance > 6.9:
                continue
            color = OCHRE
            if distance > 5.9:
                color = OCHRE_DARK
            elif (r - center) + (k - center) < -6:
                color = OCHRE_LIGHT
            gr, gk = r - 3, k - 3
            if 0 <= gr < 8 and 0 <= gk < 8 and glyph[gr][gk] == "#":
                color = OCHRE_DARK
            cells.append(
                f'<rect x="{x0 + k * px:g}" y="{y0 + r * px:g}" width="{px:g}" height="{px:g}" '
                f'fill="{color}"/>'
            )
    return "".join(cells)


def flag(x: float, y: float) -> str:
    stripes = "".join(
        f'<rect x="{x + 2}" y="{y + 2 + 4 * j}" width="64" height="4" fill="{color}"/>'
        for j, color in enumerate([BLUE, PAPER, RED, RED, PAPER, BLUE])
    )
    return f'<rect x="{x}" y="{y}" width="68" height="28" fill="none" stroke="{INK}" stroke-width="2"/>{stripes}'


def panel(x: float, y: float, w: float, h: float, fill: str = CARD, cls: str = "") -> str:
    """A card with a hard offset shadow."""
    klass = f' class="{cls}"' if cls else ""
    return (
        f'<rect x="{x + 6:g}" y="{y + 6:g}" width="{w:g}" height="{h:g}" fill="{INK}"/>'
        f'<rect{klass} x="{x:g}" y="{y:g}" width="{w:g}" height="{h:g}" fill="{fill}" '
        f'stroke="{INK}" stroke-width="3"/>'
    )


# Every visible string, per language.
COPY: dict[str, dict[str, Any]] = {
    "en": {
        "lang": "en",
        "kicker": "FROM COSTA RICA TO THE WORLD",
        "tagline": "Every money decision becomes a story.",
        "steps": [
            ("Decide", "pay or save"),
            ("Narrate", "Muse Spark"),
            ("Compute", "exact colones"),
            ("Negotiate", "by voice"),
        ],
        "rule": ("The AI tells the story.", "The engine keeps the books."),
        "desc": "ColonQuest, a personal-finance simulator that plays like a game, from Costa Rica "
        "to the world. The full content of this card is repeated as text below it.",
        "problem": (
            "THE PROBLEM",
            "In Costa Rica 87% of adults carry some debt and spend on average 34% of their income "
            "paying it, and 80.88% of people surveyed never received any financial education. "
            "Nobody teaches you to ask the right question while a salesperson is smiling at you. "
            "That is the skill ColonQuest trains.",
        ),
        "traps": [
            ("card", "Installments", "Just one payment a month, plus hidden insurance."),
            ("clock", "Minimum payment", "You pay a little and the debt keeps growing."),
            ("bag", "Gota a gota", "Fast cash today, interest every week."),
            ("pyramid", "Pyramid", "Bring three friends and get rich. Promise."),
        ],
        "stamp": "TRAP",
        "month": (
            "HOW A MONTH PLAYS",
            "Your salary arrives with CCSS and income tax already deducted. You decide what to "
            "pay, save or borrow. Muse Spark writes one event built for your situation and the "
            "C# engine applies it in whole colones. When a seller shows up, you negotiate out "
            "loud: the real price, rate, insurance and commission only come out if you ask.",
        ),
        "criteria_title": "JUDGING CRITERIA",
        "criteria": [
            ("Impact", 30, "Real Costa Rican figures, telemetry, Class Mode."),
            ("Meta tools", 25, "Five Meta models, one clear job each, plus tool-using agents."),
            ("Demo", 25, "A guided three-month run. Every call has a fallback."),
            ("Technical depth", 20, "Validated contracts, an exact engine, 1 300+ tests."),
        ],
        "models_title": "META MODELS",
        "models": [
            ("bolt", "Muse Spark", "Live game master with tools; Class Mode reports."),
            ("spark", "Muse Wonder", "Plans your year, writes the summary and event bank."),
            ("person", "Muse Spark Open", "The sellers on Baseten: tactics and memory."),
            ("mic", "Voice Transcribe", "You negotiate out loud; typing always works too."),
            ("picture", "Muse Image", "Portraits, event scenes and your ending, no text."),
        ],
        "agents_title": "AGENTS",
        "agents": [
            (
                "Game master",
                "Calls read-only tools such as simulate_purchase and project_debt, so the consequences it narrates use real numbers.",
            ),
            (
                "Season planner",
                "Sets the teaching arc of the year and brings back the concepts a player struggled with.",
            ),
            (
                "Seller",
                "Pushes longer terms to earn more commission, but can only quote figures from its hidden sheet or its own tools.",
            ),
        ],
        "arch_title": "ARCHITECTURE",
        "boxes": [
            ("Unity 2D client", ["WebGL and desktop", "C# finance engine"]),
            ("FastAPI backend", ["Pydantic v2 contracts", "cost guard, agents"]),
            ("Meta Model API", ["Wonder, Spark,", "Voice, Image"]),
            ("Neon Postgres", ["sessions,", "decisions, calls"]),
            ("Open weights", ["sellers on Baseten", "Muse Spark Open"]),
        ],
        "rules": [
            "No amount, rate or balance ever comes from a model.",
            "Money is an integer number of colones.",
            "Every model answer is validated, with a fallback.",
            "Every call goes through a daily cost guard.",
            "API keys live only in the backend.",
        ],
        "access_title": "ACCESSIBILITY",
        "status": {"done": "DONE", "progress": "IN PROGRESS", "planned": "PLANNED"},
        "access": [
            ("done", "Voice is optional", "Every negotiation turn accepts typed text."),
            ("done", "Plain language", "Short texts in everyday Costa Rican Spanish."),
            ("done", "No dead ends", "If a model fails, scripted content takes over."),
            ("done", "Readable page", "A text version below, motion stops on request."),
            ("progress", "Keyboard play", "Movement and every panel without a mouse."),
            ("progress", "Not by color alone", "Every color also has a label or an icon."),
            ("planned", "Adjustable text", "A larger font for panels and dialogue."),
        ],
        "run_title": "RUN IT AND BUILD IT",
        "run_note": "The full guide, with the env files: RUN_AND_BUILD.md",
        "team_title": "TEAM",
        "team_line": "Built together for the Meta Global AI Developer Hackathon 2026.",
        "footer": "FROM COSTA RICA TO THE WORLD",
        "license": "MIT License",
    },
    "es": {
        "lang": "es",
        "kicker": "FROM COSTA RICA TO THE WORLD",
        "tagline": "Cada decisión de plata se vuelve una historia.",
        "steps": [
            ("Decidir", "pagar o ahorrar"),
            ("Narrar", "Muse Spark"),
            ("Calcular", "colones exactos"),
            ("Negociar", "con la voz"),
        ],
        "rule": ("La IA cuenta la historia.", "El motor lleva las cuentas."),
        "desc": "ColonQuest, un simulador de finanzas personales que se juega como un juego, de "
        "Costa Rica para el mundo. Todo el contenido de esta tarjeta se repite como texto debajo "
        "de ella.",
        "problem": (
            "EL PROBLEMA",
            "En Costa Rica el 87% de los adultos tiene alguna deuda y destina en promedio el 34% de "
            "su ingreso a pagarla, y el 80,88% de los encuestados nunca recibió educación "
            "financiera. Nadie te enseña a hacer la pregunta correcta mientras un vendedor te "
            "sonríe. Esa es la habilidad que entrena ColonQuest.",
        ),
        "traps": [
            ("card", "Cuotas", "Solo una cuota al mes, y un seguro escondido."),
            ("clock", "Pago mínimo", "Pagás poquito y la deuda sigue creciendo."),
            ("bag", "Gota a gota", "Plata rápida hoy, intereses cada semana."),
            ("pyramid", "Pirámide", "Traé tres amigos y te hacés rico. Seguro."),
        ],
        "stamp": "TRAMPA",
        "month": (
            "CÓMO SE JUEGA UN MES",
            "Te llega el salario con el rebajo de la CCSS y la renta. Decidís qué pagar, ahorrar "
            "o pedir prestado. Muse Spark escribe un evento hecho para tu situación y el motor "
            "en C# lo aplica en colones enteros. Cuando aparece un vendedor, negociás en voz "
            "alta: el precio, la tasa, el seguro y la comisión reales solo salen si preguntás.",
        ),
        "criteria_title": "CRITERIOS DEL JURADO",
        "criteria": [
            ("Impacto", 30, "Cifras reales de Costa Rica, telemetría, Modo Clase."),
            ("Meta", 25, "Cinco modelos de Meta con un rol claro, más agentes."),
            ("Demo", 25, "Una ruta guiada de tres meses. Cada llamada tiene respaldo."),
            ("Técnica", 20, "Contratos validados, motor exacto, más de 1 300 pruebas."),
        ],
        "models_title": "MODELOS DE META",
        "models": [
            ("bolt", "Muse Spark", "Game master en vivo con herramientas; Modo Clase."),
            ("spark", "Muse Wonder", "Planifica tu año, escribe el resumen y los eventos."),
            ("person", "Muse Spark Open", "Los vendedores en Baseten: tácticas y memoria."),
            ("mic", "Voice Transcribe", "Negociás en voz alta; escribir siempre sirve."),
            ("picture", "Muse Image", "Retratos, escenas y tu final, pre-generados, sin texto."),
        ],
        "agents_title": "AGENTES",
        "agents": [
            (
                "Game master",
                "Usa herramientas de solo lectura como simulate_purchase y project_debt, así las consecuencias que narra usan números reales.",
            ),
            (
                "Planificador",
                "Arma el recorrido del año y repasa los conceptos en los que el jugador falló.",
            ),
            (
                "Vendedor",
                "Empuja plazos más largos para ganar más comisión, pero solo puede decir cifras de su ficha oculta o de sus herramientas.",
            ),
        ],
        "arch_title": "ARQUITECTURA",
        "boxes": [
            ("Cliente Unity 2D", ["WebGL y escritorio", "motor financiero C#"]),
            ("Backend FastAPI", ["contratos Pydantic v2", "costos, agentes"]),
            ("API de Meta", ["Wonder, Spark,", "Voice, Image"]),
            ("Neon Postgres", ["sesiones,", "decisiones"]),
            ("Modelos abiertos", ["vendedores en Baseten", "Muse Spark Open"]),
        ],
        "rules": [
            "Ningún monto, tasa ni saldo sale de un modelo.",
            "La plata es un número entero de colones.",
            "Toda respuesta de un modelo se valida y tiene respaldo.",
            "Toda llamada pasa por un control de costos diario.",
            "Las llaves de API viven solo en el backend.",
        ],
        "access_title": "ACCESIBILIDAD",
        "status": {"done": "LISTO", "progress": "EN PROGRESO", "planned": "PENDIENTE"},
        "access": [
            ("done", "Voz opcional", "Cada turno de negociación acepta texto."),
            ("done", "Lenguaje claro", "Narraciones cortas en español tico cotidiano."),
            ("done", "Sin bloqueos", "Si un modelo falla, entra contenido escrito."),
            ("done", "Página legible", "Versión en texto y animación que se puede parar."),
            ("progress", "Teclado", "Movimiento y paneles sin usar el mouse."),
            ("progress", "No solo color", "Cada color lleva también etiqueta o ícono."),
            ("planned", "Texto ajustable", "Letra más grande en paneles y diálogos."),
        ],
        "run_title": "CORRERLO Y ARMARLO",
        "run_note": "La guía completa, con los archivos env: RUN_AND_BUILD.es.md",
        "team_title": "EQUIPO",
        "team_line": "Hecho en equipo para el Meta Global AI Developer Hackathon 2026.",
        "footer": "FROM COSTA RICA TO THE WORLD",
        "license": "Licencia MIT",
    },
}

# The other pages of the docs, per language: what each one says, block by block.
COSTA_RICA_ICONS = ["coin", "person", "warning", "chart", "spark"]
RUN_ICONS = ["bolt", "folder", "terminal", "stack"]
PAGES: dict[str, dict[str, dict[str, Any]]] = {
    "costa-rica": {
        "en": {
            "lang": "en",
            "kicker": "FROM COSTA RICA TO THE WORLD",
            "title": "COSTA RICA",
            "tagline": "Why this simulator is set here.",
            "desc": "The situation of debt and financial education in Costa Rica, with cited figures. The full content of this card is repeated as text below it.",
            "icons": COSTA_RICA_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Every figure, with its source, in the text below",
            "blocks": [
                ("header", "DEBT IS ORDINARY"),
                ("stats", [
                    ("87%", "of adults have some kind of debt (OCF, 2025). It was 91% in 2023."),
                    ("34%", "of monthly income goes to debts, on average. It was 38% in 2023."),
                    ("58.3%", "for people who earn under ₡500 000 a month."),
                    ("14.7%", "at ages 18 to 24: the moment to learn is before it grows."),
                ]),
                ("header", "NOBODY TEACHES IT"),
                ("stats", [
                    ("80.88%", "of people surveyed never received financial education (CINPE-IDESPO, UNA)."),
                    ("73.89%", "say at least one course would be useful to them."),
                ]),
                ("header", "THE TRAPS ARE REAL"),
                ("stats", [
                    ("2 930", "gota a gota cases in 2025, and 2 050 by August 2026, says the OIJ."),
                    ("20-100%", "interest found by the OIJ in those loans."),
                    ("14%", "got loan offers by WhatsApp, Facebook or TikTok in a year (OCF)."),
                    ("36.48%", "a year: the legal cap on cards in colones, late 2026 (BCCR)."),
                ]),
                ("header", "THE ENGINE USES REAL RULES"),
                ("stats", [
                    ("₡373 092", "the 2026 minimum wage for an unskilled worker: the player's salary."),
                    ("10.83%", "worker's social charges in 2026, taken off the salary."),
                    ("3%", "a month on the card: 36% a year, under the cap."),
                    ("20%", "a month from the lender: the low end of what the OIJ found."),
                ]),
            ],
        },
        "es": {
            "lang": "es",
            "kicker": "FROM COSTA RICA TO THE WORLD",
            "title": "COSTA RICA",
            "tagline": "Por qué este simulador pasa aquí.",
            "desc": "La situación de las deudas y la educación financiera en Costa Rica, con cifras citadas. Todo el contenido de esta tarjeta se repite como texto debajo de ella.",
            "icons": COSTA_RICA_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Cada cifra, con su fuente, en el texto de abajo",
            "blocks": [
                ("header", "LA DEUDA ES LO NORMAL"),
                ("stats", [
                    ("87%", "de los adultos tiene alguna deuda (OCF, 2025). En 2023 era el 91%."),
                    ("34%", "del ingreso mensual va a deudas, en promedio. En 2023 era el 38%."),
                    ("58,3%", "para quienes ganan menos de ₡500 000 al mes."),
                    ("14,7%", "de 18 a 24 años: el momento de aprender es antes de que crezca."),
                ]),
                ("header", "NADIE LO ENSEÑA"),
                ("stats", [
                    ("80,88%", "de los encuestados nunca recibió educación financiera (CINPE-IDESPO, UNA)."),
                    ("73,89%", "dice que al menos un curso le sería útil."),
                ]),
                ("header", "LAS TRAMPAS SON REALES"),
                ("stats", [
                    ("2 930", "casos de gota a gota en 2025, y 2 050 a agosto de 2026, según el OIJ."),
                    ("20-100%", "de interés encontró el OIJ en esos préstamos."),
                    ("14%", "recibió ofertas de préstamo por WhatsApp, Facebook o TikTok en un año (OCF)."),
                    ("36,48%", "anual: el tope legal de las tarjetas en colones, fin de 2026 (BCCR)."),
                ]),
                ("header", "EL MOTOR USA REGLAS REALES"),
                ("stats", [
                    ("₡373 092", "el salario mínimo 2026 de un trabajador no calificado: el del jugador."),
                    ("10,83%", "de cargas sociales del trabajador en 2026, rebajadas del salario."),
                    ("3%", "al mes en la tarjeta: 36% anual, bajo el tope."),
                    ("20%", "al mes con el prestamista: lo más bajo de lo que halló el OIJ."),
                ]),
            ],
        },
    },
    "run": {
        "en": {
            "lang": "en",
            "kicker": "FOR THE JUDGES",
            "title": "RUN AND BUILD",
            "tagline": "Change the env files, build from there.",
            "desc": "How to run ColonQuest, point it to another backend and build the executable from the env files. The full content of this card is repeated as text below it.",
            "icons": RUN_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "The keys never go inside the executable",
            "blocks": [
                ("header", "JUST PLAY IT"),
                ("paragraph", "Unzip the Windows build and open ColonQuest.exe. It uses our hosted backend, so it only needs internet. Choose Guided simulation."),
                ("header", "TWO ENV FILES"),
                ("tiles", [
                    ("stack", "server/.env", "API keys, models, budgets, database. Backend only."),
                    ("folder", "colonquest.env", "Which backend the game talks to. No secrets."),
                ]),
                ("header", "YOUR OWN BACKEND"),
                ("terminal", ["cd server", "cp .env.example .env", "uv sync", "uv run alembic upgrade head", "uv run python -m app.dev"]),
                ("header", "BUILD THE EXECUTABLE"),
                ("terminal", ["cp colonquest.env.example colonquest.env", "python scripts/build_game.py"]),
            ],
        },
        "es": {
            "lang": "es",
            "kicker": "PARA LOS JUECES",
            "title": "CORRER Y ARMAR",
            "tagline": "Cambiá los env y armá desde ahí.",
            "desc": "Cómo correr ColonQuest, apuntarlo a otro backend y armar el ejecutable a partir de los archivos env. Todo el contenido de esta tarjeta se repite como texto debajo de ella.",
            "icons": RUN_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Las llaves nunca van dentro del ejecutable",
            "blocks": [
                ("header", "SOLO JUGARLO"),
                ("paragraph", "Descomprimí el build de Windows y abrí ColonQuest.exe. Usa nuestro backend en línea, así que solo ocupa internet. Elegí Simulación guiada."),
                ("header", "DOS ARCHIVOS ENV"),
                ("tiles", [
                    ("stack", "server/.env", "Llaves, modelos, presupuestos y base. Solo el backend."),
                    ("folder", "colonquest.env", "A qué backend habla el juego. Sin secretos."),
                ]),
                ("header", "TU PROPIO BACKEND"),
                ("terminal", ["cd server", "cp .env.example .env", "uv sync", "uv run alembic upgrade head", "uv run python -m app.dev"]),
                ("header", "ARMAR EL EJECUTABLE"),
                ("terminal", ["cp colonquest.env.example colonquest.env", "python scripts/build_game.py"]),
            ],
        },
    },
}


COMMANDS = [
    "cd server",
    "cp .env.example .env",
    "uv sync",
    "uv run alembic upgrade head",
    "uv run python -m app.dev",
    "cd ..",
    "cp colonquest.env.example colonquest.env",
    "python scripts/build_game.py",
]
MEMBERS = ["Keylor Pineda", "Jefferson Abarca"]
SECTION_ICONS = ["warning", "coin", "chart", "spark", "bolt", "stack", "person", "terminal", "team"]


def wrap(text: str, width: int) -> list[str]:
    return textwrap.wrap(text, width)


def lines(x: float, y: float, rows: Sequence[str], cls: str, step: float) -> str:
    return "".join(
        f'<text class="{cls}" x="{x:g}" y="{y + i * step:g}">{escape(row)}</text>'
        for i, row in enumerate(rows)
    )


class Card:
    """Stacks sections vertically; each section draws in local coordinates from y = 0."""

    def __init__(self, icons: Sequence[str] = SECTION_ICONS) -> None:
        self.parts: list[str] = []
        self.y = 0
        self.number = 0
        self.icons = icons

    def add(self, body: str, height: int) -> None:
        self.parts.append(f'<g transform="translate(0 {self.y})">{body}</g>')
        self.y += height

    def header(self, title: str) -> None:
        icon_name = self.icons[self.number % len(self.icons)]
        self.number += 1
        cursor_x = 152 + len(title) * 19.2 + 10
        self.add(
            f'<line x1="{LEFT}" y1="8" x2="{RIGHT}" y2="8" stroke="{INK}" stroke-width="2" stroke-dasharray="6 6"/>'
            f"{panel(LEFT, 34, 48, 48)}{icon(icon_name, LEFT + 8, 42, 4)}"
            f'<text class="num" x="112" y="68">{self.number:02d}</text>'
            f'<text class="h" x="152" y="70">{escape(title)}</text>'
            f'<rect class="blink" x="{cursor_x:g}" y="44" width="14" height="30" fill="{OCHRE}"/>',
            110,
        )

    def paragraph(self, text: str) -> None:
        rows = wrap(text, 52)
        self.add(lines(LEFT, 24, rows, "body", 36), 24 + 36 * len(rows) + 8)


def hero(card: Card, copy: dict[str, Any]) -> None:
    nodes = ""
    for i, (label, detail) in enumerate(copy["steps"]):
        x = 50 + 180 * i
        cx = x + 75
        nodes += (
            f"{panel(x, 262, 150, 112, cls=f'node n{i + 1}')}"
            f'<text class="step" x="{cx}" y="290" text-anchor="middle">0{i + 1}</text>'
            f'<text class="label-sm" x="{cx}" y="324" text-anchor="middle">{escape(label)}</text>'
            f'<text class="detail-sm" x="{cx}" y="354" text-anchor="middle">{escape(detail)}</text>'
        )
    story, books = copy["rule"]
    card.add(
        f"{flag(46, 40)}"
        f'<text class="kicker" x="128" y="60">{escape(copy["kicker"])}</text>'
        f'<text class="title" x="46" y="136">COLONQUEST</text>'
        f'<text class="tagline" x="48" y="178">{escape(copy["tagline"])}</text>'
        f'<rect x="46" y="196" width="430" height="30" fill="{INK}"/>'
        f'<text class="event" x="58" y="217">META GLOBAL AI DEVELOPER HACKATHON 2026</text>'
        f'<g class="bob">{big_coin(630, 76, 7)}</g>'
        f'<rect x="640" y="190" width="78" height="7" fill="{INK}" opacity="0.18"/>'
        f'<line x1="125" y1="316" x2="665" y2="316" stroke="{INK}" stroke-width="3" stroke-dasharray="6 6"/>'
        f"{nodes}"
        f'<clipPath id="track"><rect x="50" y="382" width="696" height="24"/></clipPath>'
        f'<g clip-path="url(#track)"><g class="runner hide-still">{big_coin(-64, 387, 1.5)}</g></g>'
        f'<text class="rule" x="396" y="448" text-anchor="middle">{escape(story)} '
        f'<tspan class="strong">{escape(books)}</tspan></text>',
        472,
    )


def tile_rows(card: Card, items: Sequence[tuple[str, str, str]], stamp: str | None = None) -> None:
    body = ""
    for i, (icon_name, heading, line) in enumerate(items):
        y = 16 + i * 128
        body += (
            f"{panel(LEFT, y, RIGHT - LEFT - 6, 106)}"
            f'<g class="bob" style="animation-delay: {i * 0.3:g}s">{icon(icon_name, LEFT + 20, y + 29, 6)}</g>'
            f'<text class="label" x="{LEFT + 92}" y="{y + 44}">{escape(heading)}</text>'
            f'<text class="detail" x="{LEFT + 92}" y="{y + 80}">{escape(line)}</text>'
        )
        if stamp:
            sx, sy = RIGHT - 136, y + 40
            start = 10 + 18 * i
            body += (
                f"<style>.stamp{i} {{ animation: stamp{i} 8s steps(1) infinite; }} "
                f"@keyframes stamp{i} {{ 0% {{ opacity: 0; }} {start}% {{ opacity: 1; }} 92% {{ opacity: 0; }} }}</style>"
                f'<g class="stamp{i}" transform="rotate(-8 {sx + 52} {sy})">'
                f'<rect x="{sx}" y="{sy - 20}" width="104" height="30" fill="{CARD}" stroke="{RED}" stroke-width="3"/>'
                f'<text class="stamp" x="{sx + 52}" y="{sy + 1}" text-anchor="middle">{escape(stamp)}</text></g>'
            )
    card.add(body, 16 + 128 * len(items) + 8)


def criteria(card: Card, copy: dict[str, Any]) -> None:
    body = ""
    for i, (name, weight, line) in enumerate(copy["criteria"]):
        y = 16 + i * 100
        body += f'<text class="label" x="{LEFT}" y="{y + 30}">{escape(name)}</text>'
        for s in range(10):
            filled = s < weight // 5
            x = 404 + s * 26
            if filled:
                delay = 0.3 + (i * 6 + s) * 0.08
                body += f'<rect class="seg" style="animation-delay: {delay:.2f}s" x="{x}" y="{y + 6}" width="20" height="30" fill="{OCHRE}" stroke="{INK}" stroke-width="2"/>'
            else:
                body += f'<rect x="{x}" y="{y + 6}" width="20" height="30" fill="{CARD}" stroke="{INK}" stroke-width="2"/>'
        body += f'<text class="pct" x="{RIGHT}" y="{y + 32}" text-anchor="end">{weight}%</text>'
        body += f'<text class="detail" x="{LEFT}" y="{y + 70}">{escape(line)}</text>'
    card.add(body, 16 + 100 * len(copy["criteria"]))


def agents(card: Card, copy: dict[str, Any]) -> None:
    body, y = "", 20
    for name, text in copy["agents"]:
        rows = wrap(text, 46)
        body += f'<rect x="{LEFT}" y="{y - 18}" width="14" height="14" fill="{OCHRE}" stroke="{INK}" stroke-width="2"/>'
        body += f'<text class="label" x="{LEFT + 30}" y="{y}">{escape(name)}</text>'
        body += lines(LEFT + 30, y + 36, rows, "detail", 32)
        y += 36 + 32 * len(rows) + 24
    card.add(body, y)


def architecture(card: Card, copy: dict[str, Any]) -> None:
    def box(
        x: int, y: int, w: int, h: int, content: tuple[str, Sequence[str]], dashed: bool = False
    ) -> str:
        title, rows = content
        if dashed:
            frame = f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{PAPER}" stroke="{INK}" stroke-width="3" stroke-dasharray="8 6"/>'
        else:
            frame = panel(x, y, w, h)
        text = f'<text class="label-sm" x="{x + w / 2:g}" y="{y + 36}" text-anchor="middle">{escape(title)}</text>'
        for j, row in enumerate(rows):
            text += f'<text class="detail-sm" x="{x + w / 2:g}" y="{y + 68 + j * 26}" text-anchor="middle">{escape(row)}</text>'
        return frame + text

    client, backend, meta, neon, open_weights = copy["boxes"]
    body = (
        f"{box(48, 24, 306, 128, client)}{box(440, 24, 298, 128, backend)}"
        f'<line x1="360" y1="88" x2="434" y2="88" stroke="{INK}" stroke-width="4"/>'
        f'<text class="step" x="397" y="78" text-anchor="middle">JSON</text>'
        f'<line x1="589" y1="158" x2="589" y2="212" stroke="{INK}" stroke-width="4"/>'
        f'<line x1="133" y1="212" x2="655" y2="212" stroke="{INK}" stroke-width="4"/>'
        f'<line x1="133" y1="212" x2="133" y2="250" stroke="{INK}" stroke-width="4"/>'
        f'<line x1="389" y1="212" x2="389" y2="250" stroke="{INK}" stroke-width="4"/>'
        f'<line x1="655" y1="212" x2="655" y2="250" stroke="{INK}" stroke-width="4" stroke-dasharray="6 5"/>'
        f"{box(48, 250, 222, 132, meta)}{box(282, 250, 214, 132, neon)}"
        f"{box(510, 250, 234, 132, open_weights, dashed=True)}"
        f'<rect class="p1 hide-still" x="364" y="83" width="10" height="10" fill="{OCHRE}" stroke="{INK}" stroke-width="2"/>'
        f'<rect class="p2 hide-still" x="584" y="160" width="10" height="10" fill="{OCHRE}" stroke="{INK}" stroke-width="2"/>'
        f'<rect class="p3 hide-still" x="584" y="160" width="10" height="10" fill="{BLUE}" stroke="{INK}" stroke-width="2"/>'
        f'<rect class="p4 hide-still" x="584" y="160" width="10" height="10" fill="{RED}" stroke="{INK}" stroke-width="2"/>'
    )
    y = 430
    for rule in copy["rules"]:
        body += f'<rect x="{LEFT}" y="{y - 16}" width="12" height="12" fill="{RED}"/>'
        body += f'<text class="detail" x="{LEFT + 26}" y="{y}">{escape(rule)}</text>'
        y += 38
    card.add(body, y)


def accessibility(card: Card, copy: dict[str, Any]) -> None:
    fills = {"done": (INK, CARD), "progress": (OCHRE, INK), "planned": (CARD, INK)}
    body = ""
    for i, (status, label, text) in enumerate(copy["access"]):
        y = 16 + i * 84
        background, foreground = fills[status]
        body += (
            f'<rect x="{LEFT}" y="{y + 4}" width="164" height="32" fill="{background}" stroke="{INK}" stroke-width="2"/>'
            f'<text class="chip" x="{LEFT + 82}" y="{y + 26}" text-anchor="middle" fill="{foreground}">{escape(copy["status"][status])}</text>'
            f'<text class="label-sm" x="{LEFT + 188}" y="{y + 28}">{escape(label)}</text>'
            f'<text class="detail" x="{LEFT + 188}" y="{y + 60}">{escape(text)}</text>'
        )
    card.add(body, 16 + 84 * len(copy["access"]))


def terminal(commands: Sequence[str]) -> tuple[str, int]:
    height = 40 + 36 * len(commands)
    body = f'<rect x="{LEFT + 6}" y="22" width="{RIGHT - LEFT - 6}" height="{height}" fill="{OCHRE_DARK}"/>'
    body += f'<rect x="{LEFT}" y="16" width="{RIGHT - LEFT - 6}" height="{height}" fill="{INK}"/>'
    for i, command in enumerate(commands):
        y = 56 + 36 * i
        body += f'<text class="prompt" x="{LEFT + 24}" y="{y}">$</text>'
        body += f'<text class="code" x="{LEFT + 52}" y="{y}">{escape(command)}</text>'
    body += f'<rect class="blink" x="{LEFT + 52}" y="{40 + 36 * len(commands)}" width="12" height="22" fill="{OCHRE}"/>'
    return body, height


def run_locally(card: Card, copy: dict[str, Any]) -> None:
    body, height = terminal(COMMANDS)
    # The note ends with a command; keep the command whole on its own line.
    note, windows_command = copy["run_note"].split(": ", 1)
    note_y = height + 62
    body += f'<text class="detail" x="{LEFT}" y="{note_y}">{escape(note)}:</text>'
    body += f'<text class="label-sm" x="{LEFT}" y="{note_y + 36}">{escape(windows_command)}</text>'
    card.add(body, note_y + 60)


def team(card: Card, copy: dict[str, Any]) -> None:
    body = '<text class="big" x="396" y="48" text-anchor="middle">DOWHILE TEAM</text>'
    width = 330
    for i, name in enumerate(MEMBERS):
        x = LEFT + i * (width + 30)
        body += (
            f"{panel(x, 80, width, 96)}"
            f'<g class="bob" style="animation-delay: {i * 0.6:g}s">{icon("person", x + 22, 104, 6)}</g>'
            f'<text class="label" x="{x + 92}" y="138">{escape(name)}</text>'
        )
    body += f'<text class="detail" x="396" y="230" text-anchor="middle">{escape(copy["team_line"])}</text>'
    card.add(body, 256)


def footer(card: Card, copy: dict[str, Any]) -> None:
    card.add(
        f'<rect x="{LEFT}" y="12" width="{RIGHT - LEFT}" height="6" fill="{INK}"/>'
        f'<g class="bob">{big_coin(LEFT, 46, 5)}</g>'
        f'<text class="big" x="406" y="96" text-anchor="middle">{escape(copy["footer"])}</text>'
        f"{flag(372, 120)}"
        f'<text class="detail-sm" x="406" y="182" text-anchor="middle">{escape(copy["license"])}</text>',
        212,
    )


def page_hero(card: Card, copy: dict[str, Any]) -> None:
    card.add(
        f"{flag(46, 40)}"
        f'<text class="kicker" x="128" y="60">{escape(copy["kicker"])}</text>'
        f'<text class="title" x="46" y="136">{escape(copy["title"])}</text>'
        f'<text class="tagline" x="48" y="178">{escape(copy["tagline"])}</text>'
        f'<rect x="46" y="196" width="430" height="30" fill="{INK}"/>'
        f'<text class="event" x="58" y="217">META GLOBAL AI DEVELOPER HACKATHON 2026</text>'
        f'<g class="bob">{big_coin(630, 40, 7)}</g>',
        250,
    )


def stats(card: Card, items: Sequence[tuple[str, str]]) -> None:
    body, y = "", 16
    for value, text in items:
        rows = wrap(text, 38)
        height = max(92, 28 + 32 * len(rows))
        body += panel(LEFT, y, RIGHT - LEFT - 6, height)
        body += f'<text class="stat" x="{LEFT + 18}" y="{y + height / 2 + 13:g}">{escape(value)}</text>'
        body += lines(LEFT + 232, y + 36, rows, "detail", 32)
        y += height + 22
    card.add(body, y + 4)


def build_page(name: str, copy: dict[str, Any]) -> None:
    card = Card(copy["icons"])
    card.y = 8
    page_hero(card, copy)
    for kind, content in copy["blocks"]:
        if kind == "header":
            card.header(content)
        elif kind == "paragraph":
            card.paragraph(content)
        elif kind == "stats":
            stats(card, content)
        elif kind == "tiles":
            tile_rows(card, content)
        elif kind == "terminal":
            body, height = terminal(content)
            card.add(body, height + 40)
    footer(card, copy)
    write_card(card, copy, f"{name}-{copy['lang']}.svg", copy["title"])


def write_card(card: Card, copy: dict[str, Any], file_name: str, title: str) -> None:
    height = card.y + 24
    frame = (
        f'<rect x="10" y="10" width="788" height="{height - 12}" fill="{INK}"/>'
        f'<rect x="2" y="2" width="788" height="{height - 12}" fill="{PAPER}" stroke="{INK}" stroke-width="4"/>'
    )
    document = f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {WIDTH} {height}" width="{WIDTH}" height="{height}" role="img" aria-labelledby="t d">
  <title id="t">{escape(title)}</title>
  <desc id="d">{escape(copy["desc"])}</desc>
  <style>{STYLE}{REDUCED}
  </style>
  {frame}
  {"".join(card.parts)}
</svg>
"""
    (OUT / file_name).write_text(document, encoding="utf-8", newline="\n")


def build(copy: dict[str, Any]) -> None:
    card = Card()
    card.y = 8
    hero(card, copy)
    card.header(copy["problem"][0])
    card.paragraph(copy["problem"][1])
    tile_rows(card, copy["traps"], copy["stamp"])
    card.header(copy["month"][0])
    card.paragraph(copy["month"][1])
    card.header(copy["criteria_title"])
    criteria(card, copy)
    card.header(copy["models_title"])
    tile_rows(card, copy["models"])
    card.header(copy["agents_title"])
    agents(card, copy)
    card.header(copy["arch_title"])
    architecture(card, copy)
    card.header(copy["access_title"])
    accessibility(card, copy)
    card.header(copy["run_title"])
    run_locally(card, copy)
    card.header(copy["team_title"])
    team(card, copy)
    footer(card, copy)

    write_card(card, copy, f"readme-{copy['lang']}.svg", "ColonQuest")


# The row of buttons at the top of each page, since GitHub does not let a repository add its own tabs.
TABS = {
    "readme": {"en": "README", "es": "README"},
    "costa-rica": {"en": "COSTA RICA", "es": "COSTA RICA"},
    "run": {"en": "RUN AND BUILD", "es": "CORRER Y ARMAR"},
}


def build_tab(name: str, lang: str, label: str) -> None:
    width, height = 250, 56
    document = f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" width="{width}" height="{height}" role="img" aria-label="{escape(label)}">
  <title>{escape(label)}</title>
  <rect x="6" y="6" width="{width - 6}" height="{height - 6}" fill="{INK}"/>
  <rect x="2" y="2" width="{width - 8}" height="{height - 8}" fill="{OCHRE}" stroke="{INK}" stroke-width="3"/>
  <text x="{(width - 6) / 2:g}" y="33" text-anchor="middle" font-family="{escape(MONO, quote=True)}" font-weight="800" font-size="20" letter-spacing="1" fill="{INK}">{escape(label)}</text>
</svg>
"""
    (OUT / f"tab-{name}-{lang}.svg").write_text(document, encoding="utf-8", newline="\n")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for copy in COPY.values():
        build(copy)
    for name, languages in PAGES.items():
        for copy in languages.values():
            build_page(name, copy)
    for name, labels in TABS.items():
        for lang, label in labels.items():
            build_tab(name, lang, label)


if __name__ == "__main__":
    main()
