from __future__ import annotations

from textual.widget import Widget
from textual.widgets import Static

from arcgispro_cli.logo import colorize_logo

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

        self._body.update(colorize_logo())
