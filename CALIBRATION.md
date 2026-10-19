# OpenPerimeter: Optical and Photometric Calibration Specification
**Document Version:** 1.0.0-PROPOSED  
**Classification:** Research Use Only (RUO) - Critical Measurement Protocol  
**Date:** October 2026  

---

> [!CRITICAL]
> **PHOTOMETRIC INTEGRITY REQUIREMENT**  
> Static automated perimetry measures differential light sensitivity ($\Delta L / L_{bg}$) across the retina. In consumer smartphones, display luminance varies widely across hardware models, driver revisions, thermal states, and battery levels. OpenPerimeter requires a rigorous physical calibration using a calibrated handheld photometer behind the VR viewer optics. Clinical-style research data collection is blocked unless a signed calibration file matching the device and viewer is active.

---

## 1. Perimetric Photometry and Decibel (dB) Definitions

### 1.1 Standard Automated Perimetry (HFA) Benchmark
The Humphrey Field Analyzer (HFA) standardizes stimuli based on the Weber contrast model under low-photopic / mesopic adaptation:
- **Background Luminance ($L_{bg}$):** $31.5\text{ apostilbs (asb)} = 10.03\text{ cd/m}^2$ (standardized to $10\text{ cd/m}^2$).
  *Conversion Note:* $1\text{ cd/m}^2 = \pi\text{ apostilbs} \approx 3.14159\text{ asb}$.
- **Maximum Stimulus Luminance ($L_{max}$):** $10,000\text{ asb} = \frac{10,000}{\pi} \approx 3,183.1\text{ cd/m}^2$. This defines $0\text{ dB}$ on the HFA scale.
- **HFA Decibel Scale Formula:**
  $$\text{dB}_{\text{HFA}} = 10 \cdot \log_{10}\left(\frac{L_{\text{max, HFA}}}{\Delta L}\right) = 10 \cdot \log_{10}\left(\frac{3,183.1}{\Delta L}\right)$$
  Where $\Delta L = L_{\text{stimulus}} - L_{bg}$ (luminance increment above background in $\text{cd/m}^2$).

### 1.2 Smartphone in VR Display Realities
Consumer OLED displays in smartphones cannot achieve $3,183.1\text{ cd/m}^2$ sustained in manual SDR mode behind viewer optics:
- **Typical Phone Peak Luminance:** High-end OLEDs achieve $800 - 1200\text{ cd/m}^2$ in small-window HDR or outdoor sunlight boost (High Brightness Mode). In sustained manual SDR rendering via Android APIs, peak luminance is typically $450 - 650\text{ cd/m}^2$.
- **Viewer Lens Transmission Loss:** Cardboard-style biconvex acrylic or glass lenses absorb and reflect light, typically exhibiting $88\% - 92\%$ transmission ($\sim 8 - 12\%$ loss).
- **Net Eyepiece Luminance ($L_{\text{max, device}}$):** Behind the lens, achievable maximum luminance is typically $400 - 550\text{ cd/m}^2$ ($\approx 1,250 - 1,730\text{ asb}$).

### 1.3 Decibel Formulation in OpenPerimeter
To avoid misleading comparisons, OpenPerimeter defines the native device decibel scale relative to its own documented peak behind the lens:
$$\text{dB}_{\text{device}} = 10 \cdot \log_{10}\left(\frac{L_{\text{max, device}}}{\Delta L}\right)$$

#### Critical Difference: Device dB vs. HFA dB
Because $L_{\text{max, device}} < L_{\text{max, HFA}}$, a constant offset exists:
$$\Delta\text{dB}_{\text{offset}} = 10 \cdot \log_{10}\left(\frac{3,183.1}{L_{\text{max, device}}}\right)$$

For example, if $L_{\text{max, device}} = 500\text{ cd/m}^2$:
$$\Delta\text{dB}_{\text{offset}} = 10 \cdot \log_{10}\left(\frac{3,183.1}{500}\right) \approx 8.04\text{ dB}$$

$$\text{dB}_{\text{HFA, equivalent}} = \text{dB}_{\text{device}} - \Delta\text{dB}_{\text{offset}} = \text{dB}_{\text{device}} - 8.04\text{ dB}$$

```
   Luminance (cd/m²)      HFA dB Scale       OpenPerimeter (500 cd/m² peak)
        3,183.1 ------------ 0 dB ---------- [Unachievable on phone with Size III]
        1,000.0 ------------ 5 dB ---------- [Unachievable on phone with Size III]
          500.0 ----------- 8.04 dB -------- 0 dB_device (L_max ceiling)
          158.0 ---------- 13.04 dB -------- 5 dB_device
           50.0 ---------- 18.04 dB -------- 10 dB_device
           15.8 ---------- 23.04 dB -------- 15 dB_device
            5.0 ---------- 28.04 dB -------- 20 dB_device
            1.58 --------- 33.04 dB -------- 25 dB_device
            0.50 --------- 38.04 dB -------- 30 dB_device
            0.158 -------- 43.04 dB -------- 35 dB_device
            0.050 -------- 48.04 dB -------- 40 dB_device (limited by 8-bit quantization)
```

