/*
 * PolicyOps.cs --
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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using SubCommandPair =
    System.Collections.Generic.KeyValuePair<
        string, Eagle._Interfaces.Public.ISubCommand>;

namespace Eagle._Components.Private
{
    [ObjectId("ab00e89a-8a1f-404b-91fd-32d10d0f44ba")]
    internal static class PolicyOps
    {
        #region Private Constants
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private const int HashCount = 3;

        ///////////////////////////////////////////////////////////////////////

        private const string UnsafeObjectError =
            "permission denied: safe interpreter cannot use object from {0}";

        ///////////////////////////////////////////////////////////////////////

        private const string UnsafeTypeError =
            "permission denied: safe interpreter cannot use type from {0}";

        ///////////////////////////////////////////////////////////////////////

        private const string UnsafeFileError =
            "permission denied: safe interpreter cannot use file from {0}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly PolicyDecisionType[] DecisionTypes = {
            PolicyDecisionType.Command, PolicyDecisionType.Script,
            PolicyDecisionType.File, PolicyDecisionType.Stream
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private (Internal) Data
        #region Default Sub-Command Policy Lists
        //
        // NOTE: This is the default list of [clock] sub-commands that
        //       are ALLOWED to be used by scripts running in a "safe"
        //       interpreter.
        //
        internal static readonly StringDictionary AllowedClockSubCommandNames =
            new StringDictionary(new string[] {
            "buildnumber", "days", "duration", "filetime", "format",
            "isvalid", "scan", "seconds"
        }, true, false);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default list of [file] sub-commands that
        //       are ALLOWED to be used by scripts running in a "safe"
        //       interpreter.
        //
        internal static readonly StringDictionary AllowedFileSubCommandNames =
            new StringDictionary(new string[] {
            "channels", "dirname", "join", "split", "validname"
        }, true, false);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default list of [info] sub-commands that are
        //       ALLOWED to be used by scripts running in a "safe" interpreter.
        //
        internal static readonly StringDictionary AllowedInfoSubCommandNames =
            new StringDictionary(new string[] {
            "appdomain", "args", "body", "commands", "complete", "context",
            "default", "engine", "ensembles", "exists", "functions",
            "globals", "level", "library", "locals", "nprocs", "objects",
            "operands", "operators", "patchlevel", "procs", "script",
            "subcommands", "tclversion", "vars"
        }, true, false);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default list of [interp] sub-commands that are ALLOWED to be
        //       used by scripts running in a "safe" interpreter.
        //
        internal static readonly StringDictionary AllowedInterpSubCommandNames =
            new StringDictionary(new string[] {
            "alias", "aliases", "cancel", "children", "exists", "issafe",
            "issdk", "rename"
        }, true, false);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default list of [object] sub-commands that are ALLOWED to be
        //       used by scripts running in a "safe" interpreter.
        //
        internal static readonly StringDictionary AllowedObjectSubCommandNames =
            new StringDictionary(new string[] {
            "dispose", "exists", "invoke", "invokeall", "invokeraw",
            "isnull", "isoftype"
        }, true, false);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default list of [package] sub-commands that are NOT ALLOWED
        //       to be used by scripts running in a "safe" interpreter.
        //
        internal static readonly StringDictionary DisallowedPackageSubCommandNames =
            new StringDictionary(new string[] {
            "indexes", "relativefilename", "reset", "scan", "vloaded"
        }, true, false);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default policies that are added to every interpreter.
        //
        internal static IEnumerable<ExecuteCallback> CommandCallbacks =
            new ExecuteCallback[] {
            ClockCommandCallback, FileCommandCallback,
            InfoCommandCallback, InterpCommandCallback,
            ObjectCommandCallback, PackageCommandCallback,
            SourceCommandCallback
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        private static IEnumerable<PolicyDecisionType> GetDecisionTypes()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (DecisionTypes == null)
                    return null;

                return new List<PolicyDecisionType>(DecisionTypes);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static PolicyDecision? QueryDecision(
            Interpreter interpreter,
            PolicyDecisionType decisionType,
            bool final
            )
        {
            if (interpreter != null)
            {
                switch (decisionType & PolicyDecisionType.QueryMask)
                {
                    case PolicyDecisionType.Command:
                        {
                            return final ?
                                interpreter.CommandFinalDecision :
                                interpreter.CommandInitialDecision;
                        }
                    case PolicyDecisionType.Script:
                        {
                            return final ?
                                interpreter.ScriptFinalDecision :
                                interpreter.ScriptInitialDecision;
                        }
                    case PolicyDecisionType.File:
                        {
                            return final ?
                                interpreter.FileFinalDecision :
                                interpreter.FileInitialDecision;
                        }
                    case PolicyDecisionType.Stream:
                        {
                            return final ?
                                interpreter.StreamFinalDecision :
                                interpreter.StreamInitialDecision;
                        }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode EvaluateScript(
            Interpreter interpreter, /* in */
            string text,             /* in */
            ArgumentList arguments,  /* in */
            PolicyFlags policyFlags, /* in */
            ref Result result        /* out */
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;

            //
            // NOTE: Does the caller want us to attempt to split
            //       the script into a list?
            //
            bool splitList = FlagOps.HasFlags(
                policyFlags, PolicyFlags.SplitList, true);

            //
            // NOTE: Does the caller provide an argument list to
            //       append to the script prior to evaluating it?
            //
            bool appendArguments = FlagOps.HasFlags(
                policyFlags, PolicyFlags.Arguments, true);

            if (splitList)
            {
                //
                // NOTE: We are in "list" mode.  Attempt to parse
                //       the script as a list and then append the
                //       supplied argument list before converting
                //       it back to a string.
                //
                StringList list = null;

                code = ParserOps<string>.SplitList(
                    interpreter, text, 0, Length.Invalid, false,
                    ref list, ref result);

                if (code == ReturnCode.Ok)
                {
                    if (appendArguments && (arguments != null))
                        list.Add(arguments);

                    text = list.ToString();
                }
            }
            else if (appendArguments && (arguments != null))
            {
                //
                // NOTE: Arguments were supplied; however, we are
                //       not operating in "list" mode.  Append the
                //       arguments as a single string to the script
                //       (separated by a single intervening space).
                //
                StringBuilder builder = StringOps.NewStringBuilder(
                    text);

                builder.Append(Characters.Space);
                builder.Append(arguments);

                text = builder.ToString();
            }

            //
            // NOTE: Did the list parsing code above succeed, if it
            //       was requested?
            //
            if (code == ReturnCode.Ok)
            {
                code = interpreter.EvaluateScript(
                    text, ref result); /* EXEMPT */
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode TryParseHash(
            Interpreter interpreter,      /* in: OPTIONAL */
            string hash,                  /* in */
            ref PolicyType policyType,    /* in */
            ref string hashAlgorithmName, /* out */
            ref byte[] hashValue,         /* out */
            ref Result error              /* out */
            )
        {
            //
            // NOTE: The format of the hash entry strings must be as
            //       follows:
            //
            //       PolicyType <space> HashAlgorithmName <space> HashValue
            //
            //       The policy type MUST parse to a valid enumeration
            //       value.
            //
            //       The hash algorithm name SHOULD (almost always) be
            //       SHA256; however, other valid hash algorithm names
            //       MAY be accepted.
            //
            //       The hash value MUST be a string representation of
            //       a Base16 number with an optional "0x" prefix -OR-
            //       a Base64 encoded byte array for the computed hash
            //       over the entire file.
            //
            if (String.IsNullOrEmpty(hash))
            {
                error = "invalid hash";
                return ReturnCode.Error;
            }

            StringList list = null;

            if (ParserOps<string>.SplitList(
                    interpreter, hash, 0, Length.Invalid, true,
                    ref list, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            int count = list.Count;

            if (count < HashCount)
            {
                error = String.Format(
                    "need at least {0} elements for hash, have {1}",
                    HashCount, count);

                return ReturnCode.Error;
            }

            CultureInfo cultureInfo = null;

            if (interpreter != null)
                cultureInfo = interpreter.InternalCultureInfo;

            object enumValue;

            enumValue = EnumOps.TryParseFlags(
                interpreter, typeof(PolicyType), null, list[0],
                cultureInfo, true, true, true, ref error);

            if (!(enumValue is PolicyType))
                return ReturnCode.Error;

            byte[] localHashValue = null;

            if (StringOps.GetBytesFromString(
                    list[2], cultureInfo, ref localHashValue,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            policyType = (PolicyType)enumValue;
            hashAlgorithmName = list[1];
            hashValue = localHashValue;

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Sub-Command Support Methods
        private static EnsembleDictionary FilterSubCommands(
            EnsembleDictionary possibleSubCommands,  /* in */
            EnsembleDictionary disallowedSubCommands /* in */
            )
        {
            if (possibleSubCommands == null)
                return null;

            if (disallowedSubCommands == null)
                return possibleSubCommands;

            EnsembleDictionary subCommands = new EnsembleDictionary();

            foreach (SubCommandPair pair in possibleSubCommands)
            {
                string subCommandName = pair.Key;

                if ((subCommandName != null) &&
                    !disallowedSubCommands.ContainsKey(subCommandName))
                {
                    subCommands.Add(subCommandName, pair.Value);
                }
            }

            return subCommands;
        }

        ///////////////////////////////////////////////////////////////////////

        private static EnsembleDictionary GetSubCommands(
            IEnsemble ensemble, /* in */
            bool allowed        /* in */
            )
        {
            if (ensemble == null)
                return null;

            IPolicyEnsemble policyEnsemble = ensemble as IPolicyEnsemble;

            if (policyEnsemble == null)
                return ensemble.SubCommands;

            EnsembleDictionary disallowedSubCommands =
                policyEnsemble.DisallowedSubCommands;

            if (!allowed)
                return disallowedSubCommands;

            EnsembleDictionary possibleSubCommands =
                policyEnsemble.AllowedSubCommands;

            if (possibleSubCommands == null)
                possibleSubCommands = ensemble.SubCommands;

            return FilterSubCommands(
                possibleSubCommands, disallowedSubCommands);
        }

        ///////////////////////////////////////////////////////////////////////

        private static StringDictionary GetSubCommandNames(
            IEnsemble ensemble, /* in */
            bool allowed        /* in */
            )
        {
            EnsembleDictionary subCommands = GetSubCommands(
                ensemble, allowed);

            if (subCommands == null)
                return null;

            StringDictionary subCommandNames = subCommands.CachedNames;

            if ((subCommandNames == null) ||
                (subCommandNames.Count != subCommands.Count))
            {
                subCommands.CachedNames = subCommandNames =
                    new StringDictionary();

                foreach (SubCommandPair pair in subCommands)
                {
                    string subCommandName = pair.Key;

                    if (subCommandName != null)
                        subCommandNames.Add(subCommandName, null);
                }
            }

            return subCommandNames;
        }

        ///////////////////////////////////////////////////////////////////////

        public static EnsembleDictionary GetSubCommandsUnsafe(
            IEnsemble ensemble /* in */
            )
        {
            return (ensemble != null) ? ensemble.SubCommands : null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static EnsembleDictionary GetSubCommandsSafe(
            Interpreter interpreter, /* in */
            IEnsemble ensemble       /* in */
            )
        {
            if (interpreter == null)
                return null;

            if (ensemble == null)
                return null;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!interpreter.InternalIsSafe())
                    return ensemble.SubCommands;

                return GetSubCommands(ensemble, true);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used as a callback to filter an arbitrary
        //       list of matched sub-commands based on the ones allowed
        //       for an ensemble.
        //
        /* Eagle._Components.Private.Delegates.SubCommandFilterCallback */
        public static IEnumerable<SubCommandPair> OnlyAllowedSubCommands(
            Interpreter interpreter,                 /* in */
            IEnsemble ensemble,                      /* in */
            IEnumerable<SubCommandPair> subCommands, /* in */
            ref Result error                         /* out */
            )
        {
            if (subCommands == null)
            {
                error = "invalid sub-commands";
                return null;
            }

            EnsembleDictionary allowedSubCommands = GetSubCommandsSafe(
                interpreter, ensemble);

            if (allowedSubCommands == null)
            {
                error = "invalid allowed sub-commands";
                return null;
            }

            IList<SubCommandPair> filteredSubCommands =
                new List<SubCommandPair>();

            foreach (SubCommandPair pair in subCommands)
            {
                string subCommandName = pair.Key;

                if ((subCommandName != null) &&
                    allowedSubCommands.ContainsKey(subCommandName))
                {
                    filteredSubCommands.Add(pair);
                }
            }

            return filteredSubCommands;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Policy Support Methods
        public static ReturnCode QueryDecisions(
            Interpreter interpreter,
            PolicyDecisionType decisionType,
            ref StringList list,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            IEnumerable<PolicyDecisionType> decisionTypes = GetDecisionTypes();

            if (decisionTypes == null)
            {
                error = "policy decision types unavailable";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                bool initial = FlagOps.HasFlags(
                    decisionType, PolicyDecisionType.Initial, true);

                bool final = FlagOps.HasFlags(
                    decisionType, PolicyDecisionType.Final, true);

                foreach (PolicyDecisionType localDecisionType in decisionTypes)
                {
                    if ((decisionType == PolicyDecisionType.None) || /* All */
                        FlagOps.HasFlags(decisionType, localDecisionType, true))
                    {
                        PolicyDecision? decision; /* REUSED */
                        PolicyDecision localDecision; /* REUSED */

                        if (initial)
                        {
                            decision = QueryDecision(
                                interpreter, localDecisionType, false);

                            if (decision != null)
                            {
                                localDecision = (PolicyDecision)decision;

                                if (list == null)
                                    list = new StringList();

                                list.Add(String.Format(
                                    "{0}{1}", localDecisionType,
                                    PolicyDecisionType.Initial));

                                list.Add(localDecision.ToString());
                            }
                        }

                        if (final)
                        {
                            decision = QueryDecision(
                                interpreter, localDecisionType, true);

                            if (decision != null)
                            {
                                localDecision = (PolicyDecision)decision;

                                if (list == null)
                                    list = new StringList();

                                list.Add(String.Format(
                                    "{0}{1}", localDecisionType,
                                    PolicyDecisionType.Final));

                                list.Add(localDecision.ToString());
                            }
                        }
                    }
                }
            }

            if (list == null)
            {
                error = String.Format(
                    "unsupported policy decision types {0}",
                    FormatOps.WrapOrNull(decisionType));

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasExecuteCallbacks(
            PolicyList policies /* in */
            )
        {
            if (CommandCallbacks == null)
                return true;

            if (policies == null)
                return false;

            foreach (ExecuteCallback callback in CommandCallbacks) /* O(N) */
            {
                if (callback == null)
                    continue;

                bool found = false;

                foreach (IPolicy policy in policies) /* O(M) */
                {
                    if (policy == null)
                        continue;

                    if (policy.Callback == callback)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IPolicy NewCore(
            ExecuteCallback callback, /* in */
            IClientData clientData,   /* in */
            PolicyFlags policyFlags,  /* in */
            IPlugin plugin,           /* in */
            ref Result error          /* out */
            )
        {
            if (callback == null)
            {
                error = "invalid policy callback";
                return null;
            }

            MethodInfo methodInfo = callback.Method;

            if (methodInfo == null)
            {
                error = "invalid policy callback method";
                return null;
            }

            Type type = methodInfo.DeclaringType;

            if (type == null)
            {
                error = "invalid policy callback method type";
                return null;
            }

            _Policies.Core policy = new _Policies.Core(new PolicyData(
                FormatOps.PolicyDelegateName(callback), null,
                null, clientData, type.FullName, methodInfo.Name,
                ObjectOps.GetBindingFlags(MetaBindingFlags.Delegate,
                true), AttributeOps.GetMethodFlags(methodInfo),
                policyFlags, plugin, 0));

            policy.Callback = callback;
            return policy;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsTrustedObject(
            Interpreter interpreter, /* in */
            string text,             /* in */
            ObjectFlags flags,       /* in */
            object @object,          /* in: NOT USED */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return false;
            }

            if (FlagOps.HasFlags(flags, ObjectFlags.Safe, true))
                return true;

            error = String.Format(
                UnsafeObjectError, FormatOps.WrapOrNull(text));

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsTrustedType(
            Interpreter interpreter, /* in */
            string text,             /* in */
            Type type,               /* in */
            ref Result error         /* in */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return false;
            }

            if (type == null)
            {
                error = "invalid type";
                return false;
            }

            string name = type.FullName;

            if (name == null)
            {
                error = "invalid type name";
                return false;
            }

            ObjectDictionary trustedTypes = new ObjectDictionary();

            AddTrustedTypes(interpreter, trustedTypes);

            if ((trustedTypes != null) &&
                trustedTypes.ContainsKey(name))
            {
                return true;
            }

            error = String.Format(
                UnsafeTypeError, FormatOps.WrapOrNull(text));

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsTrustedFile(
            Interpreter interpreter,  /* in: OPTIONAL */
            StringList trustedHashes, /* in: OPTIONAL */
            string fileName,          /* in */
            ref Result error          /* out */
            )
        {
            StringList hashes = new StringList();

            if (trustedHashes != null) /* OVERRIDE? */
                AddTrustedHashes(trustedHashes, hashes);
            else /* (interpreter != null) */
                AddTrustedHashes(interpreter, hashes);

            if (hashes != null)
            {
                foreach (string hash in hashes)
                {
                    if (hash == null)
                        continue;

                    Result localError; /* REUSED */
                    PolicyType policyType = PolicyType.None;
                    string hashAlgorithmName = null;
                    byte[] wantHashValue = null;

                    localError = null;

                    if (TryParseHash(
                            interpreter, hash, ref policyType,
                            ref hashAlgorithmName, ref wantHashValue,
                            ref localError) != ReturnCode.Ok)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "IsTrustedFile: parse error = {0}",
                            FormatOps.WrapOrNull(localError)),
                            typeof(PolicyOps).Name,
                            TracePriority.SecurityError);

                        continue;
                    }

                    //
                    // TODO: Currently, only the "File" policy type
                    //       is supported.  In the future, there may
                    //       be other methods for other policy types,
                    //       e.g. IsTrustedScript, etc.
                    //
                    if (policyType != PolicyType.File)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "IsTrustedFile: wrong policy type {0}",
                            FormatOps.WrapOrNull(policyType)),
                            typeof(PolicyOps).Name,
                            TracePriority.SecurityError);

                        continue;
                    }

                    byte[] haveHashValue;

                    localError = null;

                    haveHashValue = HashOps.Compute(
                        interpreter, hashAlgorithmName, fileName, null,
                        true, ref localError);

                    if (haveHashValue == null)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "IsTrustedFile: hash error = {0}",
                            FormatOps.WrapOrNull(localError)),
                            typeof(PolicyOps).Name,
                            TracePriority.SecurityError);

                        continue;
                    }

                    TraceOps.DebugTrace(String.Format(
                        "IsTrustedFile: have hash {0}, want hash {1}",
                        FormatOps.MaybeNull(ArrayOps.ToHexadecimalString(
                            haveHashValue)),
                        FormatOps.MaybeNull(ArrayOps.ToHexadecimalString(
                            wantHashValue))), typeof(PolicyOps).Name,
                        TracePriority.SecurityDebug2);

                    if (ArrayOps.Equals(haveHashValue, wantHashValue))
                        return true;
                }
            }

            error = String.Format(
                UnsafeFileError, FormatOps.WrapOrNull(fileName));

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSuccess(
            PolicyDecision decision /* in */
            )
        {
            //
            // NOTE: If the policy decision is "None" or "Approved", that is
            //       considered to be a success; however, a success does not
            //       indicate a formal policy approval.
            //
            if (PolicyContext.IsNone(decision) ||
                PolicyContext.IsApproved(decision))
            {
                return true;
            }

            //
            // NOTE: Any other decision is considered to be a failure.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSuccess(
            ReturnCode code,        /* in */
            PolicyDecision decision /* in */
            )
        {
            //
            // NOTE: Anytime a policy callback returns something other than
            //       "Ok", it is a failure.
            //
            if (code != ReturnCode.Ok)
                return false;

            //
            // NOTE: When the return code is "Ok", success is based on the
            //       formal policy decision itself.
            //
            return IsSuccess(decision);
        }

        ///////////////////////////////////////////////////////////////////////

        public static PolicyDecision FinalDecision(
            PolicyFlags flags,      /* in */
            ReturnCode? code,       /* in */
            PolicyDecision decision /* in */
            )
        {
            bool before = FlagOps.HasFlags(
                flags, PolicyFlags.EngineBeforeMask, false);

            if ((code == null) ||
                !IsSuccess((ReturnCode)code, decision))
            {
                return before ?
                    PolicyDecision.Stop : PolicyDecision.Failure;
            }

            if (PolicyContext.IsApproved(decision))
            {
                return before ?
                    PolicyDecision.Continue : PolicyDecision.Success;
            }

            if (PolicyContext.IsNone(decision))
            {
                return before ?
                    PolicyDecision.Pending : PolicyDecision.Unknown;
            }

            return PolicyDecision.None;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddTrustedDirectories(
            Interpreter interpreter,           /* in */
            PathDictionary<object> directories /* in, out */
            )
        {
            if ((interpreter == null) || (directories == null))
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // BUGFIX: Cannot add null directory to the dictionary;
                //         therefore, if the initialized path is invalid,
                //         just skip it.
                //
                string initializedPath = interpreter.InternalInitializedPath;

                if ((initializedPath != null) &&
                    !directories.ContainsKey(initializedPath))
                {
                    directories.Add(initializedPath);
                }

                //
                // NOTE: Add the paths trusted by the interpreter.
                //
                // HACK: All paths trusted by the interpreter are assumed
                //       to be directory names, not file names.
                //
                StringList trustedPaths = interpreter.InternalTrustedPaths;

                if (trustedPaths == null)
                    return;

                foreach (string path in trustedPaths)
                {
                    if ((path != null) &&
                        !directories.ContainsKey(path))
                    {
                        directories.Add(path);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddTrustedTypes(
            Interpreter interpreter, /* in */
            ObjectDictionary types   /* in, out */
            )
        {
            if ((interpreter == null) || (types == null))
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Add the types trusted by the interpreter.
                //
                ObjectDictionary trustedTypes =
                    interpreter.InternalTrustedTypes;

                if (trustedTypes == null)
                    return;

                foreach (KeyValuePair<string, object> pair in trustedTypes)
                {
                    string typeName = pair.Key;

                    if ((typeName != null) &&
                        !types.ContainsKey(typeName))
                    {
                        types.Add(typeName, pair.Value);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddTrustedHashes(
            Interpreter interpreter, /* in: OPTIONAL */
            StringList hashes        /* in, out */
            )
        {
            AddTrustedHashes(
                RuntimeOps.CombineOrCopyTrustedHashes(interpreter, false),
                hashes);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddTrustedHashes(
            StringList trustedHashes, /* in */
            StringList hashes         /* in, out */
            )
        {
            if ((trustedHashes == null) || (hashes == null))
                return;

            hashes.AddRange(trustedHashes); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool CanBeTrustedUri(
            Interpreter interpreter, /* in: NOT USED */
            Uri uri                  /* in */
            )
        {
            if (uri == null)
                return false;

            //
            // TODO: Can a "trusted" URI really ever be anything other than
            //       HTTPS?
            //
            return PathOps.IsHttpsUriScheme(uri);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddTrustedUris(
            Interpreter interpreter,   /* in */
            UriDictionary<object> uris /* in, out */
            )
        {
            if ((interpreter == null) || (uris == null))
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                Uri uri; /* REUSED */

                //
                // NOTE: Add the URI for this assembly, if any; however, make
                //       sure it is relatively secure (HTTPS).
                //
                uri = GlobalState.GetAssemblyUri();

                if ((uri != null) && CanBeTrustedUri(interpreter, uri))
                {
                    if (!uris.ContainsKey(uri))
                    {
                        //
                        // NOTE: For now, the value null is always used here.
                        //
                        uris.Add(uri, null);
                    }
                }

                //
                // NOTE: Add script URI for this assembly, if any; however,
                //       make sure it is secure (HTTPS).
                //
                uri = GlobalState.GetAssemblyScriptBaseUri();

                if ((uri != null) && CanBeTrustedUri(interpreter, uri))
                {
                    if (!uris.ContainsKey(uri))
                    {
                        //
                        // NOTE: For now, the value null is always used here.
                        //
                        uris.Add(uri, null);
                    }
                }

                //
                // NOTE: Add the other URIs trusted by the interpreter, if
                //       any; however, make sure they are relatively secure
                //       (HTTPS).
                //
                UriDictionary<object> trustedUris =
                    interpreter.InternalTrustedUris;

                if (trustedUris == null)
                    return;

                foreach (KeyValuePair<Uri, object> pair in trustedUris)
                {
                    uri = pair.Key;

                    if ((uri != null) &&
                        CanBeTrustedUri(interpreter, uri))
                    {
                        if (!uris.ContainsKey(uri))
                        {
                            //
                            // TODO: Currently, the "pair.Value" value is
                            //       purposely ignored here.
                            //
                            uris.Add(uri, null);
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractContextAndPlugin( /* POLICY HELPER METHOD */
            Interpreter interpreter,          /* in: NOT USED */
            IClientData clientData,           /* in */
            ref IPolicyContext policyContext, /* in, out */
            ref IPlugin plugin,               /* out */
            ref Result error                  /* out */
            )
        {
            if (clientData == null)
            {
                error = "invalid policy clientData";
                return ReturnCode.Error;
            }

            if (policyContext == null)
                policyContext = clientData.Data as IPolicyContext;

            if (policyContext == null)
            {
                error = "policy clientData is not a policyContext object";
                return ReturnCode.Error;
            }

            plugin = policyContext.Plugin;

            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractContextAndScript( /* POLICY HELPER METHOD */
            Interpreter interpreter,          /* in: NOT USED */
            IClientData clientData,           /* in */
            ref IPolicyContext policyContext, /* in, out */
            ref Encoding encoding,            /* out */
            ref IScript script,               /* out */
            ref Result error                  /* out */
            )
        {
            if (clientData == null)
            {
                error = "invalid policy clientData";
                return ReturnCode.Error;
            }

            if (policyContext == null)
                policyContext = clientData.Data as IPolicyContext;

            if (policyContext == null)
            {
                error = "policy clientData is not a policyContext object";
                return ReturnCode.Error;
            }

            script = policyContext.Script;

            if (script == null)
            {
                error = "invalid script";
                return ReturnCode.Error;
            }

            encoding = policyContext.Encoding;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractContextAndFileName( /* POLICY HELPER METHOD */
            Interpreter interpreter,          /* in: NOT USED */
            IClientData clientData,           /* in */
            ref IPolicyContext policyContext, /* in, out */
            ref string fileName,              /* out */
            ref Result error                  /* out */
            )
        {
            if (clientData == null)
            {
                error = "invalid policy clientData";
                return ReturnCode.Error;
            }

            if (policyContext == null)
                policyContext = clientData.Data as IPolicyContext;

            if (policyContext == null)
            {
                error = "policy clientData is not a policyContext object";
                return ReturnCode.Error;
            }

            fileName = policyContext.FileName;

            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractContextAndText( /* POLICY HELPER METHOD */
            Interpreter interpreter,          /* in: NOT USED */
            IClientData clientData,           /* in */
            ref IPolicyContext policyContext, /* in, out */
            ref string text,                  /* out */
            ref Result error                  /* out */
            )
        {
            if (clientData == null)
            {
                error = "invalid policy clientData";
                return ReturnCode.Error;
            }

            if (policyContext == null)
                policyContext = clientData.Data as IPolicyContext;

            if (policyContext == null)
            {
                error = "policy clientData is not a policyContext object";
                return ReturnCode.Error;
            }

            text = policyContext.Text;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractContextAndTextAndBytes( /* POLICY HELPER METHOD */
            Interpreter interpreter,          /* in */
            IClientData clientData,           /* in */
            ref IPolicyContext policyContext, /* in, out */
            ref Encoding encoding,            /* out */
            ref string text,                  /* out */
            ref byte[] hashValue,             /* out */
            ref ByteList bytes,               /* out */
            ref Result error                  /* out */
            )
        {
            if (clientData == null)
            {
                error = "invalid policy clientData";
                return ReturnCode.Error;
            }

            if (policyContext == null)
                policyContext = clientData.Data as IPolicyContext;

            if (policyContext == null)
            {
                error = "policy clientData is not a policyContext object";
                return ReturnCode.Error;
            }

            encoding = policyContext.Encoding;
            text = policyContext.Text;
            hashValue = policyContext.HashValue;

            IClientData policyClientData = policyContext.ClientData;

            if (policyClientData == null)
            {
                bytes = null;
                return ReturnCode.Ok;
            }

            ReadScriptClientData readScriptClientData =
                policyClientData as ReadScriptClientData;

            if (readScriptClientData == null)
            {
                bytes = null;
                return ReturnCode.Ok;
            }

            ByteList localBytes = readScriptClientData.Bytes;

            if (localBytes == null)
            {
                bytes = null;
                return ReturnCode.Ok;
            }

            bytes = new ByteList(localBytes); /* COPY */
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        private static ReturnCode LookupPluginCommandType( /* POLICY HELPER METHOD */
            Interpreter interpreter, /* in */
            IPlugin plugin,          /* in */
            Type commandType,        /* in */
            ref ICommand command     /* out */
            )
        {
            Result error = null;

            return LookupPluginCommandType(
                interpreter, plugin, commandType, ref command, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode LookupPluginCommandType( /* POLICY HELPER METHOD */
            Interpreter interpreter, /* in */
            IPlugin plugin,          /* in */
            Type commandType,        /* in */
            ref ICommand command,    /* out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            if (commandType == null)
            {
                error = "invalid command type";
                return ReturnCode.Error;
            }

            List<ICommand> commands = null;

            if (interpreter.GetPluginCommands(
                    plugin, ref commands, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            foreach (ICommand localCommand in commands)
            {
                IWrapper wrapper = localCommand as IWrapper;

                if (wrapper == null)
                    continue;

                object @object = wrapper.Object;

                if (@object == null)
                    continue;

                Type type = AppDomainOps.MaybeGetTypeOrNull(
                    @object);

                if (Object.ReferenceEquals(type, commandType))
                {
                    command = localCommand;
                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "no plugin matching command type {0} was found",
                FormatOps.TypeName(commandType));

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractContextAndCommand( /* POLICY HELPER METHOD */
            Interpreter interpreter,          /* in */
            IClientData clientData,           /* in */
            Type commandType,                 /* in */
            long commandToken,                /* in */
            ref IPolicyContext policyContext, /* in, out */
            ref bool match,                   /* out */
            ref Result error                  /* out */
            )
        {
            ICommand command = null;

            return ExtractContextAndCommand(
                interpreter, clientData, commandType, commandToken,
                ref policyContext, ref command, ref match, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ExtractContextAndCommand( /* POLICY HELPER METHOD */
            Interpreter interpreter,          /* in */
            IClientData clientData,           /* in */
            Type commandType,                 /* in */
            long commandToken,                /* in */
            ref IPolicyContext policyContext, /* in, out */
            ref ICommand command,             /* out */
            ref bool match,                   /* out */
            ref Result error                  /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (clientData == null)
            {
                error = "invalid policy clientData";
                return ReturnCode.Error;
            }

            if (policyContext == null)
                policyContext = clientData.Data as IPolicyContext;

            if (policyContext == null)
            {
                error = "policy clientData is not a policyContext object";
                return ReturnCode.Error;
            }

            IExecute execute = policyContext.Execute;

            if (execute == null)
            {
                error = "policyContext does not contain an executable object";
                return ReturnCode.Error;
            }

            //
            // NOTE: If the command type is null, skip matching against it
            //       (i.e. just extract it and return).
            //
            if (commandType == null)
            {
                command = execute as ICommand;
                match = (command != null);

                return ReturnCode.Ok;
            }

#if ISOLATED_PLUGINS
            IPlugin plugin = policyContext.Plugin;

            if (AppDomainOps.IsIsolated(plugin))
            {
                command = null;

                if (((commandToken == 0) &&
                    (LookupPluginCommandType(
                        interpreter, plugin, commandType,
                        ref command) == ReturnCode.Ok)) ||
                    ((commandToken != 0) &&
                    (interpreter.GetCommand(
                        commandToken, LookupFlags.PolicyNoVerbose,
                        ref command) == ReturnCode.Ok)))
                {
                    match = Object.ReferenceEquals(execute, command);
                }
                else
                {
                    match = false;
                }
            }
            else
#endif
            {
                //
                // BUGBUG: This method call is a serious problem for isolated
                //         plugins.  The command type cannot be sent cleanly
                //         across the AppDomain boundry.  This is now handled
                //         (correctly) by the plugin isolation check above.
                //
                command = null;

                if (((commandToken == 0) &&
                    (interpreter.GetCommand(
                        commandType, LookupFlags.PolicyNoVerbose,
                        ref command) == ReturnCode.Ok)) ||
                    ((commandToken != 0) &&
                    (interpreter.GetCommand(
                        commandToken, LookupFlags.PolicyNoVerbose,
                        ref command) == ReturnCode.Ok)))
                {
                    match = Object.ReferenceEquals(execute, command);
                }
                else
                {
                    match = false;
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Policy Implementations
        #region Trusted Sub-Command Policy Implementation
        public static ReturnCode CheckViaSubCommand( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,                /* in */
            Type commandType,                 /* in */
            long commandToken,                /* in */
            StringDictionary subCommandNames, /* in */
            bool allowed,                     /* in */
            Interpreter interpreter,          /* in */
            IClientData clientData,           /* in */
            ArgumentList arguments,           /* in */
            ref Result result                 /* out */
            )
        {
            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            ReturnCode code;
            IPolicyContext policyContext = null;
            ICommand command = null;
            bool match = false;

            code = ExtractContextAndCommand(
                interpreter, clientData, commandType, commandToken,
                ref policyContext, ref command, ref match, ref result);

            if (code != ReturnCode.Ok)
                return code;

            if (!match)
                return ReturnCode.Ok;

            string subCommandName = null;

            if (arguments.Count >= 2) /* ENSEMBLE */
                subCommandName = arguments[1];

            if (String.IsNullOrEmpty(subCommandName))
                return ReturnCode.Ok;

            if (ScriptOps.SubCommandFromEnsemble(interpreter,
                    command, OnlyAllowedSubCommands, null, true, false,
                    ref subCommandName, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Ok;
            }

            if (subCommandNames == null)
                subCommandNames = GetSubCommandNames(command, allowed);

            if (allowed)
            {
                if ((subCommandNames != null) &&
                    subCommandNames.ContainsKey(subCommandName))
                {
                    //
                    // NOTE: The sub-command is in the "allowed" list,
                    //       vote to allow the command to be executed.
                    //
                    policyContext.Approved();

                    //
                    // NOTE: Return the sub-command as the result.
                    //
                    policyContext.Result = subCommandName;
                }
            }
            else
            {
                if ((subCommandNames != null) &&
                    !subCommandNames.ContainsKey(subCommandName))
                {
                    //
                    // NOTE: The sub-command is not in the "denied" list,
                    //       vote to allow the command to be executed.
                    //
                    policyContext.Approved();

                    //
                    // NOTE: Return the sub-command as the result.
                    //
                    policyContext.Result = subCommandName;
                }
            }

            //
            // NOTE: The policy checking itself has been successful; however,
            //       this does not necessarily mean that we allow the command
            //       to be executed.
            //
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trusted URI Policy Implementation
        public static ReturnCode CheckViaUri( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,          /* in */
            Type commandType,           /* in */
            long commandToken,          /* in */
            Uri uri,                    /* in */
            UriDictionary<object> uris, /* in */
            bool allowed,               /* in */
            Interpreter interpreter,    /* in */
            IClientData clientData,     /* in */
            ArgumentList arguments,     /* in */
            ref Result result           /* out */
            )
        {
            ReturnCode code;
            IPolicyContext policyContext = null;
            bool match = false;

            code = ExtractContextAndCommand(
                interpreter, clientData, commandType, commandToken,
                ref policyContext, ref match, ref result);

            if (code != ReturnCode.Ok)
                return code;

            if (!match)
                return ReturnCode.Ok;

            if (allowed)
            {
                if ((uris != null) &&
                    uris.ContainsSchemeAndServer(uri))
                {
                    //
                    // NOTE: The URI is in the "allowed" list, vote to allow
                    //       the command.
                    //
                    policyContext.Approved();
                }
            }
            else
            {
                if ((uris != null) &&
                    !uris.ContainsSchemeAndServer(uri))
                {
                    //
                    // NOTE: The URI is not in the "denied" list, vote to
                    //       allow the command.
                    //
                    policyContext.Approved();
                }
            }

            //
            // NOTE: The policy checking itself has been successful; however,
            //       this does not necessarily mean that we allow the command
            //       to be executed.
            //
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Directory Policy Implementation
        public static ReturnCode CheckViaDirectory( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,                  /* in */
            Type commandType,                   /* in */
            long commandToken,                  /* in */
            string fileName,                    /* in */
            PathDictionary<object> directories, /* in */
            bool allowed,                       /* in */
            Interpreter interpreter,            /* in */
            IClientData clientData,             /* in */
            ArgumentList arguments,             /* in */
            ref Result result                   /* out */
            )
        {
            ReturnCode code;
            IPolicyContext policyContext = null;
            bool match = false;

            code = ExtractContextAndCommand(
                interpreter, clientData, commandType, commandToken,
                ref policyContext, ref match, ref result);

            if (code != ReturnCode.Ok)
                return code;

            if (!match)
                return ReturnCode.Ok;

            string directory = null;

            try
            {
                /* throw */
                directory = Path.GetDirectoryName(
                    PathOps.BaseDirectorySubstitution(interpreter, fileName));
            }
            catch
            {
                // do nothing.
            }

            if (String.IsNullOrEmpty(directory))
                return ReturnCode.Ok;

            if (allowed)
            {
                if ((directories != null) &&
                    directories.Contains(directory))
                {
                    //
                    // NOTE: The directory is in the "allowed" list, vote to
                    //       allow the command.
                    //
                    policyContext.Approved();
                }
            }
            else
            {
                if ((directories != null) &&
                    !directories.Contains(directory))
                {
                    //
                    // NOTE: The directory is not in the "denied" list, vote
                    //       to allow the command.
                    //
                    policyContext.Approved();
                }
            }

            //
            // NOTE: The policy checking itself has been successful; however,
            //       this does not necessarily mean that we allow the command
            //       to be executed.
            //
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Object Type Policy Implementation
        public static ReturnCode CheckViaType( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,       /* in */
            Type commandType,        /* in */
            long commandToken,       /* in */
            Type objectType,         /* in */
            TypeList types,          /* in */
            bool allowed,            /* in */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code;
            IPolicyContext policyContext = null;
            bool match = false;

            code = ExtractContextAndCommand(
                interpreter, clientData, commandType, commandToken,
                ref policyContext, ref match, ref result);

            if (code != ReturnCode.Ok)
                return code;

            if (!match)
                return ReturnCode.Ok;

            if (objectType == null)
                return ReturnCode.Ok;

            if (allowed)
            {
                if ((types != null) &&
                    types.Contains(objectType))
                {
                    //
                    // NOTE: The type is in the "allowed" list, vote to
                    //       allow the command.
                    //
                    policyContext.Approved();
                }
            }
            else
            {
                if ((types != null) &&
                    !types.Contains(objectType))
                {
                    //
                    // NOTE: The type is not in the "denied" list, vote to
                    //       allow the command.
                    //
                    policyContext.Approved();
                }
            }

            //
            // NOTE: The policy checking itself has been successful; however,
            //       this does not necessarily mean that we allow the command
            //       to be executed.
            //
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dynamic User Managed Callback Policy Implementation
        public static ReturnCode CheckViaCallback( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,       /* in */
            Type commandType,        /* in */
            long commandToken,       /* in */
            ICallback callback,      /* in */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            ReturnCode code;
            IPolicyContext policyContext = null;
            bool match = false;

            code = ExtractContextAndCommand(
                interpreter, clientData, commandType, commandToken,
                ref policyContext, ref match, ref result);

            if (code != ReturnCode.Ok)
                return code;

            if (!match)
                return ReturnCode.Ok;

            if (callback == null)
                return ReturnCode.Ok;

            ReturnCode localCode;
            Result localResult = null;

            localCode = callback.Invoke(
                new StringList(arguments), ref localResult);

            switch (localCode)
            {
                case ReturnCode.Ok:
                    {
                        //
                        // NOTE: The callback was executed successfully,
                        //       vote to allow the command.
                        //
                        policyContext.Approved();
                        break;
                    }
                case ReturnCode.Break:
                    {
                        //
                        // NOTE: No vote is made for this return code.
                        //
                        break;
                    }
                case ReturnCode.Continue:
                    {
                        //
                        // NOTE: This return code represents an official
                        //       "undecided" vote.
                        //
                        policyContext.Undecided();
                        break;
                    }
                case ReturnCode.Error:
                case ReturnCode.Return: // NOTE: Bad policy return code.
                default:
                    {
                        //
                        // NOTE: An error or any other return code is
                        //       interpreted as a "denied" vote.
                        //
                        policyContext.Denied();
                        break;
                    }
            }

            //
            // NOTE: Set the informational policy result to the string result
            //       of the callback execution.
            //
            policyContext.Result = Result.Copy(
                localResult, localCode, ResultFlags.CopyObject); /* COPY */

            //
            // NOTE: The policy checking itself has been successful; however,
            //       this does not necessarily mean that we allow the command
            //       to be executed.
            //
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dynamic User Script Evaluation Policy Implementation
        public static ReturnCode CheckViaScript( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,             /* in */
            Type commandType,              /* in */
            long commandToken,             /* in */
            Interpreter policyInterpreter, /* in */
            string text,                   /* in */
            Interpreter interpreter,       /* in */
            IClientData clientData,        /* in */
            ArgumentList arguments,        /* in */
            ref Result result              /* out */
            )
        {
            ReturnCode code;
            IPolicyContext policyContext = null;
            bool match = false;

            code = ExtractContextAndCommand(
                interpreter, clientData, commandType, commandToken,
                ref policyContext, ref match, ref result);

            if (code != ReturnCode.Ok)
                return code;

            if (!match)
                return ReturnCode.Ok;

            //
            // NOTE: *WARNING* Empty scripts are allowed, please do not
            //       change this to "!String.IsNullOrEmpty".
            //
            if ((policyInterpreter == null) || (text == null))
                return ReturnCode.Ok;

            ReturnCode localCode;
            Result localResult = null;

            localCode = EvaluateScript(
                policyInterpreter, text, arguments, flags,
                ref localResult);

            switch (localCode)
            {
                case ReturnCode.Ok:
                    {
                        //
                        // NOTE: The callback was executed successfully,
                        //       vote to allow the command.
                        //
                        policyContext.Approved();
                        break;
                    }
                case ReturnCode.Break:
                    {
                        //
                        // NOTE: No vote is made for this return code.
                        //
                        break;
                    }
                case ReturnCode.Continue:
                    {
                        //
                        // NOTE: This return code represents an official
                        //       "undecided" vote.
                        //
                        policyContext.Undecided();
                        break;
                    }
                case ReturnCode.Error:
                case ReturnCode.Return: // NOTE: Bad policy return code.
                default:
                    {
                        //
                        // NOTE: An error or any other return code is
                        //       interpreted as a "denied" vote.
                        //
                        policyContext.Denied();
                        break;
                    }
            }

            //
            // NOTE: Set the informational policy result to the string result
            //       of the callback execution.
            //
            policyContext.Result = Result.Copy(
                localResult, localCode, ResultFlags.CopyObject); /* COPY */

            //
            // NOTE: The policy checking itself has been successful; however,
            //       this does not necessarily mean that we allow the command
            //       to be executed.
            //
            return ReturnCode.Ok;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Default Core Command Policies
        #region The Default [clock] Command Policy
        [MethodFlags(
            MethodFlags.CommandPolicy | MethodFlags.System |
            MethodFlags.NoAdd)]
        private static ReturnCode ClockCommandCallback( /* POLICY */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            return CheckViaSubCommand(
                PolicyFlags.SubCommand, typeof(_Commands.Clock),
                0, null, true, interpreter, clientData, arguments,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region The Default [file] Command Policy
        [MethodFlags(
            MethodFlags.CommandPolicy | MethodFlags.System |
            MethodFlags.NoAdd)]
        private static ReturnCode FileCommandCallback( /* POLICY */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            return CheckViaSubCommand(
                PolicyFlags.SubCommand, typeof(_Commands._File),
                0, null, true, interpreter, clientData, arguments,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region The Default [info] Command Policy
        [MethodFlags(
            MethodFlags.CommandPolicy | MethodFlags.System |
            MethodFlags.NoAdd)]
        private static ReturnCode InfoCommandCallback( /* POLICY */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            return CheckViaSubCommand(
                PolicyFlags.SubCommand, typeof(_Commands.Info),
                0, null, true, interpreter, clientData, arguments,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region The Default [interp] Command Policy
        [MethodFlags(
            MethodFlags.CommandPolicy | MethodFlags.System |
            MethodFlags.NoAdd)]
        private static ReturnCode InterpCommandCallback( /* POLICY */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            return CheckViaSubCommand(
                PolicyFlags.SubCommand, typeof(_Commands.Interp),
                0, null, true, interpreter, clientData, arguments,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region The Default [object] Command Policy
        [MethodFlags(
            MethodFlags.CommandPolicy | MethodFlags.System |
            MethodFlags.NoAdd)]
        private static ReturnCode ObjectCommandCallback( /* POLICY */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            return CheckViaSubCommand(
                PolicyFlags.SubCommand, typeof(_Commands.Object),
                0, null, true, interpreter, clientData, arguments,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region The Default [package] Command Policy
        [MethodFlags(
            MethodFlags.CommandPolicy | MethodFlags.System |
            MethodFlags.NoAdd)]
        private static ReturnCode PackageCommandCallback( /* POLICY */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            return CheckViaSubCommand(
                PolicyFlags.SubCommand, typeof(_Commands.Package),
                0, null, false, interpreter, clientData, arguments,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region The Default [source] Command Policy
        [MethodFlags(
            MethodFlags.CommandPolicy | MethodFlags.System |
            MethodFlags.NoAdd)]
        private static ReturnCode SourceCommandCallback( /* POLICY */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            //
            // HACK: There is a small problem with this policy.  We need to
            //       examine the last argument to determine which policy
            //       checking method we would like to use; however, we do
            //       not actually know if the command being executed is
            //       [source].  In practice, this does not matter because
            //       the policy methods themselves double-check the command
            //       type.  Therefore, the argument checking here will be
            //       pointless (but harmless) if the command is not really
            //       [source].
            //
            string fileName = null;

            if ((arguments != null) && (arguments.Count >= 2))
                fileName = arguments[arguments.Count - 1];

            //
            // NOTE: If the file name represents a remote URI, use slightly
            //       different policy handling.
            //
            Uri uri = null;

            if (PathOps.IsRemoteUri(fileName, ref uri))
            {
                //
                // NOTE: Only allow remote sites that we know, trust, and have
                //       100% positive control over.
                //
                UriDictionary<object> trustedUris = new UriDictionary<object>();

                AddTrustedUris(interpreter, trustedUris);

                return CheckViaUri(
                    PolicyFlags.Uri, typeof(_Commands.Source), 0, uri,
                    trustedUris, true, interpreter, clientData, arguments,
                    ref result);
            }
            else
            {
                PathDictionary<object> directories = new PathDictionary<object>();

                AddTrustedDirectories(interpreter, directories);

                return CheckViaDirectory(
                    PolicyFlags.Directory, typeof(_Commands.Source), 0,
                    fileName, directories, true, interpreter, clientData,
                    arguments, ref result);
            }
        }
        #endregion
        #endregion
    }
}
