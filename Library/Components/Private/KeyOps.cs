/*
 * KeyOps.cs --
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
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;

using FormEventResultTriplet = Eagle._Interfaces.Public.IAnyTriplet<
    bool?, bool?, Eagle._Components.Public.ReturnCode?>;

using KeysPair = System.Collections.Generic.KeyValuePair<
    System.Windows.Forms.Keys, Eagle._Components.Public.Delegates.FormEventCallback>;

using ModifiersPair = System.Collections.Generic.KeyValuePair<
    System.Windows.Forms.Keys, System.Collections.Generic.Dictionary<
        System.Windows.Forms.Keys, Eagle._Components.Public.Delegates.FormEventCallback>>;

using EventTypePair = System.Collections.Generic.KeyValuePair<
    Eagle._Components.Public.EventType, System.Collections.Generic.Dictionary<
        System.Windows.Forms.Keys, System.Collections.Generic.Dictionary<
            System.Windows.Forms.Keys, Eagle._Components.Public.Delegates.FormEventCallback>>>;

using EventTypeList = System.Collections.Generic.List<Eagle._Components.Public.EventType>;
using KeysList = System.Collections.Generic.List<System.Windows.Forms.Keys>;

using KeysDictionary = System.Collections.Generic.Dictionary<
    System.Windows.Forms.Keys, Eagle._Components.Public.Delegates.FormEventCallback>;

using ModifiersKeysDictionary = System.Collections.Generic.Dictionary<
    System.Windows.Forms.Keys, System.Collections.Generic.Dictionary<
        System.Windows.Forms.Keys, Eagle._Components.Public.Delegates.FormEventCallback>>;

using EventTypesModifiersKeysDictionary = System.Collections.Generic.Dictionary<
    Eagle._Components.Public.EventType, System.Collections.Generic.Dictionary<
        System.Windows.Forms.Keys, System.Collections.Generic.Dictionary<
            System.Windows.Forms.Keys, Eagle._Components.Public.Delegates.FormEventCallback>>>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods for mapping keyboard events
    /// (by event type, modifier keys, and key) to callbacks, and for chaining
    /// those callbacks together when dispatching Windows Forms keyboard events.
    /// </summary>
    [ObjectId("9d09f3f3-b11a-444d-8a17-368c72d8ef84")]
    internal static class KeyOps
    {
        #region Keyboard Mappings Data Class
        /// <summary>
        /// This class maintains a set of mappings from keyboard events, keyed
        /// by event type, then modifier keys, then key, to the callbacks that
        /// should be invoked to handle them.
        /// </summary>
        [ObjectId("bf67db97-801c-4fcf-b4c7-3a44335daa2e")]
        internal sealed class KeyEventMap
        {
            #region Private Constants
            //
            // HACK: These are purposely not read-only.
            //
            /// <summary>
            /// The result used to indicate that a callback was not invoked.
            /// </summary>
            private static ReturnCode? CallbackNotInvoked = null;

            /// <summary>
            /// The result used to indicate that a callback was invoked
            /// successfully.
            /// </summary>
            private static ReturnCode? CallbackWasInvoked = ReturnCode.Ok;

            /// <summary>
            /// The result used to indicate that a callback threw an exception.
            /// </summary>
            private static ReturnCode? CallbackDidThrow = ReturnCode.Error;

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: This is purposely not read-only.
            //
            /// <summary>
            /// The default result triplet used prior to invoking a callback.
            /// </summary>
            private static FormEventResultTriplet CallbackResult = null;

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: These are purposely not read-only.
            //
            /// <summary>
            /// When non-zero, exceptions thrown by a callback are traced.
            /// </summary>
            private static bool CallbackTraceThrow = true;

            /// <summary>
            /// When non-zero, exceptions thrown by a callback are re-thrown.
            /// </summary>
            private static bool CallbackReThrow = false;

            /// <summary>
            /// When non-zero, the result of a callback is applied back to the
            /// event arguments.
            /// </summary>
            private static bool CallbackApplyEventArgs = true;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Data
            /// <summary>
            /// The object used to synchronize access to the instance data of
            /// this object.
            /// </summary>
            private readonly object syncRoot = new object();

            /// <summary>
            /// The mappings from event type, then modifier keys, then key, to
            /// the associated callbacks.
            /// </summary>
            private EventTypesModifiersKeysDictionary eventTypeMappings;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Constructors
            /// <summary>
            /// Constructs an instance of this class.
            /// </summary>
            private KeyEventMap()
            {
                Initialize(false);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            /// <summary>
            /// This method creates a new instance of this class.
            /// </summary>
            /// <returns>
            /// The newly created instance.
            /// </returns>
            public static KeyEventMap Create()
            {
                return new KeyEventMap();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Methods
            /// <summary>
            /// This method (re)initializes the event type mappings for this
            /// object.
            /// </summary>
            /// <param name="force">
            /// Non-zero to force the mappings to be re-created even if they
            /// already exist.
            /// </param>
            private void Initialize(
                bool force /* in */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    KeyOps.InitializeEventTypeMappings(
                        ref eventTypeMappings, force);
                }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method attempts to find the callback associated with the
            /// specified event type and event arguments.
            /// </summary>
            /// <param name="eventType">
            /// The type of event to look up.
            /// </param>
            /// <param name="e">
            /// The event arguments describing the event, including the modifier
            /// keys and key.
            /// </param>
            /// <param name="callback">
            /// Upon success, receives the associated callback; otherwise,
            /// receives null.
            /// </param>
            /// <returns>
            /// True if a callback was found; otherwise, false.
            /// </returns>
            private bool TryGetCallback(
                EventType eventType,           /* in */
                EventArgs e,                   /* in */
                out FormEventCallback callback /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (e == null)
                    {
                        callback = null;
                        return false;
                    }

                    ModifiersKeysDictionary modifierMappings;

                    if (!KeyOps.TryGetEventTypeMapping(
                            eventTypeMappings, eventType,
                            false, false, out modifierMappings))
                    {
                        callback = null;
                        return false;
                    }

                    Keys modifiers = Keys.None;
                    Keys keys = Keys.None;

                    if (!ExtractFromEventArgs(
                            e, ref modifiers, ref keys))
                    {
                        callback = null;
                        return false;
                    }

                    KeysDictionary keyMappings;

                    if (!KeyOps.TryGetModifierMapping(
                            modifierMappings, modifiers,
                            false, false, out keyMappings))
                    {
                        callback = null;
                        return false;
                    }

                    if (!KeyOps.TryGetCallback(
                            keyMappings, modifiers, keys,
                            true, out callback))
                    {
                        return false;
                    }

                    return true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            /// <summary>
            /// This method dispatches the specified event to its associated
            /// callback, if any, applying the callback's result back to the
            /// event arguments.
            /// </summary>
            /// <param name="eventType">
            /// The type of event being handled.
            /// </param>
            /// <param name="sender">
            /// The object that raised the event.
            /// </param>
            /// <param name="e">
            /// The event arguments describing the event.
            /// </param>
            /// <returns>
            /// The return code produced by the callback, or a sentinel value
            /// indicating that no callback was invoked or that the callback
            /// threw an exception.
            /// </returns>
            public ReturnCode? EventHandler(
                EventType eventType, /* in */
                object sender,       /* in */
                EventArgs e          /* in */
                )
            {
                if (e == null)
                    return CallbackNotInvoked;

                FormEventCallback callback;

                if (!TryGetCallback(eventType, e, out callback))
                    return CallbackNotInvoked;

                if (callback == null)
                    return CallbackNotInvoked;

                FormEventResultTriplet triplet = CallbackResult;

                try
                {
                    triplet = callback(eventType, sender, e);

                    return ExtractReturnCode(
                        triplet, CallbackWasInvoked);
                }
                catch (Exception ex)
                {
                    if (CallbackTraceThrow)
                    {
                        TraceOps.DebugTrace(
                            ex, typeof(KeyEventMap).Name,
                            TracePriority.EventError);
                    }

                    if (CallbackReThrow)
                        throw;

                    return CallbackDidThrow;
                }
                finally
                {
                    if (CallbackApplyEventArgs)
                        ApplyToEventArgs(triplet, e);
                }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method determines whether a callback is associated with the
            /// specified event type, modifier keys, and key.
            /// </summary>
            /// <param name="eventType">
            /// The type of event to look up.
            /// </param>
            /// <param name="modifiers">
            /// The modifier keys to look up.
            /// </param>
            /// <param name="keys">
            /// The key to look up.
            /// </param>
            /// <param name="error">
            /// Upon failure, receives information about the error.
            /// </param>
            /// <returns>
            /// True if a matching callback exists; otherwise, false.
            /// </returns>
            public bool Has(
                EventType eventType, /* in */
                Keys modifiers,      /* in */
                Keys keys,           /* in */
                ref Result error     /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    FormEventCallback callback; /* NOT USED */

                    return Get(
                        eventType, modifiers, keys, out callback, ref error);
                }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the callback associated with the specified
            /// event type, modifier keys, and key.
            /// </summary>
            /// <param name="eventType">
            /// The type of event to look up.
            /// </param>
            /// <param name="modifiers">
            /// The modifier keys to look up.
            /// </param>
            /// <param name="keys">
            /// The key to look up.
            /// </param>
            /// <param name="callback">
            /// Upon success, receives the associated callback; otherwise,
            /// receives null.
            /// </param>
            /// <param name="error">
            /// Upon failure, receives information about the error.
            /// </param>
            /// <returns>
            /// True if a matching callback was found; otherwise, false.
            /// </returns>
            public bool Get(
                EventType eventType,            /* in */
                Keys modifiers,                 /* in */
                Keys keys,                      /* in */
                out FormEventCallback callback, /* out */
                ref Result error                /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    ModifiersKeysDictionary modifierMappings;

                    if (!KeyOps.TryGetEventTypeMapping(
                            eventTypeMappings, eventType, false,
                            false, false, out modifierMappings,
                            ref error))
                    {
                        callback = null;
                        return false;
                    }

                    KeysDictionary keyMappings;

                    if (!KeyOps.TryGetModifierMapping(
                            modifierMappings, modifiers, false,
                            false, false, out keyMappings,
                            ref error))
                    {
                        callback = null;
                        return false;
                    }

                    if (!KeyOps.TryGetCallback(
                            keyMappings, modifiers, keys, true,
                            false, out callback, ref error))
                    {
                        return false;
                    }

                    return true;
                }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method adds a description of the configured mappings,
            /// optionally filtered by event type, modifier keys, and key, to
            /// the specified list.
            /// </summary>
            /// <param name="eventType">
            /// The event type to filter by, or null for all event types.
            /// </param>
            /// <param name="modifiers">
            /// The modifier keys to filter by, or null for all modifier keys.
            /// </param>
            /// <param name="keys">
            /// The key to filter by, or null for all keys.
            /// </param>
            /// <param name="list">
            /// The list to which the descriptions should be added, created if
            /// necessary.
            /// </param>
            /// <param name="error">
            /// Upon failure, receives information about the error.
            /// </param>
            /// <returns>
            /// True if the descriptions were added successfully; otherwise,
            /// false.
            /// </returns>
            public bool List(
                EventType? eventType,    /* in */
                Keys? modifiers,         /* in */
                Keys? keys,              /* in */
                ref StringPairList list, /* in, out */
                ref Result error         /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (eventTypeMappings == null)
                    {
                        error = "event type mappings not available";
                        return false;
                    }

                    foreach (EventTypePair pair1 in eventTypeMappings)
                    {
                        EventType key1 = pair1.Key;

                        if ((eventType != null) &&
                            (key1 != (EventType)eventType))
                        {
                            continue;
                        }

                        ModifiersKeysDictionary modifierMappings = pair1.Value;

                        if (modifierMappings == null)
                        {
                            if (list == null)
                                list = new StringPairList();

                            list.Add(key1.ToString(), (string)null);
                            continue;
                        }

                        StringPairList subList1 = null;

                        foreach (ModifiersPair pair2 in modifierMappings)
                        {
                            Keys key2 = pair2.Key;

                            if ((modifiers != null) &&
                                (key2 != (Keys)modifiers))
                            {
                                continue;
                            }

                            KeysDictionary keyMappings = pair2.Value;

                            if (keyMappings == null)
                            {
                                if (list == null)
                                    list = new StringPairList();

                                list.Add(key2.ToString(), (string)null);
                                continue;
                            }

                            StringPairList subList2 = new StringPairList();

                            foreach (KeysPair pair3 in keyMappings)
                            {
                                Keys key3 = pair3.Key;

                                if ((keys != null) &&
                                    (key3 != (Keys)keys))
                                {
                                    continue;
                                }

                                subList2.Add(key3.ToString(),
                                    (pair3.Value != null).ToString());
                            }

                            if (subList1 == null)
                                subList1 = new StringPairList();

                            subList1.Add(key2.ToString(), subList2.ToString());
                        }

                        if (subList1 != null)
                        {
                            if (list == null)
                                list = new StringPairList();

                            list.Add(key1.ToString(), subList1.ToString());
                        }
                    }

                    return true;
                }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method adds, overwrites, or validates the callback
            /// associated with the specified event type, modifier keys, and
            /// key.
            /// </summary>
            /// <param name="eventType">
            /// The type of event to change.
            /// </param>
            /// <param name="modifiers">
            /// The modifier keys to change.
            /// </param>
            /// <param name="keys">
            /// The key to change.
            /// </param>
            /// <param name="callback">
            /// The callback to associate with the event type, modifier keys,
            /// and key.
            /// </param>
            /// <param name="addEventType">
            /// Non-zero to add the event type mapping if it does not already
            /// exist.
            /// </param>
            /// <param name="addModifiers">
            /// Non-zero to add the modifier mapping if it does not already
            /// exist.
            /// </param>
            /// <param name="overwriteKeys">
            /// Non-zero to permit overwriting an existing key mapping.
            /// </param>
            /// <param name="error">
            /// Upon failure, receives information about the error.
            /// </param>
            /// <returns>
            /// True if the mapping was changed successfully; otherwise, false.
            /// </returns>
            public bool Change(
                EventType eventType,        /* in: NOT USED */
                Keys modifiers,             /* in */
                Keys keys,                  /* in */
                FormEventCallback callback, /* in */
                bool addEventType,          /* in */
                bool addModifiers,          /* in */
                bool overwriteKeys,         /* in */
                ref Result error            /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    ModifiersKeysDictionary modifierMappings;

                    if (!KeyOps.TryGetEventTypeMapping(
                            eventTypeMappings, eventType, addEventType,
                            false, false, out modifierMappings, ref error))
                    {
                        return false;
                    }

                    KeysDictionary keyMappings;

                    if (!KeyOps.TryGetModifierMapping(
                            modifierMappings, modifiers, addModifiers,
                            false, false, out keyMappings, ref error))
                    {
                        return false;
                    }

                    FormEventCallback localCallback; /* NOT USED */

                    if (!KeyOps.TryGetCallback(
                            keyMappings, modifiers, keys, overwriteKeys,
                            false, out localCallback, ref error))
                    {
                        return false;
                    }

                    keyMappings[keys] = callback;
                    return true;
                }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method adds a callback for the specified event type,
            /// modifier keys, and key.
            /// </summary>
            /// <param name="eventType">
            /// The type of event to add.
            /// </param>
            /// <param name="modifiers">
            /// The modifier keys to add.
            /// </param>
            /// <param name="keys">
            /// The key to add.
            /// </param>
            /// <param name="callback">
            /// The callback to associate with the event type, modifier keys,
            /// and key.
            /// </param>
            /// <param name="error">
            /// Upon failure, receives information about the error.
            /// </param>
            /// <returns>
            /// True if the callback was added successfully; otherwise, false.
            /// </returns>
            public bool Add(
                EventType eventType,        /* in */
                Keys modifiers,             /* in */
                Keys keys,                  /* in */
                FormEventCallback callback, /* in */
                ref Result error            /* out */
                )
            {
                return Change(
                    eventType, modifiers, keys, callback, false, false,
                    false, ref error);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method removes the callback associated with the specified
            /// event type, modifier keys, and key.
            /// </summary>
            /// <param name="eventType">
            /// The type of event to remove.
            /// </param>
            /// <param name="modifiers">
            /// The modifier keys to remove.
            /// </param>
            /// <param name="keys">
            /// The key to remove.
            /// </param>
            /// <param name="error">
            /// Upon failure, receives information about the error.
            /// </param>
            /// <returns>
            /// True if the callback was removed successfully; otherwise, false.
            /// </returns>
            public bool Remove(
                EventType eventType, /* in */
                Keys modifiers,      /* in */
                Keys keys,           /* in */
                ref Result error     /* out */
                )
            {
                return Remove(
                    eventType, modifiers, keys, false, false, ref error);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method removes the callback associated with the specified
            /// event type, modifier keys, and key, optionally removing the
            /// containing mappings once they become empty.
            /// </summary>
            /// <param name="eventType">
            /// The type of event to remove.
            /// </param>
            /// <param name="modifiers">
            /// The modifier keys to remove.
            /// </param>
            /// <param name="keys">
            /// The key to remove.
            /// </param>
            /// <param name="compactModifiers">
            /// Non-zero to also remove the modifier mapping when it becomes
            /// empty.
            /// </param>
            /// <param name="compactEventType">
            /// Non-zero to also remove the event type mapping when it becomes
            /// empty.
            /// </param>
            /// <param name="error">
            /// Upon failure, receives information about the error.
            /// </param>
            /// <returns>
            /// True if the callback was removed successfully; otherwise, false.
            /// </returns>
            public bool Remove(
                EventType eventType,   /* in */
                Keys modifiers,        /* in */
                Keys keys,             /* in */
                bool compactModifiers, /* in */
                bool compactEventType, /* in */
                ref Result error       /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    ModifiersKeysDictionary modifierMappings;

                    if (!KeyOps.TryGetEventTypeMapping(
                            eventTypeMappings, eventType, false,
                            false, false, out modifierMappings,
                            ref error))
                    {
                        return false;
                    }

                    KeysDictionary keyMappings;

                    if (!KeyOps.TryGetModifierMapping(
                            modifierMappings, modifiers, false,
                            true, false, out keyMappings,
                            ref error))
                    {
                        return false;
                    }

                    if ((keyMappings != null) &&
                        !keyMappings.Remove(keys))
                    {
                        error = String.Format(
                            "key mapping {0} not removed",
                            KeyOps.ToString(modifiers, keys));

                        return false;
                    }

                    if (compactModifiers &&
                        (keyMappings != null) &&
                        (keyMappings.Count == 0))
                    {
                        if (modifierMappings == null)
                        {
                            error = "modifier mappings not available";
                            return false;
                        }

                        if (!modifierMappings.Remove(modifiers))
                        {
                            error = String.Format(
                                "modifier mapping {0} not removed",
                                KeyOps.ToString(modifiers));

                            return false;
                        }
                    }

                    if (compactEventType &&
                        (modifierMappings != null) &&
                        (modifierMappings.Count == 0))
                    {
                        if (eventTypeMappings == null)
                        {
                            error = "event type mappings not available";
                            return false;
                        }

                        if (!eventTypeMappings.Remove(eventType))
                        {
                            error = String.Format(
                                "event type mapping {0} not removed",
                                KeyOps.ToString(eventType));

                            return false;
                        }
                    }

                    return true;
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The return code used when a callback returns a null result and no
        /// explicit null return code has been supplied.
        /// </summary>
        private static ReturnCode DefaultNullCode = ReturnCode.Continue;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The string used to display the absence of an event type, modifier
        /// keys, or key.
        /// </summary>
        private static readonly string DisplayNone = "<none>";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method dispatches the specified event to each of the supplied
        /// keyboard event maps in turn, accumulating the results.
        /// </summary>
        /// <param name="eventType">
        /// The type of event being handled.
        /// </param>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The event arguments describing the event.
        /// </param>
        /// <param name="throwCode">
        /// The return code to use when a callback throws an exception, or null.
        /// </param>
        /// <param name="nullCode">
        /// The return code to use when a callback returns a null result, or
        /// null.
        /// </param>
        /// <param name="chainCount">
        /// The running count of callbacks that were invoked, updated in place.
        /// </param>
        /// <param name="chainCode">
        /// The accumulated return code, updated in place.
        /// </param>
        /// <param name="chainError">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="args">
        /// The keyboard event maps to dispatch the event to.
        /// </param>
        public static void ChainEventHandlers(
            EventType eventType,      /* in */
            object sender,            /* in */
            EventArgs e,              /* in */
            ReturnCode? throwCode,    /* in */
            ReturnCode? nullCode,     /* in */
            ref int chainCount,       /* in, out */
            ref ReturnCode chainCode, /* in, out */
            ref Result chainError,    /* out */
            params KeyEventMap[] args /* in */
            )
        {
            ChainEventHandlers(
                args, eventType, sender, e, throwCode, nullCode,
                ref chainCount, ref chainCode, ref chainError);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method dispatches the specified event to each of the supplied
        /// keyboard event maps in turn, accumulating the results.
        /// </summary>
        /// <param name="keyEventMaps">
        /// The keyboard event maps to dispatch the event to.
        /// </param>
        /// <param name="eventType">
        /// The type of event being handled.
        /// </param>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The event arguments describing the event.
        /// </param>
        /// <param name="throwCode">
        /// The return code to use when a callback throws an exception, or null.
        /// </param>
        /// <param name="nullCode">
        /// The return code to use when a callback returns a null result, or
        /// null.
        /// </param>
        /// <param name="chainCount">
        /// The running count of callbacks that were invoked, updated in place.
        /// </param>
        /// <param name="chainCode">
        /// The accumulated return code, updated in place.
        /// </param>
        /// <param name="chainError">
        /// Upon failure, receives information about the error.
        /// </param>
        private static void ChainEventHandlers(
            IEnumerable<KeyEventMap> keyEventMaps, /* in */
            EventType eventType,                   /* in */
            object sender,                         /* in */
            EventArgs e,                           /* in */
            ReturnCode? throwCode,                 /* in */
            ReturnCode? nullCode,                  /* in */
            ref int chainCount,                    /* in, out */
            ref ReturnCode chainCode,              /* in, out */
            ref Result chainError                  /* out */
            )
        {
            if (keyEventMaps == null)
            {
                chainError = "key event data unavailable";
                chainCode = ReturnCode.Error;
                chainCount = Count.Invalid;

                return;
            }

            int localCount = chainCount;
            ReturnCode localCode = chainCode;
            Result localError = null;

            try
            {
                foreach (KeyEventMap keyEventMap in keyEventMaps)
                {
                    if (keyEventMap == null)
                        continue;

                    ReturnCode? code;

                    try
                    {
                        code = keyEventMap.EventHandler(
                            eventType, sender, e); /* throw */
                    }
                    catch (Exception ex)
                    {
                        localError = ex;
                        code = throwCode;
                    }

                    if (code == null)
                        code = nullCode;

                    if (code == null)
                        code = DefaultNullCode; /* NOT NULL */

                    switch ((ReturnCode)code)
                    {
                        case ReturnCode.Ok:
                            {
                                localCount++;
                                break;
                            }
                        case ReturnCode.Error:
                            {
                                localError = String.Format(
                                    "event handler for object {0} had error",
                                    FormatOps.WrapHashCode(keyEventMap));

                                localCount++;
                                localCode = (ReturnCode)code;
                                return;
                            }
                        case ReturnCode.Return:
                            {
                                localCount++;
                                localCode = (ReturnCode)code;
                                return;
                            }
                        case ReturnCode.Break:
                            {
                                localCode = (ReturnCode)code;
                                return;
                            }
                        case ReturnCode.Continue:
                            {
                                continue;
                            }
                    }
                }
            }
            finally
            {
                chainError = localError;
                chainCode = localCode;
                chainCount = localCount;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method initializes the event type mappings with the default
        /// event types, optionally forcing them to be re-created.
        /// </summary>
        /// <param name="eventTypeMappings">
        /// The event type mappings to initialize, in place.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the mappings to be re-created even if they already
        /// exist.
        /// </param>
        private static void InitializeEventTypeMappings(
            ref EventTypesModifiersKeysDictionary eventTypeMappings, /* in, out */
            bool force                                               /* in */
            )
        {
            if (force || (eventTypeMappings == null))
            {
                eventTypeMappings = new EventTypesModifiersKeysDictionary();

                EventTypeList eventTypes = GetDefaultEventTypes();

                if (eventTypes != null)
                {
                    foreach (EventType eventType in eventTypes)
                    {
                        ModifiersKeysDictionary modifierMappings = null;

                        InitializeModifierMappings(
                            ref modifierMappings, force);

                        eventTypeMappings.Add(
                            eventType, modifierMappings);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the modifier mappings with the default
        /// modifier key combinations, optionally forcing them to be re-created.
        /// </summary>
        /// <param name="modifierMappings">
        /// The modifier mappings to initialize, in place.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the mappings to be re-created even if they already
        /// exist.
        /// </param>
        private static void InitializeModifierMappings(
            ref ModifiersKeysDictionary modifierMappings, /* in, out */
            bool force                                    /* in */
            )
        {
            if (force || (modifierMappings == null))
            {
                modifierMappings = new ModifiersKeysDictionary();

                KeysList modifiers = GetDefaultModifiers();

                if (modifiers != null)
                {
                    foreach (Keys modifier in modifiers)
                    {
                        modifierMappings.Add(
                            modifier, new KeysDictionary());
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to get the modifier mappings for the specified
        /// event type.
        /// </summary>
        /// <param name="eventTypeMappings">
        /// The event type mappings to search.
        /// </param>
        /// <param name="eventType">
        /// The event type to look up.
        /// </param>
        /// <param name="addEventType">
        /// Non-zero to add the event type mapping if it does not already exist.
        /// </param>
        /// <param name="verifyNotNull">
        /// Non-zero to require that the resulting modifier mappings are
        /// non-null.
        /// </param>
        /// <param name="modifierMappings">
        /// Upon success, receives the modifier mappings for the event type;
        /// otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if the modifier mappings were obtained; otherwise, false.
        /// </returns>
        private static bool TryGetEventTypeMapping(
            EventTypesModifiersKeysDictionary eventTypeMappings, /* in */
            EventType eventType,                                 /* in */
            bool addEventType,                                   /* in */
            bool verifyNotNull,                                  /* in */
            out ModifiersKeysDictionary modifierMappings         /* out */
            )
        {
            Result error = null;

            return TryGetEventTypeMapping(
                eventTypeMappings, eventType, addEventType, verifyNotNull,
                true, out modifierMappings, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to get the modifier mappings for the specified
        /// event type.
        /// </summary>
        /// <param name="eventTypeMappings">
        /// The event type mappings to search.
        /// </param>
        /// <param name="eventType">
        /// The event type to look up.
        /// </param>
        /// <param name="addEventType">
        /// Non-zero to add the event type mapping if it does not already exist.
        /// </param>
        /// <param name="verifyNotNull">
        /// Non-zero to require that the resulting modifier mappings are
        /// non-null.
        /// </param>
        /// <param name="noError">
        /// Non-zero to suppress setting the error message on failure.
        /// </param>
        /// <param name="modifierMappings">
        /// Upon success, receives the modifier mappings for the event type;
        /// otherwise, receives null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the modifier mappings were obtained; otherwise, false.
        /// </returns>
        private static bool TryGetEventTypeMapping(
            EventTypesModifiersKeysDictionary eventTypeMappings, /* in */
            EventType eventType,                                 /* in */
            bool addEventType,                                   /* in */
            bool verifyNotNull,                                  /* in */
            bool noError,                                        /* in */
            out ModifiersKeysDictionary modifierMappings,        /* out */
            ref Result error                                     /* out */
            )
        {
            if (eventTypeMappings == null)
            {
                modifierMappings = null;

                if (!noError)
                    error = "event type mappings not available";

                return false;
            }

            if (!eventTypeMappings.TryGetValue(
                    eventType, out modifierMappings))
            {
                if (addEventType)
                {
                    modifierMappings = new ModifiersKeysDictionary();
                    eventTypeMappings[eventType] = modifierMappings;
                }
                else
                {
                    if (!noError)
                    {
                        error = String.Format(
                            "event type mapping {0} not found",
                            ToString(eventType));
                    }

                    return false;
                }
            }

            if (verifyNotNull && (modifierMappings == null))
            {
                if (!noError)
                {
                    error = String.Format(
                        "event type mapping {0} not available",
                        ToString(eventType));
                }

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to get the key mappings for the specified
        /// modifier keys.
        /// </summary>
        /// <param name="modifierMappings">
        /// The modifier mappings to search.
        /// </param>
        /// <param name="modifiers">
        /// The modifier keys to look up.
        /// </param>
        /// <param name="addModifiers">
        /// Non-zero to add the modifier mapping if it does not already exist.
        /// </param>
        /// <param name="verifyNotNull">
        /// Non-zero to require that the resulting key mappings are non-null.
        /// </param>
        /// <param name="keyMappings">
        /// Upon success, receives the key mappings for the modifier keys;
        /// otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if the key mappings were obtained; otherwise, false.
        /// </returns>
        private static bool TryGetModifierMapping(
            ModifiersKeysDictionary modifierMappings, /* in */
            Keys modifiers,                           /* in */
            bool addModifiers,                        /* in */
            bool verifyNotNull,                       /* in */
            out KeysDictionary keyMappings            /* out */
            )
        {
            Result error = null;

            return TryGetModifierMapping(
                modifierMappings, modifiers, addModifiers,
                verifyNotNull, true, out keyMappings, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to get the key mappings for the specified
        /// modifier keys.
        /// </summary>
        /// <param name="modifierMappings">
        /// The modifier mappings to search.
        /// </param>
        /// <param name="modifiers">
        /// The modifier keys to look up.
        /// </param>
        /// <param name="addModifiers">
        /// Non-zero to add the modifier mapping if it does not already exist.
        /// </param>
        /// <param name="verifyNotNull">
        /// Non-zero to require that the resulting key mappings are non-null.
        /// </param>
        /// <param name="noError">
        /// Non-zero to suppress setting the error message on failure.
        /// </param>
        /// <param name="keyMappings">
        /// Upon success, receives the key mappings for the modifier keys;
        /// otherwise, receives null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the key mappings were obtained; otherwise, false.
        /// </returns>
        private static bool TryGetModifierMapping(
            ModifiersKeysDictionary modifierMappings, /* in */
            Keys modifiers,                           /* in */
            bool addModifiers,                        /* in */
            bool verifyNotNull,                       /* in */
            bool noError,                             /* in */
            out KeysDictionary keyMappings,           /* out */
            ref Result error                          /* out */
            )
        {
            if (modifierMappings == null)
            {
                keyMappings = null;

                if (!noError)
                    error = "modifier mappings not available";

                return false;
            }

            if (!modifierMappings.TryGetValue(
                    modifiers, out keyMappings))
            {
                if (addModifiers)
                {
                    keyMappings = new KeysDictionary();
                    modifierMappings[modifiers] = keyMappings;
                }
                else
                {
                    if (!noError)
                    {
                        error = String.Format(
                            "modifier mapping {0} not found",
                            ToString(modifiers));
                    }

                    return false;
                }
            }

            if (verifyNotNull && (keyMappings == null))
            {
                if (!noError)
                {
                    error = String.Format(
                        "modifier mapping {0} not available",
                        ToString(modifiers));
                }

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to get (or validate the absence of) the
        /// callback for the specified key within the given key mappings.
        /// </summary>
        /// <param name="keyMappings">
        /// The key mappings to search.
        /// </param>
        /// <param name="modifiers">
        /// The modifier keys associated with the lookup, used for diagnostic
        /// messages.
        /// </param>
        /// <param name="keys">
        /// The key to look up.
        /// </param>
        /// <param name="overwriteKeys">
        /// Non-zero if an existing key mapping is expected (e.g. when
        /// overwriting); zero if the absence of a mapping is expected.
        /// </param>
        /// <param name="callback">
        /// Upon success, receives the associated callback, if any; otherwise,
        /// receives null.
        /// </param>
        /// <returns>
        /// True if the expected condition was met; otherwise, false.
        /// </returns>
        private static bool TryGetCallback(
            KeysDictionary keyMappings,    /* in */
            Keys modifiers,                /* in */
            Keys keys,                     /* in */
            bool overwriteKeys,            /* in */
            out FormEventCallback callback /* out */
            )
        {
            Result error = null;

            return TryGetCallback(
                keyMappings, modifiers, keys, overwriteKeys, true,
                out callback, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to get (or validate the absence of) the
        /// callback for the specified key within the given key mappings.
        /// </summary>
        /// <param name="keyMappings">
        /// The key mappings to search.
        /// </param>
        /// <param name="modifiers">
        /// The modifier keys associated with the lookup, used for diagnostic
        /// messages.
        /// </param>
        /// <param name="keys">
        /// The key to look up.
        /// </param>
        /// <param name="overwriteKeys">
        /// Non-zero if an existing key mapping is expected (e.g. when
        /// overwriting); zero if the absence of a mapping is expected.
        /// </param>
        /// <param name="noError">
        /// Non-zero to suppress setting the error message on failure.
        /// </param>
        /// <param name="callback">
        /// Upon success, receives the associated callback, if any; otherwise,
        /// receives null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the expected condition was met; otherwise, false.
        /// </returns>
        private static bool TryGetCallback(
            KeysDictionary keyMappings,     /* in */
            Keys modifiers,                 /* in */
            Keys keys,                      /* in */
            bool overwriteKeys,             /* in */
            bool noError,                   /* in */
            out FormEventCallback callback, /* out */
            ref Result error                /* out */
            )
        {
            if (keyMappings == null)
            {
                callback = null;

                if (!noError)
                {
                    error = String.Format(
                        "modifier mapping {0} not available",
                        ToString(modifiers));
                }

                return false;
            }

            if (keyMappings.TryGetValue(
                    keys, out callback) != overwriteKeys)
            {
                if (!noError)
                {
                    error = String.Format(
                        "key mapping {0} {1}",
                        ToString(modifiers, keys),
                        FormatOps.Exists(!overwriteKeys));
                }

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: If adding support for keyboard events other than KeyUp, e.g.
        //       ones that require something other than a KeyEventArgs object,
        //       this method (and perhaps its callers) must be modified.
        //
        /// <summary>
        /// This method extracts the modifier keys and key from the specified
        /// event arguments.
        /// </summary>
        /// <param name="e">
        /// The event arguments to extract from.
        /// </param>
        /// <param name="modifiers">
        /// Upon success, receives the modifier keys.
        /// </param>
        /// <param name="keys">
        /// Upon success, receives the key.
        /// </param>
        /// <returns>
        /// True if the modifier keys and key were extracted; otherwise, false.
        /// </returns>
        private static bool ExtractFromEventArgs(
            EventArgs e,        /* in */
            ref Keys modifiers, /* out */
            ref Keys keys       /* out */
            )
        {
            if (e == null)
                return false;

            KeyEventArgs keyEventArgs = e as KeyEventArgs;

            if (keyEventArgs == null)
                return false;

            modifiers = keyEventArgs.Modifiers;
            keys = keyEventArgs.KeyCode;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the return code from the specified result
        /// triplet, falling back to a default when none is present.
        /// </summary>
        /// <param name="triplet">
        /// The result triplet to extract from, or null.
        /// </param>
        /// <param name="default">
        /// The default return code to use when the triplet does not specify
        /// one.
        /// </param>
        /// <returns>
        /// The extracted return code, or the supplied default.
        /// </returns>
        private static ReturnCode? ExtractReturnCode(
            FormEventResultTriplet triplet, /* in */
            ReturnCode? @default            /* in */
            )
        {
            if ((triplet != null) && (triplet.Z != null))
                return triplet.Z;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: If adding support for keyboard events other than KeyUp, e.g.
        //       ones that require something other than a KeyEventArgs object,
        //       this method (and perhaps its callers) must be modified.
        //
        /// <summary>
        /// This method applies the values from the specified result triplet
        /// back to the given event arguments.
        /// </summary>
        /// <param name="triplet">
        /// The result triplet whose values should be applied, or null.
        /// </param>
        /// <param name="e">
        /// The event arguments to update.
        /// </param>
        /// <returns>
        /// True if the values were applied; otherwise, false.
        /// </returns>
        private static bool ApplyToEventArgs(
            FormEventResultTriplet triplet, /* in */
            EventArgs e                     /* in, out */
            )
        {
            if ((triplet == null) || (e == null))
                return false;

            KeyEventArgs keyEventArgs = e as KeyEventArgs;

            if (keyEventArgs == null)
                return false;

            if (triplet.X != null)
                keyEventArgs.SuppressKeyPress = (bool)triplet.X;

            if (triplet.Y != null)
                keyEventArgs.Handled = (bool)triplet.Y;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified event type for display.
        /// </summary>
        /// <param name="eventType">
        /// The event type to format.
        /// </param>
        /// <returns>
        /// The display string for the event type.
        /// </returns>
        private static string ToString(
            EventType eventType /* in */
            )
        {
            return (eventType != EventType.None) ?
                eventType.ToString() : DisplayNone;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified key for display.
        /// </summary>
        /// <param name="keys">
        /// The key to format.
        /// </param>
        /// <returns>
        /// The display string for the key.
        /// </returns>
        private static string ToString(
            Keys keys /* in */
            )
        {
            return (keys != Keys.None) ? keys.ToString() : DisplayNone;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified modifier keys and key for display.
        /// </summary>
        /// <param name="modifiers">
        /// The modifier keys to format.
        /// </param>
        /// <param name="keys">
        /// The key to format.
        /// </param>
        /// <returns>
        /// The display string for the modifier keys and key.
        /// </returns>
        private static string ToString(
            Keys modifiers, /* in */
            Keys keys       /* in */
            )
        {
            if (modifiers != Keys.None)
            {
                if (keys != Keys.None)
                {
                    return String.Format(
                        "{0} + {1}", modifiers, keys);
                }
                else
                {
                    return modifiers.ToString();
                }
            }
            else if (keys != Keys.None)
            {
                return keys.ToString();
            }
            else
            {
                return DisplayNone;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the list of default keyboard event types.
        /// </summary>
        /// <returns>
        /// The list of default keyboard event types.
        /// </returns>
        private static EventTypeList GetDefaultEventTypes()
        {
            EventTypeList result = new EventTypeList();

            result.Add(EventType.PreviewKeyDown);
            result.Add(EventType.KeyDown);
            result.Add(EventType.KeyPress);
            result.Add(EventType.KeyUp);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the list of default modifier key combinations.
        /// </summary>
        /// <returns>
        /// The list of default modifier key combinations.
        /// </returns>
        private static KeysList GetDefaultModifiers()
        {
            KeysList result = new KeysList();

            result.Add(Keys.Shift);
            result.Add(Keys.Control);
            result.Add(Keys.Alt);
            result.Add(Keys.Shift | Keys.Control);
            result.Add(Keys.Shift | Keys.Alt);
            result.Add(Keys.Shift | Keys.Control | Keys.Alt);

            return result;
        }
        #endregion
    }
}
