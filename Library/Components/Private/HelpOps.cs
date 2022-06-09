/*
 * HelpOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SHELL
using System;
using System.Collections.Generic;
using System.Diagnostics;

#if INTERACTIVE_COMMANDS
using System.Globalization;
#endif

using System.IO;
using System.Reflection;

#if INTERACTIVE_COMMANDS
using System.Text;

#if XML
using System.Xml;
#endif
#endif
#endif

using Eagle._Attributes;

#if SHELL
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
#endif

#if !CONSOLE && SHELL
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

#if SHELL
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
#endif

using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("35ce3166-9f74-460f-aa62-5cb27798ef66")]
    internal static class HelpOps
    {
        #region Private Constants
        #region Help Formatting
#if SHELL
        private const int UsageWidth = 71; /* magic */

        private const string lengthPlaceholder = "X";
        private const int OptionsPerLine = 4; /* magic */

#if INTERACTIVE_COMMANDS
        private const string itemSeparator = ", ";
        private const string itemSuffix = "and ";

        private const string defaultHelpType = "interactive command";

        private const string hiddenPrefix = "hidden ";
        private const string interactiveExtensionPrefix = "interactive extension ";

        private const int columnPadding = 1; /* magic */

        private const int LineWidth = 79; /* magic */

        private const int GroupsPerLine = 5; /* magic */
        private const int TopicsPerLine = 5; /* magic */

        private const string groupOnlyFormat = "{0}";
        private const string groupListFormat = "{0,X}{1}"; /* NOTE: 'X' is replaced by an integer. */
        private const string groupAndListFormat = "{0} -- {1}";

        private const string descriptionOnlyFormat2 = "{2}";
        private const string descriptionOnlyFormat3 = "{2} -- {3}";

        private const string topicOnlyFormat = "{0}{1}";
        private const string topicListFormat = "{0,X}{1}"; /* NOTE: 'X' is replaced by an integer. */
        private const string topicWithArgumentsFormat = "{0}{1} {2}";
        private const string topicAndDescriptionFormat = "{0}{1} -- {2}";
        private const string topicWithArgumentsAndDescriptionFormat = "{0}{1} {2} -- {3}";

        private const string descriptionSeparator = " -- ";

#if XML
        //
        // HACK: Use specially formatted comments with an embedded XML tag in
        //       order to support having the description for a procedure reside
        //       directly within its body.
        //
        private static readonly string BeginMagic = "# <help>";
        private static readonly string EndMagic = "# </help>";
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Default Interactive Help Options
        //
        // HACK: These are purposely not read-only.
        //
        private static bool DefaultShowTopics = true;     /* TODO: Good default? */
        private static bool DefaultUseInterpreter = true; /* TODO: Good default? */
        private static bool DefaultUseSyntax = true;      /* TODO: Good default? */
        private static bool DefaultShowHeader = true;     /* TODO: Good default? */
        private static bool DefaultMatchingOnly = false;  /* TODO: Good default? */
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const string certificateSubjectPrefix = " - ";

        private const string bannerFormat = "{0} v{1} {2}{3}{4} {5} ({6}){7}";
        private const string bannerCompactFormat = "{0} v{1} {2}{3}{4} ({6}){7}";
        private const string bannerTextFormat = " {0}";
        private const string bannerImageRuntimeVersionFormat = " {0}";
        private const string bannerOfficialFormat = "{0}";
        private const string bannerReleaseFormat = "{0}";

        private const string versionContextFormat1 = "    Process: {0}, {2}{1}";
        private const string versionContextFormat2 = "     Thread: {0}, {2}{1}";
        private const string versionContextFormat3 = "  AppDomain: {0}, {2}{1}";
        private const string versionContextFormat4 = "Interpreter: {0}, {2}{1}";

        private const string versionUpdateFormat1 = "Engine [{0}, {1}, {2}]";
        private const string versionUpdateFormat2 = "Updates @ {0}{1}";
        private const string versionUpdateFormat3 = "Downloads @ {0}";
        private const string versionSourceIdFormat = "[{0}]";
        private const string versionSourceTimeStampFormat = "[{0}]";
        private const int versionWidth = 74; /* magic */

        private const string defaultUri = "http://localhost/";
        private const string defaultSourceId = "0000000000000000000000000000000000000000";
        private const string defaultSourceTimeStamp = "0000-00-00 00:00:00 UTC";

        private const string optionFormat = "{0,X}"; /* NOTE: 'X' is replaced by an integer. */
        private const int optionWidth = 79; /* magic */
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
#if SHELL && INTERACTIVE_COMMANDS
        private static readonly object syncRoot = new object();
        private static StringListDictionary commandGroups = null;
        private static StringPairDictionary commandHelp = null;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if XML
        private static readonly string HelpXPath = "//help";
