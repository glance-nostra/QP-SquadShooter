#!/bin/bash

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


UNITY_PATH="/Applications/Unity/Hub/Editor/6000.0.30f1/Unity.app/Contents/MacOS/Unity"
LOG_DIR="$PLATFORM_PATH/logs"
UPLOAD_TARGET="cloudsmith"
CONFIG_FILE="$PLATFORM_PATH/game_config_template.json"

# Handle targets from env var or fallback
if [ -n "$TARGETS" ]; then
    IFS=',' read -ra BUILD_TARGETS <<< "$TARGETS"
else
    BUILD_TARGETS=("Android" "iOS" "WebGL" "StandaloneOSX" "StandaloneWindows64")
    echo -e "${YELLOW}No targets specified, building for all 4 default targets${NC}"
fi

# Handle profile from env var or fallback
if [ -n "$PROFILE" ]; then
    PROFILES=("$PROFILE")
    echo -e "${YELLOW}Building only for specified profile: $PROFILE${NC}"
else
    PROFILES=("Staging" "Production")
    echo -e "${YELLOW}No profile specified, building for both Staging and Production profiles${NC}"
fi

# Check if CONFIG_FILE exists
if [ ! -f "$CONFIG_FILE" ]; then
    echo -e "${RED}Error: Game config file not found at $CONFIG_FILE${NC}"
    exit 1
fi

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo -e "${RED}Error: jq is not installed. Please install it using 'brew install jq'${NC}"
    exit 1
fi

# Extract game name from config file using jq
GAME_NAME=$(jq -r '.game_name' "$CONFIG_FILE")

if [ -z "$GAME_NAME" ] || [ "$GAME_NAME" == "null" ]; then
    echo -e "${RED}Error: Could not extract game_name from config file${NC}"
    exit 1
fi

echo -e "${GREEN}Using game name: $GAME_NAME from config file${NC}"

# Create logs directory
mkdir -p "$LOG_DIR"


# Build for each target and profile combination
for BUILD_TARGET in "${BUILD_TARGETS[@]}"
do
    for CURRENT_PROFILE in "${PROFILES[@]}"
    do
        LOG_PATH="$LOG_DIR/addressables_${BUILD_TARGET}_${CURRENT_PROFILE}_build.log"
        
        echo -e "${YELLOW}Starting Addressable build for $GAME_NAME ($BUILD_TARGET) using profile $CURRENT_PROFILE...${NC}"
        UNITY_CMD=(
            "$UNITY_PATH" -batchmode -quit -projectPath "$PLATFORM_PATH"
                -executeMethod AddressableTools.BuildAddressables
                -buildTarget "$BUILD_TARGET"
                -gameName "$GAME_NAME"
                -logFile "$LOG_PATH"
                -configFile "$CONFIG_FILE"
                -profile "$CURRENT_PROFILE"
        )
        
        "${UNITY_CMD[@]}"

        # Check if build was successful
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}Addressable build for $BUILD_TARGET with profile $CURRENT_PROFILE completed successfully!${NC}"
        else
            echo -e "${RED}Addressable build for $BUILD_TARGET with profile $CURRENT_PROFILE failed! Check $LOG_PATH for details.${NC}"
            exit 1
        fi
    done
done
