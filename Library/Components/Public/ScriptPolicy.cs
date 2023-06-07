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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("655182b0-8e19-4ed5-bcf0-4311131bbff2")]
    public sealed class ScriptPolicy : IScriptPolicy
    {
        #region Private Constructors
        private ScriptPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Interpreter policyInterpreter,
            string text
            )
        {
            this.flags = flags;
            this.commandType = commandType;
            this.commandToken = commandToken;
            this.policyInterpreter = policyInterpreter;
            this.text = text;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static ScriptPolicy Create(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Interpreter policyInterpreter,
            string text
            )
        {
            return new ScriptPolicy(
                flags, commandType, commandToken, policyInterpreter, text);
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
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return PolicyOps.CheckViaScript(
                flags, commandType, commandToken, policyInterpreter,
                text, interpreter, clientData, arguments, ref result);
        }
        #endregion
    }
}
