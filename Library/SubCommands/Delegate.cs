/*
 * Delegate.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._SubCommands
{
    /// <summary>
    /// This class implements a sub-command that wraps a managed delegate and
    /// exposes it for invocation as an Eagle sub-command.  It derives from
    /// <see cref="Default" /> and is marked with
    /// <see cref="CommandFlags.Delegate" /> to indicate that its execution is
    /// backed by a delegate rather than by a vanilla sub-command.
    /// </summary>
    [ObjectId("92cc1d66-8832-4e34-8b41-a1638577730f")]
    [CommandFlags(CommandFlags.Delegate)]
    [ObjectGroup("delegate")]
    public class _Delegate : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the delegate sub-command using the
        /// specified sub-command metadata.
        /// </summary>
        /// <param name="subCommandData">
        /// The data used to create and identify this sub-command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public _Delegate(
            ISubCommandData subCommandData
            )
            : base(subCommandData)
        {
            //
            // NOTE: This is not a strictly vanilla "sub-command", it is a
            //       wrapped delegate.
            //
            this.Kind |= IdentifierKind.Delegate;

            //
            // NOTE: Normally, this flags assignment is performed by
            //       _SubCommands.Core for all commands residing in the
            //       core library; however, this class does not inherit
            //       from _SubCommands.Core.
            //
            if ((subCommandData == null) || !FlagOps.HasFlags(
                    subCommandData.CommandFlags, CommandFlags.NoAttributes,
                    true))
            {
                this.CommandFlags |=
                    AttributeOps.GetCommandFlags(GetType().BaseType) |
                    AttributeOps.GetCommandFlags(this);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the delegate sub-command using the
        /// specified sub-command metadata and the data describing the managed
        /// delegate to be wrapped.
        /// </summary>
        /// <param name="subCommandData">
        /// The data used to create and identify this sub-command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        /// <param name="delegateData">
        /// The data describing the managed delegate to be wrapped and executed
        /// by this sub-command.  This parameter may be null.
        /// </param>
        public _Delegate(
            ISubCommandData subCommandData,
            IDelegateData delegateData
            )
            : base(subCommandData, delegateData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// Executes the wrapped delegate using the supplied arguments,
        /// optionally honoring command options when invoking it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this sub-command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data supplied when the sub-command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  The element at index
        /// zero is the command name and the element at index one is the
        /// sub-command name; the remaining elements are passed to the wrapped
        /// delegate.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// wrapped delegate.  Upon failure, this must contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            DelegateFlags delegateFlags = this.DelegateFlags;

            bool allowOptions = FlagOps.HasFlags(
                delegateFlags, DelegateFlags.UseCallOptions, true);

            return ScriptOps.ExecuteOrInvokeDelegate(
                interpreter, this.Delegate, arguments,
                allowOptions, 2 /* cmd subCmd ... */,
                2, delegateFlags, ref result);
        }
        #endregion
    }
}
