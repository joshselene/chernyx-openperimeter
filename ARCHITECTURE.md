# OpenPerimeter: System Architecture Specification
**Document Version:** 1.0.0-PROPOSED  
**Classification:** Research Use Only (RUO) - Not for Clinical Decisions  
**Date:** October 2026  

---

> [!WARNING]
> **RESEARCH USE ONLY - NOT A MEDICAL DEVICE**  
> OpenPerimeter is designed solely for academic and clinical research on structure-function discordance in glaucoma. It has not received regulatory clearance (US FDA, CE-MDR, or CDSCO) and must NEVER be used for diagnostic, prognostic, or therapeutic decision-making. Every UI screen and exported report must prominently feature this disclaimer.

---

## 1. System Overview and Research Context

OpenPerimeter is a smartphone-in-VR-viewer static automated perimeter (SAP) designed to test the **24-2** (54 points, 6° grid) and **10-2** (68 points, 2° grid) visual fields. The clinical research target is **moderate-to-advanced glaucoma** (Hodapp-Parrish-Anderson criteria: Moderate MD $-6.01$ to $-12.00$ dB; Advanced MD $< -12.00$ dB), with specific comparison against:
1. **Humphrey Field Analyzer (HFA)**: Carl Zeiss Meditec, SITA Standard / SITA Faster.
2. **Elisar Advanced Vision Analyser (AVA)**: VR headset-based static perimeter.

### 1.1 Structural-Functional Discordance Focus
In advanced glaucoma, visual field damage frequently approaches the perimeter's measurement ceiling (luminance floor), leading to floor effects. Conversely, optical coherence tomography (OCT) ganglion cell complex (GCC) and retinal nerve fiber layer (RNFL) thickness reach anatomical floor levels early. OpenPerimeter implements precise dynamic range reporting, explicit scotoma censoring, and optional stimulus-size modulation (Goldmann Size V) to resolve deep scotomas without uncalibrated extrapolation.

```
+-----------------------------------------------------------------------------------+
|                                 OpenPerimeter Architecture                        |
+-----------------------------------------------------------------------------------+
|  [Hardware Tier]                                                                  |
|   - Android 10+ Smartphone (OLED, HDR/Direct Rendering capable, High Peak Nit)    |
|   - Cardboard / Fixed-Lens VR Headset (Known IPD, Eye Relief, Lens Focal Length)  |
|   - Bluetooth HID Clicker (Low-latency Gamepad/Keyboard Controller)               |
+-----------------------------------------------------------------------------------+
|  [Unity 6 LTS Engine / Android Native Layer]                                      |
|   +--------------------------+  +--------------------------+  +-----------------+ |
|   | Native Device Control    |  | Rendering Subsystem      |  | Input & Timing  | |
|   | - Brightness Override    |  | - URP Grayscale Pass     |  | - HID Clicker   | |
|   | - Thermal/Battery Guard  |  | - Gamma / Photometer LUT |  | - Nanosecond    | |
|   | - Screen Pinning / DND   |  | - Custom Stereo Viewport |  |   Event Capture | |
|   +--------------------------+  +--------------------------+  +-----------------+ |
|   +-----------------------------------------------------------------------------+ |
|   | Core Perimetry Logic                                                        | |
|   | - Grid Generators: 24-2 (54 pts), 10-2 (68 pts)                             | |
|   | - Threshold Engine: ZEST (Bayesian) + 4-2 dB Staircase Fallback             | |
|   | - Reliability Monitor: Heijl-Krakau Blind Spot, FP/FN Catch Trials          | |
|   | - Head-Pose Drift Rejection (IMU Sensor Fusion)                             | |
|   +-----------------------------------------------------------------------------+ |
|   +-----------------------------------------------------------------------------+ |
|   | Local Encrypted Storage & Privacy (DPDP Act 2023 Compliant)                 | |
|   | - SQLCipher / AES-GCM Encrypted SQLite Storage                              | |
|   | - Pseudonymous Participant Identifiers (Zero PII)                           | |
|   | - JSON/CSV Export for Agreement Analysis (HFA / AVA Format)                 | |
|   +-----------------------------------------------------------------------------+ |
+-----------------------------------------------------------------------------------+
|  [Python 3.12 Analysis & Simulation Package]                                      |
|   - Monte Carlo Synthetic Observer Simulation Harness (Prior to Human Testing)    |
|   - Bland-Altman Limits of Agreement & Tobit Censored Regression                  |
|   - Two-Way Random Absolute Intraclass Correlation Coefficient (ICC)              |
|   - Test-Retest Coefficient of Repeatability (CR)                                 |
+-----------------------------------------------------------------------------------+
```

