"""Mockup of the v2 cast (reference only, not used by the game).

Draws the 20 characters with the same primitives Unity has (circle, rounded rect, square, rotation)
so the numbers can be copied into MapBuilder.People.cs. Units match Unity world units.
Usage: python mockup_v2.py out.png [color|sil]
"""
# Mockup of the proposed v2 person, built only from shapes Unity has (circle, rounded rect, square) plus rotation.
import math, sys
from PIL import Image, ImageDraw, ImageFont
OUT=(29,34,56)
def hx(h):h=h.lstrip('#');return tuple(int(h[i:i+2],16) for i in (0,2,4))
def sh(c,f):return tuple(max(0,min(255,int(v*f))) for v in c)
def mix(a,b,t):return tuple(int(a[i]*(1-t)+b[i]*t) for i in range(3))
class Fig:
    def __init__(s): s.items=[]
    def add(s,kind,c,size,col,order,rot=0,outline=False,ol=.045):
        if outline: s.items.append((order,len(s.items),kind,c,(size[0]+ol,size[1]+ol),OUT,rot))
        s.items.append((order+(.5 if outline else 0),len(s.items),kind,c,size,col,rot))
def pts(kind,c,size,rot,k,cx,cy):
    w,h=size[0]/2,size[1]/2; P=[]
    n=48
    for i in range(n):
        t=2*math.pi*i/n; ct,st=math.cos(t),math.sin(t)
        if kind=='c': x,y=w*ct,h*st
        elif kind=='q': e=12; x=w*math.copysign(abs(ct)**(2/e),ct); y=h*math.copysign(abs(st)**(2/e),st)
        else: e=4 if kind=='r' else 6; x=w*math.copysign(abs(ct)**(2/e),ct); y=h*math.copysign(abs(st)**(2/e),st)
        r=math.radians(rot); X=x*math.cos(r)-y*math.sin(r); Y=x*math.sin(r)+y*math.cos(r)
        P.append((cx+(c[0]+X)*k, cy-(c[1]+Y)*k))
    return P
def render(fig,img,cx,cy,k):
    d=ImageDraw.Draw(img)
    for it in sorted(fig.items,key=lambda z:(z[0],z[1])):
        o,i,kind,c,size,col,rot=it; d.polygon(pts(kind,c,size,rot,k,cx,cy),fill=col)
def T(base,off,rot=0):
    r=math.radians(rot); return (base[0]+off[0]*math.cos(r)-off[1]*math.sin(r), base[1]+off[0]*math.sin(r)+off[1]*math.cos(r))
