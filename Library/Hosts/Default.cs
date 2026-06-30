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

using ObjectWrapper = Eagle._Wrappers._Object;

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
    /// <summary>
    /// This class is the abstract base implementation of the
    /// <see cref="IHost" /> interface, serving as the default Eagle host.  It
    /// provides console and display output, interactive input, color handling,
    /// introspection rendering, and the shared host infrastructure from which
    /// concrete hosts (e.g. the console and diagnostic hosts) derive.
    /// </summary>
    [ObjectId("4a02c60b-5b7b-4eb8-873e-eb6860ba3973")]
    public abstract class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IHost, IDisposable, IMaybeDisposed, ISynchronizeStatic
    {
        #region Protected Constants
        /// <summary>
        /// The binding flags used when reflecting over the host properties of
        /// this class and its derived classes.
        /// </summary>
        protected internal static readonly BindingFlags HostPropertyBindingFlags =
            ObjectOps.GetBindingFlags(MetaBindingFlags.HostProperty, true);

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
        /// <summary>
        /// The read-only array of pass identifiers used by the <c>WriteCore</c>
        /// subsystem to perform a single pass that calls <c>Write</c> and
        /// <c>WriteLine</c> with colors enabled.
        /// </summary>
        protected internal static readonly int[] OnePassForWriteCore = { 0 };
        /// <summary>
        /// The read-only array of pass identifiers used by the <c>WriteCore</c>
        /// subsystem to perform two passes that call <c>Write</c> with colors
        /// enabled and then <c>WriteLine</c> with colors disabled.
        /// </summary>
        protected internal static readonly int[] TwoPassesForWriteCore = { 1, 2 };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Section Names
        /// <summary>
        /// The name of the header section of the host display.
        /// </summary>
        protected static readonly string HeaderSectionName = "Header";
        /// <summary>
        /// The name of the footer section of the host display.
        /// </summary>
        protected static readonly string FooterSectionName = "Footer";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Box Names
        /// <summary>
        /// The name of the box used to display argument information.
        /// </summary>
        protected static readonly string ArgumentInfoBoxName = "ArgumentInfo";
        /// <summary>
        /// The name of the box used to display call frame information.
        /// </summary>
        protected static readonly string CallFrameInfoBoxName = "CallFrameInfo";
        /// <summary>
        /// The name of the box used to display call stack information.
        /// </summary>
        protected static readonly string CallStackInfoBoxName = "CallStackInfo";
        /// <summary>
        /// The name of the box used to display debugger information.
        /// </summary>
        protected static readonly string DebuggerInfoBoxName = "DebuggerInfo";
        /// <summary>
        /// The name of the box used to display flag information.
        /// </summary>
        protected static readonly string FlagInfoBoxName = "FlagInfo";
        /// <summary>
        /// The name of the box used to display host information.
        /// </summary>
        protected static readonly string HostInfoBoxName = "HostInfo";
        /// <summary>
        /// The name of the box used to display interpreter information.
        /// </summary>
        protected static readonly string InterpreterInfoBoxName = "InterpreterInfo";
        /// <summary>
        /// The name of the box used to display engine information.
        /// </summary>
        protected static readonly string EngineInfoBoxName = "EngineInfo";
        /// <summary>
        /// The name of the box used to display entity information.
        /// </summary>
        protected static readonly string EntityInfoBoxName = "EntityInfo";
        /// <summary>
        /// The name of the box used to display stack information.
        /// </summary>
        protected static readonly string StackInfoBoxName = "StackInfo";
        /// <summary>
        /// The name of the box used to display control information.
        /// </summary>
        protected static readonly string ControlInfoBoxName = "ControlInfo";
        /// <summary>
        /// The name of the box used to display test information.
        /// </summary>
        protected static readonly string TestInfoBoxName = "TestInfo";
        /// <summary>
        /// The name of the box used to display trace information.
        /// </summary>
        protected static readonly string TraceInfoBoxName = "TraceInfo";
        /// <summary>
        /// The name of the box used to display token information.
        /// </summary>
        protected static readonly string TokenInfoBoxName = "TokenInfo";
        /// <summary>
        /// The name of the box used to display variable information.
        /// </summary>
        protected static readonly string VariableInfoBoxName = "VariableInfo";
        /// <summary>
        /// The name of the box used to display object information.
        /// </summary>
        protected static readonly string ObjectInfoBoxName = "ObjectInfo";
        /// <summary>
        /// The name of the box used to display complaint information.
        /// </summary>
        protected static readonly string ComplaintInfoBoxName = "ComplaintInfo";
        /// <summary>
        /// The name of the box used to display history information.
        /// </summary>
        protected static readonly string HistoryInfoBoxName = "HistoryInfo";
        /// <summary>
        /// The name of the box used to display custom information.
        /// </summary>
        protected static readonly string CustomInfoBoxName = "CustomInfo";
        /// <summary>
        /// The name of the box used to display result information.
        /// </summary>
        protected static readonly string ResultInfoBoxName = "ResultInfo";
        /// <summary>
        /// The name of the box used to display previous result information.
        /// </summary>
        protected static readonly string PreviousResultInfoBoxName = "PreviousResultInfo";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Script Names
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The collection of well-known data names recognized by this host,
        /// including the script type names, file names without their paths, and
        /// fully qualified file names used to locate and load the standard
        /// scripts.
        /// </summary>
        protected static IDictionary<string, string> wellKnownDataNames =
            new StringDictionary(new string[]  {

            ///////////////////////////////////////////////////////////////////////////////////////////

            ScriptTypes.Loader,
            ScriptTypes.Initialization,
            ScriptTypes.Embedding,
            ScriptTypes.Vendor,
            ScriptTypes.TrustedRemote,
            ScriptTypes.Startup,
            ScriptTypes.Worker,
            ScriptTypes.Safe,
            ScriptTypes.Shell,
            ScriptTypes.ShellWorker,
            ScriptTypes.Test,
            ScriptTypes.PackageIndex,
            ScriptTypes.All,
            ScriptTypes.Constraints,
            ScriptTypes.Epilogue,
            ScriptTypes.Prologue,

            ///////////////////////////////////////////////////////////////////////////////////////////

            FileNameOnly.Loader,
            /* FileNameOnly.LoaderPackageIndex, */ /* DUPLICATE */
            FileNameOnly.Initialization,
            FileNameOnly.Embedding,
            FileNameOnly.Vendor,
            FileNameOnly.TrustedRemote,
            FileNameOnly.Startup,
            FileNameOnly.Worker,
            FileNameOnly.Safe,
            FileNameOnly.Shell,
            FileNameOnly.ShellWorker,
            FileNameOnly.Test,
            FileNameOnly.LibraryPackageIndex,
            FileNameOnly.All,
            FileNameOnly.Constraints,
            FileNameOnly.Epilogue,
            FileNameOnly.Prologue,
            /* FileNameOnly.TestPackageIndex, */ /* DUPLICATE */
            /* FileNameOnly.KitPackageIndex, */ /* DUPLICATE */

            ///////////////////////////////////////////////////////////////////////////////////////////

            FileName.Loader,
            FileName.LoaderPackageIndex,
            FileName.Initialization,
            FileName.Embedding,
            FileName.Vendor,
            FileName.TrustedRemote,
            FileName.Startup,
            FileName.Worker,
            FileName.Safe,
            FileName.Shell,
            FileName.ShellWorker,
            FileName.Test,
            FileName.LibraryPackageIndex,
            FileName.All,
            FileName.Constraints,
            FileName.Epilogue,
            FileName.Prologue,
            FileName.TestPackageIndex,
            FileName.KitPackageIndex
        }, true, false);
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constants
        /// <summary>
        /// The prefix used when building the color names associated with box
        /// rendering.
        /// </summary>
        public static readonly string BoxColorPrefix = "Box";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The character value used to indicate that no separator character
        /// should be emitted.
        /// </summary>
        public static readonly char NoSeparator = char.MinValue;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default value indicating whether this host permits the
        /// interpreter to exit.
        /// </summary>
        public static readonly bool DefaultCanExit = true;
        /// <summary>
        /// The default value indicating whether this host permits the
        /// interpreter to be forcibly exited.
        /// </summary>
        public static readonly bool DefaultCanForceExit = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Data
        /// <summary>
        /// The object used to synchronize access to the static data of this
        /// class across all threads.
        /// </summary>
        private static readonly object staticSyncRoot = new object();
        /// <summary>
        /// The identifier of the thread that currently holds the static
        /// synchronization lock, or zero if no thread holds it.
        /// </summary>
        private static long staticLockThreadId = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The current nesting level of box rendering operations in progress
        /// for this host.
        /// </summary>
        private int boxLevels;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// This method initializes a new instance of the <see cref="Default" />
        /// class.  It establishes the default display size constraints for each
        /// section of the header, initializes the available box character sets,
        /// and selects the active box character set.
        /// </summary>
        private Default()
        {
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
        /// <summary>
        /// This method initializes a new instance of the <see cref="Default" />
        /// class using the specified host data.  It sets up the identity,
        /// grouping, descriptive metadata, host creation flags, exit-related
        /// properties, and the system default user-interface colors.
        /// </summary>
        /// <param name="hostData">
        /// The host data used to initialize the identity, name, description,
        /// client data, profile, and creation flags for this host, or null to
        /// use the default values.
        /// </param>
        protected Default(
            IHostData hostData
            )
            : this()
        {
            kind = IdentifierKind.Host;

            if ((hostData == null) ||
                !FlagOps.HasFlags(hostData.HostCreateFlags,
                    HostCreateFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            if (hostData != null)
            {
                id = hostData.Id;

                EntityOps.MaybeSetupId(this);

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
        /// <summary>
        /// The backing field for the <see cref="SectionSizes" /> property.
        /// </summary>
        private Dictionary<HeaderFlags, HostFlags> sectionSizes;
        /// <summary>
        /// Gets or sets the mapping of header flags to the host flags that describe the
        /// available size for each corresponding section.
        /// </summary>
        protected virtual Dictionary<HeaderFlags, HostFlags> SectionSizes
        {
            get { return sectionSizes; }
            set { sectionSizes = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Flags Properties
        /// <summary>
        /// Gets or sets the header flags that control which sections are included when
        /// formatting header output.
        /// </summary>
        protected virtual HeaderFlags HeaderFlags
        {
            get { return headerFlags; }
            set { headerFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the detail flags that control the level of detail included when
        /// formatting output.
        /// </summary>
        protected virtual DetailFlags DetailFlags
        {
            get { return detailFlags; }
            set { detailFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Call Frame Properties
        #region Call Frame Formatting
        /// <summary>
        /// The backing field for the <see cref="VariableCallFrameSeparator" /> property.
        /// </summary>
        private char variableCallFrameSeparator = '*';
        /// <summary>
        /// Gets or sets the character used to mark a variable call frame when formatting
        /// call frame information.
        /// </summary>
        protected virtual char VariableCallFrameSeparator
        {
            get { return variableCallFrameSeparator; }
            set { variableCallFrameSeparator = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="VariableCallFrameSuffix" /> property.
        /// </summary>
        private string variableCallFrameSuffix = null;
        /// <summary>
        /// Gets or sets the suffix appended to the variable call frame type name when
        /// formatting call frame information.
        /// </summary>
        protected virtual string VariableCallFrameSuffix
        {
            get { return variableCallFrameSuffix; }
            set { variableCallFrameSuffix = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Call Frame Type Names
        /// <summary>
        /// The backing field for the <see cref="AliasCallFrameTypeName" /> property.
        /// </summary>
        private string aliasCallFrameTypeName = "alis";
        /// <summary>
        /// Gets or sets the short type name used to represent an alias call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string AliasCallFrameTypeName
        {
            get { return aliasCallFrameTypeName; }
            set { aliasCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="CurrentCallFrameTypeName" /> property.
        /// </summary>
        private string currentCallFrameTypeName = "curr";
        /// <summary>
        /// Gets or sets the short type name used to represent a current call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string CurrentCallFrameTypeName
        {
            get { return currentCallFrameTypeName; }
            set { currentCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DownlevelCallFrameTypeName" /> property.
        /// </summary>
        private string downlevelCallFrameTypeName = "dnlv";
        /// <summary>
        /// Gets or sets the short type name used to represent a downlevel call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string DownlevelCallFrameTypeName
        {
            get { return downlevelCallFrameTypeName; }
            set { downlevelCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EngineCallFrameTypeName" /> property.
        /// </summary>
        private string engineCallFrameTypeName = "engn";
        /// <summary>
        /// Gets or sets the short type name used to represent an engine call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string EngineCallFrameTypeName
        {
            get { return engineCallFrameTypeName; }
            set { engineCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="GlobalCallFrameTypeName" /> property.
        /// </summary>
        private string globalCallFrameTypeName = "glob";
        /// <summary>
        /// Gets or sets the short type name used to represent a global call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string GlobalCallFrameTypeName
        {
            get { return globalCallFrameTypeName; }
            set { globalCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="GlobalScopeCallFrameTypeName" /> property.
        /// </summary>
        private string globalScopeCallFrameTypeName = "gbsp";
        /// <summary>
        /// Gets or sets the short type name used to represent a global scope call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string GlobalScopeCallFrameTypeName
        {
            get { return globalScopeCallFrameTypeName; }
            set { globalScopeCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="LambdaCallFrameTypeName" /> property.
        /// </summary>
        private string lambdaCallFrameTypeName = "lamb";
        /// <summary>
        /// Gets or sets the short type name used to represent a lambda call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string LambdaCallFrameTypeName
        {
            get { return lambdaCallFrameTypeName; }
            set { lambdaCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="LinkedCallFrameTypeName" /> property.
        /// </summary>
        private string linkedCallFrameTypeName = "link";
        /// <summary>
        /// Gets or sets the short type name used to represent a linked call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string LinkedCallFrameTypeName
        {
            get { return linkedCallFrameTypeName; }
            set { linkedCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="NamespaceCallFrameTypeName" /> property.
        /// </summary>
        private string namespaceCallFrameTypeName = "nmsp";
        /// <summary>
        /// Gets or sets the short type name used to represent a namespace call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string NamespaceCallFrameTypeName
        {
            get { return namespaceCallFrameTypeName; }
            set { namespaceCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="NextCallFrameTypeName" /> property.
        /// </summary>
        private string nextCallFrameTypeName = "next";
        /// <summary>
        /// Gets or sets the short type name used to represent a next call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string NextCallFrameTypeName
        {
            get { return nextCallFrameTypeName; }
            set { nextCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Yes, this normally returns only whitespace.
        //
        /// <summary>
        /// The backing field for the <see cref="NormalCallFrameTypeName" /> property.
        /// </summary>
        private string normalCallFrameTypeName = "    ";
        /// <summary>
        /// Gets or sets the short type name used to represent a normal call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string NormalCallFrameTypeName
        {
            get { return normalCallFrameTypeName; }
            set { normalCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="NullCallFrameTypeName" /> property.
        /// </summary>
        private string nullCallFrameTypeName = _String.Null;
        /// <summary>
        /// Gets or sets the short type name used to represent a null call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string NullCallFrameTypeName
        {
            get { return nullCallFrameTypeName; }
            set { nullCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="OtherCallFrameTypeName" /> property.
        /// </summary>
        private string otherCallFrameTypeName = "othr";
        /// <summary>
        /// Gets or sets the short type name used to represent an other call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string OtherCallFrameTypeName
        {
            get { return otherCallFrameTypeName; }
            set { otherCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="PreviousCallFrameTypeName" /> property.
        /// </summary>
        private string previousCallFrameTypeName = "prev";
        /// <summary>
        /// Gets or sets the short type name used to represent a previous call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string PreviousCallFrameTypeName
        {
            get { return previousCallFrameTypeName; }
            set { previousCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ProcedureCallFrameTypeName" /> property.
        /// </summary>
        private string procedureCallFrameTypeName = "proc";
        /// <summary>
        /// Gets or sets the short type name used to represent a procedure call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string ProcedureCallFrameTypeName
        {
            get { return procedureCallFrameTypeName; }
            set { procedureCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ScopeCallFrameTypeName" /> property.
        /// </summary>
        private string scopeCallFrameTypeName = "scop";
        /// <summary>
        /// Gets or sets the short type name used to represent a scope call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string ScopeCallFrameTypeName
        {
            get { return scopeCallFrameTypeName; }
            set { scopeCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="TrackingCallFrameTypeName" /> property.
        /// </summary>
        private string trackingCallFrameTypeName = "trck";
        /// <summary>
        /// Gets or sets the short type name used to represent a tracking call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string TrackingCallFrameTypeName
        {
            get { return trackingCallFrameTypeName; }
            set { trackingCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UnknownCallFrameTypeName" /> property.
        /// </summary>
        private string unknownCallFrameTypeName = "unkn";
        /// <summary>
        /// Gets or sets the short type name used to represent an unknown call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string UnknownCallFrameTypeName
        {
            get { return unknownCallFrameTypeName; }
            set { unknownCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UplevelCallFrameTypeName" /> property.
        /// </summary>
        private string uplevelCallFrameTypeName = "uplv";
        /// <summary>
        /// Gets or sets the short type name used to represent an uplevel call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string UplevelCallFrameTypeName
        {
            get { return uplevelCallFrameTypeName; }
            set { uplevelCallFrameTypeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="VariableCallFrameTypeName" /> property.
        /// </summary>
        private string variableCallFrameTypeName = "vars";
        /// <summary>
        /// Gets or sets the short type name used to represent a variable call frame when
        /// formatting call frame information.
        /// </summary>
        protected virtual string VariableCallFrameTypeName
        {
            get { return variableCallFrameTypeName; }
            set { variableCallFrameTypeName = value; }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Formatting Properties
        /// <summary>
        /// Gets or sets a value indicating whether the debug formatting mode is enabled,
        /// based on the host creation flags.
        /// </summary>
        protected virtual bool Debug
        {
            get { return HasCreateFlags(HostCreateFlags.Debug, true); }
            set { MaybeEnableCreateFlags(HostCreateFlags.Debug, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether new-line sequences should be replaced when
        /// formatting output, based on the host creation flags.
        /// </summary>
        protected internal virtual bool ReplaceNewLines
        {
            get { return HasCreateFlags(HostCreateFlags.ReplaceNewLines, true); }
            set { MaybeEnableCreateFlags(HostCreateFlags.ReplaceNewLines, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether an ellipsis should be used to indicate
        /// truncated output, based on the host creation flags.
        /// </summary>
        protected internal virtual bool Ellipsis
        {
            get { return HasCreateFlags(HostCreateFlags.Ellipsis, true); }
            set { MaybeEnableCreateFlags(HostCreateFlags.Ellipsis, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether exception details should be included when
        /// formatting output, based on the host creation flags.
        /// </summary>
        protected internal virtual bool Exceptions
        {
            get { return HasCreateFlags(HostCreateFlags.Exceptions, true); }
            set { MaybeEnableCreateFlags(HostCreateFlags.Exceptions, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether display-oriented formatting is enabled,
        /// based on the host creation flags.
        /// </summary>
        protected internal virtual bool Display
        {
            get { return HasCreateFlags(HostCreateFlags.Display, true); }
            set { MaybeEnableCreateFlags(HostCreateFlags.Display, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HistoryLimit" /> property.
        /// </summary>
        private int historyLimit = 20;
        /// <summary>
        /// Gets or sets the maximum number of history entries to include when formatting
        /// history output.
        /// </summary>
        protected virtual int HistoryLimit
        {
            get { return historyLimit; }
            set { historyLimit = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="CallStackLimit" /> property.
        /// </summary>
        private int callStackLimit = 20;
        /// <summary>
        /// Gets or sets the maximum number of call stack entries to include when formatting
        /// call stack output.
        /// </summary>
        protected virtual int CallStackLimit
        {
            get { return callStackLimit; }
            set { callStackLimit = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="SectionsPerRow" /> property.
        /// </summary>
        private int sectionsPerRow = 0;
        /// <summary>
        /// Gets or sets the number of sections to display per row when formatting sectioned
        /// output.  A value of zero indicates no fixed limit.
        /// </summary>
        protected virtual int SectionsPerRow
        {
            get { return sectionsPerRow; }
            set { sectionsPerRow = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="NameValueFormat" /> property.
        /// </summary>
        private string nameValueFormat = "{0}:" + Characters.Space + "{1}";
        /// <summary>
        /// Gets or sets the composite format string used to format a name and value pair.
        /// </summary>
        protected virtual string NameValueFormat
        {
            get { return nameValueFormat; }
            set { nameValueFormat = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Prompt Properties
        /// <summary>
        /// The backing field for the <see cref="StopPrompt" /> property.
        /// </summary>
        private string stopPrompt = "[stop]";
        /// <summary>
        /// Gets or sets the prompt text displayed to indicate a stopped state.
        /// </summary>
        protected virtual string StopPrompt
        {
            get { return stopPrompt; }
            set { stopPrompt = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="GoPrompt" /> property.
        /// </summary>
        private string goPrompt = "[go]";
        /// <summary>
        /// Gets or sets the prompt text displayed to indicate a running (go) state.
        /// </summary>
        protected virtual string GoPrompt
        {
            get { return goPrompt; }
            set { goPrompt = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Output Properties
        #region Output Style Properties
        /// <summary>
        /// The backing field for the <see cref="OutputStyle" /> property.
        /// </summary>
        private OutputStyle outputStyle = OutputStyle.Default;
        /// <summary>
        /// Gets or sets the output style used when writing host output.
        /// </summary>
        protected internal virtual OutputStyle OutputStyle // TODO: Make this part of IHost?
        {
            get { return outputStyle; }
            set { outputStyle = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified output style is neither formatted nor boxed.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style to examine.
        /// </param>
        /// <returns>
        /// True if the specified output style is neither formatted nor boxed; otherwise,
        /// false.
        /// </returns>
        protected internal virtual bool IsNoneOutputStyle(
            OutputStyle outputStyle
            )
        {
            return !IsFormattedOutputStyle(outputStyle) &&
                !IsBoxedOutputStyle(outputStyle);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified output style includes the formatted flag.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style to examine.
        /// </param>
        /// <returns>
        /// True if the specified output style includes the formatted flag; otherwise, false.
        /// </returns>
        protected internal virtual bool IsFormattedOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.Formatted, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified output style includes the boxed flag.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style to examine.
        /// </param>
        /// <returns>
        /// True if the specified output style includes the boxed flag; otherwise, false.
        /// </returns>
        protected internal virtual bool IsBoxedOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.Boxed, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified output style includes the normal flag.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style to examine.
        /// </param>
        /// <returns>
        /// True if the specified output style includes the normal flag; otherwise, false.
        /// </returns>
        protected internal virtual bool IsNormalOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.Normal, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified output style includes the debug flag.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style to examine.
        /// </param>
        /// <returns>
        /// True if the specified output style includes the debug flag; otherwise, false.
        /// </returns>
        protected internal virtual bool IsDebugOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.Debug, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified output style includes the error flag.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style to examine.
        /// </param>
        /// <returns>
        /// True if the specified output style includes the error flag; otherwise, false.
        /// </returns>
        protected internal virtual bool IsErrorOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.Error, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified output style includes the reversed text flag.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style to examine.
        /// </param>
        /// <returns>
        /// True if the specified output style includes the reversed text flag; otherwise,
        /// false.
        /// </returns>
        protected virtual bool IsReversedTextOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.ReversedText, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified output style includes the reversed border flag.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style to examine.
        /// </param>
        /// <returns>
        /// True if the specified output style includes the reversed border flag; otherwise,
        /// false.
        /// </returns>
        protected virtual bool IsReversedBorderOutputStyle(
            OutputStyle outputStyle
            )
        {
            return FlagOps.HasFlags(
                outputStyle, OutputStyle.ReversedBorder, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the specified output style into the corresponding host write type.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style to convert.
        /// </param>
        /// <returns>
        /// The <see cref="HostWriteType" /> value that corresponds to the specified output
        /// style.
        /// </returns>
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
        /// <summary>
        /// The backing field for the <see cref="HostLeft" /> property.
        /// </summary>
        private int hostLeft = 0;
        /// <summary>
        /// Gets or sets the horizontal (left) position of the host.
        /// </summary>
        protected virtual int HostLeft
        {
            get { return hostLeft; }
            set { hostLeft = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HostTop" /> property.
        /// </summary>
        private int hostTop = 0;
        /// <summary>
        /// Gets or sets the vertical (top) position of the host.
        /// </summary>
        protected virtual int HostTop
        {
            get { return hostTop; }
            set { hostTop = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="WindowWidth" /> property.
        /// </summary>
        private int windowWidth = Width.Default;
        /// <summary>
        /// Gets or sets the width, in characters, of the host window.
        /// </summary>
        protected virtual int WindowWidth
        {
            get { return windowWidth; }
            set { windowWidth = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="WindowHeight" /> property.
        /// </summary>
        private int windowHeight = Height.Default;
        /// <summary>
        /// Gets or sets the height, in characters, of the host window.
        /// </summary>
        protected virtual int WindowHeight
        {
            get { return windowHeight; }
            set { windowHeight = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Content Area Properties
        /// <summary>
        /// The backing field for the <see cref="ContentMargin" /> property.
        /// </summary>
        private int contentMargin = 0;
        /// <summary>
        /// Gets or sets the margin, in characters, applied to the content area.
        /// </summary>
        protected internal virtual int ContentMargin
        {
            get { return contentMargin; }
            set { contentMargin = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ContentWidth" /> property.
        /// </summary>
        private int contentWidth = Width.Invalid;
        /// <summary>
        /// Gets or sets the width, in characters, of the content area.  When no explicit
        /// width has been set, the width is derived from the window width.
        /// </summary>
        protected virtual int ContentWidth
        {
            get { return (contentWidth != Width.Invalid) ? contentWidth : WindowWidth - 3; }
            set { contentWidth = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ContentThreshold" /> property.
        /// </summary>
        private int contentThreshold = 20;
        /// <summary>
        /// Gets or sets the minimum content width threshold, in characters, used when
        /// formatting content.
        /// </summary>
        protected virtual int ContentThreshold
        {
            get { return contentThreshold; }
            set { contentThreshold = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="MinimumLength" /> property.
        /// </summary>
        private int minimumLength = Length.Invalid;
        /// <summary>
        /// Gets or sets the minimum length, in characters, used when formatting content.
        /// </summary>
        protected virtual int MinimumLength
        {
            get { return minimumLength; }
            set { minimumLength = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Box Properties
        /// <summary>
        /// The backing field for the <see cref="BoxCharacterSet" /> property.
        /// </summary>
        private int boxCharacterSet;
        /// <summary>
        /// Gets or sets the index of the box character set currently used when drawing boxes.
        /// </summary>
        protected internal virtual int BoxCharacterSet
        {
            get { return boxCharacterSet; }
            set { boxCharacterSet = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="BoxCharacterSets" /> property.
        /// </summary>
        private StringList boxCharacterSets;
        /// <summary>
        /// Gets or sets the list of available box character sets used when drawing boxes.
        /// </summary>
        protected internal virtual StringList BoxCharacterSets
        {
            get { return boxCharacterSets; }
            set { boxCharacterSets = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="BoxWidth" /> property.
        /// </summary>
        private int boxWidth = Width.Invalid;
        /// <summary>
        /// Gets or sets the width, in characters, used when drawing boxes.
        /// </summary>
        protected virtual int BoxWidth
        {
            get { return boxWidth; }
            set { boxWidth = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="BoxMargin" /> property.
        /// </summary>
        private int boxMargin = Margin.Default;
        /// <summary>
        /// Gets or sets the margin, in characters, applied when drawing boxes.
        /// </summary>
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
        /// <summary>
        /// The backing field for the <see cref="BannerForegroundColor" /> property.
        /// </summary>
        private ConsoleColor bannerForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the banner category.
        /// </summary>
        protected virtual ConsoleColor BannerForegroundColor
        {
            get { return bannerForegroundColor; }
            set { bannerForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="BannerBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor bannerBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the banner category.
        /// </summary>
        protected virtual ConsoleColor BannerBackgroundColor
        {
            get { return bannerBackgroundColor; }
            set { bannerBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="BoxForegroundColor" /> property.
        /// </summary>
        private ConsoleColor boxForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the box category.
        /// </summary>
        protected virtual ConsoleColor BoxForegroundColor
        {
            get { return boxForegroundColor; }
            set { boxForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="BoxBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor boxBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the box category.
        /// </summary>
        protected virtual ConsoleColor BoxBackgroundColor
        {
            get { return boxBackgroundColor; }
            set { boxBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DebugForegroundColor" /> property.
        /// </summary>
        private ConsoleColor debugForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the debug category.
        /// </summary>
        protected virtual ConsoleColor DebugForegroundColor
        {
            get { return debugForegroundColor; }
            set { debugForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DebugBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor debugBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the debug category.
        /// </summary>
        protected virtual ConsoleColor DebugBackgroundColor
        {
            get { return debugBackgroundColor; }
            set { debugBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DefaultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor defaultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the default category.
        /// </summary>
        protected virtual ConsoleColor DefaultForegroundColor
        {
            get { return defaultForegroundColor; }
            set { defaultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DefaultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor defaultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the default category.
        /// </summary>
        protected virtual ConsoleColor DefaultBackgroundColor
        {
            get { return defaultBackgroundColor; }
            set { defaultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DisabledForegroundColor" /> property.
        /// </summary>
        private ConsoleColor disabledForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the disabled category.
        /// </summary>
        protected virtual ConsoleColor DisabledForegroundColor
        {
            get { return disabledForegroundColor; }
            set { disabledForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DisabledBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor disabledBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the disabled category.
        /// </summary>
        protected virtual ConsoleColor DisabledBackgroundColor
        {
            get { return disabledBackgroundColor; }
            set { disabledBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EnabledForegroundColor" /> property.
        /// </summary>
        private ConsoleColor enabledForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the enabled category.
        /// </summary>
        protected virtual ConsoleColor EnabledForegroundColor
        {
            get { return enabledForegroundColor; }
            set { enabledForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EnabledBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor enabledBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the enabled category.
        /// </summary>
        protected virtual ConsoleColor EnabledBackgroundColor
        {
            get { return enabledBackgroundColor; }
            set { enabledBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ErrorForegroundColor" /> property.
        /// </summary>
        private ConsoleColor errorForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the error category.
        /// </summary>
        protected virtual ConsoleColor ErrorForegroundColor
        {
            get { return errorForegroundColor; }
            set { errorForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ErrorBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor errorBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the error category.
        /// </summary>
        protected virtual ConsoleColor ErrorBackgroundColor
        {
            get { return errorBackgroundColor; }
            set { errorBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="FatalForegroundColor" /> property.
        /// </summary>
        private ConsoleColor fatalForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the fatal category.
        /// </summary>
        protected virtual ConsoleColor FatalForegroundColor
        {
            get { return fatalForegroundColor; }
            set { fatalForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="FatalBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor fatalBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the fatal category.
        /// </summary>
        protected virtual ConsoleColor FatalBackgroundColor
        {
            get { return fatalBackgroundColor; }
            set { fatalBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="FooterForegroundColor" /> property.
        /// </summary>
        private ConsoleColor footerForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the footer category.
        /// </summary>
        protected virtual ConsoleColor FooterForegroundColor
        {
            get { return footerForegroundColor; }
            set { footerForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="FooterBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor footerBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the footer category.
        /// </summary>
        protected virtual ConsoleColor FooterBackgroundColor
        {
            get { return footerBackgroundColor; }
            set { footerBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HeaderForegroundColor" /> property.
        /// </summary>
        private ConsoleColor headerForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the header category.
        /// </summary>
        protected virtual ConsoleColor HeaderForegroundColor
        {
            get { return headerForegroundColor; }
            set { headerForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HeaderBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor headerBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the header category.
        /// </summary>
        protected virtual ConsoleColor HeaderBackgroundColor
        {
            get { return headerBackgroundColor; }
            set { headerBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HelpForegroundColor" /> property.
        /// </summary>
        private ConsoleColor helpForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the help category.
        /// </summary>
        protected virtual ConsoleColor HelpForegroundColor
        {
            get { return helpForegroundColor; }
            set { helpForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HelpBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor helpBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the help category.
        /// </summary>
        protected virtual ConsoleColor HelpBackgroundColor
        {
            get { return helpBackgroundColor; }
            set { helpBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HelpItemForegroundColor" /> property.
        /// </summary>
        private ConsoleColor helpItemForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the help item category.
        /// </summary>
        protected virtual ConsoleColor HelpItemForegroundColor
        {
            get { return helpItemForegroundColor; }
            set { helpItemForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HelpItemBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor helpItemBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the help item category.
        /// </summary>
        protected virtual ConsoleColor HelpItemBackgroundColor
        {
            get { return helpItemBackgroundColor; }
            set { helpItemBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="LegalForegroundColor" /> property.
        /// </summary>
        private ConsoleColor legalForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the legal category.
        /// </summary>
        protected virtual ConsoleColor LegalForegroundColor
        {
            get { return legalForegroundColor; }
            set { legalForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="LegalBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor legalBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the legal category.
        /// </summary>
        protected virtual ConsoleColor LegalBackgroundColor
        {
            get { return legalBackgroundColor; }
            set { legalBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="OfficialForegroundColor" /> property.
        /// </summary>
        private ConsoleColor officialForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the official category.
        /// </summary>
        protected virtual ConsoleColor OfficialForegroundColor
        {
            get { return officialForegroundColor; }
            set { officialForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="OfficialBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor officialBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the official category.
        /// </summary>
        protected virtual ConsoleColor OfficialBackgroundColor
        {
            get { return officialBackgroundColor; }
            set { officialBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor resultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the result category.
        /// </summary>
        protected virtual ConsoleColor ResultForegroundColor
        {
            get { return resultForegroundColor; }
            set { resultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor resultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the result category.
        /// </summary>
        protected virtual ConsoleColor ResultBackgroundColor
        {
            get { return resultBackgroundColor; }
            set { resultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="StableForegroundColor" /> property.
        /// </summary>
        private ConsoleColor stableForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the stable category.
        /// </summary>
        protected virtual ConsoleColor StableForegroundColor
        {
            get { return stableForegroundColor; }
            set { stableForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="StableBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor stableBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the stable category.
        /// </summary>
        protected virtual ConsoleColor StableBackgroundColor
        {
            get { return stableBackgroundColor; }
            set { stableBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="TrustedForegroundColor" /> property.
        /// </summary>
        private ConsoleColor trustedForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the trusted category.
        /// </summary>
        protected virtual ConsoleColor TrustedForegroundColor
        {
            get { return trustedForegroundColor; }
            set { trustedForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="TrustedBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor trustedBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the trusted category.
        /// </summary>
        protected virtual ConsoleColor TrustedBackgroundColor
        {
            get { return trustedBackgroundColor; }
            set { trustedBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UndefinedForegroundColor" /> property.
        /// </summary>
        private ConsoleColor undefinedForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the undefined category.
        /// </summary>
        protected virtual ConsoleColor UndefinedForegroundColor
        {
            get { return undefinedForegroundColor; }
            set { undefinedForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UndefinedBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor undefinedBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the undefined category.
        /// </summary>
        protected virtual ConsoleColor UndefinedBackgroundColor
        {
            get { return undefinedBackgroundColor; }
            set { undefinedBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UnofficialForegroundColor" /> property.
        /// </summary>
        private ConsoleColor unofficialForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the unofficial category.
        /// </summary>
        protected virtual ConsoleColor UnofficialForegroundColor
        {
            get { return unofficialForegroundColor; }
            set { unofficialForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UnofficialBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor unofficialBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the unofficial category.
        /// </summary>
        protected virtual ConsoleColor UnofficialBackgroundColor
        {
            get { return unofficialBackgroundColor; }
            set { unofficialBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UnstableForegroundColor" /> property.
        /// </summary>
        private ConsoleColor unstableForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the unstable category.
        /// </summary>
        protected virtual ConsoleColor UnstableForegroundColor
        {
            get { return unstableForegroundColor; }
            set { unstableForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UnstableBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor unstableBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the unstable category.
        /// </summary>
        protected virtual ConsoleColor UnstableBackgroundColor
        {
            get { return unstableBackgroundColor; }
            set { unstableBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UntrustedForegroundColor" /> property.
        /// </summary>
        private ConsoleColor untrustedForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the untrusted category.
        /// </summary>
        protected virtual ConsoleColor UntrustedForegroundColor
        {
            get { return untrustedForegroundColor; }
            set { untrustedForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UntrustedBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor untrustedBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the untrusted category.
        /// </summary>
        protected virtual ConsoleColor UntrustedBackgroundColor
        {
            get { return untrustedBackgroundColor; }
            set { untrustedBackgroundColor = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Section Colors
        /// <summary>
        /// The backing field for the <see cref="AnnouncementInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor announcementInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the announcement information category.
        /// </summary>
        protected virtual ConsoleColor AnnouncementInfoForegroundColor
        {
            get { return announcementInfoForegroundColor; }
            set { announcementInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="AnnouncementInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor announcementInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the announcement information category.
        /// </summary>
        protected virtual ConsoleColor AnnouncementInfoBackgroundColor
        {
            get { return announcementInfoBackgroundColor; }
            set { announcementInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ArgumentInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor argumentInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the argument information category.
        /// </summary>
        protected virtual ConsoleColor ArgumentInfoForegroundColor
        {
            get { return argumentInfoForegroundColor; }
            set { argumentInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ArgumentInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor argumentInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the argument information category.
        /// </summary>
        protected virtual ConsoleColor ArgumentInfoBackgroundColor
        {
            get { return argumentInfoBackgroundColor; }
            set { argumentInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="CallFrameInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor callFrameInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the call frame information category.
        /// </summary>
        protected virtual ConsoleColor CallFrameInfoForegroundColor
        {
            get { return callFrameInfoForegroundColor; }
            set { callFrameInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="CallFrameInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor callFrameInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the call frame information category.
        /// </summary>
        protected virtual ConsoleColor CallFrameInfoBackgroundColor
        {
            get { return callFrameInfoBackgroundColor; }
            set { callFrameInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="CallStackInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor callStackInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the call stack information category.
        /// </summary>
        protected virtual ConsoleColor CallStackInfoForegroundColor
        {
            get { return callStackInfoForegroundColor; }
            set { callStackInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="CallStackInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor callStackInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the call stack information category.
        /// </summary>
        protected virtual ConsoleColor CallStackInfoBackgroundColor
        {
            get { return callStackInfoBackgroundColor; }
            set { callStackInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ComplaintInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor complaintInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the complaint information category.
        /// </summary>
        protected virtual ConsoleColor ComplaintInfoForegroundColor
        {
            get { return complaintInfoForegroundColor; }
            set { complaintInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ComplaintInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor complaintInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the complaint information category.
        /// </summary>
        protected virtual ConsoleColor ComplaintInfoBackgroundColor
        {
            get { return complaintInfoBackgroundColor; }
            set { complaintInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ControlInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor controlInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the control information category.
        /// </summary>
        protected virtual ConsoleColor ControlInfoForegroundColor
        {
            get { return controlInfoForegroundColor; }
            set { controlInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ControlInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor controlInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the control information category.
        /// </summary>
        protected virtual ConsoleColor ControlInfoBackgroundColor
        {
            get { return controlInfoBackgroundColor; }
            set { controlInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="CustomInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor customInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the custom information category.
        /// </summary>
        protected virtual ConsoleColor CustomInfoForegroundColor
        {
            get { return customInfoForegroundColor; }
            set { customInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="CustomInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor customInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the custom information category.
        /// </summary>
        protected virtual ConsoleColor CustomInfoBackgroundColor
        {
            get { return customInfoBackgroundColor; }
            set { customInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DebuggerInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor debuggerInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the debugger information category.
        /// </summary>
        protected virtual ConsoleColor DebuggerInfoForegroundColor
        {
            get { return debuggerInfoForegroundColor; }
            set { debuggerInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DebuggerInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor debuggerInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the debugger information category.
        /// </summary>
        protected virtual ConsoleColor DebuggerInfoBackgroundColor
        {
            get { return debuggerInfoBackgroundColor; }
            set { debuggerInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EngineInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor engineInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the engine information category.
        /// </summary>
        protected virtual ConsoleColor EngineInfoForegroundColor
        {
            get { return engineInfoForegroundColor; }
            set { engineInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EngineInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor engineInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the engine information category.
        /// </summary>
        protected virtual ConsoleColor EngineInfoBackgroundColor
        {
            get { return engineInfoBackgroundColor; }
            set { engineInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EntityInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor entityInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the entity information category.
        /// </summary>
        protected virtual ConsoleColor EntityInfoForegroundColor
        {
            get { return entityInfoForegroundColor; }
            set { entityInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EntityInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor entityInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the entity information category.
        /// </summary>
        protected virtual ConsoleColor EntityInfoBackgroundColor
        {
            get { return entityInfoBackgroundColor; }
            set { entityInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="FlagInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor flagInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the flag information category.
        /// </summary>
        protected virtual ConsoleColor FlagInfoForegroundColor
        {
            get { return flagInfoForegroundColor; }
            set { flagInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="FlagInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor flagInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the flag information category.
        /// </summary>
        protected virtual ConsoleColor FlagInfoBackgroundColor
        {
            get { return flagInfoBackgroundColor; }
            set { flagInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HistoryInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor historyInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the history information category.
        /// </summary>
        protected virtual ConsoleColor HistoryInfoForegroundColor
        {
            get { return historyInfoForegroundColor; }
            set { historyInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HistoryInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor historyInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the history information category.
        /// </summary>
        protected virtual ConsoleColor HistoryInfoBackgroundColor
        {
            get { return historyInfoBackgroundColor; }
            set { historyInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HostInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor hostInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the host information category.
        /// </summary>
        protected virtual ConsoleColor HostInfoForegroundColor
        {
            get { return hostInfoForegroundColor; }
            set { hostInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HostInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor hostInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the host information category.
        /// </summary>
        protected virtual ConsoleColor HostInfoBackgroundColor
        {
            get { return hostInfoBackgroundColor; }
            set { hostInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="InterpreterInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor interpreterInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the interpreter information category.
        /// </summary>
        protected virtual ConsoleColor InterpreterInfoForegroundColor
        {
            get { return interpreterInfoForegroundColor; }
            set { interpreterInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="InterpreterInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor interpreterInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the interpreter information category.
        /// </summary>
        protected virtual ConsoleColor InterpreterInfoBackgroundColor
        {
            get { return interpreterInfoBackgroundColor; }
            set { interpreterInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ObjectInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor objectInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the object information category.
        /// </summary>
        protected virtual ConsoleColor ObjectInfoForegroundColor
        {
            get { return objectInfoForegroundColor; }
            set { objectInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ObjectInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor objectInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the object information category.
        /// </summary>
        protected virtual ConsoleColor ObjectInfoBackgroundColor
        {
            get { return objectInfoBackgroundColor; }
            set { objectInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="TestInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor testInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the test information category.
        /// </summary>
        protected virtual ConsoleColor TestInfoForegroundColor
        {
            get { return testInfoForegroundColor; }
            set { testInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="TestInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor testInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the test information category.
        /// </summary>
        protected virtual ConsoleColor TestInfoBackgroundColor
        {
            get { return testInfoBackgroundColor; }
            set { testInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="TokenInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor tokenInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the token information category.
        /// </summary>
        protected virtual ConsoleColor TokenInfoForegroundColor
        {
            get { return tokenInfoForegroundColor; }
            set { tokenInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="TokenInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor tokenInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the token information category.
        /// </summary>
        protected virtual ConsoleColor TokenInfoBackgroundColor
        {
            get { return tokenInfoBackgroundColor; }
            set { tokenInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="TraceInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor traceInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the trace information category.
        /// </summary>
        protected virtual ConsoleColor TraceInfoForegroundColor
        {
            get { return traceInfoForegroundColor; }
            set { traceInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="TraceInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor traceInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the trace information category.
        /// </summary>
        protected virtual ConsoleColor TraceInfoBackgroundColor
        {
            get { return traceInfoBackgroundColor; }
            set { traceInfoBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="VariableInfoForegroundColor" /> property.
        /// </summary>
        private ConsoleColor variableInfoForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the variable information category.
        /// </summary>
        protected virtual ConsoleColor VariableInfoForegroundColor
        {
            get { return variableInfoForegroundColor; }
            set { variableInfoForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="VariableInfoBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor variableInfoBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the variable information category.
        /// </summary>
        protected virtual ConsoleColor VariableInfoBackgroundColor
        {
            get { return variableInfoBackgroundColor; }
            set { variableInfoBackgroundColor = value; }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Return Code Colors
        /// <summary>
        /// The backing field for the <see cref="OkResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor okResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the ok result category.
        /// </summary>
        protected virtual ConsoleColor OkResultForegroundColor
        {
            get { return okResultForegroundColor; }
            set { okResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="OkResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor okResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the ok result category.
        /// </summary>
        protected virtual ConsoleColor OkResultBackgroundColor
        {
            get { return okResultBackgroundColor; }
            set { okResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ErrorResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor errorResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the error result category.
        /// </summary>
        protected virtual ConsoleColor ErrorResultForegroundColor
        {
            get { return errorResultForegroundColor; }
            set { errorResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ErrorResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor errorResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the error result category.
        /// </summary>
        protected virtual ConsoleColor ErrorResultBackgroundColor
        {
            get { return errorResultBackgroundColor; }
            set { errorResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="OtherOkResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor otherOkResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the other ok result category.
        /// </summary>
        protected virtual ConsoleColor OtherOkResultForegroundColor
        {
            get { return otherOkResultForegroundColor; }
            set { otherOkResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="OtherOkResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor otherOkResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the other ok result category.
        /// </summary>
        protected virtual ConsoleColor OtherOkResultBackgroundColor
        {
            get { return otherOkResultBackgroundColor; }
            set { otherOkResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="OtherErrorResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor otherErrorResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the other error result category.
        /// </summary>
        protected virtual ConsoleColor OtherErrorResultForegroundColor
        {
            get { return otherErrorResultForegroundColor; }
            set { otherErrorResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="OtherErrorResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor otherErrorResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the other error result category.
        /// </summary>
        protected virtual ConsoleColor OtherErrorResultBackgroundColor
        {
            get { return otherErrorResultBackgroundColor; }
            set { otherErrorResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ReturnResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor returnResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the return result category.
        /// </summary>
        protected virtual ConsoleColor ReturnResultForegroundColor
        {
            get { return returnResultForegroundColor; }
            set { returnResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ReturnResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor returnResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the return result category.
        /// </summary>
        protected virtual ConsoleColor ReturnResultBackgroundColor
        {
            get { return returnResultBackgroundColor; }
            set { returnResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="BreakResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor breakResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the break result category.
        /// </summary>
        protected virtual ConsoleColor BreakResultForegroundColor
        {
            get { return breakResultForegroundColor; }
            set { breakResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="BreakResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor breakResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the break result category.
        /// </summary>
        protected virtual ConsoleColor BreakResultBackgroundColor
        {
            get { return breakResultBackgroundColor; }
            set { breakResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ContinueResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor continueResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the continue result category.
        /// </summary>
        protected virtual ConsoleColor ContinueResultForegroundColor
        {
            get { return continueResultForegroundColor; }
            set { continueResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ContinueResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor continueResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the continue result category.
        /// </summary>
        protected virtual ConsoleColor ContinueResultBackgroundColor
        {
            get { return continueResultBackgroundColor; }
            set { continueResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="WhatIfResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor whatIfResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the what if result category.
        /// </summary>
        protected virtual ConsoleColor WhatIfResultForegroundColor
        {
            get { return whatIfResultForegroundColor; }
            set { whatIfResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="WhatIfResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor whatIfResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the what if result category.
        /// </summary>
        protected virtual ConsoleColor WhatIfResultBackgroundColor
        {
            get { return whatIfResultBackgroundColor; }
            set { whatIfResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ExceptionResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor exceptionResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the exception result category.
        /// </summary>
        protected virtual ConsoleColor ExceptionResultForegroundColor
        {
            get { return exceptionResultForegroundColor; }
            set { exceptionResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ExceptionResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor exceptionResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the exception result category.
        /// </summary>
        protected virtual ConsoleColor ExceptionResultBackgroundColor
        {
            get { return exceptionResultBackgroundColor; }
            set { exceptionResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="NullResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor nullResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the null result category.
        /// </summary>
        protected virtual ConsoleColor NullResultForegroundColor
        {
            get { return nullResultForegroundColor; }
            set { nullResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="NullResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor nullResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the null result category.
        /// </summary>
        protected virtual ConsoleColor NullResultBackgroundColor
        {
            get { return nullResultBackgroundColor; }
            set { nullResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EmptyResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor emptyResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the empty result category.
        /// </summary>
        protected virtual ConsoleColor EmptyResultForegroundColor
        {
            get { return emptyResultForegroundColor; }
            set { emptyResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EmptyResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor emptyResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the empty result category.
        /// </summary>
        protected virtual ConsoleColor EmptyResultBackgroundColor
        {
            get { return emptyResultBackgroundColor; }
            set { emptyResultBackgroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UnknownResultForegroundColor" /> property.
        /// </summary>
        private ConsoleColor unknownResultForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the unknown result category.
        /// </summary>
        protected virtual ConsoleColor UnknownResultForegroundColor
        {
            get { return unknownResultForegroundColor; }
            set { unknownResultForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UnknownResultBackgroundColor" /> property.
        /// </summary>
        private ConsoleColor unknownResultBackgroundColor;
        /// <summary>
        /// Gets or sets the background color used when displaying output associated with the unknown result category.
        /// </summary>
        protected virtual ConsoleColor UnknownResultBackgroundColor
        {
            get { return unknownResultBackgroundColor; }
            set { unknownResultBackgroundColor = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Call Frame Colors
        /// <summary>
        /// The backing field for the <see cref="NullCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor nullCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the null call frame category.
        /// </summary>
        protected virtual ConsoleColor NullCallFrameForegroundColor
        {
            get { return nullCallFrameForegroundColor; }
            set { nullCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UnknownCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor unknownCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the unknown call frame category.
        /// </summary>
        protected virtual ConsoleColor UnknownCallFrameForegroundColor
        {
            get { return unknownCallFrameForegroundColor; }
            set { unknownCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="VariableCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor variableCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the variable call frame category.
        /// </summary>
        protected virtual ConsoleColor VariableCallFrameForegroundColor
        {
            get { return variableCallFrameForegroundColor; }
            set { variableCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UplevelCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor uplevelCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the uplevel call frame category.
        /// </summary>
        protected virtual ConsoleColor UplevelCallFrameForegroundColor
        {
            get { return uplevelCallFrameForegroundColor; }
            set { uplevelCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DownlevelCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor downlevelCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the downlevel call frame category.
        /// </summary>
        protected virtual ConsoleColor DownlevelCallFrameForegroundColor
        {
            get { return downlevelCallFrameForegroundColor; }
            set { downlevelCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="TrackingCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor trackingCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the tracking call frame category.
        /// </summary>
        protected virtual ConsoleColor TrackingCallFrameForegroundColor
        {
            get { return trackingCallFrameForegroundColor; }
            set { trackingCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EngineCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor engineCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the engine call frame category.
        /// </summary>
        protected virtual ConsoleColor EngineCallFrameForegroundColor
        {
            get { return engineCallFrameForegroundColor; }
            set { engineCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="CurrentCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor currentCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the current call frame category.
        /// </summary>
        protected virtual ConsoleColor CurrentCallFrameForegroundColor
        {
            get { return currentCallFrameForegroundColor; }
            set { currentCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ProcedureCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor procedureCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the procedure call frame category.
        /// </summary>
        protected virtual ConsoleColor ProcedureCallFrameForegroundColor
        {
            get { return procedureCallFrameForegroundColor; }
            set { procedureCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="LambdaCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor lambdaCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the lambda call frame category.
        /// </summary>
        protected virtual ConsoleColor LambdaCallFrameForegroundColor
        {
            get { return lambdaCallFrameForegroundColor; }
            set { lambdaCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ScopeCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor scopeCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the scope call frame category.
        /// </summary>
        protected virtual ConsoleColor ScopeCallFrameForegroundColor
        {
            get { return scopeCallFrameForegroundColor; }
            set { scopeCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="AliasCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor aliasCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the alias call frame category.
        /// </summary>
        protected virtual ConsoleColor AliasCallFrameForegroundColor
        {
            get { return aliasCallFrameForegroundColor; }
            set { aliasCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="GlobalCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor globalCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the global call frame category.
        /// </summary>
        protected virtual ConsoleColor GlobalCallFrameForegroundColor
        {
            get { return globalCallFrameForegroundColor; }
            set { globalCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="GlobalScopeCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor globalScopeCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the global scope call frame category.
        /// </summary>
        protected virtual ConsoleColor GlobalScopeCallFrameForegroundColor
        {
            get { return globalScopeCallFrameForegroundColor; }
            set { globalScopeCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="LinkedCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor linkedCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the linked call frame category.
        /// </summary>
        protected virtual ConsoleColor LinkedCallFrameForegroundColor
        {
            get { return linkedCallFrameForegroundColor; }
            set { linkedCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="NamespaceCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor namespaceCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the namespace call frame category.
        /// </summary>
        protected virtual ConsoleColor NamespaceCallFrameForegroundColor
        {
            get { return namespaceCallFrameForegroundColor; }
            set { namespaceCallFrameForegroundColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="NormalCallFrameForegroundColor" /> property.
        /// </summary>
        private ConsoleColor normalCallFrameForegroundColor;
        /// <summary>
        /// Gets or sets the foreground (text) color used when displaying output associated with the normal call frame category.
        /// </summary>
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
        /// <summary>
        /// This method returns the current header flags for this host.
        /// </summary>
        /// <returns>
        /// The current header flags.
        /// </returns>
        private HeaderFlags PrivateGetHeaderFlags()
        {
            return headerFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the current detail flags for this host.
        /// </summary>
        /// <returns>
        /// The current detail flags.
        /// </returns>
        private DetailFlags PrivateGetDetailFlags()
        {
            return detailFlags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        /// <summary>
        /// This method resets the cached host flags to their invalid (i.e.
        /// uninitialized) value.
        /// </summary>
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the cached host flags so that they will be
        /// recomputed the next time they are needed.
        /// </summary>
        /// <returns>
        /// True if the host flags were reset; otherwise, false.
        /// </returns>
        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the host flags to their default values when
        /// they have not yet been initialized, and returns them.
        /// </summary>
        /// <returns>
        /// The current host flags.
        /// </returns>
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

        /// <summary>
        /// This method determines whether this host is currently exiting.
        /// </summary>
        /// <returns>
        /// True if this host is exiting; otherwise, false.
        /// </returns>
        protected virtual bool IsExiting()
        {
            return HasCreateFlags(HostCreateFlags.Exiting, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets whether this host is currently exiting.
        /// </summary>
        /// <param name="exiting">
        /// Non-zero if this host is exiting; otherwise, zero.
        /// </param>
        protected virtual void SetExiting(
            bool exiting
            )
        {
            MaybeEnableCreateFlags(HostCreateFlags.Exiting, exiting);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current error should be treated
        /// as fatal (i.e. unrecoverable), based on the host flags and whether
        /// the host is exiting.
        /// </summary>
        /// <returns>
        /// True if the current error should be treated as fatal; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method enables or disables the host flag that causes errors to
        /// be treated as fatal.
        /// </summary>
        /// <param name="treatAsFatalError">
        /// Non-zero to cause errors to be treated as fatal; zero to clear that
        /// behavior.
        /// </param>
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

        /// <summary>
        /// This method determines whether this host is currently operating in
        /// verbose mode.
        /// </summary>
        /// <returns>
        /// True if the verbose host flag is set; otherwise, false.
        /// </returns>
        protected virtual bool IsVerboseMode()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.Verbose, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables the host flag that indicates a read
        /// exception has occurred.
        /// </summary>
        /// <param name="exception">
        /// Non-zero to indicate that a read exception has occurred; zero to
        /// clear that indication.
        /// </param>
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

        /// <summary>
        /// This method enables or disables the host flag that indicates a write
        /// exception has occurred.
        /// </summary>
        /// <param name="exception">
        /// Non-zero to indicate that a write exception has occurred; zero to
        /// clear that indication.
        /// </param>
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
        /// <summary>
        /// This method determines whether the specified host creation flags are
        /// currently set.
        /// </summary>
        /// <param name="hasFlags">
        /// The host creation flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero if all of the specified flags must be set; zero if any of
        /// the specified flags being set is sufficient.
        /// </param>
        /// <returns>
        /// True if the specified host creation flags are set according to the
        /// <paramref name="all" /> parameter; otherwise, false.
        /// </returns>
        protected virtual bool HasCreateFlags(
            HostCreateFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(hostCreateFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables the specified host creation flags.
        /// </summary>
        /// <param name="flags">
        /// The host creation flags to be enabled or disabled.
        /// </param>
        /// <param name="enable">
        /// Non-zero to enable the specified flags; zero to disable them.
        /// </param>
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
        /// <summary>
        /// This method checks the specified return code while the host is
        /// exiting and, when it does not represent success, either breaks into
        /// an attached debugger or complains loudly.  It was originally intended
        /// to catch termination and resource disposal errors encountered during
        /// the disposal of the interpreter.
        /// </summary>
        /// <param name="returnCode">
        /// The return code to be checked.
        /// </param>
        /// <param name="result">
        /// The result associated with the return code, used as additional
        /// detail when reporting a bad return code.
        /// </param>
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
        /// <summary>
        /// This method populates the specified detail flags from the supplied
        /// interpreter, when available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter from which to obtain the detail flags.  This value
        /// may be null.
        /// </param>
        /// <param name="detailFlags">
        /// On input, the existing detail flags; upon return, the detail flags
        /// obtained from the specified interpreter.
        /// </param>
        protected virtual void PopulateDetailFlags(
            Interpreter interpreter,
            ref DetailFlags detailFlags
            )
        {
#if DEBUGGER || SHELL
            if (interpreter != null)
                detailFlags = interpreter.DetailFlags;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified detail flags indicate
        /// that there is no content to display.
        /// </summary>
        /// <param name="detailFlags">
        /// The detail flags to be examined.
        /// </param>
        /// <returns>
        /// True if the specified detail flags indicate empty content;
        /// otherwise, false.
        /// </returns>
        protected virtual bool HasEmptyContent(
            DetailFlags detailFlags
            )
        {
            return HostOps.HasEmptyContent(detailFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates the specified header flags into the
        /// corresponding detail flags.
        /// </summary>
        /// <param name="headerFlags">
        /// The header flags to be translated into detail flags.
        /// </param>
        /// <param name="detailFlags">
        /// On input, the existing detail flags; upon return, the detail flags
        /// updated to reflect the specified header flags.
        /// </param>
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
        /// <summary>
        /// This method returns the effective color to use, taking into account
        /// whether colors are currently disabled.
        /// </summary>
        /// <param name="color">
        /// The color requested by the caller.
        /// </param>
        /// <returns>
        /// The color with no foreground or background component when the
        /// <c>NoColor</c> property is true; otherwise, the color passed in by
        /// the caller.
        /// </returns>
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

        /// <summary>
        /// This method initializes all of the foreground and background colors
        /// used by this host to their default values, honoring the
        /// <see cref="GetColor" /> color disabling behavior.
        /// </summary>
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
        /// <summary>
        /// This method conditionally swaps the foreground and background text
        /// colors when the current output style represents a reversed text
        /// output style.
        /// </summary>
        /// <param name="foregroundColor">
        /// On input, the foreground text color; upon return, the possibly
        /// swapped foreground text color.
        /// </param>
        /// <param name="backgroundColor">
        /// On input, the background text color; upon return, the possibly
        /// swapped background text color.
        /// </param>
        protected virtual void MaybeSwapTextColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            MaybeSwapTextColors(OutputStyle,
                ref foregroundColor, ref backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally swaps the foreground and background text
        /// colors when the specified output style represents a reversed text
        /// output style.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style used to determine whether the text colors should be
        /// swapped.
        /// </param>
        /// <param name="foregroundColor">
        /// On input, the foreground text color; upon return, the possibly
        /// swapped foreground text color.
        /// </param>
        /// <param name="backgroundColor">
        /// On input, the background text color; upon return, the possibly
        /// swapped background text color.
        /// </param>
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

        /// <summary>
        /// This method conditionally swaps the foreground and background border
        /// colors when the current output style represents a reversed border
        /// output style.
        /// </summary>
        /// <param name="foregroundColor">
        /// On input, the foreground border color; upon return, the possibly
        /// swapped foreground border color.
        /// </param>
        /// <param name="backgroundColor">
        /// On input, the background border color; upon return, the possibly
        /// swapped background border color.
        /// </param>
        protected virtual void MaybeSwapBorderColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            MaybeSwapBorderColors(OutputStyle,
                ref foregroundColor, ref backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally swaps the foreground and background border
        /// colors when the specified output style represents a reversed border
        /// output style.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style used to determine whether the border colors should
        /// be swapped.
        /// </param>
        /// <param name="foregroundColor">
        /// On input, the foreground border color; upon return, the possibly
        /// swapped foreground border color.
        /// </param>
        /// <param name="backgroundColor">
        /// On input, the background border color; upon return, the possibly
        /// swapped background border color.
        /// </param>
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

        /// <summary>
        /// This method determines the foreground and background colors that
        /// should be used when displaying a result, based on its associated
        /// return code.
        /// </summary>
        /// <param name="code">
        /// The return code used to select the appropriate result colors.
        /// </param>
        /// <param name="result">
        /// The result associated with the return code.  This value may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// Upon return, receives the foreground color associated with the
        /// specified return code.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon return, receives the background color associated with the
        /// specified return code.
        /// </param>
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
        /// <summary>
        /// This method determines whether the specified interpreter currently
        /// has a pending complaint.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query for a pending complaint.  This value may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the interpreter has a non-empty pending complaint;
        /// otherwise, false.
        /// </returns>
        private bool HasComplaint(
            Interpreter interpreter
            )
        {
            string complaint = null;

            return HasComplaint(interpreter, ref complaint);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter currently
        /// has a pending complaint, returning its text upon success.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query for a pending complaint.  This value may be
        /// null.
        /// </param>
        /// <param name="complaint">
        /// Upon success, receives the text of the pending complaint; otherwise,
        /// this value is left unchanged.
        /// </param>
        /// <returns>
        /// True if the interpreter has a non-empty pending complaint;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method formats an announcement string for display, decorating
        /// the specified value with a prefix, suffix, timestamp, and the active
        /// interactive loop count based on the breakpoint type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to query the number of active
        /// interactive loops.  This value may be null.
        /// </param>
        /// <param name="breakpointType">
        /// The breakpoint type flags indicating whether the announcement is for
        /// the start or the end of an interactive loop.
        /// </param>
        /// <param name="value">
        /// The base value to be included in the formatted announcement.
        /// </param>
        /// <returns>
        /// The formatted announcement string, or the original value when no
        /// decoration could be applied.
        /// </returns>
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
                    DateTime now = TimeOps.GetNow();
                    string prefix;
                    string suffix;

                    if (FlagOps.HasFlags(breakpointType,
                            BreakpointType.BeforeInteractiveLoop, true))
                    {
                        prefix = String.Format(
                            "Interactive Loop{0}",
                            !String.IsNullOrEmpty(value) ?
                            " for" : String.Empty);

                        suffix = String.Format(" @ {0} ===>",
                            FormatOps.TraceDateTime(now, true));
                    }
                    else if (FlagOps.HasFlags(breakpointType,
                            BreakpointType.AfterInteractiveLoop, true))
                    {
                        prefix = String.Format(
                            "<=== Interactive Loop{0}",
                            !String.IsNullOrEmpty(value) ?
                            " for" : String.Empty);

                        suffix = String.Format(" @ {0}",
                            FormatOps.TraceDateTime(now, true));
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
        /// <summary>
        /// This method formats a return code and result into a string suitable
        /// for display, using the default result foreground and background
        /// colors.
        /// </summary>
        /// <param name="prefix">
        /// The optional prefix string to prepend to the formatted result.
        /// </param>
        /// <param name="code">
        /// The return code associated with the result.  This value is used
        /// only when the result is null or its own return code is
        /// <see cref="ReturnCode.Ok" />.
        /// </param>
        /// <param name="result">
        /// The result to be formatted.  This value may be null.
        /// </param>
        /// <param name="errorLine">
        /// The script line number where the error occurred, or zero if there
        /// was no error.
        /// </param>
        /// <param name="exceptions">
        /// Non-zero if exceptions should be considered when determining whether
        /// the return code represents success or failure.
        /// </param>
        /// <param name="display">
        /// Non-zero if the result is being formatted for interactive display
        /// purposes.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero if the formatted result may be truncated with an ellipsis
        /// when it is too long.
        /// </param>
        /// <param name="replaceNewLines">
        /// Non-zero if embedded new line characters in the formatted result
        /// should be replaced.
        /// </param>
        /// <param name="strict">
        /// Non-zero if a placeholder string should be returned when the
        /// formatted result would otherwise be null, empty, or unknown.
        /// </param>
        /// <returns>
        /// The formatted result string.
        /// </returns>
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

        /// <summary>
        /// This method formats a return code and result into a string suitable
        /// for display, also determining the foreground and background colors
        /// that should be used when displaying it.
        /// </summary>
        /// <param name="prefix">
        /// The optional prefix string to prepend to the formatted result.
        /// </param>
        /// <param name="code">
        /// The return code associated with the result.  This value is used
        /// only when the result is null or its own return code is
        /// <see cref="ReturnCode.Ok" />.
        /// </param>
        /// <param name="result">
        /// The result to be formatted.  This value may be null.
        /// </param>
        /// <param name="errorLine">
        /// The script line number where the error occurred, or zero if there
        /// was no error.
        /// </param>
        /// <param name="exceptions">
        /// Non-zero if exceptions should be considered when determining whether
        /// the return code represents success or failure.
        /// </param>
        /// <param name="display">
        /// Non-zero if the result is being formatted for interactive display
        /// purposes.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero if the formatted result may be truncated with an ellipsis
        /// when it is too long.
        /// </param>
        /// <param name="replaceNewLines">
        /// Non-zero if embedded new line characters in the formatted result
        /// should be replaced.
        /// </param>
        /// <param name="strict">
        /// Non-zero if a placeholder string should be returned when the
        /// formatted result would otherwise be null, empty, or unknown.
        /// </param>
        /// <param name="foregroundColor">
        /// Upon return, receives the foreground color that should be used when
        /// displaying the formatted result.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon return, receives the background color that should be used when
        /// displaying the formatted result.
        /// </param>
        /// <returns>
        /// The formatted result string.
        /// </returns>
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
        /// <summary>
        /// This method gets the list of console color properties declared by
        /// the specified type that match the supplied criteria.
        /// </summary>
        /// <param name="type">
        /// The type whose color properties should be enumerated.
        /// </param>
        /// <param name="mode">
        /// The matching mode to use when comparing property names against the
        /// pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match property names, or null to match all
        /// properties.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <param name="canRead">
        /// Non-zero to include only properties that can be read.
        /// </param>
        /// <param name="canWrite">
        /// Non-zero to include only properties that can be written.
        /// </param>
        /// <returns>
        /// The list of matching console color properties.
        /// </returns>
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
        /// <summary>
        /// This method gets the descriptive type name of the specified
        /// interactive host, formatted for display.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host whose type name should be returned.
        /// </param>
        /// <returns>
        /// The formatted type name, or a placeholder if the host is null or
        /// is a transparent proxy.
        /// </returns>
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

        /// <summary>
        /// This method gets the size of the specified host, formatted for
        /// display.
        /// </summary>
        /// <param name="sizeHost">
        /// The host whose size should be queried.
        /// </param>
        /// <param name="hostSizeType">
        /// The type of size to query.
        /// </param>
        /// <returns>
        /// The formatted width and height, or a placeholder if the size
        /// cannot be obtained.
        /// </returns>
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

        /// <summary>
        /// This method queries the value of the named property or
        /// parameterless method on the specified host and formats it for
        /// display, trapping any exceptions that may be thrown.
        /// </summary>
        /// <param name="host">
        /// The host to query.
        /// </param>
        /// <param name="name">
        /// The name of a property or method (with no arguments) to query on
        /// the host.
        /// </param>
        /// <returns>
        /// The formatted value, or a placeholder if the value is null or
        /// cannot be obtained.
        /// </returns>
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
        /// <summary>
        /// This method gets the descriptive type name used to display the
        /// specified call frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="frame">
        /// The call frame whose type name should be determined.
        /// </param>
        /// <param name="special">
        /// Non-zero to treat a variable call frame as a special case.
        /// </param>
        /// <returns>
        /// The type name associated with the call frame.
        /// </returns>
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

        /// <summary>
        /// This method gets the foreground console color used to display the
        /// specified call frame, based on its type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="frame">
        /// The call frame whose foreground color should be determined.
        /// </param>
        /// <param name="special">
        /// Non-zero to treat a variable call frame as a special case.
        /// </param>
        /// <returns>
        /// The foreground console color associated with the call frame.
        /// </returns>
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

        /// <summary>
        /// This method builds a list of name/value pairs describing the
        /// specified call frame, suitable for display as introspection
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="frame">
        /// The call frame whose details should be added to the list.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail included in the
        /// resulting list.
        /// </param>
        /// <param name="list">
        /// Upon success, this list is populated with the name/value pairs
        /// describing the call frame; it is created if necessary.
        /// </param>
        /// <returns>
        /// True if information was added to the list; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method looks up the opaque object handle named by the
        /// specified value within the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="value">
        /// The value containing the name of the object to look up.
        /// </param>
        /// <returns>
        /// The object associated with the name, or null if it cannot be
        /// found.
        /// </returns>
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

        /// <summary>
        /// This method gets the type of the value wrapped by the specified
        /// object.
        /// </summary>
        /// <param name="object">
        /// The object whose wrapped value type should be returned.
        /// </param>
        /// <returns>
        /// The type of the wrapped value, or null if it cannot be
        /// determined.
        /// </returns>
        private static Type GetWrappedObjectType(
            IObject @object
            )
        {
            ObjectWrapper objectWrapper = @object as ObjectWrapper;

            if (objectWrapper != null)
                return AppDomainOps.MaybeGetType(objectWrapper.@object);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a prefix should be applied to a
        /// name/value pair key.
        /// </summary>
        /// <param name="key">
        /// The name/value pair key being considered.
        /// </param>
        /// <param name="prefix">
        /// The candidate prefix to apply to the key.
        /// </param>
        /// <returns>
        /// True if the prefix should be applied to the key; otherwise, false.
        /// </returns>
        private static bool NeedPairKeyPrefix(
            string key,
            string prefix
            )
        {
            return !String.IsNullOrEmpty(prefix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines a name/value pair key with an optional
        /// prefix.
        /// </summary>
        /// <param name="key">
        /// The original name/value pair key.
        /// </param>
        /// <param name="prefix">
        /// The optional prefix to apply to the key.
        /// </param>
        /// <returns>
        /// The key with the prefix applied, or the original key if no prefix
        /// is needed.
        /// </returns>
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

        /// <summary>
        /// This method builds a list of name/value pairs describing the
        /// specified object, suitable for display as introspection output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="object">
        /// The object whose details should be added to the list.
        /// </param>
        /// <param name="prefix">
        /// An optional prefix to prepend to each name/value pair key.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail included in the
        /// resulting list.
        /// </param>
        /// <param name="list">
        /// Upon success, this list is populated with the name/value pairs
        /// describing the object; it is created if necessary.
        /// </param>
        /// <returns>
        /// True if information was added to the list; otherwise, false.
        /// </returns>
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
                int hashCode;

                try
                {
                    StringPairList localList = new StringPairList();

                    if (empty || @object.Disposed)
                        localList.Add(GetPairKeyWithPrefix(
                            "ObjectDisposed", prefix), String.Format(
                            "{0}", FormatOps.MaybeNull(@object.Disposed)));

                    if (empty || @object.Disposing)
                        localList.Add(GetPairKeyWithPrefix(
                            "ObjectDisposing", prefix), String.Format(
                            "{0}", FormatOps.MaybeNull(@object.Disposing)));

                    hashCode = RuntimeOps.GetHashCode((object)@object);

                    if (empty || (hashCode != 0))
                    {
                        localList.Add(GetPairKeyWithPrefix(
                            "ObjectRuntimeHashCode", prefix),
                            hashCode.ToString());
                    }

                    hashCode = @object.GetHashCode();

                    if (empty || (hashCode != 0))
                    {
                        localList.Add(GetPairKeyWithPrefix(
                            "ObjectHashCode", prefix),
                            hashCode.ToString());
                    }

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
                        // HACK: Attempt to determine if the (target) object
                        //       has been disposed.  If so, indicate that in
                        //       the diagnostic output -AND- try to avoid an
                        //       ObjectDisposedException.
                        //
                        bool? disposed = ObjectOps.IsDisposed(
                            interpreter, objectValue, true, false, false);

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
                            localList.Add(GetPairKeyWithPrefix(
                                "ValueDisposed", prefix), String.Format(
                                "{0}", FormatOps.MaybeNull(disposed)));

                            hashCode = RuntimeOps.GetHashCode(objectValue);

                            if (empty || (hashCode != 0))
                            {
                                localList.Add(GetPairKeyWithPrefix(
                                    "ValueRuntimeHashCode", prefix),
                                    hashCode.ToString());
                            }

                            if ((disposed != null) && ((bool)disposed == false))
                            {
                                hashCode = objectValue.GetHashCode();

                                if (empty || (hashCode != 0))
                                {
                                    localList.Add(GetPairKeyWithPrefix(
                                        "ValueHashCode", prefix),
                                        hashCode.ToString());
                                }

                                string stringValue = objectValue.ToString();

                                if (empty || (stringValue != null))
                                {
                                    localList.Add(GetPairKeyWithPrefix(
                                        "ValueToString", prefix),
                                        (stringValue != null) ? stringValue :
                                        FormatOps.DisplayNull);
                                }
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// specified variable and, when requested via the detail flags, the
        /// chain of variables it is linked to.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> that owns the variable, or null.
        /// </param>
        /// <param name="variable">
        /// The <see cref="IVariable" /> to be described, or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included, including whether linked variables are followed.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the variable
        /// information.
        /// </param>
        /// <returns>
        /// True if the variable information was built successfully; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// specified variable, including its flags, value or array elements,
        /// associated call frame, traces, and any array searches.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> that owns the variable, or null.
        /// </param>
        /// <param name="variable">
        /// The <see cref="IVariable" /> to be described, or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the variable
        /// information.
        /// </param>
        /// <returns>
        /// True if the variable information was built successfully; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// arguments, breakpoint, and result associated with a script
        /// evaluation breakpoint.
        /// </summary>
        /// <param name="code">
        /// The <see cref="ReturnCode" /> associated with the result.
        /// </param>
        /// <param name="breakpointType">
        /// The <see cref="BreakpointType" /> that triggered the breakpoint.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the breakpoint, or null.
        /// </param>
        /// <param name="arguments">
        /// The <see cref="ArgumentList" /> being processed, or null.
        /// </param>
        /// <param name="result">
        /// The <see cref="Result" /> associated with the breakpoint, or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the argument
        /// information.
        /// </param>
        /// <returns>
        /// True if the argument information was built successfully; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// frames contained in the specified call stack, optionally including
        /// related frames for each entry.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> that owns the call stack, or null.
        /// </param>
        /// <param name="callStack">
        /// The <see cref="CallStack" /> whose frames are to be described, or
        /// null.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to traverse, or zero for no
        /// limit.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the call stack
        /// information.
        /// </param>
        /// <returns>
        /// True if the call stack information was built successfully; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// debugger associated with the specified interpreter, if any.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> whose debugger information is to be
        /// queried, or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the debugger
        /// information.
        /// </param>
        /// <returns>
        /// True if the debugger information was built successfully; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// specified sets of flags, omitting those that are set to their "None"
        /// value unless empty content is being forced.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> whose host flag information is to be
        /// included, or null.
        /// </param>
        /// <param name="engineFlags">
        /// The <see cref="EngineFlags" /> to include.
        /// </param>
        /// <param name="substitutionFlags">
        /// The <see cref="SubstitutionFlags" /> to include.
        /// </param>
        /// <param name="eventFlags">
        /// The <see cref="EventFlags" /> to include.
        /// </param>
        /// <param name="expressionFlags">
        /// The <see cref="ExpressionFlags" /> to include.
        /// </param>
        /// <param name="headerFlags">
        /// The <see cref="HeaderFlags" /> to include.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the flag information.
        /// </param>
        /// <returns>
        /// True if the flag information was built successfully; otherwise,
        /// false.
        /// </returns>
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
                list.Add(FormatOps.DisplayNothing);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Introspection Methods
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// host(s) associated with the specified interpreter, including any
        /// type-specific details (e.g. dimensions, formatting, colors, and
        /// state).
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> whose host information is to be
        /// queried, or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the host information.
        /// </param>
        /// <returns>
        /// True if the host information was built successfully; otherwise,
        /// false.
        /// </returns>
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

                        host = interpreter.InternalHost;
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
                    list.Add("UseForce", GetHostInfo(host, "UseForce"));
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
        /// <summary>
        /// This method determines whether the host should treat the current
        /// runtime as the Mono runtime.
        /// </summary>
        /// <returns>
        /// True if the current runtime should be treated as Mono; otherwise,
        /// false.
        /// </returns>
        protected virtual bool ShouldTreatAsMono()
        {
            return CommonOps.Runtime.IsMono();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host should treat the current
        /// runtime as the .NET Core runtime.
        /// </summary>
        /// <returns>
        /// True if the current runtime should be treated as .NET Core;
        /// otherwise, false.
        /// </returns>
        protected virtual bool ShouldTreatAsDotNetCore()
        {
            return CommonOps.Runtime.IsDotNetCore();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Determines whether the host environment can
        //       render Unicode box-drawing characters (U+2500
        //       range) as single-width glyphs.  The default
        //       implementation checks whether the output
        //       encoding is a Unicode encoding (UTF-8, etc.)
        //       and the runtime is modern enough to handle
        //       it.  Subclasses may override to add platform
        //       or terminal-specific checks.
        //
        /// <summary>
        /// This method determines whether the host environment can render
        /// Unicode box-drawing characters (in the U+2500 range) as
        /// single-width glyphs.
        /// </summary>
        /// <param name="encoding">
        /// The output <see cref="Encoding" /> to consider, or null if it is
        /// unknown.
        /// </param>
        /// <returns>
        /// True if Unicode box-drawing characters can be used; otherwise,
        /// false.
        /// </returns>
        protected virtual bool CanUseUnicodeBoxCharacters(
            Encoding encoding /* in */
            )
        {
#if UNIX
            //
            // NOTE: On .NET Core on Unix (Linux, macOS), modern
            //       terminal emulators universally support Unicode
            //       box-drawing characters via UTF-8.
            //
            if (ShouldTreatAsDotNetCore() &&
                !PlatformOps.IsWindowsOperatingSystem())
            {
                //
                // NOTE: If the encoding is known to be Unicode,
                //       use Unicode box characters.
                //
                if (StringOps.IsUnicodeEncoding(encoding))
                    return true;

                //
                // NOTE: If the encoding could not be determined
                //       (null), check the locale environment as
                //       a fallback.  On most modern Linux and
                //       macOS systems, the locale is UTF-8.
                //
                if ((encoding == null) &&
                    StringOps.IsUnicodeEncoding())
                {
                    return true;
                }
            }
#endif

            //
            // NOTE: On Windows or when the encoding is
            //       clearly not Unicode, do not attempt
            //       Unicode box characters via this path
            //       (the Windows path uses IsSingleByte
            //       with code page 437 instead).
            //
            if (StringOps.IsUnicodeEncoding(encoding))
                return true;

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Interpreter Introspection Methods
        /// <summary>
        /// This method builds a list of name/value pairs that describes
        /// auxiliary information about the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> whose auxiliary information is to be
        /// queried, or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the auxiliary
        /// interpreter information.
        /// </param>
        /// <returns>
        /// True if the auxiliary interpreter information was built successfully;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> whose information is to be queried,
        /// or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the interpreter
        /// information.
        /// </param>
        /// <returns>
        /// True if the interpreter information was built successfully;
        /// otherwise, false.
        /// </returns>
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
                    list.Add(FormatOps.DisplayNothing);
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// state of the script engine, including application domain and thread
        /// information, callbacks, and various subsystem details.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> whose engine information is to be
        /// queried, or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the engine information.
        /// </param>
        /// <returns>
        /// True if the engine information was built successfully; otherwise,
        /// false.
        /// </returns>
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
                AppDomainOps.GetCurrentId().ToString()); /* EXEMPT */

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

            list.Add("GlobalStateLockThread",
                GlobalState.GetCurrentLockThreadId().ToString()); /* EXEMPT */

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

            if (FlagOps.HasFlags(detailFlags, DetailFlags.HostOps, true))
                HostOps.AddInfo(list, detailFlags);

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

            if (FlagOps.HasFlags(detailFlags, DetailFlags.StringBuilderCacheInfo, true))
                StringBuilderCache.AddInfo(list, detailFlags);

            if (FlagOps.HasFlags(detailFlags, DetailFlags.StringBuilderFactoryInfo, true))
                StringBuilderFactory.AddInfo(list, detailFlags);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Entity Introspection Methods
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// entities (e.g. commands, procedures, variables, etc.) contained
        /// within the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> whose entity information is to be
        /// queried, or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the entity information.
        /// </param>
        /// <returns>
        /// True if the entity information was built successfully; otherwise,
        /// false.
        /// </returns>
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
                    list.Add(FormatOps.DisplayNothing);
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// current native stack usage for the calling thread.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context (this parameter is not used).
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the stack information.
        /// </param>
        /// <returns>
        /// True if the stack information was built successfully; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// control settings associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> whose control settings are to be
        /// queried, or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the control information.
        /// </param>
        /// <returns>
        /// True if the control information was built successfully; otherwise,
        /// false.
        /// </returns>
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
                    list.Add(FormatOps.DisplayNothing);
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// testing state associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> whose testing state is to be queried,
        /// or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the test information.
        /// </param>
        /// <returns>
        /// True if the test information was built successfully; otherwise,
        /// false.
        /// </returns>
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
                    list.Add(FormatOps.DisplayNothing);
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// specified token.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context (this parameter is not used).
        /// </param>
        /// <param name="token">
        /// The <see cref="IToken" /> to be described, or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the token information.
        /// </param>
        /// <returns>
        /// True if the token information was built successfully; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// specified trace information, or the cached trace information
        /// associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> whose cached trace information is to
        /// be queried, or null.
        /// </param>
        /// <param name="traceInfo">
        /// The <see cref="ITraceInfo" /> to be described, or null.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the trace information.
        /// </param>
        /// <returns>
        /// True if the trace information was built successfully; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// command history associated with the specified interpreter,
        /// optionally filtered.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> whose command history is to be
        /// queried, or null.
        /// </param>
        /// <param name="historyFilter">
        /// The <see cref="IHistoryFilter" /> used to limit which history items
        /// are included, or null for no filtering.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the history information.
        /// </param>
        /// <returns>
        /// True if the history information was built successfully; otherwise,
        /// false.
        /// </returns>
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
                    list.Add(FormatOps.DisplayNothing);
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
        /// <summary>
        /// This method builds a list of name/value pairs that describes the
        /// specified script result, including its return code, error line, and
        /// flags.
        /// </summary>
        /// <param name="name">
        /// The name to use for the result information section; if null, the
        /// value of the <see cref="ResultInfoBoxName" /> property is used.
        /// </param>
        /// <param name="code">
        /// The <see cref="ReturnCode" /> associated with the result.
        /// </param>
        /// <param name="result">
        /// The <see cref="Result" /> to be described.
        /// </param>
        /// <param name="errorLine">
        /// The line number where the error occurred, or zero if there was no
        /// error.
        /// </param>
        /// <param name="detailFlags">
        /// The <see cref="DetailFlags" /> that control how much detail is
        /// included.
        /// </param>
        /// <param name="list">
        /// A reference to the list of name/value pairs to populate.  If null, a
        /// new list is created.  Upon return, contains the result information.
        /// </param>
        /// <returns>
        /// True if the result information was built successfully; otherwise,
        /// false.
        /// </returns>
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
                list.Add(FormatOps.DisplayNothing);

            return true;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Test Methods
        /// <summary>
        /// This method determines whether the host is operating in test mode,
        /// based on the host flags.
        /// </summary>
        /// <returns>
        /// True if the host is operating in test mode; otherwise, false.
        /// </returns>
        protected virtual bool InTestMode()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.Test, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host test flags include the
        /// specified flags.
        /// </summary>
        /// <param name="hasFlags">
        /// The <see cref="HostTestFlags" /> to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero if all of the specified flags must be present; otherwise,
        /// any one of the specified flags being present is sufficient.
        /// </param>
        /// <returns>
        /// True if the host test flags include the specified flags (according
        /// to the <paramref name="all" /> parameter); otherwise, false.
        /// </returns>
        protected virtual bool HasTestFlags(HostTestFlags hasFlags, bool all)
        {
            return FlagOps.HasFlags(
                GetTestFlags(), hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Stream Helper Methods
        /// <summary>
        /// This method determines whether the underlying writer should be
        /// automatically flushed after each write operation, based on the host
        /// flags.
        /// </summary>
        /// <returns>
        /// True if the underlying writer should be automatically flushed after
        /// each write operation; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines how many passes should be used by the core
        /// write methods to write a value, based on whether a new line is being
        /// written and the colors involved.  When a colored value is being
        /// written with a new line and the host should not color the new line,
        /// two passes are used (one for the colored value and one for the new
        /// line written without color); otherwise, a single pass is used.
        /// </summary>
        /// <param name="newLine">
        /// Non-zero if a terminating new line is being written.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to be used when writing the value.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to be used when writing the value.
        /// </param>
        /// <returns>
        /// An array of pass identifiers, either
        /// <see cref="OnePassForWriteCore" /> or
        /// <see cref="TwoPassesForWriteCore" />.  This method never returns
        /// null.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the value should be written during
        /// the specified multi-pass write pass.  The pass values correspond to
        /// the entries in the <see cref="OnePassForWriteCore" /> and
        /// <see cref="TwoPassesForWriteCore" /> arrays.
        /// </summary>
        /// <param name="pass">
        /// The zero-based pass identifier for the current write pass.
        /// </param>
        /// <returns>
        /// True if the value should be written during the specified pass;
        /// otherwise, false.
        /// </returns>
        protected virtual bool ShouldWriteForPass(
            int pass
            )
        {
            return ((pass == 0) || (pass == 1));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the terminating new line should be
        /// written during the specified multi-pass write pass.  The pass values
        /// correspond to the entries in the <see cref="OnePassForWriteCore" />
        /// and <see cref="TwoPassesForWriteCore" /> arrays.
        /// </summary>
        /// <param name="pass">
        /// The zero-based pass identifier for the current write pass.
        /// </param>
        /// <returns>
        /// True if the terminating new line should be written during the
        /// specified pass; otherwise, false.
        /// </returns>
        protected virtual bool ShouldWriteLineForPass(
            int pass
            )
        {
            return ((pass == 0) || (pass == 2));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a flush should be performed during
        /// the specified multi-pass write pass.  The pass values correspond to
        /// the entries in the <see cref="OnePassForWriteCore" /> and
        /// <see cref="TwoPassesForWriteCore" /> arrays.
        /// </summary>
        /// <param name="pass">
        /// The zero-based pass identifier for the current write pass.
        /// </param>
        /// <returns>
        /// True if a flush should be performed during the specified pass;
        /// otherwise, false.
        /// </returns>
        protected virtual bool ShouldFlushForPass(
            int pass
            )
        {
            return ((pass == 0) || (pass == 2));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host should be automatically
        /// flushed after each write operation, based on the host flags.
        /// </summary>
        /// <returns>
        /// True if the host should be automatically flushed after each write
        /// operation; otherwise, false.
        /// </returns>
        protected virtual bool DoesAutoFlushHost()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.AutoFlushHost, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host should restore the colors
        /// after each write operation, based on the host flags.
        /// </summary>
        /// <returns>
        /// True if the colors should be restored after each write operation;
        /// otherwise, false.
        /// </returns>
        protected virtual bool DoesRestoreColorAfterWrite()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.RestoreColorAfterWrite, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores (or sets) the foreground and background colors
        /// after a write operation has completed.  When the host derives from
        /// the built-in console host, the reset and set are performed inline
        /// using methods that do not acquire the console synchronization lock,
        /// in order to avoid a potential deadlock with the static lock already
        /// held by the caller.
        /// </summary>
        /// <param name="savedForegroundColor">
        /// The previously saved foreground color to be restored.
        /// </param>
        /// <param name="savedBackgroundColor">
        /// The previously saved background color to be restored.
        /// </param>
        /// <returns>
        /// True if the colors were restored (or set) successfully; otherwise,
        /// false.
        /// </returns>
        protected virtual bool RestoreOrSetColors(
            ConsoleColor savedForegroundColor,
            ConsoleColor savedBackgroundColor
            )
        {
#if CONSOLE
            if (DoesRestoreColorAfterWrite())
            {
                //
                // HACK: This is only supported for hosts that
                //       derive from the built-in console host.
                //
                // BUGFIX: Do NOT call RestoreColors() here
                //         because it acquires Console.syncRoot,
                //         which is a different lock from the
                //         staticSyncRoot already held by the
                //         caller (WriteCore).  Acquiring two
                //         different locks risks deadlock.
                //         Instead, perform the equivalent
                //         reset+set inline using methods that
                //         do not acquire Console.syncRoot.
                //
                _Hosts.Console consoleHost =
                    this as _Hosts.Console;

                if (consoleHost != null)
                {
                    //
                    // BUGFIX: On non-Windows terminals,
                    //         setting a background color via
                    //         ANSI escape codes causes the
                    //         color to extend from the write
                    //         position to the right margin.
                    //         Always resetting attributes
                    //         (ESC[0m via ResetColor) before
                    //         restoring the saved colors
                    //         prevents this line-fill bleed.
                    //         On Windows, ResetColors is only
                    //         needed when a saved color is
                    //         None.
                    //
                    if (consoleHost.DoesResetColorForRestore() ||
                        ((savedForegroundColor ==
                            _ConsoleColor.None) ||
                        (savedBackgroundColor ==
                            _ConsoleColor.None)))
                    {
                        if (!ResetColors())
                            return false;
                    }

                    return SetColors(true, true,
                        savedForegroundColor,
                        savedBackgroundColor);
                }
            }
#endif

            return SetColors(
                true, true, savedForegroundColor, savedBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method normalizes a character value prior to it being written
        /// to the host.  For a single character, no normalization is performed;
        /// the <paramref name="done" /> flag is used to track and enforce that
        /// normalization is attempted only once.
        /// </summary>
        /// <param name="value">
        /// The character value to be normalized.  Upon success, this may be
        /// modified in place to contain the normalized value.
        /// </param>
        /// <param name="done">
        /// A flag indicating whether normalization has already been performed.
        /// Upon return, this will be non-zero to indicate that normalization is
        /// complete.
        /// </param>
        /// <returns>
        /// True if normalization succeeded; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method normalizes a string value prior to it being written to
        /// the host, replacing any DOS (carriage-return / line-feed) new lines
        /// with the platform new line.  Normalization is only performed once;
        /// the <paramref name="done" /> flag is used to track and enforce this.
        /// </summary>
        /// <param name="value">
        /// The string value to be normalized.  Upon success, this may be
        /// modified in place to contain the normalized value.
        /// </param>
        /// <param name="done">
        /// A flag indicating whether normalization has already been performed.
        /// Upon return, this will be non-zero to indicate that normalization is
        /// complete.
        /// </param>
        /// <returns>
        /// True if normalization succeeded; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a character value to the host a specified number
        /// of times via the supplied callbacks, optionally followed by a new
        /// line, using the specified colors.  The write is performed in one or
        /// more passes, as determined by <c>GetPassesForWriteCore</c>, so that
        /// the colored text and the terminating new line may be written
        /// separately.
        /// </summary>
        /// <param name="writeCharCallback">
        /// The callback used to write the character value.
        /// </param>
        /// <param name="writeLineCallback">
        /// The callback used to write the terminating new line.  This is only
        /// required when <paramref name="newLine" /> is non-zero.
        /// </param>
        /// <param name="value">
        /// The character value to be written to the host.
        /// </param>
        /// <param name="count">
        /// The number of times the character value should be written.
        /// </param>
        /// <param name="newLine">
        /// Non-zero if a terminating new line should be written after the
        /// value.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the value.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the value.
        /// </param>
        /// <returns>
        /// True if the value was written successfully; otherwise, false.
        /// </returns>
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

                            //
                            // TODO: Add flag to control if locking is used...  peer of shouldColorForPass.
                            //       Copy this block to the other three (?) spots it is needed.
                            //
                            bool locked = false;

                            try
                            {
                                if (shouldColorForPass)
                                {
                                    PrivateStaticTryLockWithWait(ref locked);

                                    if (!locked)
                                    {
                                        TraceOps.LockTrace(
                                            "WriteCore",
                                            typeof(Default).Name, false,
                                            TracePriority.LockError,
                                            MaybeWhoHasStaticLock());

                                        return false;
                                    }

                                    if (!GetColors(ref savedForegroundColor, ref savedBackgroundColor))
                                        return false;

                                    if (shouldAdjustForPass && !AdjustColors(ref foregroundColor, ref backgroundColor))
                                        return false;

                                    if (!SetColors(true, true, foregroundColor, backgroundColor))
                                        return false;
                                }

                                try
                                {
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

                                    if ((wrote == 0) && shouldFlushForPass)
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
                            finally
                            {
                                PrivateStaticExitLock(ref locked);
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

        /// <summary>
        /// This method writes a string value to the host via the supplied
        /// callbacks, optionally followed by a new line, using the specified
        /// colors.  The write is performed in one or more passes, as
        /// determined by <c>GetPassesForWriteCore</c>, so that the colored
        /// text and the terminating new line may be written separately.
        /// </summary>
        /// <param name="writeStringCallback">
        /// The callback used to write the string value.
        /// </param>
        /// <param name="writeLineCallback">
        /// The callback used to write the terminating new line.  This is only
        /// required when <paramref name="newLine" /> is non-zero.
        /// </param>
        /// <param name="value">
        /// The string value to be written to the host.
        /// </param>
        /// <param name="newLine">
        /// Non-zero if a terminating new line should be written after the
        /// value.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the value.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the value.
        /// </param>
        /// <returns>
        /// True if the value was written successfully; otherwise, false.
        /// </returns>
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

                            bool locked = false;

                            try
                            {
                                if (shouldColorForPass)
                                {
                                    PrivateStaticTryLockWithWait(ref locked);

                                    if (!locked)
                                    {
                                        TraceOps.LockTrace(
                                            "WriteCore",
                                            typeof(Default).Name, false,
                                            TracePriority.LockError,
                                            MaybeWhoHasStaticLock());

                                        return false;
                                    }

                                    if (!GetColors(ref savedForegroundColor, ref savedBackgroundColor))
                                        return false;

                                    if (shouldAdjustForPass && !AdjustColors(ref foregroundColor, ref backgroundColor))
                                        return false;

                                    if (!SetColors(true, true, foregroundColor, backgroundColor))
                                        return false;
                                }

                                try
                                {
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

                                    if ((wrote == 0) && shouldFlushForPass)
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
                            finally
                            {
                                PrivateStaticExitLock(ref locked);
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

        /// <summary>
        /// This method writes a character value to the host a specified number
        /// of times, optionally followed by a new line, using the specified
        /// write type and colors.  The write is performed in one or more
        /// passes, as determined by <c>GetPassesForWriteCore</c>, so that the
        /// colored text and the terminating new line may be written
        /// separately.  The <paramref name="hostWriteType" /> selects which
        /// output (normal, debug, error, or flush only) receives the value.
        /// </summary>
        /// <param name="hostWriteType">
        /// The type of write operation to perform, which selects the target
        /// output (e.g. normal, debug, or error) or a flush-only operation.
        /// </param>
        /// <param name="value">
        /// The character value to be written to the host.
        /// </param>
        /// <param name="count">
        /// The number of times the character value should be written.
        /// </param>
        /// <param name="newLine">
        /// Non-zero if a terminating new line should be written after the
        /// value.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the value.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the value.
        /// </param>
        /// <returns>
        /// True if the value was written successfully; otherwise, false.
        /// </returns>
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

                            bool locked = false;

                            try
                            {
                                if (shouldColorForPass)
                                {
                                    PrivateStaticTryLockWithWait(ref locked);

                                    if (!locked)
                                    {
                                        TraceOps.LockTrace(
                                            "WriteCore",
                                            typeof(Default).Name, false,
                                            TracePriority.LockError,
                                            MaybeWhoHasStaticLock());

                                        return false;
                                    }

                                    if (!GetColors(ref savedForegroundColor, ref savedBackgroundColor))
                                        return false;

                                    if (shouldAdjustForPass && !AdjustColors(ref foregroundColor, ref backgroundColor))
                                        return false;

                                    if (!SetColors(true, true, foregroundColor, backgroundColor))
                                        return false;
                                }

                                try
                                {
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

                                                if ((wrote == 0) && shouldFlushForPass)
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

                                                if ((wrote == 0) && shouldFlushForPass)
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

                                                if ((wrote == 0) && shouldFlushForPass)
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
                            finally
                            {
                                PrivateStaticExitLock(ref locked);
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

        /// <summary>
        /// This method writes a string value to the host, optionally followed
        /// by a new line, using the specified write type and colors.  The
        /// write is performed in one or more passes, as determined by
        /// <c>GetPassesForWriteCore</c>, so that the colored text and the
        /// terminating new line may be written separately.  The
        /// <paramref name="hostWriteType" /> selects which output (normal,
        /// debug, error, or flush only) receives the value.
        /// </summary>
        /// <param name="hostWriteType">
        /// The type of write operation to perform, which selects the target
        /// output (e.g. normal, debug, or error) or a flush-only operation.
        /// </param>
        /// <param name="value">
        /// The string value to be written to the host.
        /// </param>
        /// <param name="newLine">
        /// Non-zero if a terminating new line should be written after the
        /// value.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the value.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the value.
        /// </param>
        /// <returns>
        /// True if the value was written successfully; otherwise, false.
        /// </returns>
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

                        bool locked = false;

                        try
                        {
                            if (shouldColorForPass)
                            {
                                PrivateStaticTryLockWithWait(ref locked);

                                if (!locked)
                                {
                                    TraceOps.LockTrace(
                                        "WriteCore",
                                        typeof(Default).Name, false,
                                        TracePriority.LockError,
                                        MaybeWhoHasStaticLock());

                                    return false;
                                }

                                if (!GetColors(ref savedForegroundColor, ref savedBackgroundColor))
                                    return false;

                                if (shouldAdjustForPass && !AdjustColors(ref foregroundColor, ref backgroundColor))
                                    return false;

                                if (!SetColors(true, true, foregroundColor, backgroundColor))
                                    return false;
                            }

                            try
                            {
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

                                                if ((wrote == 0) && shouldFlushForPass)
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

                                                if ((wrote == 0) && shouldFlushForPass)
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

                                                if ((wrote == 0) && shouldFlushForPass)
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
                        finally
                        {
                            PrivateStaticExitLock(ref locked);
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
        /// <summary>
        /// This method writes a name and an associated list of name/value
        /// pairs to the host, using the specified output style and colors.
        /// Depending on the output style, the data may be rendered as a box,
        /// as formatted output, or not at all.
        /// </summary>
        /// <param name="outputStyle">
        /// The output style that determines how the name and list are
        /// rendered (e.g. boxed, formatted, or none).
        /// </param>
        /// <param name="name">
        /// The name (e.g. a heading or title) associated with the list of
        /// name/value pairs.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to be written to the host.
        /// </param>
        /// <param name="newLine">
        /// Non-zero if a terminating new line should be written after the
        /// output.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the output.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the output.
        /// </param>
        /// <returns>
        /// True if the output was written (or intentionally suppressed)
        /// successfully; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method gets the white space flags to use when
        /// rendering boxes, taking into account the current host
        /// environment and the specified output encoding.
        /// </summary>
        /// <param name="encoding">
        /// The output encoding used to determine which white space
        /// characters can be represented as single bytes.
        /// </param>
        /// <returns>
        /// The white space flags to use when rendering boxes.
        /// </returns>
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
            // BUGFIX: Also avoid using the Unicode or extended
            //         characters at all, since it cannot work
            //         consistently.
            //
            if (IsWindowsTerminal())
            {
                whiteSpaceFlags &= ~WhiteSpaceFlags.Extended;
                whiteSpaceFlags &= ~WhiteSpaceFlags.Unicode;
                whiteSpaceFlags |= WhiteSpaceFlags.NoArrows;
            }

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
            // BUGFIX: Also, force cleaning of pre-existing
            //         "replacement" characters for whitespace,
            //         which may be Unicode, etc.
            //
            if (!StringOps.IsSingleByte(encoding, new string(
                    Characters.WhiteSpace_Extended), true))
            {
                whiteSpaceFlags &= ~WhiteSpaceFlags.Extended;
                whiteSpaceFlags |= WhiteSpaceFlags.Clean;
            }

            if (!StringOps.IsSingleByte(encoding, new string(
                    Characters.WhiteSpace_Unicode), true))
            {
                whiteSpaceFlags &= ~WhiteSpaceFlags.Unicode;
                whiteSpaceFlags |= WhiteSpaceFlags.Clean;
            }

            return whiteSpaceFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the collection of box character
        /// sets used by this host.
        /// </summary>
        protected virtual void InitializeBoxCharacterSets()
        {
            BoxCharacterSets = new StringList(Characters.BoxCharacterSets);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the box character set at the specified
        /// index, if it is available.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the box character set to get.
        /// </param>
        /// <returns>
        /// The box character set at the specified index, or null if it
        /// is not available.
        /// </returns>
        protected virtual string MaybeGetBoxCharacterSet(
            int index
            ) /* MAY RETURN NULL */
        {
            StringList boxCharacterSets = BoxCharacterSets;

            if (boxCharacterSets == null)
                return null;

            if ((index < 0) || (index >= boxCharacterSets.Count))
                return null;

            return boxCharacterSets[index];
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the box character set currently selected
        /// for this host.
        /// </summary>
        /// <returns>
        /// The currently selected box character set, or null if it is
        /// not available.
        /// </returns>
        protected virtual string GetBoxCharacterSet() /* MAY RETURN NULL */
        {
            return MaybeGetBoxCharacterSet(BoxCharacterSet);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the fallback box character set, which is
        /// composed entirely of space characters.
        /// </summary>
        /// <returns>
        /// The fallback box character set, or null if it is not
        /// available.
        /// </returns>
        protected virtual string GetFallbackBoxCharacterSet() /* MAY RETURN NULL */
        {
            return StringOps.StrRepeat(
                (int)BoxCharacter.Count, Characters.Space);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the most appropriate box character set
        /// based on the current output encoding and host environment.
        /// </summary>
        protected virtual void SelectBoxCharacterSet()
        {
            Encoding encoding = null;

            try
            {
                //
                // HACK: Prevent a ScriptException with the error message
                //       "system console output channel is not available"
                //       from escaping this method when console I/O is not
                //       available.
                //
                encoding = OutputEncoding; /* throw */
            }
            catch
            {
                // do nothing.
            }

            SelectBoxCharacterSet(encoding);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the most appropriate box character set
        /// for the specified output encoding and the current host
        /// environment.
        /// </summary>
        /// <param name="encoding">
        /// The output encoding used to determine which box character
        /// sets can be represented.
        /// </param>
        protected virtual void SelectBoxCharacterSet(
            Encoding encoding
            )
        {
            StringList boxCharacterSets = BoxCharacterSets;

            if (boxCharacterSets == null)
            {
                BoxCharacterSet = 0; /* SAFE */
                return;
            }

            int count = boxCharacterSets.Count;

            if (count <= 0)
            {
                BoxCharacterSet = 0; /* SAFE */
                return;
            }

#if WINDOWS
            //
            // NOTE: On Windows, check if the encoding can
            //       represent the highest-fidelity character
            //       set as single bytes (e.g. code page 437).
            //
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                int index = count - 1;

                if (StringOps.IsSingleByte(encoding,
                        boxCharacterSets[index], true))
                {
                    BoxCharacterSet = index;
                    return;
                }
            }
#endif

            //
            // NOTE: Check if the host environment supports
            //       Unicode box-drawing characters for display.
            //       If so, use the single-line Unicode set
            //       (index 7), which is universally supported
            //       in modern terminals.
            //
            if (CanUseUnicodeBoxCharacters(encoding))
            {
                //
                // NOTE: Use single-line Unicode box characters
                //       These have the best cross-terminal
                //       compatibility.
                //
                int unicodeIndex = 7;

                if (unicodeIndex < count)
                {
                    BoxCharacterSet = unicodeIndex;
                    return;
                }
            }

            BoxCharacterSet = 0; /* SAFE */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a new line as part of rendering a box,
        /// dispatching to the appropriate write method based on the
        /// specified host write type.
        /// </summary>
        /// <param name="hostWriteType">
        /// The type of write operation to perform.
        /// </param>
        /// <returns>
        /// True if the new line was written successfully; otherwise,
        /// false.
        /// </returns>
        protected virtual bool WriteLineForBox(
            HostWriteType hostWriteType
            )
        {
#if CONSOLE
            //
            // BUGFIX: On non-Windows terminals, background
            //         colors extend from the write position to
            //         the right margin via ANSI escape codes.
            //         Explicitly reset all attributes before
            //         writing the box newline to prevent the
            //         box background color from filling the
            //         remainder of the line.
            //
            if (DoesResetColorForRestore())
                /* IGNORED */
                ResetColors();
#endif

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

        /// <summary>
        /// This method writes a single character value as part of
        /// rendering a box, dispatching to the appropriate write method
        /// based on the specified host write type.
        /// </summary>
        /// <param name="hostWriteType">
        /// The type of write operation to perform.
        /// </param>
        /// <param name="value">
        /// The character value to write.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the value.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the value.
        /// </param>
        /// <returns>
        /// True if the value was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a character value as part of rendering a
        /// box, dispatching to the appropriate write method based on
        /// the specified host write type.
        /// </summary>
        /// <param name="hostWriteType">
        /// The type of write operation to perform.
        /// </param>
        /// <param name="value">
        /// The character value to write.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character value.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a new line after the value.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the value.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the value.
        /// </param>
        /// <returns>
        /// True if the value was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a string value as part of rendering a
        /// box, dispatching to the appropriate write method based on
        /// the specified host write type.
        /// </summary>
        /// <param name="hostWriteType">
        /// The type of write operation to perform.
        /// </param>
        /// <param name="value">
        /// The string value to write.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the value.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the value.
        /// </param>
        /// <returns>
        /// True if the value was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a string value as part of rendering a
        /// box, dispatching to the appropriate write method based on
        /// the specified host write type.
        /// </summary>
        /// <param name="hostWriteType">
        /// The type of write operation to perform.
        /// </param>
        /// <param name="value">
        /// The string value to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a new line after the value.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the value.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the value.
        /// </param>
        /// <returns>
        /// True if the value was written successfully; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified host is a
        /// default host.
        /// </summary>
        /// <param name="host">
        /// The host instance to check.
        /// </param>
        /// <returns>
        /// True if the specified host is a default host; otherwise,
        /// false.
        /// </returns>
        protected virtual bool IsDefaultHost(
            IHost host
            )
        {
            return (host is _Hosts.Default);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified host is an
        /// engine host.
        /// </summary>
        /// <param name="host">
        /// The host instance to check.
        /// </param>
        /// <returns>
        /// True if the specified host is an engine host; otherwise,
        /// false.
        /// </returns>
        protected virtual bool IsEngineHost(
            IHost host
            )
        {
            return (host is _Hosts.Engine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified host is a
        /// file host.
        /// </summary>
        /// <param name="host">
        /// The host instance to check.
        /// </param>
        /// <returns>
        /// True if the specified host is a file host; otherwise, false.
        /// </returns>
        protected virtual bool IsFileHost(
            IHost host
            )
        {
            return (host is _Hosts.File);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified host is a
        /// profile host.
        /// </summary>
        /// <param name="host">
        /// The host instance to check.
        /// </param>
        /// <returns>
        /// True if the specified host is a profile host; otherwise,
        /// false.
        /// </returns>
        protected virtual bool IsProfileHost(
            IHost host
            )
        {
            return (host is _Hosts.Profile);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified host is a
        /// shell host.
        /// </summary>
        /// <param name="host">
        /// The host instance to check.
        /// </param>
        /// <returns>
        /// True if the specified host is a shell host; otherwise, false.
        /// </returns>
        protected virtual bool IsShellHost(
            IHost host
            )
        {
            return (host is _Hosts.Shell);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified host is a
        /// core host.
        /// </summary>
        /// <param name="host">
        /// The host instance to check.
        /// </param>
        /// <returns>
        /// True if the specified host is a core host; otherwise, false.
        /// </returns>
        protected virtual bool IsCoreHost(
            IHost host
            )
        {
            return (host is _Hosts.Core);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified host is a
        /// console host.
        /// </summary>
        /// <param name="host">
        /// The host instance to check.
        /// </param>
        /// <returns>
        /// True if the specified host is a console host; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the host is running within
        /// an X11 terminal environment.
        /// </summary>
        /// <returns>
        /// True if running within an X11 terminal; otherwise, false.
        /// </returns>
        protected virtual bool IsX11Terminal()
        {
            return WindowOps.IsX11Terminal();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host is running within
        /// the Windows Terminal (Cascadia) application.
        /// </summary>
        /// <returns>
        /// True if running within Windows Terminal; otherwise, false.
        /// </returns>
        protected virtual bool IsWindowsTerminal()
        {
            return WindowOps.IsWindowsTerminal();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Capability Detection Methods
        /// <summary>
        /// This method determines whether the host supports
        /// non-monochrome (i.e. colored) output.
        /// </summary>
        /// <returns>
        /// True if the host supports color; otherwise, false.
        /// </returns>
        protected virtual bool DoesSupportColor()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.NonMonochromeMask, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host supports reversed
        /// (i.e. swapped foreground and background) colors.
        /// </summary>
        /// <returns>
        /// True if the host supports reversed colors; otherwise, false.
        /// </returns>
        protected virtual bool DoesSupportReversedColor()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.ReversedColor, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host supports querying
        /// and/or setting its size.
        /// </summary>
        /// <returns>
        /// True if the host supports sizing; otherwise, false.
        /// </returns>
        protected virtual bool DoesSupportSizing()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.Sizing, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host supports setting
        /// the cursor position.
        /// </summary>
        /// <returns>
        /// True if the host supports positioning; otherwise, false.
        /// </returns>
        protected virtual bool DoesSupportPositioning()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.Positioning, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host should adjust
        /// requested colors to improve their visibility.
        /// </summary>
        /// <returns>
        /// True if requested colors should be adjusted; otherwise,
        /// false.
        /// </returns>
        protected virtual bool DoesAdjustColor()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.AdjustColor, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host should avoid
        /// applying colors when writing a new line.
        /// </summary>
        /// <returns>
        /// True if colors should not be applied when writing a new
        /// line; otherwise, false.
        /// </returns>
        protected virtual bool DoesNoColorNewLine()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.NoColorNewLine, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host should use the
        /// saved colors when no specific color is requested.
        /// </summary>
        /// <returns>
        /// True if the saved colors should be used when no specific
        /// color is requested; otherwise, false.
        /// </returns>
        protected virtual bool DoesSavedColorForNone()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.SavedColorForNone, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host should reset all
        /// color attributes prior to restoring saved colors.
        /// </summary>
        /// <returns>
        /// True if all color attributes should be reset before
        /// restoring; otherwise, false.
        /// </returns>
        protected virtual bool DoesResetColorForRestore()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.ResetColorForRestore, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host may need to reset
        /// the colors before setting new colors.
        /// </summary>
        /// <returns>
        /// True if the colors may need to be reset before setting new
        /// colors; otherwise, false.
        /// </returns>
        protected internal virtual bool DoesMaybeResetColorForSet()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.MaybeResetColorForSet, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host should emit trace
        /// diagnostics when a requested color change has no effect.
        /// </summary>
        /// <returns>
        /// True if such trace diagnostics should be emitted; otherwise,
        /// false.
        /// </returns>
        protected virtual bool DoesTraceColorNotChanged()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.TraceColorNotChanged, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host should avoid
        /// setting the foreground color.
        /// </summary>
        /// <returns>
        /// True if setting the foreground color should be avoided;
        /// otherwise, false.
        /// </returns>
        protected virtual bool DoesNoSetForegroundColor()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.NoSetForegroundColor, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host should avoid
        /// setting the background color.
        /// </summary>
        /// <returns>
        /// True if setting the background color should be avoided;
        /// otherwise, false.
        /// </returns>
        protected virtual bool DoesNoSetBackgroundColor()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.NoSetBackgroundColor, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host should normalize
        /// line endings to the configured new line sequence.
        /// </summary>
        /// <returns>
        /// True if line endings should be normalized; otherwise, false.
        /// </returns>
        protected virtual bool DoesNormalizeToNewLine()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.NormalizeToNewLine, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// This method determines whether the host should use native
        /// Windows console support.
        /// </summary>
        /// <returns>
        /// True if native Windows console support should be used;
        /// otherwise, false.
        /// </returns>
        protected virtual bool DoesNativeWindows()
        {
            return FlagOps.HasFlags(
                MaybeInitializeHostFlags(), HostFlags.NativeWindows, true);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Content Section Methods
        /// <summary>
        /// This method determines whether the size represented by the
        /// specified host flags is at least as large as the size
        /// required by another set of host flags.
        /// </summary>
        /// <param name="flags">
        /// The host flags representing the available size.
        /// </param>
        /// <param name="hasFlags">
        /// The host flags representing the required size.
        /// </param>
        /// <returns>
        /// True if the available size is at least the required size;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the section associated with
        /// the specified header flags will fit within the available
        /// host size.
        /// </summary>
        /// <param name="headerFlags">
        /// The header flags identifying the content section to check.
        /// </param>
        /// <param name="hostFlags">
        /// The host flags describing the available host size.
        /// </param>
        /// <returns>
        /// True if the associated section will fit; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method begins processing for a new content section,
        /// advancing the output position based on the currently
        /// configured output style.
        /// </summary>
        /// <param name="nextRow">
        /// Non-zero to advance to the start of the next row before
        /// beginning the section.
        /// </param>
        /// <param name="minimumLeft">
        /// The minimum horizontal output position for the section.
        /// </param>
        /// <param name="maximumTop">
        /// Upon success, this may be updated to reflect the new
        /// vertical output position.
        /// </param>
        /// <param name="savedTop">
        /// Upon success, this may be updated to reflect the saved
        /// vertical output position for the section.
        /// </param>
        /// <param name="count">
        /// Upon success, this may be updated to reflect the number of
        /// items written on the current row.
        /// </param>
        /// <returns>
        /// True if the section was begun successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method completes processing for the current content
        /// section, updating the maximum output positions that have
        /// been seen so far.
        /// </summary>
        /// <param name="maximumLeft">
        /// Upon success, this may be updated to reflect the maximum
        /// horizontal output position seen so far.
        /// </param>
        /// <param name="maximumTop">
        /// Upon success, this may be updated to reflect the maximum
        /// vertical output position seen so far.
        /// </param>
        /// <returns>
        /// True if the section was ended successfully; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// The backing field for the <see cref="Name" /> property.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this identifier.
        /// </summary>
        public virtual string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The backing field for the <see cref="Kind" /> property.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the enumerated kind of this identifier (for example,
        /// command or plugin).
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Id" /> property.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier associated with this identifier.
        /// </summary>
        public virtual Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The backing field for the <see cref="ClientData" /> property.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the extra, entity-specific data associated with this
        /// entity, if any.  This value may be null.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The backing field for the <see cref="Group" /> property.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the logical group that this identifier belongs to.
        /// </summary>
        public virtual string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Description" /> property.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this identifier.
        /// </summary>
        public virtual string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        /// <summary>
        /// This method is called when interactive processing is about to begin
        /// at the specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="text">
        /// On input, the text associated with the start of processing; on
        /// output, the possibly modified text.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
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

        /// <summary>
        /// This method is called when interactive processing is about to end at
        /// the specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="text">
        /// On input, the text associated with the end of processing; on output,
        /// the possibly modified text.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
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

        /// <summary>
        /// This method is called when interactive processing has completed at
        /// the specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public virtual ReturnCode DoneProcessing(
            int levels,
            ref Result error
            )
        {
            CheckDisposed();

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Title" /> property.
        /// </summary>
        private string title;
        /// <summary>
        /// Gets or sets the current window or console title used by this host.
        /// </summary>
        public virtual string Title
        {
            get { CheckDisposed(); return title; }
            set { CheckDisposed(); title = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method updates the host's window or console title to reflect
        /// its current value.
        /// </summary>
        /// <returns>
        /// True if the title was refreshed; otherwise, false.
        /// </returns>
        public abstract bool RefreshTitle(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host's interactive input has been
        /// redirected (for example, from a file or pipe).
        /// </summary>
        /// <returns>
        /// True if the input is redirected; otherwise, false.
        /// </returns>
        public abstract bool IsInputRedirected(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method displays a prompt of the specified type and reports the
        /// flags that resulted from displaying it.
        /// </summary>
        /// <param name="type">
        /// The type of prompt to display (for example, a normal or
        /// continuation prompt).
        /// </param>
        /// <param name="flags">
        /// On input, the flags that control how the prompt is displayed; on
        /// output, the flags that resulted from displaying it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public abstract ReturnCode Prompt(
            PromptType type,
            ref PromptFlags flags,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host's interactive resources are
        /// currently open.
        /// </summary>
        /// <returns>
        /// True if the host is open; otherwise, false.
        /// </returns>
        public abstract bool IsOpen(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method pauses interactive processing, typically waiting for the
        /// user to acknowledge before continuing.
        /// </summary>
        /// <returns>
        /// True if the host was paused; otherwise, false.
        /// </returns>
        public abstract bool Pause(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes any buffered host output.
        /// </summary>
        /// <returns>
        /// True if the output was flushed; otherwise, false.
        /// </returns>
        public abstract bool Flush(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field that holds the header flags returned by the
        /// <see cref="GetHeaderFlags" /> method.
        /// </summary>
        private HeaderFlags headerFlags = HeaderFlags.Default;
        /// <summary>
        /// This method returns the flags that control which header sections the
        /// host displays.
        /// </summary>
        /// <returns>
        /// The current header flags for this host.
        /// </returns>
        public virtual HeaderFlags GetHeaderFlags()
        {
            CheckDisposed();

            return PrivateGetHeaderFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field that holds the detail flags returned by the
        /// <see cref="GetDetailFlags" /> method.
        /// </summary>
        private DetailFlags detailFlags = DetailFlags.Default;
        /// <summary>
        /// This method returns the flags that control how much detail the host
        /// includes in its output.
        /// </summary>
        /// <returns>
        /// The current detail flags for this host.
        /// </returns>
        public virtual DetailFlags GetDetailFlags()
        {
            CheckDisposed();

            return PrivateGetDetailFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field that holds the cached host flags returned by the
        /// <see cref="GetHostFlags" /> method.
        /// </summary>
        private HostFlags hostFlags = HostFlags.Invalid;
        /// <summary>
        /// This method returns the flags that describe the capabilities and
        /// configuration of this host.
        /// </summary>
        /// <returns>
        /// The current host flags for this host.
        /// </returns>
        public virtual HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current nesting level of read operations in progress on
        /// this host.
        /// </summary>
        public abstract int ReadLevels { get; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current nesting level of write operations in progress on
        /// this host.
        /// </summary>
        public abstract int WriteLevels { get; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single line of interactive input from the host.
        /// </summary>
        /// <param name="value">
        /// Upon success, this is set to the line of input that was read.
        /// </param>
        /// <returns>
        /// True if a line was read; otherwise, false.
        /// </returns>
        public abstract bool ReadLine(
            ref string value
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the host output.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public virtual bool Write(
            char value
            )
        {
            CheckDisposed();

            return Write(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public virtual bool Write(
            string value
            )
        {
            CheckDisposed();

            return Write(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes an end-of-line to the host output.
        /// </summary>
        /// <returns>
        /// True if the end-of-line was written; otherwise, false.
        /// </returns>
        public abstract bool WriteLine(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string followed by an end-of-line to the host
        /// output.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <returns>
        /// True if the line was written; otherwise, false.
        /// </returns>
        public virtual bool WriteLine(
            string value
            )
        {
            CheckDisposed();

            return Write(value, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a formatted representation of a result, followed
        /// by an end-of-line, to the host output.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write.
        /// </param>
        /// <returns>
        /// True if the line was written; otherwise, false.
        /// </returns>
        public virtual bool WriteResultLine(
            ReturnCode code,
            Result result
            )
        {
            CheckDisposed();

            return WriteResult(code, result, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a formatted representation of a result, including
        /// an error line number, followed by an end-of-line, to the host
        /// output.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <returns>
        /// True if the line was written; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// The backing field for the <see cref="StreamFlags" /> property.
        /// </summary>
        private HostStreamFlags streamFlags = HostStreamFlags.Default;
        /// <summary>
        /// Gets or sets the flags that control how this host opens and manages
        /// streams.
        /// </summary>
        public virtual HostStreamFlags StreamFlags
        {
            get { CheckDisposed(); return streamFlags; }
            set { CheckDisposed(); streamFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens a stream for the specified path on behalf of the
        /// engine.
        /// </summary>
        /// <param name="path">
        /// The path of the file or resource to open.
        /// </param>
        /// <param name="mode">
        /// The mode used when opening the stream (for example, create or
        /// open).
        /// </param>
        /// <param name="access">
        /// The access requested for the stream (for example, read or write).
        /// </param>
        /// <param name="share">
        /// The sharing mode permitted for the stream.
        /// </param>
        /// <param name="bufferSize">
        /// The size, in bytes, of the buffer to use for the stream.
        /// </param>
        /// <param name="options">
        /// The additional options used when opening the stream.
        /// </param>
        /// <param name="hostStreamFlags">
        /// On input, the flags that influence how the stream is opened; on
        /// output, the flags describing the stream that was opened.
        /// </param>
        /// <param name="fullPath">
        /// Upon return, this contains the fully qualified path of the stream
        /// that was opened.
        /// </param>
        /// <param name="stream">
        /// Upon success, this contains the opened stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
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

        /// <summary>
        /// This method fetches the named data (for example, a script) on
        /// behalf of the engine.
        /// </summary>
        /// <param name="name">
        /// The name of the data to fetch.
        /// </param>
        /// <param name="dataFlags">
        /// The flags that control how the data is located and fetched.
        /// </param>
        /// <param name="scriptFlags">
        /// On input, the flags that influence how the data is fetched; on
        /// output, the flags describing the data that was fetched.
        /// </param>
        /// <param name="clientData">
        /// On input, the extra data supplied for the request, if any; on
        /// output, the extra data associated with the fetched data, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the fetched data.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
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
        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is
        /// permitted to exit.
        /// </summary>
        public virtual bool CanExit
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.CanExit, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.CanExit, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is
        /// permitted to be forcibly exited.
        /// </summary>
        public virtual bool CanForceExit
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.CanForceExit, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.CanForceExit, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is
        /// currently in the process of exiting.
        /// </summary>
        public virtual bool Exiting
        {
            get { CheckDisposed(); return IsExiting(); }
            set { CheckDisposed(); SetExiting(value); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IThreadHost Members
        /// <summary>
        /// This method creates a new thread that uses a parameterless start
        /// delegate.
        /// </summary>
        /// <param name="start">
        /// The delegate that represents the entry point for the new thread.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to use for the new thread, or
        /// zero to use the default.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the new thread will host a user interface and should be
        /// configured for single-threaded apartment use.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the new thread should be created as a background thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the new thread should inherit the active call stack from
        /// the creating thread.
        /// </param>
        /// <param name="thread">
        /// Upon success, this will contain the newly created thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method creates a new thread that uses a parameterized start
        /// delegate.
        /// </summary>
        /// <param name="start">
        /// The delegate that represents the entry point for the new thread and
        /// accepts a single object argument.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to use for the new thread, or
        /// zero to use the default.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the new thread will host a user interface and should be
        /// configured for single-threaded apartment use.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the new thread should be created as a background thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the new thread should inherit the active call stack from
        /// the creating thread.
        /// </param>
        /// <param name="thread">
        /// Upon success, this will contain the newly created thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method queues a parameterless callback for execution on a
        /// thread pool thread.
        /// </summary>
        /// <param name="callback">
        /// The delegate to invoke on a thread pool thread.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the work item is queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public abstract ReturnCode QueueWorkItem(
            ThreadStart callback,
            QueueFlags flags,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues a callback that accepts a state object for
        /// execution on a thread pool thread.
        /// </summary>
        /// <param name="callback">
        /// The delegate to invoke on a thread pool thread.
        /// </param>
        /// <param name="state">
        /// The state object to pass to the callback.  This parameter may be
        /// null.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the work item is queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public abstract ReturnCode QueueWorkItem(
            WaitCallback callback,
            object state,
            QueueFlags flags,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method suspends the current thread for the specified amount of
        /// time.
        /// </summary>
        /// <param name="milliseconds">
        /// The amount of time to suspend the current thread, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the thread was successfully suspended; otherwise, false.
        /// </returns>
        public abstract bool Sleep(
            int milliseconds
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method causes the current thread to yield execution to another
        /// thread that is ready to run on the current processor.
        /// </summary>
        /// <returns>
        /// True if the operating system switched execution to another thread;
        /// otherwise, false.
        /// </returns>
        public abstract bool Yield(); /* PRIMITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        /// <summary>
        /// Gets the default input stream for this host.
        /// </summary>
        public virtual Stream DefaultIn
        {
            get { CheckDisposed(); return In; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the default output stream for this host.
        /// </summary>
        public virtual Stream DefaultOut
        {
            get { CheckDisposed(); return Out; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the default error stream for this host.
        /// </summary>
        public virtual Stream DefaultError
        {
            get { CheckDisposed(); return Error; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the active input stream for this host.
        /// </summary>
        public abstract Stream In { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the active output stream for this host.
        /// </summary>
        public abstract Stream Out { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the active error stream for this host.
        /// </summary>
        public abstract Stream Error { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the encoding used for the input stream.
        /// </summary>
        public abstract Encoding InputEncoding { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the encoding used for the output stream.
        /// </summary>
        public abstract Encoding OutputEncoding { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the encoding used for the error stream.
        /// </summary>
        public abstract Encoding ErrorEncoding { get; set; } /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active input stream to its default.
        /// </summary>
        /// <returns>
        /// True if the input stream was reset; otherwise, false.
        /// </returns>
        public abstract bool ResetIn(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active output stream to its default.
        /// </summary>
        /// <returns>
        /// True if the output stream was reset; otherwise, false.
        /// </returns>
        public abstract bool ResetOut(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active error stream to its default.
        /// </summary>
        /// <returns>
        /// True if the error stream was reset; otherwise, false.
        /// </returns>
        public abstract bool ResetError(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the output stream for this host has
        /// been redirected.
        /// </summary>
        /// <returns>
        /// True if the output stream has been redirected; otherwise, false.
        /// </returns>
        public abstract bool IsOutputRedirected(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the error stream for this host has
        /// been redirected.
        /// </summary>
        /// <returns>
        /// True if the error stream has been redirected; otherwise, false.
        /// </returns>
        public abstract bool IsErrorRedirected(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets up the input, output, and error channels for this
        /// host.
        /// </summary>
        /// <returns>
        /// True if the channels were set up successfully; otherwise, false.
        /// </returns>
        public abstract bool SetupChannels(); /* PRIMITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        /// <summary>
        /// This method creates a copy of this host.
        /// </summary>
        /// <returns>
        /// The newly created copy of this host, or null if it could not be
        /// created.
        /// </returns>
        public abstract IHost Clone(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of this host for use with the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the cloned host will be associated with.
        /// </param>
        /// <returns>
        /// The newly created copy of this host, or null if it could not be
        /// created.
        /// </returns>
        public abstract IHost Clone(
            Interpreter interpreter
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the <see cref="HostTestFlags" /> that describe the
        /// testing capabilities of this host.
        /// </summary>
        /// <returns>
        /// The host test flags for this host.
        /// </returns>
        public abstract HostTestFlags GetTestFlags(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests that the current script evaluation be
        /// canceled.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly cancel evaluation even if cancellation has
        /// been disabled or is otherwise being prevented.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public abstract ReturnCode Cancel(
            bool force,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests that the interpreter exit.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly request the exit even if it has been disabled
        /// or is otherwise being prevented.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public abstract ReturnCode Exit(
            bool force,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a line terminator to the debug output of the
        /// host.
        /// </summary>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public abstract bool WriteDebugLine(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string, followed by a line
        /// terminator, to the debug output of the host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public virtual bool WriteDebugLine(
            string value
            )
        {
            CheckDisposed();

            return WriteDebug(value, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the debug output of
        /// the host.
        /// </summary>
        /// <param name="value">
        /// The character to write to the debug output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public virtual bool WriteDebug(
            char value
            )
        {
            CheckDisposed();

            return WriteDebug(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the debug output of
        /// the host, optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the character.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public abstract bool WriteDebug(
            char value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character a number of times to the
        /// debug output of the host, using the specified colors and optionally
        /// followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the debug output.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the characters.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified string to the debug output of the
        /// host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public virtual bool WriteDebug(
            string value
            )
        {
            CheckDisposed();

            return WriteDebug(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the debug output of the
        /// host, optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public abstract bool WriteDebug(
            string value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the debug output of the
        /// host, using the specified foreground color and optionally followed
        /// by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified string to the debug output of the
        /// host, using the specified colors and optionally followed by a line
        /// terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a line terminator to the error output of the
        /// host.
        /// </summary>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public abstract bool WriteErrorLine(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string, followed by a line
        /// terminator, to the error output of the host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public virtual bool WriteErrorLine(
            string value
            )
        {
            CheckDisposed();

            return WriteError(value, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the error output of
        /// the host.
        /// </summary>
        /// <param name="value">
        /// The character to write to the error output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public virtual bool WriteError(
            char value
            )
        {
            CheckDisposed();

            return WriteError(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the error output of
        /// the host, optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the character.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public abstract bool WriteError(
            char value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character a number of times to the
        /// error output of the host, using the specified colors and optionally
        /// followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the error output.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the characters.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified string to the error output of the
        /// host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public virtual bool WriteError(
            string value
            )
        {
            CheckDisposed();

            return WriteError(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the error output of the
        /// host, optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public abstract bool WriteError(
            string value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the error output of the
        /// host, using the specified foreground color and optionally followed
        /// by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified string to the error output of the
        /// host, using the specified colors and optionally followed by a line
        /// terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified return code and result to the
        /// host, optionally followed by a line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified return code and result to the
        /// host, optionally without additional formatting and optionally
        /// followed by a line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="raw">
        /// Non-zero to write the result without any additional formatting.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified return code, result, and error
        /// line to the host, optionally followed by a line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified return code, result, and error
        /// line to the host, optionally without additional formatting and
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="raw">
        /// Non-zero to write the result without any additional formatting.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified prefix, return code, result, and
        /// error line to the host, optionally followed by a line terminator.
        /// </summary>
        /// <param name="prefix">
        /// The string to write before the result.
        /// </param>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified prefix, return code, result, and
        /// error line to the host, optionally without additional formatting
        /// and optionally followed by a line terminator.
        /// </summary>
        /// <param name="prefix">
        /// The string to write before the result.
        /// </param>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="raw">
        /// Non-zero to write the result without any additional formatting.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method saves the current cursor position so that it can later
        /// be restored by the <see cref="RestorePosition" /> method.
        /// </summary>
        /// <returns>
        /// True if the position was saved (or saving was unnecessary);
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method restores the cursor position previously saved by the
        /// <see cref="SavePosition" /> method, optionally moving to the start
        /// of the next line.
        /// </summary>
        /// <param name="newLine">
        /// Non-zero to position the cursor at the beginning of the line below
        /// the saved position; otherwise, the exact saved position is
        /// restored.
        /// </param>
        /// <returns>
        /// True if the position was restored (or restoration was unnecessary);
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a centered announcement banner to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint that triggered this announcement being
        /// written.
        /// </param>
        /// <param name="value">
        /// The announcement text to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the announcement.
        /// </param>
        /// <returns>
        /// True if the announcement was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a centered announcement banner to the host
        /// output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint that triggered this announcement being
        /// written.
        /// </param>
        /// <param name="value">
        /// The announcement text to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the announcement.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the announcement.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the announcement.
        /// </param>
        /// <returns>
        /// True if the announcement was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about a command invocation, including
        /// its arguments and result, to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the command invocation.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint that triggered this information being
        /// written.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the breakpoint that triggered this information being
        /// written.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to include in the output.
        /// </param>
        /// <param name="result">
        /// The result associated with the command invocation.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about a command invocation, including
        /// its arguments and result, to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the command invocation.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint that triggered this information being
        /// written.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the breakpoint that triggered this information being
        /// written.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to include in the output.
        /// </param>
        /// <param name="result">
        /// The result associated with the command invocation.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a single, formatted call frame entry to the host
        /// output, optionally followed by a separator line.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write.  If this parameter is null, only the type
        /// designation is written.
        /// </param>
        /// <param name="type">
        /// The type designation to display for the call frame.  If this
        /// parameter is null, a type is derived from the call frame.
        /// </param>
        /// <param name="prefix">
        /// The text to display immediately before the call frame type.
        /// </param>
        /// <param name="suffix">
        /// The text to display immediately after the formatted call frame.
        /// </param>
        /// <param name="separator">
        /// The character used to draw the separator line, or the no-separator
        /// sentinel to skip the separator.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the call frame.
        /// </param>
        /// <returns>
        /// True if the call frame was written; otherwise, false.
        /// </returns>
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
                Characters.Colon.ToString() + Characters.SpaceString +
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

        /// <summary>
        /// This method writes information about the specified call frame to
        /// the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write information about.  If this parameter is
        /// null, nothing is written.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified call frame to
        /// the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write information about.  If this parameter is
        /// null, nothing is written.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the call frames contained in the specified call
        /// stack to the host output, one frame per line.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack whose call frames should be written.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the call frames.
        /// </param>
        /// <returns>
        /// True if the call frames were written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the call frames contained in the specified call
        /// stack to the host output, one frame per line, up to the specified
        /// limit.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack whose call frames should be written.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to write.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the call frames.
        /// </param>
        /// <returns>
        /// True if the call frames were written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified call stack to
        /// the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified call stack to
        /// the host output, limiting the number of call frames shown.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to include in the output.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified call stack to
        /// the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to include in the output.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method writes information about the script debugger associated
        /// with the interpreter to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the script debugger associated
        /// with the interpreter to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified sets of engine,
        /// substitution, event, expression, and header flags to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to include in the output.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to include in the output.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to include in the output.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to include in the output.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags to include in the output.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified sets of engine,
        /// substitution, event, expression, and header flags to the host
        /// output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to include in the output.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to include in the output.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to include in the output.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to include in the output.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags to include in the output.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about this host to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about this host to the host output,
        /// using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the interpreter itself to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the interpreter itself to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the script engine state to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the script engine state to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the interpreter entity counts
        /// to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the interpreter entity counts
        /// to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the native stack usage to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the native stack usage to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the interpreter readiness and
        /// flow control state to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the interpreter readiness and
        /// flow control state to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the testing subsystem to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the testing subsystem to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified token to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="token">
        /// The token to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified token to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="token">
        /// The token to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified trace operation
        /// to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace operation to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified trace operation
        /// to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace operation to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified variable to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="variable">
        /// The variable to write information about.  If this parameter is
        /// null, placeholder information is written instead.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified variable to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="variable">
        /// The variable to write information about.  If this parameter is
        /// null, placeholder information is written instead.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified opaque object
        /// handle to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="object">
        /// The opaque object handle to write information about.  If this
        /// parameter is null, placeholder information is written instead.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the specified opaque object
        /// handle to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="object">
        /// The opaque object handle to write information about.  If this
        /// parameter is null, placeholder information is written instead.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the most recent complaint
        /// raised by the interpreter to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the most recent complaint
        /// raised by the interpreter to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method writes information about the interpreter's command
        /// execution history to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="historyFilter">
        /// The filter that selects which history entries are written.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about the interpreter's command
        /// execution history to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="historyFilter">
        /// The filter that selects which history entries are written.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes custom, host-specific information to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes custom, host-specific information to the host
        /// output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public abstract bool WriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes complete information about a result, including
        /// the previous result, to the host output.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="previousResult">
        /// The previous result value to include in the output.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes complete information about a result, including
        /// the previous result, to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="previousResult">
        /// The previous result value to include in the output.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about a named result to the host
        /// output.
        /// </summary>
        /// <param name="name">
        /// The name associated with the result.
        /// </param>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes information about a named result to the host
        /// output, using the specified colors.
        /// </summary>
        /// <param name="name">
        /// The name associated with the result.
        /// </param>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method writes the interactive loop header to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the header is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="loopData">
        /// The data describing the interactive loop the header is associated
        /// with.
        /// </param>
        /// <param name="result">
        /// The result value to include in the header, if any.
        /// </param>
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

            DetailFlags detailFlags = (loopData != null) ?
                loopData.DetailFlags : DetailFlags.Default;

            PopulateDetailFlags(interpreter, ref detailFlags);
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

        /// <summary>
        /// This method writes the interactive loop footer to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the footer is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="loopData">
        /// The data describing the interactive loop the footer is associated
        /// with.
        /// </param>
        /// <param name="result">
        /// The result value to include in the footer, if any.
        /// </param>
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

#if false
            DetailFlags detailFlags = (loopData != null) ?
                loopData.DetailFlags : DetailFlags.Default;
#endif

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
        /// <summary>
        /// This method begins rendering a box with the specified name and
        /// content.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs that make up the content of the box.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the box was successfully begun; otherwise, false.
        /// </returns>
        public abstract bool BeginBox(
            string name,
            StringPairList list,
            IClientData clientData
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends rendering a box with the specified name and
        /// content.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs that make up the content of the box.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the box was successfully ended; otherwise, false.
        /// </returns>
        public abstract bool EndBox(
            string name,
            StringPairList list,
            IClientData clientData
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string value as the content of a
        /// box with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified string value as the content of a
        /// box with the specified name, padding the content to a minimum width.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified string value as the content of a
        /// box with the specified name, using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified string value as the content of a
        /// box with the specified name, padding the content to a minimum width
        /// and using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified string value as the content of a
        /// box with the specified name, using the specified content and box
        /// colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified string value as the content of a
        /// box with the specified name, padding the content to a minimum width
        /// and using the specified content and box colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified list of name/value pairs as the
        /// content of a box with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified list of name/value pairs as the
        /// content of a box with the specified name, padding the content to a
        /// minimum width.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified list of name/value pairs as the
        /// content of a box with the specified name, using the specified content
        /// colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified list of name/value pairs as the
        /// content of a box with the specified name, padding the content to a
        /// minimum width and using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified list of name/value pairs as the
        /// content of a box with the specified name, using the specified content
        /// and box colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified list of name/value pairs as the
        /// content of a box with the specified name, padding the content to a
        /// minimum width and using the specified content and box colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
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
                if (levels == 1)
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
                                                                Characters.SpaceString,
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
        /// <summary>
        /// Gets or sets a value indicating whether colorized output is
        /// disabled for this host.  When true, color operations have no
        /// visible effect.
        /// </summary>
        public virtual bool NoColor
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.NoColor, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.NoColor, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the host foreground and background colors to their
        /// default values.
        /// </summary>
        /// <returns>
        /// True if the colors were reset; otherwise, false.
        /// </returns>
        public abstract bool ResetColors(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the current foreground and background colors of the
        /// host.
        /// </summary>
        /// <param name="foregroundColor">
        /// Upon success, receives the current foreground color.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon success, receives the current background color.
        /// </param>
        /// <returns>
        /// True if the colors were obtained; otherwise, false.
        /// </returns>
        public abstract bool GetColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adjusts the specified foreground and background colors
        /// as necessary so that they are suitable for use by the host (e.g. to
        /// avoid an unreadable combination).
        /// </summary>
        /// <param name="foregroundColor">
        /// On input, the desired foreground color; on output, the adjusted
        /// foreground color.
        /// </param>
        /// <param name="backgroundColor">
        /// On input, the desired background color; on output, the adjusted
        /// background color.
        /// </param>
        /// <returns>
        /// True if the colors were adjusted; otherwise, false.
        /// </returns>
        public abstract bool AdjustColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the host foreground color.
        /// </summary>
        /// <param name="foregroundColor">
        /// The foreground color to set.
        /// </param>
        /// <returns>
        /// True if the foreground color was set; otherwise, false.
        /// </returns>
        public abstract bool SetForegroundColor(
            ConsoleColor foregroundColor
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the host background color.
        /// </summary>
        /// <param name="backgroundColor">
        /// The background color to set.
        /// </param>
        /// <returns>
        /// True if the background color was set; otherwise, false.
        /// </returns>
        public abstract bool SetBackgroundColor(
            ConsoleColor backgroundColor
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the host foreground and/or background colors.
        /// </summary>
        /// <param name="foreground">
        /// Non-zero to set the foreground color.
        /// </param>
        /// <param name="background">
        /// Non-zero to set the background color.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to set, used only when
        /// <paramref name="foreground" /> is true.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to set, used only when
        /// <paramref name="background" /> is true.
        /// </param>
        /// <returns>
        /// True if the requested colors were set; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets the foreground and/or background colors associated
        /// with a named entry within a color theme.
        /// </summary>
        /// <param name="theme">
        /// The name of the color theme to query, or null to use the active
        /// theme.
        /// </param>
        /// <param name="name">
        /// The name of the color entry within the theme.
        /// </param>
        /// <param name="foreground">
        /// Non-zero to obtain the foreground color.
        /// </param>
        /// <param name="background">
        /// Non-zero to obtain the background color.
        /// </param>
        /// <param name="foregroundColor">
        /// Upon success, receives the foreground color, when requested.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon success, receives the background color, when requested.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method sets the foreground and/or background colors associated
        /// with a named entry within a color theme.
        /// </summary>
        /// <param name="theme">
        /// The name of the color theme to modify, or null to use the active
        /// theme.
        /// </param>
        /// <param name="name">
        /// The name of the color entry within the theme.
        /// </param>
        /// <param name="foreground">
        /// Non-zero to set the foreground color.
        /// </param>
        /// <param name="background">
        /// Non-zero to set the background color.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to set, used only when
        /// <paramref name="foreground" /> is true.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to set, used only when
        /// <paramref name="background" /> is true.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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
        /// <summary>
        /// This method resets the current position to its default value.
        /// </summary>
        /// <returns>
        /// True if the position was reset; otherwise, false.
        /// </returns>
        public virtual bool ResetPosition()
        {
            CheckDisposed();

            hostLeft = 0;
            hostTop = 0;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the current position.
        /// </summary>
        /// <param name="left">
        /// Upon success, receives the zero-based column (horizontal)
        /// coordinate of the current position.
        /// </param>
        /// <param name="top">
        /// Upon success, receives the zero-based row (vertical) coordinate
        /// of the current position.
        /// </param>
        /// <returns>
        /// True if the position was obtained; otherwise, false.
        /// </returns>
        public abstract bool GetPosition(
            ref int left,
            ref int top
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the current position.
        /// </summary>
        /// <param name="left">
        /// The zero-based column (horizontal) coordinate to set as the
        /// current position.
        /// </param>
        /// <param name="top">
        /// The zero-based row (vertical) coordinate to set as the current
        /// position.
        /// </param>
        /// <returns>
        /// True if the position was set; otherwise, false.
        /// </returns>
        public abstract bool SetPosition(
            int left,
            int top
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the default position.
        /// </summary>
        /// <param name="left">
        /// Upon success, receives the zero-based column (horizontal)
        /// coordinate of the default position.
        /// </param>
        /// <param name="top">
        /// Upon success, receives the zero-based row (vertical) coordinate
        /// of the default position.
        /// </param>
        /// <returns>
        /// True if the default position was obtained; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method sets the default position.
        /// </summary>
        /// <param name="left">
        /// The zero-based column (horizontal) coordinate to set as the
        /// default position.
        /// </param>
        /// <param name="top">
        /// The zero-based row (vertical) coordinate to set as the default
        /// position.
        /// </param>
        /// <returns>
        /// True if the default position was set; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method resets the size of the specified host buffer and/or
        /// window to its default.
        /// </summary>
        /// <param name="hostSizeType">
        /// The <see cref="HostSizeType" /> value indicating which size should
        /// be reset.
        /// </param>
        /// <returns>
        /// True if the size was reset successfully; otherwise, false.
        /// </returns>
        public abstract bool ResetSize(
            HostSizeType hostSizeType
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the size of the specified host buffer and/or
        /// window.
        /// </summary>
        /// <param name="hostSizeType">
        /// The <see cref="HostSizeType" /> value indicating which size should
        /// be queried.
        /// </param>
        /// <param name="width">
        /// Upon success, this contains the width, in characters.
        /// </param>
        /// <param name="height">
        /// Upon success, this contains the height, in characters.
        /// </param>
        /// <returns>
        /// True if the size was queried successfully; otherwise, false.
        /// </returns>
        public abstract bool GetSize(
            HostSizeType hostSizeType,
            ref int width,
            ref int height
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method changes the size of the specified host buffer and/or
        /// window.
        /// </summary>
        /// <param name="hostSizeType">
        /// The <see cref="HostSizeType" /> value indicating which size should
        /// be changed.
        /// </param>
        /// <param name="width">
        /// The new width, in characters.
        /// </param>
        /// <param name="height">
        /// The new height, in characters.
        /// </param>
        /// <returns>
        /// True if the size was changed successfully; otherwise, false.
        /// </returns>
        public abstract bool SetSize(
            HostSizeType hostSizeType,
            int width,
            int height
            ); /* PRIMITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IReadHost Members
        /// <summary>
        /// This method reads a single character from the host.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the character that was read, or a negative
        /// value if the end of the input was reached.
        /// </param>
        /// <returns>
        /// True if the character was read; otherwise, false.
        /// </returns>
        public abstract bool Read(
            ref int value
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single key press from the host.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key press so that it is not displayed by
        /// the host.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the data describing the key that was pressed.
        /// </param>
        /// <returns>
        /// True if the key press was read; otherwise, false.
        /// </returns>
        public abstract bool ReadKey(
            bool intercept,
            ref IClientData value
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        /// <summary>
        /// This method reads a single key press from the host.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key press so that it is not displayed by
        /// the host.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the data describing the key that was pressed.
        /// </param>
        /// <returns>
        /// True if the key press was read; otherwise, false.
        /// </returns>
        [Obsolete()]
        public abstract bool ReadKey(
            bool intercept,
            ref ConsoleKeyInfo value
            ); /* PRIMITIVE */
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IWriteHost Members
        /// <summary>
        /// This method writes a single character to the host output, optionally
        /// followed by a newline.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the character.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public abstract bool Write(
            char value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the host output the
        /// specified number of times.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public virtual bool Write(
            char value,
            int count
            )
        {
            CheckDisposed();

            return Write(value, count, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the host output the
        /// specified number of times, optionally followed by a newline.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the characters.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a single character to the host output the
        /// specified number of times, optionally followed by a newline, using
        /// the specified foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the characters.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a single character to the host output using the
        /// specified foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a string to the host output using the specified
        /// foreground color.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public virtual bool Write(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return Write(value, false, foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output using the specified
        /// foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a string to the host output, optionally followed
        /// by a newline.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the string.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public abstract bool Write(
            string value,
            bool newLine
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output, optionally followed
        /// by a newline, using the specified foreground color.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a string to the host output, optionally followed
        /// by a newline, using the specified foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a formatted list of name and value pairs to the
        /// host output, optionally followed by a newline, using the specified
        /// foreground and background colors.
        /// </summary>
        /// <param name="list">
        /// The list of name and value pairs to write.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the formatted output.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the formatted output was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a string followed by a newline to the host
        /// output using the specified foreground color.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public virtual bool WriteLine(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return Write(value, true, foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string followed by a newline to the host
        /// output using the specified foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// The backing field for the <see cref="Profile" /> property.
        /// </summary>
        private string profile = null;
        /// <summary>
        /// Gets or sets the name of the profile used to load and persist this
        /// host's saved settings.
        /// </summary>
        public virtual string Profile
        {
            get { CheckDisposed(); return profile; }
            set { CheckDisposed(); profile = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DefaultTitle" /> property.
        /// </summary>
        private string defaultTitle;
        /// <summary>
        /// Gets or sets the default window or console title used by this host
        /// when no more specific title has been set.
        /// </summary>
        public virtual string DefaultTitle
        {
            get { CheckDisposed(); return defaultTitle; }
            set { CheckDisposed(); defaultTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="HostCreateFlags" /> property.
        /// </summary>
        private HostCreateFlags hostCreateFlags;
        /// <summary>
        /// Gets or sets the flags that were (or will be) used to create and
        /// configure this host.
        /// </summary>
        public virtual HostCreateFlags HostCreateFlags
        {
            get { CheckDisposed(); return hostCreateFlags; }
            set { CheckDisposed(); hostCreateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host should attach to
        /// an existing host environment (for example, an existing console)
        /// rather than creating a new one.
        /// </summary>
        public virtual bool UseAttach
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.UseAttach, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.UseAttach, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether host operations that would
        /// normally be skipped or refused should instead be forced.
        /// </summary>
        public virtual bool UseForce
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.UseForce, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.UseForce, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host should suppress
        /// changes to the window or console title.
        /// </summary>
        public virtual bool NoTitle
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.NoTitle, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.NoTitle, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host should suppress
        /// changes to the window or console icon.
        /// </summary>
        public virtual bool NoIcon
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.NoIcon, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.NoIcon, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host should skip
        /// loading and saving its profile-based settings.
        /// </summary>
        public virtual bool NoProfile
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.NoProfile, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.NoProfile, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host should disable
        /// interactive cancellation (for example, the cancel key handler).
        /// </summary>
        public virtual bool NoCancel
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.NoCancel, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.NoCancel, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host echoes the input
        /// it reads back to its output.
        /// </summary>
        public virtual bool Echo
        {
            get { CheckDisposed(); return HasCreateFlags(HostCreateFlags.Echo, true); }
            set { CheckDisposed(); MaybeEnableCreateFlags(HostCreateFlags.Echo, value); }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a snapshot of this host's current state, with
        /// the amount of detail controlled by the supplied flags.
        /// </summary>
        /// <param name="detailFlags">
        /// The flags that select how much state detail is included in the
        /// result.
        /// </param>
        /// <returns>
        /// A list describing the requested host state.
        /// </returns>
        public abstract StringList QueryState(
            DetailFlags detailFlags
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits an audible tone through the host, when supported.
        /// </summary>
        /// <param name="frequency">
        /// The tone frequency, in hertz.
        /// </param>
        /// <param name="duration">
        /// The tone duration, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the tone was emitted; otherwise, false (for example, when
        /// the host does not support audible output).
        /// </returns>
        public abstract bool Beep(
            int frequency,
            int duration
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host currently has no pending
        /// interactive input or output activity.
        /// </summary>
        /// <returns>
        /// True if the host is idle; otherwise, false.
        /// </returns>
        public abstract bool IsIdle(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the host's display area, when supported.
        /// </summary>
        /// <returns>
        /// True if the display was cleared; otherwise, false.
        /// </returns>
        public abstract bool Clear(); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this host's configuration flags to their default
        /// values.
        /// </summary>
        /// <returns>
        /// True if the flags were reset; otherwise, false.
        /// </returns>
        public virtual bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the host's interactive input history.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public abstract ReturnCode ResetHistory(
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the current mode of one of the host's standard
        /// channels.
        /// </summary>
        /// <param name="channelType">
        /// The channel whose mode is to be retrieved (for example, input or
        /// output).
        /// </param>
        /// <param name="mode">
        /// Upon success, this is set to the current channel mode.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public abstract ReturnCode GetMode(
            ChannelType channelType,
            ref uint mode,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the mode of one of the host's standard channels.
        /// </summary>
        /// <param name="channelType">
        /// The channel whose mode is to be set (for example, input or output).
        /// </param>
        /// <param name="mode">
        /// The new channel mode to apply.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public abstract ReturnCode SetMode(
            ChannelType channelType,
            uint mode,
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens, or re-opens, the host's underlying interactive
        /// resources.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public abstract ReturnCode Open(
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the host's underlying interactive resources.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public abstract ReturnCode Close(
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discards any buffered host input and/or output without
        /// closing the host.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public abstract ReturnCode Discard(
            ref Result error
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the host to its initial state, reinitializing its
        /// interactive resources.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
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

        /// <summary>
        /// This method begins a named output section, allowing the host to
        /// group or visually delimit related output.
        /// </summary>
        /// <param name="name">
        /// The name of the section to begin.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the section, if any.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the section was begun; otherwise, false.
        /// </returns>
        public abstract bool BeginSection(
            string name,
            IClientData clientData
            ); /* PRIMITIVE */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends a named output section previously begun with
        /// <c>BeginSection</c>.
        /// </summary>
        /// <param name="name">
        /// The name of the section to end.  This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the section, if any.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the section was ended; otherwise, false.
        /// </returns>
        public abstract bool EndSection(
            string name,
            IClientData clientData
            ); /* PRIMITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this host has been disposed.
        /// </summary>
        public virtual bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this host is currently in the
        /// process of being disposed.  This property is not implemented; its get
        /// accessor always throws <see cref="NotImplementedException" />.
        /// </summary>
        public virtual bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                throw new NotImplementedException();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISynchronize Support Methods
        /// <summary>
        /// This method returns the identifier of the thread that currently
        /// holds the static lock, if any.
        /// </summary>
        /// <returns>
        /// The identifier of the thread holding the static lock, or zero if the
        /// lock is not currently held.
        /// </returns>
        private static long MaybeWhoHasStaticLock()
        {
            return Interlocked.CompareExchange(
                ref staticLockThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the calling thread as the holder of the static
        /// lock when the lock has just been acquired.
        /// </summary>
        /// <param name="locked">
        /// True if the static lock was acquired and the calling thread should
        /// be recorded as its holder; otherwise, false.
        /// </param>
        private static void MaybeSomebodyHasStaticLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref staticLockThreadId,
                    GlobalState.GetCurrentLockThreadId(), 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the record of which thread holds the static lock
        /// when the calling thread has just released it.
        /// </summary>
        /// <param name="locked">
        /// True if the static lock was held and is being released by the
        /// calling thread; otherwise, false.
        /// </param>
        private static void MaybeNobodyHasStaticLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref staticLockThreadId,
                    0, GlobalState.GetCurrentLockThreadId());
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the static lock without blocking.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this is set to true if the lock was acquired;
        /// otherwise, false.
        /// </param>
        private void PrivateStaticTryLock(
            ref bool locked
            )
        {
            if (staticSyncRoot == null)
                return;

            locked = Monitor.TryEnter(staticSyncRoot);
            MaybeSomebodyHasStaticLock(locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the static lock, waiting for the
        /// configured wait-lock timeout for it to become available.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this is set to true if the lock was acquired;
        /// otherwise, false.
        /// </param>
        private void PrivateStaticTryLockWithWait(
            ref bool locked
            )
        {
            if (staticSyncRoot == null)
                return;

            locked = Monitor.TryEnter(
                staticSyncRoot, ThreadOps.GetTimeout(
                null, null, TimeoutType.WaitLock));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the static lock, waiting up to the
        /// specified amount of time for it to become available.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time to wait, in milliseconds, for the lock to
        /// become available.
        /// </param>
        /// <param name="locked">
        /// Upon return, this is set to true if the lock was acquired;
        /// otherwise, false.
        /// </param>
        private void PrivateStaticTryLock(
            int timeout,
            ref bool locked
            )
        {
            if (staticSyncRoot == null)
                return;

            locked = Monitor.TryEnter(staticSyncRoot, timeout);
            MaybeSomebodyHasStaticLock(locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the static lock if it is currently held by the
        /// calling thread.
        /// </summary>
        /// <param name="locked">
        /// On input, indicates whether the lock is currently held and should be
        /// released.  Upon return, this is set to false if the lock was
        /// released.
        /// </param>
        private void PrivateStaticExitLock(
            ref bool locked
            )
        {
            if (staticSyncRoot == null)
                return;

            if (locked)
            {
                MaybeNobodyHasStaticLock(locked);
                Monitor.Exit(staticSyncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISynchronizeStatic Members
        /// <summary>
        /// This method attempts to acquire the static lock without blocking.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this is set to true if the lock was acquired;
        /// otherwise, false.
        /// </param>
        public virtual void StaticTryLock(
            ref bool locked
            )
        {
            CheckDisposed();

            PrivateStaticTryLock(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the static lock, waiting for a
        /// default amount of time for it to become available.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this is set to true if the lock was acquired;
        /// otherwise, false.
        /// </param>
        public virtual void StaticTryLockWithWait(
            ref bool locked
            )
        {
            CheckDisposed();

            PrivateStaticTryLockWithWait(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the static lock, waiting up to the
        /// specified amount of time for it to become available.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time to wait, in milliseconds, for the lock to
        /// become available.
        /// </param>
        /// <param name="locked">
        /// Upon return, this is set to true if the lock was acquired;
        /// otherwise, false.
        /// </param>
        public virtual void StaticTryLock(
            int timeout,
            ref bool locked
            )
        {
            CheckDisposed();

            PrivateStaticTryLock(timeout, ref locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the static lock if it was previously acquired.
        /// </summary>
        /// <param name="locked">
        /// On input, indicates whether the lock is currently held and should be
        /// released.  Upon return, this is set to false if the lock was
        /// released.
        /// </param>
        public virtual void StaticExitLock(
            ref bool locked
            )
        {
            CheckDisposed();

            PrivateStaticExitLock(ref locked);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources used by this host and suppresses
        /// finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// The backing field that indicates whether this host has been
        /// disposed.  This field backs the <see cref="Disposed" /> property.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an <see cref="InterpreterDisposedException" /> if
        /// this host has been disposed and the engine is configured to throw on
        /// access to disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Default));
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this host.  It implements
        /// the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// True if this method is being called explicitly via the
        /// <see cref="Dispose()" /> method; otherwise, false, indicating it is
        /// being called from the finalizer and only unmanaged resources should
        /// be released.
        /// </param>
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
        /// <summary>
        /// Finalizes an instance of the <see cref="Default" /> class, releasing
        /// any unmanaged resources held by this host.
        /// </summary>
        ~Default()
        {
            Dispose(false);
        }
        #endregion
    }
}
