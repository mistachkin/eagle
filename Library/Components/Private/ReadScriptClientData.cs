/*
 * ReadScriptClientData.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("4f9aa772-b73f-479f-92d9-0d4eb32a1910")]
    internal class ReadScriptClientData : ClientData, IHaveText
    {
        #region Private Constructors
        private ReadScriptClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        protected ReadScriptClientData(
            object data,           /* in */
            string scriptFileName, /* in */
            string originalText,   /* in */
            string text,           /* in */
            ByteList bytes         /* in */
            )
            : this(data)
        {
            this.scriptFileName = scriptFileName;
            this.originalText = originalText;
            this.text = text;
            this.bytes = bytes;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        //
        // NOTE: This constructor does not contain a typo.  It is passing its
        //       "bytes" parameter twice into the other constructor, once for
        //       the "data" parameter and once for the "bytes" parameter.  It
        //       will allow callers to obtain the value of "bytes" using the
        //       IClientData interface.
        //
        public ReadScriptClientData(
            string scriptFileName, /* in */
            string originalText,   /* in */
            string text,           /* in */
            ByteList bytes         /* in */
            )
            : this(bytes, scriptFileName, originalText, text, bytes)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ReadScriptClientData(
            object data,                             /* in */
            GetScriptClientData getScriptClientData, /* in */
            string scriptFileName                    /* in */
            )
            : this(data)
        {
            MaybeInitializeFrom(getScriptClientData, scriptFileName);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void MaybeInitializeFrom(
            GetScriptClientData getScriptClientData, /* in */
            string scriptFileName                    /* in */
            )
        {
            if (getScriptClientData != null)
            {
                this.scriptFileName = getScriptClientData.ScriptFileName;
                this.originalText = getScriptClientData.OriginalText;
                this.text = getScriptClientData.Text;
                this.bytes = getScriptClientData.Bytes;
            }

            if (scriptFileName != null)
                this.scriptFileName = scriptFileName;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveText Members
        private string originalText;
        public string OriginalText
        {
            get { return originalText; }
            set { originalText = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string text;
        public string Text
        {
            get { return text; }
            set { text = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private string scriptFileName;
        public string ScriptFileName
        {
            get { return scriptFileName; }
            set { scriptFileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ByteList bytes;
        public ByteList Bytes
        {
            get { return bytes; }
            set { bytes = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public virtual IStringList ToList()
        {
            IStringList list = new StringPairList();
            object data = this.Data;

            if (data != null)
                list.Add("Data", data.ToString());

            if (scriptFileName != null)
                list.Add("ScriptFileName", scriptFileName);

            if (originalText != null)
                list.Add("OriginalText", originalText);

            if (text != null)
                list.Add("Text", text);

            if (bytes != null)
                list.Add("Bytes", bytes.ToString());

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToList().ToString();
        }
        #endregion
    }
}
