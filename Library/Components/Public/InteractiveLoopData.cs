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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("4e7c2281-b5d0-415c-a909-cf607544cd36")]
    public sealed class InteractiveLoopData : IInteractiveLoopData
    {
        #region Private Constructors
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
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private bool debug;
        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IEnumerable<string> args;
        public IEnumerable<string> Args
        {
            get { return args; }
            set { args = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        internal ReturnCode code; /* ref */
        public ReturnCode Code
        {
            get { return code; }
            set { code = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BreakpointType breakpointType;
        public BreakpointType BreakpointType
        {
            get { return breakpointType; }
            set { breakpointType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string breakpointName;
        public string BreakpointName
        {
            get { return breakpointName; }
            set { breakpointName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IToken token;
        public IToken Token
        {
            get { return token; }
            set { token = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ITraceInfo traceInfo;
        public ITraceInfo TraceInfo
        {
            get { return traceInfo; }
            set { traceInfo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EngineFlags engineFlags;
        public EngineFlags EngineFlags
        {
            get { return engineFlags; }
            set { engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SubstitutionFlags substitutionFlags;
        public SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
            set { substitutionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventFlags eventFlags;
        public EventFlags EventFlags
        {
            get { return eventFlags; }
            set { eventFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ExpressionFlags expressionFlags;
        public ExpressionFlags ExpressionFlags
        {
            get { return expressionFlags; }
            set { expressionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private HeaderFlags headerFlags;
        public HeaderFlags HeaderFlags
        {
            get { return headerFlags; }
            set { headerFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentList arguments;
        public ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        internal bool exit; /* ref */
        public bool Exit
        {
            get { return exit; }
            set { exit = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
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
            targetLoopData.ClientData = sourceLoopData.ClientData;
            targetLoopData.Arguments = sourceLoopData.Arguments;
            targetLoopData.Exit = sourceLoopData.Exit;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods
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
            list.Add("clientData", FormatOps.WrapOrNull(clientData));
            list.Add("arguments",
                FormatOps.WrapOrNull(true, true, arguments));

            list.Add("exit", exit.ToString());

            return list.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
