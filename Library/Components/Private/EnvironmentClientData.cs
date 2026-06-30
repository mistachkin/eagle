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
    /// <summary>
    /// This class holds a saved snapshot of environment variable values,
    /// allowing a set of named environment variables to be captured, later
    /// restored to their captured values, or bulk set and unset.  It is used
    /// as client data carried alongside an operation.
    /// </summary>
    [ObjectId("6d88251b-115f-4f23-9e23-786aa2eee7a6")]
    internal sealed class EnvironmentClientData : ClientData
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The value assigned to an environment variable when it is being set
        /// (the string representation of one).
        /// </summary>
        private static string SetValue = 1.ToString();

        /// <summary>
        /// The value assigned to an environment variable when it is being
        /// unset (a null string).
        /// </summary>
        private static string UnsetValue = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The dictionary of captured environment variable names and their
        /// saved values.
        /// </summary>
        private StringDictionary dictionary;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with the specified client data
        /// and an empty snapshot dictionary.
        /// </summary>
        /// <param name="data">
        /// The client data value to wrap.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// Captures the current values of the specified environment variables
        /// into this instance, tracing any error that occurs.
        /// </summary>
        /// <param name="names">
        /// The names of the environment variables to capture.
        /// </param>
        /// <returns>
        /// True if the values were captured successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Restores the previously captured values of the specified
        /// environment variables, tracing any error that occurs.
        /// </summary>
        /// <param name="names">
        /// The names of the environment variables to restore.
        /// </param>
        /// <returns>
        /// True if the values were restored successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Sets or unsets the specified environment variables according to the
        /// specified direction, tracing any error that occurs.
        /// </summary>
        /// <param name="names">
        /// The names of the environment variables to set or unset.
        /// </param>
        /// <param name="direction">
        /// The direction indicating whether the variables should be set or
        /// unset.
        /// </param>
        /// <returns>
        /// True if the variables were set or unset successfully; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// Emits diagnostic trace output describing the current state of this
        /// instance.
        /// </summary>
        /// <param name="methodName">
        /// The name of the calling method.  This parameter is optional and may
        /// be null.
        /// </param>
        /// <param name="names">
        /// The names of the environment variables relevant to the calling
        /// method.  This parameter is optional and may be null.
        /// </param>
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

        /// <summary>
        /// Captures the current values of the specified environment variables
        /// into this instance.
        /// </summary>
        /// <param name="names">
        /// The names of the environment variables to capture.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the values were captured successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Restores the previously captured values of the specified
        /// environment variables.
        /// </summary>
        /// <param name="names">
        /// The names of the environment variables to restore.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the values were restored successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Sets or unsets the specified environment variables according to the
        /// specified direction.
        /// </summary>
        /// <param name="names">
        /// The names of the environment variables to set or unset.
        /// </param>
        /// <param name="direction">
        /// The direction indicating whether the variables should be set or
        /// unset.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the variables were set or unset successfully; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// Returns a string representation of the captured environment
        /// variable names and values.
        /// </summary>
        /// <returns>
        /// A string containing the captured names and values, or null if no
        /// snapshot dictionary is present.
        /// </returns>
        public override string ToString()
        {
            if (dictionary == null)
                return null;

            return dictionary.KeysAndValuesToString(null, false);
        }
        #endregion
    }
}
