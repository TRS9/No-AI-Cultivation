import bpy
from pathlib import Path
SOURCE=Path(r'C:/Users/trs13/OneDrive/Dokumente/Cultivation Games/Unity/No AI Cultivation/ArtSource/PrisonAlchemist')
rig=bpy.data.objects['PrisonAlchemist_Rig'];rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
scene=bpy.context.scene;scene.render.filepath=str(SOURCE/'PrisonAlchemist_Preview.png')
scene.render.resolution_x=1200;scene.render.resolution_y=1400
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'PrisonAlchemist.blend'))
RESULT={'preview':scene.render.filepath}
