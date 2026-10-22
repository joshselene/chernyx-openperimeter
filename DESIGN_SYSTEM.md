# OpenPerimeter Design System (Phase 1)

**Status:** Phase 1 complete — tokens, type, icons, reusable UITK components, gallery  
**Engine:** Unity 6 · UI Toolkit · Android + iOS capable  
**Classification:** Research Use Only (RUO) — not a medical device  

> UI layer only. Stimulus rendering, timing, calibration, threshold engines, reliability logic, and storage encryption are **out of scope** for this design system.

---

## 1. Principles

1. **Calm clinical dark-first** — default theme is dark; light is optional.
2. **One accent** — teal for primary actions and selected states.
3. **Amber = warnings** — never use amber as a brand accent.
4. **Red = destructive / critical only** — abort, irreversible confirmations, critical failures.
5. **No emoji** — stroke icons only (`OpIconElement` / SVG sources).
6. **Touch ≥ 48dp** — interactive controls use `--op-touch-min: 48px`.
7. **8px spacing grid** — with a 4px half-step when needed.
8. **Contrast** — body text ≥ 4.5:1; large text / graphics ≥ 3:1 in both themes.

---

## 2. Location map

| Asset | Path |
| :--- | :--- |
| Tokens | `Unity/Assets/OpenPerimeter/UI/Tokens/` |
| Themes | `Unity/Assets/OpenPerimeter/UI/Themes/` |
| Components (USS) | `Unity/Assets/OpenPerimeter/UI/Components/OpComponents.uss` |
| Components (C#) | `Unity/Assets/OpenPerimeter/UI/Scripts/` |
| Fonts (OFL) | `Unity/Assets/OpenPerimeter/UI/Fonts/` (+ runtime Resources copy) |
| Icons (SVG) | `Unity/Assets/OpenPerimeter/UI/Icons/svg/` |
| Gallery scene | `Unity/Assets/OpenPerimeter/UI/Gallery/ComponentGallery.unity` |
| Editor menu | `OpenPerimeter → Design System → Open Component Gallery` |

---

## 3. Color tokens

### 3.1 Dark (default)

| Token | Hex | Role |
| :--- | :--- | :--- |
| `--op-color-canvas` | `#0F1419` | App background |
| `--op-color-surface` | `#1A222C` | Cards / rows |
| `--op-color-elevated` | `#243040` | Raised surfaces |
| `--op-color-text-primary` | `#E8EEF4` | Body / titles |
| `--op-color-text-secondary` | `#A8B4C0` | Supporting |
| `--op-color-text-muted` | `#7A8794` | Captions |
| `--op-color-accent` | `#2A9D8F` | Primary / selected |
| `--op-color-warning` | `#E9A825` | Warnings |
| `--op-color-danger` | `#D64545` | Destructive |
| `--op-color-success` | `#3D9B6E` | Success status |
| `--op-color-border` | `#2E3A48` | Dividers |

### 3.2 Light (optional)

| Token | Hex | Role |
| :--- | :--- | :--- |
| `--op-color-canvas` | `#F4F7FA` | App background |
| `--op-color-surface` | `#FFFFFF` | Cards / rows |
| `--op-color-text-primary` | `#1A222C` | Body / titles |
| `--op-color-text-secondary` | `#4A5664` | Supporting |
| `--op-color-accent` | `#1F7A70` | Primary / selected |
| `--op-color-warning` | `#B7791F` | Warnings |
| `--op-color-danger` | `#C53030` | Destructive |
| `--op-color-border` | `#D6DEE6` | Dividers |

### 3.3 Contrast notes (WCAG-oriented)

Approximate contrast ratios vs canvas / surface:

| Pair | Dark | Light | Target |
| :--- | ---: | ---: | :--- |
| Primary text on canvas | **~14.8:1** | **~14.8:1** | ≥ 4.5:1 |
| Secondary text on canvas | **~8.5:1** | **~6.2:1** | ≥ 4.5:1 |
| Muted text on canvas | **~5.1:1** | **~4.6:1** | ≥ 4.5:1 |
| Accent on canvas (large / graphics) | **~6.4:1** | **~5.3:1** | ≥ 3:1 |
| On-accent text on accent fill | **~8+:1** | **~5+:1** | ≥ 4.5:1 |
| Warning on canvas (graphics) | **~9:1** | **~4.8:1** | ≥ 3:1 |
| Danger on canvas (graphics) | **~5.5:1** | **~5.2:1** | ≥ 3:1 |

Muted text is kept at or above ~4.5:1 on canvas. Prefer primary/secondary for critical instructional copy. Re-verify with a colorimeter/contrast tool if tokens change.

---

## 4. Spacing, radius, elevation, touch

| Category | Values |
| :--- | :--- |
| Spacing | 0, **4**, **8**, 16, 24, 32, 40, 48, 64 (`--op-space-*`) |
| Radii | 4 / 8 / 12 / 16 (`sm` / `md` / `lg` / `xl`) |
| Elevation | Border-based (UITK): surface vs elevated + 1px border |
| Touch min | **48×48** CSS px (`--op-touch-min`) for buttons, toggles, steppers, list rows, dialogs |

---

## 5. Typography

| Role | Family | Notes |
| :--- | :--- | :--- |
| UI text | **IBM Plex Sans** | Regular / Medium / SemiBold / Bold / Text |
| Numbers, dB, IDs | **IBM Plex Mono** | Tabular alignment for thresholds & reliability % |

**Licensing:** SIL Open Font License 1.1 — fonts vendored from the official [IBM/plex](https://github.com/IBM/plex) release. See `Unity/Assets/OpenPerimeter/UI/Fonts/OFL-LICENSE.txt`.

**Font scaling:** design system supports **100% / 115% / 130%** via root classes `op-font-scale-100|115|130`. Gallery includes a segmented control to preview. Prefer relative token font sizes so future accessibility settings can remap cleanly.

**Unity wiring:** `OpThemeController` applies `Font` assets (and Resources fallback under `Resources/OpenPerimeter/Fonts/`). Optional TextCore FontAssets can be generated in-Editor later for SDF quality — deferred polish.

---

## 6. Icons

- Stroke style, 24×24 viewBox sources in `Icons/svg/`.
- Runtime: `OpIconElement` draws with **Painter2D** (no emoji, no mandatory bitmap atlas).
- Kinds: check, warning, error, info, close, chevron, plus, minus, play, pause, abort, eye, theme, settings.

---

## 7. Components

All styled via USS tokens under `.op-*` classes. Factories live in `OpComponents`.

| Component | Class prefix | Notes |
| :--- | :--- | :--- |
| Button | `.op-button` | `primary` / default / `ghost` / `danger` |
| Segmented control | `.op-segmented` | Eye / pattern / strategy selection |
| Card | `.op-card` | Interaction grouping only |
| Chip | `.op-chip` | Status with optional icon |
| List row | `.op-list-row` | ≥48px tall |
| Toggle | `.op-toggle` | Accent when on |
| Stepper | `.op-stepper` | Mono value |
| Banner | `.op-banner` | info / warning / success / danger |
| Dialog | `.op-dialog` | Destructive confirm uses danger button |
| Toast | `.op-toast` | Transient feedback |
| Progress bar | `.op-progress` | Accent fill + mono % |

---

## 8. Theming API

```csharp
themeController.SetTheme(OpThemeMode.Dark);   // or Light
themeController.ToggleTheme();
themeController.SetFontScale(OpFontScale.Scale130);
```

Root classes: `op-root` + `op-theme-dark|op-theme-light` + `op-font-scale-*`.

---

## 9. Gallery

1. Open Unity project.
2. Menu: **OpenPerimeter → Design System → Ensure Gallery Assets** (creates `OpPanelSettings`, assigns USS/fonts).
3. **OpenPerimeter → Design System → Open Component Gallery** (enters Play mode).
4. Use **Theme** and **100% / 115% / 130%** controls.

Scene: `Unity/Assets/OpenPerimeter/UI/Gallery/ComponentGallery.unity`.

---

## 10. Phase 1 vs later

### Done in Phase 1
- Tokens, dual theme, type, icons, component kit, gallery, this document, contrast & touch notes.

### Deferred to Phase 2+
- Migrating the live `OpenPerimeterUI` (IMGUI) exam flow onto these UITK components.
- Full app navigation / screen layouts.
- Motion system beyond token durations.
- TextCore SDF FontAssets & PanelTextSettings localization.
- Device QA of contrast under OLED brightness overrides (exam path still uses separate stimulus pipeline).
- Automated visual regression of the gallery.

---

## 11. Non-goals (hard)

Do **not** change: stimulus rendering, presentation timing, calibration math, threshold engines (ZEST / 4-2), reliability monitors, or encrypted storage while applying this design system.
