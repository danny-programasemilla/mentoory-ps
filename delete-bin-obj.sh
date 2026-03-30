#!/bin/bash

echo "Deleting all bin and obj folders..."

# Find and delete all bin and obj directories recursively
find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} + 2>/dev/null

echo "bin and obj folders successfully deleted!"