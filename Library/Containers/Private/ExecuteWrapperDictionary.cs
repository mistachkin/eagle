/*
 * ExecuteWrapperDictionary.cs --
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

using ExecuteWrapper = Eagle._Wrappers._Execute;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string keys to execute
    /// wrapper values.  It extends the underlying wrapper dictionary with a
    /// helper for producing a filtered list of its keys.
    /// </summary>
    [ObjectId("57cd06de-b117-4e92-b7ed-4fe7d4a95476")]
    internal sealed class ExecuteWrapperDictionary :
            WrapperDictionary<string, ExecuteWrapper>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty execute wrapper dictionary.
        /// </summary>
        public ExecuteWrapperDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the keys of the dictionary that match the
        /// specified pattern to the specified list.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which keys are included.  This parameter
        /// may be null to include all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the matching keys.  When null, a new list is
        /// created; otherwise, the matching keys are appended to the existing
        /// list.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public ReturnCode ToList(
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
    }
}
