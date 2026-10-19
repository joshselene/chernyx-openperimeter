"""
OpenPerimeter Simulation Harness
Synthetic observer models and Monte Carlo estimation of threshold algorithm bias, variance, and duration.
"""

from .observer import SyntheticObserver, VisualFieldPattern
from .zest_sim import SimZestStrategy
from .staircase_sim import SimStaircase42Strategy
from .harness import run_monte_carlo_evaluation

__all__ = [
    "SyntheticObserver",
    "VisualFieldPattern",
    "SimZestStrategy",
    "SimStaircase42Strategy",
    "run_monte_carlo_evaluation",
]