def person(spec):
    f=Fig(); skin=hx(spec['skin']); hair=hx(spec['hair']); shirt=hx(spec['shirt']); pants=hx(spec['pants'])
    W=spec.get('width',1.0); Hh=spec.get('height',1.0); headw=spec.get('head',1.0)
    f.add('c',(0,-.52),(.9*W,.3),(60,62,82),-5)
    # legs
    if spec.get('skirt'): f.add('r',(0,-.26*Hh),(.42*W,.26),pants,1.6,outline=True)
    for x in(-.1,.1):
        x*=W; f.add('s',(x,-.34*Hh),(.16 if not spec.get('skirt') else .1,.34*Hh),pants if not spec.get('skirt') else skin,1,outline=True); f.add('s',(x+.02,-.52*Hh+.0),(.21,.1),hx(spec.get('shoes','#2A2F45')),3,outline=True)
    ty=-.02
    # arms (behind torso): shoulder inside torso, upper arm + forearm, short sleeve
    for side in(-1,1):
        sx,sy=side*.235*W,.15+ty; a=side*22; up=.2*Hh; fo=.2*Hh
        ex,ey=sx+math.sin(math.radians(a))*up, sy-math.cos(math.radians(a))*up
        b=a-side*14; wx,wy=ex+math.sin(math.radians(b))*fo, ey-math.cos(math.radians(b))*fo
        sleeve=shirt; fore=shirt if spec.get('long_sleeve') else skin
        f.add('s',((ex+wx)/2,(ey+wy)/2),(.1,fo+.06),fore,3.0,rot=b,outline=True)
        f.add('s',((sx+ex)/2,(sy+ey)/2),(.13,up+.08),sleeve,4.0,rot=a,outline=True)
        if not spec.get('long_sleeve'): f.add('c',(ex,ey+.01),(.13,.06),sh(shirt,.82),4.8,rot=a)
        f.add('c',(wx,wy-.01),(.12,.12),skin,5.0,outline=True)
    # torso: slight taper, no default belt/buttons
    tw=.5*W; th=.5*Hh
    f.add('r',(0,ty),(tw,th),shirt,6,outline=True)
    f.add('r',(.03,ty-.15*Hh),(tw-.08,.16),sh(shirt,.84),7)
    f.add('s',(0,ty+.215*Hh),(.2,.07),sh(shirt,.78),8)
    for extra in spec.get('torso',[]):
        extra(f,ty,shirt)
    f.add('s',(0,ty+.25*Hh),(.12,.09),sh(skin,.84),7.6)
    # head
    hy=.27*Hh+ty+.02; H=lambda x,y:(x*headw,hy+y)
    hw,hh=.5*headw,.46
    hairstyle=spec['hair_style']; hat=spec.get('hat')
    # back hair dome / long hair behind head
    if hairstyle=='long': f.add('r',H(0,.12),(.6*headw,.62),hair,9,outline=True)
    elif hairstyle not in('bald',) and not hat: f.add('c',H(0,.27),(.54*headw,.4),hair,9,outline=True)
    for x in(-1,1): f.add('c',H(x*.235,.17),(.09,.12),skin,10,outline=True)
    f.add('c',H(0,.2),(hw,hh),skin,11,outline=True)
    f.add('c',H(0,.06),(hw*.8,.16),sh(skin,.9),11.8)  # soft jaw shade
    f.add('c',H(0,.2),(hw*.9,hh*.85),skin,11.9)
    if spec.get('jaw')=='square': f.add('r',H(0,.06),(hw*.86,.2),skin,11.95,outline=False)
    # eyes: smaller ovals, mostly pupil
    expr=spec.get('expr','smile')
    for x in(-1,1):
        ex_=H(x*.1,.21)
        f.add('c',ex_,(.095,.115),(255,255,255),14,outline=True,ol=.025)
        f.add('c',(ex_[0]+spec.get('look',0.012),ex_[1]-.005),(.065,.08),OUT,15)
        f.add('c',(ex_[0]+spec.get('look',0.012)+.012,ex_[1]+.012),(.022,.022),(255,255,255),16)
        if expr=='smirk' or spec.get('lids'):
            f.add('s',(ex_[0],ex_[1]+.045),(.12,.05),skin,16.5)   # heavy upper lid
        if spec.get('glasses'):
            f.add('c',ex_,(.15,.14),OUT,13.5); f.add('c',ex_,(.12,.11),mix(skin,(200,230,255),.5),13.7)
    if spec.get('glasses'): f.add('s',H(0,.215),(.06,.02),OUT,16.6)
    # brows by expression
    bc=sh(hair,.75) if hairstyle!='bald' else hx('#9A9A9A')
    brow={'smile':(0,0),'smirk':(-12,14),'frown':(14,-14),'neutral':(0,0),'warm':(-8,8)}[expr]
    for i,x in enumerate((-1,1)):
        f.add('s',H(x*.1,.305+(.015 if (expr=='smirk' and x==1) else 0)),(.12,.035),bc,17,rot=brow[i])
    f.add('c',H(0,.13),(.065,.05),sh(skin,.82),17)
    # mouth: crescent from two circles, scaled 0.25..1 in y when talking like today
    m=H(spec.get('mouth_x',0),.065)
    if expr in('smile','warm'):
        f.add('c',m,(.13,.08),hx('#7A2E3A'),18); f.add('c',(m[0],m[1]+.025),(.14,.07),skin,18.5)
    elif expr=='smirk':
        f.add('c',(m[0]+.03,m[1]),(.12,.07),hx('#7A2E3A'),18,rot=12); f.add('c',(m[0]+.025,m[1]+.025),(.13,.065),skin,18.5,rot=12)
    elif expr=='frown':
        f.add('c',m,(.11,.07),hx('#7A2E3A'),18); f.add('c',(m[0],m[1]-.025),(.12,.065),skin,18.5)
    else: f.add('s',m,(.1,.025),hx('#7A2E3A'),18)
    if spec.get('blush'): 
        for x in(-1,1): f.add('c',H(x*.15,.1),(.08,.045),mix(skin,(255,130,140),.35),17)
    if spec.get('mustache'): f.add('r',H(0,.095),(.16,.05),bc,18.8,outline=False)
    if spec.get('beard'): f.add('r',H(spec.get('mouth_x',0),.0),(.16,.08),hair,19,outline=True)
    # front hair
    if not hat or hat=='hood':
        if hairstyle=='side':
            f.add('c',H(0,.38),(.5*headw,.2),hair,20,outline=True); f.add('r',H(-.07,.36),(.3,.11),hair,20.2,rot=-18,outline=True)
        elif hairstyle=='long':
            f.add('c',H(-.1,.37),(.3,.17),hair,20,rot=-14,outline=True); f.add('c',H(.1,.37),(.3,.17),hair,20,rot=14,outline=True)
        elif hairstyle=='spiky':
            f.add('c',H(0,.39),(.48*headw,.16),hair,20,outline=True)
            for x,r in((-.15,25),(-.05,8),(.06,-8),(.16,-25)): f.add('r',H(x,.48),(.1,.17),hair,19.5,rot=r,outline=True)
        elif hairstyle=='bun':
            f.add('c',H(0,.39),(.5*headw,.18),hair,20,outline=True); f.add('c',H(0,.55),(.17,.17),hair,8.5,outline=True)
        elif hairstyle=='curly':
            for x,y in((-.18,.33),(-.1,.42),(0,.45),(.1,.42),(.18,.33)): f.add('c',H(x,y),(.16,.15),hair,20,outline=True)
        elif hairstyle=='bald':
            for x in(-1,1): f.add('c',H(x*.2,.27),(.08,.14),hair,20,outline=True)
        elif hairstyle=='ponytail':
            f.add('c',H(0,.39),(.5*headw,.18),hair,20,outline=True); f.add('r',H(.27,.18),(.11,.3),hair,8.5,rot=-12,outline=True)
        elif hairstyle=='afro':
            f.add('c',H(0,.36),(.68*headw,.42),hair,8.4,outline=True); f.add('c',H(0,.41),(.5*headw,.16),hair,20,outline=True)
        elif hairstyle=='bob':
            f.add('r',H(0,.2),(.58*headw,.42),hair,8.6,outline=True); f.add('c',H(.04,.39),(.52*headw,.18),hair,20,outline=True)
        elif hairstyle=='buzz':
            f.add('c',H(0,.4),(.48*headw,.13),hair,20,outline=True)
    if hat=='cap':
        c=hx(spec['hat_color']); f.add('c',H(0,.4),(.54*headw,.24),c,21,outline=True); f.add('c',H(.12*spec.get('brim',1),.31),(.34,.08),sh(c,.75),21.5,outline=True)
    if hat=='flatcap':
        c=hx(spec['hat_color']); f.add('r',H(0,.39),(.56*headw,.18),c,21,outline=True); f.add('r',H(0,.31),(.5,.06),sh(c,.75),21.5,outline=True)
    if hat=='helmet':
        c=hx(spec['hat_color']); f.add('c',H(0,.38),(.58*headw,.3),c,21,outline=True); f.add('r',H(0,.28),(.62*headw,.06),sh(c,.8),21.5,outline=True)
    if hat=='police':
        c=hx(spec['hat_color']); f.add('r',H(0,.42),(.56*headw,.2),c,21,outline=True); f.add('c',H(0,.31),(.5,.07),hx('#111522'),21.5,outline=True); f.add('c',H(0,.43),(.08,.08),hx('#FFD23F'),22)
    if hat=='nurse':
        c=hx(spec['hat_color']); f.add('r',H(0,.45),(.3,.1),c,21,outline=True); f.add('s',H(0,.45),(.04,.06),hx('#2E86C1'),22)
    if hat=='beanie':
        c=hx(spec['hat_color']); f.add('c',H(0,.38),(.56*headw,.3),c,21,outline=True); f.add('r',H(0,.28),(.58*headw,.07),sh(c,.8),21.5,outline=True)
    if hat=='chonete':
        c=hx(spec['hat_color']); f.add('c',H(0,.31),(.78,.13),c,21,outline=True); f.add('c',H(0,.4),(.44,.2),c,21.6,outline=True); f.add('s',H(0,.33),(.44,.035),sh(c,.7),22)
    return f
