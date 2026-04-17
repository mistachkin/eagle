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
    [ObjectId("655182b0-8e19-4ed5-bcf0-4311131bbff2")]
    public sealed class ScriptPolicy : IScriptPolicy, IDisposable
    {
        #region Private Constructors
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

        private static Interpreter CreateInterpreter(
            IInterpreterSettings interpreterSettings, /* in */
            ref Result error                          /* out */
            )
        {
            return Interpreter.Create(interpreterSettings, true, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private PolicyFlags flags;
        public PolicyFlags Flags
        {
            get { return flags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Type commandType;
        public Type CommandType
        {
            get { return commandType; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long commandToken;
        public long CommandToken
        {
            get { return commandToken; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Interpreter policyInterpreter;
        public Interpreter PolicyInterpreter
        {
            get { return policyInterpreter; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string text;
        public string Text
        {
            get { return text; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
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
        private bool disposed;
        private bool owned;

        ///////////////////////////////////////////////////////////////////////

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ///////////////////////////////////////////////////////////////////////

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
