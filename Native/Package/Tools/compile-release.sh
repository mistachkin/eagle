#!/bin/bash

scriptdir=`dirname "$BASH_SOURCE"`
extradefs="$@"

if [[ "$OSTYPE" == "darwin"* ]]; then
  basedir="/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Host.osx-x64"
  # basedir="/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Host.osx-arm64"
else
  basedir="/usr/share/dotnet/packs/Microsoft.NETCore.App.Host.linux-x64"
fi

if [ -z "${DOTNET_SDK_VERSION:-}" ]; then
  DOTNET_SDK_VERSION="$(
    for d in "$basedir"/*/; do
      name=${d%/}; name=${name##*/}
      core=${name%%-*}
      case $core in ''|*[!0-9.]* ) continue ;; esac
      IFS=. read -r a b c x <<<"$core"
      stable=0; [[ $name != *-* ]] && stable=1
      printf '%09d%09d%09d%09d %d %s\n' "${a:-0}" "${b:-0}" "${c:-0}" "${x:-0}" "$stable" "$name"
    done | sort | tail -n1 | awk '{print $3}'
  )"
fi

if [[ "$OSTYPE" == "darwin"* ]]; then
  binsubdir=netcoreapp3.0
  libname=libGarudaCore.dylib
  # NOTE: No longer works in 10.14+
  # gccflags="-arch i386 -arch x86_64"
  gccflags="-arch x86_64"
  platlibs=""
  tcldir=-L/usr/local/opt/tcl-tk/lib
  dncdir=$basedir/$DOTNET_SDK_VERSION/runtimes/osx-x64/native
  # For Apple Silicon, use:
  # dncdir=$basedir/$DOTNET_SDK_VERSION/runtimes/osx-arm64/native
else
  binsubdir=netcoreapp3.0
  libname=libGarudaCore.so
  gccflags=""
  platlibs="-ldl"
  tcldir=-L/usr/lib/x86_64-linux-gnu
  dncdir=$basedir/$DOTNET_SDK_VERSION/runtimes/linux-x64/native
fi

if grep -q hostfxr_get_dotnet_environment_info_fn "$dncdir/hostfxr.h" 2>/dev/null; then
  dncdefs=-DHAVE_DOTNET_ENVIRONMENT_INFO=1
else
  dncdefs=""
fi

pushd "$scriptdir/../src/generic" || exit 1
tclsh ../../../Common/Tools/tagViaBuild.tcl ../.. || exit 1
gcc -g -fPIC -shared -Wl,-rpath,$dncdir $gccflags -o $libname ../external/generic/ConvertUTF_v2.c Garuda.c GarudaClr.c GarudaCoreClr.c GarudaPal.c GarudaStr.c -I. -I../external/generic -I../../Tcl/include $tcldir -I$dncdir -L$dncdir -ltclstub8.6 $platlibs -lnethost -D_GNU_SOURCE=1 -DSTDC_HEADERS=1 -D_POSIX_C_SOURCE=202405L -DHAVE_UNISTD_H=1 -DUSE_TCL_STUBS=1 -DTCL_THREADS=1 -DCORE_CLR=1 $dncdefs -DUSE_GARUDA_STR=1 -D_TRACE=1 -DNDEBUG=1 $extradefs || exit 1
mkdir -p ../../../../bin/Release$CONFIGURATION_SUFFIX/bin/$binsubdir || exit 1
mv $libname ../../../../bin/Release$CONFIGURATION_SUFFIX/bin/$binsubdir/$libname || exit 1
cp ../../lib/*.tcl ../../../../bin/Debug$CONFIGURATION_SUFFIX/bin/$binsubdir || exit 1
popd || exit 1
