/*
 * stubs.h --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _STUBS_H_
#define _STUBS_H_

/*
 * NOTE: Make sure we are using a modern (8.4+) version of Tcl/Tk if one is
 *       required.
 */

#if defined(_TCL)
  #if (TCL_MAJOR_VERSION > 8) || \
      ((TCL_MAJOR_VERSION == 8) && (TCL_MINOR_VERSION >= 4))
    /* Tcl 8.4+ is considered to be a "modern" version of Tcl. */
    #define HAVE_MODERN_TCL
  #else
    /* Tcl 8.3- is considered to be a "legacy" version of Tcl. */
    #undef HAVE_MODERN_TCL
  #endif
  #if defined(USE_MODERN_TCL) && !defined(HAVE_MODERN_TCL)
    #error "USE_MODERN_TCL: A modern version of Tcl/Tk is required"
  #endif
#endif

/*
 * NOTE: Make sure the version of Tcl we are being compiled against is
 *       stubs-enabled if we are.
 */

#if defined(_TCL)
  #if TCL_MAJOR_VERSION < 8
    #error "Tcl 8.0 or greater is required to build this extension"
  #else
    #if defined(USE_TCL_STUBS)
      #if (TCL_MAJOR_VERSION == 8) && (TCL_MINOR_VERSION == 0)
        #error "Tcl stubs interface does not work in 8.0"
      #else
        #if (TCL_MAJOR_VERSION == 8) && (TCL_MINOR_VERSION == 1) && \
            (TCL_RELEASE_LEVEL != TCL_FINAL_RELEASE)
          #error "Tcl stubs interface does not work in 8.1 alpha/beta"
        #endif
      #endif
    #endif
  #endif
#endif

/*
 * NOTE: Make sure the version of Tk we are being compiled against is
 *       stubs-enabled if we are.
 */

#if defined(_TK)
  #if TK_MAJOR_VERSION < 8
    #error "Tk 8.0 or greater is required to build this extension"
  #else
    #if defined(USE_TK_STUBS)
      #if (TK_MAJOR_VERSION == 8) && (TK_MINOR_VERSION == 0)
        #error "Tk stubs interface does not work in 8.0"
      #else
        #if (TK_MAJOR_VERSION == 8) && (TK_MINOR_VERSION == 1) && \
            (TK_RELEASE_LEVEL != TK_FINAL_RELEASE)
          #error "Tk stubs interface does not work in 8.1 alpha/beta"
        #endif
      #endif
    #endif
  #endif
#endif

/*
 * NOTE: Figure out the correct library suffix (currently, this is only
 *       necessary for some Windows builds of Tcl/Tk).
 */

#if defined(_TCL) && defined(WIN32)
  #if (TCL_MAJOR_VERSION > 8) || \
      ((TCL_MAJOR_VERSION == 8) && (TCL_MINOR_VERSION >= 4))
    #if !defined(USE_ACTIVESTATE_TCL)
      #if defined(_DEBUG)
        #define TCL_LIBRARY_SUFFIX "tg"
        #if defined(_TK)
          #define TK_LIBRARY_SUFFIX "tg"
        #endif
      #else
        #define TCL_LIBRARY_SUFFIX "t"
        #if defined(_TK)
          #define TK_LIBRARY_SUFFIX "t"
        #endif
      #endif
    #else
      #define TCL_LIBRARY_SUFFIX ""
      #if defined(_TK)
        #define TK_LIBRARY_SUFFIX ""
      #endif
    #endif
  #else
    #define TCL_LIBRARY_SUFFIX ""
    #if defined(_TK)
      #define TK_LIBRARY_SUFFIX ""
    #endif
  #endif
#endif

/*
 * NOTE: Define the Tcl library names suitable for use with dlopen or
 *       LoadLibrary based on the version of Tcl we are being compiled
 *       against.
 */

#if defined(_TCL) && !defined(TCL_LIBRARY_NAME) && \
    (!defined(WIN32) || defined(TCL_LIBRARY_SUFFIX))
  #if defined(WIN32)
    #define TCL_LIBRARY_NAME "tcl" \
        STRINGIFY(JOIN(TCL_MAJOR_VERSION, TCL_MINOR_VERSION)) \
        TCL_LIBRARY_SUFFIX ".dll"
  #else
    #define TCL_LIBRARY_NAME "libtcl" TCL_VERSION ".so"
  #endif
