import click


@click.group(help="Launch the interactive Textual UI")
def tui():
    """Group for UI-related commands."""


@tui.command("run", help="Start the ArcGIS Pro TUI")
@click.option("--repo", default=".", help="Working directory (optional)")
def run(repo: str) -> None:
    from arcgispro_cli.tui.app import ArcGISProCLIApp

    ArcGISProCLIApp(repo_path=repo).run()
