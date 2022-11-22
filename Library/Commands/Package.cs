/*
 * Package.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using _ClientData = Eagle._Components.Public.ClientData;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("c8fd57c0-20b3-4594-a5a7-919d6f9a8272")]
    /* 
     * POLICY: We allow certain "safe" sub-commands.
     */
    [CommandFlags(
        CommandFlags.Unsafe | CommandFlags.Standard |
        CommandFlags.Initialize | CommandFlags.SecuritySdk |
        CommandFlags.LicenseSdk)]
    [ObjectGroup("scriptEnvironment")]
    internal sealed class Package : Core
    {
        public Package(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] { 
            "forget", "ifneeded", "indexes", "info", "loaded",
            "names", "pending", "present", "provide", "relativefilename",
            "require", "reset", "scan", "unknown", "vcompare",
            "versions", "vloaded", "vsatisfies", "vsort",
            "withdraw"
        });

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        private readonly EnsembleDictionary disallowedSubCommands = new EnsembleDictionary(
            PolicyOps.DisallowedPackageSubCommandNames);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary DisallowedSubCommands
        {
            get { return disallowedSubCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, true,
                            false, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            switch (subCommand)
                            {
                                case "forget":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            code = interpreter.PkgForget(
                                                new StringList(arguments, 2), _ClientData.Empty, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package forget ?package package ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ifneeded":
                                    {
                                        if ((arguments.Count >= 4) && (arguments.Count <= 6))
                                        {
                                            Version version = null;

                                            code = Value.GetVersion(
                                                arguments[3], interpreter.InternalCultureInfo,
                                                ref version, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string text = null;

                                                if (arguments.Count >= 5)
                                                    text = arguments[4];

                                                PackageFlags flags = interpreter.PackageFlags;

                                                if (arguments.Count >= 6)
                                                {
                                                    object enumValue = EnumOps.TryParseFlags(
                                                        interpreter, typeof(PackageFlags),
                                                        flags.ToString(), arguments[5],
                                                        interpreter.InternalCultureInfo,
                                                        true, true, true, ref result);

                                                    if (enumValue is PackageFlags)
                                                        flags = (PackageFlags)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    code = interpreter.PkgIfNeeded(
                                                        arguments[2], version, text, _ClientData.Empty,
                                                        flags, ref result);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package ifneeded package version ?script? ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "indexes":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            code = interpreter.PkgIndexes(
                                                pattern, false, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package indexes ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "info":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IPackage package = null;

                                            code = interpreter.GetPackage(
                                                arguments[2], LookupFlags.Default,
                                                ref package, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool scrub = interpreter.InternalIsSafe();
                                                PackageFlags flags = package.Flags;
                                                Guid id = AttributeOps.GetObjectId(package);

                                                result = StringList.MakeList(
                                                    "kind", package.Kind,
                                                    "id", package.Id.Equals(Guid.Empty) ? id : package.Id,
                                                    "name", package.Name,
                                                    "description", package.Description,
                                                    "indexFileName", scrub ? PathOps.ScrubPath(
                                                        GlobalState.GetBasePath(), package.IndexFileName) :
                                                        package.IndexFileName,
                                                    "provideFileName", scrub ? PathOps.ScrubPath(
                                                        GlobalState.GetBasePath(), package.ProvideFileName) :
                                                        package.ProvideFileName,
                                                    "flags", flags,
                                                    "loaded", (package.Loaded != null) ? package.Loaded : null,
                                                    "ifNeeded", (!scrub && (package.IfNeeded != null)) ?
                                                        package.IfNeeded.KeysAndValuesToString(null, false) :
                                                        null,
                                                    "wasNeeded", package.WasNeeded);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package info name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "loaded":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            code = interpreter.PkgLoaded(
                                                pattern, false, false, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package loaded ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "names":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            code = interpreter.PkgNames(
                                                pattern, false, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package names ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "pending":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                IPackage package = null;

                                                code = interpreter.GetPackage(
                                                    arguments[2], LookupFlags.Default, ref package,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    result = FlagOps.HasFlags(
                                                        package.Flags, PackageFlags.Loading, true);
                                                }
                                            }
                                            else
                                            {
                                                result = (interpreter.PackageLevels > 0);
                                                code = ReturnCode.Ok;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package pending ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "present":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] { 
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-exact", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) >= arguments.Count))
                                                {
                                                    bool exact = false;

                                                    if (options.IsPresent("-exact"))
                                                        exact = true;

                                                    Version version = null;

                                                    if ((argumentIndex + 1) < arguments.Count)
                                                        code = Value.GetVersion(
                                                            arguments[argumentIndex + 1], interpreter.InternalCultureInfo,
                                                            ref version, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        code = interpreter.PresentPackage(
                                                            arguments[argumentIndex], version, exact, ref result);
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"package present ?-exact? package ?version?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package present ?-exact? package ?version?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "provide":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            PackageFlags flags = interpreter.PackageFlags;

                                            if (!FlagOps.HasFlags(flags, PackageFlags.NoProvide, true))
                                            {
                                                Version version = null;

                                                if (arguments.Count == 4)
                                                {
                                                    code = Value.GetVersion(
                                                        arguments[3], interpreter.InternalCultureInfo,
                                                        ref version, ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    code = interpreter.PkgProvide(
                                                        arguments[2], version, _ClientData.Empty,
                                                        flags, ref result);
                                                }
                                            }
                                            else
                                            {
                                                //
                                                // HACK: Do nothing, provide no package, and return nothing.
                                                //
                                                result = String.Empty;
                                                code = ReturnCode.Ok;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package provide package ?version?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "relativefilename":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            PathComparisonType pathComparisonType = PathComparisonType.Default;

                                            if (arguments.Count == 4)
                                            {
                                                object enumValue = EnumOps.TryParseFlags(
                                                    interpreter, typeof(PathComparisonType),
                                                    pathComparisonType.ToString(), arguments[3],
                                                    interpreter.InternalCultureInfo, true, true, true,
                                                    ref result);

                                                if (enumValue is EventFlags)
                                                    pathComparisonType = (PathComparisonType)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                string fileName = null;

                                                code = PackageOps.GetRelativeFileName(
                                                    interpreter, arguments[2], pathComparisonType,
                                                    ref fileName, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = fileName;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package relativefilename fileName ?type?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "require":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] { 
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-exact", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) >= arguments.Count))
                                                {
                                                    string packageName = arguments[argumentIndex];
                                                    bool exact = false;

                                                    if (options.IsPresent("-exact"))
                                                        exact = true;

                                                    Version version = null;

                                                    if ((argumentIndex + 1) < arguments.Count)
                                                    {
                                                        code = Value.GetVersion(
                                                            arguments[argumentIndex + 1],
                                                            interpreter.InternalCultureInfo,
                                                            ref version, ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        code = interpreter.RequirePackage(
                                                            packageName, version, exact,
                                                            ref result);

                                                        if (code != ReturnCode.Ok)
                                                        {
                                                            TraceOps.DebugTrace(String.Format(
                                                                "Execute: REQUIRE FAILED, interpreter = {0}, " +
                                                                "packageName = {1}, version = {2}, exact = {3}, " +
                                                                "code = {4}, result = {5}",
                                                                FormatOps.InterpreterNoThrow(interpreter),
                                                                FormatOps.WrapOrNull(packageName),
                                                                FormatOps.WrapOrNull(version), exact,
                                                                code, FormatOps.WrapOrNull(result)),
                                                                typeof(Package).Name,
                                                                TracePriority.PackageError3);
                                                        }
                                                    }

                                                    //
                                                    // NOTE: This is a new feature.  If the initial attempt to
                                                    //       require a package fails, call the package fallback
                                                    //       delegate for the interpreter and then try requiring
                                                    //       the package again.
                                                    //
                                                    if ((code != ReturnCode.Ok) && !ScriptOps.HasFlags(
                                                            interpreter, InterpreterFlags.NoPackageFallback, true))
                                                    {
                                                        PackageCallback packageFallback = interpreter.PackageFallback;

                                                        if (packageFallback != null)
                                                        {
                                                            PackageFlags flags = interpreter.PackageFlags;

                                                            code = packageFallback(
                                                                interpreter, packageName, version, null, flags,
                                                                exact, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                code = interpreter.RequirePackage(
                                                                    packageName, version, exact, ref result);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            TraceOps.DebugTrace(String.Format(
                                                                "Execute: package fallback not " +
                                                                "configured for interpreter {0}",
                                                                FormatOps.InterpreterNoThrow(
                                                                interpreter)), typeof(Package).Name,
                                                                TracePriority.PackageDebug2);
                                                        }
                                                    }

                                                    //
                                                    // BUGFIX: This is really a new feature.  In the event of a failure
                                                    //         here, we now fallback to the "unknown package handler",
                                                    //         just like Tcl does.
                                                    //
                                                    if ((code != ReturnCode.Ok) && !ScriptOps.HasFlags(
                                                            interpreter, InterpreterFlags.NoPackageUnknown, true))
                                                    {
                                                        string packageUnknown = interpreter.PackageUnknown;

                                                        if (packageUnknown != null) /* UNCONFIGURED? */
                                                        {
                                                            string text = ScriptOps.GetPackageUnknownScript(
                                                                packageUnknown, packageName, version);

                                                            code = interpreter.EvaluateScript(
                                                                text, ref result); /* EXEMPT */

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                code = interpreter.RequirePackage(
                                                                    packageName, version, exact,
                                                                    ref result);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            TraceOps.DebugTrace(String.Format(
                                                                "Execute: package unknown not " +
                                                                "configured for interpreter {0}",
                                                                FormatOps.InterpreterNoThrow(
                                                                interpreter)), typeof(Package).Name,
                                                                TracePriority.PackageDebug2);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"package require ?-exact? package ?version?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package require ?-exact? package ?version?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "reset":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            code = interpreter.ResetPkgIndexes(false, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package reset\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "scan":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            IOption interpreterOption = new Option(
                                                null, OptionFlags.None, Index.Invalid, Index.Invalid,
                                                "-interpreter", null);

                                            PackageIndexFlags oldFlags = Defaults.PackageIndexFlags;
                                            int argumentIndex; /* REUSED */

                                            if (arguments.Count > 2)
                                            {
                                                OptionDictionary preOptions = new OptionDictionary(
                                                    new IOption[] {
                                                    interpreterOption,
                                                    Option.CreateEndOfOptions()
                                                });

                                                argumentIndex = Index.Invalid; /* IGNORED */

                                                code = interpreter.CheckOptions(
                                                    preOptions, arguments, 0, 2, Index.Invalid,
                                                    ref argumentIndex, ref result);

                                                if ((code == ReturnCode.Ok) &&
                                                    preOptions.IsPresent("-interpreter"))
                                                {
                                                    oldFlags = interpreter.PackageIndexFlags;
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                //
                                                // HACK: The "-interpreter" option has now already been processed;
                                                //       therefore, permit it to be present (because it will __still__
                                                //       be present in the "arguments" list if it was before) but
                                                //       just ignore it.
                                                //
                                                interpreterOption.Flags |= OptionFlags.Ignored;

                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    interpreterOption,
                                                    new Option(typeof(PackageIndexFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-flags", new Variant(oldFlags)),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-preferfilesystem", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-preferhost", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-host", null),
#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-plugin", null),
#else
                                                    new Option(null, OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-plugin", null),
#endif
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-normal", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nonormal", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-recursive", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-resolve", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-refresh", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-autopath", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-trace", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-verbose", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-reset", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-whatif", null),
#if NATIVE
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-notrusted", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noverified", null),
#else
                                                    new Option(null, OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-notrusted", null),
                                                    new Option(null, OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-noverified", null),
#endif
                                                    Option.CreateEndOfOptions()
                                                });

                                                argumentIndex = Index.Invalid;

                                                if (arguments.Count > 2)
                                                    code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                                else
                                                    code = ReturnCode.Ok;

                                                if (code == ReturnCode.Ok)
                                                {
                                                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                    {
                                                        bool reset = false;

                                                        if (options.IsPresent("-reset"))
                                                            reset = true;

                                                        bool autoPath = false;

                                                        if (options.IsPresent("-autopath"))
                                                            autoPath = true;

                                                        bool whatIf = false;

                                                        if (options.IsPresent("-whatif"))
                                                            whatIf = true;

                                                        Variant value = null;
                                                        PackageIndexFlags newFlags = oldFlags;

                                                        if (whatIf)
                                                        {
                                                            newFlags &= ~PackageIndexFlags.Host;

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                                                            newFlags &= ~PackageIndexFlags.Plugin;
#endif

                                                            newFlags |= PackageIndexFlags.WhatIf;
                                                        }

                                                        if (options.IsPresent("-flags", ref value))
                                                            newFlags = (PackageIndexFlags)value.Value;

                                                        if (options.IsPresent("-preferfilesystem"))
                                                            newFlags |= PackageIndexFlags.PreferFileSystem;

                                                        if (options.IsPresent("-preferhost"))
                                                            newFlags |= PackageIndexFlags.PreferHost;

                                                        if (options.IsPresent("-host"))
                                                            newFlags |= PackageIndexFlags.Host;

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                                                        if (options.IsPresent("-plugin"))
                                                            newFlags |= PackageIndexFlags.Plugin;
#endif

                                                        if (options.IsPresent("-normal"))
                                                            newFlags |= PackageIndexFlags.Normal;

                                                        if (options.IsPresent("-nonormal"))
                                                            newFlags |= PackageIndexFlags.NoNormal;

                                                        if (options.IsPresent("-recursive"))
                                                            newFlags |= PackageIndexFlags.Recursive;

                                                        if (options.IsPresent("-refresh"))
                                                            newFlags |= PackageIndexFlags.Refresh;

                                                        if (options.IsPresent("-resolve"))
                                                            newFlags |= PackageIndexFlags.Resolve;

                                                        if (options.IsPresent("-trace"))
                                                            newFlags |= PackageIndexFlags.Trace;

                                                        if (options.IsPresent("-verbose"))
                                                            newFlags |= PackageIndexFlags.Verbose;

#if NATIVE
                                                        if (options.IsPresent("-notrusted"))
                                                            newFlags |= PackageIndexFlags.NoTrusted;

                                                        if (options.IsPresent("-noverified"))
                                                            newFlags |= PackageIndexFlags.NoVerified;
#endif

                                                        StringList paths = null;

                                                        if (argumentIndex != Index.Invalid)
                                                        {
                                                            //
                                                            // NOTE: Refresh the specified path list.
                                                            //
                                                            paths = new StringList(arguments, argumentIndex);
                                                        }
                                                        else if (!whatIf)
                                                        {
                                                            //
                                                            // NOTE: Refresh the default path list.
                                                            //
                                                            paths = GlobalState.GetAutoPathList(interpreter, autoPath);

                                                            //
                                                            // NOTE: Did they request the auto-path be rebuilt?
                                                            //
                                                            if (autoPath)
                                                            {
                                                                //
                                                                // NOTE: Since the actual auto-path may have changed,
                                                                //       update the variable now.  We disable traces
                                                                //       here because we manually rescan, if necessary,
                                                                //       below.
                                                                //
                                                                code = interpreter.SetAutoPathList(paths, true, ref result);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "must scan specific directories in \"what-if\" mode";
                                                            code = ReturnCode.Error;
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (whatIf)
                                                            {
                                                                ResultList errors = null;

                                                                if (FlagOps.HasFlags(newFlags, PackageIndexFlags.Host, true))
                                                                {
                                                                    if (errors == null)
                                                                        errors = new ResultList();

                                                                    errors.Add(String.Format(
                                                                        "cannot use {0} package index flag in \"what-if\" mode",
                                                                        PackageIndexFlags.Host));
                                                                }

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                                                                if (FlagOps.HasFlags(newFlags, PackageIndexFlags.Plugin, true))
                                                                {
                                                                    if (errors == null)
                                                                        errors = new ResultList();

                                                                    errors.Add(String.Format(
                                                                        "cannot use {0} package index flag in \"what-if\" mode",
                                                                        PackageIndexFlags.Plugin));
                                                                }
#endif

                                                                if (errors != null)
                                                                {
                                                                    result = errors;
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                PackageIndexDictionary packageIndexes = reset ?
                                                                    null : interpreter.CopyPackageIndexes();

                                                                if (whatIf)
                                                                {
                                                                    PackageContextClientData packageContext =
                                                                        new PackageContextClientData();

                                                                    code = PackageOps.FindAll(
                                                                        interpreter, paths, newFlags,
                                                                        interpreter.PathComparisonType,
                                                                        ref packageIndexes, ref packageContext,
                                                                        ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                        result = packageContext.ToString();
                                                                }
                                                                else
                                                                {
                                                                    code = PackageOps.FindAll(
                                                                        interpreter, paths, newFlags,
                                                                        interpreter.PathComparisonType,
                                                                        ref packageIndexes, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        interpreter.PackageIndexes = packageIndexes;
                                                                        result = String.Empty;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package scan ?options? ?dir dir ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unknown":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                interpreter.PackageUnknown = arguments[2];
                                                result = String.Empty;
                                            }
                                            else
                                            {
                                                result = interpreter.PackageUnknown;
                                            }

                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package unknown ?command?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "vcompare":
                                case "vsort":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            bool vsort = SharedStringOps.SystemEquals(subCommand, "vsort");

                                            string versionString1 = arguments[2];
                                            Version version1 = null;

                                            code = Value.GetVersion(
                                                versionString1, interpreter.InternalCultureInfo,
                                                ref version1, ref result);

                                            if ((code != ReturnCode.Ok) && vsort)
                                            {
                                                version1 = null; /* REDUNDANT */
                                                code = ReturnCode.Ok;
                                            }

                                            string versionString2 = arguments[3];
                                            Version version2 = null;

                                            if (code == ReturnCode.Ok)
                                            {
                                                code = Value.GetVersion(
                                                    versionString2, interpreter.InternalCultureInfo,
                                                    ref version2, ref result);

                                                if ((code != ReturnCode.Ok) && vsort)
                                                {
                                                    version2 = null; /* REDUNDANT */
                                                    code = ReturnCode.Ok;
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((version1 == null) && (version2 == null))
                                                {
                                                    result = SharedStringOps.SystemCompare(
                                                        versionString1, versionString2);
                                                }
                                                else
                                                {
                                                    result = PackageOps.VersionCompare(
                                                        version1, version2);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} version1 version2\"",
                                                this.Name, subCommand);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "versions":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            code = interpreter.PkgVersions(
                                                arguments[2], ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package versions package\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "vloaded":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            code = interpreter.PkgLoaded(
                                                pattern, false, true, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package vloaded ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "vsatisfies":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            PackageFlags flags = interpreter.PackageFlags;

                                            if (!FlagOps.HasFlags(flags, PackageFlags.AlwaysSatisfy, true))
                                            {
                                                Version version1 = null;

                                                code = Value.GetVersion(
                                                    arguments[2], interpreter.InternalCultureInfo,
                                                    ref version1, ref result);

                                                Version version2 = null;

                                                if (code == ReturnCode.Ok)
                                                {
                                                    code = Value.GetVersion(
                                                        arguments[3], interpreter.InternalCultureInfo,
                                                        ref version2, ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    result = PackageOps.VersionSatisfies(
                                                        version1, version2, false);
                                                }
                                            }
                                            else
                                            {
                                                //
                                                // HACK: Always fake that this was a satisfied package request.
                                                //
                                                result = true;
                                                code = ReturnCode.Ok;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package vsatisfies version1 version2\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "withdraw":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            Version version = null;

                                            if (arguments.Count == 4)
                                                code = Value.GetVersion(
                                                    arguments[3], interpreter.InternalCultureInfo,
                                                    ref version, ref result);

                                            if (code == ReturnCode.Ok)
                                                code = interpreter.WithdrawPackage(
                                                    arguments[2], version, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package withdraw package ?version?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        result = ScriptOps.BadSubCommand(
                                            interpreter, null, null, subCommand, this, null, null);

                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"package arg ?arg ...?\"";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
