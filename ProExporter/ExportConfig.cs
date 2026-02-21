using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ProExporter
{
    /// <summary>
    /// Configuration loaded from .arcgispro/config.yml
    /// </summary>
    public class ExportConfig
    {
        public bool AutoExportEnabled { get; set; } = false;
        public bool AutoExportLocalOnly { get; set; } = true;
        public int AutoExportMaxLayers { get; set; } = 50;
        public bool ExportImages { get; set; } = true;
        public bool ExportNotebooks { get; set; } = true;
        public bool ExportFields { get; set; } = true;
        public bool ExportFastSchema { get; set; } = false;
        public int SampleRowCount { get; set; } = 10;

        /// <summary>
        /// Load config from .arcgispro/config.yml or return defaults
        /// </summary>
        public static ExportConfig Load(string arcgisproFolder)
        {
            var configPath = Path.Combine(arcgisproFolder, "config.yml");
            
            if (!File.Exists(configPath))
            {
                // Create default config
                var config = new ExportConfig();
                config.Save(configPath);
                return config;
            }

            try
            {
                var yaml = File.ReadAllText(configPath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                return deserializer.Deserialize<ExportConfig>(yaml);
            }
            catch
            {
                // On error, return defaults
                return new ExportConfig();
            }
        }

        /// <summary>
        /// Save config to file
        /// </summary>
        public void Save(string path)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(this);
            
            // Add comments
            var commented = @"# ArcGIS Pro CLI Export Configuration
# This file controls how the add-in exports data

# Auto-export on project open (default: false)
" + yaml;
            
            // Ensure directory exists before writing
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(path, commented);
        }
    }
}
