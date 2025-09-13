# Shader Variant Collection Tool
A tool for managing and collecting shader variants efficiently and quickly, and also includes a better UI for the default Unity shader variant collection file editor (Inspector element)

## Table of Contents
- [Screenshots](#screenshots)
- [Installation](#installation)
- [Quick Start Tutorial](#quick-start-tutorial)
- [Filters Guide](#filters-guide)
- [Manual Keyword Combinations](#manual-keyword-combinations)
- [Variant File Ehnanced UI and Browser](#variant-file-ehnanced-ui-and-browser)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)


## Screenshots
<img width="347" height="608" alt="image" src="https://github.com/user-attachments/assets/266da9cf-38b0-4d00-a582-f926eb2c9e47" />

<img width="2624" height="1289" alt="image" src="https://github.com/user-attachments/assets/ca4f9271-3a6a-49a0-bd2d-9e3ede1ef14d" />

<img width="2619" height="1287" alt="image" src="https://github.com/user-attachments/assets/c0491335-5367-4927-bb9f-d2cb25cd1a07" />

<img width="2614" height="1249" alt="image" src="https://github.com/user-attachments/assets/5702adb4-a6e4-4ef3-8647-b1d3f442551d" />

<img width="3817" height="1433" alt="image" src="https://github.com/user-attachments/assets/bd4dce8c-7d7d-454f-96d7-b4576e0aa6b9" />


## Installation

### Via Unity Package Manager
1. Open Unity Package Manager (Window → Package Manager)
2. Click the **+** button in the top-left corner
3. Select **"Add package from git URL..."**
4. Install with ``` https://github.com/revoconner/ShaderVariantCollectionTool.git``` 
5. Click **Add**

### Requirements
- Unity 2022.3 or higher
- Compatible with all render pipelines (Built-in, URP, HDRP) tested with URP only
- Tested with Unity 6.1

## Quick Start Tutorial

1. **Open the tool**
2. **Add a variant collection file**
3. **Add a config file**
4. **Follow the numbers on the button**
5. **Profit?**





## Filters Guide



### Variant Filters

#### VariantFilter_Shader
Filter variants based on shader inclusion/exclusion.

**Configuration:**
- **Mode**:
  - `Strip`: Remove variants for listed shaders
  - `OnlyReserveContains`: Keep only variants for listed shaders
- **Shaders**: Array of shaders to filter

**Advanced Operations** (via foldout menu):
- Add shader at specific index
- Delete shader at index
- Swap shader positions

#### VariantFilter_PassStrip
Remove variants of specific pass types.

**Configuration:**
- **Strip Passes**: Array of PassType values to remove

**Common Pass Types to Strip:**
- `Meta`: Editor-only lightmapping
- `MotionVectors`: Not needed for Quest
- `ScriptableRenderPipelineBatching`: If not using SRP Batcher

### Keywork Filters

Added in v1.2.0 
- Filters out individual keywords or combinations of keywords
- If the checkbox on the right side is checked, all combinations including those keywords will be excluded
- Takes precendent over MANUAL KEYWORD COMBINATION

## Manual Keyword Combinations

### When to Use
- Keywords enabled only at runtime via script
- Platform-specific keywords not active in editor
- Quality setting variations
- Feature toggles

### How to Add
1. Click **Manual Keyword Combinations**
2. For each combination:
   - Click **Add New Combination**
   - Enter keywords separated by spaces
   - Example: `FOG_LINEAR SHADOWS_SOFT`
   - Example: `FOVEATED_RENDERING_NON_UNIFORM_RASTER`

### Important Notes
- Each line is processed independently
- Keywords must exist in shader to be valid
- Invalid combinations are silently skipped
- Check console for processing results

## Variant File Ehnanced UI and Browser
### Features
- **Shader List**: All shaders in collection
- **Filter**: Type to filter shader names
- **Add Shader**: Object field + Add button
- **Clear**: Remove all variants (with confirmation)

### Variant Inspector
For selected shader:
- Shows variants grouped by PassType
- Displays keyword combinations
- Add button per pass type
- Remove button per variant
- Keyword filtering with exact match option

### Adding Variants Manually
1. Select shader in list
2. Click + button
3. In popup window:
   - Select PassType
   - Click keywords from available list
   - Click selected keywords to remove
   - Click "Add Variant"

## Best Practices

### 1. Collection Strategy
- Start with MaterialCollection_SceneDependency for scene materials
- Add MaterialCollection_TotalMaterial for comprehensive coverage
- Use MaterialCollection_AssignMaterial for specific additions

### 2. Filtering Strategy
- Strip editor-only passes (Meta, MotionVectors)
- Remove unused shader variants with VariantFilter_Shader
- Create custom filters for project-specific needs

### 3. Organization
- Create separate variant collections for:
  - Platform-specific variants (Quest, Mobile, Desktop)
  - Quality levels (Low, Medium, High)
  - Feature sets (VR, AR, Standard)

### 4. Validation
- Use **Material Source Check** to verify collection sources
- Use **Keyword Source Check** to find keyword usage
- Review variant count before writing to file



### Recommended Filters
1. Strip desktop-only passes:
   - MotionVectors
   - GrabPass
   - Meta (if not lightmapping)

2. Remove heavy shaders:
   - Complex transparent shaders
   - Multi-pass shaders
   - Tessellation shaders


## Troubleshooting

### No Materials Collected
- Verify collector configuration
- Check path spellings in TotalMaterial collector
- Ensure scenes are in Build Settings for SceneDependency
- Check console for error messages

### Missing Runtime Variants
- Add manual keyword combinations
- Verify keywords exist in target shaders
- Check keyword spelling (case-sensitive)
- Enable all quality levels before collection

### Tool Window Issues
- Window not opening: Check Unity version compatibility
- Missing config file: Check the Assets/Settings folder, else use the button in the UI to create one.



## Credits

Original tool concept from Chinese Unity community - Soco

---
## Versions
- *Last Updated: Sepetember 07, 2025 - Version 1.0.0*
- *Last Updated: Sepetember 10, 2025 - Version 1.1.0*
- *Last Updated: Sepetember 13, 2025 - Version 1.2.0*
