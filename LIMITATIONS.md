# OpenPerimeter: Engineering and Clinical Limitations
**Document Version:** 1.0.0-PROPOSED  
**Classification:** Research Use Only (RUO) - Critical Transparency Document  
**Date:** October 2026  

---

> [!CAUTION]
> **COMPREHENSIVE STATEMENT OF SYSTEM LIMITATIONS**  
> OpenPerimeter is an open-source investigational tool for scientific research into structure-function discordance. It is **NOT equivalent to a certified ophthalmic medical perimeter** (such as the Humphrey Field Analyzer HFA3, Octopus 900, or certified Elisar AVA units). Researchers and clinical investigators must thoroughly understand and document the following physical, optical, algorithmic, and operational limitations in all ethics submissions and publications.

---

## 1. Physical and Photometric Limitations

### 1.1 Stimulus Luminance Ceiling (Peak Brightness Limitation)
- **Clinical Benchmark (HFA):** The Humphrey Field Analyzer utilizes a high-intensity xenon projector or specialized LED array capable of $10,000\text{ apostilbs} \approx 3,183.1\text{ cd/m}^2$ ($0\text{ dB}_{\text{HFA}}$).
- **Smartphone OLED Reality:** In sustained manual rendering mode via Android APIs, modern OLED displays typically achieve $450 - 650\text{ cd/m}^2$. After accounting for $\sim 10\%$ absorption and reflection losses through VR viewer lenses, the maximum achievable stimulus luminance at the cornea is $\sim 400 - 550\text{ cd/m}^2$ ($\approx 1,250 - 1,730\text{ asb}$).
- **Impact on Glaucoma Staging:**
  - OpenPerimeter's brightest stimulus corresponds to approximately $8\text{ dB}_{\text{HFA}}$ (an offset of $\sim 8\text{ dB}$).
  - **Severe visual field defects with sensitivities between $0\text{ dB}$ and $7\text{ dB}$ on HFA cannot be differentiated with Goldmann Size III.** In this low-sensitivity range, stimuli will appear invisible to the patient and be marked as `FLOOR_CENSORED`.
  - *Workaround:* Goldmann Size V spatial summation must be utilized to measure deep scotomas, but Size V thresholds are on a distinct scale and cannot be merged without explicit modeling.

### 1.2 Display Quantization (Bit-Depth at Low Contrasts)
- Standard Android display pipelines operate in 8-bit per channel RGB ($256$ discrete levels).
- Around the $10\text{ cd/m}^2$ background (DAC $\approx 31$), a single DAC step ($31 \rightarrow 32$) represents a luminance increment of $\approx 0.4 - 0.6\text{ cd/m}^2$.
- In perimetric decibels relative to $500\text{ cd/m}^2$, $\Delta L = 0.5\text{ cd/m}^2$ corresponds to $\sim 30\text{ dB}_{\text{device}}$ ($\approx 38\text{ dB}_{\text{HFA}}$).
- **Thresholds above $32\text{ dB}_{\text{device}}$ cannot be resolved in single 1-dB increments with 8-bit DACs** without spatial dithering (which we deliberately disable to prevent micro-luminance modulation and temporal flicker).

### 1.3 OLED Average Picture Level (APL) and Thermal Throttling
- Smartphone OLED displays incorporate Automatic Current Limiting (ACL) and thermal throttling at the display controller / Android OS level.
- Although OpenPerimeter maintains a low APL ($< 3\%$), prolonged operation in warm ambient clinic environments ($> 30^\circ\text{C}$) or low battery states ($< 20\%$) can induce automatic hardware luminance throttling without OS API notification.

---

## 2. Optical and Geometric Limitations

### 2.1 Optical Aberrations of Cardboard VR Lenses
- Standard Cardboard-style VR viewers employ single-element biconvex or aspheric acrylic lenses ($40 - 45\text{ mm}$ focal length).
- **Aberrations Present:**
  - **Spherical Aberration:** Peripheral visual field points ($> 20^\circ$ eccentricity) experience peripheral blur, increasing the effective retinal blur circle.
  - **Field Curvature:** The planar smartphone screen does not match the curved Petzval focal surface of simple lenses, leading to off-axis defocus.
  - **Transverse Chromatic Aberration:** Although OpenPerimeter renders in grayscale ($R=G=B$), white light is dispersed by acrylic lenses into color fringing at peripheral eccentricities, slightly reducing contrast sensitivity at peripheral points.
