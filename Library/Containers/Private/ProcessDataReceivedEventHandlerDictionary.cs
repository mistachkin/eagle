/*
 * ProcessDataReceivedEventHandlerDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Diagnostics;
using Eagle._Attributes;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps process instances to the
    /// data-received event handlers associated with them.  It extends the
    /// generic process dictionary for use with
    /// <see cref="DataReceivedEventHandler" /> values.
    /// </summary>
    [ObjectId("17ef60a9-ef05-4743-bbda-6ad17eac57f9")]
    internal sealed class ProcessDataReceivedEventHandlerDictionary :
            ProcessDictionary<DataReceivedEventHandler>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ProcessDataReceivedEventHandlerDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion
    }
}
