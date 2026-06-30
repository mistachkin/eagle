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

using System;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements an Eagle command that wraps a managed
    /// <see cref="System.Delegate" /> so that it can be invoked from script
    /// code as though it were an ordinary command.  Unlike a vanilla command,
    /// it carries the delegate to be called together with the flags that
    /// control how it is invoked.  See <c>core_language.md</c> for how
    /// commands are dispatched and executed.
    /// </summary>
    [ObjectId("20e001ff-f8bc-46ed-b12f-1aba126fefd0")]
    [CommandFlags(
        CommandFlags.NoPopulate | CommandFlags.NoAdd |
        CommandFlags.Delegate
    )]
    [ObjectGroup("delegate")]
    public class _Delegate : Default, IDelegateData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the wrapped delegate command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public _Delegate(
            ICommandData commandData
            )
            : base(commandData)
        {
            //
            // NOTE: This is not a strictly vanilla "command", it is a
            //       wrapped delegate.
            //
            this.Kind |= IdentifierKind.Delegate;

            //
            // NOTE: Normally, this flags assignment is performed by
            //       _Commands.Core for all commands residing in the core
            //       library; however, this class does not inherit from
            //       _Commands.Core.
            //
            if ((commandData == null) || !FlagOps.HasFlags(
                    commandData.Flags, CommandFlags.NoAttributes, true))
            {
                this.Flags |=
                    AttributeOps.GetCommandFlags(GetType().BaseType) |
                    AttributeOps.GetCommandFlags(this);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the wrapped delegate command, also
        /// initializing the wrapped delegate and its flags from the supplied
        /// delegate data.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        /// <param name="delegateData">
        /// The data describing the managed delegate to be wrapped and the
        /// flags controlling how it is invoked.  This parameter may be null,
        /// in which case no delegate is configured.
        /// </param>
        public _Delegate(
            ICommandData commandData,  /* in */
            IDelegateData delegateData /* in */
            )
            : this(commandData)
        {
            if (delegateData != null)
            {
                this.@delegate = delegateData.Delegate;
                this.delegateFlags = delegateData.DelegateFlags;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// This method determines whether a wrapped delegate has been
        /// configured and should therefore be used when this command is
        /// executed.
        /// </summary>
        /// <returns>
        /// Non-zero if a wrapped delegate is present and should be used;
        /// otherwise, zero.
        /// </returns>
        protected virtual bool ShouldUseDelegate()
        {
            return @delegate != null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteDelegate Members
        /// <summary>
        /// The managed delegate wrapped by this command, or null if no
        /// delegate has been configured.
        /// </summary>
        private Delegate @delegate;
        /// <summary>
        /// Gets or sets the managed delegate that is wrapped and invoked when
        /// this command is executed.
        /// </summary>
        public virtual Delegate Delegate
        {
            get { return @delegate; }
            set { @delegate = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDelegateData Members
        /// <summary>
        /// The flags that control how the wrapped delegate is invoked.
        /// </summary>
        private DelegateFlags delegateFlags;
        /// <summary>
        /// Gets or sets the flags that control how the wrapped delegate is
        /// invoked when this command is executed.
        /// </summary>
        public virtual DelegateFlags DelegateFlags
        {
            get { return delegateFlags; }
            set { delegateFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the wrapped delegate command.  It dispatches
        /// the supplied arguments to the wrapped <see cref="System.Delegate" />
        /// honoring the configured <see cref="DelegateFlags" />, and returns
        /// the result of that invocation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name; the remaining elements are passed to the wrapped
        /// delegate.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value returned by the wrapped
        /// delegate.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// (e.g. <see cref="ReturnCode.Error" />) with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            DelegateFlags delegateFlags = this.DelegateFlags;

            bool allowOptions = FlagOps.HasFlags(
                delegateFlags, DelegateFlags.UseCallOptions, true);

            return ScriptOps.ExecuteOrInvokeDelegate(
                interpreter, this.Delegate, arguments,
                allowOptions, 1 /* cmd ... */, 1,
                delegateFlags, ref result);
        }
        #endregion
    }
}
