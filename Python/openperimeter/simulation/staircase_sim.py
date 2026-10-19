"""
Python Implementation of 4-2 dB Staircase Perimetry Strategy.
Mirrors Unity C# Staircase42Strategy for simulation.
"""

from typing import Tuple
import numpy as np


class SimStaircase42Strategy:
    def __init__(self, max_pres: int = 8):
        self.max_pres = max_pres
        self.presentation_count = 0
        self.is_complete = False
        self.current_stimulus_db = 24.0
        self.current_step_size_db = 4.0
        self.reversal_count = 0
        self.last_response_direction = 0  # +1 seen, -1 not seen
        self.last_seen_db = -1.0
        self.last_unseen_db = -1.0
        self.consecutive_misses_at_zero = 0

    def initialize(self, prior_mean: float = 24.0):
        self.presentation_count = 0
        self.is_complete = False
        self.reversal_count = 0
        self.current_step_size_db = 4.0
        self.last_response_direction = 0
        self.last_seen_db = -1.0
        self.last_unseen_db = -1.0
        self.consecutive_misses_at_zero = 0
        # Start at nearest even integer
        self.current_stimulus_db = float(np.clip(round(prior_mean / 2.0) * 2.0, 4.0, 30.0))

    def get_next_stimulus(self) -> float:
        return self.current_stimulus_db

    def process_response(self, user_responded: bool, reaction_time_ms: float = 0.0):
        self.presentation_count += 1

        if user_responded:
            self.last_seen_db = self.current_stimulus_db
            direction = +1  # step up (dimmer)
        else:
            self.last_unseen_db = self.current_stimulus_db
            direction = -1  # step down (brighter)
            if self.current_stimulus_db <= 0.0:
                self.consecutive_misses_at_zero += 1

        if self.last_response_direction != 0 and direction != self.last_response_direction:
            self.reversal_count += 1
            self.current_step_size_db = 2.0

        self.last_response_direction = direction

        if self.reversal_count >= 2:
            self.is_complete = True
            return

        if self.consecutive_misses_at_zero >= 2:
            self.is_complete = True
            return

        if self.presentation_count >= self.max_pres:
            self.is_complete = True
            return

        next_db = self.current_stimulus_db + (direction * self.current_step_size_db)
        self.current_stimulus_db = float(np.clip(next_db, 0.0, 36.0))

        if self.current_stimulus_db <= 0.0 and direction == -1 and self.presentation_count >= 3:
            self.is_complete = True
        elif self.current_stimulus_db >= 36.0 and direction == +1 and self.presentation_count >= 3:
            self.is_complete = True

    def get_final_result(self) -> Tuple[float, float, str, int]:
        if self.last_seen_db >= 0.0 and hasattr(self, 'last_unseen_db') and self.last_unseen_db >= 0.0:
            final_t = (self.last_seen_db + self.last_unseen_db) / 2.0
        elif self.last_seen_db >= 0.0:
            final_t = self.last_seen_db
        else:
            final_t = 0.0

        censoring = "MEASURED"
        if self.consecutive_misses_at_zero >= 2 or self.last_seen_db < 0.0:
            censoring = "FLOOR_CENSORED"
            final_t = 0.0
        elif final_t >= 34.0:
            censoring = "CEILING_CENSORED"

        sd = 1.41 if self.reversal_count >= 2 else 2.50
        return final_t, sd, censoring, self.presentation_count
