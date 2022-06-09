/*
 * Version.cs --
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

namespace Eagle._Commands
{
    [ObjectId("b5813212-3372-416c-96ac-0b424a1465f3")]
    [CommandFlags(CommandFlags.Unsafe |
        CommandFlags.NonStandard | CommandFlags.Diagnostic)]
    [ObjectGroup("introspection")]
    internal sealed class _Version : Core
    {
        public _Version(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if ((arguments.Count < 1) || (arguments.Count > 2))
            {
                result = "wrong # args: should be \"version ?flags?\"";
                return ReturnCode.Error;
            }

            VersionFlags versionFlags = VersionFlags.Default;

            if (arguments.Count == 2)
            {
                object enumValue = EnumOps.TryParseFlags(
                    interpreter, typeof(VersionFlags),
                    versionFlags.ToString(), arguments[1],
                    interpreter.InternalCultureInfo, true,
                    true, true, ref result);

                if (enumValue is VersionFlags)
                    versionFlags = (VersionFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            return RuntimeOps.GetVersion(
                interpreter, versionFlags, ref result);
        }
        #endregion
    }
}
