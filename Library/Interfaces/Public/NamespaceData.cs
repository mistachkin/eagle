/*
 * NamespaceData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the identity and configuration of an Eagle
    /// namespace.  It combines the identification provided by
    /// <see cref="IIdentifier" /> and the interpreter association provided by
    /// <see cref="IHaveInterpreter" /> with the parent linkage, resolver,
    /// variable frame, and unknown handler used by an <see cref="INamespace" />.
    /// </summary>
    [ObjectId("e823179a-212a-4469-87c3-a72a5ed6ffa1")]
    public interface INamespaceData : IIdentifier, IHaveInterpreter
    {
        /// <summary>
        /// Gets or sets the parent namespace of this namespace.  This value
        /// may be null for the global namespace.
        /// </summary>
        INamespace Parent { get; set; }
        /// <summary>
        /// Gets or sets the resolver used to look up commands and variables
        /// for this namespace.  This value may be null.
        /// </summary>
        IResolve Resolve { get; set; }
        /// <summary>
        /// Gets or sets the call frame that holds the variables belonging to
        /// this namespace.  This value may be null.
        /// </summary>
        ICallFrame VariableFrame { get; set; }
        /// <summary>
        /// Gets or sets the name of the command used to handle unknown
        /// commands within this namespace.  This value may be null.
        /// </summary>
        string Unknown { get; set; }
    }
}
