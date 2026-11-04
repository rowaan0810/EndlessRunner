"""Original Morioh-inspired geometry/texture source. No third-party models.

Run from the project root with Python 3 + Pillow (authoring only).
Writes only Assets/ActiveMotion3D/Art/Environment/Morioh/DesignKit/Source.
Unity's Tools > Morioh > Design Kit commands assemble these into editable prefabs.
"""
from pathlib import Path
import math, random, uuid, json, sys
from collections import defaultdict

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Temp/MoriohAuthoringDeps'))
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops

OUT = ROOT / 'Assets/ActiveMotion3D/Art/Environment/Morioh/DesignKit/Source'
MODEL = OUT / 'Models'
TEX = OUT / 'Textures'
MODEL.mkdir(parents=True, exist_ok=True)
TEX.mkdir(parents=True, exist_ok=True)
PALETTE = {
    'Ink': (49, 39, 64), 'Cream': (245, 226, 186), 'Mint': (159, 207, 180),
    'Peach': (236, 170, 161), 'Lilac': (183, 163, 210), 'Roof': (93, 81, 130),
    'Glass': (84, 143, 155), 'Wood': (98, 66, 82), 'Gold': (232, 191, 76),
    'Leaf': (141, 78, 173), 'Blossom': (201, 132, 205), 'Road': (75, 73, 88),
    'Paving': (191, 182, 194), 'White': (247, 242, 216), 'Red': (193, 73, 111),
    'FarHill': (145, 137, 168), 'NearHill': (113, 111, 145), 'Town': (107, 99, 136),
    'Sign': (255, 255, 255), 'Metal': (97, 108, 125), 'DistantGround': (168, 150, 181),
}
SIGNS = [
    ('杜王町', 'MORIOH / ROUTE 04'), ('虹マート', 'NIJI MART'),
    ('すみれ喫茶', 'SUMIRE CAFE'), ('星のパン', 'HOSHI BAKERY'),
    ('杜王商店', 'MORIOH GENERAL STORE'), ('夕凪ラジオ', 'YUNAGI RADIO'),
    ('杜王駅', 'MORIOH STATION'), ('花の小道', 'FLOWER WALK'),
    ('とびこえ', 'JUMP'), ('くぐって', 'DUCK'), ('よけて', 'CHANGE LANE'),
    ('ポーズ', 'MATCH THE POSE'), ('04', 'MORIOH'), ('本と音', 'BOOKS & RECORDS'),
    ('ゆうびん', 'POST'), ('ひだり / みぎ', 'LEFT / RIGHT'),
]

