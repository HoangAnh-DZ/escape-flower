"""Compose real reference/render comparisons; never creates runtime art."""
from pathlib import Path
from PIL import Image, ImageDraw
import json
import numpy as np

root = Path(__file__).resolve().parents[1]
evidence = root / 'Evidence' / 'ExactLayout'
metrics = {}
for state, source in [('Combat', 'Slide2_Image3.png'), ('Gear', 'Slide2_Image4.png')]:
    ref = Image.open(root / 'Assets' / 'Reference' / 'PowerPoint' / source).convert('RGB')
    # The banner at the bottom of the supplied reference is outside the game.
    ref = ref.crop((0, 0, ref.width, round(ref.height * 778 / 874)))
    paths = [evidence / (state + '_9x21.png'), evidence / (state + '_3x4.png')]
    figma = Image.open(root/'Assets/Reference/Figma'/(state+'_Reference.png')).convert('RGB').crop((0,0,402,778))
    unity = Image.open(evidence/(state+'_Unity_3x.png')).convert('RGB').resize((402,778),Image.Resampling.LANCZOS)
    delta = np.abs(np.asarray(figma,dtype=float)-np.asarray(unity,dtype=float))
    metrics[state] = {'mean_absolute_rgb_error_0_255':float(delta.mean()), 'fraction_pixels_max_channel_error_at_most_16':float((delta.max(axis=2)<=16).mean()), 'note':'Rendering comparison, not a percentage of requirements completed.'}
    unity.save(evidence/(state+'_Unity_3x_Downsample.png'))
    images = [ref,figma,unity] + [Image.open(p).convert('RGB') for p in paths]
    labels = ['PowerPoint (game area)','Figma 402 x 778','Unity (3x, downsampled)', 'Unity 900 x 2100', 'Unity 900 x 1200']
    canvas = Image.new('RGB', (1600, 820), '#242a31')
    draw = ImageDraw.Draw(canvas)
    for index, (im, label) in enumerate(zip(images, labels)):
        im.thumbnail((300, 750), Image.Resampling.LANCZOS)
        canvas.paste(im, (index * 320 + (320-im.width)//2, 40))
        draw.text((index*320+10, 12), label, fill='white')
    canvas.save(evidence / (state + '_Comparison.png'))
(evidence/'ComparisonMetrics.json').write_text(json.dumps(metrics,indent=2),encoding='utf-8')
for p in sorted(evidence.glob('*.png')):
    with Image.open(p) as im:
        print(f'{p.name}: {im.width} x {im.height}')
