#!/usr/bin/env python3
"""Generate FloodMassRollPitch.unity for the Flood Mass Integration sample."""

from pathlib import Path

OUT = Path(
    "Packages/com.rabbidwolf.com.kyle.flooding/Samples~/Mass Integration/"
    "FloodMassRollPitch.unity"
)

GUID_MANAGER = "10ec3c8e55fa4b889d2678b5cfe1af2a"
GUID_AGG = "8d6ff04e06084bf9bd8f3619939d0dac"
GUID_ADAPTER = "11c0fd70f5f646f4a2607e2cbe73d43c"
GUID_VOLUME = "03f480fc6b49c57428680e494034225f"
GUID_CUBE_RENDERER = "d26c5ed76e2242dba85ed51859befb9a"
GUID_SUPPORT = "5606d7887718475c8291d151f9a94798"
GUID_BOOTSTRAP = "c82e1f04a9b64e0f9d3a7c5b1e8f4026"
GUID_VESSEL = "74e15ce586bb4a91a46b96ec7f8f7350"
GUID_WATER = "a1b2c3d4e5f6478910abcdef01234501"
GUID_GROUND = "a1b2c3d4e5f6478910abcdef01234502"
GUID_DRY = "a1b2c3d4e5f6478910abcdef01234503"
GUID_FLOOD = "a1b2c3d4e5f6478910abcdef01234504"
GUID_COMBINED = "a1b2c3d4e5f6478910abcdef01234505"
GUID_LINE = "a1b2c3d4e5f6478910abcdef01234506"
CUBE_MESH = "10202"
SPHERE_MESH = "10207"


def game_object(fid, name, components, active=1):
    comps = "\n".join(f"  - component: {{fileID: {c}}}" for c in components)
    return f"""--- !u!1 &{fid}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comps}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: {active}
"""


def transform(fid, go_id, pos, scale, children, father=0, rot="0, 0, 0, 1"):
    rx, ry, rz, rw = [p.strip() for p in rot.split(",")]
    px, py, pz = pos
    sx, sy, sz = scale
    if children:
        children_block = "\n" + "\n".join(f"  - {{fileID: {c}}}" for c in children)
    else:
        children_block = " []"
    return f"""--- !u!4 &{fid}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: {rx}, y: {ry}, z: {rz}, w: {rw}}}
  m_LocalPosition: {{x: {px}, y: {py}, z: {pz}}}
  m_LocalScale: {{x: {sx}, y: {sy}, z: {sz}}}
  m_ConstrainProportionsScale: 0
  m_Children:{children_block}
  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""


def mesh_filter(fid, go_id, mesh_id):
    return f"""--- !u!33 &{fid}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Mesh: {{fileID: {mesh_id}, guid: 0000000000000000e000000000000000, type: 0}}
"""


def mesh_renderer(fid, go_id, mat_guid):
    return f"""--- !u!23 &{fid}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: {mat_guid}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 1
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
"""


def cube(go_id, tr, mf, mr, name, parent_tr, pos, scale, mat):
    return (
        game_object(go_id, name, [tr, mf, mr])
        + transform(tr, go_id, pos, scale, [], father=parent_tr)
        + mesh_filter(mf, go_id, CUBE_MESH)
        + mesh_renderer(mr, go_id, mat)
    )


def sphere(go_id, tr, mf, mr, name, parent_tr, pos, scale, mat):
    return (
        game_object(go_id, name, [tr, mf, mr])
        + transform(tr, go_id, pos, scale, [], father=parent_tr)
        + mesh_filter(mf, go_id, SPHERE_MESH)
        + mesh_renderer(mr, go_id, mat)
    )


def compartment(base, name, parent_tr, pos, manager_id):
    go_id = base
    tr_id = base + 1
    vol_id = base + 2
    rend_id = base + 3
    water_go = base + 10
    water_tr = base + 11
    water_mf = base + 12
    water_mr = base + 13
    parts = []
    parts.append(game_object(go_id, name, [tr_id, vol_id, rend_id]))
    parts.append(transform(tr_id, go_id, pos, (1, 1, 1), [water_tr], father=parent_tr))
    parts.append(
        f"""--- !u!114 &{vol_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_VOLUME}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Kyle.Flooding.Runtime::Kyle.Flooding.FloodVolume
  simulationManager: {{fileID: {manager_id}}}
  geometryMode: 0
  width: 1.8
  length: 2.8
  polygonFootprint:
  - {{x: -0.9, y: -1.4}}
  - {{x: 0.9, y: -1.4}}
  - {{x: 0.9, y: 1.4}}
  - {{x: -0.9, y: 1.4}}
  maximumHeight: 1
  bakedVolumeData: {{fileID: 0}}
  waterDensity: 1000
  initialVolume: 0
  legacyInitialWaterHeight: -1
