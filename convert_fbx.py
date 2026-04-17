"""
Blender script: converts Pablo's Mixamo FBX files to GLB
Run: blender --background --python convert_fbx.py
"""
import bpy
import os

BASE = "C:/Users/jonit/Desktop/pablo/"

def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for col in list(bpy.data.collections):
        bpy.data.collections.remove(col)

def convert(fbx_path, glb_path):
    clear_scene()
    print(f"\n>>> Importing: {fbx_path}")
    bpy.ops.import_scene.fbx(
        filepath=fbx_path,
        use_anim=True,
        use_custom_normals=True,
        ignore_leaf_bones=False,
        force_connect_children=False,
        automatic_bone_orientation=True,
        use_image_search=False,
    )
    print(f">>> Objects in scene: {[o.name for o in bpy.data.objects]}")
    print(f">>> Exporting to: {glb_path}")
    bpy.ops.export_scene.gltf(
        filepath=glb_path,
        export_format='GLB',
        use_selection=False,
        export_apply=False,
        export_animations=True,
        export_skins=True,
        export_morph=True,
        export_yup=True,
    )
    print(f">>> Done: {os.path.basename(glb_path)}")

convert(
    BASE + "avatar_3d/Breathing Idle (1).fbx",
    BASE + "assets/pablo_idle.glb"
)

convert(
    BASE + "avatar_3d/T-Pose (1).fbx",
    BASE + "assets/pablo_tpose.glb"
)

print("\n=== All conversions complete ===")
