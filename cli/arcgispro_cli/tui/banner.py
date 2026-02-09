from __future__ import annotations

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


class Banner(Widget):
    """Top-of-screen banner with oh-my-logo rendering."""

    DEFAULT_CSS = """
    Banner {
        height: auto;
    }
    Banner > Static { color: #87CEEB; }
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

        text = f"{LOGO}\n{TAGLINE}  ·  v{__version__}"
        self._body.update(text)
