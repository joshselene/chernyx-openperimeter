"""
Bland-Altman Agreement Analysis Module for Perimetry.

Implements:
- Bias (mean difference between OpenPerimeter and HFA/AVA)
- 95% Limits of Agreement (LoA: mean_diff +- 1.96 * SD)
- Exact 95% Confidence Intervals for bias and LoA
- Proportional bias detection via linear regression
- Explicit handling of scotoma floor-censored points
"""

import math
from dataclasses import dataclass
from typing import Dict, Any, Optional, Tuple, List
import numpy as np
from scipy import stats

from openperimeter.conversion.photometry import db_to_lambert


@dataclass
class BlandAltmanResult:
    mean_bias: float
    sd_diff: float
    lower_loa: float
    upper_loa: float
    ci_bias_lower: float
    ci_bias_upper: float
    ci_lower_loa_lower: float
    ci_lower_loa_upper: float
    ci_upper_loa_lower: float
    ci_upper_loa_upper: float
    proportional_bias_slope: float
    proportional_bias_pval: float
    n_pairs: int
    n_censored: int
    means: np.ndarray
    diffs: np.ndarray
    is_censored_mask: np.ndarray


def bland_altman_analysis(
    device_vals: np.ndarray,
    reference_vals: np.ndarray,
    is_censored: Optional[np.ndarray] = None,
    exclude_censored: bool = False,
    convert_to_lambert: bool = False
) -> BlandAltmanResult:
    """
    Performs standard Bland-Altman agreement analysis between OpenPerimeter and Reference perimeter.

    Parameters:
        device_vals: Measured sensitivities from OpenPerimeter (in dB).
        reference_vals: Measured sensitivities from reference perimeter (HFA or AVA) (in dB).
        is_censored: Boolean mask or flag indicating floor-censored points.
        exclude_censored: If True, calculates agreement on measurable uncensored points only.
        convert_to_lambert: If True, converts dB to linear 1/Lambert sensitivity prior to analysis.
    """
    d_vals = np.asarray(device_vals, dtype=float)
    r_vals = np.asarray(reference_vals, dtype=float)

    if len(d_vals) != len(r_vals):
        raise ValueError("device_vals and reference_vals must have identical lengths.")

    valid_mask = ~(np.isnan(d_vals) | np.isnan(r_vals))
    d_clean = d_vals[valid_mask]
    r_clean = r_vals[valid_mask]

    if is_censored is not None:
        c_clean = np.asarray(is_censored, dtype=bool)[valid_mask]
    else:
        # Auto-detect floor censoring (at 0 dB on either device)
        c_clean = (d_clean <= 0.0) | (r_clean <= 0.0)

    n_censored = int(np.sum(c_clean))

    if exclude_censored:
        uncensored_mask = ~c_clean
        d_eval = d_clean[uncensored_mask]
        r_eval = r_clean[uncensored_mask]
        eval_censored_mask = np.zeros(len(d_eval), dtype=bool)
    else:
        d_eval = d_clean
        r_eval = r_clean
        eval_censored_mask = c_clean

    if convert_to_lambert:
        d_eval = np.array([db_to_lambert(v) for v in d_eval])
        r_eval = np.array([db_to_lambert(v) for v in r_eval])

    n = len(d_eval)
    if n < 3:
        raise ValueError(f"Need at least 3 pairs for Bland-Altman analysis, got {n}.")

    # Differences (Device - Reference) and Means
    diffs = d_eval - r_eval
    means = (d_eval + r_eval) * 0.5

    mean_bias = float(np.mean(diffs))
    sd_diff = float(np.std(diffs, ddof=1))

    # 95% Limits of Agreement
    lower_loa = mean_bias - 1.96 * sd_diff
    upper_loa = mean_bias + 1.96 * sd_diff

    # Standard errors and 95% confidence intervals
    se_bias = sd_diff / math.sqrt(n)
    t_val = stats.t.ppf(0.975, df=n - 1)

    ci_bias_lower = mean_bias - t_val * se_bias
    ci_bias_upper = mean_bias + t_val * se_bias

    # Standard error of limits of agreement: SE(LoA) = sqrt(3 * sd^2 / n)
    se_loa = math.sqrt(3.0 * (sd_diff ** 2) / n)
    ci_lower_loa_lower = lower_loa - t_val * se_loa
    ci_lower_loa_upper = lower_loa + t_val * se_loa
    ci_upper_loa_lower = upper_loa - t_val * se_loa
    ci_upper_loa_upper = upper_loa + t_val * se_loa

    # Proportional bias test: linear regression of diffs against means
    slope, intercept, r_value, p_value, std_err = stats.linregress(means, diffs)

    return BlandAltmanResult(
        mean_bias=mean_bias,
        sd_diff=sd_diff,
        lower_loa=lower_loa,
        upper_loa=upper_loa,
        ci_bias_lower=ci_bias_lower,
        ci_bias_upper=ci_bias_upper,
        ci_lower_loa_lower=ci_lower_loa_lower,
        ci_lower_loa_upper=ci_lower_loa_upper,
        ci_upper_loa_lower=ci_upper_loa_lower,
        ci_upper_loa_upper=ci_upper_loa_upper,
        proportional_bias_slope=float(slope),
        proportional_bias_pval=float(p_value),
        n_pairs=n,
        n_censored=n_censored,
        means=means,
        diffs=diffs,
        is_censored_mask=eval_censored_mask
    )
