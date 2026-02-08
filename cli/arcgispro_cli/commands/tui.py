import click


@click.group(help="Launch the interactive Textual UI")
def tui():
    """Group for UI-related commands."""


@tui.command("run", help="Start the ArcGIS Pro TUI")
@click.option("--repo", default=".", help="Working directory (optional)")
@click.option("--no-banner", is_flag=True, help="Disable the ASCII banner")
def run(repo: str, no_banner: bool) -> None:
    from arcgispro_cli.tui.app import ArcGISProCLIApp

    ArcGISProCLIApp(repo_path=repo, show_banner=(not no_banner)).run()
