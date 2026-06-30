/*
 * Lrange.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>lrange</c> command, which returns a
    /// new list consisting of the elements of an existing list between two
    /// supplied indexes, inclusive.  See <c>core_language.md</c> for the
    /// command syntax and semantics.
    /// </summary>
    [ObjectId("dcf266a2-0a90-436d-bd0f-2995160bc6dd")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lrange : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>lrange</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lrange(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lrange</c> command.  It splits the
        /// first argument into a list, resolves the first and last index
        /// arguments against that list, clamps them to the valid range, and
        /// returns the selected sub-range of elements as a new list.
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
        /// command name; element one is the list to extract from, and elements
        /// two and three are the first and last indexes of the range to
        /// return.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the list of elements within the
        /// requested range (or an empty string when the range is empty).  Upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the extracted range
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the list or an index cannot be parsed, the interpreter
        /// is null, or the argument list is null, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count == 4)
                    {
                        StringList list = null;

                        code = ListOps.GetOrCopyOrSplitList(
                            interpreter, arguments[1], true, ref list,
                            ref result);

                        if (code == ReturnCode.Ok)
                        {
                            int firstIndex = Index.Invalid;

                            code = Value.GetIndex(
                                arguments[2], list.Count, ValueFlags.AnyIndex,
                                interpreter.InternalCultureInfo, ref firstIndex,
                                ref result);

                            if (code == ReturnCode.Ok)
                            {
                                int lastIndex = Index.Invalid;

                                code = Value.GetIndex(
                                    arguments[3], list.Count, ValueFlags.AnyIndex,
                                    interpreter.InternalCultureInfo, ref lastIndex,
                                    ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    if (firstIndex < 0)
                                        firstIndex = 0;

                                    if (lastIndex >= list.Count)
                                        lastIndex = list.Count - 1;

                                    if (firstIndex <= lastIndex)
                                    {
                                        result = StringList.GetRange(
                                            list, firstIndex, lastIndex);
                                    }
                                    else
                                    {
                                        result = String.Empty;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lrange list first last\"";
                        code = ReturnCode.Error;
                    }
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
