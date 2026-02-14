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

            // Write Mermaid diagram files
            var mermaidPath = Path.Combine(snapshotFolder, "project-structure.mmd");
            var mermaidMdPath = Path.Combine(snapshotFolder, "project-structure.md");
            await WriteMermaidDiagramAsync(mermaidPath, mermaidMdPath, context);
            files.Add(mermaidPath);
            files.Add(mermaidMdPath);

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
                    if (layout.MapFrames.Any())
                    {
                        var mapFrames = layout.MapFrames.Select(mapFrame =>
                            string.IsNullOrWhiteSpace(mapFrame.MapName)
                                ? mapFrame.Name
                                : $"{mapFrame.Name} ({mapFrame.MapName})");
                        sb.AppendLine($"- **Map Frames:** {string.Join(", ", mapFrames)}");
                    }
                    else if (layout.MapFrameNames.Any())
                    {
                        sb.AppendLine($"- **Map Frames:** {string.Join(", ", layout.MapFrameNames)}");
                    }
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
        /// Write Mermaid diagram files describing project structure
        /// </summary>
        private static async Task WriteMermaidDiagramAsync(string mermaidPath, string markdownPath, ExportContext context)
        {
            var mermaid = BuildMermaidDiagram(context);

            var md = new StringBuilder();
            md.AppendLine("# ArcGIS Pro Project Structure");
            md.AppendLine();
            md.AppendLine("```mermaid");
            md.AppendLine(mermaid);
            md.AppendLine("```");
            md.AppendLine();
            md.AppendLine("Rendered with Mermaid-compatible tools (ex: beautiful-mermaid) for visual export.");

            await File.WriteAllTextAsync(mermaidPath, mermaid, Encoding.UTF8);
            await File.WriteAllTextAsync(markdownPath, md.ToString(), Encoding.UTF8);
        }

        private static string BuildMermaidDiagram(ExportContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("flowchart LR");
            sb.AppendLine("%% ArcGIS Pro project structure");

            var projectName = context.Project?.Name ?? "ArcGIS Pro Project";
            sb.AppendLine($"project[\"Project: {EscapeLabel(projectName)}\"]");

            var layerNameCounts = context.Layers
                .GroupBy(layer => layer.Name)
                .ToDictionary(group => group.Key, group => group.Count());

            var mapNodes = new Dictionary<string, string>();
            var layoutNodes = new Dictionary<string, string>();
            var layerNodes = new Dictionary<LayerInfo, string>();
            var sharedLayerNodes = new List<string>();

            var mapIndex = 0;
            foreach (var map in context.Maps)
            {
                mapIndex++;
                var mapNode = $"map_{mapIndex}";
                mapNodes[map.Name] = mapNode;
                sb.AppendLine($"{mapNode}[\"Map: {EscapeLabel(map.Name)}\"]");
                sb.AppendLine($"project --> {mapNode}");
            }

            var layoutIndex = 0;
            foreach (var layout in context.Layouts)
            {
                layoutIndex++;
                var layoutNode = $"layout_{layoutIndex}";
                layoutNodes[layout.Name] = layoutNode;
                sb.AppendLine($"{layoutNode}[\"Layout: {EscapeLabel(layout.Name)}\"]");
                sb.AppendLine($"project --> {layoutNode}");
            }

            var layerIndex = 0;
            foreach (var map in context.Maps)
            {
                if (!mapNodes.TryGetValue(map.Name, out var mapNode))
                    continue;

                var mapLayers = context.Layers.Where(layer => layer.MapName == map.Name).ToList();
                var groupNodesByName = new Dictionary<string, string>();

                foreach (var layer in mapLayers)
                {
                    layerIndex++;
                    var layerNode = $"layer_{layerIndex}";
                    layerNodes[layer] = layerNode;

                    var layerLabel = layer.LayerType == "GroupLayer"
                        ? $"Group: {layer.Name}"
                        : $"Layer: {layer.Name}";

                    sb.AppendLine($"{layerNode}[\"{EscapeLabel(layerLabel)}\"]");

                    if (layer.LayerType == "GroupLayer" && !groupNodesByName.ContainsKey(layer.Name))
                    {
                        groupNodesByName[layer.Name] = layerNode;
                    }

                    if (layerNameCounts.TryGetValue(layer.Name, out var count) && count > 1)
                    {
                        sharedLayerNodes.Add(layerNode);
                    }
                }

                foreach (var layer in mapLayers)
                {
                    var layerNode = layerNodes[layer];
                    if (!string.IsNullOrEmpty(layer.ParentGroupLayer) &&
                        groupNodesByName.TryGetValue(layer.ParentGroupLayer, out var parentNode))
                    {
                        sb.AppendLine($"{parentNode} --> {layerNode}");
                    }
                    else
                    {
                        sb.AppendLine($"{mapNode} --> {layerNode}");
                    }
                }
            }

            foreach (var layout in context.Layouts)
            {
                if (!layoutNodes.TryGetValue(layout.Name, out var layoutNode))
                    continue;

                foreach (var mapFrame in layout.MapFrames)
                {
                    if (string.IsNullOrWhiteSpace(mapFrame.MapName))
                        continue;

                    if (mapNodes.TryGetValue(mapFrame.MapName, out var mapNode))
                    {
                        sb.AppendLine($"{layoutNode} --> {mapNode}");
                    }
                }
            }

            if (sharedLayerNodes.Any())
            {
                sb.AppendLine("classDef sharedLayer fill:#fff4cc,stroke:#d39e00,stroke-width:2px;");
                sb.AppendLine($"class {string.Join(",", sharedLayerNodes.Distinct())} sharedLayer;");
            }

            return sb.ToString().TrimEnd();
        }

        private static string EscapeLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return string.Empty;

            return label
                .Replace("\"", "'")
                .Replace("[", "(")
                .Replace("]", ")")
                .Replace("\r", " ")
                .Replace("\n", " ");
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
| `arcgispro diagram` | Render Mermaid diagram of project structure |
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
        ├── context.md      # Human-readable summary
        ├── project-structure.mmd # Mermaid diagram source
        └── project-structure.md  # Mermaid diagram markdown
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

