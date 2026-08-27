#!/usr/bin/env bash
set -euo pipefail

TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLI_DIR="$(cd "$TEST_DIR/.." && pwd)"
REPO_ROOT="$(cd "$CLI_DIR/.." && pwd)"
PROJECT="$CLI_DIR/Emuera.ReferenceCli.csproj"
PUBLISH_DIR="$CLI_DIR/bin/smoke-win-x64"

for command_name in dotnet wine winepath python3; do
    command -v "$command_name" >/dev/null || { echo "missing command: $command_name" >&2; exit 127; }
done

# A caller may supply an existing prefix; the default never touches the baseline oracle's prefix.
export WINEPREFIX="${WINEPREFIX:-$REPO_ROOT/../.wine-prefix/emuera-selfmodified-cli}"
export WINEDEBUG="${WINEDEBUG:--all}"
export MVK_CONFIG_LOG_LEVEL=0

# Restore separately so a network failure never starts the dynamic smoke suite.
dotnet restore "$PROJECT" -p:Configuration=Debug-NAudio -p:Platform=x64 \
    -p:RuntimeIdentifiers=win-x64 -r win-x64 -p:SelfContained=true -p:NuGetAudit=false
dotnet publish "$PROJECT" -c Debug-NAudio -p:Platform=x64 -p:RuntimeIdentifiers=win-x64 \
    -r win-x64 --self-contained true -p:PublishSingleFile=false --no-restore \
    -o "$PUBLISH_DIR" --nologo
python3 -c 'import ast, pathlib, sys; ast.parse(pathlib.Path(sys.argv[1]).read_text())' "$TEST_DIR/smoke.py"
git -C "$REPO_ROOT" diff --check

if [[ ! -f "$WINEPREFIX/system.reg" ]]; then
    mkdir -p "$WINEPREFIX"
    wineboot -u
fi

exec python3 "$TEST_DIR/smoke.py" --exe "$PUBLISH_DIR/Emuera.ReferenceCli.exe" --wine wine "$@"
