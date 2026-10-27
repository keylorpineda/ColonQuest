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


def header(slug: str, number: str, text: str, icon_name: str) -> None:
    cursor_x = 128 + len(text) * 18 + 10
    body = f"""  <rect x="0" y="0" width="{WIDTH}" height="72" fill="{PAPER}"/>
  <rect x="0" y="66" width="{WIDTH}" height="6" fill="{INK}"/>
  {panel(4, 10, 48, 48)}
  {icon(icon_name, 12, 18, 4)}
  <text class="num" x="72" y="44">{number}</text>
  <text class="h" x="128" y="46">{escape(text)}</text>
  <rect class="blink" x="{cursor_x}" y="22" width="14" height="28" fill="{OCHRE}"/>"""
    svg(f"h-{slug}", 72, text, f"Section {number}: {text}", body)


def hero() -> None:
    steps = [
        ("Decide", "pay, save, borrow"),
        ("Narrate", "Muse Wonder"),
        ("Compute", "exact colones"),
        ("Negotiate", "by voice"),
    ]
    nodes = ""
    for i, (label, detail) in enumerate(steps):
        x = 50 + 180 * i
        cx = x + 75
        nodes += f"""
  {panel(x, 252, 150, 108, cls=f"node n{i + 1}")}
  <text class="step" x="{cx}" y="280" text-anchor="middle">0{i + 1}</text>
  <text class="label" x="{cx}" y="314" text-anchor="middle">{label}</text>
  <text class="detail-sm" x="{cx}" y="342" text-anchor="middle">{detail}</text>"""
    style = f"""
    .node {{ animation: active 8s steps(1) infinite; }}
    .n2 {{ animation-delay: 2s; }}
    .n3 {{ animation-delay: 4s; }}
    .n4 {{ animation-delay: 6s; }}
    .runner {{ animation: run 8s steps(48) -1s infinite; }}
    @keyframes active {{ 0% {{ fill: {OCHRE}; }} 25% {{ fill: {CARD}; }} }}
    @keyframes run {{ 0% {{ transform: translateX(0); }} 100% {{ transform: translateX(720px); }} }}"""
    body = f"""  <rect x="10" y="10" width="788" height="448" fill="{INK}"/>
  <rect x="2" y="2" width="788" height="448" fill="{PAPER}" stroke="{INK}" stroke-width="4"/>
  {flag(46, 34)}
  <text class="title" x="46" y="124">COLÓN QUEST</text>
  <text class="tagline" x="48" y="164">Every money decision becomes a story.</text>
  <rect x="46" y="182" width="430" height="30" fill="{INK}"/>
  <text class="event" x="58" y="203">META GLOBAL AI DEVELOPER HACKATHON 2026</text>
  <g class="bob">{big_coin(630, 64, 7)}</g>
  <rect x="640" y="178" width="78" height="7" fill="{INK}" opacity="0.18"/>
  <line x1="125" y1="306" x2="665" y2="306" stroke="{INK}" stroke-width="3" stroke-dasharray="6 6"/>{nodes}
  <clipPath id="track"><rect x="50" y="368" width="696" height="24"/></clipPath>
  <g clip-path="url(#track)"><g class="runner hide-still">{big_coin(-64, 373, 1.5)}</g></g>
  <line x1="46" y1="400" x2="746" y2="400" stroke="{INK}" stroke-width="2"/>
  <text class="rule" x="396" y="432" text-anchor="middle">The AI tells the story. <tspan class="strong">The finance engine keeps the books.</tspan></text>"""
    svg(
        "colon-quest-card",
        460,
        "Colón Quest",
        "A financial-literacy game for Costa Rican students. Each month the player decides what "
        "to do with their money, Muse Wonder narrates the consequence, a deterministic engine "
        "computes the exact colones, and the player negotiates by voice with characters who hide "
        "fees. The AI tells the story; the finance engine keeps the books.",
        body,
        style,
    )


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


def criteria() -> None:
    rows = [
        ("Impact", 30, "telemetry, Class Mode, student pilot"),
        ("Meta tools", 25, "four models, each with one job"),
        ("Demo", 25, "one link, never breaks on a model"),
        ("Technical depth", 20, "validated contracts, exact engine"),
    ]
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
        "criteria",
        352,
        "Judging criteria",
        "Impact 30 percent: telemetry, Class Mode and a student pilot. Use of Meta tools 25 "
        "percent: four models, each with one job. Demo 25 percent: one link that never breaks "
        "because of a model. Technical depth 20 percent: validated contracts and an exact engine.",
        body,
        style,
    )


