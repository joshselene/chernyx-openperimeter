# OpenPerimeter: Mobile Testing & Cross-Platform (Android vs. iOS) Guide
**Document Version:** 1.0.0  
**Classification:** Research Use Only (RUO) - Developer & Investigator Operations  
**Date:** October 2026  

---

> [!WARNING]
> **RESEARCH USE ONLY - NOT A MEDICAL DEVICE**  
> OpenPerimeter is an investigational research static automated perimeter. It must never be used for diagnostic or clinical decision-making.

---

## 1. How to Test Connected to Your Mobile Phone (Android)

OpenPerimeter is built on **Unity 6 LTS (URP)** with an Android 64-bit (`arm64-v8a`) IL2CPP target. You can test it on a connected Android phone using three methods:

### Method A: Direct Build & Run to Phone (Recommended)
This installs the standalone application directly to your Android smartphone for testing inside the VR viewer.

#### Step 1: Enable Phone Developer Options
1. On your Android phone, go to **Settings > About Phone**.
2. Tap **Build Number** 7 times until you see *"You are now a developer!"*.
3. Go back to **Settings > System > Developer Options**:
   - Turn **USB Debugging** -> **ON**.
   - Turn **Stay Awake** (Screen will never sleep while charging) -> **ON**.
   - (Optional) Turn **Disable Absolute Volume** -> ON (if using Bluetooth clicker audio cues).

#### Step 2: Open Project in Unity 6 LTS
1. Launch **Unity Hub**.
2. Click **Add > Add project from disk** and select:  
   `/Volumes/EMCON/Projects/glaucoma/Unity`
3. Open the project with **Unity 6 LTS (6000.0.x)**.  
   *(The `Packages/manifest.json` will automatically import the Universal Render Pipeline and Android dependencies).*

#### Step 3: Switch Platform & Configure Build Settings
1. In the Unity Editor menu, go to **File > Build Profiles** (or **Build Settings**).
2. Select **Android** in the platform list and click **Switch Platform**.
3. Under **Player Settings > Player > Other Settings**:
   - **Color Space:** Set to `Linear` (critical for photometer LUT consistency).
   - **Auto Graphics API:** Uncheck; ensure `Vulkan` or `OpenGLES3` is first.
   - **Scripting Backend:** Set to `IL2CPP`.
   - **Target Architectures:** Check `ARM64` (`arm64-v8a`).
   - **Minimum API Level:** Set to `Android 10.0 (API Level 29)` or higher.
4. Plug your phone into your Mac via USB. When prompted on the phone, tap **"Always allow from this computer"**.
5. Select your phone in the **Run Device** dropdown in Unity.
6. Click **Build and Run**. Unity will compile the APK, push it to the phone over USB, and launch it automatically.

---

### Method B: Install APK via Command Line (`adb`)
If you build an APK (`openperimeter.apk`) without deploying immediately:
1. Ensure `adb` (Android Debug Bridge) is in your terminal PATH (included in Android Studio: `~/Library/Android/sdk/platform-tools/adb`).
2. Verify device connection:
   ```bash
   adb devices
   ```
3. Install and grant permissions:
   ```bash
   adb install -r openperimeter.apk
   ```
4. View live debug logs and timing drops:
   ```bash
   adb logcat -s OpenPerimeter Unity
   ```

---

### Method C: Fast Iteration with "Unity Remote 5"
For rapid UI/shader preview without building an APK every time:
1. Install **Unity Remote 5** from the Google Play Store on your Android phone.
2. In Unity Editor on your Mac, go to **Edit > Project Settings > Editor**.
3. Under **Unity Remote > Device**, select `Any Android Device`.
4. Launch the Unity Remote 5 app on your phone, then press **Play** in the Unity Editor.  
   *(The video stream and touch/gyro inputs will stream live over USB).*

---

## 2. Setting Up the Hardware Accessories

### 2.1 Bluetooth HID Clicker
1. Power on your Bluetooth clicker (generic VR gamepad, Bluetooth selfie shutter, or 8BitDo Zero 2).
2. On your phone, go to **Settings > Bluetooth** and pair the clicker.
3. In OpenPerimeter:
   - Any standard HID key (`Spacebar`, `Gamepad Button 0 / 'A'`, or `Enter`) registers immediately with sub-millisecond reaction time logging.

### 2.2 VR Headset Alignment
1. Insert the phone into the Google Cardboard v2 or fixed 45 mm focal length viewer.
2. Verify that the white vertical center partition line on screen aligns perfectly with the mechanical nose-bridge divider of the viewer.
3. Monocular shroud test:
   - When Right Eye (OD) is selected, look through the left eyepiece: it must be pitch black ($0\text{ cd/m}^2$).
   - Look through the right eyepiece: the background must appear uniform low-photopic grey ($10\text{ cd/m}^2$) with the central amber fixation cross.

