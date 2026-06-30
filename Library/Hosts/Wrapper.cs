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
    /// <summary>
    /// This class is an <see cref="IHost" /> implementation that wraps
    /// another host (the "base host") and forwards every interface member
    /// to it.  It allows a host to be adapted, decorated, or made
    /// disposable -- and, when built for isolated interpreters or plugins,
    /// marshaled across application domain boundaries -- without changing
    /// the wrapped host itself.  Each member is virtual so derived classes
    /// may override individual behaviors while delegating the rest to the
    /// base host.
    /// </summary>
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
        /// <summary>
        /// The wrapped host that is used to provide the implementations for
        /// all of the <see cref="IHost" /> interface members.
        /// </summary>
        private IHost baseHost;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This boolean field will be non-zero if the wrapped host is
        //       supposed to be "owned" by us (i.e. and must be disposed).
        //
        /// <summary>
        /// When non-zero, the wrapped host is owned by this wrapper and must
        /// be disposed when this wrapper is disposed.
        /// </summary>
        private bool baseHostOwned;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        /// <summary>
        /// Constructs an instance of this host wrapper, recording the host to
        /// wrap and copying the supplied host data into this instance.
        /// </summary>
        /// <param name="hostData">
        /// The host data used to initialize the identity and configuration
        /// of this wrapper.  This parameter may be null.
        /// </param>
        /// <param name="baseHost">
        /// The wrapped host that provides the implementations for all of
        /// the <see cref="IHost" /> interface members.  This parameter may
        /// be null.
        /// </param>
        /// <param name="baseHostOwned">
        /// Non-zero if the wrapped host is owned by this wrapper and must
        /// be disposed when this wrapper is disposed.
        /// </param>
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
        /// <summary>
        /// Gets or sets the wrapped host that provides the implementations
        /// for all of the <see cref="IHost" /> interface members.
        /// </summary>
        public virtual IHost BaseHost
        {
            get { CheckDisposed(); return baseHost; }
            set { CheckDisposed(); baseHost = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether the wrapped host is owned
        /// by this wrapper and must be disposed when this wrapper is
        /// disposed.
        /// </summary>
        public virtual bool BaseHostOwned
        {
            get { CheckDisposed(); return baseHostOwned; }
            set { CheckDisposed(); baseHostOwned = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBoxHost Members
        /// <summary>
        /// This method begins rendering a box with the specified name and
        /// content.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name and value pairs that make up the content of the
        /// box.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the box was successfully begun; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method ends rendering a box with the specified name and
        /// content.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name and value pairs that make up the content of the
        /// box.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the box was successfully ended; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified string value as the content of a box with the
        /// specified name.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return baseHost.WriteBox(
                name, value, clientData, newLine, restore, ref left, ref top);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string value as the content of a box with the
        /// specified name, padding the content to a minimum width.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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

        /// <summary>
        /// This method writes the specified string value as the content of a box with the
        /// specified name, using the specified content colors.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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

        /// <summary>
        /// This method writes the specified string value as the content of a box with the
        /// specified name, padding the content to a minimum width and using the specified content colors.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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

        /// <summary>
        /// This method writes the specified string value as the content of a box with the
        /// specified name, using the specified content and box colors.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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

        /// <summary>
        /// This method writes the specified string value as the content of a box with the
        /// specified name, padding the content to a minimum width and using the specified content and box colors.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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

        /// <summary>
        /// This method writes the specified list of name and value pairs as the content of a box with the
        /// specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name and value pairs that make up the content of the
        /// box.  This parameter may be null.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return baseHost.WriteBox(
                name, list, clientData, newLine, restore, ref left, ref top);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified list of name and value pairs as the content of a box with the
        /// specified name, padding the content to a minimum width.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name and value pairs that make up the content of the
        /// box.  This parameter may be null.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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

        /// <summary>
        /// This method writes the specified list of name and value pairs as the content of a box with the
        /// specified name, using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name and value pairs that make up the content of the
        /// box.  This parameter may be null.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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

        /// <summary>
        /// This method writes the specified list of name and value pairs as the content of a box with the
        /// specified name, padding the content to a minimum width and using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name and value pairs that make up the content of the
        /// box.  This parameter may be null.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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

        /// <summary>
        /// This method writes the specified list of name and value pairs as the content of a box with the
        /// specified name, using the specified content and box colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name and value pairs that make up the content of the
        /// box.  This parameter may be null.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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

        /// <summary>
        /// This method writes the specified list of name and value pairs as the content of a box with the
        /// specified name, padding the content to a minimum width and using the specified content and box colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name and value pairs that make up the content of the
        /// box.  This parameter may be null.
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
        /// On input, the row at which to begin writing; upon return,
        /// receives the resulting row.
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
        /// <summary>
        /// Gets or sets a value indicating whether colorized output is
        /// disabled for this host.  When true, color operations have no
        /// visible effect.
        /// </summary>
        public virtual bool NoColor
        {
            get { CheckDisposed(); return baseHost.NoColor; }
            set { CheckDisposed(); baseHost.NoColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the host foreground and background colors to
        /// their default values.
        /// </summary>
        /// <returns>
        /// True if the colors were reset; otherwise, false.
        /// </returns>
        public virtual bool ResetColors()
        {
            CheckDisposed();

            return baseHost.ResetColors();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the current foreground and background colors of
        /// the host.
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

        /// <summary>
        /// This method adjusts the specified foreground and background colors
        /// as necessary so that they are suitable for use by the host (for
        /// example, to avoid an unreadable combination).
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

        /// <summary>
        /// This method sets the host foreground color.
        /// </summary>
        /// <param name="foregroundColor">
        /// The foreground color to set.
        /// </param>
        /// <returns>
        /// True if the foreground color was set; otherwise, false.
        /// </returns>
        public virtual bool SetForegroundColor(
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return baseHost.SetForegroundColor(foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the host background color.
        /// </summary>
        /// <param name="backgroundColor">
        /// The background color to set.
        /// </param>
        /// <returns>
        /// True if the background color was set; otherwise, false.
        /// </returns>
        public virtual bool SetBackgroundColor(
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return baseHost.SetBackgroundColor(backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.SetColors(
                foreground, background, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the foreground and/or background colors
        /// associated with a named entry within a color theme.
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
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method sets the foreground and/or background colors
        /// associated with a named entry within a color theme.
        /// </summary>
        /// <param name="theme">
        /// The name of the color theme to modify, or null to use the
        /// active theme.
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
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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
        /// <summary>
        /// This method resets the current position to its default value.
        /// </summary>
        /// <returns>
        /// True if the position was reset; otherwise, false.
        /// </returns>
        public virtual bool ResetPosition()
        {
            CheckDisposed();

            return baseHost.ResetPosition();
        }

        ///////////////////////////////////////////////////////////////////////

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
        public virtual bool GetPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return baseHost.GetPosition(ref left, ref top);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public virtual bool SetPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            return baseHost.SetPosition(left, top);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.GetDefaultPosition(ref left, ref top);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.SetDefaultPosition(left, top);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISizeHost Members
        /// <summary>
        /// This method resets the size of the specified host buffer and/or
        /// window to its default.
        /// </summary>
        /// <param name="hostSizeType">
        /// The <see cref="HostSizeType" /> value indicating which size
        /// should be reset.
        /// </param>
        /// <returns>
        /// True if the size was reset successfully; otherwise, false.
        /// </returns>
        public virtual bool ResetSize(
            HostSizeType hostSizeType
            )
        {
            CheckDisposed();

            return baseHost.ResetSize(hostSizeType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the size of the specified host buffer and/or
        /// window.
        /// </summary>
        /// <param name="hostSizeType">
        /// The <see cref="HostSizeType" /> value indicating which size
        /// should be queried.
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

        /// <summary>
        /// This method changes the size of the specified host buffer and/or
        /// window.  This operation is not supported by this wrapper and
        /// always throws <see cref="NotImplementedException" />.
        /// </summary>
        /// <param name="hostSizeType">
        /// The <see cref="HostSizeType" /> value indicating which size
        /// should be changed.
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
        /// <exception cref="System.NotImplementedException">
        /// Always thrown, because this operation is not supported by this
        /// wrapper.
        /// </exception>
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
        /// <summary>
        /// This method reads a single character from the host.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the character that was read, or a
        /// negative value if the end of the input was reached.
        /// </param>
        /// <returns>
        /// True if the character was read; otherwise, false.
        /// </returns>
        public virtual bool Read(
            ref int value
            )
        {
            CheckDisposed();

            return baseHost.Read(ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single key press from the host.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key press so that it is not displayed
        /// by the host.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the data describing the key that was
        /// pressed.
        /// </param>
        /// <returns>
        /// True if the key press was read; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method reads a single key press from the host.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key press so that it is not displayed
        /// by the host.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the data describing the key that was
        /// pressed.
        /// </param>
        /// <returns>
        /// True if the key press was read; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method writes a single character to the host output,
        /// optionally followed by a newline.
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
        public virtual bool Write(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.Write(value, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.Write(value, count);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.Write(value, count, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the host output the
        /// specified number of times, optionally followed by a newline,
        /// using the specified foreground and background colors.
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

            return baseHost.Write(
                value, count, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the host output using
        /// the specified foreground and background colors.
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

            return baseHost.Write(value, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output using the
        /// specified foreground color.
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

            return baseHost.Write(value, foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output using the
        /// specified foreground and background colors.
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

            return baseHost.Write(value, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output, optionally
        /// followed by a newline.
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
        public virtual bool Write(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.Write(value, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output, optionally
        /// followed by a newline, using the specified foreground color.
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

            return baseHost.Write(value, newLine, foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output, optionally
        /// followed by a newline, using the specified foreground and
        /// background colors.
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

            return baseHost.Write(
                value, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a formatted list of name and value pairs to
        /// the host output, optionally followed by a newline, using the
        /// specified foreground and background colors.
        /// </summary>
        /// <param name="list">
        /// The list of name and value pairs to write.  This parameter may
        /// be null.
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

            return baseHost.WriteFormat(
                list, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteLine(value, foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteLine(value, foregroundColor, backgroundColor);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHost Members
        /// <summary>
        /// Gets or sets the name of the profile used to load and persist
        /// this host's saved settings.
        /// </summary>
        public virtual string Profile
        {
            get { CheckDisposed(); return baseHost.Profile; }
            set { CheckDisposed(); baseHost.Profile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the default window or console title used by this
        /// host when no more specific title has been set.
        /// </summary>
        public virtual string DefaultTitle
        {
            get { CheckDisposed(); return baseHost.DefaultTitle; }
            set { CheckDisposed(); baseHost.DefaultTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the flags that were (or will be) used to create and
        /// configure this host.
        /// </summary>
        public virtual HostCreateFlags HostCreateFlags
        {
            get { CheckDisposed(); return baseHost.HostCreateFlags; }
            set { CheckDisposed(); baseHost.HostCreateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host should attach
        /// to an existing host environment (for example, an existing
        /// console) rather than creating a new one.
        /// </summary>
        public virtual bool UseAttach
        {
            get { CheckDisposed(); return baseHost.UseAttach; }
            set { CheckDisposed(); baseHost.UseAttach = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether host operations that
        /// would normally be skipped or refused should instead be forced.
        /// </summary>
        public virtual bool UseForce
        {
            get { CheckDisposed(); return baseHost.UseForce; }
            set { CheckDisposed(); baseHost.UseForce = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host should
        /// suppress changes to the window or console title.
        /// </summary>
        public virtual bool NoTitle
        {
            get { CheckDisposed(); return baseHost.NoTitle; }
            set { CheckDisposed(); baseHost.NoTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host should
        /// suppress changes to the window or console icon.
        /// </summary>
        public virtual bool NoIcon
        {
            get { CheckDisposed(); return baseHost.NoIcon; }
            set { CheckDisposed(); baseHost.NoIcon = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host should skip
        /// loading and saving its profile-based settings.
        /// </summary>
        public virtual bool NoProfile
        {
            get { CheckDisposed(); return baseHost.NoProfile; }
            set { CheckDisposed(); baseHost.NoProfile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host should
        /// disable interactive cancellation (for example, the cancel key
        /// handler).
        /// </summary>
        public virtual bool NoCancel
        {
            get { CheckDisposed(); return baseHost.NoCancel; }
            set { CheckDisposed(); baseHost.NoCancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this host echoes the
        /// input it reads back to its output.
        /// </summary>
        public virtual bool Echo
        {
            get { CheckDisposed(); return baseHost.Echo; }
            set { CheckDisposed(); baseHost.Echo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a snapshot of this host's current state,
        /// with the amount of detail controlled by the supplied flags.
        /// </summary>
        /// <param name="detailFlags">
        /// The flags that select how much state detail is included in the
        /// result.
        /// </param>
        /// <returns>
        /// A list describing the requested host state.
        /// </returns>
        public virtual StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            return baseHost.QueryState(detailFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits an audible tone through the host, when
        /// supported.
        /// </summary>
        /// <param name="frequency">
        /// The tone frequency, in hertz.
        /// </param>
        /// <param name="duration">
        /// The tone duration, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the tone was emitted; otherwise, false.
        /// </returns>
        public virtual bool Beep(
            int frequency,
            int duration
            )
        {
            CheckDisposed();

            return baseHost.Beep(frequency, duration);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host currently has no
        /// pending interactive input or output activity.
        /// </summary>
        /// <returns>
        /// True if the host is idle; otherwise, false.
        /// </returns>
        public virtual bool IsIdle()
        {
            CheckDisposed();

            return baseHost.IsIdle();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the host's display area, when supported.
        /// </summary>
        /// <returns>
        /// True if the display was cleared; otherwise, false.
        /// </returns>
        public virtual bool Clear()
        {
            CheckDisposed();

            return baseHost.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this host's configuration flags to their
        /// default values.
        /// </summary>
        /// <returns>
        /// True if the flags were reset; otherwise, false.
        /// </returns>
        public virtual bool ResetHostFlags()
        {
            CheckDisposed();

            return baseHost.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the host's interactive input history.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode ResetHistory(
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.ResetHistory(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the current mode of one of the host's
        /// standard channels.
        /// </summary>
        /// <param name="channelType">
        /// The channel whose mode is to be retrieved (for example, input
        /// or output).
        /// </param>
        /// <param name="mode">
        /// Upon success, this is set to the current channel mode.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method sets the mode of one of the host's standard
        /// channels.
        /// </summary>
        /// <param name="channelType">
        /// The channel whose mode is to be set (for example, input or
        /// output).
        /// </param>
        /// <param name="mode">
        /// The new channel mode to apply.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method opens, or re-opens, the host's underlying
        /// interactive resources.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Open(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the host's underlying interactive
        /// resources.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode Close(
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Close(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discards any buffered host input and/or output
        /// without closing the host.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode Discard(
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Discard(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the host to its initial state,
        /// reinitializing its interactive resources.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Reset(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method begins a named output section, allowing the host to
        /// group or visually delimit related output.
        /// </summary>
        /// <param name="name">
        /// The name of the section to begin.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the section, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the section was begun; otherwise, false.
        /// </returns>
        public virtual bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return baseHost.BeginSection(name, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends a named output section previously begun with
        /// <see cref="BeginSection" />.
        /// </summary>
        /// <param name="name">
        /// The name of the section to end.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the section, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the section was ended; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method is called when interactive processing is about to
        /// begin at the specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="text">
        /// On input, the text associated with the start of processing;
        /// on output, the possibly modified text.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method is called when interactive processing is about to
        /// end at the specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="text">
        /// On input, the text associated with the end of processing; on
        /// output, the possibly modified text.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method is called when interactive processing has completed
        /// at the specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode DoneProcessing(
            int levels,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.DoneProcessing(levels, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the current window or console title used by this
        /// host.
        /// </summary>
        public virtual string Title
        {
            get { CheckDisposed(); return baseHost.Title; }
            set { CheckDisposed(); baseHost.Title = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method updates the host's window or console title to
        /// reflect its current value.
        /// </summary>
        /// <returns>
        /// True if the title was refreshed; otherwise, false.
        /// </returns>
        public virtual bool RefreshTitle()
        {
            CheckDisposed();

            return baseHost.RefreshTitle();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host's interactive input
        /// has been redirected (for example, from a file or pipe).
        /// </summary>
        /// <returns>
        /// True if the input is redirected; otherwise, false.
        /// </returns>
        public virtual bool IsInputRedirected()
        {
            CheckDisposed();

            return baseHost.IsInputRedirected();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method displays a prompt of the specified type and reports
        /// the flags that resulted from displaying it.
        /// </summary>
        /// <param name="type">
        /// The type of prompt to display (for example, a normal or
        /// continuation prompt).
        /// </param>
        /// <param name="flags">
        /// On input, the flags that control how the prompt is displayed;
        /// on output, the flags that resulted from displaying it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the host's interactive
        /// resources are currently open.
        /// </summary>
        /// <returns>
        /// True if the host is open; otherwise, false.
        /// </returns>
        public virtual bool IsOpen()
        {
            CheckDisposed();

            return baseHost.IsOpen();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method pauses interactive processing, typically waiting
        /// for the user to acknowledge before continuing.
        /// </summary>
        /// <returns>
        /// True if the host was paused; otherwise, false.
        /// </returns>
        public virtual bool Pause()
        {
            CheckDisposed();

            return baseHost.Pause();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes any buffered host output.
        /// </summary>
        /// <returns>
        /// True if the output was flushed; otherwise, false.
        /// </returns>
        public virtual bool Flush()
        {
            CheckDisposed();

            return baseHost.Flush();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the flags that control which header
        /// sections the host displays.
        /// </summary>
        /// <returns>
        /// The current header flags for this host.
        /// </returns>
        public virtual HeaderFlags GetHeaderFlags()
        {
            CheckDisposed();

            return baseHost.GetHeaderFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the flags that control how much detail the
        /// host includes in its output.
        /// </summary>
        /// <returns>
        /// The current detail flags for this host.
        /// </returns>
        public virtual DetailFlags GetDetailFlags()
        {
            CheckDisposed();

            return baseHost.GetDetailFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the flags that describe the capabilities
        /// and configuration of this host.
        /// </summary>
        /// <returns>
        /// The current host flags for this host.
        /// </returns>
        public virtual HostFlags GetHostFlags()
        {
            CheckDisposed();

            return baseHost.GetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current nesting level of read operations in progress
        /// on this host.
        /// </summary>
        public virtual int ReadLevels
        {
            get { CheckDisposed(); return baseHost.ReadLevels; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current nesting level of write operations in progress
        /// on this host.
        /// </summary>
        public virtual int WriteLevels
        {
            get { CheckDisposed(); return baseHost.WriteLevels; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single line of interactive input from the
        /// host.
        /// </summary>
        /// <param name="value">
        /// Upon success, this is set to the line of input that was read.
        /// </param>
        /// <returns>
        /// True if a line was read; otherwise, false.
        /// </returns>
        public virtual bool ReadLine(
            ref string value
            )
        {
            CheckDisposed();

            return baseHost.ReadLine(ref value);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.Write(value);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.Write(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes an end-of-line to the host output.
        /// </summary>
        /// <returns>
        /// True if the end-of-line was written; otherwise, false.
        /// </returns>
        public virtual bool WriteLine()
        {
            CheckDisposed();

            return baseHost.WriteLine();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string followed by an end-of-line to the
        /// host output.
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

            return baseHost.WriteLine(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a formatted representation of a result,
        /// followed by an end-of-line, to the host output.
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

            return baseHost.WriteResultLine(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a formatted representation of a result,
        /// including an error line number, followed by an end-of-line, to
        /// the host output.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if
        /// not applicable.
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

            return baseHost.WriteResultLine(code, result, errorLine);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of this identifier.
        /// </summary>
        public virtual string Name
        {
            get { CheckDisposed(); return baseHost.Name; }
            set { CheckDisposed(); baseHost.Name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the enumerated kind of this identifier (for
        /// example, command or plugin).
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { CheckDisposed(); return baseHost.Kind; }
            set { CheckDisposed(); baseHost.Kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier associated with this
        /// identifier.
        /// </summary>
        public virtual Guid Id
        {
            get { CheckDisposed(); return baseHost.Id; }
            set { CheckDisposed(); baseHost.Id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the extra, entity-specific data associated with
        /// this object.  This value may be null.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { CheckDisposed(); return baseHost.ClientData; }
            set { CheckDisposed(); baseHost.ClientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the logical group that this identifier belongs
        /// to.
        /// </summary>
        public virtual string Group
        {
            get { CheckDisposed(); return baseHost.Group; }
            set { CheckDisposed(); baseHost.Group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the human-readable description of this
        /// identifier.
        /// </summary>
        public virtual string Description
        {
            get { CheckDisposed(); return baseHost.Description; }
            set { CheckDisposed(); baseHost.Description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IFileSystemHost Members
        /// <summary>
        /// Gets or sets the flags that control how this host opens and
        /// manages streams.
        /// </summary>
        public virtual HostStreamFlags StreamFlags
        {
            get { CheckDisposed(); return baseHost.StreamFlags; }
            set { CheckDisposed(); baseHost.StreamFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens a stream for the specified path on behalf of
        /// the engine.
        /// </summary>
        /// <param name="path">
        /// The path of the file or resource to open.
        /// </param>
        /// <param name="mode">
        /// The mode used when opening the stream (for example, create or
        /// open).
        /// </param>
        /// <param name="access">
        /// The access requested for the stream (for example, read or
        /// write).
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
        /// On input, the flags that influence how the stream is opened;
        /// on output, the flags describing the stream that was opened.
        /// </param>
        /// <param name="fullPath">
        /// Upon return, this contains the fully qualified path of the
        /// stream that was opened.
        /// </param>
        /// <param name="stream">
        /// Upon success, this contains the opened stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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
        /// On input, the flags that influence how the data is fetched;
        /// on output, the flags describing the data that was fetched.
        /// </param>
        /// <param name="clientData">
        /// On input, the extra data supplied for the request, if any; on
        /// output, the extra data associated with the fetched data, if
        /// any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the fetched data.  Upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
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
        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is
        /// permitted to exit.
        /// </summary>
        public virtual bool CanExit
        {
            get { CheckDisposed(); return baseHost.CanExit; }
            set { CheckDisposed(); baseHost.CanExit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is
        /// permitted to be forcibly exited.
        /// </summary>
        public virtual bool CanForceExit
        {
            get { CheckDisposed(); return baseHost.CanForceExit; }
            set { CheckDisposed(); baseHost.CanForceExit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is
        /// currently in the process of exiting.
        /// </summary>
        public virtual bool Exiting
        {
            get { CheckDisposed(); return baseHost.Exiting; }
            set { CheckDisposed(); baseHost.Exiting = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadHost Members
        /// <summary>
        /// This method creates a new thread that uses a parameterless
        /// start delegate.
        /// </summary>
        /// <param name="start">
        /// The delegate that represents the entry point for the new
        /// thread.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to use for the new thread, or
        /// zero to use the default.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the new thread will host a user interface and should
        /// be configured for single-threaded apartment use.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the new thread should be created as a background
        /// thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the new thread should inherit the active call stack
        /// from the creating thread.
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

        /// <summary>
        /// This method creates a new thread that uses a parameterized
        /// start delegate.
        /// </summary>
        /// <param name="start">
        /// The delegate that represents the entry point for the new
        /// thread and accepts a single object argument.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to use for the new thread, or
        /// zero to use the default.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the new thread will host a user interface and should
        /// be configured for single-threaded apartment use.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the new thread should be created as a background
        /// thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the new thread should inherit the active call stack
        /// from the creating thread.
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
        public virtual ReturnCode QueueWorkItem(
            ThreadStart callback,
            QueueFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.QueueWorkItem(callback, flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues a callback that accepts a state object for
        /// execution on a thread pool thread.
        /// </summary>
        /// <param name="callback">
        /// The delegate to invoke on a thread pool thread.
        /// </param>
        /// <param name="state">
        /// The state object to pass to the callback.  This parameter may
        /// be null.
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
        public virtual ReturnCode QueueWorkItem(
            WaitCallback callback,
            object state,
            QueueFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.QueueWorkItem(callback, state, flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method suspends the current thread for the specified
        /// amount of time.
        /// </summary>
        /// <param name="milliseconds">
        /// The amount of time to suspend the current thread, in
        /// milliseconds.
        /// </param>
        /// <returns>
        /// True if the thread was successfully suspended; otherwise,
        /// false.
        /// </returns>
        public virtual bool Sleep(
            int milliseconds
            )
        {
            CheckDisposed();

            return baseHost.Sleep(milliseconds);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method causes the current thread to yield execution to
        /// another thread that is ready to run on the current processor.
        /// </summary>
        /// <returns>
        /// True if the operating system switched execution to another
        /// thread; otherwise, false.
        /// </returns>
        public virtual bool Yield()
        {
            CheckDisposed();

            return baseHost.Yield();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        /// <summary>
        /// Gets the default input stream for this host.
        /// </summary>
        public virtual Stream DefaultIn
        {
            get { CheckDisposed(); return baseHost.DefaultIn; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the default output stream for this host.
        /// </summary>
        public virtual Stream DefaultOut
        {
            get { CheckDisposed(); return baseHost.DefaultOut; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the default error stream for this host.
        /// </summary>
        public virtual Stream DefaultError
        {
            get { CheckDisposed(); return baseHost.DefaultError; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the active input stream for this host.
        /// </summary>
        public virtual Stream In
        {
            get { CheckDisposed(); return baseHost.In; }
            set { CheckDisposed(); baseHost.In = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the active output stream for this host.
        /// </summary>
        public virtual Stream Out
        {
            get { CheckDisposed(); return baseHost.Out; }
            set { CheckDisposed(); baseHost.Out = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the active error stream for this host.
        /// </summary>
        public virtual Stream Error
        {
            get { CheckDisposed(); return baseHost.Error; }
            set { CheckDisposed(); baseHost.Error = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the encoding used for the input stream.
        /// </summary>
        public virtual Encoding InputEncoding
        {
            get { CheckDisposed(); return baseHost.InputEncoding; }
            set { CheckDisposed(); baseHost.InputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the encoding used for the output stream.
        /// </summary>
        public virtual Encoding OutputEncoding
        {
            get { CheckDisposed(); return baseHost.OutputEncoding; }
            set { CheckDisposed(); baseHost.OutputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the encoding used for the error stream.
        /// </summary>
        public virtual Encoding ErrorEncoding
        {
            get { CheckDisposed(); return baseHost.ErrorEncoding; }
            set { CheckDisposed(); baseHost.ErrorEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active input stream to its default.
        /// </summary>
        /// <returns>
        /// True if the input stream was reset; otherwise, false.
        /// </returns>
        public virtual bool ResetIn()
        {
            CheckDisposed();

            return baseHost.ResetIn();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active output stream to its default.
        /// </summary>
        /// <returns>
        /// True if the output stream was reset; otherwise, false.
        /// </returns>
        public virtual bool ResetOut()
        {
            CheckDisposed();

            return baseHost.ResetOut();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active error stream to its default.
        /// </summary>
        /// <returns>
        /// True if the error stream was reset; otherwise, false.
        /// </returns>
        public virtual bool ResetError()
        {
            CheckDisposed();

            return baseHost.ResetError();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the output stream for this host
        /// has been redirected.
        /// </summary>
        /// <returns>
        /// True if the output stream has been redirected; otherwise,
        /// false.
        /// </returns>
        public virtual bool IsOutputRedirected()
        {
            CheckDisposed();

            return baseHost.IsOutputRedirected();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the error stream for this host
        /// has been redirected.
        /// </summary>
        /// <returns>
        /// True if the error stream has been redirected; otherwise,
        /// false.
        /// </returns>
        public virtual bool IsErrorRedirected()
        {
            CheckDisposed();

            return baseHost.IsErrorRedirected();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets up the input, output, and error channels for
        /// this host.
        /// </summary>
        /// <returns>
        /// True if the channels were set up successfully; otherwise,
        /// false.
        /// </returns>
        public virtual bool SetupChannels()
        {
            CheckDisposed();

            return baseHost.SetupChannels();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        /// <summary>
        /// This method creates a copy of this host.
        /// </summary>
        /// <returns>
        /// The newly created copy of this host, or null if it could not be
        /// created.
        /// </returns>
        public virtual IHost Clone()
        {
            CheckDisposed();

            return baseHost.Clone();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of this host for use with the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the cloned host will be associated
        /// with.
        /// </param>
        /// <returns>
        /// The newly created copy of this host, or null if it could not be
        /// created.
        /// </returns>
        public virtual IHost Clone(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return baseHost.Clone(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the <see cref="HostTestFlags" /> that
        /// describe the testing capabilities of this host.
        /// </summary>
        /// <returns>
        /// The host test flags for this host.
        /// </returns>
        public virtual HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            return baseHost.GetTestFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests that the current script evaluation be
        /// canceled.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly cancel evaluation even if cancellation
        /// has been disabled or is otherwise being prevented.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode Cancel(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Cancel(force, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests that the interpreter exit.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly request the exit even if it has been
        /// disabled or is otherwise being prevented.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode Exit(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            return baseHost.Exit(force, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a line terminator to the debug output of
        /// the host.
        /// </summary>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public virtual bool WriteDebugLine()
        {
            CheckDisposed();

            return baseHost.WriteDebugLine();
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteDebugLine(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the debug output
        /// of the host.
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

            return baseHost.WriteDebug(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the debug output
        /// of the host, optionally followed by a line terminator.
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
        public virtual bool WriteDebug(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteDebug(value, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character a number of times to
        /// the debug output of the host, using the specified colors and
        /// optionally followed by a line terminator.
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

            return baseHost.WriteDebug(
                value, count, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the debug output of
        /// the host.
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

            return baseHost.WriteDebug(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the debug output of
        /// the host, optionally followed by a line terminator.
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
        public virtual bool WriteDebug(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteDebug(value, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the debug output of
        /// the host, using the specified foreground color and optionally
        /// followed by a line terminator.
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

            return baseHost.WriteDebug(value, newLine, foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the debug output of
        /// the host, using the specified colors and optionally followed by
        /// a line terminator.
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

            return baseHost.WriteDebug(
                value, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a line terminator to the error output of
        /// the host.
        /// </summary>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public virtual bool WriteErrorLine()
        {
            CheckDisposed();

            return baseHost.WriteErrorLine();
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteErrorLine(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the error output
        /// of the host.
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

            return baseHost.WriteError(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the error output
        /// of the host, optionally followed by a line terminator.
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
        public virtual bool WriteError(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteError(value, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character a number of times to
        /// the error output of the host, using the specified colors and
        /// optionally followed by a line terminator.
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

            return baseHost.WriteError(
                value, count, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the error output of
        /// the host.
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

            return baseHost.WriteError(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the error output of
        /// the host, optionally followed by a line terminator.
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
        public virtual bool WriteError(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.WriteError(value, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the error output of
        /// the host, using the specified foreground color and optionally
        /// followed by a line terminator.
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

            return baseHost.WriteError(value, newLine, foregroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the error output of
        /// the host, using the specified colors and optionally followed by
        /// a line terminator.
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

            return baseHost.WriteError(
                value, newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteResult(code, result, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteResult(code, result, raw, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteResult(code, result, errorLine, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteResult(code, result, errorLine, raw, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified prefix, return code, result,
        /// and error line to the host, optionally followed by a line
        /// terminator.
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

            return baseHost.WriteResult(
                prefix, code, result, errorLine, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified prefix, return code, result,
        /// and error line to the host, optionally without additional
        /// formatting and optionally followed by a line terminator.
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

            return baseHost.WriteResult(
                prefix, code, result, errorLine, raw, newLine);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInformationHost Members
        /// <summary>
        /// This method saves the host's current cursor position so that it
        /// can later be restored.
        /// </summary>
        /// <returns>
        /// True if the position was saved; otherwise, false.
        /// </returns>
        public virtual bool SavePosition()
        {
            CheckDisposed();

            return baseHost.SavePosition();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the host's cursor position previously
        /// saved with <see cref="SavePosition" />.
        /// </summary>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after restoring the
        /// position.
        /// </param>
        /// <returns>
        /// True if the position was restored; otherwise, false.
        /// </returns>
        public virtual bool RestorePosition(
            bool newLine
            )
        {
            CheckDisposed();

            return baseHost.RestorePosition(newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes announcement information associated with a
        /// breakpoint to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the announcement is associated with.
        /// </param>
        /// <param name="value">
        /// The announcement text to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes announcement information associated with a
        /// breakpoint to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the announcement is associated with.
        /// </param>
        /// <param name="value">
        /// The announcement text to write.
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

        /// <summary>
        /// This method writes information about the arguments associated with
        /// a breakpoint to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the breakpoint.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the information is associated with.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the breakpoint the information is associated
        /// with.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to write information about.
        /// </param>
        /// <param name="result">
        /// The result value associated with the breakpoint.
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

            return baseHost.WriteArgumentInfo(
                interpreter, code, breakpointType, breakpointName, arguments,
                result, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the arguments associated with
        /// a breakpoint to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the breakpoint.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the information is associated with.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the breakpoint the information is associated
        /// with.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to write information about.
        /// </param>
        /// <param name="result">
        /// The result value associated with the breakpoint.
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

            return baseHost.WriteArgumentInfo(
                interpreter, code, breakpointType, breakpointName,
                arguments, result, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a representation of a single call frame to
        /// the host output, using the specified affixes and separator.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call frame is associated with.
        /// This parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write information about.
        /// </param>
        /// <param name="type">
        /// A string describing the kind of call frame being written.
        /// </param>
        /// <param name="prefix">
        /// The text to write before the call frame information.
        /// </param>
        /// <param name="suffix">
        /// The text to write after the call frame information.
        /// </param>
        /// <param name="separator">
        /// The character used to separate parts of the call frame
        /// information.
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

        /// <summary>
        /// This method writes information about a single call frame to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call frame is associated with.
        /// This parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write information about.
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

            return baseHost.WriteCallFrameInfo(
                interpreter, frame, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about a single call frame to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call frame is associated with.
        /// This parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write information about.
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

            return baseHost.WriteCallFrameInfo(
                interpreter, frame, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a representation of the specified call stack
        /// to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.
        /// This parameter should not be null.
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

        /// <summary>
        /// This method writes a representation of the specified call stack
        /// to the host output, limited to a maximum number of frames.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.
        /// This parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to write.
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

        /// <summary>
        /// This method writes information about the specified call stack
        /// to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.
        /// This parameter should not be null.
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

            return baseHost.WriteCallStackInfo(
                interpreter, callStack, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified call stack
        /// to the host output, limited to a maximum number of frames.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.
        /// This parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to write.
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

            return baseHost.WriteCallStackInfo(
                interpreter, callStack, limit, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified call stack
        /// to the host output, limited to a maximum number of frames and
        /// using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.
        /// This parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to write.
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

            return baseHost.WriteCallStackInfo(
                interpreter, callStack, limit, detailFlags, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        /// <summary>
        /// This method writes information about the interpreter's script
        /// debugger to the host output.
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

            return baseHost.WriteDebuggerInfo(
                interpreter, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's script
        /// debugger to the host output, using the specified colors.
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

            return baseHost.WriteDebuggerInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's various flag
        /// sets to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to write information about.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to write information about.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to write information about.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to write information about.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags to write information about.
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

            return baseHost.WriteFlagInfo(
                interpreter, engineFlags, substitutionFlags, eventFlags,
                expressionFlags, headerFlags, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's various flag
        /// sets to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to write information about.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to write information about.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to write information about.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to write information about.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags to write information about.
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

            return baseHost.WriteFlagInfo(
                interpreter, engineFlags, substitutionFlags, eventFlags,
                expressionFlags, headerFlags, detailFlags, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified host to the host
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

        /// <summary>
        /// This method writes information about the specified host to the host
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

        /// <summary>
        /// This method writes information about the specified interpreter to
        /// the host output.
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

            return baseHost.WriteInterpreterInfo(
                interpreter, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified interpreter to
        /// the host output, using the specified colors.
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

            return baseHost.WriteInterpreterInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's execution
        /// engine to the host output.
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

            return baseHost.WriteEngineInfo(
                interpreter, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's execution
        /// engine to the host output, using the specified colors.
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

            return baseHost.WriteEngineInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the entities defined in the
        /// interpreter to the host output.
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

            return baseHost.WriteEntityInfo(
                interpreter, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the entities defined in the
        /// interpreter to the host output, using the specified colors.
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

            return baseHost.WriteEntityInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the native call stack to the
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

            return baseHost.WriteStackInfo(
                interpreter, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the native call stack to the
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

            return baseHost.WriteStackInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's control
        /// state to the host output.
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

            return baseHost.WriteControlInfo(
                interpreter, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's control
        /// state to the host output, using the specified colors.
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

            return baseHost.WriteControlInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's test state
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

        /// <summary>
        /// This method writes information about the interpreter's test state
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

        /// <summary>
        /// This method writes information about the specified token to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the token is associated with.  This
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

            return baseHost.WriteTokenInfo(
                interpreter, token, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified token to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the token is associated with.  This
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

            return baseHost.WriteTokenInfo(
                interpreter, token, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified trace to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the trace is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information to write.
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

            return baseHost.WriteTraceInfo(
                interpreter, traceInfo, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified trace to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the trace is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information to write.
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

            return baseHost.WriteTraceInfo(
                interpreter, traceInfo, detailFlags, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified variable to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the variable is associated with.
        /// This parameter should not be null.
        /// </param>
        /// <param name="variable">
        /// The variable to write information about.
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

            return baseHost.WriteVariableInfo(
                interpreter, variable, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified variable to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the variable is associated with.
        /// This parameter should not be null.
        /// </param>
        /// <param name="variable">
        /// The variable to write information about.
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

            return baseHost.WriteVariableInfo(
                interpreter, variable, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified object to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the object is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="object">
        /// The object to write information about.
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

            return baseHost.WriteObjectInfo(
                interpreter, @object, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified object to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the object is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="object">
        /// The object to write information about.
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

            return baseHost.WriteObjectInfo(
                interpreter, @object, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteComplaintInfo(
                interpreter, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the most recent complaint
        /// raised by the interpreter to the host output, using the specified colors.
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

            return baseHost.WriteComplaintInfo(
                interpreter, detailFlags, newLine, foregroundColor,
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteHistoryInfo(
                interpreter, historyFilter, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteHistoryInfo(
                interpreter, historyFilter, detailFlags, newLine,
                foregroundColor, backgroundColor);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

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

            return baseHost.WriteCustomInfo(
                interpreter, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

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

        /// <summary>
        /// This method writes complete information about a result,
        /// including the previous result, to the host output.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if
        /// not applicable.
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

            return baseHost.WriteAllResultInfo(
                code, result, errorLine, previousResult, detailFlags,
                newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes complete information about a result,
        /// including the previous result, to the host output, using the specified colors.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if
        /// not applicable.
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

            return baseHost.WriteAllResultInfo(
                code, result, errorLine, previousResult, detailFlags,
                newLine, foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about a named result to the
        /// host output.
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
        /// The script line number associated with an error, or zero if
        /// not applicable.
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

            return baseHost.WriteResultInfo(
                name, code, result, errorLine, detailFlags, newLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about a named result to the
        /// host output, using the specified colors.
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
        /// The script line number associated with an error, or zero if
        /// not applicable.
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

            return baseHost.WriteResultInfo(
                name, code, result, errorLine, detailFlags, newLine,
                foregroundColor, backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// This method writes the interactive loop header to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the header is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="loopData">
        /// The data describing the interactive loop the header is
        /// associated with.
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

            baseHost.WriteHeader(interpreter, loopData, result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the interactive loop footer to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the footer is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="loopData">
        /// The data describing the interactive loop the footer is
        /// associated with.
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

            baseHost.WriteFooter(interpreter, loopData, result);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this wrapper has been
        /// disposed.
        /// </summary>
        public bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this wrapper is currently
        /// being disposed.  Retrieving this property is not supported and
        /// always throws <see cref="NotImplementedException" />.
        /// </summary>
        public bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                throw new NotImplementedException();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources held by this wrapper and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this wrapper has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this wrapper has already
        /// been disposed.  It is called at the start of most members to
        /// guard against use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this wrapper has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Wrapper));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this wrapper.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if
        /// it is being called from the finalizer.  When non-zero,
        /// managed resources are released.
        /// </param>
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
        /// <summary>
        /// Finalizes this wrapper, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~Wrapper()
        {
            Dispose(false);
        }
        #endregion
    }
}
