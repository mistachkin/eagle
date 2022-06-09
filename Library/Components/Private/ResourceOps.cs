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
    [ObjectId("8b47e09f-cc1b-4915-a755-ebd9bc79dfcc")]
    internal static class ResourceOps
    {
        private static readonly string FailureFormat =
            "cannot get string resource #{0}: {1}";

        ///////////////////////////////////////////////////////////////////////

        private static string Failure(
            ResourceId id, /* in */
            string message /* in */
            )
        {
            return String.Format(FailureFormat, id, message);
        }

        ///////////////////////////////////////////////////////////////////////

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
