/*
 * Lsearch.cs --
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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>lsearch</c> command, which searches
    /// the elements of a list for one that matches a pattern and returns the
    /// index (or, optionally, the value) of the first match, every match, or
    /// an indication that no match was found.  It supports the various match
    /// modes (exact, glob, regexp, substring, sorted), comparison styles
    /// (ascii, dictionary, integer, real), and modifiers (for example
    /// <c>-all</c>, <c>-inline</c>, <c>-index</c>, <c>-start</c>,
    /// <c>-nocase</c>, <c>-not</c>, and <c>-inverse</c>).  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("3e5dfc83-29bb-44a5-86c4-e78c0b7f21f0")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lsearch : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>lsearch</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lsearch(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lsearch</c> command.  It parses any
        /// options, obtains the list to search (either directly or, with
        /// <c>-variable</c>, from the named script variable), searches it for
        /// the supplied pattern according to the selected match mode and
        /// comparison style, and reports the matching index, value, or list of
        /// matches.
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
        /// command name, followed by any options, then the list (or variable
        /// name) to search and the pattern to search for.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the index of the first match (or
        /// <see cref="Index.Invalid" /> when none is found), the matching
        /// value when <c>-inline</c> is used, or the list of all matches when
        /// <c>-all</c> is used.  Upon failure, this contains an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
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
                    if (arguments.Count >= 2)
                    {
                        OptionDictionary options =
                            CommandOptions.GetCommandOptions(
                                CommandOptionType.Lsearch);

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) == arguments.Count))
                            {
                                StringList list = null;

                                ///////////////////////////////////////////////////////////////////////
                                //
                                // HACK: *PERF* This option enables an optimization that allows us to
                                //       use the cached list representation for a particular script
                                //       variable, if any, instead of re-parsing the string.  If this
                                //       option is enabled, the first non-option argument is NOT the
                                //       list to search; rather, it is the variable name containing
                                //       the list to search.
                                //
                                bool isVariable = false;

                                if (options.IsPresent("-variable"))
                                    isVariable = true;

                                if (isVariable)
                                {
                                    /* IGNORED */
                                    interpreter.GetListVariableValue(
                                        VariableFlags.None, arguments[argumentIndex], false, true,
                                        true, true, ref list);
                                }

                                ///////////////////////////////////////////////////////////////////////
                                //
                                // NOTE: If no list representation is available, then parse the first
                                //       non-option argument string into a list.
                                //
                                if (list == null)
                                {
                                    code = ListOps.GetOrCopyOrSplitList(
                                        interpreter, arguments[argumentIndex], true, ref list,
                                        ref result);
                                }

                                if (code == ReturnCode.Ok)
                                {
                                    IVariant value = null;
                                    string indexText = null;

                                    if (options.IsPresent("-index", ref value))
                                        indexText = value.ToString();

                                    bool inverse = false;

                                    if (options.IsPresent("-inverse"))
                                        inverse = true;

                                    bool subIndexes = false;

                                    if (options.IsPresent("-subindices"))
                                        subIndexes = true;

                                    if ((indexText != null) || !subIndexes)
                                    {
                                        string start = null;

                                        if (options.IsPresent("-start", ref value))
                                            start = value.ToString();

                                        int startIndex = Index.Invalid;

                                        if (start != null)
                                        {
                                            code = Value.GetIndex(
                                                start, list.Count, ValueFlags.AnyIndex,
                                                interpreter.InternalCultureInfo, ref startIndex,
                                                ref result);
                                        }

                                        if (code == ReturnCode.Ok)
                                        {
                                            bool all = false;

                                            if (options.IsPresent("-all"))
                                                all = true;

                                            bool inline = false;

                                            if (options.IsPresent("-inline"))
                                                inline = true;

                                            if (startIndex < list.Count)
                                            {
                                                if (startIndex < 0)
                                                    startIndex = 0;

                                                bool ascending = true; // FIXME: PRI 5: Default handling.

                                                if (options.IsPresent("-decreasing"))
                                                    ascending = false;
                                                else if (options.IsPresent("-increasing"))
                                                    ascending = true;

                                                MatchMode mode = StringOps.DefaultMatchMode;
                                                bool sorted = false;

                                                if (options.IsPresent("-sorted"))
                                                {
                                                    mode = MatchMode.Exact;
                                                    sorted = true;
                                                }
                                                else if (options.IsPresent("-exact"))
                                                {
                                                    mode = MatchMode.Exact;
                                                }
                                                else if (options.IsPresent("-substring"))
                                                {
                                                    mode = MatchMode.SubString;
                                                }
                                                else if (options.IsPresent("-regexp"))
                                                {
                                                    mode = MatchMode.RegExp;
                                                }
                                                else if (options.IsPresent("-glob"))
                                                {
                                                    mode = MatchMode.Glob;
                                                }

                                                bool noCase = false;

                                                if (options.IsPresent("-nocase"))
                                                    noCase = true;

                                                bool not = false;

                                                if (options.IsPresent("-not"))
                                                    not = true;

                                                IntDictionary duplicates = null;
                                                IComparer<string> comparer = null;

                                                if (options.IsPresent("-exact") || options.IsPresent("-sorted"))
                                                {
                                                    if (options.IsPresent("-dictionary"))
                                                    {
                                                        comparer = new _Comparers.StringDictionaryComparer(
                                                            interpreter, ascending, indexText, true, false,
                                                            interpreter.InternalCultureInfo, ref duplicates);
                                                    }
                                                    else if (options.IsPresent("-integer"))
                                                    {
                                                        comparer = new _Comparers.StringIntegerComparer(
                                                            interpreter, ascending, indexText, true, false,
                                                            interpreter.InternalCultureInfo, ref duplicates);
                                                    }
                                                    else if (options.IsPresent("-real"))
                                                    {
                                                        comparer = new _Comparers.StringRealComparer(
                                                            interpreter, ascending, indexText, true, false,
                                                            interpreter.InternalCultureInfo, ref duplicates);
                                                    }
                                                    else if (options.IsPresent("-ascii") || true) // FIXME: PRI 5: Default handling.
                                                    {
                                                        //
                                                        // NOTE: Check for things that the .NET Framework will not do by
                                                        //       default (via String.Compare).
                                                        //
                                                        if (!ascending || (indexText != null) || noCase)
                                                            comparer = new _Comparers.StringAsciiComparer(
                                                                interpreter, ascending, indexText, true, noCase,
                                                                false, interpreter.InternalCultureInfo, ref duplicates);
                                                    }
                                                }
                                                else if (options.IsPresent("-regexp"))
                                                {
                                                    comparer = new _Comparers.StringRegexpComparer(
                                                        interpreter, ascending, indexText, true, noCase,
                                                        false, interpreter.InternalCultureInfo, ref duplicates);
                                                }
                                                else if (options.IsPresent("-substring"))
                                                {
                                                    comparer = new _Comparers.StringSubStringComparer(
                                                        interpreter, ascending, indexText, true, noCase,
                                                        false, interpreter.InternalCultureInfo, ref duplicates);
                                                }
                                                else if (options.IsPresent("-glob") || true) // FIXME: PRI 5: Default handling.
                                                {
                                                    comparer = new _Comparers.StringGlobComparer(
                                                        interpreter, ascending, indexText, true, noCase,
                                                        false, interpreter.InternalCultureInfo, ref duplicates);
                                                }

                                                try
                                                {
                                                    string pattern = arguments[argumentIndex + 1];
                                                    int listIndex = Index.Invalid;
                                                    StringList matches = all ? new StringList() : null;

                                                    if (sorted && ((indexText == null) || (comparer != null)) && !all && !not && !inverse)
                                                    {
                                                        //
                                                        // BUGFIX: Use the built-in binary search with the selected comparer.
                                                        //         When no specialized comparer was created (the simple
                                                        //         ascending, non-indexed, case-sensitive case), the elements
                                                        //         must still be compared ORDINALLY -- exactly as the
                                                        //         non-sorted "-exact" path (StringList.IndexOf) and "lsort"
                                                        //         do.  A null comparer here would fall back to
                                                        //         Comparer<string>.Default, which is CULTURE-sensitive, so
                                                        //         the binary search could fail to find an element that IS
                                                        //         present (e.g. "lsearch -sorted {A B a b} b" returned -1)
                                                        //         and disagree with "-exact" / "lsort".
                                                        //
                                                        IComparer<string> searchComparer = (comparer != null) ?
                                                            comparer : StringComparer.Ordinal;

                                                        listIndex = list.BinarySearch(startIndex, list.Count - startIndex, pattern, searchComparer);

                                                        if (listIndex < 0)
                                                            listIndex = Index.Invalid;
                                                    }
                                                    else if ((comparer != null) || all || not || inverse)
                                                    {
                                                        //
                                                        // NOTE: Some custom handling is required, use the selected comparer
                                                        //       and options.
                                                        //
                                                        for (int searchIndex = startIndex; searchIndex < list.Count; searchIndex++)
                                                        {
                                                            //
                                                            // NOTE: If we have a comparer object, use it; otherwise, use our
                                                            //       fallback matching routine.
                                                            //
                                                            bool match;

                                                            if (inverse)
                                                            {
                                                                if (comparer != null)
                                                                    match = (comparer.Compare(pattern, list[searchIndex]) == 0);
                                                                else
                                                                    match = StringOps.Match(interpreter, mode, pattern, list[searchIndex], noCase);
                                                            }
                                                            else
                                                            {
                                                                if (comparer != null)
                                                                    match = (comparer.Compare(list[searchIndex], pattern) == 0);
                                                                else
                                                                    match = StringOps.Match(interpreter, mode, list[searchIndex], pattern, noCase);
                                                            }

                                                            //
                                                            // NOTE: Do we want to consider this to be a match?
                                                            //
                                                            if ((match && !not) || (!match && not))
                                                            {
                                                                if (all)
                                                                {
                                                                    if (inline)
                                                                    {
                                                                        if (subIndexes)
                                                                        {
                                                                            string subValue = null;

                                                                            code = ListOps.SelectFromSubList(
                                                                                interpreter, list[searchIndex], indexText, false,
                                                                                interpreter.InternalCultureInfo, ref subValue, ref result);

                                                                            if (code != ReturnCode.Ok)
                                                                                break;

                                                                            matches.Add(subValue);
                                                                        }
                                                                        else
                                                                        {
                                                                            matches.Add(list[searchIndex]);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (subIndexes)
                                                                        {
                                                                            IntList indexList = new IntList(new int[] { searchIndex });

                                                                            code = ListOps.SelectFromSubList(
                                                                                interpreter, list[searchIndex], indexText, false,
                                                                                interpreter.InternalCultureInfo, ref indexList, ref result);

                                                                            if (code != ReturnCode.Ok)
                                                                                break;

                                                                            matches.Add(indexList.ToString());
                                                                        }
                                                                        else
                                                                        {
                                                                            matches.Add(searchIndex.ToString());
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    listIndex = searchIndex;
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: No special handling required, use built-in find routine.
                                                        //
                                                        listIndex = list.IndexOf(pattern, startIndex);
                                                    }

                                                    //
                                                    // NOTE: Make sure nothing in the search loop failed.
                                                    //
                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // NOTE: Handle the result(s) of the search and build the result.
                                                        //
                                                        if (all)
                                                        {
                                                            //
                                                            // NOTE: This may be an empty list.
                                                            //
                                                            result = matches;
                                                        }
                                                        else
                                                        {
                                                            if (listIndex != Index.Invalid)
                                                            {
                                                                //
                                                                // NOTE: Match found, returning index or value, based on
                                                                //       "-inline" option.
                                                                //
                                                                if (inline)
                                                                {
                                                                    result = list[listIndex];
                                                                }
                                                                else
                                                                {
                                                                    if (subIndexes)
                                                                    {
                                                                        IntList indexList = new IntList(new int[] { listIndex });

                                                                        code = ListOps.SelectFromSubList(
                                                                            interpreter, list[listIndex], indexText, false,
                                                                            interpreter.InternalCultureInfo, ref indexList, ref result);

                                                                        if (code == ReturnCode.Ok)
                                                                            result = indexList.ToString();
                                                                    }
                                                                    else
                                                                    {
                                                                        result = listIndex;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                //
                                                                // NOTE: Match not found, returning invalid index or empty
                                                                //       value, based on "-inline" option.
                                                                //
                                                                if (inline)
                                                                {
                                                                    result = String.Empty;
                                                                }
                                                                else
                                                                {
                                                                    result = Index.Invalid;
                                                                }
                                                            }
                                                        }
                                                    }
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
                                            else
                                            {
                                                if (all || inline)
                                                    result = String.Empty;
                                                else
                                                    result = Index.Invalid;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        result = "-subindices cannot be used without -index option";
                                        code = ReturnCode.Error;
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
                                    result = "wrong # args: should be \"lsearch ?options? list pattern\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lsearch ?options? list pattern\"";
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