"""
    )
    parts.append(
        f"""--- !u!114 &{rend_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_CUBE_RENDERER}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Kyle.Flooding.Runtime::Kyle.Flooding.FloodCubeSurfaceRenderer
  floodVolume: {{fileID: {vol_id}}}
  interpolationDuration: 0.1
  waterVisual: {{fileID: {water_tr}}}
  minimumVisibleHeight: 0.01
"""
    )
    parts.append(cube(water_go, water_tr, water_mf, water_mr, "Water Visual", tr_id, (0, 0, 0), (1, 1, 1), GUID_WATER))
    return "".join(parts), vol_id, tr_id


def main():
    chunks = ["%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"]

    # Camera
    chunks.append(
        """--- !u!1 &1000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 1001}
  - component: {fileID: 1002}
  - component: {fileID: 1003}
  m_Layer: 0
  m_Name: Main Camera
  m_TagString: MainCamera
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"""
    )
    chunks.append(
        transform(
            1001,
            1000,
            (9.5, 7.5, -11),
            (1, 1, 1),
            [],
            father=0,
            rot="0.2164396, -0.3753303, 0.090534, 0.8976777",
        )
    )
    chunks.append(
        """--- !u!20 &1002
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1000}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 2
  m_BackGroundColor: {r: 0.12, g: 0.16, b: 0.2, a: 1}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: 50
  m_BladeCount: 5
  m_Curvature: {x: 2, y: 11}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {x: 36, y: 24}
  m_LensShift: {x: 0, y: 0}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 45
  orthographic: 0
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {fileID: 0}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
--- !u!81 &1003
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1000}
  m_Enabled: 1
"""
    )

    # Light
    chunks.append(game_object(2000, "Directional Light", [2001, 2002]))
    chunks.append(
        transform(
            2001,
            2000,
            (0, 8, 0),
            (1, 1, 1),
            [],
            father=0,
            rot="0.369644, -0.239118, 0.099046, 0.892399",
        )
    )
    chunks.append(
        """--- !u!108 &2002
Light:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2000}
  m_Enabled: 1
  serializedVersion: 12
  m_Type: 1
  m_Color: {r: 1, g: 0.97, b: 0.92, a: 1}
  m_Intensity: 1.15
  m_Range: 10
  m_SpotAngle: 30
  m_InnerSpotAngle: 21.8
  m_CookieSize: 10
  m_Shadows:
    m_Type: 0
    m_Resolution: -1
    m_CustomResolution: -1
    m_Strength: 1
    m_Bias: 0.05
    m_NormalBias: 0.4
    m_NearPlane: 0.2
    m_CullingMatrixOverride:
      e00: 1
      e01: 0
      e02: 0
      e03: 0
      e10: 0
      e11: 1
      e12: 0
      e13: 0
      e20: 0
      e21: 0
      e22: 1
      e23: 0
      e30: 0
      e31: 0
      e32: 0
      e33: 1
    m_UseCullingMatrixOverride: 0
  m_Cookie: {fileID: 0}
  m_DrawHalo: 0
  m_Flare: {fileID: 0}
  m_RenderMode: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingLayerMask: 1
  m_Lightmapping: 4
  m_LightShadowCasterMode: 0
  m_AreaSize: {x: 1, y: 1}
  m_BounceIntensity: 1
  m_ColorTemperature: 5000
  m_UseColorTemperature: 0
  m_BoundingSphereOverride: {x: 0, y: 0, z: 0, w: 0}
  m_UseBoundingSphereOverride: 0
  m_UseViewFrustumForShadowCasterCull: 1
  m_ForceVisible: 0
  m_ShadowRadius: 0
  m_ShadowAngle: 0
  m_LightUnit: 1
  m_LuxAtDistance: 1
  m_EnableSpotReflector: 1
"""
    )

    # Ground
    chunks.append(cube(3000, 3001, 3002, 3003, "Ground Plane", 0, (0, -0.05, 0), (24, 0.1, 24), GUID_GROUND))

    # Vessel root children transforms
    vessel_children = [
        4101,  # Hull Cutaway
        4201,  # Dry
        4301,  # Flood
        4401,  # Combined
        4501,  # Line
        5001,  # Port Bow
        5201,  # Starboard Bow
        5401,  # Port Stern
        5601,  # Starboard Stern
    ]

    chunks.append(
        game_object(
            4000,
            "Flood Mass Demo Vessel",
            [4001, 4002, 4003, 4004, 4005, 4006, 4007, 4008],
        )
    )
    chunks.append(transform(4001, 4000, (0, 1, 0), (1, 1, 1), vessel_children, father=0))
    chunks.append(
        f"""--- !u!65 &4002
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 4000}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {{x: 4, y: 1.1, z: 6}}
  m_Center: {{x: 0, y: 0, z: 0}}
