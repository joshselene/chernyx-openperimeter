# OpenPerimeter: Phase 1 Quality Assurance & Bench Verification Checklist
**Document Version:** 1.0.0  
**Phase:** Phase 1 - Optics, Geometry, Timing, and Luminance Subsystems  
**Date:** October 2026  

---

## 1. Optical and Visual Angle Verification

| Item # | Verification Step | Acceptance Criteria | Pass/Fail | Operator Initials |
| :---: | :--- | :--- | :---: | :---: |
| **OPT-01** | Display the **Angular Calibration Screen** through the VR viewer. Measure the physical angular diameter of the $10^\circ$ circle using an optical bench reticle or theodolite. | Reticle measured angular radius $= 10.0^\circ \pm 0.3^\circ$. | [ ] | |
| **OPT-02** | Verify radial symmetry of distortion correction ($k_1, k_2$) across four diagonal quadrants. | Concentric rings remain circular within $\pm 2\%$ aspect ratio across field. | [ ] | |
| **OPT-03** | Measure Goldmann Size III dot diameter on display surface using a travelling microscope. | Physical diameter $= 0.338\text{ mm} \pm 0.02\text{ mm}$ for $45\text{ mm}$ focal length. | [ ] | |
| **OPT-04** | Measure Goldmann Size V dot diameter on display surface. | Physical diameter $= 1.354\text{ mm} \pm 0.05\text{ mm}$ ($4\times$ Size III diameter). | [ ] | |

---

## 2. Monocular Shroud & Photometric Verification

| Item # | Verification Step | Acceptance Criteria | Pass/Fail | Operator Initials |
| :---: | :--- | :--- | :---: | :---: |
| **PHO-01** | Mount spot photometer behind the non-tested eye eyepiece when Right Eye (OD) is selected. Set phone to maximum brightness. | Measured luminance on non-tested half $= 0.00\text{ cd/m}^2$ ($< 0.01\text{ cd/m}^2$, true OLED black). | [ ] | |
| **PHO-02** | Mount spot photometer behind the tested eye eyepiece with uniform background displayed. | Measured background luminance $= 10.0\text{ cd/m}^2 \pm 0.2\text{ cd/m}^2$. | [ ] | |
| **PHO-03** | Present a $0\text{ dB}_{\text{device}}$ stimulus patch (maximum luminance). | Eyepiece luminance matches documented $L_{max}$ ($\sim 500\text{ cd/m}^2$) within $\pm 3\%$. | [ ] | |
| **PHO-04** | Verify LUT monotonicity across all 13 calibration steps ($0, 8, 16, \dots, 255$). | Measured luminance increases strictly monotonically: $L(DAC_{i+1}) > L(DAC_i)$. | [ ] | |
| **PHO-05** | Export `calibration.json` and verify cryptographic HMAC-SHA256 signature in app. | Signature verifies valid; UI displays "RESEARCH CALIBRATED MODE". | [ ] | |
| **PHO-06** | Intentionally tamper with one character in `calibration.json`. Restart app. | App detects signature failure and triggers "UNCALIBRATED DEMO MODE" red watermark. | [ ] | |

---

## 3. Timing and Vsync Determinism Verification

| Item # | Verification Step | Acceptance Criteria | Pass/Fail | Operator Initials |
| :---: | :--- | :--- | :---: | :---: |
| **TIM-01** | Present 100 consecutive single-point stimuli. Record stimulus duration using `Stopwatch` / `FrameTimingManager`. | Stimulus duration $= 200.0\text{ ms} \pm 16.6\text{ ms}$ (within 1 frame at 60 Hz). | [ ] | |
| **TIM-02** | Monitor dropped frames log during 100 consecutive presentations. | Zero dropped frames logged during active $200\text{ ms}$ stimulus interval. | [ ] | |
| **TIM-03** | Trigger simulated touch / HID click during presentation. Verify reaction time logging. | Reaction time logged with millisecond timestamp relative to presentation onset. | [ ] | |

---

## 4. Head Pose and Fixation Monitoring Verification

| Item # | Verification Step | Acceptance Criteria | Pass/Fail | Operator Initials |
| :---: | :--- | :--- | :---: | :---: |
| **FIX-01** | Set baseline pose with phone horizontal in viewer. Rotate phone $4.0^\circ$ around yaw axis. | System triggers `OnHeadDriftExceeded`, halts stimulus presentation, and alerts user. | [ ] | |
| **FIX-02** | Return phone to within $1.0^\circ$ of baseline. | System triggers `OnHeadPoseRestored` and unlocks stimulus presentation. | [ ] | |
| **FIX-03** | Switch fixation mark between Central Cross, Central Dot, and Diamond Four-Point. | Fixation target renders correctly at $(0^\circ, 0^\circ)$ without off-axis jitter. | [ ] | |
