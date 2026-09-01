using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;

namespace ProExporter
{
    /// <summary>
    /// Collects state from ArcGIS Pro session.
    /// All methods that access Pro objects must run inside QueuedTask.Run().
    /// </summary>
    public static class ContextCollector
    {
        /// <summary>
        /// Collect all context from the current Pro session
        /// </summary>
        public static async Task<ExportContext> CollectAsync(ExportOptions options, CancellationToken cancellationToken = default)
        {
            ExportContext context = null;
            options ??= ExportOptions.Default;
            
            await QueuedTask.Run(() =>
            {
                context = new ExportContext
                {
                    Meta = new MetaInfo()
                };

                // Collect project info
                context.Project = CollectProjectInfo();
                if (context.Project == null)
                    return;

                // Collect maps and their contents
                var project = Project.Current;
                var maps = project.GetItems<MapProjectItem>();
                
                foreach (var mapItem in maps)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var map = mapItem.GetMap();
                    if (map == null) continue;

                    var isActiveMap = MapView.Active?.Map?.Name == map.Name;
                    if (!options.ShouldIncludeMap(map.Name, isActiveMap))
                        continue;

                    var mapInfo = CollectMapInfo(map);
                    context.Maps.Add(mapInfo);

                    // Collect layers
                    var layers = CollectLayerInfo(map, options);
                    context.Layers.AddRange(layers);

                    // Collect standalone tables
                    var tables = CollectTableInfo(map, options);
                    context.Tables.AddRange(tables);
                }

                context.Project.MapNames = context.Maps.Select(m => m.Name).ToList();

                // Best-effort stable IDs for maps/layers/tables
                AssignStableIds(context);

                // Collect layouts
                var layouts = project.GetItems<LayoutProjectItem>();
                foreach (var layoutItem in layouts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var layout = layoutItem.GetLayout();
                    if (layout == null) continue;

                    var layoutInfo = CollectLayoutInfo(layout);
                    context.Layouts.Add(layoutInfo);
                }

                // Collect database connections
                context.Connections = CollectConnections();

                // Collect notebooks (if enabled)
                if (options.ExportNotebooks)
                {
                    context.Notebooks = CollectNotebooks();
                }

                // Collect geoprocessing history (best-effort)
                context.Geoprocessing = GeoprocessingCollector.Collect();
            });
            
            return context ?? new ExportContext { Meta = new MetaInfo() };
        }

