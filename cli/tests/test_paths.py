import pytest

from arcgispro_cli.paths import sanitize_map_name


@pytest.mark.parametrize(
    "raw,expected",
    [
        ("", "unnamed"),
        ("   ", "unnamed"),
        ("Parcels", "Parcels"),
        ("A/B\\C:D*E?F\"G<H>I|J", "A_B_C_D_E_F_G_H_I_J"),
        ("hello   world\tagain", "hello_world_again"),
    ],
)
def test_sanitize_map_name_basic(raw, expected):
    assert sanitize_map_name(raw) == expected


def test_sanitize_map_name_control_chars_become_underscores():
    # ASCII 0x01 and 0x1F are invalid on Windows
    raw = "A\x01B\x1fC"
    assert sanitize_map_name(raw) == "A_B_C"


def test_sanitize_map_name_truncates_to_50_chars():
    raw = "x" * 80
    out = sanitize_map_name(raw)
    assert len(out) == 50
    assert out == "x" * 50


def test_sanitize_map_name_all_invalid_falls_back_to_unnamed():
    raw = "<>:\"/\\|?*\t\n"
    assert sanitize_map_name(raw) == "unnamed"
