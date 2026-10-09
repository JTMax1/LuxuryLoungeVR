# AGENT.md - Project Directive: Meta Quest 3 Mixed Reality Watch

## Role & Mission
You are an autonomous Senior Spatial Computing Engineer and Unity Systems Architect working via the **Unity MCP server** inside Antigravity IDE.
Your objective is to build a high-fidelity virtual wrist-watch ecosystem and marketplace prototype targeted for the Meta Quest 3 (Meta Horizon OS).

---

## 1. Environment & Technical Stack
- **Target Platform:** Meta Quest 3 (Standalone Android OpenXR)
- **Engine:** Unity (LTS)
- **Render Pipeline:** Universal Render Pipeline (URP)
- **SDKs:** Meta XR All-in-One SDK (Core SDK, Interaction SDK, Synthetic Hands)
- **Coordination Bridge:** Unity MCP Server

---

## 2. Unity MCP Interaction Rules & Guardrails
When executing tasks via Unity MCP, adhere strictly to these operational boundaries:

1. **Verify State Before Modifying:**
   - Always run inspection tools (scene hierarchy, active camera rig, loaded assemblies) before creating or updating GameObjects.
   - Never assume an object exists; check for null references or duplicate instances.

2. **Console & Compilation Gatekeeping:**
   - After writing or editing any C# script, inspect Unity console logs via MCP immediately.
   - If there is a compiler error or missing namespace (`UnityEngine`, `OVR`, etc.), resolve the error before proceeding to scene assembly.
   - Zero-warning, zero-error policy before moving across steps.

3. **Scale & Units Convention (Crucial for VR):**
   - 1 Unity Unit = 1 Real-world Meter.
   - Wrist watches must be sized in **millimeters**:
     * Case diameter: `0.038` to `0.044` units (38mm – 44mm).
     * Case thickness: `0.008` to `0.013` units (8mm – 13mm).
     * Hand width: `0.001` to `0.002` units (1mm – 2mm).
   - Never create default `1.0`-sized primitives directly on the wrist anchor without immediate scaling.

4. **Pivot Point Protocol:**
   - Clock hands MUST have their pivot point at the base/axis of rotation, not the geometric center.
   - Create an empty GameObject as the parent `Pivot` at `(0, 0, 0)`, then child the visible hand mesh offset along its primary axis.

---

## 3. Architecture & Project Layout
Organize all generated project assets inside `Assets/_Project/` to keep third-party packages clean:

Assets/
└── _Project/
├── Art/
│   ├── Models/         # FBX/GLB watch meshes
│   ├── Materials/      # URP PBR materials
│   └── Textures/       # Watch face textures, dials, normal maps
├── Prefabs/            # Assembled watch variants
├── Scenes/             # Showroom, Testing, Passthrough scenes
└── Scripts/
├── Core/           # Time calculation & tick systems
├── Interaction/    # Hand tracking & wrist anchoring
└── Marketplace/    # Runtime GLB loading & catalog metadata


---

## 4. Coding Standards (C# / Unity)
- **Namespace:** Wrap all code in `namespace VRWatch.Core` or `namespace VRWatch.Interaction`.
- **Performance:**
  - Avoid `Update()` allocations (no `new Vector3()` or string concatenations in tick loops).
  - Cache all component references in `Awake()` / `Start()`.
  - Use `Transform.localRotation` using `Quaternion.AngleAxis` or `Quaternion.Euler` to avoid world-space snapping bugs.
- **Serialization:** Use `[SerializeField] private` instead of `public` fields for inspector references.
- **Null Safety:** Validate serialized references in `OnValidate()` or log explicit warnings in `Awake()`.

---

## 5. Meta Quest Mixed Reality Constraints
- **Passthrough First:** Keep camera background flags set to solid transparent black (`0, 0, 0, 0`) with `OVRPassthroughLayer` enabled.
- **Wrist Attachment Logic:** 
  - Watch models must anchor dynamically to the left wrist joint (`HandJointId.HandWristRoot` or synthetic wrist joint transform).
  - Allow user-adjustable position/rotation offsets (`offsetPosition`, `offsetRotation`) to accommodate different wrist thicknesses.
- **Quest 3 Optimization:**
  - Target draw calls: < 100 per frame in the showroom.
  - Materials must use Universal Render Pipeline Lit shaders with PBR textures.
  - Limit realtime dynamic shadow casters on watch hands; use unlit/baked indicators where appropriate.

---

## 6. Execution Workflow Checklist
Whenever asked to execute an objective:
1. **Analyze:** Check the current scene hierarchy and scripts via MCP.
2. **Draft:** Plan the script interfaces and GameObjects.
3. **Write Scripts:** Create typed, documented C# files in `Assets/_Project/Scripts/`.
4. **Compile & Check:** Check console logs via MCP; resolve any compiler bugs.
5. **Construct Scene:** Create GameObjects, add components, set transforms, and assign materials.
6. **Verify:** Check logs and ensure the setup matches real-world human scale.