"""Basic tests for arcgispro_cli."""

import pytest
from click.testing import CliRunner

from arcgispro_cli.cli import main


def test_version():
    """Test --version flag."""
    runner = CliRunner()
    result = runner.invoke(main, ["--version"])
    assert result.exit_code == 0
    # The preferred entrypoint is now `arcgis` (with aliases `arcgispro`, `agp`).
    assert "arcgis" in result.output.lower()


def test_help():
    """Test --help flag."""
    runner = CliRunner()
    result = runner.invoke(main, ["--help"])
    assert result.exit_code == 0
    assert "ArcGIS Pro CLI" in result.output


def test_install_help():
    """Test install --help."""
    runner = CliRunner()
    result = runner.invoke(main, ["install", "--help"])
    assert result.exit_code == 0
    assert "ProExporter" in result.output


def test_layers_no_folder():
    """Test layers command when no .arcgispro folder exists."""
    runner = CliRunner()
    with runner.isolated_filesystem():
        result = runner.invoke(main, ["layers"])
        assert result.exit_code == 1
        assert "No .arcgispro folder found" in result.output


def test_status_no_folder():
    """Test status command when no .arcgispro folder exists."""
    runner = CliRunner()
    with runner.isolated_filesystem():
        result = runner.invoke(main, ["status"])
        assert result.exit_code == 1
        assert "No .arcgispro folder found" in result.output


def test_addin_bundled():
    """Test that the add-in file is bundled."""
    from arcgispro_cli.commands.install import get_addin_path
    
    addin_path = get_addin_path()
    assert addin_path.exists(), f"Add-in not found at {addin_path}"
    assert addin_path.suffix == ".addin"
