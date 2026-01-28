"""Query commands - Access exported context data."""

import click
from rich.console import Console
from rich.table import Table
from rich.panel import Panel
from rich import box
from pathlib import Path

from ..paths import find_arcgispro_folder, load_context_files, load_json_file, get_context_folder

console = Console()


def require_context(path=None):
    """Find .arcgispro folder or exit with error."""
    start_path = Path(path) if path else None
    arcgispro_path = find_arcgispro_folder(start_path)
    
    if not arcgispro_path:
        console.print("[red]✗[/red] No .arcgispro folder found")
        console.print("  Run the Snapshot export from ArcGIS Pro first.")
        raise SystemExit(1)
    
    return arcgispro_path


@click.command("project")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--json", "as_json", is_flag=True, help="Output as JSON")
def project_cmd(path, as_json):
    """Show project information."""
    import json as json_lib
    
    arcgispro_path = require_context(path)
    context = load_context_files(arcgispro_path)
    project = context.get("project")
    
    if not project:
        console.print("[yellow]No project info found[/yellow]")
        raise SystemExit(1)
    
    if as_json:
        console.print(json_lib.dumps(project, indent=2))
        return
    
    console.print()
    console.print(Panel.fit(f"[bold]{project.get('name', 'Unknown')}[/bold]", title="Project"))
    console.print(f"  Path: [dim]{project.get('path', '-')}[/dim]")
    console.print(f"  Default GDB: [dim]{project.get('defaultGeodatabase', '-')}[/dim]")
    console.print(f"  Maps: {len(project.get('mapNames', []))}")
    console.print(f"  Layouts: {len(project.get('layoutNames', []))}")
    console.print()


@click.command("maps")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--json", "as_json", is_flag=True, help="Output as JSON")
def maps_cmd(path, as_json):
    """List all maps in the project."""
    import json as json_lib
    
    arcgispro_path = require_context(path)
    context = load_context_files(arcgispro_path)
    maps = context.get("maps") or []
    
    if as_json:
        console.print(json_lib.dumps(maps, indent=2))
        return
    
    if not maps:
        console.print("[yellow]No maps found[/yellow]")
        return
    
    console.print()
    for m in maps:
        active = " [green]★ Active[/green]" if m.get("isActiveMap") else ""
        console.print(f"[bold]{m.get('name', 'Unknown')}[/bold]{active}")
        console.print(f"  Type: {m.get('mapType', '-')} | SR: {m.get('spatialReferenceName', '-')}")
        console.print(f"  Layers: {m.get('layerCount', 0)} | Tables: {m.get('standaloneTableCount', 0)}")
        if m.get("scale"):
            console.print(f"  Scale: 1:{m.get('scale'):,.0f}")
        console.print()


@click.command("map")
@click.argument("name", required=False)
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--json", "as_json", is_flag=True, help="Output as JSON")
def map_cmd(name, path, as_json):
    """Show details for a specific map. If no name given, shows the active map."""
    import json as json_lib
    
    arcgispro_path = require_context(path)
    context = load_context_files(arcgispro_path)
    maps = context.get("maps") or []
    
    if not maps:
        console.print("[yellow]No maps found[/yellow]")
        raise SystemExit(1)
    
    # Find the map
    if name:
        target = next((m for m in maps if m.get("name", "").lower() == name.lower()), None)
        if not target:
            console.print(f"[red]Map '{name}' not found[/red]")
            console.print("Available maps:")
            for m in maps:
                console.print(f"  • {m.get('name')}")
            raise SystemExit(1)
    else:
        target = next((m for m in maps if m.get("isActiveMap")), maps[0] if maps else None)
    
    if as_json:
        console.print(json_lib.dumps(target, indent=2))
        return
    
    # Show layers in this map
    layers = context.get("layers") or []
    map_layers = [l for l in layers if l.get("mapName") == target.get("name")]
    
    console.print()
    console.print(Panel.fit(f"[bold]{target.get('name', 'Unknown')}[/bold]", title="Map"))
    console.print(f"  Type: {target.get('mapType', '-')}")
    console.print(f"  Spatial Reference: {target.get('spatialReferenceName', '-')} (WKID: {target.get('spatialReferenceWkid', '-')})")
    if target.get("scale"):
        console.print(f"  Scale: 1:{target.get('scale'):,.0f}")
    console.print()
    
    if map_layers:
        console.print(f"[bold]Layers ({len(map_layers)}):[/bold]")
        for l in map_layers:
            visible = "✓" if l.get("isVisible") else "✗"
            broken = " [red]⚠ BROKEN[/red]" if l.get("isBroken") else ""
            console.print(f"  [{visible}] {l.get('name')}{broken}")
    console.print()


