from __future__ import annotations

from textual.widget import Widget
from textual.widgets import Static


WIDE = r"""    _                 _     ____            ____ _     ___
   / \   _ __ ___  __ _(_)___|  _ \ _ __ ___ / ___| |   |_ _|
  / _ \ | '__/ __|/ _` | / __| |_) | '__/ _ \ |   | |    | |
 / ___ \| | | (__| (_| | \__ \  __/| | | (_) | |___| |___ | |
/_/   \_\_|  \___|\__, |_|___/_|   |_|  \___/ \____|_____|___|
                  |___/"""

COMPACT = "ArcGISPro CLI"
TAGLINE = "Automate ArcGIS Pro from your terminal"


class Banner(Widget):
    """Top-of-screen banner with width-based fallback."""

    DEFAULT_CSS = """
    Banner { margin: 0 1; }
    Banner > Static { color: $text; }
    """

    def __init__(self, *, enabled: bool = True, **kwargs):
        super().__init__(**kwargs)
        self.enabled = enabled
        self._body = Static("")

    def compose(self):
        # Always mount, but render empty when disabled.
        yield self._body

    def on_mount(self) -> None:
        self._refresh()

    def on_resize(self) -> None:
        self._refresh()

    def _refresh(self) -> None:
        if not self.enabled:
            self._body.update("")
            return

        w = self.size.width or 0

        # Choose a safe default for narrow terminals.
        if w >= 80:
            text = WIDE
            # Add tagline if it won't obviously wrap.
            if w >= 80:
                text = text + "\n" + TAGLINE
        elif w >= 40:
            text = COMPACT + "\n" + TAGLINE
        else:
            text = COMPACT

        self._body.update(text)
