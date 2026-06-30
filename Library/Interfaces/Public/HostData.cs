/*
 * HostData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Resources;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the data used when creating and configuring a
    /// host.  It composes a unique identity (<see cref="IIdentifier" />), an
    /// associated interpreter (<see cref="IHaveInterpreter" />), and a type and
    /// name (<see cref="ITypeAndName" />), and supplies the resource manager,
    /// profile, and creation flags that the host uses.
    /// </summary>
    [ObjectId("35cfe935-a23e-48ce-a395-2dab9c268c2f")]
    public interface IHostData : IIdentifier, IHaveInterpreter, ITypeAndName
    {
        /// <summary>
        /// Gets or sets the resource manager used by the host to look up
        /// localized strings and other resources.
        /// </summary>
        ResourceManager ResourceManager { get; set; }
        /// <summary>
        /// Gets or sets the name of the profile used to load and persist the
        /// host's saved settings.
        /// </summary>
        string Profile { get; set; }
        /// <summary>
        /// Gets or sets the flags used to create and configure the host.
        /// </summary>
        HostCreateFlags HostCreateFlags { get; set; }
    }
}
