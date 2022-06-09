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
    internal sealed class ReadScriptClientData : ClientData
    {
        #region Private Constructors
        private ReadScriptClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
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
            string fileName,
            string originalText,
            string text,
            ByteList bytes
            )
            : this(bytes, fileName, originalText, text, bytes)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ReadScriptClientData(
            object data,
            string fileName,
            string originalText,
            string text,
            ByteList bytes
            )
            : this(data)
        {
            this.fileName = fileName;
            this.originalText = originalText;
            this.text = text;
            this.bytes = bytes;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private string fileName;
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

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
        public IStringList ToList()
        {
            IStringList list = new StringPairList();
            object data = this.Data;

            if (data != null)
                list.Add("Data", data.ToString());

            if (fileName != null)
                list.Add("FileName", fileName);

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
