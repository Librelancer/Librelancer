#!/usr/bin/env bash
# Configure dotnet cli
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export MSBUILDTERMINALLOGGER=off
# Workaround for redhat based systems
export OPENSSL_ENABLE_SHA1_SIGNATURES=1
# Define directories.
SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
source "$SCRIPT_DIR/build.config"

# Dependency Check (Librelancer)
"$SCRIPT_DIR/scripts/depcheck_unix" || { echo >&2 "ERROR: Dependency check failed."; exit 1; }

# Check for submodules
if [ ! -f "$SCRIPT_DIR/extern/thorncompiler/CMakeLists.txt" ]; then
    echo "WARNING: Submodules not present Attempting to clone."
    ( cd "$SCRIPT_DIR" ; git submodule update --init --recursive )
    if [ $? -ne 0 ]; then
        echo "ERROR: Submodules not present and unable to clone"
        exit 1
    fi
fi

ere_quote() {
    sed 's/[][\.|$(){}?+*^]/\\&/g' <<< "$*"
}
DOTNET_GREP="^`ere_quote $DOTNET_VERSION`"
dotnet --list-sdks | grep -E $DOTNET_GREP > /dev/null

if [ $? -ne 0 ]; then
    echo SDK Version $DOTNET_VERSION was not found
    exit 2
fi

cd "$SCRIPT_DIR"
dotnet run --project ./scripts/BuildLL/BuildLL.csproj -p:RestoreUseStaticGraphEvaluation=true -- "$@"
