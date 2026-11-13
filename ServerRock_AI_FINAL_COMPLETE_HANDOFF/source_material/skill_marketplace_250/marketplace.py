#!/usr/bin/env python3
"""Server Rock AI skill marketplace *prototype*. Python stdlib + installed Git.

This is a CLI reference implementation, not a shipping native .NET service.
Clone only GitHub HTTPS repos. Never execute imported scripts. Update only on
explicit user action; check-updates is a user-invoked network operation.
"""
import argparse
import hashlib
import json
import os
import platform
import re
import shutil
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parent
CATALOG = ROOT / 'catalog_250.json'
MAX_FILES = 1500
MAX_BYTES = 100 * 1024 * 1024


def state_dir():
    if os.getenv('SERVERROCK_SKILL_HOME'):
        return Path(os.environ['SERVERROCK_SKILL_HOME']).expanduser().resolve()
    if platform.system() == 'Windows':
        return Path(os.getenv('LOCALAPPDATA', str(Path.home()))) / 'ServerRock' / 'AI' / 'AgentSkills'
    return Path(os.getenv('XDG_DATA_HOME', str(Path.home() / '.local' / 'share'))) / 'server-rock-ai' / 'agent-skills'


def git(*args, cwd=None, timeout=180):
    p = subprocess.run(['git', '-c', 'core.hooksPath=/dev/null', *map(str,args)], cwd=cwd,
                       capture_output=True, text=True, timeout=timeout, check=False,
                       env={**os.environ, 'GIT_TERMINAL_PROMPT': '0', 'GIT_CONFIG_NOSYSTEM': '1'})
    if p.returncode:
        raise RuntimeError(f'git command failed ({p.returncode}): {p.stderr.strip()[:450]}')
    return p.stdout.strip()


def safe_ident(value):
    return bool(re.fullmatch(r'[a-zA-Z0-9_.-]{1,120}', value)) and value not in ('.', '..')


def validate_entry(entry):
    repo = entry['repo']
    owner, slash, name = repo.partition('/')
    if slash != '/' or not safe_ident(owner) or not safe_ident(name):
        raise ValueError('Invalid GitHub repository identifier')
    path = entry['path']
    if path.startswith('/') or '\\' in path or ':' in path or any(p in ('.','..','') for p in path.split('/')):
        if path != '':
            raise ValueError('Invalid skill folder')
    if len(path) > 500:
        raise ValueError('Skill folder too long')
    return entry


def catalog():
    data = json.loads(CATALOG.read_text('utf-8'))
    for e in data: validate_entry(e)
    return data


def get_entry(lookup):
    found = [r for r in catalog() if lookup in (str(r['number']), f"{r['number']:03d}", r['id'])]
    if not found: raise ValueError('Unknown skill number or ID')
    return found[0]


def clone_repo(repo, directory):
    # Fixed HTTPS host, no user-controlled URL schemes/hosts.
    git('clone', '--quiet', '--depth=1', '--filter=blob:none', '--sparse', '--',
        'https://github.com/' + repo + '.git', directory, timeout=300)
    return directory


def tree_sha(checkout, skill_path):
    revision = 'HEAD' if not skill_path else 'HEAD:' + skill_path
    result = git('rev-parse', '--verify', revision, cwd=checkout)
    kind = git('cat-file', '-t', result, cwd=checkout)
    if kind != 'tree': raise ValueError('Skill source is not a directory')
    return result


def materialize(checkout, path):
    if path: git('sparse-checkout', 'set', '--', path, cwd=checkout)
    else: git('sparse-checkout', 'disable', cwd=checkout)
    skill = checkout / path if path else checkout
    if not skill.is_dir(): raise RuntimeError('Expected skill directory not checked out')
    # Git clone includes .git; never install .git into user-skill directory.
    return skill


def file_digest(path):
    sha = hashlib.sha256()
    with open(path, 'rb') as handle:
        for data in iter(lambda: handle.read(65536), b''): sha.update(data)
    return sha.hexdigest()


def snapshot(folder):
    digest = {}
    for child in sorted(folder.rglob('*')):
        rel = child.relative_to(folder)
        if any(p == '.git' for p in rel.parts): continue
        if child.is_symlink(): raise ValueError(f'Symbolic links prohibited in skills: {rel}')
        if child.is_file(): digest[rel.as_posix()] = file_digest(child)
    return digest


def copy_safe(source, target):
    files = []
    size = 0
    for f in source.rglob('*'):
        rel = f.relative_to(source)
        if '.git' in rel.parts: continue
        if f.is_symlink(): raise ValueError(f'Upstream symlink rejected: {rel}')
        if not f.is_file() and not f.is_dir(): raise ValueError(f'Unsupported file: {rel}')
        if f.is_file():
            files.append(f)
            size += f.stat().st_size
            if len(files) > MAX_FILES or size > MAX_BYTES:
                raise ValueError('Skill exceeds 1500 files / 100 MB safety cap')
    target.mkdir(parents=True,exist_ok=False)
    for f in files:
        dest = target / f.relative_to(source)
        dest.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(f,dest)  # do not preserve executable permission
        dest.chmod(0o600)
    return snapshot(target)


def read_state():
    file=state_dir()/'installed.json'
    if not file.exists(): return {}
    return json.loads(file.read_text('utf-8'))


