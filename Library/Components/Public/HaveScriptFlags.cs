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
    /// <summary>
    /// This class provides a container for the various flag values that govern
    /// script evaluation in an interpreter, including the engine mode, script,
    /// engine, substitution, event, and expression flags.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("e909b914-81a1-4318-ad1e-2f8921c99ad6")]
    public sealed class HaveScriptFlags : IHaveScriptFlags
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a script flags container, optionally initializing the
        /// contained flags to their default values.
        /// </summary>
        /// <param name="useDefaults">
        /// Non-zero to initialize the contained flags to their default values.
        /// </param>
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
        /// <summary>
        /// Stores the engine mode used for script evaluation.
        /// </summary>
        private EngineMode engineMode;
        /// <summary>
        /// Gets or sets the engine mode used for script evaluation.
        /// </summary>
        public EngineMode EngineMode
        {
            get { return engineMode; }
            set { engineMode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the script flags used for script evaluation.
        /// </summary>
        private ScriptFlags scriptFlags;
        /// <summary>
        /// Gets or sets the script flags used for script evaluation.
        /// </summary>
        public ScriptFlags ScriptFlags
        {
            get { return scriptFlags; }
            set { scriptFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the engine flags used for script evaluation.
        /// </summary>
        private EngineFlags engineFlags;
        /// <summary>
        /// Gets or sets the engine flags used for script evaluation.
        /// </summary>
        public EngineFlags EngineFlags
        {
            get { return engineFlags; }
            set { engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the substitution flags used for script evaluation.
        /// </summary>
        private SubstitutionFlags substitutionFlags;
        /// <summary>
        /// Gets or sets the substitution flags used for script evaluation.
        /// </summary>
        public SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
            set { substitutionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the event flags used for script evaluation.
        /// </summary>
        private EventFlags eventFlags;
        /// <summary>
        /// Gets or sets the event flags used for script evaluation.
        /// </summary>
        public EventFlags EventFlags
        {
            get { return eventFlags; }
            set { eventFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the expression flags used for script evaluation.
        /// </summary>
        private ExpressionFlags expressionFlags;
        /// <summary>
        /// Gets or sets the expression flags used for script evaluation.
        /// </summary>
        public ExpressionFlags ExpressionFlags
        {
            get { return expressionFlags; }
            set { expressionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DATA
        /// <summary>
        /// Stores the bundle flags used for script evaluation.
        /// </summary>
        private BundleFlags bundleFlags;
        /// <summary>
        /// Gets or sets the bundle flags used for script evaluation.
        /// </summary>
        public BundleFlags BundleFlags
        {
            get { return bundleFlags; }
            set { bundleFlags = value; }
        }
#endif
        #endregion
    }
}
