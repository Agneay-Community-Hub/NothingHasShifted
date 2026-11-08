import bpy, bmesh, math, random, os
from mathutils import Vector, Matrix
OUT = "D:/UnityProjects/NothingHasShifted/Assets/Game/Art/Models"
os.makedirs(OUT, exist_ok=True)
random.seed(7)
bpy.ops.wm.read_factory_settings(use_empty=True)
scn = bpy.context.scene

def M(name, col, metal=0.0, rough=0.6, emit=None):
    m = bpy.data.materials.get(name)
    if m: return m
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*col, 1)
    b.inputs["Metallic"].default_value = metal
    b.inputs["Roughness"].default_value = rough
    if emit:
        b.inputs["Emission Color"].default_value = (*emit, 1); b.inputs["Emission Strength"].default_value = 2
    return m

def link(o, coll):
    coll.objects.link(o); return o

def newcoll(name):
    c = bpy.data.collections.new(name); scn.collection.children.link(c); return c

def mesh_from_bm(bm, name, mat, coll):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); me.materials.append(mat); link(o, coll); return o

def box(name, size, loc, mat, coll, bevel=0.0, seg=2, rot=(0,0,0)):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    o = mesh_from_bm(bm, name, mat, coll); o.location = loc; o.rotation_euler = rot
    if bevel > 0:
        b = o.modifiers.new("bev", 'BEVEL'); b.width = bevel; b.segments = seg; b.limit_method = 'ANGLE'
    o.modifiers.new("wn", 'WEIGHTED_NORMAL') if False else None
    return o

def cyl(name, r, depth, loc, mat, coll, verts=24, rot=(0,0,0), r2=None):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=verts, radius1=r, radius2=(r2 if r2 is not None else r), depth=depth)
    o = mesh_from_bm(bm, name, mat, coll); o.location = loc; o.rotation_euler = rot
    for p in o.data.polygons: p.use_smooth = True
    return o

