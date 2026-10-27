"""Generate the pixel-art SVGs used by the README (docs/assets/*.svg).

Usage:
    python scripts/build_readme_art.py

Every image shares one palette (paper and ink, Costa Rican blue and red, colón ochre), hard
offset shadows and stepped, sprite-like animation. Each SVG carries a title and description for
screen readers and stops moving under prefers-reduced-motion. Standard library only.
"""

# ruff: noqa: E501 - SVG markup reads better on one line per element

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

BASE_STYLE = f"""
    .h {{ font: 800 30px {MONO}; fill: {INK}; letter-spacing: 1px; }}
    .num {{ font: 700 18px {MONO}; fill: {RED}; }}
    .title {{ font: 800 56px {MONO}; fill: {INK}; letter-spacing: 2px; }}
    .tagline {{ font: italic 500 24px {SERIF}; fill: {MUTED}; }}
    .event {{ font: 700 16px {MONO}; fill: {CARD}; letter-spacing: 1px; }}
    .label {{ font: 800 22px {MONO}; fill: {INK}; }}
    .label-sm {{ font: 800 20px {MONO}; fill: {INK}; }}
    .detail {{ font: 500 18px {SERIF}; fill: {MUTED}; }}
    .detail-sm {{ font: 500 16px {SERIF}; fill: {MUTED}; }}
    .step {{ font: 700 14px {MONO}; fill: {RED}; }}
    .pct {{ font: 800 24px {MONO}; fill: {RED}; }}
    .stamp {{ font: 800 18px {MONO}; fill: {RED}; letter-spacing: 2px; }}
    .rule {{ font: 500 18px {SERIF}; fill: {INK}; }}
    .strong {{ font-weight: 700; }}
    .blink {{ animation: blink 1s steps(1) infinite; }}
    .bob {{ animation: bob 1.2s steps(1) infinite; }}
    @keyframes blink {{ 50% {{ opacity: 0; }} }}
    @keyframes bob {{ 50% {{ transform: translateY(-6px); }} }}"""

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


