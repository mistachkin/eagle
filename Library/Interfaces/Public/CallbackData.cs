/*
 * CallbackData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if EMIT
using System.Reflection;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface holds the data that describes a managed callback bridge
    /// between the Eagle engine and a native delegate.  It composes a unique
    /// identity (<see cref="IIdentifier" />) and object flags
    /// (<see cref="IHaveObjectFlags" />), and exposes the marshalling
    /// configuration, the source and target methods, the delegate types, and
    /// the well-known delegate views used to fire the callback.
    /// </summary>
    [ObjectId("8a93df11-d442-4a53-b079-f63e7b7f854e")]
    public interface ICallbackData : IIdentifier, IHaveObjectFlags
    {
        /// <summary>
        /// Gets the flags that control how arguments and the return value are
        /// marshalled when the callback is invoked.
        /// </summary>
        MarshalFlags MarshalFlags { get; }
        /// <summary>
        /// Gets or sets the flags that control the behavior of the callback.
        /// </summary>
        CallbackFlags CallbackFlags { get; set; }
        /// <summary>
        /// Gets the flags that control how by-reference arguments are handled
        /// when the callback is invoked.
        /// </summary>
        ByRefArgumentFlags ByRefArgumentFlags { get; }
        /// <summary>
        /// Gets the list of extra arguments to be prepended when the callback
        /// is invoked.
        /// </summary>
        StringList Arguments { get; }

#if EMIT
        /// <summary>
        /// Gets the original method that the callback was created from, prior
        /// to any modification.
        /// </summary>
        MethodBase OldMethod { get; }
        /// <summary>
        /// Gets the modified method that the callback actually invokes.
        /// </summary>
        MethodBase NewMethod { get; }
#endif

        /// <summary>
        /// Gets the delegate that the callback is bound to.
        /// </summary>
        Delegate Delegate { get; }

        /// <summary>
        /// Gets the original delegate type that the callback was created from.
        /// </summary>
        Type OriginalDelegateType { get; }
        /// <summary>
        /// Gets the modified delegate type that the callback actually uses.
        /// </summary>
        Type ModifiedDelegateType { get; }
        /// <summary>
        /// Gets the list of parameter names for the callback.
        /// </summary>
        StringList ParameterNames { get; }
        /// <summary>
        /// Gets the return type of the callback.
        /// </summary>
        Type ReturnType { get; }
        /// <summary>
        /// Gets the list of parameter types for the callback.
        /// </summary>
        TypeList ParameterTypes { get; }

        /// <summary>
        /// Gets the callback represented as a <see cref="System.AsyncCallback" />
        /// delegate.
        /// </summary>
        AsyncCallback AsyncCallback { get; }
        /// <summary>
        /// Gets the callback represented as an <see cref="System.EventHandler" />
        /// delegate.
        /// </summary>
        EventHandler EventHandler { get; }
        /// <summary>
        /// Gets the callback represented as a
        /// <see cref="System.Threading.ThreadStart" /> delegate.
        /// </summary>
        ThreadStart ThreadStart { get; }
        /// <summary>
        /// Gets the callback represented as a
        /// <see cref="System.Threading.ParameterizedThreadStart" /> delegate.
        /// </summary>
        ParameterizedThreadStart ParameterizedThreadStart { get; }
    }
}
