/*
 * Trace.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

using FieldInfoDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Public.IAnyPair<
        System.Reflection.FieldInfo, object>>;

namespace Eagle._Plugins
{
    /// <summary>
    /// This class is the abstract base class for plugins that expose a set of
    /// named instance fields which may be queried or modified at run-time via
    /// the request-handling mechanism.  Derived classes supply the names and
    /// default values of those fields, and this class manages their reflection
    /// metadata in a thread-safe manner.
    /// </summary>
    [ObjectId("d8b3cd8d-fa09-41a4-b042-eff42dd9a193")]
    public abstract class Trace : Notify
    {
        #region Private Data
        //
        // NOTE: This is used to synchronize access to the "fields" field.
        //
        /// <summary>
        /// The object used to synchronize access to the field metadata
        /// collection.
        /// </summary>
        private readonly object syncRoot = new object();

        //
        // NOTE: This is the list of fields that may be read or written via
        //       the IExecuteRequest.Execute method.
        //
        /// <summary>
        /// The collection of fields that may be read or written via the
        /// request-handling mechanism, keyed by field name and associating
        /// each name with its reflection metadata and default value.
        /// </summary>
        private FieldInfoDictionary fields;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this plugin.  This merges the plugin
        /// flags declared via attributes, initializes the set of
        /// request-handling fields, and applies their default values.
        /// </summary>
        /// <param name="pluginData">
        /// The data used to create and identify this plugin, such as its name
        /// and flags.  This parameter may be null.
        /// </param>
        public Trace(
            IPluginData pluginData
            )
            : base(pluginData)
        {
            this.Flags |= AttributeOps.GetPluginFlags(GetType().BaseType) |
                AttributeOps.GetPluginFlags(this);

            ///////////////////////////////////////////////////////////////////

            /* NO RESULT */
            InitializeRequestFields(false);

            /* IGNORED */
            UseDefaultRequestFieldValues();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// This method populates the collection of request-handling fields
        /// from the names and default values supplied by the derived class,
        /// looking up the reflection metadata for each named field.
        /// </summary>
        /// <param name="merge">
        /// When true, existing entries are overwritten with freshly resolved
        /// metadata and default values; otherwise, entries already present are
        /// left unchanged.
        /// </param>
        protected virtual void InitializeRequestFields(
            bool merge
            )
        {
            string[] names = GetRequestFieldNames();

            if (names == null)
                return;

            int length = names.Length;

            if (length == 0)
                return;

            object[] values = GetRequestFieldValues();

            for (int index = 0; index < length; index++)
            {
                string name = names[index];

                if (name == null)
                    continue;

                FieldInfo fieldInfo;

                try
                {
                    fieldInfo = GetType().GetField(
                        name, ObjectOps.GetBindingFlags(
                            MetaBindingFlags.Trace, true));

                    if (fieldInfo == null)
                        continue;
                }
                catch
                {
                    continue;
                }

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (fields == null)
                        fields = new FieldInfoDictionary();

                    if (merge || !fields.ContainsKey(name))
                    {
                        fields[name] = new AnyPair<FieldInfo, object>(
                            fieldInfo, ArrayOps.GetValue(values, index,
                            null));
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a snapshot copy of the collection of
        /// request-handling fields.
        /// </summary>
        /// <returns>
        /// A new dictionary containing the current field metadata, or null if
        /// no fields have been initialized.
        /// </returns>
        protected virtual FieldInfoDictionary GetRequestFields()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (fields == null)
                    return null;

                return new FieldInfoDictionary(fields);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is implemented by the derived class to return the names
        /// of the instance fields that may be read or written via the
        /// request-handling mechanism.
        /// </summary>
        /// <returns>
        /// The array of field names, or null if there are none.
        /// </returns>
        protected abstract string[] GetRequestFieldNames();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is implemented by the derived class to return the
        /// default values for the request-handling fields, in the same order
        /// as the names returned by <see cref="GetRequestFieldNames" />.
        /// </summary>
        /// <returns>
        /// The array of default field values, or null if there are none.
        /// </returns>
        protected abstract object[] GetRequestFieldValues();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is implemented by the derived class to reset all of the
        /// request-handling fields to their default values.
        /// </summary>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value.
        /// </returns>
        protected abstract ReturnCode UseDefaultRequestFieldValues();
        #endregion
    }
}
