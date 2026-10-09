#!/usr/bin/env bash
# Codex 데스크톱 앱에 들어 있는 codex CLI 실행 (업데이트마다 bin/<해시> 폴더가 바뀌어서 최신 것을 찾음)
C=$(ls -t "$HOME"/AppData/Local/OpenAI/Codex/bin/*/codex.exe /c/Users/*/AppData/Local/OpenAI/Codex/bin/*/codex.exe 2>/dev/null | head -1)
[ -z "$C" ] && { echo "codex.exe 를 찾지 못함" >&2; exit 1; }
exec "$C" "$@"
