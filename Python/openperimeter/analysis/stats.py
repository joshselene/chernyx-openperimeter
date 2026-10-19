"""
Statistical Analysis Functions for Perimetry Agreement and Reliability.

Includes:
- Two-way random absolute Intraclass Correlation Coefficient (ICC 2,1)
- Pearson & Spearman correlation (with tied rank support for scotoma floors)
- Test-Retest Coefficient of Repeatability (Bland-Altman / British Standards)
- Pointwise sensitivity comparisons across visual field sectors
"""

import math
from typing import Dict, Tuple, Any, Optional
import numpy as np
import pandas as pd
from scipy import stats


def compute_icc_2_1(data_matrix: np.ndarray) -> Dict[str, float]:
    """
    Computes Intraclass Correlation Coefficient ICC(2,1):
    Two-way random effects, single rater, absolute agreement.
    Formula: Shrout & Fleiss (1979) / McGraw & Wong (1996).

    Parameters:
        data_matrix: array-like of shape (n_targets, k_raters).
                     e.g. n visual field points x 2 devices (OpenPerimeter vs HFA).

    Returns:
        Dict with keys: 'icc', 'ci_lower', 'ci_upper', 'f_stat', 'p_val'
    """
    matrix = np.asarray(data_matrix, dtype=float)
    if matrix.ndim != 2 or matrix.shape[1] < 2:
        raise ValueError("Data matrix must be 2D with at least 2 columns (raters/devices).")

    # Drop any row with NaNs
    valid_mask = ~np.isnan(matrix).any(axis=1)
    matrix = matrix[valid_mask]
    n, k = matrix.shape

    if n < 3:
        raise ValueError(f"Need at least 3 valid observations, got {n}.")

    # Grand mean and marginal means
    grand_mean = np.mean(matrix)
    row_means = np.mean(matrix, axis=1)
    col_means = np.mean(matrix, axis=0)

    # Sum of Squares
    sst = np.sum((matrix - grand_mean) ** 2)
    ssr = k * np.sum((row_means - grand_mean) ** 2)
    ssc = n * np.sum((col_means - grand_mean) ** 2)
    sse = sst - ssr - ssc

    # Degrees of Freedom
    df_r = n - 1
    df_c = k - 1
    df_e = df_r * df_c

    # Mean Squares
    msr = ssr / max(1, df_r)
    msc = ssc / max(1, df_c)
    mse = max(1e-12, sse / max(1, df_e))

    # ICC(2,1) formula
    denominator = msr + (k - 1) * mse + (k / n) * (msc - mse)
    if denominator <= 1e-12:
        icc = 0.0
    else:
        icc = (msr - mse) / denominator

    # F-statistic and p-value
    f_stat = msr / mse
    p_val = 1.0 - stats.f.cdf(f_stat, df_r, df_e)

    # 95% Confidence Intervals (McGraw & Wong formulation)
    f_c = msc / mse
    f_025 = stats.f.ppf(0.975, df_r, df_e)
    f_975 = stats.f.ppf(0.025, df_r, df_e)

    ci_lower = max(-1.0, min(1.0, (msr - f_025 * mse) / (msr + (k - 1) * f_025 * mse + (k / n) * (msc - mse))))
    ci_upper = max(-1.0, min(1.0, (msr - f_975 * mse) / (msr + (k - 1) * f_975 * mse + (k / n) * (msc - mse))))

    return {
        "icc": float(np.clip(icc, -1.0, 1.0)),
        "ci_lower": float(ci_lower),
        "ci_upper": float(ci_upper),
        "f_stat": float(f_stat),
        "p_val": float(p_val),
        "n_samples": int(n),
        "k_raters": int(k)
    }


def compute_pearson_correlation(x: np.ndarray, y: np.ndarray) -> Dict[str, float]:
    """Computes Pearson product-moment correlation coefficient and two-tailed p-value."""
    x = np.asarray(x, dtype=float)
    y = np.asarray(y, dtype=float)
    mask = ~(np.isnan(x) | np.isnan(y))
    x_clean = x[mask]
    y_clean = y[mask]

    if len(x_clean) < 3:
        return {"r": 0.0, "p_val": 1.0, "n": len(x_clean)}

    r, p_val = stats.pearsonr(x_clean, y_clean)
    return {"r": float(r), "p_val": float(p_val), "n": int(len(x_clean))}


