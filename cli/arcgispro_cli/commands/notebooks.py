"""Notebooks command - List Jupyter notebooks in the project."""

import click
from rich.console import Console
from rich.table import Table
from rich.panel import Panel
from rich import box
from pathlib import Path

from ..paths import find_arcgispro_folder, get_context_folder, load_json_file

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


@click.command("notebooks")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--json", "as_json", is_flag=True, help="Output as JSON")
def notebooks_cmd(path, as_json):
    """List Jupyter notebooks in the project."""
    import json as json_lib
    
    arcgispro_path = require_context(path)
    context_folder = get_context_folder(arcgispro_path)
    notebooks_file = context_folder / "notebooks.json"
    
    if not notebooks_file.exists():
        console.print("[yellow]No notebooks.json found[/yellow]")
        console.print("  Re-run Snapshot in ArcGIS Pro to export notebook info.")
        raise SystemExit(1)
    
    notebooks = load_json_file(notebooks_file)
    
    if not notebooks:
        console.print("[dim]No notebooks found in project[/dim]")
        return
    
    if as_json:
        console.print(json_lib.dumps(notebooks, indent=2))
        return
    
    console.print()
    console.print(f"[bold]Notebooks[/bold] ({len(notebooks)} found)")
    console.print()
    
    for nb in notebooks:
        name = nb.get("name", "Unknown")
        cell_count = nb.get("cellCount", 0)
        breakdown = nb.get("cellBreakdown", {})
        description = nb.get("description", "")
        
        # Format cell breakdown
        breakdown_str = ", ".join(f"{v} {k}" for k, v in breakdown.items())
        
        console.print(f"[cyan]{name}[/cyan]")
        console.print(f"  Cells: {cell_count} ({breakdown_str})")
        console.print(f"  Path: [dim]{nb.get('path', '-')}[/dim]")
        
        if description:
            # Show first line or truncate
            first_line = description.split('\n')[0].strip()
            if len(first_line) > 80:
                first_line = first_line[:77] + "..."
            console.print(f"  Description: {first_line}")
        
        console.print()
