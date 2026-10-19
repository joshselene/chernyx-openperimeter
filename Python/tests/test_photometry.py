"""
Unit tests for photometric calculations, dB conversions, and 1/Lambert mapping.
"""

import math
import pytest
from openperimeter.conversion.photometry import (
    HFA_MAX_LUMINANCE_CDM2,
    compute_hfa_offset_db,
    device_db_to_delta_l,
    delta_l_to_device_db,
    device_db_to_hfa_db,
    hfa_db_to_device_db,
    db_to_lambert,
    lambert_to_db,
    interpolate_dac_for_luminance,
)


def test_hfa_offset_for_500_cdm2():
    # 10 * log10(3183.1 / 500) = 8.0388...
    offset = compute_hfa_offset_db(500.0)
    assert abs(offset - 8.04) < 0.05


def test_device_db_to_delta_l_zero_db():
    delta_l = device_db_to_delta_l(0.0, 500.0)
    assert abs(delta_l - 500.0) < 1e-3


def test_device_db_to_delta_l_10_db():
    delta_l = device_db_to_delta_l(10.0, 500.0)
    assert abs(delta_l - 50.0) < 1e-3


def test_delta_l_to_device_db_inversion():
    target_db = 22.5
    delta_l = device_db_to_delta_l(target_db, 500.0)
    recovered_db = delta_l_to_device_db(delta_l, 500.0)
    assert abs(recovered_db - target_db) < 1e-4


def test_hfa_conversions():
    offset = compute_hfa_offset_db(500.0)
    device_db = 20.0
    hfa_db = device_db_to_hfa_db(device_db, offset)
    assert abs(hfa_db - (20.0 - offset)) < 1e-4
    assert abs(hfa_db_to_device_db(hfa_db, offset) - device_db) < 1e-4


def test_lambert_conversions():
    # 0 dB = 1.0 (1/Lambert)
    assert abs(db_to_lambert(0.0) - 1.0) < 1e-4
    assert abs(lambert_to_db(1.0) - 0.0) < 1e-4

    # 10 dB = 10.0 (1/Lambert)
    assert abs(db_to_lambert(10.0) - 10.0) < 1e-4
    assert abs(lambert_to_db(10.0) - 10.0) < 1e-4

    # 30 dB = 1000.0 (1/Lambert)
    assert abs(db_to_lambert(30.0) - 1000.0) < 1e-3
    assert abs(lambert_to_db(1000.0) - 30.0) < 1e-4


def test_lut_interpolation():
    lut_dac = [0, 16, 31, 64, 128, 255]
    lut_cdm2 = [0.0, 2.0, 10.0, 50.0, 200.0, 500.0]

    dac = interpolate_dac_for_luminance(10.0, lut_dac, lut_cdm2)
    assert dac == 31

    # Exact midpoint between 10 and 50 cd/m2 -> midpoint between DAC 31 and 64
    mid_l = 30.0
    dac_mid = interpolate_dac_for_luminance(mid_l, lut_dac, lut_cdm2)
    assert 31 < dac_mid < 64
