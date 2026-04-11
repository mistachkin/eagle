###############################################################################
#
# Makefile --
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Official Makefile for POSIX
#
# Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
#
# See the file "license.terms" for information on usage and redistribution of
# this file, and for a DISCLAIMER OF ALL WARRANTIES.
#
# RCS: @(#) $Id: $
#
###############################################################################

.POSIX:
.SUFFIXES:

#
# Build configuration variables.
# Override via the command line or environment, e.g.:
#
#     make BUILD_MANAGED_CONFIGURATION=Release build
#

DOTNET = dotnet

BUILD_TYPE = NetStandard21
BUILD_MANAGED_CONFIGURATION = Debug
BUILD_NATIVE_CONFIGURATION = debug
BUILD_DIRECTORY = bin/$(BUILD_MANAGED_CONFIGURATION)$(BUILD_TYPE)/bin/netcoreapp3.0
BUILD_SOLUTION = EagleNetStandard2X.sln

BUILD_ARGS = /maxcpucount:1 \
		   /property:EagleBuildType=$(BUILD_TYPE) \
		   /property:EaglePatchLevel=false

SHELL_DLL = $(BUILD_DIRECTORY)/EagleShell.dll
LIBRARY_DLL = $(BUILD_DIRECTORY)/Eagle.dll

# -----------------------------------------------------------------------------
#
# NOTE: All action targets depend on FORCE to ensure they always run, even on
#       case-insensitive file systems where directory names like "Build" and
#       "Test" would otherwise shadow the corresponding make targets.
#
# -----------------------------------------------------------------------------

all: build

build: build-managed build-native

build-managed: FORCE
	$(DOTNET) build /target:Build "/property:Configuration=$(BUILD_MANAGED_CONFIGURATION)" "$(BUILD_SOLUTION)" $(BUILD_ARGS)

build-native: FORCE
	cd Native/Utility/Tools && CONFIGURATION_SUFFIX=NetStandard21 ./compile-$(BUILD_NATIVE_CONFIGURATION).sh
	cd Native/Package/Tools && CONFIGURATION_SUFFIX=NetStandard21 ./compile-$(BUILD_NATIVE_CONFIGURATION).sh

rebuild: rebuild-native rebuild-managed

rebuild-managed: FORCE
	$(DOTNET) build /target:Rebuild "/property:Configuration=$(BUILD_MANAGED_CONFIGURATION)" "$(BUILD_SOLUTION)" $(BUILD_ARGS)

rebuild-native: force-clean build-native

clean: FORCE
	$(DOTNET) build /target:Clean "/property:Configuration=$(BUILD_MANAGED_CONFIGURATION)" "$(BUILD_SOLUTION)" $(BUILD_ARGS)

force-clean: FORCE
	rm -rf Library/obj
	rm -rf Sample/obj
	rm -rf Shell/obj
	rm -rf bin
	rm -rf obj

fresh: force-clean rebuild

restore: FORCE
	$(DOTNET) restore "$(BUILD_SOLUTION)"

run: FORCE
	$(DOTNET) exec --roll-forward Major "$(SHELL_DLL)"

test: FORCE
	$(DOTNET) exec --roll-forward Major "$(SHELL_DLL)" -file "Library/Tests/all.eagle"

help: FORCE
	@echo "Available targets:"
	@echo ""
	@echo "  all             - Build all projects (default)."
	@echo "  build           - Build all projects."
	@echo "  build-managed   - Build managed projects only."
	@echo "  build-native    - Build native projects only."
	@echo "  clean           - Clean via the .NET build system."
	@echo "  force-clean     - Forcibly remove all output directories."
	@echo "  fresh           - Clean then rebuild."
	@echo "  rebuild         - Forcibly rebuild all projects."
	@echo "  rebuild-managed - Rebuild managed projects only."
	@echo "  rebuild-native  - Rebuild native projects only."
	@echo "  restore         - Restore NuGet packages."
	@echo "  run             - Launch the interactive shell."
	@echo "  test            - Run the test suite."
	@echo "  help            - Show this message."
	@echo ""

FORCE:
