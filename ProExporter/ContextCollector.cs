using System;
using System.Collections.Generic;
using System.Linq;
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
        public static async Task<ExportContext> CollectAsync(CancellationToken cancellationToken = default)
        {
            ExportContext context = null;
            
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

                    var mapInfo = CollectMapInfo(map);
                    context.Maps.Add(mapInfo);

                    // Collect layers
                    var layers = CollectLayerInfo(map);
                    context.Layers.AddRange(layers);

                    // Collect standalone tables
                    var tables = CollectTableInfo(map);
                    context.Tables.AddRange(tables);
                }

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
            });
            
            return context ?? new ExportContext { Meta = new MetaInfo() };
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
        private static List<LayerInfo> CollectLayerInfo(Map map)
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
                    CollectFeatureLayerInfo(featureLayer, info);
                }
                // Raster layer
                else if (layer is RasterLayer rasterLayer)
                {
                    info.DataSourcePath = GetDataSourcePath(rasterLayer);
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
        private static void CollectFeatureLayerInfo(FeatureLayer featureLayer, LayerInfo info)
        {
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

                        // Feature count (can be slow for large datasets)
                        try
                        {
                            info.FeatureCount = fc.GetCount();
                        }
                        catch
                        {
                            // Count may fail for some data sources
                        }

                        // Fields
                        var fcDef = fc.GetDefinition();
                        info.Fields = CollectFieldInfo(fcDef);
                    }
                }
            }
            catch
            {
                // Data source may not be accessible
            }

            // Selection count
            try
            {
                var selection = featureLayer.GetSelection();
                info.SelectionCount = selection?.GetCount() ?? 0;
            }
            catch
            {
                // Selection may fail
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
        /// Collect standalone table information from a map
        /// </summary>
        private static List<TableInfo> CollectTableInfo(Map map)
        {
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

                            try
                            {
                                info.RowCount = tbl.GetCount();
                            }
                            catch { }

                            var tblDef = tbl.GetDefinition();
                            info.Fields = CollectTableFieldInfo(tblDef);
                        }
                    }
                }
                catch { }

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
                fields.Add(new FieldInfo
                {
                    Name = field.Name,
                    Alias = field.AliasName,
                    FieldType = field.FieldType.ToString(),
                    Length = field.Length,
                    IsNullable = field.IsNullable,
                    IsEditable = field.IsEditable
                });
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
    }
}
