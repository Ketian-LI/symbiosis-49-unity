from pathlib import Path
import hashlib, json, re, sys
from urllib.parse import unquote

root = Path(__file__).resolve().parents[2]
errors = []
for group in ('Assets', 'Packages', 'ProjectSettings', 'Documentation', 'Thesis'):
    if not (root / group).is_dir(): errors.append('Missing directory: ' + group)
for relative in ('Documentation/technical/SETUP_AND_BUILD.md',
                 'Documentation/technical/ARCHITECTURE.md',
                 'Documentation/technical/GAME_RULES.md',
                 'Documentation/technical/CONTROLS.md',
                 'Documentation/technical/SAVE_FORMAT.md',
                 'Documentation/assessment/REQUIREMENTS.md',
                 'Documentation/assessment/SUBMISSION_CHECKLIST.md',
                 'Documentation/evidence/README.md',
                 'Documentation/evidence/user-testing/README.md'):
    if not (root / relative).is_file(): errors.append('Missing guide: ' + relative)
if (root / 'Docs').exists(): errors.append('Unsorted Docs directory remains')

def anchors(path):
    result, used = set(), {}
    text = path.read_text(encoding='utf-8-sig')
    text = re.sub(r'```.*?```', '', text, flags=re.S)
    for heading in re.findall(r'^#{1,6}\s+(.+)$', text, flags=re.M):
        slug = re.sub(r'[^\w\- ]', '', heading.lower()).replace(' ', '-')
        count = used.get(slug, 0)
        used[slug] = count + 1
        result.add(slug + ('-' + str(count) if count else ''))
    return result

markdown = sorted(p for p in root.rglob('*.md') if '.git' not in p.parts)
links_checked = 0
for file in markdown:
    body = re.sub(r'```.*?```', '', file.read_text(encoding='utf-8-sig'), flags=re.S)
    for target in re.findall(r'!?\[[^\]\n]*\]\((<[^>\n]+>|[^)\n]+)\)', body):
        target = target.strip('<>')
        if not target or re.match(r'\w+://|mailto:|data:', target): continue
        name, _, fragment = target.partition('#')
        resolved = (file.parent / unquote(name)).resolve() if name else file.resolve()
        links_checked += 1
        if not resolved.is_relative_to(root):
            errors.append(f'{file.relative_to(root)}: link leaves repository: {target}')
        elif not resolved.exists():
            errors.append(f'{file.relative_to(root)}: missing target: {target}')
        elif fragment and resolved.suffix.lower() == '.md' and unquote(fragment) not in anchors(resolved):
            errors.append(f'{file.relative_to(root)}: missing heading: {target}')

for file in root.rglob('*'):
    if file.is_file() and '.git' not in file.parts and file.stat().st_size > 100_000_000:
        errors.append('File exceeds 100MB: ' + str(file.relative_to(root)))
manifest = json.loads((root / 'SOURCE_MANIFEST.json').read_text(encoding='utf-8'))
for entry in manifest['files']:
    file = root / entry['path']
    if not file.exists() or hashlib.sha256(file.read_bytes()).hexdigest() != entry['sha256']:
        errors.append('Source hash mismatch: ' + entry['path'])
print(json.dumps({'markdown_files_checked': len(markdown), 'local_links_checked': links_checked,
                  'source_files_checked': len(manifest['files']), 'errors': errors}, indent=2))
sys.exit(bool(errors))
