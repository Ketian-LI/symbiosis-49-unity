from pathlib import Path
import hashlib,json,re,sys
from urllib.parse import unquote
root=Path(__file__).resolve().parents[2]
errors=[]
for group in ['Assets','Packages','ProjectSettings','Documentation','Thesis']:
    if not (root/group).is_dir():errors.append('Missing directory: '+group)
markdown=[root/'README.md',root/'Thesis/README.md']+list((root/'Documentation').rglob('*.md'))
for file in markdown:
    for target in re.findall(r'!?\[[^\]]*\]\(([^)]+)\)',file.read_text(encoding='utf-8')):
        target=target.strip('<>').split('#')[0]
        if not target or '://' in target or target.startswith('mailto:'):continue
        if not (file.parent/unquote(target)).resolve().exists():errors.append(str(file.relative_to(root))+': '+target)
for file in root.rglob('*'):
    if not file.is_file() or '.git' in file.relative_to(root).parts:continue
    if file.stat().st_size>100_000_000:errors.append('File exceeds 100MB: '+str(file.relative_to(root)))
manifest=json.loads((root/'SOURCE_MANIFEST.json').read_text(encoding='utf-8'))
for entry in manifest['files']:
    file=root/entry['path']
    if not file.exists() or hashlib.sha256(file.read_bytes()).hexdigest()!=entry['sha256']:
        errors.append('Source hash mismatch: '+entry['path'])
print(json.dumps({'source_files_checked':len(manifest['files']),'errors':errors},indent=2))
sys.exit(bool(errors))
