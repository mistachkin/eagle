/*
 * Default.cs --
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
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

#if TEST
using TraceException = Eagle._Tests.Default.TraceException;
#endif

//
// TODO: Load all the strings used in this class from resources.
//
namespace Eagle._Hosts
{
    [ObjectId("4a02c60b-5b7b-4eb8-873e-eb6860ba3973")]
    public abstract class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IHost, IDisposable
    {
        #region Protected Constants
        protected internal static readonly BindingFlags HostPropertyBindingFlags =
            ObjectOps.GetBindingFlags(MetaBindingFlags.HostProperty, true);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the "default" beep values, per MSDN.
        //
        protected internal static readonly int BeepFrequency = 800;
        protected internal static readonly int BeepDuration = 200;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These read-only arrays are used internally by the WriteCore
        //       subsystem.  The OnePassForWriteCore array is used to cause
        //       WriteCore to call Write and WriteLine with colors enabled.
        //       The TwoPassesForWriteCore array is used to cause WriteCore
        //       to call Write with colors enabled and then WriteLine with
        //       colors disabled.  The values contained in these arrays are
        //       also hard-coded into the various Should*ForPass methods of
        //       this class.  If any of these values are changed, all of the
        //       Should*ForPass methods of this class (and derived classes?)
        //       must be changed as well.
        //
        protected internal static readonly int[] OnePassForWriteCore = { 0 };
        protected internal static readonly int[] TwoPassesForWriteCore = { 1, 2 };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Section Names
        protected static readonly string HeaderSectionName = "Header";
        protected static readonly string FooterSectionName = "Footer";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Box Names
        protected static readonly string ArgumentInfoBoxName = "ArgumentInfo";
        protected static readonly string CallFrameInfoBoxName = "CallFrameInfo";
        protected static readonly string CallStackInfoBoxName = "CallStackInfo";
        protected static readonly string DebuggerInfoBoxName = "DebuggerInfo";
        protected static readonly string FlagInfoBoxName = "FlagInfo";
        protected static readonly string HostInfoBoxName = "HostInfo";
        protected static readonly string InterpreterInfoBoxName = "InterpreterInfo";
        protected static readonly string EngineInfoBoxName = "EngineInfo";
        protected static readonly string EntityInfoBoxName = "EntityInfo";
        protected static readonly string StackInfoBoxName = "StackInfo";
        protected static readonly string ControlInfoBoxName = "ControlInfo";
        protected static readonly string TestInfoBoxName = "TestInfo";
        protected static readonly string TraceInfoBoxName = "TraceInfo";
        protected static readonly string TokenInfoBoxName = "TokenInfo";
        protected static readonly string VariableInfoBoxName = "VariableInfo";
        protected static readonly string ObjectInfoBoxName = "ObjectInfo";
        protected static readonly string ComplaintInfoBoxName = "ComplaintInfo";
        protected static readonly string HistoryInfoBoxName = "HistoryInfo";
        protected static readonly string CustomInfoBoxName = "CustomInfo";
        protected static readonly string ResultInfoBoxName = "ResultInfo";
        protected static readonly string PreviousResultInfoBoxName = "PreviousResultInfo";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Script Names
        //
        // HACK: This is purposely not read-only.
        //
        protected static IDictionary<string, string> wellKnownDataNames =
            new StringDictionary(new string[]  {

            ///////////////////////////////////////////////////////////////////////////////////////////

            ScriptTypes.Initialization, ScriptTypes.Embedding,
            ScriptTypes.Vendor, ScriptTypes.Startup,
            ScriptTypes.Safe, ScriptTypes.Shell,
            ScriptTypes.Test, ScriptTypes.PackageIndex,
            ScriptTypes.All, ScriptTypes.Constraints,
            ScriptTypes.Epilogue, ScriptTypes.Prologue,

            ///////////////////////////////////////////////////////////////////////////////////////////

            FileNameOnly.Initialization, FileNameOnly.Embedding,
            FileNameOnly.Vendor, FileNameOnly.Startup,
            FileNameOnly.Safe, FileNameOnly.Shell,
            FileNameOnly.Test, FileNameOnly.LibraryPackageIndex,
            FileNameOnly.All, FileNameOnly.Constraints,
            FileNameOnly.Epilogue, FileNameOnly.Prologue,
            /* FileNameOnly.TestPackageIndex, */ /* DUPLICATE */

            ///////////////////////////////////////////////////////////////////////////////////////////

            FileName.Initialization, FileName.Embedding,
            FileName.Vendor, FileName.Startup,
            FileName.Safe, FileName.Shell,
            FileName.Test, FileName.LibraryPackageIndex,
            FileName.All, FileName.Constraints,
            FileName.Epilogue, FileName.Prologue,
            FileName.TestPackageIndex
        }, true, false);
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constants
        public static readonly string BoxColorPrefix = "Box";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static readonly char NoSeparator = char.MinValue;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static readonly bool DefaultCanExit = true;
        public static readonly bool DefaultCanForceExit = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private int boxLevels;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private Default()
        {
            kind = IdentifierKind.Host;
            id = AttributeOps.GetObjectId(this);
            group = AttributeOps.GetObjectGroups(this);

            //
            // NOTE: Set the default display constraints for each section of
            //       the header.
            //
            if (sectionSizes == null)
                sectionSizes = new Dictionary<HeaderFlags, HostFlags>();

            if (sectionSizes.Count == 0)
            {
                sectionSizes.Add(HeaderFlags.StopPrompt, HostFlags.CompactSize);
                sectionSizes.Add(HeaderFlags.GoPrompt, HostFlags.CompactSize);
                sectionSizes.Add(HeaderFlags.AnnouncementInfo, HostFlags.FullSize);
#if DEBUGGER
                sectionSizes.Add(HeaderFlags.DebuggerInfo, HostFlags.FullSize);
#endif
                sectionSizes.Add(HeaderFlags.EngineInfo, HostFlags.FullSize);
                sectionSizes.Add(HeaderFlags.ControlInfo, HostFlags.FullSize);
                sectionSizes.Add(HeaderFlags.EntityInfo, HostFlags.JumboSize);
                sectionSizes.Add(HeaderFlags.StackInfo, HostFlags.JumboSize);
                sectionSizes.Add(HeaderFlags.FlagInfo, HostFlags.FullSize);
                sectionSizes.Add(HeaderFlags.ArgumentInfo, HostFlags.ZeroSize);
                sectionSizes.Add(HeaderFlags.TokenInfo, HostFlags.ZeroSize);
                sectionSizes.Add(HeaderFlags.TraceInfo, HostFlags.ZeroSize);
                sectionSizes.Add(HeaderFlags.InterpreterInfo, HostFlags.JumboSize);
                sectionSizes.Add(HeaderFlags.CallStack, HostFlags.CompactSize);
                sectionSizes.Add(HeaderFlags.CallStackInfo, HostFlags.FullSize);
                sectionSizes.Add(HeaderFlags.VariableInfo, HostFlags.JumboSize);
                sectionSizes.Add(HeaderFlags.ObjectInfo, HostFlags.JumboSize);
                sectionSizes.Add(HeaderFlags.HostInfo, HostFlags.SuperJumboSize);
                sectionSizes.Add(HeaderFlags.TestInfo, HostFlags.JumboSize);
                sectionSizes.Add(HeaderFlags.CallFrameInfo, HostFlags.FullSize);
                sectionSizes.Add(HeaderFlags.ResultInfo, HostFlags.ZeroSize);
#if PREVIOUS_RESULT
                sectionSizes.Add(HeaderFlags.PreviousResultInfo, HostFlags.FullSize);
#endif
                sectionSizes.Add(HeaderFlags.ComplaintInfo, HostFlags.ZeroSize);
#if HISTORY
                sectionSizes.Add(HeaderFlags.HistoryInfo, HostFlags.JumboSize);
#endif
                sectionSizes.Add(HeaderFlags.OtherInfo, HostFlags.FullSize);
                sectionSizes.Add(HeaderFlags.CustomInfo, HostFlags.ZeroSize);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            InitializeBoxCharacterSets();
            SelectBoxCharacterSet();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        protected Default(
            IHostData hostData
            )
            : this()
        {
            if (hostData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, hostData.Group);

                name = hostData.Name;
                description = hostData.Description;
                clientData = hostData.ClientData;

                //
                // NOTE: Use the profile provided by the caller, if any.
                //
                profile = hostData.Profile;

                //
                // NOTE: Use the creation flags provided by the caller, if any.
                //
                hostCreateFlags = hostData.HostCreateFlags;
            }

            //
            // NOTE: Set the defaults for the exit-related properties.  This must
            //       be done after setting the host creation flags as they impact
            //       these properties.
            //
            this.CanExit = DefaultCanExit;
            this.CanForceExit = DefaultCanForceExit;

            //
            // NOTE: Initialize the system default colors for the various elements
            //       of the user-interface.
            //
            InitializeColors();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Properties
        #region Protected Section Properties
        private Dictionary<HeaderFlags, HostFlags> sectionSizes;
        protected virtual Dictionary<HeaderFlags, HostFlags> SectionSizes
        {
            get { return sectionSizes; }
            set { sectionSizes = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Flags Properties
        protected virtual HeaderFlags HeaderFlags
        {
            get { return headerFlags; }
            set { headerFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Call Frame Properties
        #region Call Frame Formatting
        private char variableCallFrameSeparator = '*';
        protected virtual char VariableCallFrameSeparator
        {
            get { return variableCallFrameSeparator; }
            set { variableCallFrameSeparator = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string variableCallFrameSuffix = null;
        protected virtual string VariableCallFrameSuffix
        {
            get { return variableCallFrameSuffix; }
            set { variableCallFrameSuffix = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Call Frame Type Names
        private string aliasCallFrameTypeName = "alis";
        protected virtual string AliasCallFrameTypeName
        {
            get { return aliasCallFrameTypeName; }
            set { aliasCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string currentCallFrameTypeName = "curr";
        protected virtual string CurrentCallFrameTypeName
        {
            get { return currentCallFrameTypeName; }
            set { currentCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string downlevelCallFrameTypeName = "dnlv";
        protected virtual string DownlevelCallFrameTypeName
        {
            get { return downlevelCallFrameTypeName; }
            set { downlevelCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string engineCallFrameTypeName = "engn";
        protected virtual string EngineCallFrameTypeName
        {
            get { return engineCallFrameTypeName; }
            set { engineCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string globalCallFrameTypeName = "glob";
        protected virtual string GlobalCallFrameTypeName
        {
            get { return globalCallFrameTypeName; }
            set { globalCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string globalScopeCallFrameTypeName = "gbsp";
        protected virtual string GlobalScopeCallFrameTypeName
        {
            get { return globalScopeCallFrameTypeName; }
            set { globalScopeCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string lambdaCallFrameTypeName = "lamb";
        protected virtual string LambdaCallFrameTypeName
        {
            get { return lambdaCallFrameTypeName; }
            set { lambdaCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string linkedCallFrameTypeName = "link";
        protected virtual string LinkedCallFrameTypeName
        {
            get { return linkedCallFrameTypeName; }
            set { linkedCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string namespaceCallFrameTypeName = "nmsp";
        protected virtual string NamespaceCallFrameTypeName
        {
            get { return namespaceCallFrameTypeName; }
            set { namespaceCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string nextCallFrameTypeName = "next";
        protected virtual string NextCallFrameTypeName
        {
            get { return nextCallFrameTypeName; }
            set { nextCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Yes, this normally returns only whitespace.
        //
        private string normalCallFrameTypeName = "    ";
        protected virtual string NormalCallFrameTypeName
        {
            get { return normalCallFrameTypeName; }
            set { normalCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string nullCallFrameTypeName = _String.Null;
        protected virtual string NullCallFrameTypeName
        {
            get { return nullCallFrameTypeName; }
            set { nullCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string otherCallFrameTypeName = "othr";
        protected virtual string OtherCallFrameTypeName
        {
            get { return otherCallFrameTypeName; }
            set { otherCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string previousCallFrameTypeName = "prev";
        protected virtual string PreviousCallFrameTypeName
        {
            get { return previousCallFrameTypeName; }
            set { previousCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string procedureCallFrameTypeName = "proc";
        protected virtual string ProcedureCallFrameTypeName
        {
            get { return procedureCallFrameTypeName; }
            set { procedureCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string scopeCallFrameTypeName = "scop";
        protected virtual string ScopeCallFrameTypeName
        {
            get { return scopeCallFrameTypeName; }
            set { scopeCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string trackingCallFrameTypeName = "trck";
        protected virtual string TrackingCallFrameTypeName
        {
            get { return trackingCallFrameTypeName; }
            set { trackingCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string unknownCallFrameTypeName = "unkn";
        protected virtual string UnknownCallFrameTypeName
        {
            get { return unknownCallFrameTypeName; }
            set { unknownCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string uplevelCallFrameTypeName = "uplv";
        protected virtual string UplevelCallFrameTypeName
        {
            get { return uplevelCallFrameTypeName; }
            set { uplevelCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string variableCallFrameTypeName = "vars";
        protected virtual string VariableCallFrameTypeName
        {
            get { return variableCallFrameTypeName; }
            set { variableCallFrameTypeName = value; }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Formatting Properties
        protected virtual bool Debug
        {
            get { return HasCreateFlags(HostCreateFlags.Debug, true); }
            set { MaybeEnableCreateFlags(HostCreateFlags.Debug, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool ReplaceNewLines
        {
            get { return HasCreateFlags(HostCreateFlags.ReplaceNewLines, true); }
            set { MaybeEnableCreateFlags(HostCreateFlags.ReplaceNewLines, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool Ellipsis
        {
            get { return HasCreateFlags(HostCreateFlags.Ellipsis, true); }
            set { MaybeEnableCreateFlags(HostCreateFlags.Ellipsis, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool Exceptions
        {
            get { return HasCreateFlags(HostCreateFlags.Exceptions, true); }
            set { MaybeEnableCreateFlags(HostCreateFlags.Exceptions, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool Display
        {
            get { return HasCreateFlags(HostCreateFlags.Display, true); }
            set { MaybeEnableCreateFlags(HostCreateFlags.Display, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int historyLimit = 20;
        protected virtual int HistoryLimit
        {
            get { return historyLimit; }
            set { historyLimit = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int callStackLimit = 20;
        protected virtual int CallStackLimit
        {
            get { return callStackLimit; }
            set { callStackLimit = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int sectionsPerRow = 0;
        protected virtual int SectionsPerRow
        {
            get { return sectionsPerRow; }
            set { sectionsPerRow = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string nameValueFormat = "{0}:" + Characters.Space + "{1}";
        protected virtual string NameValueFormat
        {
            get { return nameValueFormat; }
            set { nameValueFormat = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Prompt Properties
        private string stopPrompt = "[stop]";
        protected virtual string StopPrompt
        {
            get { return stopPrompt; }
            set { stopPrompt = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string goPrompt = "[go]";
        protected virtual string GoPrompt
        {
            get { return goPrompt; }
            set { goPrompt = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Output Properties
        #region Output Style Properties
        private OutputStyle outputStyle = OutputStyle.Default;
        protected internal virtual OutputStyle OutputStyle // TODO: Make this part of IHost?
        {
            get { return outputStyle; }
            set { outputStyle = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool IsNoneOutputStyle(
            OutputStyle outputStyle
            )
        {
            return !IsFormattedOutputStyle(outputStyle) &&
                !IsBoxedOutputStyle(outputStyle);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool IsFormattedOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.Formatted, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool IsBoxedOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.Boxed, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool IsNormalOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.Normal, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool IsDebugOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.Debug, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool IsErrorOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.Error, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool IsReversedTextOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.ReversedText, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool IsReversedBorderOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.ReversedBorder, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual HostWriteType OutputStyleToHostWriteType(
            OutputStyle outputStyle
            )
        {
            if (IsNormalOutputStyle(outputStyle))
                return HostWriteType.Normal;
            else if (IsDebugOutputStyle(outputStyle))
                return HostWriteType.Debug;
            else if (IsErrorOutputStyle(outputStyle))
                return HostWriteType.Error;
            else
                return HostWriteType.Default;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Window Properties
        private int hostLeft = 0;
        protected virtual int HostLeft
        {
            get { return hostLeft; }
            set { hostLeft = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int hostTop = 0;
        protected virtual int HostTop
        {
            get { return hostTop; }
            set { hostTop = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int windowWidth = 80;
        protected virtual int WindowWidth
        {
            get { return windowWidth; }
            set { windowWidth = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int windowHeight = 25;
        protected virtual int WindowHeight
        {
            get { return windowHeight; }
            set { windowHeight = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Content Area Properties
        private int contentMargin = 0;
        protected virtual int ContentMargin
        {
            get { return contentMargin; }
            set { contentMargin = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int contentWidth = Width.Invalid;
        protected virtual int ContentWidth
        {
            get { return (contentWidth != Width.Invalid) ? contentWidth : WindowWidth - 3; }
            set { contentWidth = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int contentThreshold = 20;
        protected virtual int ContentThreshold
        {
            get { return contentThreshold; }
            set { contentThreshold = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int minimumLength = Length.Invalid;
        protected virtual int MinimumLength
        {
            get { return minimumLength; }
            set { minimumLength = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Box Properties
        private int boxCharacterSet;
        protected internal virtual int BoxCharacterSet
        {
            get { return boxCharacterSet; }
            set { boxCharacterSet = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private StringList boxCharacterSets;
        protected internal virtual StringList BoxCharacterSets
        {
            get { return boxCharacterSets; }
            set { boxCharacterSets = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int boxWidth = Width.Invalid;
        protected virtual int BoxWidth
        {
            get { return boxWidth; }
            set { boxWidth = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int boxMargin = 2;
        protected virtual int BoxMargin
        {
            get { return boxMargin; }
            set { boxMargin = value; }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Color Properties
        #region General & Section Colors
        #region General Colors
        private ConsoleColor bannerForegroundColor;
        protected virtual ConsoleColor BannerForegroundColor
        {
            get { return bannerForegroundColor; }
            set { bannerForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor bannerBackgroundColor;
        protected virtual ConsoleColor BannerBackgroundColor
        {
            get { return bannerBackgroundColor; }
            set { bannerBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor boxForegroundColor;
        protected virtual ConsoleColor BoxForegroundColor
        {
            get { return boxForegroundColor; }
            set { boxForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor boxBackgroundColor;
        protected virtual ConsoleColor BoxBackgroundColor
        {
            get { return boxBackgroundColor; }
            set { boxBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor debugForegroundColor;
        protected virtual ConsoleColor DebugForegroundColor
        {
            get { return debugForegroundColor; }
            set { debugForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor debugBackgroundColor;
        protected virtual ConsoleColor DebugBackgroundColor
        {
            get { return debugBackgroundColor; }
            set { debugBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor defaultForegroundColor;
        protected virtual ConsoleColor DefaultForegroundColor
        {
            get { return defaultForegroundColor; }
            set { defaultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor defaultBackgroundColor;
        protected virtual ConsoleColor DefaultBackgroundColor
        {
            get { return defaultBackgroundColor; }
            set { defaultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor disabledForegroundColor;
        protected virtual ConsoleColor DisabledForegroundColor
        {
            get { return disabledForegroundColor; }
            set { disabledForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor disabledBackgroundColor;
        protected virtual ConsoleColor DisabledBackgroundColor
        {
            get { return disabledBackgroundColor; }
            set { disabledBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor enabledForegroundColor;
        protected virtual ConsoleColor EnabledForegroundColor
        {
            get { return enabledForegroundColor; }
            set { enabledForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor enabledBackgroundColor;
        protected virtual ConsoleColor EnabledBackgroundColor
        {
            get { return enabledBackgroundColor; }
            set { enabledBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor errorForegroundColor;
        protected virtual ConsoleColor ErrorForegroundColor
        {
            get { return errorForegroundColor; }
            set { errorForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor errorBackgroundColor;
        protected virtual ConsoleColor ErrorBackgroundColor
        {
            get { return errorBackgroundColor; }
            set { errorBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor fatalForegroundColor;
        protected virtual ConsoleColor FatalForegroundColor
        {
            get { return fatalForegroundColor; }
            set { fatalForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor fatalBackgroundColor;
        protected virtual ConsoleColor FatalBackgroundColor
        {
            get { return fatalBackgroundColor; }
            set { fatalBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor footerForegroundColor;
        protected virtual ConsoleColor FooterForegroundColor
        {
            get { return footerForegroundColor; }
            set { footerForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor footerBackgroundColor;
        protected virtual ConsoleColor FooterBackgroundColor
        {
            get { return footerBackgroundColor; }
            set { footerBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor headerForegroundColor;
        protected virtual ConsoleColor HeaderForegroundColor
        {
            get { return headerForegroundColor; }
            set { headerForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor headerBackgroundColor;
        protected virtual ConsoleColor HeaderBackgroundColor
        {
            get { return headerBackgroundColor; }
            set { headerBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor helpForegroundColor;
        protected virtual ConsoleColor HelpForegroundColor
        {
            get { return helpForegroundColor; }
            set { helpForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor helpBackgroundColor;
        protected virtual ConsoleColor HelpBackgroundColor
        {
            get { return helpBackgroundColor; }
            set { helpBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor helpItemForegroundColor;
        protected virtual ConsoleColor HelpItemForegroundColor
        {
            get { return helpItemForegroundColor; }
            set { helpItemForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor helpItemBackgroundColor;
        protected virtual ConsoleColor HelpItemBackgroundColor
        {
            get { return helpItemBackgroundColor; }
            set { helpItemBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor legalForegroundColor;
        protected virtual ConsoleColor LegalForegroundColor
        {
            get { return legalForegroundColor; }
            set { legalForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor legalBackgroundColor;
        protected virtual ConsoleColor LegalBackgroundColor
        {
            get { return legalBackgroundColor; }
            set { legalBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor officialForegroundColor;
        protected virtual ConsoleColor OfficialForegroundColor
        {
            get { return officialForegroundColor; }
            set { officialForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor officialBackgroundColor;
        protected virtual ConsoleColor OfficialBackgroundColor
        {
            get { return officialBackgroundColor; }
            set { officialBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor resultForegroundColor;
        protected virtual ConsoleColor ResultForegroundColor
        {
            get { return resultForegroundColor; }
            set { resultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor resultBackgroundColor;
        protected virtual ConsoleColor ResultBackgroundColor
        {
            get { return resultBackgroundColor; }
            set { resultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor stableForegroundColor;
        protected virtual ConsoleColor StableForegroundColor
        {
            get { return stableForegroundColor; }
            set { stableForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor stableBackgroundColor;
        protected virtual ConsoleColor StableBackgroundColor
        {
            get { return stableBackgroundColor; }
            set { stableBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor trustedForegroundColor;
        protected virtual ConsoleColor TrustedForegroundColor
        {
            get { return trustedForegroundColor; }
            set { trustedForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor trustedBackgroundColor;
        protected virtual ConsoleColor TrustedBackgroundColor
        {
            get { return trustedBackgroundColor; }
            set { trustedBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor undefinedForegroundColor;
        protected virtual ConsoleColor UndefinedForegroundColor
        {
            get { return undefinedForegroundColor; }
            set { undefinedForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor undefinedBackgroundColor;
        protected virtual ConsoleColor UndefinedBackgroundColor
        {
            get { return undefinedBackgroundColor; }
            set { undefinedBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor unofficialForegroundColor;
        protected virtual ConsoleColor UnofficialForegroundColor
        {
            get { return unofficialForegroundColor; }
            set { unofficialForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor unofficialBackgroundColor;
        protected virtual ConsoleColor UnofficialBackgroundColor
        {
            get { return unofficialBackgroundColor; }
            set { unofficialBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor unstableForegroundColor;
        protected virtual ConsoleColor UnstableForegroundColor
        {
            get { return unstableForegroundColor; }
            set { unstableForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor unstableBackgroundColor;
        protected virtual ConsoleColor UnstableBackgroundColor
        {
            get { return unstableBackgroundColor; }
            set { unstableBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor untrustedForegroundColor;
        protected virtual ConsoleColor UntrustedForegroundColor
        {
            get { return untrustedForegroundColor; }
            set { untrustedForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor untrustedBackgroundColor;
        protected virtual ConsoleColor UntrustedBackgroundColor
        {
            get { return untrustedBackgroundColor; }
            set { untrustedBackgroundColor = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Section Colors
        private ConsoleColor announcementInfoForegroundColor;
        protected virtual ConsoleColor AnnouncementInfoForegroundColor
        {
            get { return announcementInfoForegroundColor; }
            set { announcementInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor announcementInfoBackgroundColor;
        protected virtual ConsoleColor AnnouncementInfoBackgroundColor
        {
            get { return announcementInfoBackgroundColor; }
            set { announcementInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor argumentInfoForegroundColor;
        protected virtual ConsoleColor ArgumentInfoForegroundColor
        {
            get { return argumentInfoForegroundColor; }
            set { argumentInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor argumentInfoBackgroundColor;
        protected virtual ConsoleColor ArgumentInfoBackgroundColor
        {
            get { return argumentInfoBackgroundColor; }
            set { argumentInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor callFrameInfoForegroundColor;
        protected virtual ConsoleColor CallFrameInfoForegroundColor
        {
            get { return callFrameInfoForegroundColor; }
            set { callFrameInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor callFrameInfoBackgroundColor;
        protected virtual ConsoleColor CallFrameInfoBackgroundColor
        {
            get { return callFrameInfoBackgroundColor; }
            set { callFrameInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor callStackInfoForegroundColor;
        protected virtual ConsoleColor CallStackInfoForegroundColor
        {
            get { return callStackInfoForegroundColor; }
            set { callStackInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor callStackInfoBackgroundColor;
        protected virtual ConsoleColor CallStackInfoBackgroundColor
        {
            get { return callStackInfoBackgroundColor; }
            set { callStackInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor complaintInfoForegroundColor;
        protected virtual ConsoleColor ComplaintInfoForegroundColor
        {
            get { return complaintInfoForegroundColor; }
            set { complaintInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor complaintInfoBackgroundColor;
        protected virtual ConsoleColor ComplaintInfoBackgroundColor
        {
            get { return complaintInfoBackgroundColor; }
            set { complaintInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor controlInfoForegroundColor;
        protected virtual ConsoleColor ControlInfoForegroundColor
        {
            get { return controlInfoForegroundColor; }
            set { controlInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor controlInfoBackgroundColor;
        protected virtual ConsoleColor ControlInfoBackgroundColor
        {
            get { return controlInfoBackgroundColor; }
            set { controlInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor customInfoForegroundColor;
        protected virtual ConsoleColor CustomInfoForegroundColor
        {
            get { return customInfoForegroundColor; }
            set { customInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor customInfoBackgroundColor;
        protected virtual ConsoleColor CustomInfoBackgroundColor
        {
            get { return customInfoBackgroundColor; }
            set { customInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor debuggerInfoForegroundColor;
        protected virtual ConsoleColor DebuggerInfoForegroundColor
        {
            get { return debuggerInfoForegroundColor; }
            set { debuggerInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor debuggerInfoBackgroundColor;
        protected virtual ConsoleColor DebuggerInfoBackgroundColor
        {
            get { return debuggerInfoBackgroundColor; }
            set { debuggerInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor engineInfoForegroundColor;
        protected virtual ConsoleColor EngineInfoForegroundColor
        {
            get { return engineInfoForegroundColor; }
            set { engineInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor engineInfoBackgroundColor;
        protected virtual ConsoleColor EngineInfoBackgroundColor
        {
            get { return engineInfoBackgroundColor; }
            set { engineInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor entityInfoForegroundColor;
        protected virtual ConsoleColor EntityInfoForegroundColor
        {
            get { return entityInfoForegroundColor; }
            set { entityInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor entityInfoBackgroundColor;
        protected virtual ConsoleColor EntityInfoBackgroundColor
        {
            get { return entityInfoBackgroundColor; }
            set { entityInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor flagInfoForegroundColor;
        protected virtual ConsoleColor FlagInfoForegroundColor
        {
            get { return flagInfoForegroundColor; }
            set { flagInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor flagInfoBackgroundColor;
        protected virtual ConsoleColor FlagInfoBackgroundColor
        {
            get { return flagInfoBackgroundColor; }
            set { flagInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor historyInfoForegroundColor;
        protected virtual ConsoleColor HistoryInfoForegroundColor
        {
            get { return historyInfoForegroundColor; }
            set { historyInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor historyInfoBackgroundColor;
        protected virtual ConsoleColor HistoryInfoBackgroundColor
        {
            get { return historyInfoBackgroundColor; }
            set { historyInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor hostInfoForegroundColor;
        protected virtual ConsoleColor HostInfoForegroundColor
        {
            get { return hostInfoForegroundColor; }
            set { hostInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor hostInfoBackgroundColor;
        protected virtual ConsoleColor HostInfoBackgroundColor
        {
            get { return hostInfoBackgroundColor; }
            set { hostInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor interpreterInfoForegroundColor;
        protected virtual ConsoleColor InterpreterInfoForegroundColor
        {
            get { return interpreterInfoForegroundColor; }
            set { interpreterInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor interpreterInfoBackgroundColor;
        protected virtual ConsoleColor InterpreterInfoBackgroundColor
        {
            get { return interpreterInfoBackgroundColor; }
            set { interpreterInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor objectInfoForegroundColor;
        protected virtual ConsoleColor ObjectInfoForegroundColor
        {
            get { return objectInfoForegroundColor; }
            set { objectInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor objectInfoBackgroundColor;
        protected virtual ConsoleColor ObjectInfoBackgroundColor
        {
            get { return objectInfoBackgroundColor; }
            set { objectInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor testInfoForegroundColor;
        protected virtual ConsoleColor TestInfoForegroundColor
        {
            get { return testInfoForegroundColor; }
            set { testInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor testInfoBackgroundColor;
        protected virtual ConsoleColor TestInfoBackgroundColor
        {
            get { return testInfoBackgroundColor; }
            set { testInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor tokenInfoForegroundColor;
        protected virtual ConsoleColor TokenInfoForegroundColor
        {
            get { return tokenInfoForegroundColor; }
            set { tokenInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor tokenInfoBackgroundColor;
        protected virtual ConsoleColor TokenInfoBackgroundColor
        {
            get { return tokenInfoBackgroundColor; }
            set { tokenInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor traceInfoForegroundColor;
        protected virtual ConsoleColor TraceInfoForegroundColor
        {
            get { return traceInfoForegroundColor; }
            set { traceInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor traceInfoBackgroundColor;
        protected virtual ConsoleColor TraceInfoBackgroundColor
        {
            get { return traceInfoBackgroundColor; }
            set { traceInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor variableInfoForegroundColor;
        protected virtual ConsoleColor VariableInfoForegroundColor
        {
            get { return variableInfoForegroundColor; }
            set { variableInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor variableInfoBackgroundColor;
        protected virtual ConsoleColor VariableInfoBackgroundColor
        {
            get { return variableInfoBackgroundColor; }
            set { variableInfoBackgroundColor = value; }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Return Code Colors
        private ConsoleColor okResultForegroundColor;
        protected virtual ConsoleColor OkResultForegroundColor
        {
            get { return okResultForegroundColor; }
            set { okResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor okResultBackgroundColor;
        protected virtual ConsoleColor OkResultBackgroundColor
        {
            get { return okResultBackgroundColor; }
            set { okResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor errorResultForegroundColor;
        protected virtual ConsoleColor ErrorResultForegroundColor
        {
            get { return errorResultForegroundColor; }
            set { errorResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor errorResultBackgroundColor;
        protected virtual ConsoleColor ErrorResultBackgroundColor
        {
            get { return errorResultBackgroundColor; }
            set { errorResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor otherOkResultForegroundColor;
        protected virtual ConsoleColor OtherOkResultForegroundColor
        {
            get { return otherOkResultForegroundColor; }
            set { otherOkResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor otherOkResultBackgroundColor;
        protected virtual ConsoleColor OtherOkResultBackgroundColor
        {
            get { return otherOkResultBackgroundColor; }
            set { otherOkResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor otherErrorResultForegroundColor;
        protected virtual ConsoleColor OtherErrorResultForegroundColor
        {
            get { return otherErrorResultForegroundColor; }
            set { otherErrorResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor otherErrorResultBackgroundColor;
        protected virtual ConsoleColor OtherErrorResultBackgroundColor
        {
            get { return otherErrorResultBackgroundColor; }
            set { otherErrorResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor returnResultForegroundColor;
        protected virtual ConsoleColor ReturnResultForegroundColor
        {
            get { return returnResultForegroundColor; }
            set { returnResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor returnResultBackgroundColor;
        protected virtual ConsoleColor ReturnResultBackgroundColor
        {
            get { return returnResultBackgroundColor; }
            set { returnResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor breakResultForegroundColor;
        protected virtual ConsoleColor BreakResultForegroundColor
        {
            get { return breakResultForegroundColor; }
            set { breakResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor breakResultBackgroundColor;
        protected virtual ConsoleColor BreakResultBackgroundColor
        {
            get { return breakResultBackgroundColor; }
            set { breakResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor continueResultForegroundColor;
        protected virtual ConsoleColor ContinueResultForegroundColor
        {
            get { return continueResultForegroundColor; }
            set { continueResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor continueResultBackgroundColor;
        protected virtual ConsoleColor ContinueResultBackgroundColor
        {
            get { return continueResultBackgroundColor; }
            set { continueResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor whatIfResultForegroundColor;
        protected virtual ConsoleColor WhatIfResultForegroundColor
        {
            get { return whatIfResultForegroundColor; }
            set { whatIfResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor whatIfResultBackgroundColor;
        protected virtual ConsoleColor WhatIfResultBackgroundColor
        {
            get { return whatIfResultBackgroundColor; }
            set { whatIfResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor exceptionResultForegroundColor;
        protected virtual ConsoleColor ExceptionResultForegroundColor
        {
            get { return exceptionResultForegroundColor; }
            set { exceptionResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor exceptionResultBackgroundColor;
        protected virtual ConsoleColor ExceptionResultBackgroundColor
        {
            get { return exceptionResultBackgroundColor; }
            set { exceptionResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor nullResultForegroundColor;
        protected virtual ConsoleColor NullResultForegroundColor
        {
            get { return nullResultForegroundColor; }
            set { nullResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor nullResultBackgroundColor;
        protected virtual ConsoleColor NullResultBackgroundColor
        {
            get { return nullResultBackgroundColor; }
            set { nullResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor emptyResultForegroundColor;
        protected virtual ConsoleColor EmptyResultForegroundColor
        {
            get { return emptyResultForegroundColor; }
            set { emptyResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor emptyResultBackgroundColor;
        protected virtual ConsoleColor EmptyResultBackgroundColor
        {
            get { return emptyResultBackgroundColor; }
            set { emptyResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor unknownResultForegroundColor;
        protected virtual ConsoleColor UnknownResultForegroundColor
        {
            get { return unknownResultForegroundColor; }
            set { unknownResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor unknownResultBackgroundColor;
        protected virtual ConsoleColor UnknownResultBackgroundColor
        {
            get { return unknownResultBackgroundColor; }
            set { unknownResultBackgroundColor = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Call Frame Colors
        private ConsoleColor nullCallFrameForegroundColor;
        protected virtual ConsoleColor NullCallFrameForegroundColor
        {
            get { return nullCallFrameForegroundColor; }
            set { nullCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor unknownCallFrameForegroundColor;
        protected virtual ConsoleColor UnknownCallFrameForegroundColor
        {
            get { return unknownCallFrameForegroundColor; }
            set { unknownCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor variableCallFrameForegroundColor;
        protected virtual ConsoleColor VariableCallFrameForegroundColor
        {
            get { return variableCallFrameForegroundColor; }
            set { variableCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor uplevelCallFrameForegroundColor;
        protected virtual ConsoleColor UplevelCallFrameForegroundColor
        {
            get { return uplevelCallFrameForegroundColor; }
            set { uplevelCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor downlevelCallFrameForegroundColor;
        protected virtual ConsoleColor DownlevelCallFrameForegroundColor
        {
            get { return downlevelCallFrameForegroundColor; }
            set { downlevelCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor trackingCallFrameForegroundColor;
        protected virtual ConsoleColor TrackingCallFrameForegroundColor
        {
            get { return trackingCallFrameForegroundColor; }
            set { trackingCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor engineCallFrameForegroundColor;
        protected virtual ConsoleColor EngineCallFrameForegroundColor
        {
            get { return engineCallFrameForegroundColor; }
            set { engineCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor currentCallFrameForegroundColor;
        protected virtual ConsoleColor CurrentCallFrameForegroundColor
        {
            get { return currentCallFrameForegroundColor; }
            set { currentCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor procedureCallFrameForegroundColor;
        protected virtual ConsoleColor ProcedureCallFrameForegroundColor
        {
            get { return procedureCallFrameForegroundColor; }
            set { procedureCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor lambdaCallFrameForegroundColor;
        protected virtual ConsoleColor LambdaCallFrameForegroundColor
        {
            get { return lambdaCallFrameForegroundColor; }
            set { lambdaCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor scopeCallFrameForegroundColor;
        protected virtual ConsoleColor ScopeCallFrameForegroundColor
        {
            get { return scopeCallFrameForegroundColor; }
            set { scopeCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor aliasCallFrameForegroundColor;
        protected virtual ConsoleColor AliasCallFrameForegroundColor
        {
            get { return aliasCallFrameForegroundColor; }
            set { aliasCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor globalCallFrameForegroundColor;
        protected virtual ConsoleColor GlobalCallFrameForegroundColor
        {
            get { return globalCallFrameForegroundColor; }
            set { globalCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor globalScopeCallFrameForegroundColor;
        protected virtual ConsoleColor GlobalScopeCallFrameForegroundColor
        {
            get { return globalScopeCallFrameForegroundColor; }
            set { globalScopeCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor linkedCallFrameForegroundColor;
        protected virtual ConsoleColor LinkedCallFrameForegroundColor
        {
            get { return linkedCallFrameForegroundColor; }
            set { linkedCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor namespaceCallFrameForegroundColor;
        protected virtual ConsoleColor NamespaceCallFrameForegroundColor
        {
            get { return namespaceCallFrameForegroundColor; }
            set { namespaceCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor normalCallFrameForegroundColor;
        protected virtual ConsoleColor NormalCallFrameForegroundColor
        {
            get { return normalCallFrameForegroundColor; }
            set { normalCallFrameForegroundColor = value; }
        }
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Header Flags Support
        private HeaderFlags PrivateGetHeaderFlags()
        {
            return headerFlags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual HostFlags MaybeInitializeHostFlags()
        {
            if (hostFlags == HostFlags.Invalid)
            {
                //
                // NOTE: We support the WriteErrorLine, WriteDebugLine,
                //       CanExit, CanForceExit, and Exiting properties.
                //
                hostFlags = HostFlags.Complain | HostFlags.Debug |
                            HostFlags.Exit;
            }

            return hostFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool IsExiting()
        {
            return HasCreateFlags(HostCreateFlags.Exiting, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void SetExiting(
            bool exiting
            )
        {
            MaybeEnableCreateFlags(HostCreateFlags.Exiting, exiting);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool ShouldTreatAsFatalError()
        {
            //
            // TODO: In the future, add additional ways of checking for
            //       a "fatal" (i.e. unrecoverable) error here.
            //
            if (FlagOps.HasFlags(
                    MaybeInitializeHostFlags(), HostFlags.TreatAsFatalError,
                    true))
            {
                return true;
            }

            //
            // NOTE: In general, when an error is encountered during an
            //       attempt to exit, it is not recoverable.
            //
            if (IsExiting())
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual void SetTreatAsFatalError(
            bool treatAsFatalError
            )
        {
            if (treatAsFatalError)
                hostFlags |= HostFlags.TreatAsFatalError;
            else
                hostFlags &= ~HostFlags.TreatAsFatalError;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool IsVerboseMode()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.Verbose, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void SetReadException(
            bool exception
            )
        {
            if (exception)
                hostFlags |= HostFlags.ReadException;
            else
                hostFlags &= ~HostFlags.ReadException;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void SetWriteException(
            bool exception
            )
        {
            if (exception)
                hostFlags |= HostFlags.WriteException;
            else
                hostFlags &= ~HostFlags.WriteException;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Creation Flags Support
        protected virtual bool HasCreateFlags(
            HostCreateFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(hostCreateFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void MaybeEnableCreateFlags(
            HostCreateFlags flags,
            bool enable
            )
        {
            if (enable)
                hostCreateFlags |= flags;
            else
                hostCreateFlags &= ~flags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Break-On-Exiting Support
#if BREAK_ON_EXITING
        protected virtual void CheckOkResultIfExiting(
            ReturnCode returnCode,
            Result result
            )
        {
            //
            // NOTE: This code was primarily designed to catch termination
            //       and/or resource disposal errors encountered during the
            //       disposal of the Interpreter class (e.g. those raised
            //       by plugins).  Originally, this code was added while
            //       tracking down a particular interpreter disposal issue
            //       and it was intended to be temporary; however, it ended
            //       up being far too useful to remove.
            //
            // NOTE: If (the host) is exiting, complain loudly about non-Ok
            //       return codes.
            //
            bool savedExiting = IsExiting();

            if (savedExiting)
            {
                //
                // NOTE: Prevent infinite recursion because the DebugOps
                //       methods call this function.
                //
                SetExiting(false);

                try
                {
                    if (returnCode != ReturnCode.Ok)
                    {
                        //
                        // NOTE: Grab metadata for the current method.
                        //
                        MethodBase methodBase = DebugOps.GetMethod(0);

                        //
                        // NOTE: Is a debugger attached to this process?
                        //
                        if (DebugOps.IsAttached())
                        {
                            //
                            // NOTE: Break into the attached debugger.
                            //
                            DebugOps.Break(null, this, methodBase, false);
                        }
                        else
                        {
                            //
                            // NOTE: Otherwise, just complain loudly.
                            //
                            string message = String.Format(
                                "received bad return code {0} while exiting",
                                returnCode);

                            DebugOps.Fail(
                                null, this, methodBase, message, result,
                                false);
                        }
                    }
                }
                finally
                {
                    SetExiting(savedExiting);
                }
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Detail Flags Support
        protected virtual bool HasEmptyContent(
            DetailFlags detailFlags
            )
        {
            return HostOps.HasEmptyContent(detailFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void HeaderFlagsToDetailFlags(
            HeaderFlags headerFlags,
            ref DetailFlags detailFlags
            )
        {
            HostOps.HeaderFlagsToDetailFlags(headerFlags, ref detailFlags);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Color Helper Methods
        #region Color Initialization Methods
        protected virtual ConsoleColor GetColor(
            ConsoleColor color
            )
        {
            //
            // BUGFIX: When the NoColor property is true, disable all colors;
            //         otherwise, just return the color passed in by the caller.
            //
            return NoColor ? _ConsoleColor.None : color;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual void InitializeColors()
        {
            #region General Colors
            bannerForegroundColor = GetColor(ConsoleColor.White);
            bannerBackgroundColor = GetColor(_ConsoleColor.None);
            boxForegroundColor = GetColor(ConsoleColor.White);
            boxBackgroundColor = GetColor(ConsoleColor.DarkGreen);
            debugForegroundColor = GetColor(ConsoleColor.White);
            debugBackgroundColor = GetColor(_ConsoleColor.None);
            defaultForegroundColor = GetColor(_ConsoleColor.None);
            defaultBackgroundColor = GetColor(_ConsoleColor.None);
            disabledForegroundColor = GetColor(ConsoleColor.White);
            disabledBackgroundColor = GetColor(ConsoleColor.DarkRed);
            enabledForegroundColor = GetColor(ConsoleColor.White);
            enabledBackgroundColor = GetColor(ConsoleColor.DarkGreen);
            errorForegroundColor = GetColor(ConsoleColor.Red);
            errorBackgroundColor = GetColor(ConsoleColor.White);
            fatalForegroundColor = GetColor(ConsoleColor.Magenta);
            fatalBackgroundColor = GetColor(ConsoleColor.White);
            footerForegroundColor = GetColor(ConsoleColor.DarkYellow);
            footerBackgroundColor = GetColor(_ConsoleColor.None);
            headerForegroundColor = GetColor(ConsoleColor.DarkYellow);
            headerBackgroundColor = GetColor(_ConsoleColor.None);
            helpForegroundColor = GetColor(ConsoleColor.White);
            helpBackgroundColor = GetColor(_ConsoleColor.None);
            helpItemForegroundColor = GetColor(ConsoleColor.Green);
            helpItemBackgroundColor = GetColor(_ConsoleColor.None);
            legalForegroundColor = GetColor(ConsoleColor.Gray);
            legalBackgroundColor = GetColor(_ConsoleColor.None);
            officialForegroundColor = GetColor(ConsoleColor.White);
            officialBackgroundColor = GetColor(ConsoleColor.DarkGreen);
            resultForegroundColor = GetColor(_ConsoleColor.None);
            resultBackgroundColor = GetColor(_ConsoleColor.None);
            stableForegroundColor = GetColor(ConsoleColor.White);
            stableBackgroundColor = GetColor(ConsoleColor.DarkGreen);
            trustedForegroundColor = GetColor(ConsoleColor.White);
            trustedBackgroundColor = GetColor(ConsoleColor.DarkGreen);
            undefinedForegroundColor = GetColor(ConsoleColor.DarkGray);
            undefinedBackgroundColor = GetColor(ConsoleColor.Yellow);
            unofficialForegroundColor = GetColor(ConsoleColor.White);
            unofficialBackgroundColor = GetColor(ConsoleColor.DarkRed);
            unstableForegroundColor = GetColor(ConsoleColor.DarkGray);
            unstableBackgroundColor = GetColor(ConsoleColor.Yellow);
            untrustedForegroundColor = GetColor(ConsoleColor.White);
            untrustedBackgroundColor = GetColor(ConsoleColor.DarkRed);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Section Colors
            announcementInfoForegroundColor = GetColor(ConsoleColor.White);
            announcementInfoBackgroundColor = GetColor(ConsoleColor.DarkRed);
            argumentInfoForegroundColor = GetColor(_ConsoleColor.None);
            argumentInfoBackgroundColor = GetColor(_ConsoleColor.None);
            callFrameInfoForegroundColor = GetColor(_ConsoleColor.None);
            callFrameInfoBackgroundColor = GetColor(_ConsoleColor.None);
            callStackInfoForegroundColor = GetColor(_ConsoleColor.None);
            callStackInfoBackgroundColor = GetColor(_ConsoleColor.None);
            complaintInfoForegroundColor = GetColor(_ConsoleColor.None);
            complaintInfoBackgroundColor = GetColor(_ConsoleColor.None);
            controlInfoForegroundColor = GetColor(_ConsoleColor.None);
            controlInfoBackgroundColor = GetColor(_ConsoleColor.None);
            customInfoForegroundColor = GetColor(_ConsoleColor.None);
            customInfoBackgroundColor = GetColor(_ConsoleColor.None);
            debuggerInfoForegroundColor = GetColor(_ConsoleColor.None);
            debuggerInfoBackgroundColor = GetColor(_ConsoleColor.None);
            engineInfoForegroundColor = GetColor(_ConsoleColor.None);
            engineInfoBackgroundColor = GetColor(_ConsoleColor.None);
            entityInfoForegroundColor = GetColor(_ConsoleColor.None);
            entityInfoBackgroundColor = GetColor(_ConsoleColor.None);
            flagInfoForegroundColor = GetColor(_ConsoleColor.None);
            flagInfoBackgroundColor = GetColor(_ConsoleColor.None);
            historyInfoForegroundColor = GetColor(_ConsoleColor.None);
            historyInfoBackgroundColor = GetColor(_ConsoleColor.None);
            hostInfoForegroundColor = GetColor(_ConsoleColor.None);
            hostInfoBackgroundColor = GetColor(_ConsoleColor.None);
            interpreterInfoForegroundColor = GetColor(_ConsoleColor.None);
            interpreterInfoBackgroundColor = GetColor(_ConsoleColor.None);
            objectInfoForegroundColor = GetColor(_ConsoleColor.None);
            objectInfoBackgroundColor = GetColor(_ConsoleColor.None);
            testInfoForegroundColor = GetColor(_ConsoleColor.None);
            testInfoBackgroundColor = GetColor(_ConsoleColor.None);
            tokenInfoForegroundColor = GetColor(_ConsoleColor.None);
            tokenInfoBackgroundColor = GetColor(_ConsoleColor.None);
            traceInfoForegroundColor = GetColor(_ConsoleColor.None);
            traceInfoBackgroundColor = GetColor(_ConsoleColor.None);
            variableInfoForegroundColor = GetColor(_ConsoleColor.None);
            variableInfoBackgroundColor = GetColor(_ConsoleColor.None);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Return Code Colors
            okResultForegroundColor = GetColor(ConsoleColor.Green);
            okResultBackgroundColor = GetColor(_ConsoleColor.None);
            errorResultForegroundColor = GetColor(ConsoleColor.Red);
            errorResultBackgroundColor = GetColor(_ConsoleColor.None);
            otherOkResultForegroundColor = GetColor(ConsoleColor.DarkGreen);
            otherOkResultBackgroundColor = GetColor(_ConsoleColor.None);
            otherErrorResultForegroundColor = GetColor(ConsoleColor.DarkRed);
            otherErrorResultBackgroundColor = GetColor(_ConsoleColor.None);
            returnResultForegroundColor = GetColor(ConsoleColor.Blue);
            returnResultBackgroundColor = GetColor(_ConsoleColor.None);
            breakResultForegroundColor = GetColor(ConsoleColor.Yellow);
            breakResultBackgroundColor = GetColor(_ConsoleColor.None);
            continueResultForegroundColor = GetColor(ConsoleColor.DarkYellow);
            continueResultBackgroundColor = GetColor(_ConsoleColor.None);
            whatIfResultForegroundColor = GetColor(ConsoleColor.Magenta);
            whatIfResultBackgroundColor = GetColor(_ConsoleColor.None);
            exceptionResultForegroundColor = GetColor(ConsoleColor.Magenta);
            exceptionResultBackgroundColor = GetColor(_ConsoleColor.None);
            nullResultForegroundColor = GetColor(ConsoleColor.White);
            nullResultBackgroundColor = GetColor(_ConsoleColor.None);
            emptyResultForegroundColor = GetColor(ConsoleColor.White);
            emptyResultBackgroundColor = GetColor(_ConsoleColor.None);
            unknownResultForegroundColor = GetColor(ConsoleColor.Cyan);
            unknownResultBackgroundColor = GetColor(_ConsoleColor.None);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Call Frame Colors
            nullCallFrameForegroundColor = GetColor(ConsoleColor.Gray);
            unknownCallFrameForegroundColor = GetColor(ConsoleColor.DarkGray);
            variableCallFrameForegroundColor = GetColor(ConsoleColor.White);
            uplevelCallFrameForegroundColor = GetColor(ConsoleColor.White);
            downlevelCallFrameForegroundColor = GetColor(ConsoleColor.White);
            trackingCallFrameForegroundColor = GetColor(ConsoleColor.White);
            engineCallFrameForegroundColor = GetColor(ConsoleColor.White);
            currentCallFrameForegroundColor = GetColor(ConsoleColor.White);
            procedureCallFrameForegroundColor = GetColor(ConsoleColor.Green);
            lambdaCallFrameForegroundColor = GetColor(ConsoleColor.Green);
            scopeCallFrameForegroundColor = GetColor(ConsoleColor.Green);
            aliasCallFrameForegroundColor = GetColor(ConsoleColor.White);
            globalCallFrameForegroundColor = GetColor(ConsoleColor.Yellow);
            globalScopeCallFrameForegroundColor = GetColor(ConsoleColor.DarkYellow);
            linkedCallFrameForegroundColor = GetColor(ConsoleColor.DarkBlue);
            namespaceCallFrameForegroundColor = GetColor(ConsoleColor.Blue);
            normalCallFrameForegroundColor = GetColor(ConsoleColor.DarkGreen);
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Color Swapping Methods
        protected virtual void MaybeSwapTextColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            MaybeSwapTextColors(OutputStyle,
                ref foregroundColor, ref backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void MaybeSwapTextColors(
            OutputStyle outputStyle,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            if (IsReversedTextOutputStyle(outputStyle))
            {
                ConsoleColor temporaryColor = foregroundColor;
                foregroundColor = backgroundColor;
                backgroundColor = temporaryColor;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void MaybeSwapBorderColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            MaybeSwapBorderColors(OutputStyle,
                ref foregroundColor, ref backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void MaybeSwapBorderColors(
            OutputStyle outputStyle,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            if (IsReversedBorderOutputStyle(outputStyle))
            {
                ConsoleColor temporaryColor = foregroundColor;
                foregroundColor = backgroundColor;
                backgroundColor = temporaryColor;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void GetResultColors(
            ReturnCode code,
            Result result,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            switch (code)
            {
                case ReturnCode.Ok:
                    {
                        foregroundColor = OkResultForegroundColor;
                        backgroundColor = OkResultBackgroundColor;
                        break;
                    }
                case ReturnCode.Error:
                    {
                        foregroundColor = ErrorResultForegroundColor;
                        backgroundColor = ErrorResultBackgroundColor;
                        break;
                    }
                case ReturnCode.Return:
                    {
                        foregroundColor = ReturnResultForegroundColor;
                        backgroundColor = ReturnResultBackgroundColor;
                        break;
                    }
                case ReturnCode.Break:
                    {
                        foregroundColor = BreakResultForegroundColor;
                        backgroundColor = BreakResultBackgroundColor;
                        break;
                    }
                case ReturnCode.Continue:
                    {
                        foregroundColor = ContinueResultForegroundColor;
                        backgroundColor = ContinueResultBackgroundColor;
                        break;
                    }
                case ReturnCode.WhatIf:
                    {
                        foregroundColor = WhatIfResultForegroundColor;
                        backgroundColor = WhatIfResultBackgroundColor;
                        break;
                    }
                case ReturnCode.Exception:
                    {
                        foregroundColor = ExceptionResultForegroundColor;
                        backgroundColor = ExceptionResultBackgroundColor;
                        break;
                    }
                default:
                    {
                        //
                        // NOTE: Check the return code (with "exceptions" enabled)
                        //       to see if it represents a success or failure.
                        //
                        if (ResultOps.IsSuccess(code, Exceptions))
                        {
                            //
                            // NOTE: This is an unknown "Ok" return code.
                            //
                            foregroundColor = OtherOkResultForegroundColor;
                            backgroundColor = OtherOkResultBackgroundColor;
                        }
                        else
                        {
                            //
                            // NOTE: This is an unknown "Error" return code.
                            //
                            foregroundColor = OtherErrorResultForegroundColor;
                            backgroundColor = OtherErrorResultBackgroundColor;
                        }
                        break;
                    }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Formatting Methods
        #region Complaint Formatting Methods
        private bool HasComplaint(
            Interpreter interpreter
            )
        {
            string complaint = null;

            return HasComplaint(interpreter, ref complaint);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool HasComplaint(
            Interpreter interpreter,
            ref string complaint
            )
        {
            if (interpreter != null)
            {
                complaint = DebugOps.SafeGetComplaint(interpreter);

                return !String.IsNullOrEmpty(complaint);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string FormatAnnouncement(
            Interpreter interpreter,
            BreakpointType breakpointType,
            string value
            )
        {
            if (interpreter != null)
            {
                int count = Count.Invalid;

                try
                {
                    count = interpreter.ActiveInteractiveLoops;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Default).Name,
                        TracePriority.Highest);
                }

                if (count != Count.Invalid)
                {
                    string prefix;
                    string suffix;

                    if (FlagOps.HasFlags(breakpointType,
                            BreakpointType.BeforeInteractiveLoop, true))
                    {
                        prefix = String.Format(
                            "Interactive Loop{0}",
                            !String.IsNullOrEmpty(value) ?
                            " for" : String.Empty);

                        suffix = " ===>";
                    }
                    else if (FlagOps.HasFlags(breakpointType,
                            BreakpointType.AfterInteractiveLoop, true))
                    {
                        prefix = String.Format(
                            "<=== Interactive Loop{0}",
                            !String.IsNullOrEmpty(value) ?
                            " for" : String.Empty);

                        suffix = null;
                    }
                    else
                    {
                        prefix = null;
                        suffix = null;
                    }

                    string formatted = String.Format(
                        "{0} {1} #{2}{3}", prefix, value, count,
                        suffix).Trim();

                    return formatted;
                }
            }

            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Result Formatting Methods
        private string FormatResult(
            string prefix,
            ReturnCode code,
            Result result,
            int errorLine,
            bool exceptions,
            bool display,
            bool ellipsis,
            bool replaceNewLines,
            bool strict
            )
        {
            ConsoleColor foregroundColor = ResultForegroundColor;
            ConsoleColor backgroundColor = ResultBackgroundColor;

            return FormatResult(
                prefix, code, result, errorLine, exceptions, display,
                ellipsis, replaceNewLines, strict, ref foregroundColor,
                ref backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual string FormatResult(
            string prefix,
            ReturnCode code,
            Result result,
            int errorLine,
            bool exceptions,
            bool display,
            bool ellipsis,
            bool replaceNewLines,
            bool strict,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            ReturnCode returnCode;

            if ((result == null) || (result.ReturnCode == ReturnCode.Ok))
                returnCode = code;
            else
                returnCode = result.ReturnCode;

            string formatted = FormatOps.Result(ResultOps.Format(prefix,
                returnCode, result, errorLine, exceptions, display),
                ellipsis, replaceNewLines);

            if (!String.IsNullOrEmpty(formatted))
            {
                GetResultColors(
                    returnCode, result, ref foregroundColor,
                    ref backgroundColor);
            }
            else if (strict && (result == null))
            {
                foregroundColor = NullResultForegroundColor;
                backgroundColor = NullResultBackgroundColor;

                return FormatOps.DisplayNull;
            }
            else if (strict && (result.Length == 0))
            {
                foregroundColor = EmptyResultForegroundColor;
                backgroundColor = EmptyResultBackgroundColor;

                return FormatOps.DisplayEmpty;
            }
            else if (strict)
            {
                foregroundColor = UnknownResultForegroundColor;
                backgroundColor = UnknownResultBackgroundColor;

                return FormatOps.DisplayUnknown;
            }

            return formatted;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Introspection Methods
        #region Color Introspection Methods
        protected virtual List<PropertyInfo> GetColorProperties(
            Type type,
            MatchMode mode,
            string pattern,
            bool noCase,
            bool canRead,
            bool canWrite
            )
        {
            List<PropertyInfo> propertyInfoList = new List<PropertyInfo>();

            if (type != null)
            {
                foreach (PropertyInfo propertyInfo in type.GetProperties(
                        HostPropertyBindingFlags))
                {
                    if (propertyInfo != null)
                    {
                        if (propertyInfo.PropertyType == typeof(ConsoleColor))
                        {
                            if ((pattern == null) || StringOps.Match(
                                    null, mode, propertyInfo.Name, pattern,
                                    noCase))
                            {
                                if ((!canRead || propertyInfo.CanRead) &&
                                    (!canWrite || propertyInfo.CanWrite))
                                {
                                    propertyInfoList.Add(propertyInfo);
                                }
                            }
                        }
                    }
                }
            }

            return propertyInfoList;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Introspection Methods
        protected virtual string GetHostType(
            IInteractiveHost interactiveHost
            )
        {
            if (interactiveHost != null)
            {
                if (!AppDomainOps.IsTransparentProxy(interactiveHost))
                {
                    Type type = interactiveHost.GetType();

                    if (
#if CONSOLE
                        (type == typeof(_Hosts.Console)) ||
#endif
                        (type == typeof(_Hosts.Core)) || (type == typeof(_Hosts.Default)) ||
                        (type == typeof(_Hosts.Diagnostic)) || (type == typeof(_Hosts.Engine)) ||
                        (type == typeof(_Hosts.File)) || (type == typeof(_Hosts.Null)) ||
                        (type == typeof(_Hosts.Profile)) || (type == typeof(_Hosts.Shell)) ||
                        (type == typeof(_Hosts.Wrapper)))
                    {
                        return String.Format(
                            FormatOps.DisplayFormat, type.Name.ToLower());
                    }
                    else
                    {
                        return String.Format(
                            FormatOps.DisplayFormat, type.FullName);
                    }
                }
                else
                {
                    return FormatOps.DisplayProxy;
                }
            }
            else
            {
                return FormatOps.DisplayNull;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual string GetHostSize(
            ISizeHost sizeHost,
            HostSizeType hostSizeType
            )
        {
            if (sizeHost != null)
            {
                try
                {
                    int width = 0;
                    int height = 0;

                    if (sizeHost.GetSize(hostSizeType, ref width, ref height))
                        return FormatOps.DisplayWidthAndHeight(width, height);
                }
                catch (Exception e)
                {
                    return FormatOps.DisplayException(e, true);
                }
            }

            return FormatOps.DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual string GetHostInfo(
            IHost host,
            string name /* NOTE: The name of a property or method [with no arguments]. */
            )
        {
            //
            // HACK: Yes, this method is a nasty hack.  However, there is no
            //       other reasonable way to query all the host properties for
            //       display while not allowing exceptions to escape the
            //       WriteHostInfo method.
            //
            if (host != null)
            {
                try
                {
                    Type type = AppDomainOps.MaybeGetType(
                        host, typeof(Default));

                    do
                    {
                        object value;

                        try
                        {
                            value = type.InvokeMember(
                                name, ObjectOps.GetBindingFlags(
                                MetaBindingFlags.HostInfo, true),
                                null, host, null);
                        }
                        catch (Exception e)
                        {
                            type = type.BaseType;

                            if (type == typeof(object))
                                return FormatOps.DisplayException(e, true);

                            continue;
                        }

                        //
                        // NOTE: *SPECIAL* We want the display name of the
                        //       encoding, not the type name.
                        //
                        if (value is Encoding)
                            return ((Encoding)value).WebName;
                        else if (value != null)
                            return value.ToString();
                        else
                            return FormatOps.DisplayNull;
                    }
                    while (true);
                }
                catch (Exception e)
                {
                    return FormatOps.DisplayException(e, true);
                }
            }

            return FormatOps.DisplayNull;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Call Frame Introspection Methods
        protected virtual string GetCallFrameType(
            Interpreter interpreter,
            ICallFrame frame,
            bool special
            )
        {
            if (interpreter != null)
            {
                //
                // TODO: This is a very poor design.  Make this logic more
                //       customizable at runtime.
                //
                if (frame == null)
                    return NullCallFrameTypeName;
                else if (special && interpreter.IsVariableCallFrame(frame))
                    return VariableCallFrameTypeName;
                else if (interpreter.IsCurrentCallFrame(frame))
                    return CurrentCallFrameTypeName;
                else if (interpreter.IsProcedureCallFrame(frame))
                    return ProcedureCallFrameTypeName;
                else if (Interpreter.IsNamespaceCallFrame(frame))
                    return NamespaceCallFrameTypeName;
                else if (interpreter.IsGlobalCallFrame(frame))
                    return GlobalCallFrameTypeName;
                else if (interpreter.IsGlobalScopeCallFrame(frame))
                    return GlobalScopeCallFrameTypeName;
                else if (CallFrameOps.IsLambda(frame))
                    return LambdaCallFrameTypeName;
                else if (CallFrameOps.IsScope(frame))
                    return ScopeCallFrameTypeName;
                else if (CallFrameOps.IsAlias(frame))
                    return AliasCallFrameTypeName;
                else if (CallFrameOps.IsUplevel(frame))
                    return UplevelCallFrameTypeName;
                else if (CallFrameOps.IsDownlevel(frame))
                    return DownlevelCallFrameTypeName;
                else if (CallFrameOps.IsTracking(frame))
                    return TrackingCallFrameTypeName;
                else if (CallFrameOps.IsEngine(frame))
                    return EngineCallFrameTypeName;
                else if (interpreter.IsCallFrameInCallStack(frame))
                    //
                    // NOTE: Yes, this normally returns only whitespace.
                    //
                    return NormalCallFrameTypeName;
            }

            return UnknownCallFrameTypeName;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ConsoleColor GetCallFrameColor(
            Interpreter interpreter,
            ICallFrame frame,
            bool special
            )
        {
            if (interpreter != null)
            {
                //
                // TODO: This is a very poor design.  Make this logic more
                //       customizable at runtime.
                //
                if (frame == null)
                    return NullCallFrameForegroundColor;
                else if (special && interpreter.IsVariableCallFrame(frame))
                    return VariableCallFrameForegroundColor;
                else if (interpreter.IsCurrentCallFrame(frame))
                    return CurrentCallFrameForegroundColor;
                else if (interpreter.IsProcedureCallFrame(frame))
                    return ProcedureCallFrameForegroundColor;
                else if (Interpreter.IsNamespaceCallFrame(frame))
                    return NamespaceCallFrameForegroundColor;
                else if (interpreter.IsGlobalCallFrame(frame))
                    return GlobalCallFrameForegroundColor;
                else if (interpreter.IsGlobalScopeCallFrame(frame))
                    return GlobalScopeCallFrameForegroundColor;
                else if (CallFrameOps.IsLambda(frame))
                    return LambdaCallFrameForegroundColor;
                else if (CallFrameOps.IsScope(frame))
                    return ScopeCallFrameForegroundColor;
                else if (CallFrameOps.IsAlias(frame))
                    return AliasCallFrameForegroundColor;
                else if (CallFrameOps.IsUplevel(frame))
                    return UplevelCallFrameForegroundColor;
                else if (CallFrameOps.IsDownlevel(frame))
                    return DownlevelCallFrameForegroundColor;
                else if (CallFrameOps.IsTracking(frame))
                    return TrackingCallFrameForegroundColor;
                else if (CallFrameOps.IsEngine(frame))
                    return EngineCallFrameForegroundColor;
                else if (interpreter.IsCallFrameInCallStack(frame))
                    return NormalCallFrameForegroundColor;
            }

            return UnknownCallFrameForegroundColor;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool BuildCallFrameInfoList(
            Interpreter interpreter,
            ICallFrame frame,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Frame");
                list.Add((IPair<string>)null);
            }

            if (frame != null)
            {
                if (frame.Disposed)
                {
                    list.Add(FormatOps.DisplayDisposed);
                }
                else
                {
                    bool linked = FlagOps.HasFlags(
                        detailFlags, DetailFlags.CallFrameLinked, true);

                    bool special = FlagOps.HasFlags(
                        detailFlags, DetailFlags.CallFrameSpecial, true);

                    bool variables = FlagOps.HasFlags(
                        detailFlags, DetailFlags.CallFrameVariables, true);

                    list.Add(frame.ToList(detailFlags));
                    list.Add("special", special.ToString());
                    list.Add((IPair<string>)null);
                    list.Add("Links");
                    list.Add((IPair<string>)null);
                    list.Add("inbound", linked.ToString());
                    list.Add("outboundOther", (frame.Other != null).ToString());
                    list.Add("outboundPrevious", (frame.Previous != null).ToString());
                    list.Add("outboundNext", (frame.Next != null).ToString());
                    list.Add((IPair<string>)null);

                    if (interpreter != null)
                    {
                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            if (!interpreter.Disposed)
                            {
                                list.Add("inCallStack", interpreter.IsCallFrameInCallStack(frame).ToString());
                                list.Add("isGlobal", interpreter.IsGlobalCallFrame(frame).ToString());
                                list.Add("isGlobalScope", interpreter.IsGlobalScopeCallFrame(frame).ToString());
                                list.Add("isCurrent", interpreter.IsCurrentCallFrame(frame).ToString());
                                list.Add("isProcedure", interpreter.IsProcedureCallFrame(frame).ToString());
                                list.Add("isUplevel", interpreter.IsUplevelCallFrame(frame).ToString());
                                list.Add("isVariable", interpreter.IsVariableCallFrame(frame).ToString());
                            }
                        }
                    }

                    list.Add("typeName", GetCallFrameType(interpreter, frame, special));
                    list.Add("color", GetCallFrameColor(interpreter, frame, special).ToString());

                    if (variables)
                    {
                        VariableDictionary frameVariables = frame.Variables;

                        if ((frameVariables != null) && (frameVariables.Count > 0))
                        {
                            list.Add((IPair<string>)null);
                            list.Add("Variables");
                            list.Add((IPair<string>)null);
                            list.Add(frameVariables.ToString());
                        }
                    }
                }
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Object Introspection Methods
        private static IObject GetObjectFromValue(
            Interpreter interpreter,
            object value
            )
        {
            string name = StringOps.GetStringFromObject(value);

            if (name != null)
            {
                IObject @object = null;

                if ((interpreter != null) &&
                    interpreter.GetObject(
                        name, LookupFlags.HostNoVerbose,
                        ref @object) == ReturnCode.Ok)
                {
                    return @object;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Type GetWrappedObjectType(
            IObject @object
            )
        {
            _Wrappers._Object objectWrapper = @object as _Wrappers._Object;

            if (objectWrapper != null)
                return AppDomainOps.MaybeGetType(objectWrapper.@object);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool NeedPairKeyPrefix(
            string key,
            string prefix
            )
        {
            return !String.IsNullOrEmpty(prefix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetPairKeyWithPrefix(
            string key,
            string prefix
            )
        {
            if (!NeedPairKeyPrefix(key, prefix))
                return key;

            return String.Format("{0} ({1})", prefix, key);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool BuildObjectInfoList(
            Interpreter interpreter,
            IObject @object,
            string prefix,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            bool empty = HasEmptyContent(detailFlags);

            if (@object != null)
            {
                try
                {
                    StringPairList localList = new StringPairList();

                    if (empty || (@object.Alias != null))
                        localList.Add(GetPairKeyWithPrefix("Alias", prefix),
                            FormatOps.WrapOrNull(@object.Alias));

                    if (empty || (@object.ReferenceCount != 0))
                        localList.Add(GetPairKeyWithPrefix("ReferenceCount", prefix),
                            @object.ReferenceCount.ToString());

                    if (empty || (@object.TemporaryReferenceCount != 0))
                        localList.Add(GetPairKeyWithPrefix("TemporaryReferenceCount", prefix),
                            @object.TemporaryReferenceCount.ToString());

                    if (empty || (@object.ObjectFlags != ObjectFlags.None))
                        localList.Add(GetPairKeyWithPrefix("ObjectFlags", prefix),
                            @object.ObjectFlags.ToString());

#if NATIVE && TCL
                    if (empty || (@object.InterpName != null))
                        localList.Add(GetPairKeyWithPrefix("InterpName", prefix),
                            (@object.InterpName != null) ? @object.InterpName :
                            FormatOps.DisplayNull);
#endif

#if DEBUGGER && DEBUGGER_ARGUMENTS
                    if (empty || (@object.ExecuteArguments != null))
                        localList.Add(GetPairKeyWithPrefix("ExecuteArguments", prefix),
                            (@object.ExecuteArguments != null) ?
                                @object.ExecuteArguments.ToString() :
                                FormatOps.DisplayNull);
#endif

                    //
                    // NOTE: What type is the wrapper for this object?
                    //
                    Type objectType = GetWrappedObjectType(@object);

                    if (empty || (objectType != null))
                    {
                        if (objectType != null)
                        {
                            if (GlobalState.IsAssembly(objectType.Assembly))
                            {
                                localList.Add(GetPairKeyWithPrefix(
                                    "ObjectType", prefix),
                                    objectType.FullName);
                            }
                            else
                            {
                                localList.Add(GetPairKeyWithPrefix(
                                    "ObjectType", prefix),
                                    objectType.AssemblyQualifiedName);
                            }
                        }
                        else
                        {
                            localList.Add(GetPairKeyWithPrefix(
                                "ObjectType", prefix), FormatOps.DisplayNull);
                        }
                    }

                    object objectValue = @object.Value;

                    if (empty || (objectValue != null))
                    {
                        //
                        // NOTE: What type is this object?
                        //
                        Type valueType = AppDomainOps.MaybeGetType(objectValue);

                        if (empty || (valueType != null))
                        {
                            if (valueType != null)
                            {
                                if (GlobalState.IsAssembly(valueType.Assembly))
                                {
                                    localList.Add(GetPairKeyWithPrefix(
                                        "ValueType", prefix),
                                        valueType.FullName);
                                }
                                else
                                {
                                    localList.Add(GetPairKeyWithPrefix(
                                        "ValueType", prefix),
                                        valueType.AssemblyQualifiedName);
                                }
                            }
                            else
                            {
                                localList.Add(GetPairKeyWithPrefix(
                                    "ValueType", prefix), FormatOps.DisplayNull);
                            }
                        }

                        if (objectValue != null)
                        {
                            string stringValue = objectValue.ToString();

                            if (empty || (stringValue != null))
                            {
                                localList.Add(GetPairKeyWithPrefix(
                                    "ToString", prefix), stringValue != null ?
                                    stringValue : FormatOps.DisplayNull);
                            }
                        }
                        else
                        {
                            localList.Add(GetPairKeyWithPrefix("Value", prefix),
                                FormatOps.DisplayNull);
                        }
                    }

                    if (localList.Count > 0)
                    {
                        if (list == null)
                            list = new StringPairList();

                        if (!NeedPairKeyPrefix(null, prefix) &&
                            (list.Count == 0))
                        {
                            list.Add("Object");
                            list.Add((IPair<string>)null);
                        }

                        list.Add(localList);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Default).Name,
                        TracePriority.HostError);
                }

                return false;
            }
            else if (empty)
            {
                if (list == null)
                    list = new StringPairList();

                if (!NeedPairKeyPrefix(null, prefix) &&
                    (list.Count == 0))
                {
                    list.Add("Object");
                    list.Add((IPair<string>)null);
                }

                list.Add(FormatOps.DisplayNull);

                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Variable Introspection Methods
        protected internal virtual bool BuildLinkedVariableInfoList(
            Interpreter interpreter,
            IVariable variable,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            StringPairList localList = null;

            if (BuildVariableInfoList(
                    interpreter, variable, detailFlags,
                    ref localList))
            {
                if (list == null)
                    list = new StringPairList();

                list.AddRange(localList);
            }
            else
            {
                return false;
            }

            bool links = FlagOps.HasFlags(
                detailFlags, DetailFlags.VariableLinks, true);

            if ((variable != null) && links)
            {
                IVariable link = variable.Link;
                string linkIndex = variable.LinkIndex;
                int count = 0;

                while (link != null)
                {
                    if (list == null)
                        list = new StringPairList();

                    list.Add((IPair<string>)null);
                    list.Add(String.Format(
                        "LinkIndex #{0}", count), (linkIndex != null) ?
                            linkIndex : FormatOps.DisplayNull);

                    list.Add((IPair<string>)null);
                    list.Add(String.Format("Link #{0}", count));
                    list.Add((IPair<string>)null);

                    localList = null;

                    if (BuildVariableInfoList(
                            interpreter, link, detailFlags,
                            ref localList))
                    {
                        list.AddRange(localList);
                    }
                    else
                    {
                        return false;
                    }

                    count++;
                    link = link.Link;

                    if (link != null)
                        linkIndex = link.LinkIndex;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool BuildVariableInfoList(
            Interpreter interpreter,
            IVariable variable,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Variable");
                list.Add((IPair<string>)null);
            }

            if (variable != null)
            {
                list.Add("Flags", variable.Flags.ToString());

                string name = (variable.Name != null) ?
                    variable.Name : FormatOps.DisplayNull;

                list.Add("Name", name);

                string threadId = (variable.ThreadId != null) ?
                    ((long)variable.ThreadId).ToString() :
                    FormatOps.DisplayNull;

                list.Add("ThreadId", threadId);

                bool isArray = EntityOps.IsArray2(variable);

                if (!isArray)
                {
                    object value = variable.Value;

                    string stringValue = (value != null) ?
                        StringOps.GetStringFromObject(value) :
                        FormatOps.DisplayNull;

                    list.Add("Type",
                        FormatOps.TypeName(value, false));

                    list.Add("Value",
                        FormatOps.DisplayString(stringValue));

                    IObject @object = GetObjectFromValue(
                        interpreter, value);

                    if (@object != null)
                    {
                        list.Add((IPair<string>)null);

                        StringPairList localList = null;

                        if (BuildObjectInfoList(
                                interpreter, @object, null,
                                detailFlags, ref localList))
                        {
                            list.AddRange(localList);
                        }
                        else
                        {
                            list.Add("Object");
                            list.Add((IPair<string>)null);
                            list.Add(FormatOps.DisplayUnknown);
                        }
                    }

                    IHaveStringBuilder haveStringBuilder =
                        value as IHaveStringBuilder;

                    if (haveStringBuilder != null)
                    {
                        list.Add((IPair<string>)null);
                        list.Add("IHaveStringBuilder");
                        list.Add((IPair<string>)null);

                        list.Add("Id", haveStringBuilder.Id.ToString());

                        list.Add("ReadWriteCount",
                            haveStringBuilder.ReadWriteCount.ToString());
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                ICallFrame frame = variable.Frame;

                if ((frame != null) && !frame.Disposed)
                {
                    StringPairList localList = new StringPairList();
                    ICallFrame link = frame;
                    int count = 0;

                    if ((link != null) && !link.Disposed)
                    {
                        localList.Add(String.Format(
                            "Name #{0}", count),
                            link.Name);

                        localList.Add(String.Format(
                            "Flags #{0}", count),
                            link.Flags.ToString());

                        count++;
                        link = link.Other;

                        if (link != null)
                        {
                            localList.Add(String.Format(
                                "OtherName #{0}", count),
                                link.Name);

                            localList.Add(String.Format(
                                "OtherFlags #{0}", count),
                                link.Flags.ToString());
                        }
                    }

                    link = frame;
                    count = 0;
                    link = link.Previous;

                    while ((link != null) && !link.Disposed)
                    {
                        localList.Add(String.Format(
                            "PreviousName #{0}", count),
                            link.Name);

                        localList.Add(String.Format(
                            "PreviousFlags #{0}", count),
                            link.Flags.ToString());

                        count++;
                        link = link.Previous;
                    }

                    link = frame;
                    count = 0;
                    link = link.Next;

                    while ((link != null) && !link.Disposed)
                    {
                        localList.Add(String.Format(
                            "NextName #{0}", count),
                            link.Name);

                        localList.Add(String.Format(
                            "NextFlags #{0}", count),
                            link.Flags.ToString());

                        count++;
                        link = link.Next;
                    }

                    if (localList.Count > 0)
                    {
                        list.Add((IPair<string>)null);
                        list.Add("Frame");
                        list.Add((IPair<string>)null);
                        list.Add(localList);
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                TraceList traces = variable.Traces;

                if (traces != null)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Traces", traces.Count.ToString());

                    if (traces.Count > 0)
                    {
                        list.Add((IPair<string>)null);

                        foreach (ITrace trace in traces)
                        {
                            if (trace == null)
                                continue;

                            //
                            // NOTE: Is this an interpreter-wide trace?
                            //
                            bool global = FlagOps.HasFlags(
                                trace.TraceFlags, TraceFlags.Global, true);

#if ISOLATED_PLUGINS
                            //
                            // NOTE: Get the plugin for this trace.
                            //
                            IPlugin plugin = trace.Plugin;

                            if (!AppDomainOps.IsIsolated(plugin) &&
                                !AppDomainOps.IsTransparentProxy(trace))
#endif
                            {
                                TraceCallback traceCallback = trace.Callback;

                                if (traceCallback != null)
                                {
                                    list.Add(global ?
                                        "Interpreter" : "Variable",
                                        FormatOps.DelegateMethodName(
                                            traceCallback.Method, false, true));
                                }
                            }
#if ISOLATED_PLUGINS
                            else
                            {
                                list.Add(global ? "Interpreter" : "Variable",
                                    FormatOps.DelegateMethodName(
                                        trace.TypeName, trace.MethodName));
                            }
#endif
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                ElementDictionary arrayValue = isArray ?
                    variable.ArrayValue : null;

                ///////////////////////////////////////////////////////////////////////////////////////

                if (isArray)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Array");
                    list.Add((IPair<string>)null);

                    if (arrayValue != null)
                    {
                        list.Add("Value", StringList.MakeList(
                            "Count", arrayValue.Count,
                            "Capacity", arrayValue.GetCapacity()));

                        list.Add("DefaultValue", FormatOps.DisplayString(
                            StringOps.GetStringFromObject(
                                arrayValue.DefaultValue)));
                    }
                    else
                    {
                        list.Add("Value", FormatOps.DisplayNull);
                        list.Add("DefaultValue", FormatOps.DisplayNull);
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                //
                // NOTE: If we have a valid interpreter context, check for array
                //       searches belonging to this variable.  This is done even
                //       when the "searches" parameter is false.
                //
                if (isArray /*&& searches*/ && (interpreter != null))
                {
                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                    {
                        StringPairList arraySearchPairs = new StringPairList();
                        ArraySearchDictionary arraySearches = interpreter.ArraySearches;

                        if (arraySearches != null)
                        {
                            foreach (KeyValuePair<string, ArraySearch> pair in arraySearches)
                            {
                                ArraySearch arraySearch = pair.Value;

                                if (arraySearch == null)
                                    continue;

                                if (Object.ReferenceEquals(arraySearch.Variable, variable))
                                {
                                    arraySearchPairs.Add(String.Format(
                                        "Name #{0}", arraySearchPairs.Count),
                                        pair.Key);
                                }
                            }
                        }

                        //
                        // NOTE: Always add the count of matching array searches
                        //       for this variable (i.e. even when the "searches"
                        //       parameter is false).
                        //
                        list.Add("Searches",
                            arraySearchPairs.Count.ToString());

                        //
                        // NOTE: When the "searches" parameter is false, simply
                        //       skip emitting the details.
                        //
                        bool searches = FlagOps.HasFlags(
                            detailFlags, DetailFlags.VariableSearches, true);

                        if (searches && (arraySearchPairs.Count > 0))
                        {
                            list.Add((IPair<string>)null);
                            list.Add("Searches");
                            list.Add((IPair<string>)null);
                            list.Add(arraySearchPairs);
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                bool elements = FlagOps.HasFlags(
                    detailFlags, DetailFlags.VariableElements, true);

                if (isArray && elements &&
                    (arrayValue != null) && (arrayValue.Count > 0))
                {
                    list.Add((IPair<string>)null);
                    list.Add("Elements");
                    list.Add((IPair<string>)null);

                    foreach (KeyValuePair<string, object> pair in arrayValue)
                    {
                        string value = (pair.Value != null) ?
                            StringOps.GetStringFromObject(pair.Value) :
                            FormatOps.DisplayNull;

                        list.Add(new StringPair(GetPairKeyWithPrefix(
                            "Type", pair.Key), FormatOps.TypeName(
                            pair.Value, false)));

                        list.Add(new StringPair(GetPairKeyWithPrefix(
                            "Value", pair.Key), value));

                        IObject @object = GetObjectFromValue(
                            interpreter, pair.Value);

                        if (@object != null)
                        {
                            StringPairList localList = null;

                            if (BuildObjectInfoList(
                                    interpreter, @object, pair.Key,
                                    detailFlags, ref localList))
                            {
                                list.AddRange(localList);
                            }
                            else
                            {
                                list.Add(new StringPair(GetPairKeyWithPrefix(
                                    "Object", pair.Key), FormatOps.DisplayNull));
                            }
                        }
                    }
                }
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Argument Introspection Methods
        protected virtual bool BuildArgumentInfoList(
            ReturnCode code,
            BreakpointType breakpointType,
            string breakpointName,
            ArgumentList arguments,
            Result result,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("ArgumentInfo");
                list.Add((IPair<string>)null);
            }

            list.Add("BreakpointType", breakpointType.ToString());
            list.Add("BreakpointName", FormatOps.DisplayString(breakpointName));

            if (arguments != null)
            {
                list.Add((IPair<string>)null);

                list.Add("Arguments", arguments.ToString(
                    ToStringFlags.NameAndValue, null, false));
            }

            list.Add((IPair<string>)null);
            list.Add("Code", code.ToString());
            list.Add("Result", FormatOps.DisplayResult(result, true, true));

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Call Stack Introspection Methods
        protected virtual bool BuildCallStackInfoList(
            Interpreter interpreter,
            CallStack callStack,
            int limit,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("CallStack");
                list.Add((IPair<string>)null);
            }

            if ((interpreter != null) && (callStack != null))
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if (interpreter.Disposed || callStack.Disposed)
                    {
                        list.Add(FormatOps.DisplayDisposed);
                    }
                    else
                    {
                        CallStack newCallStack = null;
                        Result error = null;

                        try
                        {
                            bool all = FlagOps.HasFlags(
                                detailFlags, DetailFlags.CallStackAllFrames, true);

                            if (CallFrameOps.Traverse(interpreter, callStack,
                                    null, limit, all, ref newCallStack,
                                    ref error) == ReturnCode.Ok)
                            {
                                int count = newCallStack.Count;

                                for (int index = 0; index < count; index++)
                                {
                                    ICallFrame frame = newCallStack[index];

                                    if (frame == null)
                                        continue;

                                    list.Add(frame.ToString(detailFlags));

                                    if (all)
                                    {
                                        ICallFrame otherFrame = frame.Other;

                                        if (otherFrame != null)
                                        {
                                            list.Add(otherFrame.ToString(
                                                detailFlags));
                                        }

                                        ICallFrame previousFrame = frame.Previous;

                                        if (previousFrame != null)
                                        {
                                            list.Add(previousFrame.ToString(
                                                detailFlags));
                                        }

                                        ICallFrame nextFrame = frame.Next;

                                        if (nextFrame != null)
                                        {
                                            list.Add(nextFrame.ToString(
                                                detailFlags));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                list.Add("Error", error);
                            }
                        }
                        finally
                        {
                            if (newCallStack != null)
                            {
                                newCallStack.Dispose();
                                newCallStack = null;
                            }
                        }
                    }
                }
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Debugger Introspection Methods
#if DEBUGGER
        protected virtual bool BuildDebuggerInfoList(
            Interpreter interpreter,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Debugger");
                list.Add((IPair<string>)null);
            }

            if (interpreter != null)
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if (!interpreter.Disposed)
                    {
                        IDebugger debugger = interpreter.Debugger;

                        if (debugger != null)
                        {
                            if (debugger.Disposed)
                            {
                                list.Add("Debugger", FormatOps.DisplayDisposed);
                            }
                            else
                            {
                                list.Add("Debugger", FormatOps.DisplayPresent);

                                if (FlagOps.HasFlags(detailFlags, DetailFlags.Debugger, true))
                                    debugger.AddInfo(list, detailFlags);
                            }
                        }
                        else
                        {
                            list.Add("Debugger", FormatOps.DisplayNull);
                        }
                    }
                    else
                    {
                        list.Add("Debugger", FormatOps.DisplayDisposed);
                    }
                }
            }
            else
            {
                list.Add("Debugger", FormatOps.DisplayNull);
            }

            return true;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Flag Introspection Methods
        protected virtual bool BuildFlagInfoList(
            Interpreter interpreter,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            HeaderFlags headerFlags,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Flags");
                list.Add((IPair<string>)null);
            }

            bool empty = HasEmptyContent(detailFlags);
            int headerCount = list.Count;

            if (empty || (engineFlags != EngineFlags.None))
                list.Add("EngineFlags", engineFlags.ToString());

            if (empty || (substitutionFlags != SubstitutionFlags.None))
                list.Add("SubstitutionFlags", substitutionFlags.ToString());

            if (empty || (eventFlags != EventFlags.None))
                list.Add("EventFlags", eventFlags.ToString());

            if (empty || (expressionFlags != ExpressionFlags.None))
                list.Add("ExpressionFlags", expressionFlags.ToString());

            if (empty || (headerFlags != HeaderFlags.None))
                list.Add("HeaderFlags", headerFlags.ToString());

            if (interpreter != null)
                interpreter.GetHostFlagInfo(ref list, detailFlags);

            if (list.Count == headerCount)
                list.Add(FormatOps.DisplayEmpty);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Introspection Methods
        protected virtual bool BuildHostInfoList(
            Interpreter interpreter,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Host");
                list.Add((IPair<string>)null);
            }

            bool empty = HasEmptyContent(detailFlags);

            if (interpreter != null)
            {
#if ISOLATED_PLUGINS
                IHost isolatedHost = null;
#endif

                IInteractiveHost interactiveHost = null;
                IHost host = null;

                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if (interpreter.Disposed)
                    {
                        list.Add("Host", FormatOps.DisplayDisposed);
                    }
                    else
                    {
#if ISOLATED_PLUGINS
                        isolatedHost = interpreter.IsolatedHost;
#endif

                        host = interpreter.Host;
                        interactiveHost = interpreter.InteractiveHost;
                    }
                }

#if ISOLATED_PLUGINS
                if (empty || (isolatedHost != null))
                    list.Add("IsolatedHost", GetHostType(isolatedHost));
#endif

                if (empty || (interactiveHost != null))
                    list.Add("InteractiveHost", GetHostType(interactiveHost));

                if (host != null)
                {
                    list.Add("Host", GetHostType(host));
                    list.Add("Id", GetHostInfo(host, "Id"));
                    list.Add("Kind", GetHostInfo(host, "Kind"));
                    list.Add("Name", GetHostInfo(host, "Name"));
                    list.Add("Description", GetHostInfo(host, "Description"));

                    list.Add("CanExit", GetHostInfo(host, "CanExit"));
                    list.Add("CanForceExit", GetHostInfo(host, "CanForceExit"));
                    list.Add("Exiting", GetHostInfo(host, "Exiting"));

                    list.Add("Profile", GetHostInfo(host, "Profile"));
                    list.Add("UseAttach", GetHostInfo(host, "UseAttach"));
                    list.Add("NoColor", GetHostInfo(host, "NoColor"));
                    list.Add("NoTitle", GetHostInfo(host, "NoTitle"));
                    list.Add("NoIcon", GetHostInfo(host, "NoIcon"));
                    list.Add("NoProfile", GetHostInfo(host, "NoProfile"));
                    list.Add("NoCancel", GetHostInfo(host, "NoCancel"));
                    list.Add("Echo", GetHostInfo(host, "Echo"));

                    list.Add("IsOpen", GetHostInfo(host, "IsOpen"));
                    list.Add("IsIdle", GetHostInfo(host, "IsIdle"));

                    list.Add("InputEncoding", GetHostInfo(host, "InputEncoding"));
                    list.Add("OutputEncoding", GetHostInfo(host, "OutputEncoding"));
                    list.Add("ErrorEncoding", GetHostInfo(host, "ErrorEncoding"));

                    list.Add("DefaultTitle", GetHostInfo(host, "DefaultTitle"));
                    list.Add("Title", GetHostInfo(host, "Title"));

                    list.Add("HeaderFlags", GetHostInfo(host, "GetHeaderFlags"));
                    list.Add("HostFlags", GetHostInfo(host, "GetHostFlags"));
                    list.Add("TestFlags", GetHostInfo(host, "GetTestFlags"));

                    list.Add("IsInputRedirected", GetHostInfo(host, "IsInputRedirected"));
                    list.Add("IsOutputRedirected", GetHostInfo(host, "IsOutputRedirected"));
                    list.Add("IsErrorRedirected", GetHostInfo(host, "IsErrorRedirected"));

                    list.Add("ReadLevels", GetHostInfo(host, "ReadLevels"));
                    list.Add("WriteLevels", GetHostInfo(host, "WriteLevels"));

                    list.Add("HostBufferSize", GetHostSize(host, HostSizeType.BufferCurrent));
                    list.Add("HostWindowSize", GetHostSize(host, HostSizeType.WindowCurrent));

                    _Hosts.Default defaultHost = host as _Hosts.Default;

                    if (defaultHost != null)
                    {
                        StringPairList defaultList = new StringPairList();

                        bool dimensions = FlagOps.HasFlags(
                            detailFlags, DetailFlags.HostDimensions, true);

                        if (dimensions)
                        {
                            StringPairList localList = new StringPairList();

                            localList.Add("HostLeft", defaultHost.HostLeft.ToString());
                            localList.Add("HostTop", defaultHost.HostTop.ToString());
                            localList.Add("WindowWidth", defaultHost.WindowWidth.ToString());
                            localList.Add("WindowHeight", defaultHost.WindowHeight.ToString());
                            localList.Add("ContentWidth", defaultHost.ContentWidth.ToString());
                            localList.Add("ContentMargin", defaultHost.ContentMargin.ToString());
                            localList.Add("ContentThreshold", defaultHost.ContentThreshold.ToString());
                            localList.Add("MinimumLength", defaultHost.MinimumLength.ToString());
                            localList.Add("BoxWidth", defaultHost.BoxWidth.ToString());
                            localList.Add("BoxMargin", defaultHost.BoxMargin.ToString());

                            if (localList.Count > 0)
                            {
                                defaultList.MaybeAddNull();
                                defaultList.Add("Dimensions");
                                defaultList.Add((IPair<string>)null);
                                defaultList.Add(localList);
                            }
                        }

                        bool formatting = FlagOps.HasFlags(
                            detailFlags, DetailFlags.HostFormatting, true);

                        if (formatting)
                        {
                            StringPairList localList = new StringPairList();

                            localList.Add("SectionsPerRow", defaultHost.SectionsPerRow.ToString());
                            localList.Add("CallStackLimit", defaultHost.CallStackLimit.ToString());
                            localList.Add("HistoryLimit", defaultHost.HistoryLimit.ToString());

                            localList.Add("Debug", defaultHost.Debug.ToString());
                            localList.Add("Exceptions", defaultHost.Exceptions.ToString());
                            localList.Add("Display", defaultHost.Display.ToString());
                            localList.Add("ReplaceNewLines", defaultHost.ReplaceNewLines.ToString());
                            localList.Add("Ellipsis", defaultHost.Ellipsis.ToString());

                            if (empty || (defaultHost.NameValueFormat != null))
                                localList.Add("NameValueFormat", (defaultHost.NameValueFormat != null) ?
                                    defaultHost.NameValueFormat : FormatOps.DisplayNull);

                            localList.Add("OutputStyle",
                                defaultHost.OutputStyle.ToString());

                            if (empty || (defaultHost.GoPrompt != null))
                                localList.Add("GoPrompt", (defaultHost.GoPrompt != null) ?
                                    defaultHost.GoPrompt : FormatOps.DisplayNull);

                            if (empty || (defaultHost.StopPrompt != null))
                                localList.Add("StopPrompt", (defaultHost.StopPrompt != null) ?
                                    defaultHost.StopPrompt : FormatOps.DisplayNull);

                            localList.Add("BoxCharacterSet",
                                defaultHost.BoxCharacterSet.ToString());

                            StringList boxCharacterSets = defaultHost.BoxCharacterSets;

                            if (empty || (boxCharacterSets != null))
                                localList.Add("BoxCharacterSets", (boxCharacterSets != null) ?
                                    boxCharacterSets.ToRawString() : FormatOps.DisplayNull);

                            if (localList.Count > 0)
                            {
                                defaultList.MaybeAddNull();
                                defaultList.Add("Formatting");
                                defaultList.Add((IPair<string>)null);
                                defaultList.Add(localList);
                            }
                        }

                        //
                        // NOTE: Check if they want to output the color settings.
                        //
                        bool colors = FlagOps.HasFlags(
                            detailFlags, DetailFlags.HostColors, true);

                        List<PropertyInfo> colorPropertyInfoList = colors ? GetColorProperties(
                            GetType(), MatchMode.None, null, false, true, false) : null;

                        if (colors &&
                            (colorPropertyInfoList != null) && (colorPropertyInfoList.Count > 0))
                        {
                            StringPairList localList = new StringPairList();

                            foreach (PropertyInfo propertyInfo in colorPropertyInfoList)
                            {
                                if (propertyInfo != null)
                                {
                                    string name = propertyInfo.Name;
                                    object value = propertyInfo.GetValue(this, null);

                                    //
                                    // NOTE: If the resulting value is a color (as
                                    //       it MUST always be by this point), try
                                    //       to extract it; otherwise, leave the
                                    //       color value null.
                                    //
                                    ConsoleColor? color = null;

                                    if (value is ConsoleColor)
                                        color = (ConsoleColor)value;

                                    //
                                    // HACK: Emit the string "None" for the color
                                    //       value -1 (a.k.a. _ConsoleColor.None)
                                    //       because it is not an official member
                                    //       of the real ConsoleColor enumeration.
                                    //
                                    if (color != null)
                                    {
                                        ConsoleColor localColor = (ConsoleColor)color;

                                        if (empty || (localColor != _ConsoleColor.None))
                                        {
                                            localList.Add(name, FormatOps.DisplayColor(
                                                localColor));
                                        }
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: Ok, we have no idea what this is;
                                        //       however, emit it anyhow.
                                        //
                                        if (empty || (value != null))
                                        {
                                            localList.Add(name, (value != null) ?
                                                value.ToString() : FormatOps.DisplayNull);
                                        }
                                    }
                                }
                            }

                            if (localList.Count > 0)
                            {
                                defaultList.MaybeAddNull();
                                defaultList.Add("Host Colors");
                                defaultList.Add((IPair<string>)null);
                                defaultList.Add(localList);
                            }

                            if (interpreter != null)
                            {
                                localList = new StringPairList();

                                interpreter.GetHostColorInfo(
                                    ref localList, detailFlags);

                                if (localList.Count > 0)
                                {
                                    defaultList.MaybeAddNull();
                                    defaultList.Add("Interpreter Colors");
                                    defaultList.Add((IPair<string>)null);
                                    defaultList.Add(localList);
                                }
                            }
                        }

                        if (defaultList.Count > 0)
                        {
                            list.MaybeAddNull();
                            list.Add("Default Host");
                            // list.Add((IPair<string>)null);
                            list.Add(defaultList);
                        }
                    }

                    _Hosts.File fileHost = host as _Hosts.File;

                    if (fileHost != null)
                    {
                        StringPairList localList = new StringPairList();

                        if (empty || (fileHost.LibraryResourceBaseName != null))
                            localList.Add("LibraryResourceBaseName", (fileHost.LibraryResourceBaseName != null) ?
                                fileHost.LibraryResourceBaseName : FormatOps.DisplayNull);

                        if (empty || (fileHost.LibraryResourceManager != null))
                            localList.Add("LibraryResourceManager", (fileHost.LibraryResourceManager != null) ?
                                fileHost.LibraryResourceManager.ToString() : FormatOps.DisplayNull);

                        if (empty || (fileHost.PackagesResourceBaseName != null))
                            localList.Add("PackagesResourceBaseName", (fileHost.PackagesResourceBaseName != null) ?
                                fileHost.PackagesResourceBaseName : FormatOps.DisplayNull);

                        if (empty || (fileHost.PackagesResourceManager != null))
                            localList.Add("PackagesResourceManager", (fileHost.PackagesResourceManager != null) ?
                                fileHost.PackagesResourceManager.ToString() : FormatOps.DisplayNull);

                        if (empty || (fileHost.ApplicationResourceBaseName != null))
                            localList.Add("ApplicationResourceBaseName", (fileHost.ApplicationResourceBaseName != null) ?
                                fileHost.ApplicationResourceBaseName : FormatOps.DisplayNull);

                        if (empty || (fileHost.ApplicationResourceManager != null))
                            localList.Add("ApplicationResourceManager", (fileHost.ApplicationResourceManager != null) ?
                                fileHost.ApplicationResourceManager.ToString() : FormatOps.DisplayNull);

                        if (empty || (fileHost.ResourceManager != null))
                            localList.Add("ResourceManager", (fileHost.ResourceManager != null) ?
                                fileHost.ResourceManager.ToString() : FormatOps.DisplayNull);

                        if (empty || (fileHost.LibraryScriptFlags != ScriptFlags.None))
                            localList.Add("LibraryScriptFlags", fileHost.LibraryScriptFlags.ToString());

                        if (localList.Count > 0)
                        {
                            list.Add((IPair<string>)null);
                            list.Add("File Host");
                            list.Add((IPair<string>)null);
                            list.Add(localList);
                        }
                    }

                    _Hosts.Profile profileHost = host as _Hosts.Profile;

                    if (profileHost != null)
                    {
                        StringPairList localList = new StringPairList();

                        if (empty || (profileHost.TypeName != null))
                            localList.Add("TypeName", (profileHost.TypeName != null) ?
                                profileHost.TypeName : FormatOps.DisplayNull);

                        if (empty || (profileHost.HostProfileFileEncoding != null))
                            localList.Add("HostProfileFileEncoding", (profileHost.HostProfileFileEncoding != null) ?
                                profileHost.HostProfileFileEncoding.WebName : FormatOps.DisplayNull);

                        if (empty || (profileHost.HostProfileFileName != null))
                            localList.Add("HostProfileFileName", (profileHost.HostProfileFileName != null) ?
                                profileHost.HostProfileFileName : FormatOps.DisplayNull);

                        if (localList.Count > 0)
                        {
                            list.Add((IPair<string>)null);
                            list.Add("Profile Host");
                            list.Add((IPair<string>)null);
                            list.Add(localList);
                        }
                    }

#if CONSOLE
                    _Hosts.Console consoleHost = host as _Hosts.Console;

                    if (consoleHost != null)
                    {
                        StringPairList localList = new StringPairList();

                        if (empty || (consoleHost.SharedReadLevels > 0))
                            localList.Add("SharedReadLevels",
                                consoleHost.SharedReadLevels.ToString());

                        if (empty || (consoleHost.SharedWriteLevels > 0))
                            localList.Add("SharedWriteLevels",
                                consoleHost.SharedWriteLevels.ToString());

                        if (empty || (consoleHost.CancelReadLevels > 0))
                            localList.Add("CancelReadLevels",
                                consoleHost.CancelReadLevels.ToString());

                        if (localList.Count > 0)
                        {
                            list.Add((IPair<string>)null);
                            list.Add("Console Host");
                            list.Add((IPair<string>)null);
                            list.Add(localList);
                        }
                    }
#endif

                    bool state = FlagOps.HasFlags(
                        detailFlags, DetailFlags.HostState, true);

                    if (state && FlagOps.HasFlags(
                            host.GetHostFlags(), HostFlags.QueryState, true))
                    {
                        StringList queryList = host.QueryState(detailFlags);

                        if (queryList != null)
                        {
                            StringPairList localList = new StringPairList();

                            if (queryList.Count % 2 != 0)
                                queryList.Add((string)null);

                            for (int index = 0; index < queryList.Count; index += 2)
                            {
                                string queryValue = queryList[index + 1];

                                localList.Add(
                                    queryList[index], (queryValue != null) ?
                                    queryValue : FormatOps.DisplayNull);
                            }

                            if (localList.Count > 0)
                            {
                                list.Add((IPair<string>)null);
                                list.Add("Host State");
                                list.Add((IPair<string>)null);
                                list.Add(localList);
                            }
                        }
                    }
                }
                else
                {
                    list.Add("Host", FormatOps.DisplayNull);
                }
            }
            else
            {
                list.Add("Host", FormatOps.DisplayNull);
            }

#if NATIVE && WINDOWS
            if (FlagOps.HasFlags(detailFlags, DetailFlags.NativeConsole, true))
                NativeConsole.AddInfo(list, detailFlags);
#endif

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Platform Introspection Methods
        protected virtual bool ShouldTreatAsMono()
        {
            return CommonOps.Runtime.IsMono();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool ShouldTreatAsDotNetCore()
        {
            return CommonOps.Runtime.IsDotNetCore();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Interpreter Introspection Methods
        protected virtual bool BuildAuxiliaryInterpreterInfoList(
            Interpreter interpreter,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            return HostOps.BuildInterpreterInfoList(
                interpreter, String.Empty, detailFlags, ref list);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool BuildInterpreterInfoList(
            Interpreter interpreter,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Interpreter");
                list.Add((IPair<string>)null);
            }

            if (interpreter != null)
            {
                int headerCount = list.Count;

                interpreter.GetHostInterpreterInfo(
                    ref list, detailFlags);

                if (list.Count == headerCount)
                    list.Add(FormatOps.DisplayEmpty);
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Engine Introspection Methods
        protected virtual bool BuildEngineInfoList(
            Interpreter interpreter,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Engine");
                list.Add((IPair<string>)null);
            }

            bool empty = HasEmptyContent(detailFlags);

            list.Add("ThrowOnDisposed",
                _Engine.IsThrowOnDisposed(null, false).ToString());

#if POLICY_TRACE
            bool policyTrace = GlobalState.PolicyTrace;

            if (empty || policyTrace)
                list.Add("PolicyTrace", policyTrace.ToString());
#endif

#if NATIVE
            ulong extraSpace = _Engine.GetExtraStackSpace();

            if (empty || (extraSpace > 0))
                list.Add("ExtraStackSpace", extraSpace.ToString());
#endif

            list.Add("CurrentAppDomain",
                AppDomainOps.GetCurrentId().ToString());

            list.Add("PrimaryAppDomain",
                AppDomainOps.GetPrimaryId().ToString());

            list.Add("IsCurrentDefaultAppDomain",
                AppDomainOps.IsCurrentDefault().ToString());

            list.Add("IsPrimaryDefaultAppDomain",
                AppDomainOps.IsPrimaryDefault().ToString());

            list.Add("IsPrimaryAppDomain",
                AppDomainOps.IsPrimary().ToString());

            bool sameAppDomain = AppDomainOps.IsSame(interpreter);

            list.Add("IsSameAppDomain", sameAppDomain.ToString());

            Thread currentThread = Thread.CurrentThread;

            if (empty || (currentThread != null))
                list.Add("CurrentManagedThread", (currentThread != null) ?
                    currentThread.ManagedThreadId.ToString() : FormatOps.DisplayNull);

            list.Add("CurrentNativeThread",
                AppDomain.GetCurrentThreadId().ToString()); /* EXEMPT */

            list.Add("GlobalStateCurrentThread",
                GlobalState.GetCurrentThreadId().ToString()); /* EXEMPT */

            list.Add("GlobalStateCurrentSystemThreadId",
                GlobalState.GetCurrentSystemThreadId().ToString());

            list.Add("GlobalStateCurrentContextThreadId",
                GlobalState.GetCurrentContextThreadId().ToString());

            list.Add("GlobalStateCurrentManagedThread",
                GlobalState.GetCurrentManagedThreadId().ToString());

            list.Add("GlobalStateCurrentNativeThread",
                GlobalState.GetCurrentNativeThreadId().ToString());

            list.Add("GlobalStatePrimaryThread",
                GlobalState.GetPrimaryThreadId().ToString());

            list.Add("GlobalStatePrimaryManagedThread",
                GlobalState.GetPrimaryManagedThreadId().ToString());

            list.Add("GlobalStatePrimaryNativeThread",
                GlobalState.GetPrimaryNativeThreadId().ToString());

            EventCallback newInterpreterCallback = Interpreter.NewInterpreterCallback;

            if (empty || ((newInterpreterCallback != null) && (newInterpreterCallback.Method != null)))
                list.Add("NewInterpreterCallback", (newInterpreterCallback != null) ?
                    FormatOps.DelegateMethodName(newInterpreterCallback.Method, false, true) :
                    FormatOps.DisplayNull);

            EventCallback useInterpreterCallback = Interpreter.UseInterpreterCallback;

            if (empty || ((useInterpreterCallback != null) && (useInterpreterCallback.Method != null)))
                list.Add("UseInterpreterCallback", (useInterpreterCallback != null) ?
                    FormatOps.DelegateMethodName(useInterpreterCallback.Method, false, true) :
                    FormatOps.DisplayNull);

            EventCallback freeInterpreterCallback = Interpreter.FreeInterpreterCallback;

            if (empty || ((freeInterpreterCallback != null) && (freeInterpreterCallback.Method != null)))
                list.Add("FreeInterpreterCallback", (freeInterpreterCallback != null) ?
                    FormatOps.DelegateMethodName(freeInterpreterCallback.Method, false, true) :
                    FormatOps.DisplayNull);

            NewHostCallback newHostCallback = Interpreter.NewHostCallback;

            if (empty || ((newHostCallback != null) && (newHostCallback.Method != null)))
                list.Add("NewHostCallback", (newHostCallback != null) ?
                    FormatOps.DelegateMethodName(newHostCallback.Method, false, true) :
                    FormatOps.DisplayNull);

            ComplainCallback complainCallback = Interpreter.ComplainCallback;

            if (empty || ((complainCallback != null) && (complainCallback.Method != null)))
                list.Add("ComplainCallback", (complainCallback != null) ?
                    FormatOps.DelegateMethodName(complainCallback.Method, false, true) :
                    FormatOps.DisplayNull);

            if (interpreter != null)
                interpreter.GetHostEngineInfo(ref list, detailFlags);

            BuildAuxiliaryInterpreterInfoList(interpreter, detailFlags, ref list);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.CommandCallback, true))
                CommandCallback.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.CommandCallbackWrapper, true))
                CommandCallbackWrapper.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.ParserOpsData, true))
                ParserOpsData.AddInfo(list, detailFlags);

#if NATIVE
            bool native = FlagOps.HasFlags(
                detailFlags, DetailFlags.EngineNative, true);

            if (native)
            {
#if TCL && NATIVE_PACKAGE
                if (FlagOps.HasFlags(detailFlags, DetailFlags.NativePackage, true))
                    NativePackage.AddInfo(list, detailFlags);
#endif

                if (FlagOps.HasFlags(detailFlags, DetailFlags.ArrayOps, true))
                    ArrayOps.AddInfo(list, detailFlags);

#if NATIVE_UTILITY
                if (FlagOps.HasFlags(detailFlags, DetailFlags.NativeUtility, true))
                    NativeUtility.AddInfo(list, detailFlags);
#endif

                if (FlagOps.HasFlags(detailFlags, DetailFlags.NativeStack, true))
                    NativeStack.AddInfo(list, detailFlags);
            }
#endif

            if (FlagOps.HasFlags(detailFlags, DetailFlags.EngineThread, true))
                EngineThread.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.PathOps, true))
                PathOps.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.FactoryOps, true))
                FactoryOps.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.HashOps, true))
                HashOps.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.ProcessOps, true))
                ProcessOps.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.ThreadOps, true))
                ThreadOps.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.SetupOps, true))
                SetupOps.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.TraceOps, true))
                TraceOps.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.TraceLimits, true))
                TraceLimits.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.ScriptOps, true))
                ScriptOps.AddInfo(list, detailFlags);

#if XML
            if (FlagOps.HasFlags(detailFlags, DetailFlags.ScriptXmlOps, true))
                ScriptXmlOps.AddInfo(list, detailFlags);
#endif

            if (FlagOps.HasFlags(detailFlags, DetailFlags.ScriptException, true))
                ScriptException.AddInfo(list, detailFlags);

#if TEST
            if (FlagOps.HasFlags(detailFlags, DetailFlags.TraceException, true))
                TraceException.AddInfo(list, detailFlags);
#endif

            if (FlagOps.HasFlags(detailFlags, DetailFlags.SyntaxOps, true))
                SyntaxOps.AddInfo(list, detailFlags);

#if NETWORK
            if (FlagOps.HasFlags(detailFlags, DetailFlags.WebOps, true))
                WebOps.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.SocketOps, true))
                SocketOps.AddInfo(list, detailFlags);
#endif

            if (FlagOps.HasFlags(detailFlags, DetailFlags.ConfigurationOps, true))
                ConfigurationOps.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.CertificateCacheInfo, true))
                CertificateOps.AddInfo(list, detailFlags);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Entity Introspection Methods
        protected virtual bool BuildEntityInfoList(
            Interpreter interpreter,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Entity");
                list.Add((IPair<string>)null);
            }

            if (interpreter != null)
            {
                int headerCount = list.Count;

                interpreter.GetHostEntityInfo(ref list, detailFlags);

                if (list.Count == headerCount)
                    list.Add(FormatOps.DisplayEmpty);
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Stack Introspection Methods
        protected virtual bool BuildStackInfoList(
            Interpreter interpreter, /* NOT USED */
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Stack");
                list.Add((IPair<string>)null);
            }

            //
            // NOTE: *WARNING* The numbers output here are NOT "live" as they
            //       are the values from the last trip through the interpreter
            //       stack checking code (i.e. which may be RADICALLY different
            //       from the current usage if we recently encountered a stack
            //       overflow).
            //
            UIntPtr used = UIntPtr.Zero;
            UIntPtr allocated = UIntPtr.Zero;
            UIntPtr extra = UIntPtr.Zero;
            UIntPtr margin = UIntPtr.Zero;
            UIntPtr maximum = UIntPtr.Zero;
            UIntPtr reserve = UIntPtr.Zero;
            UIntPtr commit = UIntPtr.Zero;

            Result error = null;

            if (RuntimeOps.GetStackSize(
                    ref used, ref allocated, ref extra,
                    ref margin, ref maximum, ref reserve,
                    ref commit, ref error) == ReturnCode.Ok)
            {
                list.Add("ThreadId",
                    GlobalState.GetCurrentNativeThreadId().ToString());

                list.Add((IPair<string>)null);
                list.Add("Used", used.ToString());
                list.Add("Allocated", allocated.ToString());
                list.Add("Extra", extra.ToString());
                list.Add("Margin", margin.ToString());
                list.Add("Maximum", maximum.ToString());
                list.Add("Reserve", reserve.ToString());
                list.Add("Commit", commit.ToString());
            }
            else
            {
                list.Add("Error", error);
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Control Introspection Methods
        protected virtual bool BuildControlInfoList(
            Interpreter interpreter,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Control");
                list.Add((IPair<string>)null);
            }

            if (interpreter != null)
            {
                int headerCount = list.Count;

                interpreter.GetHostControlInfo(ref list, detailFlags);

                if (list.Count == headerCount)
                    list.Add(FormatOps.DisplayEmpty);
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Test Introspection Methods
        protected virtual bool BuildTestInfoList(
            Interpreter interpreter,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Test");
                list.Add((IPair<string>)null);
            }

            if (interpreter != null)
            {
                int headerCount = list.Count;

                interpreter.GetHostTestInfo(ref list, detailFlags);

                if (list.Count == headerCount)
                    list.Add(FormatOps.DisplayEmpty);
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }

            if (FlagOps.HasFlags(detailFlags, DetailFlags.TestOps, true))
                TestOps.AddInfo(list, detailFlags);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Token Introspection Methods
        protected virtual bool BuildTokenInfoList(
            Interpreter interpreter, /* NOT USED */
            IToken token,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("Token");
                list.Add((IPair<string>)null);
            }

            if (token != null)
                list.Add(token.ToList());
            else
                list.Add(FormatOps.DisplayNull);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Trace Introspection Methods
        protected virtual bool BuildTraceInfoList(
            Interpreter interpreter,
            ITraceInfo traceInfo,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            bool cached = FlagOps.HasFlags(
                detailFlags, DetailFlags.TraceCached, true);

            if (cached)
            {
                if (interpreter != null)
                {
                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                    {
                        if (!interpreter.Disposed)
                        {
                            ITraceInfo interpreterTraceInfo = interpreter.TraceInfo;

                            if (interpreterTraceInfo != null)
                            {
                                list.Add("InterpreterTraceInfo");
                                list.Add((IPair<string>)null);
                                list.Add(interpreterTraceInfo.ToStringPairList());
                            }
                            else
                            {
                                list.Add("InterpreterTraceInfo");
                                list.Add((IPair<string>)null);
                                list.Add(FormatOps.DisplayNull);
                            }
                        }
                        else
                        {
                            list.Add("InterpreterTraceInfo");
                            list.Add((IPair<string>)null);
                            list.Add(FormatOps.DisplayDisposed);
                        }
                    }
                }
                else
                {
                    list.Add("InterpreterTraceInfo");
                    list.Add((IPair<string>)null);
                    list.Add(FormatOps.DisplayNull);
                }
            }
            else
            {
                if (traceInfo != null)
                {
                    list.Add("TraceInfo");
                    list.Add((IPair<string>)null);
                    list.Add(traceInfo.ToStringPairList());
                }
                else
                {
                    list.Add("TraceInfo");
                    list.Add((IPair<string>)null);
                    list.Add(FormatOps.DisplayNull);
                }
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region History Introspection Methods
#if HISTORY
        protected virtual bool BuildHistoryInfoList(
            Interpreter interpreter,
            IHistoryFilter historyFilter,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                list.Add("History");
                list.Add((IPair<string>)null);
            }

            if (interpreter != null)
            {
                int headerCount = list.Count;

                interpreter.GetHostHistoryItemInfo(
                    ref list, historyFilter, headerCount,
                    detailFlags);

                if (list.Count == headerCount)
                    list.Add(FormatOps.DisplayEmpty);
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }

            return true;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Result Introspection Methods
        protected virtual bool BuildResultInfoList(
            string name,
            ReturnCode code,
            Result result,
            int errorLine,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            if (list.Count == 0)
            {
                if (name == null)
                    name = ResultInfoBoxName;

                list.Add(name);
                list.Add((IPair<string>)null);
            }

            bool empty = HasEmptyContent(detailFlags);
            int headerCount = list.Count;

            if (empty || (result != null))
            {
                list.Add("Result",
                    FormatOps.DisplayResult(result, false, false));
            }

            if (empty || (code != ReturnCode.Ok))
                list.Add("ReturnCode", code.ToString());

            if (empty || (errorLine != 0))
                list.Add("ErrorLine", errorLine.ToString());

            ResultFlags flags = (result != null) ?
                result.Flags : ResultFlags.None;

            if (empty || (flags != ResultFlags.None))
                list.Add("Flags", flags.ToString());

            if (list.Count == headerCount)
                list.Add(FormatOps.DisplayEmpty);

            return true;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Test Methods
        protected virtual bool InTestMode()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.Test, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool HasTestFlags(HostTestFlags hasFlags, bool all)
        {
            return FlagOps.HasFlags(
                GetTestFlags(), hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Stream Helper Methods
        protected virtual bool DoesAutoFlushWriter()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.AutoFlushWriter, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Core Write Helper Methods
        #region Multi-Pass Core Write Helper Methods
        //
        // BUGFIX: When writing some text with a new line, always break
        //         it into two host write operations.  First, try to write
        //         the string, in color, without the terminating new line.
        //         Then, write the terminating new line by itself, without
        //         color.  This appears to prevent stray lines of color in
        //         the console window when writing past the "end" of the
        //         screen buffer.
        //
        protected virtual int[] GetPassesForWriteCore(
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            ) /* CANNOT RETURN NULL */
        {
            if (!newLine)
                return OnePassForWriteCore;

            if ((foregroundColor == _ConsoleColor.None) &&
                (backgroundColor == _ConsoleColor.None))
            {
                return OnePassForWriteCore;
            }

            if (DoesNoColorNewLine())
                return TwoPassesForWriteCore;

            return OnePassForWriteCore;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool ShouldWriteForPass(
            int pass
            )
        {
            return ((pass == 0) || (pass == 1));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool ShouldWriteLineForPass(
            int pass
            )
        {
            return ((pass == 0) || (pass == 2));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool ShouldFlushForPass(
            int pass
            )
        {
            return ((pass == 0) || (pass == 2));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesAutoFlushHost()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.AutoFlushHost, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesRestoreColorAfterWrite()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.RestoreColorAfterWrite, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool RestoreOrSetColors(
            ConsoleColor savedForegroundColor,
            ConsoleColor savedBackgroundColor
            )
        {
#if CONSOLE
            if (DoesRestoreColorAfterWrite())
            {
                //
                // HACK: This is only supported for hosts that derive
                //       from the built-in console host.
                //
                _Hosts.Console consoleHost = this as _Hosts.Console;

                if (consoleHost != null)
                    return consoleHost.RestoreColors();
            }
#endif

            return SetColors(
                true, true, savedForegroundColor, savedBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool WriteCoreNormalizeValue(
            ref char value,
            ref bool done
            )
        {
            //
            // NOTE: Do nothing.
            //
            if (done)
                return true;

            done = true;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool WriteCoreNormalizeValue(
            ref string value,
            ref bool done
            )
        {
            if (done)
                return true;

            if (value != null)
            {
                value = value.Replace(
                    Characters.DosNewLine,
                    Characters.NewLine.ToString());
            }

            done = true;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool WriteCore(
            WriteCharCallback writeCharCallback,
            WriteLineCallback writeLineCallback,
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            if ((writeCharCallback != null) && (!newLine || (writeLineCallback != null)) && (count >= 0))
            {
                try
                {
                    int[] passes = GetPassesForWriteCore(newLine, foregroundColor, backgroundColor);

                    if (passes != null)
                    {
                        bool didNormalize = false;
                        bool normalize = DoesNormalizeToNewLine();
                        bool color = DoesSupportColor();
                        bool adjust = DoesAdjustColor();
                        bool autoFlush = DoesAutoFlushHost();

                        foreach (int pass in passes)
                        {
                            bool shouldWriteForPass = ShouldWriteForPass(pass);
                            bool shouldWriteLineForPass = newLine && ShouldWriteLineForPass(pass);
                            bool shouldFlushForPass = autoFlush && ShouldFlushForPass(pass);

                            bool shouldColorForPass = color && shouldWriteForPass;
                            bool shouldAdjustForPass = adjust && shouldWriteForPass;

                            ConsoleColor savedForegroundColor = _ConsoleColor.None;
                            ConsoleColor savedBackgroundColor = _ConsoleColor.None;

                            if (shouldColorForPass && !GetColors(ref savedForegroundColor, ref savedBackgroundColor))
                                return false;

                            try
                            {
                                if (shouldAdjustForPass && !AdjustColors(ref foregroundColor, ref backgroundColor))
                                    return false;

                                if (shouldColorForPass && !SetColors(true, true, foregroundColor, backgroundColor))
                                    return false;

                                int wrote = 0;

                                if (shouldWriteForPass)
                                {
                                    while (count-- > 0)
                                    {
                                        if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                            return false;

                                        writeCharCallback(value); /* throw */
                                        wrote++;
                                    }
                                }

                                if (shouldWriteLineForPass)
                                {
                                    writeLineCallback(); /* throw */
                                    wrote++;
                                }

                                if (wrote == 0)
                                {
                                    //
                                    // NOTE: Nothing was written;
                                    //       therefore, no flush.
                                    //
                                    shouldFlushForPass = false;
                                }

                                if (shouldFlushForPass && !Flush())
                                    return false;
                            }
                            finally
                            {
                                if (shouldColorForPass)
                                    /* IGNORED */
                                    RestoreOrSetColors(savedForegroundColor, savedBackgroundColor);
                            }
                        }

                        return true;
                    }
                }
                catch (IOException)
                {
                    SetWriteException(true);
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Default).Name,
                        TracePriority.HostError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool WriteCore(
            WriteStringCallback writeStringCallback,
            WriteLineCallback writeLineCallback,
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            if ((writeStringCallback != null) && (!newLine || (writeLineCallback != null)))
            {
                try
                {
                    int[] passes = GetPassesForWriteCore(newLine, foregroundColor, backgroundColor);

                    if (passes != null)
                    {
                        bool didNormalize = false;
                        bool normalize = DoesNormalizeToNewLine();
                        bool color = DoesSupportColor();
                        bool adjust = DoesAdjustColor();
                        bool autoFlush = DoesAutoFlushHost();

                        foreach (int pass in passes)
                        {
                            bool shouldWriteForPass = ShouldWriteForPass(pass);
                            bool shouldWriteLineForPass = newLine && ShouldWriteLineForPass(pass);
                            bool shouldFlushForPass = autoFlush && ShouldFlushForPass(pass);

                            bool shouldColorForPass = color && shouldWriteForPass;
                            bool shouldAdjustForPass = adjust && shouldWriteForPass;

                            ConsoleColor savedForegroundColor = _ConsoleColor.None;
                            ConsoleColor savedBackgroundColor = _ConsoleColor.None;

                            if (shouldColorForPass && !GetColors(ref savedForegroundColor, ref savedBackgroundColor))
                                return false;

                            try
                            {
                                if (shouldAdjustForPass && !AdjustColors(ref foregroundColor, ref backgroundColor))
                                    return false;

                                if (shouldColorForPass && !SetColors(true, true, foregroundColor, backgroundColor))
                                    return false;

                                int wrote = 0;

                                if (shouldWriteForPass)
                                {
                                    if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                        return false;

                                    writeStringCallback(value); /* throw */
                                    wrote++;
                                }

                                if (shouldWriteLineForPass)
                                {
                                    writeLineCallback(); /* throw */
                                    wrote++;
                                }

                                if (wrote == 0)
                                {
                                    //
                                    // NOTE: Nothing was written;
                                    //       therefore, no flush.
                                    //
                                    shouldFlushForPass = false;
                                }

                                if (shouldFlushForPass && !Flush())
                                    return false;
                            }
                            finally
                            {
                                if (shouldColorForPass)
                                    /* IGNORED */
                                    RestoreOrSetColors(savedForegroundColor, savedBackgroundColor);
                            }
                        }

                        return true;
                    }
                }
                catch (IOException)
                {
                    SetWriteException(true);
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Default).Name,
                        TracePriority.HostError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool WriteCore(
            HostWriteType hostWriteType,
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            if (count >= 0)
            {
                try
                {
                    int[] passes = GetPassesForWriteCore(newLine, foregroundColor, backgroundColor);

                    if (passes != null)
                    {
                        bool didNormalize = false;
                        bool normalize = DoesNormalizeToNewLine();
                        bool color = DoesSupportColor();
                        bool adjust = DoesAdjustColor();
                        bool autoFlush = DoesAutoFlushHost();

                        foreach (int pass in passes)
                        {
                            bool shouldWriteForPass = ShouldWriteForPass(pass);
                            bool shouldWriteLineForPass = newLine && ShouldWriteLineForPass(pass);
                            bool shouldFlushForPass = autoFlush && ShouldFlushForPass(pass);

                            bool shouldColorForPass = color && shouldWriteForPass;
                            bool shouldAdjustForPass = adjust && shouldWriteForPass;

                            ConsoleColor savedForegroundColor = _ConsoleColor.None;
                            ConsoleColor savedBackgroundColor = _ConsoleColor.None;

                            if (shouldColorForPass && !GetColors(ref savedForegroundColor, ref savedBackgroundColor))
                                return false;

                            try
                            {
                                if (shouldAdjustForPass && !AdjustColors(ref foregroundColor, ref backgroundColor))
                                    return false;

                                if (shouldColorForPass && !SetColors(true, true, foregroundColor, backgroundColor))
                                    return false;

                                switch (hostWriteType)
                                {
                                    case HostWriteType.Normal:
                                        {
                                            int wrote = 0;

                                            if (shouldWriteForPass)
                                            {
                                                while (count-- > 0)
                                                {
                                                    if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                                        return false;

                                                    if (Write(value))
                                                        wrote++;
                                                    else
                                                        return false;
                                                }
                                            }

                                            if (shouldWriteLineForPass)
                                            {
                                                if (WriteLine())
                                                    wrote++;
                                                else
                                                    return false;
                                            }

                                            if (wrote == 0)
                                            {
                                                //
                                                // NOTE: Nothing was written;
                                                //       therefore, no flush.
                                                //
                                                shouldFlushForPass = false;
                                            }
                                            break;
                                        }
                                    case HostWriteType.Debug:
                                        {
                                            int wrote = 0;

                                            if (shouldWriteForPass)
                                            {
                                                while (count-- > 0)
                                                {
                                                    if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                                        return false;

                                                    if (WriteDebug(value))
                                                        wrote++;
                                                    else
                                                        return false;
                                                }
                                            }

                                            if (shouldWriteLineForPass)
                                            {
                                                if (WriteDebugLine())
                                                    wrote++;
                                                else
                                                    return false;
                                            }

                                            if (wrote == 0)
                                            {
                                                //
                                                // NOTE: Nothing was written;
                                                //       therefore, no flush.
                                                //
                                                shouldFlushForPass = false;
                                            }
                                            break;
                                        }
                                    case HostWriteType.Error:
                                        {
                                            int wrote = 0;

                                            if (shouldWriteForPass)
                                            {
                                                while (count-- > 0)
                                                {
                                                    if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                                        return false;

                                                    if (WriteError(value))
                                                        wrote++;
                                                    else
                                                        return false;
                                                }
                                            }

                                            if (shouldWriteLineForPass)
                                            {
                                                if (WriteErrorLine())
                                                    wrote++;
                                                else
                                                    return false;
                                            }

                                            if (wrote == 0)
                                            {
                                                //
                                                // NOTE: Nothing was written;
                                                //       therefore, no flush.
                                                //
                                                shouldFlushForPass = false;
                                            }
                                            break;
                                        }
                                    case HostWriteType.Flush:
                                        {
                                            //
                                            // NOTE: Do nothing and allow
                                            //       flush to occur (below)
                                            //       even though nothing has
                                            //       been written.
                                            //
                                            break;
                                        }
                                    default:
                                        {
                                            //
                                            // NOTE: Nothing was written;
                                            //       therefore, no flush.
                                            //
                                            shouldFlushForPass = false;
                                            break;
                                        }
                                }

                                if (shouldFlushForPass && !Flush())
                                    return false;
                            }
                            finally
                            {
                                if (shouldColorForPass)
                                    /* IGNORED */
                                    RestoreOrSetColors(savedForegroundColor, savedBackgroundColor);
                            }
                        }

                        return true;
                    }
                }
                catch (IOException)
                {
                    SetWriteException(true);
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Default).Name,
                        TracePriority.HostError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool WriteCore(
            HostWriteType hostWriteType,
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            try
            {
                int[] passes = GetPassesForWriteCore(newLine, foregroundColor, backgroundColor);

                if (passes != null)
                {
                    bool didNormalize = false;
                    bool normalize = DoesNormalizeToNewLine();
                    bool color = DoesSupportColor();
                    bool adjust = DoesAdjustColor();
                    bool autoFlush = DoesAutoFlushHost();

                    foreach (int pass in passes)
                    {
                        bool shouldWriteForPass = ShouldWriteForPass(pass);
                        bool shouldWriteLineForPass = newLine && ShouldWriteLineForPass(pass);
                        bool shouldFlushForPass = autoFlush && ShouldFlushForPass(pass);

                        bool shouldColorForPass = color && shouldWriteForPass;
                        bool shouldAdjustForPass = adjust && shouldWriteForPass;

                        ConsoleColor savedForegroundColor = _ConsoleColor.None;
                        ConsoleColor savedBackgroundColor = _ConsoleColor.None;

                        if (shouldColorForPass && !GetColors(ref savedForegroundColor, ref savedBackgroundColor))
                            return false;

                        try
                        {
                            if (shouldAdjustForPass && !AdjustColors(ref foregroundColor, ref backgroundColor))
                                return false;

                            if (shouldColorForPass && !SetColors(true, true, foregroundColor, backgroundColor))
                                return false;

                            //
                            // NOTE: *SPECIAL* If the caller wants a new-line and we are
                            //       operating in one-pass mode (i.e. both boolean flags
                            //       are true), just call the appropriate Write*Line()
                            //       method.
                            //
                            if (shouldWriteForPass && shouldWriteLineForPass)
                            {
                                switch (hostWriteType)
                                {
                                    case HostWriteType.Normal:
                                        {
                                            if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                                return false;

                                            if (!WriteLine(value))
                                                return false;

                                            break;
                                        }
                                    case HostWriteType.Debug:
                                        {
                                            if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                                return false;

                                            if (!WriteDebugLine(value))
                                                return false;

                                            break;
                                        }
                                    case HostWriteType.Error:
                                        {
                                            if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                                return false;

                                            if (!WriteErrorLine(value))
                                                return false;

                                            break;
                                        }
                                    case HostWriteType.Flush:
                                        {
                                            //
                                            // NOTE: Do nothing and allow
                                            //       flush to occur (below)
                                            //       even though nothing has
                                            //       been written.
                                            //
                                            break;
                                        }
                                    default:
                                        {
                                            //
                                            // NOTE: Nothing was written;
                                            //       therefore, no flush.
                                            //
                                            shouldFlushForPass = false;
                                            break;
                                        }
                                }
                            }
                            else
                            {
                                switch (hostWriteType)
                                {
                                    case HostWriteType.Normal:
                                        {
                                            int wrote = 0;

                                            if (shouldWriteForPass)
                                            {
                                                if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                                    return false;

                                                if (Write(value))
                                                    wrote++;
                                                else
                                                    return false;
                                            }

                                            if (shouldWriteLineForPass)
                                            {
                                                if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                                    return false;

                                                if (WriteLine())
                                                    wrote++;
                                                else
                                                    return false;
                                            }

                                            if (wrote == 0)
                                            {
                                                //
                                                // NOTE: Nothing was written;
                                                //       therefore, no flush.
                                                //
                                                shouldFlushForPass = false;
                                            }
                                            break;
                                        }
                                    case HostWriteType.Debug:
                                        {
                                            int wrote = 0;

                                            if (shouldWriteForPass)
                                            {
                                                if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                                    return false;

                                                if (WriteDebug(value))
                                                    wrote++;
                                                else
                                                    return false;
                                            }

                                            if (shouldWriteLineForPass)
                                            {
                                                if (WriteDebugLine())
                                                    wrote++;
                                                else
                                                    return false;
                                            }

                                            if (wrote == 0)
                                            {
                                                //
                                                // NOTE: Nothing was written;
                                                //       therefore, no flush.
                                                //
                                                shouldFlushForPass = false;
                                            }
                                            break;
                                        }
                                    case HostWriteType.Error:
                                        {
                                            int wrote = 0;

                                            if (shouldWriteForPass)
                                            {
                                                if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                                    return false;

                                                if (WriteError(value))
                                                    wrote++;
                                                else
                                                    return false;
                                            }

                                            if (shouldWriteLineForPass)
                                            {
                                                if (normalize && !WriteCoreNormalizeValue(ref value, ref didNormalize))
                                                    return false;

                                                if (WriteErrorLine())
                                                    wrote++;
                                                else
                                                    return false;
                                            }

                                            if (wrote == 0)
                                            {
                                                //
                                                // NOTE: Nothing was written;
                                                //       therefore, no flush.
                                                //
                                                shouldFlushForPass = false;
                                            }
                                            break;
                                        }
                                    case HostWriteType.Flush:
                                        {
                                            //
                                            // NOTE: Do nothing and allow
                                            //       flush to occur (below)
                                            //       even though nothing has
                                            //       been written.
                                            //
                                            break;
                                        }
                                    default:
                                        {
                                            //
                                            // NOTE: Nothing was written;
                                            //       therefore, no flush.
                                            //
                                            shouldFlushForPass = false;
                                            break;
                                        }
                                }
                            }

                            if (shouldFlushForPass && !Flush())
                                return false;
                        }
                        finally
                        {
                            if (shouldColorForPass)
                                /* IGNORED */
                                RestoreOrSetColors(savedForegroundColor, savedBackgroundColor);
                        }
                    }

                    return true;
                }
            }
            catch (IOException)
            {
                SetWriteException(true);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Default).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used by the various Write*Info() methods
        //       of the default host.
        //
        protected virtual bool WriteCore(
            OutputStyle outputStyle,
            string name,
            StringPairList list,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            if (IsBoxedOutputStyle(outputStyle))
            {
                return WriteBox(
                    name, list, null, false, true, ref hostLeft,
                    ref hostTop, foregroundColor, backgroundColor);
            }
            else if (IsFormattedOutputStyle(outputStyle))
            {
                return WriteFormat(
                    list, newLine, foregroundColor, backgroundColor);
            }
            else if (IsNoneOutputStyle(outputStyle))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Box Write Helper Methods
        protected virtual WhiteSpaceFlags GetBoxWhiteSpaceFlags(
            Encoding encoding
            )
        {
            WhiteSpaceFlags whiteSpaceFlags = WhiteSpaceFlags.BoxUse;

            //
            // HACK: When running on Windows Terminal (Cascadia),
            //       avoid using the Unicode arrow glyphs because
            //       the LeftwardsArrow (U+2190) appears to cause
            //       serious display (and other?) issues.
            //
            if (IsWindowsTerminal())
                whiteSpaceFlags |= WhiteSpaceFlags.NoArrows;

            //
            // HACK: Make sure the "Extended ASCII" and/or Unicode
            //       characters used with our boxes make sense for
            //       the specified encoding.  This is actually not
            //       100% correct.  What we really want to know is
            //       "Does a particular character end up occupying
            //       exactly one space on the console?"; however,
            //       checking that would require font information,
            //       among other things (e.g. character glyphs),
            //       and is deemed too complex for this code.
            //
            if (!StringOps.IsSingleByte(encoding, new string(
                    Characters.WhiteSpace_Extended), true))
            {
                whiteSpaceFlags &= ~WhiteSpaceFlags.Extended;
            }

            if (!StringOps.IsSingleByte(encoding, new string(
                    Characters.WhiteSpace_Unicode), true))
            {
                whiteSpaceFlags &= ~WhiteSpaceFlags.Unicode;
            }

            return whiteSpaceFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void InitializeBoxCharacterSets()
        {
            BoxCharacterSets = new StringList(Characters.BoxCharacterSets);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual string GetBoxCharacterSet() /* MAY RETURN NULL */
        {
            StringList boxCharacterSets = BoxCharacterSets;

            if (boxCharacterSets == null)
                return null;

            int index = BoxCharacterSet;

            if ((index < 0) || (index >= boxCharacterSets.Count))
                return null;

            return boxCharacterSets[index];
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual string GetFallbackBoxCharacterSet() /* MAY RETURN NULL */
        {
            return StringOps.StrRepeat(
                (int)BoxCharacter.Count, Characters.Space);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void SelectBoxCharacterSet()
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                StringList boxCharacterSets = BoxCharacterSets;

                if (boxCharacterSets == null)
                    return;

                int count = boxCharacterSets.Count;

                if (count > 0)
                {
                    BoxCharacterSet = count - 1;
                    return;
                }
            }
#endif

            BoxCharacterSet = 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool WriteLineForBox(
            HostWriteType hostWriteType
            )
        {
            switch (hostWriteType)
            {
                case HostWriteType.Normal:
                    return WriteLine();
                case HostWriteType.Debug:
                    return WriteDebugLine();
                case HostWriteType.Error:
                    return WriteErrorLine();
                case HostWriteType.Flush:
                    return Flush();
            }

            return false; /* NOTE: *FAIL* Unknown host write type. */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool WriteForBox(
            HostWriteType hostWriteType,
            char value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            return WriteForBox(
                hostWriteType, value, 1, false, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool WriteForBox(
            HostWriteType hostWriteType,
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            switch (hostWriteType)
            {
                case HostWriteType.Normal:
                    {
                        return Write(
                            value, count, newLine, foregroundColor,
                            backgroundColor);
                    }
                case HostWriteType.Debug:
                    {
                        return WriteDebug(
                            value, count, newLine, foregroundColor,
                            backgroundColor);
                    }
                case HostWriteType.Error:
                    {
                        return WriteError(
                            value, count, newLine, foregroundColor,
                            backgroundColor);
                    }
                case HostWriteType.Flush:
                    {
                        return Flush();
                    }
            }

            return false; /* NOTE: *FAIL* Unknown host write type. */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool WriteForBox(
            HostWriteType hostWriteType,
            string value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            return WriteForBox(
                hostWriteType, value, false, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool WriteForBox(
            HostWriteType hostWriteType,
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            switch (hostWriteType)
            {
                case HostWriteType.Normal:
                    {
                        return Write(
                            value, newLine, foregroundColor,
                            backgroundColor);
                    }
                case HostWriteType.Debug:
                    {
                        return WriteDebug(
                            value, newLine, foregroundColor,
                            backgroundColor);
                    }
                case HostWriteType.Error:
                    {
                        return WriteError(
                            value, newLine, foregroundColor,
                            backgroundColor);
                    }
                case HostWriteType.Flush:
                    {
                        return Flush();
                    }
            }

            return false; /* NOTE: *FAIL* Unknown host write type. */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Type Methods
        protected virtual bool IsDefaultHost(
            IHost host
            )
        {
            return (host is _Hosts.Default);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool IsEngineHost(
            IHost host
            )
        {
            return (host is _Hosts.Engine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool IsFileHost(
            IHost host
            )
        {
            return (host is _Hosts.File);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool IsProfileHost(
            IHost host
            )
        {
            return (host is _Hosts.Profile);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool IsShellHost(
            IHost host
            )
        {
            return (host is _Hosts.Shell);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool IsCoreHost(
            IHost host
            )
        {
            return (host is _Hosts.Core);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool IsConsoleHost(
            IHost host
            )
        {
#if CONSOLE
            return (host is _Hosts.Console);
#else
            return false;
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Environment Detection Methods
        protected virtual bool IsX11Terminal()
        {
            return WindowOps.IsX11Terminal();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool IsWindowsTerminal()
        {
            return WindowOps.IsWindowsTerminal();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Capability Detection Methods
        protected virtual bool DoesSupportColor()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.NonMonochromeMask, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesSupportReversedColor()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.ReversedColor, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesSupportSizing()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.Sizing, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesSupportPositioning()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.Positioning, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesAdjustColor()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.AdjustColor, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesNoColorNewLine()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.NoColorNewLine, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesSavedColorForNone()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.SavedColorForNone, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesResetColorForRestore()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.ResetColorForRestore, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool DoesMaybeResetColorForSet()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.MaybeResetColorForSet, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesTraceColorNotChanged()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.TraceColorNotChanged, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesNoSetForegroundColor()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.NoSetForegroundColor, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesNoSetBackgroundColor()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.NoSetBackgroundColor, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesNormalizeToNewLine()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.NormalizeToNewLine, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Content Section Methods
        protected virtual bool IsAtLeastSize(
            HostFlags flags,
            HostFlags hasFlags
            )
        {
            if (!FlagOps.HasFlags(hasFlags, HostFlags.AllSizes, false))
                return false;

            if (FlagOps.HasFlags(flags, HostFlags.UnlimitedSize, true))
                return true;

            if (FlagOps.HasFlags(hasFlags, HostFlags.SuperJumboSize, true) &&
                FlagOps.HasFlags(flags, HostFlags.SuperJumboSize, true))
            {
                return true;
            }

            if (FlagOps.HasFlags(hasFlags, HostFlags.JumboSize, true) &&
                (FlagOps.HasFlags(flags, HostFlags.SuperJumboSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.JumboSize, true)))
            {
                return true;
            }

            if (FlagOps.HasFlags(hasFlags, HostFlags.SuperFullSize, true) &&
                (FlagOps.HasFlags(flags, HostFlags.SuperJumboSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.JumboSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.SuperFullSize, true)))
            {
                return true;
            }

            if (FlagOps.HasFlags(hasFlags, HostFlags.FullSize, true) &&
                (FlagOps.HasFlags(flags, HostFlags.SuperJumboSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.JumboSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.SuperFullSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.FullSize, true)))
            {
                return true;
            }

            if (FlagOps.HasFlags(hasFlags, HostFlags.CompactSize, true) &&
                (FlagOps.HasFlags(flags, HostFlags.SuperJumboSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.JumboSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.SuperFullSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.FullSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.CompactSize, true)))
            {
                return true;
            }

            if (FlagOps.HasFlags(hasFlags, HostFlags.MinimumSize, true) &&
                (FlagOps.HasFlags(flags, HostFlags.SuperJumboSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.JumboSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.SuperFullSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.FullSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.CompactSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.MinimumSize, true)))
            {
                return true;
            }

            if (FlagOps.HasFlags(hasFlags, HostFlags.ZeroSize, true) &&
                (FlagOps.HasFlags(flags, HostFlags.SuperJumboSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.JumboSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.SuperFullSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.FullSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.CompactSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.MinimumSize, true) ||
                 FlagOps.HasFlags(flags, HostFlags.ZeroSize, true)))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool DoesHeaderFit(
            HeaderFlags headerFlags,
            HostFlags hostFlags
            )
        {
            HostFlags hasFlags;

            if ((sectionSizes != null) &&
                sectionSizes.TryGetValue(headerFlags, out hasFlags) &&
                IsAtLeastSize(hostFlags, hasFlags))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool BeginSection(
            bool nextRow,
            int minimumLeft,
            ref int maximumTop,
            ref int savedTop,
            ref int count
            )
        {
            if (IsBoxedOutputStyle(OutputStyle))
            {
                if (nextRow || (savedTop == _Position.Invalid))
                {
                    //
                    // NOTE: Advance to the start of the next line.
                    //
                    hostLeft = minimumLeft;
                    hostTop = ++maximumTop;

                    savedTop = hostTop;
                    count = 0;
                }
                else
                {
                    hostLeft++;
                    hostTop = savedTop;

                    count++;
                }

                return true;
            }
            else if (IsFormattedOutputStyle(OutputStyle))
            {
                //
                // NOTE: Advance to the start of the next line.
                //
                hostLeft = minimumLeft;
                hostTop = ++maximumTop;

                //
                // NOTE: For formatted mode, always update the
                //       actual output position with our internal
                //       variables.
                //
                if (DoesSupportPositioning() &&
                    !SetPosition(hostLeft, hostTop))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool EndSection(
            ref int maximumLeft,
            ref int maximumTop
            )
        {
            //
            // NOTE: For formatted mode, always update the
            //       internal variables to our actual output
            //       position.
            //
            if (!IsBoxedOutputStyle(OutputStyle) &&
                DoesSupportPositioning() &&
                !GetPosition(ref hostLeft, ref hostTop))
            {
                return false;
            }

            //
            // NOTE: For both supported modes, check and update
            //       the maximum output positions we have seen
            //       so far.
            //
            if (IsFormattedOutputStyle(OutputStyle) ||
                IsBoxedOutputStyle(OutputStyle))
            {
                if (hostLeft > maximumLeft)
                    maximumLeft = hostLeft;

                if (hostTop > maximumTop)
                    maximumTop = hostTop;

                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public virtual string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        public virtual ReturnCode BeginProcessing(
            int levels,
            ref string text,
            ref Result error
            )
        {
            CheckDisposed();

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual ReturnCode EndProcessing(
            int levels,
            ref string text,
            ref Result error
            )
        {
            CheckDisposed();

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual ReturnCode DoneProcessing(
            int levels,
            ref Result error
            )
        {
            CheckDisposed();

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string title;
        public virtual string Title
        {
            get { CheckDisposed(); return title; }
            set { CheckDisposed(); title = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool RefreshTitle(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool IsInputRedirected(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode Prompt(
            PromptType type,
            ref PromptFlags flags,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool IsOpen(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool Pause(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool Flush(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private HeaderFlags headerFlags = HeaderFlags.Default;
        public virtual HeaderFlags GetHeaderFlags()
        {
            CheckDisposed();

            return PrivateGetHeaderFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private HostFlags hostFlags = HostFlags.Invalid;
        public virtual HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract int ReadLevels { get; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract int WriteLevels { get; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool ReadLine(
            ref string value
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value
            )
        {
            CheckDisposed();

            return Write(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value
            )
        {
            CheckDisposed();

            return Write(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool WriteLine(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteLine(
            string value
            )
        {
            CheckDisposed();

            return Write(value, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteResultLine(
            ReturnCode code,
            Result result
            )
        {
            CheckDisposed();

            return WriteResult(code, result, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteResultLine(
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
            CheckDisposed();

            return WriteResult(code, result, errorLine, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IFileSystemHost Members
        private HostStreamFlags streamFlags = HostStreamFlags.Default;
        public virtual HostStreamFlags StreamFlags
        {
            get { CheckDisposed(); return streamFlags; }
            set { CheckDisposed(); streamFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode GetStream(
            string path,
            FileMode mode,
            FileAccess access,
            FileShare share,
            int bufferSize,
            FileOptions options,
            ref HostStreamFlags hostStreamFlags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode GetData(
            string name,
            DataFlags dataFlags,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result result
            ); /* PRIMITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IProcessHost Members
        public virtual bool CanExit
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.CanExit, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.CanExit, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool CanForceExit
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.CanForceExit, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.CanForceExit, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Exiting
        {
            get { CheckDisposed(); return IsExiting(); }
            set { CheckDisposed(); SetExiting(value); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IThreadHost Members
        public abstract ReturnCode CreateThread(
            ThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode CreateThread(
            ParameterizedThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode QueueWorkItem(
            WaitCallback callback,
            object state,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool Sleep(
            int milliseconds
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool Yield(); /* PRIMITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        public virtual Stream DefaultIn
        {
            get { CheckDisposed(); return In; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual Stream DefaultOut
        {
            get { CheckDisposed(); return Out; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual Stream DefaultError
        {
            get { CheckDisposed(); return Error; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract Stream In { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract Stream Out { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract Stream Error { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract Encoding InputEncoding { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract Encoding OutputEncoding { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract Encoding ErrorEncoding { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool ResetIn(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool ResetOut(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool ResetError(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool IsOutputRedirected(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool IsErrorRedirected(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool SetupChannels(); /* PRIMITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        public abstract IHost Clone(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract IHost Clone(
            Interpreter interpreter
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract HostTestFlags GetTestFlags(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode Cancel(
            bool force,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode Exit(
            bool force,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool WriteDebugLine(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebugLine(
            string value
            )
        {
            CheckDisposed();

            return WriteDebug(value, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            char value
            )
        {
            CheckDisposed();

            return WriteDebug(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool WriteDebug(
            char value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return WriteCore(HostWriteType.Debug, value, count, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            string value
            )
        {
            CheckDisposed();

            return WriteDebug(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool WriteDebug(
            string value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return WriteDebug(value, newLine, foregroundColor, DebugBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return WriteCore(HostWriteType.Debug, value, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool WriteErrorLine(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteErrorLine(
            string value
            )
        {
            CheckDisposed();

            return WriteError(value, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            char value
            )
        {
            CheckDisposed();

            return WriteError(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool WriteError(
            char value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return WriteCore(HostWriteType.Error, value, count, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            string value
            )
        {
            CheckDisposed();

            return WriteError(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool WriteError(
            string value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            bool isFatal = ShouldTreatAsFatalError();

            return WriteError(
                value, newLine, foregroundColor, isFatal ?
                FatalBackgroundColor : ErrorBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return WriteCore(HostWriteType.Error, value, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            ReturnCode code,
            Result result,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteResult(code, result, false, newLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            ReturnCode code,
            Result result,
            bool raw,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteResult(null, code, result, 0, raw, newLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            ReturnCode code,
            Result result,
            int errorLine,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteResult(null, code, result, errorLine, false, newLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            ReturnCode code,
            Result result,
            int errorLine,
            bool raw,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteResult(null, code, result, errorLine, raw, newLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            string prefix,
            ReturnCode code,
            Result result,
            int errorLine,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteResult(prefix, code, result, errorLine, false, newLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            string prefix,
            ReturnCode code,
            Result result,
            int errorLine,
            bool raw,
            bool newLine
            )
        {
            CheckDisposed();

            ReturnCode returnCode;

            if ((result == null) || (result.ReturnCode == ReturnCode.Ok))
                returnCode = code;
            else
                returnCode = result.ReturnCode;

            bool wrote;
            string formatted;

            if (raw)
            {
                formatted = result;
            }
            else
            {
                //
                // TODO: Possibly have the caller pass in the exceptions
                //       argument and the other three arguments that are
                //       simply hard-coded to false here (i.e. ellipsis,
                //       replaceNewLines, and strict).
                //
                formatted = FormatResult(
                    prefix, returnCode, result, errorLine, Exceptions,
                    Display, false, false, false);
            }

            if (!String.IsNullOrEmpty(formatted))
            {
                ConsoleColor foregroundColor = ResultForegroundColor;
                ConsoleColor backgroundColor = ResultBackgroundColor;

                if (DoesSupportReversedColor())
                {
                    MaybeSwapTextColors(
                        ref foregroundColor, ref backgroundColor);
                }

                GetResultColors(
                    returnCode, result, ref foregroundColor,
                    ref backgroundColor);

                wrote = Write(
                    formatted, newLine, foregroundColor, backgroundColor);
            }
            else
            {
                wrote = false;
            }

            ///////////////////////////////////////////////////////////////////

            #region Break-On-Exiting Support
#if BREAK_ON_EXITING
            CheckOkResultIfExiting(returnCode, result);
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            return wrote;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInformationHost Members
        public virtual bool SavePosition()
        {
            CheckDisposed();

            if (!IsBoxedOutputStyle(OutputStyle))
                return true;

            if (!DoesSupportPositioning())
                return true;

            return GetPosition(ref hostLeft, ref hostTop);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool RestorePosition(bool newLine)
        {
            CheckDisposed();

            if (!IsBoxedOutputStyle(OutputStyle))
                return true;

            if (!DoesSupportPositioning())
                return !newLine || WriteLine();

            if (newLine)
                return SetPosition(0, hostTop);
            else
                return SetPosition(hostLeft, hostTop);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteAnnouncementInfo(
            Interpreter interpreter,
            BreakpointType breakpointType,
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteAnnouncementInfo(
                interpreter, breakpointType, value,
                newLine, AnnouncementInfoForegroundColor,
                AnnouncementInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteAnnouncementInfo(
            Interpreter interpreter,
            BreakpointType breakpointType,
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            if (IsNoneOutputStyle(OutputStyle))
                return true;

            if (DoesSupportReversedColor())
                MaybeSwapTextColors(ref foregroundColor, ref backgroundColor);

            //
            // BUGFIX: To avoid writing more than one line of color, make sure
            //         the initial line-terminator is written all by itself.
            //
            if (!WriteLine())
                return false;

            if (!Write(StringOps.PadCenter(FormatAnnouncement(
                        interpreter, breakpointType, value),
                    ContentWidth - ContentMargin, Characters.Space),
                    false, foregroundColor, backgroundColor))
            {
                return false;
            }

            //
            // BUGFIX: To avoid writing more than one line of color, make sure
            //         the trailing line-terminator, if any, is written all by
            //         itself.
            //
            if (newLine && !WriteLine())
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteArgumentInfo(
            Interpreter interpreter,
            ReturnCode code,
            BreakpointType breakpointType,
            string breakpointName,
            ArgumentList arguments,
            Result result,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteArgumentInfo(
                interpreter, code, breakpointType, breakpointName, arguments,
                result, detailFlags, newLine, ArgumentInfoForegroundColor,
                ArgumentInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteArgumentInfo(
            Interpreter interpreter,
            ReturnCode code,
            BreakpointType breakpointType,
            string breakpointName,
            ArgumentList arguments,
            Result result,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildArgumentInfoList(
                    code, breakpointType, breakpointName, arguments,
                    result, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, ArgumentInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallFrame(
            Interpreter interpreter,
            ICallFrame frame,
            string type,
            string prefix,
            string suffix,
            char separator,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            if (IsNoneOutputStyle(OutputStyle))
                return true;

            string value = (frame != null) ?
                Characters.Colon.ToString() + Characters.Space.ToString() +
                frame.ToString(detailFlags) : String.Empty;

            bool linked = FlagOps.HasFlags(
                detailFlags, DetailFlags.CallFrameLinked, true);

            bool special = FlagOps.HasFlags(
                detailFlags, DetailFlags.CallFrameSpecial, true);

            if (type == null)
            {
                type = linked ?
                    LinkedCallFrameTypeName :
                    GetCallFrameType(interpreter, frame, special);
            }

            ConsoleColor foregroundColor = linked ?
                LinkedCallFrameForegroundColor :
                GetCallFrameColor(interpreter, frame, special);

            ConsoleColor backgroundColor = CallFrameInfoBackgroundColor;

            string formatted = Characters.OpenBracket +
                String.Format("{0}{1}", prefix, type) + value +
                Characters.CloseBracket;

            if (!String.IsNullOrEmpty(suffix)) formatted += suffix;

            int separatorLength = Math.Min(
                formatted.Length, ContentWidth - ContentMargin);

            if (DoesSupportReversedColor())
                MaybeSwapTextColors(ref foregroundColor, ref backgroundColor);

            if (!Write(formatted, false, foregroundColor, backgroundColor))
                return false;

            //
            // BUGFIX: To avoid writing more than one line of color, make sure
            //         the trailing line-terminator, if any, is written all by
            //         itself.
            //
            if (newLine && !WriteLine())
                return false;

            if (separator == NoSeparator)
                return true;

            foregroundColor = DefaultForegroundColor;
            backgroundColor = DefaultBackgroundColor;

            if (DoesSupportReversedColor())
                MaybeSwapTextColors(ref foregroundColor, ref backgroundColor);

            if (!Write(
                    separator, separatorLength, false, foregroundColor,
                    backgroundColor))
            {
                return false;
            }

            //
            // BUGFIX: To avoid writing more than one line of color, make sure
            //         the trailing line-terminator, if any, is written all by
            //         itself.
            //
            if (!WriteLine())
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallFrameInfo(
            Interpreter interpreter,
            ICallFrame frame,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteCallFrameInfo(
                interpreter, frame, detailFlags, newLine,
                CallFrameInfoForegroundColor,
                CallFrameInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallFrameInfo(
            Interpreter interpreter,
            ICallFrame frame,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            if (frame != null)
            {
                StringPairList list = null;

                if (BuildCallFrameInfoList(
                        interpreter, frame, detailFlags, ref list))
                {
                    return WriteCore(
                        OutputStyle, CallFrameInfoBoxName, list, newLine,
                        foregroundColor, backgroundColor);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallStack(
            Interpreter interpreter,
            CallStack callStack,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteCallStack(
                interpreter, callStack, CallStackLimit, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallStack(
            Interpreter interpreter,
            CallStack callStack,
            int limit,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            if ((interpreter != null) && (callStack != null))
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if (!interpreter.Disposed && !callStack.Disposed)
                    {
                        ICallFrame variableFrame = null;

                        bool all = FlagOps.HasFlags(
                            detailFlags, DetailFlags.CallStackAllFrames, true);

                        if (all)
                        {
                            /* IGNORED */
                            interpreter.GetVariableFrameViaResolvers(
                                LookupFlags.NoVerbose, ref variableFrame);

                            if (variableFrame != null)
                            {
                                DetailFlags frameDetailFlags = detailFlags |
                                    DetailFlags.CallFrameSpecial;

                                if (!WriteCallFrame(
                                        interpreter, variableFrame, null,
                                        null, VariableCallFrameSuffix,
                                        VariableCallFrameSeparator,
                                        frameDetailFlags, true))
                                {
                                    return false;
                                }

                                frameDetailFlags |= DetailFlags.CallFrameLinked;

                                if ((variableFrame.Other != null) &&
                                    !WriteCallFrame(
                                        interpreter, variableFrame.Other,
                                        OtherCallFrameTypeName, null, null,
                                        NoSeparator, frameDetailFlags, true))
                                {
                                    return false;
                                }

                                if ((variableFrame.Previous != null) &&
                                    !WriteCallFrame(
                                        interpreter, variableFrame.Previous,
                                        PreviousCallFrameTypeName, null, null,
                                        NoSeparator, frameDetailFlags, true))
                                {
                                    return false;
                                }

                                if ((variableFrame.Next != null) &&
                                    !WriteCallFrame(
                                        interpreter, variableFrame.Next,
                                        NextCallFrameTypeName, null, null,
                                        NoSeparator, frameDetailFlags, true))
                                {
                                    return false;
                                }
                            }
                        }

                        CallStack newCallStack = null;
                        Result error = null;
                        int count = 0;

                        try
                        {
                            if (CallFrameOps.Traverse(
                                    interpreter, callStack, variableFrame,
                                    limit, all, ref newCallStack,
                                    ref error) == ReturnCode.Ok)
                            {
                                count = newCallStack.Count;

                                for (int index = 0; index < count; index++)
                                {
                                    ICallFrame frame = newCallStack[index];

                                    if (frame == null)
                                        continue;

                                    DetailFlags frameDetailFlags = detailFlags;

                                    if (all && (variableFrame != null) &&
                                        (index == 0) &&
                                        !IsNoneOutputStyle(OutputStyle) &&
                                        !WriteLine())
                                    {
                                        return false;
                                    }

                                    if (!WriteCallFrame(
                                            interpreter, frame, null, null, null,
                                            NoSeparator, frameDetailFlags, true))
                                    {
                                        return false;
                                    }

                                    frameDetailFlags |= DetailFlags.CallFrameLinked;

                                    if (all && (frame.Other != null) &&
                                        !WriteCallFrame(
                                            interpreter, frame.Other,
                                            OtherCallFrameTypeName, null, null,
                                            NoSeparator, frameDetailFlags, true))
                                    {
                                        return false;
                                    }

                                    if (all && (frame.Previous != null) &&
                                        !WriteCallFrame(
                                            interpreter, frame.Previous,
                                            PreviousCallFrameTypeName, null, null,
                                            NoSeparator, frameDetailFlags, true))
                                    {
                                        return false;
                                    }

                                    if (all && (frame.Next != null) &&
                                        !WriteCallFrame(
                                            interpreter, frame.Next,
                                            NextCallFrameTypeName, null, null,
                                            NoSeparator, frameDetailFlags, true))
                                    {
                                        return false;
                                    }
                                }
                            }
                            else
                            {
                                DebugOps.Complain(
                                    interpreter, ReturnCode.Error, error);

                                return false;
                            }
                        }
                        finally
                        {
                            if (newCallStack != null)
                            {
                                newCallStack.Dispose();
                                newCallStack = null;
                            }
                        }

                        if (newLine && (count > 0) &&
                            !IsNoneOutputStyle(OutputStyle) &&
                            !WriteLine())
                        {
                            return false;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallStackInfo(
            Interpreter interpreter,
            CallStack callStack,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteCallStackInfo(
                interpreter, callStack, CallStackLimit, detailFlags,
                newLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallStackInfo(
            Interpreter interpreter,
            CallStack callStack,
            int limit,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteCallStackInfo(
                interpreter, callStack, limit, detailFlags, newLine,
                CallStackInfoForegroundColor, CallStackInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallStackInfo(
            Interpreter interpreter,
            CallStack callStack,
            int limit,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildCallStackInfoList(
                    interpreter, callStack, limit, detailFlags,
                    ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, CallStackInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER
        public virtual bool WriteDebuggerInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteDebuggerInfo(
                interpreter, detailFlags, newLine,
                DebuggerInfoForegroundColor, DebuggerInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebuggerInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildDebuggerInfoList(
                    interpreter, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, DebuggerInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteFlagInfo(
            Interpreter interpreter,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            HeaderFlags headerFlags,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteFlagInfo(
                interpreter, engineFlags, substitutionFlags, eventFlags,
                expressionFlags, headerFlags, detailFlags, newLine,
                FlagInfoForegroundColor, FlagInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteFlagInfo(
            Interpreter interpreter,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            HeaderFlags headerFlags,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildFlagInfoList(
                    interpreter, engineFlags, substitutionFlags, eventFlags,
                    expressionFlags, headerFlags, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, FlagInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteHostInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteHostInfo(
                interpreter, detailFlags, newLine,
                HostInfoForegroundColor, HostInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteHostInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildHostInfoList(
                    interpreter, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, HostInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteInterpreterInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteInterpreterInfo(
                interpreter, detailFlags, newLine,
                InterpreterInfoForegroundColor, InterpreterInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteInterpreterInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildInterpreterInfoList(
                    interpreter, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, InterpreterInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteEngineInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteEngineInfo(
                interpreter, detailFlags, newLine,
                EngineInfoForegroundColor, EngineInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteEngineInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildEngineInfoList(
                    interpreter, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, EngineInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteEntityInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteEntityInfo(
                interpreter, detailFlags, newLine,
                EntityInfoForegroundColor, EntityInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteEntityInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildEntityInfoList(
                    interpreter, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, EntityInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteStackInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteStackInfo(
                interpreter, detailFlags, newLine,
                EngineInfoForegroundColor, EngineInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteStackInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildStackInfoList(
                    interpreter, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, StackInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteControlInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteControlInfo(
                interpreter, detailFlags, newLine,
                ControlInfoForegroundColor, ControlInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteControlInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildControlInfoList(
                    interpreter, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, ControlInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteTestInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            return WriteTestInfo(
                interpreter, detailFlags, newLine,
                TestInfoForegroundColor, TestInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteTestInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildTestInfoList(
                    interpreter, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, TestInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteTokenInfo(
            Interpreter interpreter,
            IToken token,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteTokenInfo(
                interpreter, token, detailFlags, newLine,
                TokenInfoForegroundColor, TokenInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteTokenInfo(
            Interpreter interpreter,
            IToken token,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildTokenInfoList(
                    interpreter, token, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, TokenInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteTraceInfo(
            Interpreter interpreter,
            ITraceInfo traceInfo,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteTraceInfo(
                interpreter, traceInfo, detailFlags, newLine,
                TraceInfoForegroundColor, TraceInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteTraceInfo(
            Interpreter interpreter,
            ITraceInfo traceInfo,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildTraceInfoList(
                    interpreter, traceInfo, detailFlags, ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, TraceInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteVariableInfo(
            Interpreter interpreter,
            IVariable variable,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteVariableInfo(
                interpreter, variable, detailFlags,
                newLine, VariableInfoForegroundColor,
                VariableInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteVariableInfo(
            Interpreter interpreter,
            IVariable variable,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = new StringPairList();

            if (variable != null)
            {
                StringPairList localList = null;

                if (BuildLinkedVariableInfoList(
                        interpreter, variable, detailFlags,
                        ref localList))
                {
                    list.AddRange(localList);
                }
                else
                {
                    list.Add("Variable");
                    list.Add((IPair<string>)null);
                    list.Add(FormatOps.DisplayUnknown);
                }
            }
            else
            {
                list.Add("Variable");
                list.Add((IPair<string>)null);
                list.Add(FormatOps.DisplayNull);
            }

            return WriteCore(
                OutputStyle, VariableInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteObjectInfo(
            Interpreter interpreter,
            IObject @object,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteObjectInfo(
                interpreter, @object, detailFlags, newLine,
                ObjectInfoForegroundColor, ObjectInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteObjectInfo(
            Interpreter interpreter,
            IObject @object,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = new StringPairList();

            if (@object != null)
            {
                StringPairList localList = null;

                if (BuildObjectInfoList(
                        interpreter, @object, null,
                        detailFlags, ref localList))
                {
                    list.AddRange(localList);
                }
                else
                {
                    list.Add("Object");
                    list.Add((IPair<string>)null);
                    list.Add(FormatOps.DisplayUnknown);
                }
            }
            else
            {
                list.Add("Object");
                list.Add((IPair<string>)null);
                list.Add(FormatOps.DisplayNull);
            }

            return WriteCore(
                OutputStyle, ObjectInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteComplaintInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteComplaintInfo(
                interpreter, detailFlags, newLine,
                ComplaintInfoForegroundColor, ComplaintInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteComplaintInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            int count = 0;

            try
            {
                string complaint = null;
                StringPairList list = null;

                //
                // BUGFIX: We never use the colors specified by the caller
                //         (e.g. "Red" / "Black") unless there is actually
                //         a bona fide complaint; therefore, start out with
                //         the system default colors (e.g. "None" / "None").
                //
                ConsoleColor localForegroundColor = DefaultForegroundColor;
                ConsoleColor localBackgroundColor = DefaultBackgroundColor;

                //
                // NOTE: Grab the complaint from the interpreter and check
                //       to see if it is valid (non-null/empty).  We will
                //       display it even if it is empty if we have been
                //       instructed to do so by the caller.  These tests
                //       must be performed in exactly this order because
                //       the first one has the potential for side-effects.
                //
                if (HasComplaint(interpreter, ref complaint) ||
                    HasEmptyContent(detailFlags))
                {
                    count++; /* NOTE: Yes, the complaint is valid. */

                    //
                    // NOTE: Is there a valid complaint?  If so, use the
                    //       colors specified by the caller; otherwise,
                    //       use the colors already setup (see above).
                    //
                    if (!String.IsNullOrEmpty(complaint))
                    {
                        localForegroundColor = foregroundColor;
                        localBackgroundColor = backgroundColor;
                    }

                    list = new StringPairList("Complaint", null,
                        FormatOps.DisplayString(complaint));
                }

                if (list != null)
                {
                    return WriteCore(
                        OutputStyle, ComplaintInfoBoxName, list, newLine,
                        localForegroundColor, localBackgroundColor) &&
                        (++count > 0); /* NOTE: Yes, the complaint was displayed. */
                }
                else
                {
                    return true;
                }
            }
            finally
            {
                //
                // NOTE: *FAIL-SAFE* Only clear the previously stored complaint
                //       if it was valid AND we actually managed to display it.
                //
                if ((count >= 2) && (interpreter != null))
                {
                    /* IGNORED */
                    DebugOps.SafeSetComplaint(interpreter, null);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if HISTORY
        public virtual bool WriteHistoryInfo(
            Interpreter interpreter,
            IHistoryFilter historyFilter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteHistoryInfo(
                interpreter, historyFilter, detailFlags, newLine,
                HistoryInfoForegroundColor, HistoryInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteHistoryInfo(
            Interpreter interpreter,
            IHistoryFilter historyFilter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildHistoryInfoList(
                    interpreter, historyFilter, detailFlags,
                    ref list))
            {
                return false;
            }

            return WriteCore(
                OutputStyle, HistoryInfoBoxName, list, newLine,
                foregroundColor, backgroundColor);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteCustomInfo(
                interpreter, detailFlags, newLine,
                CustomInfoForegroundColor, CustomInfoBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool WriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteAllResultInfo(
            ReturnCode code,
            Result result,
            int errorLine,
            Result previousResult,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteAllResultInfo(
                code, result, errorLine, previousResult, detailFlags, newLine,
                ResultForegroundColor, ResultBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteAllResultInfo(
            ReturnCode code,
            Result result,
            int errorLine,
            Result previousResult,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            if (IsNoneOutputStyle(OutputStyle))
                return true;

            bool empty = HasEmptyContent(detailFlags);
            int count = 0;

            if (empty || (code != ReturnCode.Ok) || (result != null))
            {
                ConsoleColor localForegroundColor = foregroundColor;
                ConsoleColor localBackgroundColor = backgroundColor;

                if (DoesSupportReversedColor())
                {
                    MaybeSwapTextColors(
                        ref localForegroundColor, ref localBackgroundColor);
                }

                string formatted;

                if (result != null)
                {
                    formatted = FormatResult(
                        null, code, result, errorLine, Exceptions,
                        Display, Ellipsis, ReplaceNewLines, true,
                        ref localForegroundColor, ref localBackgroundColor);
                }
                else
                {
                    GetResultColors(
                        code, result, ref localForegroundColor,
                        ref localBackgroundColor);

                    formatted = FormatOps.DisplayNull;
                }

                StringPairList list = new StringPairList();

                list.Add("Result", formatted);

                list.Add("Flags", (result != null) ?
                    result.Flags.ToString() : FormatOps.DisplayNull);

                if (Write(
                        list.ToString(), localForegroundColor,
                        localBackgroundColor))
                {
                    if (!WriteLine())
                        return false;

                    count++;
                }
                else
                {
                    return false;
                }

                list.Clear();
            }

            if (empty || (previousResult != null))
            {
                ConsoleColor localForegroundColor = foregroundColor;
                ConsoleColor localBackgroundColor = backgroundColor;

                if (DoesSupportReversedColor())
                {
                    MaybeSwapTextColors(
                        ref localForegroundColor, ref localBackgroundColor);
                }

                string formatted;

                if (previousResult != null)
                {
                    formatted = FormatResult(
                        null, previousResult.ReturnCode, previousResult,
                        previousResult.ErrorLine, Exceptions, Display,
                        Ellipsis, ReplaceNewLines, true,
                        ref localForegroundColor, ref localBackgroundColor);
                }
                else
                {
                    formatted = FormatOps.DisplayNull;
                }

                StringPairList list = new StringPairList();

                list.Add("PreviousResult", formatted);

                list.Add("Flags", (previousResult != null) ?
                    previousResult.Flags.ToString() : FormatOps.DisplayNull);

                if (Write(
                        list.ToString(), localForegroundColor,
                        localBackgroundColor))
                {
                    if (!WriteLine())
                        return false;

                    count++;
                }
                else
                {
                    return false;
                }

                list.Clear();
            }

            if (newLine && (count > 0) && !WriteLine())
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteResultInfo(
            string name,
            ReturnCode code,
            Result result,
            int errorLine,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return WriteResultInfo(
                name, code, result, errorLine, detailFlags, newLine,
                ResultForegroundColor, ResultBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteResultInfo(
            string name,
            ReturnCode code,
            Result result,
            int errorLine,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            StringPairList list = null;

            if (!BuildResultInfoList(
                    name, code, result, errorLine, detailFlags,
                    ref list))
            {
                return false;
            }

            ConsoleColor localForegroundColor = foregroundColor;
            ConsoleColor localBackgroundColor = backgroundColor;

            GetResultColors(
                code, result, ref localForegroundColor,
                ref localBackgroundColor);

            return WriteCore(
                OutputStyle, name, list, newLine,
                localForegroundColor, localBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        public virtual void WriteHeader(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            Result result
            )
        {
            CheckDisposed();

            if (!BeginSection(HeaderSectionName, clientData))
                return;

            HeaderFlags headerFlags = (loopData != null) ?
                loopData.HeaderFlags : HeaderFlags.Default;

            DetailFlags detailFlags = DetailFlags.Default;

            HeaderFlagsToDetailFlags(headerFlags, ref detailFlags);

            bool autoSize = FlagOps.HasFlags(
                headerFlags, HeaderFlags.AutoSize, true);

            bool autoRetry = FlagOps.HasFlags(
                headerFlags, HeaderFlags.AutoRetry, true);

            bool emptySection = FlagOps.HasFlags(
                headerFlags, HeaderFlags.EmptySection, true);

            bool emptyContent = FlagOps.HasFlags(
                headerFlags, HeaderFlags.EmptyContent, true);

            HostFlags hostFlags = autoSize ? MaybeInitializeHostFlags() : HostFlags.None;

            bool customInfo = FlagOps.HasFlags(
                hostFlags, HostFlags.CustomInfo, true);

            if ((!autoSize || DoesHeaderFit(HeaderFlags.StopPrompt, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.StopPrompt, true))
            {
                if (!IsNoneOutputStyle(OutputStyle))
                {
                    ConsoleColor foregroundColor = HeaderForegroundColor;
                    ConsoleColor backgroundColor = HeaderBackgroundColor;

                    if (DoesSupportReversedColor())
                        MaybeSwapTextColors(ref foregroundColor, ref backgroundColor);

                    Write(StopPrompt, foregroundColor, backgroundColor);
                    WriteLine();
                }
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.AnnouncementInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.AnnouncementInfo, true))
            {
                WriteAnnouncementInfo(interpreter,
                    BreakpointType.BeforeInteractiveLoop,
#if DEBUGGER
                    FlagOps.HasFlags(headerFlags, HeaderFlags.Debug, true) ?
                        GlobalState.GetDebuggerName() :
                        GlobalState.GetPackageName(),
#else
                    GlobalState.GetPackageName(),
#endif
                    true);
            }

            bool positioning = DoesSupportPositioning();

            if (positioning)
                GetPosition(ref hostLeft, ref hostTop);

            //
            // NOTE: Make sure we are positioned at the far left.
            //
            if (hostLeft > 0)
            {
                hostLeft = 0;
                hostTop++;

                if (positioning)
                    SetPosition(hostLeft, hostTop);
                else
                    WriteLine();
            }

            int minimumLeft = hostLeft;
            int maximumLeft = minimumLeft;
            int minimumTop = hostTop;
            int maximumTop = minimumTop;
            int savedTop = _Position.Invalid;
            int count = 0;

#if DEBUGGER
            if ((!autoSize || DoesHeaderFit(HeaderFlags.DebuggerInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.DebuggerInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteDebuggerInfo(
                        interpreter, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteDebuggerInfo(
                        interpreter, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }
#endif

            if ((!autoSize || DoesHeaderFit(HeaderFlags.EngineInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.EngineInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteEngineInfo(
                        interpreter, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteEngineInfo(
                        interpreter, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.ControlInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.ControlInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteControlInfo(
                        interpreter, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteControlInfo(
                        interpreter, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.EntityInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.EntityInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteEntityInfo(
                        interpreter, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteEntityInfo(
                        interpreter, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.StackInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.StackInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteStackInfo(
                        interpreter, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteStackInfo(
                        interpreter, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.FlagInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.FlagInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                EngineFlags engineFlags = EngineFlags.None;
                SubstitutionFlags substitutionFlags = SubstitutionFlags.None;
                EventFlags eventFlags = EventFlags.None;
                ExpressionFlags expressionFlags = ExpressionFlags.None;

                if (loopData != null)
                {
                    engineFlags = loopData.EngineFlags;
                    substitutionFlags = loopData.SubstitutionFlags;
                    eventFlags = loopData.EventFlags;
                    expressionFlags = loopData.ExpressionFlags;
                }

                if (!WriteFlagInfo(
                        interpreter, engineFlags, substitutionFlags,
                        eventFlags, expressionFlags, headerFlags,
                        detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteFlagInfo(
                        interpreter, engineFlags, substitutionFlags,
                        eventFlags, expressionFlags, headerFlags,
                        detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.HostInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.HostInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteHostInfo(
                        interpreter, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteHostInfo(
                        interpreter, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            ReturnCode code = ReturnCode.Ok;
            BreakpointType breakpointType = BreakpointType.None;
            ArgumentList arguments = null;

            if (loopData != null)
            {
                code = loopData.Code;
                breakpointType = loopData.BreakpointType;
                arguments = loopData.Arguments;
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.ArgumentInfo, hostFlags)) &&
                (emptySection || (breakpointType != BreakpointType.None) ||
                    (arguments != null)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.ArgumentInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                string breakpointName = null;

                if (loopData != null)
                    breakpointName = loopData.BreakpointName;

                if (!WriteArgumentInfo(
                        interpreter, code, breakpointType, breakpointName,
                        arguments, result, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteArgumentInfo(
                        interpreter, code, breakpointType, breakpointName,
                        arguments, result, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.TestInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.TestInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteTestInfo(
                        interpreter, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteTestInfo(
                        interpreter, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            IToken token = null;

            if (loopData != null)
                token = loopData.Token;

            if ((!autoSize || DoesHeaderFit(HeaderFlags.TokenInfo, hostFlags)) &&
                (emptySection || (token != null)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.TokenInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteTokenInfo(
                        interpreter, token, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteTokenInfo(
                        interpreter, token, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            ITraceInfo traceInfo = null;

            if (loopData != null)
                traceInfo = loopData.TraceInfo;

            if ((!autoSize || DoesHeaderFit(HeaderFlags.TraceInfo, hostFlags)) &&
                (emptySection || (traceInfo != null)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.TraceInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteTraceInfo(
                        interpreter, traceInfo, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteTraceInfo(
                        interpreter, traceInfo, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            IVariable variable = (traceInfo != null) ? traceInfo.Variable : null;

            if ((!autoSize || DoesHeaderFit(HeaderFlags.VariableInfo, hostFlags)) &&
                (emptySection || (variable != null)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.VariableInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteVariableInfo(
                        interpreter, variable, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteVariableInfo(
                        interpreter, variable, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            IObject @object = GetObjectFromValue(interpreter, variable);

            if ((!autoSize || DoesHeaderFit(HeaderFlags.ObjectInfo, hostFlags)) &&
                (emptySection || (@object != null)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.ObjectInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteObjectInfo(
                        interpreter, @object, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteObjectInfo(
                        interpreter, @object, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.InterpreterInfo, hostFlags)) &&
                (emptySection || (interpreter != null)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.InterpreterInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteInterpreterInfo(
                        interpreter, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteInterpreterInfo(
                        interpreter, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.ComplaintInfo, hostFlags)) &&
                (emptySection || HasComplaint(interpreter)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.ComplaintInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                DetailFlags complaintDetailFlags = detailFlags;

                if (emptySection || emptyContent)
                    complaintDetailFlags |= DetailFlags.EmptyContent;

                if (!WriteComplaintInfo(
                        interpreter, complaintDetailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteComplaintInfo(
                        interpreter, complaintDetailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.CustomInfo, hostFlags)) &&
                customInfo && /* NOTE: Does the host support CustomInfo? */
                FlagOps.HasFlags(headerFlags, HeaderFlags.CustomInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteCustomInfo(
                        interpreter, detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteCustomInfo(
                        interpreter, detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.CallStackInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.CallStackInfo, true) &&
                AppDomainOps.IsSame(interpreter)) // NOTE: Non-serializable property.
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteCallStackInfo(
                        interpreter, (interpreter != null) ?
                            interpreter.CallStack : null,
                        detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteCallStackInfo(
                        interpreter, (interpreter != null) ?
                            interpreter.CallStack : null,
                        detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.ResultInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.ResultInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteResultInfo(
                        null, code, result,
                        Interpreter.GetErrorLine(interpreter),
                        detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteResultInfo(
                        null, code, result,
                        Interpreter.GetErrorLine(interpreter),
                        detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }

#if PREVIOUS_RESULT
            Result previousResult = Interpreter.GetPreviousResult(interpreter);

            if ((!autoSize || DoesHeaderFit(HeaderFlags.PreviousResultInfo, hostFlags)) &&
                (emptySection || (previousResult != null)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.PreviousResultInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                if (!WriteResultInfo(
                        PreviousResultInfoBoxName, (previousResult != null) ?
                        previousResult.ReturnCode : ReturnCode.Ok, previousResult,
                        (previousResult != null) ? previousResult.ErrorLine : 0,
                        detailFlags, true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteResultInfo(
                        PreviousResultInfoBoxName, (previousResult != null) ?
                        previousResult.ReturnCode : ReturnCode.Ok, previousResult,
                        (previousResult != null) ? previousResult.ErrorLine : 0,
                        detailFlags, true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }
#endif

#if HISTORY
            if ((!autoSize || DoesHeaderFit(HeaderFlags.HistoryInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.HistoryInfo, true))
            {
                BeginSection((count + 1) == SectionsPerRow, minimumLeft,
                    ref maximumTop, ref savedTop, ref count);

                IHistoryFilter historyFilter = null;

                if (interpreter != null)
                {
                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                    {
                        if (!interpreter.Disposed)
                            historyFilter = interpreter.HistoryInfoFilter;
                    }
                }

                if (historyFilter == null)
                    historyFilter = HistoryOps.DefaultInfoFilter;

                if (!WriteHistoryInfo(
                        interpreter, historyFilter, detailFlags,
                        true) &&
                    autoRetry)
                {
                    BeginSection(true, minimumLeft,
                        ref maximumTop, ref savedTop, ref count);

                    WriteHistoryInfo(
                        interpreter, historyFilter, detailFlags,
                        true);
                }

                EndSection(ref maximumLeft, ref maximumTop);
            }
#endif

            if (hostLeft != minimumLeft)
                maximumTop++;

            if (positioning)
                SetPosition(minimumLeft, maximumTop);
            else
                WriteLine();

            if ((!autoSize || DoesHeaderFit(HeaderFlags.CallStack, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.CallStack, true) &&
                AppDomainOps.IsSame(interpreter)) // NOTE: Non-serializable property.
            {
                WriteCallStack(
                    interpreter, (interpreter != null) ?
                        interpreter.CallStack : null,
                    detailFlags, true);
            }

            //
            // NOTE: Notify the host implementation that we are finished writing the header.
            //
            /* IGNORED */
            EndSection(HeaderSectionName, clientData);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual void WriteFooter(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            Result result
            )
        {
            CheckDisposed();

            if (!BeginSection(FooterSectionName, clientData))
                return;

            HeaderFlags headerFlags = (loopData != null) ?
                loopData.HeaderFlags : HeaderFlags.Default;

            bool autoSize = FlagOps.HasFlags(
                headerFlags, HeaderFlags.AutoSize, true);

            HostFlags hostFlags = autoSize ? MaybeInitializeHostFlags() : HostFlags.None;

            if ((!autoSize || DoesHeaderFit(HeaderFlags.AnnouncementInfo, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.AnnouncementInfo, true))
            {
                WriteAnnouncementInfo(interpreter,
                    BreakpointType.AfterInteractiveLoop,
#if DEBUGGER
                    FlagOps.HasFlags(headerFlags, HeaderFlags.Debug, true) ?
                        GlobalState.GetDebuggerName() :
                        GlobalState.GetPackageName(),
#else
                    GlobalState.GetPackageName(),
#endif
                    true);
            }

            if ((!autoSize || DoesHeaderFit(HeaderFlags.GoPrompt, hostFlags)) &&
                FlagOps.HasFlags(headerFlags, HeaderFlags.GoPrompt, true))
            {
                if (!IsNoneOutputStyle(OutputStyle))
                {
                    ConsoleColor foregroundColor = FooterForegroundColor;
                    ConsoleColor backgroundColor = FooterBackgroundColor;

                    if (DoesSupportReversedColor())
                        MaybeSwapTextColors(ref foregroundColor, ref backgroundColor);

                    Write(GoPrompt, foregroundColor, backgroundColor);
                    WriteLine();
                }
            }

            //
            // NOTE: Notify the host implementation that we are finished writing the footer.
            //
            /* IGNORED */
            EndSection(FooterSectionName, clientData);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IBoxHost Members
        public abstract bool BeginBox(
            string name,
            StringPairList list,
            IClientData clientData
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool EndBox(
            string name,
            StringPairList list,
            IClientData clientData
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top   /* in, out */
            )
        {
            CheckDisposed();

            return WriteBox(
                name, value, clientData, newLine, restore, ref left, ref top,
                DefaultForegroundColor, DefaultBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top   /* in, out */
            )
        {
            CheckDisposed();

            return WriteBox(
                name, value, clientData, minimumLength, newLine, restore,
                ref left, ref top, DefaultForegroundColor, DefaultBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top,  /* in, out */
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return WriteBox(
                name, value, clientData, newLine, restore, ref left, ref top,
                foregroundColor, backgroundColor, BoxForegroundColor,
                BoxBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top,  /* in, out */
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return WriteBox(
                name, value, clientData, minimumLength, newLine, restore,
                ref left, ref top, foregroundColor, backgroundColor,
                BoxForegroundColor, BoxBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top,  /* in, out */
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            return WriteBox(
                name, new StringPairList(value), clientData, newLine,
                restore, ref left, ref top, foregroundColor, backgroundColor,
                boxForegroundColor, boxBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top,  /* in, out */
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            return WriteBox(
                name, new StringPairList(value), clientData, minimumLength,
                newLine, restore, ref left, ref top, foregroundColor,
                backgroundColor, boxForegroundColor, boxBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top   /* in, out */
            )
        {
            CheckDisposed();

            return WriteBox(
                name, list, clientData, newLine, restore, ref left, ref top,
                DefaultForegroundColor, DefaultBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top   /* in, out */
            )
        {
            CheckDisposed();

            return WriteBox(
                name, list, clientData, minimumLength, newLine, restore,
                ref left, ref top, DefaultForegroundColor, DefaultBackgroundColor,
                BoxForegroundColor, BoxBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top,  /* in, out */
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return WriteBox(
                name, list, clientData, newLine, restore, ref left, ref top,
                foregroundColor, backgroundColor, BoxForegroundColor,
                BoxBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top,  /* in, out */
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return WriteBox(
                name, list, clientData, minimumLength, newLine, restore,
                ref left, ref top, foregroundColor, backgroundColor,
                BoxForegroundColor, BoxBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top,  /* in, out */
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            return WriteBox(
                name, list, clientData, MinimumLength, newLine, restore,
                ref left, ref top, foregroundColor, backgroundColor,
                boxForegroundColor, boxBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left, /* in, out */
            ref int top,  /* in, out */
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            int levels = Interlocked.Increment(ref boxLevels);

            try
            {
                if (boxLevels == 1)
                {
                    if (list != null)
                    {
                        //
                        // NOTE: What is the console output encoding in use right now?
                        //
                        Encoding encoding = OutputEncoding; /* PROPERTY */

                        //
                        // NOTE: Have we run out of space while trying to write the box
                        //       and/or the content?
                        //
                        bool outOfSpace = false;

                        //
                        // NOTE: Is the host derived from the built-in console host?
                        //
                        bool isConsole = IsConsoleHost(this);

                        //
                        // NOTE: This is the place where we will jump to and retry the
                        //       write operation if we run "out-of-space".
                        //

                    retry:

                        //
                        // NOTE: Does the host support sizing and positioning for the
                        //       content area?
                        //
                        bool sizing = (!isConsole || !outOfSpace) ?
                            DoesSupportSizing() : false;

                        bool positioning = (!isConsole || !outOfSpace) ?
                            DoesSupportPositioning() : false;

                        //
                        // NOTE: Notify the host implementation that we are about to
                        //       write a box.  This lets the host implementation know
                        //       that all the following writes (i.e. until the call to
                        //       EndBox) logically belong to the same composite item.
                        //
                        if (!BeginBox(name, list, clientData))
                            return false;

                        try
                        {
                            //
                            // NOTE: If necessary, save the current host position for later
                            //       restoration.
                            //
                            int savedLeft = _Position.Invalid;
                            int savedTop = _Position.Invalid;

                            if (positioning && restore &&
                                !GetPosition(ref savedLeft, ref savedTop))
                            {
                                return false;
                            }

                            try
                            {
                                //
                                // NOTE: What is the margin between the name and value on a
                                //       particular line in the box (if applicable to that
                                //       line)?
                                //
                                int margin = BoxMargin;

                                if (margin < 0)
                                    margin = ContentMargin;

                                //
                                // NOTE: What is the overall content length limit?
                                //
                                int limit = ContentWidth - margin;

                                //
                                // NOTE: The length limit must take into account the horizontal
                                //       offset into the display area.
                                //
                                // TODO: Should this always be done unconditionally (i.e. maybe
                                //       make it optional in the future, via a bool argument)?
                                //
                                limit -= left;

                                //
                                // NOTE: Make sure we have enough horizontal space to output
                                //       something meaningful.
                                //
                                if ((limit > 0) && (limit >= ContentThreshold))
                                {
                                    //
                                    //  NOTE: If necessary, get the total width and height of
                                    //        the content area.
                                    //
                                    int width = 0;
                                    int height = 0;

                                    //
                                    // NOTE: *COMPAT* Use the buffer size (i.e. not the window size)
                                    //       here when figuring out if there is enough space to output
                                    //       the requested box.
                                    //
                                    if (!sizing || GetSize(HostSizeType.BufferCurrent, ref width, ref height))
                                    {
                                        //
                                        // NOTE: Calculate how much vertical space we need.
                                        //
                                        int maximumTop = top + 2 + (newLine ? (list.Count * 2) : list.Count);

                                        //
                                        // NOTE: Make sure we have enough vertical space to output
                                        //       the entire box.
                                        //
                                        if (!sizing || (maximumTop < height))
                                        {
                                            //
                                            // NOTE: Fetch the currently configured box limit (i.e. the maximum
                                            //       width allowed for one box).  If no valid box limit is set
                                            //       or it exceeds the overall limit, use the overall limit
                                            //       instead.
                                            //
                                            int boxLimit = BoxWidth;

                                            if ((boxLimit < 0) || (boxLimit > limit))
                                                boxLimit = limit;

                                            //
                                            // NOTE: Remove the content margin from the box limit.  We do this
                                            //       because we need to calculate the maximum string length
                                            //       (below) without taking into the account the margin.  The
                                            //       value written will be truncated to this limit and ellipsed
                                            //       as necessary.
                                            //
                                            boxLimit -= margin;

                                            //
                                            // NOTE: Make sure we can still output something.
                                            //
                                            if (boxLimit >= 0)
                                            {
                                                //
                                                // NOTE: Figure out the maximum length for any item in the supplied
                                                //       list that will fit within the physical bounds of the content
                                                //       area.
                                                //
                                                int length = ListOps.GetMaximumLength(list, NameValueFormat, boxLimit);

                                                //
                                                // NOTE: Enforce the minimum line length requested by the caller, if
                                                //       any.  Do this before adding any extra margin.
                                                //
                                                if ((minimumLength > 0) &&
                                                    (minimumLength <= boxLimit) && (length < minimumLength))
                                                {
                                                    length = minimumLength;
                                                }

                                                //
                                                // NOTE: Add a margin for ease of reading.  These spaces, if any,
                                                //       will be used for padding in between the name and the value
                                                //       of each name/value pair.
                                                //
                                                length += margin;

                                                //
                                                // NOTE: Use the caller's position variables as the starting point
                                                //       for the box.
                                                //
                                                int newLeft = left;
                                                int newTop = top;

                                                //
                                                // NOTE: Grab the "character set" (i.e. string) that we are going
                                                //       to use to draw the pieces of the box.
                                                //
                                                string characterSet = GetBoxCharacterSet();

                                                if (characterSet == null)
                                                    characterSet = GetFallbackBoxCharacterSet();

                                                //
                                                // NOTE: Make sure the configured character set includes all the
                                                //       characters we need.
                                                //
                                                if ((characterSet == null) ||
                                                    (characterSet.Length < (int)BoxCharacter.Count))
                                                {
                                                    return false;
                                                }

                                                //
                                                // NOTE: Where type of host write is this?
                                                //
                                                HostWriteType hostWriteType = OutputStyleToHostWriteType(
                                                    OutputStyle);

                                                //
                                                // NOTE: Set the current position to the initial position of the
                                                //       top line of the box (i.e. the initial position of the box).
                                                //
                                                if (positioning && !SetPosition(newLeft, newTop))
                                                    return false;
                                                else if (!positioning && !WriteLineForBox(hostWriteType))
                                                    return false;

                                                //
                                                // NOTE: Does the host support reversed colors?
                                                //
                                                bool reversed = DoesSupportReversedColor();

                                                //
                                                // NOTE: Determine if the foreground and background colors need to
                                                //       be swapped prior to writing anything, for both the text
                                                //       and the borders.
                                                //
                                                if (reversed)
                                                {
                                                    MaybeSwapTextColors(
                                                        ref foregroundColor, ref backgroundColor);

                                                    MaybeSwapBorderColors(
                                                        ref boxForegroundColor, ref boxBackgroundColor);
                                                }

                                                //
                                                // NOTE: Draw the upper left corner of the box.
                                                //
                                                if (!WriteForBox(hostWriteType,
                                                        characterSet[(int)BoxCharacter.TopLeft],
                                                        boxForegroundColor, boxBackgroundColor))
                                                {
                                                    return false;
                                                }

                                                //
                                                // NOTE: Draw the upper middle section of the box.
                                                //
                                                if (!WriteForBox(hostWriteType,
                                                        characterSet[(int)BoxCharacter.Horizontal],
                                                        length, false, boxForegroundColor,
                                                        boxBackgroundColor))
                                                {
                                                    return false;
                                                }

                                                //
                                                // NOTE: Draw the upper right corner of the box.
                                                //
                                                if (!WriteForBox(hostWriteType,
                                                        characterSet[(int)BoxCharacter.TopRight],
                                                        boxForegroundColor, boxBackgroundColor))
                                                {
                                                    return false;
                                                }

                                                //
                                                // NOTE: We just wrote a line, advance the line counter.
                                                //
                                                newTop++;

                                                //
                                                // NOTE: Grab the whitespace normalization flags to be
                                                //       used within the content output loop.
                                                //
                                                WhiteSpaceFlags whiteSpaceFlags = GetBoxWhiteSpaceFlags(
                                                    encoding);

                                                foreach (IPair<string> element in list)
                                                {
                                                    IToString toString = element as IToString;

                                                    if ((element != null) && (toString != null))
                                                    {
                                                        string value;

                                                        //
                                                        // NOTE: Check to see if this is really a name/value pair or
                                                        //       just a single value.
                                                        //
                                                        if ((element.X != null) && (element.Y != null))
                                                        {
                                                            //
                                                            // NOTE: Start with the default format.
                                                            //
                                                            string format = NameValueFormat;

                                                            //
                                                            // NOTE: Grab and format the content of the box for this
                                                            //       line.
                                                            //
                                                            value = StringOps.NormalizeWhiteSpace(
                                                                toString.ToString(format, boxLimit, true),
                                                                Characters.Space, whiteSpaceFlags);

                                                            //
                                                            // NOTE: Recalculate the layout to justify the name to
                                                            //       the left and the value to the right.
                                                            //
                                                            format = format.Replace(
                                                                Characters.Space.ToString(),
                                                                StringOps.StrRepeat(
                                                                    (length - value.Length) + 1,
                                                                    characterSet[(int)BoxCharacter.Space]));

                                                            //
                                                            // NOTE: Reformat the content of the box for this line
                                                            //       with the name and value left and right justified.
                                                            //
                                                            value = StringOps.NormalizeWhiteSpace(
                                                                toString.ToString(format, boxLimit + margin, true),
                                                                Characters.Space, whiteSpaceFlags);
                                                        }
                                                        else if (element.X != null)
                                                        {
                                                            value = StringOps.NormalizeWhiteSpace(
                                                                element.X, Characters.Space, whiteSpaceFlags);

                                                            //
                                                            // NOTE: Truncate the content to fit within the actual
                                                            //       physical limits of the content area.
                                                            //
                                                            if (value.Length > boxLimit)
                                                                value = FormatOps.Ellipsis(value, boxLimit, true);

                                                            //
                                                            // NOTE: Center pad the content of the box for this line
                                                            //       with spaces to the maximum length of any of the
                                                            //       content.
                                                            //
                                                            value = StringOps.PadCenter(
                                                                value, length, characterSet[(int)BoxCharacter.Space]);
                                                        }
                                                        else if (element.Y != null)
                                                        {
                                                            value = StringOps.NormalizeWhiteSpace(
                                                                element.Y, Characters.Space, whiteSpaceFlags);

                                                            //
                                                            // NOTE: Truncate the content to fit within the actual
                                                            //       physical limits of the content area.
                                                            //
                                                            if (value.Length > boxLimit)
                                                                value = FormatOps.Ellipsis(value, boxLimit, true);

                                                            //
                                                            // NOTE: Center pad the content of the box for this line
                                                            //       with spaces to the maximum length of any of the
                                                            //       content.
                                                            //
                                                            value = StringOps.PadCenter(
                                                                value, length, characterSet[(int)BoxCharacter.Space]);
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // NOTE: There is no name or value to display.
                                                            //
                                                            value = null;
                                                        }

                                                        //
                                                        // NOTE: Do we need to output anything for this content line
                                                        //       of the box?
                                                        //
                                                        if (value != null)
                                                        {
                                                            //
                                                            // NOTE: Set the current position to the initial position
                                                            //       of the current line of the box.
                                                            //
                                                            if (positioning && !SetPosition(newLeft, newTop))
                                                                return false;
                                                            else if (!positioning && !WriteLineForBox(hostWriteType))
                                                                return false;

                                                            //
                                                            // NOTE: Draw the left side of the box for this line.
                                                            //
                                                            if (!WriteForBox(hostWriteType,
                                                                    characterSet[(int)BoxCharacter.Vertical],
                                                                    boxForegroundColor, boxBackgroundColor))
                                                            {
                                                                return false;
                                                            }

                                                            //
                                                            // NOTE: Draw the content of the box for this line.
                                                            //
                                                            if (!WriteForBox(hostWriteType,
                                                                    value, foregroundColor, backgroundColor))
                                                            {
                                                                return false;
                                                            }

                                                            //
                                                            // NOTE: Draw the right side of the box for this line.
                                                            //
                                                            if (!WriteForBox(hostWriteType,
                                                                    characterSet[(int)BoxCharacter.Vertical],
                                                                    boxForegroundColor, boxBackgroundColor))
                                                            {
                                                                return false;
                                                            }

                                                            //
                                                            // NOTE: We just wrote another line, advance the line
                                                            //       counter.
                                                            //
                                                            newTop++;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: Set the current position to the initial position
                                                        //       of the current line of the box.
                                                        //
                                                        if (positioning && !SetPosition(newLeft, newTop))
                                                            return false;
                                                        else if (!positioning && !WriteLineForBox(hostWriteType))
                                                            return false;

                                                        //
                                                        // NOTE: Draw the left side junction of the box for this
                                                        //       line.
                                                        //
                                                        if (!WriteForBox(hostWriteType,
                                                                characterSet[(int)BoxCharacter.LeftJunction],
                                                                boxForegroundColor, boxBackgroundColor))
                                                        {
                                                            return false;
                                                        }

                                                        //
                                                        // NOTE: Draw a horizontal line.
                                                        //
                                                        if (!WriteForBox(hostWriteType,
                                                                characterSet[(int)BoxCharacter.Horizontal],
                                                                length, false, boxForegroundColor,
                                                                boxBackgroundColor))
                                                        {
                                                            return false;
                                                        }

                                                        //
                                                        // NOTE: Draw the right side junction of the box for this
                                                        //       line.
                                                        //
                                                        if (!WriteForBox(hostWriteType,
                                                                characterSet[(int)BoxCharacter.RightJunction],
                                                                boxForegroundColor, boxBackgroundColor))
                                                        {
                                                            return false;
                                                        }

                                                        //
                                                        // NOTE: We just wrote another line, advance the line
                                                        //       counter.
                                                        //
                                                        newTop++;
                                                    }

                                                    if (newLine)
                                                    {
                                                        //
                                                        // NOTE: Set the current position to the initial position
                                                        //       of the next line of the box (the line counter was
                                                        //       just advanced, above).
                                                        //
                                                        if (positioning && !SetPosition(newLeft, newTop))
                                                            return false;
                                                        else if (!positioning && !WriteLineForBox(hostWriteType))
                                                            return false;

                                                        //
                                                        // NOTE: Draw the left side of the box for this blank line.
                                                        //
                                                        if (!WriteForBox(hostWriteType,
                                                                characterSet[(int)BoxCharacter.Vertical],
                                                                boxForegroundColor, boxBackgroundColor))
                                                        {
                                                            return false;
                                                        }

                                                        //
                                                        // NOTE: Draw a blank line.
                                                        //
                                                        if (!WriteForBox(hostWriteType,
                                                                characterSet[(int)BoxCharacter.Space],
                                                                length, false, foregroundColor,
                                                                backgroundColor))
                                                        {
                                                            return false;
                                                        }

                                                        //
                                                        // NOTE: Draw the right side of the box for this blank line.
                                                        //
                                                        if (!WriteForBox(hostWriteType,
                                                                characterSet[(int)BoxCharacter.Vertical],
                                                                boxForegroundColor, boxBackgroundColor))
                                                        {
                                                            return false;
                                                        }

                                                        //
                                                        // NOTE: We just wrote another line, advance the line
                                                        //       counter.
                                                        //
                                                        newTop++;
                                                    }
                                                }

                                                //
                                                // NOTE: Set the current position to the initial position
                                                //       of the bottom line of the box.
                                                //
                                                if (positioning && !SetPosition(newLeft, newTop))
                                                    return false;
                                                else if (!positioning && !WriteLineForBox(hostWriteType))
                                                    return false;

                                                //
                                                // NOTE: Draw the lower left corner of the box.
                                                //
                                                if (!WriteForBox(hostWriteType,
                                                        characterSet[(int)BoxCharacter.BottomLeft],
                                                        boxForegroundColor, boxBackgroundColor))
                                                {
                                                    return false;
                                                }

                                                //
                                                // NOTE: Draw the lower middle section of the box.
                                                //
                                                if (!WriteForBox(hostWriteType,
                                                        characterSet[(int)BoxCharacter.Horizontal],
                                                        length, false, boxForegroundColor,
                                                        boxBackgroundColor))
                                                {
                                                    return false;
                                                }

                                                //
                                                // NOTE: Draw the lower right corner of the box.
                                                //
                                                if (!WriteForBox(hostWriteType,
                                                        characterSet[(int)BoxCharacter.BottomRight],
                                                        boxForegroundColor, boxBackgroundColor))
                                                {
                                                    return false;
                                                }

                                                //
                                                // NOTE: We just wrote another line, advance the line
                                                //       counter.
                                                //
                                                newTop++;

                                                //
                                                // NOTE: Update the horizontal position as well (so that
                                                //       the caller knows where we left off).
                                                //
                                                newLeft += (length + 2);

                                                //
                                                // NOTE: Update the caller's variables with the new
                                                //       positions.
                                                //
                                                left = newLeft;
                                                top = newTop;

                                                //
                                                // NOTE: If we previously ran out of space and the host
                                                //       does not support positioning, attempt to advance
                                                //       to the next line now.
                                                //
                                                if (outOfSpace && !positioning &&
                                                    !WriteLineForBox(hostWriteType))
                                                {
                                                    return false;
                                                }

                                                //
                                                // NOTE: If we get to this point then we have totally
                                                //       succeeded.
                                                //
                                                return true;
                                            }
                                        }
                                    }
                                }

                                //
                                // NOTE: The box and/or content could not be written (at all)
                                //       because there is not enough "space".  Therefore, set
                                //       the "out-of-space" flag and then retry the operation.
                                //       This should cause most of the positioning and sizing
                                //       checks to be skipped, thereby allowing the box and
                                //       content to be written.
                                //
                                if (!outOfSpace)
                                {
                                    outOfSpace = true;
                                    goto retry;
                                }

                                //
                                // NOTE: We cannot output anything because there is no space
                                //       (either horizontal, vertical, or both) to do so after
                                //       taking into account the amount of content the caller
                                //       requests us to write and the physical bounds of the
                                //       content area.
                                //
                                TraceOps.DebugTrace(
                                    "WriteBox: cannot write after retry, insufficient space",
                                    typeof(Default).Name, TracePriority.HostError);

                                /* FALL-THROUGH */
                            }
                            finally
                            {
                                //
                                // NOTE: Restore the previously saved position?
                                //
                                if (positioning && restore)
                                    /* IGNORED */
                                    SetPosition(savedLeft, savedTop);
                            }
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(Default).Name,
                                TracePriority.HostError);
                        }
                        finally
                        {
                            //
                            // NOTE: Notify the host implementation that we are
                            //       finished writing the box.
                            //
                            /* IGNORED */
                            EndBox(name, list, clientData);
                        }
                    }

                    return false;
                }
                else
                {
                    //
                    // HACK: Since a box is pending, we cannot continue.
                    //
                    TraceOps.DebugTrace(
                        "WriteBox: cannot write, one is already pending",
                        typeof(Default).Name, TracePriority.HostError);

                    return false;
                }
            }
            finally
            {
                Interlocked.Decrement(ref boxLevels);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        public virtual bool NoColor
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.NoColor, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.NoColor, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool ResetColors(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool GetColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool AdjustColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool SetForegroundColor(
            ConsoleColor foregroundColor
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool SetBackgroundColor(
            ConsoleColor backgroundColor
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool SetColors(
            bool foreground,
            bool background,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

#if CONSOLE
            //
            // HACK: This is only supported for hosts that derive
            //       from the built-in console host.
            //
            _Hosts.Console consoleHost = this as _Hosts.Console;

            if ((consoleHost != null) &&
                consoleHost.DoesMaybeResetColorForSet() &&
                consoleHost.ShouldResetColorsForSetColors(
                    foreground, background, foregroundColor,
                    backgroundColor))
            {
                return ResetColors();
            }
#endif

            if (foreground && !SetForegroundColor(foregroundColor))
                return false;

            if (background && !SetBackgroundColor(backgroundColor))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode GetColors(
            string theme, /* RESERVED */
            string name,
            bool foreground,
            bool background,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode SetColors(
            string theme, /* RESERVED */
            string name,
            bool foreground,
            bool background,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ref Result error
            ); /* PRIMITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPositionHost Members
        public virtual bool ResetPosition()
        {
            CheckDisposed();

            hostLeft = 0;
            hostTop = 0;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool GetPosition(
            ref int left,
            ref int top
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool SetPosition(
            int left,
            int top
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool GetDefaultPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            left = hostLeft;
            top = hostTop;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool SetDefaultPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            hostLeft = left;
            hostTop = top;

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISizeHost Members
        public abstract bool ResetSize(
            HostSizeType hostSizeType
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool GetSize(
            HostSizeType hostSizeType,
            ref int width,
            ref int height
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool SetSize(
            HostSizeType hostSizeType,
            int width,
            int height
            ); /* PRIMITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IReadHost Members
        public abstract bool Read(
            ref int value
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool ReadKey(
            bool intercept,
            ref IClientData value
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        [Obsolete()]
        public abstract bool ReadKey(
            bool intercept,
            ref ConsoleKeyInfo value
            ); /* PRIMITIVE */
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IWriteHost Members
        public abstract bool Write(
            char value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value,
            int count
            )
        {
            CheckDisposed();

            return Write(value, count, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value,
            int count,
            bool newLine
            )
        {
            CheckDisposed();

            return Write(value, count, newLine, DefaultForegroundColor, DefaultBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return WriteCore(HostWriteType.Normal, value, count, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return Write(value, 1, false, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return Write(value, false, foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return Write(value, false, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool Write(
            string value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return Write(value, newLine, foregroundColor, DefaultBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return WriteCore(HostWriteType.Normal, value, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteFormat(
            StringPairList list,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            if (DoesSupportReversedColor())
                MaybeSwapTextColors(ref foregroundColor, ref backgroundColor);

            return WriteCore(
                HostWriteType.Normal, (list != null) ? list.ToString() : null,
                newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteLine(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return Write(value, true, foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool WriteLine(
            string value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return Write(value, true, foregroundColor, backgroundColor);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHost Members
        private string profile = null;
        public virtual string Profile
        {
            get { CheckDisposed(); return profile; }
            set { CheckDisposed(); profile = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string defaultTitle;
        public virtual string DefaultTitle
        {
            get { CheckDisposed(); return defaultTitle; }
            set { CheckDisposed(); defaultTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private HostCreateFlags hostCreateFlags;
        public virtual HostCreateFlags HostCreateFlags
        {
            get { CheckDisposed(); return hostCreateFlags; }
            set { CheckDisposed(); hostCreateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool UseAttach
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.UseAttach, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.UseAttach, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool NoTitle
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.NoTitle, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.NoTitle, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool NoIcon
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.NoIcon, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.NoIcon, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool NoProfile
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.NoProfile, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.NoProfile, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool NoCancel
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.NoCancel, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.NoCancel, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Echo
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.Echo, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.Echo, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract StringList QueryState(
            DetailFlags detailFlags
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool Beep(
            int frequency,
            int duration
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool IsIdle(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool Clear(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode GetMode(
            ChannelType channelType,
            ref uint mode,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode SetMode(
            ChannelType channelType,
            uint mode,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode Open(
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode Close(
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract ReturnCode Discard(
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            if (!PrivateResetHostFlags()) /* NON-VIRTUAL */
            {
                error = "failed to reset flags";
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool BeginSection(
            string name,
            IClientData clientData
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public abstract bool EndSection(
            string name,
            IClientData clientData
            ); /* PRIMITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Default));
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                //if (disposing)
                //{
                //    ////////////////////////////////////
                //    // dispose managed resources here...
                //    ////////////////////////////////////
                //}

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Default()
        {
            Dispose(false);
        }
        #endregion
    }
}
