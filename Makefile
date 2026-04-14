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

# =============================================================================
#                      Latest Installed .NET SDK Version
# =============================================================================

#
# The DOTNET_SDK_VERSION environment variable is intentionally not set here.
#
# Please uncomment the following line, set it in your environment, or pass
# it via the make command line.  Without the DOTNET_SDK_VERSION environment
# variable set to the correct version, the native .NET libraries cannot be
# successfully built and will be always be skipped.
#
# DOTNET_SDK_VERSION = 10.0.5

# =============================================================================
#                     Installation Configuration Variables
# =============================================================================

DESTDIR =
PREFIX = /opt/eagle

# =============================================================================
#                         Fetch Configuration Variables
# =============================================================================

GIT = git
GIT_DOCS_URI = https://github.com/mistachkin/docs
GIT_EXTRA_URI = https://github.com/mistachkin/extra

# =============================================================================
#                         Build Configuration Variables
# =============================================================================

INSTALL = install
INSTALL_BIN = $(INSTALL) -m 755
INSTALL_DATA = $(INSTALL) -m 644

MKDIR_P = mkdir -p
CP_R = cp -R

CHMOD = chmod
CHMOD_R = $(CHMOD) -R
CHMOD_PERMS = a+rX,og-w

DOTNET = dotnet
DOTNET_ARGS = --roll-forward Major

# -----------------------------------------------------------------------------

BUILD_MANAGED_CONFIGURATION = Debug
BUILD_NATIVE_CONFIGURATION = debug
BUILD_TYPE = NetStandard21
BUILD_NET_DIRECTORY = netcoreapp3.0
BUILD_SOLUTION_1 = EagleEnterpriseNetStandard2X.sln
BUILD_SOLUTION_2 = EagleNetStandard2X.sln

# -----------------------------------------------------------------------------

BUILD_SUB_DIRECTORY = $(BUILD_MANAGED_CONFIGURATION)$(BUILD_TYPE)
BUILD_DIRECTORY = bin/$(BUILD_SUB_DIRECTORY)/bin/$(BUILD_NET_DIRECTORY)

BUILD_ARGS = \
    /maxcpucount:1 \
    /property:EagleBuildType=$(BUILD_TYPE) \
    /property:EaglePatchLevel=false

# -----------------------------------------------------------------------------

RUNTIMECONFIG_JSON_NAME = EagleShell.runtimeconfig.json
SHELL_DLL_NAME = EagleShell.dll
SHELL_SH_NAME = eagle.sh
SHELL_DLL_ARGS = -anyFile Makefile.eagle
LIBRARY_DLL_NAME = Eagle.dll
TEST_ARGS =

# -----------------------------------------------------------------------------

RUNTIMECONFIG_JSON_PATH = $(BUILD_DIRECTORY)/$(RUNTIMECONFIG_JSON_NAME)
SHELL_DLL_PATH = $(BUILD_DIRECTORY)/$(SHELL_DLL_NAME)
SHELL_SH_PATH = Shell/Tools/$(SHELL_SH_NAME)
LIBRARY_DLL_PATH = $(BUILD_DIRECTORY)/$(LIBRARY_DLL_NAME)

# =============================================================================
#                          Disable Microsoft Telemetry
#                                     and
#                           Provide .NET SDK Version
# =============================================================================

VSCMD_SKIP_SENDTELEMETRY = 1
VCPKG_KEEP_ENV_VARS = VSCMD_SKIP_SENDTELEMETRY
VCPKG_DISABLE_METRICS = 1
DOTNET_CLI_TELEMETRY_OPTOUT = 1
DOTNET_SCAFFOLD_TELEMETRY_OPTOUT = 1

DOTNET_ENV = \
    DOTNET_SDK_VERSION=$(DOTNET_SDK_VERSION) \
    VSCMD_SKIP_SENDTELEMETRY=$(VSCMD_SKIP_SENDTELEMETRY) \
    VCPKG_KEEP_ENV_VARS=$(VCPKG_KEEP_ENV_VARS) \
    VCPKG_DISABLE_METRICS=$(VCPKG_DISABLE_METRICS) \
    DOTNET_CLI_TELEMETRY_OPTOUT=$(DOTNET_CLI_TELEMETRY_OPTOUT) \
    DOTNET_SCAFFOLD_TELEMETRY_OPTOUT=$(DOTNET_SCAFFOLD_TELEMETRY_OPTOUT)

# =============================================================================
#                               Default Targets
# =============================================================================

all: build

