#!/bin/bash

# MentooryDb Publish Script (Bash version)
# Usage: ./publish-mentoorydb.sh [options]
# Options:
#   -c, --config <config>      Build configuration (Debug or Release, default: Debug)
#   -p, --publish              Execute publish after build
#   -f, --profile <profile>    Profile to use (Development or Production, default: Development)
#   -F, --force                Skip confirmation prompt for Production
#   -s, --generate-script      Generate deployment script instead of publishing
#   -h, --help                 Show this help message

set -e

# Default values
CONFIG="Debug"
PUBLISH=false
PROFILE="Development"
FORCE=false
GENERATE_SCRIPT=false

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

function print_help() {
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  -c, --config <config>      Build configuration (Debug or Release, default: Debug)"
    echo "  -p, --publish              Execute publish after build"
    echo "  -f, --profile <profile>    Profile to use (Development or Production, default: Development)"
    echo "  -F, --force                Skip confirmation prompt for Production"
    echo "  -s, --generate-script      Generate deployment script instead of publishing"
    echo "  -h, --help                 Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                                    # Just build Debug"
    echo "  $0 -c Release                         # Build Release"
    echo "  $0 -p                                 # Build and publish to Development"
    echo "  $0 -p -f Production -F                # Publish to Production without prompt"
    echo "  $0 -s -f Production                   # Generate deployment script for Production"
    exit 0
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--config)
            CONFIG="$2"
            shift 2
            ;;
        -p|--publish)
            PUBLISH=true
            shift
            ;;
        -f|--profile)
            PROFILE="$2"
            shift 2
            ;;
        -F|--force)
            FORCE=true
            shift
            ;;
        -s|--generate-script)
            GENERATE_SCRIPT=true
            shift
            ;;
        -h|--help)
            print_help
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            print_help
            ;;
    esac
done

# Validate configuration
if [[ "$CONFIG" != "Debug" && "$CONFIG" != "Release" ]]; then
    echo -e "${RED}Error: Configuration must be 'Debug' or 'Release'${NC}"
    exit 1
fi

# Validate profile
if [[ "$PROFILE" != "Development" && "$PROFILE" != "Production" ]]; then
    echo -e "${RED}Error: Profile must be 'Development' or 'Production'${NC}"
    exit 1
fi

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Paths
SQLPROJ_PATH="$SCRIPT_DIR/MentooryDb.sqlproj"
OUTPUT_DIR="$SCRIPT_DIR/bin/$CONFIG"

# Pick publish profile
if [[ "$PROFILE" == "Production" ]]; then
    PUBLISH_PROFILE="$SCRIPT_DIR/MentooryDb.publish.xml"
else
    PUBLISH_PROFILE="$SCRIPT_DIR/MentooryDb.Development.publish.xml"
fi

# Check if sqlproj exists
if [[ ! -f "$SQLPROJ_PATH" ]]; then
    echo -e "${RED}Error: MentooryDb.sqlproj not found at $SQLPROJ_PATH${NC}"
    exit 1
fi

# Check for dotnet
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: dotnet CLI not found. Please install .NET SDK.${NC}"
    exit 1
fi

echo -e "${CYAN}Using dotnet: $(which dotnet)${NC}"

# Check for sqlpackage if needed
if [[ "$PUBLISH" == true ]] || [[ "$GENERATE_SCRIPT" == true ]]; then
    if ! command -v sqlpackage &> /dev/null; then
        echo -e "${RED}Error: sqlpackage not found. Please install SqlPackage CLI.${NC}"
        echo -e "${YELLOW}Install with: dotnet tool install -g microsoft.sqlpackage${NC}"
        exit 1
    fi
    echo -e "${CYAN}Using SqlPackage: $(which sqlpackage)${NC}"
fi

# Step 1: Build
echo ""
echo -e "${CYAN}Building SQL project: $SQLPROJ_PATH${NC}"
dotnet build "$SQLPROJ_PATH" -c "$CONFIG"

if [[ $? -ne 0 ]]; then
    echo -e "${RED}Build failed${NC}"
    exit 1
fi

# Step 2: Locate DACPAC
DACPAC=$(find "$OUTPUT_DIR" -name "*.dacpac" -type f -printf '%T@ %p\n' 2>/dev/null | sort -rn | head -1 | cut -d' ' -f2-)

if [[ -z "$DACPAC" ]]; then
    echo -e "${RED}Error: Could not find DACPAC in $OUTPUT_DIR${NC}"
    exit 1
fi

echo -e "${GREEN}Found DACPAC: $DACPAC${NC}"

# Step 3: Generate script (optional)
if [[ "$GENERATE_SCRIPT" == true ]]; then
    TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
    SCRIPT_FILENAME="MentooryDb_${PROFILE}_${TIMESTAMP}.sql"
    SCRIPT_PATH="$SCRIPT_DIR/$SCRIPT_FILENAME"

    echo ""
    echo -e "${CYAN}Generating deployment script: $SCRIPT_FILENAME${NC}"

    sqlpackage \
        /Action:Script \
        /SourceFile:"$DACPAC" \
        /Profile:"$PUBLISH_PROFILE" \
        /OutputPath:"$SCRIPT_PATH"

    if [[ $? -eq 0 ]]; then
        echo -e "${GREEN}Script generated successfully: $SCRIPT_PATH${NC}"
    else
        echo -e "${RED}Script generation failed${NC}"
        exit 1
    fi
fi

# Step 4: Publish (optional)
if [[ "$PUBLISH" == true ]]; then
    # Production confirmation
    if [[ "$PROFILE" == "Production" ]] && [[ "$FORCE" != true ]]; then
        echo ""
        echo -e "${YELLOW}WARNING: You are about to publish to PRODUCTION using profile: $PUBLISH_PROFILE${NC}"
        read -p "Continue? (y/N): " answer
        if [[ ! "$answer" =~ ^[Yy]$ ]]; then
            echo "Publish canceled by user."
            exit 0
        fi
    fi

    echo ""
    echo -e "${CYAN}Publishing using profile: $PUBLISH_PROFILE${NC}"

    sqlpackage \
        /Action:Publish \
        /SourceFile:"$DACPAC" \
        /Profile:"$PUBLISH_PROFILE"

    if [[ $? -eq 0 ]]; then
        echo -e "${GREEN}Publish completed successfully.${NC}"
    else
        echo -e "${RED}Publish failed${NC}"
        exit 1
    fi
else
    if [[ "$GENERATE_SCRIPT" != true ]]; then
        echo ""
        echo -e "${YELLOW}Skipping publish step (use -p or --publish to enable).${NC}"
    fi
fi

echo ""
echo -e "${GREEN}Done!${NC}"