class Mesh:
    def __init__(self, name):
        self.name, self.parts = name, defaultdict(list)

    def face(self, points, mat, uv=None):
        if uv is None: uv = [(0, 0), (1, 0), (1, 1), (0, 1)][:len(points)]
        self.parts[mat].append((points, uv))

    def box(self, p, s, mat, yaw=0):
        x, y, z = [n / 2 for n in s]
        v = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
             (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
        c, sn = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        v = [(p[0]+a*c+b*sn, p[1]+h, p[2]-a*sn+b*c) for a,h,b in v]
        for ids in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)]:
            self.face([v[i] for i in ids], mat)

    def beam(self, a, b, radius, mat, sides=6):
        d = [b[i]-a[i] for i in range(3)]
        n = math.sqrt(sum(k*k for k in d))
        if n < 1e-6: return
        d = [k/n for k in d]
        ref = (0,0,1) if abs(d[2]) < .9 else (0,1,0)
        u = (d[1]*ref[2]-d[2]*ref[1], d[2]*ref[0]-d[0]*ref[2], d[0]*ref[1]-d[1]*ref[0])
        un = math.sqrt(sum(k*k for k in u)); u = [k/un for k in u]
        v = (d[1]*u[2]-d[2]*u[1], d[2]*u[0]-d[0]*u[2], d[0]*u[1]-d[1]*u[0])
        ring = lambda p: [tuple(p[j] + radius*(u[j]*math.cos(i*math.tau/sides)+v[j]*math.sin(i*math.tau/sides)) for j in range(3)) for i in range(sides)]
        ra, rb = ring(a), ring(b)
        for i in range(sides):
            k=(i+1)%sides
            self.face([ra[i],ra[k],rb[k],rb[i]], mat)
        self.face(list(reversed(ra)), mat, [(0,0)]*sides)
        self.face(rb, mat, [(0,0)]*sides)

    def ellipsoid(self, p, s, mat, seed=0):
        rng=random.Random(seed)
        rings, sides = 5, 10
        v=[]
        for j in range(rings+1):
            phi=math.pi*j/rings
            for i in range(sides):
                theta=i*math.tau/sides
                r=1+rng.uniform(-.1,.1)
                v.append((p[0]+s[0]*math.sin(phi)*math.cos(theta)*r/2,
                          p[1]+s[1]*math.cos(phi)*r/2,
                          p[2]+s[2]*math.sin(phi)*math.sin(theta)*r/2))
        for j in range(rings):
            for i in range(sides):
                k=(i+1)%sides
                self.face([v[j*sides+i],v[j*sides+k],v[(j+1)*sides+k],v[(j+1)*sides+i]],mat)

    def sign(self, p, w, h, index):
        col,row=index%4,index//4
        u0,u1=col/4+.008,(col+1)/4-.008
        v0,v1=1-(row+1)/4+.008,1-row/4-.008
        x,y,z=p
        self.box((x,y,z+.04),(w+.12,h+.12,.1),'Ink')
        self.face([(x-w/2,y-h/2,z-.02),(x-w/2,y+h/2,z-.02),
                   (x+w/2,y+h/2,z-.02),(x+w/2,y-h/2,z-.02)], 'Sign',
                  [(u0,v0),(u0,v1),(u1,v1),(u1,v0)])

    def append(self, other, offset=(0,0,0), yaw=0):
        c,s=math.cos(math.radians(yaw)),math.sin(math.radians(yaw))
        for mat,faces in other.parts.items():
            for points,uv in faces:
                self.face([(offset[0]+x*c+z*s,offset[1]+y,offset[2]-x*s+z*c) for x,y,z in points],mat,uv)

    def save(self):
        lines=[f'# Original stylized art; metre units; facade faces -Z',f'mtllib Palette.mtl']
        offset=1
        triangles=0
        for mat,faces in self.parts.items():
            lines.extend([f'g {self.name}_{mat}', f'usemtl {mat}'])
            for pts,uv in faces:
                if len(pts) != len(uv): raise ValueError(self.name)
                for p in pts: lines.append('v '+' '.join(f'{k:.5f}' for k in p))
                for t in uv: lines.append('vt '+' '.join(f'{k:.6f}' for k in t))
                for k in range(1,len(pts)-1):
                    ids=[offset,offset+k,offset+k+1]
                    # Skip zero-area pole triangles.
                    a,b,c=[pts[i-offset] for i in ids]
                    ab=[b[j]-a[j] for j in range(3)]; ac=[c[j]-a[j] for j in range(3)]
                    area=sum(q*q for q in (ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0]))
                    if area > 1e-12:
                        lines.append('f '+' '.join(f'{i}/{i}' for i in ids));triangles+=1
                offset+=len(pts)
        (MODEL/(self.name+'.obj')).write_text('\n'.join(lines)+'\n',encoding='utf-8')
        return {'name':self.name,'triangles':triangles,'materialSlots':len(self.parts)}

def roof(m,w,d,y,kind='hip'):
    x,z=w/2+.36,d/2+.32
    rise=1.05
    if kind=='flat':
        m.box((0,y,0),(w+.4,.2,d+.4),'Ink')
        for a in [-1,1]: m.box((a*w/2,y+.3,0),(.18,.6,d+.4),'Cream')
        m.box((0,y+.3,d/2),(w,.6,.18),'Cream');return
    v=[(-x,y,-z),(x,y,-z),(x,y,z),(-x,y,z),(-x*.45,y+rise,0),(x*.45,y+rise,0)]
    faces=[(0,4,5,1),(1,5,2),(2,5,4,3),(3,4,0)]
    if kind=='mono':
        v=[(-x,y,-z),(x,y,-z),(x,y+rise,z),(-x,y+rise,z)]
        faces=[(0,3,2,1)]
    for ids in faces: m.face([v[i] for i in ids],'Roof')
    for a,b in [(0,1),(1,2),(2,3),(3,0)]: m.beam(v[a],v[b],.085,'Ink')
    if kind!='mono':
        m.beam(v[4],v[5],.14,'Roof')
        for a,b in [(0,4),(1,5),(2,5),(3,4)]:m.beam(v[a],v[b],.07,'Ink')
        # Modeled ceramic courses follow each roof slope.
        for j in range(1,6):
            t=j/6
            for side in [-1,1]:
                m.beam((-x+(x-x*.45)*t,y+rise*t,side*z*(1-t)),
                       (x-(x-x*.45)*t,y+rise*t,side*z*(1-t)),.034,'Cream',4)
    m.box((0,y-.12,0),(w+.6,.18,d+.55),'Wood')

