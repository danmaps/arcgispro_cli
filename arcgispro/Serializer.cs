using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProExporter
{
    /// <summary>
    /// Serializes export context to JSON and Markdown files
    /// </summary>
    public static class Serializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Write all context files to the output folder
        /// </summary>
        public static async Task<List<string>> WriteContextAsync(ExportContext context, string outputFolder)
        {
            var files = new List<string>();
            
            // Create folders
            var contextFolder = Path.Combine(outputFolder, "context");
            var snapshotFolder = Path.Combine(outputFolder, "snapshot");
            Directory.CreateDirectory(contextFolder);
            Directory.CreateDirectory(snapshotFolder);

            // Write meta.json to root
            var metaPath = Path.Combine(outputFolder, "meta.json");
            await WriteJsonAsync(metaPath, context.Meta);
            files.Add(metaPath);

            // Write individual context files
            var projectPath = Path.Combine(contextFolder, "project.json");
            await WriteJsonAsync(projectPath, context.Project);
            files.Add(projectPath);

            var mapsPath = Path.Combine(contextFolder, "maps.json");
            await WriteJsonAsync(mapsPath, context.Maps);
            files.Add(mapsPath);

            var layersPath = Path.Combine(contextFolder, "layers.json");
            await WriteJsonAsync(layersPath, context.Layers);
            files.Add(layersPath);

            var tablesPath = Path.Combine(contextFolder, "tables.json");
            await WriteJsonAsync(tablesPath, context.Tables);
            files.Add(tablesPath);

            var connectionsPath = Path.Combine(contextFolder, "connections.json");
            await WriteJsonAsync(connectionsPath, context.Connections);
            files.Add(connectionsPath);

            var layoutsPath = Path.Combine(contextFolder, "layouts.json");
            await WriteJsonAsync(layoutsPath, context.Layouts);
            files.Add(layoutsPath);

            // Write human-readable markdown
            var contextMdPath = Path.Combine(snapshotFolder, "context.md");
            await WriteContextMarkdownAsync(contextMdPath, context);
            files.Add(contextMdPath);

            // Write skill files
            var contextSkillPath = Path.Combine(snapshotFolder, "CONTEXT_SKILL.md");
            await WriteContextSkillAsync(contextSkillPath);
            files.Add(contextSkillPath);

            var agentSkillPath = Path.Combine(snapshotFolder, "AGENT_TOOL_SKILL.md");
            await WriteAgentToolSkillAsync(agentSkillPath);
            files.Add(agentSkillPath);

            // Write active project marker
            if (context.Project != null)
            {
                var activeProjectPath = Path.Combine(outputFolder, "active_project.txt");
                await File.WriteAllTextAsync(activeProjectPath, context.Project.Path ?? context.Project.Name);
                files.Add(activeProjectPath);
            }

            return files;
        }

        /// <summary>
        /// Write an object as JSON to a file
        /// </summary>
        private static async Task WriteJsonAsync<T>(string path, T obj)
        {
            var json = JsonSerializer.Serialize(obj, JsonOptions);
            await File.WriteAllTextAsync(path, json, Encoding.UTF8);
        }

        /// <summary>
        /// Write human-readable context markdown
        /// </summary>
        private static async Task WriteContextMarkdownAsync(string path, ExportContext context)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("# ArcGIS Pro Session Context");
            sb.AppendLine();
            sb.AppendLine($"*Exported: {context.Meta.ExportedAt:yyyy-MM-dd HH:mm:ss} UTC*");
            sb.AppendLine();

            // Project section
            if (context.Project != null)
            {
                sb.AppendLine("## Project");
                sb.AppendLine();
                sb.AppendLine($"- **Name:** {context.Project.Name}");
                sb.AppendLine($"- **Path:** `{context.Project.Path}`");
                if (!string.IsNullOrEmpty(context.Project.DefaultGeodatabase))
                    sb.AppendLine($"- **Default Geodatabase:** `{context.Project.DefaultGeodatabase}`");
                sb.AppendLine($"- **Maps:** {context.Project.MapNames.Count}");
                sb.AppendLine($"- **Layouts:** {context.Project.LayoutNames.Count}");
                sb.AppendLine();
            }

            // Maps section
            if (context.Maps.Any())
            {
                sb.AppendLine("## Maps");
                sb.AppendLine();
                foreach (var map in context.Maps)
                {
                    var activeMarker = map.IsActiveMap ? " ⭐ *Active*" : "";
                    sb.AppendLine($"### {map.Name}{activeMarker}");
                    sb.AppendLine();
                    sb.AppendLine($"- **Type:** {map.MapType}");
                    if (!string.IsNullOrEmpty(map.SpatialReferenceName))
                        sb.AppendLine($"- **Spatial Reference:** {map.SpatialReferenceName} (WKID: {map.SpatialReferenceWkid})");
                    sb.AppendLine($"- **Layers:** {map.LayerCount}");
                    sb.AppendLine($"- **Standalone Tables:** {map.StandaloneTableCount}");
                    if (map.Scale.HasValue)
                        sb.AppendLine($"- **Scale:** 1:{map.Scale:N0}");
                    sb.AppendLine();
                }
            }

            // Layers section
            if (context.Layers.Any())
            {
                sb.AppendLine("## Layers");
                sb.AppendLine();
                
                // Group by map
                var layersByMap = context.Layers.GroupBy(l => l.MapName);
                foreach (var mapGroup in layersByMap)
                {
                    sb.AppendLine($"### {mapGroup.Key}");
                    sb.AppendLine();
                    sb.AppendLine("| Layer | Type | Geometry | Features | Visible |");
                    sb.AppendLine("|-------|------|----------|----------|---------|");
                    
                    foreach (var layer in mapGroup)
                    {
                        var visibleIcon = layer.IsVisible ? "✅" : "❌";
                        var brokenMarker = layer.IsBroken ? " ⚠️" : "";
                        var featureCount = layer.FeatureCount?.ToString("N0") ?? "-";
                        var geometry = layer.GeometryType ?? "-";
                        
                        sb.AppendLine($"| {layer.Name}{brokenMarker} | {layer.LayerType} | {geometry} | {featureCount} | {visibleIcon} |");
                    }
                    sb.AppendLine();
                }
            }

            // Standalone tables section
            if (context.Tables.Any())
            {
                sb.AppendLine("## Standalone Tables");
                sb.AppendLine();
                sb.AppendLine("| Table | Rows | Data Source |");
                sb.AppendLine("|-------|------|-------------|");
                
                foreach (var table in context.Tables)
                {
                    var rowCount = table.RowCount?.ToString("N0") ?? "-";
                    var source = table.DataSourceType ?? "-";
                    sb.AppendLine($"| {table.Name} | {rowCount} | {source} |");
                }
                sb.AppendLine();
            }

            // Layouts section
            if (context.Layouts.Any())
            {
                sb.AppendLine("## Layouts");
                sb.AppendLine();
                foreach (var layout in context.Layouts)
                {
                    sb.AppendLine($"### {layout.Name}");
                    sb.AppendLine();
                    sb.AppendLine($"- **Size:** {layout.PageWidth} x {layout.PageHeight} {layout.PageUnits}");
                    if (layout.MapFrameNames.Any())
                        sb.AppendLine($"- **Map Frames:** {string.Join(", ", layout.MapFrameNames)}");
                    sb.AppendLine();
                }
            }

            // Connections section
            if (context.Connections.Any())
            {
                sb.AppendLine("## Data Connections");
                sb.AppendLine();
                sb.AppendLine("| Name | Type | Path |");
                sb.AppendLine("|------|------|------|");
                
                foreach (var conn in context.Connections)
                {
                    sb.AppendLine($"| {conn.Name} | {conn.ConnectionType} | `{conn.Path}` |");
                }
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Write the CONTEXT_SKILL.md file explaining how to use the exported context
        /// </summary>
        private static async Task WriteContextSkillAsync(string path)
        {
            var content = @"# How to Use This Context

This folder contains exported context from an ArcGIS Pro session. Use this information to understand what the user is working on.

## File Structure

```
.arcgispro/
├── meta.json           # Export metadata (timestamp, version)
├── active_project.txt  # Path to the active .aprx project
├── context/
│   ├── project.json    # Project-level info
│   ├── maps.json       # All maps in the project
│   ├── layers.json     # All layers with metadata
│   ├── tables.json     # Standalone tables
│   ├── connections.json # Database/folder connections
│   └── layouts.json    # Print layouts
├── snapshot/
│   ├── context.md      # Human-readable summary (start here!)
│   ├── CONTEXT_SKILL.md # This file
│   └── AGENT_TOOL_SKILL.md # CLI usage guide
└── images/
    ├── map_*.png       # Map view screenshots
    └── layout_*.png    # Layout exports
```

## Quick Start

1. **Read `snapshot/context.md`** for a human-readable overview
2. **Check `images/`** to see what the map looks like
3. **Parse `context/*.json`** for detailed programmatic access

## Key Fields

### layers.json
- `name`: Layer display name
- `layerType`: FeatureLayer, RasterLayer, GroupLayer, etc.
- `geometryType`: Point, Polyline, Polygon
- `featureCount`: Number of features (may be null if unavailable)
- `selectionCount`: Currently selected features
- `isVisible`: Whether layer is visible in the map
- `isBroken`: Whether the data source is broken/missing
- `definitionQuery`: SQL filter applied to the layer
- `fields`: Array of field definitions

### maps.json
- `isActiveMap`: true for the currently displayed map
- `scale`: Current map scale (1:X)
- `extent`: Current view extent (xmin, ymin, xmax, ymax)

## Tips

- The `isActiveMap` flag tells you which map the user is looking at
- Check `selectionCount` to see if the user has selected features
- `isBroken: true` indicates a data source problem
- Field information is only available for feature layers with accessible data sources
";
            await File.WriteAllTextAsync(path, content, Encoding.UTF8);
        }

        /// <summary>
        /// Write the AGENT_TOOL_SKILL.md file explaining CLI usage
        /// </summary>
        private static async Task WriteAgentToolSkillAsync(string path)
        {
            var content = @"# ArcGIS Pro CLI Tool

The `arcgispro` CLI tool reads exported context and provides commands for AI agents.

## Installation

```bash
pip install arcgispro-cli
# or
pip install -e path/to/arcgispro_cli
```

## Commands

### Inspect Current Context
```bash
arcgispro inspect
```
Prints a human-readable summary of the exported context.

### Validate Exports
```bash
arcgispro dump     # Validate JSON context files
arcgispro images   # Validate exported images
```

### Create Full Snapshot
```bash
arcgispro snapshot
```
Assembles context + images into the `snapshot/` folder.

### Clean Up
```bash
arcgispro clean --all       # Remove all exports
arcgispro clean --images    # Remove only images
arcgispro clean --context   # Remove only context JSON
arcgispro clean --snapshot  # Remove only snapshot folder
```

## Folder Contract

The CLI expects exports in `.arcgispro/` relative to the current directory or an ancestor directory.

## Workflow

1. **In ArcGIS Pro:** Click ""Snapshot"" button in the ArcGIS Pro CLI ribbon group
2. **In terminal:** Run `arcgispro inspect` to see what was exported
3. **Use context:** Read JSON files or markdown summary for analysis

## Notes

- The CLI is read-only and never modifies the ArcGIS Pro project
- All outputs are written to `.arcgispro/` only
- Re-run export from Pro to update stale context
";
            await File.WriteAllTextAsync(path, content, Encoding.UTF8);
        }
    }
}
