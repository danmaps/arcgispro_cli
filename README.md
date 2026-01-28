# ArcGIS Pro CLI

Make ArcGIS Pro sessions observable for AI agents and automation tools.

## Two Components, Clear Roles

| Component | Role | What it does |
|-----------|------|--------------|
| **ProExporter Add-in** | Context exfiltration | Exports session state from ArcGIS Pro to disk |
| **arcgispro CLI** | Query interface | Reads exported data and answers questions |

The add-in **exports**. The CLI **queries**. Simple.

## Quick Start

```bash
pip install arcgispro-cli
arcgispro install
```

That's it! The `install` command launches the add-in installer. Click "Install Add-In" and restart ArcGIS Pro.

## How It Works

1. Open a project in ArcGIS Pro
2. Click **Snapshot** in the **CLI** ribbon tab → exports context to `.arcgispro/`
3. Query the exported context:
   ```bash
   cd /path/to/your/project
   arcgispro layers              # What layers do I have?
   arcgispro layer "Parcels"     # Tell me about this layer
   arcgispro fields "Parcels"    # What fields are in it?
   ```

## CLI Commands

### Setup Commands

| Command | Description |
|---------|-------------|
| `arcgispro install` | Install the ProExporter add-in |
| `arcgispro uninstall` | Show uninstall instructions |
| `arcgispro status` | Show export status and validate files |
| `arcgispro clean` | Remove generated files |
| `arcgispro open` | Open folder or select project |

### Query Commands

| Command | Description |
|---------|-------------|
| `arcgispro project` | Show project info |
| `arcgispro maps` | List all maps |
| `arcgispro map [name]` | Show map details (default: active map) |
| `arcgispro layers` | List all layers |
| `arcgispro layers --broken` | List broken layers |
| `arcgispro layer <name>` | Show layer details + field schema |
| `arcgispro fields <name>` | Show just the field schema |
| `arcgispro tables` | List standalone tables |
| `arcgispro connections` | List data connections |
| `arcgispro context` | Print full markdown summary |

All query commands support `--json` for machine-readable output.

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
├── meta.json              # Export timestamp, tool version
├── context/
│   ├── project.json       # Project name, path, geodatabases
│   ├── maps.json          # Map names, spatial references, scales
│   ├── layers.json        # Full layer details with field schemas
│   ├── tables.json        # Standalone tables
│   ├── connections.json   # Database connections
│   └── layouts.json       # Print layouts
├── images/
│   ├── map_*.png          # Screenshots of each map view
│   └── layout_*.png       # Screenshots of each layout
└── snapshot/
    └── context.md         # Human-readable summary
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
