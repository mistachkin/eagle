/*
 * ArgumentManager.cs --
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

namespace Eagle._Interfaces.Public
{
    [ObjectId("ea382119-493f-4a78-bc45-24fd7ed0b4e9")]
    public interface IArgumentManager
    {
        ///////////////////////////////////////////////////////////////////////
        // ARGUMENT & OPTION HANDLING
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The "listCount" argument is only used when processing any
        //       MustBeIndex (e.g. "end-<int>") style options that apply to a
        //       given list.  When there are no index style options, simply
        //       pass zero as the value.
        //
        ReturnCode CheckOptions(
            OptionDictionary options,
            ArgumentList arguments,
            int listCount,
            int startIndex,
            int stopIndex,
            ref int nextIndex,
            ref Result error
            );

        ReturnCode GetOptions(
            OptionDictionary options,
            ArgumentList arguments,
            int listCount,
            int startIndex,
            int stopIndex,
            bool strict,
            ref int nextIndex,
            ref Result error
            );

        ReturnCode GetOptions(
            IIdentifier identifier,
            OptionDictionary options,
            ArgumentList arguments,
            ref int argumentIndex,
            ref Result error
            );

        //
        // NOTE: Get, set, or reset the arguments (i.e. the "argv" variable)
        //       for a script to use.
        //
        ReturnCode GetArguments(
            ref StringList arguments,
            ref Result error
            );

        ReturnCode GetArguments(
            ref StringList arguments,
            bool strict,
            ref Result error
            );

        ReturnCode SetArguments(
            StringList arguments,
            ref Result error
            );

        ReturnCode SetArguments(
            StringList arguments,
            bool strict,
            ref Result error
            );

        //
        // NOTE: Merge two sets of arguments, including those that represent
        //       valid options, and return the resulting list.
        //
        ReturnCode MergeArguments(
            OptionDictionary options,
            ArgumentList arguments1,
            ArgumentList arguments2,
            int startIndex1,
            int startIndex2,
            bool skipFirst1,
            bool skipFirst2,
            bool useRemaining1,
            ref ArgumentList arguments,
            ref Result error
            );
    }
}
