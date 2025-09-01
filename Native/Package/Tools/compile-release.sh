#!/bin/bash

scriptdir=`dirname "$BASH_SOURCE"`
extradefs="$@"

if [[ "$OSTYPE" == "darwin"* ]]; then
  libname=libGarudaCore.dylib
  # NOTE: No longer works in 10.14+
  # gccflags="-arch i386 -arch x86_64"
  gccflags="-arch x86_64"
  platlibs=""
  dncdir=""
else
  libname=libGarudaCore.so
  gccflags=""
  platlibs="-ldl"
  dncdir=/usr/share/dotnet/packs/Microsoft.NETCore.App.Host.linux-x64/$DOTNET_SDK_VERSION/runtimes/linux-x64/native
fi

pushd "$scriptdir/../src/generic" || exit 1
tclsh ../../../Common/Tools/tagViaBuild.tcl ../..
gcc -g -fPIC -shared -Wl,-rpath,$dncdir $gccflags -o $libname ../external/generic/ConvertUTF_v2.c Garuda.c GarudaClr.c GarudaCoreClr.c GarudaPal.c GarudaStr.c -I. -I../external/generic -I../../Tcl/include -L/usr/lib/x86_64-linux-gnu -I$dncdir -L$dncdir -ltclstub8.6 $platlibs -lnethost -D_GNU_SOURCE=1 -DSTDC_HEADERS=1 -DHAVE_UNISTD_H=1 -DUSE_TCL_STUBS=1 -DTCL_THREADS=1 -DUSE_CORE_CLR=1 -DUSE_GARUDA_STR=1 -D_TRACE=1 -DNDEBUG=1 $extradefs
mkdir -p ../../../../bin/Release$CONFIGURATION_SUFFIX/bin
mv $libname ../../../../bin/Release$CONFIGURATION_SUFFIX/bin/$libname
cp ../../lib/*.tcl ../../../../bin/Debug$CONFIGURATION_SUFFIX/bin
popd || exit 1
