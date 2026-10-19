# OpenPerimeter Healthy-Eye Pilot Bench Study Agreement Analysis Report
**Status:** RESEARCH USE ONLY - NOT FOR CLINICAL DECISIONS  
**Evaluated Pairs:** N = 1620 points (0 scotoma floor-censored)  

---

> [!WARNING]
> **RESEARCH USE ONLY - NOT FOR CLINICAL DIAGNOSIS**  
> This analysis is intended solely for scientific investigation into structure-function discordance in glaucoma. Decibel values reflect device-specific calibrated dynamic ranges.

---

## 1. Bland-Altman Agreement Statistics
- **Mean Systematic Bias:** **-0.22 dB** (95% CI: [-0.30, -0.14] dB)
- **Standard Deviation of Differences:** 1.66 dB
- **95% Limits of Agreement (LoA):**
  - **Upper LoA (+1.96 SD):** **+3.03 dB** (95% CI: [+2.89, +3.17] dB)
  - **Lower LoA (-1.96 SD):** **-3.47 dB** (95% CI: [-3.61, -3.33] dB)
- **Proportional Bias Test:** Slope = +0.032 (p = 0.1094)

---

## 2. Correlation and Intraclass Agreement
- **Intraclass Correlation Coefficient ICC(2,1):** **0.696** (95% CI: [0.676, 0.716], F = 8.07, p < 0.001)
- **Pearson Linear Correlation ($r$):** **0.724** (p < 0.001, N = 1620)
- **Spearman Rank Correlation ($\rho$):** **0.711** (p < 0.001, N = 1620)  
  *(Note: Spearman correlation preserves monotonic scotoma ranking with tied floor-censored points).*


### 3. Test-Retest Repeatability (Precision)
- **Coefficient of Repeatability (CR):** 3.30 dB (95% difference expected within +- 3.30 dB)
- **Within-Subject Standard Deviation ($s_w$):** 1.19 dB
- **Test-Retest Mean Difference:** -0.04 dB


---

## 4. Methodological Conclusions
1. The systematic bias (-0.22 dB) indicates negligible systematic drift when using the calibrated luminance LUT.
2. The 95% limits of agreement ([-3.47, +3.03] dB) align with inter-test variability observed between commercial perimeters in moderate-to-advanced glaucoma.
3. Points marked as floor-censored (0 points) reflect true display dynamic range boundaries and require Tobit regression or non-parametric analysis when correlating with OCT GCC thickness.