@click.command("layers")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--map", "-m", "map_name", help="Filter by map name")
@click.option("--broken", is_flag=True, help="Show only broken layers")
@click.option("--json", "as_json", is_flag=True, help="Output as JSON")
def layers_cmd(path, map_name, broken, as_json):
    """List all layers."""
    import json as json_lib
    
    arcgispro_path = require_context(path)
    context = load_context_files(arcgispro_path)
    layers = context.get("layers") or []
    
    # Apply filters
    if map_name:
        layers = [l for l in layers if l.get("mapName", "").lower() == map_name.lower()]
    if broken:
        layers = [l for l in layers if l.get("isBroken")]
    
    if as_json:
        console.print(json_lib.dumps(layers, indent=2))
        return
    
    if not layers:
        msg = "No layers found"
        if broken:
            msg = "No broken layers found"
        console.print(f"[yellow]{msg}[/yellow]")
        return
    
    table = Table(box=box.SIMPLE, show_header=True, header_style="bold")
    table.add_column("Layer", style="cyan")
    table.add_column("Map")
    table.add_column("Type")
    table.add_column("Geometry")
    table.add_column("Features", justify="right")
    table.add_column("V", justify="center")
    
    for layer in layers:
        visible = "✓" if layer.get("isVisible") else ""
        broken_mark = " ⚠" if layer.get("isBroken") else ""
        features = f"{layer.get('featureCount', '-'):,}" if layer.get('featureCount') else "-"
        
        table.add_row(
            f"{layer.get('name', 'Unknown')}{broken_mark}",
            layer.get("mapName", "-"),
            layer.get("layerType", "-"),
            layer.get("geometryType", "-") or "-",
            features,
            visible
        )
    
    console.print()
    console.print(table)
    console.print()


