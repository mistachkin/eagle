/*
 * Host.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by the host environment that an Eagle
    /// interpreter uses for all of its external interaction -- console and
    /// display output, interactive input, file-system access, threading,
    /// process control, stream access, debugging, and reading and writing of
    /// data.  It composes the specialized host sub-interfaces
    /// (<see cref="IDisplayHost" />, <see cref="IInteractiveHost" />,
    /// <see cref="IFileSystemHost" />, <see cref="IThreadHost" />,
    /// <see cref="IProcessHost" />, <see cref="IStreamHost" />,
    /// <see cref="IDebugHost" />, <see cref="IReadHost" />,
    /// <see cref="IWriteHost" />, and <see cref="IInformationHost" />) and
    /// adds host-wide configuration and lifecycle members.  An interpreter is
    /// created with a host (a default console host is supplied when none is
    /// given); replacing the host is how an embedding application redirects or
    /// customizes that interaction.
    /// </summary>
    [ObjectId("0b056d8f-c52e-4cb9-a92d-ddc478a00be7")]
    public interface IHost :
            IDisplayHost, IInteractiveHost, IFileSystemHost,
            IThreadHost, IProcessHost, IStreamHost, IDebugHost,
            IReadHost, IWriteHost, IInformationHost
    {
        /// <summary>
        /// Gets or sets the name of the profile used to load and persist this
        /// host's saved settings.
        /// </summary>
        string Profile { get; set; }

        /// <summary>
        /// Gets or sets the default window or console title used by this host
        /// when no more specific title has been set.
        /// </summary>
        string DefaultTitle { get; set; }

        /// <summary>
        /// Gets or sets the flags that were (or will be) used to create and
        /// configure this host.
        /// </summary>
        HostCreateFlags HostCreateFlags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this host should attach to
        /// an existing host environment (for example, an existing console)
        /// rather than creating a new one.
        /// </summary>
        bool UseAttach { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether host operations that would
        /// normally be skipped or refused should instead be forced.
        /// </summary>
        bool UseForce { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this host should suppress
        /// changes to the window or console title.
        /// </summary>
        bool NoTitle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this host should suppress
        /// changes to the window or console icon.
        /// </summary>
        bool NoIcon { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this host should skip
        /// loading and saving its profile-based settings.
        /// </summary>
        bool NoProfile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this host should disable
        /// interactive cancellation (for example, the cancel key handler).
        /// </summary>
        bool NoCancel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this host echoes the input
        /// it reads back to its output.
        /// </summary>
        bool Echo { get; set; }

        /// <summary>
        /// This method returns a snapshot of this host's current state, with
        /// the amount of detail controlled by the supplied flags (this backs
        /// the <c>host query</c> sub-command).
        /// </summary>
        /// <param name="detailFlags">
        /// The flags that select how much state detail is included in the
        /// result.
        /// </param>
        /// <returns>
        /// A list describing the requested host state.
        /// </returns>
        StringList QueryState(
            DetailFlags detailFlags /* in */
            ); /* [host query] */

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
        bool Beep(
            int frequency, /* in */
            int duration   /* in */
            );

        /// <summary>
        /// This method determines whether the host currently has no pending
        /// interactive input or output activity.
        /// </summary>
        /// <returns>
        /// True if the host is idle; otherwise, false.
        /// </returns>
        bool IsIdle();

        /// <summary>
        /// This method clears the host's display area, when supported.
        /// </summary>
        /// <returns>
        /// True if the display was cleared; otherwise, false.
        /// </returns>
        bool Clear();

        /// <summary>
        /// This method resets this host's configuration flags to their
        /// default values.
        /// </summary>
        /// <returns>
        /// True if the flags were reset; otherwise, false.
        /// </returns>
        bool ResetHostFlags();

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
        ReturnCode ResetHistory(
            ref Result error /* out */
            );

        /// <summary>
        /// This method retrieves the current mode of one of the host's
        /// standard channels.
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
        ReturnCode GetMode(
            ChannelType channelType, /* in */
            ref uint mode,           /* out */
            ref Result error         /* out */
            );

        /// <summary>
        /// This method sets the mode of one of the host's standard channels.
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
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        ReturnCode SetMode(
            ChannelType channelType, /* in */
            uint mode,               /* in */
            ref Result error         /* out */
            );

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
        ReturnCode Open(
            ref Result error /* out */
            );

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
        ReturnCode Close(
            ref Result error /* out */
            );

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
        ReturnCode Discard(
            ref Result error /* out */
            );

        /// <summary>
        /// This method resets the host to its initial state, reinitializing
        /// its interactive resources.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        ReturnCode Reset(
            ref Result error /* out */
            );

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
        bool BeginSection(
            string name,           /* in */
            IClientData clientData /* in */
            );

        /// <summary>
        /// This method ends a named output section previously begun with
        /// <see cref="BeginSection" />.
        /// </summary>
        /// <param name="name">
        /// The name of the section to end.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the section, if any.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the section was ended; otherwise, false.
        /// </returns>
        bool EndSection(
            string name,           /* in */
            IClientData clientData /* in */
            );
    }
}