#endif
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Help Support Methods
#if SHELL && INTERACTIVE_COMMANDS
        public static int ClearCache()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (commandGroups != null)
                {
                    result += commandGroups.Count;

                    commandGroups.Clear();
                    commandGroups = null;
                }

                if (commandHelp != null)
                {
                    result += commandHelp.Count;

                    commandHelp.Clear();
                    commandHelp = null;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if XML
        private static ReturnCode GetHelp(
            XmlDocument document,
            ref string text,
            ref Result error
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            try
            {
                XmlNamespaceManager namespaceManager = null;

                if (XmlOps.GetAssemblyNamespaceManager(
                        document.NameTable, ref namespaceManager,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                XmlNodeList nodeList = document.SelectNodes(
                    HelpXPath, namespaceManager);

                if ((nodeList == null) || (nodeList.Count == 0))
                {
                    error = "no help nodes found";
                    return ReturnCode.Error;
                }

                StringBuilder builder = StringOps.NewStringBuilder();

                foreach (XmlNode node in nodeList)
                {
                    if (node == null)
                        continue;

                    string nodeText = node.InnerText.Trim();

                    if (String.IsNullOrEmpty(nodeText))
                        continue;

                    if (builder.Length > 0)
                        builder.Append(Characters.Space);

                    builder.Append(nodeText);
                }

                if (builder.Length == 0)
                {
                    error = "no help text found";
                    return ReturnCode.Error;
                }

                text = builder.ToString();
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string ExtractHelpFromScript(
            string text /* in */
            )
        {
            if ((text == null) || (text.Length == 0))
                return null;

            int beginIndex = text.IndexOf(
                BeginMagic, SharedStringOps.SystemComparisonType);

            if (beginIndex == Index.Invalid)
                return null;

            int beginMagicLength = BeginMagic.Length;

            int endIndex = text.IndexOf(
                EndMagic, beginIndex + beginMagicLength,
                SharedStringOps.SystemComparisonType);

            if (endIndex == Index.Invalid)
                return null;

            int endMagicLength = EndMagic.Length;

            string value = text.Substring(
                beginIndex, endIndex - beginIndex + endMagicLength);

            ReturnCode code;
            Result error = null;

            code = StringOps.ExtractDataFromComments(ref value, ref error);

            if (code != ReturnCode.Ok)
            {
                DebugOps.Complain(code, error);
                return null;
            }

            XmlDocument document = null;

            code = XmlOps.LoadString(value, ref document, ref error);

            if (code != ReturnCode.Ok)
            {
                DebugOps.Complain(code, error);
                return null;
            }

            code = GetHelp(document, ref value, ref error);

            if (code != ReturnCode.Ok)
            {
                DebugOps.Complain(code, error);
                return null;
            }

            return StringOps.CollapseWhiteSpace(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetHelp(
            IIdentifier identifier /* in */
            )
        {
            return ExtractHelpFromScript(
                GetBody(identifier as IProcedure));
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsInteractiveExtension(
            IExecute execute, /* in */
            string name       /* in */
            )
        {
            IIdentifierName identifierName = execute as IIdentifierName;

            if (identifierName == null)
                return IsInteractiveExtension(name);

            return IsInteractiveExtension(identifierName.Name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsInteractiveExtension(
            IExecute execute /* in */
            )
        {
            IIdentifierName identifierName = execute as IIdentifierName;

            if (identifierName == null)
                return false;

            return IsInteractiveExtension(identifierName.Name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsInteractiveExtension(
            string name /* in */
            )
        {
            if (String.IsNullOrEmpty(name))
                return false;

            foreach (string prefix in new string[] {
                    ShellOps.InteractiveSystemCommandPrefix,
                    ShellOps.InteractiveCommandPrefix })
            {
                if (String.IsNullOrEmpty(prefix))
                    continue;

                if (name.StartsWith(prefix,
                        SharedStringOps.SystemNoCaseComparisonType))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MaybeAdjustHelpItemTopic(
            string topic,         /* in */
            string defaultPrefix, /* in */
            out string prefix     /* out */
            )
        {
            string helpType;

            return MaybeAdjustHelpItemTopic(
                topic, defaultPrefix, out prefix, out helpType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MaybeAdjustHelpItemTopic(
            string topic,         /* in */
            string defaultPrefix, /* in */
            out string prefix,    /* out */
            out string helpType   /* out */
            )
        {
            if (!String.IsNullOrEmpty(topic))
            {
                string[] prefixes = ShellOps.InteractiveCommandPrefixes;
                char prefixChar = ShellOps.InteractiveCommandPrefixChar;

                if (prefixes != null)
                {
                    int prefixesLength = prefixes.Length;

                    if ((prefixesLength % 2) == 0)
                    {
                        for (int index = 0; index < prefixesLength; index += 2)
                        {
                            string localPrefix = prefixes[index];

                            if (String.IsNullOrEmpty(localPrefix))
                                continue;

                            string localHelpType = prefixes[index + 1];

                            if (String.IsNullOrEmpty(localHelpType))
                                localHelpType = defaultHelpType;

                            if (topic.StartsWith(localPrefix,
                                    SharedStringOps.SystemNoCaseComparisonType))
                            {
                                int prefixLength = localPrefix.Length;

                                if (topic[prefixLength] != prefixChar)
                                {
                                    prefix = localPrefix;
                                    helpType = localHelpType;

                                    return topic.Substring(prefixLength);
                                }
                            }
                        }
                    }
                }
            }

            prefix = defaultPrefix;
            helpType = defaultHelpType;

            return topic;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetIExecuteViaResolvers(
            Interpreter interpreter, /* in */
            string topic1,           /* in */
            string topic2,           /* in */
            ref string name,         /* out */
            ref IExecute execute     /* out */
            )
        {
            if (interpreter == null)
                return ReturnCode.Error;

            IExecute localExecute; /* REUSED */
            EngineFlags engineFlags = interpreter.GetResolveEngineFlagsNoLock(true);

            localExecute = null;

            if (interpreter.GetIExecuteViaResolvers(
                    engineFlags, topic1, null, LookupFlags.HelpNoVerbose,
                    ref localExecute) == ReturnCode.Ok)
            {
                name = topic1;
                execute = localExecute;

                return ReturnCode.Ok;
            }

            localExecute = null;

            if (interpreter.GetIExecuteViaResolvers(
                    engineFlags, topic2, null, LookupFlags.HelpNoVerbose,
                    ref localExecute) == ReturnCode.Ok)
            {
                name = topic2;
                execute = localExecute;

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string FormatHelpItem( /* CANNOT RETURN NULL */
            Interpreter interpreter, /* in */
            IExecute execute,        /* in */
            string name,             /* in */
            bool summary             /* in */
            )
        {
            StringBuilder builder = StringOps.NewStringBuilder();

            if (execute != null)
            {
                bool verbatim;

                string syntax = GetSyntaxForIExecute(
                    interpreter, execute, null, true, false,
                    out verbatim);

                if (!verbatim)
                {
                    string type = GetTopicTypeForIExecute(
                        execute, name, null, false);

                    if (type != null)
                    {
                        if (builder.Length > 0)
                            builder.Append(Characters.Space);

                        builder.Append(type);
                    }

                    if (name != null)
                    {
                        if (builder.Length > 0)
                            builder.Append(Characters.Space);

                        builder.Append(name);
                    }
                }

                if (syntax != null)
                {
                    if (builder.Length > 0)
                        builder.Append(Characters.Space);

                    builder.Append(syntax);
                }

                if (!summary)
                {
                    string description = GetDescriptionForIExecute(
                        execute, null);

                    if (description != null)
                    {
                        if (builder.Length > 0)
                            builder.Append(descriptionSeparator);

                        builder.Append(description);
                    }
                }
            }
            else
            {
                builder.AppendFormat(
                    "No help is available for {0}.",
                    FormatOps.WrapOrNull(name));
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string FormatHelpItem( /* CANNOT RETURN NULL */
            Interpreter interpreter,     /* in */
            StringListDictionary groups, /* in */
            StringPairDictionary help,   /* in */
            string groupType,            /* in */
            string helpType,             /* in */
            string topic,                /* in */
            string topicType,            /* in */
            bool noError,                /* in */
            bool noPrefix,               /* in */
            bool summary,                /* in */
            bool noTopic,                /* in */
            bool useInterpreter,         /* in */
            bool useSyntax,              /* in */
            out string type              /* out */
            )
        {
            if (topic == null)
            {
                type = null;

                if (noError)
                {
                    return null;
                }
                else
                {
                    return String.Format(
                        "Invalid help {0}.", topicType);
                }
            }

            string localTopic;
            string prefix;

            localTopic = MaybeAdjustHelpItemTopic(topic,
                noPrefix ? null : ShellOps.DefaultInteractiveCommandPrefix,
                out prefix);

            if (localTopic == null)
            {
                type = null;

                if (noError)
                {
                    return null;
                }
                else
                {
                    return String.Format(
                        "Invalid adjusted help {0}.", topicType);
                }
            }

            if (groups != null)
            {
                StringList helpGroup;

                if (groups.TryGetValue(localTopic, out helpGroup))
                {
                    type = groupType;

                    if (helpGroup != null)
                    {
                        if (helpGroup.Count > 0)
                        {
                            return String.Format(
                                groupAndListFormat, localTopic,
                                GenericOps<string>.ListToEnglish(
                                    helpGroup, itemSeparator,
                                    Characters.Space.ToString(),
                                    itemSuffix, prefix, null));
                        }
                        else
                        {
                            return String.Format(groupOnlyFormat, localTopic);
                        }
                    }
                    else
                    {
                        if (noError)
                        {
                            type = null;

                            return null;
                        }
                        else
                        {
                            return String.Format(
                                "Invalid help group for {0} {1}.",
                                topicType, FormatOps.WrapOrNull(
                                localTopic));
                        }
                    }
                }
            }

            if (help != null)
            {
                IPair<string> helpItem;

                if (help.TryGetValue(localTopic, out helpItem))
                {
                    type = helpType;

                    if (helpItem != null)
                    {
                        if ((helpItem.X != null) && (helpItem.Y != null))
                        {
                            if (summary)
                            {
                                return String.Format(
                                    noTopic ? descriptionOnlyFormat2 :
                                    topicWithArgumentsFormat, prefix,
                                    localTopic, helpItem.X);
                            }
                            else
                            {
                                return String.Format(
                                    noTopic ? descriptionOnlyFormat3 :
                                    topicWithArgumentsAndDescriptionFormat,
                                    prefix, localTopic, helpItem.X,
                                    helpItem.Y);
                            }
                        }
                        else if (helpItem.X != null)
                        {
                            if (summary)
                            {
                                return String.Format(
                                    noTopic ? descriptionOnlyFormat2 :
                                    topicWithArgumentsFormat, prefix,
                                    localTopic, helpItem.X);
                            }
                            else
                            {
                                return String.Format(
                                    noTopic ? descriptionOnlyFormat2 :
                                    topicAndDescriptionFormat, prefix,
                                    localTopic, helpItem.X);
                            }
                        }
                        else if (helpItem.Y != null)
                        {
                            if (summary)
                            {
                                if (noTopic)
                                {
                                    if (noError)
                                    {
                                        type = null;

                                        return null;
                                    }
                                    else
                                    {
                                        return String.Format(
                                            "No help is available for {0} {1}.",
                                            topicType, FormatOps.WrapOrNull(
                                            localTopic));
                                    }
                                }
                                else
                                {
                                    return String.Format(
                                        topicOnlyFormat, prefix,
                                        localTopic);
                                }
                            }
                            else
                            {
                                return String.Format(
                                    noTopic ? descriptionOnlyFormat2 :
                                    topicAndDescriptionFormat, prefix,
                                    localTopic, helpItem.Y);
                            }
                        }
                        else
                        {
                            if (summary)
                            {
                                if (noTopic)
                                {
                                    if (noError)
                                    {
                                        type = null;

                                        return null;
                                    }
                                    else
                                    {
                                        return String.Format(
                                            "No help is available for {0} {1}.",
                                            topicType, FormatOps.WrapOrNull(
                                            localTopic));
                                    }
                                }
                                else
                                {
                                    return String.Format(
                                        topicOnlyFormat, prefix,
                                        localTopic);
                                }
                            }
                            else
                            {
                                if (noError)
                                {
                                    type = null;

                                    return null;
                                }
                                else
                                {
                                    return String.Format(
                                        "No help is available for {0} {1}.",
                                        topicType, FormatOps.WrapOrNull(
                                        localTopic));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (noError)
                        {
                            type = null;

                            return null;
                        }
                        else
                        {
                            return String.Format(
                                "Invalid help item for {0} {1}.",
                                topicType, FormatOps.WrapOrNull(
                                localTopic));
                        }
                    }
                }
            }

            if (useInterpreter)
            {
                //
                // NOTE: This may be some kind of command that was
                //       added to the interpreter.  Attempt to use
                //       the configured resolvers to locate it.
                //
                string name = null;
                IExecute execute = null;

                if (GetIExecuteViaResolvers(
                        interpreter, topic, localTopic, ref name,
                        ref execute) == ReturnCode.Ok)
                {
                    if (execute is IProcedure)
                        type = "procedure";
                    else
                        type = "command";

                    return FormatHelpItem(
                        interpreter, execute, name, summary);
                }
            }

            if (useSyntax)
            {
                //
                // HACK: This may be a sub-command of some kind,
                //       e.g. built-in or otherwise.  Therefore,
                //       use a direct lookup into the built-in
                //       syntax cache.
                //
                // NOTE: This must use the original, unmodified
                //       topic because the command syntax lookup
                //       may need its interactive command prefix
                //       in order to find the correct command or
                //       sub-command.
                //
                string syntax;
                string localType = null;

                syntax = SyntaxOps.GetFormatted(
                    interpreter, topic, null, null, ref localType);

                if (syntax != null)
                {
                    if (localType != null)
                        type = localType;
                    else
                        type = "syntax";

                    return syntax;
                }
            }

            //
            // NOTE: If no syntax help was available or the command
            //       was not found we simply return a suitable error
            //       message.
            //
            type = null;

            if (noError)
            {
                return null;
            }
            else
            {
                return String.Format(
                    "There is no help {0} {1}.", topicType,
                    FormatOps.WrapOrNull(topic));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList GetInteractiveCommandNames(
            Interpreter interpreter, /* in */
            string pattern,          /* in */
            bool noCase              /* in */
            )
        {
            StringPairDictionary help = GetCachedInteractiveCommandHelp(
                interpreter, pattern, noCase);

            return (help != null) ? new StringList(help.Keys) : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringPair GetInteractiveCommandHelpItem(
            Interpreter interpreter, /* in */
            string name              /* in */
            )
        {
            if (name != null)
            {
                StringPairDictionary help = GetCachedInteractiveCommandHelp(
                    interpreter, null, false);

                IPair<string> result;

                if ((help != null) &&
                    help.TryGetValue(name, out result) &&
                    (result != null))
                {
                    return new StringPair(result.X, result.Y); /* COPY */
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StringListDictionary GetInteractiveCommandGroups(
            Interpreter interpreter /* in */
            ) /* CANNOT RETURN NULL */
        {
            StringListDictionary result = new StringListDictionary();

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Help Topic Group Names
            result.Add("debugger", new StringList());
            result.Add("engine", new StringList());
            result.Add("flags", new StringList());

#if HISTORY
            result.Add("history", new StringList());
#endif

            result.Add("host", new StringList());
            result.Add("info", new StringList());
            result.Add("result", new StringList());
            result.Add("shell", new StringList());
            result.Add("state", new StringList());
            result.Add("diagnostic", new StringList());
            result.Add("trace", new StringList());
            result.Add("update", new StringList());
            result.Add("view", new StringList());
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Shell/Debugger Commands
#if DEBUGGER
            result["debugger"].Add("break");
            result["debugger"].Add("deval");
            result["debugger"].Add("dsubst");
#endif

            result["debugger"].Add("go");
            result["debugger"].Add("halt");

#if DEBUGGER
            result["debugger"].Add("reset");
            result["debugger"].Add("resume");
            result["debugger"].Add("run");
            result["debugger"].Add("step");
            result["debugger"].Add("suspend");
#endif

            result["debugger"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Engine Related Commands
#if CONSOLE
            result["engine"].Add("cancel");
#endif

#if CALLBACK_QUEUE
            result["engine"].Add("clearq");
#endif

            result["engine"].Add("eval");
            result["engine"].Add("fresc");
            result["engine"].Add("fresh");
            result["engine"].Add("queue");
            result["engine"].Add("resc");
            result["engine"].Add("resh");

            result["engine"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Flag Introspection & Manipulation Commands
            result["flags"].Add("ceflags");
            result["flags"].Add("cflags");
            result["flags"].Add("dcflags");
            result["flags"].Add("dflags");
            result["flags"].Add("diflags");
            result["flags"].Add("dizflags");
            result["flags"].Add("dscflags");
            result["flags"].Add("evflags");
            result["flags"].Add("exflags");
            result["flags"].Add("hflags");
            result["flags"].Add("ieflags");
            result["flags"].Add("ievflags");
            result["flags"].Add("iexflags");
            result["flags"].Add("iflags");
            result["flags"].Add("isflags");
            result["flags"].Add("izflags");
            result["flags"].Add("ldflags");
            result["flags"].Add("leflags");
            result["flags"].Add("levflags");
            result["flags"].Add("lexflags");
            result["flags"].Add("lhflags");
            result["flags"].Add("lsflags");

#if NOTIFY || NOTIFY_OBJECT
            result["flags"].Add("nflags");
            result["flags"].Add("ntypes");
#endif

            result["flags"].Add("paflags");
            result["flags"].Add("pflags");
            result["flags"].Add("prflags");
            result["flags"].Add("scflags");
            result["flags"].Add("seflags");
            result["flags"].Add("sflags");

            result["flags"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region History Related Commands
#if HISTORY
            result["history"].Add("histclear");
            result["history"].Add("histfile");
            result["history"].Add("histload");
            result["history"].Add("histsave");

            result["history"].Sort();
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Interpreter Host Related Commands
            result["host"].Add("canexit");
            result["host"].Add("color");
            result["host"].Add("exceptions");
            result["host"].Add("hcancel");
            result["host"].Add("hexit");
            result["host"].Add("rehash");
            result["host"].Add("style");
            result["host"].Add("useattach");

            result["host"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region General Information Commands
            result["info"].Add("about");
            result["info"].Add("help");
            result["info"].Add("ihelp");
            result["info"].Add("usage");
            result["info"].Add("version");
            result["info"].Add("website");

            result["info"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Self-Update Related Commands
            result["update"].Add("check");
            result["update"].Add("stable");

            result["update"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Result Introspection & Manipulation Commands
            result["result"].Add("clearr");
            result["result"].Add("copyr");
            result["result"].Add("fresr");
            result["result"].Add("grinfo");
            result["result"].Add("lrinfo");
            result["result"].Add("mover");

#if PREVIOUS_RESULT
            result["result"].Add("nextr");
#endif

            result["result"].Add("nullr");
            result["result"].Add("overr");

#if PREVIOUS_RESULT
            result["result"].Add("prevr");
#endif

            result["result"].Add("resr");
            result["result"].Add("rinfo");
            result["result"].Add("setr");
            result["result"].Add("sresult");

            result["result"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Shell Control Commands
#if DEBUGGER
            result["shell"].Add("again");
#endif

            result["shell"].Add("cmd");
            result["shell"].Add("done");
            result["shell"].Add("exact");
            result["shell"].Add("exit");
            result["shell"].Add("nop");
            result["shell"].Add("pause");
            result["shell"].Add("paused");

#if NATIVE && TCL
            result["shell"].Add("tclinterp");
            result["shell"].Add("tclsh");
#endif

            result["shell"].Add("tclshrc");
            result["shell"].Add("unpause");

            result["shell"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Interpreter State Manipulation Commands
            result["state"].Add("chans");
            result["state"].Add("init");
            result["state"].Add("purge");
            result["state"].Add("relimit");
            result["state"].Add("restc");
            result["state"].Add("intsec");

#if NOTIFY && NOTIFY_ARGUMENTS
            result["state"].Add("restm");
#endif

            result["state"].Add("restv");
            result["state"].Add("rlimit");
            result["state"].Add("trustclr");
            result["state"].Add("trustdir");
            result["state"].Add("vout");

            result["state"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Diagnostic & Test Suite Commands
            result["diagnostic"].Add("ptest");
            result["diagnostic"].Add("test");
            result["diagnostic"].Add("testdir");
            result["diagnostic"].Add("testgc");

            result["diagnostic"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Trace Related Commands
            result["trace"].Add("tcancel");
            result["trace"].Add("tcode");
            result["trace"].Add("tnewvalue");
            result["trace"].Add("toldvalue");

            result["trace"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Library State Introspection Commands
            result["view"].Add("ainfo");
            result["view"].Add("args");
            result["view"].Add("cinfo");
            result["view"].Add("complaint");
            result["view"].Add("cuinfo");

#if DEBUGGER
            result["view"].Add("dinfo");
#endif

            result["view"].Add("dpath");
            result["view"].Add("einfo");
            result["view"].Add("eninfo");
            result["view"].Add("finfo");
            result["view"].Add("frinfo");
            result["view"].Add("hinfo");

#if HISTORY
            result["view"].Add("histinfo");
#endif

            result["view"].Add("iinfo");
            result["view"].Add("lfinfo");

#if NATIVE && TCL && NATIVE_PACKAGE
            result["view"].Add("npinfo");
#endif

            result["view"].Add("oinfo");
            result["view"].Add("show");
            result["view"].Add("sinfo");
            result["view"].Add("stack");
            result["view"].Add("testinfo");
            result["view"].Add("tinfo");
            result["view"].Add("toinfo");
            result["view"].Add("vinfo");

            result["view"].Sort();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Extension Related Commands
            AddInteractiveCommandExtensionGroup(interpreter, ref result);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void AddInteractiveCommandExtensionGroup(
            Interpreter interpreter,        /* in */
            ref StringListDictionary groups /* in, out */
            )
        {
            StringList names = null;

            GetInteractiveExtensionCommandNames(
                interpreter, ShellOps.InteractiveCommandPrefix,
                ref names);

            if ((names != null) && (names.Count > 0))
            {
                if (groups == null)
                    groups = new StringListDictionary();

                names.Sort();

                groups.Add("extension", names);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StringListDictionary GetCachedInteractiveCommandGroups(
            Interpreter interpreter, /* in */
            string pattern,          /* in */
            bool noCase              /* in */
            ) /* CANNOT RETURN NULL */
        {
            StringListDictionary result = null;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (commandGroups == null)
                    commandGroups = GetInteractiveCommandGroups(null);

                ///////////////////////////////////////////////////////////////////////////////////////

                result = new StringListDictionary(commandGroups);

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Extension Related Commands
                AddInteractiveCommandExtensionGroup(interpreter, ref result);

                ///////////////////////////////////////////////////////////////////////////////////////

                if (pattern != null) result = result.Filter(pattern, noCase);
                #endregion
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ArgumentList GetArguments(
            IProcedure procedure /* in */
            )
        {
            if (procedure == null)
                return null;

            return procedure.Arguments;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ArgumentDictionary GetNamedArguments(
            IProcedure procedure /* in */
            )
        {
            if (procedure == null)
                return null;

            return procedure.NamedArguments;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if XML
        private static string GetBody(
            IProcedure procedure /* in */
            )
        {
            if (procedure == null)
                return null;

            return procedure.Body;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetTopicTypeForIExecute(
            IExecute execute, /* in */
            string name,      /* in */
            string @default,  /* in */
            bool noType       /* in */
            )
        {
            if (execute is ICommand)
            {
                return String.Format("{0}{1}command",
                    EntityOps.IsHidden((ICommand)execute) ?
                    hiddenPrefix : String.Empty,
                    IsInteractiveExtension(execute) ?
                        interactiveExtensionPrefix :
                        String.Empty);
            }

            if (execute is IProcedure)
            {
                return String.Format("{0}{1}procedure",
                    EntityOps.IsHidden((IProcedure)execute) ?
                    hiddenPrefix : String.Empty,
                    IsInteractiveExtension(execute) ?
                        interactiveExtensionPrefix :
                        String.Empty);
            }

            if (!noType)
            {
                string result = FormatOps.RawTypeNameOrFullName(
                    execute);

                if (!String.IsNullOrEmpty(result))
                {
                    return String.Format("{0}{1}",
                        IsInteractiveExtension(execute, name) ?
                            interactiveExtensionPrefix :
                            String.Empty,
                        result);
                }
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetSyntax(
            ISyntax syntax,   /* in */
            string @default,  /* in */
            out bool verbatim /* out */
            )
        {
            if (syntax == null)
            {
                verbatim = false;
                return null;
            }

            string result = syntax.Syntax;

            if (!String.IsNullOrEmpty(result))
            {
                verbatim = false;
                return result;
            }

            verbatim = false;
            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetSyntaxForIEnsemble(
            IEnsemble ensemble, /* in */
            string @default,    /* in */
            bool noName,        /* in */
            out bool verbatim   /* out */
            )
        {
            if (ensemble == null)
            {
                verbatim = false;
                return @default;
            }

            EnsembleDictionary subCommands = ensemble.SubCommands;

            if ((subCommands == null) || (subCommands.Count == 0))
            {
                verbatim = false;
                return null;
            }

            IIdentifierName identifierName = noName ?
                null : ensemble as IIdentifierName;

            verbatim = false;

            return GetSyntaxForIEnsemble(
                (identifierName != null) ? identifierName.Name : null,
                new StringList(subCommands.Keys));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetSyntaxForIEnsemble(
            string name,               /* in: OPTIONAL */
            StringList subCommandNames /* in */
            )
        {
            if ((subCommandNames == null) || (subCommandNames.Count == 0))
                return null;

            return String.Format(
                "{0} sub-commands: {1}", name, subCommandNames).Trim();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetSyntaxForCommand(
            Interpreter interpreter, /* in */
            ICommand command,        /* in */
            string @default,         /* in */
            bool noName              /* in */
            )
        {
            bool verbatim;

            return GetSyntaxForCommand(
                interpreter, command, @default, noName, out verbatim);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetSyntaxForCommand(
            Interpreter interpreter, /* in */
            ICommand command,        /* in */
            string @default,         /* in */
            bool noName,             /* in */
            out bool verbatim        /* out */
            )
        {
            string result = GetSyntax(
                command, null, out verbatim);

            if (!String.IsNullOrEmpty(result))
                return result;

            string extra = GetSyntaxForIEnsemble(
                command, null, noName, out verbatim);

            if (!String.IsNullOrEmpty(extra))
            {
                result = SyntaxOps.GetFormatted(
                    interpreter, command, extra, null);

                if (!String.IsNullOrEmpty(result))
                {
                    verbatim = true;
                    return result;
                }

                return extra;
            }
            else
            {
                result = SyntaxOps.GetFormatted(
                    interpreter, command, null, null);

                if (!String.IsNullOrEmpty(result))
                {
                    verbatim = true;
                    return result;
                }
            }

            verbatim = false;
            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetSyntaxForProcedure(
            IProcedure procedure, /* in */
            string @default       /* in */
            )
        {
            bool verbatim;

            return GetSyntaxForProcedure(
                procedure, @default, out verbatim);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetSyntaxForProcedure(
            IProcedure procedure, /* in */
            string @default,      /* in */
            out bool verbatim     /* out */
            )
        {
            if (EntityOps.IsPositionalArguments(procedure))
            {
                ArgumentList arguments = GetArguments(procedure);

                if ((arguments != null) && (arguments.Count > 0))
                {
                    string result = arguments.ToRawString(
                        ToStringFlags.Decorated, Characters.Space.ToString());

                    if (!String.IsNullOrEmpty(result))
                    {
                        verbatim = false;
                        return result;
                    }
                }
            }

            if (EntityOps.IsNamedArguments(procedure))
            {
                ArgumentDictionary arguments = GetNamedArguments(procedure);

                if ((arguments != null) && (arguments.Count > 0))
                {
                    string result = arguments.ToRawString(
                        ToStringFlags.Decorated, Characters.Space.ToString());

                    if (!String.IsNullOrEmpty(result))
                    {
                        verbatim = false;
                        return result;
                    }
                }
            }

            ISyntax syntax = procedure as ISyntax;

            if (syntax != null)
                return GetSyntax(syntax, @default, out verbatim);

            verbatim = false;
            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetSyntaxForIExecute(
            Interpreter interpreter, /* in */
            IExecute execute,        /* in */
            string @default,         /* in */
            bool noName,             /* in */
            bool noType              /* in */
            )
        {
            bool verbatim;

            return GetSyntaxForIExecute(
                interpreter, execute, @default, noName, noType,
                out verbatim);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetSyntaxForIExecute(
            Interpreter interpreter, /* in */
            IExecute execute,        /* in */
            string @default,         /* in */
            bool noName,             /* in */
            bool noType,             /* in */
            out bool verbatim        /* out */
            )
        {
            string result = GetSyntaxForCommand(
                interpreter, execute as ICommand, null, noName,
                out verbatim);

            if (!String.IsNullOrEmpty(result))
                return result;

            result = GetSyntaxForProcedure(
                execute as IProcedure, null, out verbatim);

            if (!String.IsNullOrEmpty(result))
                return result;

            result = GetSyntax(
                execute as ISyntax, null, out verbatim);

            if (!String.IsNullOrEmpty(result))
                return result;

            result = GetSyntaxForIEnsemble(
                execute as IEnsemble, null, noName, out verbatim);

            if (!String.IsNullOrEmpty(result))
                return result;

            if (!noType)
            {
                result = FormatOps.RawTypeNameOrFullName(execute);

                if (!String.IsNullOrEmpty(result))
                {
                    verbatim = false;
                    return result;
                }
            }

            verbatim = false;
            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetDescription(
            IIdentifier identifier, /* in */
            string @default         /* in */
            )
        {
            if (identifier == null)
                return @default;

            string result = identifier.Description;

            if (!String.IsNullOrEmpty(result))
                return result;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetDescriptionForCommand(
            ICommand command, /* in */
            string @default   /* in */
            )
        {
            string result = GetDescription(command, null);

            if (!String.IsNullOrEmpty(result))
                return result;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetDescriptionForProcedure(
            IProcedure procedure, /* in */
            string @default       /* in */
            )
        {
            string result = GetDescription(procedure, null);

            if (!String.IsNullOrEmpty(result))
                return result;

#if XML
            result = ExtractHelpFromScript(GetBody(procedure));

            if (!String.IsNullOrEmpty(result))
                return result;
#endif

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetDescriptionForIExecute(
            IExecute execute, /* in */
            string @default   /* in */
            )
        {
            ICommand command = execute as ICommand;

            if (command != null)
                return GetDescriptionForCommand(command, @default);

            IProcedure procedure = execute as IProcedure;

            if (procedure != null)
                return GetDescriptionForProcedure(procedure, @default);

            IIdentifier identifier = execute as IIdentifier;

            if (identifier != null)
                return GetDescription(identifier, @default);

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void GetInteractiveExtensionCommandNames(
            Interpreter interpreter, /* in */
            string prefix,           /* in */
            ref StringList names     /* in, out */
            )
        {
            StringPairDictionary help = null;

            GetInteractiveExtensionCommandNamesOrHelp(interpreter,
                prefix, true, false, ref names, ref help);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void GetInteractiveExtensionCommandHelp(
            Interpreter interpreter,      /* in */
            string prefix,                /* in */
            ref StringPairDictionary help /* in, out */
            )
        {
            StringList names = null;

            GetInteractiveExtensionCommandNamesOrHelp(interpreter,
                prefix, false, true, ref names, ref help);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void GetInteractiveExtensionCommandNamesOrHelp(
            Interpreter interpreter,      /* in */
            string prefix,                /* in */
            bool getNames,                /* in */
            bool getHelp,                 /* in */
            ref StringList names,         /* in, out */
            ref StringPairDictionary help /* in, out */
            )
        {
            if (interpreter == null)
                return;

            string pattern = null;

            if (prefix != null)
                pattern = prefix + Characters.Asterisk;

            ReturnCode[] localCodes = {
                ReturnCode.Ok, /* command list result */
                ReturnCode.Ok, /* procedure list result */
                ReturnCode.Ok, /* execute list result */
                ReturnCode.Ok, /* hidden command list result */
                ReturnCode.Ok, /* hidden procedure list result */
                ReturnCode.Ok  /* hidden execute list result */
            };

            StringList[] localNames = {
                null, /* matching command names */
                null, /* matching procedure names */
                null, /* matching execute names */
                null, /* matching hidden command names */
                null, /* matching hidden procedure names */
                null  /* matching hidden execute names */
            };

            localCodes[0] = interpreter.ListCommands(
                CommandFlags.None, CommandFlags.None, false, false,
                pattern, false, false, ref localNames[0]);

            localCodes[1] = interpreter.ListProcedures(
                ProcedureFlags.None, ProcedureFlags.None, false, false,
                pattern, false, false, ref localNames[1]);

            localCodes[2] = interpreter.ListIExecutes(
                pattern, false, false, ref localNames[2]);

            localCodes[3] = interpreter.ListHiddenCommands(
                CommandFlags.None, CommandFlags.None, false, false,
                pattern, false, false, ref localNames[3]);

            localCodes[4] = interpreter.ListHiddenProcedures(
                ProcedureFlags.None, ProcedureFlags.None, false, false,
                pattern, false, false, ref localNames[4]);

            localCodes[5] = interpreter.ListHiddenIExecutes(
                pattern, false, false, ref localNames[5]);

            if (((localCodes[0] == ReturnCode.Ok) &&
                    (localNames[0] != null) && (localNames[0].Count > 0)) ||
                ((localCodes[1] == ReturnCode.Ok) &&
                    (localNames[1] != null) && (localNames[1].Count > 0)) ||
                ((localCodes[2] == ReturnCode.Ok) &&
                    (localNames[2] != null) && (localNames[2].Count > 0)) ||
                ((localCodes[3] == ReturnCode.Ok) &&
                    (localNames[3] != null) && (localNames[3].Count > 0)) ||
                ((localCodes[4] == ReturnCode.Ok) &&
                    (localNames[4] != null) && (localNames[4].Count > 0)) ||
                ((localCodes[5] == ReturnCode.Ok) &&
                    (localNames[5] != null) && (localNames[5].Count > 0)))
            {
                if (getNames && (names == null))
                    names = new StringList();

                if (getHelp && (help == null))
                    help = new StringPairDictionary();

                ///////////////////////////////////////////////////////////////////////////////////////

                if (localNames[0] != null)
                {
                    foreach (string name in localNames[0])
                    {
                        if (name == null)
                            continue;

                        string newName = (prefix != null) ?
                            name.Substring(prefix.Length) : name;

                        if (getNames)
                            names.Add(newName);

                        if (getHelp)
                        {
                            ICommand command = null;

                            if (interpreter.GetCommand(
                                    name, LookupFlags.HelpNoVerbose,
                                    ref command) != ReturnCode.Ok)
                            {
                                continue;
                            }

                            help[newName] = new StringPair(
                                GetSyntaxForCommand(interpreter, command,
                                    null, false), GetDescriptionForCommand(
                                        command, "Interactive extension command."));
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (localNames[1] != null)
                {
                    foreach (string name in localNames[1])
                    {
                        if (name == null)
                            continue;

                        string newName = (prefix != null) ?
                            name.Substring(prefix.Length) : name;

                        if (getNames)
                            names.Add(newName);

                        if (getHelp)
                        {
                            IProcedure procedure = null;

                            if (interpreter.GetProcedure(
                                    name, LookupFlags.HelpNoVerbose,
                                    ref procedure) != ReturnCode.Ok)
                            {
                                continue;
                            }

                            help[newName] = new StringPair(
                                GetSyntaxForProcedure(procedure, null),
                                    GetDescriptionForProcedure(procedure,
                                        "Interactive extension procedure."));
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (localNames[2] != null)
                {
                    foreach (string name in localNames[2])
                    {
                        if (name == null)
                            continue;

                        string newName = (prefix != null) ?
                            name.Substring(prefix.Length) : name;

                        if (getNames)
                            names.Add(newName);

                        if (getHelp)
                        {
                            IExecute execute = null;

                            if (interpreter.GetIExecute(
                                    name, LookupFlags.HelpNoVerbose,
                                    ref execute) != ReturnCode.Ok)
                            {
                                continue;
                            }

                            help[newName] = new StringPair(
                                GetSyntaxForIExecute(interpreter, execute,
                                    null, false, false), GetDescriptionForIExecute(
                                        execute, "Interactive extension."));
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (localNames[3] != null)
                {
                    foreach (string name in localNames[3])
                    {
                        if (name == null)
                            continue;

                        string newName = (prefix != null) ?
                            name.Substring(prefix.Length) : name;

                        if (getNames)
                            names.Add(newName);

                        if (getHelp)
                        {
                            ICommand command = null;

                            if (interpreter.GetHiddenCommand(
                                    name, LookupFlags.HelpNoVerbose,
                                    ref command) != ReturnCode.Ok)
                            {
                                continue;
                            }

                            help[newName] = new StringPair(
                                GetSyntaxForCommand(interpreter, command,
                                    null, false), GetDescriptionForCommand(
                                        command, "Interactive extension command (hidden)."));
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (localNames[4] != null)
                {
                    foreach (string name in localNames[4])
                    {
                        if (name == null)
                            continue;

                        string newName = (prefix != null) ?
                            name.Substring(prefix.Length) : name;

                        if (getNames)
                            names.Add(newName);

                        if (getHelp)
                        {
                            IProcedure procedure = null;

                            if (interpreter.GetHiddenProcedure(
                                    name, LookupFlags.HelpNoVerbose,
                                    ref procedure) != ReturnCode.Ok)
                            {
                                continue;
                            }

                            help[newName] = new StringPair(
                                GetSyntaxForProcedure(procedure, null),
                                    GetDescriptionForProcedure(procedure,
                                        "Interactive extension procedure (hidden)."));
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (localNames[5] != null)
                {
                    foreach (string name in localNames[5])
                    {
                        if (name == null)
                            continue;

                        string newName = (prefix != null) ?
                            name.Substring(prefix.Length) : name;

                        if (getNames)
                            names.Add(newName);

                        if (getHelp)
                        {
                            IExecute execute = null;

                            if (interpreter.GetHiddenIExecute(
                                    name, LookupFlags.HelpNoVerbose,
                                    ref execute) != ReturnCode.Ok)
                            {
                                continue;
                            }

                            help[newName] = new StringPair(
                                GetSyntaxForIExecute(interpreter, execute,
                                    null, false, false), GetDescriptionForIExecute(
                                        execute, "Interactive extension (hidden)."));
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (getNames)
                    names.Sort();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StringPairDictionary GetInteractiveCommandHelp() /* CANNOT RETURN NULL */
        {
            StringPairDictionary result = new StringPairDictionary();

            ///////////////////////////////////////////////////////////////////////////////////////////

            string flagHelp = String.Format(
                "Where \"flags\" is a list of zero or more values, separated by " +
                "spaces or commas, which may be prefixed by {0} (add), {1} " +
                "(remove), {2} (set), {3} (set, then add), or {4} (keep).  " +
                "If no prefix is specified, {5} (set, then add) is assumed.  " +
                "Available flags are: {6}.",
                FormatOps.WrapOrNull(EnumOps.AddFlagOperator),
                FormatOps.WrapOrNull(EnumOps.RemoveFlagOperator),
                FormatOps.WrapOrNull(EnumOps.SetFlagOperator),
                FormatOps.WrapOrNull(EnumOps.SetAddFlagOperator),
                FormatOps.WrapOrNull(EnumOps.KeepFlagOperator),
                FormatOps.WrapOrNull(EnumOps.DefaultFlagOperator), "{0}");

            string createFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(CreateFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string scriptFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(ScriptFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string engineFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(EngineFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string interpreterFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(InterpreterFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string initializeFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(InitializeFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string substitutionFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(SubstitutionFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string eventFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(EventFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string expressionFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(ExpressionFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string headerFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(HeaderFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string detailFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(DetailFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string packageFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(PackageFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string pluginFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(PluginFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string procedureFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(ProcedureFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

#if NOTIFY || NOTIFY_OBJECT
            string notifyTypesHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(NotifyType))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));

            string notifyFlagsHelpItem = String.Format(flagHelp,
                GenericOps<string>.DictionaryToEnglish(
                    new StringSortedList(Enum.GetNames(typeof(NotifyFlags))),
                    itemSeparator, Characters.Space.ToString(), itemSuffix));
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

            result.Add("about",
                new StringPair("?what?",
                    "Displays the version summary and licensing information."));

#if DEBUGGER
            result.Add("again",
                new StringPair(null,
                    "Processes the previous interactive input again."));
#endif

            result.Add("ainfo",
                new StringPair(null,
                    "Displays the argument information for this interactive debuggging session."));

            result.Add("args",
                new StringPair(null,
                    "Displays the command line arguments as they were passed to this interactive debuggging session."));

#if DEBUGGER
            result.Add("break",
                new StringPair(null,
                    "Enters a new interactive debugging session."));
#endif

#if CONSOLE
            result.Add("cancel",
                new StringPair(null,
                    "Initiates script cancellation for all registered interpreters."));
#endif

            result.Add("canexit",
                new StringPair(null,
                    "Toggles whether the interpreter host should allow scripts to exit."));

            result.Add("ceflags",
                new StringPair("?flags?",
                    "Displays or sets the context engine flags for the interactive interpreter.  " + engineFlagsHelpItem));

            result.Add("cflags",
                new StringPair("?flags?",
                    "Displays or sets the creation flags for the interactive interpreter.  " + createFlagsHelpItem));

            result.Add("chans",
                new StringPair("?replace?", String.Format(
                    "Adds or replaces the standard channels {0}, {1}, and {2} for the interactive interpreter.",
                    FormatOps.WrapOrNull(StandardChannel.Input), FormatOps.WrapOrNull(StandardChannel.Output),
                    FormatOps.WrapOrNull(StandardChannel.Error))));

            result.Add("check",
                new StringPair("?wantScripts? ?quiet? ?prompt? ?automatic? ?actionType? ?releaseType? ?updateType?", String.Format(
                    "Checks the official updates site ({0}) to see if this is the latest available build of the script engine.  " +
                    "Optionally, it can either fetch the latest available release package of the specified type (e.g. " +
                    "\"source\", \"setup\", or \"binary\") from an official download site ({1}) or launch an external updater " +
                    "and then exit.  Alternatively, checks for any update scripts applicable to the currently running build of the script engine.",
                    FormatOps.WrapOrNull(GlobalState.GetAssemblyUpdateBaseUri()), FormatOps.WrapOrNull(GlobalState.GetAssemblyDownloadBaseUri()))));

            result.Add("cinfo",
                new StringPair(null,
                    "Displays the control information for the interactive interpreter, such as whether the " +
                    "script in progress is being canceled."));

#if CALLBACK_QUEUE
            result.Add("clearq",
                new StringPair(null,
                    "Clears the callback queue for the interactive interpreter."));
#endif

            /* LOCAL RESULT */
            result.Add("clearr",
                new StringPair(null,
                    "Clears the local result.  The global result is untouched."));

            result.Add("cmd",
                new StringPair("?arg ...?",
                    "Executes the \"Command Processor\" (i.e. shell) configured for the operating system " +
                    "with the specified arguments."));

            result.Add("color",
                new StringPair(null,
                    "Toggles whether color is used by the interpreter host."));

            result.Add("complaint",
                new StringPair(null,
                    "Displays the previously stored complaint for the interactive interpreter."));

            /* LOCAL/GLOBAL RESULT */
            result.Add("copyr",
                new StringPair(null,
                    "Copies the global result into the local result.  The global result is untouched."));

            result.Add("cuinfo",
                new StringPair(null,
                    "Displays the custom information for the interpreter host, if any."));

            result.Add("dcflags",
                new StringPair("?flags?",
                    "Displays or sets the default creation flags for the interactive interpreter.  " + createFlagsHelpItem));

            result.Add("dflags",
                new StringPair("?flags?",
                    "Displays or sets the detail display flags for the interactive interpreter.  " + detailFlagsHelpItem));

            result.Add("diflags",
                new StringPair("?flags?",
                    "Displays or sets the default instance flags for the interactive interpreter.  " + interpreterFlagsHelpItem));

#if DEBUGGER
            result.Add("deval",
                new StringPair("arg ?arg ...?",
                    "Evaluates the specified arguments using the isolated debugger interpreter."));

            result.Add("dinfo",
                new StringPair(null,
                    "Displays the debugger information for this interactive debuggging session."));
#endif

            result.Add("dizflags",
                new StringPair("?flags?",
                    "Displays or sets the default initialization flags for the interactive interpreter.  " + initializeFlagsHelpItem));

            result.Add("done",
                new StringPair("?code? ?result?",
                    "Unconditionally exits this interactive debuggging session."));

            result.Add("dpath",
                new StringPair("?flags?",
                    "Displays the path information for the interactive interpreter."));

            result.Add("dscflags",
                new StringPair("?flags?",
                    "Displays or sets the default script flags for the interactive interpreter.  " + scriptFlagsHelpItem));

#if DEBUGGER
            result.Add("dsubst",
                new StringPair("?-nobackslashes? ?-nocommands? ?-novariables? string",
                    "Substitutes the specified arguments using the isolated debugger interpreter."));
#endif

            result.Add("einfo",
                new StringPair(null,
                    "Displays the engine status information for the interactive interpreter."));

            result.Add("eninfo",
                new StringPair(null,
                    "Displays the entity summary information for the interactive interpreter."));

            result.Add("eval",
                new StringPair("arg ?arg ...?",
                    "Evaluates the specified arguments using the interactive interpreter."));

            result.Add("evflags",
                new StringPair("?flags?",
                    "Displays or sets the event flags for the interactive interpreter.  " + eventFlagsHelpItem));

            result.Add("exact",
                new StringPair(null,
                    "Toggles exact name matching for interactive extension commands."));

            result.Add("exceptions",
                new StringPair(null,
                    "Toggles how exceptional return codes are formatted by the interpreter host."));

            result.Add("exflags",
                new StringPair("?flags?",
                    "Displays or sets the expression flags for the interactive interpreter.  " + expressionFlagsHelpItem));

            result.Add("exit",
                new StringPair(null,
                    "Exits the interactive debuggging session immediately."));

            result.Add("finfo",
                new StringPair(null,
                    "Displays the flags for the interactive interpreter and the flags that were passed to this " +
                    "interactive debuggging session."));

            result.Add("fresc",
                new StringPair(null,
                    "Toggles whether script cancellation flags are forcibly reset before interactive input."));

            result.Add("fresh",
                new StringPair(null,
                    "Toggles whether the halt flag is forcibly reset before interactive input."));

            /* LOCAL/GLOBAL RESULT */
            result.Add("fresr",
                new StringPair(null,
                    "Resets the global and local results in-place."));

            result.Add("frinfo",
                new StringPair("level ?flags?",
                    "Displays detailed information about the specified call frame."));

            result.Add("go",
                new StringPair(null,
                    "When actively debugging, continues execution of the script in progress."));

            /* GLOBAL RESULT */
            result.Add("grinfo",
                new StringPair("?previous?",
                    "Displays detailed information about the global result, leaving it untouched.  " +
                    "The local result is untouched."));

            result.Add("halt",
                new StringPair(null,
                    "When actively debugging, halts execution of the script in progress and breaks out of all nested " +
                    "interactive debugging sessions immediately."));

            result.Add("hcancel",
                new StringPair(null,
                    "Queues an asynchronous event that causes the interpreter host to cancel pending evaluations in all interpreters."));

            result.Add("help",
                new StringPair("?topic? ?showGroups? ?showTopics? ?useInterpreter? ?useSyntax? ?showHeader? ?matchingOnly?",
                    "Displays information on the specified command, sub-command, help topic, or a list of available help topics."));

            result.Add("hexit",
                new StringPair(null,
                    "Queues an asynchronous event that causes the interpreter host to exit (which may or may not terminate the entire application)."));

            result.Add("hflags",
                new StringPair("?flags?",
                    "Displays or sets the header display flags for the interactive interpreter.  " + headerFlagsHelpItem));

            result.Add("hinfo",
                new StringPair("?flags?",
                    "Displays detailed information about the interpreter host for the interactive interpreter."));

#if HISTORY
            result.Add("histclear",
                new StringPair(null,
                    "Clears the command history for the interactive interpreter."));

            result.Add("histfile",
                new StringPair("?fileName?",
                    "Displays or sets the default file name to use when loading and saving the command history for the interactive interpreter."));

            result.Add("histinfo",
                new StringPair(null,
                    "Displays the command history for the interactive interpreter."));

            result.Add("histload",
                new StringPair("fileName",
                    "Loads the command history for the interactive interpreter."));

            result.Add("histsave",
                new StringPair("fileName",
                    "Saves the command history for the interactive interpreter."));
#endif

            result.Add("ieflags",
                new StringPair("?flags?",
                    "Displays or sets the interactive command engine flags for the interactive interpreter.  " + engineFlagsHelpItem));

            result.Add("ievflags",
                new StringPair("?flags?",
                    "Displays or sets the interactive command event flags for the interactive interpreter.  " + eventFlagsHelpItem));

            result.Add("iexflags",
                new StringPair("?flags?",
                    "Displays or sets the interactive command expression flags for the interactive interpreter.  " + expressionFlagsHelpItem));

            result.Add("iflags",
                new StringPair("?flags?",
                    "Displays or sets the instance flags for the interactive interpreter.  " + interpreterFlagsHelpItem));

            result.Add("ihelp",
                new StringPair("?topic?",
                    "Displays information on the specified command, sub-command, procedure, et al."));

            result.Add("iinfo",
                new StringPair(null,
                    "Displays information about the interactive interpreter."));

            result.Add("init",
                new StringPair("?shell? ?force?",
                    "Initializes the core or shell script library, optionally forcing it to reinitialize."));

            result.Add("intsec",
                new StringPair("?enabled? ?force?",
                    "Enables or disables script signing policies and core script certificates for the interactive interpreter."));

            result.Add("isflags",
                new StringPair("?flags?",
                    "Displays or sets the interactive command substitution flags for the interactive interpreter.  " + substitutionFlagsHelpItem));

            result.Add("izflags",
                new StringPair("?flags?",
                    "Displays or sets the initialization flags for the interactive interpreter.  " + initializeFlagsHelpItem));

            result.Add("ldflags",
                new StringPair("?flags?",
                    "Displays or sets the local detail display flags for the interactive interpreter.  " + detailFlagsHelpItem));

            result.Add("leflags",
                new StringPair("?flags?",
                    "Displays or sets the local engine flags for the interactive interpreter.  " + engineFlagsHelpItem));

            result.Add("levflags",
                new StringPair("?flags?",
                    "Displays or sets the local event flags for the interactive interpreter.  " + eventFlagsHelpItem));

            result.Add("lexflags",
                new StringPair("?flags?",
                    "Displays or sets the local expression flags for the interactive interpreter.  " + expressionFlagsHelpItem));

            result.Add("lfinfo",
                new StringPair(null,
                    "Displays the local flags and the flags for the interactive interpreter."));

            result.Add("lhflags",
                new StringPair("?flags?",
                    "Displays or sets the local header display flags for the interactive interpreter.  " + headerFlagsHelpItem));

            /* LOCAL RESULT */
            result.Add("lrinfo",
                new StringPair(null,
                    "Displays detailed information about the local result, leaving it untouched.  " +
                    "The global result is untouched."));

            result.Add("lsflags",
                new StringPair("?flags?",
                    "Displays or sets the local substitution flags for the interactive interpreter.  " + substitutionFlagsHelpItem));

            /* LOCAL/GLOBAL RESULT */
            result.Add("mover",
                new StringPair(null,
                    "Moves the local result into the global result.  The local result is reset."));

#if PREVIOUS_RESULT
            /* LOCAL/PREVIOUS RESULT */
            result.Add("nextr",
                new StringPair(null,
                    "Sets the previous result for the interactive interpreter to the local result.  The global result is untouched."));
#endif

#if NOTIFY || NOTIFY_OBJECT
            result.Add("nflags",
                new StringPair("?flags?",
                    "Displays or sets the plugin notification flags for the interactive interpreter.  " + notifyFlagsHelpItem));
#endif

            result.Add("nop",
                new StringPair(null,
                    "Does nothing.  The local and global results are untouched."));

#if NATIVE && TCL && NATIVE_PACKAGE
            result.Add("npinfo",
                new StringPair(null,
                    "Displays detailed information about the native package."));
#endif

#if NOTIFY || NOTIFY_OBJECT
            result.Add("ntypes",
                new StringPair("?flags?",
                    "Displays or sets the plugin notification types for the interactive interpreter.  " + notifyTypesHelpItem));
#endif

            /* LOCAL RESULT */
            result.Add("nullr",
                new StringPair(null,
                    "Sets the local result to null.  The global result is untouched."));

            result.Add("oinfo",
                new StringPair("name",
                    "Displays detailed information about the specified object."));

            /* LOCAL RESULT */
            result.Add("overr",
                new StringPair("?options?",
                    "Sets the local result to the specified value.  The global result is untouched."));

            result.Add("paflags",
                new StringPair("?flags?",
                    "Displays or sets the package creation flags for the interactive interpreter.  " + packageFlagsHelpItem));

            result.Add("pause",
                new StringPair("?threadId? ?appDomainId? ?microseconds?",
                    "Pauses the specified interactive debuggging session."));

            result.Add("paused",
                new StringPair(null,
                    "Returns the list of paused interactive debuggging sessions."));

            result.Add("pflags",
                new StringPair("?flags?",
                    "Displays or sets the plugin loader flags for the interactive interpreter.  " + pluginFlagsHelpItem));

#if PREVIOUS_RESULT
            /* LOCAL/PREVIOUS RESULT */
            result.Add("prevr",
                new StringPair(null,
                    "Sets the local result to the previous result for the interactive interpreter.  The global result is untouched."));
#endif

            result.Add("prflags",
                new StringPair("?flags?",
                    "Displays or sets the procedure creation flags for the interactive interpreter.  " + procedureFlagsHelpItem));

            result.Add("ptest",
                new StringPair("?pattern? ?all? ?extraPath?",
                    "Runs one or more plugin tests for the interactive interpreter."));

            result.Add("purge",
                new StringPair(null,
                    "Purges undefined variables in the current call frame for the interactive interpreter."));

            result.Add("queue",
                new StringPair(null,
                    "Enters queued input mode.  The next command or script entered will be queued for asynchronous evaluation."));

            result.Add("rehash",
                new StringPair("?profile? ?encoding?",
                    "Reloads the user-specific interpreter host profile."));

            result.Add("relimit",
                new StringPair("?limit?",
                    "Displays or sets the readiness limit for the interactive interpreter."));

            result.Add("resc",
                new StringPair("?global?",
                    "Resets the cancel and unwind flag(s) for the interactive interpreter."));

#if DEBUGGER
            result.Add("reset",
                new StringPair(null,
                    "Resets the state of the debugger to its initial default, enabled and inactive."));
#endif

            result.Add("resh",
                new StringPair("?global?",
                    "Resets the halt flag(s) for the interactive interpreter."));

            /* LOCAL/GLOBAL RESULT */
            result.Add("resr",
                new StringPair(null,
                    "Resets the global and local results."));

            result.Add("restc",
                new StringPair("?strict?",
                    "Restores the core plugin for the interactive interpreter."));

#if NOTIFY && NOTIFY_ARGUMENTS
            result.Add("restm",
                new StringPair("?strict?",
                    "Restores the monitor plugin for the interactive interpreter."));
#endif

            result.Add("restv",
                new StringPair(null,
                    "Restores the core variables for the interactive interpreter."));

#if DEBUGGER
            result.Add("resume",
                new StringPair(null,
                    "When actively debugging, restores the debugger to its previous state."));
#endif

            /* LOCAL/GLOBAL RESULT */
            result.Add("rinfo",
                new StringPair(null,
                    "Displays the global and local results, leaving them untouched."));

            result.Add("rlimit",
                new StringPair("?limit?",
                    "Displays or sets the recursion limit for the interactive interpreter."));

#if DEBUGGER
            result.Add("run",
                new StringPair(null,
                    "When actively debugging, disables single-stepping and continues execution of the script in progress."));
#endif

            result.Add("scflags",
                new StringPair("?flags?",
                    "Displays or sets the script flags for the interactive interpreter.  " + scriptFlagsHelpItem));

            result.Add("seflags",
                new StringPair("?flags?",
                    "Displays or sets the shared engine flags for the interactive interpreter.  " + engineFlagsHelpItem));

            /* LOCAL/GLOBAL RESULT */
            result.Add("setr",
                new StringPair(null,
                    "Copies the local result into the global result.  The local result is untouched."));

            result.Add("sflags",
                new StringPair("?flags?",
                    "Displays or sets the substitution flags for the interactive interpreter.  " + substitutionFlagsHelpItem));

            /* LOCAL/GLOBAL RESULT */
            result.Add("show",
                new StringPair("?local? ?empty?",
                    "Displays the information that corresponds to the local header display flags."));

            result.Add("sinfo",
                new StringPair("?refresh?",
                    "Displays the native stack information for the interactive interpreter."));

            /* LOCAL/GLOBAL RESULT */
            result.Add("sresult",
                new StringPair("?varName? ?global?",
                    "Stores the local or global result into the specified variable.  The local and global results are untouched."));

            result.Add("stable",
                new StringPair("?stable?", String.Format(
                    "Displays or sets the \"stability\" level used when checking for the latest build of the script engine from " +
                    "the distribution site via the \"{0}check\" interactive command.", ShellOps.InteractiveCommandPrefix)));

            result.Add("stack",
                new StringPair("?limit? ?flags? ?info?",
                    "Displays the call stack for the interactive interpreter."));

#if DEBUGGER
            result.Add("step",
                new StringPair(null,
                    "Toggles whether single-stepping through scripts is enabled."));
#endif

            result.Add("style",
                new StringPair("?style?",
                    "Displays or sets the interpreter host output style for the debugger."));

#if DEBUGGER
            result.Add("suspend",
                new StringPair(null,
                    "When actively debugging, saves the debugger state and then temporarily suspends debugging."));
#endif

            result.Add("tcancel",
                new StringPair("?cancel?",
                    "Displays or sets the cancel flag within the variable trace information for this interactive debuggging session."));

#if NATIVE && TCL
            result.Add("tclinterp",
                new StringPair("?interp?",
                    "Displays or sets the the selected native Tcl interpreter for this interactive debuggging session, if available."));

            result.Add("tclsh",
                new StringPair(null,
                    "Toggles whether all scripts entered interactively are evaluated using the selected native Tcl interpreter, if available."));
#endif

            result.Add("tclshrc",
                new StringPair("?arg ...?", String.Format(
                    "Executes the \"Text Editor\" configured for the operating system to edit the {0} script file with the specified arguments.",
                    FormatOps.WrapOrNull(TclVars.Core.RunCommandsFileName))));

            result.Add("tcode",
                new StringPair("?code?",
                    "Displays or sets the return code within the variable trace information for this interactive debuggging session."));

            result.Add("test",
                new StringPair("?pattern? ?all? ?extraPath?",
                    "Runs one or more tests using the interactive interpreter."));

            result.Add("testdir",
                new StringPair("directory",
                    "Displays or sets the directory used when searching for test files matching a specific pattern."));

            result.Add("testgc",
                new StringPair("start", String.Format(
                     "Starts or stops the thread that collects garbage every {0} milliseconds.",
                     Interpreter.testGcSleepTime)));

            result.Add("testinfo",
                new StringPair(null,
                    "Displays the test suite information for the interactive interpreter."));

            result.Add("tinfo",
                new StringPair("?flags?",
                    "Displays the variable trace information for this interactive debuggging session or from the per-thread cache."));

            result.Add("toinfo",
                new StringPair(null,
                    "Displays the token information for this interactive debuggging session."));

            result.Add("trustclr",
                new StringPair(null,
                    "Clears the list of trusted directories for the interactive interpreter."));

            result.Add("trustdir",
                new StringPair("directory",
                    "Displays or adds to the list of trusted directories for the interactive interpreter."));

            result.Add("toldvalue",
                new StringPair("?value?",
                    "Displays or sets the old value within the variable trace information for this interactive debuggging session."));

            result.Add("tnewvalue",
                new StringPair("?value?",
                    "Displays or sets the new value within the variable trace information for this interactive debuggging session."));

            result.Add("unpause",
                new StringPair("?threadId? ?appDomainId?",
                    "Unpauses the specified interactive debuggging session."));

            result.Add("usage",
                new StringPair("?banner? ?legalese? ?options? ?environment? ?compactMode?",
                    "Displays the complete command line syntax for the default shell and describes all the environment variables that may be used with it."));

            result.Add("useattach",
                new StringPair(null,
                    "Toggles whether an attempt will be made by the interpreter host to use an existing interface, if any."));

            result.Add("version",
                new StringPair("?banner? ?legalese? ?source? ?update? ?context? ?plugins? ?certificate? ?options? ?compactMode?",
                    "Displays detailed version information."));

            result.Add("vinfo",
                new StringPair("name ?flags?",
                    "Displays detailed information about the specified variable."));

            result.Add("vout",
                new StringPair("?channel? ?enabled?",
                    "Enables, disables, or displays queued virtual output for a channel."));

            result.Add("website",
                new StringPair(null, String.Format(
                    "Executes the \"Web Browser\" configured for the operating system to view the official web site for this library ({0}).",
                    FormatOps.WrapOrNull(SharedAttributeOps.GetAssemblyUri(GlobalState.GetAssembly())))));

            ///////////////////////////////////////////////////////////////////////////////////////////

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StringPairDictionary GetCachedInteractiveCommandHelp(
            Interpreter interpreter, /* in */
            string pattern,          /* in */
            bool noCase              /* in */
            ) /* CANNOT RETURN NULL */
        {
            StringPairDictionary result = null;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (commandHelp == null)
                    commandHelp = GetInteractiveCommandHelp();

                ///////////////////////////////////////////////////////////////////////////////////////

                result = new StringPairDictionary(commandHelp);

                ///////////////////////////////////////////////////////////////////////////////////////

                GetInteractiveExtensionCommandHelp(
                    interpreter, ShellOps.InteractiveCommandPrefix,
                    ref result);

                ///////////////////////////////////////////////////////////////////////////////////////

                if (pattern != null) result = result.Filter(pattern, noCase);
            }

            return result;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        private static IDisplayHost GetDisplayHost(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.GetInteractiveHost(
                typeof(IDisplayHost)) as IDisplayHost;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HostTryWriteColor(
            IWriteHost writeHost,         /* in */
            string value,                 /* in */
            bool newLine,                 /* in */
            ConsoleColor foregroundColor, /* in */
            ConsoleColor backgroundColor  /* in */
            )
        {
            bool wrote = false;

            if (writeHost != null)
            {
                try
                {
                    if (FlagOps.HasFlags(writeHost.GetHostFlags(),
                            HostFlags.NonMonochromeMask, false))
                    {
                        if (newLine)
                        {
                            wrote = writeHost.WriteLine(
                                value, foregroundColor, backgroundColor);
                        }
                        else
                        {
                            wrote = writeHost.Write(
                                value, foregroundColor, backgroundColor);
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }

                if (!wrote)
                {
                    wrote = newLine ?
                        writeHost.WriteLine(value) : writeHost.Write(value);
                }
            }

            return wrote;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetVersionLine(
            Assembly assembly, /* in */
            string fileName,   /* in */
            bool compactMode   /* in */
            )
        {
            if (assembly == null)
                assembly = GlobalState.GetAssembly();

            if (fileName == null)
                fileName = (assembly != null) ? assembly.Location : null;

            string text = RuntimeOps.GetAssemblyTextOrSuffix(assembly);

            string imageRuntimeVersion = FormatOps.ShortImageRuntimeVersion(
                AssemblyOps.GetImageRuntimeVersion(assembly));

            string configuration = AttributeOps.GetAssemblyConfiguration(
                assembly);

            string certificateSubject = RuntimeOps.GetCertificateSubject(
                fileName, certificateSubjectPrefix, true, true, true);

            return String.Format(
                compactMode ? bannerCompactFormat : bannerFormat,
                GlobalState.GetPackageName(),
                GlobalState.GetAssemblyVersion(),
                SharedAttributeOps.GetAssemblyTag(assembly),
                !String.IsNullOrEmpty(text) ?
                    String.Format(bannerTextFormat, text) :
                    String.Empty,
                !String.IsNullOrEmpty(imageRuntimeVersion) ?
                    String.Format(bannerImageRuntimeVersionFormat,
                        imageRuntimeVersion) :
                    String.Empty,
                FormatOps.PackageDateTime(
                    SharedAttributeOps.GetAssemblyDateTime(assembly)),
                configuration, certificateSubject);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetReleaseLine(
            Assembly assembly /* in */
            )
        {
            if (assembly == null)
                assembly = GlobalState.GetAssembly();

            string release = SharedAttributeOps.GetAssemblyRelease(assembly);

            if (!String.IsNullOrEmpty(release))
                return String.Format(bannerReleaseFormat, release);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetDescriptionLine()
        {
            return String.Format(
                Vars.Description.Package, TclVars.Package.VersionValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool WriteBanner(
            Interpreter interpreter, /* in */
            bool noRelease,          /* in */
            bool noDescription,      /* in */
            bool noOfficial,         /* in */
            bool noTrusted,          /* in */
            bool noStable,           /* in */
            bool noSafe,             /* in */
            bool noSecurity,         /* in */
            bool noPlugins,          /* in */
            bool compactMode         /* in */
            )
        {
            if (interpreter != null)
            {
                IDisplayHost displayHost = GetDisplayHost(interpreter);

                if (displayHost != null)
                {
                    ConsoleColor bannerForegroundColor = _ConsoleColor.Default;
                    ConsoleColor bannerBackgroundColor = _ConsoleColor.Default;

                    interpreter.GetHostColors(
                        displayHost, ColorName.Banner, false, ref bannerForegroundColor,
                        ref bannerBackgroundColor);

                    Assembly assembly = GlobalState.GetAssembly();
                    string fileName = (assembly != null) ? assembly.Location : null;
                    string value = GetVersionLine(assembly, fileName, compactMode);
                    bool wrote = false;

                    if (!String.IsNullOrEmpty(value))
                    {
                        if (!HostTryWriteColor(
                                displayHost, value, true, bannerForegroundColor,
                                bannerBackgroundColor))
                        {
                            return false;
                        }

                        wrote = true;
                    }

                    if (!noRelease)
                    {
                        value = GetReleaseLine(assembly);

                        if (!String.IsNullOrEmpty(value))
                        {
                            if (wrote && !displayHost.WriteLine())
                                return false;

                            if (!HostTryWriteColor(
                                    displayHost, value, true, bannerForegroundColor,
                                    bannerBackgroundColor))
                            {
                                return false;
                            }

                            wrote = true;
                        }
                    }

                    if (!noDescription)
                    {
                        value = GetDescriptionLine();

                        if (!String.IsNullOrEmpty(value))
                        {
                            if (wrote && !displayHost.WriteLine())
                                return false;

                            if (!HostTryWriteColor(
                                    displayHost, value, true, bannerForegroundColor,
                                    bannerBackgroundColor))
                            {
                                return false;
                            }

                            wrote = true;
                        }
                    }

                    bool wroteOfficial = false;

                    if (!noOfficial)
                    {
                        bool official = RuntimeOps.IsOfficial();

                        if (official || !compactMode)
                        {
                            ConsoleColor officialForegroundColor = _ConsoleColor.Default;
                            ConsoleColor officialBackgroundColor = _ConsoleColor.Default;

                            interpreter.GetHostColors(
                                displayHost, official ? ColorName.Official : ColorName.Unofficial,
                                false, ref officialForegroundColor, ref officialBackgroundColor);

                            value = official ?
                                Vars.Description.Official : Vars.Description.Unofficial;

                            if (!String.IsNullOrEmpty(value))
                            {
                                if (wrote && !displayHost.WriteLine())
                                    return false;

                                if (!HostTryWriteColor(
                                        displayHost, value, true, officialForegroundColor,
                                        officialBackgroundColor))
                                {
                                    return false;
                                }

                                wroteOfficial = true;
                                wrote = true;
                            }
                        }
                    }

                    bool wroteTrusted = false;

                    if (!noTrusted)
                    {
                        bool trusted = (RuntimeOps.GetFileTrusted(fileName) != null);

                        if (trusted || !compactMode)
                        {
                            ConsoleColor trustedForegroundColor = _ConsoleColor.Default;
                            ConsoleColor trustedBackgroundColor = _ConsoleColor.Default;

                            interpreter.GetHostColors(
                                displayHost, trusted ? ColorName.Trusted : ColorName.Untrusted,
                                false, ref trustedForegroundColor, ref trustedBackgroundColor);

                            value = trusted ?
                                Vars.Description.Trusted : Vars.Description.Untrusted;

                            if (!String.IsNullOrEmpty(value))
                            {
                                if (wrote && !wroteOfficial && !displayHost.WriteLine())
                                    return false;

                                if (!HostTryWriteColor(
                                        displayHost, value, true, trustedForegroundColor,
                                        trustedBackgroundColor))
                                {
                                    return false;
                                }

                                wroteTrusted = true;
                                wrote = true;
                            }
                        }
                    }

                    bool wroteStable = false;

                    if (!noStable)
                    {
                        bool stable = RuntimeOps.IsStable();

                        if (stable || !compactMode)
                        {
                            ConsoleColor stableForegroundColor = _ConsoleColor.Default;
                            ConsoleColor stableBackgroundColor = _ConsoleColor.Default;

                            interpreter.GetHostColors(
                                displayHost, stable ? ColorName.Stable : ColorName.Unstable,
                                false, ref stableForegroundColor, ref stableBackgroundColor);

                            value = stable ?
                                Vars.Description.Stable : Vars.Description.Unstable;

                            if (!String.IsNullOrEmpty(value))
                            {
                                if (wrote && !wroteOfficial && !wroteTrusted &&
                                    !displayHost.WriteLine())
                                {
                                    return false;
                                }

                                if (!HostTryWriteColor(
                                        displayHost, value, true, stableForegroundColor,
                                        stableBackgroundColor))
                                {
                                    return false;
                                }

                                wroteStable = true;
                                wrote = true;
                            }
                        }
                    }

                    bool wroteSafe = false;

                    if (!noSafe)
                    {
                        bool safe = interpreter.InternalIsSafe();

                        if (safe || !compactMode)
                        {
                            ConsoleColor safeForegroundColor = _ConsoleColor.Default;
                            ConsoleColor safeBackgroundColor = _ConsoleColor.Default;

                            interpreter.GetHostColors(
                                displayHost, safe ? ColorName.Enabled : ColorName.Undefined,
                                false, ref safeForegroundColor, ref safeBackgroundColor);

                            value = String.Format(
                                Vars.Description.Safe, FormatOps.InterpreterNoThrow(
                                interpreter), safe ? "safe" : "unsafe");

                            if (!String.IsNullOrEmpty(value))
                            {
                                if (wrote && !wroteOfficial && !wroteTrusted &&
                                    !wroteStable && !displayHost.WriteLine())
                                {
                                    return false;
                                }

                                if (wrote &&
                                    (wroteOfficial || wroteTrusted || wroteStable) &&
                                    !displayHost.WriteLine())
                                {
                                    return false;
                                }

                                if (!HostTryWriteColor(
                                        displayHost, value, true, safeForegroundColor,
                                        safeBackgroundColor))
                                {
                                    return false;
                                }

                                wroteSafe = true;
                                wrote = true;
                            }
                        }
                    }

#if ISOLATED_PLUGINS
                    bool wroteSecurity = false;
#endif

                    if (!noSecurity)
                    {
                        bool security = interpreter.HasSecurity();

                        if (security || !compactMode)
                        {
                            ConsoleColor securityForegroundColor = _ConsoleColor.Default;
                            ConsoleColor securityBackgroundColor = _ConsoleColor.Default;

                            interpreter.GetHostColors(
                                displayHost, security ? ColorName.Enabled : ColorName.Disabled,
                                false, ref securityForegroundColor, ref securityBackgroundColor);

                            value = String.Format(
                                Vars.Description.Security, FormatOps.InterpreterNoThrow(
                                interpreter), security ? "enabled" : "disabled");

                            if (!String.IsNullOrEmpty(value))
                            {
                                if (wrote && !wroteOfficial && !wroteTrusted &&
                                    !wroteStable && !wroteSafe && !displayHost.WriteLine())
                                {
                                    return false;
                                }

                                if (wrote &&
                                    (wroteOfficial || wroteTrusted || wroteStable) &&
                                    !wroteSafe && !displayHost.WriteLine())
                                {
                                    return false;
                                }

                                if (!HostTryWriteColor(
                                        displayHost, value, true, securityForegroundColor,
                                        securityBackgroundColor))
                                {
                                    return false;
                                }

#if ISOLATED_PLUGINS
                                wroteSecurity = true;
#endif

                                wrote = true;
                            }
                        }
                    }

// #if ISOLATED_PLUGINS
//                     bool wroteIsolated = false;
// #endif

                    if (!noPlugins)
                    {
#if ISOLATED_PLUGINS
                        bool isolated = FlagOps.HasFlags(
                            interpreter.PluginFlags, PluginFlags.Isolated, true);

                        if (isolated || !compactMode)
                        {
                            ConsoleColor isolatedForegroundColor = _ConsoleColor.Default;
                            ConsoleColor isolatedBackgroundColor = _ConsoleColor.Default;

                            interpreter.GetHostColors(
                                displayHost, isolated ? ColorName.Enabled : ColorName.Disabled,
                                false, ref isolatedForegroundColor, ref isolatedBackgroundColor);

                            value = String.Format(
                                Vars.Description.Isolated, FormatOps.InterpreterNoThrow(
                                interpreter), isolated ? "enabled" : "disabled");

                            if (!String.IsNullOrEmpty(value))
                            {
                                if (wrote && !wroteOfficial && !wroteTrusted &&
                                    !wroteStable && !wroteSafe && !wroteSecurity &&
                                    !displayHost.WriteLine())
                                {
                                    return false;
                                }

                                if (wrote &&
                                    (wroteOfficial || wroteTrusted || wroteStable) &&
                                    !wroteSafe && !wroteSecurity && !displayHost.WriteLine())
                                {
                                    return false;
                                }

                                if (!HostTryWriteColor(
                                        displayHost, value, true, isolatedForegroundColor,
                                        isolatedBackgroundColor))
                                {
                                    return false;
                                }

                                // wroteIsolated = true;
                                wrote = true;
                            }
                        }
#endif

                        PluginWrapperDictionary plugins = interpreter.CopyPlugins();

                        if (plugins != null)
                        {
                            foreach (KeyValuePair<string, _Wrappers.Plugin> pair in plugins)
                            {
                                IPlugin plugin = pair.Value;

                                if (plugin == null)
                                    continue;

                                ///////////////////////////////////////////////////////////////////////

                                ReturnCode pluginCode;
                                Result pluginResult = null;

                                pluginCode = WritePluginBanner(
                                    interpreter, plugin, ref pluginResult);

                                if (pluginCode != ReturnCode.Ok)
                                {
                                    DebugOps.Complain(
                                        interpreter, pluginCode, pluginResult);
                                }
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool WriteLegalese(
            Interpreter interpreter, /* in */
            bool summaryOnly         /* in */
            )
        {
            if (interpreter != null)
            {
                IDisplayHost displayHost = GetDisplayHost(interpreter);

                if (displayHost != null)
                {
                    ConsoleColor foregroundColor = _ConsoleColor.Default;
                    ConsoleColor backgroundColor = _ConsoleColor.Default;

                    interpreter.GetHostColors(
                        displayHost, ColorName.Legal, false, ref foregroundColor,
                        ref backgroundColor);

                    Assembly assembly = GlobalState.GetAssembly();

                    string copyright = AttributeOps.GetAssemblyCopyright(
                        assembly, true);

                    bool wrote = false;

                    if (!String.IsNullOrEmpty(copyright))
                    {
                        if (HostTryWriteColor(displayHost,
                                copyright, true, foregroundColor, backgroundColor))
                        {
                            wrote = true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    string license = AttributeOps.GetAssemblyLicense(assembly,
                        summaryOnly);

                    if (!String.IsNullOrEmpty(license))
                    {
                        if (wrote && !displayHost.WriteLine())
                            return false;

                        if (HostTryWriteColor(displayHost,
                                license, true, foregroundColor, backgroundColor))
                        {
                            wrote = true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void WriteSectionHeader(
            IInteractiveHost interactiveHost, /* in */
            string text,                      /* in */
            int length                        /* in */
            )
        {
            if (interactiveHost != null)
            {
                if (!String.IsNullOrEmpty(text))
                {
                    interactiveHost.WriteLine(
                        String.Format("{0}{1}", Characters.HorizontalTab,
                        StringOps.StrRepeat(length, Characters.MinusSign)));

                    interactiveHost.WriteLine(
                        String.Format("{0}{1}", Characters.HorizontalTab,
                        StringOps.PadCenter(text, length, Characters.Space)));
                }

                interactiveHost.WriteLine(
                    String.Format("{0}{1}", Characters.HorizontalTab,
                    StringOps.StrRepeat(length, Characters.MinusSign)));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode WriteUsage(
            Interpreter interpreter, /* in */
            string error,            /* in */
            bool showBanner,         /* in */
            bool showLegalese,       /* in */
            bool showOptions,        /* in */
            bool showEnvironment,    /* in */
            bool compactMode,        /* in */
            ref Result result        /* out */
            )
        {
            try
            {
                if (interpreter != null)
                {
                    IDisplayHost displayHost = GetDisplayHost(interpreter);

                    if (displayHost != null)
                    {
                        //
                        // NOTE: Did the caller specify an error message to
                        //       display?
                        //
                        bool showError = !String.IsNullOrEmpty(error);

                        //
                        // HACK: Honor the quiet mode for the interpreter.
                        //
                        if (showError && interpreter.ShouldBeQuiet())
                            showError = false;

                        //
                        // NOTE: If we are going to display the error message
                        //       do it now, before any other output.
                        //
                        if (showError)
                        {
                            displayHost.WriteResultLine(
                                ReturnCode.Error, error);
                        }

                        if (showBanner)
                        {
                            if (showError)
                                displayHost.WriteLine();

                            WriteBanner(
                                interpreter, false, false, false, false,
                                false, false, false, false, compactMode);
                        }

                        if (showLegalese)
                        {
                            if (showError || showBanner)
                                displayHost.WriteLine();

                            WriteLegalese(interpreter, true);
                        }

                        bool showUsage = (showOptions || showEnvironment);
                        string executableFileName = PathOps.GetExecutableNameOnly();

                        if (showUsage)
                        {
                            if (showError || showBanner || showLegalese)
                                displayHost.WriteLine();

                            WriteSectionHeader(displayHost, "Usage", UsageWidth);
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}{1} [options]",
                                Characters.HorizontalTab, executableFileName));
                        }

                        if (showOptions)
                        {
                            if (showError || showBanner || showLegalese || showUsage)
                                displayHost.WriteLine();

                            string argvFileName = ShellOps.GetArgumentsFileName(executableFileName);

                            WriteSectionHeader(displayHost, "Command Line Notes", UsageWidth);
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}The option names below are all case-insensitive.  Most of these options\n" +
                                "{0}are processed precisely in the order they are encountered.  All options\n" +
                                "{0}with names beginning with \"-startup\" may be processed prior to creation\n" +
                                "{0}of the interpreter.",
                                Characters.HorizontalTab));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the file \"{1}\" (or supported per-user, per-machine,\n" +
                                "{0}or per-domain variations thereof) exist(s) in the executable directory,\n" +
                                "{0}its entire contents will be read and processed as though the file name\n" +
                                "{0}was specified via the \"-{2}\" option (see below) and then inserted\n" +
                                "{0}before any preexisting arguments, prior to further argument processing.",
                                Characters.HorizontalTab, argvFileName, CommandLineOption.Arguments));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If any unrecognized argument is encountered it will be passed to the\n" +
                                "{0}shell argument callback, if any; otherwise, an error will be generated.",
                                Characters.HorizontalTab));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If no arguments are supplied, or if at any time there are no arguments\n" +
                                "{0}remaining to be processed, the interactive loop will be entered (unless\n" +
                                "{0}it has been disabled).",
                                Characters.HorizontalTab));
                            displayHost.WriteLine();
                            WriteSectionHeader(displayHost, "Command Line Options", UsageWidth);
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <fileName> : Evaluates the specified file (whether or not the\n" +
                                "{0}                      script library has been initialized) and then\n" +
                                "{0}                      continues processing arguments.  Also see the\n" +
                                "{0}                      \"-{2}\", \"-{3}\", and \"-{4}\" options.",
                                Characters.HorizontalTab, CommandLineOption.AnyFile,
                                CommandLineOption.PreFile, CommandLineOption.File,
                                CommandLineOption.PostFile));
                            displayHost.WriteLine();
#if !ENTERPRISE_LOCKDOWN
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <script> : Evaluates the specified script (whether or\n" +
                                "{0}                          not the script library has been initialized)\n" +
                                "{0}                          and then continues processing arguments.\n" +
                                "{0}                          Also see the \"-{2}\", \"-{3}\",\n" +
                                "{0}                          and \"-{4}\" options.",
                                Characters.HorizontalTab, CommandLineOption.AnyInitialize,
                                CommandLineOption.PreInitialize, CommandLineOption.Initialize,
                                CommandLineOption.PostInitialize));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <fileName> : Reads the entire specified file, attempting to\n" +
                                "{0}                        interpret each line as a list of arguments to\n" +
                                "{0}                        be inserted in order, replacing both the\n" +
                                "{0}                        \"-{1}\" option and the file name and then\n" +
                                "{0}                        continues processing arguments.  The literal\n" +
                                "{0}                        string \"{2}\" or \"{3}\" may be used to specify\n" +
                                "{0}                        the standard input stream as the file to read.",
                                Characters.HorizontalTab, CommandLineOption.Arguments,
                                CommandLineArgument.StandardInput, StandardChannel.Input));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Waits until a key is pressed and then triggers a managed\n" +
                                "{0}         debugger break (useful for attaching a managed debugger before\n" +
                                "{0}         any arguments after it are processed).",
                                Characters.HorizontalTab, CommandLineOption.Break));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Changes the interpreter to its child interpreter, if\n" +
                                "{0}         available, and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.Child));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Clears all trace listeners and then continues processing\n"+
                                "{0}              arguments.",
                                Characters.HorizontalTab, CommandLineOption.ClearTrace));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Enables debug mode for the interpreter and then continues\n" +
                                "{0}         processing arguments (i.e. various strategically placed\n" +
                                "{0}         diagnostic messages are produced to help troubleshoot the\n" +
                                "{0}         startup process).",
                                Characters.HorizontalTab, CommandLineOption.Debug));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <encodingName> : Sets the encoding to use for script files\n" +
                                "{0}                           and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.Encoding));
                            displayHost.WriteLine();
#if !ENTERPRISE_LOCKDOWN
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} [string ...] : Evaluates the specified string(s) and then\n" +
                                "{0}                         exits.",
                                Characters.HorizontalTab, CommandLineOption.Evaluate));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} [string ...] : Evaluates the specified base64 encoded\n" +
                                "{0}                                string(s) and then exits.  This is\n" +
                                "{0}                                useful for strings that may contain\n" +
                                "{0}                                complex quoting constructs that would\n" +
                                "{0}                                otherwise conflict with the operating\n" +
                                "{0}                                system command line quoting.",
                                Characters.HorizontalTab, CommandLineOption.EvaluateEncoded));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <fileName> [argument ...] : Evaluates the specified file and then\n" +
                                "{0}                                  exits.",
                                Characters.HorizontalTab, CommandLineOption.File));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Enables forced initialization of the script library\n" +
                                "{0}                   and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.ForceInitialize));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Displays version and syntax information and then exits.",
                                Characters.HorizontalTab, CommandLineOption.Help));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Immediately attempts to initialize the script library for\n" +
                                "{0}              the interpreter and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.Initialize));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Enables interactive mode for the interpreter and then\n" +
                                "{0}               continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.Interactive));
                            displayHost.WriteLine();
#if ISOLATED_PLUGINS
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <enable> : Enables or disables plugin isolation for the\n" +
                                "{0}                     interpreter and then continues processing\n" +
                                "{0}                     arguments.",
                                Characters.HorizontalTab, CommandLineOption.Isolated));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Arranges for the interactive loop to be reentered instead of\n" +
                                "{0}         exiting the process and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.Kiosk));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : This option is processed prior to the standard\n" +
                                "{0}                     argument processing and it causes all existing\n" +
                                "{0}                     arguments to be replaced by those returned from\n" +
                                "{0}                     the interpreter host; furthermore, it causes any\n" +
                                "{0}                     arguments from the file \"{2}\" to\n" +
                                "{0}                     be ignored.  If the interpreter host does not\n" +
                                "{0}                     return any arguments, this option has no effect.",
                                Characters.HorizontalTab, CommandLineOption.LockHostArguments, argvFileName));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <enable> : Enables or disables Tcl {2} compliant namespace\n" +
                                "{0}                       support for the interpreter and then continues\n" +
                                "{0}                       processing arguments.  However, unlike native\n" +
                                "{0}                       Tcl, the \"dangers of creative writing\" does not\n" +
                                "{0}                       apply (see \"https://wiki.tcl.tk/1030\").",
                                Characters.HorizontalTab, CommandLineOption.Namespaces,
                                TclVars.Package.VersionValue));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : This option is processed prior to the standard\n" +
                                "{0}                 argument processing and it causes processing of\n" +
                                "{0}                 arguments from the application settings to be skipped\n" +
                                "{0}                 and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.NoAppSettings));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : This option is processed prior to the standard\n" +
                                "{0}                        argument processing and it causes processing of\n" +
                                "{0}                        arguments from various files, including\n" +
                                "{0}                        \"{2}\", to be skipped (i.e. if\n" +
                                "{0}                        it exists in the executable directory) and then\n" +
                                "{0}                        continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.NoArgumentsFileNames, argvFileName));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Arranges for the interactive loop to be entered instead of\n" +
                                "{0}          exiting the process and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.NoExit));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Disables automatic trimming of surrounding whitespace for all\n" +
                                "{0}          subsequent arguments and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.NoTrim));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Changes the interpreter to its parent interpreter, if\n" +
                                "{0}          available, and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.Parent));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Waits for a key to be pressed and then continues processing\n" +
                                "{0}         arguments (useful for attaching a managed debugger before any\n" +
                                "{0}         scripts [with the possible exception of the script library]\n" +
                                "{0}         have been evaluated).",
                                Characters.HorizontalTab, CommandLineOption.Pause));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <pluginName> <arguments> : Stores the specified\n" +
                                "{0}                                            arguments and arranges for\n" +
                                "{0}                                            them to be passed into the\n" +
                                "{0}                                            specified plugin if/when it\n" +
                                "{0}                                            is subsequently loaded into\n" +
                                "{0}                                            the interpreter and then\n" +
                                "{0}                                            continues processing\n" +
                                "{0}                                            arguments.  If the plugin\n" +
                                "{0}                                            is never loaded into the\n" +
                                "{0}                                            interpreter after this\n" +
                                "{0}                                            point, the arguments will\n" +
                                "{0}                                            never be used again.",
                                Characters.HorizontalTab, CommandLineOption.PluginArguments));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} [pattern] [all] [argument ...] : Runs the specified plugin\n" +
                                "{0}                                             test(s) or the full plugin\n" +
                                "{0}                                             test suite(s) if no\n" +
                                "{0}                                             pattern is supplied and\n" +
                                "{0}                                             then exits.",
                                Characters.HorizontalTab, CommandLineOption.PluginTest));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <fileName> : Evaluates the specified file (after the script\n" +
                                "{0}                       library has been initialized) and then continues\n" +
                                "{0}                       processing arguments.  If the script library has\n" +
                                "{0}                       not been initialized, an error is generated (see\n" +
                                "{0}                       the \"-{2}\" option, above).",
                                Characters.HorizontalTab, CommandLineOption.PostFile,
                                CommandLineOption.Initialize));
                            displayHost.WriteLine();
#if !ENTERPRISE_LOCKDOWN
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <script> : Evaluates the specified script (after the\n" +
                                "{0}                           script library has been initialized) and\n" +
                                "{0}                           then continues processing arguments.  If the\n" +
                                "{0}                           script library has not been initialized, an\n" +
                                "{0}                           error is generated (see the \"-{2}\"\n" +
                                "{0}                           option, above).",
                                Characters.HorizontalTab, CommandLineOption.PostInitialize,
                                CommandLineOption.Initialize));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <fileName> : Evaluates the specified file (before the script\n" +
                                "{0}                      library has been initialized) and then continues\n" +
                                "{0}                      processing arguments.  If the script library has\n" +
                                "{0}                      been initialized, an error is generated (see the\n" +
                                "{0}                      \"-{2}\" option, above).",
                                Characters.HorizontalTab, CommandLineOption.PreFile,
                                CommandLineOption.Initialize));
                            displayHost.WriteLine();
#if !ENTERPRISE_LOCKDOWN
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <script> : Evaluates the specified script (before the\n" +
                                "{0}                          script library has been initialized) and then\n" +
                                "{0}                          continues processing arguments.  If the\n" +
                                "{0}                          script library has been initialized, an error\n" +
                                "{0}                          is generated (see the \"-{2}\" option,\n" +
                                "{0}                          above).",
                                Characters.HorizontalTab, CommandLineOption.PreInitialize,
                                CommandLineOption.Initialize));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <profile> : Loads the specified interpreter host profile and\n" +
                                "{0}                     then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.Profile));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <enable> : Enables or disables quiet mode for the shell itself\n" +
                                "{0}                  and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.Quiet));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <settings> : Loads the specified interpreter settings,\n" +
                                "{0}                          recreates the interpreter based on the new\n" +
                                "{0}                          settings, and then continues processing\n" +
                                "{0}                          arguments.",
                                Characters.HorizontalTab, CommandLineOption.Reconfigure));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Copies the interpreter settings from the interpreter,\n" +
                                "{0}            recreates the interpreter based (mostly) on the old\n" +
                                "{0}            settings, and then continues processing\n"+
                                "{0}            arguments.",
                                Characters.HorizontalTab, CommandLineOption.Recreate));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <optionName> : Adds, removes, or resets the specified\n" +
                                "{0}                              runtime option(s) and then continues\n" +
                                "{0}                              processing arguments.  If the script\n" +
                                "{0}                              library has not been initialized, an\n" +
                                "{0}                              error is generated (see the \"-{2}\"\n" +
                                "{0}                              option, above).",
                                Characters.HorizontalTab, CommandLineOption.RuntimeOption,
                                CommandLineOption.Initialize));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Enables \"safe\" mode for the interpreter and then continues\n" +
                                "{0}        processing arguments (all \"unsafe\" commands will be hidden).",
                                Characters.HorizontalTab, CommandLineOption.Safe));
                            displayHost.WriteLine();
#if TEST
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <value> : Uses the specified value to create and add a\n" +
                                "{0}                       trace listener and then continues processing\n" +
                                "{0}                       arguments.",
                                Characters.HorizontalTab, CommandLineOption.ScriptTrace));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <enable> : Enables or disables script signing policies and\n" +
                                "{0}                     core script certificates for the interpreter,\n" +
                                "{0}                     using plugins that belong to the security package\n" +
                                "{0}                     (e.g. Harpy and Badge), and then continues\n" +
                                "{0}                     processing arguments.  If any of the necessary\n" +
                                "{0}                     plugins are unavailable, an error is generated.",
                                Characters.HorizontalTab, CommandLineOption.Security));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Arranges for the interpreter to be recreated the next time\n" +
                                "{0}             its available commands would have been modified and then\n" +
                                "{0}             continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.SetCreate));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <enable> : Enables or disables initialization of the\n" +
                                "{0}                          script library and then continues processing\n" +
                                "{0}                          arguments.",
                                Characters.HorizontalTab, CommandLineOption.SetInitialize));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <enable> : Enables or disables entering the interactive loop\n" +
                                "{0}                    and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.SetLoop));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Sets up trace listeners appropriate to the current debug\n"+
                                "{0}              mode and then continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.SetupTrace));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Enables \"standard\" mode for the interpreter and then\n" +
                                "{0}            continues processing arguments (all \"non-standard\" commands\n" +
                                "{0}            will be hidden).",
                                Characters.HorizontalTab, CommandLineOption.Standard));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <directory> : This option is processed prior to\n" +
                                "{0}                              interpreter creation.  Sets the script\n" +
                                "{0}                              library location to the specified\n" +
                                "{0}                              directory and then continues processing\n" +
                                "{0}                              arguments.",
                                Characters.HorizontalTab, CommandLineOption.StartupLibrary));
                            displayHost.WriteLine();
#if TEST
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <fileName> : This option is processed prior to\n" +
                                "{0}                             interpreter creation.  Sets up tracing to\n" +
                                "{0}                             the specified file and then continues\n" +
                                "{0}                             processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.StartupLogFile));
                            displayHost.WriteLine();
#endif
#if !ENTERPRISE_LOCKDOWN
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <script> : This option is processed prior to\n" +
                                "{0}                                 interpreter creation.  Evaluates the\n" +
                                "{0}                                 specified script (before the script\n" +
                                "{0}                                 library has been initialized) and then\n" +
                                "{0}                                 continues processing arguments.  If\n" +
                                "{0}                                 the script library has been\n" +
                                "{0}                                 initialized, an error is generated\n" +
                                "{0}                                 (see the \"-{2}\" option, above).",
                                Characters.HorizontalTab, CommandLineOption.StartupPreInitialize,
                                CommandLineOption.Initialize));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Enables single-step mode for the script debugger and then\n" +
                                "{0}        continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.Step));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} [pattern] [all] [argument ...] : Runs the specified test(s) or\n" +
                                "{0}                                       the full test suite(s) if no\n" +
                                "{0}                                       pattern is supplied and then\n" +
                                "{0}                                       exits.",
                                Characters.HorizontalTab, CommandLineOption.Test));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <directory> : Sets the base directory for the test\n" +
                                "{0}                             suite.  This directory is used when\n" +
                                "{0}                             searching for test files matching a\n" +
                                "{0}                             specific pattern.",
                                Characters.HorizontalTab, CommandLineOption.TestDirectory));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Enables trace listener output to potentially be written\n" +
                                "{0}               to the interpreter host and then continues processing\n" +
                                "{0}               arguments.  This option is intended for use with custom\n" +
                                "{0}               shells.  The default shell does not make use of the\n" +
                                "{0}               interpreter flag set by this option.",
                                Characters.HorizontalTab, CommandLineOption.TraceToHost));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} <path> : Sets the vendor path (i.e. the name of an\n" +
                                "{0}                     additional sub-directory within each directory\n" +
                                "{0}                     searched when attempting to find user-specific\n" +
                                "{0}                     and/or application-specific files) and then\n" +
                                "{0}                     continues processing arguments.",
                                Characters.HorizontalTab, CommandLineOption.VendorPath));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}-{1} : Displays detailed version information and then exits.",
                                Characters.HorizontalTab, CommandLineOption.Version));
                        }

                        if (showEnvironment)
                        {
                            if (showError || showBanner || showLegalese || showUsage || showOptions)
                                displayHost.WriteLine();

                            WriteSectionHeader(displayHost, "Environment Notes", UsageWidth);
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}Environment variable names may be case-sensitive (i.e. it depends on\n" +
                                "{0}the underlying operating system).",
                                Characters.HorizontalTab));
                            displayHost.WriteLine();
                            WriteSectionHeader(displayHost, "Environment Variables", UsageWidth);
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}internal calls into the garbage collector will always wait for all\n" +
                                "{0}pending finalizers to complete, even when run from a non-default\n" +
                                "{0}application domain context.  By default, waiting for all pending\n" +
                                "{0}finalizers to complete is disabled in non-default application domain\n" +
                                "{0}contexts in an attempt to prevent potential deadlocks.",
                                Characters.HorizontalTab, EnvVars.AlwaysWaitForGC));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}it will be used as the anchor point for assembly paths instead of the\n" +
                                "{0}application domain base directory and/or the process binary directory.",
                                Characters.HorizontalTab, EnvVars.AssemblyAnchorPath));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set\n" +
                                "{0}[to anything], its value will be used to set the list of \"bonus\" trace\n" +
                                "{0}categories.  If the value cannot be converted to a trace category list,\n" +
                                "{0}it will be ignored.",
                                Characters.HorizontalTab, EnvVars.BonusTraceCategories));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], waits until a\n" +
                                "{0}key is pressed and then triggers a managed debugger break (useful for\n" +
                                "{0}attaching a managed debugger before any significant initialization has\n" +
                                "{0}been performed).",
                                Characters.HorizontalTab, EnvVars.Break));
                            displayHost.WriteLine();
#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set, its value will be\n" +
                                "{0}used to alter or set the default cache level.  If the value cannot be\n" +
                                "{0}converted to an integer, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.BumpCacheLevel));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set, its value will be used\n" +
                                "{0}to alter or set the cache flags for the interpreter.  If the value\n" +
                                "{0}cannot be converted to cache flags, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.CacheFlags));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], all\n" +
                                "{0}trace listeners will be cleared before any are added.",
                                Characters.HorizontalTab, EnvVars.ClearTrace));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}complaint output may be sent to the \"{2}\" and/or\n" +
                                "{0}\"{3}\" classes.",
                                Characters.HorizontalTab, EnvVars.ComplainViaTrace,
                                typeof(Debug).FullName, typeof(Trace).FullName));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}complaint output may be sent to the \"{2}\" or \"{3}\" commands\n" +
                                "{0}within the interpreter.",
                                Characters.HorizontalTab, EnvVars.ComplainViaTest, TestOps.putsNormalCommand,
                                TestOps.putsFallbackCommand));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the console\n" +
                                "{0}window will be enabled.  This option is intended to be processed by\n" +
                                "{0}custom shells.  The default shell does not process this option (i.e. it\n" +
                                "{0}always attempts to use and/or create a console window).",
                                Characters.HorizontalTab, EnvVars.Console));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set, its value will be\n" +
                                "{0}used to alter or set the creation flags for the interpreter.  If the\n" +
                                "{0}value cannot be converted to creation flags, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.CreateFlags));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], debug mode is\n" +
                                "{0}enabled immediately after creating the interpreter (i.e. various\n" +
                                "{0}strategically placed diagnostic messages are produced to help\n" +
                                "{0}troubleshoot the startup process).",
                                Characters.HorizontalTab, EnvVars.Debug));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], quiet\n" +
                                "{0}mode is enabled immediately after creating the interpreter (see the\n" +
                                "{0}\"{2}\" environment variable, below).",
                                Characters.HorizontalTab, EnvVars.DefaultQuiet, EnvVars.Quiet));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}tracing of managed call stack information is enabled immediately after\n" +
                                "{0}creating the interpreter (see the \"{2}\" environment variable,\n" +
                                "{0}below).",
                                Characters.HorizontalTab, EnvVars.DefaultTraceStack, EnvVars.TraceStack));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], it will be\n" +
                                "{0}interpreted as the name of the base directory to be used when figuring\n" +
                                "{0}out where the script library is located; however, it will only be used\n" +
                                "{0}when explicitly attempting to automatically detect its location.",
                                Characters.HorizontalTab, EnvVars.Eagle));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], it will\n" +
                                "{0}be interpreted as the name of the base directory to be used when\n" +
                                "{0}figuring out where the script library, packages, and tests are located.",
                                Characters.HorizontalTab, EnvVars.EagleBase));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" or \"{2}\" environment variables are set [to\n" +
                                "{0}anything], they will be interpreted as lists of directory names where\n" +
                                "{0}the script library and/or additional package indexes may be located.",
                                Characters.HorizontalTab, EnvVars.EagleLibPath, EnvVars.TclLibPath));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" or \"{2}\" environment variables are set\n" +
                                "{0}[to anything], they will be interpreted as directory names where the\n" +
                                "{0}script library and/or additional package indexes may be located.",
                                Characters.HorizontalTab, EnvVars.EagleLibrary, EnvVars.TclLibrary));
                            displayHost.WriteLine();
#if CONSOLE
                            displayHost.WriteLine(String.Format(
                                "{0}The \"{1}\" environment variable is used\n" +
                                "{0}to track references to the console interpreter host provided by the\n" +
                                "{0}core library.  It is not designed for use outside of the core library\n" +
                                "{0}itself.",
                                Characters.HorizontalTab,
                                ConsoleOps.GetEnvironmentVariable(ProcessOps.GetId())));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\", \"{2}\", or \"{3}\"\n" +
                                "{0}environment variables are set [to anything], their values will be used\n" +
                                "{0}by the test suite as the name of the directory where temporary files\n" +
                                "{0}should be stored.",
                                Characters.HorizontalTab, EnvVars.EagleTestTemp, EnvVars.EagleTemp,
                                EnvVars.XdgRuntimeDir));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\", \"{2}\", \"{3}\", or \"{4}\"\n" +
                                "{0}environment variables are set [to anything], their values will be used\n" +
                                "{0}by the native Tcl integration subsystem as directory/file locations to\n" +
                                "{0}check for native Tcl libraries.",
                                Characters.HorizontalTab, EnvVars.EagleTclDll, EnvVars.EagleTkDll,
                                EnvVars.TclDll, EnvVars.TkDll));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\", \"{2}\", \"{3}\", or \"{4}\"\n" +
                                "{0}environment variables are set [to anything], their values will be used\n" +
                                "{0}by the test suite as directory/file locations to check for native Tcl\n" +
                                "{0}shells.",
                                Characters.HorizontalTab, EnvVars.EagleTclShell, EnvVars.EagleTkShell,
                                EnvVars.TclShell, EnvVars.TkShell));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set, its value will be\n"+
                                "{0}used to override the length limit for elided text that would otherwise\n"+
                                "{0}use the default length limit.  If the value cannot be converted to an\n"+
                                "{0}integer, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.EllipsisLimit));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], it\n" +
                                "{0}will bypass detection of potential error conditions that may prevent\n" +
                                "{0}plugins that belong to the security package (e.g. Harpy and Badge)\n" +
                                "{0}from being loaded.",
                                Characters.HorizontalTab, EnvVars.ForceSecurity));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set, its value will be\n" +
                                "{0}used to alter or set the creation flags for the interpreter host.  If\n" +
                                "{0}the value cannot be converted to host creation flags, it will be\n"+
                                "{0}ignored.",
                                Characters.HorizontalTab, EnvVars.HostCreateFlags));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set, its value will be\n" +
                                "{0}used to alter or set the initialize flags for the interpreter.  If the\n" +
                                "{0}value cannot be converted to initialize flags, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.InitializeFlags));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}interactive mode is enabled immediately after creating the interpreter.",
                                Characters.HorizontalTab, EnvVars.Interactive));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set, its value will\n" +
                                "{0}be used to alter or set the instance flags for the interpreter.  If the\n" +
                                "{0}value cannot be converted to instance flags, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.InterpreterFlags));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], various\n" +
                                "{0}time measurements will be made to help troubleshoot performance issues.",
                                Characters.HorizontalTab, EnvVars.MeasureTime));
                            displayHost.WriteLine();