# =============================================================================
#                                Shared Targets
# =============================================================================

validate-git: FORCE
	@if ! $(GIT) --version > /dev/null 2>&1; then \
	    echo "ERROR: $(GIT) is not installed or not working properly."; \
	    exit 1; \
	fi

# -----------------------------------------------------------------------------

validate-dotnet: FORCE
	@if ! $(DOTNET_ENV) $(DOTNET) --info > /dev/null 2>&1; then \
	    echo "ERROR: $(DOTNET) is not installed or not working properly."; \
	    exit 1; \
	fi

# -----------------------------------------------------------------------------

validate-dirs: FORCE
	@if [ -z "$(PREFIX)" ]; then \
	    echo "ERROR: PREFIX must not be empty."; \
	    exit 1; \
	fi
	@case "$(PREFIX)" in \
	    /*) ;; \
	    *) echo "ERROR: PREFIX must be an absolute path: $(PREFIX)"; exit 1 ;; \
	esac
	@if [ -n "$(DESTDIR)" ]; then \
	    case "$(DESTDIR)" in \
	        /*) ;; \
	        *) echo "ERROR: DESTDIR must be empty or an absolute path: $(DESTDIR)"; exit 1 ;; \
	    esac \
	fi

# =============================================================================
#                                 NuGet Targets
# =============================================================================

restore: validate-dotnet
	$(DOTNET_ENV) $(DOTNET) restore "$(BUILD_SOLUTION_1)" || \
	$(DOTNET_ENV) $(DOTNET) restore "$(BUILD_SOLUTION_2)"

# =============================================================================
#                                Build Targets
# =============================================================================

build-managed: validate-dotnet
	$(DOTNET_ENV) $(DOTNET) build /target:Build "/property:Configuration=$(BUILD_MANAGED_CONFIGURATION)" "$(BUILD_SOLUTION_1)" $(BUILD_ARGS) || \
	$(DOTNET_ENV) $(DOTNET) build /target:Build "/property:Configuration=$(BUILD_MANAGED_CONFIGURATION)" "$(BUILD_SOLUTION_2)" $(BUILD_ARGS)

# -----------------------------------------------------------------------------

build-native: FORCE
	@if [ -z "$$DOTNET_SDK_VERSION" ]; then \
	    echo "Skipping build-native: DOTNET_SDK_VERSION is not set."; \
	else \
	    _SAVED_BUILD_NATIVE_PWD="$$PWD" && cd Native/Utility/Tools && \
	    $(DOTNET_ENV) CONFIGURATION_SUFFIX=NetStandard21 ./compile-$(BUILD_NATIVE_CONFIGURATION).sh && \
	    cd "$$_SAVED_BUILD_NATIVE_PWD" && cd Native/Package/Tools && \
	    $(DOTNET_ENV) CONFIGURATION_SUFFIX=NetStandard21 ./compile-$(BUILD_NATIVE_CONFIGURATION).sh; \
	fi

# -----------------------------------------------------------------------------

rebuild-native: force-clean build-native

# -----------------------------------------------------------------------------

rebuild-managed: validate-dotnet
	$(DOTNET_ENV) $(DOTNET) build /target:Rebuild "/property:Configuration=$(BUILD_MANAGED_CONFIGURATION)" "$(BUILD_SOLUTION_1)" $(BUILD_ARGS) || \
	$(DOTNET_ENV) $(DOTNET) build /target:Rebuild "/property:Configuration=$(BUILD_MANAGED_CONFIGURATION)" "$(BUILD_SOLUTION_2)" $(BUILD_ARGS)

# -----------------------------------------------------------------------------

# HACK: Always rebuild native libraries first, to force-clean.
rebuild: rebuild-native rebuild-managed

build: build-native build-managed

fresh: force-clean build

# =============================================================================
#                                Clean Targets
# =============================================================================

clean: validate-dotnet
	$(DOTNET_ENV) $(DOTNET) build /target:Clean "/property:Configuration=$(BUILD_MANAGED_CONFIGURATION)" "$(BUILD_SOLUTION_1)" $(BUILD_ARGS) || \
	$(DOTNET_ENV) $(DOTNET) build /target:Clean "/property:Configuration=$(BUILD_MANAGED_CONFIGURATION)" "$(BUILD_SOLUTION_2)" $(BUILD_ARGS)

# -----------------------------------------------------------------------------

force-clean: FORCE
	-rm -rf Library/obj
	-rm -rf Native/Package/src/generic/libGarudaCore.dylib.dSYM
	-rm -rf Native/Utility/src/generic/libSpilornis.dylib.dSYM
	-rm -rf Sample/obj
	-rm -rf Service/bin
	-rm -rf Service/obj
	-rm -f Service/Web.config
	-rm -rf Shell/obj
	-rm -rf bin
	-rm -rf obj

# -----------------------------------------------------------------------------

dist-clean: force-clean

distclean: dist-clean

# =============================================================================
#                                 Test Targets
# =============================================================================

run: validate-dotnet
	$(DOTNET_ENV) $(DOTNET) exec $(DOTNET_ARGS) "$(SHELL_DLL_PATH)" $(SHELL_DLL_ARGS)

# -----------------------------------------------------------------------------

test: validate-dotnet
	$(DOTNET_ENV) $(DOTNET) exec $(DOTNET_ARGS) "$(SHELL_DLL_PATH)" $(SHELL_DLL_ARGS) -file "Library/Tests/all.eagle" $(TEST_ARGS)

# -----------------------------------------------------------------------------

shell: run

check: test

# =============================================================================
#                               Git Fetch Targets
# =============================================================================

fetch-docs: validate-git validate-dirs
	$(GIT) clone "$(GIT_DOCS_URI)" "$(DESTDIR)$(PREFIX)/docs/"

# -----------------------------------------------------------------------------

fetch-extra: validate-git validate-dirs
	$(GIT) clone "$(GIT_EXTRA_URI)" "$(DESTDIR)$(PREFIX)/lib/Extra1.0"

# -----------------------------------------------------------------------------

unfetch-docs: validate-dirs
	-rm -rf "$(DESTDIR)$(PREFIX)/docs/"
	-rmdir "$(DESTDIR)$(PREFIX)"

# -----------------------------------------------------------------------------

unfetch-extra: validate-dirs
	-rm -rf "$(DESTDIR)$(PREFIX)/lib/Extra1.0/"
	-rmdir "$(DESTDIR)$(PREFIX)/lib"

# -----------------------------------------------------------------------------

fetch: fetch-docs fetch-extra

unfetch: unfetch-docs unfetch-extra

# =============================================================================
#                                Install Targets
# =============================================================================

install-dirs: validate-dirs
	$(MKDIR_P) "$(DESTDIR)$(PREFIX)/bin"
	$(MKDIR_P) "$(DESTDIR)$(PREFIX)/lib/Eagle1.0"
	$(MKDIR_P) "$(DESTDIR)$(PREFIX)/lib/Test1.0"
	$(MKDIR_P) "$(DESTDIR)$(PREFIX)/Tests"

# HACK: Creating these directories will break the "git clone" targets.
install-all-dirs: install-dirs
	$(MKDIR_P) "$(DESTDIR)$(PREFIX)/docs"
	$(MKDIR_P) "$(DESTDIR)$(PREFIX)/lib/Extra1.0"

# -----------------------------------------------------------------------------

install-bin: validate-dirs
	$(MKDIR_P) "$(DESTDIR)$(PREFIX)/bin"
	$(INSTALL_DATA) "$(RUNTIMECONFIG_JSON_PATH)" "$(DESTDIR)$(PREFIX)/bin/"
	$(INSTALL_DATA) "$(SHELL_DLL_PATH)" "$(DESTDIR)$(PREFIX)/bin/"
	$(INSTALL_DATA) "$(LIBRARY_DLL_PATH)" "$(DESTDIR)$(PREFIX)/bin/"
	$(INSTALL_BIN) "$(SHELL_SH_PATH)" "$(DESTDIR)$(PREFIX)/bin/"

# -----------------------------------------------------------------------------

install-lib: validate-dirs
	$(MKDIR_P) "$(DESTDIR)$(PREFIX)/lib/Eagle1.0"
	$(CP_R) lib/Eagle1.0/. "$(DESTDIR)$(PREFIX)/lib/Eagle1.0/"
	$(CHMOD_R) $(CHMOD_PERMS) "$(DESTDIR)$(PREFIX)/lib/Eagle1.0/"
	$(MKDIR_P) "$(DESTDIR)$(PREFIX)/lib/Test1.0"
	$(CP_R) lib/Test1.0/. "$(DESTDIR)$(PREFIX)/lib/Test1.0/"
	$(CHMOD_R) $(CHMOD_PERMS) "$(DESTDIR)$(PREFIX)/lib/Test1.0/"

# -----------------------------------------------------------------------------

install-tests: validate-dirs
	$(MKDIR_P) "$(DESTDIR)$(PREFIX)/Tests"
	$(CP_R) Library/Tests/. "$(DESTDIR)$(PREFIX)/Tests/"
	-rm -f "$(DESTDIR)$(PREFIX)/Tests/Default.cs"
	$(CHMOD_R) $(CHMOD_PERMS) "$(DESTDIR)$(PREFIX)/Tests/"

# -----------------------------------------------------------------------------

uninstall-bin: validate-dirs
	-rm -f "$(DESTDIR)$(PREFIX)/bin/$(RUNTIMECONFIG_JSON_NAME)"
	-rm -f "$(DESTDIR)$(PREFIX)/bin/$(SHELL_DLL_NAME)"
	-rm -f "$(DESTDIR)$(PREFIX)/bin/$(LIBRARY_DLL_NAME)"
	-rm -f "$(DESTDIR)$(PREFIX)/bin/$(SHELL_SH_NAME)"
	-rmdir "$(DESTDIR)$(PREFIX)/bin"

# -----------------------------------------------------------------------------

uninstall-lib: validate-dirs
	-rm -rf "$(DESTDIR)$(PREFIX)/lib/Eagle1.0/"
	-rm -rf "$(DESTDIR)$(PREFIX)/lib/Test1.0/"
	-rmdir "$(DESTDIR)$(PREFIX)/lib"

# -----------------------------------------------------------------------------

uninstall-tests: validate-dirs
	-rm -rf "$(DESTDIR)$(PREFIX)/Tests/"

# -----------------------------------------------------------------------------

uninstall: validate-dirs uninstall-bin uninstall-lib uninstall-tests
	-rmdir "$(DESTDIR)$(PREFIX)"

# -----------------------------------------------------------------------------

installdirs: install-dirs

install: build install-bin install-lib install-tests

install-all: fetch install

uninstall-all: unfetch uninstall

# =============================================================================
#                                 Help Targets
# =============================================================================

help: FORCE
	@echo "Available and fully supported targets (others may exist):"
	@echo ""
	@echo "  all              - Build all projects (default)."
	@echo ""
	@echo "  validate-dirs    - REQUIRED: Validate path(s) \"$(DESTDIR)$(PREFIX)\"."
	@echo "  validate-dotnet  - REQUIRED: Does the .NET runtime appear to be working?"
	@echo "  validate-git     - OPTIONAL: Does the installed Git appear to be working?"
	@echo ""
	@echo "  build            - Build all projects."
	@echo "  build-managed    - Build managed projects only."
	@echo "  build-native     - Build native projects only."
	@echo "  fresh            - Forcibly clean and then rebuild."
	@echo ""
	@echo "  clean            - Clean via the .NET build system."
	@echo "  force-clean      - Forcibly remove output directories."
	@echo "  dist-clean       - Also forcibly remove output directories."
	@echo "  distclean        - Alias for \"dist-clean\"."
	@echo ""
	@echo "  rebuild          - Forcibly rebuild all projects."
	@echo "  rebuild-managed  - Rebuild managed projects only."
	@echo "  rebuild-native   - Rebuild native projects only."
	@echo ""
	@echo "  restore          - Restore NuGet packages."
	@echo ""
	@echo "  run              - Run the interactive shell."
	@echo "  shell            - Alias for \"run\"."
	@echo ""
	@echo "  test             - Run the test suite."
	@echo "  check            - Alias for \"test\"."
	@echo ""
	@echo "  install          - Install files to \"$(DESTDIR)$(PREFIX)\"."
	@echo "  install-all      - Install everything, including remote extras."
	@echo ""
	@echo "  uninstall        - Uninstall files from \"$(DESTDIR)$(PREFIX)\"."
	@echo "  uninstall-all    - Uninstall everything, including remote extras."
	@echo ""
	@echo "  fetch            - Install docs and extras."
	@echo "  unfetch          - Uninstall docs and extras."
	@echo "  fetch-docs       - Install docs from \"$(GIT_DOCS_URI)\"."
	@echo "  fetch-extra      - Install extras from \"$(GIT_EXTRA_URI)\"."
	@echo ""
	@echo "  install-dirs     - Create directories in \"$(DESTDIR)$(PREFIX)\"."
	@echo "  install-all-dirs - Create \"install-dirs\" and extras directories."
	@echo "  installdirs      - Alias for \"install-dirs\"."
	@echo ""
	@echo "  help             - Show this message."
	@echo ""

FORCE:
