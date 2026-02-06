"""snapshot command - Assemble full snapshot."""

import click
import shutil
from rich.console import Console
from pathlib import Path

from ..paths import (
    find_arcgispro_folder,
    load_context_files,
    list_image_files,
    get_snapshot_folder,
    get_images_folder,
)

console = Console()


@click.command("snapshot")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option("--force", "-f", is_flag=True, help="Overwrite existing snapshot")
def snapshot_cmd(path, force):
    """Assemble a complete snapshot from context and images.
    
    Verifies that context JSON and images exist, then assembles
    everything into the snapshot/ folder for AI consumption.
    
    The snapshot includes:
    - context.md: Human-readable summary
    - CONTEXT_SKILL.md: How to use the exports
    - AGENT_TOOL_SKILL.md: CLI usage guide  
    - images/: Copy of exported images
    """
    start_path = Path(path) if path else None
    arcgispro_path = find_arcgispro_folder(start_path)
    
    if not arcgispro_path:
        console.print("[red]✗[/red] No .arcgispro folder found")
        console.print("  Run the Snapshot export from ArcGIS Pro first.")
        raise SystemExit(1)
    
    console.print(f"[bold]Assembling snapshot from:[/bold] {arcgispro_path}")
    console.print()
    
    # Verify context exists
    context = load_context_files(arcgispro_path)
    missing_context = [k for k, v in context.items() if v is None]
    
    if missing_context:
        console.print(f"[yellow]⚠[/yellow] Missing context files: {', '.join(missing_context)}")
        console.print("  Run 'Dump Context' or 'Snapshot' from ArcGIS Pro.")
    
    # Check for images
    images = list_image_files(arcgispro_path)
    if not images:
        console.print("[yellow]⚠[/yellow] No images found")
        console.print("  Run 'Export Images' or 'Snapshot' from ArcGIS Pro.")
    
    # Check if snapshot already exists
    snapshot_folder = get_snapshot_folder(arcgispro_path)
    
    if snapshot_folder.exists():
        existing_files = list(snapshot_folder.iterdir())
        if existing_files and not force:
            console.print(f"[yellow]⚠[/yellow] Snapshot folder already contains {len(existing_files)} items")
            console.print("  Use --force to overwrite, or delete snapshot/ manually.")
            raise SystemExit(1)
    
    # Check if the add-in already created the snapshot files
    context_md = snapshot_folder / "context.md"
    context_skill = snapshot_folder / "CONTEXT_SKILL.md"
    agent_skill = snapshot_folder / "AGENT_TOOL_SKILL.md"
    diagram_source = snapshot_folder / "project-structure.mmd"
    diagram_md = snapshot_folder / "project-structure.md"
    
    files_created = 0
    
    if context_md.exists():
        console.print(f"[green]✓[/green] context.md exists")
        files_created += 1
    else:
        console.print(f"[yellow]⚠[/yellow] context.md missing (should be created by add-in)")
    
    if context_skill.exists():
        console.print(f"[green]✓[/green] CONTEXT_SKILL.md exists")
        files_created += 1
    else:
        console.print(f"[yellow]⚠[/yellow] CONTEXT_SKILL.md missing")
    
    if agent_skill.exists():
        console.print(f"[green]✓[/green] AGENT_TOOL_SKILL.md exists")
        files_created += 1
    else:
        console.print(f"[yellow]⚠[/yellow] AGENT_TOOL_SKILL.md missing")

    if diagram_source.exists():
        console.print(f"[green]✓[/green] project-structure.mmd exists")
    else:
        console.print(f"[yellow]⚠[/yellow] project-structure.mmd missing")

    if diagram_md.exists():
        console.print(f"[green]✓[/green] project-structure.md exists")
    else:
        console.print(f"[yellow]⚠[/yellow] project-structure.md missing")
    
    # Copy images to snapshot folder
    snapshot_images_folder = snapshot_folder / "images"
    
    if images:
        snapshot_images_folder.mkdir(parents=True, exist_ok=True)
        
        copied = 0
        for img in images:
            dest = snapshot_images_folder / img.name
            if not dest.exists() or force:
                shutil.copy2(img, dest)
                copied += 1
        
        console.print(f"[green]✓[/green] Copied {copied} images to snapshot/images/")
    
    console.print()
    
    # Summary
    if files_created >= 2 and images:
        console.print("[green]✓ Snapshot is ready![/green]")
        console.print()
        console.print("[bold]Contents:[/bold]")
        console.print(f"  {snapshot_folder}/")
        console.print(f"    context.md         - Human-readable summary")
        console.print(f"    project-structure.mmd - Mermaid diagram source")
        console.print(f"    project-structure.md  - Mermaid diagram markdown")
        console.print(f"    CONTEXT_SKILL.md   - How to use exports")
        console.print(f"    AGENT_TOOL_SKILL.md - CLI usage")
        console.print(f"    images/            - {len(images)} PNG files")
    else:
        console.print("[yellow]⚠ Snapshot incomplete[/yellow]")
        console.print("  Run 'Snapshot' from ArcGIS Pro to generate all files.")
        raise SystemExit(1)
