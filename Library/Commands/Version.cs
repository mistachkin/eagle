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
    /// <summary>
    /// This class implements the Eagle <c>version</c> command, which reports
    /// version and build information about the running Eagle library, with the
    /// reported details controlled by an optional set of flags.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("b5813212-3372-416c-96ac-0b424a1465f3")]
    [CommandFlags(CommandFlags.Unsafe |
        CommandFlags.NonStandard | CommandFlags.Diagnostic)]
    [ObjectGroup("introspection")]
    internal sealed class _Version : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>version</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public _Version(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>version</c> command.  It accepts an
        /// optional set of <see cref="VersionFlags" /> selecting which pieces
        /// of version and build information to report, and places the formatted
        /// version information into <paramref name="result" />.
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
        /// command name; an optional element one supplies the
        /// <see cref="VersionFlags" /> value that selects which version
        /// information to report.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the formatted version information.  Upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the version
        /// information placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the flags cannot be parsed, the interpreter is null, or
        /// the argument list is null, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
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
