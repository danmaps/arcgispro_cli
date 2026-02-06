"""diagram command - Render project structure diagrams."""

import shutil
import subprocess
from pathlib import Path

import click
from rich.console import Console

from ..paths import find_arcgispro_folder, get_snapshot_folder

console = Console()


@click.command("diagram")
@click.option("--path", "-p", type=click.Path(exists=True), help="Path to search for .arcgispro folder")
@click.option(
    "--render/--no-render",
    default=True,
    help="Render images with beautiful-mermaid if available",
)
@click.option(
    "--format",
    "format_",
    type=click.Choice(["svg", "png", "both"], case_sensitive=False),
    default="svg",
    show_default=True,
    help="Image format to render",
)
@click.option(
    "--renderer",
    type=click.Path(),
    help="Path to beautiful-mermaid executable (defaults to PATH lookup)",
)
def diagram_cmd(path, render, format_, renderer):
    """Render Mermaid diagrams for the exported ArcGIS Pro project structure."""
    start_path = Path(path) if path else None
    arcgispro_path = find_arcgispro_folder(start_path)

    if not arcgispro_path:
        console.print("[red]✗[/red] No .arcgispro folder found")
        console.print("  Run the Snapshot export from ArcGIS Pro first.")
        raise SystemExit(1)

    snapshot_folder = get_snapshot_folder(arcgispro_path)
    mermaid_path = snapshot_folder / "project-structure.mmd"

    if not mermaid_path.exists():
        console.print("[red]✗[/red] Mermaid diagram source not found")
        console.print("  Re-run Snapshot export from ArcGIS Pro to generate it.")
        raise SystemExit(1)

    console.print(f"[green]✓[/green] Mermaid source: {mermaid_path}")

    if not render:
        return

    renderer_path = Path(renderer) if renderer else None
    if renderer_path:
        executable = renderer_path
    else:
        executable = shutil.which("beautiful-mermaid")
        executable = Path(executable) if executable else None

    if not executable:
        console.print("[yellow]⚠[/yellow] beautiful-mermaid not found in PATH")
        console.print("  Install it to render images from Mermaid code.")
        return

    formats = [format_.lower()] if format_.lower() != "both" else ["svg", "png"]
    outputs = []

    for fmt in formats:
        output_path = snapshot_folder / f"project-structure.{fmt}"
        cmd = [
            str(executable),
            "--input",
            str(mermaid_path),
            "--output",
            str(output_path),
            "--format",
            fmt,
        ]

        try:
            subprocess.run(cmd, check=True)
            outputs.append(output_path)
        except subprocess.CalledProcessError:
            console.print(f"[red]✗[/red] Failed to render {fmt} using beautiful-mermaid")
            raise SystemExit(1)

    for output_path in outputs:
        console.print(f"[green]✓[/green] Rendered {output_path}")
