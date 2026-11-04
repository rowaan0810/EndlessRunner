"""Software preview of actual OBJ geometry; does not launch or emulate Unity."""
from pathlib import Path
import math
from generate_design_kit import ROOT, MODEL, TEX, PALETTE
from PIL import Image, ImageDraw, ImageFont

def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def unit(v):
    n=math.sqrt(dot(v,v));return tuple(x/n for x in v) if n else (0,0,0)

def load(name):
    v=[];uv=[];faces=[];material='Ink'
    for line in (MODEL/(name+'.obj')).read_text().splitlines():
        fields=line.split()
        if not fields:continue
        if fields[0]=='v':v.append(tuple(map(float,fields[1:])))
        elif fields[0]=='vt':uv.append(tuple(map(float,fields[1:])))
        elif fields[0]=='usemtl':material=fields[1]
        elif fields[0]=='f':
            ids=[p.split('/') for p in fields[1:]]
            faces.append(([v[int(p[0])-1] for p in ids],[uv[int(p[1])-1] for p in ids],material))
    return faces

ATLAS=Image.open(TEX/'NeighborhoodSigns.png').convert('RGB')

def texture_triangle(canvas,pts,uv):
    left=max(0,int(min(p[0] for p in pts)));top=max(0,int(min(p[1] for p in pts)))
    right=min(canvas.width,int(max(p[0] for p in pts))+1);bottom=min(canvas.height,int(max(p[1] for p in pts))+1)
    if right<=left or bottom<=top:return
    p=[(x-left,y-top) for x,y in pts]
    x0,y0=p[0];x1,y1=p[1];x2,y2=p[2]
    det=(x1-x0)*(y2-y0)-(x2-x0)*(y1-y0)
    if abs(det)<.01:return
    u=[k[0]*ATLAS.width for k in uv];v=[(1-k[1])*ATLAS.height for k in uv]
    def coeff(t):
        a=((t[1]-t[0])*(y2-y0)-(t[2]-t[0])*(y1-y0))/det
        b=((x1-x0)*(t[2]-t[0])-(x2-x0)*(t[1]-t[0]))/det
        return a,b,t[0]-a*x0-b*y0
    tile=ATLAS.transform((right-left,bottom-top),Image.Transform.AFFINE,coeff(u)+coeff(v),Image.Resampling.BILINEAR)
    mask=Image.new('L',tile.size);ImageDraw.Draw(mask).polygon(p,fill=255)
    canvas.paste(tile,(left,top),mask)

def render(name,w=480,h=390):
    img=Image.new('RGB',(w,h),(184,174,201));d=ImageDraw.Draw(img)
    d.rectangle((0,int(h*.77),w,h),fill=(210,202,210))
    camera=(11,8,-14);target=(0,3,0)
    forward=unit(sub(target,camera));right=unit(cross(forward,(0,1,0)));up=cross(right,forward)
    def project(v):
        a=sub(v,camera);z=dot(a,forward)
        return (w*.5-dot(a,right)*w*1.3/z,h*.53-dot(a,up)*w*1.3/z,z)
    light=unit((-.4,.8,-.6));triangles=[]
    for points,uv,mat in load(name):
        n=unit(cross(sub(points[1],points[0]),sub(points[2],points[0])))
        center=tuple(sum(p[j] for p in points)/3 for j in range(3))
        if dot(n,sub(camera,center))<=0:continue
        screen=[project(p) for p in points]
        if min(p[2] for p in screen)<.1:continue
        shade=.68+.32*max(0,dot(n,light))
        color=tuple(int(v*shade) for v in PALETTE[mat])
        triangles.append(([(p[0],p[1]) for p in screen],color,mat,uv,[p[2] for p in screen]))
    # Depth buffering keeps windows and thin signs in front of the large walls.
    depth=[float('inf')]*(w*h);pixels=img.load();texels=ATLAS.load()
    for pts,color,mat,uv,zs in triangles:
        (x0,y0),(x1,y1),(x2,y2)=pts
        det=(x1-x0)*(y2-y0)-(y1-y0)*(x2-x0)
        if abs(det)<.0001:continue
        xmin=max(0,int(min(x0,x1,x2)));xmax=min(w-1,int(max(x0,x1,x2))+1)
        ymin=max(0,int(min(y0,y1,y2)));ymax=min(h-1,int(max(y0,y1,y2))+1)
        for y in range(ymin,ymax+1):
            for x in range(xmin,xmax+1):
                b=((x+.5-x0)*(y2-y0)-(y+.5-y0)*(x2-x0))/det
                c=((x1-x0)*(y+.5-y0)-(y1-y0)*(x+.5-x0))/det;a=1-b-c
                if min(a,b,c)<0:continue
                inv=a/zs[0]+b/zs[1]+c/zs[2];z=1/inv;index=y*w+x
                if z>=depth[index]:continue
                depth[index]=z
                if mat=='Sign':
                    u=(a*uv[0][0]/zs[0]+b*uv[1][0]/zs[1]+c*uv[2][0]/zs[2])*z
                    v=(a*uv[0][1]/zs[0]+b*uv[1][1]/zs[1]+c*uv[2][1]/zs[2])*z
                    pixels[x,y]=texels[min(ATLAS.width-1,max(0,int(u*ATLAS.width))),min(ATLAS.height-1,max(0,int((1-v)*ATLAS.height)))]
                else:pixels[x,y]=color
    return img

def main():
    models=[('HippedRoofResidence','Hipped roof residence'),('SplitLevelResidence','Parapet residence'),
            ('NijiConvenience','Niji convenience store'),('SumireCafe','Sumire cafe'),
            ('HoshiBakery','Hoshi bakery'),('YunagiRadio','Yunagi radio'),
            ('BooksAndRecords','Books and records'),('ClockLandmark','Clock plaza landmark')]
    canvas=Image.new('RGB',(1920,1000),(38,30,52));d=ImageDraw.Draw(canvas)
    fontpath=ROOT/'Assets/ActiveMotion3D/Art/FontEX/Bevan-Regular.ttf'
    title=ImageFont.truetype(str(fontpath),34);caption=ImageFont.truetype(str(fontpath),19)
    d.text((32,20),'MORIOH / NEIGHBORHOOD DESIGN KIT',font=title,fill=(245,226,186))
    for i,(model,label) in enumerate(models):
        x=(i%4)*480;y=95+(i//4)*435
        canvas.paste(render(model),(x,y))
        d.text((x+16,y+395),label,font=caption,fill=(245,226,186))
    small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
    d.text((32,968),'Actual source geometry / software preview. Unity materials, atmosphere and final framing require Editor review.',font=small,fill=(208,193,220))
    output=ROOT/'Docs/MoriohDesign/SourceArtPreview.png';output.parent.mkdir(parents=True,exist_ok=True)
    canvas.save(output);print(output)

if __name__=='__main__':main()