@click.command("layer")
@click.argument("name")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--json", "as_json", is_flag=True, help="Output as JSON")
def layer_cmd(name, path, as_json):
    """Show details for a specific layer, including field schema."""
    import json as json_lib
    
    arcgispro_path = require_context(path)
    context = load_context_files(arcgispro_path)
    layers = context.get("layers") or []
    
    # Find the layer (case-insensitive partial match)
    matches = [l for l in layers if name.lower() in l.get("name", "").lower()]
    
    if not matches:
        console.print(f"[red]Layer '{name}' not found[/red]")
        raise SystemExit(1)
    
    if len(matches) > 1:
        exact = [l for l in matches if l.get("name", "").lower() == name.lower()]
        if exact:
            matches = exact
        else:
            console.print(f"[yellow]Multiple layers match '{name}':[/yellow]")
            for l in matches:
                console.print(f"  • {l.get('name')} ({l.get('mapName')})")
            console.print("Be more specific.")
            raise SystemExit(1)
    
    layer = matches[0]
    
    if as_json:
        console.print(json_lib.dumps(layer, indent=2))
        return
    
    console.print()
    console.print(Panel.fit(f"[bold]{layer.get('name', 'Unknown')}[/bold]", title="Layer"))
    console.print(f"  Map: {layer.get('mapName', '-')}")
    console.print(f"  Type: {layer.get('layerType', '-')}")
    console.print(f"  Geometry: {layer.get('geometryType', '-') or '-'}")
    console.print(f"  Visible: {'Yes' if layer.get('isVisible') else 'No'}")
    console.print(f"  Editable: {'Yes' if layer.get('isEditable') else 'No'}")
    
    if layer.get("isBroken"):
        console.print(f"  [red]⚠ Data source is BROKEN[/red]")
    
    if layer.get("featureCount") is not None:
        console.print(f"  Features: {layer.get('featureCount'):,}")
    if layer.get("selectionCount"):
        console.print(f"  Selected: {layer.get('selectionCount'):,}")
    
    if layer.get("dataSourcePath"):
        console.print(f"  Source: [dim]{layer.get('dataSourcePath')}[/dim]")
    if layer.get("definitionQuery"):
        console.print(f"  Definition Query: {layer.get('definitionQuery')}")
    if layer.get("rendererType"):
        console.print(f"  Renderer: {layer.get('rendererType')}")
        if layer.get("rendererField"):
            console.print(f"  Renderer Field: {layer.get('rendererField')}")
    
    # Fields
    fields = layer.get("fields") or []
    if fields:
        console.print()
        console.print(f"[bold]Fields ({len(fields)}):[/bold]")
        
        table = Table(box=box.SIMPLE, show_header=True, header_style="bold")
        table.add_column("Name", style="cyan")
        table.add_column("Alias")
        table.add_column("Type")
        table.add_column("Length", justify="right")
        table.add_column("Null", justify="center")
        
        for field in fields:
            table.add_row(
                field.get("name", "-"),
                field.get("alias", "-") if field.get("alias") != field.get("name") else "-",
                field.get("fieldType", "-"),
                str(field.get("length", "-")) if field.get("length") else "-",
                "✓" if field.get("isNullable") else ""
            )
        
        console.print(table)
    console.print()


@click.command("fields")
@click.argument("layer_name")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--json", "as_json", is_flag=True, help="Output as JSON")
def fields_cmd(layer_name, path, as_json):
    """Show field schema for a layer."""
    import json as json_lib
    
    arcgispro_path = require_context(path)
    context = load_context_files(arcgispro_path)
    layers = context.get("layers") or []
    
    # Find the layer
    matches = [l for l in layers if layer_name.lower() in l.get("name", "").lower()]
    
    if not matches:
        console.print(f"[red]Layer '{layer_name}' not found[/red]")
        raise SystemExit(1)
    
    if len(matches) > 1:
        exact = [l for l in matches if l.get("name", "").lower() == layer_name.lower()]
        if exact:
            matches = exact
        else:
            console.print(f"[yellow]Multiple layers match '{layer_name}':[/yellow]")
            for l in matches:
                console.print(f"  • {l.get('name')}")
            raise SystemExit(1)
    
    layer = matches[0]
    fields = layer.get("fields") or []
    
    if as_json:
        console.print(json_lib.dumps(fields, indent=2))
        return
    
    if not fields:
        console.print(f"[yellow]No fields found for '{layer.get('name')}'[/yellow]")
        return
    
    console.print()
    console.print(f"[bold]Fields for {layer.get('name')}[/bold] ({len(fields)} fields)")
    console.print()
    
    table = Table(box=box.SIMPLE, show_header=True, header_style="bold")
    table.add_column("Name", style="cyan")
    table.add_column("Alias")
    table.add_column("Type")
    table.add_column("Length", justify="right")
    table.add_column("Nullable", justify="center")
    table.add_column("Editable", justify="center")
    table.add_column("Domain")
    
    for field in fields:
        table.add_row(
            field.get("name", "-"),
            field.get("alias", "-") if field.get("alias") != field.get("name") else "-",
            field.get("fieldType", "-"),
            str(field.get("length", "")) if field.get("length") else "-",
            "✓" if field.get("isNullable") else "",
            "✓" if field.get("isEditable") else "",
            field.get("domainName", "") or "-"
        )
    
    console.print(table)
    console.print()