def tie(col):
    def g(f,ty,shirt): f.add('s',(0,ty+.03),(.08,.28),hx(col),9,outline=True)
    return g
def badge(f,ty,shirt): f.add('s',(-.13,ty+.08),(.1,.07),(250,250,250),9,outline=True)
def phone(f,ty,shirt): pass
def chain(f,ty,shirt): f.add('c',(0,ty+.15),(.2,.12),hx('#FFD23F'),8.6); f.add('c',(0,ty+.17),(.16,.1),shirt,8.7)
def overall(col):
    def g(f,ty,shirt): f.add('r',(0,ty-.04),(.42,.4),hx(col),8.5,outline=True); f.add('s',(.12,ty-.12),(.08,.07),hx('#2B2B2B'),9)
    return g
def apron(f,ty,shirt): f.add('r',(0,ty-.06),(.38,.38),hx('#F1F4FF'),8.5,outline=True); f.add('s',(0,ty-.12),(.16,.08),hx('#E0E4F0'),9)
def pouch(f,ty,shirt): f.add('r',(.05,ty-.2),(.3,.09),hx('#2A2A2A'),9,outline=True)
def lanyard(col):
    def g(f,ty,shirt): f.add('s',(-.06,ty+.08),(.025,.22),hx(col),8.8,rot=-12); f.add('s',(.06,ty+.08),(.025,.22),hx(col),8.8,rot=12); f.add('s',(0,ty-.04),(.1,.12),(250,250,250),9,outline=True)
    return g
