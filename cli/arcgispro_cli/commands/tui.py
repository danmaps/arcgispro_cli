import click


@click.command("tui", help="Launch the interactive Textual UI")
@click.option("--repo", default=".", help="Working directory (optional)")
@click.option("--no-banner", is_flag=True, help="Disable the ASCII banner")
def tui_cmd(repo: str, no_banner: bool) -> None:
    """Start the ArcGIS Pro TUI."""
    from arcgispro_cli.tui.app import ArcGISProCLIApp

    ArcGISProCLIApp(repo_path=repo, show_banner=(not no_banner)).run()
