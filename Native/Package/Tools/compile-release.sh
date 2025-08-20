#!/bin/bash

scriptdir=`dirname "$BASH_SOURCE"`
extradefs="$@"

if [[ "$OSTYPE" == "darwin"* ]]; then
  libname=libGaruda.dylib
  # NOTE: No longer works in 10.14+
  # gccflags="-arch i386 -arch x86_64"
  gccflags="-arch x86_64"
  platlibs=""
else
  libname=libGaruda.so
  gccflags=""
  platlibs="-ldl"
fi

pushd "$scriptdir/../src/generic" || exit 1
tclsh ../../../Common/Tools/tagViaBuild.tcl ../..
gcc -g -fPIC -shared $gccflags -o $libname ../external/generic/ConvertUTF_v2.c Garuda.c GarudaClr.c GarudaCoreClr.c GarudaPal.c -I. -I../external/generic -I../../Tcl/include -L/usr/lib/x86_64-linux-gnu -I/usr/share/dotnet/packs/Microsoft.NETCore.App.Host.linux-x64/$DOTNET_SDK_VERSION/runtimes/linux-x64/native -L/usr/share/dotnet/packs/Microsoft.NETCore.App.Host.linux-x64/$DOTNET_SDK_VERSION/runtimes/linux-x64/native -ltcl8.6 $platlibs -lnethost -D_GNU_SOURCE=1 -DSTDC_HEADERS=1 -DHAVE_UNISTD_H=1 -DTCL_THREADS=1 -DUSE_CORE_CLR=1 -DNDEBUG=1 $extradefs
mkdir -p ../../../../bin/Release$CONFIGURATION_SUFFIX/bin
mv $libname ../../../../bin/Release$CONFIGURATION_SUFFIX/bin/$libname
popd || exit 1
