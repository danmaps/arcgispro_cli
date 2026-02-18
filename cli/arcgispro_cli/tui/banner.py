from __future__ import annotations

from rich.text import Text
from textual.widget import Widget
from textual.widgets import Static

from arcgispro_cli import __version__


TAGLINE = "Automate ArcGIS Pro from your terminal"

# Generated with: npx oh-my-logo "ArcGISPro CLI" ocean --filled --letter-spacing 0
LOGO = """\
 █████╗ ██████╗  ██████╗ ██████╗ ██╗███████╗██████╗ ██████╗  ██████╗     ██████╗██╗     ██╗
██╔══██╗██╔══██╗██╔════╝██╔════╝ ██║██╔════╝██╔══██╗██╔══██╗██╔═══██╗   ██╔════╝██║     ██║
███████║██████╔╝██║     ██║  ███╗██║███████╗██████╔╝██████╔╝██║   ██║   ██║     ██║     ██║
██╔══██║██╔══██╗██║     ██║   ██║██║╚════██║██╔═══╝ ██╔══██╗██║   ██║   ██║     ██║     ██║
██║  ██║██║  ██║╚██████╗╚██████╔╝██║███████║██║     ██║  ██║╚██████╔╝   ╚██████╗███████╗██║
╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝ ╚═════╝ ╚═╝╚══════╝╚═╝     ╚═╝  ╚═╝ ╚═════╝     ╚═════╝╚══════╝╚═╝"""

SHADOW_CHARS = set("╔╗╚╝═║")
BLOCK_COLOR = "#87CEEB"
SHADOW_COLOR = "#555555"


def _colorize_logo() -> Text:
    """Apply light blue to block chars and dark grey to shadow chars."""
    text = Text()
    for ch in LOGO:
        if ch in SHADOW_CHARS:
            text.append(ch, style=SHADOW_COLOR)
        elif ch == "\n" or ch == " ":
            text.append(ch)
        else:
            text.append(ch, style=BLOCK_COLOR)
    text.append(f"\n{TAGLINE}  ·  v{__version__}", style="dim")
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
