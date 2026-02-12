# 3D Outline Shaders - Usage Guide

I've created three different outline shader approaches for you to try. Each has different use cases and performance characteristics.

---

## 1. Inverted Hull Outline
**File:** `Assets/Shaders/OutlineInvertedHull.shader`

### How It Works:
- Renders the object twice
- First pass: Expands vertices along normals and renders backfaces
- Second pass: Normal forward rendering

### How to Use:
1. Create a new Material in Unity
2. Set shader to: **Custom/OutlineInvertedHull**
3. Assign material to any 3D object
4. Adjust properties:
   - **Outline Color**: Color of the outline
   - **Outline Width**: Thickness (0-0.1)
   - **Texture & Color**: Base appearance

### Best For:
- Characters and organic shapes
- Clean, uniform outlines
- Good performance
- Cartoony/anime style

### Limitations:
- Doesn't work well on objects with sharp edges or hard normals
- Outline thickness is in world space

---

## 2. Post-Process Outline
**Files:** 
- `Assets/Shaders/OutlinePostProcess.shader`
- `Assets/Scripts/Rendering/OutlinePostProcessFeature.cs`

### How It Works:
- Screen-space effect using depth and normals
- Detects edges using Sobel filter
- Applied as post-processing to entire scene or specific layers

### How to Use:
1. **Setup URP Renderer:**
   - Go to your URP Renderer asset (usually in Settings folder)
   - Click "Add Renderer Feature"
   - Select "Outline Post Process Feature"

2. **Configure the Feature:**
   - Create a material with shader: **Hidden/OutlinePostProcess**
   - Assign material to the feature
   - Adjust settings:
     - **Outline Color**: Edge color
     - **Outline Thickness**: Edge width (0.1-5)
     - **Depth Sensitivity**: How sensitive to depth changes (0-100)
     - **Normal Sensitivity**: How sensitive to normal changes (0-10)

3. **Enable Depth & Normals:**
   - In your URP Renderer asset, enable:
     - Depth Texture: ON
     - Opaque Texture: Optional
   - Or add this to your pipeline settings

### Best For:
- Highlighting selected objects
- Entire scene outlines
- Edge detection effects
- Technical/blueprint style

### Limitations:
- More expensive (processes entire screen)
- Can't control per-object
- Requires depth and normal textures

---

## 3. Vertex Color Based Outline
**File:** `Assets/Shaders/OutlineVertexColor.shader`

### How It Works:
- Same as inverted hull, but uses vertex colors
- Red channel controls outline width
- Allows artist control per-vertex in modeling software

### How to Use:
1. Create a new Material
2. Set shader to: **Custom/OutlineVertexColor**
3. Assign to object with vertex colors painted
4. Enable "Use Vertex Color for Outline" checkbox
5. Adjust properties:
   - **Outline Color**: Base outline color
   - **Outline Width**: Base thickness
   - **Vertex Color Influence**: How much vertex colors affect the result (0-1)

### Painting Vertex Colors:
- In Blender/Maya/3D software:
  - Red channel: Controls outline width (1 = normal, 0 = no outline)
  - Can also tint outline color

### Best For:
- Artistic control over outline appearance
- Variable width outlines
- Character details (thicker outlines on limbs, thinner on face)
- Stylized effects

### Limitations:
- Requires vertex color data on mesh
- More setup work in 3D software

---

## Quick Comparison

| Feature | Inverted Hull | Post-Process | Vertex Color |
|---------|---------------|--------------|--------------|
| **Performance** | Good | Medium | Good |
| **Setup** | Easy | Medium | Hard |
| **Control** | Per-Material | Scene-wide | Per-Vertex |
| **Quality** | Clean | Very Clean | Artist-Controlled |
| **Works on** | Individual Objects | Everything | Individual Objects |

---

## Tips & Recommendations

**For Characters:** Start with Inverted Hull - it's the easiest and looks great

**For Entire Scene:** Use Post-Process for consistent look across everything

**For Advanced Art:** Use Vertex Color if you have artists who can paint vertex data

**Performance:** All three are reasonably fast, but Inverted Hull is the lightest

---

## Troubleshooting

**Outlines not showing:**
- Check material is assigned
- Verify URP is active
- For Post-Process: Check renderer feature is added and enabled

**Outlines look weird:**
- Inverted Hull: Recalculate normals on your mesh
- Post-Process: Adjust sensitivity values
- Vertex Color: Check vertex color data exists

**Depth/Normal errors (Post-Process):**
- Enable Depth Texture in URP Renderer settings
- Ensure objects cast shadows or are marked as renderers

Let me know which one works best for your needs!
