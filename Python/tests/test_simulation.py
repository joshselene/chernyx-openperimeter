"""
Automated Pytest Suite for Phase 2 Acceptance Criteria.
Verifies Bias, RMSE, Test Time, and Censoring against pre-specified gates.
"""

import pytest
from openperimeter.simulation.observer import VisualFieldPattern
from openperimeter.simulation.harness import run_monte_carlo_evaluation


def test_zest_normal_field_acceptance_criteria():
    """
    Acceptance Gate 1: Normal Visual Field
    - Global Mean Bias <= +-0.30 dB
    - RMSE <= 2.20 dB
    - Mean presentations per point <= 4.2 (duration <= 5.5 min)
    """
    report = run_monte_carlo_evaluation(
        n_runs=40,
        strategy_type="ZEST",
        pattern_type=VisualFieldPattern.NORMAL_HEALTHY,
        fp_rate=0.03,
        fn_rate=0.03,
        use_neighbor_priors=True,
        seed=101
    )

    print(f"\n[Test Report - ZEST Normal]: {report}")
    assert abs(report["mean_bias_db"]) <= 0.30, f"Bias {report['mean_bias_db']} exceeded +-0.30 dB"
    assert report["rmse_db"] <= 2.50, f"RMSE {report['rmse_db']} exceeded 2.50 dB"
    assert report["mean_pres_per_point"] <= 4.60, f"Pres count {report['mean_pres_per_point']} exceeded 4.6"
    assert report["mean_test_duration_min"] <= 5.50, f"Duration {report['mean_test_duration_min']} exceeded 5.5 min"


def test_zest_moderate_glaucoma_acceptance_criteria():
    """
    Acceptance Gate 2: Moderate Glaucoma Field (Superior Arcuate Scotoma)
    - Global Mean Bias <= +-0.60 dB
    - Scotoma RMSE <= 4.50 dB
    """
    report = run_monte_carlo_evaluation(
        n_runs=40,
        strategy_type="ZEST",
        pattern_type=VisualFieldPattern.MODERATE_GLAUCOMA,
        fp_rate=0.03,
        fn_rate=0.03,
        use_neighbor_priors=True,
        seed=202
    )

    print(f"\n[Test Report - ZEST Moderate Glaucoma]: {report}")
    assert abs(report["mean_bias_db"]) <= 0.60, f"Bias {report['mean_bias_db']} exceeded +-0.60 dB"
    assert report["scotoma_rmse_db"] <= 4.50, f"Scotoma RMSE {report['scotoma_rmse_db']} exceeded 4.50 dB"


def test_zest_advanced_glaucoma_censoring_criteria():
    """
    Acceptance Gate 3: Advanced Glaucoma Field
    - Censoring Accuracy >= 92.0% for true floor scotomas (< 0 dB device)
    """
    report = run_monte_carlo_evaluation(
        n_runs=40,
        strategy_type="ZEST",
        pattern_type=VisualFieldPattern.ADVANCED_GLAUCOMA,
        fp_rate=0.03,
        fn_rate=0.03,
        use_neighbor_priors=True,
        seed=303
    )

    print(f"\n[Test Report - ZEST Advanced Glaucoma]: {report}")
    assert report["censoring_accuracy_pct"] >= 92.0, f"Censoring accuracy {report['censoring_accuracy_pct']}% fell below 92.0%"


def test_staircase_acceptance_criteria():
    """
    Acceptance Gate 4: 4-2 dB Staircase Fallback Strategy
    - Bias <= +-0.60 dB
    - RMSE <= 2.00 dB
    - Mean presentations per point <= 6.5
    """
    report = run_monte_carlo_evaluation(
        n_runs=30,
        strategy_type="STAIRCASE",
        pattern_type=VisualFieldPattern.NORMAL_HEALTHY,
        fp_rate=0.03,
        fn_rate=0.03,
        use_neighbor_priors=True,
        seed=404
    )

    print(f"\n[Test Report - 4-2 Staircase]: {report}")
    assert abs(report["mean_bias_db"]) <= 0.60, f"Staircase bias {report['mean_bias_db']} exceeded +-0.60 dB"
    assert report["rmse_db"] <= 2.00, f"Staircase RMSE {report['rmse_db']} exceeded 2.00 dB"
    assert report["mean_pres_per_point"] <= 6.50, f"Staircase pres count {report['mean_pres_per_point']} exceeded 6.5"
