"""
ArcGIS Pro CLI - Main entry point

Commands:
    # Setup
    arcgispro install       - Install the ProExporter add-in
    arcgispro uninstall     - Show uninstall instructions
    arcgispro status        - Show export status and validate files
    arcgispro clean         - Remove generated files
    arcgispro open          - Open folder or select project
    arcgispro launch        - Launch ArcGIS Pro
    
    # Query
    arcgispro project       - Show project info
    arcgispro maps          - List all maps
    arcgispro map [name]    - Show map details
    arcgispro layers        - List all layers
    arcgispro layer <name>  - Show layer details + fields
    arcgispro fields <name> - Show field schema for a layer
    arcgispro tables        - List standalone tables
    arcgispro connections   - List data connections
    arcgispro context       - Print full markdown summary
"""

import click
from rich.console import Console

from . import __version__
from .commands import clean, open_project, install, query, launch

console = Console()


@click.group()
@click.version_option(version=__version__, prog_name="arcgispro")
@click.pass_context
def main(ctx):
    """ArcGIS Pro CLI - Query exported session context.
    
    This tool reads exports from the .arcgispro/ folder created by the
    ProExporter add-in. Use it to query project info, layers, fields,
    and more.
    
    \b
    Quick start:
        pip install arcgispro-cli
        arcgispro install            # Install add-in (one time)
        # In ArcGIS Pro: Click "Snapshot" in the CLI tab
        arcgispro layers             # List layers
        arcgispro layer "Parcels"    # Get layer details
        arcgispro fields "Parcels"   # Get field schema
    """
    ctx.ensure_object(dict)


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
main.add_command(query.context_cmd, name="context")


if __name__ == "__main__":
    main()
