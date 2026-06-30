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
    /// <summary>
    /// This class accumulates script fragments -- literal command text, string
    /// lists of arguments, scripts, and other script builders -- and combines
    /// them, in insertion order, into a single well-formed Eagle script.  It
    /// implements <see cref="IScriptBuilder" /> and can produce either a plain
    /// string form (optionally nested as a bracketed command) or a complete
    /// <see cref="IScript" /> instance.
    /// </summary>
    [ObjectId("18ec6c5a-4225-4336-8885-1e4ddcc40c42")]
    public sealed class ScriptBuilder : IScriptBuilder
    {
        #region Private Data
        /// <summary>
        /// Stores the accumulated script fragments, keyed by their insertion
        /// order.
        /// </summary>
        private SortedDictionary<long, object> items;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a script builder from the specified identity parameters.
        /// This constructor provides internal support for the <c>Create</c>
        /// static factory methods.
        /// </summary>
        /// <param name="id">
        /// The globally unique identifier of the script builder.
        /// </param>
        /// <param name="name">
        /// The name of the script builder.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of the script builder.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of the script builder.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the script builder.  This parameter
        /// may be null.
        /// </param>
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
        /// <summary>
        /// This method creates a new, empty script builder with no identity
        /// information.
        /// </summary>
        /// <returns>
        /// A new <see cref="IScriptBuilder" /> instance.
        /// </returns>
        public static IScriptBuilder Create()
        {
            return Create(null, null, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new, empty script builder with the specified
        /// identity information.
        /// </summary>
        /// <param name="name">
        /// The name of the script builder.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of the script builder.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of the script builder.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the script builder.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="IScriptBuilder" /> instance.
        /// </returns>
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
        /// <summary>
        /// This method computes the next key to use when adding a script
        /// fragment, preserving the insertion order of the fragments.
        /// </summary>
        /// <returns>
        /// The next key to use for an added script fragment.
        /// </returns>
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
        /// <summary>
        /// Stores the name of the script builder.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of the script builder.
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
        /// Stores the identifier kind of the script builder.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of the script builder.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of the script builder.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of the script builder.
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
        /// Stores the client data associated with the script builder.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with the script builder.
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
        /// Stores the group of the script builder.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of the script builder.
        /// </summary>
        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of the script builder.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of the script builder.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptBuilder Members
        /// <summary>
        /// Gets the number of script fragments currently accumulated by this
        /// script builder, or an invalid count when the fragment collection is
        /// not available.
        /// </summary>
        public int Count
        {
            get
            {
                return (items != null) ?
                    items.Count : _Constants.Count.Invalid;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all of the accumulated script fragments from this
        /// script builder.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
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

        /// <summary>
        /// This method appends the specified literal script text to this script
        /// builder.
        /// </summary>
        /// <param name="text">
        /// The literal script text to append.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
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

        /// <summary>
        /// This method appends the specified list of arguments (a single
        /// command) to this script builder.
        /// </summary>
        /// <param name="arguments">
        /// The list of arguments to append.  This parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
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

        /// <summary>
        /// This method appends the specified script to this script builder.
        /// </summary>
        /// <param name="script">
        /// The script to append.  This parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
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

        /// <summary>
        /// This method appends the script fragments of the specified script
        /// builder to this script builder.  A script builder cannot be added to
        /// itself.
        /// </summary>
        /// <param name="builder">
        /// The script builder whose fragments are appended.  This parameter
        /// should not be null and must not refer to this same instance.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
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

        /// <summary>
        /// This method combines the accumulated script fragments, in insertion
        /// order, into a single script string, normalizing line endings as
        /// required by the script engine.
        /// </summary>
        /// <param name="nested">
        /// Non-zero to render the combined script as a single nested,
        /// bracketed command (using command separators between fragments); zero
        /// to render it as top-level script text (using line separators).
        /// </param>
        /// <returns>
        /// The combined script string, or null when the fragment collection is
        /// not available.
        /// </returns>
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

        /// <summary>
        /// This method combines the accumulated script fragments into a single
        /// script and wraps it in a new <see cref="IScript" /> instance, using
        /// the identity information of this script builder.
        /// </summary>
        /// <param name="nested">
        /// Non-zero to render the combined script as a single nested,
        /// bracketed command; zero to render it as top-level script text.
        /// </param>
        /// <returns>
        /// A new <see cref="IScript" /> instance wrapping the combined script.
        /// </returns>
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
        /// <summary>
        /// This method produces a string describing this script builder, which
        /// is the combined, top-level script text of its accumulated fragments.
        /// </summary>
        /// <returns>
        /// The combined, top-level script string for this script builder.
        /// </returns>
        public override string ToString()
        {
            return GetString(false);
        }
        #endregion
    }
}
