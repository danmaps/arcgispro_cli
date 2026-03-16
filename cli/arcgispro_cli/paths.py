"""Utility functions for finding and validating .arcgispro folders."""

import json
import re
from pathlib import Path
from typing import Optional, Dict, Any, List


def find_arcgispro_folder(start_path: Optional[Path] = None) -> Optional[Path]:
    """
    Find the .arcgispro folder by searching current directory and ancestors.
    
    Args:
        start_path: Starting directory to search from. Defaults to cwd.
        
    Returns:
        Path to .arcgispro folder, or None if not found.
    """
    if start_path is None:
        start_path = Path.cwd()
    
    current = start_path.resolve()
    
    # Search current directory and ancestors
    while current != current.parent:
        candidate = current / ".arcgispro"
        if candidate.is_dir():
            return candidate
        current = current.parent
    
    # Check root as well
    candidate = current / ".arcgispro"
    if candidate.is_dir():
        return candidate
    
    return None


def get_context_folder(arcgispro_path: Path) -> Path:
    """Get the context subfolder path."""
    return arcgispro_path / "context"


def get_images_folder(arcgispro_path: Path) -> Path:
    """Get the images subfolder path."""
    return arcgispro_path / "images"


def get_snapshot_folder(arcgispro_path: Path) -> Path:
    """Get the snapshot subfolder path."""
    return arcgispro_path / "snapshot"


def sanitize_map_name(name: str) -> str:
    """Sanitize a map/layer name for use as a filename.

    Intended to match the ProExporter add-in's filename sanitization.

    Rules:
    - Replace Windows-invalid filename characters with underscores
    - Replace ASCII control chars (0-31) with underscores
    - Collapse whitespace to single underscores
    - Truncate to 50 characters

    Returns "unnamed" if the input is empty/whitespace, or if sanitization
    results in an empty name.
    """
    if not name or not name.strip():
        return "unnamed"

    # Windows-invalid filename characters: <>:"/\\|?*
    # Plus ASCII control chars (0-31) which Windows also disallows.
    invalid_chars = set('<>:"/\\|?*')

    def repl(match: re.Match) -> str:
        ch = match.group(0)
        if ch in invalid_chars or ord(ch) < 32:
            return "_"
        # Any whitespace becomes underscore
        return "_"

    # Replace invalid chars and whitespace with underscores.
    sanitized = re.sub(r"[\s<>:\"/\\|\?\*\x00-\x1f]", repl, name)

    # Collapse repeated underscores and trim.
    sanitized = re.sub(r"_+", "_", sanitized).strip("_")

    if not sanitized:
        return "unnamed"

    return sanitized[:50]


def load_json_file(path: Path) -> Optional[Dict[str, Any]]:
    """
    Load and parse a JSON file.
    
    Args:
        path: Path to JSON file
        
    Returns:
        Parsed JSON as dict, or None if file doesn't exist or is invalid.
    """
    if not path.exists():
        return None
    
    try:
        with open(path, "r", encoding="utf-8-sig") as f:
            return json.load(f)
    except (json.JSONDecodeError, IOError):
        return None


def load_context_files(arcgispro_path: Path) -> Dict[str, Any]:
    """
    Load all context JSON files.
    
    Args:
        arcgispro_path: Path to .arcgispro folder
        
    Returns:
        Dict with keys: meta, project, maps, layers, tables, connections, layouts
        Values are the parsed JSON or None if missing/invalid.
    """
    context_dir = get_context_folder(arcgispro_path)
    
    return {
        "meta": load_json_file(arcgispro_path / "meta.json"),
        "project": load_json_file(context_dir / "project.json"),
        "maps": load_json_file(context_dir / "maps.json"),
        "layers": load_json_file(context_dir / "layers.json"),
        "tables": load_json_file(context_dir / "tables.json"),
        "connections": load_json_file(context_dir / "connections.json"),
        "layouts": load_json_file(context_dir / "layouts.json"),
        "geoprocessing": load_json_file(context_dir / "geoprocessing.json"),
    }


def list_image_files(arcgispro_path: Path) -> List[Path]:
    """
    List all PNG files in the images folder.
    
    Args:
        arcgispro_path: Path to .arcgispro folder
        
    Returns:
        List of paths to PNG files.
    """
    images_dir = get_images_folder(arcgispro_path)
    if not images_dir.exists():
        return []
    
    return list(images_dir.glob("*.png"))


def get_active_project(arcgispro_path: Path) -> Optional[str]:
    """
    Read the active project path from active_project.txt.
    
    Args:
        arcgispro_path: Path to .arcgispro folder
        
    Returns:
        Active project path string, or None if not set.
    """
    active_file = arcgispro_path / "active_project.txt"
    if not active_file.exists():
        return None
    
    try:
        return active_file.read_text(encoding="utf-8").strip()
    except IOError:
        return None


def find_aprx_files(directory: Path, max_depth: int = 2) -> List[Path]:
    """
    Find .aprx files in directory and subdirectories.
    
    Args:
        directory: Starting directory
        max_depth: Maximum depth to search
        
    Returns:
        List of paths to .aprx files.
    """
    aprx_files = []
    
    def search(path: Path, depth: int):
        if depth > max_depth:
            return
        
        try:
            for item in path.iterdir():
                if item.is_file() and item.suffix.lower() == ".aprx":
                    aprx_files.append(item)
                elif item.is_dir() and not item.name.startswith("."):
                    search(item, depth + 1)
        except PermissionError:
            pass
    
    search(directory, 0)
    return aprx_files