- Unlike bowl perimeters (HFA / Octopus) which project onto an equidistant hemispherical cupola ($30\text{ cm}$ radius), flat-panel VR perimeters require optical compromises.

### 2.2 Lack of Corneal / Pupil Gaze Tracking
- Clinical perimeters like HFA3 utilize an infrared camera and real-time image processing (Gazetracker) to monitor:
  1. Fixation gaze deviations (in degrees).
  2. Eye blinks during presentation.
  3. Pupil diameter (in mm).
- **OpenPerimeter has NO internal eye tracking camera.**
- Fixation is monitored solely via:
  1. **Heijl-Krakau Blind-Spot Catch Trials** (intermittent presentations at the optic nerve head).
  2. **Phone IMU Gyroscope / Accelerometer** for head-pose drift detection.
- *Limitation:* If a patient rotates their eye within the viewer while keeping their head stationary, OpenPerimeter cannot detect the saccade in real time, relying strictly on blind-spot presentation responses.

### 2.3 Mechanical Alignment and IPD Variations
- Cardboard-style viewers feature fixed or coarsely adjustable Interpupillary Distance (IPD) and eye relief.
- Facial anatomy variations (nasal bridge height, cheekbone prominence) can introduce tilt or decentration between the eye's visual axis and the lens optical axis.
- Absence of a chin-rest and forehead-rest means head-strap tension and operator fitting are critical variables.

---

## 3. Algorithmic and Normative Limitations

### 3.1 Non-Replication of SITA (Proprietary Algorithm)
- The **Swedish Interactive Thresholding Algorithm (SITA)** (SITA Standard, SITA Fast, SITA Faster) is the proprietary intellectual property and trade secret of Carl Zeiss Meditec.
- **OpenPerimeter does NOT and CANNOT replicate SITA.**
- OpenPerimeter implements **ZEST (Zippy Estimation by Sequential Testing)**, a well-documented, open Bayesian sequential testing framework with neighbor-informed priors.
- While ZEST correlates strongly with SITA, differences in prior distributions, psychometric slope assumptions, and termination criteria will produce systematic differences in test time and threshold estimates.

### 3.2 Normative Database and Deviation Indices
- Clinical perimeters rely on extensive, multi-center, age-matched normative databases ($> 500$ healthy eyes across decades 20 to 80) to calculate:
  - **Total Deviation (TD):** Difference from age-matched normal median.
  - **Pattern Deviation (PD):** TD corrected for generalized depression (e.g. cataract, miosis).
  - **Mean Deviation (MD) and Pattern Standard Deviation (PSD).**
- **OpenPerimeter ships with NO validated normative database.**
- All TD, PD, MD, and PSD calculations generated by OpenPerimeter are labeled:
  `"UNVALIDATED NORMS - FOR TESTING PIPELINE ONLY"`
- Researchers must establish their own local healthy control cohort to construct valid age-stratified normative values.

---

## 4. Operational and Study Limitations

### 4.1 Monocular Occlusion Without Physical Patch
- OpenPerimeter renders the non-tested eye's screen area in pure OLED black ($0\text{ cd/m}^2$).
- However, dark-adapting the fellow eye in the dark half-screen while the tested eye views a $10\text{ cd/m}^2$ background can induce:
  - Consensual pupillary fluctuations.
  - Binocular rivalry or Troxler fading in sensitive subjects.
- *Recommendation:* When testing patients prone to binocular rivalry, a physical eye patch should be placed over the fellow eye inside the VR headset.

### 4.2 Research Use Only Regulatory Boundary
- OpenPerimeter is **NOT approved by the Central Drugs Standard Control Organisation (CDSCO, India), the US Food and Drug Administration (FDA), or the European Medicines Agency (CE-MDR)**.
- It must not be used to diagnose glaucoma, alter medication regimens, schedule surgeries, or make any clinical management decisions.
