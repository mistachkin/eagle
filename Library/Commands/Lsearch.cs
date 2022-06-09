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
    [ObjectId("3e5dfc83-29bb-44a5-86c4-e78c0b7f21f0")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lsearch : Core
    {
        public Lsearch(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null, OptionFlags.None, 1, Index.Invalid, "-ascii", null),
                            new Option(null, OptionFlags.None, 1, Index.Invalid, "-dictionary", null),
                            new Option(null, OptionFlags.None, 1, Index.Invalid, "-integer", null),
                            new Option(null, OptionFlags.None, 1, Index.Invalid, "-real", null),
                            new Option(null, OptionFlags.None, 2, Index.Invalid, "-decreasing", null),
                            new Option(null, OptionFlags.None, 2, Index.Invalid, "-increasing", null),
                            new Option(null, OptionFlags.None, 3, Index.Invalid, "-exact", null),
                            new Option(null, OptionFlags.None, 3, Index.Invalid, "-substring", null),
                            new Option(null, OptionFlags.None, 3, Index.Invalid, "-glob", null),
                            new Option(null, OptionFlags.None, 3, Index.Invalid, "-regexp", null),
                            new Option(null, OptionFlags.None, 3, Index.Invalid, "-sorted", null), // NOTE: Implies "-exact"
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-variable", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-inverse", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-subindices", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-all", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-inline", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocase", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-not", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-start", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-index", null),
                            Option.CreateEndOfOptions()
                        });

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
                                    Variant value = null;
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
                                                        // NOTE: Use the built-in binary search with the selected comparer.
                                                        //
                                                        listIndex = list.BinarySearch(startIndex, list.Count - startIndex, pattern, comparer);

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
