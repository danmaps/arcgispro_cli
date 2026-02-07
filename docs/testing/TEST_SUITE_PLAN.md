# ArcGIS Pro CLI test suite plan

Goal: high-confidence regression coverage for snapshot/inspect/query/dump across common ArcGIS Pro project shapes.

## 1. Test assets (project archetypes)
Each archetype is a small .aprx plus required data in a fixtures folder. Prefer reproducible creation steps.

### A. Empty + minimal
- Empty project (no maps)
- One map, one layer, default symbology

### B. Layer variety
- Feature layers: point/line/polygon, multipart, Z/M
- Raster layers (local)
- Tables (standalone)
- Group layers, nested groups
- Definition queries
- Joins/relates

### C. Data source variety
- File geodatabase
- Shapefile
- GeoPackage
- Enterprise (if available) or mocked placeholder

### D. Coordinate systems
- Mixed SR layers in one map
- Map SR differs from layer SR

### E. Layouts
- One layout with map frame
- Multiple layouts and map frames

### F. Weird but real
- Broken layer (missing source)
- Layers with long names + unicode
- Deep layer tree (performance)

## 2. CLI behavior to verify
### Snapshot
- Produces expected files (meta/project/maps/layers/etc)
- Deterministic fields where promised
- Handles broken layers without crashing

### Inspect
- Human-readable output, includes 0 counts correctly
- Caps output (e.g. first N layers) but reports totals

### Query / Dump
- Errors are actionable (exit codes, messages)
- JSON schema stable across versions

## 3. Execution strategy
- Unit tests: pure python helpers (parsers/formatters)
- Integration tests: run CLI commands against fixture exports
- Golden files: store expected JSON outputs per fixture

## 4. CI (pragmatic)
- Default CI runs unit tests only (no ArcGIS Pro dependency)
- Optional/manual job: integration tests on a Windows runner with ArcGIS Pro installed
