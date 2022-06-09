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
    [ObjectId("92cc1d66-8832-4e34-8b41-a1638577730f")]
    [CommandFlags(CommandFlags.Delegate)]
    [ObjectGroup("delegate")]
    public class _Delegate : Default
    {
        #region Public Constructors
        public _Delegate(
            ISubCommandData subCommandData
            )
            : base(subCommandData)
        {
            //
            // NOTE: This is not a strictly vanilla "sub-command", it is
            //       a wrapped delegate.
            //
            this.Kind |= IdentifierKind.Delegate;

            //
            // NOTE: Normally, this flags assignment is performed by
            //       _SubCommands.Core for all commands residing in the
            //       core library; however, this class does not inherit
            //       from _SubCommands.Core.
            //
            this.CommandFlags |=
                AttributeOps.GetCommandFlags(GetType().BaseType) |
                AttributeOps.GetCommandFlags(this);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return ScriptOps.ExecuteOrInvokeDelegate(
                interpreter, this.Delegate, arguments,
                2 /* cmd subCmd ... */, this.DelegateFlags,
                ref result);
        }
        #endregion
    }
}
