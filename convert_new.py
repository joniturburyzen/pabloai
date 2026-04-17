"""Convierte los 4 FBX nuevos (sin skin) a GLB"""
import bpy, os

BASE = "C:/Users/jonit/Desktop/pablo/"
FILES = [
    ("Start Walking (1).fbx", "pablo_walk.glb"),
    ("Happy Idle.fbx",        "pablo_happy.glb"),
    ("Shrugging.fbx",         "pablo_shrug.glb"),
    ("Surprised.fbx",         "pablo_surprised.glb"),
]

def clear():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()

def convert(fbx_name, glb_name):
    clear()
    fbx = BASE + "avatar_3d/" + fbx_name
    glb = BASE + "assets/" + glb_name
    print(f"\n>>> {fbx_name}")
    bpy.ops.import_scene.fbx(
        filepath=fbx,
        use_anim=True,
        automatic_bone_orientation=True,
        ignore_leaf_bones=False,
        use_image_search=False,
    )
    bpy.ops.export_scene.gltf(
        filepath=glb,
        export_format='GLB',
        use_selection=False,
        export_apply=False,
        export_animations=True,
        export_skins=True,
        export_yup=True,
    )
    size = os.path.getsize(glb) // 1024
    print(f">>> OK: {glb_name} ({size} KB)")

for fbx, glb in FILES:
    convert(fbx, glb)

print("\n=== Listo ===")