def architecture() -> None:
    def box(
        x: int, y: int, w: int, h: int, title: str, lines: Sequence[str], dashed: bool = False
    ) -> str:
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
  {box(24, 24, 330, 128, "Unity 2D client", ["WebGL and desktop", "C# finance engine"])}
  {box(440, 24, 330, 128, "FastAPI backend", ["Pydantic v2 contracts", "cost guard, cache, agents"])}
  <line x1="360" y1="88" x2="434" y2="88" stroke="{INK}" stroke-width="4"/>
  <text class="step" x="397" y="78" text-anchor="middle">JSON</text>
  <line x1="605" y1="158" x2="605" y2="212" stroke="{INK}" stroke-width="4"/>
  <line x1="139" y1="212" x2="661" y2="212" stroke="{INK}" stroke-width="4"/>
  <line x1="139" y1="212" x2="139" y2="250" stroke="{INK}" stroke-width="4"/>
  <line x1="405" y1="212" x2="405" y2="250" stroke="{INK}" stroke-width="4"/>
  <line x1="661" y1="212" x2="661" y2="250" stroke="{INK}" stroke-width="4" stroke-dasharray="6 5"/>
  {box(24, 250, 230, 126, "Meta Model API", ["Wonder, Spark,", "Voice and Image"])}
  {box(290, 250, 230, 126, "Neon Postgres", ["sessions, decisions,", "model calls"])}
  {box(556, 250, 214, 126, "Open weights", ["optional NPCs", "Baseten or Modal"], dashed=True)}
  <rect class="p1 hide-still" x="364" y="83" width="10" height="10" fill="{OCHRE}" stroke="{INK}" stroke-width="2"/>
  <rect class="p2 hide-still" x="600" y="160" width="10" height="10" fill="{OCHRE}" stroke="{INK}" stroke-width="2"/>
  <rect class="p3 hide-still" x="600" y="160" width="10" height="10" fill="{BLUE}" stroke="{INK}" stroke-width="2"/>
  <rect class="p4 hide-still" x="600" y="160" width="10" height="10" fill="{RED}" stroke="{INK}" stroke-width="2"/>"""
    svg(
        "architecture",
        400,
        "Architecture",
        "The Unity 2D client, which runs the C# finance engine on WebGL and desktop, talks JSON "
        "with a FastAPI backend that holds the Pydantic contracts, the cost guard, the cache and "
        "the agents. The backend calls the Meta Model API (Wonder, Spark, Voice and Image), "
        "stores sessions, decisions and model calls in Neon Postgres, and can optionally run "
        "the characters on open-weight models hosted on Baseten or Modal.",
        body,
        style,
    )


def footer() -> None:
    body = f"""  <rect x="0" y="0" width="{WIDTH}" height="96" fill="{PAPER}"/>
  <rect x="0" y="0" width="{WIDTH}" height="6" fill="{INK}"/>
  {flag(24, 36)}
  <text class="label" x="112" y="58">Hecho en Costa Rica por DoWhile Team</text>
  <g class="bob">{big_coin(712, 28, 4)}</g>"""
    svg("footer", 96, "Made in Costa Rica", "Made in Costa Rica by the DoWhile Team.", body)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    hero()
    tiles(
        "traps",
        "The traps",
        "Four traps the game teaches players to spot: installments with insurance nobody "
        "mentioned; paying only the card minimum while interest keeps growing; gota a gota "
        "lenders with fast money and brutal interest; and pyramid schemes that pay you for "
        "recruiting friends.",
        [
            ("card", "Cuotas", "Solo una cuota al mes...", "y un seguro escondido."),
            ("clock", "Pago mínimo", "Pagás poquito cada mes", "y la deuda sigue creciendo."),
            ("bag", "Gota a gota", "Plata rápida hoy,", "intereses cada semana."),
            ("pyramid", "Pirámide", "Traé tres amigos", "y te hacés rico. Seguro."),
        ],
        stamp="TRAMPA",
    )
    criteria()
    tiles(
        "models",
        "Meta models",
        "Muse Wonder is the game master and the voice of the characters. Muse Spark writes the "
        "season plan, the year-end summary and Class Mode explanations. Muse Voice Transcribe "
        "turns the player's voice into text while negotiating. Muse Image draws pixel-art "
        "portraits and scenes, pre-generated and cached.",
        [
            ("spark", "Muse Wonder", "Game master and the", "voices of the characters"),
            ("bolt", "Muse Spark", "Season plan, summary", "and Class Mode reports"),
            ("mic", "Voice Transcribe", "You negotiate out loud;", "typing always works too"),
            ("picture", "Muse Image", "Pixel-art portraits", "pre-generated and cached"),
        ],
    )
    architecture()
    for slug, number, text, icon_name in [
        ("problem", "01", "THE PROBLEM", "warning"),
        ("criteria", "02", "JUDGING CRITERIA", "chart"),
        ("models", "03", "META MODELS", "spark"),
        ("architecture", "04", "ARCHITECTURE", "stack"),
        ("accessibility", "05", "ACCESSIBILITY", "person"),
        ("start", "06", "GETTING STARTED", "terminal"),
        ("repo", "07", "REPOSITORY", "folder"),
        ("team", "08", "TEAM", "team"),
    ]:
        header(slug, number, text, icon_name)
    footer()


if __name__ == "__main__":
    main()
