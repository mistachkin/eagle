/*
 * ChangeTypeData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class carries the input and output state for a type conversion
    /// (change-type) operation, recording the caller, the target type, the
    /// original value, the conversion options and culture, and the resulting
    /// value along with flags describing the outcome of the attempt.  It
    /// implements <see cref="IChangeTypeData" />.
    /// </summary>
    [ObjectId("b952ca92-8794-40d0-a89b-95e23ca533e0")]
    public class ChangeTypeData : IChangeTypeData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a change-type data instance describing a type conversion
        /// operation to be attempted.
        /// </summary>
        /// <param name="caller">
        /// The name of the method requesting the conversion.  This parameter
        /// may be null.
        /// </param>
        /// <param name="type">
        /// The target type to which the value should be converted.
        /// </param>
        /// <param name="oldValue">
        /// The original value to be converted.  This parameter may be null.
        /// </param>
        /// <param name="options">
        /// The options governing the conversion.  This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during the conversion.  This parameter may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the conversion.  This parameter
        /// may be null.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags controlling how the conversion is marshalled.
        /// </param>
        public ChangeTypeData(
            string caller,
            Type type,
            object oldValue,
            OptionDictionary options,
            CultureInfo cultureInfo,
            IClientData clientData,
            MarshalFlags marshalFlags
            )
        {
            this.caller = caller;
            this.type = type;
            this.oldValue = oldValue;
            this.options = options;
            this.cultureInfo = cultureInfo;
            this.clientData = clientData;
            this.marshalFlags = marshalFlags;

            ///////////////////////////////////////////////////////////////////

            Initialize();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method initializes the derived state of this instance from the
        /// marshal flags supplied to the constructor.
        /// </summary>
        private void Initialize()
        {
            noHandle = FlagOps.HasFlags(
                marshalFlags, MarshalFlags.NoHandle, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveCultureInfo Members
        /// <summary>
        /// The culture to use during the conversion.
        /// </summary>
        private CultureInfo cultureInfo;
        /// <summary>
        /// Gets or sets the culture to use during the conversion.  The set
        /// accessor is not supported and always throws.
        /// </summary>
        public virtual CultureInfo CultureInfo
        {
            get { return cultureInfo; }
            set { throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IChangeTypeData Members
        /// <summary>
        /// The name of the method requesting the conversion.
        /// </summary>
        private string caller;
        /// <summary>
        /// Gets the name of the method requesting the conversion.
        /// </summary>
        public virtual string Caller
        {
            get { return caller; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The target type to which the value should be converted.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets the target type to which the value should be converted.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The original value to be converted.
        /// </summary>
        private object oldValue;
        /// <summary>
        /// Gets the original value to be converted.
        /// </summary>
        public virtual object OldValue
        {
            get { return oldValue; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The options governing the conversion.
        /// </summary>
        private OptionDictionary options;
        /// <summary>
        /// Gets the options governing the conversion.
        /// </summary>
        public virtual OptionDictionary Options
        {
            get { return options; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The client data associated with the conversion.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets the client data associated with the conversion.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags controlling how the conversion is marshalled.
        /// </summary>
        private MarshalFlags marshalFlags;
        /// <summary>
        /// Gets or sets the flags controlling how the conversion is
        /// marshalled.
        /// </summary>
        public MarshalFlags MarshalFlags
        {
            get { return marshalFlags; }
            set { marshalFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The value resulting from the conversion.
        /// </summary>
        private object newValue;
        /// <summary>
        /// Gets or sets the value resulting from the conversion.
        /// </summary>
        public virtual object NewValue
        {
            get { return newValue; }
            set { newValue = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the conversion should avoid creating an opaque object
        /// handle.
        /// </summary>
        private bool noHandle;
        /// <summary>
        /// Gets or sets a value indicating whether the conversion should avoid
        /// creating an opaque object handle.
        /// </summary>
        public virtual bool NoHandle
        {
            get { return noHandle; }
            set { noHandle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the original value was an opaque object.
        /// </summary>
        private bool wasObject;
        /// <summary>
        /// Gets or sets a value indicating whether the original value was an
        /// opaque object.
        /// </summary>
        public virtual bool WasObject
        {
            get { return wasObject; }
            set { wasObject = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the conversion was attempted.
        /// </summary>
        private bool attempted;
        /// <summary>
        /// Gets or sets a value indicating whether the conversion was
        /// attempted.
        /// </summary>
        public virtual bool Attempted
        {
            get { return attempted; }
            set { attempted = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the conversion succeeded.
        /// </summary>
        private bool converted;
        /// <summary>
        /// Gets or sets a value indicating whether the conversion succeeded.
        /// </summary>
        public virtual bool Converted
        {
            get { return converted; }
            set { converted = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the converted value matches the target type.
        /// </summary>
        private bool doesMatch;
        /// <summary>
        /// Gets or sets a value indicating whether the converted value matches
        /// the target type.
        /// </summary>
        public virtual bool DoesMatch
        {
            get { return doesMatch; }
            set { doesMatch = value; }
        }
        #endregion
    }
}
