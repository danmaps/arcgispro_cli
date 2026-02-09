from __future__ import annotations

from typing import Any, Dict

from textual.message import Message
from textual.widgets import Tree
from textual.widgets._tree import TreeNode


class ProjectTree(Tree):
    """Tree view of Project -> Maps -> Layers -> Fields."""
    
    BINDINGS = [
        ("left", "collapse_node", "Collapse"),
        ("right", "expand_node", "Expand"),
    ]

    class ItemSelected(Message):
        """Fired when a tree node with data is selected."""

        def __init__(self, kind: str, data: Dict[str, Any]) -> None:
            self.kind = kind
            self.data = data
            super().__init__()

    def __init__(self, state, **kwargs) -> None:
        super().__init__("Project", **kwargs)
        self.state = state
        self.guide_depth = 3
    
    def action_collapse_node(self) -> None:
        """Collapse current node or move to parent."""
        if self.cursor_node:
            if self.cursor_node.is_expanded:
                self.cursor_node.collapse()
            else:
                # Move to parent if already collapsed
                if self.cursor_node.parent:
                    self.cursor_line = self.cursor_node.parent._line
    
    def action_expand_node(self) -> None:
        """Expand current node or move to first child."""
        if self.cursor_node:
            if not self.cursor_node.is_expanded and self.cursor_node.allow_expand:
                self.cursor_node.expand()
            elif self.cursor_node.children:
                # Move to first child if already expanded
                self.cursor_line = self.cursor_node.children[0]._line

    def on_mount(self) -> None:
        self.rebuild()
        self.focus()

    def rebuild(self) -> None:
        """(Re)populate the tree from the current state."""
        self.clear()

        project = self.state.get_project()
        if project:
            self.root.set_label(project.get("name", "Project"))
            self.root.data = {"_kind": "project", **project}
        else:
            self.root.set_label("[red]No .arcgispro data found[/]")
            self.root.data = None
            return

        # -- Maps (directly under root) ----------------------------------
        for m in self.state.get_maps():
            active = " ★" if m.get("isActiveMap") else ""
            label = f"{m.get('name', '?')}{active}"
            map_node = self.root.add(label)
            map_node.data = {"_kind": "map", **m}

            for lyr in self.state.get_layers(map_name=m.get("name")):
                vis = "✓" if lyr.get("isVisible") else "✗"
                broken = " ⚠" if lyr.get("isBroken") else ""
                lyr_label = f"[{vis}] {lyr.get('name', '?')}{broken}"
                lyr_node = map_node.add(lyr_label)
                lyr_node.data = {"_kind": "layer", **lyr}

                for fld in lyr.get("fields") or []:
                    ftype = fld.get("fieldType", "")
                    fld_label = f"{fld.get('name', '?')}  ({ftype})"
                    fld_node = lyr_node.add_leaf(fld_label)
                    fld_node.data = {"_kind": "field", "_layer": lyr.get("name"), **fld}

        # -- Tables ------------------------------------------------------
        tables = self.state.get_tables()
        if tables:
            tables_node = self.root.add("Tables")
            tables_node.data = None
            for t in tables:
                tbl_node = tables_node.add_leaf(t.get("name", "?"))
                tbl_node.data = {"_kind": "table", **t}

        # -- Connections -------------------------------------------------
        connections = self.state.get_connections()
        if connections:
            conn_node = self.root.add("Connections")
            conn_node.data = None
            for c in connections:
                cn = conn_node.add_leaf(c.get("name", "?"))
                cn.data = {"_kind": "connection", **c}

        self.root.expand()
        for child in self.root.children:
            child.collapse()

    def on_tree_node_selected(self, event: Tree.NodeSelected) -> None:
        node: TreeNode = event.node
        if node.data:
            kind = node.data.get("_kind", "unknown")
            self.state.selected_kind = kind
            self.state.selected_item = node.data
            self.post_message(self.ItemSelected(kind, node.data))

    def on_tree_node_highlighted(self, event: Tree.NodeHighlighted) -> None:
        node: TreeNode = event.node
        if node.data:
            kind = node.data.get("_kind", "unknown")
            self.state.selected_kind = kind
            self.state.selected_item = node.data
            self.post_message(self.ItemSelected(kind, node.data))
