# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
