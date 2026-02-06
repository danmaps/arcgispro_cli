from __future__ import annotations

from pathlib import Path
from typing import Any, Dict

from rich.panel import Panel
from rich.table import Table
from textual.app import ComposeResult
from textual.widgets import Static

from arcgispro_cli.tui.panels.project_tree import ProjectTree

# Keys to skip when rendering a generic property table
_SKIP_KEYS = {"_kind", "_layer", "fields"}


class DetailPanel(Static):
    """Right-hand pane showing properties of the selected tree item."""

    def __init__(self, state, **kwargs) -> None:
        super().__init__(**kwargs)
        self.state = state

    def on_mount(self) -> None:
        self.update(Panel("Select an item in the tree.", title="Details", border_style="cyan"))

    def show_item(self, kind: str, data: Dict[str, Any]) -> None:
        """Display details for the selected item."""
        if kind == "project":
            self._show_project(data)
        elif kind == "map":
            self._show_map(data)
        elif kind == "layer":
            self._show_layer(data)
        elif kind == "field":
            self._show_field(data)
        elif kind == "table":
            self._show_table(data)
        elif kind == "connection":
            self._show_connection(data)
        else:
            self._show_generic(kind, data)

    # ------------------------------------------------------------------
    # Renderers
    # ------------------------------------------------------------------

    def _show_project(self, d: Dict[str, Any]) -> None:
        lines = [
            f"[bold]{d.get('name', 'Unknown')}[/]",
            f"Path: {d.get('path', '-')}",
            f"Default GDB: {d.get('defaultGeodatabase', '-')}",
            f"Maps: {len(d.get('mapNames', []))}",
            f"Layouts: {len(d.get('layoutNames', []))}",
        ]
        self.update(Panel("\n".join(lines), title="Project", border_style="cyan"))

    def _show_map(self, d: Dict[str, Any]) -> None:
        active = " ★ Active" if d.get("isActiveMap") else ""
        lines = [
            f"[bold]{d.get('name', 'Unknown')}[/]{active}",
            f"Type: {d.get('mapType', '-')}",
            f"Spatial Reference: {d.get('spatialReferenceName', '-')} (WKID {d.get('spatialReferenceWkid', '-')})",
            f"Layers: {d.get('layerCount', 0)}",
            f"Tables: {d.get('standaloneTableCount', 0)}",
        ]
        if d.get("scale"):
            lines.append(f"Scale: 1:{d['scale']:,.0f}")
        
        # Check for map image
        ap = self.state.arcgispro_path
        if ap:
            from arcgispro_cli.paths import get_images_folder
            img_folder = get_images_folder(ap)
            map_name = d.get('name', '').replace(' ', '_')
            img_path = img_folder / f"{map_name}.png"
            if img_path.exists():
                lines.append(f"\n[dim]Image: {img_path.name}[/dim]")
        
        # Show layer list
        layers = self.state.get_layers(map_name=d.get('name'))
        if layers:
            lines.append(f"\n[bold]Layers ({len(layers)}):[/bold]")
            for lyr in layers[:20]:  # Limit to first 20
                vis = "✓" if lyr.get("isVisible") else "✗"
                broken = " ⚠" if lyr.get("isBroken") else ""
                lines.append(f"  [{vis}] {lyr.get('name', '?')}{broken}")
            if len(layers) > 20:
                lines.append(f"  [dim]... and {len(layers) - 20} more[/dim]")
        
        self.update(Panel("\n".join(lines), title="Map", border_style="cyan"))

    def _show_layer(self, d: Dict[str, Any]) -> None:
        from rich.console import Group
        from rich.text import Text
        
        vis = "Yes" if d.get("isVisible") else "No"
        broken = " [red]⚠ BROKEN[/]" if d.get("isBroken") else ""
        
        info_lines = [
            f"[bold]{d.get('name', 'Unknown')}[/]{broken}",
            f"Map: {d.get('mapName', '-')}",
            f"Type: {d.get('layerType', '-')}",
            f"Geometry: {d.get('geometryType', '-') or '-'}",
            f"Visible: {vis}",
        ]
        if d.get("featureCount") is not None:
            info_lines.append(f"Features: {d['featureCount']:,}")
        if d.get("selectionCount"):
            info_lines.append(f"Selected: {d['selectionCount']:,}")
        if d.get("dataSourcePath"):
            info_lines.append(f"Source: [dim]{d['dataSourcePath']}[/dim]")
        if d.get("definitionQuery"):
            info_lines.append(f"Def Query: [dim]{d['definitionQuery']}[/dim]")
        if d.get("rendererType"):
            info_lines.append(f"Renderer: {d['rendererType']}")

        # Field schema table
        fields = d.get("fields") or []
        renderables = [Text.from_markup("\n".join(info_lines))]
        
        if fields:
            renderables.append(Text())
            renderables.append(Text.from_markup(f"[bold]Schema ({len(fields)} fields)[/bold]"))
            tbl = Table(box=None, show_header=True, header_style="bold cyan", padding=(0, 1), show_edge=False)
            tbl.add_column("Field", style="cyan", no_wrap=True)
            tbl.add_column("Type", style="yellow")
            tbl.add_column("Len", justify="right", style="dim")
            tbl.add_column("Null", justify="center", style="dim")
            tbl.add_column("Domain", style="dim")
            
            for f in fields[:30]:  # Limit to 30 fields
                tbl.add_row(
                    f.get("name", "-"),
                    f.get("fieldType", "-"),
                    str(f.get("length", "")) if f.get("length") else "-",
                    "✓" if f.get("isNullable") else "",
                    f.get("domainName", "") or "-",
                )
            if len(fields) > 30:
                renderables.append(tbl)
                renderables.append(Text.from_markup(f"[dim]... and {len(fields) - 30} more fields[/dim]"))
            else:
                renderables.append(tbl)
        
        self.update(Panel(Group(*renderables), title="Layer", border_style="cyan"))

    def _show_field(self, d: Dict[str, Any]) -> None:
        lines = [
            f"[bold]{d.get('name', 'Unknown')}[/]",
            f"Layer: {d.get('_layer', '-')}",
            f"Type: {d.get('fieldType', '-')}",
            f"Alias: {d.get('alias', '-')}",
            f"Length: {d.get('length', '-')}",
            f"Nullable: {'Yes' if d.get('isNullable') else 'No'}",
            f"Editable: {'Yes' if d.get('isEditable') else 'No'}",
        ]
        if d.get("domainName"):
            lines.append(f"Domain: {d['domainName']}")
        if d.get("defaultValue") is not None:
            lines.append(f"Default: {d['defaultValue']}")
        self.update(Panel("\n".join(lines), title="Field", border_style="cyan"))

    def _show_table(self, d: Dict[str, Any]) -> None:
        lines = [
            f"[bold]{d.get('name', 'Unknown')}[/]",
            f"Map: {d.get('mapName', '-')}",
            f"Source Type: {d.get('dataSourceType', '-')}",
        ]
        if d.get("rowCount") is not None:
            lines.append(f"Rows: {d['rowCount']:,}")
        self.update(Panel("\n".join(lines), title="Table", border_style="cyan"))

    def _show_connection(self, d: Dict[str, Any]) -> None:
        lines = [
            f"[bold]{d.get('name', 'Unknown')}[/]",
            f"Type: {d.get('connectionType', '-')}",
            f"Path: {d.get('path', '-')}",
        ]
        self.update(Panel("\n".join(lines), title="Connection", border_style="cyan"))

    def _show_generic(self, kind: str, d: Dict[str, Any]) -> None:
        lines = [f"{k}: {v}" for k, v in d.items() if k not in _SKIP_KEYS]
        self.update(Panel("\n".join(lines) or "(empty)", title=kind.title(), border_style="cyan"))