> [!WARNING]
> **NON-INTERCHANGEABILITY STATEMENT**  
> Decibel values from OpenPerimeter are NOT directly interchangeable with Humphrey Field Analyzer (HFA) or Elisar AVA values without applying the documented calibration offset. In scientific publications and research records, all values must be reported with:
> 1. Native $\text{dB}_{\text{device}}$.
> 2. Documented $L_{bg}$ ($10\text{ cd/m}^2$) and $L_{\text{max, device}}$ ($\text{cd/m}^2$).
> 3. Equivalent $\Delta L$ in $\text{cd/m}^2$ or apostilbs.

---

## 2. Dynamic Range and Scotoma Truncation (Ceiling Effects)

### 2.1 The Advanced Glaucoma Problem
In moderate-to-advanced glaucoma, damaged visual field points exhibit profound sensitivity loss ($\text{dB}_{\text{HFA}} < 10\text{ dB}$, down to absolute defects $< 0\text{ dB}$).
- Because the smartphone display caps at $\sim 500\text{ cd/m}^2$ ($\approx 8\text{ dB}_{\text{HFA}}$), **any retinal defect requiring a stimulus $> 500\text{ cd/m}^2$ cannot be detected using standard Goldmann Size III**.
- Standard commercial perimeters might silently clip or mark such points as $0\text{ dB}$, masking true scotoma progression.

### 2.2 OpenPerimeter Mitigation: Censoring and Spatial Modulation
1. **Explicit Censoring Flag:**
   Any point where the stimulus exceeds $L_{\text{max, device}}$ without patient response is marked with `is_censored = 1 (FLOOR_CENSORED)`. It is never silently clamped to an arbitrary number.
2. **Optional Stimulus-Size Modulation (Goldmann Size V):**
   - By Ricco's / Piper's law of spatial summation, stimulating a larger retinal area increases stimulus energy without raising display luminance.
   - Goldmann Size V ($1.72^\circ$ diameter, $16\times$ the area of Size III) provides an effective sensitivity extension of $\sim 1.2\text{ log units}$ ($12\text{ dB}$).
   - When enabled, points that fail Size III at $0\text{ dB}_{\text{device}}$ are re-tested with Size V. The output data explicitly tags `stimulus_size = SIZE_V` to maintain strict partition from fixed-size data.

---

## 3. Hardware Calibration Protocol (Behind-the-Lens Measurement)

```
+-----------------------------------------------------------------------------------+
|                        Physical Calibration Setup                                 |
+-----------------------------------------------------------------------------------+
|                                                                                   |
|    +--------------------+       +---------------+       +--------------------+    |
|    |  Smartphone OLED   | ===== | VR Viewer     | ===== | Calibrated         |    |
|    |  Display Surface   |  gap  | Biconvex Lens |  gap  | Luminance Meter    |    |
|    |  (Grayscale Pass)  |       | (Acrylic/Glass)       | (e.g. LS-100/150)  |    |
|    +--------------------+       +---------------+       +--------------------+    |
|                                                                                   |
|    <- Fixed Window Brightness -> <- Light-tight Shroud -> <- Eyepiece Position -> |
+-----------------------------------------------------------------------------------+
```

### 3.1 Equipment Required
1. **Calibrated Spot Luminance Meter / Photometer:**
   - Measurement spot size: $\le 1^\circ$ (e.g., Konica Minolta LS-100/110/150, SpyderX with custom adapter, or Sekonic C-800).
   - Accuracy: $\pm 2\%$ at $10\text{ cd/m}^2$, calibrated traceable to NIST/NPL standards within 12 months.
2. **Light-Tight Optical Mount:**
   - Machined 3D-printed fixture coupling the photometer objective to the VR viewer eyepiece at the design eye relief distance ($12 - 15\text{ mm}$ behind the lens vertex).
3. **Target Smartphone & VR Viewer:**
   - Smartphone charged to $> 80\%$ battery.
   - Android "Stay Awake" enabled in Developer Options.

### 3.2 Step-by-Step Step-Response LUT Measurement
The calibration utility in OpenPerimeter displays 33 uniform grayscale patches covering the full DAC range ($0, 8, 16, 24, \dots, 248, 255$ for 8-bit):
1. **Dark Adaptation & Zeroing:** Mount the phone and photometer in a darkroom ($< 0.1\text{ lux}$ ambient). Allow phone and photometer to warm up for 15 minutes.
2. **Background DAC Calibration:**
   - Target $L_{bg} = 10.0\text{ cd/m}^2 \pm 0.2\text{ cd/m}^2$.
   - The calibration app iterates DAC values near estimated gray level until measured eyepiece luminance reads $10.0\text{ cd/m}^2$. Store this as `background_dac_value`.
