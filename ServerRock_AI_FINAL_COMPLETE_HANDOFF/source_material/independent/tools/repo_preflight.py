#!/usr/bin/env python3
"""Read-only local checks for a Server Rock repository. Safe to run on a dirty worktree."""
import argparse,subprocess,pathlib,re,json,sys
p=argparse.ArgumentParser();p.add_argument('--repo',default='.');a=p.parse_args();r=pathlib.Path(a.repo).resolve()
def run(*args):
 try:return subprocess.run(args,cwd=r,text=True,capture_output=True,timeout=25).stdout.strip()
 except Exception as e:return f'[unavailable: {type(e).__name__}: {e}]'
print('Repository:',r)
print('HEAD:',run('git','rev-parse','HEAD'))
print('Branch:',run('git','branch','--show-current'))
print('Dirty state (unchanged):\n',run('git','status','--short')[:12000])
print('SDK:',run('dotnet','--version'))
paths=['scripts/audit-native-ui.cjs','packaging/native/install.ps1','packaging/native/rollback.ps1','native/SkaiDesktop.Core/Services/GitHubWorkspaceService.cs','native/ServerRock.Native/MainPage.xaml','native/ServerRock.Native/MainPage.Layout.cs','native/ServerRock.Native/MainPage.CodingAgent.Proposals.cs']
for rel in paths:
 f=r/rel
 if not f.is_file():print('MISSING',rel);continue
 s=f.read_text(encoding='utf-8',errors='replace'); print('EXISTS',rel,'lines=',len(s.splitlines()),'bytes=',f.stat().st_size)
 if rel.endswith('audit-native-ui.cjs'):
  print('Audit source uses exact includes:', '.includes(text)' in s or 'includes(text)' in s)
 if rel.endswith('install.ps1'):
  print('Copies detached executable:', 'Copy-Item -LiteralPath (Join-Path $versionRoot' in s)
 if rel.endswith('GitHubWorkspaceService.cs'):
  print('Uses direct commit call:', '["commit", "-m", message]' in s)
print('This script does not modify the repository or execute tests/builds.')
