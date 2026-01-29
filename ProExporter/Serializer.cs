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

            var notebooksPath = Path.Combine(contextFolder, "notebooks.json");
            await WriteJsonAsync(notebooksPath, context.Notebooks);
            files.Add(notebooksPath);

            // Write human-readable markdown
            var contextMdPath = Path.Combine(snapshotFolder, "context.md");
            await WriteContextMarkdownAsync(contextMdPath, context);
            files.Add(contextMdPath);

            // Write AGENTS.md to project root for immediate discoverability
            var projectRoot = Directory.GetParent(outputFolder)?.FullName;
            if (projectRoot != null)
            {
                var agentsPath = Path.Combine(projectRoot, "AGENTS.md");
                await WriteAgentsFileAsync(agentsPath, context);
                files.Add(agentsPath);
            }

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

            // Notebooks section
            if (context.Notebooks.Any())
            {
                sb.AppendLine("## Notebooks");
                sb.AppendLine();
                
                foreach (var notebook in context.Notebooks)
                {
                    sb.AppendLine($"### {notebook.Name}");
                    sb.AppendLine();
                    sb.AppendLine($"- **Path:** `{notebook.Path}`");
                    sb.AppendLine($"- **Cells:** {notebook.CellCount} ({string.Join(", ", notebook.CellBreakdown.Select(kv => $"{kv.Value} {kv.Key}"))})");
                    if (notebook.LastModified.HasValue)
                        sb.AppendLine($"- **Modified:** {notebook.LastModified:yyyy-MM-dd HH:mm}");
                    if (!string.IsNullOrEmpty(notebook.Description))
                    {
                        sb.AppendLine();
                        sb.AppendLine("**Description:**");
                        sb.AppendLine("```");
                        sb.AppendLine(notebook.Description.Length > 300 ? notebook.Description.Substring(0, 297) + "..." : notebook.Description);
                        sb.AppendLine("```");
                    }
                    sb.AppendLine();
                }
            }

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Write the AGENTS.md file - a skill file for AI agents
        /// </summary>
        private static async Task WriteAgentsFileAsync(string path, ExportContext context)
        {
            var timestamp = context.Meta.ExportedAt.ToString("yyyy-MM-dd HH:mm:ss");
            var content = $@"# ArcGIS Pro Session Context

> **Snapshot taken:** {timestamp} UTC
> **Use the `arcgispro` CLI to query this data.**

## Quick Start

```bash
arcgispro layers              # List all layers
arcgispro layer ""LayerName""   # Layer details + fields  
arcgispro fields ""LayerName""  # Just the fields
arcgispro context             # Full markdown summary
```

Add `--json` to any command for structured output.

## When to Request a New Snapshot

Ask the user to click **Snapshot** in ArcGIS Pro when:
- You need current layer/field information before making changes
- User mentions they modified something in Pro
- Data seems stale (check timestamp above)
- You see `isBroken: true` and want to verify it's still broken

## What NOT to Assume

- **Pro state matches this export** — User may have added/removed layers since snapshot
- **All data sources are valid** — Check `isBroken` field in layer info
- **Field names are exact** — Pro field names are case-insensitive but aliases may differ
- **Feature counts are current** — Counts are from snapshot time, not live

## Available Commands

| Command | Purpose |
|---------|---------|
| `arcgispro project` | Project name, path, geodatabases |
| `arcgispro maps` | List all maps |
| `arcgispro map ""Name""` | Map details (scale, extent, SR) |
| `arcgispro layers` | List all layers across all maps |
| `arcgispro layers --broken` | Only layers with broken data sources |
| `arcgispro layer ""Name""` | Layer details + field schema |
| `arcgispro fields ""Name""` | Just the fields for a layer |
| `arcgispro tables` | Standalone tables |
| `arcgispro connections` | Database/folder connections |
| `arcgispro notebooks` | Jupyter notebooks in project |
| `arcgispro context` | Full markdown dump (good for pasting) |
| `arcgispro status` | Validate export files |

## File Structure

```
project_root/
├── AGENTS.md           # This file (start here!)
├── TSPM_Overview.aprx  # Your ArcGIS Pro project
└── .arcgispro/         # Export folder (CLI queries this)
    ├── config.yml          # Export settings (auto-export, toggles)
    ├── meta.json           # Export timestamp, tool version
    ├── active_project.txt  # Path to the .aprx file
    ├── context/
    │   ├── project.json    # Project metadata
    │   ├── maps.json       # All maps with extents/scales
    │   ├── layers.json     # All layers with field schemas
    │   ├── tables.json     # Standalone tables
    │   ├── connections.json # Data connections
    │   ├── layouts.json    # Print layouts
    │   └── notebooks.json  # Jupyter notebooks
    ├── images/
    │   ├── map_*.png       # Screenshots of each map view
    │   └── layout_*.png    # Screenshots of each layout
    └── snapshot/
        └── context.md      # Human-readable summary
```

## Configuration

Edit `.arcgispro/config.yml` to control exports:
- `autoExportEnabled` — Auto-export on project open (default: false)
- `autoExportLocalOnly` — Skip network drives (default: true)
- `autoExportMaxLayers` — Safety limit (default: 50)
- `exportImages` — Include map screenshots (default: true)
- `exportNotebooks` — Include notebook metadata (default: true)
- `exportFields` — Include layer field schemas (default: true)

## Key JSON Fields

### layers.json
- `name` — Display name
- `layerType` — FeatureLayer, RasterLayer, GroupLayer, etc.
- `geometryType` — Point, Polyline, Polygon (null for non-spatial)
- `featureCount` — Feature count (may be null)
- `selectionCount` — Currently selected features
- `isVisible` — Layer visibility in map
- `isBroken` — Data source is missing/broken
- `definitionQuery` — SQL filter on the layer
- `fields[]` — Array of field definitions

### maps.json  
- `isActiveMap` — true = user is currently viewing this map
- `scale` — Current map scale (1:X)
- `extent` — View bounds (xmin, ymin, xmax, ymax)

### notebooks.json
- `name` — Notebook filename
- `path` — Full path to .ipynb file
- `description` — First markdown cell (or code cell if no markdown)
- `cellCount` — Total number of cells
- `cellBreakdown` — Count by type (markdown, code)

## Tips

- Use `arcgispro layer ""partial""` — partial name matching works
- Check `selectionCount` to see if user has features selected
- The CLI is **read-only** — it never modifies the .aprx or data
- Run from the project folder or any subfolder
";
            await File.WriteAllTextAsync(path, content, Encoding.UTF8);
        }
    }
}
