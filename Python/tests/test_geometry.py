"""
Unit tests for visual angle trigonometry, Goldmann sizes, and lens distortion inversion.
"""

import math
import pytest
from openperimeter.conversion.geometry import (
    GoldmannSize,
    get_angular_diameter_deg,
    degrees_to_screen_mm,
    screen_mm_to_degrees,
    mm_to_pixels,
    pixels_to_mm,
    visual_field_to_screen_pixels,
)


def test_goldmann_diameters():
    assert get_angular_diameter_deg(GoldmannSize.SIZE_III) == 0.4310
    assert get_angular_diameter_deg(GoldmannSize.SIZE_V) == 1.7240
    # Area ratio should be (1.7240 / 0.4310)^2 = 16.0
    diam_ratio = get_angular_diameter_deg(GoldmannSize.SIZE_V) / get_angular_diameter_deg(GoldmannSize.SIZE_III)
    assert abs(diam_ratio - 4.0) < 1e-3


def test_degrees_to_screen_mm():
    # 45mm focal length, 10 degrees -> 45 * tan(10 deg) = 7.9347 mm
    mm = degrees_to_screen_mm(10.0, 45.0)
    assert abs(mm - 7.9347) < 1e-3


def test_screen_mm_to_degrees_inversion():
    orig_deg = 24.0
    mm = degrees_to_screen_mm(orig_deg, 45.0)
    recovered_deg = screen_mm_to_degrees(mm, 45.0)
    assert abs(recovered_deg - orig_deg) < 1e-4


def test_mm_and_pixels_conversion():
    ppi = 512.0
    mm = 25.4
    px = mm_to_pixels(mm, ppi)
    assert abs(px - 512.0) < 1e-4
    assert abs(pixels_to_mm(px, ppi) - mm) < 1e-4


def test_visual_field_to_screen_pixels_center():
    px_x, px_y = visual_field_to_screen_pixels(0.0, 0.0, 45.0, 512.0)
    assert px_x == 0.0
    assert px_y == 0.0


def test_visual_field_to_screen_pixels_with_distortion():
    px_x, px_y = visual_field_to_screen_pixels(15.0, 0.0, 45.0, 512.0, k1=0.34, k2=0.12)
    # Undistorted ideal px:
    ideal_mm = degrees_to_screen_mm(15.0, 45.0)
    ideal_px = mm_to_pixels(ideal_mm, 512.0)
    # Pre-warped coordinate should be smaller than ideal to counteract pincushion expansion
    assert 0.0 < px_x < ideal_px
    assert px_y == 0.0
