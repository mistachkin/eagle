/*
 * Linsert.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>linsert</c> command, which returns a
    /// new list formed by inserting one or more new elements into a copy of an
    /// existing list before the element at a given index.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("2d555605-3100-483c-950b-c6e4e87446be")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Linsert : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>linsert</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Linsert(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>linsert</c> command.  It parses the
        /// source list, resolves the insertion index (supporting the
        /// <c>end</c> and <c>end-N</c> forms against the post-insertion length
        /// and clamping numeric indices into range), inserts the supplied
        /// values before that position, and returns the resulting list.
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
        /// command name; element one is the source list; element two is the
        /// insertion index; the remaining elements are the values to insert.
        /// This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the new list with the values inserted.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the resulting list
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the source list cannot be parsed, the index cannot be
        /// resolved, the interpreter is null, or the argument list is null,
        /// with details placed in <paramref name="result" />.
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
                    if (arguments.Count >= 4)
                    {
                        StringList list = null;

                        //
                        // WARNING: Cannot cache list representation here, the list
                        //          is modified below.
                        //
                        code = ListOps.GetOrCopyOrSplitList(
                            interpreter, arguments[1], false, ref list, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            int index = Index.Invalid;

                            //
                            // BUGFIX: For [linsert] the index is an INSERTION
                            //         point, so there are list.Count + 1 valid
                            //         positions (0 through list.Count) and the
                            //         "end" index must resolve to list.Count (the
                            //         append point), not list.Count - 1.  Resolve
                            //         the index against that length so "end" and
                            //         "end-N" match Tcl; plain numeric indices are
                            //         still clamped to the range below.
                            //
                            code = Value.GetIndex(
                                arguments[2], list.Count + 1, ValueFlags.AnyIndex,
                                interpreter.InternalCultureInfo, ref index, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                //
                                // NOTE: Auto-normalize index to be within range.
                                //
                                if (index < 0)
                                    index = 0;
                                else if (index > list.Count)
                                    index = list.Count;

                                StringList subList = new StringList(arguments, 3);

                                list.InsertRange(index, subList);

                                result = list;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"linsert list index value ?value ...?\"";
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