#if XML
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}application settings from applicable sources will be merged.",
                                Characters.HorizontalTab, EnvVars.MergeAllAppSettings));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}application settings from applicable XML files will be merged.",
                                Characters.HorizontalTab, EnvVars.MergeXmlAppSettings));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to\n"+
                                "{0}anything], it will be used as the script to evaluate just prior to\n"+
                                "{0}initializing an interpreter created by the native package.",
                                Characters.HorizontalTab, EnvVars.NativePackagePreInitialize));
                            displayHost.WriteLine();
#if NETWORK
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], it\n" +
                                "{0}will be interpreted as an integer number of milliseconds to use as the\n" +
                                "{0}default network timeout.",
                                Characters.HorizontalTab, EnvVars.NetworkTimeout));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], internal\n" +
                                "{0}calls into the garbage collector will be disabled.",
                                Characters.HorizontalTab, EnvVars.NeverGC));
                            displayHost.WriteLine();
#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}internal calls into the garbage collector will never compact the large\n" +
                                "{0}object heap.  By default, the large object heap will be compacted when\n"+
                                "{0}the memory load exceeds a configurable threshold.",
                                Characters.HorizontalTab, EnvVars.NeverCompactForGC));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}internal calls into the garbage collector will never wait for all\n" +
                                "{0}pending finalizers to complete.  By default, waiting for all pending\n" +
                                "{0}finalizers to complete is disabled in non-default application domain\n" +
                                "{0}contexts in an attempt to prevent potential deadlocks.",
                                Characters.HorizontalTab, EnvVars.NeverWaitForGC));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}processing of arguments from the application settings will be skipped.",
                                Characters.HorizontalTab, EnvVars.NoAppSettings));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], attempts to\n" +
                                "{0}trigger a managed debugger break will be logged and then ignored.",
                                Characters.HorizontalTab, EnvVars.NoBreak));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the\n" +
                                "{0}interpreter host script cancellation interface will not be enabled\n" +
                                "{0}(e.g. the Control-C key being pressed will not trigger script\n" +
                                "{0}cancellation).",
                                Characters.HorizontalTab, EnvVars.NoCancel));
                            displayHost.WriteLine();