3. **Luminance LUT Generation:**
   - For each DAC step $i \in [0, 255]$:
     - Present full-field uniform patch for 3 seconds.
     - Record measured luminance $L(i)$ in $\text{cd/m}^2$.
   - Calculate native electro-optical transfer function (gamma curve):
     $$L(\text{DAC}) = L_{black} + (L_{max} - L_{black}) \cdot \left(\frac{\text{DAC}}{255}\right)^\gamma$$
4. **Spatial Uniformity Profiling:**
   - Measure $L_{bg}$ and $L_{max}$ at 9 visual field locations: Center ($0^\circ, 0^\circ$), and 8 eccentric positions ($\pm 15^\circ, \pm 15^\circ$).
   - Compute peripheral vignetting/transmission ratio:
     $$U(\theta) = \frac{L(\theta)}{L(0^\circ)}$$
   - Store $U(\theta)$ as a radial falloff polynomial to correct stimulus DAC values in the periphery.

---

## 4. Calibration File Schema and Cryptographic Signing

The calibration file (`calibration.json`) contains the complete optical and photometric profile. To prevent tampering or uncalibrated runs in clinical research, the file is signed with an HMAC-SHA256 signature generated during laboratory calibration.

```json
{
  "schema_version": "1.0.0",
  "calibration_id": "CAL-PX7-CARDBOARD2-20261019-01",
  "created_at_utc": "2026-10-19T08:30:00Z",
  "laboratory": "Glaucoma Research Laboratory, Optometry Div",
  "operator_id": "OP-CAL-04",
  "device_profile": {
    "manufacturer": "Google",
    "model": "Pixel 7 Pro",
    "android_version": "14",
    "display": {
      "panel_technology": "OLED",
      "resolution_px": [1440, 3120],
      "ppi": 512,
      "refresh_rate_hz": 90,
      "color_mode": "Natural"
    }
  },
  "viewer_profile": {
    "manufacturer": "Google",
    "model": "Cardboard v2",
    "lens_type": "Biconvex Acrylic",
    "focal_length_mm": 45.0,
    "ipd_mm": 64.0,
    "eye_relief_mm": 13.5,
    "distortion_coefficients": {
      "k1": 0.34,
      "k2": 0.12
    }
  },
  "photometric_measurements": {
    "photometer_model": "Konica Minolta LS-150",
    "photometer_serial": "8374921",
    "last_npl_calibration_date": "2026-03-15",
    "background_target_cdm2": 10.0,
    "background_measured_cdm2": 10.04,
    "background_dac_value": 31,
    "peak_measured_cdm2": 520.4,
    "measured_dynamic_range_db": 34.2,
    "hfa_offset_db": 7.86,
    "lut_samples_dac": [0, 8, 16, 24, 31, 48, 64, 96, 128, 160, 192, 224, 255],
    "lut_samples_cdm2": [0.002, 0.42, 2.15, 5.82, 10.04, 26.3, 52.8, 118.4, 202.1, 301.5, 408.2, 480.0, 520.4]
  },
  "signature": {
    "algorithm": "HMAC-SHA256",
    "hash": "7a3f89b6c4e0193da5342a1ef24896d8b94098939a3f2955dce451b6a1e94883"
  }
}
```

### 4.1 Application Startup Gatekeeper
When OpenPerimeter launches:
1. It queries Android `Build.MODEL`, `Build.MANUFACTURER`, and display resolution.
2. It looks for a matching `calibration.json` in local storage.
3. If valid: Test sessions operate in **"RESEARCH CALIBRATED MODE"**.
4. If missing, mismatched, or altered:
   - A persistent, bold red watermark is stamped across all viewports:
     `"UNCALIBRATED DEMO MODE - NOT VALID FOR RESEARCH DATA"`
   - Database records flag `is_calibrated = 0`.
   - Export CSV files flag all records with `UNVERIFIED_CALIBRATION`.

---

## 5. Screen In-App Angular Calibration Tool

To verify visual angle projection independently of photometers:
1. OpenPerimeter provides an **Angular Calibration Grid** rendering concentric circles at $5^\circ, 10^\circ, 15^\circ, 20^\circ,$ and $24^\circ$.
2. It renders crosshairs with millimeter-scale ticks calibrated to display PPI.
3. The researcher inspects through the eyepiece against a reticle or physical optical bench target to verify that a $10^\circ$ circle exactly subtends the expected physical visual angle without off-axis distortion.