---

## 2. Technical Stack and Architectural Rationale

### 2.1 Engine and Graphics Pipeline
- **Platform:** Unity 6 LTS (6000.x), targeting Android (API Level 29+ / Android 10+), 64-bit ARM (`arm64-v8a`), scripting backend **IL2CPP**.
- **Render Pipeline:** Universal Render Pipeline (URP).
- **Display Fidelity Invariants:**
  - **Post-processing completely disabled:** No Tonemapping (ACES/Neutral), Bloom, Vignette, Color Grading, or Ambient Occlusion.
  - **HDR disabled:** Screen backbuffer set to deterministic 8-bit integer RGB or native 10-bit RGBA64 if supported, avoiding driver-level tone curve compression.
  - **Anti-Aliasing & Dithering disabled:** Dithering introduces spatial-temporal high-frequency noise that alters micro-luminance; anti-aliasing (MSAA/FXAA) creates sub-pixel interpolation at stimulus edges, altering effective visual angle and luminance integration. Stimuli are rendered with sharp, analytical geometric boundaries.
  - **Color Pipeline:** Pure grayscale (`R = G = B`). Output luminance is governed strictly by a hardware-calibrated Luminance Lookup Table (LUT).

### 2.2 Google Cardboard SDK vs. Custom Distortion Viewport
| Criteria | Google Cardboard XR Plugin | Custom Deterministic Stereo Viewport (Recommended) |
| :--- | :--- | :--- |
| **Unity 6 Compatibility** | Fragile across major LTS upgrades; deprecation risks. | 100% native Unity URP C# + single-pass vertex/fragment shader. |
| **Lens Distortion Control** | Opaque polynomial coefficients loaded via QR codes; black-box warp. | Explicit Brown-Conrady or radial polynomial ($k_1, k_2$) parameterization; exact ray-tracing match to photometer measurement. |
| **Vsync & Timing Predictability**| Dependent on Google XR activity lifecycle and asynchronous reprojection hooks. | Direct control over frame presentation loop, vsync lock, and Choreographer timestamps. |
| **Zero External Dependency** | Requires external package and Android manifest modifications. | Fully self-contained, auditable, and easily modified. |

**Decision:** We implement a **Custom Deterministic Stereo Viewport** as the primary optical pipeline, with optional Google Cardboard profile import for viewer QR compatibility.

### 2.3 Monocular Display Protocol
- Static automated perimetry requires strict monocular testing.
- The tested eye viewport displays the calibrated background ($10\text{ cd/m}^2$, $\approx 31.5\text{ asb}$) and active stimuli.
- The non-tested eye half-screen is rendered in **uniform OLED black** ($0\text{ cd/m}^2$, DAC value 0). 
- *Physical Safety Note:* Because dark adaptation in the non-tested eye can induce consensual pupillary dilation or binocular rivalry, the study protocol permits physical eye patching inside the viewer if binocular rivalry is reported.

---

## 3. Optical and Visual Angle Geometry

### 3.1 Angular-to-Pixel Mapping Formulation
Visual stimuli are specified in visual field angles (degrees, $\theta$). To convert visual angle $\theta$ to physical screen displacement $r_s$ and pixel offset $(x_p, y_p)$:

$$r_s = f_{lens} \cdot \tan\left(\theta \cdot \frac{\pi}{180}\right)$$

Where:
- $f_{lens}$ is the effective focal length of the viewer lens (typically $40\text{ mm} - 45\text{ mm}$).
- $r_s$ is the radial displacement on the display surface (in millimeters).
- Given display pixel density $PPI$ (pixels per inch) or dot pitch $d_p = \frac{25.4}{PPI}\text{ mm/pixel}$:

$$r_p = \frac{r_s}{d_p} = r_s \cdot \frac{PPI}{25.4}$$

### 3.2 Optical Distortion Correction
Real VR viewer lenses exhibit positive radial pincushion distortion. To project a stimulus at undistorted retinal angle $\theta$, the display pre-distorts the coordinates with an inverse barrel warp:

$$r_{distorted} = r_p \cdot (1 + k_1 \cdot \tilde{r}^2 + k_2 \cdot \tilde{r}^4)$$

