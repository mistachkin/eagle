/*
 * GlobalConfiguration.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
    [ObjectId("ec7e7b01-b6c3-40fb-87a0-4a9eefc6f192")]
    internal static class GlobalConfiguration
    {
        #region Private Constants
        //
        // NOTE: This format string is used when building the package
        //       prefixed environment variable names (e.g. Eagle_Foo).
        //
        private static readonly string EnvVarFormat = "{0}_{1}";

        ///////////////////////////////////////////////////////////////////////

        //
        //
        // NOTE: This is the prefix (not including the trailing underscore)
        //       that is used when handling environment variables that are
        //       package-specific.
        //
        // WARNING: *HACK* Hard-code the package environment variable prefix
        //          here because using the package name would require using
        //          the GlobalState class, which relies upon this class.
        //
        private static readonly string EnvVarPrefix = "Eagle";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: When this value is non-zero, trace messages will be written
        //       whenever a global configuration value is read, modified, or
        //       removed.
        //
        // HACK: This is purposely not read-only.
        //
        private static bool DefaultVerbose = ShouldBeVerbose();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags will be added (or removed) from every call into
        //       this class that uses the GetFlags helper method.  This will
        //       be very useful in testing and debugging.
        //
        // HACK: These are purposely not read-only.
        //
        private static ConfigurationFlags enableFlags = ConfigurationFlags.None;
        private static ConfigurationFlags disableFlags = ConfigurationFlags.None;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        private static bool ShouldBeVerbose() /* THREAD-SAFE */
        {
            if (!Build.Debug)
                return false;

            if (CommonOps.Environment.DoesVariableExist(EnvVars.NoVerbose))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string FormatDoesExist(
            string value /* in */
            ) /* THREAD-SAFE */
        {
            return String.Format(
                "DOES {0}EXIST", (value == null) ? "NOT " : String.Empty);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeMutateValue(
            ConfigurationFlags flags, /* in */
            ref string value,         /* in, out */
            ref Result error          /* out */
            ) /* THREAD-SAFE */
        {
            if (FlagOps.HasFlags(flags, ConfigurationFlags.Expand, true))
                value = CommonOps.Environment.ExpandVariables(value);

            if (FlagOps.HasFlags(flags, ConfigurationFlags.ListValue, true))
            {
                //
                // TODO: *PERF* We cannot have this call to SplitList perform
                //       any caching because the returned list is modified by
                //       the code below.
                //
                StringList list = null;

                if (ParserOps<string>.SplitList(
                        null, value, 0, Length.Invalid, false, ref list,
                        ref error) != ReturnCode.Ok)
                {
                    value = null;
                    return false;
                }

                if (FlagOps.HasFlags(
                        flags, ConfigurationFlags.NativePathValue, true))
                {
                    int count = list.Count;

                    for (int index = 0; index < count; index++)
                        list[index] = PathOps.GetNativePath(list[index]);
                }

                value = list.ToString();
            }
            else
            {
                if (FlagOps.HasFlags(
                        flags, ConfigurationFlags.NativePathValue, true))
                {
                    value = PathOps.GetNativePath(value);
                }
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Configuration Value Management Methods
        private static string GetValue(
            string variable,            /* in */
            ConfigurationFlags flags,   /* in */
            ref string prefixedVariable /* out */
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: The error message, if any, of a step in the process
            //       that failed and caused a null value to be returned.
            //
            string value = null;

            //
            // NOTE: The error message, if any, of a step in the process that
            //       failed and caused a null value to be returned.
            //
            Result error = null;

            //
            // NOTE: If the variable name is null or empty, return the default
            //       value (null) instead of potentially throwing an exception
            //       later.
            //
            if (String.IsNullOrEmpty(variable))
                goto done;

            //
            // NOTE: Try to get the variable name without the package name
            //       prefix?
            //
            bool unprefixed = FlagOps.HasFlags(
                flags, ConfigurationFlags.Unprefixed, true);

            //
            // NOTE: Set the variable name prefixed by package name instead?
            //
            if ((EnvVarPrefix != null) &&
                FlagOps.HasFlags(flags, ConfigurationFlags.Prefixed, true))
            {
                prefixedVariable = String.Format(
                    EnvVarFormat, EnvVarPrefix, variable);
            }

            //
            // NOTE: Does the caller want to check the environment variables?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.Environment, true))
            {
                //
                // NOTE: Try the variable name prefixed by our package name
                //       first?
                //
                if ((prefixedVariable != null) && (value == null))
                    value = CommonOps.Environment.GetVariable(prefixedVariable);

                //
                // NOTE: Failing that, just try for the variable name?
                //
                if (unprefixed && !FlagOps.HasFlags(
                        flags, ConfigurationFlags.SkipUnprefixedEnvironment,
                        true) && (value == null))
                {
                    value = CommonOps.Environment.GetVariable(variable);
                }
            }

            //
            // NOTE: Does the caller want to check the loaded AppSettings?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.AppSettings, true))
            {
                //
                // NOTE: Try the variable name prefixed by our package name
                //       first?
                //
                if ((prefixedVariable != null) && (value == null))
                    value = ConfigurationOps.GetAppSetting(prefixedVariable);

                //
                // NOTE: Failing that, just try for the variable name?
                //
                if (unprefixed && !FlagOps.HasFlags(
                        flags, ConfigurationFlags.SkipUnprefixedAppSettings,
                        true) && (value == null))
                {
                    value = ConfigurationOps.GetAppSetting(variable);
                }
            }

            //
            // NOTE: If necessary, mutate the value to be returned based on
            //       the flags specified by the caller.
            //
            if (!String.IsNullOrEmpty(value) &&
                !MaybeMutateValue(flags, ref value, ref error))
            {
                goto done; /* REDUNDANT */
            }

        done:

            //
            // NOTE: Output diagnostic message about the configuration value
            //       request, if necessary.
            //
            if (!FlagOps.HasFlags(
                    flags, ConfigurationFlags.ExistOnly, true) &&
                (DefaultVerbose || FlagOps.HasFlags(
                    flags, ConfigurationFlags.Verbose, true)))
            {
                TraceOps.MaybeDebugTrace(String.Format(
                    "GetValue: variable = {0}, prefixedVariable = {1}, " +
                    "value = {2}, defaultVerbose = {3}, flags = {4}, " +
                    "error = {5}", FormatOps.WrapOrNull(variable),
                    FormatOps.WrapOrNull(prefixedVariable),
                    FormatOps.WrapOrNull(value), DefaultVerbose,
                    FormatOps.WrapOrNull(flags),
                    FormatOps.WrapOrNull(error)),
                    typeof(GlobalConfiguration).Name,
                    TracePriority.StartupDebug);
            }

            return value;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void SetValue(
            string variable,            /* in */
            string value,               /* in */
            ConfigurationFlags flags,   /* in */
            ref string prefixedVariable /* out */
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: The error message, if any, of a step in the process
            //       that failed and caused a null value to be returned.
            //
            Result error = null;

            //
            // NOTE: If the variable name is null or empty, do nothing.
            //
            if (String.IsNullOrEmpty(variable))
                goto done;

            //
            // NOTE: Try to set the variable name without the package name
            //       prefix?
            //
            bool unprefixed = FlagOps.HasFlags(
                flags, ConfigurationFlags.Unprefixed, true);

            //
            // NOTE: Set the variable name prefixed by package name instead?
            //
            if ((EnvVarPrefix != null) &&
                FlagOps.HasFlags(flags, ConfigurationFlags.Prefixed, true))
            {
                prefixedVariable = String.Format(
                    EnvVarFormat, EnvVarPrefix, variable);
            }

            //
            // NOTE: If necessary, mutate the value to be returned based on
            //       the flags specified by the caller.
            //
            if (!String.IsNullOrEmpty(value) &&
                !MaybeMutateValue(flags, ref value, ref error))
            {
                goto done;
            }

            //
            // NOTE: Does the caller want to modify the loaded AppSettings?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.AppSettings, true))
            {
                //
                // NOTE: Attempt to set the requested AppSettings value,
                //       also using the prefixed name if requested.
                //
                if (unprefixed && !FlagOps.HasFlags(
                        flags, ConfigurationFlags.SkipUnprefixedAppSettings,
                        true))
                {
                    /* NO RESULT */
                    ConfigurationOps.SetAppSetting(variable, value);
                }

                if (prefixedVariable != null)
                {
                    /* NO RESULT */
                    ConfigurationOps.SetAppSetting(prefixedVariable, value);
                }
            }

            //
            // NOTE: Does the caller want to modify the environment variables?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.Environment, true))
            {
                //
                // NOTE: Attempt to set the requested environment variable,
                //       also using the prefixed name if requested.
                //
                if (unprefixed && !FlagOps.HasFlags(
                        flags, ConfigurationFlags.SkipUnprefixedEnvironment,
                        true))
                {
                    /* IGNORED */
                    CommonOps.Environment.SetVariable(variable, value);
                }

                if (prefixedVariable != null)
                {
                    /* IGNORED */
                    CommonOps.Environment.SetVariable(prefixedVariable, value);
                }
            }

        done:

            //
            // NOTE: Output diagnostic message about the configuration value
            //       request, if necessary.
            //
            if (DefaultVerbose ||
                FlagOps.HasFlags(flags, ConfigurationFlags.Verbose, true))
            {
                TraceOps.MaybeDebugTrace(String.Format(
                    "SetValue: variable = {0}, prefixedVariable = {1}, " +
                    "value = {2}, defaultVerbose = {3}, flags = {4}, " +
                    "error = {5}", FormatOps.WrapOrNull(variable),
                    FormatOps.WrapOrNull(prefixedVariable),
                    FormatOps.WrapOrNull(value), DefaultVerbose,
                    FormatOps.WrapOrNull(flags),
                    FormatOps.WrapOrNull(error)),
                    typeof(GlobalConfiguration).Name,
                    TracePriority.StartupDebug);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void UnsetValue(
            string variable,            /* in */
            ConfigurationFlags flags,   /* in */
            ref string prefixedVariable /* out */
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: If the variable name is null or empty, do nothing.
            //
            if (String.IsNullOrEmpty(variable))
                goto done;

            //
            // NOTE: Try to unset the variable name without the package name
            //       prefix?
            //
            bool unprefixed = FlagOps.HasFlags(
                flags, ConfigurationFlags.Unprefixed, true);

            //
            // NOTE: Set the variable name prefixed by package name instead?
            //
            if ((EnvVarPrefix != null) &&
                FlagOps.HasFlags(flags, ConfigurationFlags.Prefixed, true))
            {
                prefixedVariable = String.Format(
                    EnvVarFormat, EnvVarPrefix, variable);
            }

            //
            // NOTE: Does the caller want to remove the loaded AppSettings?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.AppSettings, true))
            {
                //
                // NOTE: Try to unset the requested AppSettings value(s).
                //
                if (unprefixed && !FlagOps.HasFlags(
                        flags, ConfigurationFlags.SkipUnprefixedAppSettings,
                        true))
                {
                    /* NO RESULT */
                    ConfigurationOps.UnsetAppSetting(variable);
                }

                if (prefixedVariable != null)
                {
                    /* NO RESULT */
                    ConfigurationOps.UnsetAppSetting(prefixedVariable);
                }
            }

            //
            // NOTE: Does the caller want to remove the environment variables?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.Environment, true))
            {
                //
                // NOTE: Try to unset the requested environment variable(s).
                //
                if (unprefixed && !FlagOps.HasFlags(
                        flags, ConfigurationFlags.SkipUnprefixedEnvironment,
                        true))
                {
                    /* IGNORED */
                    CommonOps.Environment.UnsetVariable(variable);
                }

                if (prefixedVariable != null)
                {
                    /* IGNORED */
                    CommonOps.Environment.UnsetVariable(prefixedVariable);
                }
            }

        done:

            //
            // NOTE: Output diagnostic message about the configuration value
            //       request, if necessary.
            //
            if (DefaultVerbose ||
                FlagOps.HasFlags(flags, ConfigurationFlags.Verbose, true))
            {
                TraceOps.MaybeDebugTrace(String.Format(
                    "UnsetValue: variable = {0}, prefixedVariable = {1}, " +
                    "defaultVerbose = {2}, flags = {3}",
                    FormatOps.WrapOrNull(variable),
                    FormatOps.WrapOrNull(prefixedVariable),
                    DefaultVerbose, FormatOps.WrapOrNull(flags)),
                    typeof(GlobalConfiguration).Name,
                    TracePriority.StartupDebug);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Configuration Value Management Methods
        public static ConfigurationFlags GetFlags(
            ConfigurationFlags flags, /* in */
            bool verbose              /* in */
            ) /* THREAD-SAFE */
        {
            ConfigurationFlags result = flags;

            if (verbose)
                result |= ConfigurationFlags.Verbose;

            ConfigurationFlags localFlags = enableFlags;

            if (localFlags != ConfigurationFlags.None)
                result |= localFlags;

            localFlags = disableFlags;

            if (localFlags != ConfigurationFlags.None)
                result &= ~localFlags;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DoesValueExist(
            string variable,         /* in */
            ConfigurationFlags flags /* in */
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: The default value is null, which means that the value
            //       is not available and/or not set.
            //
            string value = null; /* NOT USED */

            //
            // NOTE: Delegate to the private method.  This does the actual
            //       work, including any necessary diagnostic messaging.
            //
            return DoesValueExist(variable, flags, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DoesValueExist(
            string variable,          /* in */
            ConfigurationFlags flags, /* in */
            ref string value          /* out */
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: This will contain the variable name, with the optional
            //       default prefix, if necessary.
            //
            string prefixedVariable = null;

            //
            // NOTE: Delegate to the private method.  This does the actual
            //       work; however, prevent it from emitting a diagnostic
            //       message by passing the ExistOnly flag (i.e. we have a
            //       diagnostic message of our own).
            //
            string localValue = GetValue(variable,
                flags | ConfigurationFlags.ExistOnly, ref prefixedVariable);

            //
            // NOTE: Output diagnostic message about the configuration value
            //       request, if necessary.
            //
            if (DefaultVerbose ||
                FlagOps.HasFlags(flags, ConfigurationFlags.Verbose, true))
            {
                TraceOps.MaybeDebugTrace(String.Format(
                    "DoesValueExist: variable = {0}, " +
                    "prefixedVariable = {1}, {2}, " +
                    "defaultVerbose = {3}, flags = {4}",
                    FormatOps.WrapOrNull(variable),
                    FormatOps.WrapOrNull(prefixedVariable),
                    FormatDoesExist(localValue), DefaultVerbose,
                    FormatOps.WrapOrNull(flags)),
                    typeof(GlobalConfiguration).Name,
                    TracePriority.StartupDebug);
            }

            //
            // NOTE: This method returns non-zero if the specified variable
            //       exists.  Given how this subsystem works, a null value
            //       can never be valid for a variable that exists.
            //
            value = localValue;

            return (localValue != null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetValue(
            string variable,         /* in */
            ConfigurationFlags flags /* in */
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: This will contain the variable name, with the optional
            //       default prefix, if necessary.  The resulting value is
            //       not used by this method.
            //
            string prefixedVariable = null; /* NOT USED */

            //
            // NOTE: Delegate to the private method.  This does the actual
            //       work, including any necessary diagnostic messaging.
            //
            return GetValue(variable, flags, ref prefixedVariable);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetValue(
            string variable,         /* in */
            string value,            /* in */
            ConfigurationFlags flags /* in */
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: This will contain the variable name, with the optional
            //       default prefix, if necessary.  The resulting value is
            //       not used by this method.
            //
            string prefixedVariable = null; /* NOT USED */

            SetValue(variable, value, flags, ref prefixedVariable);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void UnsetValue(
            string variable,         /* in */
            ConfigurationFlags flags /* in */
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: This will contain the variable name, with the optional
            //       default prefix, if necessary.  The resulting value is
            //       not used by this method.
            //
            string prefixedVariable = null; /* NOT USED */

            UnsetValue(variable, flags, ref prefixedVariable);
        }
        #endregion
    }
}
