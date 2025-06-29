/*
 * HaveScriptFlags.cs --
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
using Eagle._Components.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("e909b914-81a1-4318-ad1e-2f8921c99ad6")]
    public sealed class HaveScriptFlags : IHaveScriptFlags
    {
        #region Public Constructors
        public HaveScriptFlags(
            bool useDefaults /* in */
            )
        {
            if (useDefaults)
            {
                engineMode = EngineMode.Default;
                engineFlags = EngineFlags.Default;
                substitutionFlags = SubstitutionFlags.Default;
                eventFlags = EventFlags.Default;
                expressionFlags = ExpressionFlags.Default;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveScriptFlags Members
        private EngineMode engineMode;
        public EngineMode EngineMode
        {
            get { return engineMode; }
            set { engineMode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ScriptFlags scriptFlags;
        public ScriptFlags ScriptFlags
        {
            get { return scriptFlags; }
            set { scriptFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EngineFlags engineFlags;
        public EngineFlags EngineFlags
        {
            get { return engineFlags; }
            set { engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SubstitutionFlags substitutionFlags;
        public SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
            set { substitutionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventFlags eventFlags;
        public EventFlags EventFlags
        {
            get { return eventFlags; }
            set { eventFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ExpressionFlags expressionFlags;
        public ExpressionFlags ExpressionFlags
        {
            get { return expressionFlags; }
            set { expressionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DATA
        private BundleFlags bundleFlags;
        public BundleFlags BundleFlags
        {
            get { return bundleFlags; }
            set { bundleFlags = value; }
        }
#endif
        #endregion
    }
}
