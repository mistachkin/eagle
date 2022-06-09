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
    [ObjectId("9d09f3f3-b11a-444d-8a17-368c72d8ef84")]
    internal static class KeyOps
    {
        #region Keyboard Mappings Data Class
        [ObjectId("bf67db97-801c-4fcf-b4c7-3a44335daa2e")]
        internal sealed class KeyEventMap
        {
            #region Private Constants
            //
            // HACK: These are purposely not read-only.
            //
            private static ReturnCode? CallbackNotInvoked = null;
            private static ReturnCode? CallbackWasInvoked = ReturnCode.Ok;
            private static ReturnCode? CallbackDidThrow = ReturnCode.Error;

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: This is purposely not read-only.
            //
            private static FormEventResultTriplet CallbackResult = null;

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: These are purposely not read-only.
            //
            private static bool CallbackTraceThrow = true;
            private static bool CallbackReThrow = false;
            private static bool CallbackApplyEventArgs = true;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Data
            private readonly object syncRoot = new object();
            private EventTypesModifiersKeysDictionary eventTypeMappings;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Constructors
            private KeyEventMap()
            {
                Initialize(false);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static KeyEventMap Create()
            {
                return new KeyEventMap();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Methods
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

                            list.Add(key1.ToString(), null);
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

                                list.Add(key2.ToString(), null);
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
        private static ReturnCode DefaultNullCode = ReturnCode.Continue;

        ///////////////////////////////////////////////////////////////////////

        private static readonly string DisplayNone = "<none>";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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

        private static string ToString(
            EventType eventType /* in */
            )
        {
            return (eventType != EventType.None) ?
                eventType.ToString() : DisplayNone;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string ToString(
            Keys keys /* in */
            )
        {
            return (keys != Keys.None) ? keys.ToString() : DisplayNone;
        }

        ///////////////////////////////////////////////////////////////////////

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
