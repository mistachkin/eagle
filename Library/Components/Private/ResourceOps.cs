/*
 * ResourceOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Globalization;
using System.Resources;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods used to look up named string
    /// resources within the resources associated with an interpreter and to
    /// enumerate the available resource names.
    /// </summary>
    [ObjectId("8b47e09f-cc1b-4915-a755-ebd9bc79dfcc")]
    internal static class ResourceOps
    {
        /// <summary>
        /// The format string used to build a fallback error message when a
        /// string resource cannot be obtained.
        /// </summary>
        private static readonly string FailureFormat =
            "cannot get string resource #{0}: {1}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a fallback error message describing why the
        /// specified string resource could not be obtained.
        /// </summary>
        /// <param name="id">
        /// The identifier of the string resource that could not be obtained.
        /// </param>
        /// <param name="message">
        /// The message describing why the string resource could not be obtained.
        /// </param>
        /// <returns>
        /// A formatted error message describing the failure.
        /// </returns>
        private static string Failure(
            ResourceId id, /* in */
            string message /* in */
            )
        {
            return String.Format(FailureFormat, id, message);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up the string resource for the specified identifier
        /// within the culture configured for the interpreter, performing any
        /// requested parameter insertions.  This method never returns null; a
        /// fallback error message is returned on failure.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose resources are searched.  This parameter may be
        /// null, in which case a fallback error message is returned.
        /// </param>
        /// <param name="id">
        /// The identifier of the string resource to look up.
        /// </param>
        /// <param name="objects">
        /// The optional values to insert into the resource string.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The resolved string resource, or a fallback error message if it
        /// cannot be obtained.
        /// </returns>
        public static string GetString( /* CANNOT RETURN NULL */
            Interpreter interpreter, /* in */
            ResourceId id,           /* in */
            params object[] objects  /* in */
            )
        {
            if (interpreter != null)
            {
                //
                // NOTE: Search for the resource string for the specified Id
                //       in the culture configured for the interpreter.
                //
                string result = interpreter.GetString(null, id.ToString());

                //
                // NOTE: Did we find the resource string we were searching
                //       for?
                //
                if (result != null)
                {
                    //
                    // NOTE: Perform parameter insertions, if necessary.
                    //
                    if ((objects != null) && (objects.Length > 0))
                        result = String.Format(result, objects);
                }
                else
                {
                    //
                    // NOTE: Return an appropriate fallback error message.
                    //
                    result = Failure(id, "not found");
                }

                return result;
            }
            else
            {
                //
                // NOTE: At this point, we cannot even try to lookup the
                //       resource string because we require the interpreter
                //       to be able to do that.
                //
                return Failure(id, "invalid interpreter");
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method collects the names of all resources contained in the
        /// specified resource manager for the specified culture, appending
        /// them to the specified list.
        /// </summary>
        /// <param name="resourceManager">
        /// The resource manager whose resource names are collected.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which resource names are collected.  This parameter
        /// may be null, in which case the default culture is used.
        /// </param>
        /// <param name="createIfNotExists">
        /// Non-zero to create the resource set for the specified culture if it
        /// does not already exist.
        /// </param>
        /// <param name="list">
        /// Upon return, receives the collected resource names, which are added
        /// to the existing list when one is provided.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the resource names were collected
        /// successfully; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNames(
            ResourceManager resourceManager, /* in */
            CultureInfo cultureInfo,         /* in */
            bool createIfNotExists,          /* in */
            ref StringList list,             /* in, out */
            ref Result error                 /* out */
            )
        {
            if (resourceManager == null)
            {
                error = "invalid resource manager";
                return ReturnCode.Error;
            }

            if (cultureInfo == null)
                cultureInfo = Value.GetDefaultCulture();

            ResourceSet resourceSet = resourceManager.GetResourceSet(
                cultureInfo, createIfNotExists, false);

            if (resourceSet == null)
            {
                error = "invalid resource set";
                return ReturnCode.Error;
            }

            StringList localList = null;

            foreach (DictionaryEntry entry in resourceSet)
            {
                string name = StringOps.GetStringFromObject(entry.Key);

                if (name == null)
                    continue;

                if (localList == null)
                    localList = new StringList();

                localList.Add(name);
            }

            if (localList != null)
            {
                if (list == null)
                    list = new StringList();

                list.AddRange(localList);
            }

            return ReturnCode.Ok;
        }
    }
}
