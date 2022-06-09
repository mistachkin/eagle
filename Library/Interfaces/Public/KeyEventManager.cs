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
    [ObjectId("b241f495-011e-45ab-aa37-78abc354f0f5")]
    public interface IKeyEventManager
    {
        ReturnCode FireKeyEventHandlers(
            EventType eventType,
            object sender,
            EventArgs e,
            ref int count,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode HasKeyEventMapping(
            EventType eventType,
            Keys modifiers,
            Keys keys,
            bool? useOverrides,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetKeyEventMapping(
            EventType eventType,
            Keys modifiers,
            Keys keys,
            bool? useOverrides,
            ref FormEventCallback callback,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode AddKeyEventMapping(
            EventType eventType,
            Keys modifiers,
            Keys keys,
            FormEventCallback callback,
            bool? useOverrides,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode RemoveKeyEventMapping(
            EventType eventType,
            Keys modifiers,
            Keys keys,
            bool? useOverrides,
            ref Result error
        );
    }
}