def sphere(name, r, loc, mat, coll, seg=16, scale=(1,1,1)):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=max(8,seg//2), radius=r)
    o = mesh_from_bm(bm, name, mat, coll); o.location = loc; o.scale = scale
    for p in o.data.polygons: p.use_smooth = True
    return o

def tube(name, pts, radius, mat, coll, res=6):
    cu = bpy.data.curves.new(name+"_c", 'CURVE'); cu.dimensions = '3D'
    sp = cu.splines.new('POLY'); sp.points.add(len(pts)-1)
    for i,p in enumerate(pts): sp.points[i].co = (p[0],p[1],p[2],1)
    cu.bevel_depth = radius; cu.bevel_resolution = max(0,res//4); cu.use_fill_caps = True
    tmp = bpy.data.objects.new(name+"_tmp", cu); scn.collection.objects.link(tmp)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(tmp.evaluated_get(dg))
    bpy.data.objects.remove(tmp); 
    o = bpy.data.objects.new(name, me); me.materials.clear(); me.materials.append(mat); link(o, coll)
    for p in me.polygons: p.use_smooth = True
    return o

def smooth_curve(ctrl, n=24):
    # catmull-rom through control points
    out=[]
    P=[Vector(c) for c in ctrl]
    P=[P[0]]+P+[P[-1]]
    for i in range(1,len(P)-2):
        for k in range(n):
            t=k/n; p0,p1,p2,p3=P[i-1],P[i],P[i+1],P[i+2]
            out.append(0.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
    out.append(P[-2]); return out

def lathe(name, prof, mat, coll, steps=48):
    bm = bmesh.new()
    vs = [bm.verts.new((r,0,z)) for r,z in prof]
    for a,b in zip(vs[:-1],vs[1:]): bm.edges.new((a,b))
    bmesh.ops.spin(bm, geom=bm.verts[:]+bm.edges[:], angle=math.tau, steps=steps, axis=(0,0,1), cent=(0,0,0))
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    o = mesh_from_bm(bm, name, mat, coll)
    for p in o.data.polygons: p.use_smooth = True
    return o

def export(coll, fname):
    bpy.ops.object.select_all(action='DESELECT')
    objs=list(coll.all_objects)
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.convert(target='MESH')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join()
    j=bpy.context.view_layer.objects.active; j.name=fname
    bpy.ops.object.select_all(action='DESELECT'); j.select_set(True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT, fname+".glb"), export_format='GLB', use_selection=True, export_apply=True, export_yup=True)

# ---------------- materials
m_white = M("ChappalTop", (0.85,0.85,0.82), rough=0.8)
m_blue = M("ChappalBlue", (0.05,0.18,0.55), rough=0.7)
m_red = M("BucketRed", (0.6,0.04,0.03), rough=0.35)
m_mugblue = M("MugBlue", (0.03,0.2,0.6), rough=0.35)
m_steel = M("Steel", (0.75,0.75,0.75), metal=1.0, rough=0.3)
m_darksteel = M("DarkSteel", (0.2,0.2,0.2), metal=0.8, rough=0.5)
m_plate = M("CoolerPlate", (0.05,0.2,0.55), rough=0.4)
m_willow = M("Willow", (0.8,0.62,0.38), rough=0.6)
m_grip = M("Grip", (0.55,0.05,0.05), rough=0.8)
m_skin = M("GhostSkin", (0.55,0.58,0.6), rough=0.7)
m_saree = M("Saree", (0.78,0.76,0.7), rough=0.9)
m_hair = M("Hair", (0.01,0.01,0.01), rough=0.5)
m_cloth = M("Cloth", (0.7,0.7,0.7), rough=0.9)
m_doorwood = M("DoorPaint", (0.25,0.13,0.07), rough=0.6)
m_frame = M("DoorFrame", (0.2,0.11,0.06), rough=0.6)
m_brass = M("Brass", (0.85,0.62,0.25), metal=1.0, rough=0.35)
m_iron = M("Iron", (0.12,0.12,0.12), metal=0.9, rough=0.45)
m_grille = M("GrillePaint", (0.08,0.28,0.2), metal=0.6, rough=0.5)
m_marigold = M("Marigold", (0.95,0.45,0.02), rough=0.8)
m_marigold2 = M("MarigoldY", (0.98,0.72,0.05), rough=0.8)
m_leaf = M("MangoLeaf", (0.1,0.32,0.06), rough=0.6)
m_string = M("Thread", (0.8,0.75,0.6), rough=0.9)

# ---------------- chappal
def chappal(coll, name, mirror=1, off=(0,0,0), rotz=0):
    L=0.26; pts=[]
    for i in range(48):
        th = i/48*math.tau
        x = 0.05*math.cos(th); y = (L/2)*math.sin(th)
        t = (y/L)+0.5
        x *= 0.78 + 0.32*t
        if x*mirror < 0 and abs(y) < L*0.18: x *= 0.82
        pts.append((x*mirror, y))
    parts=[]
    for (z0,z1,mat,nm) in [(0,0.009,m_blue,"sole"),(0.009,0.017,m_white,"top")]:
        bm=bmesh.new()
        vb=[bm.verts.new((p[0],p[1],z0)) for p in pts]
        f=bm.faces.new(vb)
        ext=bmesh.ops.extrude_face_region(bm, geom=[f])
        bmesh.ops.translate(bm, vec=(0,0,z1-z0), verts=[v for v in ext['geom'] if isinstance(v,bmesh.types.BMVert)])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        o=mesh_from_bm(bm, name+"_"+nm, mat, coll)
        b=o.modifiers.new("bev",'BEVEL'); b.width=0.003; b.segments=2
        parts.append(o)
    toe=(0.004*mirror, 0.075, 0.017)
    for side in (1,-1):
        sx = side*0.043*(1 if side==mirror else 0.95)
        c=smooth_curve([toe,(sx*0.45,0.055,0.035),(sx*0.85,0.0,0.03),(sx,-0.02,0.017)],8)
        parts.append(tube(name+"_strap%d"%side, c, 0.006, m_blue, coll))
    for o in parts:
        o.location = Vector(off); o.rotation_euler=(0,0,rotz)
    return parts

c=newcoll("chappal_pair")
chappal(c,"L",1,(-0.06,0,0),0.06); chappal(c,"R",-1,(0.06,0.01,0),-0.05)
export(c,"chappal_pair")

# ---------------- bucket + mug
c=newcoll("bucket_mug")
prof=[(0,0.004),(0.12,0.0),(0.125,0.008),(0.155,0.27),(0.166,0.28),(0.168,0.292),(0.158,0.292),(0.147,0.272),(0.117,0.014),(0,0.014)]
lathe("bucket", prof, m_red, c)
h=[]
for i in range(25):
    a=math.pi*i/24; h.append((0.166*math.cos(a), 0.0, 0.29+0.13*math.sin(a)))
tube("bucket_handle", h, 0.0035, m_darksteel, c)
mprof=[(0,0.0),(0.05,0),(0.055,0.006),(0.064,0.11),(0.067,0.115),(0.06,0.115),(0.058,0.11),(0.05,0.006),(0,0.006)]
mug=lathe("mug", mprof, m_mugblue, c, 32); mug.location=(0.27,0.05,0)
hm=smooth_curve([(0.27+0.064,0.05,0.095),(0.27+0.1,0.05,0.085),(0.27+0.105,0.05,0.04),(0.27+0.06,0.05,0.03)],8)
tube("mug_handle", hm, 0.007, m_mugblue, c)
export(c,"bucket_mug")

# ---------------- water cooler
c=newcoll("water_cooler")
box("cooler_body",(0.62,0.48,1.05),(0,0,0.25+0.525),m_steel,c,0.02,3)
box("cooler_front_inset",(0.5,0.02,0.32),(0,-0.245,0.72),m_darksteel,c,0.01,2)
box("cooler_plate",(0.3,0.01,0.08),(0,-0.245,1.12),m_plate,c,0.005,2)
for i,x in enumerate((-0.14,0,0.14)):
    cyl("tap_body%d"%i,0.018,0.09,(x,-0.29,0.78),m_steel,c,16,(math.pi/2,0,0))
    cyl("tap_spout%d"%i,0.009,0.06,(x,-0.325,0.75),m_steel,c,12)
    box("tap_lever%d"%i,(0.012,0.012,0.06),(x,-0.31,0.82),m_darksteel,c,0.004)
box("tray",(0.5,0.12,0.03),(0,-0.3,0.6),m_steel,c,0.008)
for i in range(9):
    box("tray_bar%d"%i,(0.006,0.11,0.006),(-0.2+i*0.05,-0.3,0.618),m_darksteel,c)
for sx in (-0.27,0.27):
    for sy in (-0.2,0.2):
        cyl("leg",0.02,0.25,(sx,sy,0.125),m_darksteel,c,12)
box("vent",(0.5,0.01,0.25),(0,0.245,0.45),m_darksteel,c,0.005)
export(c,"water_cooler")

# ---------------- cricket bat
c=newcoll("cricket_bat")
blade=box("bat_blade",(0.108,0.045,0.56),(0,0,0.28),m_willow,c,0.012,3)
box("bat_spine",(0.05,0.03,0.4),(0,0.03,0.22),m_willow,c,0.014,3)
cyl("bat_handle",0.017,0.3,(0,0,0.56+0.15),m_grip,c,16)
sphere("bat_knob",0.019,(0,0,0.86),m_grip,c,12,(1,1,0.6))
export(c,"cricket_bat")

# ---------------- ghost woman
c=newcoll("ghost")
bm=bmesh.new()
V=lambda p: bm.verts.new(p)
pel=V((0,0,1.0)); ch=V((0,0,1.42)); nk=V((0,0,1.62))
bm.edges.new((pel,ch)); bm.edges.new((ch,nk))
for s in (1,-1):
    sh=V((0.19*s,0,1.52)); el=V((0.23*s,-0.03,1.2)); wr=V((0.24*s,-0.06,0.9)); fi=V((0.245*s,-0.07,0.78))
    bm.edges.new((ch,sh)); bm.edges.new((sh,el)); bm.edges.new((el,wr)); bm.edges.new((wr,fi))
    kn=V((0.1*s,0,0.5)); ft=V((0.11*s,-0.05,0.04)); bm.edges.new((pel,kn)); bm.edges.new((kn,ft))
body=mesh_from_bm(bm,"ghost_body",m_skin,c)
sk=body.modifiers.new("skin",'SKIN')
rad={0:0.13,1:0.15,2:0.05}
for i,v in enumerate(body.data.skin_vertices[0].data):
    z=body.data.vertices[i].co.z; x=abs(body.data.vertices[i].co.x)
    r=0.035 if x>0.15 else (0.05 if z<0.9 and x>0.05 else 0.12)
    if i==2: r=0.045
    v.radius=(r,r)
body.data.skin_vertices[0].data[0].use_root=True
sd=body.modifiers.new("sub",'SUBSURF'); sd.levels=2; sd.render_levels=2
head=sphere("ghost_head",0.105,(0,-0.01,1.74),m_skin,c,24,(0.88,0.95,1.18))
# saree / long dress with folds
prof=[]
for i in range(21):
    t=i/20; z=1.48-t*1.46; r=0.15+0.2*(t**1.4)
    prof.append((r,z))
sar=lathe("ghost_saree",[(0.0,1.5)]+prof,m_saree,c,64)
tex=bpy.data.textures.new("folds",'CLOUDS'); tex.noise_scale=0.12
dm=sar.modifiers.new("disp",'DISPLACE'); dm.texture=tex; dm.strength=0.05
ss=sar.modifiers.new("sub",'SUBSURF'); ss.levels=1
# veil over head and shoulders
veil=sphere("ghost_veil",0.135,(0,0.02,1.72),m_saree,c,24,(1.0,1.0,1.35))
vbm=bmesh.new(); vbm.from_mesh(veil.data)
for v in vbm.verts:
    if v.co.y < -0.06 and v.co.z > -0.08: v.co.y = -0.06 + (v.co.y+0.06)*0.2  # open at face
    if v.co.z < 0: v.co.z *= 2.6; v.co.x *= 1.0 + (-v.co.z)*1.2
vbm.to_mesh(veil.data); vbm.free()
# hair strands falling over face
for i in range(240):
    a=random.uniform(-2.2,2.2) + math.pi*1.5  # mostly front-ish
    if random.random()<0.35: a=random.uniform(0,math.tau)
    rx=0.1*math.cos(a); ry=0.1*math.sin(a)
    start=(rx*0.9, ry*0.9-0.01, 1.83+random.uniform(-0.03,0.02))
    length=random.uniform(0.55,0.85)
    pts=[]
    for k in range(9):
        t=k/8
        out=0.105+0.03*t
        px=math.cos(a)*out*(1-0.3*t)+random.uniform(-0.01,0.01)
        py=math.sin(a)*out-0.04*t*(1 if math.sin(a)<0 else 0)+random.uniform(-0.01,0.01)
        pz=start[2]-0.08*min(1,t*4)-length*t
        pts.append((px,py-0.01,pz))
    tube("hair%d"%i,pts,0.0035,m_hair,c,0)
export(c,"ghost")

# ---------------- hanging cloth (folded over a line)
c=newcoll("cloth_hanging")
def cloth(name, W, L, off):
    bm=bmesh.new(); nx,ny=12,30
    grid=[[None]*(ny+1) for _ in range(nx+1)]
    for i in range(nx+1):
        for j in range(ny+1):
            x=-W/2+W*i/nx; s=-L/2+L*j/ny
            ang=max(-1,min(1,s/0.03))*math.pi/2
            y=0.012*math.sin(ang)
            z=-(abs(s)-0.03) if abs(s)>0.03 else 0.012*(math.cos(ang)-1)
            y+=0.012*math.sin(x*14+s*6)*min(1,abs(s)*4)
            grid[i][j]=bm.verts.new((x+off,y,z))
    for i in range(nx):
        for j in range(ny):
            bm.faces.new((grid[i][j],grid[i+1][j],grid[i+1][j+1],grid[i][j+1]))
    uv=bm.loops.layers.uv.new()
    for f in bm.faces:
        for lp in f.loops:
            lp[uv].uv=((lp.vert.co.x-off)/W+0.5, (lp.vert.co.z+L/2)/(L))
    o=mesh_from_bm(bm,name,m_cloth,c)
    for p in o.data.polygons: p.use_smooth=True
    so=o.modifiers.new("solid",'SOLIDIFY'); so.thickness=0.004
    return o
cloth("cloth",0.55,1.1,0)
export(c,"cloth_hanging")

# ---------------- door frame + door leaf + hardware
c=newcoll("door_frame")
W,H=1.0,2.15; t=0.07; d=0.16
box("jamb_l",(t,d,H),(-W/2-t/2,0,H/2),m_frame,c,0.008)
box("jamb_r",(t,d,H),(W/2+t/2,0,H/2),m_frame,c,0.008)
box("head",(W+2*t,d,t),(0,0,H+t/2),m_frame,c,0.008)
box("stop_l",(0.02,0.04,H),(-W/2+0.01,0.03,H/2),m_frame,c,0.004)
box("stop_r",(0.02,0.04,H),(W/2-0.01,0.03,H/2),m_frame,c,0.004)
box("stop_h",(W,0.04,0.02),(0,0.03,H-0.01),m_frame,c,0.004)
export(c,"door_frame")

c=newcoll("door_leaf")
DW,DH,DT=0.98,2.13,0.045
# leaf origin at hinge (left edge, x=0), extending +x
def L(x): return x+DW/2
st=0.12
box("stile_l",(st,DT,DH),(L(-DW/2+st/2),0,DH/2),m_doorwood,c,0.006)
box("stile_r",(st,DT,DH),(L(DW/2-st/2),0,DH/2),m_doorwood,c,0.006)
for nm,z,h in (("rail_b",0.12,0.24),("rail_m",1.0,0.16),("rail_m2",1.55,0.1),("rail_t",DH-0.07,0.14)):
    box(nm,(DW-2*st,DT,h),(L(0),0,z),m_doorwood,c,0.006)
# panels
for nm,z0,z1 in (("p1",0.24,0.92),("p2",1.08,1.5),("p3",1.6,DH-0.14)):
    for side in (-1,1):
        x=L(side*(DW-2*st)/4 if False else 0)
    box(nm,(DW-2*st,DT*0.5,z1-z0),(L(0),0,(z0+z1)/2),m_doorwood,c,0.03,4)
    box(nm+"_mid",(0.04,DT*0.8,z1-z0),(L(0),0,(z0+z1)/2),m_doorwood,c,0.008)
# handle
hp=smooth_curve([(L(DW/2-0.09),-DT/2,1.02),(L(DW/2-0.09),-DT/2-0.05,1.05),(L(DW/2-0.09),-DT/2-0.05,1.15),(L(DW/2-0.09),-DT/2,1.18)],8)
tube("handle",hp,0.009,m_brass,c)
box("tower_bolt_base",(0.14,0.012,0.035),(L(DW/2-0.12),-DT/2-0.006,1.3),m_brass,c,0.003)
cyl("tower_bolt_rod",0.007,0.17,(L(DW/2-0.1),-DT/2-0.018,1.3),m_brass,c,12,(0,math.pi/2,0))
box("hasp",(0.16,0.006,0.04),(L(DW/2-0.06),-DT/2-0.004,1.1),m_iron,c,0.002)
export(c,"door_leaf")

c=newcoll("padlock")
box("lock_body",(0.055,0.025,0.065),(0,0,-0.045),m_brass,c,0.008,3)
sh=[]
for i in range(17):
    a=math.pi*i/16; sh.append((0.018*math.cos(a),0,0.018*math.sin(a)))
sh=[(sh[0][0],0,-0.02)]+sh+[(sh[-1][0],0,-0.02)]
tube("lock_shackle",sh,0.004,m_steel,c)
export(c,"padlock")

# ---------------- grille panel
c=newcoll("grille_panel")
GW,GH=1.7,1.4
box("g_top",(GW,0.03,0.03),(0,0,GH),m_grille,c,0.004)
box("g_bot",(GW,0.03,0.03),(0,0,0),m_grille,c,0.004)
box("g_l",(0.03,0.03,GH),(-GW/2,0,GH/2),m_grille,c,0.004)
box("g_r",(0.03,0.03,GH),(GW/2,0,GH/2),m_grille,c,0.004)
box("g_mid",(GW,0.02,0.02),(0,0,GH*0.62),m_grille,c,0.003)
n=16
for i in range(1,n):
    x=-GW/2+GW*i/n
    cyl("g_bar%d"%i,0.007,GH,(x,0,GH/2),m_grille,c,10)
for i in range(n):
    x=-GW/2+GW*(i+0.5)/n
    bmt=bmesh.new(); 
    # diamond/circle motif using a ring tube
    ring=[(x+0.045*math.cos(a),0,GH*0.62+0.045*math.sin(a)) for a in [k/16*math.tau for k in range(17)]]
    tube("g_ring%d"%i,ring,0.0045,m_grille,c)
export(c,"grille_panel")

# ---------------- marigold toran
c=newcoll("marigold_toran")
fl=[]
N=30
for i in range(N+1):
    t=i/N; x=-0.55+1.1*t; z=-0.16*(1-(2*t-1)**2)
    o=sphere("f%d"%i,0.022,(x,0,z),m_marigold if i%2 else m_marigold2,c,10)
    tx=bpy.data.textures.new("bump%d"%i,'STUCCI'); tx.noise_scale=0.02
    dm=o.modifiers.new("d",'DISPLACE'); dm.texture=tx; dm.strength=0.012
    if i%5==0 and 0<i<N:
        lf=sphere("leaf%d"%i,0.05,(x,-0.005,z-0.06),m_leaf,c,10,(0.35,0.08,1.0))
    if i in (5,15,25):
        for k in range(1,7):
            sphere("s%d_%d"%(i,k),0.02,(x,0,z-0.04*k),m_marigold2 if k%2 else m_marigold,c,10)
str_pts=[(-0.55+1.1*i/40,0,-0.16*(1-(2*i/40-1)**2)) for i in range(41)]
tube("thread",str_pts,0.002,m_string,c)
export(c,"marigold_toran")
print("DONE")
