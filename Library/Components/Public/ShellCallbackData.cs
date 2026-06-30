/*
 * ShellCallbackData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class holds the set of callbacks and associated state used by the
    /// Eagle shell to preview command-line arguments, handle unknown
    /// arguments, evaluate scripts and files, and (optionally) enter the
    /// interactive loop.  It implements <see cref="IShellCallbackData" /> and
    /// tracks which callbacks were already present so they can be selectively
    /// reset or left intact.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("725d7253-4202-43e0-b9d4-c78193b447f8")]
    public sealed class ShellCallbackData : IShellCallbackData
    {
        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of this class, initializing its identifier
        /// kind and object identifier.
        /// </summary>
        private ShellCallbackData()
        {
            this.kind = IdentifierKind.ShellCallbackData;
            this.id = AttributeOps.GetObjectId(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class, copying the callbacks and
        /// associated state from the specified shell callback data.
        /// </summary>
        /// <param name="callbackData">
        /// The shell callback data to copy from.  This parameter may be null.
        /// </param>
        private ShellCallbackData(
            IShellCallbackData callbackData
            )
            : this()
        {
            Copy(callbackData as ShellCallbackData, this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new, empty shell callback data instance.
        /// </summary>
        /// <returns>
        /// The newly created <see cref="IShellCallbackData" /> instance.
        /// </returns>
        public static IShellCallbackData Create()
        {
            return new ShellCallbackData();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new shell callback data instance that is a
        /// copy of the specified shell callback data.
        /// </summary>
        /// <param name="callbackData">
        /// The shell callback data to copy from.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created <see cref="IShellCallbackData" /> instance.
        /// </returns>
        //
        // NOTE: For use by the PrivateShellMainCore method only.
        //
        internal static IShellCallbackData Create(
            IShellCallbackData callbackData
            )
        {
            return new ShellCallbackData(callbackData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Static Methods
        /// <summary>
        /// This method attempts to obtain the preview argument callback from the
        /// specified shell callback data.
        /// </summary>
        /// <param name="callbackData">
        /// The shell callback data to query.  This parameter may be null.
        /// </param>
        /// <param name="previewArgumentCallback">
        /// Upon success, this parameter will be set to the preview argument
        /// callback.  Upon failure, this parameter will be set to null.
        /// </param>
        /// <returns>
        /// True if a non-null preview argument callback was obtained;
        /// otherwise, false.
        /// </returns>
        internal static bool GetPreviewArgumentCallback(
            IShellCallbackData callbackData,
            out PreviewArgumentCallback previewArgumentCallback
            )
        {
            if (callbackData == null)
            {
                previewArgumentCallback = null;
                return false;
            }

            previewArgumentCallback = callbackData.PreviewArgumentCallback;
            return previewArgumentCallback != null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the unknown argument callback from the
        /// specified shell callback data.
        /// </summary>
        /// <param name="callbackData">
        /// The shell callback data to query.  This parameter may be null.
        /// </param>
        /// <param name="unknownArgumentCallback">
        /// Upon success, this parameter will be set to the unknown argument
        /// callback.  Upon failure, this parameter will be set to null.
        /// </param>
        /// <returns>
        /// True if a non-null unknown argument callback was obtained;
        /// otherwise, false.
        /// </returns>
        internal static bool GetUnknownArgumentCallback(
            IShellCallbackData callbackData,
            out UnknownArgumentCallback unknownArgumentCallback
            )
        {
            if (callbackData == null)
            {
                unknownArgumentCallback = null;
                return false;
            }

            unknownArgumentCallback = callbackData.UnknownArgumentCallback;
            return unknownArgumentCallback != null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the evaluate script callback from the specified
        /// shell callback data.
        /// </summary>
        /// <param name="callbackData">
        /// The shell callback data to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The evaluate script callback, or null if none is available.
        /// </returns>
        internal static EvaluateScriptCallback GetEvaluateScriptCallback(
            IShellCallbackData callbackData
            )
        {
            if (callbackData == null)
                return null;

            return callbackData.EvaluateScriptCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the evaluate file callback from the specified
        /// shell callback data.
        /// </summary>
        /// <param name="callbackData">
        /// The shell callback data to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The evaluate file callback, or null if none is available.
        /// </returns>
        internal static EvaluateFileCallback GetEvaluateFileCallback(
            IShellCallbackData callbackData
            )
        {
            if (callbackData == null)
                return null;

            return callbackData.EvaluateFileCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the evaluate encoded file callback from the
        /// specified shell callback data.
        /// </summary>
        /// <param name="callbackData">
        /// The shell callback data to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The evaluate encoded file callback, or null if none is available.
        /// </returns>
        internal static EvaluateEncodedFileCallback GetEvaluateEncodedFileCallback(
            IShellCallbackData callbackData
            )
        {
            if (callbackData == null)
                return null;

            return callbackData.EvaluateEncodedFileCallback;
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        /// <summary>
        /// This method attempts to obtain the interactive loop callback from the
        /// specified shell callback data.
        /// </summary>
        /// <param name="callbackData">
        /// The shell callback data to query.  This parameter may be null.
        /// </param>
        /// <param name="interactiveLoopCallback">
        /// Upon success, this parameter will be set to the interactive loop
        /// callback.  Upon failure, this parameter will be set to null.
        /// </param>
        /// <returns>
        /// True if a non-null interactive loop callback was obtained;
        /// otherwise, false.
        /// </returns>
        internal static bool GetInteractiveLoopCallback(
            IShellCallbackData callbackData,
            out InteractiveLoopCallback interactiveLoopCallback
            )
        {
            if (callbackData == null)
            {
                interactiveLoopCallback = null;
                return false;
            }

            interactiveLoopCallback = callbackData.InteractiveLoopCallback;
            return interactiveLoopCallback != null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method copies the callbacks and associated state from the source
        /// shell callback data to the target shell callback data.
        /// </summary>
        /// <param name="sourceCallbackData">
        /// The shell callback data to copy from.  This parameter may be null.
        /// </param>
        /// <param name="targetCallbackData">
        /// The shell callback data to copy to.  This parameter may be null.
        /// </param>
        private static void Copy(
            ShellCallbackData sourceCallbackData,
            ShellCallbackData targetCallbackData
            )
        {
            if ((sourceCallbackData == null) ||
                (targetCallbackData == null))
            {
                return;
            }

            targetCallbackData.PreviewArgumentCallback =
                sourceCallbackData.PreviewArgumentCallback;

            targetCallbackData.UnknownArgumentCallback =
                sourceCallbackData.UnknownArgumentCallback;

            targetCallbackData.EvaluateScriptCallback =
                sourceCallbackData.EvaluateScriptCallback;

            targetCallbackData.EvaluateFileCallback =
                sourceCallbackData.EvaluateFileCallback;

            targetCallbackData.EvaluateEncodedFileCallback =
                sourceCallbackData.EvaluateEncodedFileCallback;

#if DEBUGGER
            targetCallbackData.InteractiveLoopCallback =
                sourceCallbackData.InteractiveLoopCallback;
#endif

            targetCallbackData.HadPreviewArgumentCallback =
                sourceCallbackData.HadPreviewArgumentCallback;

            targetCallbackData.HadUnknownArgumentCallback =
                sourceCallbackData.HadUnknownArgumentCallback;

            targetCallbackData.HadEvaluateScriptCallback =
                sourceCallbackData.HadEvaluateScriptCallback;

            targetCallbackData.HadEvaluateFileCallback =
                sourceCallbackData.HadEvaluateFileCallback;

            targetCallbackData.HadEvaluateEncodedFileCallback =
                sourceCallbackData.HadEvaluateEncodedFileCallback;

#if DEBUGGER
            targetCallbackData.HadInteractiveLoopCallback =
                sourceCallbackData.HadInteractiveLoopCallback;
#endif

            targetCallbackData.Initialized =
                sourceCallbackData.Initialized;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this shell callback data.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this shell callback data.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Stores the identifier kind of this shell callback data.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this shell callback data.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this shell callback data.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this shell callback
        /// data.
        /// </summary>
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the client data associated with this shell callback data.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this shell callback
        /// data.
        /// </summary>
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Stores the group of this shell callback data.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this shell callback data.
        /// </summary>
        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this shell callback data.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this shell callback data.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IShellManager Members
        /// <summary>
        /// Stores the callback used to preview command-line arguments.
        /// </summary>
        private PreviewArgumentCallback previewArgumentCallback;
        /// <summary>
        /// Gets or sets the callback used to preview command-line arguments.
        /// </summary>
        public PreviewArgumentCallback PreviewArgumentCallback
        {
            get { return previewArgumentCallback; }
            set { previewArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the callback used to handle unknown command-line arguments.
        /// </summary>
        private UnknownArgumentCallback unknownArgumentCallback;
        /// <summary>
        /// Gets or sets the callback used to handle unknown command-line
        /// arguments.
        /// </summary>
        public UnknownArgumentCallback UnknownArgumentCallback
        {
            get { return unknownArgumentCallback; }
            set { unknownArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the callback used to evaluate a script.
        /// </summary>
        private EvaluateScriptCallback evaluateScriptCallback;
        /// <summary>
        /// Gets or sets the callback used to evaluate a script.
        /// </summary>
        public EvaluateScriptCallback EvaluateScriptCallback
        {
            get { return evaluateScriptCallback; }
            set { evaluateScriptCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the callback used to evaluate a file.
        /// </summary>
        private EvaluateFileCallback evaluateFileCallback;
        /// <summary>
        /// Gets or sets the callback used to evaluate a file.
        /// </summary>
        public EvaluateFileCallback EvaluateFileCallback
        {
            get { return evaluateFileCallback; }
            set { evaluateFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the callback used to evaluate an encoded file.
        /// </summary>
        private EvaluateEncodedFileCallback evaluateEncodedFileCallback;
        /// <summary>
        /// Gets or sets the callback used to evaluate an encoded file.
        /// </summary>
        public EvaluateEncodedFileCallback EvaluateEncodedFileCallback
        {
            get { return evaluateEncodedFileCallback; }
            set { evaluateEncodedFileCallback = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInteractiveLoopManager Members
#if DEBUGGER
        /// <summary>
        /// Stores the callback used to enter the interactive loop.
        /// </summary>
        private InteractiveLoopCallback interactiveLoopCallback;
        /// <summary>
        /// Gets or sets the callback used to enter the interactive loop.
        /// </summary>
        public InteractiveLoopCallback InteractiveLoopCallback
        {
            get { return interactiveLoopCallback; }
            set { interactiveLoopCallback = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IShellCallbackData Members
        /// <summary>
        /// Stores a value indicating whether the shell should operate in
        /// what-if mode (i.e. without making changes).
        /// </summary>
        private bool whatIf;
        /// <summary>
        /// Gets or sets a value indicating whether the shell should operate in
        /// what-if mode (i.e. without making changes).
        /// </summary>
        public bool WhatIf
        {
            get { return whatIf; }
            set { whatIf = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the shell should stop processing
        /// upon encountering an unknown argument.
        /// </summary>
        private bool stopOnUnknown;
        /// <summary>
        /// Gets or sets a value indicating whether the shell should stop
        /// processing upon encountering an unknown argument.
        /// </summary>
        public bool StopOnUnknown
        {
            get { return stopOnUnknown; }
            set { stopOnUnknown = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records which callbacks are already present so that they
        /// can later be treated as pre-existing.  It has no effect once this
        /// shell callback data has been initialized.
        /// </summary>
        public void CheckForPreExisting()
        {
            if (initialized)
                return;

            hadPreviewArgumentCallback = (previewArgumentCallback != null);
            hadUnknownArgumentCallback = (unknownArgumentCallback != null);
            hadEvaluateScriptCallback = (evaluateScriptCallback != null);
            hadEvaluateFileCallback = (evaluateFileCallback != null);

            hadEvaluateEncodedFileCallback =
                (evaluateEncodedFileCallback != null);

#if DEBUGGER
            hadInteractiveLoopCallback = (interactiveLoopCallback != null);
#endif

            initialized = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets each callback to the specified value, optionally
        /// preserving any callback that was already present.  When a callback
        /// was pre-existing and <paramref name="resetPreExisting" /> is false,
        /// the existing value is retained.
        /// </summary>
        /// <param name="previewArgumentCallback">
        /// The new preview argument callback.  This parameter may be null.
        /// </param>
        /// <param name="unknownArgumentCallback">
        /// The new unknown argument callback.  This parameter may be null.
        /// </param>
        /// <param name="evaluateScriptCallback">
        /// The new evaluate script callback.  This parameter may be null.
        /// </param>
        /// <param name="evaluateFileCallback">
        /// The new evaluate file callback.  This parameter may be null.
        /// </param>
        /// <param name="evaluateEncodedFileCallback">
        /// The new evaluate encoded file callback.  This parameter may be null.
        /// </param>
        /// <param name="interactiveLoopCallback">
        /// The new interactive loop callback.  This parameter may be null.
        /// </param>
        /// <param name="resetPreExisting">
        /// Non-zero to overwrite callbacks that were already present; zero to
        /// leave pre-existing callbacks unchanged.
        /// </param>
        public void SetNewOrResetPreExisting(
            PreviewArgumentCallback previewArgumentCallback,
            UnknownArgumentCallback unknownArgumentCallback,
            EvaluateScriptCallback evaluateScriptCallback,
            EvaluateFileCallback evaluateFileCallback,
            EvaluateEncodedFileCallback evaluateEncodedFileCallback,
#if DEBUGGER
            InteractiveLoopCallback interactiveLoopCallback,
#endif
            bool resetPreExisting
            )
        {
            if (resetPreExisting || !hadPreviewArgumentCallback)
                this.previewArgumentCallback = previewArgumentCallback;

            if (resetPreExisting || !hadUnknownArgumentCallback)
                this.unknownArgumentCallback = unknownArgumentCallback;

            if (resetPreExisting || !hadEvaluateScriptCallback)
                this.evaluateScriptCallback = evaluateScriptCallback;

            if (resetPreExisting || !hadEvaluateFileCallback)
                this.evaluateFileCallback = evaluateFileCallback;

            if (resetPreExisting || !hadEvaluateEncodedFileCallback)
                this.evaluateEncodedFileCallback = evaluateEncodedFileCallback;

#if DEBUGGER
            if (resetPreExisting || !hadInteractiveLoopCallback)
                this.interactiveLoopCallback = interactiveLoopCallback;
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Properties
        /// <summary>
        /// Stores a value indicating whether a preview argument callback was
        /// already present.
        /// </summary>
        private bool hadPreviewArgumentCallback;
        /// <summary>
        /// Gets or sets a value indicating whether a preview argument callback
        /// was already present.
        /// </summary>
        private bool HadPreviewArgumentCallback
        {
            get { return hadPreviewArgumentCallback; }
            set { hadPreviewArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether an unknown argument callback was
        /// already present.
        /// </summary>
        private bool hadUnknownArgumentCallback;
        /// <summary>
        /// Gets or sets a value indicating whether an unknown argument callback
        /// was already present.
        /// </summary>
        private bool HadUnknownArgumentCallback
        {
            get { return hadUnknownArgumentCallback; }
            set { hadUnknownArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether an evaluate script callback was
        /// already present.
        /// </summary>
        private bool hadEvaluateScriptCallback;
        /// <summary>
        /// Gets or sets a value indicating whether an evaluate script callback
        /// was already present.
        /// </summary>
        private bool HadEvaluateScriptCallback
        {
            get { return hadEvaluateScriptCallback; }
            set { hadEvaluateScriptCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether an evaluate file callback was
        /// already present.
        /// </summary>
        private bool hadEvaluateFileCallback;
        /// <summary>
        /// Gets or sets a value indicating whether an evaluate file callback
        /// was already present.
        /// </summary>
        private bool HadEvaluateFileCallback
        {
            get { return hadEvaluateFileCallback; }
            set { hadEvaluateFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether an evaluate encoded file callback
        /// was already present.
        /// </summary>
        private bool hadEvaluateEncodedFileCallback;
        /// <summary>
        /// Gets or sets a value indicating whether an evaluate encoded file
        /// callback was already present.
        /// </summary>
        private bool HadEvaluateEncodedFileCallback
        {
            get { return hadEvaluateEncodedFileCallback; }
            set { hadEvaluateEncodedFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        /// <summary>
        /// Stores a value indicating whether an interactive loop callback was
        /// already present.
        /// </summary>
        private bool hadInteractiveLoopCallback;
        /// <summary>
        /// Gets or sets a value indicating whether an interactive loop callback
        /// was already present.
        /// </summary>
        private bool HadInteractiveLoopCallback
        {
            get { return hadInteractiveLoopCallback; }
            set { hadInteractiveLoopCallback = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the pre-existing callback state of
        /// this shell callback data has been initialized.
        /// </summary>
        private bool initialized;
        /// <summary>
        /// Gets or sets a value indicating whether the pre-existing callback
        /// state of this shell callback data has been initialized.
        /// </summary>
        private bool Initialized
        {
            get { return initialized; }
            set { initialized = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods
        /// <summary>
        /// This method produces a string describing the callbacks and
        /// associated state of this shell callback data, suitable for
        /// diagnostic display.
        /// </summary>
        /// <returns>
        /// A string containing the trace details of this shell callback data.
        /// </returns>
        internal string ToTraceString()
        {
            IStringList list = new StringPairList();

            list.Add("PreviewArgumentCallback", FormatOps.WrapOrNull(
                FormatOps.DelegateName(previewArgumentCallback)));

            list.Add("UnknownArgumentCallback", FormatOps.WrapOrNull(
                FormatOps.DelegateName(unknownArgumentCallback)));

            list.Add("EvaluateScriptCallback", FormatOps.WrapOrNull(
                FormatOps.DelegateName(evaluateScriptCallback)));

            list.Add("EvaluateFileCallback", FormatOps.WrapOrNull(
                FormatOps.DelegateName(evaluateFileCallback)));

            list.Add("EvaluateEncodedFileCallback", FormatOps.WrapOrNull(
                FormatOps.DelegateName(evaluateEncodedFileCallback)));

#if DEBUGGER
            list.Add("InteractiveLoopCallback", FormatOps.WrapOrNull(
                FormatOps.DelegateName(interactiveLoopCallback)));
#endif

            list.Add("HadPreviewArgumentCallback",
                hadPreviewArgumentCallback.ToString());

            list.Add("HadUnknownArgumentCallback",
                hadUnknownArgumentCallback.ToString());

            list.Add("HadEvaluateScriptCallback",
                hadEvaluateScriptCallback.ToString());

            list.Add("HadEvaluateFileCallback",
                hadEvaluateFileCallback.ToString());

            list.Add("HadEvaluateEncodedFileCallback",
                hadEvaluateEncodedFileCallback.ToString());

#if DEBUGGER
            list.Add("HadInteractiveLoopCallback",
                hadInteractiveLoopCallback.ToString());
#endif

            list.Add("Initialized", initialized.ToString());

            return list.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string describing this shell callback data
        /// using its name only.
        /// </summary>
        /// <returns>
        /// A string containing the name of this shell callback data (or an
        /// empty string when it has no name).
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
