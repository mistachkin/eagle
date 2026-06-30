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
    /// <summary>
    /// This class is a proof-of-concept Windows Forms top-level window that is
    /// associated with an Eagle interpreter.  It registers itself in the
    /// interpreter's collection of top-level windows and allows buttons to be
    /// added to it dynamically from script.
    /// </summary>
    [ObjectId("19b4ce06-7a3a-4b84-9e51-c759484f7750")]
    public partial class Toplevel : Form
    {
        /// <summary>
        /// The name of the interpreter object that holds the collection of
        /// top-level windows.
        /// </summary>
        internal const string CollectionName = "toplevels";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This delegate represents a method used to add a button to a
        /// top-level window.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the new button.
        /// </param>
        /// <param name="text">
        /// The text to display on the new button.
        /// </param>
        /// <param name="left">
        /// The horizontal (left) position of the new button.
        /// </param>
        /// <param name="top">
        /// The vertical (top) position of the new button.
        /// </param>
        /// <param name="clickHandler">
        /// The event handler to invoke when the new button is clicked.
        /// </param>
        [ObjectId("0e8aedf8-ec1b-4143-8dd6-2e5e2ea67557")]
        public delegate void AddButtonDelegate(
            string name,
            string text,
            int left,
            int top,
            EventHandler clickHandler
        );

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The interpreter that this top-level window is associated with.
        /// </summary>
        private Interpreter interpreter;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        private Toplevel()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds a button with the specified properties to this
        /// top-level window.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the new button.
        /// </param>
        /// <param name="text">
        /// The text to display on the new button.
        /// </param>
        /// <param name="left">
        /// The horizontal (left) position of the new button.
        /// </param>
        /// <param name="top">
        /// The vertical (top) position of the new button.
        /// </param>
        /// <param name="clickHandler">
        /// The event handler to invoke when the new button is clicked.
        /// </param>
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

        /// <summary>
        /// This method registers this top-level window, under the specified
        /// name, in the associated interpreter's collection of top-level
        /// windows, creating that collection if it does not already exist.
        /// </summary>
        /// <param name="name">
        /// The name under which to register this top-level window.
        /// </param>
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

        /// <summary>
        /// Constructs an instance of this class that is associated with the
        /// specified interpreter and registers it, under the specified name,
        /// in that interpreter's collection of top-level windows.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to associate with this top-level window.
        /// </param>
        /// <param name="name">
        /// The name under which to register this top-level window.
        /// </param>
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