def police(f,ty,shirt): f.add('c',(-.13,ty+.09),(.08,.09),hx('#FFD23F'),9,outline=True); f.add('s',(0,ty-.2),(.5,.06),hx('#111522'),8.6)
def steth(f,ty,shirt): f.add('c',(0,ty+.1),(.24,.2),hx('#3A3F52'),8.6); f.add('c',(0,ty+.12),(.19,.15),shirt,8.7); f.add('c',(.07,ty+.0),(.06,.06),hx('#B0B6C6'),9,outline=True)
def coat(f,ty,shirt): f.add('s',(-.14,ty-.02),(.2,.48),(246,248,252),8.4,outline=True); f.add('s',(.14,ty-.02),(.2,.48),(246,248,252),8.4,outline=True)
def robe(f,ty,shirt): f.add('s',(0,ty+.18),(.14,.08),(250,250,250),9,outline=True)
def scarf(col):
    def g(f,ty,shirt): f.add('r',(0,ty+.2),(.4,.09),hx(col),8.8,outline=True); f.add('s',(.1,ty+.05),(.08,.2),hx(col),8.7,outline=True)
    return g
def stripe(col):
    def g(f,ty,shirt): f.add('s',(0,ty+.02),(.48,.07),hx(col),8.6)
    return g
def backpack(f,ty,shirt): f.add('s',(-.17,ty+.04),(.05,.36),hx('#2B2F45'),8.8); f.add('s',(.17,ty+.04),(.05,.36),hx('#2B2F45'),8.8)
def cardigan(col):
    def g(f,ty,shirt): f.add('s',(-.15,ty-.03),(.18,.46),hx(col),8.4,outline=True); f.add('s',(.15,ty-.03),(.18,.46),hx(col),8.4,outline=True)
    return g