def window(m,x,y,z,w=1.1,h=1.4,shutters=False):
    m.box((x,y,z),(w+.16,h+.16,.13),'Cream')
    m.box((x,y,z-.08),(w,h,.09),'Glass')
    m.box((x,y,z-.145),(.055,h,.045),'Ink')
    m.box((x,y-h*.12,z-.15),(w,.055,.045),'Ink')
    m.box((x,y-h/2-.1,z-.14),(w+.32,.14,.3),'Ink')
    if shutters:
        for side in [-1,1]:
            sx=x+side*(w/2+.21)
            m.box((sx,y,z-.02),(.28,h,.12),'Wood')
            for j in range(7):m.box((sx,y-h/2+.1+j*h/7,z-.095),(.26,.025,.045),'Cream')

def balcony(m,y,w,z):
    m.box((0,y,z+.25),(w,.16,1),'Cream')
    m.beam((-w/2,y+1,z-.2),(w/2,y+1,z-.2),.045,'Ink')
    for i in range(13):
        x=-w/2+i*w/12
        m.beam((x,y,z-.2),(x,y+1,z-.2),.025,'Ink',4)
    for side in [-1,1]:m.beam((side*w/2,y+1,z-.2),(side*w/2,y+1,z+.75),.045,'Ink')

def side_window(m,side,x,y,z,w=1.1,h=1.1):
    detail=Mesh('SideWindow');window(detail,0,0,-.08,w,h)
    m.append(detail,(side*x,y,z),-90 if side>0 else 90)

def house(name,wall,kind,variant):
    m=Mesh(name);w,d=4.6,5.4; y=5.7 if variant==0 else 6.25
    m.box((0,y/2,0),(w,y,d),wall)
    m.box((0,.16,0),(w+.24,.32,d+.24),'Ink')
    roof(m,w,d,y,kind)
    m.box((-.9,1.12,-d/2-.08),(1.1,2.24,.18),'Wood')
    for i in range(6):m.box((-1.32+i*.16,1.15,-d/2-.2),(.035,1.95,.03),'Cream')
    m.box((-.9,2.45,-d/2-.3),(1.6,.12,.95),'Roof')
    for x in [-1.15,1.15]:window(m,x,4.25,-d/2-.1,1.22,1.6,variant==0)
    window(m,1.08,1.65,-d/2-.1,1.25,1.1)
    for side in [-1,1]:
        for z in [-1.2,1.25]:side_window(m,side,w/2,4.25,z,.9,1.25)
        side_window(m,side,w/2,1.75,.8,.8,.7)
    balcony(m,3.15,3.6,-d/2-.62)
    # Exterior air conditioner, rain pipe, television antenna, asymmetric entry step.
    m.box((1.15,.58,-d/2-.34),(1.1,.65,.48),'Cream')
    for i in range(5):m.box((1.15,.36+i*.1,-d/2-.59),(.9,.025,.03),'Metal')
    m.beam((2.18,.25,-2.85),(2.18,y-.1,-2.85),.055,'Metal')
    m.box((-.9,.1,-3.03),(1.55,.2,.8),'Paving')
    m.beam((1.45,y+.5,1.4),(1.45,y+2,1.4),.035,'Ink')
    for j in range(3):m.beam((.8,y+1.5+j*.16,1.4),(2.1,y+1.5+j*.16,1.4),.02,'Metal')
    return m

