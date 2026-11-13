#!/usr/bin/env python3
"""Offline integrity check for Server Rock AI handoff; never runs app code."""
import json,zipfile,hashlib,collections,csv,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(rel): return json.loads((root/rel).read_text(encoding='utf-8'))
errors=[]
def check(cond,msg):
 if not cond: errors.append(msg)
required=['START_HERE_PREMIUM.md','SPEC_PRECEDENCE.md','AGENT_HANDOFF_PROMPT.md','00_AGENT_READ_FIRST.md','roadmap/100_STEP_ROADMAP.md','upgrade/ux/SCREEN_BLUEPRINTS.md','upgrade/architecture/AGENT_RUNTIME.md','upgrade/marketplace/catalog_250_candidates.json','upgrade/data/requirements.json','upgrade/data/routes.json','upgrade/data/event_catalog.json','upgrade/data/acceptance_cases.json']
for p in required: check((root/p).is_file(),'Missing '+p)
re=read('upgrade/data/requirements.json');routes=read('upgrade/data/routes.json');comps=read('upgrade/data/component_contracts.json');ev=read('upgrade/data/event_catalog.json');cases=read('upgrade/data/acceptance_cases.json');flows=read('upgrade/data/workflow_templates.json');catalog=read('upgrade/marketplace/catalog_250_candidates.json')
for name,items,key in [('requirements',re,'id'),('routes',routes,'id'),('components',comps,'id'),('events',ev,'type'),('cases',cases,'caseId'),('flows',flows,'id'),('skill catalog',catalog,'id')]:
 vals=[a.get(key) for a in items];check(all(vals),f'{name} missing IDs');check(len(vals)==len(set(vals)),f'{name} duplicate IDs')
check(len(catalog)==250,'Expected precisely 250 historical candidate rows')
check(all(x.get('requires_review') is True for x in catalog),'Some catalog entries lack requires_review=true')
check(len(routes)>=20,'Expected >=20 routes');check(len(re)>=75,'Expected >=75 requirements');check(len(cases)>=60,'Expected >=60 scenarios')
for f in flows:
 nodes={x['id'] for x in f['nodes']}
 check(len(nodes)==len(f['nodes']),f"Workflow {f['id']} duplicate node")
 check(all(e['from'] in nodes and e['to'] in nodes for e in f['edges']),f"Workflow {f['id']} broken edge")
 check(f.get('templateOnly') is True, f"Workflow {f['id']} must be marked proposal")
files=[p for p in root.rglob('*') if p.is_file() and p.name not in {'BUNDLE_MANIFEST.json','VERIFICATION_REPORT.md'}]
rel_paths=[p.relative_to(root).as_posix() for p in files]
check(len(rel_paths)==len(set(rel_paths)),'Duplicate normalized file paths')
check(all('..' not in Path(s).parts for s in rel_paths),'Unsafe relative paths')
if errors:
 print('FAILED offline validation:')
 for e in errors: print(' -',e)
 sys.exit(1)
print(f'PASS: {len(files)} files; {len(re)} requirements; {len(routes)} routes; {len(comps)} UI components; {len(ev)} event types; {len(cases)} cases; {len(flows)} workflow templates; {len(catalog)} historical skill candidates.')
