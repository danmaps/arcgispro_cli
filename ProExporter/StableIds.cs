using System;
using System.Security.Cryptography;
using System.Text;

namespace ProExporter
{
    /// <summary>
    /// Best-effort stable IDs for exported objects.
    ///
    /// If the ArcGIS Pro SDK exposes a true GUID for an object, prefer that.
    /// Otherwise, derive a deterministic GUID from stable-ish properties.
    ///
    /// Notes:
    /// - IDs are stable only if the inputs stay stable.
    /// - Renaming a map/layer/group path can change the derived ID.
    /// - For data-backed layers/tables, DataSourcePath helps keep IDs stable across display-name changes.
    /// </summary>
    internal static class StableIds
    {
        // Fixed namespace GUID for this project (arbitrary but constant).
        private static readonly Guid Namespace = Guid.Parse("2b62dd2f-2b6e-4f1a-9a0d-c1f7c54fd0db");

        public static string ForMap(string projectUri, MapInfo map)
        {
            var key = $"map|project={Normalize(projectUri)}|name={Normalize(map?.Name)}|type={Normalize(map?.MapType)}";
            return GuidV5(Namespace, key).ToString();
        }

        public static string ForLayer(string projectUri, LayerInfo layer, string layerPath)
        {
            var key = $"layer|project={Normalize(projectUri)}|map={Normalize(layer?.MapName)}|path={Normalize(layerPath)}|ds={Normalize(layer?.DataSourcePath)}|type={Normalize(layer?.LayerType)}";
            return GuidV5(Namespace, key).ToString();
        }

        public static string ForTable(string projectUri, TableInfo table)
        {
            var key = $"table|project={Normalize(projectUri)}|map={Normalize(table?.MapName)}|name={Normalize(table?.Name)}|ds={Normalize(table?.DataSourcePath)}|type={Normalize(table?.DataSourceType)}";
            return GuidV5(Namespace, key).ToString();
        }

        private static string Normalize(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "";
            return v.Trim().Replace("\\", "/");
        }

        // UUIDv5-like deterministic GUID from namespace + name (SHA1).
        private static Guid GuidV5(Guid ns, string name)
        {
            var nsBytes = ns.ToByteArray();
            SwapGuidByteOrder(nsBytes);

            var nameBytes = Encoding.UTF8.GetBytes(name ?? "");

            byte[] hash;
            using (var sha1 = SHA1.Create())
            {
                sha1.TransformBlock(nsBytes, 0, nsBytes.Length, null, 0);
                sha1.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
                hash = sha1.Hash;
            }

            var newGuid = new byte[16];
            Array.Copy(hash, 0, newGuid, 0, 16);

            // Set version to 5
            newGuid[6] = (byte)((newGuid[6] & 0x0F) | (5 << 4));
            // Set variant to RFC 4122
            newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

            SwapGuidByteOrder(newGuid);
            return new Guid(newGuid);
        }

        // .NET Guid byte order differs from RFC 4122 in the first 3 fields.
        private static void SwapGuidByteOrder(byte[] guid)
        {
            void Swap(int a, int b)
            {
                var tmp = guid[a];
                guid[a] = guid[b];
                guid[b] = tmp;
            }

            Swap(0, 3);
            Swap(1, 2);
            Swap(4, 5);
            Swap(6, 7);
        }
    }
}