def shop(name,wall,index,style):
    m=Mesh(name);w=6.2 if style!='cafe' else 5.2;d=5.6
    h=3.6 if style=='mart' else (6.5 if style=='radio' else 5.8)
    m.box((0,h/2,0),(w,h,d),wall)
    roof(m,w,d,h,'flat' if style in ['mart','radio'] else 'hip' if style=='general' else 'mono')
    m.box((0,.15,0),(w+.2,.3,d+.2),'Ink')
    for x in [-w*.3,w*.3]:window(m,x,1.65,-2.86,w*.32,2.3)
    m.box((0,1.25,-2.88),(1.05,2.5,.16),'Ink')
    m.box((0,1.5,-2.99),(.88,1.8,.05),'Glass')
    m.sign((0,3.15,-2.98),w-.5,.65,index)
    for side in [-1,1]:
        for z in [-1.4,1.2]:side_window(m,side,w/2,1.6,z,1.2,1.8 if style=='mart' else 1.25)
        if style!='mart':side_window(m,side,w/2,4.6,.7,1.25,1.2)
    if style=='mart':
        corner_sign=Mesh('ReturnSign');corner_sign.sign((0,0,0),3.7,.5,index)
        m.append(corner_sign,(w/2+.1,3.12,0),-90)
    if style=='mart':
        for y,mat in [(2.74,'Mint'),(2.63,'Gold'),(2.52,'Lilac')]:m.box((0,y,-2.98),(w,.095,.12),mat)
        m.box((0,3.68,-2.8),(w+.4,.16,.9),'Cream')
    else:
        for x in [-w*.28,w*.28]: window(m,x,4.6,-2.86,1.2,1.4,style=='bakery')
        for i in range(12):m.box((-w/2+.22+i*(w-.44)/11,2.72,-3.2),((w-.44)/11,.12,1),'Cream' if i%2 else 'Lilac',0)
        m.box((0,2.6,-3.7),(w,.28,.1),'Wood')
    if style=='cafe':
        for x in [-1.8,1.8]:
            m.beam((x,.1,-3.6),(x,.9,-3.6),.08,'Metal')
            m.box((x,.94,-3.6),(.85,.1,.85),'Wood')
        m.sign((2.35,1.1,-3.25),.6,1.1,2)
    if style=='bakery':
        for x in [-1.8,1.8]:
            m.box((x,.5,-3.22),(1.1,.7,.6),'Wood')
            for j in range(4):m.ellipsoid((x-.33+j*.22,.92,-3.23),(.25,.15,.32),'Gold',j)
    if style=='general':
        for x in [-2,1.9]:
            for y in [.3,.85]:
                m.box((x,y,-3.23),(.85,.5,.65),'Wood')
                for j in range(3):m.box((x,y-.16+j*.16,-3.57),(.84,.045,.03),'Cream')
    if style=='radio':
        m.sign((w/2-.1,4.5,-3.25),.65,1.45,5)
        balcony(m,4.02,2.6,-3.3)
        m.beam((1,h,0),(1,h+2.8,0),.05,'Ink')
        for j in range(4):m.beam((.2,h+1+j*.35,0),(1.8,h+1+j*.35,0),.03,'Metal')
    return m

def bookshop():
    m=Mesh('BooksAndRecords');w,d=4.8,5.2
    m.box((0,2.65,0),(w,5.3,d),'Cream');roof(m,w,d,5.3,'hip')
    m.box((0,.2,0),(5,.4,5.4),'Ink')
    m.box((0,1.45,-2.67),(4.4,2.5,.13),'Wood')
    for x in [-1.5,1.5]:window(m,x,1.6,-2.79,.9,1.9)
    for x in [-.7,.7]:window(m,x,4.3,-2.68,1.1,1.15)
    for i in range(3):m.box((-.55+i*.55,2.32,-2.88),(.5,.55,.04),'Mint')
    m.sign((0,3.35,-2.84),4.2,.68,13)
    for side in [-1,1]:
        side_window(m,side,w/2,4.1,.5,1.3,1.2)
        for z in [-.8,.8]:side_window(m,side,w/2,1.8,z,.7,1.1)
    m.box((1.7,.64,-3.2),(1,.9,.48),'Wood')
    for j in range(6):m.box((1.32+j*.14,.97,-3.19),(.09,.48,.28),['Lilac','Mint','Gold'][j%3])
    return m

def tower():
    m=Mesh('ClockLandmark')
    m.box((0,3.5,0),(2.1,7,2.1),'Cream')
    m.box((0,1.7,0),(2.8,3.4,2.5),'Mint')
    roof(m,2.6,2.6,7,'hip')
    for y in [.3,3.4,5.9]:m.box((0,y,0),(2.6,.18,2.5),'Ink')
    m.beam((0,5.1,-1.12),(0,5.1,-1.26),.72,'Ink',32)
    m.beam((0,5.1,-1.27),(0,5.1,-1.31),.63,'Gold',32)
    m.beam((0,5.1,-1.34),(0,5.57,-1.34),.035,'Ink')
    m.beam((0,5.1,-1.34),(.36,4.94,-1.34),.035,'Ink')
    m.sign((0,2.4,-1.29),2.2,.6,0)
    return m

def tree():
    m=Mesh('PurpleFlowerTree');rng=random.Random(44)
    m.beam((0,0,0),(.1,3.6,0),.17,'Wood',8)
    for i in range(9):
        a=i*2.4;end=(math.cos(a)*1.45,3.5+rng.random()*1.25,math.sin(a)*1.4)
        m.beam((.1,2.4,0),end,.07,'Wood',6)
        m.ellipsoid(end,(2.25,1.6,2.1),'Leaf' if i%3 else 'Blossom',i)
    return m