--- !u!54 &4003
Rigidbody:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 4000}}
  serializedVersion: 5
  m_Mass: 1500
  m_LinearDamping: 0.8
  m_AngularDamping: 2
  m_CenterOfMass: {{x: 0, y: 0, z: 0}}
  m_InertiaTensor: {{x: 1, y: 1, z: 1}}
  m_InertiaRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ImplicitCom: 1
  m_ImplicitTensor: 1
  m_UseGravity: 1
  m_IsKinematic: 0
  m_Interpolate: 1
  m_Constraints: 0
  m_CollisionDetection: 0
--- !u!114 &4004
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 4000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_MANAGER}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Kyle.Flooding.Runtime::Kyle.Flooding.FloodSimulationManager
  ticksPerSecond: 10
  maximumTicksPerFrame: 4
  simulateAutomatically: 1
  gravityMode: 0
  customGravity: {{x: 0, y: -9.81, z: 0}}
--- !u!114 &4005
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 4000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_AGG}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Kyle.Flooding.Runtime::Kyle.Flooding.FloodMassAggregator
  includeInactive: 0
--- !u!114 &4006
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 4000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_ADAPTER}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Kyle.Flooding.Runtime::Kyle.Flooding.RigidbodyFloodMassAdapter
  floodMass: {{fileID: 4005}}
  dryMass: 1500
  dryCenterOfMassLocal: {{x: 0, y: 0, z: 0}}
--- !u!114 &4007
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 4000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_SUPPORT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  supportHeight: 1
  springStiffness: 14000
  damping: 2800
  halfWidth: 2
  halfLength: 3
  supportPointY: -0.55
"""
    )

    # Hull cutaway
    hull_children = [4111, 4121, 4131, 4141, 4151, 4161, 4171]
    chunks.append(game_object(4100, "Hull Cutaway", [4101]))
    chunks.append(transform(4101, 4100, (0, 0, 0), (1, 1, 1), hull_children, father=4001))
    chunks.append(cube(4110, 4111, 4112, 4113, "Floor", 4101, (0, -0.55, 0), (4.1, 0.1, 6.1), GUID_VESSEL))
    chunks.append(cube(4120, 4121, 4122, 4123, "Port Wall", 4101, (-2, -0.25, 0), (0.08, 0.5, 6), GUID_VESSEL))
    chunks.append(cube(4130, 4131, 4132, 4133, "Starboard Wall", 4101, (2, -0.25, 0), (0.08, 0.5, 6), GUID_VESSEL))
    chunks.append(cube(4140, 4141, 4142, 4143, "Bow Wall", 4101, (0, -0.25, 3), (4, 0.5, 0.08), GUID_VESSEL))
    chunks.append(cube(4150, 4151, 4152, 4153, "Stern Wall", 4101, (0, -0.25, -3), (4, 0.5, 0.08), GUID_VESSEL))
    chunks.append(cube(4160, 4161, 4162, 4163, "Centerline Bulkhead", 4101, (0, -0.25, 0), (0.06, 0.5, 5.8), GUID_VESSEL))
    chunks.append(cube(4170, 4171, 4172, 4173, "Cross Bulkhead", 4101, (0, -0.25, 0), (3.8, 0.5, 0.06), GUID_VESSEL))

    # COM markers
    chunks.append(sphere(4200, 4201, 4202, 4203, "Dry Com Marker", 4001, (0, 0, 0), (0.18, 0.18, 0.18), GUID_DRY))
    chunks.append(sphere(4300, 4301, 4302, 4303, "Flood Com Marker", 4001, (0, 0, 0), (0.16, 0.16, 0.16), GUID_FLOOD))
    chunks.append(sphere(4400, 4401, 4402, 4403, "Combined Com Marker", 4001, (0, 0, 0), (0.2, 0.2, 0.2), GUID_COMBINED))

    # Line renderer
    chunks.append(game_object(4500, "COM Shift Line", [4501, 4502]))
    chunks.append(transform(4501, 4500, (0, 0, 0), (1, 1, 1), [], father=4001))
    chunks.append(
        f"""--- !u!120 &4502
