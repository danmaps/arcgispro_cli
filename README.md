# ArcGIS Pro CLI

[![PyPI](https://img.shields.io/pypi/v/arcgispro-cli)](https://pypi.org/project/arcgispro-cli/)
[![CI](https://github.com/danmaps/arcgispro_cli/workflows/CI/badge.svg)](https://github.com/danmaps/arcgispro_cli/actions)

Give AI agents eyes into ArcGIS Pro.

```bash
pip install arcgispro-cli

# Optional: install TUI dependencies
pip install arcgispro-cli[tui]

arcgis install
```

## First 5 Minutes (Quickstart)

1. Open an ArcGIS Pro project (.aprx)
2. Click **Snapshot** in the **CLI** ribbon tab
3. In a terminal, run:
   ```bash
   arcgis status
   arcgis layers
   arcgis layer "Parcels"
   ```

More: [docs/quickstart.md](docs/quickstart.md)

## What's New in v0.4.0

- Enhanced TUI with map preview support and improved banner rendering
- Mermaid project structure export (`project-structure.mmd` + markdown wrapper)
- Best-effort stable IDs for maps/layers/tables to improve snapshot tracking
- Geoprocessing history export scaffold for richer context artifacts
- Reliability fixes including Python 3.9 compatibility and improved terminal/add-in robustness

## How It Works

ProExporter (Pro add-in) creates detailed flat files that explain the state of your ArcGIS Pro project. The `arcgis` CLI tool facilitates frictionless reasoning over the context. Fewer assumptions and annoying follow-up questions. Helps the AI help you.

(Backwards compatible aliases: `arcgispro`, `agp`.)

1. Open a project in ArcGIS Pro
2. Click **Snapshot** in the **CLI** ribbon tab
3. Ask questions:
   ```bash
   arcgis layers              # What layers do I have?
   arcgis layer "Parcels"     # Tell me about this layer
   arcgis fields "Parcels"    # What fields are in it?
   ```

## CLI Commands

### Setup

| Command | Description |
|---------|-------------|
| `arcgis install` | Install the ProExporter add-in |
| `arcgis uninstall` | Show uninstall instructions |
| `arcgis launch` | Launch ArcGIS Pro (opens .aprx in current dir if found) |
| `arcgis status` | Show export status and validate files |
| `arcgis clean` | Remove generated files |
| `arcgis open` | Open export folder |

### Query

| Command | Description |
|---------|-------------|
| `arcgis project` | Show project info |
| `arcgis maps` | List all maps |
| `arcgis map [name]` | Map details |
| `arcgis layers` | List all layers |
| `arcgis layers --broken` | Just the broken ones |
| `arcgis layer <name>` | Layer details + fields |
| `arcgis fields <name>` | Just the fields |
| `arcgis tables` | Standalone tables |
| `arcgis connections` | Data connections |
| `arcgis notebooks` | Jupyter notebooks in project |
| `arcgis context` | Full markdown dump |
| `arcgis diagram` | Render Mermaid diagram of project structure |

Add `--json` to any query command for machine-readable output.

## Troubleshooting

**`arcgispro` launches ArcGIS Pro instead of the CLI?**

This happens if `C:\Program Files\ArcGIS\Pro\bin` is on your PATH. Options:
- Prefer `arcgis` (new default): `arcgis layers`, `arcgis launch`
- Use `agp` (alias): `agp layers`, `agp launch`
- Or fix PATH order: ensure Python Scripts comes before ArcGIS Pro bin

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

---

## Using with AI Agents

This tool is designed to make ArcGIS Pro sessions observable for AI coding assistants.

### What Gets Exported

When you click **Snapshot** in ArcGIS Pro, the project structure is:

```
project_root/
├── AGENTS.md              # AI agent skill file (start here!)
├── YourProject.aprx       # ArcGIS Pro project file
└── .arcgispro/
    ├── config.yml         # Export settings (auto-export, toggles)
    ├── meta.json          # Export timestamp, tool version
    ├── context/
    │   ├── project.json       # Project name, path, geodatabases
    │   ├── maps.json          # Map names, spatial references, scales
    │   ├── layers.json        # Full layer details with field schemas
    │   ├── tables.json        # Standalone tables
    │   ├── connections.json   # Database connections
    │   ├── layouts.json       # Print layouts
    │   └── notebooks.json     # Jupyter notebooks
    ├── images/
    │   ├── map_*.png          # Screenshots of each map view
    │   └── layout_*.png       # Screenshots of each layout
    └── snapshot/
        ├── context.md         # Human-readable summary
        ├── project-structure.mmd # Mermaid diagram source
        └── project-structure.md  # Mermaid diagram markdown
```

The `AGENTS.md` file teaches AI agents how to use the CLI and interpret the exported data; no user explanation needed.

### Configuration

Edit `.arcgispro/config.yml` to control export behavior:

```yaml
# Auto-export on project open (default: false)
autoExportEnabled: false
autoExportLocalOnly: true   # Skip network drives
autoExportMaxLayers: 50     # Safety limit

# Content toggles
exportImages: true          # Map/layout screenshots
exportNotebooks: true       # Jupyter notebook metadata
exportFields: true          # Layer field schemas
```

### Claude Code / Copilot CLI / Gemini CLI

These tools can read files and run commands in your working directory. Navigate to your ArcGIS Pro project folder and start your AI session:

```bash
cd /path/to/your/project
claude   # or: copilot, gemini
```

**Example prompts:**

```
What layers are in this project?
> AI runs: arcgis layers

What fields are in the Parcels layer?
> AI runs: arcgis fields "Parcels"

Which layers have broken data sources?
> AI runs: arcgis layers --broken

Give me the full project context
> AI runs: arcgis context

Look at the map screenshot and describe what you see
> AI reads: .arcgispro/images/map_*.png
```

### Tips for Best Results

1. **Click Snapshot in Pro before starting your AI session** - ensures context is fresh

2. **Ask naturally** - the CLI commands map to common questions:
   - "What layers do I have?" → `arcgis layers`
   - "Tell me about the Parcels layer" → `arcgis layer Parcels`
   - "What's the schema?" → `arcgis fields Parcels`

3. **Use `--json` for programmatic access** - AI can parse structured output:
   ```bash
   arcgis layers --json
   arcgis layer "Parcels" --json
   ```

4. **Check images for visual context** - map screenshots help AI understand spatial data

5. **Be bold. Try pasting in a question you'd normally answer by working in ArcGIS Pro manually.**
   - "Jeff wants an updated map of the project area with an imagery basemap instead of streets"
     - AI generates a (working) python script that exports the PDF directly, using your existing map and layout. You get to go to lunch early, and get a raise.

### Custom Agent Integration

The JSON files are designed for programmatic access:

```python
import json
from pathlib import Path

context_dir = Path(".arcgispro/context")
layers = json.loads((context_dir / "layers.json").read_text(encoding="utf-8-sig"))

for layer in layers:
    print(f"{layer['name']}: {layer.get('featureCount', 'N/A')} features")
    for field in layer.get('fields', []):
        print(f"  - {field['name']} ({field['fieldType']})")
```
