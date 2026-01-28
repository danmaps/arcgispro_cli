"""install command - Install the ArcGIS Pro add-in."""

import os
import shutil
import tempfile
from pathlib import Path

import click
from rich.console import Console

console = Console()


def get_addin_path() -> Path:
    """Get the path to the bundled .addin file."""
    return Path(__file__).parent.parent / "addin" / "ProExporter.addin"


@click.command("install")
def install_cmd():
    """Install the ProExporter add-in for ArcGIS Pro.
    
    Extracts the bundled add-in and launches the installer.
    You'll see the ArcGIS Pro Add-In Installation Utility dialog.
    Click "Install Add-In" to complete installation.
    """
    addin_source = get_addin_path()
    
    if not addin_source.exists():
        console.print("[red]✗[/red] Add-in file not found in package.")
        console.print(f"  Expected: {addin_source}")
        raise SystemExit(1)
    
    # Copy to temp folder (some systems have issues launching from site-packages)
    temp_dir = Path(tempfile.gettempdir()) / "arcgispro_cli"
    temp_dir.mkdir(exist_ok=True)
    
    addin_dest = temp_dir / "ProExporter.esriAddinX"
    shutil.copy2(addin_source, addin_dest)
    
    console.print("[bold]Installing ProExporter add-in...[/bold]")
    console.print()
    console.print(f"  Add-in: {addin_dest}")
    console.print()
    
    # Launch the installer (Windows shell "open" action)
    try:
        os.startfile(str(addin_dest))
        console.print("[green]✓[/green] Add-in installer launched!")
        console.print()
        console.print("  [dim]Click 'Install Add-In' in the dialog that appeared.[/dim]")
        console.print("  [dim]Then restart ArcGIS Pro to use ProExporter.[/dim]")
    except OSError as e:
        console.print(f"[red]✗[/red] Failed to launch installer: {e}")
        console.print()
        console.print("  Try double-clicking the file manually:")
        console.print(f"  {addin_dest}")
        raise SystemExit(1)


@click.command("uninstall")
def uninstall_cmd():
    """Show instructions for uninstalling the add-in.
    
    ArcGIS Pro add-ins must be removed through ArcGIS Pro settings.
    """
    console.print("[bold]Uninstalling ProExporter add-in[/bold]")
    console.print()
    console.print("  Add-ins are managed in ArcGIS Pro:")
    console.print()
    console.print("  1. Open ArcGIS Pro")
    console.print("  2. Go to [cyan]Project → Add-In Manager[/cyan]")
    console.print("  3. Find [cyan]ProExporter[/cyan] in the list")
    console.print("  4. Click [cyan]Delete this Add-In[/cyan]")
    console.print("  5. Restart ArcGIS Pro")
    console.print()
