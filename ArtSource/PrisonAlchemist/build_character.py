"""Procedural source for the Prison Alchemist. Executed in the live Blender UI."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Quaternion

ROOT = Path(r'C:/Users/trs13/OneDrive/Dokumente/Cultivation Games/Unity/No AI Cultivation')
SOURCE = ROOT / 'ArtSource/PrisonAlchemist'
EXPORT = ROOT / 'Assets/_Project/Characters/PrisonAlchemist'
EXPORT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
if not (SOURCE/'PreCharacterWorkspace.blend').exists():
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'PreCharacterWorkspace.blend'), copy=True)

scene = bpy.data.scenes.new('Prison Alchemist | Character Studio')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
scene.render.fps = 30
scene.render.engine = 'CYCLES'
scene.cycles.samples = 32
scene.render.resolution_x = 1400
scene.render.resolution_y = 1400
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('Alchemist Studio World')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.055,0.075,0.10,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .4
collection = bpy.data.collections.new('CHARACTER | Prison Alchemist')
scene.collection.children.link(collection)
parts=[]
mats={}
def mat(name,color,metal=0,rough=.65,emission=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    if emission:
        p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emission
    mats[name]=m;return m
mat('Ink',(.035,.055,.077));mat('Jade',(.045,.27,.23));mat('DeepJade',(.027,.13,.13))
mat('Linen',(.64,.57,.40));mat('Copper',(.59,.29,.095),.65,.35)
mat('Sash',(.30,.055,.075));mat('Skin',(.57,.34,.21));mat('Hair',(.018,.025,.032))
mat('Eyes',(.83,.78,.61));mat('Qi',(.10,.72,.54),.25,.24,.35);mat('Leather',(.12,.065,.042))

def wsingle(b):return {b:1.0}
def bodyw(z):
    if z<1.12:return {'Hips':1}
    if z<1.28:
        t=(z-1.12)/.16;return {'Hips':1-t,'Spine':t}
    if z<1.43:
        t=(z-1.28)/.15;return {'Spine':1-t,'Chest':t}
    return {'Chest':1}
def limbw(t,joint,a,b,width=.065):
    q=max(0,min(1,(t-joint+width)/(2*width)))
    return {a:1-q,b:q}
def mesh(name,verts,faces,material,weights):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    obj=bpy.data.objects.new(name,data);collection.objects.link(obj);data.materials.append(mats[material])
    groups={}
    for i,weight in enumerate(weights):
        for bone,value in weight.items():
            if value<=1e-6:continue
            if bone not in groups:groups[bone]=obj.vertex_groups.new(name=bone)
            groups[bone].add([i],value,'REPLACE')
    for poly in data.polygons:poly.use_smooth=True
    parts.append(obj);return obj
def loft_z(name,rows,material,weight,n=16,cx=0,cy=0):
    # rows: z, x-radius, y-radius; support loops centered at joints.
    verts=[];ws=[];faces=[]
    for z,rx,ry in rows:
        for i in range(n):
            a=2*math.pi*i/n
            verts.append((cx+rx*math.cos(a),cy+ry*math.sin(a),z));ws.append(weight(z))
    for j in range(len(rows)-1):
        for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    faces+=[tuple(range(n-1,-1,-1)),tuple((len(rows)-1)*n+i for i in range(n))]
    return mesh(name,verts,faces,material,ws)
def loft_x(name,rows,side,material,weight,cy=0,cz=1.51,n=12):
    verts=[];ws=[];faces=[]
    for x,ry,rz in rows:
        for i in range(n):
            a=2*math.pi*i/n;verts.append((side*x,cy+ry*math.cos(a),cz+rz*math.sin(a)));ws.append(weight(x))
    for j in range(len(rows)-1):
        for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    faces += [tuple(range(n-1,-1,-1)),tuple((len(rows)-1)*n+i for i in range(n))]
    if side<0:faces=[f[::-1] for f in faces]
    return mesh(name,verts,faces,material,ws)
def ellipsoid(name,center,scale,material,bone,segments=16,rings=10):
    verts=[];faces=[]
    for j in range(rings+1):
        phi=math.pi*(.001+(j/rings)*.998)
        for i in range(segments):
            a=2*math.pi*i/segments
            verts.append((center[0]+scale[0]*math.sin(phi)*math.cos(a),center[1]+scale[1]*math.sin(phi)*math.sin(a),center[2]+scale[2]*math.cos(phi)))
    for j in range(rings):
        for i in range(segments):faces.append((j*segments+i,j*segments+(i+1)%segments,(j+1)*segments+(i+1)%segments,(j+1)*segments+i))
    return mesh(name,verts,faces,material,[wsingle(bone)]*len(verts))
def strip(name,points,width,material,weight):
    verts=[]
    for x,y,z in points:verts.extend([(x-width/2,y,z),(x+width/2,y,z)])
    faces=[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(points)-1)]
    ob=mesh(name,verts,faces,material,[weight(v[2]) for v in verts])
    return ob
def block(name,center,size,material,bone,bevel=.012):
    x,y,z=center;rx,ry,rz=[v/2 for v in size]
    vs=[(x+dx*rx,y+dy*ry,z+dz*rz) for dx,dy,dz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    ob=mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],material,[wsingle(bone)]*8)
    if bevel:
        bpy.context.view_layer.objects.active=ob;ob.select_set(True)
        mod=ob.modifiers.new('Crafted edges','BEVEL');mod.width=bevel;mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name);ob.select_set(False)
    return ob

# Human proportions independent of the placeholder appearance.
loft_z('Tailored jacket',[(1.015,.205,.12),(1.065,.205,.12),(1.13,.19,.115),(1.25,.205,.125),(1.40,.25,.142),(1.47,.265,.13),(1.52,.225,.11),(1.56,.15,.095),(1.575,.09,.078)],'Jade',bodyw,20)
loft_z('Linen under collar',[(1.53,.10,.08),(1.59,.085,.07),(1.615,.076,.064)],'Linen',bodyw)
loft_z('Neck',[(1.565,.066,.057),(1.60,.064,.058),(1.65,.069,.06)],'Skin',lambda z:{'Neck':1})
loft_z('Waist sash',[(1.01,.212,.127),(1.035,.216,.13),(1.085,.209,.126),(1.105,.207,.125)],'Sash',lambda z:{'Hips':1},20)
loft_z('Utility belt',[(1.045,.22,.137),(1.073,.22,.137)],'Leather',lambda z:{'Hips':1},20)
block('Copper buckle',(0,-.153,1.057),(.067,.027,.053),'Copper','Hips',.006)
block('Jade buckle inset',(0,-.173,1.057),(.030,.008,.028),'Qi','Hips',.003)

# Crossed lapels follow spine weighting rather than floating as rigid armor.
strip('Crossed linen lapel',[(.11,-.085,1.565),(.075,-.138,1.47),(-.055,-.153,1.33),(-.15,-.13,1.14)],.052,'Linen',bodyw)
strip('Copper lapel piping',[(.077,-.090,1.564),(.043,-.145,1.47),(-.088,-.158,1.33),(-.179,-.137,1.14)],.009,'Copper',bodyw)
strip('Inner lapel', [(-.11,-.085,1.565),(-.066,-.139,1.47),(.026,-.155,1.37)],.035,'DeepJade',bodyw)
strip('Workshop harness',[(.155,-.099,1.51),(.162,-.149,1.39),(.167,-.14,1.26),(.167,-.138,1.11)],.032,'Leather',bodyw)
for zz in (1.39,1.35,1.31):
    block('Harness clasp',(.166,-.155,zz),(.044,.013,.012),'Copper','Chest' if zz>1.38 else 'Spine',.002)

for side,label in [(1,'Left'),(-1,'Right')]:
    thigh=label+'UpperLeg';shin=label+'LowerLeg';foot=label+'Foot';upper=label+'UpperArm';lower=label+'LowerArm';hand=label+'Hand'
    legw=lambda z,a=thigh,b=shin:limbw(-z,-.54,a,b,.075)
    loft_z(label+' trouser',[(.12,.059,.061),(.25,.077,.073),(.40,.086,.082),(.49,.086,.083),(.525,.09,.089),(.55,.095,.095),(.58,.096,.098),(.66,.12,.115),(.82,.132,.13),(.98,.132,.135),(1.03,.12,.12)],'Ink',legw,16,cx=side*.115)
    # Coattails are split above the thighs and bend with their own leg.
    rows=[(.67,.04,.26),(.74,.03,.255),(.90,.022,.23),(1.02,.014,.208)]
    verts=[];weights=[]
    for z,inner,outer in rows:
        for x,y in [(inner,-.14),(outer,-.105),(outer,.11),(inner,.15)]:
            verts.append((side*x,y,z));t=max(0,min(.82,(1.02-z)/.32));weights.append({'Hips':1-t,thigh:t})
    faces=[]
    for j in range(3):
        for i in range(3):faces.append((j*4+i,j*4+i+1,(j+1)*4+i+1,(j+1)*4+i))
    panel=mesh(label+' split work coat',verts,faces,'DeepJade',weights)
    strip(label+' coat gold edge',[(side*.259,-.109,.674),(side*.252,-.112,.75),(side*.228,-.119,.90),(side*.208,-.128,1.018)],.009,'Copper',lambda z,a=thigh:{'Hips':1-min(.82,(1.02-z)/.32),a:min(.82,(1.02-z)/.32)})
    # Boots have a narrow ankle, broad toe box, and separate protective cuff.
    loft_z(label+' boot shaft',[(.08,.063,.07),(.17,.065,.068),(.29,.078,.075),(.40,.089,.084),(.425,.095,.092)],'Leather',lambda z,b=shin:{b:1},16,cx=side*.115)
    loft_z(label+' boot cuff',[(.392,.097,.09),(.42,.099,.092),(.44,.095,.09)],'Linen',lambda z,b=shin:{b:1},16,cx=side*.115)
    for z in [.21,.255,.30,.345]:
        loft_z(label+' leg wrap',[(z,.083 if z>.25 else .073,.080),(z+.018,.085 if z>.25 else .075,.081)],'Linen',lambda z,b=shin:{b:1},12,cx=side*.115)
    block(label+' boot sole',(side*.115,-.063,.026),(.15,.285,.048),'Ink',foot,.017)
    ellipsoid(label+' boot toe',(side*.115,-.076,.073),(.077,.135,.070),'Leather',foot)
    block(label+' toe cap',(side*.115,-.157,.065),(.13,.055,.07),'Copper',foot,.015)
    armw=lambda x,a=upper,b=lower:limbw(x,.515,a,b,.06)
    loft_x(label+' sleeve',[(.22,.107,.105),(.265,.111,.106),(.34,.103,.102),(.435,.092,.087),(.485,.077,.077),(.505,.075,.075),(.525,.074,.074),(.548,.073,.075),(.61,.067,.069),(.67,.062,.062)],side,'Jade',armw)
    loft_x(label+' sleeve cuff',[(.641,.070,.07),(.668,.073,.073),(.683,.070,.07)],side,'Linen',lambda x,b=lower:{b:1})
    loft_x(label+' forearm',[(.658,.054,.052),(.72,.046,.044),(.766,.037,.035),(.78,.036,.034)],side,'Skin',lambda x,b=lower:{b:1})
    loft_x(label+' wrist guard',[(.698,.057,.056),(.729,.053,.051),(.756,.047,.045)],side,'Leather',lambda x,b=lower:{b:1})
    loft_x(label+' copper cuff',[(.719,.057,.055),(.735,.055,.052)],side,'Copper',lambda x,b=lower:{b:1})
    block(label+' palm',(side*.82,0,1.51),(.093,.085,.043),'Skin',hand,.014)
    # Separated fingers with two bend zones; full humanoid finger chains below.
    for finger,fy,length in [('Index',-.029,.09),('Middle',-.008,.102),('Ring',.014,.094),('Little',.034,.075)]:
        start=.855;end=start+length
        rows=[(start,.011,.014),(start+length*.25,.011,.012),(start+length*.45,.010,.011),(start+length*.55,.010,.010),(end-.011,.009,.009),(end,.003,.006)]
        def fw(x,lab=label,f=finger,st=start,ln=length):
            if x<st+ln*.42:return {lab+f+'Proximal':1}
            if x<st+ln*.72:return {lab+f+'Intermediate':1}
            return {lab+f+'Distal':1}
        loft_x(label+finger,rows,side,'Skin',fw,cy=fy,cz=1.51,n=8)
    ellipsoid(label+' thumb',(side*.816,-.064,1.504),(.041,.019,.018),'Skin',label+'ThumbProximal',12,6)

# Angular human head, not a capsule: jaw/chin, cheek, brow and cranium loops.
head=loft_z('Face and cranium',[(1.64,.054,.054),(1.665,.082,.070),(1.705,.112,.088),(1.755,.13,.101),(1.81,.135,.104),(1.855,.126,.100),(1.897,.099,.085),(1.919,.062,.057)],'Skin',lambda z:{'Head':1},24,cy=-.003)
for side,label in [(1,'Left'),(-1,'Right')]:
    ellipsoid(label+' ear',(side*.131,.004,1.769),(.028,.02,.045),'Skin','Head',12,8)
    ellipsoid(label+' ear inset',(side*.15,-.008,1.768),(.009,.011,.024),'Sash','Head',10,6)
    # Eyes slightly inset beneath a heavier, directional brow.
    ellipsoid(label+' eye white',(side*.050,-.100,1.800),(.032,.015,.012),'Eyes','Head',16,8)
    ellipsoid(label+' iris',(side*.048,-.114,1.800),(.010,.004,.009),'Jade','Head',12,8)
    ellipsoid(label+' pupil',(side*.048,-.117,1.800),(.0045,.002,.006),'Hair','Head',10,6)
    strip(label+' eyebrow',[(side*.021,-.111,1.819),(side*.05,-.114,1.825),(side*.083,-.096,1.824)],.010,'Hair',lambda z:{'Head':1})
    loft_z(label+' sideburn',[(1.725,.009,.013),(1.82,.018,.018),(1.864,.018,.018)],'Hair',lambda z:{'Head':1},8,cx=side*.122,cy=-.03)
# Nose bridge and distinct planes of nostrils.
mesh('Nose', [(-.013,-.095,1.808),(.013,-.095,1.808),(-.018,-.103,1.755),(.018,-.103,1.755),(0,-.132,1.765),(0,-.109,1.813)],[(0,5,4,2),(5,1,3,4),(2,4,3),(0,2,3,1)],'Skin',[{'Head':1}]*6)
strip('Upper lip',[(-.028,-.090,1.721),(0,-.099,1.724),(.028,-.090,1.721)],.005,'Sash',lambda z:{'Head':1})
strip('Lower lip',[(-.023,-.091,1.716),(0,-.098,1.714),(.023,-.091,1.716)],.004,'Skin',lambda z:{'Head':1})
# Swept hair cap and tied knot, with copper hair pin.
loft_z('Swept hair cap',[(1.834,.129,.109),(1.873,.127,.106),(1.91,.107,.09),(1.937,.060,.054)],'Hair',lambda z:{'Head':1},24,cy=.012)
for i in range(7):
    x=-.105+i*.035
    verts=[(x-.025,-.081,1.902),(x+.029,-.081,1.915),(x+.032,-.098,1.86),(x+.010,-.105,1.841)]
    mesh('Swept fringe',verts,[(0,1,2,3)],'Hair',[{'Head':1}]*4)
ellipsoid('Topknot',(0,.050,1.955),(.052,.061,.045),'Hair','Head',16,8)
loft_z('Topknot clasp',[(1.939,.044,.046),(1.951,.044,.046)],'Copper',lambda z:{'Head':1},16,cy=.05)
block('Hair pin',(0,.05,1.954),(.155,.012,.012),'Copper','Head',.005)
# Alchemical field kit: asymmetry tells the craftsperson story.
block('Field satchel',(-.226,.025,1.016),(.116,.132,.155),'Leather','Hips',.016)
block('Satchel flap',(-.236,-.053,1.049),(.12,.018,.070),'Linen','Hips',.012)
block('Satchel fastener',(-.238,-.067,1.038),(.025,.011,.03),'Copper','Hips',.003)
for i in range(3):
    x=.072+i*.047
    loft_z('Qi reagent vial',[(.953,.017,.017),(.960,.019,.018),(1.002,.018,.018),(1.012,.012,.012),(1.025,.012,.012)],'Qi',lambda z:{'Hips':1},12,cx=x,cy=-.151)
    loft_z('Vial stopper',[(1.014,.016,.016),(1.031,.016,.016)],'Copper',lambda z:{'Hips':1},12,cx=x,cy=-.151)
strip('Sash tail',[(.035,-.16,1.023),(.057,-.175,.94),(.037,-.185,.845),(.073,-.19,.78)],.062,'Sash',lambda z:{'Hips':1})
block('Jade seal on chest',(-.13,-.145,1.401),(.040,.012,.059),'Copper','Chest',.006)
block('Jade seal inset',(-.13,-.156,1.401),(.025,.008,.041),'Qi','Chest',.005)

# Build humanoid skeleton in a true T pose. Bone local Y points along each segment.
arm=bpy.data.armatures.new('PrisonAlchemist_Humanoid')
rig=bpy.data.objects.new('PrisonAlchemist_Rig',arm);collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=arm.edit_bones.new(name);b.head=head;b.tail=tail
    if parent:b.parent=arm.edit_bones[parent]
    # Stable local X = world X for sagittal leg/spine rotations.
    if abs((Vector(tail)-Vector(head)).x)<.1:b.align_roll(Vector((0,1,0)))
    return b
bone('Root',(0,0,0),(0,0,.15))
bone('Hips',(0,0,1.01),(0,0,1.16),'Root')
bone('Spine',(0,0,1.16),(0,0,1.34),'Hips')
bone('Chest',(0,0,1.34),(0,0,1.54),'Spine')
bone('Neck',(0,0,1.54),(0,0,1.65),'Chest')
bone('Head',(0,0,1.65),(0,0,1.91),'Neck')
for side,label in [(1,'Left'),(-1,'Right')]:
    bone(label+'UpperLeg',(side*.115,0,1.01),(side*.115,-.012,.54),'Hips')
    bone(label+'LowerLeg',(side*.115,-.012,.54),(side*.115,0,.135),label+'UpperLeg')
    bone(label+'Foot',(side*.115,0,.135),(side*.115,-.145,.068),label+'LowerLeg')
    bone(label+'Toes',(side*.115,-.145,.068),(side*.115,-.205,.068),label+'Foot')
    bone(label+'Shoulder',(side*.055,0,1.51),(side*.235,0,1.51),'Chest')
    bone(label+'UpperArm',(side*.235,0,1.51),(side*.515,0,1.51),label+'Shoulder')
    bone(label+'LowerArm',(side*.515,0,1.51),(side*.775,0,1.51),label+'UpperArm')
    bone(label+'Hand',(side*.775,0,1.51),(side*.855,0,1.51),label+'LowerArm')
    for finger,fy,length in [('Index',-.029,.09),('Middle',-.008,.102),('Ring',.014,.094),('Little',.034,.075)]:
        points=[(.855,fy,1.51),(.855+length*.42,fy,1.51),(.855+length*.72,fy,1.51),(.855+length,fy,1.51)]
        par=label+'Hand'
        for i,suffix in enumerate(['Proximal','Intermediate','Distal']):
            name=label+finger+suffix;bone(name,(side*points[i][0],fy,1.51),(side*points[i+1][0],fy,1.51),par);par=name
    par=label+'Hand'
    for i,suffix in enumerate(['Proximal','Intermediate','Distal']):
        name=label+'Thumb'+suffix;bone(name,(side*(.798+i*.018),-.035-i*.018,1.504),(side*(.816+i*.018),-.053-i*.018,1.504),par);par=name
bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False);rig.show_in_front=True

# Join geometry to one skinned mesh, with material regions and hand-authored weights.
for ob in bpy.context.selected_objects:ob.select_set(False)
for ob in parts:ob.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();body=bpy.context.object;body.name='PrisonAlchemist_Body'
# Face orientation is repaired consistently across mirrored procedural sections.
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
body.parent=rig
mod=body.modifiers.new('Humanoid deformation','ARMATURE');mod.object=rig;mod.use_deform_preserve_volume=True
body['design']='Imprisoned chemist / jade alchemist. Tailored split work coat, reagent belt, copper tools.'
body['height_m']=2.0
rig['export_forward']='-Z';rig['export_up']='Y';rig['root_motion']='In place; translation controlled by Unity Rigidbody'

# UVs are supplied even though the first art pass uses a deliberate material palette.
bpy.context.view_layer.objects.active=body;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')

# Presentation camera, lights, and stage are not part of the FBX selection.
studio=bpy.data.collections.new('STUDIO | Not exported');scene.collection.children.link(studio)
def move_studio(ob):
    for c in list(ob.users_collection):c.objects.unlink(ob)
    studio.objects.link(ob)
def aim(ob,point):ob.rotation_euler=(Vector(point)-ob.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('Character portrait');cam=bpy.data.objects.new('Character portrait',camdata);studio.objects.link(cam)
cam.location=(3.2,-6.2,2.8);aim(cam,(0,0,1.05));camdata.type='ORTHO';camdata.ortho_scale=2.7;scene.camera=cam
for name,loc,energy,size,color in [('Warm key',(2,-4,5),450,4,(1,.82,.63)),('Cool fill',(-3,-2,2.7),250,3,(.60,.80,1)),('Jade rim',(1,3,3.5),600,3,(.57,1,.87))]:
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.shape='DISK';d.size=size;d.color=color
    ob=bpy.data.objects.new(name,d);studio.objects.link(ob);ob.location=loc;aim(ob,(0,0,1))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.008));floor=bpy.context.object;floor.name='Studio floor';move_studio(floor)
floor.data.materials.append(mat('StudioFloor',(.028,.041,.055)))
floor.hide_set(True)
for ob in studio.objects:
    if ob.type=='LIGHT' or ob.type=='CAMERA':ob.hide_set(True)
for ob in bpy.context.selected_objects:ob.select_set(False)
body.select_set(True);bpy.context.view_layer.objects.active=body
for area in bpy.context.screen.areas:
    if area.type=='CONSOLE':area.type='VIEW_3D'
    if area.type=='VIEW_3D':
        area.spaces.active.shading.type='SOLID';area.spaces.active.shading.color_type='MATERIAL'
        area.spaces.active.shading.light='STUDIO';area.spaces.active.shading.show_shadows=True
        area.spaces.active.overlay.show_floor=False
        area.spaces.active.region_3d.view_distance=3.5
        area.spaces.active.region_3d.view_location=(0,0,1.03)
        area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion()

bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'PrisonAlchemist.blend'))
RESULT={'scene':scene.name,'vertices':len(body.data.vertices),'polygons':len(body.data.polygons),'bones':len(arm.bones),'materials':len(body.data.materials),'live_file':bpy.data.filepath}
