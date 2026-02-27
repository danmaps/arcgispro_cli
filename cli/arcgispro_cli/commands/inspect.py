"""inspect command - Print human-readable summary of exports."""

import os
from datetime import datetime
from typing import Any, Dict, List

import click
from rich.console import Console
from rich.panel import Panel
from rich.table import Table
from rich import box

from ..paths import find_arcgispro_folder, load_context_files, list_image_files

console = Console()

SERVICE_LAYER_KEYWORDS = (
    "service",
    "feature service",
    "map service",
    "image service",
    "tile service",
    "scene service",
)
MAX_HINTS = 3


@click.command("inspect")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--no-suggestions", is_flag=True, help="Hide the next-steps hint block")
def inspect_cmd(path, no_suggestions):
    """Print a human-readable summary of the exported context.
    
    Shows project info, maps, layers, and export metadata in a
    formatted display.
    """
    from pathlib import Path
    
    start_path = Path(path) if path else None
    arcgispro_path = find_arcgispro_folder(start_path)
    
    if not arcgispro_path:
        console.print("[red]✗[/red] No .arcgispro folder found")
        console.print("  Run the Snapshot export from ArcGIS Pro first.")
        raise SystemExit(1)
    
    context = load_context_files(arcgispro_path)
    images = list_image_files(arcgispro_path)
    
    # Header
    console.print()
    console.print(Panel.fit(
        "[bold blue]ArcGIS Pro Session Context[/bold blue]",
        border_style="blue"
    ))
    
    # Meta info
    meta = context.get("meta")
    if meta:
        exported_at = meta.get("exportedAt", "Unknown")
        if isinstance(exported_at, str) and "T" in exported_at:
            try:
                dt = datetime.fromisoformat(exported_at.replace("Z", "+00:00"))
                exported_at = dt.strftime("%Y-%m-%d %H:%M:%S UTC")
            except ValueError:
                pass
        console.print(f"[dim]Exported: {exported_at}[/dim]")
        console.print(f"[dim]Location: {arcgispro_path}[/dim]")
    console.print()
    
    # Project info
    project = context.get("project")
    if project:
        console.print("[bold]📁 Project[/bold]")
        console.print(f"   Name: [cyan]{project.get('name', 'Unknown')}[/cyan]")
        if project.get("path"):
            console.print(f"   Path: {project.get('path')}")
        map_count = len(project.get("mapNames", []))
        layout_count = len(project.get("layoutNames", []))
        console.print(f"   Maps: {map_count} | Layouts: {layout_count}")
        console.print()
    else:
        console.print("[yellow]⚠ No project info found[/yellow]")
        console.print()
    
    # Maps
    maps = context.get("maps") or []
    if maps:
        console.print("[bold]🗺️  Maps[/bold]")
        for m in maps:
            active = " [green]★ Active[/green]" if m.get("isActiveMap") else ""
            console.print(f"   • {m.get('name', 'Unknown')}{active}")
            console.print(f"     Type: {m.get('mapType', '-')} | Layers: {m.get('layerCount', 0)} | Tables: {m.get('standaloneTableCount', 0)}")
            if m.get("scale"):
                console.print(f"     Scale: 1:{m.get('scale'):,.0f}")
        console.print()
    
    # Layers summary
    layers = context.get("layers") or []
    if layers:
        console.print("[bold]📊 Layers[/bold]")
        
        table = Table(box=box.SIMPLE, show_header=True, header_style="bold")
        table.add_column("Layer", style="cyan")
        table.add_column("Type")
        table.add_column("Geometry")
        table.add_column("Features", justify="right")
        table.add_column("Visible")
        
        for layer in layers[:15]:  # Limit to first 15
            visible = "✓" if layer.get("isVisible") else "✗"
            broken = " [red]⚠[/red]" if layer.get("isBroken") else ""
            feature_count = layer.get("featureCount")
            features = f"{feature_count:,}" if feature_count is not None else "-"
            
            table.add_row(
                f"{layer.get('name', 'Unknown')}{broken}",
                layer.get("layerType", "-"),
                layer.get("geometryType", "-"),
                features,
                visible
            )
        
        console.print(table)
        
        if len(layers) > 15:
            console.print(f"   [dim]...and {len(layers) - 15} more layers[/dim]")
        console.print()
    
    # Geoprocessing
    gp = context.get("geoprocessing") or {}
    if gp:
        console.print("[bold]🧰 Geoprocessing[/bold]")
        try:
            count = gp.get("count")
            history = gp.get("history") or []
            console.print(f"   Runs: {count if count is not None else len(history)}")
            # Show up to 10 newest entries (if present)
            if history:
                for h in history[:10]:
                    tool = h.get("displayName") or h.get("toolName") or "(unknown)"
                    started = h.get("startedAt") or "-"
                    ok = h.get("succeeded")
                    ok_txt = "✓" if ok is True else ("✗" if ok is False else "-")
                    console.print(f"   • {tool} | {started} | {ok_txt}")
        except Exception:
            console.print("   [dim](failed to parse geoprocessing.json)[/dim]")
        console.print()

    # Images
    if images:
        console.print("[bold]🖼️  Images[/bold]")
        for img in images:
            console.print(f"   • {img.name}")
        console.print()
    
    # Summary
    console.print("[bold]Summary[/bold]")
    console.print(f"   Context files: {sum(1 for v in context.values() if v is not None)}/7")
    console.print(f"   Images: {len(images)}")
    console.print()

    if _should_show_suggestions(no_suggestions):
        suggestions = _collect_next_steps(context)
        if suggestions:
            console.print()
            console.print(Panel.fit(
                "\n".join(suggestions),
                title="Next steps",
                border_style="magenta",
                subtitle="Use --no-suggestions or ARCGISPRO_CLI_NO_SUGGESTIONS=1 to hide these hints"
            ))


