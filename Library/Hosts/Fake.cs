/*
 * Fake.cs --
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
    [ObjectId("f894f056-0b4f-4337-a379-66fc7d79aee0")]
    public class Fake :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IHost, IDisposable, IMaybeDisposed
    {
        #region Private Constructors
        private Fake()
        {
            kind = IdentifierKind.Host;
            id = AttributeOps.GetObjectId(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Fake(
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
        public virtual string Name /* EXEMPT */
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind /* EXEMPT */
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id /* EXEMPT */
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group /* EXEMPT */
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description /* EXEMPT */
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData /* EXEMPT */
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        public virtual ReturnCode BeginProcessing(
            int levels,
            ref string text,
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode EndProcessing(
            int levels,
            ref string text,
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode DoneProcessing(
            int levels,
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        private string title;
        public virtual string Title /* EXEMPT */
        {
            get { CheckDisposed(); return title; }
            set { CheckDisposed(); title = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool RefreshTitle()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool IsInputRedirected()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Prompt(
            PromptType type,
            ref PromptFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool IsOpen()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Pause()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Flush()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual HeaderFlags GetHeaderFlags()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual HostFlags GetHostFlags()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int ReadLevels
        {
            get
            {
                CheckDisposed();

                throw new NotImplementedException();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int WriteLevels
        {
            get
            {
                CheckDisposed();

                throw new NotImplementedException();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ReadLine(
            ref string value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteLine()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteLine(
            string value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteResultLine(
            ReturnCode code,
            Result result
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteResultLine(
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IFileSystemHost Members
        private HostStreamFlags streamFlags = HostStreamFlags.Invalid;
        public virtual HostStreamFlags StreamFlags /* EXEMPT */
        {
            get { CheckDisposed(); return streamFlags; }
            set { CheckDisposed(); streamFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode GetStream(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode GetData(
            string name,
            DataFlags dataFlags,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result result
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcessHost Members
        private bool canExit;
        public virtual bool CanExit /* EXEMPT */
        {
            get { CheckDisposed(); return canExit; }
            set { CheckDisposed(); canExit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool canForceExit;
        public virtual bool CanForceExit /* EXEMPT */
        {
            get { CheckDisposed(); return canForceExit; }
            set { CheckDisposed(); canForceExit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool exiting;
        public virtual bool Exiting /* EXEMPT */
        {
            get { CheckDisposed(); return exiting; }
            set { CheckDisposed(); exiting = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadHost Members
        public virtual ReturnCode CreateThread(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode CreateThread(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode QueueWorkItem(
            ThreadStart callback,
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode QueueWorkItem(
            WaitCallback callback,
            object state,
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Sleep(
            int milliseconds
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Yield()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        public virtual Stream DefaultIn
        {
            get
            {
                CheckDisposed();

                throw new NotImplementedException();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Stream DefaultOut
        {
            get
            {
                CheckDisposed();

                throw new NotImplementedException();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Stream DefaultError
        {
            get
            {
                CheckDisposed();

                throw new NotImplementedException();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private Stream input;
        public virtual Stream In /* EXEMPT */
        {
            get { CheckDisposed(); return input; }
            set { CheckDisposed(); input = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Stream output;
        public virtual Stream Out /* EXEMPT */
        {
            get { CheckDisposed(); return output; }
            set { CheckDisposed(); output = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Stream error;
        public virtual Stream Error /* EXEMPT */
        {
            get { CheckDisposed(); return error; }
            set { CheckDisposed(); error = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Encoding inputEncoding;
        public virtual Encoding InputEncoding /* EXEMPT */
        {
            get { CheckDisposed(); return inputEncoding; }
            set { CheckDisposed(); inputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Encoding outputEncoding;
        public virtual Encoding OutputEncoding /* EXEMPT */
        {
            get { CheckDisposed(); return outputEncoding; }
            set { CheckDisposed(); outputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Encoding errorEncoding;
        public virtual Encoding ErrorEncoding /* EXEMPT */
        {
            get { CheckDisposed(); return errorEncoding; }
            set { CheckDisposed(); errorEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ResetIn()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ResetOut()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ResetError()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool IsOutputRedirected()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool IsErrorRedirected()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetupChannels()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        public virtual IHost Clone() /* EXEMPT */
        {
            CheckDisposed();

            return Clone(null);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual IHost Clone( /* EXEMPT */
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return new Fake(new HostData(
                Name, Group, Description, ClientData, typeof(Fake).Name,
                interpreter, null, Profile, HostCreateFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Cancel(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Exit(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebugLine()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebugLine(
            string value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            char value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            string value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteErrorLine()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteErrorLine(
            string value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            char value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            string value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            ReturnCode code,
            Result result,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            ReturnCode code,
            Result result,
            bool raw,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            ReturnCode code,
            Result result,
            int errorLine,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            ReturnCode code,
            Result result,
            int errorLine,
            bool raw,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            string prefix,
            ReturnCode code,
            Result result,
            int errorLine,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInformationHost Members
        public virtual bool SavePosition()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool RestorePosition(
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteAnnouncementInfo(
            Interpreter interpreter,
            BreakpointType breakpointType,
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallFrameInfo(
            Interpreter interpreter,
            ICallFrame frame,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallStack(
            Interpreter interpreter,
            CallStack callStack,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallStack(
            Interpreter interpreter,
            CallStack callStack,
            int limit,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallStackInfo(
            Interpreter interpreter,
            CallStack callStack,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteCallStackInfo(
            Interpreter interpreter,
            CallStack callStack,
            int limit,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebuggerInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebuggerInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteHostInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteHostInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteInterpreterInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteInterpreterInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteEngineInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteEngineInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteEntityInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteEntityInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteStackInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteStackInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteControlInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteControlInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteTestInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteTestInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteTokenInfo(
            Interpreter interpreter,
            IToken token,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteTraceInfo(
            Interpreter interpreter,
            ITraceInfo traceInfo,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteVariableInfo(
            Interpreter interpreter,
            IVariable variable,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteObjectInfo(
            Interpreter interpreter,
            IObject @object,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteComplaintInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteComplaintInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

#if HISTORY
        public virtual bool WriteHistoryInfo(
            Interpreter interpreter,
            IHistoryFilter historyFilter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        public virtual void WriteHeader(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            Result result
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void WriteFooter(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            Result result
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBoxHost Members
        public virtual bool BeginBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool EndBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteBox(
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

            throw new NotImplementedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        private bool noColor;
        public virtual bool NoColor /* EXEMPT */
        {
            get { CheckDisposed(); return noColor; }
            set { CheckDisposed(); noColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ResetColors()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool GetColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool AdjustColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetForegroundColor(
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetBackgroundColor(
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetColors(
            bool foreground,
            bool background,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode GetColors(
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

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode SetColors(
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

            throw new NotImplementedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPositionHost Members
        public virtual bool ResetPosition()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool GetPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool GetDefaultPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetDefaultPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISizeHost Members
        public virtual bool ResetSize(
            HostSizeType hostSizeType
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool GetSize(
            HostSizeType hostSizeType,
            ref int width,
            ref int height
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetSize(
            HostSizeType hostSizeType,
            int width,
            int height
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IReadHost Members
        public virtual bool Read(
            ref int value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ReadKey(
            bool intercept,
            ref IClientData value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        [Obsolete()]
        public virtual bool ReadKey(
            bool intercept,
            ref ConsoleKeyInfo value
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWriteHost Members
        public virtual bool Write(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value,
            int count
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value,
            int count,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();


            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteFormat(
            StringPairList list,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteLine(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteLine(
            string value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHost Members
        private string profile;
        public virtual string Profile /* EXEMPT */
        {
            get { CheckDisposed(); return profile; }
            set { CheckDisposed(); profile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private HostCreateFlags hostCreateFlags;
        public virtual HostCreateFlags HostCreateFlags /* EXEMPT */
        {
            get { CheckDisposed(); return hostCreateFlags; }
            set { CheckDisposed(); hostCreateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string defaultTitle;
        public virtual string DefaultTitle /* EXEMPT */
        {
            get { CheckDisposed(); return defaultTitle; }
            set { CheckDisposed(); defaultTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool useAttach;
        public virtual bool UseAttach /* EXEMPT */
        {
            get { CheckDisposed(); return useAttach; }
            set { CheckDisposed(); useAttach = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noTitle;
        public virtual bool NoTitle /* EXEMPT */
        {
            get { CheckDisposed(); return noTitle; }
            set { CheckDisposed(); noTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noIcon;
        public virtual bool NoIcon /* EXEMPT */
        {
            get { CheckDisposed(); return noIcon; }
            set { CheckDisposed(); noIcon = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noProfile;
        public virtual bool NoProfile /* EXEMPT */
        {
            get { CheckDisposed(); return noProfile; }
            set { CheckDisposed(); noProfile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noCancel;
        public virtual bool NoCancel /* EXEMPT */
        {
            get { CheckDisposed(); return noCancel; }
            set { CheckDisposed(); noCancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool echo;
        public virtual bool Echo /* EXEMPT */
        {
            get { CheckDisposed(); return echo; }
            set { CheckDisposed(); echo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Beep(
            int frequency,
            int duration
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool IsIdle()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Clear()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ResetHostFlags()
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode ResetHistory(
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode GetMode(
            ChannelType channelType,
            ref uint mode,
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode SetMode(
            ChannelType channelType,
            uint mode,
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Close(
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Discard(
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool EndSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            throw new NotImplementedException();
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
        public virtual void Dispose()
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
                throw new InterpreterDisposedException(typeof(Fake));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Fake()
        {
            Dispose(false);
        }
        #endregion
    }
}
