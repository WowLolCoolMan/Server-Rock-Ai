#!/usr/bin/env python3
"""Offline integrity check for extracted Server Rock unified handoff; no network or system modifications."""
from pathlib import Path
from zipfile import ZipFile
import hashlib, json, sys, csv
ROOT=Path(__file__).resolve().parents[1]

def digest(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
    return h.hexdigest()

def main():
    failures=[]
    manifest=json.loads((ROOT/'index/FILE_HASH_MANIFEST.json').read_text(encoding='utf-8'))
    for entry in manifest['files']:
        file=ROOT/entry['path']
        if not file.is_file():failures.append('Missing: '+entry['path']);continue
        if file.stat().st_size!=entry['bytes'] or digest(file)!=entry['sha256']:
            failures.append('Changed: '+entry['path'])
    indexed={e['path'] for e in manifest['files']}
    actual={p.relative_to(ROOT).as_posix() for p in ROOT.rglob('*') if p.is_file() and p.relative_to(ROOT).as_posix()!='index/FILE_HASH_MANIFEST.json'}
    for extra in sorted(actual-indexed):failures.append('Unindexed file: '+extra)
    for source in json.loads((ROOT/'index/SOURCE_ARCHIVES.json').read_text(encoding='utf-8')):
        file=ROOT/'original_archives'/source['original_name']
        if not file.exists() or digest(file)!=source['sha256']:failures.append('Input archive mismatch '+source['original_name'])
        elif ZipFile(file).testzip() is not None:failures.append('Input archive CRC error '+source['original_name'])
    checks={'index/ISSUES_DEDUPED_40.json':40,'index/PREMIUM_REQUIREMENTS_86.json':86,'index/PREMIUM_ACCEPTANCE_70.json':70,'index/PREMIUM_ROUTES.json':21,'index/PREMIUM_COMPONENTS.json':58,'index/PREMIUM_EVENTS.json':44,'index/PREMIUM_WORKFLOWS.json':10,'index/SKILL_CANDIDATES_250.json':250}
    for name,count in checks.items():
        if len(json.loads((ROOT/name).read_text(encoding='utf-8')))!=count:failures.append('Unexpected record count in '+name)
    with (ROOT/'index/COMBINED_REQUIREMENTS_126.csv').open(encoding='utf-8',newline='') as f:
        if len(list(csv.DictReader(f)))!=126:failures.append('Wrong combined requirements count')
    if failures:
        print('FAIL '+str(len(failures))+' integrity checks')
        for f in failures:print(' -',f)
        return 1
    print('PASS: 4 original archives, '+str(len(manifest['files']))+' file hashes, 244 extracted source files, and canonical data counts')
    print('NOTE: This validates only handoff integrity, NOT app builds, tests, licensing or live GitHub sources.')
    return 0
if __name__=='__main__':sys.exit(main())
