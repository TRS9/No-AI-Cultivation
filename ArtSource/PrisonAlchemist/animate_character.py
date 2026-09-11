import bpy, math, json
from mathutils import Vector, Quaternion
from pathlib import Path
ROOT=Path(r'C:/Users/trs13/OneDrive/Dokumente/Cultivation Games/Unity/No AI Cultivation')
SOURCE=ROOT/'ArtSource/PrisonAlchemist'
rig=bpy.data.objects['PrisonAlchemist_Rig'];body=bpy.data.objects['PrisonAlchemist_Body'];scene=bpy.context.scene
def reset_pose():
    for pb in rig.pose.bones:
        pb.rotation_mode='QUATERNION';pb.rotation_quaternion=(1,0,0,0);pb.location=(0,0,0);pb.scale=(1,1,1)
def turn(name,axis,degrees):
    pb=rig.pose.bones[name]
    local=pb.bone.matrix_local.to_3x3().inverted() @ Vector(axis)
    pb.rotation_quaternion=pb.rotation_quaternion @ Quaternion(local,math.radians(degrees))
def relaxed_arms(swing=0,elbow=12):
    for side,label in [(1,'Left'),(-1,'Right')]:
        turn(label+'UpperArm',(0,1,0),side*77)
        turn(label+'UpperArm',(0,0,1),side*swing)
        turn(label+'LowerArm',(0,0,1),-side*elbow)
        for finger in ['Index','Middle','Ring','Little']:
            for suffix,deg in [('Proximal',12),('Intermediate',16),('Distal',8)]:
                turn(label+finger+suffix,(0,0,1),-side*deg)
def hip_offset(height):
    pb=rig.pose.bones['Hips']
    pb.location=pb.bone.matrix_local.to_3x3().inverted()@Vector((0,0,height))
def pose(kind,phase):
    reset_pose();s=math.sin(phase);c=math.cos(phase)
    if kind=='Idle':
        relaxed_arms(1.4*s,12)
        turn('Chest',(1,0,0),.9*s);turn('Head',(0,0,1),1.1*math.sin(phase+.7))
        hip_offset(.0025*s)
    elif kind in ['Walk','Run']:
        run=kind=='Run';amp=38 if run else 24
        relaxed_arms(0,60 if run else 18)
        turn('Chest',(1,0,0),-8 if run else -2)
        turn('Chest',(0,0,1),3*s)
        # Vertical movement only: no accumulated horizontal root translation.
        hip_offset(0)
        for side,label in [(1,'Left'),(-1,'Right')]:
            p=phase+(math.pi if side<0 else 0);swing=math.sin(p)
            turn(label+'UpperLeg',(1,0,0),-amp*swing)
            knee=7+(55 if run else 32)*max(0,-math.sin(p))
            turn(label+'LowerLeg',(1,0,0),knee)
            turn(label+'Foot',(1,0,0),amp*swing-knee*.8)
            turn(label+'UpperArm',(0,0,1),side*(30 if run else 19)*swing)
    elif kind=='Jump':
        # One-shot compression -> tucked flight -> controlled landing.
        t=phase/(2*math.pi)
        keys=[(0,0),(.15,1),(.28,.05),(.48,.85),(.70,.65),(.88,1),(1,0)]
        bend=0
        for (a,x),(b,y) in zip(keys,keys[1:]):
            if a<=t<=b:
                u=(t-a)/(b-a);u=u*u*(3-2*u);bend=x+(y-x)*u;break
        relaxed_arms(-14 if .24<t<.75 else 4,25+25*bend)
        for label in ['Left','Right']:
            turn(label+'UpperLeg',(1,0,0),-32*bend)
            turn(label+'LowerLeg',(1,0,0),60*bend)
            turn(label+'Foot',(1,0,0),-25*bend)
        turn('Chest',(1,0,0),-9*bend)
        # Unity owns the jump trajectory; only a small anticipatory squat.
        hip_offset(-.075*bend if t<.23 or t>.80 else -.008*bend)

rig.animation_data_create()
for action in list(bpy.data.actions):
    if action.name in ['Idle','Walk','Run','Jump'] and action.get('prison_alchemist'):
        bpy.data.actions.remove(action)
clips={}
for name,end in [('Idle',91),('Walk',31),('Run',23),('Jump',31)]:
    action=bpy.data.actions.new(name);action['prison_alchemist']=True;action.use_fake_user=True
    rig.animation_data.action=action
    for frame in range(1,end+1):
        scene.frame_set(frame);phase=2*math.pi*(frame-1)/(end-1);pose(name,phase)
        if name in ['Walk','Run']:
            bpy.context.view_layer.update()
            ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get())
            floor_z=min((ev.matrix_world@v.co).z for v in ev.data.vertices)
            clearance=.002+(.022*math.sin(2*phase)**2 if name=='Run' else 0)
            hip_offset(clearance-floor_z)
        for pb in rig.pose.bones:
            pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name)
            pb.keyframe_insert(data_path='location',frame=frame,group=pb.name)
    action['loop']=name!='Jump';clips[name]={'start':1,'end':end,'fps':30,'loop':name!='Jump'}
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_start=1;scene.frame_end=91;scene.frame_set(1)
rig.show_in_front=False;rig.hide_set(True)
# A clean front view makes silhouette and joint issues visible during QA.
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_rotation=Quaternion((1,0,0),math.pi/2)
        area.spaces.active.region_3d.view_distance=3.1
        area.spaces.active.region_3d.view_location=(0,0,1.02)
        area.spaces.active.overlay.show_extras=False
(SOURCE/'clips.json').write_text(json.dumps(clips,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'PrisonAlchemist.blend'))
RESULT={'clips':clips,'unweighted_vertices':sum(1 for v in body.data.vertices if not v.groups),'max_influences':max(len(v.groups) for v in body.data.vertices)}
