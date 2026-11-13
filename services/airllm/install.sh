#!/usr/bin/env bash
set -euo pipefail
environment_path="${1:-${XDG_DATA_HOME:-$HOME/.local/share}/skai/airllm}"
python_bin="$(command -v python3 || command -v python)"
mkdir -p "$environment_path"
venv="$environment_path/venv"
if [[ ! -x "$venv/bin/python" ]]; then "$python_bin" -m venv "$venv"; fi
"$venv/bin/python" -m pip install --upgrade pip
"$venv/bin/python" -m pip install -r "$(dirname "$0")/requirements.txt"
echo "Skai AirLLM runtime installed. Set AirLLM Python executable to: $venv/bin/python"
