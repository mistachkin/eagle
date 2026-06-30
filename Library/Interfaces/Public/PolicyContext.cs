/*
 * PolicyContext.cs --
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
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents the context passed to a policy when it is
    /// invoked to make a decision about a proposed operation (e.g. executing
    /// a script or command).  It composes the client data accessor
    /// (<see cref="IGetClientData" />), the owning interpreter
    /// (<see cref="IHaveInterpreter" />), the owning plugin
    /// (<see cref="IHavePlugin" />), and the type and name information
    /// (<see cref="ITypeAndName" />).  It carries the inputs being examined
    /// and records the resulting decision and the reason for it.
    /// </summary>
    [ObjectId("94d0d340-c819-4054-abbf-bb6ccfed0ed3")]
    public interface IPolicyContext :
            IGetClientData, IHaveInterpreter, IHavePlugin, ITypeAndName
    {
        //
        // NOTE: *WARNING* This is the plugin that contains
        //       the policy currently being invoked (i.e. it
        //       can change with each callback).
        //
        // IPlugin Plugin { get; set; }

        /// <summary>
        /// Gets the policy flags that describe the kind of operation being
        /// examined and the context of this policy invocation.
        /// </summary>
        PolicyFlags Flags { get; }

        /// <summary>
        /// Gets the name of the assembly associated with the operation being
        /// examined, if any.  This value may be null.
        /// </summary>
        AssemblyName AssemblyName { get; }

        /// <summary>
        /// Gets the entity, if any, that is the subject of the operation
        /// being examined.  This value may be null.
        /// </summary>
        IExecute Execute { get; }
        /// <summary>
        /// Gets the list of arguments associated with the operation being
        /// examined, if any.  This value may be null.
        /// </summary>
        ArgumentList Arguments { get; }

        /// <summary>
        /// Gets the script associated with the operation being examined, if
        /// any.  This value may be null.
        /// </summary>
        IScript Script { get; }
        /// <summary>
        /// Gets the name of the file associated with the operation being
        /// examined, if any.  This value may be null.
        /// </summary>
        string FileName { get; }
        /// <summary>
        /// Gets the raw bytes associated with the operation being examined,
        /// if any.  This value may be null.
        /// </summary>
        byte[] Bytes { get; }
        /// <summary>
        /// Gets the text associated with the operation being examined, if
        /// any.  This value may be null.
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Gets the character encoding associated with the operation being
        /// examined, if any.  This value may be null.
        /// </summary>
        Encoding Encoding { get; }
        /// <summary>
        /// Gets the timeout, in milliseconds, associated with the operation
        /// being examined, if any.  This value may be null.
        /// </summary>
        int? Timeout { get; }

        /// <summary>
        /// Gets the hash value computed for the operation being examined, if
        /// any.  This value may be null.
        /// </summary>
        byte[] HashValue { get; }
        /// <summary>
        /// Gets the name of the hash algorithm used to compute
        /// <see cref="HashValue" />, if any.  This value may be null.
        /// </summary>
        string HashAlgorithmName { get; }

        //
        // NOTE: *WARNING* For informational purposes only.
        //       Please DO NOT USE to make policy decisions.
        //
        /// <summary>
        /// Gets or sets the result associated with this policy context.
        /// This value is provided for informational purposes only and must
        /// not be used to make policy decisions.
        /// </summary>
        Result Result { get; set; }

        /// <summary>
        /// Gets the policy decision that was in effect before this policy
        /// was invoked.
        /// </summary>
        PolicyDecision OriginalDecision { get; }
        /// <summary>
        /// Gets the current policy decision, as it stands after any changes
        /// made by this policy.
        /// </summary>
        PolicyDecision Decision { get; }
        /// <summary>
        /// Gets the optional reason associated with the current policy
        /// decision, if any.  This value may be null.
        /// </summary>
        Result Reason { get; } /* OPTIONAL: Reason for the decision. */

        /// <summary>
        /// Determines whether the current policy decision is undecided.
        /// </summary>
        /// <returns>
        /// True if the decision is undecided; otherwise, false.
        /// </returns>
        bool IsUndecided();
        /// <summary>
        /// Determines whether the current policy decision is denied.
        /// </summary>
        /// <returns>
        /// True if the decision is denied; otherwise, false.
        /// </returns>
        bool IsDenied();
        /// <summary>
        /// Determines whether the current policy decision is approved.
        /// </summary>
        /// <returns>
        /// True if the decision is approved; otherwise, false.
        /// </returns>
        bool IsApproved();

        /// <summary>
        /// Sets the current policy decision to undecided.
        /// </summary>
        void Undecided();
        /// <summary>
        /// Sets the current policy decision to denied.
        /// </summary>
        void Denied();
        /// <summary>
        /// Sets the current policy decision to approved.
        /// </summary>
        void Approved();

        /// <summary>
        /// Sets the current policy decision to undecided, recording the
        /// specified reason.
        /// </summary>
        /// <param name="reason">
        /// The reason for the decision.  This parameter may be null.
        /// </param>
        void Undecided(Result reason);
        /// <summary>
        /// Sets the current policy decision to denied, recording the
        /// specified reason.
        /// </summary>
        /// <param name="reason">
        /// The reason for the decision.  This parameter may be null.
        /// </param>
        void Denied(Result reason);
        /// <summary>
        /// Sets the current policy decision to approved, recording the
        /// specified reason.
        /// </summary>
        /// <param name="reason">
        /// The reason for the decision.  This parameter may be null.
        /// </param>
        void Approved(Result reason);

        /// <summary>
        /// Emits a diagnostic trace describing this policy context.
        /// </summary>
        /// <param name="category">
        /// The category to associate with the trace output.  This parameter
        /// may be null.
        /// </param>
        /// <param name="priority">
        /// The priority to associate with the trace output.
        /// </param>
        [Obsolete()]
        void Trace(string category, TracePriority priority);

        /// <summary>
        /// Emits a diagnostic trace describing this policy context.
        /// </summary>
        /// <param name="category">
        /// The category to associate with the trace output.  This parameter
        /// may be null.
        /// </param>
        /// <param name="priority">
        /// The priority to associate with the trace output.
        /// </param>
        /// <param name="full">
        /// Non-zero to include full detail in the trace output; otherwise, a
        /// summary is emitted.
        /// </param>
        void Trace(string category, TracePriority priority, bool full);
    }
}
