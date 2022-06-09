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
    [ObjectId("d8b3cd8d-fa09-41a4-b042-eff42dd9a193")]
    public abstract class Trace : Notify
    {
        #region Private Data
        //
        // NOTE: This is used to synchronize access to the "fields" field.
        //
        private readonly object syncRoot = new object();

        //
        // NOTE: This is the list of fields that may be read or written via
        //       the IExecuteRequest.Execute method.
        //
        private FieldInfoDictionary fields;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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

        protected abstract string[] GetRequestFieldNames();

        ///////////////////////////////////////////////////////////////////////

        protected abstract object[] GetRequestFieldValues();

        ///////////////////////////////////////////////////////////////////////

        protected abstract ReturnCode UseDefaultRequestFieldValues();
        #endregion
    }
}