def bicycle():
    m=Mesh('NeighborhoodBicycle')
    for x in [-.7,.7]:
        for i in range(20):
            a=i*math.tau/20;b=(i+1)*math.tau/20
            m.beam((x+.49*math.cos(a),.52+.49*math.sin(a),0),(x+.49*math.cos(b),.52+.49*math.sin(b),0),.04,'Ink')
        for i in range(10):
            a=i*math.tau/10;m.beam((x,.52,0),(x+.45*math.cos(a),.52+.45*math.sin(a),0),.009,'Metal',3)
    for a,b in [((-.7,.52,0),(-.2,1.3,0)),((-.2,1.3,0),(.1,.5,0)),((.1,.5,0),(-.7,.52,0)),((-.2,1.3,0),(.6,1.3,0)),((.6,1.3,0),(.1,.5,0)),((.6,1.3,0),(.7,.52,0))]:m.beam(a,b,.033,'Mint')
    m.box((-.2,1.4,0),(.42,.1,.24),'Wood')
    m.beam((.6,1.3,0),(.56,1.6,0),.025,'Metal')
    m.beam((.56,1.6,-.25),(.56,1.6,.25),.025,'Metal')
    m.box((.8,1.25,0),(.42,.3,.36),'Cream');return m

def vending():
    m=Mesh('VendingMachine');m.box((0,1,0),(1.05,2,.65),'Red')
    m.box((-.12,1.22,-.35),(.7,1.22,.07),'Cream')
    for row in range(3):
        for col in range(4):
            m.box((-.37+col*.17,.9+row*.32,-.4),(.1,.21,.08),['Mint','Gold','Lilac','Red'][col])
    m.box((0,.3,-.35),(.65,.22,.07),'Ink')
    m.box((.38,.95,-.36),(.14,.28,.1),'Gold');return m

def planter():
    m=Mesh('FlowerPlanter');m.box((0,.25,0),(1.5,.5,.65),'Cream')
    m.box((0,.52,0),(1.6,.12,.74),'Ink')
    for i in range(5):m.ellipsoid((-.55+i*.27,.77,0),(.43,.5,.5),'Blossom',i)
    return m

def mailbox():
    m=Mesh('PostBox');m.box((0,.6,0),(.16,1.2,.16),'Ink')
    m.box((0,1.4,0),(.65,.65,.45),'Red');m.box((0,1.5,-.24),(.4,.055,.03),'Ink')
    m.sign((0,1.26,-.25),.45,.19,14);return m

def pole(lamp=False):
    m=Mesh('StreetLamp' if lamp else 'UtilityPole')
    m.beam((0,0,0),(0,7.3 if not lamp else 5.4,0),.09,'Metal' if lamp else 'Wood')
    if lamp:
        m.beam((0,5.35,0),(-1,5.35,0),.055,'Ink')
        m.box((-.95,5.23,0),(.65,.16,.45),'Cream')
    else:
        for y in [6.5,7.1]:
            m.beam((-1.1,y,0),(1.1,y,0),.055,'Metal')
            for x in [-.85,0,.85]:m.beam((x,y,0),(x,y+.23,0),.07,'Cream')
        m.box((.25,5.4,0),(.5,.8,.45),'Metal')
    return m

def street():
    m=Mesh('StreetBase20m')
    m.box((0,-.13,10),(40,.12,20),'Paving')
    m.box((0,-.055,10),(10.6,.1,20),'Road')
    for side in [-1,1]:
        m.box((side*6.4,.02,10),(2.2,.18,20),'Paving')
        m.box((side*5.35,.08,10),(.15,.25,20),'Cream')
        m.box((side*4.8,.012,10),(.07,.025,20),'White')
        for z in range(1,20,2):
            m.box((side*1.6,.015,z),(.09,.025,1.2),'Gold')
        for z in [1.6,11.6]:
            m.box((side*5.1,.03,z),(.33,.025,.7),'Ink')
            for i in range(5):m.box((side*5.1,.048,z-.26+i*.13),(.31,.01,.035),'Metal')
        # Sidewalk paving has modeled joint lines, not emissive road edges.
        for z in range(20):m.box((side*6.4,.118,z+.02),(2.05,.012,.018),'Ink')
    return m

def wires():
    m=Mesh('OverheadWireSpan20m')
    for x in [-.85,0,.85]:
        for i in range(20):
            f=lambda z:7.33-.65*math.sin(math.pi*z/20)
            m.beam((x,f(i),i),(x,f(i+1),i+1),.013,'Ink',3)
    return m

