/*
 * ObjectData.cs --
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

#if DEBUGGER && DEBUGGER_ARGUMENTS
using Eagle._Containers.Public;
#endif

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the metadata and state associated with an
    /// opaque object handle managed by an Eagle interpreter.  It composes the
    /// unique identity (<see cref="IIdentifier" />), the object flags
    /// (<see cref="IHaveObjectFlags" />), the wrapper bookkeeping
    /// (<see cref="IWrapperData" />), and the disposal state
    /// (<see cref="IMaybeDisposed" />).
    /// </summary>
    [ObjectId("a2691e49-85f6-4df3-8725-3aded340e6eb")]
    public interface IObjectData : IIdentifier, IHaveObjectFlags, IWrapperData, IMaybeDisposed
    {
        /// <summary>
        /// Gets or sets a value indicating whether this object has been
        /// disposed.
        /// </summary>
        new bool Disposed { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this object is currently
        /// being disposed.
        /// </summary>
        new bool Disposing { get; set; }

        /// <summary>
        /// Gets or sets the underlying type of the wrapped object.
        /// </summary>
        Type Type { get; set; }

        /// <summary>
        /// Gets or sets the alias, if any, associated with this object.
        /// </summary>
        IAlias Alias { get; set; }

        /// <summary>
        /// Gets or sets the current reference count for this object.
        /// </summary>
        int ReferenceCount { get; set; }
        /// <summary>
        /// Gets or sets the current temporary reference count for this object.
        /// </summary>
        int TemporaryReferenceCount { get; set; }

#if NATIVE && TCL
        /// <summary>
        /// Gets or sets the name of the native Tcl interpreter associated with
        /// this object, if any.
        /// </summary>
        string InterpName { get; set; }
#endif

#if DEBUGGER && DEBUGGER_ARGUMENTS
        /// <summary>
        /// Gets or sets the arguments used to execute the operation that
        /// produced this object, for debugging purposes.
        /// </summary>
        ArgumentList ExecuteArguments { get; set; }
#endif
    }
}
