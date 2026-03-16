import click


@click.command("tui", help="Launch the interactive Textual UI")
@click.option("--repo", default=".", help="Working directory (optional)")
@click.option("--no-banner", is_flag=True, help="Disable the ASCII banner")
def tui_cmd(repo: str, no_banner: bool) -> None:
    """Start the ArcGIS Pro TUI."""
    try:
        from arcgispro_cli.tui.app import ArcGISProCLIApp
    except ModuleNotFoundError as e:
        # The most common case is missing optional TUI deps (textual).
        if (e.name or "").startswith("textual"):
            raise click.ClickException(
                "TUI dependencies are not installed. Install with: pip install arcgispro-cli[tui]"
            )
        raise

    ArcGISProCLIApp(repo_path=repo, show_banner=(not no_banner)).run()
