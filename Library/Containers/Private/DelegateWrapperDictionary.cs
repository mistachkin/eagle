/*
 * DelegateWrapperDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

#if DEAD_CODE
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
#endif

using DelegateWrapper = Eagle._Wrappers.Delegate;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps delegate names to the
    /// delegate wrapper objects that manage them.  It is a thin specialization
    /// of the generic wrapper dictionary.
    /// </summary>
    [ObjectId("30c67aea-9696-4ead-907a-5f65de826476")]
    internal sealed class DelegateWrapperDictionary :
            WrapperDictionary<string, DelegateWrapper>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public DelegateWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method produces a list of the delegate names contained in this
        /// dictionary, optionally restricted to those matching the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each delegate name must match in order to be included
        /// in the result.  This parameter may be null, in which case all names
        /// are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the list of matching delegate names.  If this
        /// is null, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private ReturnCode ToList(
            string pattern,
            bool noCase,
            ref StringList list,
            ref Result error
            )
        {
            StringList inputList = new StringList(this.Keys);

            if (list == null)
                list = new StringList();

            return GenericOps<string>.FilterList(
                inputList, list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, pattern, noCase, ref error);
        }
#endif
        #endregion
    }
}
