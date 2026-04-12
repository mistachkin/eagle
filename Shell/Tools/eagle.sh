#!/usr/bin/env bash
#
# eagle.sh --
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Official Shell Wrapper Script
#
# Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
#
# See the file "license.terms" for information on usage and redistribution of
# this file, and for a DISCLAIMER OF ALL WARRANTIES.
#
# RCS: @(#) $Id: $
#
###############################################################################

set -euo pipefail

DOTNET="${DOTNET:-dotnet}"

if ! command -v "${DOTNET}" > /dev/null 2>&1; then
    echo "Error: ${DOTNET} is not installed or not in PATH." >&2
    exit 1
fi

if [[ -z "${EAGLE_DLL:-}" ]]; then
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    EAGLE_DLL="${SCRIPT_DIR}/EagleShell.dll"
fi

if [[ ! -f "${EAGLE_DLL}" ]]; then
    echo "Error: EagleShell.dll not found at: ${EAGLE_DLL}" >&2
    echo "" >&2
    echo "Set EAGLE_DLL to the full path of EagleShell.dll, e.g.:" >&2
    echo "" >&2
    echo "    EAGLE_DLL=/opt/eagle/bin/EagleShell.dll" >&2
    exit 1
fi

#
# HACK: Enable all "unsupported" script commands.
#
export InitializeFlags=+Unsupported

#
# NOTE: Use --roll-forward Major so that a .NET Core 3.0 build of
#       the Eagle shell can run on the .NET 5 and later runtimes.
#
exec "${DOTNET}" exec --roll-forward Major "${EAGLE_DLL}" "$@"
