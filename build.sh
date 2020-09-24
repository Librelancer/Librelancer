#!/usr/bin/env bash

# Define directories.
SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
TOOLS_DIR=$SCRIPT_DIR/tools
CAKE_EXE=$TOOLS_DIR/dotnet-cake
CAKE_PATH=$TOOLS_DIR/.store/cake.tool/$CAKE_VERSION

source $SCRIPT_DIR/build.config

# Dependency Check (Librelancer)
$SCRIPT_DIR/scripts/monocheck || { echo >&2 "ERROR: Dependency check failed."; exit 1; }

dotnet_output=$(dotnet --version 2>&1)

DOTNET_MAJOR_VERSION="$(cut -d '.' -f 1 <<< "$dotnet_output")"."$(cut -d '.' -f 2 <<< "$dotnet_output")"

# Define default arguments.
SCRIPT="build.cake"
CAKE_ARGUMENTS=()

# Parse arguments.
for i in "$@"; do
    case $1 in
        -s|--script) SCRIPT="$2"; shift ;;
        --) shift; CAKE_ARGUMENTS+=("$@"); break ;;
        *) CAKE_ARGUMENTS+=("$1") ;;
    esac
    shift
done

# Make sure the tools folder exist.
if [ ! -d "$TOOLS_DIR" ]; then
  mkdir "$TOOLS_DIR"
fi

if [ "$DOTNET_VERSION" != "$DOTNET_MAJOR_VERSION" ]; then
    echo Dotnet version is $DOTNET_MAJOR_VERSION
    echo Need $DOTNET_VERSION
    exit 2
fi

CAKE_INSTALLED_VERSION=$($CAKE_EXE --version 2>&1)

if [ "$CAKE_VERSION" != "$CAKE_INSTALLED_VERSION" ]; then
    if [ ! -f "$CAKE_EXE" ] || [ ! -d "$CAKE_PATH" ]; then
        if [ -f "$CAKE_EXE" ]; then
            dotnet tool uninstall --tool-path $TOOLS_DIR Cake.Tool
        fi

        echo "Installing Cake $CAKE_VERSION..."
        dotnet tool install --tool-path $TOOLS_DIR --version $CAKE_VERSION Cake.Tool
        if [ $? -ne 0 ]; then
            echo "An error occured while installing Cake."
            exit 1
        fi
    fi

    # Make sure that Cake has been installed.
    if [ ! -f "$CAKE_EXE" ]; then
        echo "Could not find Cake.exe at '$CAKE_EXE'."
        exit 1
    fi
fi

(exec "$CAKE_EXE" $SCRIPT --bootstrap) && (exec "$CAKE_EXE" $SCRIPT $CAKE_ARGUMENTS)