def watch(f,ty,shirt): f.add('s',(.21,ty+.0),(.03,.03),hx('#FFD23F'),9)
def phone(f,ty,shirt): f.add('r',(.2,ty-.2),(.08,.13),hx('#1D2238'),9.5,outline=True); f.add('s',(.2,ty-.2),(.055,.09),hx('#55E08F'),9.8)
def bag(f,ty,shirt): f.add('r',(-.25,ty-.22),(.16,.15),hx('#8B5A3C'),9.4,outline=True)
def pen(f,ty,shirt): f.add('s',(.13,ty+.1),(.025,.09),hx('#2E86C1'),9)
cast=[
 ("Jugador",dict(skin='#D9A47A',hair='#3A2418',hair_style='side',shirt='#E07A3F',pants='#2F3E66',expr='smile',look=.0,torso=[backpack])),
 ("Kevin, vendedor",dict(skin='#EBC29A',hair='#2A1F1A',hair_style='spiky',shirt='#2E86C1',pants='#3A3F52',expr='smile',height=1.06,width=.92,torso=[tie('#E8505B'),badge,phone],look=.018)),
 ("Don Rafa, mecanico",dict(skin='#8A5A3C',hair='#2A1F1A',hair_style='buzz',shirt='#5B6E8C',pants='#3E4A63',expr='frown',width=1.14,height=.94,head=1.05,jaw='square',mustache=True,hat='cap',hat_color='#C0392B',brim=-1,torso=[overall('#3E5A86')])),
 ("Dona Marta, pulperia",dict(skin='#C98E66',hair='#9A9A9A',hair_style='bun',shirt='#D9707F',pants='#5B4B4B',expr='warm',width=1.1,height=.9,blush=True,glasses=True,torso=[apron])),
 ("Prestamista",dict(skin='#B5835A',hair='#1A1A1A',hair_style='buzz',shirt='#2D2D2D',pants='#1F2433',expr='smirk',width=1.12,hat='flatcap',hat_color='#3A3A3A',beard=True,lids=True,torso=[chain,pouch],mouth_x=.02)),
 ("Brayan, piramide",dict(skin='#E2B48C',hair='#4A2F20',hair_style='side',shirt='#F4F1E8',pants='#2B3550',expr='smile',height=1.04,width=.95,torso=[tie('#7D3C98'),watch,phone],look=.02)),
 ("Don Beto, finca",dict(skin='#A06A45',hair='#6A6A6A',hair_style='bald',shirt='#8FAF6A',pants='#5A4A3A',expr='warm',height=.98,hat='chonete',hat_color='#E8D9A8',mustache=True,shoes='#1E1E1E')),
 ("Cajera del banco",dict(skin='#C68A62',hair='#2A1F1A',hair_style='ponytail',shirt='#EAF0F8',pants='#1F3A5F',expr='neutral',skirt=True,torso=[scarf('#1D4F8C'),lanyard('#1D4F8C')])),
 ("Oficial F. Publica",dict(skin='#9C6B47',hair='#1A1A1A',hair_style='buzz',shirt='#22324F',pants='#1A2438',expr='neutral',width=1.08,hat='police',hat_color='#22324F',long_sleeve=True,torso=[police])),
 ("Jueza",dict(skin='#D7A27E',hair='#5A3A28',hair_style='bob',shirt='#1E1E26',pants='#1E1E26',expr='neutral',glasses=True,long_sleeve=True,torso=[robe])),
 ("Enfermero CCSS",dict(skin='#B07A52',hair='#2A1F1A',hair_style='curly',shirt='#7FC8B8',pants='#7FC8B8',expr='smile',torso=[pen])),
 ("Doctora",dict(skin='#F0CDB0',hair='#B0452D',hair_style='ponytail',shirt='#6FA8DC',pants='#3A3F52',expr='warm',long_sleeve=False,torso=[coat,steth])),
 ("Asesora coope",dict(skin='#8A5A3C',hair='#1A1A1A',hair_style='afro',shirt='#2E8B57',pants='#2F3E66',expr='smile',torso=[cardigan('#F1E4C8'),lanyard('#2E8B57')])),
 ("Dona Lucia, soda",dict(skin='#C98E66',hair='#3A2418',hair_style='bun',shirt='#F2C14E',pants='#7A4E3A',expr='warm',width=1.12,height=.92,blush=True,hat='beanie',hat_color='#E8505B',torso=[apron])),
 ("Don Chepe, tienda",dict(skin='#E0B08A',hair='#BDBDBD',hair_style='bald',shirt='#9E3D3D',pants='#3A3F52',expr='smile',width=1.08,glasses=True,mustache=True,torso=[cardigan('#6B5B50')])),
 ("Don Mario, vecino",dict(skin='#B5835A',hair='#555A66',hair_style='buzz',shirt='#E9E2D0',pants='#4A5A3A',expr='neutral',width=1.05,height=.97,beard=True,torso=[stripe('#C0392B')])),
 ("Financiera",dict(skin='#E7B994',hair='#D9A441',hair_style='bob',shirt='#F7F7F7',pants='#2B2B3A',expr='smirk',height=1.03,skirt=True,lids=True,torso=[cardigan('#2A7F8C'),bag],mouth_x=.02)),
 ("Agente inmobiliario",dict(skin='#9C6B47',hair='#2A1F1A',hair_style='side',shirt='#34495E',pants='#2C3E50',expr='smile',height=1.05,long_sleeve=True,torso=[tie('#F2C14E'),pen])),
 ("Estudiante UNA",dict(skin='#D9A47A',hair='#1A1A1A',hair_style='long',shirt='#C0392B',pants='#2F3E66',expr='smile',height=.97,width=.92,torso=[backpack,stripe('#F4F1E8')])),
 ("Repartidor",dict(skin='#80553A',hair='#1A1A1A',hair_style='buzz',shirt='#F39C12',pants='#2D2D2D',expr='neutral',hat='helmet',hat_color='#2D2D2D',long_sleeve=True,torso=[stripe('#2D2D2D')])),
]
mode=sys.argv[2] if len(sys.argv)>2 else "color"
K=280; Wc=330; Hc=580; cols=5; rows=4
img=Image.new("RGB",(Wc*cols,Hc*rows+70),(236,238,244)); d=ImageDraw.Draw(img)
fnt=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",22)
big=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",30)
d.text((20,18),"Colon Quest - elenco v2 (propuesta)" + (" - siluetas" if mode=='sil' else ""),fill=OUT,font=big)
for i,(n,spec) in enumerate(cast):
    cx=Wc*(i%cols)+Wc//2; cy=70+Hc*(i//cols)+330
    fig=person(spec)
    if mode=='sil': fig.items=[(o,j,kd,c,s,OUT,r) for (o,j,kd,c,s,col,r) in fig.items if o>=0]
    render(fig,img,cx,cy,K); d.text((cx,cy+200),n,fill=OUT,font=fnt,anchor="mm")
img.save(sys.argv[1])
