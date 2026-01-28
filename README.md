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

---

## Using with AI Agents

This tool is designed to make ArcGIS Pro sessions observable for AI coding assistants. Here's how to use it with popular CLI-based agents.

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
    └── context.md         # Human-readable summary (best for AI)
```

### Claude Code / Copilot CLI / Gemini CLI

These tools can read files from your working directory. Just navigate to your ArcGIS Pro project folder:

```bash
cd /path/to/your/project
arcgispro inspect   # Verify exports exist

# Then start your AI session
claude               # or: copilot, gemini
```

**Example prompts:**

```
Read .arcgispro/snapshot/context.md and summarize what layers are in this project.

Look at .arcgispro/context/layers.json and tell me which layers have 
more than 100,000 features.

Based on the field schemas in .arcgispro/context/layers.json, write a 
Python script using arcpy to calculate a new field.

Look at .arcgispro/images/map_Map.png and describe what you see.
```

### Tips for Best Results

1. **Run `arcgispro snapshot` before starting your AI session** - ensures context is fresh

2. **Point the agent to context.md first** - it's a concise summary:
   ```
   Read .arcgispro/snapshot/context.md to understand my ArcGIS Pro project.
   ```

3. **Use layers.json for detailed field info** - includes field names, types, and aliases:
   ```
   What fields are available in the "parcels" layer? Check .arcgispro/context/layers.json
   ```

4. **Share images for visual context** - map screenshots help AI understand your data:
   ```
   Look at .arcgispro/images/map_Map.png - what type of data is being displayed?
   ```

### Automated Workflows

You can script exports for CI/CD or batch processing:

```bash
# Export context from Pro (requires clicking Snapshot in Pro first)
# Then validate and use in automation:
arcgispro dump && arcgispro images && echo "Exports valid"

# Clean up after processing
arcgispro clean --all
```

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
