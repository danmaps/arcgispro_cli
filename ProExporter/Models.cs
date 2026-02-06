using System;
using System.Collections.Generic;

namespace ProExporter
{
    /// <summary>
    /// Metadata about the export itself
    /// </summary>
    public class MetaInfo
    {
        public string Version { get; set; } = "1.0";
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public string MachineName { get; set; } = Environment.MachineName;
        public string UserName { get; set; } = Environment.UserName;
    }

    /// <summary>
    /// Project-level information
    /// </summary>
    public class ProjectInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string DefaultGeodatabase { get; set; }
        public string DefaultToolbox { get; set; }
        public DateTime? LastModified { get; set; }
        public List<string> MapNames { get; set; } = new List<string>();
        public List<string> LayoutNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// Map-level information
    /// </summary>
    public class MapInfo
    {
        public string Name { get; set; }
        public string MapType { get; set; }  // "Map" or "Scene"
        public string SpatialReferenceName { get; set; }
        public int? SpatialReferenceWkid { get; set; }
        public int LayerCount { get; set; }
        public int StandaloneTableCount { get; set; }
        public ExtentInfo Extent { get; set; }
        public double? Scale { get; set; }
        public bool IsActiveMap { get; set; }
    }

    /// <summary>
    /// Spatial extent information
    /// </summary>
    public class ExtentInfo
    {
        public double XMin { get; set; }
        public double YMin { get; set; }
        public double XMax { get; set; }
        public double YMax { get; set; }
        public int? SpatialReferenceWkid { get; set; }
    }

    /// <summary>
    /// Layer information (feature layer, raster, group, etc.)
    /// </summary>
    public class LayerInfo
    {
        public string Name { get; set; }
        public string MapName { get; set; }
        public string LayerType { get; set; }  // FeatureLayer, RasterLayer, GroupLayer, etc.
        public string GeometryType { get; set; }  // Point, Polyline, Polygon, null for non-feature
        public string DataSourcePath { get; set; }
        public string DataSourceType { get; set; }  // FileGDB, Shapefile, EnterpriseGDB, etc.
        public bool IsVisible { get; set; }
        public bool IsEditable { get; set; }
        public bool IsBroken { get; set; }
        public string DefinitionQuery { get; set; }
        public string RendererType { get; set; }  // SimpleRenderer, UniqueValueRenderer, etc.
        public string RendererField { get; set; }  // Primary field driving the renderer
        public long? FeatureCount { get; set; }
        public long? SelectionCount { get; set; }
        public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();
        public List<string> JoinedTables { get; set; } = new List<string>();
        public List<string> RelatedTables { get; set; } = new List<string>();
        public string ParentGroupLayer { get; set; }
        public List<SampleRow> SampleData { get; set; } = new List<SampleRow>();
    }

    /// <summary>
    /// Field/attribute information
    /// </summary>
    public class FieldInfo
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public string FieldType { get; set; }  // String, Integer, Double, Date, etc.
        public int? Length { get; set; }
        public bool IsNullable { get; set; }
        public bool IsEditable { get; set; }
        public string DomainName { get; set; }
    }

    /// <summary>
    /// Standalone table information
    /// </summary>
    public class TableInfo
    {
        public string Name { get; set; }
        public string MapName { get; set; }
        public string DataSourcePath { get; set; }
        public string DataSourceType { get; set; }
        public bool IsBroken { get; set; }
        public string DefinitionQuery { get; set; }
        public long? RowCount { get; set; }
        public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();
        public List<SampleRow> SampleData { get; set; } = new List<SampleRow>();
    }

    /// <summary>
    /// Sample data row with attributes and optional GeoJSON geometry
    /// </summary>
    public class SampleRow
    {
        /// <summary>
        /// Dictionary of field name to value (all types serialized as appropriate JSON types)
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// GeoJSON geometry (null for tables or non-geometry layers)
        /// </summary>
        public object Geometry { get; set; }
    }

    /// <summary>
    /// Database/folder connection information
    /// </summary>
    public class ConnectionInfo
    {
        public string Name { get; set; }
        public string ConnectionType { get; set; }  // FileGDB, EnterpriseGDB, Folder, etc.
        public string Path { get; set; }
        // Note: Credentials are intentionally NOT included for security
    }

    /// <summary>
    /// Layout information
    /// </summary>
    public class LayoutInfo
    {
        public string Name { get; set; }
        public double PageWidth { get; set; }
        public double PageHeight { get; set; }
        public string PageUnits { get; set; }
        public List<string> MapFrameNames { get; set; } = new List<string>();
        public List<MapFrameInfo> MapFrames { get; set; } = new List<MapFrameInfo>();
    }

    /// <summary>
    /// Map frame information inside a layout
    /// </summary>
    public class MapFrameInfo
    {
        public string Name { get; set; }
        public string MapName { get; set; }
    }

    /// <summary>
    /// Notebook information
    /// </summary>
    public class NotebookInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public int CellCount { get; set; }
        public Dictionary<string, int> CellBreakdown { get; set; } = new Dictionary<string, int>();
        public DateTime? LastModified { get; set; }
    }

    /// <summary>
    /// Complete export context containing all collected information
    /// </summary>
    public class ExportContext
    {
        public MetaInfo Meta { get; set; } = new MetaInfo();
        public ProjectInfo Project { get; set; }
        public List<MapInfo> Maps { get; set; } = new List<MapInfo>();
        public List<LayerInfo> Layers { get; set; } = new List<LayerInfo>();
        public List<TableInfo> Tables { get; set; } = new List<TableInfo>();
        public List<ConnectionInfo> Connections { get; set; } = new List<ConnectionInfo>();
        public List<LayoutInfo> Layouts { get; set; } = new List<LayoutInfo>();
        public List<NotebookInfo> Notebooks { get; set; } = new List<NotebookInfo>();
    }

    /// <summary>
    /// Result of an export operation
    /// </summary>
    public class ExportResult
    {
        public bool Success { get; set; }
        public string OutputPath { get; set; }
        public List<string> FilesCreated { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public TimeSpan Duration { get; set; }
    }
}
