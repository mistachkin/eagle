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
    [ObjectId("9ed12565-e6f5-467e-aa35-43aa7ae02288")]
    internal sealed class RegsubClientData : ClientData
    {
        public RegsubClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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

        private Regex regEx;
        public Regex RegEx
        {
            get { return regEx; }
            set { regEx = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string pattern;
        public string Pattern
        {
            get { return pattern; }
            set { pattern = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string input;
        public string Input
        {
            get { return input; }
            set { input = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string replacement;
        public string Replacement
        {
            get { return replacement; }
            set { replacement = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IScriptLocation replacementLocation;
        public IScriptLocation ReplacementLocation
        {
            get { return replacementLocation; }
            set { replacementLocation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string text;
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IScriptLocation textLocation;
        public IScriptLocation TextLocation
        {
            get { return textLocation; }
            set { textLocation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int count;
        public int Count
        {
            get { return count; }
            set { count = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool quote;
        public bool Quote
        {
            get { return quote; }
            set { quote = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool extra;
        public bool Extra
        {
            get { return extra; }
            set { extra = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool strict;
        public bool Strict
        {
            get { return strict; }
            set { strict = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool verbatim;
        public bool Verbatim
        {
            get { return verbatim; }
            set { verbatim = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool literal;
        public bool Literal
        {
            get { return literal; }
            set { literal = value; }
        }
    }
}
