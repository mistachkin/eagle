/*
 * PackageManager.cs --
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

namespace Eagle._Interfaces.Public
{
    [ObjectId("74ac785b-566a-407f-89ea-325053ca4976")]
    public interface IPackageManager
    {
        ReturnCode PresentPackage(
            string name,
            Version version,
            bool exact,
            ref Result result
            );

        ReturnCode ProvidePackage(
            string name,
            Version version,
            ref Result result
            );

        ReturnCode RequirePackage(
            string name,
            Version version,
            bool exact,
            ref Result result
            );

        ReturnCode WithdrawPackage(
            string name,
            Version version,
            ref Result result
            );
    }
}