## Task Checkpoints (for agent-driven work)

If you generate **analysis or automation artifacts** for a specific request/task (scripts, notebooks, conclusions, data transforms), write a small checkpoint note **once per task** and commit it alongside the work.

- Filename: `checkpoint_YYYYMMDDTHHMMSSZ.md` (UTC, filesystem-safe)
- Location: project root (same folder as this `AGENTS.md`)

Template:

```md
# Checkpoint (YYYY-MM-DDTHH:MM:SSZ)

## Intent
What question are we answering?

## Inputs
- Datasets:
- Layer names:
- Environment assumptions:

## Constraints
- Accuracy:
- Performance:
- Governance / access rules:

## Decisions
- Key forks taken and why
- Alternatives considered

## Validation
- Counts checked:
- Spot checks:
- Maps reviewed:

## Next
What you’d do if this was asked again.
```

Notes:
- This is **not** required for every conversation turn.
- This is **not** required for every commit.
- It is required when the agent’s work would otherwise be hard to audit or reproduce.

## Tips

- Use `arcgispro layer ""partial""` — partial name matching works
- Check `selectionCount` to see if user has features selected
- The CLI is **read-only** — it never modifies the .aprx or data
- Run from the project folder or any subfolder

## Advisors (Paid)

Proprietary AI advisors are available for deeper analysis:

- **Preflight** — Catches brittle data sources, credential issues, and endpoint resilience before scripting
- **Approach** — Validates mixed spatial references, joins/relates, and suggests reliable automation strategies

Access via: https://api.x402layer.cc/e/esri-smells

Run `arcgispro inspect` to see context-aware suggestions for when to use these advisors.
";
            await File.WriteAllTextAsync(path, content, Encoding.UTF8);
        }
    }
}
