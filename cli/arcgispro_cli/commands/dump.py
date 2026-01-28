"""dump command - Validate context JSON files."""

import click
from rich.console import Console
from pathlib import Path

from ..paths import find_arcgispro_folder, get_context_folder, load_json_file

console = Console()

EXPECTED_FILES = [
    ("meta.json", False),  # (filename, is_in_context_folder)
    ("project.json", True),
    ("maps.json", True),
    ("layers.json", True),
    ("tables.json", True),
    ("connections.json", True),
    ("layouts.json", True),
]


@click.command("dump")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--verbose", "-v", is_flag=True, help="Show file contents")
def dump_cmd(path, verbose):
    """Validate that context JSON files exist and are valid.
    
    Checks for expected JSON files in the .arcgispro/context/ folder
    and validates they contain valid JSON.
    
    Exit code 0 if all files valid, 1 if any issues found.
    """
    start_path = Path(path) if path else None
    arcgispro_path = find_arcgispro_folder(start_path)
    
    if not arcgispro_path:
        console.print("[red]✗[/red] No .arcgispro folder found")
        raise SystemExit(1)
    
    context_folder = get_context_folder(arcgispro_path)
    
    console.print(f"[bold]Validating context files in:[/bold] {arcgispro_path}")
    console.print()
    
    all_valid = True
    valid_count = 0
    
    for filename, in_context in EXPECTED_FILES:
        if in_context:
            file_path = context_folder / filename
        else:
            file_path = arcgispro_path / filename
        
        if not file_path.exists():
            console.print(f"[yellow]⚠[/yellow] {filename}: [yellow]missing[/yellow]")
            all_valid = False
            continue
        
        data = load_json_file(file_path)
        if data is None:
            console.print(f"[red]✗[/red] {filename}: [red]invalid JSON[/red]")
            all_valid = False
            continue
        
        # Get some stats about the data
        if isinstance(data, list):
            info = f"{len(data)} items"
        elif isinstance(data, dict):
            info = f"{len(data)} keys"
        else:
            info = "valid"
        
        console.print(f"[green]✓[/green] {filename}: {info}")
        valid_count += 1
        
        if verbose and isinstance(data, dict):
            for key in list(data.keys())[:5]:
                console.print(f"    {key}: {type(data[key]).__name__}")
    
    console.print()
    console.print(f"[bold]Result:[/bold] {valid_count}/{len(EXPECTED_FILES)} files valid")
    
    if not all_valid:
        console.print("[yellow]Some files are missing or invalid. Re-run export from ArcGIS Pro.[/yellow]")
        raise SystemExit(1)
    
    console.print("[green]All context files valid![/green]")
