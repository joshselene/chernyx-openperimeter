"""
OpenPerimeter Statistical Analysis and Agreement Package.

Provides:
- Bland-Altman limits of agreement (bias, 95% LoA, confidence intervals)
- Two-way random absolute Intraclass Correlation Coefficient (ICC)
- Pearson and Spearman rank correlation
- Test-retest Coefficient of Repeatability (CR)
- Pointwise visual field difference mapping
- Comprehensive publication-ready agreement reporting
"""

from .stats import (
    compute_icc_2_1,
    compute_pearson_correlation,
    compute_spearman_correlation,
    compute_repeatability_coefficient,
    pointwise_sensitivity_comparison,
)
from .agreement import (
    BlandAltmanResult,
    bland_altman_analysis,
)
from .reporting import (
    generate_bland_altman_plot,
    generate_pointwise_difference_map,
    generate_markdown_agreement_report,
)

__all__ = [
    "compute_icc_2_1",
    "compute_pearson_correlation",
    "compute_spearman_correlation",
    "compute_repeatability_coefficient",
    "pointwise_sensitivity_comparison",
    "BlandAltmanResult",
    "bland_altman_analysis",
    "generate_bland_altman_plot",
    "generate_pointwise_difference_map",
    "generate_markdown_agreement_report",
]
