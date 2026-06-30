/*
 * Lsort.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Public = Eagle._Components.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>lsort</c> command, which sorts the
    /// elements of a list and returns a new, sorted list.  It supports a
    /// variety of options that control the comparison mode (for example
    /// <c>-ascii</c>, <c>-dictionary</c>, <c>-integer</c>, <c>-real</c>,
    /// <c>-random</c>, and <c>-command</c>), the sort direction
    /// (<c>-increasing</c> or <c>-decreasing</c>), case sensitivity
    /// (<c>-nocase</c>), sub-element selection (<c>-index</c>), and removal of
    /// duplicate elements (<c>-unique</c>).  See <c>core_language.md</c> for
    /// the command syntax and semantics.
    /// </summary>
    [ObjectId("f4947321-92bf-42a3-8e87-9b562a39d9f4")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lsort : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>lsort</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lsort(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lsort</c> command.  It parses any
        /// leading options, obtains a private copy of the supplied list,
        /// selects the appropriate comparer based on the requested comparison
        /// mode, sorts the list (optionally removing duplicates when
        /// <c>-unique</c> is specified), and returns the resulting sorted
        /// list.
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
        /// command name; it is followed by any options and, finally, the list
        /// to be sorted.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the sorted list.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the sorted list
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the arguments are invalid, an
        /// unknown option is supplied, or the comparison fails, with details
        /// placed in <paramref name="result" />.
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
                    if (arguments.Count >= 2)
                    {
                        OptionDictionary options =
                            CommandOptions.GetCommandOptions(
                                CommandOptionType.Lsort);

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                            {
                                StringList list = null;

                                //
                                // WARNING: Cannot cache list representation here, the list
                                //          is modified below.
                                //
                                code = ListOps.GetOrCopyOrSplitList(
                                    interpreter, arguments[argumentIndex], false, ref list,
                                    ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    IVariant value = null;
                                    string indexText = null;

                                    if (options.IsPresent("-index", ref value))
                                        indexText = value.ToString();

                                    bool ascending = true; // FIXME: PRI 5: Default handling.

                                    if (options.IsPresent("-decreasing"))
                                        ascending = false;
                                    else if (options.IsPresent("-increasing"))
                                        ascending = true;

                                    bool noCase = false;

                                    if (options.IsPresent("-nocase"))
                                        noCase = true;

                                    bool unique = false;

                                    if (options.IsPresent("-unique"))
                                        unique = true;

                                    IntDictionary duplicates = null;
                                    IComparer<string> comparer = null;

                                    if (options.IsPresent("-command", ref value))
                                    {
                                        StringList callbackArguments = null;

                                        if (value.IsList())
                                        {
                                            callbackArguments = (StringList)value.Value;
                                        }
                                        else
                                        {
                                            string temporary = value.ToString();

                                            code = ParserOps<string>.SplitList(
                                                interpreter, temporary, 0, Length.Invalid,
                                                true, ref callbackArguments);
                                        }

                                        if (code == ReturnCode.Ok)
                                        {
                                            ICallback callback = CommandCallback.Create(
                                                 MarshalFlags.Default, CallbackFlags.Default,
                                                 ObjectFlags.Callback, ByRefArgumentFlags.None,
                                                 interpreter, _Public.ClientData.Empty, null,
                                                 callbackArguments, ref result);

                                            if (callback != null)
                                                comparer = new _Comparers.StringCommandComparer(
                                                    interpreter, callback, ascending, indexText, false,
                                                    unique, interpreter.InternalCultureInfo, ref duplicates);
                                            else
                                                code = ReturnCode.Error;
                                        }
                                    }
                                    else if (options.IsPresent("-dictionary"))
                                    {
                                        comparer = new _Comparers.StringDictionaryComparer(
                                            interpreter, ascending, indexText, false, unique,
                                            interpreter.InternalCultureInfo, ref duplicates);
                                    }
                                    else if (options.IsPresent("-integer"))
                                    {
                                        comparer = new _Comparers.StringIntegerComparer(
                                            interpreter, ascending, indexText, false, unique,
                                            interpreter.InternalCultureInfo, ref duplicates);
                                    }
                                    else if (options.IsPresent("-random"))
                                    {
                                        comparer = new _Comparers.StringRandomComparer(
                                            interpreter, ascending, indexText, false, unique,
                                            interpreter.InternalCultureInfo, interpreter.InternalProvideEntropy,
                                            interpreter.RandomNumberGenerator, ref duplicates);
                                    }
                                    else if (options.IsPresent("-real"))
                                    {
                                        comparer = new _Comparers.StringRealComparer(
                                            interpreter, ascending, indexText, false, unique,
                                            interpreter.InternalCultureInfo, ref duplicates);
                                    }
                                    else if (options.IsPresent("-ascii") || true) // FIXME: PRI 5: Default handling.
                                    {
                                        comparer = new _Comparers.StringAsciiComparer(
                                            interpreter, ascending, indexText, false, noCase,
                                            unique, interpreter.InternalCultureInfo, ref duplicates);
                                    }

                                    if (code == ReturnCode.Ok)
                                    {
                                        try
                                        {
                                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                            {
                                                if (comparer != null)
                                                {
                                                    list.Sort(comparer);
                                                }
                                                else
                                                {
                                                    //
                                                    // FIXME: This will never be hit because we always default 
                                                    //        to using the StringAsciiComparer (above).
                                                    //
                                                    list.Sort(); // use .NET Framework defaults
                                                }
                                            }

                                            //
                                            // NOTE: If we are in unique mode, remove any duplicates from 
                                            //       the final resulting list now.
                                            //
                                            if (unique)
                                            {
                                                StringList uniqueList = new StringList();

                                                //
                                                // NOTE: Process each element in the list to see if it has
                                                //       been counted as a duplicate value by the comparer.
                                                //
                                                //       If the value has not been added to the final resulting
                                                //       list yet, add it now and mark the value so that it will
                                                //       never be added again.
                                                //
                                                // BUGFIX: Per Tcl, [lsort -unique] retains the LAST element
                                                //         of every group of equal elements (not the first);
                                                //         this is only observable when "equal" elements
                                                //         differ in representation (e.g. case under -nocase,
                                                //         or whole sublists under -index).  Since the list is
                                                //         now sorted, equal elements are adjacent, so walk it
                                                //         in REVERSE: the last element of each group is then
                                                //         the first one seen (and thus retained), and the
                                                //         original sort order is restored at the end
                                                //         (COMPAT: Tcl).
                                                //
                                                // HACK: In the worst possible case, this loop can have a runtime
                                                //       of O(N^2), including called functions, primarily due to
                                                //       the inability of .NET to provide proper context to
                                                //       IComparer callbacks.  This code could be avoided entirely
                                                //       if there was an interface for sorting comparison callbacks
                                                //       that provided the indexes of the elements being compared
                                                //       in addition to their values.
                                                //
                                                for (int elementIndex = list.Count - 1; /* O(N) */
                                                        elementIndex >= 0; elementIndex--)
                                                {
                                                    string element = list[elementIndex];

                                                    //
                                                    // NOTE: Has this value been marked as having been previously
                                                    //       added to the final resulting list?
                                                    //
                                                    int count =
                                                        ListOps.GetDuplicateCount(comparer, duplicates, element);

                                                    if (count != Count.Invalid)
                                                    {
                                                        //
                                                        // NOTE: Add this element into the final resulting list.
                                                        //       Either it has no duplicates or we have not yet
                                                        //       added it to the final resulting list.
                                                        //
                                                        uniqueList.Add(element);

                                                        //
                                                        // NOTE: If this value had any duplicates, mark the value
                                                        //       as having been added to the final resulting list.
                                                        //
                                                        if (!ListOps.SetDuplicateCount(comparer, duplicates, element, Count.Invalid))
                                                        {
                                                            result = String.Format(
                                                                "failed to update duplicate count for element \"{0}\"",
                                                                element);

                                                            code = ReturnCode.Error;
                                                            break;
                                                        }
                                                    }
                                                }

                                                //
                                                // NOTE: The unique elements were collected in reverse order;
                                                //       restore the proper (sorted) order, then the list of
                                                //       unique elements is the result.
                                                //
                                                if (code == ReturnCode.Ok)
                                                {
                                                    uniqueList.Reverse();
                                                    list = uniqueList;
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = list;
                                        }
                                        catch (Exception e)
                                        {
                                            Engine.SetExceptionErrorCode(interpreter, e);

                                            if (e.InnerException != null)
                                                result = e.InnerException.Message;
                                            else if (e is ScriptException)
                                                result = e.Message;
                                            else
                                                result = e;

                                            code = ReturnCode.Error;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    Option.LooksLikeOption(arguments[argumentIndex]))
                                {
                                    result = OptionDictionary.BadOption(
                                        options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                }
                                else
                                {
                                    result = "wrong # args: should be \"lsort ?options? list\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lsort ?options? list\"";
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
