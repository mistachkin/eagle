/*
 * InteractiveLoopData.cs --
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
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class carries the data that describes the state of an interactive
    /// loop, including the debugging mode, command-line arguments, return
    /// code, active breakpoint, token and trace information, the various
    /// engine and formatting flags, client data, arguments, and whether the
    /// loop should exit.  It implements <see cref="IInteractiveLoopData" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("4e7c2281-b5d0-415c-a909-cf607544cd36")]
    public sealed class InteractiveLoopData : IInteractiveLoopData
    {
        #region Private Constructors
        /// <summary>
        /// Constructs interactive loop data from the fully specified set of
        /// state parameters.  This is the most general constructor; the other
        /// constructors delegate to it.
        /// </summary>
        /// <param name="debug">
        /// Non-zero if the interactive loop is in debugging mode.
        /// </param>
        /// <param name="args">
        /// The command-line arguments associated with the interactive loop, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the interactive loop.
        /// </param>
        /// <param name="breakpointType">
        /// The type of the active breakpoint, if any.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the active breakpoint, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="token">
        /// The token associated with the interactive loop, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information associated with the interactive loop, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags in effect for the interactive loop.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags in effect for the interactive loop.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags in effect for the interactive loop.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags in effect for the interactive loop.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags that control which header information is displayed.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the interactive loop, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The argument list associated with the interactive loop, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="exit">
        /// Non-zero if the interactive loop should exit.
        /// </param>
        private InteractiveLoopData(
            bool debug,
            IEnumerable<string> args,
            ReturnCode code,
            BreakpointType breakpointType,
            string breakpointName,
            IToken token,
            ITraceInfo traceInfo,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            HeaderFlags headerFlags,
            IClientData clientData,
            ArgumentList arguments,
            bool exit
            )
        {
            this.kind = IdentifierKind.InteractiveLoopData;
            this.id = AttributeOps.GetObjectId(this);
            this.debug = debug;
            this.args = args;
            this.code = code;
            this.breakpointType = breakpointType;
            this.breakpointName = breakpointName;
            this.token = token;
            this.traceInfo = traceInfo;
            this.engineFlags = engineFlags;
            this.substitutionFlags = substitutionFlags;
            this.eventFlags = eventFlags;
            this.expressionFlags = expressionFlags;
            this.headerFlags = headerFlags;
            this.clientData = clientData;
            this.arguments = arguments;
            this.exit = exit;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs interactive loop data in a default, reset state.
        /// </summary>
        private InteractiveLoopData()
            : this(false, null, ReturnCode.Ok, BreakpointType.None, null,
                   null, null, EngineFlags.None, SubstitutionFlags.None,
                   EventFlags.None, ExpressionFlags.None, HeaderFlags.None,
                   null, null, false)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Copy Constructors
        /// <summary>
        /// Constructs interactive loop data as a copy of the specified
        /// interactive loop data.
        /// </summary>
        /// <param name="loopData">
        /// The interactive loop data to copy.  This parameter may be null.
        /// </param>
        private InteractiveLoopData(
            IInteractiveLoopData loopData
            )
            : this()
        {
            Copy(loopData, this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Copy Constructors
        /// <summary>
        /// Constructs interactive loop data as a copy of the specified
        /// interactive loop data, overriding its debugging mode.
        /// </summary>
        /// <param name="loopData">
        /// The interactive loop data to copy.  This parameter may be null.
        /// </param>
        /// <param name="debug">
        /// Non-zero if the interactive loop is in debugging mode.
        /// </param>
        //
        // NOTE: For Debugger class use only.
        //
        internal InteractiveLoopData(
            IInteractiveLoopData loopData,
            bool debug
            )
            : this(loopData)
        {
            this.debug = debug;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs interactive loop data as a copy of the specified
        /// interactive loop data, overriding its token, trace information, and
        /// header flags.
        /// </summary>
        /// <param name="loopData">
        /// The interactive loop data to copy.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// The token associated with the interactive loop, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information associated with the interactive loop, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags that control which header information is displayed.
        /// </param>
        //
        // NOTE: For Interpreter class use only.
        //
        internal InteractiveLoopData(
            IInteractiveLoopData loopData,
            IToken token,
            ITraceInfo traceInfo,
            HeaderFlags headerFlags
            )
            : this(loopData)
        {
            this.token = token;
            this.traceInfo = traceInfo;
            this.headerFlags = headerFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs interactive loop data as a copy of the specified
        /// interactive loop data, overriding its return code, token, trace
        /// information, and header flags.
        /// </summary>
        /// <param name="loopData">
        /// The interactive loop data to copy.  This parameter may be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the interactive loop.
        /// </param>
        /// <param name="token">
        /// The token associated with the interactive loop, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information associated with the interactive loop, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags that control which header information is displayed.
        /// </param>
        //
        // NOTE: For InteractiveOps.Commands.show() and
        //       _Tests.Default.TestDisposedWriteHeader() use only.
        //
        internal InteractiveLoopData(
            IInteractiveLoopData loopData,
            ReturnCode code,
            IToken token,
            ITraceInfo traceInfo,
            HeaderFlags headerFlags
            )
            : this(loopData)
        {
            this.code = code;
            this.token = token;
            this.traceInfo = traceInfo;
            this.headerFlags = headerFlags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constructors
        /// <summary>
        /// Constructs interactive loop data with the specified command-line
        /// arguments and all other state left at its default values.
        /// </summary>
        /// <param name="args">
        /// The command-line arguments associated with the interactive loop, if
        /// any.  This parameter may be null.
        /// </param>
        //
        // NOTE: For [debug shell], InteractiveLoop(), and ShellMainCore()
        //       use only.
        //
        internal InteractiveLoopData(
            IEnumerable<string> args
            )
            : this()
        {
            this.args = args;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs interactive loop data with the specified return code,
        /// breakpoint, header flags, client data, and arguments.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the interactive loop.
        /// </param>
        /// <param name="breakpointType">
        /// The type of the active breakpoint, if any.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the active breakpoint, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags that control which header information is displayed.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the interactive loop, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The argument list associated with the interactive loop, if any.
        /// This parameter may be null.
        /// </param>
        //
        // NOTE: For [debug break] use only.
        //
        internal InteractiveLoopData(
            ReturnCode code,
            BreakpointType breakpointType,
            string breakpointName,
            HeaderFlags headerFlags,
            IClientData clientData,
            ArgumentList arguments
            )
            : this()
        {
            this.code = code;
            this.breakpointType = breakpointType;
            this.breakpointName = breakpointName;
            this.headerFlags = headerFlags;
            this.clientData = clientData;
            this.arguments = arguments;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs interactive loop data with the specified return code,
        /// breakpoint, token, trace information, and engine, substitution,
        /// event, expression, and header flags.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the interactive loop.
        /// </param>
        /// <param name="breakpointType">
        /// The type of the active breakpoint, if any.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the active breakpoint, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="token">
        /// The token associated with the interactive loop, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information associated with the interactive loop, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags in effect for the interactive loop.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags in effect for the interactive loop.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags in effect for the interactive loop.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags in effect for the interactive loop.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags that control which header information is displayed.
        /// </param>
        //
        // NOTE: For use by Engine.CheckWatchpoints() only.
        //
        internal InteractiveLoopData(
            ReturnCode code,
            BreakpointType breakpointType,
            string breakpointName,
            IToken token,
            ITraceInfo traceInfo,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            HeaderFlags headerFlags
            )
            : this()
        {
            this.code = code;
            this.breakpointType = breakpointType;
            this.breakpointName = breakpointName;
            this.token = token;
            this.traceInfo = traceInfo;
            this.engineFlags = engineFlags;
            this.substitutionFlags = substitutionFlags;
            this.eventFlags = eventFlags;
            this.expressionFlags = expressionFlags;
            this.headerFlags = headerFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs interactive loop data with the specified return code,
        /// breakpoint, token, trace information, engine, substitution, event,
        /// expression, and header flags, client data, and arguments.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the interactive loop.
        /// </param>
        /// <param name="breakpointType">
        /// The type of the active breakpoint, if any.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the active breakpoint, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="token">
        /// The token associated with the interactive loop, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information associated with the interactive loop, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags in effect for the interactive loop.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags in effect for the interactive loop.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags in effect for the interactive loop.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags in effect for the interactive loop.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags that control which header information is displayed.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the interactive loop, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The argument list associated with the interactive loop, if any.
        /// This parameter may be null.
        /// </param>
        //
        // NOTE: For use by InteractiveOps.Commands._break() and
        //       Engine.CheckBreakpoints() only.
        //
        internal InteractiveLoopData(
            ReturnCode code,
            BreakpointType breakpointType,
            string breakpointName,
            IToken token,
            ITraceInfo traceInfo,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            HeaderFlags headerFlags,
            IClientData clientData,
            ArgumentList arguments
            )
            : this()
        {
            this.code = code;
            this.breakpointType = breakpointType;
            this.breakpointName = breakpointName;
            this.token = token;
            this.traceInfo = traceInfo;
            this.engineFlags = engineFlags;
            this.substitutionFlags = substitutionFlags;
            this.eventFlags = eventFlags;
            this.expressionFlags = expressionFlags;
            this.headerFlags = headerFlags;
            this.clientData = clientData;
            this.arguments = arguments;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new interactive loop data instance in a
        /// default, reset state.
        /// </summary>
        /// <returns>
        /// The newly created interactive loop data instance.
        /// </returns>
        //
        // WARNING: For use by the StatusFormOps.CreateInteractiveLoopThread
        //          method only.
        //
        public static IInteractiveLoopData Create()
        {
            return new InteractiveLoopData();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of this interactive loop data.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this interactive loop data.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The kind of identifier represented by this interactive loop data.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this interactive
        /// loop data.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier of this interactive loop data.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier of this interactive loop data.
        /// </summary>
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with the interactive loop, if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with the interactive loop,
        /// if any.
        /// </summary>
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The group of this interactive loop data.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this interactive loop data.
        /// </summary>
        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The description of this interactive loop data.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this interactive loop data.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Non-zero if the interactive loop is in debugging mode.
        /// </summary>
        private bool debug;
        /// <summary>
        /// Gets or sets a value indicating whether the interactive loop is in
        /// debugging mode.
        /// </summary>
        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The command-line arguments associated with the interactive loop, if
        /// any.
        /// </summary>
        private IEnumerable<string> args;
        /// <summary>
        /// Gets or sets the command-line arguments associated with the
        /// interactive loop, if any.
        /// </summary>
        public IEnumerable<string> Args
        {
            get { return args; }
            set { args = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The return code associated with the interactive loop.
        /// </summary>
        private ReturnCode code;
        /// <summary>
        /// Gets or sets the return code associated with the interactive loop.
        /// </summary>
        public ReturnCode Code
        {
            get { return code; }
            set { code = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The type of the active breakpoint, if any.
        /// </summary>
        private BreakpointType breakpointType;
        /// <summary>
        /// Gets or sets the type of the active breakpoint, if any.
        /// </summary>
        public BreakpointType BreakpointType
        {
            get { return breakpointType; }
            set { breakpointType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the active breakpoint, if any.
        /// </summary>
        private string breakpointName;
        /// <summary>
        /// Gets or sets the name of the active breakpoint, if any.
        /// </summary>
        public string BreakpointName
        {
            get { return breakpointName; }
            set { breakpointName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The token associated with the interactive loop, if any.
        /// </summary>
        private IToken token;
        /// <summary>
        /// Gets or sets the token associated with the interactive loop, if any.
        /// </summary>
        public IToken Token
        {
            get { return token; }
            set { token = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The trace information associated with the interactive loop, if any.
        /// </summary>
        private ITraceInfo traceInfo;
        /// <summary>
        /// Gets or sets the trace information associated with the interactive
        /// loop, if any.
        /// </summary>
        public ITraceInfo TraceInfo
        {
            get { return traceInfo; }
            set { traceInfo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The engine flags in effect for the interactive loop.
        /// </summary>
        private EngineFlags engineFlags;
        /// <summary>
        /// Gets or sets the engine flags in effect for the interactive loop.
        /// </summary>
        public EngineFlags EngineFlags
        {
            get { return engineFlags; }
            set { engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The substitution flags in effect for the interactive loop.
        /// </summary>
        private SubstitutionFlags substitutionFlags;
        /// <summary>
        /// Gets or sets the substitution flags in effect for the interactive
        /// loop.
        /// </summary>
        public SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
            set { substitutionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The event flags in effect for the interactive loop.
        /// </summary>
        private EventFlags eventFlags;
        /// <summary>
        /// Gets or sets the event flags in effect for the interactive loop.
        /// </summary>
        public EventFlags EventFlags
        {
            get { return eventFlags; }
            set { eventFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The expression flags in effect for the interactive loop.
        /// </summary>
        private ExpressionFlags expressionFlags;
        /// <summary>
        /// Gets or sets the expression flags in effect for the interactive
        /// loop.
        /// </summary>
        public ExpressionFlags ExpressionFlags
        {
            get { return expressionFlags; }
            set { expressionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The header flags that control which header information is displayed.
        /// </summary>
        private HeaderFlags headerFlags;
        /// <summary>
        /// Gets or sets the header flags that control which header information
        /// is displayed.
        /// </summary>
        public HeaderFlags HeaderFlags
        {
            get { return headerFlags; }
            set { headerFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The detail flags that control how much detail is displayed.
        /// </summary>
        private DetailFlags detailFlags;
        /// <summary>
        /// Gets or sets the detail flags that control how much detail is
        /// displayed.
        /// </summary>
        public DetailFlags DetailFlags
        {
            get { return detailFlags; }
            set { detailFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The argument list associated with the interactive loop, if any.
        /// </summary>
        private ArgumentList arguments;
        /// <summary>
        /// Gets or sets the argument list associated with the interactive loop,
        /// if any.
        /// </summary>
        public ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the interactive loop should exit.
        /// </summary>
        private bool exit;
        /// <summary>
        /// Gets or sets a value indicating whether the interactive loop should
        /// exit.
        /// </summary>
        public bool Exit
        {
            get { return exit; }
            set { exit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forces the interactive loop to exit, emitting a trace
        /// message that records the change.
        /// </summary>
        public void SetExit()
        {
            exit = true;

            TraceOps.DebugTrace(
                "SetExit: forced on via interactive loop",
                typeof(InteractiveLoopData).Name,
                TracePriority.InteractiveError);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forces the return code of the interactive loop to the
        /// specified value, emitting a trace message that records the change.
        /// </summary>
        /// <param name="code">
        /// The new return code for the interactive loop.
        /// </param>
        public void SetCode(
            ReturnCode code
            )
        {
            this.code = code;

            TraceOps.DebugTrace(String.Format(
                "SetCode: forced {0} via interactive loop",
                FormatOps.WrapOrNull(code)),
                typeof(InteractiveLoopData).Name,
                TracePriority.InteractiveDebug);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method copies the state from one interactive loop data
        /// instance to another.
        /// </summary>
        /// <param name="sourceLoopData">
        /// The interactive loop data to copy from.  This parameter may be
        /// null, in which case nothing is copied.
        /// </param>
        /// <param name="targetLoopData">
        /// The interactive loop data to copy to.  This parameter may be null,
        /// in which case nothing is copied.
        /// </param>
        private static void Copy(
            IInteractiveLoopData sourceLoopData,
            IInteractiveLoopData targetLoopData
            )
        {
            if ((sourceLoopData == null) || (targetLoopData == null))
                return;

            targetLoopData.Debug = sourceLoopData.Debug;
            targetLoopData.Args = sourceLoopData.Args;
            targetLoopData.Code = sourceLoopData.Code;
            targetLoopData.BreakpointType = sourceLoopData.BreakpointType;
            targetLoopData.BreakpointName = sourceLoopData.BreakpointName;
            targetLoopData.Token = sourceLoopData.Token;
            targetLoopData.TraceInfo = sourceLoopData.TraceInfo;
            targetLoopData.EngineFlags = sourceLoopData.EngineFlags;
            targetLoopData.SubstitutionFlags = sourceLoopData.SubstitutionFlags;
            targetLoopData.EventFlags = sourceLoopData.EventFlags;
            targetLoopData.ExpressionFlags = sourceLoopData.ExpressionFlags;
            targetLoopData.HeaderFlags = sourceLoopData.HeaderFlags;
            targetLoopData.DetailFlags = sourceLoopData.DetailFlags;
            targetLoopData.ClientData = sourceLoopData.ClientData;
            targetLoopData.Arguments = sourceLoopData.Arguments;
            targetLoopData.Exit = sourceLoopData.Exit;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods
        /// <summary>
        /// This method returns a detailed string representation of this
        /// interactive loop data, suitable for use in trace output.
        /// </summary>
        /// <returns>
        /// A string containing the name and value of each piece of interactive
        /// loop state.
        /// </returns>
        internal string ToTraceString()
        {
            IStringList list = new StringPairList();

            list.Add("debug", debug.ToString());
            list.Add("args",
                FormatOps.WrapArgumentsOrNull(true, true, args));

            list.Add("code", code.ToString());
            list.Add("breakpointType", breakpointType.ToString());
            list.Add("breakpointName", FormatOps.WrapOrNull(breakpointName));
            list.Add("token", (token != null).ToString());
            list.Add("traceInfo", (traceInfo != null).ToString());
            list.Add("engineFlags", FormatOps.WrapOrNull(engineFlags));

            list.Add("substitutionFlags",
                FormatOps.WrapOrNull(substitutionFlags));
            list.Add("eventFlags", FormatOps.WrapOrNull(eventFlags));

            list.Add("expressionFlags",
                FormatOps.WrapOrNull(expressionFlags));

            list.Add("headerFlags", FormatOps.WrapOrNull(headerFlags));
            list.Add("detailFlags", FormatOps.WrapOrNull(detailFlags));
            list.Add("clientData", FormatOps.WrapOrNull(clientData));
            list.Add("arguments",
                FormatOps.WrapOrNull(true, true, arguments));

            list.Add("exit", exit.ToString());

            return list.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this interactive
        /// loop data.
        /// </summary>
        /// <returns>
        /// The name of this interactive loop data, or an empty string if it
        /// has no name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