LineRenderer:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 4500}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 0
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: {GUID_LINE}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_Positions:
  - {{x: 0, y: 0, z: 0}}
  - {{x: 0, y: 0, z: 0}}
  m_Parameters:
    serializedVersion: 3
    widthMultiplier: 1
    widthCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0.04
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0.33333334
        outWeight: 0.33333334
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
    colorGradient:
      serializedVersion: 2
      key0: {{r: 1, g: 0.75, b: 0.15, a: 1}}
      key1: {{r: 0.85, g: 0.2, b: 1, a: 1}}
      key2: {{r: 0, g: 0, b: 0, a: 0}}
      key3: {{r: 0, g: 0, b: 0, a: 0}}
      key4: {{r: 0, g: 0, b: 0, a: 0}}
      key5: {{r: 0, g: 0, b: 0, a: 0}}
      key6: {{r: 0, g: 0, b: 0, a: 0}}
      key7: {{r: 0, g: 0, b: 0, a: 0}}
      ctime0: 0
      ctime1: 65535
      ctime2: 0
      ctime3: 0
      ctime4: 0
      ctime5: 0
      ctime6: 0
      ctime7: 0
      atime0: 0
      atime1: 65535
      atime2: 0
      atime3: 0
      atime4: 0
      atime5: 0
      atime6: 0
      atime7: 0
      m_Mode: 0
      m_ColorSpace: -1
      m_NumColorKeys: 2
      m_NumAlphaKeys: 2
    numCornerVertices: 0
    numCapVertices: 0
    alignment: 0
    textureMode: 0
    textureScale: {{x: 1, y: 1}}
    shadowBias: 0.5
    generateLightingData: 0
  m_MaskInteraction: 0
  m_UseWorldSpace: 1
  m_Loop: 0
  m_ApplyActiveColorSpace: 1
"""
    )

    # Floor top is at local Y = -0.5; compartment local Y=0 is the floor plane.
    pb, pb_vol, _ = compartment(5000, "Port Bow Compartment", 4001, (-0.95, -0.5, 1.45), 4004)
    sb, sb_vol, _ = compartment(5200, "Starboard Bow Compartment", 4001, (0.95, -0.5, 1.45), 4004)
    ps, ps_vol, _ = compartment(5400, "Port Stern Compartment", 4001, (-0.95, -0.5, -1.45), 4004)
    ss, ss_vol, _ = compartment(5600, "Starboard Stern Compartment", 4001, (0.95, -0.5, -1.45), 4004)
    chunks.append(pb)
    chunks.append(sb)
    chunks.append(ps)
    chunks.append(ss)

    # Bootstrap last so refs exist
    chunks.append(
        f"""--- !u!114 &4008
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 4000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_BOOTSTRAP}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  vesselRigidbody: {{fileID: 4003}}
  massAdapter: {{fileID: 4006}}
  massAggregator: {{fileID: 4005}}
  portBow: {{fileID: {pb_vol}}}
  starboardBow: {{fileID: {sb_vol}}}
  portStern: {{fileID: {ps_vol}}}
  starboardStern: {{fileID: {ss_vol}}}
  dryComMarker: {{fileID: 4201}}
  floodComMarker: {{fileID: 4301}}
  combinedComMarker: {{fileID: 4401}}
  comShiftLine: {{fileID: 4502}}
  presetVolumePerCompartment: 2.4
  transferRate: 1.5
  autoDemoHoldSeconds: 4
  autoDemoResetSeconds: 2
  autoDemoEnabled: 1
"""
    )

    # Scene settings / roots
    chunks.append(
        """--- !u!104 &9000
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 10
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.2, g: 0.22, b: 0.25, a: 1}
  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}
  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 3
  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}
  m_SkyboxMaterial: {fileID: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
  m_SpotCookie: {fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 0}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &9001
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 12
  m_GIWorkflowMode: 1
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 0
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {fileID: 0}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_FinalGather: 0
    m_FinalGatherFiltering: 1
    m_FinalGatherRayCount: 256
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 2
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserTypeDirect: 1
    m_PVRDenoiserTypeIndirect: 1
    m_PVRDenoiserTypeAO: 1
    m_PVRFilterTypeDirect: 0
    m_PVRFilterTypeIndirect: 0
    m_PVRFilterTypeAO: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadiusDirect: 1
    m_PVRFilteringGaussRadiusIndirect: 5
    m_PVRFilteringGaussRadiusAO: 2
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {fileID: 0}
  m_LightingSettings: {fileID: 0}
--- !u!196 &9002
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}
"""
    )

    OUT.write_text("".join(chunks), encoding="utf-8", newline="\n")
    print(f"Wrote {OUT}")


if __name__ == "__main__":
    main()