def svg(name: str, height: int, title: str, desc: str, body: str, style: str = "") -> None:
    document = f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {WIDTH} {height}" width="{WIDTH}" height="{height}" role="img" aria-labelledby="t d">
  <title id="t">{escape(title)}</title>
  <desc id="d">{escape(desc)}</desc>
  <style>{BASE_STYLE}{style}{REDUCED}
  </style>
{body}
</svg>
"""
    (OUT / f"{name}.svg").write_text(document, encoding="utf-8", newline="\n")


# Every visible string, per language. English files keep the plain name; others get a suffix.
COPY: dict[str, dict[str, Any]] = {
    "en": {
        "suffix": "",
        "tagline": "Every money decision becomes a story.",
        "steps": [
            ("Decide", "pay, save, borrow"),
            ("Narrate", "Muse Wonder"),
            ("Compute", "exact colones"),
            ("Negotiate", "by voice"),
        ],
        "rule": ("The AI tells the story.", "The finance engine keeps the books."),
        "hero_desc": "A financial-literacy game for Costa Rican students. Each month the player "
        "decides what to do with their money, Muse Wonder narrates the consequence, a "
        "deterministic engine computes the exact colones, and the player negotiates by voice "
        "with characters who hide fees. The AI tells the story; the finance engine keeps the "
        "books.",
        "section": "Section",
        "headers": [
            "THE PROBLEM",
            "JUDGING CRITERIA",
            "META MODELS",
            "ARCHITECTURE",
            "ACCESSIBILITY",
            "GETTING STARTED",
            "REPOSITORY",
            "TEAM",
        ],
        "traps_title": "The traps",
        "traps_desc": "Four traps the game teaches players to spot: installments with insurance "
        "nobody mentioned; paying only the card minimum while the debt keeps growing; gota a "
        "gota lenders with fast cash and weekly interest; and pyramid schemes that pay you for "
        "recruiting friends.",
        "traps": [
            ("card", "Installments", "Just one payment a month...", "plus hidden insurance."),
            (
                "clock",
                "Minimum payment",
                "You pay a little each month",
                "and the debt keeps growing.",
            ),
            ("bag", "Gota a gota", "Fast cash today,", "interest every week."),
            ("pyramid", "Pyramid", "Bring three friends", "and get rich. Promise."),
        ],
        "stamp": "TRAP",
        "criteria_title": "Judging criteria",
        "criteria_desc": "Impact 30 percent: telemetry, Class Mode and a student pilot. Use of "
        "Meta tools 25 percent: four models, each with one job. Demo 25 percent: one link that "
        "never breaks because of a model. Technical depth 20 percent: validated contracts and "
        "an exact engine.",
        "criteria": [
            ("Impact", 30, "telemetry, Class Mode, student pilot"),
            ("Meta tools", 25, "four models, each with one job"),
            ("Demo", 25, "one link, never breaks on a model"),
            ("Technical depth", 20, "validated contracts, exact engine"),
        ],
        "models_title": "Meta models",
        "models_desc": "Muse Wonder is the game master and the voice of the characters. Muse "
        "Spark writes the season plan, the year-end summary and Class Mode reports. Muse Voice "
        "Transcribe turns the voice of the player into text while negotiating. Muse Image draws "
        "pixel-art portraits, pre-generated and cached.",
        "models": [
            ("spark", "Muse Wonder", "Game master and the", "voices of the characters"),
            ("bolt", "Muse Spark", "Season plan, summary", "and Class Mode reports"),
            ("mic", "Voice Transcribe", "You negotiate out loud;", "typing always works too"),
            ("picture", "Muse Image", "Pixel-art portraits", "pre-generated and cached"),
        ],
        "arch_title": "Architecture",
        "arch_desc": "The Unity 2D client, which runs the C# finance engine on WebGL and "
        "desktop, talks JSON with a FastAPI backend that holds the Pydantic contracts, the cost "
        "guard, the cache and the agents. The backend calls the Meta Model API (Wonder, Spark, "
        "Voice and Image), stores sessions, decisions and model calls in Neon Postgres, and can "
        "optionally run the characters on open-weight models hosted on Baseten or Modal.",
        "boxes": [
            ("Unity 2D client", ["WebGL and desktop", "C# finance engine"]),
            ("FastAPI backend", ["Pydantic v2 contracts", "cost guard, cache, agents"]),
            ("Meta Model API", ["Wonder, Spark,", "Voice and Image"]),
            ("Neon Postgres", ["sessions, decisions,", "model calls"]),
            ("Open weights", ["optional NPCs", "Baseten or Modal"]),
        ],
        "footer": "Made in Costa Rica by DoWhile Team",
    },
    "es": {
        "suffix": "-es",
        "tagline": "Cada decisión de plata se vuelve una historia.",
        "steps": [
            ("Decidir", "pagar o ahorrar"),
            ("Narrar", "Muse Wonder"),
            ("Calcular", "colones exactos"),
            ("Negociar", "con la voz"),
        ],
        "rule": ("La IA cuenta la historia.", "El motor financiero lleva las cuentas."),
        "hero_desc": "Un juego de educación financiera para estudiantes de Costa Rica. Cada mes "
        "el jugador decide qué hacer con su plata, Muse Wonder narra la consecuencia, un motor "
        "determinista calcula los colones exactos y el jugador negocia por voz con personajes "
        "que esconden cargos. La IA cuenta la historia; el motor financiero lleva las cuentas.",
        "section": "Sección",
        "headers": [
            "EL PROBLEMA",
            "CRITERIOS DEL JURADO",
            "MODELOS DE META",
            "ARQUITECTURA",
            "ACCESIBILIDAD",
            "CÓMO EMPEZAR",
            "REPOSITORIO",
            "EQUIPO",
        ],
        "traps_title": "Las trampas",
        "traps_desc": "Cuatro trampas que el juego enseña a detectar: cuotas con un seguro que "
        "nadie mencionó; pagar solo el mínimo de la tarjeta mientras la deuda crece; "
        "prestamistas gota a gota con plata rápida e intereses semanales; y esquemas piramidales "
        "que te pagan por meter amigos.",
        "traps": [
            ("card", "Cuotas", "Solo una cuota al mes...", "y un seguro escondido."),
            ("clock", "Pago mínimo", "Pagás poquito cada mes", "y la deuda sigue creciendo."),
            ("bag", "Gota a gota", "Plata rápida hoy,", "intereses cada semana."),
            ("pyramid", "Pirámide", "Traé tres amigos", "y te hacés rico. Seguro."),
        ],
        "stamp": "TRAMPA",
        "criteria_title": "Criterios del jurado",
        "criteria_desc": "Impacto 30 por ciento: telemetría, Modo Clase y una prueba piloto con "
        "estudiantes. Herramientas de Meta 25 por ciento: cuatro modelos, cada uno con un rol. "
        "Demo 25 por ciento: un link que nunca se cae por un modelo. Profundidad técnica 20 por "
        "ciento: contratos validados y un motor exacto.",
        "criteria": [
            ("Impacto", 30, "telemetría, Modo Clase, piloto"),
            ("Meta", 25, "cuatro modelos, cada uno con un rol"),
            ("Demo", 25, "un link que no se cae por un modelo"),
            ("Técnica", 20, "contratos validados, motor exacto"),
        ],
        "models_title": "Modelos de Meta",
        "models_desc": "Muse Wonder es el game master y la voz de los personajes. Muse Spark "
        "escribe el plan de la temporada, el resumen del año y los reportes del Modo Clase. Muse "
        "Voice Transcribe convierte la voz del jugador en texto al negociar. Muse Image dibuja "
        "retratos en pixel art, pre-generados y en caché.",
        "models": [
            ("spark", "Muse Wonder", "Game master y la", "voz de los personajes"),
            ("bolt", "Muse Spark", "Plan de temporada,", "resumen y Modo Clase"),
            ("mic", "Voice Transcribe", "Negociás en voz alta;", "escribir siempre sirve"),
            ("picture", "Muse Image", "Retratos en pixel art", "pre-generados y en caché"),
        ],
        "arch_title": "Arquitectura",
        "arch_desc": "El cliente Unity 2D, que corre el motor financiero en C# en WebGL y "
        "escritorio, se comunica por JSON con un backend FastAPI que tiene los contratos "
        "Pydantic, el control de costos, la caché y los agentes. El backend llama a la API de "
        "modelos de Meta (Wonder, Spark, Voice e Image), guarda sesiones, decisiones y llamadas "
        "en Neon Postgres y puede correr los personajes en modelos abiertos en Baseten o Modal.",
        "boxes": [
            ("Cliente Unity 2D", ["WebGL y escritorio", "motor financiero C#"]),
            ("Backend FastAPI", ["contratos Pydantic v2", "costos, caché, agentes"]),
            ("API de Meta", ["Wonder, Spark,", "Voice e Image"]),
            ("Neon Postgres", ["sesiones, decisiones,", "llamadas a modelos"]),
            ("Modelos abiertos", ["NPCs opcionales", "Baseten o Modal"]),
        ],
        "footer": "Hecho en Costa Rica por DoWhile Team",
    },
}

HEADER_SLUGS = [
    ("problem", "warning"),
    ("criteria", "chart"),
    ("models", "spark"),
    ("architecture", "stack"),
    ("accessibility", "person"),
    ("start", "terminal"),
    ("repo", "folder"),
    ("team", "team"),
]


def header(slug: str, number: str, text: str, icon_name: str, copy: dict[str, Any]) -> None:
    cursor_x = 128 + len(text) * 18 + 10
    body = f"""  <rect x="0" y="0" width="{WIDTH}" height="72" fill="{PAPER}"/>
  <rect x="0" y="66" width="{WIDTH}" height="6" fill="{INK}"/>
  {panel(4, 10, 48, 48)}
  {icon(icon_name, 12, 18, 4)}
  <text class="num" x="72" y="44">{number}</text>
  <text class="h" x="128" y="46">{escape(text)}</text>
  <rect class="blink" x="{cursor_x}" y="22" width="14" height="28" fill="{OCHRE}"/>"""
    svg(f"h-{slug}{copy['suffix']}", 72, text, f"{copy['section']} {number}: {text}", body)


def hero(copy: dict[str, Any]) -> None:
    nodes = ""
    for i, (label, detail) in enumerate(copy["steps"]):
        x = 50 + 180 * i
        cx = x + 75
        nodes += f"""
  {panel(x, 252, 150, 108, cls=f"node n{i + 1}")}
  <text class="step" x="{cx}" y="280" text-anchor="middle">0{i + 1}</text>
  <text class="label" x="{cx}" y="314" text-anchor="middle">{escape(label)}</text>
  <text class="detail-sm" x="{cx}" y="342" text-anchor="middle">{escape(detail)}</text>"""
    style = f"""
    .node {{ animation: active 8s steps(1) infinite; }}
    .n2 {{ animation-delay: 2s; }}
    .n3 {{ animation-delay: 4s; }}
    .n4 {{ animation-delay: 6s; }}
    .runner {{ animation: run 8s steps(48) -1s infinite; }}
    @keyframes active {{ 0% {{ fill: {OCHRE}; }} 25% {{ fill: {CARD}; }} }}
    @keyframes run {{ 0% {{ transform: translateX(0); }} 100% {{ transform: translateX(720px); }} }}"""
    story, books = copy["rule"]
    body = f"""  <rect x="10" y="10" width="788" height="448" fill="{INK}"/>
  <rect x="2" y="2" width="788" height="448" fill="{PAPER}" stroke="{INK}" stroke-width="4"/>
  {flag(46, 34)}
  <text class="title" x="46" y="124">COLÓN QUEST</text>
  <text class="tagline" x="48" y="164">{escape(copy["tagline"])}</text>
  <rect x="46" y="182" width="430" height="30" fill="{INK}"/>
  <text class="event" x="58" y="203">META GLOBAL AI DEVELOPER HACKATHON 2026</text>
  <g class="bob">{big_coin(630, 64, 7)}</g>
  <rect x="640" y="178" width="78" height="7" fill="{INK}" opacity="0.18"/>
  <line x1="125" y1="306" x2="665" y2="306" stroke="{INK}" stroke-width="3" stroke-dasharray="6 6"/>{nodes}
  <clipPath id="track"><rect x="50" y="368" width="696" height="24"/></clipPath>
  <g clip-path="url(#track)"><g class="runner hide-still">{big_coin(-64, 373, 1.5)}</g></g>
  <line x1="46" y1="400" x2="746" y2="400" stroke="{INK}" stroke-width="2"/>
  <text class="rule" x="396" y="432" text-anchor="middle">{escape(story)} <tspan class="strong">{escape(books)}</tspan></text>"""
    svg(f"colon-quest-card{copy['suffix']}", 460, "Colón Quest", copy["hero_desc"], body, style)


def tiles(
    name: str,
    title: str,
    desc: str,
    items: Sequence[tuple[str, str, str, str]],
    stamp: str | None = None,
) -> None:
    """A 2x2 grid of cards: (icon, heading, line 1, line 2)."""
    w, h, gap, margin = 364, 140, 24, 24
    body = f'  <rect x="0" y="0" width="{WIDTH}" height="{2 * h + gap + 2 * margin + 6}" fill="{PAPER}"/>'
    keyframes = ""
    for i, (icon_name, heading, line1, line2) in enumerate(items):
        x = margin + (i % 2) * (w + gap)
        y = margin + (i // 2) * (h + gap)
        body += f"""
  {panel(x, y, w, h)}
  <g class="bob" style="animation-delay: {i * 0.3:g}s">{icon(icon_name, x + 20, y + 22, 6)}</g>
  <text class="label" x="{x + 88}" y="{y + 46}">{escape(heading)}</text>
  <text class="detail" x="{x + 88}" y="{y + 80}">{escape(line1)}</text>
  <text class="detail" x="{x + 88}" y="{y + 108}">{escape(line2)}</text>"""
        if stamp:
            sx, sy = x + w - 122, y + h - 30
            body += f"""
  <g class="stamp{i}" transform="rotate(-8 {sx + 50} {sy})">
    <rect x="{sx}" y="{sy - 20}" width="104" height="30" fill="none" stroke="{RED}" stroke-width="3"/>
    <text class="stamp" x="{sx + 52}" y="{sy + 1}" text-anchor="middle">{escape(stamp)}</text>
  </g>"""
            start = 10 + 18 * i
            keyframes += f"""
    .stamp{i} {{ animation: stamp{i} 8s steps(1) infinite; }}
    @keyframes stamp{i} {{ 0% {{ opacity: 0; }} {start}% {{ opacity: 1; }} 92% {{ opacity: 0; }} }}"""
    svg(name, 2 * h + gap + 2 * margin + 6, title, desc, body, keyframes)


def criteria(copy: dict[str, Any]) -> None:
    rows = copy["criteria"]
    body = f'  <rect x="0" y="0" width="{WIDTH}" height="352" fill="{PAPER}"/>\n  {panel(24, 20, 746, 306)}'
    style = f"""
    .seg {{ animation: flash 0.25s steps(1) 1; }}
    @keyframes flash {{ 0% {{ fill: {OCHRE_LIGHT}; }} 100% {{ fill: {OCHRE}; }} }}"""
    for i, (name, weight, line) in enumerate(rows):
        y = 44 + i * 72
        body += f"""
  <text class="label" x="48" y="{y + 26}">{escape(name)}</text>
  <text class="detail-sm" x="48" y="{y + 52}">{escape(line)}</text>"""
        for s in range(10):
            filled = s < weight // 5
            x = 418 + s * 24
            fill = OCHRE if filled else CARD
            delay = f' style="animation-delay: {0.3 + (i * 6 + s) * 0.08:.2f}s"' if filled else ""
            klass = ' class="seg"' if filled else ""
            body += (
                f'\n  <rect{klass}{delay} x="{x}" y="{y + 8}" width="18" height="28" '
                f'fill="{fill}" stroke="{INK}" stroke-width="2"/>'
            )
        body += f'\n  <text class="pct" x="746" y="{y + 32}" text-anchor="end">{weight}%</text>'
        if i < len(rows) - 1:
            body += f'\n  <line x1="48" y1="{y + 66}" x2="746" y2="{y + 66}" stroke="{INK}" stroke-width="1" stroke-dasharray="4 6"/>'
    svg(
        f"criteria{copy['suffix']}", 352, copy["criteria_title"], copy["criteria_desc"], body, style
    )


def architecture(copy: dict[str, Any]) -> None:
    def box(
        x: int, y: int, w: int, h: int, content: tuple[str, Sequence[str]], dashed: bool = False
    ) -> str:
        title, lines = content
        if dashed:
            frame = (
                f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{PAPER}" stroke="{INK}" '
                f'stroke-width="3" stroke-dasharray="8 6"/>'
            )
        else:
            frame = panel(x, y, w, h)
        text = f'<text class="label-sm" x="{x + w / 2:g}" y="{y + 34}" text-anchor="middle">{escape(title)}</text>'
        for j, line in enumerate(lines):
            text += (
                f'<text class="detail-sm" x="{x + w / 2:g}" y="{y + 62 + j * 22}" '
                f'text-anchor="middle">{escape(line)}</text>'
            )
        return frame + text

    client, backend, meta, neon, open_weights = copy["boxes"]
    style = """
    .p1 { animation: p1 3s steps(30) infinite; }
    .p2 { animation: p2 4s steps(40) infinite; }
    .p3 { animation: p3 4s steps(40) 1.3s infinite; }
    .p4 { animation: p4 4s steps(40) 2.6s infinite; }
    @keyframes p1 { 0% { transform: translate(0, 0); } 50% { transform: translate(56px, 0); } 100% { transform: translate(0, 0); } }
    @keyframes p2 { 0% { transform: translate(0, 0); } 30% { transform: translate(0, 52px); } 100% { transform: translate(-456px, 52px); } }
    @keyframes p3 { 0% { transform: translate(0, 0); } 60% { transform: translate(0, 52px); } 100% { transform: translate(-200px, 52px); } }
    @keyframes p4 { 0% { transform: translate(0, 0); } 40% { transform: translate(0, 52px); } 100% { transform: translate(56px, 52px); } }"""
    body = f"""  <rect x="0" y="0" width="{WIDTH}" height="400" fill="{PAPER}"/>
  {box(24, 24, 330, 128, client)}
  {box(440, 24, 330, 128, backend)}
  <line x1="360" y1="88" x2="434" y2="88" stroke="{INK}" stroke-width="4"/>
  <text class="step" x="397" y="78" text-anchor="middle">JSON</text>
  <line x1="605" y1="158" x2="605" y2="212" stroke="{INK}" stroke-width="4"/>
  <line x1="139" y1="212" x2="661" y2="212" stroke="{INK}" stroke-width="4"/>
  <line x1="139" y1="212" x2="139" y2="250" stroke="{INK}" stroke-width="4"/>
  <line x1="405" y1="212" x2="405" y2="250" stroke="{INK}" stroke-width="4"/>
  <line x1="661" y1="212" x2="661" y2="250" stroke="{INK}" stroke-width="4" stroke-dasharray="6 5"/>
  {box(24, 250, 230, 126, meta)}
  {box(290, 250, 230, 126, neon)}
  {box(556, 250, 214, 126, open_weights, dashed=True)}
  <rect class="p1 hide-still" x="364" y="83" width="10" height="10" fill="{OCHRE}" stroke="{INK}" stroke-width="2"/>
  <rect class="p2 hide-still" x="600" y="160" width="10" height="10" fill="{OCHRE}" stroke="{INK}" stroke-width="2"/>
  <rect class="p3 hide-still" x="600" y="160" width="10" height="10" fill="{BLUE}" stroke="{INK}" stroke-width="2"/>
  <rect class="p4 hide-still" x="600" y="160" width="10" height="10" fill="{RED}" stroke="{INK}" stroke-width="2"/>"""
    svg(f"architecture{copy['suffix']}", 400, copy["arch_title"], copy["arch_desc"], body, style)


def footer(copy: dict[str, Any]) -> None:
    body = f"""  <rect x="0" y="0" width="{WIDTH}" height="96" fill="{PAPER}"/>
  <rect x="0" y="0" width="{WIDTH}" height="6" fill="{INK}"/>
  {flag(24, 36)}
  <text class="label" x="112" y="58">{escape(copy["footer"])}</text>
  <g class="bob">{big_coin(712, 28, 4)}</g>"""
    svg(f"footer{copy['suffix']}", 96, copy["footer"], copy["footer"] + ".", body)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for copy in COPY.values():
        suffix = copy["suffix"]
        hero(copy)
        tiles(
            f"traps{suffix}", copy["traps_title"], copy["traps_desc"], copy["traps"], copy["stamp"]
        )
        criteria(copy)
        tiles(f"models{suffix}", copy["models_title"], copy["models_desc"], copy["models"])
        architecture(copy)
        headers = zip(HEADER_SLUGS, copy["headers"], strict=True)
        for number, ((slug, icon_name), text) in enumerate(headers, start=1):
            header(slug, f"{number:02d}", text, icon_name, copy)
        footer(copy)


if __name__ == "__main__":
    main()