def write_state(data):
    base=state_dir();base.mkdir(parents=True,exist_ok=True)
    out=base/'installed.json'
    fd,temp=tempfile.mkstemp(prefix='.installed-',suffix='.json',dir=base)
    try:
        with os.fdopen(fd,'w',encoding='utf-8') as f:
            json.dump(data,f,indent=2);f.write('\n');f.flush();os.fsync(f.fileno())
        os.replace(temp,out)
    finally:
        if os.path.exists(temp): os.unlink(temp)


def install(entry, approval=False, upgrade=False):
    if not approval: raise ValueError('Explicit --approve required to download/copy a skill')
    st=read_state()
    key=entry['id']; prior=st.get(key)
    if prior and not upgrade: raise ValueError('Already installed. Run update after inspecting available changes.')
    if upgrade and not prior: raise ValueError('Not installed')
    target=state_dir()/'installed'/str(entry['number']).zfill(3)
    if prior and prior['files'] != snapshot(target):
        raise ValueError('Local files changed; back them up or resolve before update')
    with tempfile.TemporaryDirectory(prefix='srai-skills-') as td:
        checkout=Path(td)/'repo'; clone_repo(entry['repo'],checkout)
        sha=tree_sha(checkout,entry['path'])
        if prior and prior['tree_sha']==sha:
            print(json.dumps({'result':'current','skill':key,'tree_sha':sha}));return
        source=materialize(checkout,entry['path'])
        if not (source/'SKILL.md').is_file():
            raise ValueError('No SKILL.md at this path; import as a non-standard resource only after manual review')
        newdir=Path(td)/'skill'
        digests=copy_safe(source,newdir)
        if not digests or 'SKILL.md' not in digests: raise ValueError('Invalid skill payload')
        dest=target.parent;dest.mkdir(parents=True,exist_ok=True)
        staged=dest/(target.name+'.staging-'+os.urandom(4).hex())
        shutil.copytree(newdir,staged)
        backup=None
        try:
            if target.exists():
                backup=dest/(target.name+'.previous')
                if backup.exists(): raise ValueError('Previous backup exists; inspect it first')
                target.rename(backup)
            staged.rename(target)
        except Exception:
            if backup and backup.exists() and not target.exists():backup.rename(target)
            raise
        finally:
            if staged.exists():shutil.rmtree(staged)
        st[key]={ 'number':entry['number'],'name':entry['name'],'repo':entry['repo'],
                  'path':entry['path'],'tree_sha':sha,'installed_utc':datetime.now(timezone.utc).isoformat(),
                  'files':digests,'status':'installed-unreviewed','source_url':entry['url']}
        write_state(st)
    print(json.dumps({'result':'installed-unreviewed','skill':key,'tree_sha':sha,'path':str(target),'backup':str(backup) if backup else None}))


def inspect_status():
    result=[]
    for key,rec in read_state().items():
        directory=state_dir()/'installed'/str(rec['number']).zfill(3)
        local='missing' if not directory.is_dir() else ('modified' if snapshot(directory)!=rec['files'] else 'clean')
        result.append({'id':key,'name':rec['name'],'version':rec['tree_sha'],'local':local,'review':'unreviewed'})
    print(json.dumps(result,indent=2))


def updates():
    installed=read_state(); grouped={}
    for key,rec in installed.items(): grouped.setdefault(rec['repo'],[]).append((key,rec))
    out=[]
    # One Git operation per repo, not one per skill.
    for repo,records in sorted(grouped.items()):
        try:
            with tempfile.TemporaryDirectory(prefix='srai-check-') as td:
                clone=Path(td)/'repo';clone_repo(repo,clone)
                for key,rec in records:
                    try:
                        remote=tree_sha(clone,rec['path'])
                        local_dir=state_dir()/'installed'/str(rec['number']).zfill(3)
                        dirty=not local_dir.is_dir() or snapshot(local_dir)!=rec['files']
                        out.append({'id':key,'repo':repo,'status':'update-available' if remote != rec['tree_sha'] else 'up-to-date',
                                    'remote_tree_sha':remote,'installed_tree_sha':rec['tree_sha'],'locally_modified':dirty})
                    except Exception as e:out.append({'id':key,'repo':repo,'status':'check-failed','reason':str(e)[:250]})
        except Exception as e:
            for key,_ in records:out.append({'id':key,'repo':repo,'status':'check-failed','reason':str(e)[:250]})
    print(json.dumps({'checked_at':datetime.now(timezone.utc).isoformat(),'updates':out},indent=2))


def main():
    p=argparse.ArgumentParser(description='Server Rock AI marketplace GitHub prototype')
    sub=p.add_subparsers(dest='command',required=True)
    c=sub.add_parser('catalog');c.add_argument('--query',default='')
    i=sub.add_parser('install');i.add_argument('skill');i.add_argument('--approve',action='store_true')
    u=sub.add_parser('update');u.add_argument('skill');u.add_argument('--approve',action='store_true')
    sub.add_parser('status');sub.add_parser('check-updates')
    args=p.parse_args()
    try:
        if args.command=='catalog':
            q=args.query.lower(); print(json.dumps([r for r in catalog() if q in (r['name']+' '+r['description']+' '+r['repo']).lower()],indent=2))
        elif args.command in ('install','update'): install(get_entry(args.skill),args.approve,args.command=='update')
        elif args.command=='status':inspect_status()
        elif args.command=='check-updates':updates()
    except (ValueError,RuntimeError,subprocess.TimeoutExpired,OSError) as exc:
        print(json.dumps({'error':str(exc)}),file=sys.stderr);sys.exit(2)

if __name__=='__main__':main()