Where $\tilde{r} = \frac{r_p}{r_{max}}$ is normalized radial distance, and $k_1, k_2$ are calibrated viewer constants.

### 3.3 Stimulus Dimensions (Goldmann Size III - V)
Goldmann stimulus diameters subtend fixed visual angles:
- **Size III (Standard):** $0.431^\circ$ ($25.86\text{ arcmin}$, area $4\text{ mm}^2$ at $30\text{ cm}$ bowl radius).
- **Size IV:** $0.862^\circ$ ($51.72\text{ arcmin}$, area $16\text{ mm}^2$).
- **Size V (Macular/Severe Scotoma):** $1.724^\circ$ ($103.44\text{ arcmin}$, area $64\text{ mm}^2$).

At $f_{lens} = 45\text{ mm}$ and a $450\text{ PPI}$ OLED screen ($d_p \approx 0.0564\text{ mm/px}$):
- Size III physical diameter on screen: $45 \cdot \tan(0.431^\circ) \approx 0.338\text{ mm} \approx 6.0\text{ pixels}$.
- Size V physical diameter on screen: $45 \cdot \tan(1.724^\circ) \approx 1.354\text{ mm} \approx 24.0\text{ pixels}$.

---

## 4. Hardware and Android Device Control

### 4.1 Luminance and Display Locking
To eliminate brightness fluctuation during testing:
1. **Window Brightness Override:** Through native Android `WindowManager.LayoutParams`:
   ```java
   WindowManager.LayoutParams layout = getWindow().getAttributes();
   layout.screenBrightness = calibratedBrightnessSetting; // e.g., 1.0f (max SDR)
   getWindow().setAttributes(layout);
   ```
2. **Screen-On Enforcement:** `FLAG_KEEP_SCREEN_ON` activated permanently during test execution.
3. **OS-Level Interference Mitigation:**
   - Guidance banner prompting the researcher to disable **Adaptive Brightness / Auto-Brightness**, **Night Light / Eye Comfort Shield**, and **True Tone / Ambient Color Adaptation**.
   - Android **Screen Pinning** (App Pinning) and **Do Not Disturb (DND)** activation.

### 4.2 OLED APL and Thermal Management
- **Average Picture Level (APL):** In OLED panels, total power consumption drives Automatic Current Limiting (ACL), which dims the screen when APL is high. In OpenPerimeter:
  - Background is $10\text{ cd/m}^2$ (very low gray, DAC $\approx 25-35$ on typical gamma 2.2).
  - Stimuli are small dots (Size III is $0.005\%$ of screen area).
  - Effective APL is $< 3\%$, well below the threshold for ACL activation.
- **Thermal and Battery Telemetry:**
  - Continuous polling of Android `ThermalStatusListener` (`PowerManager.THERMAL_STATUS_*`) and `BatteryManager.EXTRA_TEMPERATURE`.
  - If thermal throttling (`THERMAL_STATUS_MODERATE` or higher) occurs, the test logs a warning; if severe, it forces a safe pause.

### 4.3 Input Timing and Latency
- HID Bluetooth clicker input captured at the native OS event queue level.
- High-resolution timestamping via `System.nanoTime()` / `Time.realtimeSinceStartupAsDouble`.
- Default presentation duration: **$200\text{ ms}$** (12 frames at 60 Hz, 18 frames at 90 Hz, 24 frames at 120 Hz).
- Configurable response window: Default **$1000\text{ ms}$** starting at stimulus onset.

---

## 5. Threshold Engines: ZEST and 4-2 Staircase

