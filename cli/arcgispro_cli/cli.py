"""
ArcGIS Pro CLI - Main entry point

Commands:
    # Setup
    arcgis install       - Install the ProExporter add-in
    arcgis uninstall     - Show uninstall instructions
    arcgis status        - Show export status and validate files
    arcgis clean         - Remove generated files
    arcgis open          - Open folder or select project
    arcgis launch        - Launch ArcGIS Pro
    
    # Query
    arcgis project       - Show project info
    arcgis maps          - List all maps
    arcgis map [name]    - Show map details
    arcgis layers        - List all layers
    arcgis layer <name>  - Show layer details + fields
    arcgis fields <name> - Show field schema for a layer
    arcgis tables        - List standalone tables
    arcgis connections   - List data connections
    arcgis notebooks     - List Jupyter notebooks
    arcgis context       - Print full markdown summary
    arcgis diagram       - Render project structure diagram
"""

import sys
import io

import click
from rich.console import Console

from . import __version__
from .commands import clean, open_project, install, query, launch, notebooks, tui, diagram
from .tui.banner import _colorize_logo

# Ensure Unicode output on Windows
if sys.stdout.encoding != "utf-8":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

console = Console()


@click.group(invoke_without_command=True)
@click.version_option(version=__version__, prog_name="arcgis")
@click.pass_context
def main(ctx):
    """ArcGIS Pro CLI - Query exported session context.
    
    This tool reads exports from the .arcgispro/ folder created by the
    ProExporter add-in. Use it to query project info, layers, fields,
    and more.
    
    \b
    Quick start:
        pip install arcgispro-cli
        arcgis install               # Install add-in (one time)
        # In ArcGIS Pro: Click "Snapshot" in the CLI tab
        arcgis layers                # List layers
        arcgis layer "Parcels"       # Get layer details
        arcgis fields "Parcels"      # Get field schema
    """
    ctx.ensure_object(dict)
    if ctx.invoked_subcommand is None:
        console.print(_colorize_logo())
        console.print()
        console.print(ctx.get_help())


# Setup commands
main.add_command(install.install_cmd, name="install")
main.add_command(install.uninstall_cmd, name="uninstall")
main.add_command(query.status_cmd, name="status")
main.add_command(clean.clean_cmd, name="clean")
main.add_command(open_project.open_cmd, name="open")
main.add_command(launch.launch_cmd, name="launch")

# Query commands
main.add_command(query.project_cmd, name="project")
main.add_command(query.maps_cmd, name="maps")
main.add_command(query.map_cmd, name="map")
main.add_command(query.layers_cmd, name="layers")
main.add_command(query.layer_cmd, name="layer")
main.add_command(query.fields_cmd, name="fields")
main.add_command(query.tables_cmd, name="tables")
main.add_command(query.connections_cmd, name="connections")
main.add_command(notebooks.notebooks_cmd, name="notebooks")
main.add_command(query.context_cmd, name="context")
main.add_command(diagram.diagram_cmd, name="diagram")
main.add_command(tui.tui_cmd, name="tui")


if __name__ == "__main__":
    main()
