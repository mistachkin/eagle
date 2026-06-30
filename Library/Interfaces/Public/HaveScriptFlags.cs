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

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that carry the various sets
    /// of flags used to control script evaluation, expression evaluation, and
    /// event processing.
    /// </summary>
    [ObjectId("c753ddc8-fa43-4f3f-881d-1eba30210af6")]
    public interface IHaveScriptFlags
    {
        /// <summary>
        /// Gets or sets the <see cref="EngineMode" /> that controls how the
        /// engine processes scripts for this entity.
        /// </summary>
        EngineMode EngineMode { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="ScriptFlags" /> that control how
        /// scripts are located and loaded for this entity.
        /// </summary>
        ScriptFlags ScriptFlags { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="EngineFlags" /> that control engine
        /// behavior during script evaluation for this entity.
        /// </summary>
        EngineFlags EngineFlags { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="SubstitutionFlags" /> that control
        /// which kinds of substitution are performed for this entity.
        /// </summary>
        SubstitutionFlags SubstitutionFlags { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="EventFlags" /> that control event
        /// processing for this entity.
        /// </summary>
        EventFlags EventFlags { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="ExpressionFlags" /> that control how
        /// expressions are evaluated for this entity.
        /// </summary>
        ExpressionFlags ExpressionFlags { get; set; }

#if DATA
        /// <summary>
        /// Gets or sets the <see cref="BundleFlags" /> that control how data
        /// bundles are processed for this entity.
        /// </summary>
        BundleFlags BundleFlags { get; set; }
#endif
    }
}