```
+-----------------------------------------------------------------------------------+
|                            Threshold Engine Pipeline                              |
+-----------------------------------------------------------------------------------+
|                                                                                   |
|  [Spatial Point Selection]                                                        |
|   - Pseudo-random non-adjacent point order (prevents local retinal adaptation)    |
|   - Blind-spot and Catch-Trial interleaved schedule                               |
|                                                                                   |
|  [Strategy: ZEST (Bayesian)]                                                      |
|   1. Initialize Prior PDF(T)                                                      |
|      - Normal subject normative mean or Neighbor-Informed Seed                    |
|   2. Select Presentation Level x = Median(PDF) or Mean(PDF)                       |
|   3. Present Stimulus (200 ms, Size III)                                          |
|   4. Patient Response r in {0, 1}                                                 |
|   5. Update Posterior:                                                            |
|      PDF_{new}(T) = PDF_{old}(T) * P(Response = r | Stimulus = x, Threshold = T)  |
|   6. Evaluate Stop Rules:                                                         |
|      - Posterior Standard Deviation <= 1.5 dB                                     |
|      - OR Min/Max Presentation Count reached (Min 2, Max 5)                       |
|                                                                                   |
|  [Strategy: 4-2 dB Staircase (Fallback)]                                          |
|   1. Initial stimulus at expected normal or 25 dB                                 |
|   2. Step by 4 dB until first reversal (crossing seeing/non-seeing)              |
|   3. Step by 2 dB until second reversal                                          |
|   4. Final threshold = luminance of last seen presentation                       |
|                                                                                   |
|  [Dynamic Range & Censoring Handler]                                              |
|   - Stimulus > Device L_max -> Mark Floor-Censored (< 0 dB_device)                |
|   - Stimulus < Device L_min -> Mark Ceiling-Censored (> 35 dB_device)             |
|   - Optional: Trigger Size V presentation to measure deep scotoma                 |
+-----------------------------------------------------------------------------------+
```

### 5.1 ZEST Algorithm Formulation
ZEST (Zippy Estimation by Sequential Testing) maintains a discrete probability density function $p(T)$ over the sensitivity domain $T \in [0, 40]\text{ dB}$ (in $0.5$ or $1.0\text{ dB}$ steps).
The psychometric function (frequency-of-seeing curve) defines the probability of seeing stimulus $x$ given true threshold $T$:

$$P(\text{Seen} \mid x, T) = \gamma + (1 - \gamma - \lambda) \cdot \Phi\left(\frac{x - T}{S}\right)$$

Where:
- $\gamma$ is the false-positive rate (guess rate, typically $0.03$).
- $\lambda$ is the false-negative rate (lapse rate, typically $0.03$).
- $S$ is the psychometric slope (spread parameter, typically $1.0 - 1.5\text{ dB}$).
- $\Phi$ is the cumulative standard normal distribution.

Upon response $r \in \{0 (\text{not seen}), 1 (\text{seen})\}$:

$$p_{k+1}(T) = \frac{p_k(T) \cdot P(r \mid x_k, T)}{\int p_k(\tau) \cdot P(r \mid x_k, \tau) \, d\tau}$$

The next stimulus level $x_{k+1}$ is placed at the mean or median of $p_{k+1}(T)$.

### 5.2 Stopping Criteria
A point terminates when:
1. Standard deviation of the posterior $\sigma_{post} \le 1.5\text{ dB}$, OR
2. Presentation count reaches `max_presentations` (default: 4 for ZEST fast, 5 for standard), subject to `min_presentations >= 2`.

---

## 6. Reliability Metrics and Catch Trials

| Reliability Metric | Implementation Protocol | Pass/Fail Study Threshold |
| :--- | :--- | :--- |
| **Fixation Losses (FL)** | Heijl-Krakau technique: present Goldmann Size III stimulus into the physiological blind spot (nominally $15^\circ$ temporal, $-2^\circ$ to $-3^\circ$ vertical for OD, $-15^\circ$ for OS). Blind spot location confirmed during initial fixation search. | $< 20\%$ |
| **False Positive (FP)** | Catch trials with zero visual stimulus (timing shutter only). Any click registered within the response window is logged as a false positive. | $< 15\%$ |
| **False Negative (FN)** | Catch trials presenting a supra-threshold stimulus ($+9\text{ dB}$ brighter than the currently established threshold) at previously confirmed seeing points. Failure to click is logged as a false negative. | $< 15\%$ |
| **Head Drift** | IMU tracker rejects presentations and pauses test if head rotation exceeds $\pm 3.0^\circ$ from origin. | Continuous guard |

---

## 7. Data Storage, Export, and Privacy (DPDP Act 2023 Compliance)

### 7.1 Data Minimization and Pseudonymization
Under India's **Digital Personal Data Protection (DPDP) Act 2023**:
- No personally identifiable information (PII) is captured. No names, phone numbers, email addresses, national IDs, or hospital patient IDs are stored.
- Pseudonymous Study Subject IDs only: Format `OP-XXXX-EYE` (e.g., `OP-G042-OD`).
- Test records stored in local SQLite database encrypted at rest using AES-256 (via SQLCipher).
- Zero cloud connectivity in v1: 100% air-gapped on device.