        private static void AssignStableIds(ExportContext context)
        {
            try
            {
                var projectUri = context?.Project?.Path ?? "";

                // Maps
                var mapByName = new Dictionary<string, MapInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var m in (context?.Maps ?? new List<MapInfo>()))
                {
                    if (m == null) continue;
                    m.Id = StableIds.ForMap(projectUri, m);
                    if (!string.IsNullOrWhiteSpace(m.Name))
                        mapByName[m.Name] = m;
                }

                // Layers
                foreach (var l in (context?.Layers ?? new List<LayerInfo>()))
                {
                    if (l == null) continue;
                    var layerPath = BuildLayerPath(l);
                    l.Id = StableIds.ForLayer(projectUri, l, layerPath);
                }

                // Tables
                foreach (var t in (context?.Tables ?? new List<TableInfo>()))
                {
                    if (t == null) continue;
                    t.Id = StableIds.ForTable(projectUri, t);
                }
            }
            catch
            {
                // Non-critical
            }
        }

        private static string BuildLayerPath(LayerInfo layer)
        {
            // For now we only have ParentGroupLayer (single level) and Name.
            // This keeps the ID stable for many cases, and DataSourcePath covers the rest.
            try
            {
                if (layer == null) return "";
                if (!string.IsNullOrWhiteSpace(layer.ParentGroupLayer))
                    return $"{layer.ParentGroupLayer}/{layer.Name}";
                return layer.Name ?? "";
            }
            catch
            {
                return layer?.Name ?? "";
            }
        }

        /// <summary>
        /// Collect project-level information
        /// </summary>
        private static ProjectInfo CollectProjectInfo()
        {
            var project = Project.Current;
            if (project == null)
                return null;

            var info = new ProjectInfo
            {
                Name = project.Name,
                Path = project.URI,
                DefaultGeodatabase = project.DefaultGeodatabasePath,
                DefaultToolbox = project.DefaultToolboxPath
            };

            // Collect map names
            var maps = project.GetItems<MapProjectItem>();
            info.MapNames = maps.Select(m => m.Name).ToList();

            // Collect layout names
            var layouts = project.GetItems<LayoutProjectItem>();
            info.LayoutNames = layouts.Select(l => l.Name).ToList();

            return info;
        }

        /// <summary>
        /// Collect map-level information
        /// </summary>
        private static MapInfo CollectMapInfo(Map map)
        {
            var activeMapView = MapView.Active;
            var isActive = activeMapView?.Map?.Name == map.Name;

            var info = new MapInfo
            {
                Name = map.Name,
                MapType = map.MapType.ToString(),
                LayerCount = map.Layers.Count,
                StandaloneTableCount = map.StandaloneTables.Count,
                IsActiveMap = isActive
            };

            // Spatial reference
            var sr = map.SpatialReference;
            if (sr != null)
            {
                info.SpatialReferenceName = sr.Name;
                info.SpatialReferenceWkid = sr.Wkid;
            }

            // Get extent and scale from active view if this is the active map
            if (isActive && activeMapView != null)
            {
                var extent = activeMapView.Extent;
                if (extent != null)
                {
                    info.Extent = new ExtentInfo
                    {
                        XMin = extent.XMin,
                        YMin = extent.YMin,
                        XMax = extent.XMax,
                        YMax = extent.YMax,
                        SpatialReferenceWkid = extent.SpatialReference?.Wkid
                    };
                }
                info.Scale = activeMapView.Camera?.Scale;
            }

            return info;
        }

        /// <summary>
        /// Collect layer information from a map
        /// </summary>
        private static List<LayerInfo> CollectLayerInfo(Map map, ExportOptions options)
        {
            var layers = new List<LayerInfo>();
            
            foreach (var layer in map.GetLayersAsFlattenedList())
            {
                var info = new LayerInfo
                {
                    Name = layer.Name,
                    MapName = map.Name,
                    LayerType = layer.GetType().Name,
                    IsVisible = layer.IsVisible,
                    IsBroken = !layer.ConnectionStatus.HasFlag(ConnectionStatus.Connected)
                };

                // Get parent group layer if any
                var parent = layer.Parent as GroupLayer;
                if (parent != null)
                {
                    info.ParentGroupLayer = parent.Name;
                }

                // Feature layer specific properties
                if (layer is FeatureLayer featureLayer)
                {
                    CollectFeatureLayerInfo(featureLayer, info, options);
                }
                // Raster layer
                else if (layer is RasterLayer rasterLayer)
                {
                    info.DataSourcePath = GetDataSourcePath(rasterLayer);
                    info.DataSourceKind = NormalizeDataSourceKind(null, info.DataSourcePath, "raster");
                }
                // Group layer
                else if (layer is GroupLayer groupLayer)
                {
                    // Group layers don't have data sources
                }

                layers.Add(info);
            }

            return layers;
        }

        /// <summary>
        /// Collect feature layer specific information
        /// </summary>
        private static void CollectFeatureLayerInfo(FeatureLayer featureLayer, LayerInfo info, ExportOptions options)
        {
            var exportFields = options.ExportFields;
            var sampleRowCount = options.SampleRowCount;
            var exportFastSchema = options.ExportFastSchema;
            info.IsEditable = featureLayer.IsEditable;
            info.DefinitionQuery = featureLayer.DefinitionQuery;

            // Geometry type
            var shapeType = featureLayer.ShapeType;
            info.GeometryType = shapeType.ToString();

            // Data source
            try
            {
                using (var fc = featureLayer.GetFeatureClass())
                {
                    if (fc != null)
                    {
                        var dataStore = fc.GetDatastore();
                        info.DataSourcePath = GetDataStorePath(dataStore);
                        info.DataSourceType = GetDataStoreType(dataStore);
                        info.DataSourceKind = NormalizeDataSourceKind(info.DataSourceType, info.DataSourcePath, null);

                        if (!exportFastSchema)
                        {
                            // Feature count (can be slow for large datasets)
                            try
                            {
                                info.FeatureCount = fc.GetCount();
                            }
                            catch
                            {
                                // Count may fail for some data sources
                            }
                        }

                        // Fields (if enabled)
                        if (exportFields)
                        {
                            var fcDef = fc.GetDefinition();
                            info.Fields = exportFastSchema ? CollectFieldInfoFast(fcDef) : CollectFieldInfo(fcDef);
                        }

                        // Sample data (if enabled)
                        if (!exportFastSchema && sampleRowCount > 0)
                        {
                            try
                            {
                                info.SampleData = CollectSampleDataFromFeatureClass(fc, sampleRowCount, options.SampleGeometry);
                            }
                            catch
                            {
                                // Sample data collection may fail
                            }
                        }

                        // Field statistics (if enabled)
                        if (options.ExportFieldStats && info.Fields != null && info.Fields.Count > 0)
                        {
                            try
                            {
                                CollectFieldStatistics(fc, info.Fields, options.FieldStatsMaxRows);
                            }
                            catch
                            {
                                // Stats collection may fail
                            }
                        }
                    }
                }
            }
            catch
            {
                // Data source may not be accessible
            }

            // Selection count
            if (!exportFastSchema)
            {
                try
                {
                    var selection = featureLayer.GetSelection();
                    info.SelectionCount = selection?.GetCount() ?? 0;
                }
                catch
                {
                    // Selection may fail
                }
            }

            if (string.IsNullOrWhiteSpace(info.DataSourceKind))
            {
                info.DataSourceKind = NormalizeDataSourceKind(info.DataSourceType, info.DataSourcePath, null);
            }

            // Renderer info
            try
            {
                var renderer = featureLayer.GetRenderer();
                if (renderer != null)
                {
                    info.RendererType = renderer.GetType().Name;
                    
                    // Get primary renderer field for common renderer types
                    if (renderer is CIMUniqueValueRenderer uvr)
                    {
                        info.RendererField = uvr.Fields?.FirstOrDefault();
                    }
                    else if (renderer is CIMClassBreaksRenderer cbr)
                    {
                        info.RendererField = cbr.Field;
                    }
                }
            }
            catch
            {
                // Renderer access may fail
            }

            // Joins - Skip for now as API may vary
            // try
            // {
            //     var joins = featureLayer.GetJoin();
            //     if (joins != null)
            //     {
            //         info.JoinedTables.Add(joins.JoinTable.Name);
            //     }
            // }
            // catch { }
        }

        /// <summary>
        /// Collect field information from a feature class definition
        /// </summary>
        private static List<FieldInfo> CollectFieldInfo(FeatureClassDefinition fcDef)
        {
            var fields = new List<FieldInfo>();
            
            foreach (var field in fcDef.GetFields())
            {
                var fieldInfo = new FieldInfo
                {
                    Name = field.Name,
                    Alias = field.AliasName,
                    FieldType = field.FieldType.ToString(),
                    Length = field.Length,
                    IsNullable = field.IsNullable,
                    IsEditable = field.IsEditable
                };

                var domain = field.GetDomain();
                if (domain != null)
                {
                    fieldInfo.DomainName = domain.GetName();
                }

                fields.Add(fieldInfo);
            }

            return fields;
        }

        /// <summary>
        /// Collect field information in "fast schema" mode (names/types only, no extras)
        /// </summary>
        private static List<FieldInfo> CollectFieldInfoFast(FeatureClassDefinition fcDef)
        {
            var fields = new List<FieldInfo>();

            foreach (var field in fcDef.GetFields())
            {
                var info = new FieldInfo
                {
                    Name = field.Name,
                    FieldType = field.FieldType.ToString(),
                    Length = field.Length,
                    IsNullable = field.IsNullable,
                };

                try
                {
                    var domain = field.GetDomain();
                    if (domain != null)
                        info.DomainName = domain.GetName();
                }
                catch { }

                fields.Add(info);
            }

            return fields;
        }

        /// <summary>
        /// Collect table field information in "fast schema" mode (names/types only, no extras)
        /// </summary>
        private static List<FieldInfo> CollectTableFieldInfoFast(TableDefinition tblDef)
        {
            var fields = new List<FieldInfo>();

            foreach (var field in tblDef.GetFields())
            {
                var info = new FieldInfo
                {
                    Name = field.Name,
                    FieldType = field.FieldType.ToString(),
                    Length = field.Length,
                    IsNullable = field.IsNullable,
                };

                try
                {
                    var domain = field.GetDomain();
                    if (domain != null)
                        info.DomainName = domain.GetName();
                }
                catch { }

                fields.Add(info);
            }

            return fields;
        }

        /// <summary>
        /// Collect standalone table information from a map
        /// </summary>
        private static List<TableInfo> CollectTableInfo(Map map, ExportOptions options)
        {
            var exportFields = options.ExportFields;
            var sampleRowCount = options.SampleRowCount;
            var exportFastSchema = options.ExportFastSchema;
            var tables = new List<TableInfo>();
            
            foreach (var table in map.StandaloneTables)
            {
                var info = new TableInfo
                {
                    Name = table.Name,
                    MapName = map.Name,
                    IsBroken = !table.ConnectionStatus.HasFlag(ConnectionStatus.Connected),
                    DefinitionQuery = table.DefinitionQuery
                };

                try
                {
                    using (var tbl = table.GetTable())
                    {
                        if (tbl != null)
                        {
                            var dataStore = tbl.GetDatastore();
                            info.DataSourcePath = GetDataStorePath(dataStore);
                            info.DataSourceType = GetDataStoreType(dataStore);
                            info.DataSourceKind = NormalizeDataSourceKind(info.DataSourceType, info.DataSourcePath, null);

                            if (!exportFastSchema)
                            {
                                try
                                {
                                    info.RowCount = tbl.GetCount();
                                }
                                catch { }
                            }

                            if (exportFields)
                            {
                                var tblDef = tbl.GetDefinition();
                                info.Fields = exportFastSchema ? CollectTableFieldInfoFast(tblDef) : CollectTableFieldInfo(tblDef);
                            }

                            // Sample data (if enabled)
                            if (!exportFastSchema && sampleRowCount > 0)
                            {
                                try
                                {
                                    info.SampleData = CollectSampleDataFromTable(tbl, sampleRowCount);
                                }
                                catch
                                {
                                    // Sample data collection may fail
                                }
                            }

                            // Field statistics (if enabled)
                            if (options.ExportFieldStats && info.Fields != null && info.Fields.Count > 0)
                            {
                                try
                                {
                                    CollectFieldStatisticsFromTable(tbl, info.Fields, options.FieldStatsMaxRows);
                                }
                                catch
                                {
                                    // Stats collection may fail
                                }
                            }
                        }
                    }
                }
                catch { }

                if (string.IsNullOrWhiteSpace(info.DataSourceKind))
                {
                    info.DataSourceKind = NormalizeDataSourceKind(info.DataSourceType, info.DataSourcePath, null);
                }

                tables.Add(info);
            }

            return tables;
        }

        /// <summary>
        /// Collect field information from a table definition
        /// </summary>
        private static List<FieldInfo> CollectTableFieldInfo(TableDefinition tblDef)
        {
            var fields = new List<FieldInfo>();
            
            foreach (var field in tblDef.GetFields())
            {
                var info = new FieldInfo
                {
                    Name = field.Name,
                    Alias = field.AliasName,
                    FieldType = field.FieldType.ToString(),
                    Length = field.Length,
                    IsNullable = field.IsNullable,
                    IsEditable = field.IsEditable
                };

                try
                {
                    var domain = field.GetDomain();
                    if (domain != null)
                        info.DomainName = domain.GetName();
                }
                catch { }

                fields.Add(info);
            }

            return fields;
        }

        /// <summary>
        /// Collect layout information
        /// </summary>
        private static LayoutInfo CollectLayoutInfo(ArcGIS.Desktop.Layouts.Layout layout)
        {
            var info = new LayoutInfo
            {
                Name = layout.Name
            };

            var page = layout.GetPage();
            if (page != null)
            {
                info.PageWidth = page.Width;
                info.PageHeight = page.Height;
                info.PageUnits = page.Units.ToString();
            }

            // Collect map frame names
            foreach (var element in layout.GetElements())
            {
                if (element is ArcGIS.Desktop.Layouts.MapFrame mapFrame)
                {
                    info.MapFrames.Add(new MapFrameInfo
                    {
                        Name = mapFrame.Name,
                        MapName = mapFrame.Map?.Name
                    });
                    info.MapFrameNames.Add(mapFrame.Name);
                }
            }

            return info;
        }

        /// <summary>
        /// Collect database/folder connections
        /// </summary>
        private static List<ConnectionInfo> CollectConnections()
        {
            var connections = new List<ConnectionInfo>();
            var project = Project.Current;
            if (project == null) return connections;

            // Get all project items and filter by type
            try
            {
                // Database connections (geodatabases)
                foreach (var item in project.GetItems<Item>())
                {
                    if (item.TypeID == "database_geodb")
                    {
                        connections.Add(new ConnectionInfo
                        {
                            Name = item.Name,
                            Path = item.Path,
                            ConnectionType = "Geodatabase"
                        });
                    }
                    else if (item.TypeID == "folder_conn")
                    {
                        connections.Add(new ConnectionInfo
                        {
                            Name = item.Name,
                            Path = item.Path,
                            ConnectionType = "Folder"
                        });
                    }
                }
            }
            catch
            {
                // Connection enumeration may fail
            }

            return connections;
        }

        /// <summary>
        /// Get path from a raster layer
        /// </summary>
        private static string GetDataSourcePath(RasterLayer rasterLayer)
        {
            try
            {
                // Raster layer data source path extraction
                return rasterLayer.Name; // Simplified - full path extraction is more complex
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get path from a datastore
        /// </summary>
        private static string GetDataStorePath(Datastore dataStore)
        {
            if (dataStore == null) return null;

            try
            {
                if (dataStore is Geodatabase gdb)
                {
                    var connector = gdb.GetConnector();
                    if (connector is FileGeodatabaseConnectionPath fgdb)
                        return fgdb.Path.LocalPath;
                    if (connector is DatabaseConnectionFile dbConn)
                        return dbConn.Path.LocalPath;
                }
                else if (dataStore is FileSystemDatastore fsds)
                {
                    var connector = fsds.GetConnector();
                    if (connector is FileSystemConnectionPath fscp)
                        return fscp.Path.LocalPath;
                }
            }
            catch { }

            return dataStore.GetConnectionString();
        }

        /// <summary>
        /// Get type description for a datastore
        /// </summary>
        private static string GetDataStoreType(Datastore dataStore)
        {
            if (dataStore == null) return "Unknown";

            try
            {
                if (dataStore is Geodatabase gdb)
                {
                    var connector = gdb.GetConnector();
                    if (connector is FileGeodatabaseConnectionPath)
                        return "FileGDB";
                    if (connector is DatabaseConnectionFile)
                        return "EnterpriseGDB";
                }
                else if (dataStore is FileSystemDatastore)
                {
                    return "FileSystem";
                }
            }
            catch { }

            return dataStore.GetType().Name;
        }

        /// <summary>
        /// Normalize data source kind for downstream tooling
        /// </summary>
        private static string NormalizeDataSourceKind(string dataSourceType, string dataSourcePath, string layerTypeHint)
        {
            if (!string.IsNullOrWhiteSpace(layerTypeHint) && layerTypeHint.Equals("raster", StringComparison.OrdinalIgnoreCase))
            {
                return "raster";
            }

            var path = dataSourcePath ?? "";
            var type = dataSourceType ?? "";

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                path.IndexOf("FeatureServer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("MapServer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("arcgis.com", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "service";
            }

            if (path.EndsWith(".shp", StringComparison.OrdinalIgnoreCase))
            {
                return "shapefile";
            }

            if (type.Equals("FileGDB", StringComparison.OrdinalIgnoreCase))
            {
                return "file_gdb";
            }

            if (type.Equals("EnterpriseGDB", StringComparison.OrdinalIgnoreCase))
            {
                return "enterprise_gdb";
            }

            return "unknown";
        }

        /// <summary>
        /// Collect notebooks from the project
        /// </summary>
        private static List<NotebookInfo> CollectNotebooks()
        {
            var notebooks = new List<NotebookInfo>();
            var project = Project.Current;
            if (project == null) return notebooks;

            try
            {
                // Get all project items and filter for notebooks
                foreach (var item in project.GetItems<Item>())
                {
                    // Check for notebook items by extension or type
                    if (item.Path != null && 
                        (item.Path.EndsWith(".ipynb", StringComparison.OrdinalIgnoreCase) ||
                         item.TypeID?.Contains("notebook", StringComparison.OrdinalIgnoreCase) == true))
                    {
                        var notebookInfo = ParseNotebook(item.Name, item.Path);
                        if (notebookInfo != null)
                        {
                            notebooks.Add(notebookInfo);
                        }
                    }
                }
            }
            catch
            {
                // Notebook enumeration may fail
            }

            return notebooks;
        }

        /// <summary>
        /// Parse a notebook file to extract metadata and description
        /// </summary>
        private static NotebookInfo ParseNotebook(string name, string path)
        {
            var info = new NotebookInfo
            {
                Name = name,
                Path = path
            };

            try
            {
                // Get file info
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists)
                {
                    info.LastModified = fileInfo.LastWriteTimeUtc;

                    // Read and parse the notebook JSON
                    var json = File.ReadAllText(path);
                    using (var doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        
                        if (root.TryGetProperty("cells", out var cells) && cells.ValueKind == JsonValueKind.Array)
                        {
                            info.CellCount = cells.GetArrayLength();
                            
                            // Count cells by type
                            string firstMarkdownContent = null;
                            string firstCodeContent = null;

                            foreach (var cell in cells.EnumerateArray())
                            {
                                if (cell.TryGetProperty("cell_type", out var cellType))
                                {
                                    var type = cellType.GetString() ?? "unknown";
                                    
                                    if (info.CellBreakdown.ContainsKey(type))
                                        info.CellBreakdown[type]++;
                                    else
                                        info.CellBreakdown[type] = 1;

                                    // Capture first markdown or code cell for description
                                    if (cell.TryGetProperty("source", out var source))
                                    {
                                        var content = GetCellContent(source);
                                        
                                        if (type == "markdown" && firstMarkdownContent == null && !string.IsNullOrWhiteSpace(content))
                                        {
                                            firstMarkdownContent = content;
                                        }
                                        else if (type == "code" && firstCodeContent == null && !string.IsNullOrWhiteSpace(content))
                                        {
                                            firstCodeContent = content;
                                        }
                                    }
                                }
                            }

                            // Use first markdown cell, fall back to first code cell
                            var description = firstMarkdownContent ?? firstCodeContent;
                            if (description != null)
                            {
                                // Truncate to ~500 chars
                                info.Description = description.Length > 500 
                                    ? description.Substring(0, 497) + "..." 
                                    : description;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Failed to parse notebook, return basic info
            }

            return info;
        }

        /// <summary>
        /// Extract cell content from source property (can be string or array of strings)
        /// </summary>
        private static string GetCellContent(JsonElement source)
        {
            if (source.ValueKind == JsonValueKind.String)
            {
                return source.GetString();
            }
            else if (source.ValueKind == JsonValueKind.Array)
            {
                var lines = new List<string>();
                foreach (var line in source.EnumerateArray())
                {
                    if (line.ValueKind == JsonValueKind.String)
                    {
                        lines.Add(line.GetString());
                    }
                }
                return string.Join("", lines);
            }
            return null;
        }

        /// <summary>
        /// Collect sample data rows from a feature class. Geometry is included only when
        /// includeGeometry is true (off by default to keep manifests small).
        /// </summary>
        private static List<SampleRow> CollectSampleDataFromFeatureClass(FeatureClass fc, int maxRows, bool includeGeometry = false)
        {
            var samples = new List<SampleRow>();
            if (maxRows <= 0) return samples;

            try
            {
                var fcDef = fc.GetDefinition();
                var fields = fcDef.GetFields();
                
                using (var cursor = fc.Search())
                {
                    int count = 0;
                    while (cursor.MoveNext() && count < maxRows)
                    {
                        using (var row = cursor.Current as Feature)
                        {
                            if (row == null) continue;

                            var sampleRow = new SampleRow();
                            
                            // Collect attributes
                            for (int i = 0; i < fields.Count; i++)
                            {
                                var field = fields[i];
                                if (field.Name.Equals("Shape", StringComparison.OrdinalIgnoreCase))
                                    continue; // Skip geometry field in attributes
                                
                                try
                                {
                                    var value = row[i];
                                    sampleRow.Attributes[field.Name] = FormatAttributeValue(value);
                                }
                                catch
                                {
                                    sampleRow.Attributes[field.Name] = null;
                                }
                            }

                            // Convert geometry to GeoJSON (only when explicitly enabled)
                            if (includeGeometry)
                            {
                                try
                                {
                                    var geom = row.GetShape();
                                    if (geom != null && !geom.IsEmpty)
                                    {
                                        sampleRow.Geometry = GeometryToGeoJson(geom);
                                    }
                                }
                                catch
                                {
                                    // Geometry conversion may fail
                                }
                            }

                            samples.Add(sampleRow);
                            count++;
                        }
                    }
                }
            }
            catch
            {
                // Sample collection may fail
            }

            return samples;
        }

        /// <summary>
        /// Collect sample data rows from a standalone table (no geometry)
        /// </summary>
        private static List<SampleRow> CollectSampleDataFromTable(Table table, int maxRows)
        {
            var samples = new List<SampleRow>();
            if (maxRows <= 0) return samples;

            try
            {
                var tableDef = table.GetDefinition();
                var fields = tableDef.GetFields();
                
                using (var cursor = table.Search())
                {
                    int count = 0;
                    while (cursor.MoveNext() && count < maxRows)
                    {
                        using (var row = cursor.Current)
                        {
                            if (row == null) continue;

                            var sampleRow = new SampleRow();
                            
                            // Collect attributes
                            for (int i = 0; i < fields.Count; i++)
                            {
                                var field = fields[i];
                                try
                                {
                                    var value = row[i];
                                    sampleRow.Attributes[field.Name] = FormatAttributeValue(value);
                                }
                                catch
                                {
                                    sampleRow.Attributes[field.Name] = null;
                                }
                            }

                            samples.Add(sampleRow);
                            count++;
                        }
                    }
                }
            }
            catch
            {
                // Sample collection may fail
            }

            return samples;
        }

        /// <summary>
        /// Compute per-field statistics from a feature class and populate FieldInfo.Stats
        /// </summary>
        private static void CollectFieldStatistics(FeatureClass fc, List<FieldInfo> fieldInfos, int maxRows)
        {
            var fcDef = fc.GetDefinition();
            var fields = fcDef.GetFields();
            ComputeFieldStatistics(fc, fields, fieldInfos, maxRows);
        }

        /// <summary>
        /// Compute per-field statistics from a standalone table and populate FieldInfo.Stats
        /// </summary>
        private static void CollectFieldStatisticsFromTable(Table table, List<FieldInfo> fieldInfos, int maxRows)
        {
            var tblDef = table.GetDefinition();
            var fields = tblDef.GetFields();
            ComputeFieldStatistics(table, fields, fieldInfos, maxRows);
        }

        private static readonly HashSet<string> NumericFieldTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SmallInteger", "Integer", "Single", "Double", "OID", "BigInteger"
        };

        private static readonly HashSet<string> StringFieldTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "String", "GUID", "GlobalID", "XML"
        };

        private static readonly HashSet<string> DateFieldTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Date", "DateOnly", "TimeOnly", "TimestampOffset"
        };

        /// <summary>
        /// Scan rows and compute summary statistics for each field
        /// </summary>
        private static void ComputeFieldStatistics(Table table, IReadOnlyList<Field> fields, List<FieldInfo> fieldInfos, int maxRows)
        {
            // Build lookup from field name to FieldInfo
            var fieldMap = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var fi in fieldInfos)
                fieldMap[fi.Name] = fi;

            // Build per-field accumulators (skip Shape and fields not in our FieldInfo list)
            var accumulators = new List<FieldAccumulator>();
            var fieldIndices = new List<int>();
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (field.Name.Equals("Shape", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!fieldMap.ContainsKey(field.Name))
                    continue;

                var ft = field.FieldType.ToString();
                accumulators.Add(new FieldAccumulator(field.Name, ft));
                fieldIndices.Add(i);
            }

            if (accumulators.Count == 0)
                return;

            // Scan rows
            using (var cursor = table.Search())
            {
                int rowsRead = 0;
                while (cursor.MoveNext() && (maxRows <= 0 || rowsRead < maxRows))
                {
                    using (var row = cursor.Current)
                    {
                        if (row == null) continue;

                        for (int j = 0; j < accumulators.Count; j++)
                        {
                            try
                            {
                                var value = row[fieldIndices[j]];
                                accumulators[j].Add(value);
                            }
                            catch
                            {
                                accumulators[j].Add(null);
                            }
                        }
                        rowsRead++;
                    }
                }
            }

            // Finalize and assign stats to FieldInfo objects
            foreach (var acc in accumulators)
            {
                if (fieldMap.TryGetValue(acc.FieldName, out var fi))
                {
                    fi.Stats = acc.ToStatistics();
                }
            }
        }

        /// <summary>
        /// Accumulates values for a single field during row scan
        /// </summary>
        private class FieldAccumulator
        {
            public string FieldName { get; }
            private readonly string _fieldType;

            private long _count;
            private long _nullCount;

            // Numeric
            private double _sum;
            private double _sumSq;
            private double _min = double.MaxValue;
            private double _max = double.MinValue;
            private long _numericCount;

            // String top values
            private Dictionary<string, int> _valueCounts;
            private bool _isString;

            // Date
            private DateTime _minDate = DateTime.MaxValue;
            private DateTime _maxDate = DateTime.MinValue;
            private bool _isDate;
            private long _dateCount;

            // Unique tracking (for all types)
            private HashSet<string> _uniqueValues;
            private bool _uniqueOverflow;
            private const int MaxUniqueTracked = 10000;

            public FieldAccumulator(string fieldName, string fieldType)
            {
                FieldName = fieldName;
                _fieldType = fieldType;
                _isString = StringFieldTypes.Contains(fieldType);
                _isDate = DateFieldTypes.Contains(fieldType);

                if (_isString)
                    _valueCounts = new Dictionary<string, int>(StringComparer.Ordinal);

                _uniqueValues = new HashSet<string>(StringComparer.Ordinal);
            }

            public void Add(object value)
            {
                _count++;

                if (value == null || value is DBNull)
                {
                    _nullCount++;
                    return;
                }

                // Track uniques (up to limit)
                if (!_uniqueOverflow)
                {
                    var key = value.ToString();
                    _uniqueValues.Add(key);
                    if (_uniqueValues.Count > MaxUniqueTracked)
                        _uniqueOverflow = true;
                }

                if (NumericFieldTypes.Contains(_fieldType))
                {
                    if (TryToDouble(value, out double d))
                    {
                        _numericCount++;
                        _sum += d;
                        _sumSq += d * d;
                        if (d < _min) _min = d;
                        if (d > _max) _max = d;
                    }
                }
                else if (_isString)
                {
                    var s = value.ToString();
                    if (_valueCounts.ContainsKey(s))
                        _valueCounts[s]++;
                    else if (_valueCounts.Count < MaxUniqueTracked)
                        _valueCounts[s] = 1;
                }
                else if (_isDate)
                {
                    DateTime dt;
                    if (value is DateTime d)
                        dt = d;
                    else if (value is DateTimeOffset dto)
                        dt = dto.UtcDateTime;
                    else
                        return;

                    _dateCount++;
                    if (dt < _minDate) _minDate = dt;
                    if (dt > _maxDate) _maxDate = dt;
                }
            }

            public FieldStatistics ToStatistics()
            {
                var stats = new FieldStatistics
                {
                    Count = _count,
                    NullCount = _nullCount,
                    UniqueCount = _uniqueOverflow ? null : (long?)_uniqueValues.Count
                };

                if (NumericFieldTypes.Contains(_fieldType) && _numericCount > 0)
                {
                    stats.Min = _min;
                    stats.Max = _max;
                    stats.Mean = Math.Round(_sum / _numericCount, 6);
                    if (_numericCount > 1)
                    {
                        var variance = (_sumSq / _numericCount) - (stats.Mean.Value * stats.Mean.Value);
                        stats.Std = Math.Round(Math.Sqrt(Math.Max(0, variance)), 6);
                    }
                }
                else if (_isString && _valueCounts != null && _valueCounts.Count > 0)
                {
                    stats.TopValues = _valueCounts
                        .OrderByDescending(kv => kv.Value)
                        .Take(5)
                        .Select(kv => kv.Key)
                        .ToList();
                }
                else if (_isDate && _dateCount > 0)
                {
                    stats.MinDate = _minDate.ToString("o");
                    stats.MaxDate = _maxDate.ToString("o");
                }

                return stats;
            }

            private static bool TryToDouble(object value, out double result)
            {
                if (value is double d) { result = d; return true; }
                if (value is int i) { result = i; return true; }
                if (value is long l) { result = l; return true; }
                if (value is float f) { result = f; return true; }
                if (value is short s) { result = s; return true; }
                if (value is decimal m) { result = (double)m; return true; }
                result = 0;
                return false;
            }
        }

        /// <summary>
        /// Format attribute value for JSON serialization
        /// </summary>
        private static object FormatAttributeValue(object value)
        {
            if (value == null || value is DBNull)
                return null;
            
            if (value is DateTime dt)
                return dt.ToString("o"); // ISO 8601 format
            
            if (value is DateTimeOffset dto)
                return dto.ToString("o");
            
            if (value is Guid guid)
                return guid.ToString();
            
            if (value is byte[] bytes)
                return Convert.ToBase64String(bytes);
            
            // Numbers, strings, bools serialize directly
            return value;
        }

        /// <summary>
        /// Convert ArcGIS geometry to GeoJSON object
        /// </summary>
        private static object GeometryToGeoJson(Geometry geom)
        {
            if (geom == null || geom.IsEmpty)
                return null;

            var geoJson = new Dictionary<string, object>();
            
            // Point
            if (geom is MapPoint point)
            {
                geoJson["type"] = "Point";
                geoJson["coordinates"] = new[] { point.X, point.Y };
                if (point.HasZ && !double.IsNaN(point.Z))
                {
                    geoJson["coordinates"] = new[] { point.X, point.Y, point.Z };
                }
            }
            // Multipoint
            else if (geom is Multipoint multipoint)
            {
                geoJson["type"] = "MultiPoint";
                var coords = new List<double[]>();
                foreach (var pt in multipoint.Points)
                {
                    coords.Add(new[] { pt.X, pt.Y });
                }
                geoJson["coordinates"] = coords;
            }
            // Polyline
            else if (geom is Polyline polyline)
            {
                if (polyline.PartCount == 1)
                {
                    geoJson["type"] = "LineString";
                    var coords = new List<double[]>();
                    foreach (var pt in polyline.Points)
                    {
                        coords.Add(new[] { pt.X, pt.Y });
                    }
                    geoJson["coordinates"] = coords;
                }
                else
                {
                    geoJson["type"] = "MultiLineString";
                    var parts = new List<List<double[]>>();
                    foreach (var part in polyline.Parts)
                    {
                        var coords = new List<double[]>();
                        var points = part.AsEnumerable().Select(seg => seg.StartPoint).ToList();
                        // Add the endpoint of the last segment
                        if (part.Count > 0)
                        {
                            points.Add(part[part.Count - 1].EndPoint);
                        }
                        foreach (var pt in points)
                        {
                            coords.Add(new[] { pt.X, pt.Y });
                        }
                        parts.Add(coords);
                    }
                    geoJson["coordinates"] = parts;
                }
            }
            // Polygon
            else if (geom is Polygon polygon)
            {
                if (polygon.PartCount == 1)
                {
                    geoJson["type"] = "Polygon";
                    var rings = new List<List<double[]>>();
                    foreach (var part in polygon.Parts)
                    {
                        var ring = new List<double[]>();
                        var points = part.AsEnumerable().Select(seg => seg.StartPoint).ToList();
                        // Add the endpoint of the last segment (which should close the ring)
                        if (part.Count > 0)
                        {
                            points.Add(part[part.Count - 1].EndPoint);
                        }
                        foreach (var pt in points)
                        {
                            ring.Add(new[] { pt.X, pt.Y });
                        }
                        rings.Add(ring);
                    }
                    geoJson["coordinates"] = rings;
                }
                else
                {
                    geoJson["type"] = "MultiPolygon";
                    var polygons = new List<List<List<double[]>>>();
                    foreach (var part in polygon.Parts)
                    {
                        var rings = new List<List<double[]>>();
                        var ring = new List<double[]>();
                        var points = part.AsEnumerable().Select(seg => seg.StartPoint).ToList();
                        // Add the endpoint of the last segment
                        if (part.Count > 0)
                        {
                            points.Add(part[part.Count - 1].EndPoint);
                        }
                        foreach (var pt in points)
                        {
                            ring.Add(new[] { pt.X, pt.Y });
                        }
                        rings.Add(ring);
                        polygons.Add(rings);
                    }
                    geoJson["coordinates"] = polygons;
                }
            }
            // Envelope (convert to Polygon)
            else if (geom is Envelope env)
            {
                geoJson["type"] = "Polygon";
                var ring = new List<double[]>
                {
                    new[] { env.XMin, env.YMin },
                    new[] { env.XMax, env.YMin },
                    new[] { env.XMax, env.YMax },
                    new[] { env.XMin, env.YMax },
                    new[] { env.XMin, env.YMin }
                };
                geoJson["coordinates"] = new List<List<double[]>> { ring };
            }

            return geoJson;
        }
    }
}
