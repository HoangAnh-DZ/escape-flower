"""Measure individual exported sprites against the Figma reference, never slice a frame into art."""
import csv
import json
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ALIASES_PATH=ROOT/'Tools/FigmaAssetAliases.json'
ALIASES=json.loads(ALIASES_PATH.read_text(encoding='utf-8')) if ALIASES_PATH.exists() else {}

def correlation(image, kernel):
    sh = tuple(1 << (a+b-2).bit_length() for a, b in zip(image.shape, kernel.shape))
    result = np.fft.irfft2(np.fft.rfft2(image, sh) * np.fft.rfft2(kernel[::-1, ::-1], sh), sh)
    h, w = kernel.shape
    return result[h-1:image.shape[0], w-1:image.shape[1]]

def fit(state):
    reference = np.asarray(Image.open(ROOT / 'Assets/Reference/Figma' / (state+'_Reference.png')).convert('RGB'), dtype=np.float32)[:778]
    text_boxes = json.loads((ROOT/'Tools/figma_text_boxes.json').read_text())[state]
    seen = {}
    used = {}
    output = []
    with (ROOT / 'Tools' / (state+'_FigmaMeasurements.tsv')).open(encoding='utf-8') as f:
        rows = list(csv.DictReader(f, delimiter='\t'))
    for row in rows:
        name = row['name']
        ordinal = seen.get(name, 0)
        seen[name] = ordinal+1
        suffix = '' if ordinal == 0 else '-'+str(ordinal)
        asset = 'Assets/Art/Figma/'+state+'/'+name+suffix+'.png'
        asset = ALIASES.get(asset,asset)
        path = ROOT / asset
        if not path.exists():
            raise FileNotFoundError(path)
        if name in ('environment background', 'image 39', 'image 55'):
            continue
        image = Image.open(path).convert('RGBA')
        w, h = round(image.width/3), round(image.height/3)
        small = np.asarray(image.resize((w,h), Image.Resampling.LANCZOS), dtype=np.float32)
        mask = (small[:,:,3]>220).astype(np.float32)
        if mask.sum()<2:
            mask = (small[:,:,3]>max(1,small[:,:,3].max()*.7)).astype(np.float32)
        if mask.sum()<2: continue
        target = small[:,:,:3]
        score = np.zeros((reference.shape[0]-h+1,reference.shape[1]-w+1))
        for channel in range(3):
            score += correlation(reference[:,:,channel]**2, mask)
            score -= 2*correlation(reference[:,:,channel], target[:,:,channel]*mask)
            score += np.sum(target[:,:,channel]**2*mask)
        score = np.maximum(score/(mask.sum()*3),0)
        yy,xx=np.indices(score.shape)
        prior = bool(row['x'] and row['y'])
        text_box = text_boxes.get(row['id'].split('-')[-1])
        if text_box:
            px=text_box[0]+(float(row['w'])-image.width/3)/2
            py=text_box[1]+(float(row['h'])-image.height/3)/2
            score[(abs(xx-px)>8)|(abs(yy-py)>8)]=1e9
        if prior:
            px,py=float(row['x']),float(row['y'])
            # Inspector coordinates may describe a transformed box while export
            # bounds include strokes, rotated extents and transparent padding.
            radius = 48 if name=='Gear Rotor' else 15
            score[(abs(xx-px)>radius)|(abs(yy-py)>radius)]=1e9
        key=(name, w, h, image.tobytes())
        if not prior and not text_box:
            for x,y in used.get(key,[]):
                score[(abs(xx-x)<4)&(abs(yy-y)<4)]=1e9
        index=int(np.argmin(score));y,x=np.unravel_index(index,score.shape)
        error=float(score[y,x]**0.5)
        # Large uniform panels are occluded by child sprites. Their Inspector
        # bounds, rather than a color-only match, are the authoritative position.
        if prior and (w*h>18000 or name.startswith('Rectangle')):
            x,y=round(float(row['x'])),round(float(row['y']))
        if prior and name.startswith('slider gameplay_'):
            x=float(row['x'])-(image.width/3-float(row['w']))/2
            y=float(row['y'])-(image.height/3-float(row['h']))/2
        if name=='Boss1': x,y=0,214
        used.setdefault(key,[]).append((x,y))
        output.append(dict(id=row['id'],name=name,path=asset,x=float(x),y=float(y),w=image.width/3,h=image.height/3,fontSize=row['fontSize'],matchRmse=round(error,3)))
    destination=ROOT/'Assets/Resources/FlowerEscape'/ (state+'_Layout.json')
    destination.write_text(json.dumps(dict(frame=state,width=402,height=778,layers=output),ensure_ascii=False,indent=2),encoding='utf-8')
    print(state, 'layers',len(output),'high residual:',[(r['id'],r['name'],r['matchRmse']) for r in output if r['matchRmse']>25])

if __name__ == '__main__':
    fit('Combat')
    fit('Gear')