#endif

/*
 * NOTE: Define the Tk library names suitable for use with dlopen or
 *       LoadLibrary based on the version of Tk we are being compiled
 *       against.
 */

#if defined(_TK) && !defined(TK_LIBRARY_NAME) && \
    (!defined(WIN32) || defined(TK_LIBRARY_SUFFIX))
  #if defined(WIN32)
    #define TK_LIBRARY_NAME "tk" \
        STRINGIFY(JOIN(TK_MAJOR_VERSION, TK_MINOR_VERSION)) \
        TK_LIBRARY_SUFFIX ".dll"
  #else
    #define TK_LIBRARY_NAME "libtk" TK_VERSION ".so"
  #endif
#endif

/*
 * NOTE: For Microsoft Visual C++ only, emit the necessary linker comments to
 *       force us to be linked against the Tcl and Tk libraries [or Tcl and
 *       Tk stubs libraries] based on the versions of Tcl and Tk we are being
 *       compiled against.
 */

#if defined(_MSC_VER)
  #if defined(_TCL)
    #if !defined(USE_TCL_PRIVATE_STUBS)
      #if defined(USE_TCL_STUBS)
        /* Mark this object file as needing Tcl's Stubs library. */
        #pragma comment(lib, "tclstub" \
            STRINGIFY(JOIN(TCL_MAJOR_VERSION, TCL_MINOR_VERSION)) ".lib")
        #if !defined(_MT) || !defined(_DLL) || defined(_DEBUG)
          /*
           * This fixes a bug with how the Stubs library was compiled.
           * The requirement for msvcrt.lib from tclstubXX.lib should
           * be removed.
           */
          #pragma comment(linker, "-nodefaultlib:msvcrt.lib")
        #endif
      #else
        /* Mark this object file needing the Tcl import library. */
        #pragma comment(lib, "tcl" \
            STRINGIFY(JOIN(TCL_MAJOR_VERSION, TCL_MINOR_VERSION)) \
            TCL_LIBRARY_SUFFIX ".lib")
      #endif
    #endif /* !defined(USE_TCL_PRIVATE_STUBS) */
  #endif /* defined(_TCL) */
  #if defined(_TK)
    #if !defined(USE_TK_PRIVATE_STUBS)
      #if defined(USE_TK_STUBS)
        /* Mark this object file as needing Tk's Stubs library. */
        #pragma comment(lib, "tkstub" \
            STRINGIFY(JOIN(TK_MAJOR_VERSION, TK_MINOR_VERSION)) ".lib")
        #if !defined(_MT) || !defined(_DLL) || defined(_DEBUG)
          /*
           * This fixes a bug with how the Stubs library was compiled.
           * The requirement for msvcrt.lib from tkstubXX.lib should
           * be removed.
           */
          #pragma comment(linker, "-nodefaultlib:msvcrt.lib")
        #endif
      #else
        /* Mark this object file needing the Tk import library. */
        #pragma comment(lib, "tk" \
            STRINGIFY(JOIN(TK_MAJOR_VERSION, TK_MINOR_VERSION)) \
            TK_LIBRARY_SUFFIX ".lib")
      #endif
    #endif /* !defined(USE_TK_PRIVATE_STUBS) */
  #endif /* defined(_TK) */
#endif /* defined(_MSC_VER) */

/*
 * NOTE: The function types for Tcl_FindExecutable and Tcl_CreateInterp are
 *       needed when the Tcl library is being loaded dynamically by a
 *       stubs-enabled application (i.e. the inverse of using a stubs-enabled
 *       package).  These are the only Tcl API functions that MUST be called
 *       prior to being able to call Tcl_InitStubs (i.e. because it requires
 *       a Tcl interpreter).
 */

#if defined(_TCL) && defined(USE_TCL_STUBS)
typedef void (tcl_FindExecutableProc) (const char *argv0);
typedef Tcl_Interp * (tcl_CreateInterpProc) (void);
#endif /* defined(_TCL) && defined(USE_TCL_STUBS) */

#endif /* _STUBS_H_ */
