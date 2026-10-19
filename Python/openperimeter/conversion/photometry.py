"""
Photometric and Decibel (dB) Conversion Module for OpenPerimeter.

Converts between:
- Native device dB (relative to phone max luminance L_max behind lens)
- HFA standard dB (relative to 10,000 asb = 3183.1 cd/m2)
- Differential light sensitivity in cd/m2 and apostilbs
- 1/Lambert (linear sensitivity scale used in structure-function modeling)
"""

import math
from typing import List, Tuple, Optional
import numpy as np

# Standard Automated Perimetry Constants
HFA_MAX_LUMINANCE_CDM2: float = 3183.0988618  # 10,000 asb / pi
HFA_BACKGROUND_CDM2: float = 10.02676         # 31.5 asb / pi
ASB_PER_CDM2: float = math.pi                 # 1 cd/m2 = pi asb


def compute_hfa_offset_db(peak_measured_cdm2: float) -> float:
    """
    Computes constant dB offset between HFA 0 dB (3183.1 cd/m2) and device 0 dB (L_max).
    Offset = 10 * log10(3183.1 / L_max)
    """
    if peak_measured_cdm2 <= 1e-4:
        raise ValueError("Peak luminance must be positive.")
    return 10.0 * math.log10(HFA_MAX_LUMINANCE_CDM2 / peak_measured_cdm2)


def device_db_to_delta_l(device_db: float, peak_measured_cdm2: float) -> float:
    """
    Converts device dB to differential stimulus luminance increment delta_L (cd/m2).
    delta_L = L_max * 10^(-dB / 10)
    """
    if device_db < 0.0:
        return peak_measured_cdm2
    return peak_measured_cdm2 * (10.0 ** (-device_db / 10.0))


def delta_l_to_device_db(delta_l_cdm2: float, peak_measured_cdm2: float) -> float:
    """
    Converts stimulus luminance increment delta_L (cd/m2) to device dB.
    dB = 10 * log10(L_max / delta_L)
    """
    if delta_l_cdm2 <= 1e-6:
        return 50.0  # high sensitivity clamp
    if delta_l_cdm2 >= peak_measured_cdm2:
        return 0.0
    return 10.0 * math.log10(peak_measured_cdm2 / delta_l_cdm2)


def device_db_to_hfa_db(device_db: float, hfa_offset_db: float) -> float:
    """
    Converts native device dB to equivalent HFA dB.
    dB_HFA = dB_device - offset
    """
    return device_db - hfa_offset_db


def hfa_db_to_device_db(hfa_db: float, hfa_offset_db: float) -> float:
    """
    Converts HFA dB to native device dB.
    dB_device = dB_HFA + offset
    """
    return hfa_db + hfa_offset_db


def db_to_lambert(db: float) -> float:
    """
    Converts perimetric dB to 1/Lambert (linear sensitivity scale).
    In perimetry: 1 Lambert = 10,000 asb (HFA 0 dB reference).
    Sensitivity (1/Lambert) = 10^(dB / 10).
    E.g. 0 dB = 1.0 (1/Lambert), 10 dB = 10.0, 20 dB = 100.0, 30 dB = 1000.0.
    """
    return 10.0 ** (db / 10.0)


def lambert_to_db(inv_lambert: float) -> float:
    """
    Converts 1/Lambert linear sensitivity to perimetric dB.
    dB = 10 * log10(1/Lambert)
    """
    if inv_lambert <= 0.0:
        return 0.0
    return 10.0 * math.log10(inv_lambert)


def interpolate_dac_for_luminance(
    target_cdm2: float,
    lut_dac: List[int],
    lut_cdm2: List[float]
) -> int:
    """
    Interpolates integer DAC value (0-255) for a requested luminance using monotonic piecewise linear LUT.
    """
    if len(lut_dac) == 0 or len(lut_dac) != len(lut_cdm2):
        # Fallback gamma 2.2 with 500 cd/m2 max
        normalized = max(0.0, min(1.0, target_cdm2 / 500.0))
        return round((normalized ** (1.0 / 2.2)) * 255)

    if target_cdm2 <= lut_cdm2[0]:
        return lut_dac[0]
    if target_cdm2 >= lut_cdm2[-1]:
        return lut_dac[-1]

    for i in range(len(lut_cdm2) - 1):
        if lut_cdm2[i] <= target_cdm2 <= lut_cdm2[i + 1]:
            denom = max(1e-5, lut_cdm2[i + 1] - lut_cdm2[i])
            t = (target_cdm2 - lut_cdm2[i]) / denom
            dac = lut_dac[i] + t * (lut_dac[i + 1] - lut_dac[i])
            return int(np.clip(round(dac), 0, 255))

    return 255
