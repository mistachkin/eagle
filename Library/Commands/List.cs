/*
 * List.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("5e0a255f-9c13-4239-bf86-299606048791")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.SecuritySdk)]
    [ObjectGroup("list")]
    internal sealed class List : Core
    {
        public List(
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
            ReturnCode code = ReturnCode.Ok;

            //
            // NOTE: This command returns a list comprised of all the args, or
            //       an empty string if no args are specified. Braces and
            //       backslashes get added as necessary, so that the lindex
            //       command may be used on the result to re-extract the
            //       original arguments, and also so that eval may be used to
            //       execute the resulting list, with arg1 comprising the 
            //       command's name and the other args comprising its
            //       arguments.  List produces slightly different results than
            //       concat: concat removes one level of grouping before
            //       forming the list, while list works directly from the
            //       original arguments.
            //
            if (interpreter != null)
            {
                if (arguments != null)
                {
                    result = new StringList(arguments, 1);
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