#if CONSOLE
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the console\n" +
                                "{0}window cannot be closed.",
                                Characters.HorizontalTab, EnvVars.NoClose));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the console\n" +
                                "{0}output will not be in color.",
                                Characters.HorizontalTab, EnvVars.NoColor));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the\n" +
                                "{0}console window will be disabled.  This option is intended to be\n" +
                                "{0}processed by custom shells.  The default shell does not process this\n" +
                                "{0}option (i.e. it always attempts to use and/or create a console window).",
                                Characters.HorizontalTab, EnvVars.NoConsole));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the\n" +
                                "{0}console interpreter host will not setup the console window (i.e. the\n" +
                                "{0}title, icon, mode, and cancel key press handler will not be modified).",
                                Characters.HorizontalTab, EnvVars.NoConsoleSetup));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the core\n" +
                                "{0}interpreter host will deny exit requests.",
                                Characters.HorizontalTab, EnvVars.NoExit));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the console\n" +
                                "{0}icon will not be changed.",
                                Characters.HorizontalTab, EnvVars.NoIcon));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], script\n" +
                                "{0}library initialization is skipped after creating the interpreter.",
                                Characters.HorizontalTab, EnvVars.NoInitialize));
                            displayHost.WriteLine();
#if SHELL
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}shell script library initialization is skipped when entering the\n" +
                                "{0}interactive loop.",
                                Characters.HorizontalTab, EnvVars.NoInitializeShell));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the\n" +
                                "{0}interactive loop will not be entered.",
                                Characters.HorizontalTab, EnvVars.NoLoop));
                            displayHost.WriteLine();
