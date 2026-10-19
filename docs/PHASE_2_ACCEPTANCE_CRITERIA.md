# OpenPerimeter: Phase 2 Acceptance Criteria (Threshold Engine & Simulation)
**Document Version:** 1.0.0  
**Phase:** Phase 2 - Bayesian ZEST, 4-2 Staircase, and Synthetic Observer Monte Carlo Simulation  
**Date:** October 2026  

---

> [!IMPORTANT]
> **PRE-SPECIFIED SIMULATION ACCEPTANCE GATES**  
> In accordance with perimetric engineering standards, all mathematical and performance acceptance criteria for the threshold engine must be explicitly documented and committed to source control **PRIOR to running simulation and validation tests**.

---

## 1. Primary Simulation Acceptance Criteria

Monte Carlo simulations are evaluated across $N = 1,000$ synthetic test sessions per condition using the standard 24-2 visual field pattern (54 points):

### Criterion 1: Normal Visual Field Accuracy (Healthy Observer)
- **Profile:** True sensitivity $T \in [24, 34]\text{ dB}$ (hill of vision), False Positive Rate $\gamma = 3\%$, False Negative Rate $\lambda = 3\%$.
- **Acceptance Gate 1A (Bias):** Global Mean Bias $\le \pm 0.30\text{ dB}$ ($\frac{1}{N} \sum (\hat{T} - T_{\text{true}})$).
- **Acceptance Gate 1B (Precision / RMSE):** Root Mean Squared Error (RMSE) $\le 2.50\text{ dB}$.
- **Acceptance Gate 1C (Test Efficiency):** Mean presentation count per point $\le 4.6$ presentations (Total test presentations $\le 250$ for 54 points; estimated test time $\le 5.5$ minutes).

### Criterion 2: Moderate Glaucoma Field (HPA Moderate: MD -6.01 to -12 dB)
- **Profile:** Superior arcuate scotoma ($T \in [8, 16]\text{ dB}$), normal inferior hemifield ($T \in [26, 32]\text{ dB}$).
- **Acceptance Gate 2A (Bias):** Global Mean Bias $\le \pm 0.60\text{ dB}$.
- **Acceptance Gate 2B (RMSE in Scotoma):** RMSE within scotoma points $\le 4.50\text{ dB}$.
- **Acceptance Gate 2C (Neighbor-Informed Seed Benefit):** Convergence speed improved by $\ge 10\%$ in presentations per scotoma point compared to uninformed flat priors.

### Criterion 3: Advanced Glaucoma Field (HPA Advanced: MD < -12 dB)
- **Profile:** Dense bi-hemifield scotoma ($T < 5\text{ dB}_{\text{device}}$), including absolute defect points ($T < 0\text{ dB}_{\text{device}}$).
- **Acceptance Gate 3A (Floor Censoring Accuracy):** Points with true sensitivity $< 0\text{ dB}_{\text{device}}$ are correctly classified as `FLOOR_CENSORED` in $\ge 92.0\%$ of runs (accounting for stochastic 3% FP false clicking at 0 dB).
- **Acceptance Gate 3B (No Silent Clamping):** Absolute zero instances of negative dB values or silent truncation to arbitrary values in export.

### Criterion 4: Staircase 4-2 dB Fallback Strategy
- **Acceptance Gate 4A (Reversal Logic):** Verifies double-crossing staircase terminates at the second reversal point.
- **Acceptance Gate 4B (Efficiency):** Mean presentations per point $\le 6.5$.
- **Acceptance Gate 4C (Normal Bias):** Bias $\le \pm 0.60\text{ dB}$, RMSE $\le 2.00\text{ dB}$.

### Criterion 5: Reliability & Catch Trial Classification
- **Acceptance Gate 5A (FP Identification):** Catch trials accurately detect simulated false-positive clicking rates within $\pm 3.0\%$ margin of error.
- **Acceptance Gate 5B (FN Identification):** Supra-threshold $+9\text{ dB}$ catch trials accurately detect simulated lapse rates within $\pm 3.0\%$ margin of error.
