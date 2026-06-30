/*
 * AliasData.cs --
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
    /// This interface defines the persistent data that describes an alias,
    /// including its flags, the source and target interpreters and
    /// namespaces, the execution target it forwards to, and any leading
    /// arguments and options associated with it.
    /// </summary>
    [ObjectId("6291616e-faba-4cf2-8f9d-6ce746adaf3b")]
    public interface IAliasData : IIdentifier, IWrapperData
    {
        /// <summary>
        /// Gets or sets the flags that control the behavior of this alias.
        /// </summary>
        AliasFlags AliasFlags { get; set; }

        /// <summary>
        /// Gets or sets the name token associated with this alias, used to
        /// track the source command name it was created from.
        /// </summary>
        string NameToken { get; set; }

        /// <summary>
        /// Gets or sets the interpreter that this alias was created in and is
        /// invoked from.
        /// </summary>
        Interpreter SourceInterpreter { get; set; } // TODO: Change this to use the IInterpreter type.

        /// <summary>
        /// Gets or sets the interpreter that contains the target entity this
        /// alias forwards its invocation to.
        /// </summary>
        Interpreter TargetInterpreter { get; set; } // TODO: Change this to use the IInterpreter type.

        /// <summary>
        /// Gets or sets the namespace, within the source interpreter, that
        /// this alias belongs to.  This value may be null.
        /// </summary>
        INamespace SourceNamespace { get; set; }

        /// <summary>
        /// Gets or sets the namespace, within the target interpreter, that
        /// contains the target entity.  This value may be null.
        /// </summary>
        INamespace TargetNamespace { get; set; }

        /// <summary>
        /// Gets or sets the entity that this alias forwards its invocation
        /// to.
        /// </summary>
        IExecute Target { get; set; }

        /// <summary>
        /// Gets or sets the list of leading arguments that are prepended to
        /// the arguments supplied at invocation time before the target is
        /// executed.  This value may be null.
        /// </summary>
        ArgumentList Arguments { get; set; }

        /// <summary>
        /// Gets or sets the options associated with this alias.  This value
        /// may be null.
        /// </summary>
        OptionDictionary Options { get; set; }

        /// <summary>
        /// Gets or sets the index, within the argument list of an invocation,
        /// where the arguments to be passed to the target begin.
        /// </summary>
        int StartIndex { get; set; }
    }
}
