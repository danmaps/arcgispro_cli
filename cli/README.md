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

# Assemble snapshot
arcgispro snapshot

# Clean up exports
arcgispro clean --all

# Select active project
arcgispro open
```

## Workflow

1. **In ArcGIS Pro:** Click "Snapshot" button in the ArcGIS Pro CLI ribbon
2. **In terminal:** Run `arcgispro inspect` to see what was exported
3. **Use context:** Read JSON files or markdown summary for AI analysis

## Folder Structure

The CLI reads from `.arcgispro/` folder created by the add-in:

```
.arcgispro/
├── meta.json           # Export metadata
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
