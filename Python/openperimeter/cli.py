"""
Command Line Interface for OpenPerimeter Research Analysis.

Commands:
- analyze: Run Bland-Altman, ICC, and correlation analysis between OpenPerimeter and reference (HFA / AVA).
- simulate: Run Monte Carlo synthetic observer simulation for normal, moderate, or advanced glaucoma.
"""

import argparse
import sys
import os
import pandas as pd
import numpy as np

from openperimeter.conversion.photometry import compute_hfa_offset_db
from openperimeter.simulation.harness import run_monte_carlo_evaluation
from openperimeter.simulation.observer import VisualFieldPattern
from openperimeter.analysis.agreement import bland_altman_analysis
from openperimeter.analysis.stats import (
    compute_icc_2_1,
    compute_pearson_correlation,
    compute_spearman_correlation,
    compute_repeatability_coefficient,
    pointwise_sensitivity_comparison,
)
from openperimeter.analysis.reporting import (
    generate_bland_altman_plot,
    generate_pointwise_difference_map,
    generate_markdown_agreement_report,
    RUO_BANNER,
)


def run_analyze(args):
    print(f"\n=======================================================")
    print(f"{RUO_BANNER}")
    print(f"OpenPerimeter Agreement Analysis Engine")
    print(f"=======================================================\n")

    if not os.path.exists(args.device_csv):
        print(f"Error: Device CSV not found: {args.device_csv}")
        sys.exit(1)
    if not os.path.exists(args.ref_csv):
        print(f"Error: Reference CSV not found: {args.ref_csv}")
        sys.exit(1)

    os.makedirs(args.output_dir, exist_ok=True)

    # Read CSV files, ignoring comment lines starting with '#'
    df_dev = pd.read_csv(args.device_csv, comment="#")
    df_ref = pd.read_csv(args.ref_csv, comment="#")

    # Match by point coordinates
    merged = pd.merge(df_dev, df_ref, on=["x_deg", "y_deg"], suffixes=("_dev", "_ref"))
    print(f"Loaded {len(merged)} paired visual field points.")

    dev_vals = merged["threshold_db_device_dev" if "threshold_db_device_dev" in merged.columns else "threshold_db_device"].values
    ref_vals = merged["threshold_db_ref" if "threshold_db_ref" in merged.columns else "threshold_db"].values

    is_censored = (dev_vals <= 0.0) | (ref_vals <= 0.0)

    # Bland-Altman
    ba = bland_altman_analysis(dev_vals, ref_vals, is_censored=is_censored, exclude_censored=args.exclude_censored)
    icc = compute_icc_2_1(np.column_stack([dev_vals, ref_vals]))
    pearson = compute_pearson_correlation(dev_vals, ref_vals)
    spearman = compute_spearman_correlation(dev_vals, ref_vals)

    # Plots
    ba_plot_path = os.path.join(args.output_dir, "bland_altman_agreement.png")
    generate_bland_altman_plot(ba, ba_plot_path)

    # Pointwise difference map
    df_pw = pointwise_sensitivity_comparison(
        df_dev, df_ref,
        col_val_dev="threshold_db_device" if "threshold_db_device" in df_dev.columns else "threshold_db",
        col_val_ref="threshold_db" if "threshold_db" in df_ref.columns else "threshold_db_device"
    )
    pw_plot_path = os.path.join(args.output_dir, "pointwise_difference_map.png")
    generate_pointwise_difference_map(df_pw, pw_plot_path)

    # Markdown Report
    report_path = os.path.join(args.output_dir, "agreement_report.md")
    report_txt = generate_markdown_agreement_report(
        study_name=args.study_name,
        ba_result=ba,
        icc_result=icc,
        pearson_result=pearson,
        spearman_result=spearman,
        output_path=report_path
    )

    print(report_txt)
    print(f"\nSaved analysis outputs to: {args.output_dir}")


def run_simulate(args):
    print(f"\n=======================================================")
    print(f"{RUO_BANNER}")
    print(f"OpenPerimeter Monte Carlo Simulation Harness")
    print(f"=======================================================\n")

    pat_map = {
        "normal": VisualFieldPattern.NORMAL_HEALTHY,
        "moderate": VisualFieldPattern.MODERATE_GLAUCOMA,
        "advanced": VisualFieldPattern.ADVANCED_GLAUCOMA
    }
    pattern = pat_map.get(args.pattern.lower(), VisualFieldPattern.NORMAL_HEALTHY)

    print(f"Running {args.runs} simulated sessions for pattern: {pattern.value} using {args.strategy}...")
    report = run_monte_carlo_evaluation(
        n_runs=args.runs,
        strategy_type=args.strategy.upper(),
        pattern_type=pattern,
        use_neighbor_priors=not args.flat_priors,
        seed=args.seed
    )

    print("\n--- Simulation Results ---")
    for k, v in report.items():
        print(f"  {k}: {v}")


def main():
    parser = argparse.ArgumentParser(description="OpenPerimeter Python CLI (Research Use Only)")
    subparsers = parser.add_subparsers(dest="command", required=True)

    # analyze command
    p_analyze = subparsers.add_parser("analyze", help="Analyze agreement between OpenPerimeter and reference perimeter")
    p_analyze.add_argument("--device-csv", required=True, help="Path to OpenPerimeter session CSV export")
    p_analyze.add_argument("--ref-csv", required=True, help="Path to Reference perimeter CSV export")
    p_analyze.add_argument("--output-dir", default="./analysis_output", help="Output directory for plots and reports")
    p_analyze.add_argument("--study-name", default="OpenPerimeter Comparative Study", help="Study title for report")
    p_analyze.add_argument("--exclude-censored", action="store_true", help="Exclude scotoma floor-censored points from Bland-Altman")

    # simulate command
    p_sim = subparsers.add_parser("simulate", help="Run Monte Carlo simulation harness")
    p_sim.add_argument("--pattern", default="normal", choices=["normal", "moderate", "advanced"], help="Visual field phenotype")
    p_sim.add_argument("--strategy", default="ZEST", choices=["ZEST", "STAIRCASE"], help="Thresholding strategy")
    p_sim.add_argument("--runs", type=int, default=40, help="Number of simulated sessions")
    p_sim.add_argument("--flat-priors", action="store_true", help="Disable neighbor-informed priors")
    p_sim.add_argument("--seed", type=int, default=42, help="Random seed")

    args = parser.parse_args()
    if args.command == "analyze":
        run_analyze(args)
    elif args.command == "simulate":
        run_simulate(args)


if __name__ == "__main__":
    main()
