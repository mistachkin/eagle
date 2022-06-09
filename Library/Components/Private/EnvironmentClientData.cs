/*
 * EnvironmentClientData.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("6d88251b-115f-4f23-9e23-786aa2eee7a6")]
    internal sealed class EnvironmentClientData : ClientData
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static string SetValue = 1.ToString();
        private static string UnsetValue = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private StringDictionary dictionary;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public EnvironmentClientData(
            object data /* in */
            )
            : base(data)
        {
            dictionary = new StringDictionary();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public bool Save(
            IEnumerable<string> names /* in */
            )
        {
            Result error = null;

            if (Save(names, ref error))
                return true;

            TraceOps.DebugTrace(String.Format(
                "Save: error = {0}", FormatOps.WrapOrNull(
                error)), typeof(EnvironmentClientData).Name,
                TracePriority.PlatformError);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Restore(
            IEnumerable<string> names /* in */
            )
        {
            Result error = null;

            if (Restore(names, ref error))
                return true;

            TraceOps.DebugTrace(String.Format(
                "Restore: error = {0}", FormatOps.WrapOrNull(
                error)), typeof(EnvironmentClientData).Name,
                TracePriority.PlatformError);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetOrUnset(
            IEnumerable<string> names, /* in */
            SetDirection direction     /* in */
            )
        {
            Result error = null;

            if (SetOrUnset(names, direction, ref error))
                return true;

            TraceOps.DebugTrace(String.Format(
                "SetOrUnset: error = {0}", FormatOps.WrapOrNull(
                error)), typeof(EnvironmentClientData).Name,
                TracePriority.PlatformError);

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
#if DEBUG || FORCE_TRACE
        private void Dump(
            string methodName,        /* in: OPTIONAL */
            IEnumerable<string> names /* in: OPTIONAL */
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Dump: methodName = {0}, names = {1}, data = {2}",
                FormatOps.WrapOrNull(methodName),
                FormatOps.WrapOrNull(names),
                FormatOps.WrapOrNull(ToString())),
                typeof(EnvironmentClientData).Name,
                TracePriority.EnvironmentDebug);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private bool Save(
            IEnumerable<string> names, /* in */
            ref Result error           /* out */
            )
        {
            try
            {
                if (names == null)
                {
                    error = "invalid names";
                    return false;
                }

                if (dictionary == null)
                {
                    error = "invalid dictionary";
                    return false;
                }

                foreach (string name in names)
                {
                    if (String.IsNullOrEmpty(name))
                        continue;

                    string value;

                    value = Environment.GetEnvironmentVariable(
                        name); /* throw */

                    if (!String.IsNullOrEmpty(value))
                    {
                        dictionary[name] = value;
                    }
                    else
                    {
                        /* IGNORED */
                        dictionary.Remove(name);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }
#if DEBUG || FORCE_TRACE
            finally
            {
                Dump("Save", names);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private bool Restore(
            IEnumerable<string> names, /* in */
            ref Result error           /* out */
            )
        {
            try
            {
                if (names == null)
                {
                    error = "invalid names";
                    return false;
                }

                if (dictionary == null)
                {
                    error = "invalid dictionary";
                    return false;
                }

                foreach (string name in names)
                {
                    if (String.IsNullOrEmpty(name))
                        continue;

                    string value;

                    if (dictionary.TryGetValue(name, out value) &&
                        (value != null))
                    {
                        Environment.SetEnvironmentVariable(
                            name, value); /* throw */
                    }
                    else
                    {
                        Environment.SetEnvironmentVariable(
                            name, null); /* throw */
                    }

                    /* IGNORED */
                    dictionary.Remove(name);
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }
#if DEBUG || FORCE_TRACE
            finally
            {
                Dump("Restore", names);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private bool SetOrUnset(
            IEnumerable<string> names, /* in */
            SetDirection direction,    /* in */
            ref Result error           /* out */
            )
        {
            try
            {
                if (names == null)
                {
                    error = "invalid names";
                    return false;
                }

                string value;

                switch (direction)
                {
                    case SetDirection.Set:
                        {
                            value = SetValue;
                            break;
                        }
                    case SetDirection.Unset:
                        {
                            value = UnsetValue;
                            break;
                        }
                    default:
                        {
                            error = "invalid set direction";
                            return false;
                        }
                }

                foreach (string name in names)
                {
                    if (String.IsNullOrEmpty(name))
                        continue;

                    Environment.SetEnvironmentVariable(
                        name, value); /* throw */
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }
#if DEBUG || FORCE_TRACE
            finally
            {
                Dump("SetOrUnset", names);
            }
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            if (dictionary == null)
                return null;

            return dictionary.KeysAndValuesToString(null, false);
        }
        #endregion
    }
}
