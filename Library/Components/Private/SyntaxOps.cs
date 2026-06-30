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
    /// <summary>
    /// This class provides the private helper methods used to load, parse,
    /// cache, format, and save the command syntax (help) data used by Eagle.
    /// Syntax data is read from tab-separated resources and files (including
    /// per-plugin resources), merged into an in-memory cache keyed by command
    /// name, and formatted for display.  It also implements the lower-level
    /// delimited-data parser, including optional value wrapping and escaping.
    /// All members are static; the class is never instantiated.
    /// </summary>
    [ObjectId("1ca735b8-15d2-465a-9439-42ed6a42b14a")]
    internal static class SyntaxOps
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The file name search pattern used when loading syntax data files
        /// from a directory.
        /// </summary>
        private static string ResourcePattern = "syntax*.tsv";

        /// <summary>
        /// The name of the embedded resource that contains the core command
        /// syntax data.
        /// </summary>
        private static string CoreResourceName = "syntax.tsv";

        /// <summary>
        /// The name of the per-plugin resource that contains plugin command
        /// syntax data.
        /// </summary>
        private static string PluginResourceName = "syntax.tsv";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The characters that introduce a comment line in syntax data.
        /// </summary>
        private static char[] CommentChars = { Characters.SemiColon };

        /// <summary>
        /// The characters that terminate a line in syntax data.
        /// </summary>
        private static char[] LineChars = Characters.LineTerminatorChars;

        /// <summary>
        /// The characters that separate fields within a line of syntax data.
        /// </summary>
        private static char[] FieldChars = { Characters.HorizontalTab };

        /// <summary>
        /// The characters used to wrap (quote) a field value in syntax data.
        /// </summary>
        private static char[] WrapChars = { Characters.QuotationMark };

        /// <summary>
        /// The characters used to escape special characters within a wrapped
        /// field value in syntax data.
        /// </summary>
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
        /// <summary>
        /// The prefix that denotes a syntax entry consisting only of a list of
        /// its sub-commands (for example, <c>[host screen]</c>).
        /// </summary>
        private static string SubCommandsOnlyPrefix =
            Characters.Comment.ToString();
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The separator used between alternative values when formatting syntax
        /// for display.
        /// </summary>
        private static string ValueSeparator = String.Format(
            "{0}-OR-{0}", Characters.Space);

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The metadata name under which the comment characters are reported to
        /// the parse callback.
        /// </summary>
        private static string CommentMetadataName = "comment";

        /// <summary>
        /// The metadata name under which the line characters are reported to the
        /// parse callback.
        /// </summary>
        private static string LineMetadataName = "line";

        /// <summary>
        /// The metadata name under which the field characters are reported to
        /// the parse callback.
        /// </summary>
        private static string FieldMetadataName = "field";

        /// <summary>
        /// The metadata name under which the wrap characters are reported to the
        /// parse callback.
        /// </summary>
        private static string WrapMetadataName = "wrap";

        /// <summary>
        /// The metadata name under which the escape characters are reported to
        /// the parse callback.
        /// </summary>
        private static string EscapeMetadataName = "escape";

        /// <summary>
        /// The metadata name under which the syntax data flags are reported to
        /// the parse callback.
        /// </summary>
        private static string FlagsMetadataName = "flags";

        /// <summary>
        /// The metadata name under which the remove-empty setting is reported to
        /// the parse callback.
        /// </summary>
        private static string RemoveEmptyMetadataName = "removeEmpty";

        /// <summary>
        /// The metadata name under which the current line index is reported to
        /// the parse callback.
        /// </summary>
        private static string IndexMetadataName = "index";

        /// <summary>
        /// The metadata name under which the current field count is reported to
        /// the parse callback.
        /// </summary>
        private static string CountMetadataName = "count";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the syntax data cache and
        /// configuration.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, all syntax data initialization is disabled.
        /// </summary>
        private static bool Disabled = false;

        /// <summary>
        /// When non-zero, the core embedded syntax resource is loaded during
        /// initialization.
        /// </summary>
        private static bool UseCore = true;

        /// <summary>
        /// The optional set of external file names from which to load syntax
        /// data during initialization.  This may be null.
        /// </summary>
        private static IEnumerable<string> UseFileNames = null;

        /// <summary>
        /// The optional text encoding used when reading external syntax data
        /// files.  This may be null, meaning the default encoding.
        /// </summary>
        private static Encoding UseEncoding = null;

        /// <summary>
        /// When non-zero, per-plugin syntax resources are loaded during
        /// initialization.
        /// </summary>
        private static bool UsePlugins = true;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, duplicate values loaded from the core syntax resource
        /// are removed.
        /// </summary>
        private static bool CoreUnique = false;

        /// <summary>
        /// When non-zero, duplicate values loaded from external syntax files are
        /// removed.
        /// </summary>
        private static bool FileUnique = true;

        /// <summary>
        /// When non-zero, duplicate values loaded from per-plugin syntax
        /// resources are removed.
        /// </summary>
        private static bool PluginUnique = true;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The in-memory cache of syntax data, keyed by command name.  This may
        /// be null until initialization populates it.
        /// </summary>
        private static SyntaxData cache;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method merges two collections of syntax data, combining the
        /// value lists of any keys present in both, and optionally removing
        /// duplicate values.
        /// </summary>
        /// <param name="oldData">
        /// The existing syntax data to start from.  This parameter may be null.
        /// </param>
        /// <param name="newData">
        /// The new syntax data to merge in.  This parameter may be null, in
        /// which case the old data is returned unchanged.
        /// </param>
        /// <param name="unique">
        /// Non-zero to remove duplicate values from each merged value list.
        /// </param>
        /// <param name="outData">
        /// Upon success, this contains the merged syntax data.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method initializes the syntax data cache, loading the core
        /// resource, any configured external files, and per-plugin resources, as
        /// enabled by the current configuration.  If the cache is already
        /// populated, it is only rebuilt when forced.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to enumerate plugins and resolve per-plugin
        /// syntax resources.  This parameter may be null.
        /// </param>
        /// <param name="force">
        /// Non-zero to clear and rebuild the cache even if it is already
        /// populated.
        /// </param>
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

        /// <summary>
        /// This method gets copies of the character sets used when loading and
        /// parsing syntax data, validating that none of them are null or empty.
        /// </summary>
        /// <param name="commentChars">
        /// Upon success, this contains a copy of the comment characters.
        /// </param>
        /// <param name="lineChars">
        /// Upon success, this contains a copy of the line characters.
        /// </param>
        /// <param name="fieldChars">
        /// Upon success, this contains a copy of the field characters.
        /// </param>
        /// <param name="wrapChars">
        /// Upon success, this contains a copy of the wrap characters.
        /// </param>
        /// <param name="escapeChars">
        /// Upon success, this contains a copy of the escape characters.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if all character sets were valid and copied; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets the single comment, line, and field characters (and
        /// copies of the wrap and escape character sets) used when saving syntax
        /// data.
        /// </summary>
        /// <param name="commentChar">
        /// Upon success, this contains the comment character.
        /// </param>
        /// <param name="lineChar">
        /// Upon success, this contains the line character.
        /// </param>
        /// <param name="fieldChar">
        /// Upon success, this contains the field character.
        /// </param>
        /// <param name="wrapChars">
        /// Upon success, this contains a copy of the wrap characters.
        /// </param>
        /// <param name="escapeChars">
        /// Upon success, this contains a copy of the escape characters.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the characters were obtained successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method serializes the specified syntax data into delimited text,
        /// optionally wrapping the names and values, and sorting the resulting
        /// lines.
        /// </summary>
        /// <param name="data">
        /// The syntax data to serialize.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling serialization, such as whether values are
        /// wrapped and escaped.
        /// </param>
        /// <param name="text">
        /// Upon success, this contains the serialized text.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
        public static ReturnCode SaveData(
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
                        !MaybeWrap(
                            ref localValue, wrapChars, escapeChars,
                            flags, ref error))
                    {
                        return ReturnCode.Error;
                    }

                    lines.Add(String.Format(
                        "{0}{1}{2}", name, fieldChar, localValue));
                }
            }

            lines.Sort(); /* O(N log N) */

            text = lines.ToRawString(lineChar.ToString());
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        /// <summary>
        /// This method checks whether the specified syntax value denotes a
        /// sub-commands-only entry and, if so, builds the syntax string for its
        /// list of sub-commands.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to split the value into a list.  This parameter
        /// may be null.
        /// </param>
        /// <param name="name">
        /// The command name associated with the value.  This parameter may be
        /// null.
        /// </param>
        /// <param name="value">
        /// The syntax value to check.  This parameter may be null.
        /// </param>
        /// <param name="noName">
        /// Non-zero to omit the command name from the generated syntax string.
        /// </param>
        /// <returns>
        /// The generated sub-command syntax string, or null if the value is not
        /// a sub-commands-only entry.
        /// </returns>
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
        /// <summary>
        /// This method appends a section of diagnostic information about the
        /// syntax data subsystem (its cache and configuration) to the specified
        /// list.
        /// </summary>
        /// <param name="list">
        /// The list to which the information is appended.  This parameter may be
        /// null, in which case nothing is done.
        /// </param>
        /// <param name="detailFlags">
        /// The flags controlling the level of detail, including whether empty
        /// values are included.
        /// </param>
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

                if (empty || (CountMetadataName != null))
                {
                    localList.Add("CountMetadataName",
                        (CountMetadataName != null) ?
                            FormatOps.DisplayString(CountMetadataName) :
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
        /// <summary>
        /// This method clears the in-memory syntax data cache.
        /// </summary>
        /// <returns>
        /// The number of entries that were present in the cache before it was
        /// cleared.
        /// </returns>
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

        /// <summary>
        /// This method gets the formatted syntax string for the command
        /// identified by the specified identifier name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null.
        /// </param>
        /// <param name="identifierName">
        /// The identifier whose name selects the syntax entry.  This parameter
        /// may be null.
        /// </param>
        /// <param name="extra">
        /// An optional extra value to append to the syntax values.  This
        /// parameter may be null.
        /// </param>
        /// <param name="default">
        /// The value to return when no syntax is available.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The formatted syntax string, or <paramref name="default" /> when no
        /// syntax is available.
        /// </returns>
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

        /// <summary>
        /// This method gets the formatted syntax string for the command with
        /// the specified name, also reporting the kind of help that was found.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The command name that selects the syntax entry.  This parameter may
        /// be null.
        /// </param>
        /// <param name="extra">
        /// An optional extra value to append to the syntax values.  This
        /// parameter may be null.
        /// </param>
        /// <param name="default">
        /// The value to return when no syntax is available.  This parameter may
        /// be null.
        /// </param>
        /// <param name="type">
        /// Upon return, this contains the kind of help that was located (for
        /// example, a sub-command, a help topic, or a resolved entity type).
        /// </param>
        /// <returns>
        /// The formatted syntax string, or <paramref name="default" /> when no
        /// syntax is available.
        /// </returns>
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

        /// <summary>
        /// This method formats a list of syntax values for display, joining
        /// them with the configured value separator and applying line breaks and
        /// indentation as appropriate.
        /// </summary>
        /// <param name="values">
        /// The syntax values to format.
        /// </param>
        /// <returns>
        /// The formatted syntax string.
        /// </returns>
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

        /// <summary>
        /// This method gets the list of command names for which syntax data is
        /// available, discarding any error message.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to initialize the syntax data.  This
        /// parameter may be null.
        /// </param>
        /// <param name="names">
        /// Upon success, this contains the list of available command names.
        /// </param>
        /// <returns>
        /// True if the names were obtained successfully; otherwise, false.
        /// </returns>
        public static bool GetNames(
            Interpreter interpreter, /* in */
            ref StringList names     /* out */
            )
        {
            Result error = null;

            return GetNames(interpreter, ref names, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the list of command names for which syntax data is
        /// available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to initialize the syntax data.  This
        /// parameter may be null.
        /// </param>
        /// <param name="names">
        /// Upon success, this contains the list of available command names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the names were obtained successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets the syntax values associated with the specified
        /// command name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to initialize the syntax data.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The command name whose values are requested.  This parameter may be
        /// null.
        /// </param>
        /// <param name="values">
        /// Upon success, this contains a copy of the syntax values for the
        /// command.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the values were obtained successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets a dictionary mapping each command name to its
        /// formatted syntax string, discarding any error message.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to initialize the syntax data.  This
        /// parameter may be null.
        /// </param>
        /// <param name="merge">
        /// Non-zero to overwrite entries already present in the dictionary; zero
        /// to leave existing entries unchanged.
        /// </param>
        /// <param name="dictionary">
        /// Upon success, this contains the mapping of command names to formatted
        /// syntax strings.
        /// </param>
        /// <returns>
        /// True if the dictionary was populated successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets a dictionary mapping each command name to its
        /// formatted syntax string.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to initialize the syntax data.  This
        /// parameter may be null.
        /// </param>
        /// <param name="merge">
        /// Non-zero to overwrite entries already present in the dictionary; zero
        /// to leave existing entries unchanged.
        /// </param>
        /// <param name="dictionary">
        /// Upon success, this contains the mapping of command names to formatted
        /// syntax strings.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the dictionary was populated successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified character is present in
        /// the supplied set of characters.
        /// </summary>
        /// <param name="haveCharacter">
        /// The character to look for.
        /// </param>
        /// <param name="wantCharacters">
        /// The set of characters to search.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the character is present in the set; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines the wrap prefix, wrap suffix, and escape
        /// characters from the supplied wrap and escape character sets.
        /// </summary>
        /// <param name="wrapChars">
        /// The wrap characters.  This parameter may be null.
        /// </param>
        /// <param name="escapeChars">
        /// The escape characters.  This parameter may be null.
        /// </param>
        /// <param name="prefixChar">
        /// Upon success, this contains the wrap prefix character.
        /// </param>
        /// <param name="suffixChar">
        /// Upon success, this contains the wrap suffix character (the same as
        /// the prefix when only one wrap character is supplied).
        /// </param>
        /// <param name="escapeChar">
        /// Upon success, this contains the escape character.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the characters were determined successfully; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method wraps the specified value in the configured wrap
        /// characters, optionally escaping any embedded wrap and escape
        /// characters first.
        /// </summary>
        /// <param name="value">
        /// On input, the value to wrap; upon success, the wrapped (and possibly
        /// escaped) value.  This parameter may be null, which is an error.
        /// </param>
        /// <param name="wrapChars">
        /// The wrap characters.  This parameter may be null.
        /// </param>
        /// <param name="escapeChars">
        /// The escape characters.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling wrapping, including whether values are escaped.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the value was wrapped successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method unwraps the specified value, removing the configured wrap
        /// characters and optionally unescaping any embedded wrap and escape
        /// characters, when the value is actually wrapped.
        /// </summary>
        /// <param name="value">
        /// On input, the value to unwrap; upon success, the unwrapped (and
        /// possibly unescaped) value.  This parameter may be null, which is an
        /// error.
        /// </param>
        /// <param name="wrapChars">
        /// The wrap characters.  This parameter may be null.
        /// </param>
        /// <param name="escapeChars">
        /// The escape characters.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling unwrapping, including whether values are
        /// escaped.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the value was unwrapped (or was not wrapped) successfully;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method parses delimited (tab-separated) text into rows of
        /// fields, invoking the specified callback for each non-comment data
        /// line.  It honors the configured comment, line, field, wrap, and
        /// escape character sets and the supplied flags, including optional
        /// value unwrapping and removal of empty fields.
        /// </summary>
        /// <param name="text">
        /// The text to parse.  This parameter may not be null or empty.
        /// </param>
        /// <param name="callback">
        /// The callback invoked for each data row.  This parameter may not be
        /// null.
        /// </param>
        /// <param name="commentChars">
        /// The characters that introduce a comment line.  This parameter may not
        /// be null.
        /// </param>
        /// <param name="lineChars">
        /// The characters that terminate a line.  This parameter may not be
        /// null.
        /// </param>
        /// <param name="fieldChars">
        /// The characters that separate fields within a line.  This parameter
        /// may not be null.
        /// </param>
        /// <param name="wrapChars">
        /// The characters used to wrap a field value.  This parameter may not be
        /// null.
        /// </param>
        /// <param name="escapeChars">
        /// The characters used to escape special characters within a wrapped
        /// value.  This parameter may not be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling parsing behavior.
        /// </param>
        /// <param name="clientData">
        /// The client data passed to (and updated by) the callback.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Exception" /> if the callback threw an
        /// exception; otherwise, an appropriate error return code.
        /// </returns>
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
                int fieldLength = fields.Length;

                if (FlagOps.HasFlags(
                        flags, SyntaxDataFlags.WrapValues, true))
                {
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

                    metadata[CountMetadataName] = new StringPair(
                        CountMetadataName, fieldLength.ToString());
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

        /// <summary>
        /// This method is the parse callback used to load syntax data, adding
        /// each parsed <c>name</c>/<c>value</c> row into the syntax data carried
        /// by the client data, optionally splitting the value into a list and/or
        /// removing duplicates.
        /// </summary>
        /// <param name="metadata">
        /// The per-row metadata supplied by the parser.  This parameter is not
        /// used.
        /// </param>
        /// <param name="row">
        /// The fields of the current row.  This parameter may be null, which is
        /// an error.
        /// </param>
        /// <param name="clientData">
        /// The client data carrying the syntax data being built.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the row was processed successfully; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method loads syntax data from the specified text, translating
        /// the supplied boolean options into the corresponding syntax data
        /// flags.
        /// </summary>
        /// <param name="text">
        /// The text to load.
        /// </param>
        /// <param name="unique">
        /// Non-zero to remove duplicate values.
        /// </param>
        /// <param name="listValues">
        /// Non-zero to treat each value as a list to be split into individual
        /// values.
        /// </param>
        /// <param name="data">
        /// On input, the existing syntax data to merge into; upon success, the
        /// merged syntax data.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method loads syntax data from the specified text by parsing it
        /// and merging the result into the supplied syntax data.
        /// </summary>
        /// <param name="text">
        /// The text to load.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the data is loaded and merged.
        /// </param>
        /// <param name="data">
        /// On input, the existing syntax data to merge into; upon success, the
        /// merged syntax data.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method loads syntax data from the specified text and merges it
        /// directly into the shared in-memory cache.
        /// </summary>
        /// <param name="text">
        /// The text to load.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the data is loaded and merged.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method loads syntax data from the specified file and merges it
        /// into the supplied syntax data.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to read.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use when reading the file.  This parameter may
        /// be null, meaning the default encoding.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the data is loaded and merged.
        /// </param>
        /// <param name="data">
        /// On input, the existing syntax data to merge into; upon success, the
        /// merged syntax data.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method loads syntax data from all matching files in the
        /// specified directory (optionally recursively) and merges them into the
        /// supplied syntax data.
        /// </summary>
        /// <param name="directory">
        /// The directory to search for syntax data files.  This parameter may
        /// not be null or empty.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use when reading the files.  This parameter may
        /// be null, meaning the default encoding.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the data is loaded and merged, including
        /// whether the search is recursive and how errors are handled.
        /// </param>
        /// <param name="data">
        /// On input, the existing syntax data to merge into; upon success, the
        /// merged syntax data.  This parameter may be null.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this contains the accumulated error messages.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method loads syntax data from all matching files in the
        /// specified directory and merges them directly into the shared
        /// in-memory cache.
        /// </summary>
        /// <param name="directory">
        /// The directory to search for syntax data files.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use when reading the files.  This parameter may
        /// be null, meaning the default encoding.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the data is loaded and merged.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this contains the accumulated error messages.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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
