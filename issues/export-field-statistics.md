# Export field-level statistics instead of (or alongside) raw sample data

## Problem

`layers.json` grows huge when projects contain layers with many fields. In a real-world project with a handful of layers (including an enterprise feature class with millions of rows and hundreds of fields), `layers.json` hit **~78 MB** — too large for agents to read, and the raw sample data (first N rows) it contains is often less useful than summary stats anyway.

```
layers.json        ~78,000 KB   ← unreadable by agents
tables.json              3 KB   ← fine (1 table, 4 fields)
```

This will be common for any project that references enterprise feature classes with wide schemas.

### What agents actually need

When an agent encounters a table or layer, it needs to understand the **shape and distribution** of the data — not see 10 literal rows. The Data Engineering View in ArcGIS Pro already computes exactly this.

## Proposal

Add per-field summary statistics to the export, similar to what Data Engineering View provides:

```json
{
  "name": "region_code",
  "fieldType": "String",
  "stats": {
    "count": 1500,
    "nullCount": 12,
    "uniqueCount": 85,
    "topValues": ["NORTH_01", "CENTRAL_03", "SOUTH_02"]
  }
}
```

```json
{
  "name": "risk_score",
  "fieldType": "Double",
  "stats": {
    "count": 1500,
    "nullCount": 0,
    "min": 0.0,
    "max": 0.95,
    "mean": 0.34,
    "std": 0.18
  }
}
```

This replaces or supplements `sampleData` and gives agents far more useful context in a fraction of the size.

## Considerations

- **Performance**: Computing stats on large layers (millions of rows) could be slow. Options:
  - Only compute for layers under a configurable row threshold (e.g., `statsMaxRows: 50000`)
  - Use `arcpy.da.SearchCursor` with a sample, not full scan
  - Make it opt-in via `config.yml` (`exportFieldStats: true`)
- **Scope**: Could apply to both `layers.json` and `tables.json`
- **Backward compat**: Add `stats` alongside existing fields — don't break existing schema consumers
- **Field schema size**: The field definitions themselves (name, type, length, alias, nullable, editable) are relatively small. The bloat comes from `sampleData` and from exporting every field on wide FCs. Consider a `maxFieldsExported` cap or only exporting fields referenced in symbology/definition queries
