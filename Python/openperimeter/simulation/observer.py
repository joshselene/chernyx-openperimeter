"""
Synthetic Human Observer Model for Automated Perimetry.

Implements standard cumulative Gaussian Frequency-of-Seeing (FOS) curves
with configurable false-positive rates, false-negative lapse rates, and reaction times.
"""

import math
from enum import Enum
from typing import Dict, Tuple, List
import numpy as np
from scipy.stats import norm


class VisualFieldPattern(Enum):
    NORMAL_HEALTHY = "NORMAL_HEALTHY"
    MODERATE_GLAUCOMA = "MODERATE_GLAUCOMA"
    ADVANCED_GLAUCOMA = "ADVANCED_GLAUCOMA"


def generate_24_2_coordinates() -> List[Tuple[float, float]]:
    """
    Generates standard Humphrey 24-2 Cartesian grid coordinates (54 points).
    6-degree grid with 3-degree offset from horizontal and vertical axes.
    Excludes the two physiological blind spot points at (+15, -3) and (+15, +3).
    """
    coords = []
    # 24-2 spans x from -27 to +27, y from -21 to +21
    # Typical 24-2 pattern point mask:
    grid_def = [
        # y = +21
        [(-9, 21), (-3, 21), (3, 21), (9, 21)],
        # y = +15
        [(-15, 15), (-9, 15), (-3, 15), (3, 15), (9, 15), (15, 15)],
        # y = +9
        [(-21, 9), (-15, 9), (-9, 9), (-3, 9), (3, 9), (9, 9), (15, 9), (21, 9)],
        # y = +3
        [(-27, 3), (-21, 3), (-15, 3), (-9, 3), (-3, 3), (3, 3), (9, 3), (15, 3), (21, 3)],
        # y = -3
        [(-27, -3), (-21, -3), (-15, -3), (-9, -3), (-3, -3), (3, -3), (9, -3), (15, -3), (21, -3)],
        # y = -9
        [(-21, -9), (-15, -9), (-9, -9), (-3, -9), (3, -9), (9, -9), (15, -9), (21, -9)],
        # y = -15
        [(-15, -15), (-9, -15), (-3, -15), (3, -15), (9, -15), (15, -15)],
        # y = -21
        [(-9, -21), (-3, -21), (3, -21), (9, -21)],
    ]
    for row in grid_def:
        for pt in row:
            coords.append((float(pt[0]), float(pt[1])))
    return coords


def create_visual_field_model(pattern: VisualFieldPattern) -> Dict[Tuple[float, float], float]:
    """
    Creates true sensitivity map (in device dB) across 54 points of 24-2 visual field.
    """
    coords = generate_24_2_coordinates()
    field = {}

    for (x, y) in coords:
        eccentricity = math.sqrt(x * x + y * y)

        if pattern == VisualFieldPattern.NORMAL_HEALTHY:
            # Normal hill of vision: 33 dB central, falling by ~0.3 dB per degree eccentricity
            t_true = 33.0 - (eccentricity * 0.28)
            field[(x, y)] = float(np.clip(t_true, 24.0, 34.0))

        elif pattern == VisualFieldPattern.MODERATE_GLAUCOMA:
            # Superior arcuate scotoma (Bjerrum area: y > 0, x between -15 and +15)
            base_t = 32.0 - (eccentricity * 0.28)
            if y > 0 and abs(x) <= 15:
                # Dense arcuate depression (8 to 15 dB)
                t_true = base_t - 16.0 + (x * 0.2)
            elif y == 3 and x <= -9:
                # Nasal step defect
                t_true = base_t - 14.0
            else:
                t_true = base_t
            field[(x, y)] = float(np.clip(t_true, 6.0, 33.0))

        elif pattern == VisualFieldPattern.ADVANCED_GLAUCOMA:
            # Dense bi-hemifield scotoma, with absolute defect (< 0 dB device)
            base_t = 30.0 - (eccentricity * 0.28)
            if y > 0:
                # Severe superior loss: deep scotoma (0 dB down to absolute -4 dB)
                t_true = base_t - 32.0
            elif y < 0 and abs(x) >= 9:
                # Inferior arcuate loss
                t_true = base_t - 22.0
            else:
                # Residual macular island
                t_true = base_t - 10.0
            # Allow true values to go below 0 dB to test scotoma floor censoring
            field[(x, y)] = float(t_true)

    return field


class SyntheticObserver:
    """
    Simulates a human visual field test taker.
    """
    def __init__(
        self,
        true_thresholds: Dict[Tuple[float, float], float],
        fp_rate: float = 0.03,
        fn_rate: float = 0.03,
        slope: float = 1.2,
        seed: int = None
    ):
        self.true_thresholds = true_thresholds
        self.fp_rate = fp_rate
        self.fn_rate = fn_rate
        self.slope = slope
        self.rng = np.random.default_rng(seed)

    def respond(self, x_deg: float, y_deg: float, stimulus_db: float) -> Tuple[bool, float]:
        """
        Samples whether the observer sees the stimulus at (x, y) with intensity stimulus_db.
        Returns: (seen: bool, reaction_time_ms: float)
        """
        t_true = self.true_thresholds.get((x_deg, y_deg), 25.0)

        # Psychometric cumulative Gaussian:
        # Stimulus is seen when it is brighter than or equal to threshold.
        # Since lower dB = brighter stimulus: z = (T_true - stimulus_db) / slope
        z = (t_true - stimulus_db) / self.slope
        p_seen = self.fp_rate + (1.0 - self.fp_rate - self.fn_rate) * norm.cdf(z)

        # Draw response
        seen = bool(self.rng.random() < p_seen)

        # Reaction time model: Log-Normal with Piéron-style latency shift near threshold
        if seen:
            intensity_factor = max(0.0, (t_true - stimulus_db))
            latency_shift = max(0.0, 80.0 - 5.0 * intensity_factor)
            rt_ms = float(self.rng.lognormal(mean=math.log(340.0), sigma=0.22) + latency_shift)
        else:
            rt_ms = -1.0

        return seen, rt_ms
