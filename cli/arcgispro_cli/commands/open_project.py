"""open command - Select active project."""

import click
from rich.console import Console
from pathlib import Path

from ..paths import find_arcgispro_folder, find_aprx_files

console = Console()


@click.command("open")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .aprx files")
@click.argument("project", required=False)
def open_cmd(path, project):
    """Select the active ArcGIS Pro project.
    
    If PROJECT is not specified, searches for .aprx files in the
    current directory and lets you choose.
    
    The selected project path is written to .arcgispro/active_project.txt
    
    \b
    Examples:
        arcgispro open                    # Search and select
        arcgispro open MyProject.aprx     # Select specific project
    """
    search_path = Path(path) if path else Path.cwd()
    
    # If project specified directly, use it
    if project:
        project_path = Path(project)
        if not project_path.exists():
            # Try relative to search path
            project_path = search_path / project
        
        if not project_path.exists():
            console.print(f"[red]✗[/red] Project not found: {project}")
            raise SystemExit(1)
        
        if not project_path.suffix.lower() == ".aprx":
            console.print(f"[red]✗[/red] Not an ArcGIS Pro project file: {project}")
            raise SystemExit(1)
        
        _save_active_project(search_path, project_path)
        return
    
    # Search for .aprx files
    console.print(f"[bold]Searching for .aprx files in:[/bold] {search_path}")
    console.print()
    
    aprx_files = find_aprx_files(search_path)
    
    if not aprx_files:
        console.print("[yellow]No .aprx files found[/yellow]")
        console.print("  Navigate to a folder containing an ArcGIS Pro project.")
        raise SystemExit(1)
    
    # Show found projects
    console.print(f"[bold]Found {len(aprx_files)} project(s):[/bold]")
    console.print()
    
    for i, aprx in enumerate(aprx_files, 1):
        relative = aprx.relative_to(search_path) if aprx.is_relative_to(search_path) else aprx
        console.print(f"  {i}. {relative}")
    
    console.print()
    
    # Let user choose
    if len(aprx_files) == 1:
        choice = 1
        console.print(f"[dim]Auto-selecting the only project found.[/dim]")
    else:
        choice = click.prompt(
            "Select project",
            type=click.IntRange(1, len(aprx_files)),
            default=1
        )
    
    selected = aprx_files[choice - 1]
    _save_active_project(search_path, selected)


def _save_active_project(base_path: Path, project_path: Path):
    """Save the selected project to .arcgispro/active_project.txt"""
    
    # Create .arcgispro folder if needed
    arcgispro_folder = base_path / ".arcgispro"
    arcgispro_folder.mkdir(exist_ok=True)
    
    # Write active project
    active_file = arcgispro_folder / "active_project.txt"
    active_file.write_text(str(project_path.resolve()), encoding="utf-8")
    
    console.print()
    console.print(f"[green]✓[/green] Active project set to: [cyan]{project_path.name}[/cyan]")
    console.print(f"  Path: {project_path.resolve()}")
    console.print()
    console.print("[dim]Open this project in ArcGIS Pro and run Snapshot to export context.[/dim]")
