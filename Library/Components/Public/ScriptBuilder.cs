/*
 * ScriptBuilder.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("18ec6c5a-4225-4336-8885-1e4ddcc40c42")]
    public sealed class ScriptBuilder : IScriptBuilder
    {
        #region Private Data
        private SortedDictionary<long, object> items;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private ScriptBuilder(
            Guid id,
            string name,
            string group,
            string description,
            IClientData clientData
            )
        {
            this.kind = IdentifierKind.ScriptBuilder;
            this.id = id;
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.items = new SortedDictionary<long, object>();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static IScriptBuilder Create()
        {
            return Create(null, null, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScriptBuilder Create(
            string name,
            string group,
            string description,
            IClientData clientData
            )
        {
            return new ScriptBuilder(
                Guid.Empty, name, group, description, clientData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private long GetNextKey()
        {
            long result = 0;

            if (items != null)
                result += ((long)items.Count + 1);

            return result;
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

        #region IScriptBuilder Members
        public int Count
        {
            get
            {
                return (items != null) ?
                    items.Count : _Constants.Count.Invalid;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Clear(
            ref Result error
            )
        {
            if (items == null)
            {
                error = "items not available";
                return ReturnCode.Error;
            }

            items.Clear();
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Add(
            string text,
            ref Result error
            )
        {
            if (text == null)
            {
                error = "invalid script";
                return ReturnCode.Error;
            }

            if (items == null)
            {
                error = "items not available";
                return ReturnCode.Error;
            }

            items.Add(GetNextKey(), text);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Add(
            IStringList arguments,
            ref Result error
            )
        {
            if (arguments == null)
            {
                error = "invalid argument list";
                return ReturnCode.Error;
            }

            if (items == null)
            {
                error = "items not available";
                return ReturnCode.Error;
            }

            items.Add(GetNextKey(), arguments);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Add(
            IScript script,
            ref Result error
            )
        {
            if (script == null)
            {
                error = "invalid script";
                return ReturnCode.Error;
            }

            if (items == null)
            {
                error = "items not available";
                return ReturnCode.Error;
            }

            items.Add(GetNextKey(), script);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Add(
            IScriptBuilder builder,
            ref Result error
            )
        {
            if (builder == null)
            {
                error = "invalid script builder";
                return ReturnCode.Error;
            }

            if (Object.ReferenceEquals(builder, this))
            {
                error = "cannot add script builder instance to itself";
                return ReturnCode.Error;
            }

            if (items == null)
            {
                error = "items not available";
                return ReturnCode.Error;
            }

            items.Add(GetNextKey(), builder);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public string GetString(
            bool nested
            )
        {
            if (items == null)
                return null;

            StringBuilder result = StringBuilderFactory.Create();

            foreach (KeyValuePair<long, object> pair in items)
            {
                object value = pair.Value;

                if (value == null)
                    continue;

                ///////////////////////////////////////////////////////////////

                //
                // HACK: Add a command separator to the overall result.  This
                //       may have issues if literal strings are mixed in with
                //       the actual commands, especially if they contain any
                //       line-ending characters.  Right now, this is not done
                //       if the current value happens to be a string instead
                //       of a string list.
                //
                if ((result.Length > 0) && !(value is string))
                {
                    result.Append(nested ?
                        Characters.SemiColon : Characters.LineFeed);
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Always attempt to normalize the block line-endings to
                //       line-feed only, as required by the script engine.
                //
                StringBuilder block = StringBuilderFactory.Create(
                    (value is IScript) ? ((IScript)value).Text :
                    value.ToString());

                StringOps.FixupLineEndings(block);

                ///////////////////////////////////////////////////////////////

                result.Append(block);

                ///////////////////////////////////////////////////////////////

                StringBuilderCache.Release(ref block);
            }

            if (nested && (result.Length > 0))
            {
                result.Insert(0, Characters.OpenBracket);
                result.Append(Characters.CloseBracket);
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public IScript GetScript(
            bool nested
            )
        {
            return Script.Create(
                name, group, description, ScriptTypes.Invalid,
                GetString(nested), TimeOps.GetUtcNow(),
                EngineMode.EvaluateScript, ScriptFlags.None,
                EngineFlags.None, SubstitutionFlags.Default,
                EventFlags.None, ExpressionFlags.Default,
                clientData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return GetString(false);
        }
        #endregion
    }
}
