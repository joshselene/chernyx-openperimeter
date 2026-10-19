# OpenPerimeter: Healthy-Eye Pilot Bench Study Protocol (Phase 5)
**Document Version:** 1.0.0-FINAL  
**Protocol Code:** OP-PILOT-HEALTHY-2026-V1  
**Classification:** Research Use Only (RUO) - Investigational Protocol Guidance  
**Target Population:** Healthy Adult Volunteers (N = 30 eyes)  
**Date:** October 2026  

---

> [!WARNING]
> **RESEARCH USE ONLY - NOT FOR CLINICAL DIAGNOSIS OR DECISION-MAKING**  
> OpenPerimeter is an investigational static automated perimeter designed strictly for academic and clinical research into structure-function discordance in glaucoma. This bench study protocol must be executed and approved under Institutional Ethics Committee (IEC) / Institutional Review Board (IRB) oversight prior to enrolling any glaucoma patients.

---

## 1. Study Rationale and Objectives

### 1.1 Why Healthy Eyes First?
Before subjecting vulnerable glaucoma patients (particularly those with moderate-to-advanced visual field defects who suffer from severe perimetric fatigue) to an investigational smartphone-in-VR perimeter, it is imperative to establish:
1. **Instrument Test-Retest Repeatability ($CR$):** Determine the baseline within-subject variability ($s_w$) of OpenPerimeter in stable, reliable normal eyes.
2. **Systematic Bias against Clinical Standard:** Quantify the mean decibel offset and 95% Limits of Agreement between OpenPerimeter, Humphrey Field Analyzer 3 (HFA3 SITA Standard), and Elisar AVA.
3. **Hardware Stability and Usability:** Confirm optical alignment stability, absence of thermal luminance throttling, IMU head-drift frequency, and patient comfort in Cardboard-style VR viewers.

### 1.2 Primary and Secondary Objectives
- **Primary Objective 1:** Determine the test-retest Coefficient of Repeatability ($CR = 1.96 \cdot \sqrt{2} \cdot s_w$) of OpenPerimeter 24-2 thresholds in healthy eyes.
- **Primary Objective 2:** Evaluate Bland-Altman agreement (systematic bias and 95% LoA) between OpenPerimeter and HFA3 SITA Standard.
- **Secondary Objective 1:** Estimate the Intraclass Correlation Coefficient ($\text{ICC}(2,1)$, two-way random, absolute agreement) across OpenPerimeter, HFA3, and Elisar AVA.
- **Secondary Objective 2:** Assess visual fatigue, VR viewer comfort (100 mm Visual Analog Scale), and presentation timing integrity (frame drops).

---

## 2. Participant Eligibility and Sample Size

### 2.1 Sample Size Calculation
- Based on Bland-Altman limits of agreement precision (Bland & Altman 1999):
  To estimate the 95% limits of agreement with a confidence interval width $\le \pm 0.35 \cdot s_d$, a sample size of **$N = 30$ eyes** (from 30 unique participants, one study eye randomly selected) provides $80\%$ statistical power at two-tailed $\alpha = 0.05$ for detecting an $\text{ICC} \ge 0.85$ against a null hypothesis of $\text{ICC} \le 0.65$.

### 2.2 Inclusion Criteria
1. Age $20$ to $40$ years (to minimize pre-existing lens opacities, senile miosis, and subclinical neurodegeneration).
2. Best-Corrected Visual Acuity (BCVA) $\ge 20/25$ ($0.1\text{ logMAR}$) in the study eye.
3. Spherical equivalent refraction between $-5.00\text{ D}$ and $+2.00\text{ D}$, with cylinder $\le 1.50\text{ D}$.
4. Intraocular Pressure (IOP) $\le 21\text{ mmHg}$ measured by Goldmann applanation tonometry.
5. Normal anterior and posterior segment examination on slit-lamp biomicroscopy.
6. Reliable normal baseline HFA 24-2 SITA Standard test:
   - Mean Deviation (MD) between $-2.00\text{ dB}$ and $+2.00\text{ dB}$.
   - Pattern Standard Deviation (PSD) within $95\%$ normal limits ($p > 0.05$).
   - Fixation losses $< 20\%$, False positives $< 15\%$, False negatives $< 15\%$.

