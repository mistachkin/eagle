/*
 * SizeHost.cs --
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
    /// This interface is implemented by hosts that support querying and
    /// changing the size of their input/output buffer and/or window.
    /// </summary>
    [ObjectId("9d10e1ab-e22b-4f51-acd7-4328dbc42889")]
    public interface ISizeHost : IInteractiveHost
    {
        /// <summary>
        /// Resets the size of the specified host buffer and/or window to its
        /// default.
        /// </summary>
        /// <param name="hostSizeType">
        /// The <see cref="HostSizeType" /> value indicating which size should
        /// be reset.
        /// </param>
        /// <returns>
        /// True if the size was reset successfully; otherwise, false.
        /// </returns>
        bool ResetSize(HostSizeType hostSizeType);
        /// <summary>
        /// Queries the size of the specified host buffer and/or window.
        /// </summary>
        /// <param name="hostSizeType">
        /// The <see cref="HostSizeType" /> value indicating which size should
        /// be queried.
        /// </param>
        /// <param name="width">
        /// Upon success, this contains the width, in characters.
        /// </param>
        /// <param name="height">
        /// Upon success, this contains the height, in characters.
        /// </param>
        /// <returns>
        /// True if the size was queried successfully; otherwise, false.
        /// </returns>
        bool GetSize(HostSizeType hostSizeType, ref int width, ref int height);
        /// <summary>
        /// Changes the size of the specified host buffer and/or window.
        /// </summary>
        /// <param name="hostSizeType">
        /// The <see cref="HostSizeType" /> value indicating which size should
        /// be changed.
        /// </param>
        /// <param name="width">
        /// The new width, in characters.
        /// </param>
        /// <param name="height">
        /// The new height, in characters.
        /// </param>
        /// <returns>
        /// True if the size was changed successfully; otherwise, false.
        /// </returns>
        bool SetSize(HostSizeType hostSizeType, int width, int height);
    }
}
