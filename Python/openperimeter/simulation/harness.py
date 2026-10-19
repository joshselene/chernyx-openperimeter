"""
Monte Carlo Simulation Harness for OpenPerimeter Threshold Engine.

Simulates thousands of visual field tests to measure:
1. Systematic Bias (mean estimation error in dB)
2. Precision / RMSE (Root Mean Squared Error in dB)
3. Test Duration and Presentation Counts
4. Scotoma Floor Censoring Accuracy
"""

import time
import math
from typing import Dict, Any, List, Tuple
import numpy as np
import pandas as pd

from .observer import (
    VisualFieldPattern,
    create_visual_field_model,
    generate_24_2_coordinates,
    SyntheticObserver,
)
from .zest_sim import SimZestStrategy
from .staircase_sim import SimStaircase42Strategy


def get_euclidean_neighbors(pt: Tuple[float, float], all_pts: List[Tuple[float, float]], radius: float = 8.5) -> List[Tuple[float, float]]:
    """Returns adjacent 24-2 visual field points within standard 6-degree neighbor radius."""
    x0, y0 = pt
    neighbors = []
    for x, y in all_pts:
        if (x, y) != pt:
            dist = math.sqrt((x - x0) ** 2 + (y - y0) ** 2)
            if dist <= radius:
                neighbors.append((x, y))
    return neighbors


def run_single_session(
    coords: List[Tuple[float, float]],
    true_field: Dict[Tuple[float, float], float],
    observer: SyntheticObserver,
    strategy_type: str = "ZEST",
    use_neighbor_priors: bool = True
) -> Dict[str, Any]:
    """
    Simulates one complete 54-point 24-2 perimetry test session.
    """
    results = {}
    completed_thresholds = {}
    total_presentations = 0
    total_test_time_sec = 0.0

    # Primary seed points in 4 quadrants tested first to establish regional gradient
    seed_points = [(-9.0, 9.0), (9.0, 9.0), (-9.0, -9.0), (9.0, -9.0)]
    remaining_points = [pt for pt in coords if pt not in seed_points]
    # Pseudo-randomize remaining points to prevent localized retinal adaptation
    np.random.shuffle(remaining_points)
    execution_order = seed_points + remaining_points

    for pt in execution_order:
        x, y = pt

        # Determine prior: broad weakly-informative prior prevents scotoma truncation
        if use_neighbor_priors and len(completed_thresholds) > 0:
            neighbors = get_euclidean_neighbors(pt, coords)
            completed_neighbor_vals = [completed_thresholds[n] for n in neighbors if n in completed_thresholds]
            if len(completed_neighbor_vals) > 0:
                prior_mean = float(np.mean(completed_neighbor_vals))
                prior_sd = 6.5  # Realistic neighbor correlation
            else:
                prior_mean = 28.0
                prior_sd = 8.5
        else:
            prior_mean = 28.0
            prior_sd = 8.5

        if strategy_type == "ZEST":
            engine = SimZestStrategy(stop_sd=1.5, min_pres=2, max_pres=5)
            engine.initialize(prior_mean=prior_mean, prior_sd=prior_sd)
        else:
            engine = SimStaircase42Strategy(max_pres=8)
            engine.initialize(prior_mean=prior_mean)

        while not engine.is_complete:
            stim_db = engine.get_next_stimulus()
            seen, rt_ms = observer.respond(x, y, stim_db)
            engine.process_response(seen, rt_ms)

            # Presentation timing simulation:
            # Response window starts at stimulus onset (up to 1.000s) + 600ms inter-stimulus interval
            duration = ((rt_ms / 1000.0) if seen else 1.000) + 0.600
            total_test_time_sec += duration

        t_est, sd_est, censoring, pres_count = engine.get_final_result()
        results[pt] = {
            "t_true": true_field[pt],
            "t_est": t_est,
            "sd_est": sd_est,
            "censoring": censoring,
            "pres_count": pres_count
        }
        completed_thresholds[pt] = t_est
        total_presentations += pres_count

    return {
        "point_results": results,
        "total_presentations": total_presentations,
        "mean_pres_per_point": total_presentations / len(coords),
        "total_test_time_sec": total_test_time_sec,
    }


def run_monte_carlo_evaluation(
    n_runs: int = 100,
    strategy_type: str = "ZEST",
    pattern_type: VisualFieldPattern = VisualFieldPattern.NORMAL_HEALTHY,
    fp_rate: float = 0.03,
    fn_rate: float = 0.03,
    use_neighbor_priors: bool = True,
    seed: int = 42
) -> Dict[str, Any]:
    """
    Executes N Monte Carlo simulated visual field sessions and computes statistical metrics.
    """
    coords = generate_24_2_coordinates()
    true_field = create_visual_field_model(pattern_type)
    rng = np.random.default_rng(seed)

    all_errors = []
    scotoma_errors = []
    pres_counts = []
    durations_sec = []
    correctly_censored_count = 0
    total_true_floor_scotoma_points = 0

    for i in range(n_runs):
        sub_seed = int(rng.integers(1, 10000000))
        observer = SyntheticObserver(true_field, fp_rate=fp_rate, fn_rate=fn_rate, seed=sub_seed)
        session = run_single_session(coords, true_field, observer, strategy_type, use_neighbor_priors)

        pres_counts.append(session["mean_pres_per_point"])
        durations_sec.append(session["total_test_time_sec"])

        for pt, data in session["point_results"].items():
            t_true = data["t_true"]
            t_est = data["t_est"]
            censoring = data["censoring"]

            if t_true < 0.0:
                # True absolute scotoma
                total_true_floor_scotoma_points += 1
                if censoring == "FLOOR_CENSORED":
                    correctly_censored_count += 1
            else:
                # Measurable sensitivity
                err = t_est - t_true
                all_errors.append(err)
                if t_true <= 18.0:
                    scotoma_errors.append(err)

    all_errors = np.array(all_errors)
    mean_bias = float(np.mean(all_errors)) if len(all_errors) > 0 else 0.0
    rmse = float(np.sqrt(np.mean(all_errors ** 2))) if len(all_errors) > 0 else 0.0

    scotoma_rmse = float(np.sqrt(np.mean(np.array(scotoma_errors) ** 2))) if len(scotoma_errors) > 0 else 0.0
    censoring_accuracy = (
        (correctly_censored_count / total_true_floor_scotoma_points * 100.0)
        if total_true_floor_scotoma_points > 0
        else 100.0
    )

    report = {
        "n_runs": n_runs,
        "strategy": strategy_type,
        "pattern": pattern_type.value,
        "fp_rate": fp_rate,
        "fn_rate": fn_rate,
        "use_neighbor_priors": use_neighbor_priors,
        "mean_bias_db": round(mean_bias, 3),
        "rmse_db": round(rmse, 3),
        "scotoma_rmse_db": round(scotoma_rmse, 3),
        "mean_pres_per_point": round(float(np.mean(pres_counts)), 2),
        "mean_test_duration_min": round(float(np.mean(durations_sec)) / 60.0, 2),
        "censoring_accuracy_pct": round(censoring_accuracy, 1),
    }

    return report
