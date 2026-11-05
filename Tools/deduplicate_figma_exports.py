"""Share byte-identical exports, then archive unreferenced copies reversibly."""
from pathlib import Path
import hashlib
import json
import re
import shutil
import sys

ROOT=Path(__file__).resolve().parents[1]
MANIFEST=ROOT/'Tools/FigmaAssetAliases.json'

def plan():
    hashes={}
    aliases=json.loads(MANIFEST.read_text(encoding='utf-8')) if MANIFEST.exists() else {}
    # Prefer the unsuffixed filename within the Combat frame, then Gear.
    paths=sorted((ROOT/'Assets/Art/Figma').rglob('*.png'),key=lambda p:(p.parts[-2],bool(re.search(r'-\d+$',p.stem)),p.as_posix()))
    for path in paths:
        digest=hashlib.sha256(path.read_bytes()).hexdigest()
        source=path.relative_to(ROOT).as_posix()
        if digest in hashes: aliases[source]=hashes[digest]
        else: hashes[digest]=source
    MANIFEST.write_text(json.dumps(aliases,ensure_ascii=False,indent=2),encoding='utf-8')
    for state in ('Combat','Gear'):
        path=ROOT/'Assets/Resources/FlowerEscape'/(state+'_Layout.json')
        data=json.loads(path.read_text(encoding='utf-8'))
        for layer in data['layers']:
            layer['path']=aliases.get(layer['path'],layer['path'])
        path.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'{len(hashes)} unique files; {len(aliases)} alias mappings saved. Rebuild scene BEFORE archive.')

def archive():
    aliases=json.loads(MANIFEST.read_text(encoding='utf-8'))
    # Reject moves when any serialized project asset still uses the old GUID.
    serialized=[p for p in (ROOT/'Assets').rglob('*') if p.suffix in ('.unity','.prefab','.asset','.mat','.controller','.anim')]
    content={p:p.read_text(encoding='utf-8',errors='replace') for p in serialized}
    for source,target in aliases.items():
        src=ROOT/source
        dst=ROOT/target
        if not src.exists(): continue
        if hashlib.sha256(src.read_bytes()).digest()!=hashlib.sha256(dst.read_bytes()).digest():
            raise RuntimeError('Alias content mismatch: '+source)
        meta=src.with_suffix(src.suffix+'.meta')
        guid=re.search(r'^guid: (\w+)',meta.read_text(),re.M).group(1)
        references=[str(p.relative_to(ROOT)) for p,text in content.items() if guid in text]
        if references: raise RuntimeError(f'{source} still referenced by {references}')
    moved=0
    for source in aliases:
        src=ROOT/source
        if not src.exists(): continue
        destination=ROOT/'Evidence/ArchivedDuplicateExports'/Path(source).relative_to('Assets/Art/Figma')
        destination.parent.mkdir(parents=True,exist_ok=True)
        if destination.exists(): raise RuntimeError('Archive already exists: '+str(destination))
        shutil.move(str(src),str(destination))
        shutil.move(str(src.with_suffix(src.suffix+'.meta')),str(destination.with_suffix(destination.suffix+'.meta')))
        moved+=1
    print(f'{moved} unreferenced copies archived outside Assets; original bytes and GUID metadata preserved.')

if __name__=='__main__':
    {'plan':plan,'archive':archive}[sys.argv[1]]()
