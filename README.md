# ArcGIS Pro CLI

Make ArcGIS Pro sessions observable for AI agents and automation tools.

## Quick Start

```bash
pip install arcgispro-cli
arcgispro install
```

That's it! The `install` command launches the add-in installer. Click "Install Add-In" and restart ArcGIS Pro.

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

## CLI Commands

| Command | Description |
|---------|-------------|
| `arcgispro install` | Install the ProExporter add-in |
| `arcgispro uninstall` | Show uninstall instructions |
| `arcgispro inspect` | Print human-readable summary |
| `arcgispro dump` | Validate context JSON files |
| `arcgispro images` | Validate exported images |
| `arcgispro snapshot` | Assemble full snapshot |
| `arcgispro clean` | Remove generated files |
| `arcgispro open` | Select active project |

## Requirements

- Windows 10/11
- ArcGIS Pro 3.x
- Python 3.9+

## Development

To build the add-in from source, you'll need:
- Visual Studio 2022 with ArcGIS Pro SDK extension
- .NET 8 SDK

```bash
# Clone and install CLI in dev mode
git clone https://github.com/danmaps/arcgispro_cli.git
cd arcgispro_cli/cli
pip install -e .

# Build add-in in Visual Studio
# Open ProExporter/ProExporter.sln
# Build → Build Solution (Release)
```

## License

MIT
