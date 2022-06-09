#!/bin/bash

scriptdir=`dirname "$BASH_SOURCE"`
extradefs="$@"

if [[ "$OSTYPE" == "darwin"* ]]; then
  libname=libSpilornis.dylib
  # NOTE: No longer works in 10.14+
  # gccflags="-arch i386 -arch x86_64"
  gccflags="-arch x86_64"
else
  libname=libSpilornis.so
  gccflags=""
fi

pushd "$scriptdir/../src/generic"
tclsh ../../../Common/Tools/tagViaBuild.tcl ../..
gcc -g -fPIC -shared $gccflags -o $libname Spilornis.c -I. -DHAVE_MALLOC_H=1 -DHAVE_MALLOC_USABLE_SIZE=1 -DUSE_32BIT_SIZE_T=1 -D_DEBUG=1 $extradefs
mkdir -p ../../../../bin/Debug$CONFIGURATION_SUFFIX/bin
mv $libname ../../../../bin/Debug$CONFIGURATION_SUFFIX/bin/spilornis.dll
popd
