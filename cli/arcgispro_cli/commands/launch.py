"""launch command - Launch ArcGIS Pro."""

import os
import subprocess
from pathlib import Path

import click
from rich.console import Console

console = Console()

# Common ArcGIS Pro install locations
PRO_PATHS = [
    Path(r"C:\Program Files\ArcGIS\Pro\bin\ArcGISPro.exe"),
    Path(os.environ.get("PROGRAMFILES", "")) / "ArcGIS" / "Pro" / "bin" / "ArcGISPro.exe",
]


def find_arcgis_pro() -> Path | None:
    """Find ArcGISPro.exe."""
    for path in PRO_PATHS:
        if path.exists():
            return path
    return None


def find_aprx_in_dir(directory: Path) -> Path | None:
    """Find a .aprx file in the given directory."""
    aprx_files = list(directory.glob("*.aprx"))
    if len(aprx_files) == 1:
        return aprx_files[0]
    elif len(aprx_files) > 1:
        # Multiple projects - return None, let user specify
        return None
    return None


@click.command("launch")
@click.argument("project", required=False, type=click.Path(exists=True))
@click.option("--new", is_flag=True, help="Start with a new blank project")
def launch_cmd(project, new):
    """Launch ArcGIS Pro.
    
    \b
    Examples:
        arcgispro launch              # Open project in current dir, or just launch Pro
        arcgispro launch MyMap.aprx   # Open specific project
        arcgispro launch --new        # Start with blank project
    
    If no project is specified and there's exactly one .aprx in the current
    directory, it will be opened automatically.
    """
    pro_exe = find_arcgis_pro()
    
    if not pro_exe:
        console.print("[red]✗[/red] ArcGIS Pro not found")
        console.print("  Expected at: C:\\Program Files\\ArcGIS\\Pro\\bin\\ArcGISPro.exe")
        raise SystemExit(1)
    
    args = [str(pro_exe)]
    
    if new:
        # Just launch Pro, it will create a new project
        console.print("[dim]Launching ArcGIS Pro (new project)...[/dim]")
    elif project:
        # Open specified project
        project_path = Path(project).resolve()
        if project_path.suffix.lower() != ".aprx":
            console.print(f"[red]✗[/red] Not a project file: {project}")
            raise SystemExit(1)
        args.append(str(project_path))
        console.print(f"[dim]Opening {project_path.name}...[/dim]")
    else:
        # Try to find a project in current directory
        aprx = find_aprx_in_dir(Path.cwd())
        if aprx:
            args.append(str(aprx))
            console.print(f"[dim]Opening {aprx.name}...[/dim]")
        else:
            aprx_files = list(Path.cwd().glob("*.aprx"))
            if len(aprx_files) > 1:
                console.print("[yellow]Multiple .aprx files found:[/yellow]")
                for f in aprx_files:
                    console.print(f"  • {f.name}")
                console.print("Specify one: [cyan]arcgispro launch MyProject.aprx[/cyan]")
                console.print("[dim]Launching ArcGIS Pro without a project...[/dim]")
            else:
                console.print("[dim]Launching ArcGIS Pro...[/dim]")
    
    # Launch Pro (detached so CLI exits immediately)
    subprocess.Popen(args, start_new_session=True)
