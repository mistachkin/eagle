/*
 * Wrapper.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Hosts
{
    [ObjectId("4fc58cc4-a6b5-4a16-94c7-d5b22c722687")]
    public class Wrapper :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IHost, IDisposable, IMaybeDisposed
    {
        #region Private Data
        //
        // NOTE: The wrapped host that is used to provide the implementations
        //       for all the IHost interface members.
        //
        private IHost baseHost;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This boolean field will be non-zero if the wrapped host is
        //       supposed to be "owned" by us (i.e. and must be disposed).
        //
        private bool baseHostOwned;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        protected internal Wrapper(
            IHostData hostData,
            IHost baseHost,
            bool baseHostOwned
            )
        {
            this.baseHost = baseHost;
            this.baseHostOwned = baseHostOwned;

            ///////////////////////////////////////////////////////////////////

            //
            // BUGFIX: All of these properties require the base host to be
            //         valid.  This issue was found by Coverity.
            //
            if ((hostData != null) && (baseHost != null))
            {
                this.Kind = hostData.Kind;
                this.Id = hostData.Id;
                this.Name = hostData.Name;
                this.Group = hostData.Group;
                this.Description = hostData.Description;
                this.ClientData = hostData.ClientData;
                this.Profile = hostData.Profile;
                this.HostCreateFlags = hostData.HostCreateFlags;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        public virtual IHost BaseHost
        {
            get { CheckDisposed(); return baseHost; }
            set { CheckDisposed(); baseHost = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool BaseHostOwned
        {
            get { CheckDisposed(); return baseHostOwned; }
            set { CheckDisposed(); baseHostOwned = value; }
        }
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

            return baseHost.BeginBox(name, list, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool EndBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return baseHost.EndBox(name, list, clientData);
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

            return baseHost.WriteBox(
                name, value, clientData, newLine, restore, ref left, ref top);
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

            return baseHost.WriteBox(
                name, value, clientData, minimumLength, newLine, restore,
                ref left, ref top);
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

            return baseHost.WriteBox(
                name, value, clientData, newLine, restore, ref left, ref top,
                foregroundColor, backgroundColor);
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

            return baseHost.WriteBox(
                name, value, clientData, minimumLength, newLine, restore,
                ref left, ref top, foregroundColor, backgroundColor);
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

            return baseHost.WriteBox(
                name, value, clientData, newLine, restore, ref left, ref top,
                foregroundColor, backgroundColor, boxForegroundColor,
                boxBackgroundColor);
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

            return baseHost.WriteBox(
                name, value, clientData, minimumLength, newLine, restore,
                ref left, ref top, foregroundColor, backgroundColor,
                boxForegroundColor, boxBackgroundColor);
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

            return baseHost.WriteBox(
                name, list, clientData, newLine, restore, ref left, ref top);
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

            return baseHost.WriteBox(
                name, list, clientData, minimumLength, newLine, restore,
                ref left, ref top);
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

            return baseHost.WriteBox(
                name, list, clientData, newLine, restore, ref left, ref top,
                foregroundColor, backgroundColor);
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

            return baseHost.WriteBox(
                name, list, clientData, minimumLength, newLine, restore,
                ref left, ref top, foregroundColor, backgroundColor);
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

            return baseHost.WriteBox(
                name, list, clientData, newLine, restore, ref left, ref top,
                foregroundColor, backgroundColor, boxForegroundColor,
                boxBackgroundColor);
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

            return baseHost.WriteBox(
                name, list, clientData, minimumLength, newLine, restore,
                ref left, ref top, foregroundColor, backgroundColor,
                boxForegroundColor, boxBackgroundColor);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        public virtual bool NoColor
        {
            get { CheckDisposed(); return baseHost.NoColor; }
            set { CheckDisposed(); baseHost.NoColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ResetColors()
        {
            CheckDisposed();

            return baseHost.ResetColors();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool GetColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return baseHost.GetColors(
                ref foregroundColor, ref backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool AdjustColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return baseHost.AdjustColors(
                ref foregroundColor, ref backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetForegroundColor(
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return baseHost.SetForegroundColor(foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetBackgroundColor(
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return baseHost.SetBackgroundColor(backgroundColor);
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

            return baseHost.SetColors(
                foreground, background, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode GetColors(
            string theme,
            string name,
            bool foreground,
            bool background,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.GetColors(
                theme, name, foreground, background, ref foregroundColor,
                ref backgroundColor, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode SetColors(
            string theme,
            string name,
            bool foreground,
            bool background,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.SetColors(
                theme, name, foreground, background, foregroundColor,
                backgroundColor, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPositionHost Members
        public virtual bool ResetPosition()
        {
            CheckDisposed();

            return baseHost.ResetPosition();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool GetPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return baseHost.GetPosition(ref left, ref top);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            return baseHost.SetPosition(left, top);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool GetDefaultPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return baseHost.GetDefaultPosition(ref left, ref top);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetDefaultPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            return baseHost.SetDefaultPosition(left, top);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISizeHost Members
        public virtual bool ResetSize(
            HostSizeType hostSizeType
            )
        {
            CheckDisposed();

            return baseHost.ResetSize(hostSizeType);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool GetSize(
            HostSizeType hostSizeType,
            ref int width,
            ref int height
            )
        {
            CheckDisposed();

            return baseHost.GetSize(hostSizeType, ref width, ref height);
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

            return baseHost.Read(ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ReadKey(
            bool intercept,
            ref IClientData value
            )
        {
            CheckDisposed();

            return baseHost.ReadKey(intercept, ref value);
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

            return baseHost.ReadKey(intercept, ref value);
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

            return baseHost.Write(value, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value,
            int count
            )
        {
            CheckDisposed();

            return baseHost.Write(value, count);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value,
            int count,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.Write(value, count, newLine);
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

            return baseHost.Write(
                value, count, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return baseHost.Write(value, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return baseHost.Write(value, foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return baseHost.Write(value, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.Write(value, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return baseHost.Write(value, newLine, foregroundColor);
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

            return baseHost.Write(
                value, newLine, foregroundColor, backgroundColor);
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

            return baseHost.WriteFormat(
                list, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteLine(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return baseHost.WriteLine(value, foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteLine(
            string value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return baseHost.WriteLine(value, foregroundColor, backgroundColor);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHost Members
        public virtual string Profile
        {
            get { CheckDisposed(); return baseHost.Profile; }
            set { CheckDisposed(); baseHost.Profile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string DefaultTitle
        {
            get { CheckDisposed(); return baseHost.DefaultTitle; }
            set { CheckDisposed(); baseHost.DefaultTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual HostCreateFlags HostCreateFlags
        {
            get { CheckDisposed(); return baseHost.HostCreateFlags; }
            set { CheckDisposed(); baseHost.HostCreateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool UseAttach
        {
            get { CheckDisposed(); return baseHost.UseAttach; }
            set { CheckDisposed(); baseHost.UseAttach = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool NoTitle
        {
            get { CheckDisposed(); return baseHost.NoTitle; }
            set { CheckDisposed(); baseHost.NoTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool NoIcon
        {
            get { CheckDisposed(); return baseHost.NoIcon; }
            set { CheckDisposed(); baseHost.NoIcon = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool NoProfile
        {
            get { CheckDisposed(); return baseHost.NoProfile; }
            set { CheckDisposed(); baseHost.NoProfile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool NoCancel
        {
            get { CheckDisposed(); return baseHost.NoCancel; }
            set { CheckDisposed(); baseHost.NoCancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Echo
        {
            get { CheckDisposed(); return baseHost.Echo; }
            set { CheckDisposed(); baseHost.Echo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            return baseHost.QueryState(detailFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Beep(
            int frequency,
            int duration
            )
        {
            CheckDisposed();

            return baseHost.Beep(frequency, duration);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool IsIdle()
        {
            CheckDisposed();

            return baseHost.IsIdle();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Clear()
        {
            CheckDisposed();

            return baseHost.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ResetHostFlags()
        {
            CheckDisposed();

            return baseHost.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode GetMode(
            ChannelType channelType,
            ref uint mode,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.GetMode(channelType, ref mode, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode SetMode(
            ChannelType channelType,
            uint mode,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.SetMode(channelType, mode, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Open(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Close(
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Close(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Discard(
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Discard(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Reset(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return baseHost.BeginSection(name, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool EndSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return baseHost.EndSection(name, clientData);
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

            return baseHost.BeginProcessing(levels, ref text, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode EndProcessing(
            int levels,
            ref string text,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.EndProcessing(levels, ref text, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode DoneProcessing(
            int levels,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.DoneProcessing(levels, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string Title
        {
            get { CheckDisposed(); return baseHost.Title; }
            set { CheckDisposed(); baseHost.Title = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool RefreshTitle()
        {
            CheckDisposed();

            return baseHost.RefreshTitle();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool IsInputRedirected()
        {
            CheckDisposed();

            return baseHost.IsInputRedirected();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Prompt(
            PromptType type,
            ref PromptFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Prompt(type, ref flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool IsOpen()
        {
            CheckDisposed();

            return baseHost.IsOpen();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Pause()
        {
            CheckDisposed();

            return baseHost.Pause();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Flush()
        {
            CheckDisposed();

            return baseHost.Flush();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual HeaderFlags GetHeaderFlags()
        {
            CheckDisposed();

            return baseHost.GetHeaderFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual HostFlags GetHostFlags()
        {
            CheckDisposed();

            return baseHost.GetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int ReadLevels
        {
            get { CheckDisposed(); return baseHost.ReadLevels; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int WriteLevels
        {
            get { CheckDisposed(); return baseHost.WriteLevels; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ReadLine(
            ref string value
            )
        {
            CheckDisposed();

            return baseHost.ReadLine(ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            char value
            )
        {
            CheckDisposed();

            return baseHost.Write(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Write(
            string value
            )
        {
            CheckDisposed();

            return baseHost.Write(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteLine()
        {
            CheckDisposed();

            return baseHost.WriteLine();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteLine(
            string value
            )
        {
            CheckDisposed();

            return baseHost.WriteLine(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteResultLine(
            ReturnCode code,
            Result result
            )
        {
            CheckDisposed();

            return baseHost.WriteResultLine(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteResultLine(
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
            CheckDisposed();

            return baseHost.WriteResultLine(code, result, errorLine);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public virtual string Name
        {
            get { CheckDisposed(); return baseHost.Name; }
            set { CheckDisposed(); baseHost.Name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public virtual IdentifierKind Kind
        {
            get { CheckDisposed(); return baseHost.Kind; }
            set { CheckDisposed(); baseHost.Kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Guid Id
        {
            get { CheckDisposed(); return baseHost.Id; }
            set { CheckDisposed(); baseHost.Id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public virtual string Group
        {
            get { CheckDisposed(); return baseHost.Group; }
            set { CheckDisposed(); baseHost.Group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string Description
        {
            get { CheckDisposed(); return baseHost.Description; }
            set { CheckDisposed(); baseHost.Description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public virtual IClientData ClientData
        {
            get { CheckDisposed(); return baseHost.ClientData; }
            set { CheckDisposed(); baseHost.ClientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IFileSystemHost Members
        public virtual HostStreamFlags StreamFlags
        {
            get { CheckDisposed(); return baseHost.StreamFlags; }
            set { CheckDisposed(); baseHost.StreamFlags = value; }
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

            return baseHost.GetStream(
                path, mode, access, share, bufferSize, options,
                ref hostStreamFlags, ref fullPath, ref stream,
                ref error);
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

            return baseHost.GetData(
                name, dataFlags, ref scriptFlags, ref clientData,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcessHost Members
        public virtual bool CanExit
        {
            get { CheckDisposed(); return baseHost.CanExit; }
            set { CheckDisposed(); baseHost.CanExit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool CanForceExit
        {
            get { CheckDisposed(); return baseHost.CanForceExit; }
            set { CheckDisposed(); baseHost.CanForceExit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Exiting
        {
            get { CheckDisposed(); return baseHost.Exiting; }
            set { CheckDisposed(); baseHost.Exiting = value; }
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

            return baseHost.CreateThread(
                start, maxStackSize, userInterface, isBackground,
                useActiveStack, ref thread, ref error);
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

            return baseHost.CreateThread(
                start, maxStackSize, userInterface, isBackground,
                useActiveStack, ref thread, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode QueueWorkItem(
            WaitCallback callback,
            object state,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.QueueWorkItem(callback, state, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Sleep(
            int milliseconds
            )
        {
            CheckDisposed();

            return baseHost.Sleep(milliseconds);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Yield()
        {
            CheckDisposed();

            return baseHost.Yield();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        public virtual Stream DefaultIn
        {
            get { CheckDisposed(); return baseHost.DefaultIn; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Stream DefaultOut
        {
            get { CheckDisposed(); return baseHost.DefaultOut; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Stream DefaultError
        {
            get { CheckDisposed(); return baseHost.DefaultError; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Stream In
        {
            get { CheckDisposed(); return baseHost.In; }
            set { CheckDisposed(); baseHost.In = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Stream Out
        {
            get { CheckDisposed(); return baseHost.Out; }
            set { CheckDisposed(); baseHost.Out = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Stream Error
        {
            get { CheckDisposed(); return baseHost.Error; }
            set { CheckDisposed(); baseHost.Error = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Encoding InputEncoding
        {
            get { CheckDisposed(); return baseHost.InputEncoding; }
            set { CheckDisposed(); baseHost.InputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Encoding OutputEncoding
        {
            get { CheckDisposed(); return baseHost.OutputEncoding; }
            set { CheckDisposed(); baseHost.OutputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Encoding ErrorEncoding
        {
            get { CheckDisposed(); return baseHost.ErrorEncoding; }
            set { CheckDisposed(); baseHost.ErrorEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ResetIn()
        {
            CheckDisposed();

            return baseHost.ResetIn();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ResetOut()
        {
            CheckDisposed();

            return baseHost.ResetOut();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ResetError()
        {
            CheckDisposed();

            return baseHost.ResetError();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool IsOutputRedirected()
        {
            CheckDisposed();

            return baseHost.IsOutputRedirected();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool IsErrorRedirected()
        {
            CheckDisposed();

            return baseHost.IsErrorRedirected();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetupChannels()
        {
            CheckDisposed();

            return baseHost.SetupChannels();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        public virtual IHost Clone()
        {
            CheckDisposed();

            return baseHost.Clone();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual IHost Clone(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return baseHost.Clone(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            return baseHost.GetTestFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Cancel(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Cancel(force, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Exit(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Exit(force, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebugLine()
        {
            CheckDisposed();

            return baseHost.WriteDebugLine();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebugLine(
            string value
            )
        {
            CheckDisposed();

            return baseHost.WriteDebugLine(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            char value
            )
        {
            CheckDisposed();

            return baseHost.WriteDebug(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteDebug(value, newLine);
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

            return baseHost.WriteDebug(
                value, count, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            string value
            )
        {
            CheckDisposed();

            return baseHost.WriteDebug(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteDebug(value, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteDebug(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return baseHost.WriteDebug(value, newLine, foregroundColor);
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

            return baseHost.WriteDebug(
                value, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteErrorLine()
        {
            CheckDisposed();

            return baseHost.WriteErrorLine();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteErrorLine(
            string value
            )
        {
            CheckDisposed();

            return baseHost.WriteErrorLine(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            char value
            )
        {
            CheckDisposed();

            return baseHost.WriteError(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteError(value, newLine);
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

            return baseHost.WriteError(
                value, count, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            string value
            )
        {
            CheckDisposed();

            return baseHost.WriteError(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteError(value, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteError(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return baseHost.WriteError(value, newLine, foregroundColor);
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

            return baseHost.WriteError(
                value, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteResult(
            ReturnCode code,
            Result result,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteResult(code, result, newLine);
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

            return baseHost.WriteResult(code, result, raw, newLine);
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

            return baseHost.WriteResult(code, result, errorLine, newLine);
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

            return baseHost.WriteResult(code, result, errorLine, raw, newLine);
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

            return baseHost.WriteResult(
                prefix, code, result, errorLine, newLine);
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

            return baseHost.WriteResult(
                prefix, code, result, errorLine, raw, newLine);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInformationHost Members
        public virtual bool SavePosition()
        {
            CheckDisposed();

            return baseHost.SavePosition();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool RestorePosition(
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.RestorePosition(newLine);
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

            return baseHost.WriteAnnouncementInfo(
                interpreter, breakpointType, value, newLine);
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

            return baseHost.WriteAnnouncementInfo(
                interpreter, breakpointType, value, newLine,
                foregroundColor, backgroundColor);
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

            return baseHost.WriteArgumentInfo(
                interpreter, code, breakpointType, breakpointName, arguments,
                result, detailFlags, newLine);
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

            return baseHost.WriteArgumentInfo(
                interpreter, code, breakpointType, breakpointName,
                arguments, result, detailFlags, newLine, foregroundColor,
                backgroundColor);
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

            return baseHost.WriteCallFrame(
                interpreter, frame, type, prefix, suffix, separator,
                detailFlags, newLine);
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

            return baseHost.WriteCallFrameInfo(
                interpreter, frame, detailFlags, newLine);
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

            return baseHost.WriteCallFrameInfo(
                interpreter, frame, detailFlags, newLine, foregroundColor,
                backgroundColor);
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

            return baseHost.WriteCallStack(
                interpreter, callStack, detailFlags, newLine);
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

            return baseHost.WriteCallStack(
                interpreter, callStack, limit, detailFlags, newLine);
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

            return baseHost.WriteCallStackInfo(
                interpreter, callStack, detailFlags, newLine);
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

            return baseHost.WriteCallStackInfo(
                interpreter, callStack, limit, detailFlags, newLine);
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

            return baseHost.WriteCallStackInfo(
                interpreter, callStack, limit, detailFlags, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        public virtual bool WriteDebuggerInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteDebuggerInfo(
                interpreter, detailFlags, newLine);
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

            return baseHost.WriteDebuggerInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }
#endif

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

            return baseHost.WriteFlagInfo(
                interpreter, engineFlags, substitutionFlags, eventFlags,
                expressionFlags, headerFlags, detailFlags, newLine);
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

            return baseHost.WriteFlagInfo(
                interpreter, engineFlags, substitutionFlags, eventFlags,
                expressionFlags, headerFlags, detailFlags, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteHostInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteHostInfo(
                interpreter, detailFlags, newLine);
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

            return baseHost.WriteHostInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteInterpreterInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteInterpreterInfo(
                interpreter, detailFlags, newLine);
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

            return baseHost.WriteInterpreterInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteEngineInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteEngineInfo(
                interpreter, detailFlags, newLine);
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

            return baseHost.WriteEngineInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteEntityInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteEntityInfo(
                interpreter, detailFlags, newLine);
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

            return baseHost.WriteEntityInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteStackInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteStackInfo(
                interpreter, detailFlags, newLine);
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

            return baseHost.WriteStackInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteControlInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteControlInfo(
                interpreter, detailFlags, newLine);
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

            return baseHost.WriteControlInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteTestInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteTestInfo(interpreter, detailFlags, newLine);
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

            return baseHost.WriteTestInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
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

            return baseHost.WriteTokenInfo(
                interpreter, token, detailFlags, newLine);
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

            return baseHost.WriteTokenInfo(
                interpreter, token, detailFlags, newLine, foregroundColor,
                backgroundColor);
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

            return baseHost.WriteTraceInfo(
                interpreter, traceInfo, detailFlags, newLine);
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

            return baseHost.WriteTraceInfo(
                interpreter, traceInfo, detailFlags, newLine,
                foregroundColor, backgroundColor);
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

            return baseHost.WriteVariableInfo(
                interpreter, variable, detailFlags, newLine);
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

            return baseHost.WriteVariableInfo(
                interpreter, variable, detailFlags, newLine, foregroundColor,
                backgroundColor);
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

            return baseHost.WriteObjectInfo(
                interpreter, @object, detailFlags, newLine);
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

            return baseHost.WriteObjectInfo(
                interpreter, @object, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool WriteComplaintInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteComplaintInfo(
                interpreter, detailFlags, newLine);
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

            return baseHost.WriteComplaintInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
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

            return baseHost.WriteHistoryInfo(
                interpreter, historyFilter, detailFlags, newLine);
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

            return baseHost.WriteHistoryInfo(
                interpreter, historyFilter, detailFlags, newLine,
                foregroundColor, backgroundColor);
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

            return baseHost.WriteCustomInfo(
                interpreter, detailFlags, newLine);
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

            return baseHost.WriteCustomInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
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

            return baseHost.WriteAllResultInfo(
                code, result, errorLine, previousResult, detailFlags,
                newLine);
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

            return baseHost.WriteAllResultInfo(
                code, result, errorLine, previousResult, detailFlags,
                newLine, foregroundColor, backgroundColor);
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

            return baseHost.WriteResultInfo(
                name, code, result, errorLine, detailFlags, newLine);
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

            return baseHost.WriteResultInfo(
                name, code, result, errorLine, detailFlags, newLine,
                foregroundColor, backgroundColor);
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

            baseHost.WriteHeader(interpreter, loopData, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void WriteFooter(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            Result result
            )
        {
            CheckDisposed();

            baseHost.WriteFooter(interpreter, loopData, result);
        }
#endif
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
                throw new InterpreterDisposedException(typeof(Wrapper));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void Dispose(
            bool disposing
            )
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

                if (baseHost != null)
                {
                    if (baseHostOwned)
                    {
                        IDisposable disposable = baseHost as IDisposable;

                        if (disposable != null)
                        {
                            disposable.Dispose(); /* throw */
                            disposable = null;
                        }
                    }

                    baseHost = null;
                }

                ///////////////////////////////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Wrapper()
        {
            Dispose(false);
        }
        #endregion
    }
}
