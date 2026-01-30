"""Helper for connecting to the current ArcGIS Pro session.

This module helps Python scripts connect to a running ArcGIS Pro instance
to avoid "Directory does not exist" errors with stale temp workspace paths.
"""
import json
import os
from pathlib import Path
from typing import Optional


def get_session_info() -> Optional[dict]:
    """Read current ArcGIS Pro session info from .arcgispro/session.json.
    
    Returns:
        Dict with processId, timestamp, tempPath, proTempPath, or None if not found.
    """
    try:
        # Look for session.json starting from current directory
        session_file = Path.cwd() / ".arcgispro" / "session.json"
        
        if not session_file.exists():
            # Try parent directories
            for parent in Path.cwd().parents:
                session_file = parent / ".arcgispro" / "session.json"
                if session_file.exists():
                    break
            else:
                return None
        
        with open(session_file) as f:
            return json.load(f)
    except Exception:
        return None


def ensure_arcpy_connection() -> bool:
    """Ensure arcpy is connected to the running ArcGIS Pro session.
    
    Sets the ARCGISPRO_PID environment variable if a session.json file
    is found. This prevents arcpy from using stale temp directory paths.
    
    Returns:
        True if session info was found and PID was set, False otherwise.
        
    Example:
        >>> import arcgispro_cli.session as session
        >>> if session.ensure_arcpy_connection():
        ...     import arcpy
        ...     # arcpy will now connect to the running Pro session
        ... else:
        ...     print("No active Pro session found")
    """
    session = get_session_info()
    if not session:
        return False
    
    # Set PID if not already set
    if "ARCGISPRO_PID" not in os.environ:
        os.environ["ARCGISPRO_PID"] = str(session["processId"])
    
    return True


def get_pro_temp_path() -> Optional[Path]:
    """Get the current ArcGIS Pro session's temp directory path.
    
    Returns:
        Path to the Pro temp directory (e.g., C:/Users/.../Temp/ArcGISProTemp12345),
        or None if no session info is available.
    """
    session = get_session_info()
    if not session or "proTempPath" not in session:
        return None
    
    return Path(session["proTempPath"])


def is_pro_running() -> bool:
    """Check if an ArcGIS Pro session is currently running.
    
    Returns:
        True if session.json exists and appears recent (< 24 hours old).
    """
    try:
        session_file = Path.cwd() / ".arcgispro" / "session.json"
        
        if not session_file.exists():
            # Try parent directories
            for parent in Path.cwd().parents:
                session_file = parent / ".arcgispro" / "session.json"
                if session_file.exists():
                    break
            else:
                return False
        
        # Check if file is less than 24 hours old
        import time
        age_hours = (time.time() - session_file.stat().st_mtime) / 3600
        return age_hours < 24
    except Exception:
        return False
