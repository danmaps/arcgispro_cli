from __future__ import annotations

from textual.app import App, ComposeResult
from textual.containers import Horizontal, Vertical
from textual.widgets import Header, Footer

from arcgispro_cli.tui.panels.project_tree import ProjectTree
from arcgispro_cli.tui.panels.detail_panel import DetailPanel
from arcgispro_cli.tui.panels.log_panel import LogPanel
from arcgispro_cli.tui.panels.map_preview_panel import MapPreviewPanel
from arcgispro_cli.tui.state import TUIState


class ArcGISProCLIApp(App):
    """Textual UI for browsing .arcgispro session exports."""

    CSS_PATH = "theme.tcss"
    BINDINGS = [
        ("r", "reload", "Reload"),
        ("f1", "help", "Help"),
        ("q", "quit", "Quit"),
    ]

    def __init__(self, repo_path: str = ".", **kwargs):
        super().__init__(**kwargs)
        self.state = TUIState(repo_path=repo_path)

    def compose(self) -> ComposeResult:
        yield Header(show_clock=False)
        with Horizontal(id="main"):
            yield ProjectTree(id="tree-panel", state=self.state)
            with Vertical(id="right"):
                yield DetailPanel(id="detail", state=self.state)
                yield MapPreviewPanel(id="map-preview", classes="hidden")
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
        """Forward tree selection to detail panel and update map preview."""
        detail = self.query_one("#detail", DetailPanel)
        detail.show_item(msg.kind, msg.data)
        
        # Show map preview for maps and layers
        map_preview = self.query_one("#map-preview", MapPreviewPanel)
        
        if msg.kind in ("map", "layer"):
            # Get image path for the map
            from arcgispro_cli.paths import get_images_folder, sanitize_map_name
            
            ap = self.state.arcgispro_path
            if ap:
                img_folder = get_images_folder(ap)
                
                # For layers, get the map name; for maps, use the map name directly
                if msg.kind == "layer":
                    map_name = msg.data.get('mapName', '')
                else:
                    map_name = msg.data.get('name', '')
                
                sanitized = sanitize_map_name(map_name)
                img_path = img_folder / f"map_{sanitized}.png"
                
                map_preview.show_map_preview(msg.data, img_path)
                map_preview.remove_class("hidden")
            else:
                map_preview.add_class("hidden")
        else:
            # Hide map preview for non-map/layer items
            map_preview.add_class("hidden")

    def action_help(self) -> None:
        self.notify(
            "Navigate the tree to browse maps, layers, and fields. "
            "Press r to reload, q to quit.",
            title="Help",
        )
