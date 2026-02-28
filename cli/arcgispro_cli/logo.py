from __future__ import annotations

from rich.text import Text

LOGO_LINES = [
    " █████╗ ██████╗  ██████╗  ██████╗ ██╗███████╗",
    "██╔══██╗██╔══██╗██╔════╝ ██╔════╝ ██║██╔════╝",
    "███████║██████╔╝██║      ██║  ███╗██║███████╗",
    "██╔══██║██╔══██╗██║      ██║   ██║██║╚════██║",
    "██║  ██║██║  ██║╚██████╗ ╚██████╔╝██║███████║",
    "╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝  ╚═════╝ ╚═╝╚══════╝",
]

BLUES = [
    "#9dd8ff",
    "#87cefa",
    "#6fbff6",
    "#55aef0",
    "#3f9be8",
    "#2f88e0",
]

GRAYS = [
    "color(250)",
    "color(248)",
    "color(245)",
    "color(243)",
    "color(240)",
    "color(238)",
]

SHADOW_CHARS = set("╔╗╚╝═║")


def colorize_logo() -> Text:
    """Render ARCGIS logo with blue gradient fills and gray shadow strokes."""
    text = Text()
    for row_index, line in enumerate(LOGO_LINES):
        block_color = BLUES[min(row_index, len(BLUES) - 1)]
        shadow_color = GRAYS[min(row_index, len(GRAYS) - 1)]
        for ch in line:
            if ch in SHADOW_CHARS:
                text.append(ch, style=shadow_color)
            elif ch == " ":
                text.append(ch)
            else:
                text.append(ch, style=block_color)
        if row_index < len(LOGO_LINES) - 1:
            text.append("\n")
    return text
