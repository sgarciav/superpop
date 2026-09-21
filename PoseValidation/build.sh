#!/usr/bin/env bash
# Reproducible batch-mode build wrapper for the PoseValidation project.
#
# Usage:
#   ./build.sh linux      # StandaloneLinux64  -> Builds/Linux/PoseValidation.x86_64
#   ./build.sh windows    # StandaloneWindows64-> Builds/Windows/PoseValidation.exe
#
# NOTE: close the Unity Editor first (a project can only be opened once).
set -euo pipefail

UNITY="${UNITY_BIN:-$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity}"
PROJECT="$(cd "$(dirname "$0")" && pwd)"

case "${1:-linux}" in
  linux)   METHOD=BuildScript.BuildLinux ;;
  windows) METHOD=BuildScript.BuildWindows ;;
  *) echo "usage: $0 [linux|windows]" >&2; exit 2 ;;
esac

echo "Building $1 with $UNITY"
"$UNITY" \
  -quit -batchmode -nographics \
  -projectPath "$PROJECT" \
  -executeMethod "$METHOD" \
  -logFile -