def _should_show_suggestions(no_suggestions: bool) -> bool:
    if no_suggestions:
        return False
    return not _env_flag_true("ARCGISPRO_CLI_NO_SUGGESTIONS")


def _env_flag_true(key: str) -> bool:
    value = os.getenv(key)
    if not value:
        return False
    return value.strip().lower() not in {"0", "false", "no", ""}


def _collect_next_steps(context: Dict[str, Any]) -> List[str]:
    hints = []

    if _has_broken_sources(context):
        hints.append("Tip: broken sources detected. Run `arcgispro layers --broken` and fix paths before scripting.")
    if _has_mixed_spatial_references(context):
        hints.append("Tip: mixed spatial references across maps. Double-check projections before automation.")
    if _has_service_layers(context):
        hints.append("Tip: service layers detected. Expect auth/latency flakiness; consider local copies for repeatable runs.")
    if _has_complex_relations(context):
        hints.append("Tip: multiple joins/relates detected. Sanity check relationship keys before automating edits.")

    return hints[:MAX_HINTS]


def _has_broken_sources(context: Dict[str, Any]) -> bool:
    layers = context.get("layers") or []
    tables = context.get("tables") or []
    return any(layer.get("isBroken") for layer in layers) or any(table.get("isBroken") for table in tables)


def _has_mixed_spatial_references(context: Dict[str, Any]) -> bool:
    wkids = {
        map_info.get("spatialReferenceWkid")
        for map_info in context.get("maps") or []
        if map_info.get("spatialReferenceWkid") is not None
    }
    return len(wkids) > 1


def _has_service_layers(context: Dict[str, Any]) -> bool:
    for layer in context.get("layers") or []:
        text = " ".join([
            layer.get("layerType", ""),
            layer.get("dataSourceType", ""),
            layer.get("dataSourcePath", "")
        ]).lower()
        if any(keyword in text for keyword in SERVICE_LAYER_KEYWORDS):
            return True
    return False


def _has_complex_relations(context: Dict[str, Any]) -> bool:
    total_relations = 0
    for layer in context.get("layers") or []:
        total_relations += len(layer.get("joinedTables") or [])
        total_relations += len(layer.get("relatedTables") or [])
        if total_relations >= 3:
            return True
    return False
