from __future__ import annotations

from rich.text import Text
from textual.widget import Widget
from textual.widgets import Static

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


def _colorize_logo() -> Text:
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


class Banner(Widget):
    """Top-of-screen banner with oh-my-logo rendering."""

    DEFAULT_CSS = """
    Banner {
        height: auto;
    }
    """

    def __init__(self, *, enabled: bool = True, **kwargs):
        super().__init__(**kwargs)
        self.enabled = enabled
        self._body = Static("")

    def compose(self):
        yield self._body

    def on_mount(self) -> None:
        self._refresh()

    def on_resize(self) -> None:
        self._refresh()

    def _refresh(self) -> None:
        if not self.enabled:
            self._body.update("")
            return

        self._body.update(_colorize_logo())