### 2.3 Exclusion Criteria
1. History of glaucoma, ocular hypertension, or family history of glaucoma in a first-degree relative.
2. Any history of ocular surgery, corneal disease, retinal disease, or optic neuropathy.
3. Systemic conditions affecting the visual pathway (diabetes mellitus, multiple sclerosis, pituitary adenoma).
4. Pronounced susceptibility to motion sickness or cybersickness in VR headsets.

---

## 3. Multi-Device Study Design and Randomization

Each participant undergoes two identical study visits separated by **1 to 7 days** at the same time of day ($\pm 2\text{ hours}$) to control for diurnal variation.

```
+-----------------------------------------------------------------------------------+
|                        Healthy Pilot Bench Protocol Flow                          |
+-----------------------------------------------------------------------------------+
|                                                                                   |
|  [Visit 1: Baseline Agreement & Run 1]                                            |
|   1. Written Informed Consent (DPDP Act 2023 Digital Timestamp)                   |
|   2. Comprehensive Ophthalmic Exam (BCVA, Slit-lamp, Tonometry, Fundus)          |
|   3. Randomization to 1 of 6 Device Sequences (15-min rest between devices)       |
|      - Device A: OpenPerimeter (Calibrated LUT, 24-2, ZEST)                       |
|      - Device B: Humphrey Field Analyzer 3 (24-2, SITA Standard)                  |
|      - Device C: Elisar Advanced Vision Analyser (24-2 AVA)                       |
|   4. Usability and VR Comfort Survey (VAS scale 0-100)                            |
|                                                                                   |
|  [Visit 2: Test-Retest Reliability (1 to 7 days later)]                           |
|   1. Repeat identical 3-device sequence in counterbalanced order                  |
|   2. OpenPerimeter Run 2 (Evaluates test-retest CR against Run 1)                 |
|   3. Final study debriefing and data export                                       |
+-----------------------------------------------------------------------------------+
```

### 3.1 Counterbalanced Randomization (Latin Square)
Participants are randomized in equal blocks to one of six sequences:
1. `OpenPerimeter -> HFA -> AVA`
2. `OpenPerimeter -> AVA -> HFA`
3. `HFA -> OpenPerimeter -> AVA`
4. `HFA -> AVA -> OpenPerimeter`
5. `AVA -> HFA -> OpenPerimeter`
6. `AVA -> OpenPerimeter -> HFA`

**Mandatory Rest Period:** Exactly **15 minutes** in ambient photopic light ($150\text{ lux}$) between device changes to dissipate retinal bleaching and cognitive fatigue.

---

## 4. Operational Setup for OpenPerimeter Bench Testing

### 4.1 Hardware Pre-Flight Verification
1. Target Smartphone (e.g., Google Pixel 7/8 or Samsung S23/S24) cleaned and charged to $> 80\%$.
2. VR Viewer (Google Cardboard v2 or fixed 45 mm focal length viewer) verified for lens cleanliness and optical center alignment ($64\text{ mm}$ IPD).
3. Calibration file `calibration.json` verified on device; UI confirms **"RESEARCH CALIBRATED MODE"**.
4. Spot photometer behind-the-lens confirmation:
   - Non-tested eye viewport $= 0.00\text{ cd/m}^2$.
   - Tested eye background $= 10.0\text{ cd/m}^2 \pm 0.2\text{ cd/m}^2$.
5. Bluetooth HID clicker paired and test-clicked to verify millisecond response logging.
6. Android "Do Not Disturb" active, screen brightness locked, and screen pinning enabled.

### 4.2 Test Execution Protocol
1. Participant seated comfortably with VR viewer secured via head-strap.
2. Monocular occlusion: Tested eye aligned with active viewport; non-tested eye views uniform black screen (physical eye patch placed over fellow eye if binocular rivalry reported).
3. Fixation target set to Central Crosshair. Participant instructed: *"Keep looking steadily at the central amber cross. Whenever you see a small white light flash anywhere in your peripheral vision, click the button immediately. Do not search for the lights with your eyes."*
4. Run standard 24-2 ZEST program.
5. App automatically monitors head drift via IMU; if head rotates $> 3.0^\circ$, audio cue prompts subject to realign.

