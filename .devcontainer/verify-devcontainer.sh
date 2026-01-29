#!/bin/bash

# Script to verify all tools/features from devcontainer.json
# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Dev Container Tool Verification Script${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Counter for passed/failed checks
PASSED=0
FAILED=0
CHECKS=()

# Function to check command availability and version
check_tool() {
    local tool_name=$1
    local command=$2
    local version_flag=${3:-"--version"}
    
    echo -e "${YELLOW}Checking ${tool_name}...${NC}"
    
    if command -v "$command" &> /dev/null; then
        version_output=$($command $version_flag 2>&1 | head -n 5)
        echo -e "${GREEN}✓ ${tool_name} is installed${NC}"
        echo -e "  Version info: $(echo "$version_output" | head -n 1)"
        echo ""
        PASSED=$((PASSED + 1))
        CHECKS+=("${GREEN}✓${NC} $tool_name")
        return 0
    else
        echo -e "${RED}✗ ${tool_name} is NOT installed${NC}"
        echo ""
        FAILED=$((FAILED + 1))
        CHECKS+=("${RED}✗${NC} $tool_name")
        return 1
    fi
}

# Function to check Bicep (part of Azure CLI)
check_bicep() {
    echo -e "${YELLOW}Checking Bicep (Azure CLI feature)...${NC}"
    
    if command -v az &> /dev/null && az bicep version &> /dev/null; then
        version_output=$(az bicep version 2>&1)
        echo -e "${GREEN}✓ Bicep is installed${NC}"
        echo -e "  Version info: $version_output"
        echo ""
        PASSED=$((PASSED + 1))
        CHECKS+=("${GREEN}✓${NC} Bicep")
        return 0
    else
        echo -e "${RED}✗ Bicep is NOT installed${NC}"
        echo ""
        FAILED=$((FAILED + 1))
        CHECKS+=("${RED}✗${NC} Bicep")
        return 1
    fi
}

# Check .NET SDK (base image)
check_tool ".NET SDK" "dotnet" "--version"

# Check Azure CLI (feature) - Extract version without warnings
echo -e "${YELLOW}Checking Azure CLI...${NC}"
if command -v az &> /dev/null; then
    version_line=$(az version 2>/dev/null | grep -oP '"azure-cli":\s*"\K[^"]+' || az --version 2>&1 | grep -oP 'azure-cli\s+\K[0-9.]+' | head -n 1)
    echo -e "${GREEN}✓ Azure CLI is installed${NC}"
    echo -e "  Version info: azure-cli $version_line"
    echo ""
    PASSED=$((PASSED + 1))
    CHECKS+=("${GREEN}✓${NC} Azure CLI")
else
    echo -e "${RED}✗ Azure CLI is NOT installed${NC}"
    echo ""
    FAILED=$((FAILED + 1))
    CHECKS+=("${RED}✗${NC} Azure CLI")
fi

# Check Bicep (Azure CLI feature with installBicep: true)
check_bicep

# Check PowerShell (feature)
check_tool "PowerShell" "pwsh" "--version"

# Check Git (feature)
check_tool "Git" "git" "--version"

# Check GitHub CLI (feature)
check_tool "GitHub CLI" "gh" "--version"

# Summary
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Summary${NC}"
echo -e "${BLUE}========================================${NC}"

for check in "${CHECKS[@]}"; do
    echo -e "  $check"
done

echo ""
echo -e "Total: ${GREEN}${PASSED} passed${NC}, ${RED}${FAILED} failed${NC}"
echo ""

# Exit with appropriate status
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}All tools are installed successfully!${NC}"
    exit 0
else
    echo -e "${RED}Some tools are missing. Please check the devcontainer configuration.${NC}"
    exit 1
fi
