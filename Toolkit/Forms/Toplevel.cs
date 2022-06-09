/*
 * Toplevel.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

///////////////////////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* 
//
// Please do not use this code, it is a proof-of-concept only.  It is not production ready.
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* 
///////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Forms
{
    [ObjectId("19b4ce06-7a3a-4b84-9e51-c759484f7750")]
    public partial class Toplevel : Form
    {
        internal const string CollectionName = "toplevels";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [ObjectId("0e8aedf8-ec1b-4143-8dd6-2e5e2ea67557")]
        public delegate void AddButtonDelegate(
            string name,
            string text,
            int left,
            int top,
            EventHandler clickHandler
        );

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Interpreter interpreter;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Toplevel()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal void AddButton(
            string name,
            string text,
            int left,
            int top,
            EventHandler clickHandler
            )
        {
            Button button = new Button();

            button.Name = name;
            button.Text = text;
            button.Left = left;
            button.Top = top;
            button.Click += clickHandler;

            Controls.Add(button);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private void AddToplevel(
            string name
            )
        {
            ReturnCode code;
            Result result = null;
            IObject @object = null;
            ToplevelDictionary toplevels = null;

            code = interpreter.GetObject(
                CollectionName, LookupFlags.Default,
                ref @object, ref result);

            if (code == ReturnCode.Ok)
            {
                toplevels = @object.Value as ToplevelDictionary;

                if (toplevels == null)
                {
                    toplevels = new ToplevelDictionary();
                    @object.Value = toplevels;
                }

                toplevels.Add(name, new AnyPair<Thread, Toplevel>(
                    Thread.CurrentThread, this));
            }
            else
            {
                toplevels = new ToplevelDictionary();

                toplevels.Add(name, new AnyPair<Thread, Toplevel>(
                    Thread.CurrentThread, this));

                long token = 0;

                code = interpreter.AddObject(
                    CollectionName, null, ObjectFlags.Default,
                    ClientData.Empty, 0,
#if NATIVE && TCL
                    null,
#endif
#if DEBUGGER && DEBUGGER_ARGUMENTS
                    null,
#endif
                    toplevels, ref token, ref result);
            }

            if (code != ReturnCode.Ok)
                throw new ScriptException(code, result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Toplevel(
            Interpreter interpreter,
            string name
            )
            : this()
        {
            this.interpreter = interpreter;

            AddToplevel(name);

            InitializeComponent();
        }
    }
}