#if NATIVE && WINDOWS
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the\n" +
                                "{0}mutexes normally checked by external updaters and setup packages will\n" +
                                "{0}not be created or opened.",
                                Characters.HorizontalTab, EnvVars.NoMutexes));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], any\n" +
                                "{0}features that require native console integration will be disabled.",
                                Characters.HorizontalTab, EnvVars.NoNativeConsole));
                            displayHost.WriteLine();
#endif
#if NATIVE
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the\n" +
                                "{0}native stack checking subsystem will be disabled.",
                                Characters.HorizontalTab, EnvVars.NoNativeStack));
                            displayHost.WriteLine();
#endif
#if NATIVE && NATIVE_UTILITY
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the\n" +
                                "{0}native utility library will not be loaded.",
                                Characters.HorizontalTab, EnvVars.NoNativeUtility));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}(asynchronous) population of the \"{2}\" array element\n" +
                                "{0}will be skipped.",
                                Characters.HorizontalTab, EnvVars.NoPopulateOsExtra,
                                FormatOps.VariableName(TclVars.Platform.Name, TclVars.Platform.OsExtra)));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the\n" +
                                "{0}interpreter host profile will not be loaded.",
                                Characters.HorizontalTab, EnvVars.NoProfile));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}plugin update checks will be skipped for the security package.",
                                Characters.HorizontalTab, EnvVars.NoSecurityUpdate));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}exceptions will not be thrown when a disposed object is accessed.",
                                Characters.HorizontalTab, EnvVars.NoThrowOnDisposed));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the console\n" +
                                "{0}title will not be changed.",
                                Characters.HorizontalTab, EnvVars.NoTitle));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], all tracing\n" +
                                "{0}within the core library will be disabled.  If the \"{1}\" environment\n" +
                                "{0}variable is set [to anything], the \"{2}\" environment variable has no\n" +
                                "{0}effect.",
                                Characters.HorizontalTab, EnvVars.NoTrace, EnvVars.Trace));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}its value will be used to set the list of disabled trace categories.\n" +
                                "{0}If the value cannot be converted to a trace category list, it will be\n" +
                                "{0}ignored.",
                                Characters.HorizontalTab, EnvVars.NoTraceCategories));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], all\n" +
                                "{0}frequency limits on tracing within the core library will be disabled.",
                                Characters.HorizontalTab, EnvVars.NoTraceLimits));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], assembly\n" +
                                "{0}files will not be checked for trust during the interpreter creation\n" +
                                "{0}process.",
                                Characters.HorizontalTab, EnvVars.NoTrusted));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], checking\n" +
                                "{0}for updates will be disabled.  This restriction only applies automatic\n" +
                                "{0}checks within the core library itself.",
                                Characters.HorizontalTab, EnvVars.NoUpdates));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], selected\n" +
                                "{0}diagnostic messages will be disabled during the interpreter creation\n" +
                                "{0}process.",
                                Characters.HorizontalTab, EnvVars.NoVerbose));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], assembly\n" +
                                "{0}strong name signatures will not be verified during the interpreter\n" +
                                "{0}creation process.",
                                Characters.HorizontalTab, EnvVars.NoVerified));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set\n" +
                                "{0}[to anything], its value will be used to set the list of \"penalty\"\n" +
                                "{0}trace categories.  If the value cannot be converted to a trace category\n" +
                                "{0}list, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.PenaltyTraceCategories));
                            displayHost.WriteLine();
