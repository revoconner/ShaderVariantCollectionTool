# Shader Variant Collection Tool
A tool for managing and collecting shader variants efficiently and quickly, and also includes a better UI for the default Unity shader variant collection file editor (Inspector element)




## Table of Contents
- [Installation](#installation)
- [Quick Start Tutorial](#quick-start-tutorial)
- [Features Overview](#features-overview)
- [Detailed Workflows](#detailed-workflows)
- [Material Collectors Guide](#material-collectors-guide)
- [Filters Guide](#filters-guide)
- [Manual Keyword Combinations](#manual-keyword-combinations)
- [Quick Browse Mode](#quick-browse-mode)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

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

### Step 1: Open the Tool
Navigate to **Tools → ShaderVariantCollectionTools → OpenWindow**

### Step 2: Create Required Files
1. **Variant Collection File**: 
   - Right-click in Project window
   - Create → Shader → Shader Variant Collection
   - Name it (e.g., "MyProjectVariants")
   
2. **Tool Configuration File**:
   - Auto-created on first use as "Default ShaderVariantCollection Tool Config"
   - Or create manually: Create → ShaderVariantCollectionTools → Create Config

### Step 3: Basic Collection Workflow
1. Select your Variant Collection File in the tool window
2. Click **Project Collection Tool**
3. Click **Collect Materials** to gather materials
4. Click **Material Variant Collection** to generate variants
5. Click **Write to Collection File** to save

## Features Overview

### Two Main Modes

1. **Quick Browse Mode**
   - View all shaders in your collection
   - See variants per shader with pass types
   - Add/remove variants manually
   - Filter variants by keywords

2. **Project Collection Tool**
   - Automated material collection
   - Manual keyword combination support
   - Material and variant filtering
   - Source tracking capabilities

## Detailed Workflows

### Complete Variant Collection Process

#### 1. Setup Material Collectors
Click **Material Collector List** and add collectors:

- **Add Collector**: Select type from dropdown → Click "Add Material Collector"
- **Configure**: Each collector appears with options
- **Enable/Disable**: Toggle "Use" checkbox
- **Reorder**: Use Up/Down buttons for priority

#### 2. Collect Materials
- Click **Collect Materials** button
- Number in parentheses shows collected count
- Progress bar shows collection status

#### 3. Add Manual Keywords (Optional) this helps combination of runtime keywords you want to include without running the game and taking values from there
For runtime-only keywords:
1. Click **Manual Keyword Combinations**
2. Click **Add New Combination**
3. Enter keywords separated by spaces 
4. Enable/disable with checkboxes

#### 4. Generate Variants
- Click **Material Variant Collection**
- Processes both material keywords and manual combinations
- Shows variant count in parentheses

#### 5. Apply Filters (Optional)
Configure filters before writing to file:
- **Material Filter**: Filter which materials to process
- **Variant Filter**: Filter which variants to keep

#### 6. Save to File
- Toggle **"Override source file content"** if replacing existing
- Click **Write to Collection File**
- Confirm in dialog

### One-Click Collection
For quick collection with current settings:
1. Configure collectors and filters once
2. Click **One-Click Collect Variants**
3. Confirms and runs entire pipeline automatically

## Material Collectors Guide

### MaterialCollection_TotalMaterial
Collects all materials from specified paths.

**Configuration:**
- **Path Mode**: 
  - `Asset`: Use folder references (drag folders)
  - `String`: Use string paths
- **Include Paths**: Array of paths to search
- **Folders**: Drag folders when in Asset mode

**Example Setup:**
```
Path Mode: String
Include Paths: ["Assets", "Packages"]
```

### MaterialCollection_SceneDependency
Collects all materials referenced by scenes in Build Settings.

**Configuration:**
- **Collect Only Enabled**: Only process enabled scenes in Build Settings

**Use Case:** Perfect for ensuring all scene materials have variants compiled.

### MaterialCollection_AssignMaterial
Manually specify exact materials to collect.

**Configuration:**
- **Materials**: Array of material references

**Use Case:** Testing specific materials or adding materials missed by other collectors.

## Filters Guide

### Material Filters

#### TestMaterialFilter
A template filter that passes all materials (does nothing).
Use as a base for custom filters.



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

## Quick Browse Mode

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


### Essential Manual Keywords for Quest
```
// Foveated Rendering
FOVEATED_RENDERING_NON_UNIFORM_RASTER

// Mobile Optimizations
SHADER_API_MOBILE
SHADER_API_GLES30

// VR Specific
STEREO_INSTANCING_ON
STEREO_MULTIVIEW_ON

// Performance
LOD_FADE_CROSSFADE
SHADOWS_SOFT
FOG_LINEAR

// Occlusion
META_OCCLUSION_ENABLED _ALPHATEST_ON
```

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

### Large Variant Count
- Use VariantFilter_Shader to limit shaders
- Strip unnecessary pass types
- Review manual keyword combinations
- Consider splitting into multiple collections

### Tool Window Issues
- Window not opening: Check Unity version compatibility
- Missing config file: Will auto-create on first use, reopen the tool



## Credits

Original tool concept from Chinese Unity community - Soco

---

*Last Updated: January 2025 - Version 1.0.0*
