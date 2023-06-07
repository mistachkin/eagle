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
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using SyntaxData = System.Collections.Generic.Dictionary<
    string, Eagle._Containers.Public.StringList>;

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
        private static string CoreResourceName = "syntax.tsv";
        private static string PluginResourceName = "syntax.tsv";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static char[] LineChars = Characters.LineTerminatorChars;
        private static char[] FieldChars = { Characters.HorizontalTab };

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

                        foreach (KeyValuePair<string, _Wrappers.Plugin> pair
                                in plugins)
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

        private static bool GetLoadChars(
            ref char[] lineChars,  /* out */
            ref char[] fieldChars, /* out */
            ref Result error       /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
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

                lineChars = LineChars.Clone() as char[];
                fieldChars = FieldChars.Clone() as char[];

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool GetSaveChars(
            ref char? lineChar,  /* out */
            ref char? fieldChar, /* out */
            ref Result error     /* out */
            )
        {
            char[] lineChars = null;
            char[] fieldChars = null;

            if (!GetLoadChars(
                    ref lineChars, ref fieldChars,
                    ref error))
            {
                return false;
            }

            lineChar = lineChars[0];
            fieldChar = fieldChars[0];

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SaveData( /* NOT USED */
            SyntaxData data, /* in */
            ref string text, /* out */
            ref Result error /* out */
            )
        {
            if (data == null)
            {
                error = "invalid data";
                return ReturnCode.Error;
            }

            char? lineChar = null;
            char? fieldChar = null;

            if (!GetSaveChars(
                    ref lineChar, ref fieldChar,
                    ref error))
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

                foreach (string value in values)
                {
                    lines.Add(String.Format(
                        "{0}{1}{2}", name, fieldChar, value));
                }
            }

            lines.Sort(); /* O(N) */

            text = lines.ToRawString(lineChar.ToString());
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        private static string CheckForSubCommandsOnly(
            Interpreter interpreter,
            string name,
            string value,
            bool noName
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

                if (empty || (ValueSeparator != null))
                {
                    localList.Add("ValueSeparator",
                        (ValueSeparator != null) ?
                            FormatOps.DisplayString(ValueSeparator) :
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
                if (name.IndexOf(Characters.Space) == Index.Invalid)
                    type = "command";
                else
                    type = "sub-command";
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

            int maximumLength = ListOps.GetMaximumLength(
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
            if (String.IsNullOrEmpty(text))
            {
                error = "invalid text";
                return ReturnCode.Error;
            }

            char[] lineChars = null;
            char[] fieldChars = null;

            if (!GetLoadChars(
                    ref lineChars, ref fieldChars,
                    ref error))
            {
                return ReturnCode.Error;
            }

            string[] lines = text.Split(lineChars,
                StringSplitOptions.RemoveEmptyEntries);

            if (lines == null)
            {
                error = "could not split text";
                return ReturnCode.Error;
            }

            int length = lines.Length;

            if (length == 0)
            {
                error = "there are no lines";
                return ReturnCode.Error;
            }

            SyntaxData localData = null;

            for (int index = 0; index < length; index++)
            {
                string line = lines[index];

                if (String.IsNullOrEmpty(line))
                    continue;

                string[] fields = line.Split(fieldChars,
                    StringSplitOptions.RemoveEmptyEntries);

                if (fields == null)
                {
                    error = "could not split line";
                    return ReturnCode.Error;
                }

                if (fields.Length != 2) /* name <tab> value */
                {
                    error = "wrong number of fields";
                    return ReturnCode.Error;
                }

                string name = fields[0];

                if (String.IsNullOrEmpty(name))
                {
                    error = "invalid name field";
                    return ReturnCode.Error;
                }

                string value = fields[1];

                if (String.IsNullOrEmpty(value))
                {
                    error = "invalid value field";
                    return ReturnCode.Error;
                }

                if (localData == null)
                    localData = new SyntaxData();

                StringList newValues;

                if (listValues)
                {
                    newValues = null;

                    if (ParserOps<string>.SplitList(
                            null, value, 0, Length.Invalid, false,
                            ref newValues, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
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

                if (unique && (newValues != null))
                    newValues.MakeUnique();
            }

            if (MergeData(
                    data, localData, unique, ref localData,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            data = localData;
            return ReturnCode.Ok;
        }
        #endregion
    }
}