#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], it\n" +
                                "{0}will be interpreted as a list of patterns to match against candidate\n" +
                                "{0}plugin file names.",
                                Characters.HorizontalTab, EnvVars.PluginPatterns));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the\n" +
                                "{0}interpreter host will attempt to load the specified profile.",
                                Characters.HorizontalTab, EnvVars.Profile));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], potentially\n" +
                                "{0}important diagnostic messages about non-fatal (yet otherwise\n" +
                                "{0}unreportable) errors will not cause a modal message box to be displayed\n" +
                                "{0}when the originating interpreter [host] is unknown or unavailable.  Use\n" +
                                "{0}of this environment variable is not recommended (i.e. normally, it\n" +
                                "{0}would be preferable to determine the root cause of the message and fix\n" +
                                "{0}that instead).",
                                Characters.HorizontalTab, EnvVars.Quiet));
                            displayHost.WriteLine();
#if CONFIGURATION
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}the application settings will be refreshed before the next time they\n"+
                                "{0}are used.",
                                Characters.HorizontalTab, EnvVars.RefreshAppSettings));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], created\n" +
                                "{0}result objects will capture managed call stack information.",
                                Characters.HorizontalTab, EnvVars.ResultStack));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], enable \"safe\"\n" +
                                "{0}mode for the interpreter (all \"unsafe\" commands will be hidden).",
                                Characters.HorizontalTab, EnvVars.Safe));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set, its value will be\n" +
                                "{0}used to alter or set the script flags for the interpreter.  If the\n" +
                                "{0}value cannot be converted to script flags, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.ScriptFlags));
                            displayHost.WriteLine();
