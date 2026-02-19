# ArcGIS Pro CLI

A command-line tool for inspecting and managing ArcGIS Pro session exports.

## Installation

```bash
cd cli
pip install -e .
```

## Usage

```bash
# View help
arcgispro --help

# Inspect exported context
arcgispro inspect

# Validate JSON exports
arcgispro dump

# Validate images
arcgispro images

# Render project diagram
arcgispro diagram

# Clean up exports
arcgispro clean --all

# Select active project
arcgispro open
```

## Using arcpy with Terminal Sessions

The ArcGIS Pro CLI add-in includes a **Terminal** button that opens cmd.exe with the Pro Python environment activated. To avoid "Directory does not exist" errors with stale temp workspace paths, use the session helper:

```python
import arcgispro_cli.session as session

# Ensure connection to running Pro session
if session.ensure_arcpy_connection():
    import arcpy
    # arcpy will now use the current Pro session's workspace
    print("Connected to Pro session:", session.get_session_info())
else:
    print("No active Pro session found")
```

### Session Helper API

- `ensure_arcpy_connection()` - Sets ARCGISPRO_PID env var, returns True if successful
- `get_session_info()` - Returns dict with processId, timestamp, tempPath, proTempPath
- `get_pro_temp_path()` - Returns Path to current Pro temp directory
- `is_pro_running()` - Returns True if session.json exists and is recent (<24hrs)

This prevents errors like:
```
ERROR Directory does not exist or cannot be accessed: C:\Users\...\Temp\ArcGISProTemp27980
```

## Workflow

1. **In ArcGIS Pro:** Click "Snapshot" button in the **CLI** ribbon tab
2. **In terminal:** Run `arcgispro inspect` to see what was exported
3. **Use context:** Read JSON files or markdown summary for AI analysis

## Folder Structure

The CLI reads from `.arcgispro/` folder created by the add-in:

```
.arcgispro/
├── meta.json           # Export metadata
├── session.json        # Current Pro session info (PID, temp paths)
├── active_project.txt  # Path to active .aprx
├── context/
│   ├── project.json
│   ├── maps.json
│   ├── layers.json
│   ├── tables.json
│   ├── connections.json
│   └── layouts.json
├── snapshot/
│   ├── context.md
│   ├── project-structure.mmd
│   ├── project-structure.md
│   ├── CONTEXT_SKILL.md
│   └── AGENT_TOOL_SKILL.md
└── images/
    ├── map_*.png
    └── layout_*.png
```

## Requirements

- Python 3.9+
- click
- rich
