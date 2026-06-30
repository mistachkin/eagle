/*
 * ModuleWrapperDictionary.cs --
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

using ModuleWrapper = Eagle._Wrappers._Module;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary of module wrappers, keyed by name.
    /// It extends the generic wrapper dictionary with a type name suitable for
    /// use within Eagle.
    /// </summary>
    [ObjectId("10badaa0-3d77-4dc1-9e9c-1a79467113ec")]
    internal sealed class ModuleWrapperDictionary : WrapperDictionary<string, ModuleWrapper>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ModuleWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Builds a list of the keys in this dictionary, optionally filtered by
        /// a pattern, and appends the matching keys to the supplied list.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the keys, or null to include all of them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the matching keys.  If this is null, a new
        /// list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the
        /// operation could not be completed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
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

            return GenericOps<string>.FilterList(inputList, list, Index.Invalid,
                Index.Invalid, ToStringFlags.None, pattern, noCase, ref error);
        }
#endif
        #endregion
    }
}
