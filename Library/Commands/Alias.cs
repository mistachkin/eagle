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
    [ObjectId("b338e2c4-6e66-456e-93af-91b5c21b449c")]
    /*
     * POLICY: This "command" is "safe" because it provides no
     *         functionality by itself (i.e. it is merely a
     *         transparent conduit to functionality that MAY
     *         be available elsewhere in the interpreter).
     */
    [CommandFlags(CommandFlags.Alias | CommandFlags.Safe)]
    [ObjectGroup("alias")]
    internal sealed class Alias : Core, IAlias
    {
        #region Public Constructors
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

        public Alias(
            ICommandData commandData,
            IAliasData aliasData
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
        private string nameToken;
        public string NameToken
        {
            get { return nameToken; }
            set { nameToken = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Interpreter sourceInterpreter;
        public Interpreter SourceInterpreter
        {
            get { return sourceInterpreter; }
            set { sourceInterpreter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Interpreter targetInterpreter;
        public Interpreter TargetInterpreter
        {
            get { return targetInterpreter; }
            set { targetInterpreter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private INamespace sourceNamespace;
        public INamespace SourceNamespace
        {
            get { return sourceNamespace; }
            set { sourceNamespace = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private INamespace targetNamespace;
        public INamespace TargetNamespace
        {
            get { return targetNamespace; }
            set { targetNamespace = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IExecute target;
        public IExecute Target
        {
            get { return target; }
            set { target = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentList arguments;
        public ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private OptionDictionary options;
        public OptionDictionary Options
        {
            get { return options; }
            set { options = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private AliasFlags aliasFlags;
        public AliasFlags AliasFlags
        {
            get { return aliasFlags; }
            set { aliasFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int startIndex;
        public int StartIndex
        {
            get { return startIndex; }
            set { startIndex = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAlias Members
        private DisposeCallback postInterpreterDisposed;
        public DisposeCallback PostInterpreterDisposed
        {
            get { return postInterpreterDisposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return (arguments != null) ? arguments.ToString() : String.Empty;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        public override ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
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
                    code = interpreter.RemoveAlias(
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
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
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
    }
}
