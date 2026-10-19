"""
Visual field angle, optical distortion, and screen geometry conversions.
"""

import math
from enum import IntEnum
from typing import Tuple


class GoldmannSize(IntEnum):
    SIZE_I = 1   # 0.108 deg
    SIZE_II = 2  # 0.216 deg
    SIZE_III = 3 # 0.431 deg (Standard)
    SIZE_IV = 4  # 0.862 deg
    SIZE_V = 5   # 1.724 deg (Macular/Severe scotoma extension)


GOLDMANN_DIAMETERS_DEG = {
    GoldmannSize.SIZE_I: 0.1077,
    GoldmannSize.SIZE_II: 0.2155,
    GoldmannSize.SIZE_III: 0.4310,
    GoldmannSize.SIZE_IV: 0.8620,
    GoldmannSize.SIZE_V: 1.7240,
}


def get_angular_diameter_deg(size: GoldmannSize) -> float:
    return GOLDMANN_DIAMETERS_DEG.get(size, 0.4310)


def degrees_to_screen_mm(degrees: float, focal_length_mm: float) -> float:
    """
    Converts visual angle degrees to physical millimeter displacement on screen surface.
    r_mm = f * tan(theta * pi / 180)
    """
    rad = math.radians(degrees)
    return focal_length_mm * math.tan(rad)


def screen_mm_to_degrees(r_mm: float, focal_length_mm: float) -> float:
    """
    Converts physical millimeter displacement to visual angle degrees.
    theta = atan(r_mm / f) * 180 / pi
    """
    rad = math.atan(r_mm / focal_length_mm)
    return math.degrees(rad)


def mm_to_pixels(mm: float, ppi: float) -> float:
    return mm * (ppi / 25.4)


def pixels_to_mm(pixels: float, ppi: float) -> float:
    return pixels * (25.4 / ppi)


def visual_field_to_screen_pixels(
    theta_x_deg: float,
    theta_y_deg: float,
    focal_length_mm: float,
    ppi: float,
    k1: float = 0.34,
    k2: float = 0.12,
    normalize_radius_px: float = 1200.0,
) -> Tuple[float, float]:
    """
    Calculates pre-distorted screen pixel offset from center for a visual field point (theta_x, theta_y).
    Uses Newton-Raphson to invert radial pincushion distortion.
    """
    x_mm = degrees_to_screen_mm(theta_x_deg, focal_length_mm)
    y_mm = degrees_to_screen_mm(theta_y_deg, focal_length_mm)

    ideal_px_x = mm_to_pixels(x_mm, ppi)
    ideal_px_y = mm_to_pixels(y_mm, ppi)

    r_ideal = math.sqrt(ideal_px_x ** 2 + ideal_px_y ** 2)
    if r_ideal < 1e-4:
        return 0.0, 0.0

    r_norm_target = r_ideal / normalize_radius_px
    r_pre = r_norm_target

    for _ in range(8):
        r2 = r_pre * r_pre
        r4 = r2 * r2
        f = r_pre * (1.0 + k1 * r2 + k2 * r4) - r_norm_target
        df = 1.0 + 3.0 * k1 * r2 + 5.0 * k2 * r4
        if abs(df) < 1e-6:
            break
        delta = f / df
        r_pre -= delta
        if abs(delta) < 1e-6:
            break

    final_radius_px = r_pre * normalize_radius_px
    scale = final_radius_px / r_ideal

    return ideal_px_x * scale, ideal_px_y * scale
