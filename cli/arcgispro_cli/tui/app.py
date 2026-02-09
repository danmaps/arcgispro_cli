from __future__ import annotations

from textual.app import App, ComposeResult
from textual.containers import Horizontal, Vertical
from textual.widgets import Footer

from arcgispro_cli.tui.banner import Banner
from arcgispro_cli.tui.panels.project_tree import ProjectTree
from arcgispro_cli.tui.panels.detail_panel import DetailPanel
from arcgispro_cli.tui.panels.log_panel import LogPanel
from arcgispro_cli.tui.state import TUIState


class ArcGISProCLIApp(App):
    """Textual UI for browsing .arcgispro session exports."""

    CSS_PATH = "theme.tcss"
    BINDINGS = [
        ("r", "reload", "Reload"),
        ("f1", "help", "Help"),
        ("q", "quit", "Quit"),
    ]

    def __init__(self, repo_path: str = ".", *, show_banner: bool = True, **kwargs):
        super().__init__(**kwargs)
        self.state = TUIState(repo_path=repo_path)
        self.show_banner = show_banner

    def compose(self) -> ComposeResult:
        yield Banner(enabled=self.show_banner, id="banner")
        with Horizontal(id="main"):
            yield ProjectTree(id="tree-panel", state=self.state)
            with Vertical(id="right"):
                yield DetailPanel(id="detail", state=self.state)
                yield LogPanel(id="logs")
        yield Footer()

    def action_reload(self) -> None:
        """Reload context files from disk."""
        self.state.reload()
        tree = self.query_one("#tree-panel", ProjectTree)
        tree.rebuild()
        log = self.query_one("#logs", LogPanel)
        log.write("[green]Reloaded .arcgispro context[/]")

    def on_project_tree_item_selected(self, msg: ProjectTree.ItemSelected) -> None:
        """Forward tree selection to detail panel."""
        detail = self.query_one("#detail", DetailPanel)
        detail.show_item(msg.kind, msg.data)

    def action_help(self) -> None:
        self.notify(
            "Navigate the tree to browse maps, layers, and fields. "
            "Press r to reload, q to quit.",
            title="Help",
        )
