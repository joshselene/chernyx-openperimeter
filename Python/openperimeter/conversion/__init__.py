"""
Photometric and geometric conversion utilities for perimetry.
"""

from .photometry import (
    HFA_MAX_LUMINANCE_CDM2,
    HFA_BACKGROUND_CDM2,
    compute_hfa_offset_db,
    device_db_to_delta_l,
    delta_l_to_device_db,
    device_db_to_hfa_db,
    hfa_db_to_device_db,
    db_to_lambert,
    lambert_to_db,
    interpolate_dac_for_luminance,
)
from .geometry import (
    GoldmannSize,
    get_angular_diameter_deg,
    degrees_to_screen_mm,
    screen_mm_to_degrees,
    mm_to_pixels,
    pixels_to_mm,
    visual_field_to_screen_pixels,
)

__all__ = [
    "HFA_MAX_LUMINANCE_CDM2",
    "HFA_BACKGROUND_CDM2",
    "compute_hfa_offset_db",
    "device_db_to_delta_l",
    "delta_l_to_device_db",
    "device_db_to_hfa_db",
    "hfa_db_to_device_db",
    "db_to_lambert",
    "lambert_to_db",
    "interpolate_dac_for_luminance",
    "GoldmannSize",
    "get_angular_diameter_deg",
    "degrees_to_screen_mm",
    "screen_mm_to_degrees",
    "mm_to_pixels",
    "pixels_to_mm",
    "visual_field_to_screen_pixels",
]
