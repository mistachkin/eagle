/*
 * Null.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Hosts
{
    [ObjectId("acbef62d-d8c5-429c-87aa-9500d91e7a7c")]
    public sealed class Null :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IHost, IDisposable, IMaybeDisposed
    {
        #region Private Constructors
        private Null()
        {
            kind = IdentifierKind.Host;
            id = AttributeOps.GetObjectId(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Null(
            IHostData hostData
            )
            : this()
        {
            if (hostData != null)
            {
                name = hostData.Name;
                group = hostData.Group;
                description = hostData.Description;
                clientData = hostData.ClientData;
                profile = hostData.Profile;
                hostCreateFlags = hostData.HostCreateFlags;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        public ReturnCode BeginProcessing(
            int levels,
            ref string text,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode EndProcessing(
            int levels,
            ref string text,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode DoneProcessing(
            int levels,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private string title;
        public string Title
        {
            get { CheckDisposed(); return title; }
            set { CheckDisposed(); title = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool RefreshTitle()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsInputRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Prompt(
            PromptType type,
            ref PromptFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsOpen()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Pause()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Flush()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public HeaderFlags GetHeaderFlags()
        {
            CheckDisposed();

            return HeaderFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        public HostFlags GetHostFlags()
        {
            CheckDisposed();

            return HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        public int ReadLevels
        {
            get
            {
                CheckDisposed();

                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int WriteLevels
        {
            get
            {
                CheckDisposed();

                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReadLine(
            ref string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Write(
            char value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Write(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteLine()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteLine(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteResultLine(
            ReturnCode code,
            Result result
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteResultLine(
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IFileSystemHost Members
        private HostStreamFlags streamFlags = HostStreamFlags.Invalid;
        public HostStreamFlags StreamFlags
        {
            get { CheckDisposed(); return streamFlags; }
            set { CheckDisposed(); streamFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode GetStream(
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
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode GetData(
            string name,
            DataFlags dataFlags,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result result
            )
        {
            CheckDisposed();

            result = "not implemented";
            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcessHost Members
        private bool canExit;
        public bool CanExit
        {
            get { CheckDisposed(); return canExit; }
            set { CheckDisposed(); canExit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool canForceExit;
        public bool CanForceExit
        {
            get { CheckDisposed(); return canForceExit; }
            set { CheckDisposed(); canForceExit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool exiting;
        public bool Exiting
        {
            get { CheckDisposed(); return exiting; }
            set { CheckDisposed(); exiting = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadHost Members
        public ReturnCode CreateThread(
            ThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode CreateThread(
            ParameterizedThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode QueueWorkItem(
            ThreadStart callback,
            QueueFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode QueueWorkItem(
            WaitCallback callback,
            object state,
            QueueFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Sleep(
            int milliseconds
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Yield()
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        public Stream DefaultIn
        {
            get { CheckDisposed(); return input; }
        }

        ///////////////////////////////////////////////////////////////////////

        public Stream DefaultOut
        {
            get { CheckDisposed(); return output; }
        }

        ///////////////////////////////////////////////////////////////////////

        public Stream DefaultError
        {
            get { CheckDisposed(); return error; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Stream input;
        public Stream In
        {
            get { CheckDisposed(); return input; }
            set { CheckDisposed(); input = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Stream output;
        public Stream Out
        {
            get { CheckDisposed(); return output; }
            set { CheckDisposed(); output = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Stream error;
        public Stream Error
        {
            get { CheckDisposed(); return error; }
            set { CheckDisposed(); error = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Encoding inputEncoding;
        public Encoding InputEncoding
        {
            get { CheckDisposed(); return inputEncoding; }
            set { CheckDisposed(); inputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Encoding outputEncoding;
        public Encoding OutputEncoding
        {
            get { CheckDisposed(); return outputEncoding; }
            set { CheckDisposed(); outputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Encoding errorEncoding;
        public Encoding ErrorEncoding
        {
            get { CheckDisposed(); return errorEncoding; }
            set { CheckDisposed(); errorEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ResetIn()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ResetOut()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ResetError()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsOutputRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsErrorRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetupChannels()
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        public IHost Clone()
        {
            CheckDisposed();

            return Clone(null);
        }

        ///////////////////////////////////////////////////////////////////////

        public IHost Clone(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return new Null(new HostData(
                Name, Group, Description, ClientData, typeof(Null).Name,
                interpreter, null, Profile, HostCreateFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        public HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            return HostTestFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Cancel(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Exit(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteDebugLine()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteDebugLine(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteDebug(
            char value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteDebug(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteDebug(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteDebug(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteDebug(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteDebug(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteDebug(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteErrorLine()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteErrorLine(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteError(
            char value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteError(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteError(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteError(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteError(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteError(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteError(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteResult(
            ReturnCode code,
            Result result,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteResult(
            ReturnCode code,
            Result result,
            bool raw,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteResult(
            ReturnCode code,
            Result result,
            int errorLine,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteResult(
            ReturnCode code,
            Result result,
            int errorLine,
            bool raw,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteResult(
            string prefix,
            ReturnCode code,
            Result result,
            int errorLine,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteResult(
            string prefix,
            ReturnCode code,
            Result result,
            int errorLine,
            bool raw,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInformationHost Members
        public bool SavePosition()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool RestorePosition(
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteAnnouncementInfo(
            Interpreter interpreter,
            BreakpointType breakpointType,
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteAnnouncementInfo(
            Interpreter interpreter,
            BreakpointType breakpointType,
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteArgumentInfo(
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

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteArgumentInfo(
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

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteCallFrame(
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

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteCallFrameInfo(
            Interpreter interpreter,
            ICallFrame frame,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteCallFrameInfo(
            Interpreter interpreter,
            ICallFrame frame,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteCallStack(
            Interpreter interpreter,
            CallStack callStack,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteCallStack(
            Interpreter interpreter,
            CallStack callStack,
            int limit,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteCallStackInfo(
            Interpreter interpreter,
            CallStack callStack,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteCallStackInfo(
            Interpreter interpreter,
            CallStack callStack,
            int limit,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteCallStackInfo(
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

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteDebuggerInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteDebuggerInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteFlagInfo(
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

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteFlagInfo(
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

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteHostInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteHostInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteInterpreterInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteInterpreterInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteEngineInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteEngineInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteEntityInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteEntityInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteStackInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteStackInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteControlInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteControlInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteTestInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteTestInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteTokenInfo(
            Interpreter interpreter,
            IToken token,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteTokenInfo(
            Interpreter interpreter,
            IToken token,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteTraceInfo(
            Interpreter interpreter,
            ITraceInfo traceInfo,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteTraceInfo(
            Interpreter interpreter,
            ITraceInfo traceInfo,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteVariableInfo(
            Interpreter interpreter,
            IVariable variable,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteVariableInfo(
            Interpreter interpreter,
            IVariable variable,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteObjectInfo(
            Interpreter interpreter,
            IObject @object,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteObjectInfo(
            Interpreter interpreter,
            IObject @object,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteComplaintInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteComplaintInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if HISTORY
        public bool WriteHistoryInfo(
            Interpreter interpreter,
            IHistoryFilter historyFilter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteHistoryInfo(
            Interpreter interpreter,
            IHistoryFilter historyFilter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public bool WriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteAllResultInfo(
            ReturnCode code,
            Result result,
            int errorLine,
            Result previousResult,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteAllResultInfo(
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

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteResultInfo(
            string name,
            ReturnCode code,
            Result result,
            int errorLine,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteResultInfo(
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

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        public void WriteHeader(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            Result result
            )
        {
            CheckDisposed();
        }

        ///////////////////////////////////////////////////////////////////////

        public void WriteFooter(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            Result result
            )
        {
            CheckDisposed();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBoxHost Members
        public bool BeginBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool EndBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        private bool noColor;
        public bool NoColor
        {
            get { CheckDisposed(); return noColor; }
            set { CheckDisposed(); noColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ResetColors()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AdjustColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetForegroundColor(
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetBackgroundColor(
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetColors(
            bool foreground,
            bool background,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode GetColors(
            string theme, /* RESERVED */
            string name,
            bool foreground,
            bool background,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode SetColors(
            string theme, /* RESERVED */
            string name,
            bool foreground,
            bool background,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPositionHost Members
        public bool ResetPosition()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetDefaultPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetDefaultPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISizeHost Members
        public bool ResetSize(
            HostSizeType hostSizeType
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetSize(
            HostSizeType hostSizeType,
            ref int width,
            ref int height
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetSize(
            HostSizeType hostSizeType,
            int width,
            int height
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IReadHost Members
        public bool Read(
            ref int value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReadKey(
            bool intercept,
            ref IClientData value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        [Obsolete()]
        public bool ReadKey(
            bool intercept,
            ref ConsoleKeyInfo value
            )
        {
            CheckDisposed();

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWriteHost Members
        public bool Write(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Write(
            char value,
            int count
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Write(
            char value,
            int count,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Write(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();


            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Write(
            char value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Write(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Write(
            string value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Write(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Write(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Write(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteFormat(
            StringPairList list,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteLine(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteLine(
            string value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHost Members
        private string profile;
        public string Profile
        {
            get { CheckDisposed(); return profile; }
            set { CheckDisposed(); profile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private HostCreateFlags hostCreateFlags;
        public HostCreateFlags HostCreateFlags
        {
            get { CheckDisposed(); return hostCreateFlags; }
            set { CheckDisposed(); hostCreateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string defaultTitle;
        public string DefaultTitle
        {
            get { CheckDisposed(); return defaultTitle; }
            set { CheckDisposed(); defaultTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool useAttach;
        public bool UseAttach
        {
            get { CheckDisposed(); return useAttach; }
            set { CheckDisposed(); useAttach = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noTitle;
        public bool NoTitle
        {
            get { CheckDisposed(); return noTitle; }
            set { CheckDisposed(); noTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noIcon;
        public bool NoIcon
        {
            get { CheckDisposed(); return noIcon; }
            set { CheckDisposed(); noIcon = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noProfile;
        public bool NoProfile
        {
            get { CheckDisposed(); return noProfile; }
            set { CheckDisposed(); noProfile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noCancel;
        public bool NoCancel
        {
            get { CheckDisposed(); return noCancel; }
            set { CheckDisposed(); noCancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool echo;
        public bool Echo
        {
            get { CheckDisposed(); return echo; }
            set { CheckDisposed(); echo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Beep(
            int frequency,
            int duration
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsIdle()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Clear()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ResetHostFlags()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ResetHistory(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode GetMode(
            ChannelType channelType,
            ref uint mode,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode SetMode(
            ChannelType channelType,
            uint mode,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Close(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Discard(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool EndSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public bool Disposed
        {
            get { return disposed; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Disposing
        {
            get { throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Null));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(bool disposing)
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

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Null()
        {
            Dispose(false);
        }
        #endregion
    }
}
