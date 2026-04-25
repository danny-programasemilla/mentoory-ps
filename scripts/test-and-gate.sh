#!/usr/bin/env bash
# Run a test suite, then re-verify the coverage gate.
#
# `dotnet test` already runs the gate during its implicit build phase, so this
# wrapper is only useful when you've been editing test traits during a long
# iteration and want a final "everything still aligned" check after the test
# run. Pre-push hook (`.githooks/pre-push`) covers the "before push" case.
#
# Usage:
#     scripts/test-and-gate.sh                                          # all tests
#     scripts/test-and-gate.sh tests/Mentoory.Tests.E2E                 # one project
#     scripts/test-and-gate.sh tests/Mentoory.Tests.Integration --filter Identity
#
# Authority: .specify/memory/access-security-constitution.md  Section 13

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
TARGET="${1:-Mentoory.sln}"
shift || true

echo "→ Running tests: $TARGET $*"
dotnet test "$REPO_ROOT/$TARGET" --configuration Debug --logger "console;verbosity=minimal" "$@"

echo
echo "→ Re-running the coverage gate..."
dotnet build "$REPO_ROOT/tools/Mentoory.Specs.CoverageCheck" --configuration Debug --nologo --verbosity quiet
echo "✓ coverage gate clean (and tests passed)"
