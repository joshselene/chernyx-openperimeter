"""
Unit tests for reading, parsing, and validating OpenPerimeter study export CSV and JSON.
"""

import io
import pandas as pd
import pytest


SAMPLE_CSV_CONTENT = """# RESEARCH USE ONLY - NOT FOR CLINICAL DECISIONS
# OpenPerimeter Test Session Export | Session ID: SES-DEMO-001 | Generated: 2026-10-19T10:00:00Z
# Normative Status: UNVALIDATED NORMS
# Calibration Status: RESEARCH_CALIBRATED
# Reliability: RELIABLE (FL: 5.0%, FP: 0.0%, FN: 5.0%)
device,session_id,participant_id,tested_eye,test_pattern,algorithm,point_index,x_deg,y_deg,threshold_db_device,threshold_db_hfa_equiv,is_censored,stimulus_size,gcc_sector,fl_rate_pct,fp_rate_pct,fn_rate_pct,is_reliable
OpenPerimeter,SES-DEMO-001,OP-SUBJ-001,OD,24-2,ZEST,0,-9.0,21.0,25.50,17.46,MEASURED,SIZE_III,NONE,5.0,0.0,5.0,1
OpenPerimeter,SES-DEMO-001,OP-SUBJ-001,OD,24-2,ZEST,1,-3.0,3.0,0.00,-8.04,FLOOR_CENSORED,SIZE_III,SN_MACULA,5.0,0.0,5.0,1
OpenPerimeter,SES-DEMO-001,OP-SUBJ-001,OD,24-2,ZEST,2,3.0,3.0,18.20,10.16,MEASURED,SIZE_III,ST_MACULA,5.0,0.0,5.0,1
"""


def test_parse_export_csv_schema():
    # Read ignoring comment lines starting with #
    lines = [line for line in SAMPLE_CSV_CONTENT.strip().split("\n") if not line.startswith("#")]
    csv_str = "\n".join(lines)

    df = pd.read_csv(io.StringIO(csv_str))

    assert len(df) == 3
    expected_cols = [
        "device", "session_id", "participant_id", "tested_eye", "test_pattern",
        "algorithm", "point_index", "x_deg", "y_deg", "threshold_db_device",
        "threshold_db_hfa_equiv", "is_censored", "stimulus_size", "gcc_sector",
        "fl_rate_pct", "fp_rate_pct", "fn_rate_pct", "is_reliable"
    ]
    for col in expected_cols:
        assert col in df.columns

    # Verify censoring flag
    assert df.loc[1, "is_censored"] == "FLOOR_CENSORED"
    assert df.loc[1, "threshold_db_device"] == 0.0

    # Verify macular GCC sector mapping
    assert df.loc[1, "gcc_sector"] == "SN_MACULA"
    assert df.loc[2, "gcc_sector"] == "ST_MACULA"
