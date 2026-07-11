# Auto Retopology Blender Add-On

A comprehensive Blender add-on for automatic mesh retopology with multiple methods and customization options.

## Features

- **Multiple Retopology Methods:**
  - Voxel Remesh (Fast, uniform topology)
  - Quadriflow (Quad-based remeshing with flow)
  - Decimate (Polygon reduction)
  - Remesh Modifier (Smooth uniform mesh)

- **Quick Actions:**
  - One-click quick retopology
  - Mesh analysis and statistics
  - Geometry cleanup tools

- **Smart Features:**
  - Automatic backup creation
  - Smooth shading application
  - Volume preservation
  - Customizable parameters

## Installation

1. Open Blender (3.0 or newer)
2. Go to `Edit` → `Preferences` → `Add-ons`
3. Click `Install...` button
4. Navigate to and select `auto_retopology_addon.py`
5. Enable the add-on by checking the checkbox next to "Mesh: Auto Retopology Tool"

## Usage

### Accessing the Add-on

1. In the 3D Viewport, press `N` to open the sidebar
2. Click on the `Retopo` tab
3. The Auto Retopology panel will appear

### Quick Start

1. Select a mesh object
2. Click **Quick Retopo** for instant retopology with automatic settings
3. Done!

### Advanced Usage

#### Step-by-Step Workflow:

1. **Analyze Mesh**
   - Click "Analyze Mesh" to see current polygon count and statistics
   - Check the console (Window → Toggle System Console) for detailed info

2. **Cleanup Geometry** (Recommended)
   - Click "Cleanup Mesh" to remove doubles, loose geometry, and fix normals
   - This prepares the mesh for better retopology results

3. **Choose Method**
   - Select your preferred retopology method:
     - **Voxel**: Best for organic shapes, fast processing
     - **Quadriflow**: Best for character models, creates quad-based topology
     - **Decimate**: Best for reducing polygon count while preserving shape
     - **Remesh**: Best for creating smooth, uniform surfaces

4. **Adjust Settings**
   - **Voxel Size** (Voxel method): Lower = more detail (try 0.05-0.2)
   - **Target Faces** (Quadriflow): Desired polygon count (5000-20000 typical)
   - **Ratio** (Decimate): Percentage to keep (0.5 = 50% reduction)
   - **Octree Depth** (Remesh): Higher = more detail (4-8 recommended)

5. **General Options**
   - **Smooth Shading**: Automatically applies smooth shading to result
   - **Create Backup**: Creates hidden copy of original (recommended!)

6. **Execute**
   - Click **Execute Retopology** button
   - Wait for processing (time varies by mesh complexity)
   - Check the result and adjust settings if needed

## Method Comparison

### Voxel Remesh
- **Speed**: Very Fast ⚡⚡⚡
- **Quality**: Good
- **Best For**: Organic shapes, sculpts
- **Pros**: Fast, uniform, preserves volume
- **Cons**: Less control over topology flow

### Quadriflow
- **Speed**: Slow ⚡
- **Quality**: Excellent
- **Best For**: Character models, animation
- **Pros**: Quad-based, follows surface flow
- **Cons**: Slow, requires Quadriflow support

### Decimate
- **Speed**: Fast ⚡⚡
- **Quality**: Variable
- **Best For**: LOD creation, optimization
- **Pros**: Precise control, preserves original topology
- **Cons**: Can create triangles, less uniform

### Remesh
- **Speed**: Medium ⚡⚡
- **Quality**: Good
- **Best For**: Clean, uniform surfaces
- **Pros**: Smooth result, customizable
- **Cons**: Less feature preservation

## Tips & Best Practices

1. **Always create a backup** (enable "Create Backup" option)
2. **Start with Quick Retopo** to test if default settings work
3. **Cleanup geometry first** for better results
4. **Use Analyze Mesh** to check before and after polygon counts
5. **For animation**: Use Quadriflow or Voxel methods
6. **For game assets**: Use Decimate for LODs
7. **Lower voxel size** = more detail but more polygons
8. **Higher target faces** = more detail (Quadriflow)

## Troubleshooting

### Quadriflow Not Available
- Quadriflow requires special Blender build or add-on
- Use Voxel Remesh as alternative

### Mesh Too Dense/Sparse
- **Too dense**: Increase voxel size or decrease target faces
- **Too sparse**: Decrease voxel size or increase target faces

### Modifier Not Applying
- Ensure object is in Object Mode
- Check for existing modifiers that might conflict
- Enable "Apply Modifier" option

### Result Has Artifacts
- Run "Cleanup Mesh" before retopology
- Try different method
- Adjust settings (usually lower detail helps)

## Keyboard Shortcuts

No default shortcuts, but you can assign them:
1. Right-click any button in the panel
2. Select "Assign Shortcut"
3. Press your desired key combination

## System Requirements

- Blender 3.0 or newer
- Python 3.x (included with Blender)
- For Quadriflow: Blender with Quadriflow support

## Support & Development

This add-on is part of the Achronas Project.

## License

Free to use and modify for personal and commercial projects.

## Changelog

### Version 1.0.0
- Initial release
- Multiple retopology methods
- Quick actions and cleanup tools
- Mesh analysis
- Automatic backup system
- Comprehensive UI with tips and info

---

**Happy Retopologizing! 🎨**
