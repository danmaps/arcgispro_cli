from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, List, Optional

from arcgispro_cli.paths import find_arcgispro_folder, load_context_files


@dataclass
class TUIState:
    """Shared state for the TUI — wraps the existing path/query helpers."""

    repo_path: str
    selected_kind: Optional[str] = None
    selected_item: Optional[Dict[str, Any]] = None
    _context: Optional[Dict[str, Any]] = field(default=None, init=False, repr=False)

    @property
    def arcgispro_path(self) -> Optional[Path]:
        return find_arcgispro_folder(Path(self.repo_path))

    @property
    def context(self) -> Dict[str, Any]:
        if self._context is None:
            ap = self.arcgispro_path
            if ap:
                self._context = load_context_files(ap)
            else:
                self._context = {}
        return self._context

    def reload(self) -> None:
        self._context = None

    def get_project(self) -> Optional[Dict[str, Any]]:
        return self.context.get("project")

    def get_maps(self) -> List[Dict[str, Any]]:
        return self.context.get("maps") or []

    def get_layers(self, map_name: Optional[str] = None) -> List[Dict[str, Any]]:
        layers = self.context.get("layers") or []
        if map_name:
            layers = [l for l in layers if l.get("mapName") == map_name]
        return layers

    def get_tables(self) -> List[Dict[str, Any]]:
        return self.context.get("tables") or []

    def get_connections(self) -> List[Dict[str, Any]]:
        return self.context.get("connections") or []

    def get_meta(self) -> Optional[Dict[str, Any]]:
        return self.context.get("meta")