def guardrail():
    m=Mesh('GuardRail')
    for x in [-1.2,0,1.2]:m.beam((x,0,0),(x,.85,0),.06,'Cream')
    for y in [.4,.8]:m.beam((-1.4,y,0),(1.4,y,0),.06,'Cream')
    return m

def route_sign():
    m=Mesh('RouteMarker');m.beam((0,0,0),(0,2.9,0),.045,'Metal')
    m.sign((0,2.6,-.05),.8,.8,12);return m

def bench():
    m=Mesh('NeighborhoodBench')
    for x in [-.7,.7]:m.box((x,.3,0),(.12,.6,.6),'Ink')
    for z in [-.24,-.08,.08,.24]:m.box((0,.64,z),(1.9,.1,.12),'Wood')
    for y in [.95,1.2]:m.box((0,y,.35),(1.9,.14,.1),'Wood')
    return m

def crossing():
    m=Mesh('ZebraCrossing')
    for i in range(12):m.box((-4.4+i*.8,.025,0),(.46,.018,2.2),'White')
    return m

def side_street():
    m=Mesh('SideStreetApron')
    m.box((0,.019,0),(12,.025,3.6),'Road')
    for z in [-1.8,1.8]:m.box((0,.05,z),(12,.1,.1),'Cream')
    return m

def plaza():
    m=Mesh('ClockPlazaPaving')
    m.box((0,.02,0),(8,.12,12),'Paving')
    for i in range(24):
        a=i*math.tau/24;b=(i+1)*math.tau/24
        m.beam((2.7*math.cos(a),.095,2.7*math.sin(a)),(2.7*math.cos(b),.095,2.7*math.sin(b)),.045,'Gold',4)
    return m

def kiosk():
    m=Mesh('StationKiosk')
    m.box((0,1.5,0),(3.4,3,3),'Mint');roof(m,3.4,3,3,'hip')
    window(m,-.65,1.4,-1.57,.9,1.7);m.box((.7,1.1,-1.55),(.85,2.2,.15),'Wood')
    m.sign((0,2.65,-1.65),2.9,.45,6)
    return m

def barrier(name,kind):
    m=Mesh(name)
    # Visual X is normalized to [-.5,.5]; spawner alone supplies lane width.
    if kind=='jump':
        m.box((0,0,0),(1,.6,.42),'Ink')
        m.box((0,0,-.225),(.97,.48,.035),'Cream')
        m.sign((0,0,-.25),.38,.46,8)
        for side in [-1,1]:
            for j in range(3):m.box((side*(.32+j*.07),0,-.25),(.035,.43,.025),'Lilac')
    elif kind=='duck':
        m.box((0,0,0),(1,1,.4),'Ink')
        m.box((0,.04,-.225),(.97,.76,.04),'Lilac')
        m.box((0,-.41,-.24),(1,.12,.045),'Gold')
        m.sign((0,.02,-.265),.38,.62,9)
        for side in [-1,1]:
            for j in range(3):m.box((side*(.23+j*.09),-.22,-.26),(.045,.18,.03),'Cream')
    else:
        m.box((0,0,0),(1,4,.46),'Ink')
        m.box((0,0,-.245),(.91,3.82,.025),'Peach')
        m.sign((0,1.25,-.27),.72,.65,10)
        for y in [-1.45,-.5,.45]:
            for side in [-1,1]:m.beam((side*.03,y-.22,-.29),(side*.28,y+.2,-.29),.035,'Ink',4)
        m.box((0,-1.8,-.28),(.94,.22,.035),'Gold')
    return m

def coin():
    m=Mesh('MoriohCrestCoin')
    m.beam((0,0,-.07),(0,0,.07),.36,'Ink',32)
    m.beam((0,0,-.081),(0,0,.081),.31,'Gold',32)
    for side in [-1,1]:
        z=side*.09
        for a,b in [((-.17,0,z),(0,.2,z)),((0,.2,z),(.17,0,z)),((.17,0,z),(0,-.2,z)),((0,-.2,z),(-.17,0,z))]:m.beam(a,b,.025,'Lilac',4)
    return m

def target():
    m=Mesh('HighSideCrest')
    m.beam((0,0,-.13),(0,0,.13),.57,'Ink',12)
    m.beam((0,0,-.16),(0,0,.16),.49,'Cream',12)
    m.beam((0,0,-.18),(0,0,.18),.38,'Lilac',12)
    for side in [-1,1]:
        for a,b in [((-.22,0,side*.2),(0,.26,side*.2)),((0,.26,side*.2),(.22,0,side*.2)),((.22,0,side*.2),(0,-.26,side*.2)),((0,-.26,side*.2),(-.22,0,side*.2))]:m.beam(a,b,.045,'Gold')
    return m

