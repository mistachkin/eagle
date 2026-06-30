/*
 * UpdateData.cs --
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
using Eagle._Components.Shared;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    //
    // NOTE: This interface is currently private; however, it may be "promoted"
    //       to public at some point.
    //
    /// <summary>
    /// This interface is implemented by entities that describe the parameters
    /// of an Eagle update operation, including the location and identity of the
    /// update, its target patch level and time stamp, and various flags that
    /// control how the update is performed.
    /// </summary>
    [ObjectId("c3510a45-bb25-46b4-ae84-f7dc15ec20aa")]
    internal interface IUpdateData : IIdentifier
    {
        /// <summary>
        /// Gets or sets the local directory that the update should target.
        /// </summary>
        string TargetDirectory { get; set; }

        /// <summary>
        /// Gets or sets the uniform resource identifier (URI) from which the
        /// update is obtained.
        /// </summary>
        Uri Uri { get; set; }

        /// <summary>
        /// Gets or sets the public key token used to verify the identity of the
        /// update.
        /// </summary>
        byte[] PublicKeyToken { get; set; }

        /// <summary>
        /// Gets or sets the culture associated with the update.
        /// </summary>
        string Culture { get; set; }

        /// <summary>
        /// Gets or sets the target patch level (version) of the update.
        /// </summary>
        Version PatchLevel { get; set; }

        /// <summary>
        /// Gets or sets the time stamp associated with the update, if any.
        /// </summary>
        DateTime? TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the type of action to be performed for the update.
        /// </summary>
        ActionType ActionType { get; set; }

        /// <summary>
        /// Gets or sets the release type associated with the update.
        /// </summary>
        ReleaseType ReleaseType { get; set; }

        /// <summary>
        /// Gets or sets the type of update to be performed.
        /// </summary>
        UpdateType UpdateType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether scripts associated with the
        /// update should be processed.
        /// </summary>
        bool WantScripts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the update should be
        /// performed quietly, suppressing non-essential output.
        /// </summary>
        bool Quiet { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user should be prompted
        /// during the update.
        /// </summary>
        bool Prompt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the update should be
        /// performed automatically, without user interaction.
        /// </summary>
        bool Automatic { get; set; }
    }
}
