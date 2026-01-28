"""clean command - Remove generated files."""

import click
import shutil
from rich.console import Console
from pathlib import Path

from ..paths import (
    find_arcgispro_folder,
    get_context_folder,
    get_images_folder,
    get_snapshot_folder,
)

console = Console()


@click.command("clean")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--images", "clean_images", is_flag=True, help="Remove images/ folder")
@click.option("--context", "clean_context", is_flag=True, help="Remove context/ folder")
@click.option("--snapshot", "clean_snapshot", is_flag=True, help="Remove snapshot/ folder")
@click.option("--all", "clean_all", is_flag=True, help="Remove everything in .arcgispro/")
@click.option("--yes", "-y", is_flag=True, help="Skip confirmation")
def clean_cmd(path, clean_images, clean_context, clean_snapshot, clean_all, yes):
    """Remove generated files from .arcgispro/ folder.
    
    Use flags to specify what to remove:
    
    \b
        --images    Remove images/ folder
        --context   Remove context/ folder  
        --snapshot  Remove snapshot/ folder
        --all       Remove everything
    
    By default, asks for confirmation before deleting.
    """
    start_path = Path(path) if path else None
    arcgispro_path = find_arcgispro_folder(start_path)
    
    if not arcgispro_path:
        console.print("[red]✗[/red] No .arcgispro folder found")
        raise SystemExit(1)
    
    # If no flags specified, show help
    if not any([clean_images, clean_context, clean_snapshot, clean_all]):
        console.print("[yellow]No cleanup option specified.[/yellow]")
        console.print()
        console.print("Use one of:")
        console.print("  --images    Remove images/ folder")
        console.print("  --context   Remove context/ folder")
        console.print("  --snapshot  Remove snapshot/ folder")
        console.print("  --all       Remove everything in .arcgispro/")
        raise SystemExit(1)
    
    to_remove = []
    
    if clean_all:
        # Remove everything
        for item in arcgispro_path.iterdir():
            to_remove.append(item)
    else:
        if clean_images:
            images_folder = get_images_folder(arcgispro_path)
            if images_folder.exists():
                to_remove.append(images_folder)
        
        if clean_context:
            context_folder = get_context_folder(arcgispro_path)
            if context_folder.exists():
                to_remove.append(context_folder)
            # Also remove meta.json and active_project.txt
            meta_file = arcgispro_path / "meta.json"
            if meta_file.exists():
                to_remove.append(meta_file)
            active_file = arcgispro_path / "active_project.txt"
            if active_file.exists():
                to_remove.append(active_file)
        
        if clean_snapshot:
            snapshot_folder = get_snapshot_folder(arcgispro_path)
            if snapshot_folder.exists():
                to_remove.append(snapshot_folder)
    
    if not to_remove:
        console.print("[dim]Nothing to remove.[/dim]")
        return
    
    # Show what will be removed
    console.print("[bold]Will remove:[/bold]")
    for item in to_remove:
        if item.is_dir():
            count = sum(1 for _ in item.rglob("*") if _.is_file())
            console.print(f"  📁 {item.name}/ ({count} files)")
        else:
            console.print(f"  📄 {item.name}")
    
    # Confirm
    if not yes:
        console.print()
        if not click.confirm("Proceed with deletion?"):
            console.print("[dim]Cancelled.[/dim]")
            return
    
    # Delete
    removed = 0
    for item in to_remove:
        try:
            if item.is_dir():
                shutil.rmtree(item)
            else:
                item.unlink()
            removed += 1
            console.print(f"[green]✓[/green] Removed {item.name}")
        except Exception as e:
            console.print(f"[red]✗[/red] Failed to remove {item.name}: {e}")
    
    console.print()
    console.print(f"[bold]Removed {removed} item(s)[/bold]")
