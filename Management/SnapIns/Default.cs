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
    [RunInstaller(true)]
    [ObjectId("58d63269-b714-4ea3-9359-46d7ec1bb29d")]
    public sealed class Default : PSSnapIn
    {
        public Default()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public override string Description
        {
            get { return _Constants.SnapIn.Description; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override string Name
        {
            get { return _Constants.SnapIn.Name; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override string Vendor
        {
            get { return _Constants.SnapIn.Vendor; }
        }
    }
}