### 7.2 Database Schema
```sql
CREATE TABLE IF NOT EXISTS participants (
    participant_id TEXT PRIMARY KEY,
    consent_timestamp INTEGER NOT NULL,
    age_bracket TEXT NOT NULL, -- e.g. "50-59", minimizing exact DOB
    study_arm TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS test_sessions (
    session_id TEXT PRIMARY KEY,
    participant_id TEXT NOT NULL,
    timestamp INTEGER NOT NULL,
    tested_eye TEXT CHECK(tested_eye IN ('OD', 'OS')),
    test_pattern TEXT CHECK(test_pattern IN ('24-2', '10-2')),
    algorithm TEXT CHECK(algorithm IN ('ZEST', 'STAIRCASE_4_2')),
    stimulus_size TEXT NOT NULL, -- 'SIZE_III', 'SIZE_V'
    device_model TEXT NOT NULL,
    viewer_model TEXT NOT NULL,
    calibration_id TEXT NOT NULL,
    test_duration_sec REAL NOT NULL,
    fixation_loss_ratio REAL NOT NULL,
    false_pos_ratio REAL NOT NULL,
    false_neg_ratio REAL NOT NULL,
    mean_defect_db REAL,
    pattern_std_dev_db REAL,
    is_reliable INTEGER NOT NULL,
    FOREIGN KEY(participant_id) REFERENCES participants(participant_id)
);

CREATE TABLE IF NOT EXISTS test_points (
    point_id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL,
    point_index INTEGER NOT NULL,
    x_deg REAL NOT NULL,
    y_deg REAL NOT NULL,
    threshold_db REAL NOT NULL,
    is_censored INTEGER NOT NULL, -- 0=measured, 1=floor (<0), 2=ceiling (>max)
    presentation_count INTEGER NOT NULL,
    gcc_sector TEXT, -- optional mapped macular sector
    FOREIGN KEY(session_id) REFERENCES test_sessions(session_id)
);

CREATE TABLE IF NOT EXISTS presentation_log (
    presentation_id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL,
    point_index INTEGER NOT NULL,
    presentation_order INTEGER NOT NULL,
    stimulus_db REAL NOT NULL,
    stimulus_cdm2 REAL NOT NULL,
    user_responded INTEGER NOT NULL,
    reaction_time_ms REAL,
    head_pose_yaw REAL,
    head_pose_pitch REAL,
    is_catch_trial TEXT, -- NULL, 'BLIND_SPOT', 'FP', 'FN'
    timestamp_ns INTEGER NOT NULL,
    FOREIGN KEY(session_id) REFERENCES test_sessions(session_id)
);
```

### 7.3 Interoperability Export Schema (HFA / AVA Comparison)
Export produces paired CSV and JSON files formatted for agreement analysis:
`session_id, device_type, eye, point_index, x_deg, y_deg, threshold_db, is_censored, reliability_flag, gcc_sector`

---

## 8. Pluggable Normative Database Engine
- Deviation calculations require a healthy normative population.
- OpenPerimeter enforces that unless a validated `norms.json` file is explicitly supplied by the researcher, all Mean Deviation (MD), Pattern Deviation (PD), and probability maps are watermarked with:
  `"UNVALIDATED NORMS - FOR TESTING PIPELINE ONLY"`
- The normative database engine is abstracted via an interface `INormativeDatabase` that reads:
  ```json
  {
    "pattern": "24-2",
    "version": "0.1-synthetic",
    "is_validated": false,
    "age_groups": [
      {
        "min_age": 50,
        "max_age": 59,
        "points": [
          {"x": -9, "y": 3, "expected_db": 30.2, "sd_db": 2.1},
          ...
        ]
      }
    ]
  }
  ```

---

## 9. Verification and Quality Gates
1. **Simulation Harness First (Phase 2):** Monte Carlo simulation of $10,000$ synthetic runs across normal and scotoma fields must establish zero bias ($< 0.2\text{ dB}$) and root mean squared error ($< 1.8\text{ dB}$) before device testing.
2. **Display Timing Verification:** Unity `FrameTimingManager` must report 0 dropped frames during 200 ms stimulus presentation.
3. **Photometric Calibration Lock:** App refuses to initiate test sessions in "Calibrated Mode" without a cryptographically verified `calibration.json`.