def gate():
    m=Mesh('PoseGateFrame')
    for side in [-1,1]:
        m.box((side*5.2,3.3,0),(.4,6.6,.6),'Ink')
        m.box((side*5.2,3.2,-.34),(.22,5.9,.08),'Cream')
        m.box((side*4,3.3,.15),(1.8,6.5,.35),'Lilac')
        for y in [.5,5.7]:m.box((side*4,y,-.08),(1.7,.25,.14),'Gold')
    m.box((0,6.65,0),(10.85,.35,.65),'Ink')
    m.box((0,6.8,0),(11.1,.18,.85),'Roof')
    m.box((0,6.4,-.36),(10.5,.16,.06),'Gold')
    return m

def horizon():
    m=Mesh('ContinuousHorizon');rng=random.Random(101)
    # Below the existing track/foreground fill: prevents a sky gap past the pool.
    m.box((0,-.35,0),(900,.1,900),'DistantGround')
    for layer,radius,mat in [(0,260,'FarHill'),(1,230,'NearHill')]:
        pts=[]
        for i in range(129):
            a=i*math.tau/128
            h=14+layer*3+8*math.sin(a*3+layer)+6*math.cos(a*7)
            pts.append((radius*math.cos(a),h,radius*math.sin(a)))
        for i in range(128):
            a,b=pts[i],pts[i+1]
            # Faces inward; two sided material also guards camera movement.
            m.face([(a[0],-30,a[2]),(a[0],a[1],a[2]),(b[0],b[1],b[2]),(b[0],-30,b[2])],mat)
    for i in range(80):
        a=i*math.tau/80;r=215;h=rng.uniform(3,10)
        m.box((r*math.cos(a),h/2-1,r*math.sin(a)),(rng.uniform(3,7),h,5),'Town',-math.degrees(a))
    return m

