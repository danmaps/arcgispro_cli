# Changelog

<!-- markdownlint-configure-file {"MD024": {"siblings_only": true}} -->

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- **Add-in:** AGENTS.md now written to project root instead of .arcgispro/ for immediate discoverability
- **Add-in:** Replaced settings UI with simple `.arcgispro/config.yml` file (easier for agents to edit)
- **Add-in:** Snapshot now fully replaces prior output by deleting existing `.arcgispro/` and regenerating `AGENTS.md`/`agents.md`

## [0.3.2] - 2026-01-29

### Added

- **Add-in:** Auto-export on project open (optional, disabled by default)
- **Add-in:** Safety checks for auto-export (local drive only, max layers threshold)
- **Add-in:** Export content toggles via config.yml (images, notebooks, fields)

### Removed

- **Add-in:** Settings page from Options menu (replaced with config.yml)

## [0.3.1] - 2026-01-29

### Added

- **CLI:** `notebooks` command to list Jupyter notebooks in the project
- **Add-in:** Exports notebook metadata (name, path, description, cell breakdown) to `notebooks.json`
- Notebook descriptions extracted from first markdown cell (falls back to first code cell)

## [0.3.0] - 2026-01-29

### Added

- **CLI:** `launch` command to open ArcGIS Pro (auto-detects .aprx in current dir)
- **Add-in:** Terminal button opens cmd at project folder with Pro Python env activated
- **Add-in:** Generates `AGENTS.md` skill file for AI agent discovery
- **Repo:** Added `.github/copilot-instructions.md` for Copilot context

### Changed

- Consolidated `CONTEXT_SKILL.md` and `AGENT_TOOL_SKILL.md` into single `AGENTS.md` at `.arcgispro/` root

## [0.2.1] - 2026-01-28

### Changed

- Updated README with component roles table (add-in exports, CLI queries)
- Updated AGENT_TOOL_SKILL.md with v0.2.0 query commands

## [0.2.0] - 2026-01-28

### Changed

- **Breaking:** Restructured CLI commands to be query-focused
- Removed `inspect`, `dump`, `images`, `snapshot` commands (add-in handles export)
- Added query commands: `project`, `maps`, `map`, `layers`, `layer`, `fields`, `tables`, `connections`, `context`
- Added `status` command (replaces dump/images validation)
- All query commands support `--json` flag for machine-readable output
- `layers` command supports `--broken` and `--map` filters
- `layer` and `fields` commands support partial name matching
- Updated README with clearer examples and natural language prompts

## [0.1.4] - 2026-01-28

### Changed

- Add-in now appears on its own "CLI" tab instead of the generic "Add-In" tab

## [0.1.3] - 2026-01-28

### Fixed

- PyPI package now uses root README for better documentation

## [0.1.2] - 2026-01-28

### Fixed

- Renamed bundled add-in to avoid PyPI ZIP inspection issue

## [0.1.1] - 2026-01-28

### Fixed

- Fixed wheel build issue with bundled add-in

## [0.1.0] - 2026-01-28

### Added

- **ProExporter add-in** for ArcGIS Pro
  - Snapshot button: exports full context + images
  - Dump Context button: exports JSON and Markdown only
  - Export Images button: exports map/layout PNGs only
  - Open Folder button: opens .arcgispro/ in Explorer
- **arcgispro CLI** commands:
  - `arcgispro install` - install the bundled add-in
  - `arcgispro uninstall` - show uninstall instructions
  - `arcgispro inspect` - human-readable summary of exports
  - `arcgispro dump` - validate context JSON files
  - `arcgispro images` - validate exported images
  - `arcgispro snapshot` - assemble full snapshot
  - `arcgispro clean` - remove generated files
  - `arcgispro open` - select active project
- Export includes:
  - Project metadata (name, path, geodatabases)
  - Maps with spatial reference, scale, extent
  - Layers with full field schemas (name, type, alias, length)
  - Feature counts and selection counts
  - Standalone tables
  - Database connections
  - Layouts
  - Map/layout images as PNG
  - Markdown summary for AI consumption

[Unreleased]: https://github.com/danmaps/arcgispro_cli/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/danmaps/arcgispro_cli/releases/tag/v0.1.0
