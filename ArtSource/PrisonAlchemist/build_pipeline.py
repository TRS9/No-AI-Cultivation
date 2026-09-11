"""Run in Blender after opening PreCharacterWorkspace.blend to rebuild cleanly.
The default scene is preserved; the character gets its own studio scene.
"""
from pathlib import Path
source=Path(r'C:/Users/trs13/OneDrive/Dokumente/Cultivation Games/Unity/No AI Cultivation/ArtSource/PrisonAlchemist')
for filename in ['build_character.py','refine_weights.py','refine_silhouette.py',
                 'final_fit.py','shoulder_seam.py','coat_weights.py','animate_gameplay.py',
                 'optimize_character.py','export_character.py']:
    exec(compile((source/filename).read_text(encoding='utf-8'),str(source/filename),'exec'),globals())
