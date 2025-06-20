#!/bin/bash

# Error handling
set -e

# Get project root directory
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PLATFORM_PATH="$(cd "$SCRIPT_DIR/.." && pwd)"
# echo -e "${YELLOW}Project root directory: $PLATFORM_PATH${NC}"
# Configuration
CLOUDSMITH_API_KEY=$1
CLOUDSMITH_ORG=$2
CLOUDSMITH_REPO=$3
PROFILE=$4
GAME_NAME=$5
shift 5  # Remove first four parameters (API key, org, repo, game name)
BUILD_TARGETS=("$@")  # Rest of parameters are build targets

# ===== Function Definitions =====

# Verify API access
function verify_cloudsmith_access() {
    echo -e "Verifying Cloudsmith access..."
    if ! curl -s -f -H "X-Api-Key: $CLOUDSMITH_API_KEY" "https://api.cloudsmith.io/v1/repos/$CLOUDSMITH_ORG/$CLOUDSMITH_REPO/" > /dev/null; then
        echo -e "${RED}Failed to access Cloudsmith repository. Please check your API key and permissions.${NC}"
        exit 1
    fi
}

# Upload build target to Cloudsmith
function upload_to_cloudsmith() {
    BUILD_TARGET=$1
    SOURCE_DIR="$PLATFORM_PATH/ServerData/$PROFILE/$BUILD_TARGET"


    if [ ! -d "$SOURCE_DIR" ]; then
        echo -e "${RED}Source directory $SOURCE_DIR not found!${NC}"
        return 1
    fi

    echo -e "\n${YELLOW}Uploading $BUILD_TARGET build...${NC}"
    
    # Create a temporary zip file for the build target
    ZIP_FILE="/tmp/${GAME_NAME}_${BUILD_TARGET}_${PROFILE}.zip"
    echo -e "Creating zip file: $ZIP_FILE"
    zip -r "$ZIP_FILE" "$SOURCE_DIR" > /dev/null

    # Upload to Cloudsmith
    echo -e "Uploading to Cloudsmith..."
    echo -e "Repository: https://app.cloudsmith.com/$CLOUDSMITH_ORG/$CLOUDSMITH_REPO"
    echo -e "Package Name: ${GAME_NAME}_${BUILD_TARGET}"
    echo -e "Version: $NEXT_VERSION"
    echo -e "File Path: addressables/$GAME_NAME/$PROFILE/$BUILD_TARGET/$(basename "$ZIP_FILE")"
    
    if cloudsmith push raw "$CLOUDSMITH_ORG/$CLOUDSMITH_REPO" "$ZIP_FILE" \
        --name="${GAME_NAME}_${BUILD_TARGET}" \
        --version="$NEXT_VERSION" \
        --description="Addressables build for $GAME_NAME ($BUILD_TARGET) - $PROFILE" \
        --tags="${GAME_NAME},${BUILD_TARGET},${PROFILE}" \
        --api-key "$CLOUDSMITH_API_KEY"; then
        echo -e "${GREEN}Upload successful for $BUILD_TARGET${NC}"
        echo -e "View package at: https://app.cloudsmith.com/$CLOUDSMITH_ORG/$CLOUDSMITH_REPO/packages/"
    else
        echo -e "${RED}Upload failed for $BUILD_TARGET${NC}"
        rm -f "$ZIP_FILE"
        return 1
    fi

    # Clean up zip file
    rm -f "$ZIP_FILE"
    return 0
}

function get_latest_addressables_version_from_cloudsmith() {
    PACKAGE_NAME="${GAME_NAME}_${BUILD_TARGET}"

    # Query Cloudsmith API (latest 25 results, sorted by version)
    API_URL="https://api.cloudsmith.io/v1/packages/$CLOUDSMITH_ORG/$CLOUDSMITH_REPO/?page=1&page_size=25&query=$PACKAGE_NAME&ordering=-version"

    API_RESPONSE=$(curl -s -H "X-Api-Key: $CLOUDSMITH_API_KEY" "$API_URL")
    # echo -e "API Response: $API_URL"

    # Extract version matching the PROFILE prefix
    VERSION=$(echo "$API_RESPONSE" | grep -o '"version": *"'"$PROFILE"'-[^"]*' | head -n1 | cut -d'"' -f4)

    if [ -z "$VERSION" ]; then
        echo "No previous $PROFILE version found. Using $PROFILE-1.0.0"
        NEXT_VERSION="${PROFILE}-1.0.0"
    else
        echo "Latest version found in cloudsmith: $VERSION"

        # Extract the numeric part: 1.0.5 from Production-1.0.5
        NUM_VERSION="${VERSION#${PROFILE}-}"

        IFS='.' read -r MAJOR MINOR PATCH <<< "$NUM_VERSION"
        PATCH=$((PATCH + 1))
        NEXT_VERSION="${PROFILE}-${MAJOR}.${MINOR}.${PATCH}"
    fi

    echo "Next version: $NEXT_VERSION"
}

# Upload all build targets
function upload_all_build_targets() {
    echo -e "Using profile: ${YELLOW}$PROFILE${NC}"
    echo -e "Using repository: ${YELLOW}$CLOUDSMITH_ORG/$CLOUDSMITH_REPO${NC}"
    echo -e "Build targets: ${YELLOW}${BUILD_TARGETS[*]}${NC}"
    echo -e "Using version: ${YELLOW}$NEXT_VERSION${NC}"
    
    # Upload for each build target
    for BUILD_TARGET in "${BUILD_TARGETS[@]}"; do
        if ! upload_to_cloudsmith "$BUILD_TARGET"; then
            echo -e "${RED}Upload failed for $BUILD_TARGET. Exiting.${NC}"
            exit 1
        fi
    done
    
    echo -e "${GREEN}All uploads completed successfully!${NC}"
}

# Parse command line arguments
function parse_arguments() {
    # Check required parameters
    if [ -z "$1" ]; then
        echo -e "${RED}Error: Missing required parameters${NC}"
        exit 1
    fi

    # Check if any build targets were provided
    if [ ${#BUILD_TARGETS[@]} -eq 0 ]; then
        echo -e "${RED}Error: No build targets provided${NC}"
        exit 1
    fi

}

# Check required tools and install if missing
function check_required_tools() {
    # Check if Cloudsmith CLI is installed
    if ! command -v cloudsmith &> /dev/null; then
        echo -e "${YELLOW}Cloudsmith CLI is not installed. Installing now...${NC}"
        pip3 install --user --upgrade cloudsmith-cli
        
        # Verify installation was successful
        if ! command -v cloudsmith &> /dev/null; then
            echo -e "${RED}Failed to install Cloudsmith CLI. Please install it manually using: pip3 install --user --upgrade cloudsmith-cli${NC}"
            exit 1
        else
            echo -e "${GREEN}Successfully installed Cloudsmith CLI${NC}"
        fi
    else
        echo -e "${GREEN}Cloudsmith CLI is already installed${NC}"
    fi
}

# ===== Main Execution =====

parse_arguments "$@"
check_required_tools
verify_cloudsmith_access
get_latest_addressables_version_from_cloudsmith
upload_all_build_targets

echo -e "\n${GREEN}All uploads to Cloudsmith completed successfully and version updated!${NC}"
