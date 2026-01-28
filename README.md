# ArcGIS Pro CLI

Make ArcGIS Pro sessions observable for AI agents and automation tools.

## Overview

This project consists of two components:

1. **ProExporter** - An ArcGIS Pro add-in that exports session context to disk
2. **arcgispro-cli** - A Python CLI tool that reads and validates the exports

## How It Works

1. Open a project in ArcGIS Pro
2. Click **Snapshot** in the ProExporter ribbon tab
3. A `.arcgispro/` folder is created next to your `.aprx` file containing:
   - Project metadata (maps, layouts, geodatabases)
   - Layer details (fields, feature counts, visibility, symbology type)
   - Standalone tables and data connections
   - Map/layout images as PNG
   - Markdown summaries for AI consumption

4. Use the CLI to inspect exports:
   ```bash
   cd /path/to/your/project
   arcgispro inspect
   ```

## Installation

### ArcGIS Pro Add-in

1. Build `ProExporter.sln` in Visual Studio (Release configuration)
2. Double-click `ProExporter.esriAddinX` to install
3. Restart ArcGIS Pro

### Python CLI

```bash
cd cli
pip install -e .
```

## CLI Commands

| Command | Description |
|---------|-------------|
| `arcgispro inspect` | Print human-readable summary |
| `arcgispro dump` | Validate context JSON files |
| `arcgispro images` | Validate exported images |
| `arcgispro snapshot` | Assemble full snapshot |
| `arcgispro clean` | Remove generated files |
| `arcgispro open` | Select active project |

## Requirements

- ArcGIS Pro 3.x with .NET 8 SDK
- Python 3.10+
- Visual Studio 2022 with ArcGIS Pro SDK extension

## License

MIT
