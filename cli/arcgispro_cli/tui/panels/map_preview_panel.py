"""Map preview panel with ASCII art rendering."""

import webbrowser
from pathlib import Path
from typing import Dict, Any, Optional

from rich.text import Text
from textual.containers import ScrollableContainer
from textual.widget import Widget
from textual.widgets import Static


class MapPreviewPanel(ScrollableContainer):
    """Panel that displays map preview as ASCII art or metadata."""
    
    DEFAULT_CSS = """
    MapPreviewPanel {
        border: round #53565A;
        padding: 1 1;
    }
    
    MapPreviewPanel.hidden {
        display: none;
    }
    """
    
    BINDINGS = [
        ("enter", "open_image", "Open full image"),
    ]
    
    def __init__(self, **kwargs) -> None:
        super().__init__(**kwargs)
        self.current_image_path: Optional[Path] = None
        self._preview_widget = Static("", id="preview-content")
    
    def compose(self):
        """Compose the panel contents."""
        yield self._preview_widget
    
    def show_map_preview(self, item_data: Dict[str, Any], image_path: Optional[Path]) -> None:
        """
        Display preview for a map or layer.
        
        Args:
            item_data: Map or layer data dictionary
            image_path: Path to the PNG file, or None if not found
        """
        self.current_image_path = image_path
        
        if image_path and image_path.exists():
            # Try ASCII art preview
            try:
                from PIL import Image
                preview_text = self._render_ascii_preview(image_path, item_data)
                self._preview_widget.update(preview_text)
                return
            except ImportError:
                # Pillow not available, fall back to metadata
                pass
            except Exception as e:
                # Render error, fall back to metadata
                self.log(f"Failed to render ASCII preview: {e}")
        
        # Fallback: show metadata
        metadata_text = self._render_metadata(item_data, image_path)
        self._preview_widget.update(metadata_text)
    
    def _render_ascii_preview(self, image_path: Path, item_data: Dict[str, Any]) -> Text:
        """
        Render PNG as ASCII art using Unicode block characters.
        
        Args:
            image_path: Path to PNG file
            item_data: Map/layer metadata
            
        Returns:
            Rich Text object with colored ASCII art
        """
        from PIL import Image
        
        # Load and resize image to fit terminal width (~80 chars wide, maintain aspect)
        img = Image.open(image_path)
        
        # Target width in characters (accounting for padding)
        target_width = 70
        aspect_ratio = img.height / img.width
        target_height = int(target_width * aspect_ratio * 0.5)  # 0.5 because chars are ~2x tall
        
        # Resize image
        img_resized = img.resize((target_width, target_height), Image.Resampling.LANCZOS)
        img_rgb = img_resized.convert('RGB')
        
        # Convert to ASCII using Unicode block characters
        text = Text()
        text.append(f"Map: {item_data.get('name', 'Unknown')}\n", style="bold cyan")
        text.append(f"Image: {image_path.name}\n", style="dim")
        text.append("─" * target_width + "\n", style="dim")
        
        # Use Unicode half blocks for 2 pixels per character
        for y in range(0, target_height - 1, 2):
            for x in range(target_width):
                # Get top and bottom pixel colors
                r_top, g_top, b_top = img_rgb.getpixel((x, y))
                r_bot, g_bot, b_bot = img_rgb.getpixel((x, min(y + 1, target_height - 1)))
                
                # Use upper half block (▀) with top color as foreground, bottom as background
                # Simplify colors to reduce ANSI overhead
                fg_color = f"rgb({r_top},{g_top},{b_top})"
                bg_color = f"rgb({r_bot},{g_bot},{b_bot})"
                
                text.append("▀", style=f"{fg_color} on {bg_color}")
            text.append("\n")
        
        text.append("─" * target_width + "\n", style="dim")
        text.append("\n[dim]Press Enter to open full image in viewer[/dim]")
        
        return text
    
    def _render_metadata(self, item_data: Dict[str, Any], image_path: Optional[Path]) -> Text:
        """
        Render metadata fallback when ASCII preview is not available.
        
        Args:
            item_data: Map or layer data
            image_path: Path to image (may not exist)
            
        Returns:
            Rich Text with metadata information
        """
        text = Text()
        
        name = item_data.get('name', 'Unknown')
        text.append(f"Map Preview: {name}\n\n", style="bold cyan")
        
        if image_path:
            if image_path.exists():
                # Show image metadata
                size_kb = image_path.stat().st_size / 1024
                text.append(f"Image: {image_path.name}\n", style="green")
                text.append(f"Size: {size_kb:.1f} KB\n", style="dim")
                text.append("\n[dim]Press Enter to open full image[/dim]\n\n")
            else:
                text.append(f"Image not found: {image_path.name}\n", style="yellow")
                text.append("Run export in ArcGIS Pro to generate images.\n\n", style="dim")
        else:
            text.append("No preview image available\n", style="yellow")
            text.append("Run export in ArcGIS Pro to generate images.\n\n", style="dim")
        
        # Show map metadata if available
        if 'mapType' in item_data:
            text.append(f"\nMap Type: {item_data['mapType']}\n", style="cyan")
        
        if 'extent' in item_data:
            extent = item_data['extent']
            text.append(f"\nExtent:\n", style="cyan")
            text.append(f"  X: {extent.get('xmin', 'N/A')} to {extent.get('xmax', 'N/A')}\n", style="dim")
            text.append(f"  Y: {extent.get('ymin', 'N/A')} to {extent.get('ymax', 'N/A')}\n", style="dim")
            if 'spatialReference' in extent:
                sr = extent['spatialReference']
                wkid = sr.get('wkid', 'Unknown')
                text.append(f"  WKID: {wkid}\n", style="dim")
        
        if 'scale' in item_data:
            text.append(f"\nScale: 1:{item_data['scale']:,.0f}\n", style="cyan")
        
        if 'layerCount' in item_data:
            text.append(f"Layers: {item_data['layerCount']}\n", style="cyan")
        
        return text
    
    def action_open_image(self) -> None:
        """Open the current image in the system default viewer."""
        if self.current_image_path and self.current_image_path.exists():
            try:
                webbrowser.open(str(self.current_image_path))
                self.notify(f"Opening {self.current_image_path.name}")
            except Exception as e:
                self.notify(f"Failed to open image: {e}", severity="error")
        else:
            self.notify("No image to open", severity="warning")
    
    def clear_preview(self) -> None:
        """Clear the preview panel."""
        self.current_image_path = None
        self._preview_widget.update("")
