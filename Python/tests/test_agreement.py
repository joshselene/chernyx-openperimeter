"""
Unit tests for Python agreement-analysis package:
- Bland-Altman limits of agreement
- ICC(2,1) two-way random absolute agreement
- Pearson & Spearman correlations
- Test-retest coefficient of repeatability (CR)
- Plot and report generation
"""

import os
import tempfile
import numpy as np
import pandas as pd
import pytest

from openperimeter.analysis.stats import (
    compute_icc_2_1,
    compute_pearson_correlation,
    compute_spearman_correlation,
    compute_repeatability_coefficient,
    pointwise_sensitivity_comparison,
)
from openperimeter.analysis.agreement import bland_altman_analysis
from openperimeter.analysis.reporting import (
    generate_bland_altman_plot,
    generate_pointwise_difference_map,
    generate_markdown_agreement_report,
)


def test_compute_icc_2_1_known_values():
    # Construct synthetic data with high inter-device agreement
    rng = np.random.default_rng(42)
    true_vals = rng.normal(25.0, 5.0, size=50)
    dev1 = true_vals + rng.normal(0.0, 0.5, size=50)
    dev2 = true_vals + rng.normal(0.0, 0.5, size=50)

    matrix = np.column_stack([dev1, dev2])
    res = compute_icc_2_1(matrix)

    assert "icc" in res
    assert res["icc"] > 0.90, f"Expected high ICC, got {res['icc']}"
    assert res["ci_lower"] <= res["icc"] <= res["ci_upper"]
    assert res["p_val"] < 0.001


def test_bland_altman_known_dataset():
    # Device 1 and Device 2 with fixed +1.5 dB bias and 1.0 dB SD
    rng = np.random.default_rng(123)
    ref = np.linspace(10.0, 30.0, 100)
    dev = ref + 1.5 + rng.normal(0.0, 1.0, size=100)

    ba = bland_altman_analysis(dev, ref)

    assert abs(ba.mean_bias - 1.5) < 0.25
    assert abs(ba.sd_diff - 1.0) < 0.25
    assert ba.upper_loa > ba.mean_bias
    assert ba.lower_loa < ba.mean_bias
    assert ba.ci_bias_lower <= ba.mean_bias <= ba.ci_bias_upper


def test_bland_altman_censored_handling():
    # Dataset with 10 floor-censored points at 0 dB
    ref = np.array([0.0, 0.0, 0.0, 15.0, 20.0, 25.0, 30.0])
    dev = np.array([0.0, 0.0, 0.0, 14.5, 20.2, 24.8, 29.9])
    censored_flags = np.array([True, True, True, False, False, False, False])

    # Uncensored only
    ba_uncensored = bland_altman_analysis(dev, ref, is_censored=censored_flags, exclude_censored=True)
    assert ba_uncensored.n_pairs == 4
    assert ba_uncensored.n_censored == 3

    # All pairs
    ba_all = bland_altman_analysis(dev, ref, is_censored=censored_flags, exclude_censored=False)
    assert ba_all.n_pairs == 7


def test_repeatability_coefficient():
    # Test-retest with SD of differences = 1.414 -> s_w = 1.0 -> CR = 2.77
    t1 = np.array([20.0, 25.0, 30.0, 22.0, 28.0])
    t2 = np.array([21.0, 24.0, 31.0, 23.0, 27.0])

    cr = compute_repeatability_coefficient(t1, t2)
    assert "cr_db" in cr
    assert "sw_db" in cr
    assert cr["sw_db"] > 0.0
    assert abs(cr["cr_db"] - 1.96 * np.sqrt(2.0) * cr["sw_db"]) < 1e-4


def test_pointwise_comparison():
    df_dev = pd.DataFrame({
        "x_deg": [-9.0, 9.0],
        "y_deg": [9.0, 9.0],
        "threshold_db_device": [25.0, 28.0]
    })
    df_ref = pd.DataFrame({
        "x_deg": [-9.0, 9.0],
        "y_deg": [9.0, 9.0],
        "threshold_db": [24.0, 27.5]
    })

    pw = pointwise_sensitivity_comparison(df_dev, df_ref)
    assert len(pw) == 2
    assert "mean_diff_db" in pw.columns
    assert pw.loc[0, "mean_diff_db"] == 1.0


def test_reporting_generation():
    with tempfile.TemporaryDirectory() as tmpdir:
        dev = np.array([15.0, 20.0, 25.0, 30.0, 0.0])
        ref = np.array([14.5, 20.5, 24.8, 29.5, 0.0])
        ba = bland_altman_analysis(dev, ref)

        # Plot generation
        plot_path = os.path.join(tmpdir, "bland_altman.png")
        generate_bland_altman_plot(ba, plot_path)
        assert os.path.exists(plot_path)
        assert os.path.getsize(plot_path) > 1000

        # Markdown report generation
        icc = compute_icc_2_1(np.column_stack([dev, ref]))
        pearson = compute_pearson_correlation(dev, ref)
        spearman = compute_spearman_correlation(dev, ref)
        report_path = os.path.join(tmpdir, "report.md")

        report_txt = generate_markdown_agreement_report(
            "OpenPerimeter vs HFA Study",
            ba,
            icc,
            pearson,
            spearman,
            output_path=report_path
        )

        assert "RESEARCH USE ONLY - NOT FOR CLINICAL DECISIONS" in report_txt
        assert "Bland-Altman Agreement Statistics" in report_txt
        assert os.path.exists(report_path)