def textures():
    fontpath=next((p for p in [Path('C:/Windows/Fonts/YuGothM.ttc'),Path('C:/Windows/Fonts/msgothic.ttc')] if p.exists()),None)
    if fontpath is None: raise RuntimeError('Japanese font required for source atlas; existing Windows Yu Gothic or MS Gothic expected.')
    jp=ImageFont.truetype(str(fontpath),64);en=ImageFont.truetype(str(fontpath),26)
    atlas=Image.new('RGB',(2048,1024));d=ImageDraw.Draw(atlas)
    for i,(japanese,english) in enumerate(SIGNS):
        x,y=(i%4)*512,(i//4)*256
        bg=[PALETTE['Cream'],PALETTE['Mint'],PALETTE['Lilac'],PALETTE['Gold']][i%4]
        d.rectangle((x,y,x+511,y+255),fill=PALETTE['Ink'])
        d.rectangle((x+10,y+10,x+501,y+245),fill=bg)
        if i in [8,9,10,11]:
            # Large English action cues take priority on fast approaching hazards.
            action=english.split() if i in [10,11] else [english]
            af=ImageFont.truetype(str(ROOT/'Assets/ActiveMotion3D/Art/FontEX/Bevan-Regular.ttf'),66 if len(action)>1 else 103)
            jf=ImageFont.truetype(str(fontpath),30)
            bb=d.textbbox((0,0),japanese,font=jf)
            d.text((x+256-(bb[2]-bb[0])/2,y+25),japanese,font=jf,fill=PALETTE['Ink'])
            for row,text in enumerate(action):
                bb=d.textbbox((0,0),text,font=af)
                cy=y+(130 if len(action)==1 else 108+row*78)
                d.text((x+256-(bb[2]-bb[0])/2,cy-(bb[3]-bb[1])/2-bb[1]),text,font=af,fill=PALETTE['Ink'])
            continue
        d.line((x+30,y+170,x+482,y+170),fill=PALETTE['Ink'],width=3)
        for text,font,cy in [(japanese,jp,y+108),(english,en,y+204)]:
            bb=d.textbbox((0,0),text,font=font);tw=bb[2]-bb[0]
            if tw>455:
                font=ImageFont.truetype(str(fontpath),int(font.size*455/tw));bb=d.textbbox((0,0),text,font=font)
            d.text((x+256-(bb[2]-bb[0])/2,cy-(bb[3]-bb[1])/2-bb[1]),text,font=font,fill=PALETTE['Ink'])
    atlas.save(TEX/'NeighborhoodSigns.png')
    # A seamless wrap panorama of original, layered ink-edged cloud masses.
    clouds=Image.new('RGBA',(2048,1024));rng=random.Random(94)
    for i in range(14):
        cx=rng.randrange(2048);cy=rng.randrange(230,475);w=rng.randrange(120,240);h=rng.randrange(40,80)
        mask=Image.new('L',(w*2,h*4));md=ImageDraw.Draw(mask)
        md.ellipse((w*.35,h*1.65,w*1.65,h*2.5),fill=255)
        for j in range(7):
            x=w*.46+j*w*.18;rise=(1-abs(j-3)/4)*h*.75
            bw=w*rng.uniform(.15,.21);bh=h*rng.uniform(.55,.8)
            md.ellipse((x-bw,h*1.8-rise-bh,x+bw,h*1.8-rise+bh),fill=255)
        cluster=Image.new('RGBA',mask.size)
        cluster.paste((111,112,155,255),(0,0),mask.filter(ImageFilter.MaxFilter(5)))
        cluster.paste((174,192,205,255),(0,0),mask)
        highlight=ImageChops.multiply(mask,ImageChops.offset(mask,-8,-9))
        cluster.paste((246,237,215,255),(0,0),highlight)
        for wrap in [-2048,0,2048]:
            clouds.alpha_composite(cluster,(cx-w+wrap,cy-h*2))
    clouds.save(TEX/'LavenderCloudPanorama.png')
    paving=Image.new('RGB',(256,256),(193,184,195));dd=ImageDraw.Draw(paving)
    for row in range(4):
        for col in range(4):
            x,y=col*64,row*64;v=181+((row*7+col*13)%23)
            dd.rectangle((x+2,y+2,x+62,y+62),fill=(v+7,v,v+9))
            dd.line((x+4,y+4,x+60,y+4),fill=(218,209,219),width=2)
    paving.save(TEX/'SidewalkPavers.png')

def main():
    meshes=[house('HippedRoofResidence','Mint','hip',0),house('SplitLevelResidence','Lilac','flat',1),
            shop('NijiConvenience','Cream',1,'mart'),shop('SumireCafe','Mint',2,'cafe'),
            shop('HoshiBakery','Peach',3,'bakery'),shop('YunagiRadio','Lilac',5,'radio'),
            shop('MoriohGeneralStore','Peach',4,'general'),bookshop(),
            kiosk(),tower(),tree(),bicycle(),vending(),planter(),
            mailbox(),pole(),pole(True),street(),wires(),guardrail(),route_sign(),bench(),crossing(),side_street(),plaza(),
            barrier('JumpPromenadeRail','jump'),barrier('DuckShopCanopy','duck'),
            barrier('DodgeDeliveryScreen','dodge'),coin(),target(),gate(),horizon()]
    stats=[m.save() for m in meshes]
    mtl=[]
    for name,color in PALETTE.items():
        mtl += [f'newmtl {name}','Kd '+' '.join(f'{v/255:.4f}' for v in color),'Ka 0 0 0','Ks 0.05 0.05 0.05','Ns 8','d 1']
        if name=='Sign':mtl.append('map_Kd ../Textures/NeighborhoodSigns.png')
    (MODEL/'Palette.mtl').write_text('\n'.join(mtl)+'\n')
    textures()
    (OUT/'manifest.json').write_text(json.dumps({'units':'metres','facadeForward':'-Z','models':stats,'palette':PALETTE,'signs':SIGNS},indent=2,ensure_ascii=False),encoding='utf-8')
    # Stable GUIDs, including folders, without touching existing metadata.
    for path in sorted(OUT.rglob('*'))+[OUT,OUT.parent]:
        if path.suffix=='.meta':continue
        meta=Path(str(path)+'.meta')
        if not meta.exists():
            guid=uuid.uuid5(uuid.NAMESPACE_URL,path.relative_to(ROOT).as_posix()).hex
            meta.write_text(f'fileFormatVersion: 2\nguid: {guid}\n'+('folderAsset: yes\nDefaultImporter:\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n' if path.is_dir() else ''),encoding='utf-8')
            if path.suffix=='.png':
                with meta.open('a',encoding='utf-8') as handle:
                    handle.write('TextureImporter:\n  textureType: 0\n  textureShape: 1\n')
    print(f'Created {len(stats)} models, 3 textures; {sum(s["triangles"] for s in stats):,} total triangles across source library.')

if __name__=='__main__':main()
