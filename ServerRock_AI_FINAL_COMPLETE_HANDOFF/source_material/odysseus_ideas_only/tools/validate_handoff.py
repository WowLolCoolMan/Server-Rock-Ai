#!/usr/bin/env python3
import json,pathlib,sys
root=pathlib.Path(__file__).resolve().parents[1]
d=json.loads((root/'backlog/issues.json').read_text(encoding='utf-8'))
required=['README.md','00-START-HERE/EXECUTION_ORDER.md','00-START-HERE/CODEX_MASTER_PROMPT.md','architecture/AGENT_RUNTIME.md','architecture/MODELS_AND_MARKETPLACE.md','architecture/SECURITY_AND_RELEASE.md','references/ODYSSEUS_MAPPING.md','ui/01-DESIGN_SYSTEM.md','ui/02-NAVIGATION_AND_PAGES.md','ui/03-OVERLAY_FOCUS_AND_NO_HIDDEN_UI.md','ui/04-INTERACTION_MATRIX.md','ui/05-UX_VALIDATION.md','backlog/issues.json','backlog/issues.csv']
missing=[x for x in required if not (root/x).is_file()]
assert not missing,missing
assert len(d)>=35,len(d)
assert len(set(x['id'] for x in d))==len(d)
for item in d:
 for k in ['priority','title','files','symptom','fix','acceptance','confidence']:assert item[k],(item['id'],k)
print('Handoff valid:',len(d),'issues,',len(required),'required files.')
