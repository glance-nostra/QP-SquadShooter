#!/bin/bash
# upload_addressables_to_s3.js.sh - Wrapper script for Node.js addressables uploader

# Exit on any error
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Get project root directory
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PLATFORM_PATH="$(cd "$SCRIPT_DIR/.." && pwd)"
echo -e "${YELLOW}Project root directory: $PLATFORM_PATH${NC}"

# Check if node is installed
if ! command -v node &> /dev/null; then
    echo -e "${RED}Error: Node.js is not installed. Please install it first.${NC}"
    exit 1
fi

# Check if npm is installed
if ! command -v npm &> /dev/null; then
    echo -e "${RED}Error: npm is not installed. Please install it first.${NC}"
    exit 1
fi

# Install required dependencies if not already installed
echo -e "${YELLOW}Checking for required Node.js dependencies...${NC}"
if [ ! -d "$SCRIPT_DIR/node_modules" ]; then
    echo "Installing Node.js dependencies..."
    cd "$SCRIPT_DIR"
    npm install
    cd - > /dev/null
fi


# Set up environment variables for the Node.js script
export PR_NUMBER="${PR_NUMBER:-unknown-pr}"
export PROFILE="${PROFILE}"
export TARGETS="${TARGETS}"
export RESULTS_FILE="$RESULTS_FILE"

# Execute the Node.js script
echo -e "${YELLOW}Running addressables upload script...${NC}"
cd "$SCRIPT_DIR"
node "upload_addressables.js"

# Check if the upload was successful
if [ $? -eq 0 ]; then
    echo -e "${GREEN}Addressables upload completed successfully!${NC}"
    exit 0
else
    echo -e "${RED}Addressables upload failed. Check the logs for details.${NC}"
    exit 1
fi