---

## 5. Statistical Analysis Plan and Publication Endpoints

All data will be processed using the OpenPerimeter Python analysis package (`openperimeter.analysis`):

### 5.1 Primary Endpoint Analysis: Test-Retest Repeatability ($CR$)
- Evaluated on paired OpenPerimeter tests (Visit 1 vs. Visit 2):
  $$s_w = \sqrt{\frac{\sum_{i=1}^{N} (T_{i, \text{visit1}} - T_{i, \text{visit2}})^2}{2N}}$$
  $$CR = 1.96 \cdot \sqrt{2} \cdot s_w = 2.77 \cdot s_w$$
- Evaluated pointwise and globally for Mean Deviation (MD).
- Compared against published HFA SITA Standard repeatability ($CR \approx 1.5 - 2.5\text{ dB}$ in healthy eyes).

### 5.2 Primary Endpoint Analysis: Bland-Altman Agreement
- Comparison of OpenPerimeter Visit 1 vs. HFA3 Visit 1:
  - Mean systematic bias: $\bar{d} = \frac{1}{M} \sum (T_{\text{OpenPerimeter}} - T_{\text{HFA}})$.
  - 95% Limits of Agreement: $\bar{d} \pm 1.96 \cdot s_d$.
  - Proportional bias linear regression test: $d_i = \beta_0 + \beta_1 \cdot \text{Mean}_i$.

### 5.3 Secondary Endpoint: Intraclass Correlation ($\text{ICC}(2,1)$)
- Two-way random effects, single rater, absolute agreement $\text{ICC}(2,1)$ across OpenPerimeter, HFA3, and Elisar AVA.

---

## 6. Pre-Specified "Go / No-Go" Criteria for Glaucoma Patient Cohort

Before opening enrollment to the clinical glaucoma structure-function discordance study, the healthy-eye pilot bench study must satisfy all the following **Go / No-Go Gates**:

| Quality Gate | Metric | Go Criteria | No-Go Action |
| :---: | :--- | :--- | :--- |
| **GATE-1** | Test-Retest Repeatability ($CR$) | Global $CR \le 3.50\text{ dB}$ ($s_w \le 1.25\text{ dB}$) on healthy eyes | Re-evaluate timing jitter, clicker latency, and prior SD parameterization. |
| **GATE-2** | Systematic Bias vs. HFA | Global Mean Bias $\le \pm 1.50\text{ dB}$ | Recalibrate behind-the-lens luminance LUT; re-measure photometer gamma curve. |
| **GATE-3** | Limits of Agreement (LoA) | 95% LoA span ($+1.96 SD - (-1.96 SD)$) $\le 7.00\text{ dB}$ | Inspect lens distortion profile ($k_1, k_2$) and visual angle projection geometry. |
| **GATE-4** | Test Duration | Mean test time for 24-2 $\le 5.5\text{ minutes}$ | Optimize ZEST stopping rules or review inter-stimulus interval. |
| **GATE-5** | Reliability Rates | Healthy subjects achieve $\text{FL} < 20\%$, $\text{FP} < 15\%$, $\text{FN} < 15\%$ in $\ge 90\%$ of sessions | Verify blind-spot presentation coordinates ($(15^\circ, -2^\circ)$). |
| **GATE-6** | Display Timing Integrity | 0 frame drops reported during 200 ms presentation in $100\%$ of sessions | Investigate phone background tasks, CPU governor, or URP shader complexity. |

---

## 7. Data Protection, Privacy, and DPDP Act 2023 Compliance

1. **Pseudonymous Participant Codes:** Assigned in format `OP-HLTH-001` through `OP-HLTH-030`. No names or contact numbers stored in the app or SQLite database.
2. **Digital Consent Record:** The app records the timestamp of verified written informed consent prior to unlocking test execution.
3. **Local Encrypted Storage:** AES-256 encrypted SQLite file stored locally on the research device. Zero internet or cloud permissions in Android manifest.
4. **Data Portability and Deletion:** At study completion or upon participant withdrawal, data can be exported in standardized CSV/JSON format or permanently purged via the secure erase tool.
