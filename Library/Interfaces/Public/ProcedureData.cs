/*
 * ProcedureData.cs --
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
    /// This interface defines the identity and definition for a procedure that
    /// can be added to and dispatched by an Eagle interpreter.  It composes the
    /// unique identity (<see cref="IIdentifier" />) and the wrapper bookkeeping
    /// (<see cref="IWrapperData" />), and adds the procedure flags, formal
    /// arguments, body, and source location.
    /// </summary>
    [ObjectId("16dc0c56-ed0a-4e41-9797-3a9ae2af7e13")]
    public interface IProcedureData : IIdentifier, IWrapperData
    {
        /// <summary>
        /// Gets or sets the flags that control the behavior of this procedure.
        /// </summary>
        ProcedureFlags Flags { get; set; }
        /// <summary>
        /// Gets or sets the list of formal arguments for this procedure.
        /// </summary>
        ArgumentList Arguments { get; set; }
        /// <summary>
        /// Gets or sets the named formal arguments for this procedure, keyed by
        /// argument name.
        /// </summary>
        ArgumentDictionary NamedArguments { get; set; }
        /// <summary>
        /// Gets or sets the list of arguments used to overwrite the formal
        /// arguments of this procedure, if any.
        /// </summary>
        ArgumentList OverwriteArguments { get; set; }
        /// <summary>
        /// Gets or sets the list of arguments for this procedure with any
        /// extra metadata removed.
        /// </summary>
        ArgumentList CleanArguments { get; set; }
        /// <summary>
        /// Gets or sets the script body for this procedure.
        /// </summary>
        string Body { get; set; }
        /// <summary>
        /// Gets or sets the source location associated with the body of this
        /// procedure.
        /// </summary>
        IScriptLocation Location { get; set; }
    }
}
