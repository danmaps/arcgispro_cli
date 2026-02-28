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


def test_tui_without_optional_deps_gives_helpful_error():
    runner = CliRunner()
    result = runner.invoke(main, ["tui"])

    # Without the optional Textual dependency, this should fail fast with guidance.
    # (In dev envs where textual is installed, this test may need updating.)
    assert result.exit_code != 0
    assert "arcgispro-cli[tui]" in result.output


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


def _write_json(path, obj):
    import json

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(obj, indent=2), encoding="utf-8")


def test_layers_active_map_filter():
    runner = CliRunner()
    with runner.isolated_filesystem():
        from pathlib import Path

        _write_json(
            Path(".arcgispro/context/maps.json"),
            [
                {"name": "Map A", "isActiveMap": True},
                {"name": "Map B", "isActiveMap": False},
            ],
        )
        _write_json(
            Path(".arcgispro/context/layers.json"),
            [
                {"name": "L1", "mapName": "Map A", "isVisible": True},
                {"name": "L2", "mapName": "Map B", "isVisible": True},
            ],
        )

        result = runner.invoke(main, ["layers", "--active"])
        assert result.exit_code == 0
        assert "L1" in result.output
        assert "L2" not in result.output


def test_tables_active_map_filter_json():
    runner = CliRunner()
    with runner.isolated_filesystem():
        from pathlib import Path

        _write_json(
            Path(".arcgispro/context/maps.json"),
            [
                {"name": "Map A", "isActiveMap": True},
                {"name": "Map B", "isActiveMap": False},
            ],
        )
        _write_json(
            Path(".arcgispro/context/tables.json"),
            [
                {"name": "T1", "mapName": "Map A"},
                {"name": "T2", "mapName": "Map B"},
            ],
        )

        result = runner.invoke(main, ["tables", "--active", "--json"])
        assert result.exit_code == 0
        assert "T1" in result.output
        assert "T2" not in result.output


def test_layers_active_map_conflicts_with_map():
    runner = CliRunner()
    with runner.isolated_filesystem():
        from pathlib import Path

        _write_json(Path(".arcgispro/context/maps.json"), [{"name": "Map A", "isActiveMap": True}])
        _write_json(Path(".arcgispro/context/layers.json"), [])

        result = runner.invoke(main, ["layers", "--active", "--map", "Map A"])
        assert result.exit_code == 1
        assert "either --map or --active" in result.output


def test_layer_shows_active_map_flag():
    runner = CliRunner()
    with runner.isolated_filesystem():
        from pathlib import Path

        _write_json(
            Path(".arcgispro/context/maps.json"),
            [
                {"name": "Map A", "isActiveMap": True},
                {"name": "Map B", "isActiveMap": False},
            ],
        )
        _write_json(
            Path(".arcgispro/context/layers.json"),
            [
                {"name": "Layer 1", "mapName": "Map A", "isVisible": True},
            ],
        )

        result = runner.invoke(main, ["layer", "Layer 1"])
        assert result.exit_code == 0
        assert "Active map: Yes" in result.output
