"""
Python Vectorized Implementation of Bayesian ZEST Perimetry Strategy.
Mirrors Unity C# ZestStrategy for Monte Carlo simulation.
"""

import math
from typing import Tuple
import numpy as np
from scipy.stats import norm


class SimZestStrategy:
    def __init__(
        self,
        domain_min: float = -5.0,
        domain_max: float = 40.0,
        domain_step: float = 0.5,
        gamma: float = 0.03,
        lambda_: float = 0.03,
        slope: float = 1.2,
        stop_sd: float = 1.5,
        min_pres: int = 2,
        max_pres: int = 5
    ):
        self.domain = np.arange(domain_min, domain_max + domain_step * 0.5, domain_step)
        self.gamma = gamma
        self.lambda_ = lambda_
        self.slope = slope
        self.stop_sd = stop_sd
        self.min_pres = min_pres
        self.max_pres = max_pres

        self.pdf = np.zeros_like(self.domain)
        self.presentation_count = 0
        self.is_complete = False
        self.last_stimulus_db = 25.0
        self.consecutive_misses = 0
        self.consecutive_misses_at_zero = 0

    def initialize(self, prior_mean: float = 28.0, prior_sd: float = 4.0):
        self.presentation_count = 0
        self.is_complete = False
        self.consecutive_misses = 0
        self.consecutive_misses_at_zero = 0

        # Gaussian prior
        diff = self.domain - prior_mean
        self.pdf = np.exp(-(diff ** 2) / (2.0 * (prior_sd ** 2)))
        sum_p = np.sum(self.pdf)
        if sum_p > 1e-6:
            self.pdf /= sum_p
        else:
            self.pdf = np.ones_like(self.domain) / len(self.domain)

    def get_next_stimulus(self) -> float:
        # Floor scotoma probe rule: if patient misses 3 consecutive stimuli, probe at 0 dB immediately
        if self.consecutive_misses >= 3:
            self.last_stimulus_db = 0.0
            return 0.0

        mean_t = np.sum(self.domain * self.pdf)
        # Round to nearest 0.5 dB and clamp to physical device range [0, 35] dB
        self.last_stimulus_db = float(np.clip(np.round(mean_t * 2.0) / 2.0, 0.0, 35.0))
        return self.last_stimulus_db

    def process_response(self, user_responded: bool, reaction_time_ms: float = 0.0):
        self.presentation_count += 1

        if user_responded:
            self.consecutive_misses = 0
        else:
            self.consecutive_misses += 1
            if self.last_stimulus_db <= 0.5:
                self.consecutive_misses_at_zero += 1

        # Vectorized likelihood across all domain values:
        # P(Seen | stimulus, T) is high when T >= stimulus (stimulus is brighter than threshold)
        z = (self.domain - self.last_stimulus_db) / self.slope
        p_seen = self.gamma + (1.0 - self.gamma - self.lambda_) * norm.cdf(z)

        likelihood = p_seen if user_responded else (1.0 - p_seen)
        self.pdf *= likelihood

        # Normalize posterior
        s = np.sum(self.pdf)
        if s > 1e-12:
            self.pdf /= s

        # Check stopping criteria
        mean_t = np.sum(self.domain * self.pdf)
        variance = np.sum(((self.domain - mean_t) ** 2) * self.pdf)
        current_sd = math.sqrt(max(0.0, variance))

        if self.presentation_count >= self.min_pres:
            if current_sd <= self.stop_sd or self.presentation_count >= self.max_pres:
                self.is_complete = True
            if self.consecutive_misses_at_zero >= 2:
                self.is_complete = True

    def get_final_result(self) -> Tuple[float, float, str, int]:
        """
        Returns: (estimated_threshold_db, posterior_sd, censoring_status, presentation_count)
        """
        mean_t = float(np.sum(self.domain * self.pdf))
        variance = float(np.sum(((self.domain - mean_t) ** 2) * self.pdf))
        sd_t = math.sqrt(max(0.0, variance))

        censoring = "MEASURED"
        if self.consecutive_misses_at_zero >= 1 or mean_t < 0.5:
            censoring = "FLOOR_CENSORED"
            mean_t = 0.0
        elif mean_t > 35.0:
            censoring = "CEILING_CENSORED"

        return mean_t, sd_t, censoring, self.presentation_count
