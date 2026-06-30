/*
 * Default.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.ComponentModel;
using System.Management.Automation;
using Eagle._Attributes;

namespace Eagle._SnapIns
{
    /// <summary>
    /// This class implements the default PowerShell snap-in used to register
    /// the Eagle cmdlets with the PowerShell runtime.  It supplies the
    /// identifying metadata (name, vendor, and description) for the snap-in.
    /// </summary>
    [RunInstaller(true)]
    [ObjectId("58d63269-b714-4ea3-9359-46d7ec1bb29d")]
    public sealed class Default : PSSnapIn
    {
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        public Default()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the human-readable description of this snap-in.
        /// </summary>
        public override string Description
        {
            get { return _Constants.SnapIn.Description; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the name used to identify this snap-in.
        /// </summary>
        public override string Name
        {
            get { return _Constants.SnapIn.Name; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the name of the vendor that produced this snap-in.
        /// </summary>
        public override string Vendor
        {
            get { return _Constants.SnapIn.Vendor; }
        }
    }
}
