from __future__ import annotations

from typing import List

from rich.panel import Panel
from textual.widgets import Static


class LogPanel(Static):
    """Scrollable log output panel."""

    MAX_LINES = 500

    def on_mount(self) -> None:
        self.lines: List[str] = []
        self.update(Panel("Ready.", title="Log", border_style="dim"))

    def write(self, msg: str) -> None:
        self.lines.append(msg)
        tail = "\n".join(self.lines[-self.MAX_LINES:])
        self.update(Panel(tail, title="Log", border_style="dim"))
