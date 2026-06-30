/*
 * DbConnectionParameters.cs --
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

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by objects that hold the parameters used
    /// to create a database connection, including the connection types,
    /// public key tokens, connection string, assembly file name, and value
    /// flags.  It extends <see cref="ITypeAndFullName" />.
    /// </summary>
    [ObjectId("7cc7b141-f01d-4acd-ba59-8a717680d913")]
    public interface IDbConnectionParameters : ITypeAndFullName
    {
        /// <summary>
        /// Gets or sets the primary <see cref="DbConnectionType" /> used to
        /// create the database connection.
        /// </summary>
        DbConnectionType DbConnectionType1 { get; set; }

        /// <summary>
        /// Gets or sets the secondary <see cref="DbConnectionType" /> used to
        /// create the database connection.
        /// </summary>
        DbConnectionType DbConnectionType2 { get; set; }

        /// <summary>
        /// Gets or sets the public key token of the primary assembly that
        /// provides the database connection type.
        /// </summary>
        byte[] PublicKeyToken1 { get; set; }

        /// <summary>
        /// Gets or sets the public key token of the secondary assembly that
        /// provides the database connection type.
        /// </summary>
        byte[] PublicKeyToken2 { get; set; }

        /// <summary>
        /// Gets or sets the connection string used to open the database
        /// connection.
        /// </summary>
        string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the file name of the assembly that provides the
        /// database connection type.
        /// </summary>
        string AssemblyFileName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ValueFlags" /> used when processing the
        /// connection parameter values.
        /// </summary>
        ValueFlags ValueFlags { get; set; }
    }
}
