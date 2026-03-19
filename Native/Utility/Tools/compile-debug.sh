#!/bin/bash

scriptdir=$(cd "$(dirname "$0")" && pwd -P)
extradefs="$@"

if [[ "$OSTYPE" == "darwin"* ]]; then
  binsubdir=netcoreapp3.0
  libname=libSpilornis.dylib
  # NOTE: No longer works in 10.14+
  # gccflags="-arch i386 -arch x86_64"
  gccflags="-arch x86_64"
else
  binsubdir=netcoreapp3.0
  libname=libSpilornis.so
  gccflags=""
fi

pushd "$scriptdir/../src/generic" || exit 1
tclsh ../../../Common/Tools/tagViaBuild.tcl ../.. || exit 1
gcc -g -fPIC -shared $gccflags -o $libname Spilornis.c -I. -DHAVE_MALLOC_H=1 -DHAVE_MALLOC_USABLE_SIZE=1 -DUSE_32BIT_SIZE_T=1 -D_DEBUG=1 $extradefs || exit 1
mkdir -p ../../../../bin/Debug$CONFIGURATION_SUFFIX/bin/$binsubdir || exit 1
mv $libname ../../../../bin/Debug$CONFIGURATION_SUFFIX/bin/$binsubdir/spilornis.dll || exit 1
popd || exit 1
