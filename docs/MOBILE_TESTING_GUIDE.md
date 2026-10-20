# OpenPerimeter: Mobile Testing & Cross-Platform (Android vs. iOS) Guide
**Document Version:** 2.0.0  
**Classification:** Research Use Only (RUO) - Developer & Investigator Operations  
**Date:** October 2026  
**Unity Version:** 6 LTS (6000.6.6f1)  
**Main Scene:** `Assets/Scenes/OpenPerimeter.unity`  

---

> [!WARNING]
> **RESEARCH USE ONLY - NOT A MEDICAL DEVICE**  
> OpenPerimeter is an investigational research static automated perimeter for bench validation, software timing verification, and clinical agreement research (e.g., against Humphrey HFA II-i and Elisar AVA). It must **never** be used for clinical diagnosis, patient management, or clinical decision-making. Software functionality does not imply clinical efficacy or regulatory clearance. Display photometric calibration, angular geometry, and stimulus timing must be independently validated before scientific study.

---

## 1. Project Architecture & Scene Setup

The primary Unity project is located at:
```
/Volumes/EMCON/Projects/glaucoma/Unity
```

### Key Components in `Assets/Scenes/OpenPerimeter.unity`:
1. **`Main Camera`**: Configured as an Orthographic camera with solid black background (`0 cd/m²`) looking directly at the stereo viewport.
2. **`StereoViewportQuad`**: Mesh quad running `OpenPerimeter/PerimeterStereoViewport` shader with `StereoViewportFitter`, automatically fitting the screen aspect ratio.
3. **`[OpenPerimeterCore]`**: Orchestration GameObject hosting:
   - [`OpenPerimeterBootstrap`](file:///Volumes/EMCON/Projects/glaucoma/Unity/Assets/OpenPerimeter/Scripts/Core/OpenPerimeterBootstrap.cs): Automatically verifies calibration signatures, initializes textures, and resolves dependencies.
   - [`PerimetrySessionController`](file:///Volumes/EMCON/Projects/glaucoma/Unity/Assets/OpenPerimeter/Scripts/Core/PerimetrySessionController.cs): Manages the test loop (24-2 / 10-2), ZEST / 4-2 Staircase algorithms, catch trials, and cancellations.
   - [`StimulusPresenter`](file:///Volumes/EMCON/Projects/glaucoma/Unity/Assets/OpenPerimeter/Scripts/Display/StimulusPresenter.cs): Renders circular stimuli, monitors vsync duration (200 ms), draws the central amber fixation cross, and measures frame drops.
   - [`FixationTarget`](file:///Volumes/EMCON/Projects/glaucoma/Unity/Assets/OpenPerimeter/Scripts/Core/FixationTarget.cs): Tares head pose via device gyroscope and pauses the test if drift exceeds 3.0°.
   - [`MobileDeviceController`](file:///Volumes/EMCON/Projects/glaucoma/Unity/Assets/OpenPerimeter/Scripts/Device/MobileDeviceController.cs): Locks maximum calibrated brightness, prevents sleep, and polls battery/thermal telemetry on Android and iOS.
   - [`OpenPerimeterUI`](file:///Volumes/EMCON/Projects/glaucoma/Unity/Assets/OpenPerimeter/Scripts/UI/OpenPerimeterUI.cs): Provides the researcher dashboard, eye selection (OD/OS), live progress/reliability stats, and data export summary.

---

## 2. Option 1: Fast Interactive Preview with "Unity Remote 5"

Unity Remote 5 allows rapid UI, gyro, and shader interaction testing without compiling an APK every time.

### How Unity Remote 5 Works:
- The game view is compressed and streamed over USB from the Mac Unity Editor to your phone.
- The phone sends touch events, mouse clicks, and gyroscope/accelerometer sensor streams back to the Editor.
- **Limitation:** Unity Remote runs inside the Editor, so it **does not validate native Android VSync timing, exact OLED DAC output, or IL2CPP execution**. It is strictly for rapid workflow and interaction debugging.

### Step-by-Step Instructions:
1. **Prepare Phone:**
   - On Android: Go to **Settings > About Phone** -> Tap **Build Number** 7 times.
   - Go to **Settings > System > Developer Options**:
     - Turn **USB Debugging** -> **ON**.
     - Turn **Stay Awake** -> **ON**.
   - Install **Unity Remote 5** from the Google Play Store.
2. **Connect to Mac:**
   - Connect your phone via USB-C cable.
   - On the phone prompt *"Allow USB Debugging from this computer?"*, select **"Always allow"** and tap **Allow**.
3. **Configure Unity Editor:**
   - In Unity, go to **Edit > Project Settings > Editor**.
   - Under **Unity Remote**:
     - **Device:** Select `Any Android Device`.
     - **Compression:** Select `JPEG` (higher framerate) or `PNG`.
     - **Resolution:** Select `Normal (fit to screen)`.
4. **Launch Preview:**
   - Launch the **Unity Remote 5** app on your phone.
   - In Unity Editor, open `Assets/Scenes/OpenPerimeter.unity` and press the **Play (▶)** button.
   - The scene will immediately stream to your phone screen. Tapping the screen will register responses in the UI and test loop.

### Troubleshooting Unity Remote 5:
- **Phone shows blue screen with "Connect to computer...":**
  1. Open Terminal on your Mac and verify ADB detection:
     ```bash
     ~/Library/Android/sdk/platform-tools/adb devices
     ```
  2. If the daemon is stuck, restart it:
     ```bash
     adb kill-server && adb start-server
     ```
  3. Close Unity Editor, relaunch Unity Remote on the phone, and re-open Unity Editor.

---

## 3. Option 2: Standalone Android APK Build & Run (Recommended for Testing & Trials)

This compiles a standalone IL2CPP ARM64 binary and deploys it directly to the smartphone for true independent VR perimetry.

### Verified Project Settings:
- **Package Name:** `org.openperimeter.research`
- **Minimum API Level:** Android 10.0 (API Level 29)
- **Target Architecture:** ARM64 (`arm64-v8a`)
- **Scripting Backend:** IL2CPP
- **Color Space:** Linear (Mandatory for photometric LUT consistency)
- **Orientation:** Landscape Left (Fixed for Cardboard VR)

### Step-by-Step Build & Deploy:
1. Ensure the **Android Build Support** module (including Android SDK & NDK tools and OpenJDK) is installed in Unity Hub.
2. Plug your Android phone into the Mac via USB.
3. In Unity Editor, open `Assets/Scenes/OpenPerimeter.unity`.
4. Go to **File > Build Settings**:
   - Ensure `Assets/Scenes/OpenPerimeter.unity` is checked in **Scenes In Build**.
   - Platform: **Android**.
   - In **Run Device**, select your connected phone.
5. Click **Build and Run**:
   - Choose a filename (e.g. `openperimeter.apk`).
   - Unity compiles the APK, pushes it via `adb install`, and automatically launches it in full-screen VR mode on your phone.

---

## 4. Hardware Accessories Setup

### 4.1 Bluetooth HID Clicker
1. Turn on your Bluetooth clicker (8BitDo Zero 2 in Keyboard mode, VR selfie clicker, or gamepad).
2. Go to phone **Settings > Bluetooth** and pair the clicker.
3. OpenPerimeter natively accepts:
   - Keyboard `Space` or `Return`
   - Gamepad Button 0 (`JoystickButton0`)
   - Screen Touches (`Input.touchCount > 0`)
   - Sub-millisecond reaction time is recorded for every event.

### 4.2 VR Viewer Alignment & Monocular Shroud Test
1. Insert the phone into the Google Cardboard v2 or fixed 45 mm focal length viewer.
2. Align the vertical divider line on screen with the mechanical septum of the viewer.
3. **Monocular Shroud Validation:**
   - When **OD (Right Eye)** is selected: look through the left eyepiece. It **must be pitch black ($0\text{ cd/m}^2$)**. Look through the right eyepiece: the background is uniform low-photopic grey ($10\text{ cd/m}^2$) with the central amber fixation cross.
   - When **OS (Left Eye)** is selected: the right eyepiece must be pitch black ($0\text{ cd/m}^2$).

---

## 5. Laboratory Photometer Calibration Protocol

Before clinical or research data collection:

1. **Mount Spot Photometer:**
   - Mount a spot photometer (Konica Minolta LS-100 or equivalent certified photometer) behind the viewer eyepiece.
2. **Execute Darkroom Verification:**
   - Ambient illuminance must be $< 0.1\text{ lux}$.
3. **Determine $10.0\text{ cd/m}^2$ Background DAC:**
   - Measure screen luminance at varying grey DAC steps (0–255).
   - Find the exact DAC value that generates $10.0\text{ cd/m}^2$ behind the lens.
4. **Deploy Signed `calibration.json`:**
   - Generate the profile JSON and sign it with HMAC-SHA256 using the laboratory key.
   - Push to the device via ADB:
     ```bash
     adb push calibration.json /sdcard/Android/data/org.openperimeter.research/files/calibration.json
     ```
   - On next launch, OpenPerimeter will switch from **`UNCALIBRATED DEMO / DEV MODE`** to **`RESEARCH CALIBRATED`**.

---

## 6. Data Storage & Exports

Session data is stored securely in accordance with participant privacy principles (DPDP Act 2023):
- **Encrypted Database:** `/sdcard/Android/data/org.openperimeter.research/files/openperimeter_local.enc` (AES-256)
- **Export Directory:** `/sdcard/Android/data/org.openperimeter.research/files/exports/`
  - `<session_id>_export.csv`: One row per test point formatted for Bland-Altman and ICC agreement analysis with Humphrey HFA and Elisar AVA.
  - `<session_id>_export.json`: Full session archive with presentation logs, reliability metrics, and normative deviations.
- **Pull Data to Mac:**
  ```bash
  adb pull /sdcard/Android/data/org.openperimeter.research/files/exports/ ./pilot_results/
  ```
