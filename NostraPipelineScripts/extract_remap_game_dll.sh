#!/bin/zsh
# Script to extract DLLs from asmdef files and remap prefabs
# Usage: ./extract_remap_game_dll.sh [GAME_NAME]

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


# Validate if GameName.asmdef exists in the game directory
SPECIFIC_ASMDEF="$PROJECT_PATH/$GAME_DIR_PATH/$GAME_NAME.asmdef"

if [[ ! -f "$SPECIFIC_ASMDEF" ]]; then
  echo -e "${RED}❌ Error: $GAME_NAME.asmdef not found in $GAME_DIR_PATH${NC}"
  exit 1
else
  echo -e "${GREEN}✅ Found $GAME_NAME.asmdef${NC}"
fi

# Check if GameName.dll already exists in the game directory
SPECIFIC_DLL="$PROJECT_PATH/$GAME_DIR_PATH/$GAME_NAME.dll"
SPECIFIC_DLL_META="$SPECIFIC_DLL.meta"

if [[ -f "$SPECIFIC_DLL" ]]; then
  echo -e "${YELLOW}ℹ️ $GAME_NAME.dll already exists in $GAME_DIR_PATH${NC}"
  echo -e "${YELLOW}Removing existing DLL and its meta file before extraction...${NC}"
  
  # Remove the DLL
  rm -f "$SPECIFIC_DLL"
  
  # Remove the meta file if it exists
  if [[ -f "$SPECIFIC_DLL_META" ]]; then
    rm -f "$SPECIFIC_DLL_META"
  fi
fi

# Control whether to perform remapping (true/false)
# Set to true to both extract DLLs and remap prefabs
# Set to false to only extract DLLs without remapping

SHOULD_REMAP=true
REMAP_FLAG=""
if [[ "$SHOULD_REMAP" == "true" ]]; then
  REMAP_FLAG="-remap"
  echo "🔄 Remapping will be performed after extraction"
fi

# Execute Unity with the extract and remap tool
echo "🔄 Extracting DLLs..."
"$UNITY_PATH" \
  -batchmode \
  -projectPath "$PROJECT_PATH" \
  -executeMethod NostraTools.Editor.ExtractAndRemapDll.Main \
  -scriptsPath "$GAME_DIR_PATH" \
  $REMAP_FLAG \
  -logFile Logs/unity_dll_extraction.log \
  -quit

# Check exit code
EXIT_CODE=$?
if [[ $EXIT_CODE -eq 0 ]]; then
  echo "✅ DLL extraction completed successfully!"
else
  echo "❌ DLL extraction failed with exit code $EXIT_CODE! Skipping remapping."
  exit 1
fi

# Call Remap script if remapping is enabled
if [[ "$SHOULD_REMAP" == "true" ]]; then
  echo "🔄 Calling remap script... "
  chmod +x "$PLATFORM_PATH/NostraPipelineScripts/remap_dlls.sh"
  "$PLATFORM_PATH/NostraPipelineScripts/remap_dlls.sh" "$GAME_NAME"
else
  echo "ℹ️ Remapping is disabled, skipping remap script."
fi
