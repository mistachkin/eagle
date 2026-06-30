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
    /// <summary>
    /// This class provides a container for the client data produced when a script
    /// file is read, capturing the script file name, the original and processed
    /// text, the raw bytes, and a flag indicating whether reading errors should
    /// be silent.
    /// </summary>
    [ObjectId("4f9aa772-b73f-479f-92d9-0d4eb32a1910")]
    internal class ReadScriptClientData : ClientData, IHaveText
    {
        #region Private Constants
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default value used to indicate whether script reading is silent
        /// when no specific value is available.
        /// </summary>
        private static bool DefaultSilent = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Helper Methods
        /// <summary>
        /// This method determines whether script reading should be silent, based
        /// on the supplied client data, falling back to the default value when
        /// no client data is available.
        /// </summary>
        /// <param name="clientData">
        /// The client data to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if script reading should be silent; otherwise, false.
        /// </returns>
        public static bool IsSilent(
            ReadScriptClientData clientData /* in */
            )
        {
            if (clientData != null)
                return clientData.Silent;

            return DefaultSilent;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class wrapping the specified opaque
        /// data payload.
        /// </summary>
        /// <param name="data">
        /// The opaque data payload to associate with this object.  This
        /// parameter may be null.
        /// </param>
        private ReadScriptClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class capturing the full set of state
        /// describing a script that has been read.
        /// </summary>
        /// <param name="data">
        /// The opaque data payload to associate with this object.  This
        /// parameter may be null.
        /// </param>
        /// <param name="scriptFileName">
        /// The name of the script file that was read.  This parameter may be
        /// null.
        /// </param>
        /// <param name="originalText">
        /// The original, unprocessed text of the script.  This parameter may be
        /// null.
        /// </param>
        /// <param name="text">
        /// The processed text of the script.  This parameter may be null.
        /// </param>
        /// <param name="bytes">
        /// The raw bytes of the script.  This parameter may be null.
        /// </param>
        /// <param name="silent">
        /// Non-zero if errors encountered while reading the script should be
        /// silent.
        /// </param>
        protected ReadScriptClientData(
            object data,           /* in */
            string scriptFileName, /* in */
            string originalText,   /* in */
            string text,           /* in */
            ByteList bytes,        /* in */
            bool silent            /* in */
            )
            : this(data)
        {
            this.scriptFileName = scriptFileName;
            this.originalText = originalText;
            this.text = text;
            this.bytes = bytes;
            this.silent = silent;
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
        /// <summary>
        /// Constructs an instance of this class describing a script that has
        /// been read, exposing the raw bytes through both the client data
        /// payload and the dedicated bytes property.
        /// </summary>
        /// <param name="scriptFileName">
        /// The name of the script file that was read.  This parameter may be
        /// null.
        /// </param>
        /// <param name="originalText">
        /// The original, unprocessed text of the script.  This parameter may be
        /// null.
        /// </param>
        /// <param name="text">
        /// The processed text of the script.  This parameter may be null.
        /// </param>
        /// <param name="bytes">
        /// The raw bytes of the script.  This parameter may be null.
        /// </param>
        /// <param name="silent">
        /// Non-zero if errors encountered while reading the script should be
        /// silent.
        /// </param>
        public ReadScriptClientData(
            string scriptFileName, /* in */
            string originalText,   /* in */
            string text,           /* in */
            ByteList bytes,        /* in */
            bool silent            /* in */
            )
            : this(bytes, scriptFileName, originalText, text, bytes, silent)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class, optionally initializing its
        /// state from the supplied script client data and overriding the script
        /// file name.
        /// </summary>
        /// <param name="data">
        /// The opaque data payload to associate with this object.  This
        /// parameter may be null.
        /// </param>
        /// <param name="getScriptClientData">
        /// The script client data from which to initialize this object.  This
        /// parameter may be null.
        /// </param>
        /// <param name="scriptFileName">
        /// The script file name used to override the one obtained from
        /// <paramref name="getScriptClientData" />.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method initializes the state of this object from the supplied
        /// script client data, when available, and overrides the script file
        /// name when one is supplied.
        /// </summary>
        /// <param name="getScriptClientData">
        /// The script client data from which to initialize this object.  This
        /// parameter may be null.
        /// </param>
        /// <param name="scriptFileName">
        /// The script file name used to override the one obtained from
        /// <paramref name="getScriptClientData" />.  This parameter may be null.
        /// </param>
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
                this.silent = getScriptClientData.Silent;
            }

            if (scriptFileName != null)
                this.scriptFileName = scriptFileName;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveText Members
        /// <summary>
        /// Stores the original, unprocessed text of the script.
        /// </summary>
        private string originalText;
        /// <summary>
        /// Gets or sets the original, unprocessed text of the script.
        /// </summary>
        public string OriginalText
        {
            get { return originalText; }
            set { originalText = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the processed text of the script.
        /// </summary>
        private string text;
        /// <summary>
        /// Gets or sets the processed text of the script.
        /// </summary>
        public string Text
        {
            get { return text; }
            set { text = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Stores the name of the script file that was read.
        /// </summary>
        private string scriptFileName;
        /// <summary>
        /// Gets or sets the name of the script file that was read.
        /// </summary>
        public string ScriptFileName
        {
            get { return scriptFileName; }
            set { scriptFileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the raw bytes of the script.
        /// </summary>
        private ByteList bytes;
        /// <summary>
        /// Gets or sets the raw bytes of the script.
        /// </summary>
        public ByteList Bytes
        {
            get { return bytes; }
            set { bytes = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether errors encountered while reading
        /// the script should be silent.
        /// </summary>
        private bool silent;
        /// <summary>
        /// Gets or sets a value indicating whether errors encountered while
        /// reading the script should be silent.
        /// </summary>
        public bool Silent
        {
            get { return silent; }
            set { silent = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method produces a list of name/value pairs describing the state
        /// of this object, suitable for diagnostic display.
        /// </summary>
        /// <returns>
        /// An <see cref="IStringList" /> containing the populated details of this
        /// object.
        /// </returns>
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

            list.Add("Silent", silent.ToString());

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of the state of this
        /// object.
        /// </summary>
        /// <returns>
        /// A string containing the details of this object.
        /// </returns>
        public override string ToString()
        {
            return ToList().ToString();
        }
        #endregion
    }
}
