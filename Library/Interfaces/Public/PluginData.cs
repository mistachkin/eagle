/*
 * PluginData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Resources;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the identity and metadata for a plugin that
    /// can be loaded into and managed by an Eagle interpreter.  It composes
    /// the unique identity (<see cref="IIdentifier" />), the wrapper
    /// bookkeeping (<see cref="IWrapperData" />), and the type and name
    /// information (<see cref="ITypeAndName" />).  The remaining members
    /// describe the backing assembly, where it was loaded from, and the
    /// commands, policies, and tokens it contributes.
    /// </summary>
    [ObjectId("822714c8-a9c5-4118-888e-c561c96cbb3e")]
    public interface IPluginData : IIdentifier, IWrapperData, ITypeAndName
    {
        //
        // NOTE: The flags for this plugin.
        //
        /// <summary>
        /// Gets or sets the flags that control the behavior of this plugin.
        /// </summary>
        PluginFlags Flags { get; set; }

        //
        // NOTE: The version of the assembly containing this plugin.
        //
        /// <summary>
        /// Gets or sets the version of the assembly that contains this
        /// plugin.
        /// </summary>
        Version Version { get; set; }

        //
        // NOTE: The URL this plugin was loaded from, if any.
        //
        /// <summary>
        /// Gets or sets the URI this plugin was loaded from, if any.  This
        /// value may be null.
        /// </summary>
        Uri Uri { get; set; }

        //
        // NOTE: The URL this plugin should use to check for updates, if any.
        //
        /// <summary>
        /// Gets or sets the URI this plugin should use to check for updates,
        /// if any.  This value may be null.
        /// </summary>
        Uri UpdateUri { get; set; }

        //
        // NOTE: The application domain containing this plugin.
        //
        /// <summary>
        /// Gets or sets the application domain that contains this plugin.
        /// </summary>
        AppDomain AppDomain { get; set; }

        //
        // NOTE: The assembly containing this plugin.
        //
        /// <summary>
        /// Gets or sets the assembly that contains this plugin.
        /// </summary>
        Assembly Assembly { get; set; }

        //
        // NOTE: The name of the assembly containing this plugin.
        //
        /// <summary>
        /// Gets or sets the name of the assembly that contains this plugin.
        /// </summary>
        AssemblyName AssemblyName { get; set; }

        //
        // NOTE: The date and time when the associated assembly was compiled.
        //
        /// <summary>
        /// Gets or sets the date and time when the associated assembly was
        /// compiled, if known.  This value may be null.
        /// </summary>
        DateTime? DateTime { get; set; }

        //
        // NOTE: The full [local] path and file name of the assembly containing
        //       this plugin, if any.
        //
        /// <summary>
        /// Gets or sets the full local path and file name of the assembly
        /// that contains this plugin, if any.  This value may be null.
        /// </summary>
        string FileName { get; set; }

        //
        // NOTE: The initial list of command data for this plugin, if any.
        //
        /// <summary>
        /// Gets or sets the initial list of command data contributed by this
        /// plugin, if any.  This value may be null.
        /// </summary>
        CommandDataList Commands { get; set; }

        //
        // NOTE: The initial list of policy data for this plugin, if any.
        //
        /// <summary>
        /// Gets or sets the initial list of policy data contributed by this
        /// plugin, if any.  This value may be null.
        /// </summary>
        PolicyDataList Policies { get; set; }

        //
        // NOTE: The initial list of command tokens for this plugin, if any.
        //
        /// <summary>
        /// Gets or sets the initial list of command tokens for this plugin,
        /// if any.  This value may be null.
        /// </summary>
        LongList CommandTokens { get; set; }

        //
        // NOTE: The initial list of function tokens for this plugin, if any.
        //
        /// <summary>
        /// Gets or sets the initial list of function tokens for this plugin,
        /// if any.  This value may be null.
        /// </summary>
        LongList FunctionTokens { get; set; }

        //
        // NOTE: The initial list of policy tokens for this plugin, if any.
        //
        /// <summary>
        /// Gets or sets the initial list of policy tokens for this plugin,
        /// if any.  This value may be null.
        /// </summary>
        LongList PolicyTokens { get; set; }

        //
        // NOTE: The initial list of trace tokens for this plugin, if any.
        //
        /// <summary>
        /// Gets or sets the initial list of trace tokens for this plugin, if
        /// any.  This value may be null.
        /// </summary>
        LongList TraceTokens { get; set; }

        //
        // NOTE: The resource manager for this plugin, if any.
        //
        /// <summary>
        /// Gets or sets the resource manager for this plugin, if any.  This
        /// value may be null.
        /// </summary>
        ResourceManager ResourceManager { get; set; }

        //
        // NOTE: The extra data for this plugin, if any.
        //
        /// <summary>
        /// Gets or sets the extra, plugin-specific data for this plugin, if
        /// any.  This value may be null.
        /// </summary>
        ObjectDictionary AuxiliaryData { get; set; }
    }
}
