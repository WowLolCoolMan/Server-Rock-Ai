# Backlog conventions

`issues.json` is authoritative and `issues.csv` is for spreadsheet/task tools. `confirmed-code` means inspected code evidence; it does not mean runtime reproduction. `confirmed-CI` means observed workflow data; other qualifiers such as `risk-to-test`/`planned-feature` must not be presented as reproduced failures. Every acceptance condition must be demonstrated on the current checkout. A fix is done only when tests and user-visible behavior are correct, not when a code pattern merely appears.
