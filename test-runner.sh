#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Clear screen
clear

echo -e "${BLUE}🧪 Auth Service Test Runner${NC}"
echo "=================================="
echo

# Test Type Selection
echo -e "${YELLOW}Select Test Type:${NC}"
echo "1) Unit Tests"
echo "2) Integration Tests"
echo "3) All Tests"
echo
read -p "Enter your choice (1-3): " test_type

case $test_type in
    1)
        TEST_PROJECT="src/tests/unit/AuthService.UnitTests.csproj"
        TEST_TYPE_NAME="Unit Tests"
        ;;
    2)
        TEST_PROJECT="src/tests/integration/AuthService.IntegrationTests.csproj"
        TEST_TYPE_NAME="Integration Tests"
        ;;
    3)
        TEST_PROJECT=""
        TEST_TYPE_NAME="All Tests"
        ;;
    *)
        echo -e "${RED}Invalid choice. Exiting.${NC}"
        exit 1
        ;;
esac

echo
echo -e "${YELLOW}Select Feature to Test:${NC}"
echo "1) System Admin (Auth, Login, Seeding)"
echo "2) Schools (Registration, Management)"
echo "3) Users (Entity, Management)"
echo "4) Database (Configuration, Seeding)"
echo "5) All Features"
echo
read -p "Enter your choice (1-5): " feature_choice

case $feature_choice in
    1)
        FILTER="SystemAdmin|Auth|Login|Seed"
        FEATURE_NAME="System Admin"
        ;;
    2)
        FILTER="School"
        FEATURE_NAME="Schools"
        ;;
    3)
        FILTER="User"
        FEATURE_NAME="Users"
        ;;
    4)
        FILTER="Database"
        FEATURE_NAME="Database"
        ;;
    5)
        FILTER=""
        FEATURE_NAME="All Features"
        ;;
    *)
        echo -e "${RED}Invalid choice. Exiting.${NC}"
        exit 1
        ;;
esac

# Build the command
echo
echo -e "${GREEN}🔨 Building project...${NC}"
dotnet build --verbosity quiet

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Build failed. Exiting.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Build successful${NC}"
echo
echo -e "${BLUE}🚀 Running $TEST_TYPE_NAME for $FEATURE_NAME...${NC}"
echo "=================================================="
echo

# Construct the test command
CMD="dotnet test"

if [ ! -z "$TEST_PROJECT" ]; then
    CMD="$CMD $TEST_PROJECT"
fi

if [ ! -z "$FILTER" ]; then
    CMD="$CMD --filter \"$FILTER\""
fi

CMD="$CMD --verbosity detailed --logger \"console;verbosity=detailed\" --no-build"

# Show the command being executed
echo -e "${YELLOW}Executing:${NC} $CMD"
echo

# Execute the command
eval $CMD

# Check result
if [ $? -eq 0 ]; then
    echo
    echo -e "${GREEN}✅ All tests passed!${NC}"
else
    echo
    echo -e "${RED}❌ Some tests failed.${NC}"
fi

echo
echo -e "${BLUE}Test execution completed.${NC}"