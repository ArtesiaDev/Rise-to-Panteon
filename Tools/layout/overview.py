import sys
from PIL import Image
names=["abyss_f1","abyss_f2","abyss_f3","catacombs_f1","catacombs_f2","catacombs_f3","catacombs_f4"]
d=sys.argv[1]
ims=[Image.open(f"{d}/{n}.png") for n in names]
w=max(i.width for i in ims); h=max(i.height for i in ims)
sheet=Image.new("RGB",(w*4,h*2),(18,18,22))
for k,im in enumerate(ims):
    r,c=(0,k) if k<3 else (1,k-3)
    sheet.paste(im,(c*w,r*h))
sheet.save(f"{d}/overview.png")
