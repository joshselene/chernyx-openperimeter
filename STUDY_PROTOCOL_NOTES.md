# OpenPerimeter: Study Protocol & Structure-Function Methodology Notes
**Document Version:** 1.0.0-PROPOSED  
**Classification:** Research Use Only (RUO) - Investigational Protocol Guidance  
**Target Study:** Structure-Function Discordance in Moderate-to-Advanced Glaucoma  
**Date:** October 2026  

---

> [!WARNING]
> **RESEARCH USE ONLY - NO CLINICAL CLAIMS**  
> This protocol is intended strictly for observational clinical research under Institutional Review Board (IRB) / Institutional Ethics Committee (IEC) oversight. OpenPerimeter is an investigational research tool, not a diagnostic device.

---

## 1. Research Objectives and Clinical Rationale

### 1.1 Study Focus: Moderate-to-Advanced Glaucoma
In early glaucoma, structural changes (retinal nerve fiber layer [RNFL] thinning, ganglion cell complex [GCC] loss on OCT) often precede detectable visual field defects. However, in **moderate-to-advanced glaucoma**, structural measurements experience a biological "measurement floor":
- Beyond an MD of $-10$ to $-15\text{ dB}$, RNFL thickness approaches residual glial tissue levels ($\sim 40 - 50\ \mu\text{m}$), losing dynamic resolution.
- Conversely, functional visual field assessment (perimetry) becomes the **sole modality capable of monitoring progressive field loss** and threatening fixation.
- **The Core Scientific Problem:** Evaluating whether low-cost, portable smartphone-in-VR perimetry (OpenPerimeter) demonstrates acceptable agreement with the clinical reference standard (Humphrey Field Analyzer [HFA] SITA Standard) and contemporary commercial VR perimetry (Elisar AVA) in eyes with moderate-to-advanced visual field defects.

### 1.2 Severity Classification (Hodapp-Parrish-Anderson Criteria)
Participants are stratified according to baseline HFA SITA Standard 24-2 Mean Deviation (MD):
1. **Moderate Glaucoma:** HFA MD between $-6.01\text{ dB}$ and $-12.00\text{ dB}$, OR visual field defect meeting HPA moderate criteria (e.g., scotoma in both hemifields or within $5^\circ$ of fixation).
2. **Advanced Glaucoma:** HFA MD $< -12.00\text{ dB}$, OR deep scotoma extending across both hemifields with fixation threat.
3. **Healthy Normative Controls:** Normal anterior/posterior segment exam, intraocular pressure $\le 21\text{ mmHg}$, normal optic nerve head appearance, normal OCT RNFL/GCC, and reliable normal HFA ($MD > -2.00\text{ dB}$, $PSD$ within $95\%$ normal limits).

---

## 2. Multi-Device Comparative Protocol (HFA vs. Elisar AVA vs. OpenPerimeter)

### 2.1 Devices Under Evaluation
1. **Humphrey Field Analyzer 3 (HFA3):** Carl Zeiss Meditec. SITA Standard strategy, 24-2 and 10-2 test grids, Goldmann Size III, background $31.5\text{ asb}$ ($10\text{ cd/m}^2$).
2. **Elisar Advanced Vision Analyser (AVA):** Head-mounted VR static automated perimeter. Full threshold / fast threshold strategy, 24-2 and 10-2 programs, Goldmann Size III.
3. **OpenPerimeter:** Smartphone-in-VR perimeter. ZEST strategy with neighbor-informed priors, 24-2 and 10-2 programs, calibrated background $10\text{ cd/m}^2$, calibrated luminance LUT.

### 2.2 Counterbalanced Randomization of Device Order
Perimetric fatigue and learning effects are significant confounding variables in elderly glaucoma patients. Testing across three devices on the same subject must be counterbalanced:
- Participants are assigned to one of six randomized testing sequences in a $1:1:1:1:1:1$ ratio:
  - **Seq 1:** HFA $\rightarrow$ OpenPerimeter $\rightarrow$ AVA
  - **Seq 2:** HFA $\rightarrow$ AVA $\rightarrow$ OpenPerimeter
  - **Seq 3:** OpenPerimeter $\rightarrow$ HFA $\rightarrow$ AVA
  - **Seq 4:** OpenPerimeter $\rightarrow$ AVA $\rightarrow$ HFA
  - **Seq 5:** AVA $\rightarrow$ HFA $\rightarrow$ OpenPerimeter
  - **Seq 6:** AVA $\rightarrow$ OpenPerimeter $\rightarrow$ HFA
- **Mandatory Rest Interval:** A mandatory minimum rest period of **15 minutes** in photopic room light ($100 - 200\text{ lux}$) is enforced between device tests to clear retinal and cognitive fatigue.

---

## 3. Structural-Functional Correlation with OCT GCC (Macular Sectors)

### 3.1 Macular 10-2 and GCC Sector Mapping
Central visual field points (from the 10-2 pattern, and central 4 points of 24-2) directly correlate with macular ganglion cell loss measured via spectral-domain OCT (e.g., Zeiss Cirrus, Heidelberg Spectralis, or Optovue Solix GCC).
- Rather than hard-coding anatomical maps into the software engine, OpenPerimeter loads a researcher-supplied configuration file: `sector_map.json`.
- This maps each $(x, y)$ visual field coordinate to its corresponding retinal / GCC anatomical sector (e.g., Superior-Temporal, Superior-Nasal, Inferior-Temporal, Inferior-Nasal, Foveal).

