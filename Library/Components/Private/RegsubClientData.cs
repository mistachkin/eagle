/*
 * RegsubClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a container for the client data used by a regular
    /// expression substitution operation, capturing the compiled expression, the
    /// pattern, the input and replacement text, their script locations, and the
    /// various flags that control the substitution behavior.
    /// </summary>
    [ObjectId("9ed12565-e6f5-467e-aa35-43aa7ae02288")]
    internal sealed class RegsubClientData : ClientData
    {
        /// <summary>
        /// Constructs an instance of this class wrapping the specified opaque
        /// data payload.
        /// </summary>
        /// <param name="data">
        /// The opaque data payload to associate with this object.  This
        /// parameter may be null.
        /// </param>
        public RegsubClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class capturing the full set of state
        /// describing a regular expression substitution operation.
        /// </summary>
        /// <param name="data">
        /// The opaque data payload to associate with this object.  This
        /// parameter may be null.
        /// </param>
        /// <param name="regEx">
        /// The compiled regular expression used by the substitution.
        /// </param>
        /// <param name="pattern">
        /// The regular expression pattern used by the substitution.
        /// </param>
        /// <param name="input">
        /// The input string to which the substitution is applied.
        /// </param>
        /// <param name="replacement">
        /// The replacement string or script used by the substitution.
        /// </param>
        /// <param name="replacementLocation">
        /// The script location of the replacement, if any.
        /// </param>
        /// <param name="text">
        /// The text being processed by the substitution.
        /// </param>
        /// <param name="textLocation">
        /// The script location of the text, if any.
        /// </param>
        /// <param name="count">
        /// The number of substitutions performed.
        /// </param>
        /// <param name="quote">
        /// Non-zero if replacement values should be quoted.
        /// </param>
        /// <param name="extra">
        /// Non-zero if extra processing is enabled for the substitution.
        /// </param>
        /// <param name="strict">
        /// Non-zero if strict processing is enabled for the substitution.
        /// </param>
        /// <param name="verbatim">
        /// Non-zero if the replacement should be treated verbatim.
        /// </param>
        /// <param name="literal">
        /// Non-zero if the replacement should be treated as a literal string.
        /// </param>
        public RegsubClientData(
            object data,
            Regex regEx,
            string pattern,
            string input,
            string replacement,
            IScriptLocation replacementLocation,
            string text,
            IScriptLocation textLocation,
            int count,
            bool quote,
            bool extra,
            bool strict,
            bool verbatim,
            bool literal
            )
            : this(data)
        {
            this.regEx = regEx;
            this.pattern = pattern;
            this.input = input;
            this.replacement = replacement;
            this.replacementLocation = replacementLocation;
            this.text = text;
            this.textLocation = textLocation;
            this.count = count;
            this.quote = quote;
            this.extra = extra;
            this.strict = strict;
            this.verbatim = verbatim;
            this.literal = literal;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the compiled regular expression used by the substitution.
        /// </summary>
        private Regex regEx;
        /// <summary>
        /// Gets or sets the compiled regular expression used by the
        /// substitution.
        /// </summary>
        public Regex RegEx
        {
            get { return regEx; }
            set { regEx = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the regular expression pattern used by the substitution.
        /// </summary>
        private string pattern;
        /// <summary>
        /// Gets or sets the regular expression pattern used by the substitution.
        /// </summary>
        public string Pattern
        {
            get { return pattern; }
            set { pattern = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the input string to which the substitution is applied.
        /// </summary>
        private string input;
        /// <summary>
        /// Gets or sets the input string to which the substitution is applied.
        /// </summary>
        public string Input
        {
            get { return input; }
            set { input = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the replacement string or script used by the substitution.
        /// </summary>
        private string replacement;
        /// <summary>
        /// Gets or sets the replacement string or script used by the
        /// substitution.
        /// </summary>
        public string Replacement
        {
            get { return replacement; }
            set { replacement = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the script location of the replacement.
        /// </summary>
        private IScriptLocation replacementLocation;
        /// <summary>
        /// Gets or sets the script location of the replacement.
        /// </summary>
        public IScriptLocation ReplacementLocation
        {
            get { return replacementLocation; }
            set { replacementLocation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the text being processed by the substitution.
        /// </summary>
        private string text;
        /// <summary>
        /// Gets or sets the text being processed by the substitution.
        /// </summary>
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the script location of the text.
        /// </summary>
        private IScriptLocation textLocation;
        /// <summary>
        /// Gets or sets the script location of the text.
        /// </summary>
        public IScriptLocation TextLocation
        {
            get { return textLocation; }
            set { textLocation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the number of substitutions performed.
        /// </summary>
        private int count;
        /// <summary>
        /// Gets or sets the number of substitutions performed.
        /// </summary>
        public int Count
        {
            get { return count; }
            set { count = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether replacement values should be
        /// quoted.
        /// </summary>
        private bool quote;
        /// <summary>
        /// Gets or sets a value indicating whether replacement values should be
        /// quoted.
        /// </summary>
        public bool Quote
        {
            get { return quote; }
            set { quote = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether extra processing is enabled for the
        /// substitution.
        /// </summary>
        private bool extra;
        /// <summary>
        /// Gets or sets a value indicating whether extra processing is enabled
        /// for the substitution.
        /// </summary>
        public bool Extra
        {
            get { return extra; }
            set { extra = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether strict processing is enabled for
        /// the substitution.
        /// </summary>
        private bool strict;
        /// <summary>
        /// Gets or sets a value indicating whether strict processing is enabled
        /// for the substitution.
        /// </summary>
        public bool Strict
        {
            get { return strict; }
            set { strict = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the replacement should be treated
        /// verbatim.
        /// </summary>
        private bool verbatim;
        /// <summary>
        /// Gets or sets a value indicating whether the replacement should be
        /// treated verbatim.
        /// </summary>
        public bool Verbatim
        {
            get { return verbatim; }
            set { verbatim = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the replacement should be treated
        /// as a literal string.
        /// </summary>
        private bool literal;
        /// <summary>
        /// Gets or sets a value indicating whether the replacement should be
        /// treated as a literal string.
        /// </summary>
        public bool Literal
        {
            get { return literal; }
            set { literal = value; }
        }
    }
}
