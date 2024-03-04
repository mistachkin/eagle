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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("725d7253-4202-43e0-b9d4-c78193b447f8")]
    public sealed class ShellCallbackData : IShellCallbackData
    {
        #region Private Constructors
        private ShellCallbackData()
        {
            this.kind = IdentifierKind.ShellCallbackData;
            this.id = AttributeOps.GetObjectId(this);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static IShellCallbackData Create()
        {
            return new ShellCallbackData();
        }

        ///////////////////////////////////////////////////////////////////////

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

        internal static EvaluateScriptCallback GetEvaluateScriptCallback(
            IShellCallbackData callbackData
            )
        {
            if (callbackData == null)
                return null;

            return callbackData.EvaluateScriptCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static EvaluateFileCallback GetEvaluateFileCallback(
            IShellCallbackData callbackData
            )
        {
            if (callbackData == null)
                return null;

            return callbackData.EvaluateFileCallback;
        }

        ///////////////////////////////////////////////////////////////////////

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
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IShellManager Members
        private PreviewArgumentCallback previewArgumentCallback;
        public PreviewArgumentCallback PreviewArgumentCallback
        {
            get { return previewArgumentCallback; }
            set { previewArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private UnknownArgumentCallback unknownArgumentCallback;
        public UnknownArgumentCallback UnknownArgumentCallback
        {
            get { return unknownArgumentCallback; }
            set { unknownArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EvaluateScriptCallback evaluateScriptCallback;
        public EvaluateScriptCallback EvaluateScriptCallback
        {
            get { return evaluateScriptCallback; }
            set { evaluateScriptCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EvaluateFileCallback evaluateFileCallback;
        public EvaluateFileCallback EvaluateFileCallback
        {
            get { return evaluateFileCallback; }
            set { evaluateFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EvaluateEncodedFileCallback evaluateEncodedFileCallback;
        public EvaluateEncodedFileCallback EvaluateEncodedFileCallback
        {
            get { return evaluateEncodedFileCallback; }
            set { evaluateEncodedFileCallback = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInteractiveLoopManager Members
#if DEBUGGER
        private InteractiveLoopCallback interactiveLoopCallback;
        public InteractiveLoopCallback InteractiveLoopCallback
        {
            get { return interactiveLoopCallback; }
            set { interactiveLoopCallback = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IShellCallbackData Members
        private bool whatIf;
        public bool WhatIf
        {
            get { return whatIf; }
            set { whatIf = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool stopOnUnknown;
        public bool StopOnUnknown
        {
            get { return stopOnUnknown; }
            set { stopOnUnknown = value; }
        }

        ///////////////////////////////////////////////////////////////////////

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
        private bool hadPreviewArgumentCallback;
        private bool HadPreviewArgumentCallback
        {
            get { return hadPreviewArgumentCallback; }
            set { hadPreviewArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hadUnknownArgumentCallback;
        private bool HadUnknownArgumentCallback
        {
            get { return hadUnknownArgumentCallback; }
            set { hadUnknownArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hadEvaluateScriptCallback;
        private bool HadEvaluateScriptCallback
        {
            get { return hadEvaluateScriptCallback; }
            set { hadEvaluateScriptCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hadEvaluateFileCallback;
        private bool HadEvaluateFileCallback
        {
            get { return hadEvaluateFileCallback; }
            set { hadEvaluateFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hadEvaluateEncodedFileCallback;
        private bool HadEvaluateEncodedFileCallback
        {
            get { return hadEvaluateEncodedFileCallback; }
            set { hadEvaluateEncodedFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        private bool hadInteractiveLoopCallback;
        private bool HadInteractiveLoopCallback
        {
            get { return hadInteractiveLoopCallback; }
            set { hadInteractiveLoopCallback = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private bool initialized;
        private bool Initialized
        {
            get { return initialized; }
            set { initialized = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods
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
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
