# Machine-readable implementation inventories

- `routes.json`: 21 explicit app routes with scope/actions/states.
- `component_contracts.json`: 58 named native UI components and state requirements.
- `event_catalog.json`: 44 ordered runtime event types.
- `workflow_templates.json`: 10 proposal workflows with DAG steps. NOT executable definitions until wired and tested.
- `requirements.json` / `.csv`: 86 unique feature requirements with evidence demands. Status labels mean **unverified**, not known-absent.
- `acceptance_cases.json` / `.csv`: 70 targeted tests with expected results. None were executed by this document-generation process.
- `../marketplace/catalog_250_candidates.json`: 250 historical research candidates, no current install validation.

A runtime implementation must refine schemas and compatibility versions without blindly importing these planning records as authoritative app state.
