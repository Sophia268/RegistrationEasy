#!/bin/bash

# Set project path
PROJECT_PATH="RegistrationEasy/RegistrationEasy.csproj"

echo "=========================================="
echo "Build Script for RegistrationEasy"
echo "=========================================="

# 1. Clean
echo "Cleaning project..."
dotnet clean "$PROJECT_PATH"
if [ $? -ne 0 ]; then
    echo "Clean failed!"
    exit 1
fi

# 2. Restore
echo "Restoring dependencies..."
dotnet restore "$PROJECT_PATH"
if [ $? -ne 0 ]; then
    echo "Restore failed!"
    exit 1
fi

# 3. Build
echo "Building project..."
dotnet build "$PROJECT_PATH" --no-restore
if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

echo "Build successful!"
echo "=========================================="

# 4. Run
echo "Running application..."
dotnet run --project "$PROJECT_PATH" --no-build
