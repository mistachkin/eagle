/*
 * Module.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by entities that represent a loadable
    /// native (unmanaged) module, i.e. a dynamic-link library that has been or
    /// may be loaded into the process.  In addition to the identity
    /// (<see cref="IIdentifier" />) and wrapper bookkeeping
    /// (<see cref="IWrapperData" />) it composes, it exposes the underlying
    /// native module handle along with methods to load and unload it.
    /// </summary>
    [ObjectId("e56d5052-dcb4-48b7-bdd4-e9530427d1a6")]
    internal interface IModule : IIdentifier, IWrapperData
    {
        /// <summary>
        /// Gets the flags that describe the state and behavior of this module.
        /// </summary>
        ModuleFlags Flags { get; }

        /// <summary>
        /// Gets the file name of the native module backing this instance.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Gets the native operating-system handle for the loaded module, or
        /// <see cref="IntPtr.Zero" /> if it is not currently loaded.
        /// </summary>
        IntPtr Module { get; }

        /// <summary>
        /// Gets the number of outstanding references to this module, used to
        /// determine when it can actually be unloaded.
        /// </summary>
        int ReferenceCount { get; }

        /// <summary>
        /// This method loads the native module into the process, incrementing
        /// its reference count.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Load(ref Result error);

        /// <summary>
        /// This method loads the native module into the process, incrementing
        /// its reference count and reporting the resulting reference count.
        /// </summary>
        /// <param name="loaded">
        /// Upon success, this will contain the reference count after the load
        /// operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Load(ref int loaded, ref Result error);

        /// <summary>
        /// This method unloads the native module from the process,
        /// decrementing its reference count.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Unload(ref Result error);

        /// <summary>
        /// This method unloads the native module from the process,
        /// decrementing its reference count and reporting the resulting
        /// reference count.
        /// </summary>
        /// <param name="loaded">
        /// Upon success, this will contain the reference count after the
        /// unload operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Unload(ref int loaded, ref Result error);
    }
}