#if TEST
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], its\n" +
                                "{0}value will be used to create and add a trace listener.  If the value\n" +
                                "{0}cannot be converted to a trace listener, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.ScriptTrace));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], trace\n" +
                                "{0}listeners appropriate to the current debug mode will be setup.",
                                Characters.HorizontalTab, EnvVars.SetupTrace));
                            displayHost.WriteLine();
#if !ENTERPRISE_LOCKDOWN
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}the default shell will pre-scan for a \"-{2}\" option, causing\n" +
                                "{0}the interpreter created for the shell to evaluate the specified script\n" +
                                "{0}very early during its creation process.  It should be noted that using\n" +
                                "{0}this environment variable may cause the first script specified using a\n" +
                                "{0}\"-{2}\" option to be evaluated twice.",
                                Characters.HorizontalTab, EnvVars.ShellPreInitialize,
                                CommandLineOption.PreInitialize));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If an \"{1}_<name>\" environment variable is set [to anything],\n" +
                                "{0}its value will be used in lieu of the value from GetFolderPath(<name>)\n" +
                                "{0}for the folder identified by <name>.",
                                Characters.HorizontalTab, EnvVars.SpecialFolder));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], enable\n" +
                                "{0}\"standard\" mode for the interpreter (all \"non-standard\" commands will\n" +
                                "{0}be hidden).",
                                Characters.HorizontalTab, EnvVars.Standard));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], single-step\n" +
                                "{0}mode for the script debugger is enabled immediately after creating the\n" +
                                "{0}interpreter.",
                                Characters.HorizontalTab, EnvVars.Step));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}assumptions about the directory layout of the application domain will\n" +
                                "{0}be minimized.",
                                Characters.HorizontalTab, EnvVars.StrictBasePath));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], it will be\n" +
                                "{0}interpreted as the name of the directory containing the stub assembly.",
                                Characters.HorizontalTab, EnvVars.StubPath));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], unhandled\n" +
                                "{0}exceptions are rethrown after being reported.",
                                Characters.HorizontalTab, EnvVars.Throw));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], all tracing\n" +
                                "{0}within the core library will be enabled.  If the \"{2}\" environment\n" +
                                "{0}variable is set [to anything], the \"{1}\" environment variable has no\n" +
                                "{0}effect.",
                                Characters.HorizontalTab, EnvVars.Trace, EnvVars.NoTrace));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], its\n" +
                                "{0}value will be used to set the list of enabled trace categories.  If the\n" +
                                "{0}value cannot be converted to a trace category list, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.TraceCategories));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], its\n" +
                                "{0}value will be used to alter or set the default trace priority mask.  If\n" +
                                "{0}the value cannot be converted to a trace priority mask, it will be\n" +
                                "{0}ignored.",
                                Characters.HorizontalTab, EnvVars.TracePriorities));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], its\n" +
                                "{0}value will be used to alter or set the default trace priority.  If the\n" +
                                "{0}value cannot be converted to a trace priority, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.TracePriority));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to\n" +
                                "{0}anything], its value will be used to alter or set the trace priority\n" +
                                "{0}mask for message frequency limits.  If the value cannot be converted to\n" +
                                "{0}a trace priority mask, it will be ignored.",
                                Characters.HorizontalTab, EnvVars.TracePriorityLimits));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], trace\n" +
                                "{0}listener output may include managed call stack information.",
                                Characters.HorizontalTab, EnvVars.TraceStack));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], trace\n" +
                                "{0}listener output may be sent to the interpreter host.  This option is\n" +
                                "{0}intended for use with custom shells.  The default shell does not make\n" +
                                "{0}use of the interpreter flag set by this option.",
                                Characters.HorizontalTab, EnvVars.TraceToHost));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}trace listener output will always be sent to active trace listeners,\n" +
                                "{0}even if a stream is active.",
                                Characters.HorizontalTab, EnvVars.TraceToListeners));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n"+
                                "{0}attempt to treat the current runtime as thought it were .NET Core.\n"+
                                "{0}This may cause functionality to be disabled and/or malfunction.",
                                Characters.HorizontalTab, EnvVars.TreatAsDotNetCore));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], \n" +
                                "{0}attempt to treat the current runtime as thought it were .NET Framework\n" +
                                "{0}2.0.  This may cause functionality to be disabled and/or malfunction.",
                                Characters.HorizontalTab, EnvVars.TreatAsFramework20));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], \n" +
                                "{0}attempt to treat the current runtime as thought it were .NET Framework\n" +
                                "{0}4.0.  This may cause functionality to be disabled and/or malfunction.",
                                Characters.HorizontalTab, EnvVars.TreatAsFramework40));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], attempt\n" +
                                "{0}to treat the current runtime as thought it were Mono.  This may cause\n" +
                                "{0}functionality to be disabled and/or malfunction.",
                                Characters.HorizontalTab, EnvVars.TreatAsMono));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the\n" +
                                "{0}existing console will be used, if available (i.e. by attaching to it).",
                                Characters.HorizontalTab, EnvVars.UseAttach));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], the\n" +
                                "{0}internal wrapper class will be used for named events.",
                                Characters.HorizontalTab, EnvVars.UseNamedEvents));
                            displayHost.WriteLine();
#if XML
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything],\n" +
                                "{0}application settings from applicable XML files will be favored over\n" +
                                "{0}those provided by the runtime.",
                                Characters.HorizontalTab, EnvVars.UseXmlFiles));
                            displayHost.WriteLine();