@click.command("tables")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--json", "as_json", is_flag=True, help="Output as JSON")
def tables_cmd(path, as_json):
    """List standalone tables."""
    import json as json_lib
    
    arcgispro_path = require_context(path)
    context = load_context_files(arcgispro_path)
    tables = context.get("tables") or []
    
    if as_json:
        console.print(json_lib.dumps(tables, indent=2))
        return
    
    if not tables:
        console.print("[yellow]No standalone tables found[/yellow]")
        return
    
    console.print()
    table = Table(box=box.SIMPLE, show_header=True, header_style="bold")
    table.add_column("Table", style="cyan")
    table.add_column("Map")
    table.add_column("Rows", justify="right")
    table.add_column("Source Type")
    
    for t in tables:
        rows = f"{t.get('rowCount', '-'):,}" if t.get('rowCount') else "-"
        table.add_row(
            t.get("name", "Unknown"),
            t.get("mapName", "-"),
            rows,
            t.get("dataSourceType", "-")
        )
    
    console.print(table)
    console.print()


@click.command("connections")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--json", "as_json", is_flag=True, help="Output as JSON")
def connections_cmd(path, as_json):
    """List data connections (geodatabases, folders)."""
    import json as json_lib
    
    arcgispro_path = require_context(path)
    context = load_context_files(arcgispro_path)
    connections = context.get("connections") or []
    
    if as_json:
        console.print(json_lib.dumps(connections, indent=2))
        return
    
    if not connections:
        console.print("[yellow]No connections found[/yellow]")
        return
    
    console.print()
    table = Table(box=box.SIMPLE, show_header=True, header_style="bold")
    table.add_column("Name", style="cyan")
    table.add_column("Type")
    table.add_column("Path")
    
    for conn in connections:
        table.add_row(
            conn.get("name", "Unknown"),
            conn.get("connectionType", "-"),
            conn.get("path", "-")
        )
    
    console.print(table)
    console.print()


@click.command("context")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
def context_cmd(path):
    """Print the full markdown context summary (for pasting to AI)."""
    arcgispro_path = require_context(path)
    
    context_md = arcgispro_path / "snapshot" / "context.md"
    if not context_md.exists():
        console.print("[yellow]No context.md found. Run Snapshot in ArcGIS Pro first.[/yellow]")
        raise SystemExit(1)
    
    content = context_md.read_text(encoding="utf-8-sig")
    console.print(content)


@click.command("status")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
def status_cmd(path):
    """Show export status and validate files."""
    from datetime import datetime
    from ..paths import list_image_files
    
    arcgispro_path = require_context(path)
    context = load_context_files(arcgispro_path)
    images = list_image_files(arcgispro_path)
    
    console.print()
    console.print(f"[bold]Export Status[/bold]")
    console.print(f"  Location: {arcgispro_path}")
    
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
        console.print(f"  Exported: {exported_at}")
    console.print()
    
    # File validation
    console.print("[bold]Files:[/bold]")
    files = [
        ("meta.json", context.get("meta")),
        ("project.json", context.get("project")),
        ("maps.json", context.get("maps")),
        ("layers.json", context.get("layers")),
        ("tables.json", context.get("tables")),
        ("connections.json", context.get("connections")),
        ("layouts.json", context.get("layouts")),
    ]
    
    valid = 0
    for name, data in files:
        if data is not None:
            if isinstance(data, list):
                info = f"{len(data)} items"
            elif isinstance(data, dict):
                info = "valid"
            else:
                info = "valid"
            console.print(f"  [green]✓[/green] {name} ({info})")
            valid += 1
        else:
            console.print(f"  [red]✗[/red] {name} (missing)")
    
    console.print()
    console.print(f"[bold]Images:[/bold] {len(images)}")
    for img in images:
        console.print(f"  • {img.name}")
    
    console.print()
    console.print(f"[bold]Summary:[/bold] {valid}/7 context files, {len(images)} images")
    console.print()
