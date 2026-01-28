"""
ArcGIS Pro CLI - Main entry point

Commands:
    arcgispro install    - Install the ProExporter add-in
    arcgispro uninstall  - Show uninstall instructions
    arcgispro inspect    - Print human-readable summary of exports
    arcgispro dump       - Validate context JSON files
    arcgispro images     - Validate exported images
    arcgispro snapshot   - Assemble full snapshot
    arcgispro clean      - Remove generated files
    arcgispro open       - Select active project
"""

import click
from rich.console import Console

from . import __version__
from .commands import inspect, dump, images, snapshot, clean, open_project, install

console = Console()


@click.group()
@click.version_option(version=__version__, prog_name="arcgispro")
@click.pass_context
def main(ctx):
    """ArcGIS Pro CLI - Inspect and manage session exports.
    
    This tool reads exports from the .arcgispro/ folder created by the
    ProExporter add-in. Use it to validate exports, view summaries,
    and assemble snapshots for AI agents.
    
    \b
    Quick start:
        pip install arcgispro-cli
        arcgispro install        # Install add-in (one time)
        # In ArcGIS Pro: Click "Snapshot" button
        arcgispro inspect        # View exported context
    """
    ctx.ensure_object(dict)


# Register commands
main.add_command(install.install_cmd, name="install")
main.add_command(install.uninstall_cmd, name="uninstall")
main.add_command(inspect.inspect_cmd, name="inspect")
main.add_command(dump.dump_cmd, name="dump")
main.add_command(images.images_cmd, name="images")
main.add_command(snapshot.snapshot_cmd, name="snapshot")
main.add_command(clean.clean_cmd, name="clean")
main.add_command(open_project.open_cmd, name="open")


if __name__ == "__main__":
    main()
