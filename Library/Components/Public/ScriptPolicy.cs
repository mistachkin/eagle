/*
 * ScriptPolicy.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a policy implemented by a script that decides
    /// whether a particular command or operation is permitted.  The policy may
    /// run in the supplied interpreter (non-isolated) or in a dedicated,
    /// privately created interpreter (isolated), and it is invoked through the
    /// <see cref="IExecute" /> mechanism.  It implements
    /// <see cref="IScriptPolicy" /> and is disposable; disposing it releases any
    /// owned policy interpreter.  See <c>security.md</c> for policy concepts.
    /// </summary>
    [ObjectId("655182b0-8e19-4ed5-bcf0-4311131bbff2")]
    public sealed class ScriptPolicy : IScriptPolicy, IDisposable
    {
        #region Private Constructors
        /// <summary>
        /// Constructs a script policy from the specified flags, target command,
        /// policy interpreter, ownership flag, and policy script text.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling the behavior of this policy.
        /// </param>
        /// <param name="commandType">
        /// The type of command this policy applies to, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="commandToken">
        /// The token of the command this policy applies to, if any.
        /// </param>
        /// <param name="policyInterpreter">
        /// The interpreter used to evaluate this policy.  This parameter may be
        /// null.
        /// </param>
        /// <param name="owned">
        /// Non-zero if this policy owns <paramref name="policyInterpreter" /> and
        /// is responsible for disposing it.
        /// </param>
        /// <param name="text">
        /// The policy script text to evaluate.  This parameter may be null.
        /// </param>
        private ScriptPolicy(
            PolicyFlags flags,             /* in */
            Type commandType,              /* in */
            long commandToken,             /* in */
            Interpreter policyInterpreter, /* in */
            bool owned,                    /* in */
            string text                    /* in */
            )
        {
            this.flags = flags;
            this.commandType = commandType;
            this.commandToken = commandToken;
            this.policyInterpreter = policyInterpreter;
            this.owned = owned;
            this.text = text;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method creates the interpreter settings used to construct an
        /// isolated policy interpreter, loading them from a file when one is
        /// specified or using default settings otherwise.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to load the interpreter settings from, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when loading the interpreter settings.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The created interpreter settings, or null if they could not be
        /// created.
        /// </returns>
        private static IInterpreterSettings CreateInterpreterSettings(
            string fileName,         /* in */
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            )
        {
            IInterpreterSettings interpreterSettings;

            if (fileName != null)
            {
                interpreterSettings = InterpreterSettings.CreateFrom(
                    fileName, cultureInfo, false, true, ref error);
            }
            else
            {
                interpreterSettings = InterpreterSettings.CreateDefault();
                interpreterSettings.CreateFlags = CreateFlags.EmbeddedUse;
            }

            return interpreterSettings;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an isolated policy interpreter using the
        /// specified interpreter settings.
        /// </summary>
        /// <param name="interpreterSettings">
        /// The interpreter settings to use when creating the interpreter.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The created interpreter, or null if it could not be created.
        /// </returns>
        private static Interpreter CreateInterpreter(
            IInterpreterSettings interpreterSettings, /* in */
            ref Result error                          /* out */
            )
        {
            return Interpreter.Create(interpreterSettings, true, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an isolated script policy, which evaluates its
        /// policy script in a dedicated, privately created interpreter that it
        /// owns.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling the behavior of the new policy.
        /// </param>
        /// <param name="commandType">
        /// The type of command the new policy applies to, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="commandToken">
        /// The token of the command the new policy applies to, if any.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when creating the isolated interpreter.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="text">
        /// The policy script text to evaluate.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to load the interpreter settings from, if any.
        /// This parameter is optional and may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The created script policy, or null if it could not be created.
        /// </returns>
        private static IScriptPolicy CreateIsolated(
            PolicyFlags flags,       /* in */
            Type commandType,        /* in */
            long commandToken,       /* in */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            string text,             /* in */
            string fileName,         /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            IInterpreterSettings interpreterSettings =
                CreateInterpreterSettings(
                    fileName, cultureInfo, ref error);

            if (interpreterSettings == null)
                return null;

            Interpreter policyInterpreter = CreateInterpreter(
                interpreterSettings, ref error);

            if (policyInterpreter == null)
                return null;

            return new ScriptPolicy(
                flags, commandType, commandToken, policyInterpreter,
                true, text);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a non-isolated script policy, which evaluates its
        /// policy script in the supplied interpreter that it does not own.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling the behavior of the new policy.
        /// </param>
        /// <param name="commandType">
        /// The type of command the new policy applies to, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="commandToken">
        /// The token of the command the new policy applies to, if any.
        /// </param>
        /// <param name="policyInterpreter">
        /// The interpreter used to evaluate the new policy.  This parameter is
        /// optional and may be null.
        /// </param>
        /// <param name="text">
        /// The policy script text to evaluate.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The created script policy.
        /// </returns>
        private static IScriptPolicy CreateNonIsolated(
            PolicyFlags flags,             /* in */
            Type commandType,              /* in */
            long commandToken,             /* in */
            Interpreter policyInterpreter, /* in: OPTIONAL */
            string text,                   /* in */
            ref Result error               /* out: NOT USED */
            )
        {
            return new ScriptPolicy(
                flags, commandType, commandToken, policyInterpreter,
                false, text);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a script policy, choosing between an isolated and
        /// a non-isolated policy based on the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling the behavior of the new policy; when these
        /// include the isolated flag, an isolated policy is created.
        /// </param>
        /// <param name="commandType">
        /// The type of command the new policy applies to, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="commandToken">
        /// The token of the command the new policy applies to, if any.
        /// </param>
        /// <param name="policyInterpreter">
        /// The interpreter used to evaluate a non-isolated policy.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when creating an isolated interpreter.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="text">
        /// The policy script text to evaluate.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to load the interpreter settings from for an
        /// isolated policy, if any.  This parameter is optional and may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The created script policy, or null if it could not be created.
        /// </returns>
        public static IScriptPolicy Create(
            PolicyFlags flags,             /* in */
            Type commandType,              /* in */
            long commandToken,             /* in */
            Interpreter policyInterpreter, /* in: OPTIONAL */
            CultureInfo cultureInfo,       /* in: OPTIONAL */
            string text,                   /* in */
            string fileName,               /* in: OPTIONAL */
            ref Result error               /* out */
            )
        {
            if (FlagOps.HasFlags(flags, PolicyFlags.Isolated, true))
            {
                return CreateIsolated(
                    flags, commandType, commandToken, cultureInfo, text,
                    fileName, ref error);
            }
            else
            {
                return CreateNonIsolated(
                    flags, commandType, commandToken, policyInterpreter,
                    text, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptPolicy Members
        /// <summary>
        /// The flags controlling the behavior of this policy.
        /// </summary>
        private PolicyFlags flags;

        /// <summary>
        /// Gets the flags controlling the behavior of this policy.
        /// </summary>
        public PolicyFlags Flags
        {
            get { return flags; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The type of command this policy applies to, if any.
        /// </summary>
        private Type commandType;

        /// <summary>
        /// Gets the type of command this policy applies to, if any.
        /// </summary>
        public Type CommandType
        {
            get { return commandType; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The token of the command this policy applies to, if any.
        /// </summary>
        private long commandToken;

        /// <summary>
        /// Gets the token of the command this policy applies to, if any.
        /// </summary>
        public long CommandToken
        {
            get { return commandToken; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The interpreter used to evaluate this policy.
        /// </summary>
        private Interpreter policyInterpreter;

        /// <summary>
        /// Gets the interpreter used to evaluate this policy.
        /// </summary>
        public Interpreter PolicyInterpreter
        {
            get { return policyInterpreter; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The policy script text to evaluate.
        /// </summary>
        private string text;

        /// <summary>
        /// Gets the policy script text to evaluate.
        /// </summary>
        public string Text
        {
            get { return text; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method evaluates the policy script to decide whether the
        /// operation represented by the specified arguments is permitted.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the policy is being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied by the caller, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="arguments">
        /// The arguments describing the operation being checked.  This parameter
        /// may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will be modified to contain the policy
        /// decision.  Upon failure, this parameter will be modified to contain
        /// an appropriate error message.
        /// </param>
        /// <returns>
        /// The return code indicating the outcome of evaluating the policy.
        /// </returns>
        [MethodFlags(
            MethodFlags.CommandPolicy | MethodFlags.System |
            MethodFlags.NoAdd)]
        public ReturnCode Execute( /* POLICY */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            return PolicyOps.CheckViaScript(
                flags, commandType, commandToken, policyInterpreter,
                text, interpreter, clientData, arguments, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// Non-zero if this object instance has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Non-zero if this policy owns its policy interpreter and is
        /// responsible for disposing it.
        /// </summary>
        private bool owned;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases all resources held by this object instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this object instance,
        /// disposing any owned policy interpreter when called from the public
        /// <c>Dispose</c> method.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the public
        /// <c>Dispose</c> method; zero if it is being called from the finalizer.
        /// </param>
        private void Dispose(
            bool disposing /* in */
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    if (policyInterpreter != null)
                    {
                        if (owned)
                        {
                            ObjectOps.TryDisposeOrComplain<Interpreter>(
                                policyInterpreter, ref policyInterpreter);
                        }

                        policyInterpreter = null;
                    }
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion
    }
}