#endif
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], it will\n" +
                                "{0}be interpreted as the name of the file or directory where the optional\n" +
                                "{0}native utility library is located.",
                                Characters.HorizontalTab, EnvVars.UtilityPath));
                            displayHost.WriteLine();
                            displayHost.WriteLine(String.Format(
                                "{0}If the \"{1}\" environment variable is set [to anything], it will\n" +
                                "{0}be interpreted as the name of an additional sub-directory within each\n" +
                                "{0}directory searched when attempting to find user-specific and/or\n" +
                                "{0}application-specific files.",
                                Characters.HorizontalTab, EnvVars.VendorPath));
                        }

                        result = String.Empty;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        result = "interpreter host not available";
                    }
                }
                else
                {
                    result = "invalid interpreter";
                }
            }
            catch (Exception e)
            {
                result = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode WritePluginBanner(
            Interpreter interpreter, /* in */
            IPlugin plugin,          /* in */
            ref Result result        /* out */
            )
        {
            try
            {
                return plugin.Banner(interpreter, ref result); /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(HelpOps).Name,
                    TracePriority.InternalError);

                result = null;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetPluginAbout(
            Interpreter interpreter, /* in */
            IPlugin plugin,          /* in */
            bool showCertificate,    /* in */
            ref Result result        /* out */
            )
        {
            try
            {
                ReturnCode code;
                Result localResult = null;

                code = plugin.About(
                    interpreter, ref localResult); /* throw */

                if (code == ReturnCode.Ok)
                {
                    if (showCertificate)
                    {
                        Result certificateResult = Result.Copy(
                            localResult, ResultFlags.CopyObject); /* COPY */

                        if (ScriptOps.CheckSecurityCertificate(
                                interpreter,
                                ref certificateResult) == ReturnCode.Ok)
                        {
                            result = certificateResult;
                        }
                        else
                        {
                            //
                            // HACK: This is not really an error, per se.
                            //
                            TraceOps.DebugTrace(String.Format(
                                "GetPluginAbout: certificateResult = {0}",
                                FormatOps.WrapOrNull(certificateResult)),
                                typeof(HelpOps).Name,
                                TracePriority.SecurityError);

                            result = localResult;
                        }
                    }
                    else
                    {
                        result = localResult;
                    }
                }
                else
                {
                    result = localResult;
                }

                return code;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(HelpOps).Name,
                    TracePriority.InternalError);

                result = null;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeWritePluginsHeader(
            IInteractiveHost interactiveHost, /* in */
            bool showSystem,                  /* in */
            bool showBanner,                  /* in */
            bool showLegalese,                /* in */
            bool showSource,                  /* in */
            bool showUpdate,                  /* in */
            bool showContext,                 /* in */
            bool wrotePlugin,                 /* in */
            ref bool wrote                    /* in, out */
            )
        {
            if (!wrote)
            {
                //
                // NOTE: If we are going to output system plugin information,
                //       make sure to emit the spacer line, if necessary.
                //
                if (showSystem)
                {
                    //
                    // NOTE: The system spacer line is needed if any of the
                    //       previous output sections were enabled.
                    //
                    if (showBanner || showLegalese || showSource ||
                        showUpdate || showContext)
                    {
                        interactiveHost.WriteLine();
                    }
                }
                else
                {
                    //
                    // NOTE: The system spacer line is needed if any of the
                    //       previous output sections were enabled -OR- if
                    //       any [system?] plugin was output.
                    //
                    if (wrotePlugin ||
                        showBanner || showLegalese || showSource ||
                        showUpdate || showContext)
                    {
                        interactiveHost.WriteLine();
                    }
                }

                interactiveHost.WriteLine(String.Format(
                    "Using {0} plugins:", showSystem ? "core" : "loaded"));

                interactiveHost.WriteLine();

                wrote = true;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void WritePluginAbout(
            Interpreter interpreter,  /* in */
            IDisplayHost displayHost, /* in */
            Result aboutResult,       /* in */
            bool positioning,         /* in */
            bool showSystem,          /* in */
            bool showBanner,          /* in */
            bool showLegalese,        /* in */
            bool showSource,          /* in */
            bool showUpdate,          /* in */
            bool showContext,         /* in */
            bool wrotePlugin,         /* in */
            ref bool wrote,           /* in, out */
            ref bool wroteSingleLine, /* in, out */
            ref bool wroteMultiLine,  /* in, out */
            ref bool wroteBox         /* in, out */
            )
        {
            StringPairList aboutList = (aboutResult != null) ?
                aboutResult.Value as StringPairList : null;

            if (aboutList != null)
            {
                MaybeWritePluginsHeader(
                    displayHost, showSystem, showBanner, showLegalese,
                    showSource, showUpdate, showContext, wrotePlugin,
                    ref wrote);

                //
                // NOTE: Boxed about results written to the host will always
                //       occupy multiple lines; emit the spacer line if any
                //       previous plugin about results were output.
                //
                if (wroteSingleLine || wroteMultiLine || wroteBox)
                    displayHost.WriteLine();

                int left = 0;
                int top = 0;

                if (positioning && !displayHost.GetPosition(ref left, ref top))
                {
                    //
                    // NOTE: The interpreter host does support positioning
                    //       -AND- the query for the current position failed;
                    //       skip the output for this plugin.
                    //
                    DebugOps.Complain(interpreter,
                        ReturnCode.Error, "could not get host position");

                    return;
                }

                displayHost.WriteBox(
                    null, aboutList, null, versionWidth, false, false,
                    ref left, ref top);

                displayHost.WriteLine();

                wroteBox = true;
            }
            else
            {
                string aboutString = aboutResult;

                if (!String.IsNullOrEmpty(aboutString))
                {
                    MaybeWritePluginsHeader(
                        displayHost, showSystem, showBanner, showLegalese,
                        showSource, showUpdate, showContext, wrotePlugin,
                        ref wrote);

                    bool isMultiLine = StringOps.IsMultiLine(aboutString);

                    if (isMultiLine)
                    {
                        //
                        // NOTE: The about result for this plugin occupies
                        //       multiple lines and is not a box; emit the
                        //       spacer line if any previous plugin about
                        //       results were output.
                        //
                        if (wroteSingleLine || wroteMultiLine || wroteBox)
                            displayHost.WriteLine();
                    }
                    else
                    {
                        //
                        // NOTE: The about result for this plugin occupies
                        //       a single line and is not a box; emit the
                        //       spacer line only if any previous plugin
                        //       about results occupied multiple lines or
                        //       were boxes.
                        //
                        if (wroteMultiLine || wroteBox)
                            displayHost.WriteLine();
                    }

                    displayHost.WriteLine(aboutString);

                    if (isMultiLine)
                        wroteMultiLine = true;
                    else
                        wroteSingleLine = true;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode WriteVersion(
            Interpreter interpreter, /* in */
            bool showBanner,         /* in */
            bool showLegalese,       /* in */
            bool showSource,         /* in */
            bool showUpdate,         /* in */
            bool showContext,        /* in */
            bool showPlugins,        /* in */
            bool showCertificate,    /* in */
            bool showOptions,        /* in */
            bool compactMode,        /* in */
            ref Result result        /* out */
            )
        {
            try
            {
                if (interpreter != null)
                {
                    IDisplayHost displayHost = GetDisplayHost(interpreter);

                    if (displayHost != null)
                    {
                        if (showBanner)
                        {
                            WriteBanner(
                                interpreter, false, false, false, false,
                                false, false, false, false, compactMode);
                        }

                        if (showLegalese)
                        {
                            if (showBanner)
                                displayHost.WriteLine();

                            WriteLegalese(interpreter, true);
                        }

                        Assembly assembly = showSource || showUpdate ?
                            GlobalState.GetAssembly() : null;

                        if (showSource)
                        {
                            if (showBanner || showLegalese)
                                displayHost.WriteLine();

                            displayHost.WriteLine("Compiled from sources:");
                            displayHost.WriteLine();
                            displayHost.Write(Characters.HorizontalTab);
                            displayHost.WriteLine(String.Format(
                                versionSourceIdFormat,
                                FormatOps.SourceId(assembly,
                                    defaultSourceId)));
                            displayHost.Write(Characters.HorizontalTab);
                            displayHost.WriteLine(String.Format(
                                versionSourceTimeStampFormat,
                                FormatOps.SourceTimeStamp(assembly,
                                    defaultSourceTimeStamp)));
                        }

                        if (showUpdate)
                        {
                            if (showBanner || showLegalese || showSource)
                                displayHost.WriteLine();

                            AssemblyName assemblyName = GlobalState.GetAssemblyName();

                            displayHost.WriteLine("Using update settings:");
                            displayHost.WriteLine();
                            displayHost.Write(Characters.HorizontalTab);
                            displayHost.WriteLine(String.Format(
                                versionUpdateFormat1,
                                FormatOps.PublicKeyToken(assemblyName,
                                    Characters.Asterisk.ToString()),
                                Vars.Package.Name,
                                FormatOps.CultureName(
                                    GlobalState.GetAssemblyCultureInfo(),
                                    false)));
                            displayHost.Write(Characters.HorizontalTab);
                            displayHost.WriteLine(String.Format(
                                versionUpdateFormat2,
                                FormatOps.UpdateUri(assembly, defaultUri),
                                String.Format(
                                    Vars.Platform.UpdatePathAndQueryValue,
                                    GlobalState.GetAssemblyUpdateVersion(),
                                    null)));
                            displayHost.Write(Characters.HorizontalTab);
                            displayHost.WriteLine(String.Format(
                                versionUpdateFormat3,
                                FormatOps.DownloadUri(assembly, defaultUri)));
                        }

                        if (showContext)
                        {
                            if (showBanner || showLegalese || showSource ||
                                showUpdate)
                            {
                                displayHost.WriteLine();
                            }

                            string[] processContexts = {
                                "parent=" + ProcessOps.GetParentId().ToString(),
                                "current=" + ProcessOps.GetId().ToString()
                            };

                            string[] threadContexts = {
                                "primary=" +
                                    interpreter.ThreadId.ToString(),
                                "current=" +
                                    GlobalState.GetCurrentSystemThreadId().ToString()
                            };

                            string[] appDomainContexts = {
                                "primary=" +
                                    AppDomainOps.GetIdString(interpreter.GetAppDomain()),
                                "current=" +
                                    AppDomainOps.GetCurrentId().ToString()
                            };

                            Interpreter parentInterpreter =
                                interpreter.ParentInterpreter; /* ? */

                            string[] interpreterContexts = {
                                "parent=" + ((parentInterpreter != null) ?
                                    parentInterpreter.IdNoThrow.ToString() :
                                    FormatOps.DisplayNull),
                                "current=" + interpreter.IdNoThrow.ToString()
                            };

                            int maximumLength = StringOps.GetMaximumLength(
                                processContexts[0], threadContexts[0],
                                appDomainContexts[0], interpreterContexts[0]);

                            displayHost.WriteLine("Using script context:");
                            displayHost.WriteLine();
                            displayHost.Write(Characters.HorizontalTab);
                            displayHost.WriteLine(String.Format(
                                versionContextFormat1, processContexts[0],
                                processContexts[1], StringOps.StrRepeat(
                                processContexts[0], maximumLength,
                                Characters.Space)));
                            displayHost.Write(Characters.HorizontalTab);
                            displayHost.WriteLine(String.Format(
                                versionContextFormat2, threadContexts[0],
                                threadContexts[1], StringOps.StrRepeat(
                                threadContexts[0], maximumLength,
                                Characters.Space)));
                            displayHost.Write(Characters.HorizontalTab);
                            displayHost.WriteLine(String.Format(
                                versionContextFormat3, appDomainContexts[0],
                                appDomainContexts[1], StringOps.StrRepeat(
                                appDomainContexts[0], maximumLength,
                                Characters.Space)));
                            displayHost.Write(Characters.HorizontalTab);
                            displayHost.WriteLine(String.Format(
                                versionContextFormat4, interpreterContexts[0],
                                interpreterContexts[1], StringOps.StrRepeat(
                                interpreterContexts[0], maximumLength,
                                Characters.Space)));
                        }

                        bool wrotePlugin = false; /* NOTE: Was there a plugin? */

                        if (showPlugins)
                        {
                            PluginWrapperDictionary plugins = interpreter.CopyPlugins();

                            if (plugins != null)
                            {
                                bool positioning = FlagOps.HasFlags(
                                    displayHost.GetHostFlags(), HostFlags.Positioning, true);

                                foreach (bool showSystem in new bool[] { true, false })
                                {
                                    bool wrote = false; /* NOTE: For plugins header. */
                                    bool wroteSingleLine = false;
                                    bool wroteMultiLine = false;
                                    bool wroteBox = false;

                                    foreach (KeyValuePair<string, _Wrappers.Plugin> pair in plugins)
                                    {
                                        IPlugin plugin = pair.Value;

                                        if (plugin == null)
                                            continue;

                                        ///////////////////////////////////////////////////////////////

                                        PluginFlags pluginFlags = EntityOps.GetFlagsNoThrow(
                                            plugin);

                                        if (pluginFlags == PluginFlags.None) // NOTE: Impossible.
                                            continue;

                                        bool isSystem = FlagOps.HasFlags(
                                            pluginFlags, PluginFlags.System, true);

                                        if (isSystem != showSystem)
                                            continue;

                                        bool isPrimary = FlagOps.HasFlags(
                                            pluginFlags, PluginFlags.Primary, true);

                                        ///////////////////////////////////////////////////////////////

                                        ReturnCode aboutCode;
                                        Result aboutResult = null;

                                        aboutCode = GetPluginAbout(
                                            interpreter, plugin, isPrimary && isSystem &&
                                            showCertificate, ref aboutResult);

                                        if (aboutCode == ReturnCode.Ok)
                                        {
                                            WritePluginAbout(
                                                interpreter, displayHost, aboutResult,
                                                positioning, showSystem, showBanner,
                                                showLegalese, showSource, showUpdate,
                                                showContext, wrotePlugin, ref wrote,
                                                ref wroteSingleLine, ref wroteMultiLine,
                                                ref wroteBox);
                                        }
                                    }

                                    if (!wrotePlugin &&
                                        (wroteSingleLine || wroteMultiLine || wroteBox))
                                    {
                                        wrotePlugin = true;
                                    }
                                }
                            }
                        }

                        if (showOptions)
                        {
                            StringList options = DefineConstants.OptionList;

                            if (options != null)
                            {
                                int optionMaximumLength = ListOps.GetMaximumLength(
                                    options);

                                int optionItemsPerLine = GetItemsPerLine(
                                    null, 0, optionMaximumLength, OptionsPerLine,
                                    optionWidth);

                                if (showBanner || showLegalese || showSource ||
                                    showUpdate || showContext || wrotePlugin)
                                {
                                    displayHost.WriteLine();
                                }

                                displayHost.WriteLine("Compiled with options:");
                                displayHost.WriteLine();

                                int count = 0;

                                foreach (string option in options)
                                {
                                    if (!String.IsNullOrEmpty(option))
                                    {
                                        //
                                        // HACK: Right align the option within this column.
                                        //
                                        displayHost.Write(Characters.HorizontalTab);
                                        displayHost.Write(String.Format(
                                            optionFormat.Replace(lengthPlaceholder,
                                            optionMaximumLength.ToString()), option));

                                        if ((++count % optionItemsPerLine) == 0)
                                        {
                                            displayHost.WriteLine();
                                            count = 0;
                                        }
                                    }
                                }

                                if (count > 0)
                                {
                                    displayHost.WriteLine();
                                    count = 0;
                                }
                            }
                        }

                        result = String.Empty;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        result = "interpreter host not available";
                    }
                }
                else
                {
                    result = "invalid interpreter";
                }
            }
            catch (Exception e)
            {
                result = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int GetItemsPerLine(
            string prefix,           /* in */
            int padding,             /* in */
            int maximumItemLength,   /* in */
            int maximumItemsPerLine, /* in */
            int maximumLineLength    /* in */
            )
        {
            int itemsPerLine = maximumItemsPerLine;
            int prefixLength = (prefix != null) ? prefix.Length : 0;

            while (true)
            {
                //
                // NOTE: At the very minimum, there must be at least *one*
                //       item per line.
                //
                if (itemsPerLine <= 1)
                    break;

                //
                // NOTE: Figure out the basic line length using the current
                //       number of items per line, taking into account the
                //       length of the item prefix and column padding, if
                //       any.
                //
                int lineLength = maximumItemLength + prefixLength + padding;

                if (itemsPerLine > 1)
                    lineLength *= itemsPerLine;

                //
                // NOTE: If there are two or more items per line, then add
                //       a character (per item) for the mandatory spacing
                //       between items.  The first item does not count for
                //       the purposes of this calculation.
                //
                if (itemsPerLine > 1)
                    lineLength += (itemsPerLine - 1);

                //
                // NOTE: If the calculated line length now fits within the
                //       allowed maximum, stop now and return the resulting
                //       number of items per line.
                //
                if (lineLength <= maximumLineLength)
                    break;

                //
                // NOTE: Otherwise, reduce the number of items per line by
                //       one and try again.
                //
                itemsPerLine--;
            }

            return itemsPerLine;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if INTERACTIVE_COMMANDS
        private static bool WriteInteractiveHelpColumns(
            IWriteHost writeHost,           /* in */
            IEnumerable<string> collection, /* in */
            string format,                  /* in */
            string prefix,                  /* in */
            int maximumLength,              /* in */
            int countPerLine,               /* in */
            ConsoleColor foregroundColor,   /* in */
            ConsoleColor backgroundColor    /* in */
            )
        {
            int count = 0;
            int prefixLength = (prefix != null) ? prefix.Length : 0;

            foreach (string item in collection)
            {
                if (item == null)
                    continue;

                //
                // HACK: Right align the item name within this column.
                //
                int length = maximumLength - item.Length + columnPadding;

                if (!HostTryWriteColor(writeHost,
                        String.Format(format.Replace(lengthPlaceholder,
                        (length + prefixLength).ToString()), prefix, item),
                        false, foregroundColor, backgroundColor))
                {
                    return false;
                }

                if ((++count % countPerLine) == 0)
                {
                    if (!writeHost.WriteLine())
                        return false;

                    count = 0;
                }
                else if (!writeHost.Write(Characters.Space))
                {
                    return false;
                }
            }

            if (count > 0)
            {
                if (!writeHost.WriteLine())
                    return false;

                count = 0;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int GetDefaultMaximumLength()
        {
            //
            // NOTE: Purposely skip extension commands so we can obtain the
            //       maximum length required for the built-in commands only.
            //
            StringPairDictionary help = GetInteractiveCommandHelp();

            if (help == null)
                return 0;

            return ListOps.GetMaximumLength(help.Keys);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode WriteInteractiveHelp(
            Interpreter interpreter, /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            string topic = null;

            if ((arguments != null) && (arguments.Count >= 2))
                topic = StringOps.NullIfEmpty(arguments[1]);

            CultureInfo cultureInfo = interpreter.InternalCultureInfo;
            bool boolValue; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            bool? showGroups = null;

            if ((arguments != null) && (arguments.Count >= 3))
            {
                boolValue = false;

                if (Value.GetBoolean2(
                        arguments[2], ValueFlags.AnyBoolean, cultureInfo,
                        ref boolValue, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                showGroups = boolValue;
            }

            ///////////////////////////////////////////////////////////////////

            bool? showTopics = null;

            if ((arguments != null) && (arguments.Count >= 4))
            {
                boolValue = false;

                if (Value.GetBoolean2(
                        arguments[3], ValueFlags.AnyBoolean, cultureInfo,
                        ref boolValue, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                showTopics = boolValue;
            }

            ///////////////////////////////////////////////////////////////////

            bool? useInterpreter = null;

            if ((arguments != null) && (arguments.Count >= 5))
            {
                boolValue = false;

                if (Value.GetBoolean2(
                        arguments[4], ValueFlags.AnyBoolean, cultureInfo,
                        ref boolValue, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                useInterpreter = boolValue;
            }

            ///////////////////////////////////////////////////////////////////

            bool? useSyntax = null;

            if ((arguments != null) && (arguments.Count >= 6))
            {
                boolValue = false;

                if (Value.GetBoolean2(
                        arguments[5], ValueFlags.AnyBoolean, cultureInfo,
                        ref boolValue, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                useSyntax = boolValue;
            }

            ///////////////////////////////////////////////////////////////////

            bool? showHeader = null;

            if ((arguments != null) && (arguments.Count >= 7))
            {
                boolValue = false;

                if (Value.GetBoolean2(
                        arguments[6], ValueFlags.AnyBoolean, cultureInfo,
                        ref boolValue, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                showHeader = boolValue;
            }

            ///////////////////////////////////////////////////////////////////

            bool? matchingOnly = null;

            if ((arguments != null) && (arguments.Count >= 8))
            {
                boolValue = false;

                if (Value.GetBoolean2(
                        arguments[7], ValueFlags.AnyBoolean, cultureInfo,
                        ref boolValue, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                matchingOnly = boolValue;
            }

            ///////////////////////////////////////////////////////////////////

            bool found = false; /* NOT USED */

            return WriteInteractiveHelp(
                interpreter, topic, false, false, showGroups, showTopics,
                useInterpreter, useSyntax, showHeader, matchingOnly,
                ref found, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode WriteInteractiveHelp(
            Interpreter interpreter, /* in */
            string topic,            /* in */
            bool noError,            /* in */
            bool noTopic,            /* in */
            bool? showGroups,        /* in */
            bool? showTopics,        /* in */
            bool? useInterpreter,    /* in */
            bool? useSyntax,         /* in */
            bool? showHeader,        /* in */
            bool? matchingOnly,      /* in */
            ref bool found,          /* out */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            IDisplayHost displayHost = GetDisplayHost(interpreter);

            if (displayHost == null)
            {
                result = "interpreter host not available";
                return ReturnCode.Error;
            }

            string pattern;
            string prefix;
            string helpType;

            pattern = MaybeAdjustHelpItemTopic(
                topic, null, out prefix, out helpType);

            bool localShowGroups;

            if (showGroups != null)
                localShowGroups = (bool)showGroups;
            else
                localShowGroups = (prefix != null);

            bool localShowTopics;

            if (showTopics != null)
                localShowTopics = (bool)showTopics;
            else
                localShowTopics = DefaultShowTopics;

            bool localUseInterpreter;

            if (useInterpreter != null)
                localUseInterpreter = (bool)useInterpreter;
            else
                localUseInterpreter = DefaultUseInterpreter;

            bool localUseSyntax;

            if (useSyntax != null)
                localUseSyntax = (bool)useSyntax;
            else
                localUseSyntax = DefaultUseSyntax;

            bool localShowHeader;

            if (showHeader != null)
                localShowHeader = (bool)showHeader;
            else
                localShowHeader = DefaultShowHeader;

            bool localMatchingOnly;

            if (matchingOnly != null)
                localMatchingOnly = (bool)matchingOnly;
            else
                localMatchingOnly = DefaultMatchingOnly;

            StringListDictionary groups = localShowGroups &&
                !localMatchingOnly ? GetCachedInteractiveCommandGroups(
                    interpreter, null, false) : null;

            StringPairDictionary help = localShowTopics ?
                GetCachedInteractiveCommandHelp(
                    interpreter, localMatchingOnly ? pattern : null,
                    false) : null;

            string groupType = "interactive command group"; /* CONST? */
            string topicType = "topic";                     /* CONST? */
            bool noPrefix = false;                          /* CONST? */

            ConsoleColor helpForegroundColor = _ConsoleColor.Default;
            ConsoleColor helpBackgroundColor = _ConsoleColor.Default;

            interpreter.GetHostColors(displayHost, ColorName.Help, false,
                ref helpForegroundColor, ref helpBackgroundColor);

            ConsoleColor helpItemForegroundColor = _ConsoleColor.Default;
            ConsoleColor helpItemBackgroundColor = _ConsoleColor.Default;

            interpreter.GetHostColors(displayHost, ColorName.HelpItem, false,
                ref helpItemForegroundColor, ref helpItemBackgroundColor);

            if (!localMatchingOnly && (topic != null))
            {
                string formatted;
                string type;

                formatted = FormatHelpItem(
                    interpreter, groups, help, groupType, helpType,
                    topic, topicType, noError, noPrefix, false,
                    noTopic, localUseInterpreter, localUseSyntax,
                    out type);

                if (formatted != null)
                {
                    // displayHost.WriteLine();

                    if (type != null)
                    {
                        //
                        // HACK: All uppercase topic types look better
                        //       here?
                        //
                        type = type.ToUpperInvariant();

                        HostTryWriteColor(
                            displayHost, type, true, helpForegroundColor,
                            helpBackgroundColor);

                        displayHost.WriteLine();

                        found = true;
                    }

                    HostTryWriteColor(
                        displayHost, formatted, true, helpItemForegroundColor,
                        helpItemBackgroundColor);

                    // displayHost.WriteLine();
                }
            }
            else
            {
                prefix = noPrefix ?
                    null : ShellOps.InteractiveCommandPrefix;

                if (localShowHeader)
                {
                    char prefixChar = ShellOps.InteractiveCommandPrefixChar;

                    displayHost.WriteLine();

                    HostTryWriteColor(displayHost, String.Format(
                        "WARNING: Please note that all of the following commands work best when invoked\n" +
                        "         interactively (i.e. they cannot be easily or consistently invoked via\n" +
                        "         scripts).\n\n" +
                        "         Built-in interactive commands may be overridden by defining a command\n" +
                        "         or procedure with an identical name.  Overridden built-in interactive\n" +
                        "         commands and interactive extension commands will be listed within the\n" +
                        "         \"extension\" interactive command group.\n\n" +
                        "         The original built-in interactive commands may be invoked, whether or\n" +
                        "         not they have been overridden, by using one extra {0} in front of the\n" +
                        "         built-in interactive command name.\n\n" +
                        "         The interactive command callback, which allows an extension to modify\n" +
                        "         command text entered interactively, may be bypassed by using an extra\n" +
                        "         two {0} in front of any interactive command name, whether built-in or\n" +
                        "         not.\n\n" +
                        "         The following list summarizes how to invoke interactive commands:\n\n" +
                        "         {1}name -- Invoke any command with the callback enabled.\n\n" +
                        "         {1}{1}name -- Invoke built-in command with the callback enabled.\n\n" +
                        "         {1}{1}{1}name -- Invoke any command with the callback disabled.\n\n" +
                        "         {1}{1}{1}{1}name -- Invoke built-in command with the callback disabled.",
                        FormatOps.WrapOrNull(prefixChar), prefixChar),
                        true, helpForegroundColor, helpBackgroundColor);

                    displayHost.WriteLine();

                    HostTryWriteColor(displayHost, String.Format(
                        "Please type \"{0}help ?{1}?\" for more information on a specific {1}.",
                        prefix, topicType), true, helpForegroundColor, helpBackgroundColor);
                }

                if (help != null)
                {
                    int helpMaximumLength = ListOps.GetMaximumLength(
                        help.Keys);

                    int helpItemsPerLine = GetItemsPerLine(
                        prefix, columnPadding, helpMaximumLength,
                        TopicsPerLine, LineWidth);

                    if (groups != null)
                    {
                        int defaultMaximumLength = GetDefaultMaximumLength();

                        int defaultItemsPerLine = GetItemsPerLine(
                            prefix, columnPadding, defaultMaximumLength,
                            TopicsPerLine, LineWidth);

                        StringSortedList keys = new StringSortedList(
                            groups.Keys);

                        if ((keys != null) && (keys.Count > 0))
                        {
                            foreach (string key in keys.Keys)
                            {
                                StringList topics;

                                if (!groups.TryGetValue(key, out topics))
                                    continue;

                                if ((topics == null) || (topics.Count == 0))
                                    continue;

                                int groupMaximumLength = ListOps.GetMaximumLength(
                                    topics);

                                int groupItemsPerLine = GetItemsPerLine(
                                    prefix, columnPadding, groupMaximumLength,
                                    TopicsPerLine, LineWidth);

                                if ((groupItemsPerLine >= helpItemsPerLine) &&
                                    (helpItemsPerLine >= TopicsPerLine))
                                {
                                    if (groupMaximumLength < helpMaximumLength)
                                        groupMaximumLength = helpMaximumLength;

                                    if (groupItemsPerLine > helpItemsPerLine)
                                        groupItemsPerLine = helpItemsPerLine;
                                }
                                else if (groupItemsPerLine >= defaultItemsPerLine)
                                {
                                    if (groupMaximumLength < defaultMaximumLength)
                                        groupMaximumLength = defaultMaximumLength;

                                    if (groupItemsPerLine > defaultItemsPerLine)
                                        groupItemsPerLine = defaultItemsPerLine;
                                }

                                displayHost.WriteLine();

                                HostTryWriteColor(displayHost, String.Format(
                                    "The {0} {1} {2} {3}: ", topics.Count,
                                    localMatchingOnly ? "matching" : "available",
                                    FormatOps.WrapOrNull(key), String.Format(
                                        topics.Count != 1 ? "{0}s are" : "{0} is",
                                        topicType)),
                                    true, helpForegroundColor, helpBackgroundColor);

                                displayHost.WriteLine();

                                WriteInteractiveHelpColumns(displayHost, topics,
                                    topicListFormat, prefix, groupMaximumLength,
                                    groupItemsPerLine, helpItemForegroundColor,
                                    helpItemBackgroundColor);
                            }
                        }
                    }
                    else
                    {
                        StringSortedList keys = new StringSortedList(
                            help.Keys);

                        if ((keys != null) && (keys.Count > 0))
                        {
                            displayHost.WriteLine();

                            HostTryWriteColor(displayHost, String.Format(
                                "The {0} {1} {2}: ", keys.Count,
                                localMatchingOnly ? "matching" : "available",
                                String.Format(
                                    keys.Count != 1 ? "{0}s are" : "{0} is",
                                    topicType)),
                                true, helpForegroundColor, helpBackgroundColor);

                            displayHost.WriteLine();

                            WriteInteractiveHelpColumns(displayHost, keys.Keys,
                                topicListFormat, prefix, helpMaximumLength,
                                helpItemsPerLine, helpItemForegroundColor,
                                helpItemBackgroundColor);
                        }
                    }

                    // displayHost.WriteLine();
                }
                else if (groups != null)
                {
                    StringSortedList keys = new StringSortedList(
                        groups.Keys);

                    if ((keys != null) && (keys.Count > 0))
                    {
                        displayHost.WriteLine();

                        HostTryWriteColor(displayHost, String.Format(
                            "The {0} {1} {2} {3}: ", keys.Count,
                            localMatchingOnly ? "matching" : "available",
                            topicType, keys.Count != 1 ? "groups are" :
                            "group is"), true, helpForegroundColor,
                            helpBackgroundColor);

                        int groupsMaximumLength = ListOps.GetMaximumLength(
                            groups.Keys);

                        int groupsItemsPerLine = GetItemsPerLine(
                            null, columnPadding, groupsMaximumLength,
                            GroupsPerLine, LineWidth);

                        displayHost.WriteLine();

                        WriteInteractiveHelpColumns(displayHost, keys.Keys,
                            groupListFormat, null, groupsMaximumLength,
                            groupsItemsPerLine, helpItemForegroundColor,
                            helpItemBackgroundColor);

                        // displayHost.WriteLine();
                    }
                }
            }

            //
            // NOTE: If we managed to display any help at all, return Ok.
            //
            result = String.Empty;
            return ReturnCode.Ok;
        }
#endif
#endif
        #endregion
    }
}
