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
        "desc": "ColonQuest, a personal-finance simulator that feels like real life, from Costa Rica "
        "to the world. The full content of this card is repeated as text below it.",
        "problem": (
            "THE PROBLEM",
            "In Costa Rica a third of monthly income goes to paying debts, more than half for those "
            "who earn under $1 000, and 8 in 10 people surveyed never had any financial education. "
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
        "why_title": "THE USURY LAW AND ITS GAP",
        "why_stats": [
            ("52%", "of income went to debts in 2020, with no legal cap on interest."),
            ("2020", "Law 9859 caps every rate, checks total debt and protects the minimum wage."),
            ("47 > 428", "extortion complaints over informal loans, from 2020 to 2024."),
            ("20%", "a week or a month from street lenders, far above the cap."),
        ],
        "why_close": "A law can cap a rate. It cannot teach anyone the right question. "
        "That is why Costa Rica needs financial education, and why ColonQuest exists.",
        "month": (
            "HOW A MONTH WORKS",
            "Your salary arrives with CCSS and income tax already deducted. You decide what to "
            "pay, save or borrow. Muse Spark writes one event built for your situation and the "
            "C# engine applies it in whole colones. When a seller shows up, you negotiate out "
            "loud: the real price, rate, insurance and commission only come out if you ask.",
        ),
        "ways_title": "THREE WAYS IN",
        "ways": [
            ("bolt", "Guided simulation", "Three real months in three minutes. Start here."),
            ("spark", "Feature demo", "Every AI piece, one click away, with its calls."),
            ("person", "Story mode", "The whole town, with three save slots."),
        ],
        "inside_title": "WHAT IS INSIDE",
        "inside": [
            ("coin", "Real money rules", "Minimum wage, CCSS, tax, card, installments."),
            ("person", "A town to live in", "About 17 places, a job, courses, day and night."),
            ("card", "A phone", "SINPE, bills, goals, search, a loan calculator."),
            ("warning", "Consequences", "Collectors, power cuts, a cell, the hospital."),
        ],
        "criteria_title": "JUDGING CRITERIA",
        "criteria": [
            ("Impact", 30, "Real Costa Rican figures, telemetry, Class Mode."),
            ("Meta tools", 25, "Five Meta models, one clear job each, plus tool-using agents."),
            ("Demo", 25, "A guided three-month run. Every call has a fallback."),
            ("Technical depth", 20, "Validated contracts, an exact engine, 1 300+ tests."),
        ],
        "models_title": "META MODELS",
        "models": [
            ("bolt", "Muse Spark", "Meta API. Live simulation director with tools."),
            ("spark", "Muse Wonder", "Meta API, background. Plans your year, writes the end."),
            ("person", "Muse Spark Open", "Baseten. Every character you talk to."),
            ("mic", "Voice Transcribe", "Meta API. You negotiate out loud, or you type."),
            ("picture", "Muse Image", "Meta API, pre-generated. Portraits, scenes, endings."),
        ],
        "categories_title": "CATEGORIES WE ENTER",
        "categories": [
            ("Indie track", "Impact, Meta tools, demo and technical depth: the criteria above."),
            ("Best Build on Meta Open Weights", "Every character you talk to runs on Muse Spark Open in Baseten."),
            ("Best Agentic Use-Case", "Three agents with tools: the simulation director, the year planner and the seller."),
            ("Best Creative Build", "An original Costa Rican town. Muse Image draws it, Muse Wonder writes your year."),
            ("Community Vote", "The votes in the hackathon's public gallery."),
        ],
        "agents_title": "AGENTS",
        "agents": [
            (
                "Simulation director",
                "Calls read-only tools such as simulate_purchase and project_debt, so the consequences it narrates use real numbers.",
            ),
            (
                "Season planner",
                "Sets the teaching arc of the year and brings back the concepts a user struggled with.",
            ),
            (
                "Seller",
                "Pushes longer terms to earn more commission, but can only quote figures from its hidden sheet or its own tools.",
            ),
        ],
        "arch_title": "ARCHITECTURE",
        "boxes": [
            ("Unity 2D client", ["Windows desktop", "C# finance engine"]),
            ("FastAPI backend", ["Pydantic v2 contracts", "cost guard, agents"]),
            ("Meta Model API", ["Wonder, Spark,", "Voice, Image"]),
            ("Database", ["SQLite or Postgres:", "sessions, decisions"]),
            ("Open weights", ["sellers on Baseten", "Muse Spark Open"]),
        ],
        "rules": [
            "No amount, rate or balance ever comes from a model.",
            "Money is an integer number of colones.",
            "Every model answer is validated, with a fallback.",
            "Every call goes through a daily cost guard.",
            "API keys live only in the backend.",
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
        "desc": "ColonQuest, un simulador de finanzas personales, de "
        "Costa Rica para el mundo. Todo el contenido de esta tarjeta se repite como texto debajo "
        "de ella.",
        "problem": (
            "EL PROBLEMA",
            "En Costa Rica un tercio del ingreso mensual se va en deudas, más de la mitad para quienes "
            "ganan menos de ₡500 000, y 8 de cada 10 encuestados nunca recibieron educación "
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
        "why_title": "LA LEY DE USURA Y SU VACÍO",
        "why_stats": [
            ("52%", "del ingreso se iba en deudas en 2020, sin tope legal de interés."),
            ("2020", "La Ley 9859 topa toda tasa, revisa la deuda total y protege el salario mínimo."),
            ("47 > 428", "denuncias por extorsión de préstamos informales, de 2020 a 2024."),
            ("20%", "por semana o por mes con el gota a gota, muy encima del tope."),
        ],
        "why_close": "Una ley puede topar una tasa. No puede enseñar la pregunta correcta. "
        "Por eso Costa Rica necesita educación financiera, y por eso existe ColonQuest.",
        "month": (
            "CÓMO FUNCIONA UN MES",
            "Te llega el salario con el rebajo de la CCSS y la renta. Decidís qué pagar, ahorrar "
            "o pedir prestado. Muse Spark escribe un evento hecho para tu situación y el motor "
            "en C# lo aplica en colones enteros. Cuando aparece un vendedor, negociás en voz "
            "alta: el precio, la tasa, el seguro y la comisión reales solo salen si preguntás.",
        ),
        "ways_title": "TRES FORMAS DE ENTRAR",
        "ways": [
            ("bolt", "Simulación guiada", "Tres meses reales en tres minutos. Empezá aquí."),
            ("spark", "Demo de funciones", "Cada pieza de IA a un clic, con sus llamadas."),
            ("person", "Modo historia", "Todo el pueblo, con tres partidas guardadas."),
        ],
        "inside_title": "QUÉ TRAE",
        "inside": [
            ("coin", "Reglas reales de plata", "Salario mínimo, CCSS, renta, tarjeta, cuotas."),
            ("person", "Un pueblo para vivir", "Unos 17 lugares, trabajo, cursos, día y noche."),
            ("card", "Un celular", "SINPE, recibos, metas, buscador, calculadora."),
            ("warning", "Consecuencias", "Cobradores, cortes de luz, celda, hospital."),
        ],
        "criteria_title": "CRITERIOS DEL JURADO",
        "criteria": [
            ("Impacto", 30, "Cifras reales de Costa Rica, telemetría, Modo Clase."),
            ("Meta", 25, "Cinco modelos de Meta con un rol claro, más agentes."),
            ("Demo", 25, "Una ruta guiada de tres meses. Cada llamada tiene respaldo."),
            ("Técnica", 20, "Contratos validados, motor exacto, más de 1 300 pruebas."),
        ],
        "models_title": "MODELOS DE META",
        "models": [
            ("bolt", "Muse Spark", "API de Meta. Director en vivo con herramientas."),
            ("spark", "Muse Wonder", "API de Meta, en segundo plano. Tu año y tu final."),
            ("person", "Muse Spark Open", "Baseten. Cada personaje con el que hablás."),
            ("mic", "Voice Transcribe", "API de Meta. Negociás en voz alta o escribís."),
            ("picture", "Muse Image", "API de Meta, pre-generadas. Retratos y finales."),
        ],
        "categories_title": "CATEGORÍAS",
        "categories": [
            ("Track Indie", "Impacto, herramientas de Meta, demo y técnica: los criterios de arriba."),
            ("Best Build on Meta Open Weights", "Cada personaje con el que hablás corre en Muse Spark Open en Baseten."),
            ("Best Agentic Use-Case", "Tres agentes con herramientas: el director, el planificador y el vendedor."),
            ("Best Creative Build", "Un pueblo tico original. Muse Image lo dibuja, Muse Wonder escribe tu año."),
            ("Community Vote", "Los votos en la galería pública del hackathon."),
        ],
        "agents_title": "AGENTES",
        "agents": [
            (
                "Director de la simulación",
                "Usa herramientas de solo lectura como simulate_purchase y project_debt, así las consecuencias que narra usan números reales.",
            ),
            (
                "Planificador",
                "Arma el recorrido del año y repasa los conceptos en los que el usuario falló.",
            ),
            (
                "Vendedor",
                "Empuja plazos más largos para ganar más comisión, pero solo puede decir cifras de su ficha oculta o de sus herramientas.",
            ),
        ],
        "arch_title": "ARQUITECTURA",
        "boxes": [
            ("Cliente Unity 2D", ["Escritorio Windows", "motor financiero C#"]),
            ("Backend FastAPI", ["contratos Pydantic v2", "costos, agentes"]),
            ("API de Meta", ["Wonder, Spark,", "Voice, Image"]),
            ("Base de datos", ["SQLite o Postgres:", "sesiones, decisiones"]),
            ("Modelos abiertos", ["vendedores en Baseten", "Muse Spark Open"]),
        ],
        "rules": [
            "Ningún monto, tasa ni saldo sale de un modelo.",
            "La plata es un número entero de colones.",
            "Toda respuesta de un modelo se valida y tiene respaldo.",
            "Toda llamada pasa por un control de costos diario.",
            "Las llaves de API viven solo en el backend.",
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
BENCHMARK_ICONS = ["clock", "coin", "warning", "spark"]
WORKFLOW_ICONS = ["chart", "terminal", "stack"]
CONTRIBUTING_ICONS = ["folder", "terminal", "team", "warning"]
SECURITY_ICONS = ["warning", "stack"]
CONDUCT_ICONS = ["team", "person", "warning", "spark"]
PAGES: dict[str, dict[str, dict[str, Any]]] = {
    "benchmarks": {
        "en": {
            "lang": "en",
            "kicker": "FOR THE JUDGES",
            "title": "BENCHMARKS",
            "tagline": "Every model got its job by the clock.",
            "desc": "The latency and cost of every Meta model in ColonQuest, measured with real keys, and the decisions that came from them. The full content of this card is repeated as text below it.",
            "icons": BENCHMARK_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Measured on 28 and 29 October with real keys",
            "blocks": [
                ("header", "WHO ANSWERS IN TIME"),
                ("stats", [
                    ("4.4 s", "Muse Spark at most, with minimal effort: always inside the 8 s, so it runs live."),
                    ("10.5 s", "Muse Wonder at most: the best writer, too slow live, so it runs in the background."),
                    ("2.8 s", "Muse Spark Open on Baseten at most: the fastest, so it voices every character."),
                    ("17 s", "Muse Image per picture: drawn once beforehand, never while you play."),
                ]),
                ("header", "WHAT A RUN COSTS"),
                ("stats", [
                    ("$0.06", "a three-month run: $0.04 of Meta credit and $0.02 of Baseten."),
                    ("$0.0025", "one turn of a conversation on Baseten."),
                    ("$0", "in images per run: they come in the zip."),
                ]),
                ("header", "WHAT WE LEARNED"),
                ("paragraph", "Every Meta model reasons before it answers, and those tokens count against max_tokens. With little room the answer came back empty, so every call leaves 2 000 tokens or more, and 3 072 more on Baseten."),
            ],
        },
        "es": {
            "lang": "es",
            "kicker": "PARA LOS JUECES",
            "title": "BENCHMARKS",
            "tagline": "Cada modelo tiene su trabajo por el reloj.",
            "desc": "La latencia y el costo de cada modelo de Meta en ColonQuest, medidos con llaves reales, y las decisiones que salieron de ahí. Todo el contenido de esta tarjeta se repite como texto debajo de ella.",
            "icons": BENCHMARK_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Medido el 28 y el 29 de octubre con llaves reales",
            "blocks": [
                ("header", "QUIÉN RESPONDE A TIEMPO"),
                ("stats", [
                    ("4,4 s", "Muse Spark como máximo, con esfuerzo mínimo: siempre dentro de los 8 s, así que corre en vivo."),
                    ("10,5 s", "Muse Wonder como máximo: el que mejor escribe, muy lento en vivo, así que corre en segundo plano."),
                    ("2,8 s", "Muse Spark Open en Baseten como máximo: el más rápido, así que le da voz a cada personaje."),
                    ("17 s", "Muse Image por imagen: se dibuja antes, nunca mientras jugás."),
                ]),
                ("header", "LO QUE CUESTA UNA SIMULACIÓN"),
                ("stats", [
                    ("$0,06", "tres meses: $0,04 de crédito de Meta y $0,02 de Baseten."),
                    ("$0,0025", "un turno de conversación en Baseten."),
                    ("$0", "en imágenes por simulación: vienen en el zip."),
                ]),
                ("header", "LO QUE APRENDIMOS"),
                ("paragraph", "Todos los modelos de Meta razonan antes de responder, y esos tokens cuentan contra max_tokens. Con poco margen la respuesta llegaba vacía, así que cada llamada deja 2 000 tokens o más, y 3 072 extra en Baseten."),
            ],
        },
    },
    "costa-rica": {
        "en": {
            "lang": "en",
            "kicker": "FROM COSTA RICA TO THE WORLD",
            "title": "COSTA RICA",
            "tagline": "The problem in numbers, in plain words.",
            "desc": "The situation of debt and financial education in Costa Rica, with cited figures. The full content of this card is repeated as text below it.",
            "icons": COSTA_RICA_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Every figure, with its source, in the text below",
            "blocks": [
                ("header", "THE SHORT VERSION"),
                ("stats", [
                    ("34%", "of monthly income goes to paying debts, on average."),
                    ("58.3%", "for those who earn under $1 000 a month."),
                    ("80.88%", "of people surveyed never had any financial education."),
                    ("2 930", "street-lender cases counted by investigators in 2025."),
                ]),
                ("header", "WHAT THE SIMULATOR COPIES"),
                ("stats", [
                    ("$750", "a month: the real 2026 minimum wage is your salary."),
                    ("3%", "a month on the card: 36% a year, under the legal cap of 36.48%."),
                    ("20%", "a month from the lender: 240% a year, over six times the legal cap."),
                ]),
            ],
        },
        "es": {
            "lang": "es",
            "kicker": "FROM COSTA RICA TO THE WORLD",
            "title": "COSTA RICA",
            "tagline": "El problema en cifras, en palabras claras.",
            "desc": "La situación de las deudas y la educación financiera en Costa Rica, con cifras citadas. Todo el contenido de esta tarjeta se repite como texto debajo de ella.",
            "icons": COSTA_RICA_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Cada cifra, con su fuente, en el texto de abajo",
            "blocks": [
                ("header", "LA VERSIÓN CORTA"),
                ("stats", [
                    ("34%", "del ingreso mensual se va en pagar deudas, en promedio."),
                    ("58,3%", "para quienes ganan menos de ₡500 000 al mes."),
                    ("80,88%", "de los encuestados nunca recibió educación financiera."),
                    ("2 930", "casos de gota a gota contados por el OIJ en 2025."),
                ]),
                ("header", "LO QUE EL SIMULADOR COPIA"),
                ("stats", [
                    ("₡373 092", "al mes: tu salario es el mínimo real de 2026."),
                    ("3%", "al mes en la tarjeta: 36% anual, bajo el tope legal de 36,48%."),
                    ("20%", "al mes con el prestamista: 240% anual, más de seis veces el tope legal."),
                ]),
            ],
        },
    },
    "run": {
        "en": {
            "lang": "en",
            "kicker": "FOR THE JUDGES",
            "title": "RUN AND BUILD",
            "tagline": "Unzip, paste your key, simulate.",
            "desc": "How to run ColonQuest on your computer with your own Meta key, with one launcher that starts the local backend, and how to build the executable. The full content of this card is repeated as text below it.",
            "icons": RUN_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Your keys stay on your computer",
            "blocks": [
                ("header", "EVALUATE IT IN THREE STEPS"),
                ("paragraph", "Unzip the latest release. Double-click start_colonquest.bat and paste your Meta key. It installs what it needs, starts the local backend and opens the simulator. Choose Guided simulation."),
                ("header", "YOUR KEYS, YOUR COMPUTER"),
                ("tiles", [
                    ("stack", "server/.env", "Your Meta and Baseten keys. Only the local backend reads them."),
                    ("folder", "colonquest.env", "Which backend the simulator talks to. No secrets."),
                ]),
                ("header", "THE LAUNCHER"),
                ("terminal", ["start_colonquest.bat"]),
                ("header", "BUILD THE EXECUTABLE"),
                ("terminal", ["cp colonquest.env.example colonquest.env", "python scripts/build_game.py"]),
            ],
        },
        "es": {
            "lang": "es",
            "kicker": "PARA LOS JUECES",
            "title": "CORRER Y ARMAR",
            "tagline": "Descomprimí, pegá tu llave, simulá.",
            "desc": "Cómo correr ColonQuest en tu computadora con tu propia llave de Meta, con un lanzador que levanta el backend local, y cómo armar el ejecutable. Todo el contenido de esta tarjeta se repite como texto debajo de ella.",
            "icons": RUN_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Tus llaves se quedan en tu computadora",
            "blocks": [
                ("header", "EVALUALO EN TRES PASOS"),
                ("paragraph", "Descomprimí el último release. Hacé doble clic en start_colonquest.bat y pegá tu llave de Meta. Instala lo que necesita, levanta el backend local y abre el simulador. Elegí Simulación guiada."),
                ("header", "TUS LLAVES, TU COMPUTADORA"),
                ("tiles", [
                    ("stack", "server/.env", "Tus llaves de Meta y Baseten. Solo las lee el backend local."),
                    ("folder", "colonquest.env", "A qué backend habla el simulador. Sin secretos."),
                ]),
                ("header", "EL LANZADOR"),
                ("terminal", ["start_colonquest.bat"]),
                ("header", "ARMAR EL EJECUTABLE"),
                ("terminal", ["cp colonquest.env.example colonquest.env", "python scripts/build_game.py"]),
            ],
        },
    },
    # The community pages are written in English only, like the files GitHub links them from.
    "workflow": {
        "en": {
            "lang": "en",
            "kicker": "HOW WE WORKED",
            "title": "WORKFLOW",
            "tagline": "Jira tickets, small PRs, green CI.",
            "desc": "How the DoWhile Team built ColonQuest: Jira sprints, one branch and pull request per ticket, the checks of CI and the releases. The full content of this card is repeated as text below it.",
            "icons": WORKFLOW_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Every change traceable to a Jira ticket",
            "blocks": [
                ("header", "EIGHT DAYS, FOUR SPRINTS"),
                ("stats", [
                    ("150", "Jira tickets, each with its own branch and pull request."),
                    ("142", "pull requests merged into develop, every one with green CI."),
                    ("790+", "small commits since 26 October, all in the history."),
                ]),
                ("header", "WHAT CI CHECKS"),
                ("tiles", [
                    ("terminal", "Server", "Ruff, mypy --strict and pytest."),
                    ("coin", "Finance engine", "Its NUnit tests, outside Unity."),
                    ("bolt", "Unity", "EditMode tests on every change."),
                    ("warning", "Jira", "Every title and branch names its ticket."),
                ]),
                ("header", "RELEASES"),
                ("paragraph", "main only receives tagged releases from develop. Each one carries the Windows zip that a judge runs with their own key."),
            ],
        },
    },
    "contributing": {
        "en": {
            "lang": "en",
            "kicker": "FOR CONTRIBUTORS",
            "title": "CONTRIBUTING",
            "tagline": "One ticket, one branch, small commits.",
            "desc": "How to contribute to ColonQuest: branches, commits, pull requests and code rules. The full content of this card is repeated as text below it.",
            "icons": CONTRIBUTING_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Every change traceable to a Jira ticket",
            "blocks": [
                ("header", "BRANCHES"),
                ("tiles", [
                    ("stack", "main", "Stable releases only, each one tagged."),
                    ("folder", "develop", "Integration branch: every PR lands here."),
                    ("bolt", "type/SCRUM-n-...", "One branch per Jira ticket, from develop."),
                ]),
                ("header", "COMMITS"),
                ("terminal", ["feat(server): add health endpoint [SCRUM-7]"]),
                ("header", "PULL REQUESTS"),
                ("paragraph", "Small and focused on one ticket, split into commits that each build and pass the tests. Feature PRs go to develop; only releases and hotfixes go to main."),
                ("header", "RULES THAT DO NOT BREAK"),
                ("tiles", [
                    ("coin", "Integer colones", "Money is never a float."),
                    ("warning", "Keys in the backend", "Never in Unity, never in the repo."),
                    ("spark", "Validated answers", "Every model reply has a schema and a fallback."),
                ]),
            ],
        },
    },
    "security": {
        "en": {
            "lang": "en",
            "kicker": "FOR RESEARCHERS",
            "title": "SECURITY",
            "tagline": "Report privately. Keys stay in the backend.",
            "desc": "How to report a vulnerability in ColonQuest and how its secrets are handled. The full content of this card is repeated as text below it.",
            "icons": SECURITY_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Please never open a public issue for a vulnerability",
            "blocks": [
                ("header", "REPORT A VULNERABILITY"),
                ("paragraph", "Use GitHub private vulnerability reporting or write to keylorpineda729@gmail.com. We aim to answer within 48 hours during the hackathon."),
                ("header", "SECRETS"),
                ("tiles", [
                    ("stack", "Backend only", "Keys come from environment variables."),
                    ("folder", ".env is ignored", "server/.env.example is the reference."),
                    ("warning", "Leaked a key?", "Rotate it at once; history is not enough."),
                ]),
            ],
        },
    },
    "conduct": {
        "en": {
            "lang": "en",
            "kicker": "FOR EVERYONE",
            "title": "CODE OF CONDUCT",
            "tagline": "A harassment-free space for everyone.",
            "desc": "The code of conduct of ColonQuest, based on the Contributor Covenant 2.1. The full content of this card is repeated as text below it.",
            "icons": CONDUCT_ICONS,
            "footer": "FROM COSTA RICA TO THE WORLD",
            "license": "Based on the Contributor Covenant 2.1",
            "blocks": [
                ("header", "OUR PLEDGE"),
                ("paragraph", "Everyone takes part free of harassment, whatever their age, body, disability, origin, gender, experience, education, income, nationality, looks, religion or orientation."),
                ("header", "WE DO"),
                ("tiles", [
                    ("person", "Respect", "Different opinions and experiences."),
                    ("chart", "Feedback", "Given and taken with grace."),
                    ("team", "Community first", "What is best for everyone."),
                ]),
                ("header", "WE DO NOT"),
                ("tiles", [
                    ("warning", "Harass", "No insults or derogatory comments."),
                    ("folder", "Expose", "No private information without consent."),
                ]),
                ("header", "REPORT IT"),
                ("paragraph", "Write to keylorpineda729@gmail.com. Every report is reviewed promptly and fairly."),
            ],
        },
    },
}


COMMANDS = [
    "start_colonquest.bat",
    "cp colonquest.env.example colonquest.env",
    "python scripts/build_game.py",
]
MEMBERS = ["Keylor Pineda", "Jefferson Abarca"]
SECTION_ICONS = ["warning", "coin", "chart", "clock", "pyramid", "spark", "bag", "bolt", "stack", "person", "terminal", "team"]


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
    entries(card, copy["agents"])


def entries(card: Card, items: Sequence[tuple[str, str]]) -> None:
    """A list of named entries, each with a short paragraph under its name."""
    body, y = "", 20
    for name, text in items:
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
    card.header(copy["why_title"])
    stats(card, copy["why_stats"])
    card.paragraph(copy["why_close"])
    card.header(copy["month"][0])
    card.paragraph(copy["month"][1])
    card.header(copy["ways_title"])
    tile_rows(card, copy["ways"])
    card.header(copy["inside_title"])
    tile_rows(card, copy["inside"])
    card.header(copy["criteria_title"])
    criteria(card, copy)
    card.header(copy["categories_title"])
    entries(card, copy["categories"])
    card.header(copy["models_title"])
    tile_rows(card, copy["models"])
    card.header(copy["agents_title"])
    agents(card, copy)
    card.header(copy["arch_title"])
    architecture(card, copy)
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
    "benchmarks": {"en": "BENCHMARKS", "es": "BENCHMARKS"},
    "download": {"en": "DOWNLOAD", "es": "DESCARGAR"},
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
