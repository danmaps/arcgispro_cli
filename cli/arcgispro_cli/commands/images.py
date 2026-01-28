"""images command - Validate exported images."""

import click
from rich.console import Console
from pathlib import Path

from ..paths import find_arcgispro_folder, list_image_files, get_images_folder

console = Console()


@click.command("images")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
def images_cmd(path):
    """Validate that exported images exist.
    
    Checks for PNG files in the .arcgispro/images/ folder.
    
    Exit code 0 if images exist, 1 if none found.
    """
    start_path = Path(path) if path else None
    arcgispro_path = find_arcgispro_folder(start_path)
    
    if not arcgispro_path:
        console.print("[red]✗[/red] No .arcgispro folder found")
        raise SystemExit(1)
    
    images_folder = get_images_folder(arcgispro_path)
    
    console.print(f"[bold]Checking images in:[/bold] {images_folder}")
    console.print()
    
    if not images_folder.exists():
        console.print("[yellow]⚠[/yellow] images/ folder does not exist")
        console.print("  Run 'Export Images' or 'Snapshot' from ArcGIS Pro.")
        raise SystemExit(1)
    
    images = list_image_files(arcgispro_path)
    
    if not images:
        console.print("[yellow]⚠[/yellow] No PNG images found")
        console.print("  Make sure a map view is active when exporting.")
        raise SystemExit(1)
    
    # Categorize images
    map_images = [img for img in images if img.name.startswith("map_")]
    layout_images = [img for img in images if img.name.startswith("layout_")]
    other_images = [img for img in images if not img.name.startswith(("map_", "layout_"))]
    
    console.print("[bold]Map images:[/bold]")
    if map_images:
        for img in map_images:
            size_kb = img.stat().st_size / 1024
            console.print(f"  [green]✓[/green] {img.name} ({size_kb:.1f} KB)")
    else:
        console.print("  [dim]None[/dim]")
    
    console.print()
    console.print("[bold]Layout images:[/bold]")
    if layout_images:
        for img in layout_images:
            size_kb = img.stat().st_size / 1024
            console.print(f"  [green]✓[/green] {img.name} ({size_kb:.1f} KB)")
    else:
        console.print("  [dim]None[/dim]")
    
    if other_images:
        console.print()
        console.print("[bold]Other images:[/bold]")
        for img in other_images:
            size_kb = img.stat().st_size / 1024
            console.print(f"  [green]✓[/green] {img.name} ({size_kb:.1f} KB)")
    
    console.print()
    console.print(f"[bold]Total:[/bold] {len(images)} images found")
    console.print("[green]Image validation passed![/green]")
