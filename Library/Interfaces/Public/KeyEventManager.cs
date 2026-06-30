/*
 * KeyEventManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that manage the mappings
    /// between key events and their associated callbacks, including firing the
    /// registered handlers for a key event and querying, adding, and removing
    /// the individual key event mappings.
    /// </summary>
    [ObjectId("b241f495-011e-45ab-aa37-78abc354f0f5")]
    public interface IKeyEventManager
    {
        /// <summary>
        /// Fires the handlers registered for the specified key event.
        /// </summary>
        /// <param name="eventType">
        /// The type of event being fired.
        /// </param>
        /// <param name="sender">
        /// The object that is the source of the event.  This parameter may be
        /// null.
        /// </param>
        /// <param name="e">
        /// The event arguments associated with the event.  This parameter may
        /// be null.
        /// </param>
        /// <param name="count">
        /// Upon success, this will contain the number of handlers that were
        /// fired.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode FireKeyEventHandlers(
            EventType eventType,
            object sender,
            EventArgs e,
            ref int count,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether a key event mapping exists for the specified
        /// event type, modifier keys, and keys.
        /// </summary>
        /// <param name="eventType">
        /// The type of event to check for a mapping.
        /// </param>
        /// <param name="modifiers">
        /// The modifier keys that the mapping must match.
        /// </param>
        /// <param name="keys">
        /// The keys that the mapping must match.
        /// </param>
        /// <param name="useOverrides">
        /// Non-zero if any override mappings should be considered, null to use
        /// the default behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a matching mapping exists;
        /// otherwise, a non-Ok value with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode HasKeyEventMapping(
            EventType eventType,
            Keys modifiers,
            Keys keys,
            bool? useOverrides,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the callback associated with the key event mapping for the
        /// specified event type, modifier keys, and keys.
        /// </summary>
        /// <param name="eventType">
        /// The type of event to look up a mapping for.
        /// </param>
        /// <param name="modifiers">
        /// The modifier keys that the mapping must match.
        /// </param>
        /// <param name="keys">
        /// The keys that the mapping must match.
        /// </param>
        /// <param name="useOverrides">
        /// Non-zero if any override mappings should be considered, null to use
        /// the default behavior.
        /// </param>
        /// <param name="callback">
        /// Upon success, this will contain the callback associated with the
        /// matching mapping.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetKeyEventMapping(
            EventType eventType,
            Keys modifiers,
            Keys keys,
            bool? useOverrides,
            ref FormEventCallback callback,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds a key event mapping that associates the specified callback with
        /// the specified event type, modifier keys, and keys.
        /// </summary>
        /// <param name="eventType">
        /// The type of event to add a mapping for.
        /// </param>
        /// <param name="modifiers">
        /// The modifier keys that the mapping should match.
        /// </param>
        /// <param name="keys">
        /// The keys that the mapping should match.
        /// </param>
        /// <param name="callback">
        /// The callback to associate with the mapping.
        /// </param>
        /// <param name="useOverrides">
        /// Non-zero if the mapping should be added as an override, null to use
        /// the default behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode AddKeyEventMapping(
            EventType eventType,
            Keys modifiers,
            Keys keys,
            FormEventCallback callback,
            bool? useOverrides,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the key event mapping associated with the specified event
        /// type, modifier keys, and keys.
        /// </summary>
        /// <param name="eventType">
        /// The type of event to remove a mapping for.
        /// </param>
        /// <param name="modifiers">
        /// The modifier keys that the mapping must match.
        /// </param>
        /// <param name="keys">
        /// The keys that the mapping must match.
        /// </param>
        /// <param name="useOverrides">
        /// Non-zero if any override mappings should be considered, null to use
        /// the default behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode RemoveKeyEventMapping(
            EventType eventType,
            Keys modifiers,
            Keys keys,
            bool? useOverrides,
            ref Result error
        );
    }
}