```json
{
  "map_name": "Garway-Heath-Hood-Macular-GCC-v1",
  "pattern": "10-2",
  "sectors": [
    {
      "sector_id": "ST_MACULA",
      "description": "Superior-Temporal Macular Ganglion Cell Complex",
      "points": [
        {"x": 1.0, "y": 1.0},
        {"x": 3.0, "y": 1.0},
        {"x": 1.0, "y": 3.0},
        {"x": 3.0, "y": 3.0},
        {"x": 5.0, "y": 1.0}
      ]
    },
    {
      "sector_id": "IT_MACULA",
      "description": "Inferior-Temporal Macular Ganglion Cell Complex",
      "points": [
        {"x": 1.0, "y": -1.0},
        {"x": 3.0, "y": -1.0},
        {"x": 1.0, "y": -3.0},
        {"x": 3.0, "y": -3.0},
        {"x": 5.0, "y": -1.0}
      ]
    }
  ]
}
```

---

## 4. Scotoma Management and Censored Data Handling

### 4.1 Floor Truncation in Severe Defects
When a patient has deep sensitivity loss ($\text{dB}_{\text{HFA}} < 8\text{ dB}$), OpenPerimeter's display cannot produce a stimulus brighter than $L_{\text{max, device}}$ ($\sim 500\text{ cd/m}^2$).
- **Data Integrity Rule:** The system outputs `is_censored = 1 (FLOOR_CENSORED)`.
- In statistical analysis, treating censored points as $0\text{ dB}$ introduces severe artificial positive bias in scotoma depth and attenuates structural-functional correlation coefficients.
- **Mandatory Analytical Strategy:**
  1. **Tobit Censored Regression:** For modeling relationship between OCT thickness and visual field sensitivity.
  2. **Non-parametric Rank Correlation (Spearman's $\rho$):** Treats all censored points as tied lowest rank without distorting rank order.
  3. **Censored Bland-Altman Analysis:** Excluding censored points for paired difference calculation or employing survival-analysis methods for interval-censored differences.

### 4.2 Goldmann Size V Extended Range Protocol
In the optional Size V extension:
- If a point fails to respond at $0\text{ dB}_{\text{device}}$ with Size III, the researcher may activate the secondary Size V protocol.
- Size V activates spatial summation (Ricco's area), delivering $16\times$ photon flux.
- Size V records must be flagged with `stimulus_size = SIZE_V` and analyzed in a separate, dedicated regression model. They must NEVER be combined directly with Size III values without an empirical conversion formula.

---

## 5. Reliability Gates and Data Quality Assurance

Tests are flagged as **UNRELIABLE** and excluded from primary agreement analysis if any of the following study criteria are breached:
1. **Fixation Losses (FL):** $\ge 20\%$ (Heijl-Krakau presentations into blind spot).
2. **False Positive Rate (FP):** $\ge 15\%$ (Catch trials with shutter interval only).
3. **False Negative Rate (FN):** $\ge 15\%$ (Supra-threshold $+9\text{ dB}$ presentations at known seeing points).
4. **Head-Pose Drift Violations:** $> 5$ uncorrectable head drift events ($> 3^\circ$) requiring manual operator repositioning.
5. **Frame Presentation Drops:** $\ge 1$ dropped frame during active stimulus presentation (logged by `FrameTimingManager`).

---

## 6. Participant Privacy and DPDP Act 2023 Compliance Protocol

The study adheres strictly to the principles of India's **Digital Personal Data Protection (DPDP) Act 2023**:
1. **Notice & Consent:** The participant or legal guardian signs an informed consent form approved by the Institutional Ethics Committee (IEC). OpenPerimeter records the digital timestamp of consent verification before unlocking data entry.
2. **Data Minimization:** No personal identifiers (name, national ID, address, phone, IP address) are entered into or stored by OpenPerimeter.
3. **Pseudonymous Participant IDs:** Assigned by the clinical coordinator in the Master Subject Key Log (kept in a separate, encrypted institutional spreadsheet disconnected from the perimeters).
4. **Right to Erasure (Delete):** In compliance with the DPDP Act, the OpenPerimeter operator interface provides a cryptographically secure "Purge Subject Records" function that completely removes all sessions associated with a given Subject ID from local storage.

---

## 7. Prerequisite Pilot Bench Study Protocol on Healthy Eyes (Phase 5)

Prior to initiating enrollment of the clinical glaucoma cohort, an investigational bench study on **N = 30 healthy adult eyes** must be completed in accordance with [PILOT_BENCH_STUDY_PROTOCOL.md](file:///Volumes/EMCON/Projects/glaucoma/docs/PILOT_BENCH_STUDY_PROTOCOL.md):
- **Test-Retest Repeatability ($CR$):** Verifies that within-subject standard deviation $s_w \le 1.25\text{ dB}$ ($CR \le 3.50\text{ dB}$).
- **Bland-Altman Agreement against HFA3:** Verifies systematic bias $\le \pm 1.50\text{ dB}$ and 95% LoA span $\le 7.00\text{ dB}$.
- **Decision Gates:** All 6 pre-specified Go / No-Go quality gates must be certified before submitting patient study amendments to the Institutional Ethics Committee (IEC).
