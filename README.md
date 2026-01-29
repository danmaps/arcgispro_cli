# ArcGIS Pro CLI

[![PyPI](https://img.shields.io/pypi/v/arcgispro-cli)](https://pypi.org/project/arcgispro-cli/)
[![CI](https://github.com/danmaps/arcgispro_cli/workflows/CI/badge.svg)](https://github.com/danmaps/arcgispro_cli/actions)

Give AI agents eyes into ArcGIS Pro.

```bash
pip install arcgispro-cli
arcgispro install
```

## How It Works

**Add-in exports. CLI queries.**

1. Open a project in ArcGIS Pro
2. Click **Snapshot** in the **CLI** ribbon tab
3. Ask questions:
   ```bash
   arcgispro layers              # What layers do I have?
   arcgispro layer "Parcels"     # Tell me about this layer
   arcgispro fields "Parcels"    # What fields are in it?
   ```

## CLI Commands

### Setup

| Command | Description |
|---------|-------------|
| `arcgispro install` | Install the ProExporter add-in |
| `arcgispro uninstall` | Show uninstall instructions |
| `arcgispro launch` | Launch ArcGIS Pro (opens .aprx in current dir if found) |
| `arcgispro status` | Show export status and validate files |
| `arcgispro clean` | Remove generated files |
| `arcgispro open` | Open export folder |

### Query

| Command | Description |
|---------|-------------|
| `arcgispro project` | Show project info |
| `arcgispro maps` | List all maps |
| `arcgispro map [name]` | Map details |
| `arcgispro layers` | List all layers |
| `arcgispro layers --broken` | Just the broken ones |
| `arcgispro layer <name>` | Layer details + fields |
| `arcgispro fields <name>` | Just the fields |
| `arcgispro tables` | Standalone tables |
| `arcgispro connections` | Data connections |
| `arcgispro notebooks` | Jupyter notebooks in project |
| `arcgispro context` | Full markdown dump |

Add `--json` to any query command for machine-readable output.

## Troubleshooting

**`arcgispro` launches ArcGIS Pro instead of the CLI?**

This happens if `C:\Program Files\ArcGIS\Pro\bin` is on your PATH. Options:
- Use `agp` instead (alias): `agp layers`, `agp launch`
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

When you click **Snapshot** in ArcGIS Pro, the `.arcgispro/` folder contains:

```
.arcgispro/
├── AGENTS.md              # AI agent skill file (start here!)
├── meta.json              # Export timestamp, tool version
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
    └── context.md         # Human-readable summary
```

The `AGENTS.md` file teaches AI agents how to use the CLI and interpret the exported data — no user explanation needed.

### Claude Code / Copilot CLI / Gemini CLI

These tools can read files and run commands in your working directory. Navigate to your ArcGIS Pro project folder and start your AI session:

```bash
cd /path/to/your/project
claude   # or: copilot, gemini
```

**Example prompts:**

```
What layers are in this project?
> AI runs: arcgispro layers

What fields are in the Parcels layer?
> AI runs: arcgispro fields "Parcels"

Which layers have broken data sources?
> AI runs: arcgispro layers --broken

Give me the full project context
> AI runs: arcgispro context

Look at the map screenshot and describe what you see
> AI reads: .arcgispro/images/map_*.png
```

### Tips for Best Results

1. **Click Snapshot in Pro before starting your AI session** - ensures context is fresh

2. **Ask naturally** - the CLI commands map to common questions:
   - "What layers do I have?" → `arcgispro layers`
   - "Tell me about the Parcels layer" → `arcgispro layer Parcels`
   - "What's the schema?" → `arcgispro fields Parcels`

3. **Use `--json` for programmatic access** - AI can parse structured output:
   ```bash
   arcgispro layers --json
   arcgispro layer "Parcels" --json
   ```

4. **Check images for visual context** - map screenshots help AI understand spatial data

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
