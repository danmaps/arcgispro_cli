# Quickstart (First 5 Minutes)

Goal: generate a snapshot from ArcGIS Pro, then query it locally so an AI (or you) can answer real questions without guessing.

## 1) Install

```bash
pip install arcgispro-cli
arcgispro install
```

## 2) Generate a snapshot in ArcGIS Pro

1. Open your project (.aprx)
2. Go to the **CLI** ribbon tab
3. Click **Snapshot**

This writes a set of flat files into an export folder.

## 3) Validate the export

```bash
arcgispro status
```

If anything is missing, ArcGIS Pro may need to be open with the project loaded, then click **Snapshot** again.

## 4) Ask basic questions

```bash
arcgispro project
arcgispro maps
arcgispro layers
arcgispro layer "Parcels"
arcgispro fields "Parcels"
```

## 5) Keep it clean

```bash
arcgispro clean
```

(Deletes generated export files only, not your .aprx.)
