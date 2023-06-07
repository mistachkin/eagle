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
    [ObjectId("822714c8-a9c5-4118-888e-c561c96cbb3e")]
    public interface IPluginData : IIdentifier, IWrapperData, ITypeAndName
    {
        //
        // NOTE: The flags for this plugin.
        //
        PluginFlags Flags { get; set; }

        //
        // NOTE: The version of the assembly containing this plugin.
        //
        Version Version { get; set; }

        //
        // NOTE: The URL this plugin was loaded from, if any.
        //
        Uri Uri { get; set; }

        //
        // NOTE: The URL this plugin should use to check for updates, if any.
        //
        Uri UpdateUri { get; set; }

        //
        // NOTE: The application domain containing this plugin.
        //
        AppDomain AppDomain { get; set; }

        //
        // NOTE: The assembly containing this plugin.
        //
        Assembly Assembly { get; set; }

        //
        // NOTE: The name of the assembly containing this plugin.
        //
        AssemblyName AssemblyName { get; set; }

        //
        // NOTE: The date and time when the associated assembly was compiled.
        //
        DateTime? DateTime { get; set; }

        //
        // NOTE: The full [local] path and file name of the assembly containing
        //       this plugin, if any.
        //
        string FileName { get; set; }

        //
        // NOTE: The initial list of command data for this plugin, if any.
        //
        CommandDataList Commands { get; set; }

        //
        // NOTE: The initial list of policy data for this plugin, if any.
        //
        PolicyDataList Policies { get; set; }

        //
        // NOTE: The initial list of command tokens for this plugin, if any.
        //
        LongList CommandTokens { get; set; }

        //
        // NOTE: The initial list of function tokens for this plugin, if any.
        //
        LongList FunctionTokens { get; set; }

        //
        // NOTE: The initial list of policy tokens for this plugin, if any.
        //
        LongList PolicyTokens { get; set; }

        //
        // NOTE: The initial list of trace tokens for this plugin, if any.
        //
        LongList TraceTokens { get; set; }

        //
        // NOTE: The resource manager for this plugin, if any.
        //
        ResourceManager ResourceManager { get; set; }

        //
        // NOTE: The extra data for this plugin, if any.
        //
        ObjectDictionary AuxiliaryData { get; set; }
    }
}
