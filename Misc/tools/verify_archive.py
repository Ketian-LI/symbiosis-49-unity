from pathlib import Path
import hashlib, json, posixpath, re, sys
from urllib.parse import unquote

root = Path(__file__).resolve().parents[2]
errors = []
for group in ('Project', 'Documentation', 'Thesis', 'Misc'):
    if not (root / group).is_dir(): errors.append('Missing directory: ' + group)
for group in ('Assets', 'Packages', 'ProjectSettings'):
    if not (root / 'Project/04-Final' / group).is_dir(): errors.append('Missing final Unity directory: ' + group)
for group in ('Assets', 'Packages', 'ProjectSettings'):
    if (root / group).exists(): errors.append('Loose Unity directory at submission root: ' + group)
for relative in ('Project/README.md', 'Project/04-Final/README.md',
                 'Documentation/README.md',
                 'Misc/README.md',
                 'Documentation/PROJECT_STATUS.md',
                 'Misc/technical/README.md',
                 'Misc/process/README.md',
                 'Documentation/assessment/README.md',
                 'Documentation/media/README.md',
                 'Misc/technical/SCENES_AND_MODES.md',
                 'Misc/technical/SETUP_AND_BUILD.md',
                 'Misc/technical/ARCHITECTURE.md',
                 'Misc/technical/GAME_RULES.md',
                 'Misc/technical/CONTROLS.md',
                 'Misc/technical/SAVE_FORMAT.md',
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

def exact_path(file, name):
    # Windows accepts wrong letter case; GitHub links do not.
    logical = posixpath.normpath(posixpath.join(
        file.parent.relative_to(root).as_posix(), unquote(name))) if name else file.relative_to(root).as_posix()
    if logical.startswith('../') or logical.startswith('/'):
        return False
    current = root
    for part in Path(logical).parts:
        if part == '.': continue
        if not current.is_dir() or part not in {p.name for p in current.iterdir()}:
            return False
        current = current / part
    return True

markdown = sorted(p for p in root.rglob('*.md') if '.git' not in p.parts)
graph = {p.resolve(): set() for p in markdown}
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
        elif not exact_path(file, name):
            errors.append(f'{file.relative_to(root)}: wrong letter case: {target}')
        elif fragment and resolved.suffix.lower() == '.md' and unquote(fragment) not in anchors(resolved):
            errors.append(f'{file.relative_to(root)}: missing heading: {target}')
        if resolved.is_dir() and (resolved / 'README.md').is_file():
            resolved = (resolved / 'README.md').resolve()
        if resolved in graph:
            graph[file.resolve()].add(resolved)

seen, pending = set(), [(root / 'README.md').resolve()]
while pending:
    node = pending.pop()
    if node in seen: continue
    seen.add(node)
    pending.extend(graph.get(node, set()) - seen)
active = [p.resolve() for p in markdown if
          p.is_relative_to(root / 'Documentation') or
          p.is_relative_to(root / 'Thesis') or
          (p.is_relative_to(root / 'Project') and len(p.relative_to(root).parts) <= 3)]
for file in active:
    if file not in seen:
        errors.append('No reading route from README: ' + str(file.relative_to(root)))

for file in root.rglob('*'):
    if file.is_file() and '.git' not in file.parts and file.stat().st_size > 100_000_000:
        errors.append('File exceeds 100MB: ' + str(file.relative_to(root)))
manifest = json.loads((root / 'SOURCE_MANIFEST.json').read_text(encoding='utf-8'))
for entry in manifest['files']:
    file = root / entry['path']
    if not file.exists() or hashlib.sha256(file.read_bytes()).hexdigest() != entry['sha256']:
        errors.append('Source hash mismatch: ' + entry['path'])
print(json.dumps({'markdown_files_checked': len(markdown), 'local_links_checked': links_checked,
                  'active_documents_reachable': sum(p in seen for p in active),
                  'source_files_checked': len(manifest['files']), 'errors': errors}, indent=2))
sys.exit(bool(errors))
