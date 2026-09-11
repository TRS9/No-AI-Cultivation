"""Contact-driven locomotion and gameplay actions in the live Blender session."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Quaternion
SOURCE=Path(r'C:/Users/trs13/OneDrive/Dokumente/Cultivation Games/Unity/No AI Cultivation/ArtSource/PrisonAlchemist')
# Reuse the anatomical-axis helpers, without regenerating the old actions.
exec(compile((SOURCE/'animate_character.py').read_text().split('rig.animation_data_create()')[0], 'pose_helpers', 'exec'))
base_pose=pose

def leg_target(label,y,z):
    hipz=1.01+(rig.pose.bones['Hips'].bone.matrix_local.to_3x3()@rig.pose.bones['Hips'].location).z
    a=math.hypot(.47,.012);b=math.hypot(.405,.012)
    down=hipz-z;d=min(a+b-.002,max(abs(a-b)+.002,math.hypot(y,down)))
    phi=math.atan2(y,down)
    alpha=math.acos(max(-1,min(1,(a*a+d*d-b*b)/(2*a*d))))
    knee=math.pi-math.acos(max(-1,min(1,(a*a+b*b-d*d)/(2*a*b))))
    upper=math.degrees(phi-alpha)-math.degrees(math.atan2(-.012,.47))
    lower=math.degrees(knee)-(math.degrees(math.atan2(.012,.405))-math.degrees(math.atan2(-.012,.47)))
    turn(label+'UpperLeg',(1,0,0),upper)
    turn(label+'LowerLeg',(1,0,0),lower)
    turn(label+'Foot',(1,0,0),-upper-lower)

def gameplay_pose(name,t):
    reset_pose();phase=t*2*math.pi;s=math.sin(phase)
    if name=='Idle':base_pose('Idle',phase);hip_offset(0)
    elif name in ['Walk','Run']:
        run=name=='Run';stance=.4 if run else .6;span=.94 if run else .76
        hip_offset(-.14 if run else -.09)
        relaxed_arms(0,62 if run else 18)
        turn('Chest',(1,0,0),-7 if run else -1.5)
        turn('Chest',(0,0,1),3*s)
        for side,label in [(1,'Left'),(-1,'Right')]:
            p=(t+(0 if side>0 else .5))%1
            if p<stance:
                y=-span/2+span*p/stance;z=.135
            else:
                u=(p-stance)/(1-stance);e=u*u*(3-2*u)
                y=span/2-span*e;z=.135+(.24 if run else .10)*math.sin(math.pi*u)
            leg_target(label,y,z)
            turn(label+'UpperArm',(0,0,1),side*(33 if run else 22)*math.sin(2*math.pi*p))
    elif name in ['Jump','Fall','Land','Dodge']:
        if name=='Jump':bend=10+14*math.sin(t*math.pi/2)
        elif name=='Fall':bend=14+2*s
        elif name=='Land':bend=26*math.sin(math.pi*t)
        else:bend=35*math.sin(math.pi*t)
        relaxed_arms(-12 if name!='Land' else 0,38 if name!='Land' else 18)
        for label in ['Left','Right']:
            turn(label+'UpperLeg',(1,0,0),-bend*.6)
            turn(label+'LowerLeg',(1,0,0),bend)
            turn(label+'Foot',(1,0,0),-bend*.4)
        turn('Chest',(1,0,0),-bend*.23)
        if name in ['Land','Dodge']:hip_offset(-.06*math.sin(math.pi*t))
    elif name in ['Meditate','Craft']:
        relaxed_arms(0,85)
        turn('Chest',(1,0,0),1.1*s)
        for side,label in [(1,'Left'),(-1,'Right')]:
            turn(label+'UpperArm',(0,0,1),-side*(13+(5*side*s if name=='Craft' else 0)))
            turn(label+'Hand',(1,0,0),side*(55 if name=='Meditate' else 15))
            if name=='Craft':turn(label+'LowerArm',(0,0,1),side*15*s)
    elif name=='Attack':
        relaxed_arms(0,40)
        punch=math.sin(math.pi*min(1,t/.45)) if t<.45 else 0
        turn('Chest',(0,0,1),-12+30*punch)
        turn('RightUpperArm',(0,0,1),65*punch)
        turn('RightLowerArm',(0,0,1),-35*punch)
        turn('RightHand',(1,0,0),70)
    elif name=='Hurt':
        relaxed_arms(0,18);turn('Chest',(1,0,0),15*math.sin(math.pi*t));turn('Head',(1,0,0),8*math.sin(math.pi*t))
    elif name=='Death':
        relaxed_arms(0,15)
        ease=t*t*(3-2*t)
        turn('Hips',(1,0,0),82*ease)
        turn('Chest',(1,0,0),15*ease)
        turn('Head',(1,0,0),12*ease)
    else:raise ValueError(name)

rig.animation_data_create()
definitions={'Idle':(91,True),'Walk':(25,True),'Run':(21,True),'Jump':(10,False),
             'Fall':(31,True),'Land':(10,False),'Meditate':(91,True),'Craft':(31,True),
             'Attack':(13,False),'Dodge':(11,False),'Hurt':(10,False),'Death':(31,False)}
for name in definitions:
    old=bpy.data.actions.get(name)
    if old and old.get('prison_alchemist'):bpy.data.actions.remove(old)
clips={}
for name,(end,loop) in definitions.items():
    action=bpy.data.actions.new(name);action['prison_alchemist']=True;action['loop']=loop;action.use_fake_user=True
    rig.animation_data.action=action
    for frame in range(1,end+1):
        scene.frame_set(frame);gameplay_pose(name,(frame-1)/(end-1))
        if name in ['Land','Death','Dodge']:
            bpy.context.view_layer.update();ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get())
            floor=min((ev.matrix_world@v.co).z for v in ev.data.vertices)
            current=(rig.pose.bones['Hips'].bone.matrix_local.to_3x3()@rig.pose.bones['Hips'].location).z
            hip_offset(current+.002-floor)
        for pb in rig.pose.bones:
            pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name)
            pb.keyframe_insert(data_path='location',frame=frame,group=pb.name)
    clips[name]={'start':1,'end':end,'fps':30,'loop':loop}
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_start=1;scene.frame_end=91;scene.frame_set(1)
(SOURCE/'clips.json').write_text(json.dumps(clips,indent=2))
RESULT={'clips':clips,'walk_authored_speed':.76/(.6*.8),'run_authored_speed':.94/(.4*(20/30))}