---

## 3. Does This Work on iOS (iPhone)?

### The Short Answer
**Yes, the Unity C# codebase can compile and run on an iPhone via Xcode. HOWEVER, for clinical-grade static perimetry, iOS imposes severe platform restrictions that require manual investigator overrides.**

Below is an honest, rigorous engineering comparison:

| Technical Requirement | Android (Target Platform) | iOS / iPhone | Impact on Perimetry |
| :--- | :--- | :--- | :--- |
| **Programmatic Brightness Override** | **Supported:** `WindowManager.LayoutParams.screenBrightness = 1.0f` overrides brightness directly in code. | **Restricted:** Apple blocks apps from programmatically forcing display brightness without user consent. | On iOS, if brightness isn't at 100%, the photometer LUT is invalid. |
| **Ambient Auto-Dimming & Color Shifting** | Guidance prompts DND; native window lock ignores ambient light sensor. | **Aggressive:** Apple's **True Tone** and **Night Shift** alter color temperature; **Auto-Brightness** alters nits dynamically. | If ambient room light shifts, iOS alters stimulus luminance mid-test unless manually disabled in Accessibility. |
| **True Black ($0\text{ cd/m}^2$) Monocular Shroud** | **Universal on OLED Androids:** OLED pixels at RGB(0,0,0) emit zero light. | **OLED iPhones Only:** (iPhone X, XS, 11 Pro, 12, 13, 14, 15, 16 Pro/standard). | **LCD iPhones (iPhone 8, XR, 11, SE) CANNOT be used.** LCD backlights bleed $\sim 0.5 - 1.2\text{ cd/m}^2$, corrupting dark adaptation in the non-tested eye. |
| **Thermal / Battery Telemetry** | Native `PowerManager.OnThermalStatusChangedListener` and `BatteryManager` JNI. | Restricted sandboxed APIs (`NSProcessInfo.thermalState`). | Harder to detect silent thermal throttling on iPhone. |
| **Physical Headset Fit** | Many Android phones have flush backs or centered camera modules. | Modern iPhones (13 Pro - 16 Pro) have **massive protruding camera bumps** and the **Dynamic Island / Notch**. | The iPhone sits tilted inside Cardboard viewers unless a custom 3D-printed spacer gasket is used. |

---

## 4. How to Run on iOS if Required

If your laboratory protocol requires testing on an iPhone:

### Step 1: Hardware Restrictions
1. **Must use an OLED iPhone** (iPhone 12, 13, 14, 15, or 16). Do NOT use iPhone SE, 8, or XR (LCD backlight bleed will invalidate monocular testing).
2. Measure physical clearance: use a VR viewer with a soft silicone cradle that accommodates the camera bump without tilting the display.

### Step 2: Mandatory Manual iOS Settings (Pre-Flight Checklist)
Before launching OpenPerimeter on iOS, the investigator MUST disable all automatic display adjustments:
1. **Disable Auto-Brightness:**  
   Go to `Settings > Accessibility > Display & Text Size > Auto-Brightness` -> **OFF**.
2. **Disable True Tone:**  
   Go to `Settings > Display & Brightness > True Tone` -> **OFF**.
3. **Disable Night Shift:**  
   Go to `Settings > Display & Brightness > Night Shift` -> **OFF**.
4. **Set Display Brightness to 100%:**  
   Drag the brightness slider in Control Center to maximum.
5. **Enable Guided Access (Screen Pinning):**  
   Go to `Settings > Accessibility > Guided Access` -> **ON**. Triple-click the side button in-app to lock notifications and gesture bars.

### Step 3: Building via Xcode
1. In Unity Editor, go to **File > Build Settings**.
2. Select **iOS** and click **Switch Platform**.
3. Click **Build** to generate the Xcode project folder (`OpenPerimeter_iOS`).
4. Open `Unity-iPhone.xcodeproj` in **Xcode**.
5. Select your connected iPhone, choose your Apple Developer Team in **Signing & Capabilities**, and press **Run (Cmd + R)**.

### Step 4: iOS Behind-the-Lens Photometer Calibration
Because Apple applies different factory gamma curves (Display P3) than Android:
- You must mount the spot photometer behind the viewer lens with the iPhone running the app.
- Measure the background DAC level that produces $10.0\text{ cd/m}^2$.
- Record the peak luminance and export an iPhone-specific `calibration.json`.
- Do NOT use an Android calibration profile on an iPhone.
