"""Original, visual-only distant town geometry for the hybrid background."""
from pathlib import Path
import math, random, json
import generate_design_kit as kit

OUT=kit.ROOT/'Assets/ActiveMotion3D/Art/Environment/Morioh/HybridBackground/Models'
OUT.mkdir(parents=True,exist_ok=True)
kit.MODEL=OUT
COLORS={'TownNear':(112,107,143),'TownMid':(139,129,163),'Roof':(93,89,124),
        'Window':(179,171,190),'Trim':(89,84,118),'Ground':(168,150,181)}

def hip(m,w,d,h,mat='Roof'):
    x,z=w/2+.3,d/2+.3
    v=[(-x,h,-z),(x,h,-z),(x,h,z),(-x,h,z),(-x*.4,h+1.2,0),(x*.4,h+1.2,0)]
    for ids in [(0,4,5,1),(1,5,2),(2,5,4,3),(3,4,0)]:m.face([v[i] for i in ids],mat)
    m.beam(v[4],v[5],.09,'Trim')

def building(i,height,w,d):
    m=kit.Mesh('Building')
    m.box((0,height/2,0),(w,height,d),'TownNear' if i%3 else 'TownMid')
    if i%3==0:hip(m,w,d,height)
    else:
        m.box((0,height,0),(w+.3,.25,d+.3),'Roof')
        m.box((w*.2,height+.45,d*.2),(w*.35,.9,d*.4),'TownMid')
    for floor in range(max(1,int(height/2.7))):
        y=1.6+floor*2.7
        for x in [-w*.27,w*.27]:m.box((x,y,-d/2-.06),(w*.22,.9,.08),'Window')
    if i%4==0:
        m.beam((w*.25,height,d*.2),(w*.25,height+2,d*.2),.035,'Trim')
        m.beam((w*.25-1,height+1.6,d*.2),(w*.25+1,height+1.6,d*.2),.025,'Trim')
    if i%7==0:
        m.beam((-.8,height,0),(-.8,height+1.6,0),.06,'Trim')
        m.beam((.8,height,0),(.8,height+1.6,0),.06,'Trim')
        m.box((0,height+1.8,0),(2,1.5,1.8),'TownMid')
    return m

def main():
    town=kit.Mesh('TownSilhouette');rng=random.Random(481)
    for ring,count,radius in [(0,56,225),(1,78,268)]:
        for i in range(count):
            angle=i*math.tau/count+rng.uniform(-.018,.018)
            if min(angle,math.tau-angle)<.05:continue
            h=rng.uniform(4,11)+(2 if i%9==0 else 0)
            b=building(i+ring,h,rng.uniform(4,8),rng.uniform(4,7))
            town.append(b,(math.sin(angle)*radius,0,math.cos(angle)*radius),math.degrees(angle))
    def place(model,angle,radius):
        a=math.radians(angle);town.append(model,(math.sin(a)*radius,0,math.cos(a)*radius),angle)
    # A clock spire, low station, radio mast and a rooftop water tower break the skyline.
    clock=kit.Mesh('ClockSpire');clock.box((0,10,0),(4,20,4),'TownNear')
    for y in [5,14,19]:clock.box((0,y,0),(4.7,.3,4.7),'Trim')
    hip(clock,5,5,20)
    clock.beam((0,17,-2.05),(0,17,-2.18),1.25,'Window',24)
    clock.beam((0,17,-2.22),(0,17.8,-2.22),.06,'Trim')
    clock.beam((0,17,-2.22),(.7,16.75,-2.22),.06,'Trim')
    place(clock,18,235)
    station=kit.Mesh('Station');station.box((0,4,0),(20,8,8),'TownMid');hip(station,21,9,8)
    station.box((0,6,-4.15),(12,.8,.15),'Window')
    station.box((0,10,0),(5,5,5),'TownNear');hip(station,6,6,12.5)
    place(station,-8,240)
    mast=kit.Mesh('RadioMast');mast.box((0,6,0),(7,12,7),'TownMid')
    for x in [-1.3,1.3]:mast.beam((x,12,0),(x*.1,29,0),.08,'Trim')
    for y in range(13,29,2):
        half=1.3*(29-y)/17
        mast.beam((-half,y,0),(half,y+1.5,0),.05,'Trim')
    for y in [20,25,28]:mast.beam((-2,y,0),(2,y,0),.06,'Roof')
    place(mast,6,260)
    water=kit.Mesh('WaterTower');water.box((0,5.5,0),(8,11,7),'TownNear')
    for x in [-1.4,1.4]:
        for z in [-1.4,1.4]:water.beam((x,11,z),(x,17,z),.09,'Trim')
    water.beam((0,16,0),(0,20,0),2,'TownMid',12)
    water.box((0,20,0),(4.5,.25,4.5),'Roof');place(water,-23,245)
    stats=town.save()
    lines=[]
    for name,rgb in COLORS.items():lines += [f'newmtl {name}','Kd '+' '.join(str(v/255) for v in rgb),'d 1']
    (OUT/'Palette.mtl').write_text('\n'.join(lines)+'\n')
    (OUT/'manifest.json').write_text(json.dumps({'model':stats,'palette':COLORS,'seed':481},indent=2))
    print(stats)

if __name__=='__main__':main()
