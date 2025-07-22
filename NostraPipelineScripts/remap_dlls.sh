#!/bin/zsh
# Script to remap prefabs with DLLs extracted from asmdef files
# Usage: ./remap_dlls.sh [GAME_NAME]

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Error handling
set -e

# Get project root directory
PLATFORM_PATH="$(cd "$(dirname "$0")/.." && pwd)"

echo -e "${YELLOW}Project root directory: $PLATFORM_PATH${NC}"

# Check if game name is provided as argument
if [[ -z "$1" ]]; then
  echo -e "${RED}❌ Error: Game name is required in PascalCase !${NC}"
  echo -e "${YELLOW}Usage: $0 [GAME_NAME]${NC}"
  echo -e "${YELLOW}Example: $0 GameName${NC}"
  exit 1
fi

# Define game name and paths
GAME_NAME="$1"
UNITY_VERSION="6000.0.30f1"
UNITY_PATH="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
PROJECT_PATH="$PLATFORM_PATH"
GAME_DIR_PATH="Assets/Games/$GAME_NAME"

# Check if game directory exists
if [[ ! -d "$PROJECT_PATH/$GAME_DIR_PATH" ]]; then
  echo -e "${RED}❌ Error: Game directory for $GAME_NAME not found at $GAME_DIR_PATH${NC}"
  exit 1
fi

# Execute Unity with the extract and remap tool
echo "🔄 Remapping Prefabs..."
"$UNITY_PATH" \
  -batchmode \
  -projectPath "$PROJECT_PATH" \
  -executeMethod NostraTools.Editor.RemapDllUtils.Main \
  -gameName "$GAME_NAME" \
  -scriptsPath "$GAME_DIR_PATH" \
  -logFile Logs/unity_dll_extraction.log \
  -quit

# Check exit code
EXIT_CODE=$?
if [[ $EXIT_CODE -ne 0 ]]; then
  echo -e "${RED}❌ Error: Unity remapping process failed with exit code $EXIT_CODE${NC}"
  exit $EXIT_CODE
else
  echo -e "${GREEN}✅ Successfully remapped prefabs for $GAME_NAME${NC}"
fi
