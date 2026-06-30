/*
 * Alias.cs --
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
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>alias</c> command, which acts as a
    /// transparent conduit that forwards an invocation to a target command,
    /// procedure, or other executable entity, possibly residing in another
    /// (target) interpreter.  The command itself provides no functionality of
    /// its own; it merely redirects to the configured target, optionally
    /// prepending fixed leading arguments.  See <c>core_language.md</c> for
    /// the command syntax and semantics.
    /// </summary>
    [ObjectId("b338e2c4-6e66-456e-93af-91b5c21b449c")]
    /*
     * POLICY: This "command" is "safe" because it provides no
     *         functionality by itself (i.e. it is merely a
     *         transparent conduit to functionality that MAY
     *         be available elsewhere in the interpreter).
     */
    [CommandFlags(
        CommandFlags.NoPopulate | CommandFlags.NoAdd |
        CommandFlags.Alias | CommandFlags.Safe
    )]
    [ObjectGroup("alias")]
    internal sealed class Alias : Core, IAlias
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>alias</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Alias(
            ICommandData commandData
            )
            : base(commandData)
        {
            //
            // NOTE: This is not a strictly vanilla "command", it is a
            //       "command alias".
            //
            this.Kind |= IdentifierKind.Alias;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the <c>alias</c> command, initializing
        /// its alias-specific state from the supplied alias data.  When a
        /// target interpreter is present, a disposal callback is registered so
        /// that this alias (and its associated command) can be removed from
        /// the source interpreter once the target interpreter is disposed.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        /// <param name="aliasData">
        /// The alias-specific data used to configure this command, such as its
        /// name token, source and target interpreters, namespaces, target,
        /// arguments, options, flags, and starting index.  This parameter may
        /// be null.
        /// </param>
        public Alias(
            ICommandData commandData, /* in */
            IAliasData aliasData      /* in */
            )
            : this(commandData)
        {
            if (aliasData != null)
            {
                nameToken = aliasData.NameToken;
                sourceInterpreter = aliasData.SourceInterpreter;
                targetInterpreter = aliasData.TargetInterpreter;
                sourceNamespace = aliasData.SourceNamespace;
                targetNamespace = aliasData.TargetNamespace;
                target = aliasData.Target;
                arguments = aliasData.Arguments;
                options = aliasData.Options;
                aliasFlags = aliasData.AliasFlags;
                startIndex = aliasData.StartIndex;

                //
                // BUGFIX: We need to know when the target interpreter is
                //         disposed so that we can remove this alias (and
                //         its associated command) from the source interpreter.
                //         Otherwise, attempts to invoke the command may raise
                //         an exception about the target interpreter being
                //         disposed.
                //
                if (targetInterpreter != null)
                {
                    postInterpreterDisposed = new DisposeCallback(
                        TargetInterpreterDisposed);

                    targetInterpreter.PostInterpreterDisposed +=
                        postInterpreterDisposed;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method handles disposal of the target interpreter.  When the
        /// disposed object is the target interpreter, it removes this alias
        /// (and its associated command) from the source interpreter, provided
        /// the source and target interpreters differ and a name token is
        /// available, and then clears the cached target interpreter reference.
        /// </summary>
        /// <param name="object">
        /// The object that was disposed; this is expected to be the target
        /// interpreter being torn down.  This parameter may be null.
        /// </param>
        private void TargetInterpreterDisposed(
            object @object
            )
        {
            if ((@object != null) &&
                Object.ReferenceEquals(@object, targetInterpreter))
            {
                if ((sourceInterpreter != null) && !Object.ReferenceEquals(
                        sourceInterpreter, targetInterpreter) &&
                    (nameToken != null))
                {
                    ReturnCode code;
                    Result error = null;

                    code = sourceInterpreter.RemoveAliasAndCommand(
                        nameToken, null, false, ref error);

                    if (code != ReturnCode.Ok)
                        DebugOps.Complain(sourceInterpreter, code, error);
                }

                targetInterpreter = null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAliasData Members
        /// <summary>
        /// The unique name token that identifies this alias (and its
        /// associated command) within the source interpreter.
        /// </summary>
        private string nameToken;
        /// <summary>
        /// Gets or sets the unique name token that identifies this alias (and
        /// its associated command) within the source interpreter.
        /// </summary>
        public string NameToken
        {
            get { return nameToken; }
            set { nameToken = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The interpreter in which this alias and its associated command are
        /// defined.
        /// </summary>
        private Interpreter sourceInterpreter;
        /// <summary>
        /// Gets or sets the interpreter in which this alias and its associated
        /// command are defined.
        /// </summary>
        public Interpreter SourceInterpreter
        {
            get { return sourceInterpreter; }
            set { sourceInterpreter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The interpreter in which the target of this alias is resolved and
        /// executed.  This may be the same as the source interpreter.
        /// </summary>
        private Interpreter targetInterpreter;
        /// <summary>
        /// Gets or sets the interpreter in which the target of this alias is
        /// resolved and executed.  This may be the same as the source
        /// interpreter.
        /// </summary>
        public Interpreter TargetInterpreter
        {
            get { return targetInterpreter; }
            set { targetInterpreter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The namespace within the source interpreter that this alias belongs
        /// to, if any.
        /// </summary>
        private INamespace sourceNamespace;
        /// <summary>
        /// Gets or sets the namespace within the source interpreter that this
        /// alias belongs to, if any.
        /// </summary>
        public INamespace SourceNamespace
        {
            get { return sourceNamespace; }
            set { sourceNamespace = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The namespace within the target interpreter that the target of this
        /// alias is resolved in, if any.
        /// </summary>
        private INamespace targetNamespace;
        /// <summary>
        /// Gets or sets the namespace within the target interpreter that the
        /// target of this alias is resolved in, if any.
        /// </summary>
        public INamespace TargetNamespace
        {
            get { return targetNamespace; }
            set { targetNamespace = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The executable entity that this alias forwards invocations to.
        /// This may be null when the alias is late-bound and resolved by name
        /// at execution time.
        /// </summary>
        private IExecute target;
        /// <summary>
        /// Gets or sets the executable entity that this alias forwards
        /// invocations to.  This may be null when the alias is late-bound and
        /// resolved by name at execution time.
        /// </summary>
        public IExecute Target
        {
            get { return target; }
            set { target = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The fixed leading arguments that are prepended to the arguments
        /// supplied at invocation time before being forwarded to the target.
        /// </summary>
        private ArgumentList arguments;
        /// <summary>
        /// Gets or sets the fixed leading arguments that are prepended to the
        /// arguments supplied at invocation time before being forwarded to the
        /// target.
        /// </summary>
        public ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The options associated with this alias that govern how it is
        /// processed, if any.
        /// </summary>
        private OptionDictionary options;
        /// <summary>
        /// Gets or sets the options associated with this alias that govern how
        /// it is processed, if any.
        /// </summary>
        public OptionDictionary Options
        {
            get { return options; }
            set { options = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that control the behavior of this alias, such as whether
        /// it is a system alias or whether its target is evaluated as a
        /// script.
        /// </summary>
        private AliasFlags aliasFlags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of this alias, such
        /// as whether it is a system alias or whether its target is evaluated
        /// as a script.
        /// </summary>
        public AliasFlags AliasFlags
        {
            get { return aliasFlags; }
            set { aliasFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The index into the invocation arguments at which the
        /// caller-supplied arguments begin when they are combined with the
        /// fixed leading arguments of this alias.
        /// </summary>
        private int startIndex;
        /// <summary>
        /// Gets or sets the index into the invocation arguments at which the
        /// caller-supplied arguments begin when they are combined with the
        /// fixed leading arguments of this alias.
        /// </summary>
        public int StartIndex
        {
            get { return startIndex; }
            set { startIndex = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAlias Members
        /// <summary>
        /// The callback registered with the target interpreter so that this
        /// alias is notified after that interpreter has been disposed.
        /// </summary>
        private DisposeCallback postInterpreterDisposed;
        /// <summary>
        /// Gets the callback registered with the target interpreter so that
        /// this alias is notified after that interpreter has been disposed.
        /// </summary>
        public DisposeCallback PostInterpreterDisposed
        {
            get { return postInterpreterDisposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this alias.  It
        /// returns the string form of the fixed leading arguments, or an empty
        /// string when no such arguments are present.
        /// </summary>
        /// <returns>
        /// The string representation of the fixed leading arguments of this
        /// alias, or an empty string when none are present.
        /// </returns>
        public override string ToString()
        {
            return (arguments != null) ? arguments.ToString() : String.Empty;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// This method terminates this alias when its associated command is
        /// being deleted.  When a name token is present, the alias is removed
        /// from the interpreter; otherwise it is assumed to have already been
        /// deleted and the step is skipped.  The base class termination is then
        /// performed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the interpreter is null or the
        /// alias could not be removed, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Terminate(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                if (nameToken != null)
                {
                    //
                    // NOTE: The command is being deleted.  Delete the alias
                    //       as well.
                    //
                    code = interpreter.InternalRemoveAlias(
                        nameToken, clientData, ref result);
                }
                else
                {
                    //
                    // NOTE: The alias has already been deleted, skip it.
                    //
                    code = ReturnCode.Ok;
                }

                if (code == ReturnCode.Ok)
                    code = base.Terminate(interpreter, clientData, ref result);
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        /// <summary>
        /// Gets the sub-commands exposed by this alias.  When the target of
        /// this alias is itself an ensemble, its sub-commands are returned;
        /// otherwise, the target is resolved (when late-bound) through the
        /// target interpreter and, if that resolved target is an ensemble, its
        /// sub-commands are returned.  Returns null when no ensemble target can
        /// be determined.
        /// </summary>
        public override EnsembleDictionary SubCommands
        {
            get
            {
                IEnsemble ensemble = target as IEnsemble;

                if (ensemble != null)
                {
                    return ensemble.SubCommands;
                }
                else
                {
                    if (targetInterpreter != null)
                    {
                        string targetName = null;

                        if (targetInterpreter.GetAliasArguments(this,
                                arguments, ref targetName) == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Grab the target of the alias (this will
                            //       be null if we are late-bound).
                            //
                            IExecute aliasTarget = null;

                            if (targetInterpreter.GetAliasTarget(
                                    this, targetName, arguments,
                                    LookupFlags.NoVerbose, true,
                                    ref aliasTarget) == ReturnCode.Ok)
                            {
                                ensemble = aliasTarget as IEnsemble;

                                if (ensemble != null)
                                    return ensemble.SubCommands;
                            }
                        }
                    }
                }

                return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>alias</c> command.  It resolves the
        /// target of the alias (looking it up by name when late-bound),
        /// combines the fixed leading arguments with the supplied arguments,
        /// pushes a tracking call frame, and forwards the invocation to the
        /// target in the target interpreter.  The target arguments are either
        /// evaluated as a script or executed directly, depending on the alias
        /// flags.
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
        /// command name; the remaining elements are forwarded to the target
        /// after the fixed leading arguments of this alias.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result produced by the target of
        /// the alias.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// (e.g. <see cref="ReturnCode.Error" /> when the target interpreter is
        /// null or the target cannot be resolved) with details placed in
        /// <paramref name="result" />.  The return code produced by the target
        /// is propagated back to the caller.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            if (targetInterpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            ReturnCode code;
            string targetName = null;
            ArgumentList targetArguments = null;

            code = targetInterpreter.GetAliasArguments(
                this, arguments, ref targetName, ref targetArguments,
                ref result);

            if (code == ReturnCode.Ok)
            {
                //
                // NOTE: Grab the target of the alias (this will be null if we
                //       are late-bound).
                //
                IExecute target = null;
                bool useUnknown = false;

                code = targetInterpreter.GetAliasTarget(this, targetName,
                    targetArguments, LookupFlags.Default, true, ref target,
                    ref useUnknown, ref result);

                //
                // NOTE: Do we have a valid target now (we may have already had
                //       one or we may have just succeeded in looking it up)?
                //
                if (code == ReturnCode.Ok)
                {
                    //
                    // NOTE: Create and push a new call frame to track the
                    //       activation of this alias.
                    //
                    string name = StringList.MakeList("alias", this.Name);

                    AliasFlags aliasFlags = this.AliasFlags;

                    ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                        CallFrameFlags.Alias);

                    interpreter.PushAutomaticCallFrame(frame);

                    ///////////////////////////////////////////////////////////

                    if (useUnknown)
                        targetInterpreter.EnterUnknownLevel();

                    try
                    {
                        if (FlagOps.HasFlags(
                                aliasFlags, AliasFlags.Evaluate, true))
                        {
                            code = targetInterpreter.EvaluateScript(
                                targetArguments, 0, ref result);
                        }
                        else
                        {
                            //
                            // NOTE: Save the current engine flags and then
                            //       enable the external execution flags.
                            //
                            EngineFlags savedEngineFlags =
                                targetInterpreter.BeginExternalExecution();

                            code = targetInterpreter.Execute(
                                targetName, target, clientData,
                                targetArguments, ref result);

                            //
                            // NOTE: Restore the saved engine flags, masking
                            //       off the external execution flags as
                            //       necessary.
                            //
                            /* IGNORED */
                            targetInterpreter.EndAndCleanupExternalExecution(
                                savedEngineFlags);
                        }
                    }
                    finally
                    {
                        if (useUnknown &&
                            Engine.IsUsableNoLock(targetInterpreter))
                        {
                            targetInterpreter.ExitUnknownLevel();
                        }
                    }

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: Pop the original call frame that we pushed above
                    //       and any intervening scope call frames that may be
                    //       leftover (i.e. they were not explicitly closed).
                    //
                    /* IGNORED */
                    interpreter.PopScopeCallFramesAndOneMore();
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /// <summary>
        /// This method determines whether the specified executable entity is a
        /// system alias.  It unwraps the entity when it is an
        /// <see cref="IWrapper" /> around an <see cref="IAlias" />, and then
        /// checks whether the resulting alias has the
        /// <see cref="AliasFlags.System" /> flag set.
        /// </summary>
        /// <param name="execute">
        /// The executable entity to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero when <paramref name="execute" /> is (or wraps) an alias
        /// that has the <see cref="AliasFlags.System" /> flag set; otherwise,
        /// zero.
        /// </returns>
        public static bool IsSystemAlias(
            IExecute execute /* in */
            )
        {
            if (execute == null)
                return false;

            IAlias alias;
            IWrapper wrapper = execute as IWrapper;

            if ((wrapper != null) &&
                (wrapper.Object is IAlias)) /* NOT REDUNDANT: DNR */
            {
                alias = wrapper.Object as IAlias;
            }
            else
            {
                alias = execute as IAlias;
            }

            if (alias == null)
                return false;

            return FlagOps.HasFlags(
                alias.AliasFlags, AliasFlags.System, true);
        }
        #endregion
    }
}
