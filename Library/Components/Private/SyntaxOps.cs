/*
 * SyntaxOps.cs --
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
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using PluginPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Plugin>;

using SyntaxData = System.Collections.Generic.Dictionary<
    string, Eagle._Containers.Public.StringList>;

using LoadDataPair = Eagle._Components.Public.MutableAnyPair<
    Eagle._Components.Public.SyntaxDataFlags,
    System.Collections.Generic.Dictionary<
        string, Eagle._Containers.Public.StringList>>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("1ca735b8-15d2-465a-9439-42ed6a42b14a")]
    internal static class SyntaxOps
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static string ResourcePattern = "syntax*.tsv";
        private static string CoreResourceName = "syntax.tsv";
        private static string PluginResourceName = "syntax.tsv";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static char[] CommentChars = { Characters.SemiColon };
        private static char[] LineChars = Characters.LineTerminatorChars;
        private static char[] FieldChars = { Characters.HorizontalTab };
        private static char[] WrapChars = { Characters.QuotationMark };
        private static char[] EscapeChars = { Characters.Backslash };

        ///////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        //
        // NOTE: This is used to denote that a given (sub-command) syntax
        //       entry consists only of a list of its (sub-)sub-commands
        //       (e.g. [host screen]).
        //
        // HACK: This is purposely not read-only.
        //
        private static string SubCommandsOnlyPrefix =
            Characters.Comment.ToString();
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static string ValueSeparator = String.Format(
            "{0}-OR-{0}", Characters.Space);

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string CommentMetadataName = "comment";
        private static string LineMetadataName = "line";
        private static string FieldMetadataName = "field";
        private static string WrapMetadataName = "wrap";
        private static string EscapeMetadataName = "escape";
        private static string FlagsMetadataName = "flags";
        private static string RemoveEmptyMetadataName = "removeEmpty";
        private static string IndexMetadataName = "index";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static bool Disabled = false;
        private static bool UseCore = true;
        private static IEnumerable<string> UseFileNames = null;
        private static Encoding UseEncoding = null;
        private static bool UsePlugins = true;

        ///////////////////////////////////////////////////////////////////////

        private static bool CoreUnique = false;
        private static bool FileUnique = true;
        private static bool PluginUnique = true;

        ///////////////////////////////////////////////////////////////////////

        private static SyntaxData cache;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static ReturnCode MergeData(
            SyntaxData oldData,     /* in: OPTIONAL */
            SyntaxData newData,     /* in: OPTIONAL */
            bool unique,            /* in */
            ref SyntaxData outData, /* out */
            ref Result error        /* out */
            )
        {
            SyntaxData localOutData;

            if (oldData != null)
                localOutData = new SyntaxData(oldData);
            else
                localOutData = new SyntaxData();

            if (newData == null)
            {
                outData = localOutData;
                return ReturnCode.Ok;
            }

            foreach (KeyValuePair<string, StringList> pair in newData)
            {
                string newName = pair.Key;

                if (String.IsNullOrEmpty(newName))
                    continue;

                StringList newValues = pair.Value;
                StringList oldValues;

                if (localOutData.TryGetValue(newName, out oldValues))
                {
                    if (oldValues != null)
                    {
                        oldValues.AddRange(newValues);
                    }
                    else
                    {
                        oldValues = new StringList(newValues);
                        localOutData[newName] = oldValues;
                    }
                }
                else
                {
                    oldValues = new StringList(newValues);
                    localOutData.Add(newName, oldValues);
                }

                if (unique && (oldValues != null))
                    oldValues.MakeUnique();
            }

            outData = localOutData;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void Initialize(
            Interpreter interpreter, /* in */
            bool force               /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (Disabled)
                    return;

                ///////////////////////////////////////////////////////////////

                if (cache != null)
                {
                    if (!force)
                        return;

                    cache.Clear();
                    cache = null;
                }

                ///////////////////////////////////////////////////////////////

                string resourceName; /* REUSED */
                string text; /* REUSED */
                ReturnCode code; /* REUSED */
                Result error; /* REUSED */

                ///////////////////////////////////////////////////////////////

                if (UseCore)
                {
                    resourceName = CoreResourceName;
                    error = null;

                    text = AssemblyOps.GetResourceStreamData(
                        GlobalState.GetAssembly(), resourceName,
                        null, false, ref error) as string;

                    if (text == null)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Initialize: get resource = {0}, " +
                            "error = {1}", FormatOps.WrapOrNull(
                            resourceName), FormatOps.WrapOrNull(
                            error)), typeof(SyntaxOps).Name,
                            TracePriority.SyntaxError);

                        return;
                    }

                    error = null;

                    code = LoadData(
                        text, CoreUnique, false, ref cache,
                        ref error);

                    if (code != ReturnCode.Ok)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Initialize: load resource = {0}, " +
                            "error = {1}", FormatOps.WrapOrNull(
                            resourceName), FormatOps.WrapOrNull(
                            error)), typeof(SyntaxOps).Name,
                            TracePriority.SyntaxError);

                        return;
                    }
                }

                ///////////////////////////////////////////////////////////////

                if (UseFileNames != null)
                {
                    foreach (string fileName in UseFileNames)
                    {
                        text = null;

                        try
                        {
                            if (UseEncoding != null)
                            {
                                text = File.ReadAllText(
                                    fileName, UseEncoding); /* throw */
                            }
                            else
                            {
                                text = File.ReadAllText(
                                    fileName); /* throw */
                            }
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(SyntaxOps).Name,
                                TracePriority.SyntaxError);
                        }

                        if (text != null)
                        {
                            error = null;

                            code = LoadData(
                                text, FileUnique, false, ref cache,
                                ref error);

                            if (code != ReturnCode.Ok)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "Initialize: load file = {0}, " +
                                    "code = {1}, error = {2}",
                                    FormatOps.WrapOrNull(fileName),
                                    code, FormatOps.WrapOrNull(error)),
                                    typeof(SyntaxOps).Name,
                                    TracePriority.SyntaxError);
                            }
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////

                if (UsePlugins && (interpreter != null))
                {
                    CultureInfo cultureInfo = interpreter.InternalCultureInfo;
                    PluginWrapperDictionary plugins = interpreter.CopyPlugins();

                    if (plugins != null)
                    {
                        resourceName = PluginResourceName;

                        foreach (PluginPair pair in plugins)
                        {
                            IPlugin plugin = pair.Value;

                            if (plugin == null)
                                continue;

                            PluginFlags pluginFlags = EntityOps.GetFlagsNoThrow(
                                plugin);

                            if (pluginFlags == PluginFlags.None)
                                continue; // NOTE: Impossible.

                            if (FlagOps.HasFlags(
                                    pluginFlags, PluginFlags.System, true))
                            {
                                continue; // NOTE: Core syntax already loaded.
                            }

                            string pluginName = EntityOps.GetNameNoThrow(
                                plugin);

                            error = null;

                            text = plugin.GetString(
                                interpreter, PluginResourceName, cultureInfo,
                                ref error);

                            if (text == null)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "Initialize: get resource = {0}, " +
                                    "plugin = {1}, error = {2}",
                                    FormatOps.WrapOrNull(resourceName),
                                    FormatOps.WrapOrNull(pluginName),
                                    FormatOps.WrapOrNull(error)),
                                    typeof(SyntaxOps).Name,
                                    TracePriority.SyntaxError);

                                continue;
                            }

                            error = null;

                            code = LoadData(
                                text, PluginUnique, false, ref cache,
                                ref error);

                            if (code != ReturnCode.Ok)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "Initialize: load resource = {0}, " +
                                    "plugin = {1}, code = {2}, error = {3}",
                                    FormatOps.WrapOrNull(resourceName),
                                    FormatOps.WrapOrNull(pluginName),
                                    code, FormatOps.WrapOrNull(error)),
                                    typeof(SyntaxOps).Name,
                                    TracePriority.SyntaxError);
                            }
                        }
                    }
                    else
                    {
                        TraceOps.DebugTrace(
                            "Initialize: plugins not available",
                            typeof(SyntaxOps).Name,
                            TracePriority.SyntaxError);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetLoadChars(
            ref char[] commentChars, /* out */
            ref char[] lineChars,    /* out */
            ref char[] fieldChars,   /* out */
            ref char[] wrapChars,    /* out */
            ref char[] escapeChars,  /* out */
            ref Result error         /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (CommentChars == null)
                {
                    error = "invalid comment characters";
                    return false;
                }

                if (CommentChars.Length == 0)
                {
                    error = "missing comment characters";
                    return false;
                }

                if (LineChars == null)
                {
                    error = "invalid line characters";
                    return false;
                }

                if (LineChars.Length == 0)
                {
                    error = "missing line characters";
                    return false;
                }

                if (FieldChars == null)
                {
                    error = "invalid field characters";
                    return false;
                }

                if (FieldChars.Length == 0)
                {
                    error = "missing field characters";
                    return false;
                }

                if (WrapChars == null)
                {
                    error = "invalid wrap characters";
                    return false;
                }

                if (WrapChars.Length == 0)
                {
                    error = "missing wrap characters";
                    return false;
                }

                if (EscapeChars == null)
                {
                    error = "invalid escape characters";
                    return false;
                }

                if (EscapeChars.Length == 0)
                {
                    error = "missing escape characters";
                    return false;
                }

                commentChars = CommentChars.Clone() as char[];
                lineChars = LineChars.Clone() as char[];
                fieldChars = FieldChars.Clone() as char[];
                wrapChars = WrapChars.Clone() as char[];
                escapeChars = EscapeChars.Clone() as char[];

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool GetSaveChars(
            ref char? commentChar,  /* out */
            ref char? lineChar,     /* out */
            ref char? fieldChar,    /* out */
            ref char[] wrapChars,   /* out */
            ref char[] escapeChars, /* out */
            ref Result error        /* out */
            )
        {
            char[] commentChars = null;
            char[] lineChars = null;
            char[] fieldChars = null;

            if (!GetLoadChars(
                    ref commentChars, ref lineChars, ref fieldChars,
                    ref wrapChars, ref escapeChars, ref error))
            {
                return false;
            }

            commentChar = commentChars[0];
            lineChar = lineChars[0];
            fieldChar = fieldChars[0];

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static ReturnCode SaveData(
            SyntaxData data,       /* in */
            SyntaxDataFlags flags, /* in */
            ref string text,       /* out */
            ref Result error       /* out */
            )
        {
            if (data == null)
            {
                error = "invalid data";
                return ReturnCode.Error;
            }

            char? commentChar = null; /* NOT USED */
            char? lineChar = null;
            char? fieldChar = null;
            char[] wrapChars = null;
            char[] escapeChars = null;

            if (!GetSaveChars(
                    ref commentChar, ref lineChar, ref fieldChar,
                    ref wrapChars, ref escapeChars, ref error))
            {
                return ReturnCode.Error;
            }

            StringList lines = new StringList();

            foreach (KeyValuePair<string, StringList> pair in data)
            {
                string name = pair.Key;

                if (String.IsNullOrEmpty(name))
                    continue;

                StringList values = pair.Value;

                if (values == null)
                    continue;

                if (FlagOps.HasFlags(
                        flags, SyntaxDataFlags.WrapValues, true) &&
                    !MaybeWrap(
                        ref name, wrapChars, escapeChars, flags,
                        ref error))
                {
                    return ReturnCode.Error;
                }

                foreach (string value in values)
                {
                    string localValue = value;

                    if (FlagOps.HasFlags(
                            flags, SyntaxDataFlags.WrapValues, true) &&
                        MaybeWrap(
                            ref localValue, wrapChars, escapeChars,
                            flags, ref error))
                    {
                        return ReturnCode.Error;
                    }

                    lines.Add(String.Format(
                        "{0}{1}{2}", name, fieldChar, localValue));
                }
            }

            lines.Sort(); /* O(N) */

            text = lines.ToRawString(lineChar.ToString());
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        private static string CheckForSubCommandsOnly(
            Interpreter interpreter, /* in */
            string name,             /* in */
            string value,            /* in */
            bool noName              /* in */
            )
        {
            if ((value == null) || (SubCommandsOnlyPrefix == null))
                return null;

            int prefixLength = SubCommandsOnlyPrefix.Length;

            if (prefixLength == 0)
                return null;

            int valueLength = value.Length;

            if ((valueLength > prefixLength) && value.StartsWith(
                    SubCommandsOnlyPrefix, StringComparison.Ordinal))
            {
                StringList list = null;
                Result error = null;

                if (ParserOps<string>.SplitList(interpreter,
                        value.Substring(prefixLength), 0, Length.Invalid,
                        true, ref list, ref error) == ReturnCode.Ok)
                {
                    return HelpOps.GetSyntaxForIEnsemble(
                        noName ? null : name, list);
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "CheckForSubCommandsOnly: error = {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(SyntaxOps).Name,
                        TracePriority.SyntaxError);
                }
            }

            return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || ((cache != null) && (cache.Count > 0)))
                {
                    localList.Add("Cache", (cache != null) ?
                        cache.Count.ToString() : FormatOps.DisplayNull);
                }

                if (empty || Disabled)
                    localList.Add("Disabled", Disabled.ToString());

                if (empty || UseCore)
                    localList.Add("UseCore", UseCore.ToString());

                if (empty || (UseFileNames != null))
                {
                    localList.Add("UseFileNames", (UseFileNames != null) ?
                        UseFileNames.ToString() : FormatOps.DisplayNull);
                }

                if (empty || (UseEncoding != null))
                {
                    localList.Add("UseEncoding", (UseEncoding != null) ?
                        UseEncoding.WebName : FormatOps.DisplayNull);
                }

                if (empty || UsePlugins)
                    localList.Add("UsePlugins", UsePlugins.ToString());

                if (empty || CoreUnique)
                    localList.Add("CoreUnique", CoreUnique.ToString());

                if (empty || FileUnique)
                    localList.Add("FileUnique", FileUnique.ToString());

                if (empty || PluginUnique)
                    localList.Add("PluginUnique", PluginUnique.ToString());

                if (empty || (CoreResourceName != null))
                {
                    localList.Add("CoreResourceName",
                        (CoreResourceName != null) ?
                            FormatOps.DisplayString(CoreResourceName) :
                            FormatOps.DisplayNull);
                }

                if (empty || (PluginResourceName != null))
                {
                    localList.Add("PluginResourceName",
                        (PluginResourceName != null) ?
                            FormatOps.DisplayString(PluginResourceName) :
                            FormatOps.DisplayNull);
                }

                if (empty || (CommentChars != null))
                {
                    localList.Add("CommentChars",
                        FormatOps.DisplayChars(CommentChars));
                }

                if (empty || (LineChars != null))
                {
                    localList.Add("LineChars",
                        FormatOps.DisplayChars(LineChars));
                }

                if (empty || (FieldChars != null))
                {
                    localList.Add("FieldChars",
                        FormatOps.DisplayChars(FieldChars));
                }

                if (empty || (WrapChars != null))
                {
                    localList.Add("WrapChars",
                        FormatOps.DisplayChars(WrapChars));
                }

                if (empty || (EscapeChars != null))
                {
                    localList.Add("EscapeChars",
                        FormatOps.DisplayChars(EscapeChars));
                }

                if (empty || (ValueSeparator != null))
                {
                    localList.Add("ValueSeparator",
                        (ValueSeparator != null) ?
                            FormatOps.DisplayString(ValueSeparator) :
                            FormatOps.DisplayNull);
                }

                if (empty || (CommentMetadataName != null))
                {
                    localList.Add("CommentMetadataName",
                        (CommentMetadataName != null) ?
                            FormatOps.DisplayString(CommentMetadataName) :
                            FormatOps.DisplayNull);
                }

                if (empty || (LineMetadataName != null))
                {
                    localList.Add("LineMetadataName",
                        (LineMetadataName != null) ?
                            FormatOps.DisplayString(LineMetadataName) :
                            FormatOps.DisplayNull);
                }

                if (empty || (FieldMetadataName != null))
                {
                    localList.Add("FieldMetadataName",
                        (FieldMetadataName != null) ?
                            FormatOps.DisplayString(FieldMetadataName) :
                            FormatOps.DisplayNull);
                }

                if (empty || (WrapMetadataName != null))
                {
                    localList.Add("WrapMetadataName",
                        (WrapMetadataName != null) ?
                            FormatOps.DisplayString(WrapMetadataName) :
                            FormatOps.DisplayNull);
                }

                if (empty || (EscapeMetadataName != null))
                {
                    localList.Add("EscapeMetadataName",
                        (EscapeMetadataName != null) ?
                            FormatOps.DisplayString(EscapeMetadataName) :
                            FormatOps.DisplayNull);
                }

                if (empty || (FlagsMetadataName != null))
                {
                    localList.Add("FlagsMetadataName",
                        (FlagsMetadataName != null) ?
                            FormatOps.DisplayString(FlagsMetadataName) :
                            FormatOps.DisplayNull);
                }

                if (empty || (RemoveEmptyMetadataName != null))
                {
                    localList.Add("RemoveEmptyMetadataName",
                        (RemoveEmptyMetadataName != null) ?
                            FormatOps.DisplayString(RemoveEmptyMetadataName) :
                            FormatOps.DisplayNull);
                }

                if (empty || (IndexMetadataName != null))
                {
                    localList.Add("IndexMetadataName",
                        (IndexMetadataName != null) ?
                            FormatOps.DisplayString(IndexMetadataName) :
                            FormatOps.DisplayNull);
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Command Syntax");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static int ClearCache()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (cache != null)
                {
                    result += cache.Count;

                    cache.Clear();
                    cache = null;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetFormatted(
            Interpreter interpreter,        /* in */
            IIdentifierName identifierName, /* in */
            string extra,                   /* in */
            string @default                 /* in */
            )
        {
            if (identifierName == null)
                return @default;

            string type = null; /* NOT USED */

            return GetFormatted(
                interpreter, identifierName.Name, extra, @default, ref type);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetFormatted(
            Interpreter interpreter, /* in */
            string name,             /* in */
            string extra,            /* in */
            string @default,         /* in */
            ref string type          /* out */
            )
        {
            StringList values = null;
            Result error = null;

            if (!GetValues(interpreter, name, ref values, ref error))
                return @default;

            if (values == null)
                return @default;

            if (extra != null)
                values.Add(extra);

#if SHELL && INTERACTIVE_COMMANDS
            if (values.Count == 1)
            {
                string subCommands = CheckForSubCommandsOnly(
                    interpreter, name, values[0], false);

                if (subCommands != null)
                {
                    type = "sub-command";

                    return subCommands;
                }
            }
#endif

            if (name != null)
            {
#if SHELL && INTERACTIVE_COMMANDS
                StringList names = null;

                if ((ParserOps<string>.SplitList(
                        null, name, 0, Length.Invalid, true,
                        ref names) != ReturnCode.Ok) ||
                    (HelpOps.GetIExecuteViaResolvers(
                        interpreter, names,
                        ref type) != ReturnCode.Ok))
#endif
                {
                    type = "help";
                }
            }

            return GetFormatted(values);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetFormatted(
            StringList values /*in */
            )
        {
            string separator;

            lock (syncRoot)
            {
                separator = ValueSeparator;
            }

            if (separator == null)
                return values.ToString();

            int separatorLength = separator.Length;

            int maximumLength = ListOps.GetMaximumLength<string>(
                values);

            int spaceLength = maximumLength - separatorLength;

            if (spaceLength <= 0)
            {
                separator = String.Format(
                    "{0}{0}{1}{0}{0}", Characters.NewLine,
                    separator);

                return values.ToRawString(separator);
            }

            separator = String.Format(
                "{0}{0}{1}{2}{0}{0}", Characters.NewLine,
                StringOps.StrRepeat(spaceLength / 2,
                Characters.Space), separator);

            return values.ToRawString(separator);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetNames(
            Interpreter interpreter, /* in */
            ref StringList names     /* out */
            )
        {
            Result error = null;

            return GetNames(interpreter, ref names, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetNames(
            Interpreter interpreter, /* in */
            ref StringList names,    /* out */
            ref Result error         /* out */
            )
        {
            Initialize(interpreter, false);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (cache == null)
                {
                    error = "cache not available";
                    return false;
                }

                names = new StringList(cache.Keys);
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetValues(
            Interpreter interpreter, /* in */
            string name,             /* in */
            ref StringList values,   /* out */
            ref Result error         /* out */
            )
        {
            Initialize(interpreter, false);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (name == null)
                {
                    error = "invalid name";
                    return false;
                }

                if (cache == null)
                {
                    error = "cache not available";
                    return false;
                }

                StringList localValues;

                if (cache.TryGetValue(name, out localValues))
                {
                    values = new StringList(localValues);
                }
                else
                {
                    error = "name not found";
                    return false;
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetFormattedNamesAndValues(
            Interpreter interpreter,        /* in */
            bool merge,                     /* in */
            ref StringDictionary dictionary /* out */
            )
        {
            Result error = null;

            return GetFormattedNamesAndValues(
                interpreter, merge, ref dictionary, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool GetFormattedNamesAndValues(
            Interpreter interpreter,         /* in */
            bool merge,                      /* in */
            ref StringDictionary dictionary, /* out */
            ref Result error                 /* out */
            )
        {
            Initialize(interpreter, false);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (cache == null)
                {
                    error = "cache not available";
                    return false;
                }

                if (dictionary == null)
                    dictionary = new StringDictionary();

                foreach (KeyValuePair<string, StringList> pair in cache)
                {
                    StringList list = pair.Value;

                    if (list == null)
                        continue;

                    int count = list.Count;

                    if (count == 0)
                        continue;

                    string name = pair.Key;

                    if (merge || !dictionary.ContainsKey(name))
                    {
                        string value;

#if SHELL && INTERACTIVE_COMMANDS
                        if (count > 1)
                        {
                            value = GetFormatted(list);
                        }
                        else
                        {
                            value = CheckForSubCommandsOnly(
                                interpreter, name, list[0], false);

                            if (value == null)
                                value = GetFormatted(list);
                        }
#else
                        value = GetFormatted(list);
#endif

                        dictionary[name] = value;
                    }
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HaveCharacter(
            char haveCharacter,   /* in */
            char[] wantCharacters /* in */
            )
        {
            if (wantCharacters != null)
                foreach (char wantCharacter in wantCharacters)
                    if (haveCharacter == wantCharacter)
                        return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool GetWrapChars(
            char[] wrapChars,     /* in */
            char[] escapeChars,   /* in */
            ref char? prefixChar, /* out */
            ref char? suffixChar, /* out */
            ref char? escapeChar, /* out */
            ref Result error      /* out */
            )
        {
            if (wrapChars == null)
            {
                error = "invalid wrap characters";
                return false;
            }

            int wrapLength = wrapChars.Length;

            if (wrapLength == 0)
            {
                error = "missing wrap characters";
                return false;
            }

            if (escapeChars == null)
            {
                error = "invalid escape characters";
                return false;
            }

            int escapeLength = escapeChars.Length;

            if (escapeLength == 0)
            {
                error = "missing escape characters";
                return false;
            }

            prefixChar = wrapChars[0];

            if (wrapLength >= 2)
                suffixChar = wrapChars[1];
            else
                suffixChar = wrapChars[0];

            escapeChar = escapeChars[0];
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeWrap(
            ref string value,      /* in, out */
            char[] wrapChars,      /* in */
            char[] escapeChars,    /* in */
            SyntaxDataFlags flags, /* in */
            ref Result error       /* out */
            )
        {
            if (value == null)
            {
                error = "cannot wrap, invalid value";
                return false;
            }

            char? prefixChar = null;
            char? suffixChar = null;
            char? escapeChar = null;

            if (!GetWrapChars(
                    wrapChars, escapeChars, ref prefixChar,
                    ref suffixChar, ref escapeChar, ref error))
            {
                return false;
            }

            if ((escapeChar != null) && FlagOps.HasFlags(
                    flags, SyntaxDataFlags.EscapeValues, true))
            {
                value = value.Replace(
                    ((char)escapeChar).ToString(), String.Format(
                    "{0}{0}", (char)escapeChar));

                value = value.Replace(
                    ((char)prefixChar).ToString(), String.Format(
                    "{0}{1}", (char)escapeChar, (char)prefixChar));

                if (suffixChar != prefixChar)
                {
                    value = value.Replace(
                        ((char)suffixChar).ToString(), String.Format(
                        "{0}{1}", (char)escapeChar, (char)suffixChar));
                }
            }

            value = String.Format("{0}{1}{2}",
                (char)prefixChar, value, (char)suffixChar); /* WRAP */

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeUnwrap(
            ref string value,      /* in, out */
            char[] wrapChars,      /* in */
            char[] escapeChars,    /* in */
            SyntaxDataFlags flags, /* in */
            ref Result error       /* out */
            )
        {
            if (value == null)
            {
                error = "cannot unwrap, invalid value";
                return false;
            }

            char? prefixChar = null;
            char? suffixChar = null;
            char? escapeChar = null;

            if (!GetWrapChars(
                    wrapChars, escapeChars, ref prefixChar,
                    ref suffixChar, ref escapeChar, ref error))
            {
                return false;
            }

            int valueLength = value.Length;

            if (valueLength < 2)
            {
                if (valueLength > 0)
                {
                    char valueChar = value[0];

                    if ((valueChar == (char)prefixChar) ||
                        (valueChar == (char)suffixChar) ||
                        (valueChar == (char)escapeChar))
                    {
                        error = "cannot unwrap, reserved character";
                        return false;
                    }
                }

                //
                // HACK: This is fine because the value is empty
                //       -OR- it has a single character that is
                //       not used for wrapping or escaping.
                //
                return true;
            }

            if ((value[0] != (char)prefixChar) ||
                (value[valueLength - 1] != (char)suffixChar))
            {
                if ((value[0] == (char)prefixChar) ||
                    (value[valueLength - 1] == (char)suffixChar))
                {
                    error = "cannot unwrap, invalid wrapping";
                    return false;
                }
                else
                {
                    //
                    // HACK: This is fine because the value is
                    //       simply not wrapped.
                    //
                    return true;
                }
            }

            if ((escapeChar != null) && FlagOps.HasFlags(
                    flags, SyntaxDataFlags.EscapeValues, true))
            {
                //
                // NOTE: When value escape handling is enabled,
                //       the wrap suffix character at the end
                //       of the value string must cannot occur
                //       immediately after the escape character.
                //       It should be noted that this check is
                //       not needed for wrap prefix characters
                //       because they are at the very start of
                //       the string and the escape character
                //       must always occur before the character
                //       it actually escapes.
                //
                if ((valueLength >= 2) &&
                    (value[valueLength - 2] == (char)escapeChar) &&
                    ((char)suffixChar != (char)escapeChar))
                {
                    error = "cannot unwrap, suffix is escaped";
                    return false;
                }

                value = value.Replace(String.Format(
                    "{0}{1}", (char)escapeChar, (char)suffixChar),
                    ((char)suffixChar).ToString());

                value = value.Replace(
                    String.Format("{0}{0}", (char)escapeChar),
                    ((char)escapeChar).ToString());
            }

            value = value.Substring(1, valueLength - 2); /* UNWRAP */
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ParseData(
            string text,                    /* in */
            StringDataRowCallback callback, /* in */
            char[] commentChars,            /* in */
            char[] lineChars,               /* in */
            char[] fieldChars,              /* in */
            char[] wrapChars,               /* in */
            char[] escapeChars,             /* in */
            SyntaxDataFlags flags,          /* in */
            ref IClientData clientData,     /* in */
            ref Result error                /* out */
            )
        {
            if (String.IsNullOrEmpty(text))
            {
                error = "invalid text";
                return ReturnCode.Error;
            }

            if (callback == null)
            {
                error = "invalid callback";
                return ReturnCode.Error;
            }

            if (commentChars == null)
            {
                error = "invalid comment characters";
                return ReturnCode.Error;
            }

            if (lineChars == null)
            {
                error = "invalid line characters";
                return ReturnCode.Error;
            }

            if (fieldChars == null)
            {
                error = "invalid field characters";
                return ReturnCode.Error;
            }

            if (wrapChars == null)
            {
                error = "invalid wrap characters";
                return ReturnCode.Error;
            }

            if (escapeChars == null)
            {
                error = "invalid escape characters";
                return ReturnCode.Error;
            }

            //
            // HACK: Since we always want to skip blank lines, use
            //       the option to simply remove entry entries here.
            //       Blank lines are not data.
            //
            string[] lines = text.Split(
                lineChars, StringSplitOptions.RemoveEmptyEntries);

            if (lines == null) /* IMPOSSIBLE (?) */
            {
                error = "could not split text";
                return ReturnCode.Error;
            }

            int lineLength = lines.Length;

            if (lineLength == 0)
            {
                if (FlagOps.HasFlags(
                        flags, SyntaxDataFlags.ErrorOnEmpty, true))
                {
                    error = "there are no lines";
                    return ReturnCode.Error;
                }
                else
                {
                    return ReturnCode.Ok;
                }
            }

            bool removeEmpty = FlagOps.HasFlags(
                flags, SyntaxDataFlags.RemoveEmpty, true);

            StringSplitOptions splitOptions = removeEmpty ?
                StringSplitOptions.RemoveEmptyEntries :
                StringSplitOptions.None;

            StringPairDictionary metadata;

            if (FlagOps.HasFlags(
                    flags, SyntaxDataFlags.NoMetadata, true))
            {
                metadata = null;
            }
            else
            {
                metadata = new StringPairDictionary();

                metadata.Add(
                    CommentMetadataName, new StringPair(
                    CommentMetadataName, StringList.MakeList(
                    commentChars)));

                metadata.Add(
                    LineMetadataName, new StringPair(
                    LineMetadataName, StringList.MakeList(
                    lineChars)));

                metadata.Add(
                    FieldMetadataName, new StringPair(
                    FieldMetadataName, StringList.MakeList(
                    fieldChars)));

                metadata.Add(
                    WrapMetadataName, new StringPair(
                    WrapMetadataName, StringList.MakeList(
                    wrapChars)));

                metadata.Add(
                    EscapeMetadataName, new StringPair(
                    EscapeMetadataName, StringList.MakeList(
                    escapeChars)));

                metadata.Add(
                    FlagsMetadataName, new StringPair(
                    FlagsMetadataName, flags.ToString()));

                metadata.Add(
                    RemoveEmptyMetadataName, new StringPair(
                    RemoveEmptyMetadataName, removeEmpty.ToString()));
            }

            for (int lineIndex = 0;
                    lineIndex < lineLength; lineIndex++)
            {
                string line = lines[lineIndex];

                //
                // NOTE: Rule #1: Blank lines are not data.
                //
                if (String.IsNullOrEmpty(line))
                    continue; /* NOTE: Blank line. */

                //
                // NOTE: Rule #2: Comment lines are not data.
                //
                if (HaveCharacter(line[0], commentChars))
                    continue; /* NOTE: Comment line. */

                //
                // NOTE: Rule #3: Lines are delimited fields.
                //
                string[] fields = line.Split(
                    fieldChars, splitOptions);

                if (fields == null)
                {
                    error = "could not split line";
                    return ReturnCode.Error;
                }

                //
                // NOTE: Rule #4: Optionally unwrap fields, only
                //       when they are confirmed to be wrapped in
                //       the current value wrapping character set.
                //
                if (FlagOps.HasFlags(
                        flags, SyntaxDataFlags.WrapValues, true))
                {
                    int fieldLength = fields.Length;

                    for (int fieldIndex = 0;
                            fieldIndex < fieldLength; fieldIndex++)
                    {
                        string field = fields[fieldIndex];

                        if (MaybeUnwrap(
                                ref field, wrapChars, escapeChars,
                                flags, ref error))
                        {
                            fields[fieldIndex] = field;
                        }
                        else
                        {
                            return ReturnCode.Error;
                        }
                    }
                }

                if (metadata != null)
                {
                    //
                    // HACK: Provide the current line index to the
                    //       callback method.
                    //
                    metadata[IndexMetadataName] = new StringPair(
                        IndexMetadataName, lineIndex.ToString());
                }

                //
                // NOTE: Rule #5: If the callback method returns
                //       failure (false) for any (or no) reason,
                //       including exceptions, abort the entire
                //       operation.
                //
                try
                {
                    if (!callback(
                            (metadata != null) ? metadata.Values : null,
                            fields, ref clientData, ref error))
                    {
                        return ReturnCode.Error;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Exception;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool LoadDataCallback(
            IEnumerable<IPair<string>> metadata, /* in: NOT USED */
            IEnumerable<string> row,             /* in */
            ref IClientData clientData,          /* in, out */
            ref Result error                     /* out */
            )
        {
            if (row == null) /* IMPOSSIBLE (?) */
            {
                error = "invalid row";
                return false;
            }

            if (clientData == null)
            {
                error = "invalid clientData";
                return false;
            }

            string[] fields = row as string[];

            if (fields == null) /* IMPOSSIBLE (?) */
            {
                error = "invalid fields from row";
                return false;
            }

            if (fields.Length != 2) /* name <tab> value */
            {
                error = "wrong number of fields";
                return false;
            }

            string name = fields[0];

            if (String.IsNullOrEmpty(name))
            {
                error = "invalid name field";
                return false;
            }

            string value = fields[1];

            if (String.IsNullOrEmpty(value))
            {
                error = "invalid value field";
                return false;
            }

            LoadDataPair pair = clientData.Data as LoadDataPair;

            if (pair == null) /* IMPOSSIBLE (?) */
            {
                error = "invalid triplet";
                return false;
            }

            SyntaxData localData = pair.Y;

            if (localData == null)
                localData = pair.Y = new SyntaxData();

            StringList newValues;

            if (FlagOps.HasFlags(
                    pair.X, SyntaxDataFlags.ListValues, true))
            {
                newValues = null;

                if (ParserOps<string>.SplitList(
                        null, value, 0, Length.Invalid, false,
                        ref newValues, ref error) != ReturnCode.Ok)
                {
                    return false;
                }

                StringList oldValues;

                if (localData.TryGetValue(name, out oldValues))
                {
                    if (oldValues != null)
                    {
                        oldValues.AddRange(newValues);
                        newValues = oldValues;
                    }
                    else
                    {
                        localData[name] = newValues;
                    }
                }
                else
                {
                    localData.Add(name, newValues);
                }
            }
            else
            {
                //
                // HACK: Allow escape codes for the various space
                //       characters that we wish to allow in the
                //       help text.
                //
                StringOps.UnescapeWhiteSpace(ref value);

                if (localData.TryGetValue(name, out newValues))
                {
                    if (newValues != null)
                    {
                        newValues.Add(value);
                    }
                    else
                    {
                        newValues = new StringList(value);
                        localData[name] = newValues;
                    }
                }
                else
                {
                    newValues = new StringList(value);
                    localData.Add(name, newValues);
                }
            }

            if ((newValues != null) && FlagOps.HasFlags(
                    pair.X, SyntaxDataFlags.Unique, true))
            {
                newValues.MakeUnique();
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method was originally private.  It is now public so it
        //       can be used to support loading lists of well-known mappings
        //       of assembly file names to plugin type names.
        //
        public static ReturnCode LoadData(
            string text,         /* in */
            bool unique,         /* in */
            bool listValues,     /* in */
            ref SyntaxData data, /* in, out */
            ref Result error     /* out */
            )
        {
            SyntaxDataFlags flags = SyntaxDataFlags.LoadData;

            if (unique)
                flags |= SyntaxDataFlags.Unique;

            if (listValues)
                flags |= SyntaxDataFlags.ListValues;

            return LoadData(text, flags, ref data, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadData(
            string text,           /* in */
            SyntaxDataFlags flags, /* in */
            ref SyntaxData data,   /* in, out */
            ref Result error       /* out */
            )
        {
            char[] commentChars = null;
            char[] lineChars = null;
            char[] fieldChars = null;
            char[] wrapChars = null;
            char[] escapeChars = null;

            if (!GetLoadChars(
                    ref commentChars, ref lineChars, ref fieldChars,
                    ref wrapChars, ref escapeChars, ref error))
            {
                return ReturnCode.Error;
            }

            LoadDataPair pair = new LoadDataPair(true, flags, null);
            IClientData clientData = new ClientData(pair);

            if (ParseData(text,
                    new StringDataRowCallback(LoadDataCallback),
                    commentChars, lineChars, fieldChars, wrapChars,
                    escapeChars, flags, ref clientData,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            SyntaxData localData = pair.Y;

            if (MergeData(
                    data, localData, FlagOps.HasFlags(flags,
                    SyntaxDataFlags.Unique, true), ref localData,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            data = localData;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadAndCacheData(
            string text,           /* in */
            SyntaxDataFlags flags, /* in */
            ref Result error       /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return LoadData(
                    text, flags, ref cache, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadDataFrom(
            string fileName,       /* in */
            Encoding encoding,     /* in */
            SyntaxDataFlags flags, /* in */
            ref SyntaxData data,   /* in, out */
            ref Result error       /* out */
            )
        {
            string text;

            try
            {
                if (encoding != null)
                    text = File.ReadAllText(fileName, encoding);
                else
                    text = File.ReadAllText(fileName);
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }

            return LoadData(text, flags, ref data, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadDataFrom(
            string directory,      /* in */
            Encoding encoding,     /* in */
            SyntaxDataFlags flags, /* in */
            ref SyntaxData data,   /* in, out */
            ref ResultList errors  /* out */
            )
        {
            if (String.IsNullOrEmpty(directory))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid directory");
                return ReturnCode.Error;
            }

            if (!Directory.Exists(directory))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("directory does not exist");
                return ReturnCode.Error;
            }

            SearchOption searchOption = FileOps.GetSearchOption(
                FlagOps.HasFlags(flags, SyntaxDataFlags.Recursive,
                true));

            string[] fileNames;

            try
            {
                fileNames = Directory.GetFiles(
                    directory, ResourcePattern, searchOption);
            }
            catch (Exception e)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
                return ReturnCode.Error;
            }

            if ((fileNames == null) || (fileNames.Length == 0))
            {
                if (FlagOps.HasFlags(flags,
                        SyntaxDataFlags.ErrorOnEmpty, true))
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("no syntax data files found");
                    return ReturnCode.Error;
                }
                else
                {
                    return ReturnCode.Ok;
                }
            }

            int errorCount = 0;

            foreach (string fileName in fileNames)
            {
                if (String.IsNullOrEmpty(fileName))
                    continue;

                Result error = null;

                if (LoadDataFrom(
                        fileName, encoding, flags, ref data,
                        ref error) != ReturnCode.Ok)
                {
                    errorCount++;

                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(error);

                    if (FlagOps.HasFlags(flags,
                            SyntaxDataFlags.StopOnError, true))
                    {
                        return ReturnCode.Error;
                    }
                }
            }

            return (errorCount > 0) ?
                ReturnCode.Error : ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadAndCacheDataFrom(
            string directory,      /* in */
            Encoding encoding,     /* in */
            SyntaxDataFlags flags, /* in */
            ref ResultList errors  /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return LoadDataFrom(
                    directory, encoding, flags, ref cache, ref errors);
            }
        }
        #endregion
    }
}
