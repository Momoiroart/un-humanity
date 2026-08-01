# UN-HUMANITY — street kit import report
_Generated 2026-08-01 18:43 by StreetKitSetup.RunPhase1 (measured, not typed)_

- removed template item `Assets/TutorialInfo`
- removed template item `Assets/Readme.asset`
- folders ensured under `Assets/Art/StreetBlock`
- parsed 18 distinct SURF_* materials from MTL library
- created 18 shared M_* materials (URP/Lit, metallic 0, smoothness 0, spec off)
- copied 46 kit files into `Assets/Art/StreetBlock/Meshes` and imported
- built 23 prefabs (box collider fitted to bounds, Batching/Occludee/GI static; Occluder off on decals)
- PC_Renderer → rendering path Forward+
- PC_RPAsset → GPU Resident Drawer: instanced drawing
- PC_RPAsset → GPU occlusion culling: on
- PC_RPAsset → shadow distance 100 m, 2 cascades
- scene `Assets/Scenes/SC_02_StreetBlock.unity`: key light, ink/wine flat ambient, exp fog, diorama camera (FOV 26 · pitch 32 · clip 1/120)

## Measured meshes

| mesh | tris | verts | bounds (m) | materials |
|---|---|---|---|---|
| SM_Barricade_Stand | 276 | 552 | 2.1×1.21×0.67 | M_Aluminium_Brushed, M_Emissive_Display, M_Rubber_Black, M_Steel_Painted, M_Tape_Warning |
| SM_Bench_Aged | 216 | 432 | 1.56×0.98×0.56 | M_Laminate_Kiosk, M_Rubber_Black, M_Steel_Rust, M_Wood_Pole |
| SM_Bench_Standard | 204 | 408 | 1.56×0.98×0.56 | M_Aluminium_Brushed, M_Rubber_Black, M_Steel_Painted |
| SM_Bollard_01 | 224 | 266 | 0.3×1.15×0.3 | M_Concrete_Kerb, M_Emissive_Display, M_Metal_Galvanised, M_Paper_Newsprint, M_Steel_Painted, M_Steel_Rust |
| SM_BusStopSign_01 | 148 | 244 | 0.56×2.84×0.32 | M_Aluminium_Brushed, M_Laminate_Kiosk, M_Metal_Galvanised, M_Paper_Newsprint, M_Steel_Painted, M_Steel_Rust, M_Tape_Warning |
| SM_CrosswalkDecal_Set | 96 | 192 | 5.4×0.01×3.85 | M_Asphalt_Damp, M_Paper_Newsprint |
| SM_Curb_Corner | 84 | 168 | 1.45×0.41×1.45 | M_Concrete_Kerb, M_Concrete_Sidewalk |
| SM_Curb_Straight_4m | 48 | 96 | 0.31×0.41×4 | M_Concrete_Kerb, M_Concrete_Sidewalk, M_Steel_Painted |
| SM_FacadeModule_A_Shop | 296 | 564 | 4×4.66×0.87 | M_Aluminium_Brushed, M_Brick_Facade, M_Concrete_Kerb, M_Glass_Tempered, M_Laminate_Kiosk, M_Metal_Galvanised, M_Plaster_Facade, M_Steel_Painted, M_Steel_Rust |
| SM_FacadeModule_B_Flats | 444 | 888 | 4×6.02×0.72 | M_Brick_Facade, M_Concrete_Kerb, M_Glass_Tempered, M_Laminate_Kiosk, M_Metal_Galvanised, M_Plaster_Facade, M_Steel_Painted, M_Steel_Rust |
| SM_GutterChannel_4m | 48 | 96 | 0.5×0.26×4 | M_Asphalt_Damp, M_Concrete_Kerb |
| SM_RoadModule_8x8 | 84 | 168 | 8×0.21×8 | M_Asphalt_Damp, M_Asphalt_Worn, M_Paper_Newsprint |
| SM_RouteSign_01 | 136 | 238 | 0.72×2.5×0.24 | M_Aluminium_Brushed, M_Metal_Galvanised, M_Paper_Newsprint, M_Steel_Painted, M_Tape_Warning |
| SM_Shutter_Corrugated | 240 | 480 | 3.04×2.8×0.36 | M_Metal_Galvanised, M_Rubber_Black, M_Shutter_Corrugated, M_Steel_Painted, M_Steel_Rust |
| SM_SidewalkSlab_1x4 | 72 | 144 | 1×0.16×4 | M_Asphalt_Worn, M_Concrete_Kerb, M_Concrete_Sidewalk |
| SM_SidewalkSlab_Corner | 60 | 120 | 1×0.16×1 | M_Asphalt_Worn, M_Concrete_Kerb, M_Concrete_Sidewalk |
| SM_StormDrain_Grate | 156 | 312 | 0.94×0.14×0.66 | M_Concrete_Kerb, M_Metal_Galvanised, M_Rubber_Black, M_Steel_Rust |
| SM_StreetLight_01 | 320 | 432 | 0.42×6.68×2.91 | M_Aluminium_Brushed, M_Concrete_Kerb, M_Emissive_Display, M_Metal_Galvanised, M_Steel_Painted, M_Steel_Rust |
| SM_TimetableCase_01 | 160 | 264 | 0.78×1.9×0.1 | M_Aluminium_Brushed, M_Glass_Tempered, M_Metal_Galvanised, M_Paper_Newsprint, M_Steel_Painted, M_Tape_Warning |
| SM_TransitKiosk_01 | 144 | 288 | 0.84×2.11×0.58 | M_Aluminium_Brushed, M_Emissive_Display, M_Laminate_Kiosk, M_Metal_Galvanised, M_Paper_Newsprint, M_Rubber_Black, M_Steel_Painted |
| SM_TrashBin_LidOpen | 268 | 328 | 0.62×1.43×0.62 | M_Metal_Galvanised, M_Paper_Newsprint, M_Rubber_Black, M_Steel_Painted, M_Steel_Rust |
| SM_UtilityPole_01 | 452 | 580 | 2.2×8.06×0.58 | M_Glass_Tempered, M_Metal_Galvanised, M_Steel_Painted, M_Steel_Rust, M_Wood_Pole |
| SM_VendingMachine_01 | 204 | 408 | 0.99×1.93×0.85 | M_Aluminium_Brushed, M_Emissive_Display, M_Glass_Tempered, M_Laminate_Kiosk, M_Rubber_Black, M_Steel_Painted, M_Tape_Warning |

**Total imported triangles: 4380** (Unity-measured)
