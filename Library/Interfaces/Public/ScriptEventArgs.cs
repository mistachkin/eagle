/*
 * ScriptEventArgs.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface describes the event arguments supplied to
    /// script-related notification callbacks, exposing details about the
    /// notification such as its identifier, the kinds of events and flags
    /// involved, the arguments, the result, any associated exception, and
    /// resource message information.  It extends
    /// <see cref="IGetInterpreter" /> and <see cref="IGetClientData" />.
    /// </summary>
    [ObjectId("acd98820-f016-440a-a087-4a268cba1310")]
    public interface IScriptEventArgs : IGetInterpreter, IGetClientData
    {
        /// <summary>
        /// Gets the unique identifier for this notification.
        /// </summary>
        long Id { get; }
        /// <summary>
        /// Gets the kinds of notification this event represents.
        /// </summary>
        NotifyType NotifyTypes { get; }
        /// <summary>
        /// Gets the flags associated with this notification.
        /// </summary>
        NotifyFlags NotifyFlags { get; }
        /// <summary>
        /// Gets the list of arguments associated with this notification, if
        /// any.
        /// </summary>
        ArgumentList Arguments { get; }
        /// <summary>
        /// Gets the result associated with this notification, if any.
        /// </summary>
        Result Result { get; }
        /// <summary>
        /// Gets the exception associated with this notification, if any.
        /// </summary>
        ScriptException Exception { get; }
        /// <summary>
        /// Gets the kind of interruption associated with this notification, if
        /// any.
        /// </summary>
        InterruptType InterruptType { get; }
        /// <summary>
        /// Gets the name of the resource associated with this notification, if
        /// any.
        /// </summary>
        string ResourceName { get; }
        /// <summary>
        /// Gets the array of arguments used to format the message associated
        /// with this notification, if any.
        /// </summary>
        object[] MessageArgs { get; }
    }
}