def compute_spearman_correlation(x: np.ndarray, y: np.ndarray) -> Dict[str, float]:
    """
    Computes Spearman rank correlation coefficient.
    Ideal for clinical perimetry datasets containing tied floor-censored points.
    """
    x = np.asarray(x, dtype=float)
    y = np.asarray(y, dtype=float)
    mask = ~(np.isnan(x) | np.isnan(y))
    x_clean = x[mask]
    y_clean = y[mask]

    if len(x_clean) < 3:
        return {"rho": 0.0, "p_val": 1.0, "n": len(x_clean)}

    rho, p_val = stats.spearmanr(x_clean, y_clean)
    return {"rho": float(rho), "p_val": float(p_val), "n": int(len(x_clean))}


def compute_repeatability_coefficient(test1: np.ndarray, test2: np.ndarray) -> Dict[str, float]:
    """
    Computes Test-Retest Coefficient of Repeatability (CR) according to British Standards / Bland & Altman.
    CR = 1.96 * sqrt(2) * s_w = 2.77 * s_w
    Where s_w is the within-subject standard deviation: s_w = sqrt(sum(d_i^2) / (2n)).
    """
    t1 = np.asarray(test1, dtype=float)
    t2 = np.asarray(test2, dtype=float)
    mask = ~(np.isnan(t1) | np.isnan(t2))
    t1 = t1[mask]
    t2 = t2[mask]
    n = len(t1)

    if n < 3:
        raise ValueError(f"Need at least 3 valid test-retest pairs, got {n}.")

    diffs = t1 - t2
    # Within-subject variance
    var_w = np.sum(diffs ** 2) / (2.0 * n)
    s_w = math.sqrt(max(0.0, var_w))

    # Coefficient of Repeatability (95% difference limit)
    cr = 1.96 * math.sqrt(2.0) * s_w  # ~ 2.77 * s_w

    return {
        "cr_db": float(cr),
        "sw_db": float(s_w),
        "mean_diff_db": float(np.mean(diffs)),
        "sd_diff_db": float(np.std(diffs, ddof=1)),
        "n_pairs": int(n)
    }


def pointwise_sensitivity_comparison(
    df_device: pd.DataFrame,
    df_reference: pd.DataFrame,
    col_x: str = "x_deg",
    col_y: str = "y_deg",
    col_val_dev: str = "threshold_db_device",
    col_val_ref: str = "threshold_db"
) -> pd.DataFrame:
    """
    Performs pointwise visual field comparison between OpenPerimeter and reference device (HFA or AVA).
    Returns DataFrame with mean difference, absolute error, and correlation per point.
    """
    merged = pd.merge(
        df_device,
        df_reference,
        on=[col_x, col_y],
        suffixes=("_openperim", "_ref")
    )

    # Suffixes only apply if columns collide
    dev_col = f"{col_val_dev}_openperim" if col_val_dev == col_val_ref else col_val_dev
    ref_col = f"{col_val_ref}_ref" if col_val_dev == col_val_ref else col_val_ref

    summary_rows = []
    for (x, y), group in merged.groupby([col_x, col_y]):
        dev_vals = group[dev_col].values
        ref_vals = group[ref_col].values
        diffs = dev_vals - ref_vals

        summary_rows.append({
            "x_deg": x,
            "y_deg": y,
            "mean_dev_db": float(np.mean(dev_vals)),
            "mean_ref_db": float(np.mean(ref_vals)),
            "mean_diff_db": float(np.mean(diffs)),
            "sd_diff_db": float(np.std(diffs, ddof=1)) if len(diffs) > 1 else 0.0,
            "rmse_db": float(np.sqrt(np.mean(diffs ** 2))),
            "n_pairs": len(diffs)
        })

    return pd.DataFrame(summary_rows)
