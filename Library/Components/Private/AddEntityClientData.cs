/*
 * AddEntityClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the per-operation client data used when adding an
    /// entity (e.g. a command, procedure, or other interpreter object),
    /// capturing the safety and standard-compliance flags that govern how the
    /// entity is created and whether non-conforming entities are hidden.
    /// </summary>
    [ObjectId("6d4116fa-75d8-4d6e-bcbd-79a92a127d01")]
    internal sealed class AddEntityClientData : ClientData
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an instance wrapping the specified opaque client data.
        /// This constructor provides shared initialization for the public
        /// constructors.
        /// </summary>
        /// <param name="data">
        /// The opaque client data to wrap.  This parameter may be null.
        /// </param>
        private AddEntityClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance wrapping the specified opaque client data and
        /// initializes its flags from the specified interpreter.
        /// </summary>
        /// <param name="data">
        /// The opaque client data to wrap.  This parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter used to initialize the creation and hiding flags.
        /// This parameter may be null.
        /// </param>
        public AddEntityClientData(
            object data,
            Interpreter interpreter
            )
            : this(data)
        {
            Initialize(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance wrapping the specified opaque client data and
        /// initializes its flags from the specified creation and interpreter
        /// flags.
        /// </summary>
        /// <param name="data">
        /// The opaque client data to wrap.  This parameter may be null.
        /// </param>
        /// <param name="createFlags">
        /// The creation flags used to initialize the creation and hiding flags.
        /// </param>
        /// <param name="interpreterFlags">
        /// The interpreter flags used to initialize the creation and hiding
        /// flags.
        /// </param>
        public AddEntityClientData(
            object data,
            CreateFlags createFlags,
            InterpreterFlags interpreterFlags
            )
            : this(data)
        {
            Initialize(createFlags, interpreterFlags);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method initializes the creation and hiding flags from the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to initialize the flags.  If this parameter is
        /// null, no action is taken.
        /// </param>
        private void Initialize(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            interpreter.SetupAddEntityClientData(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the creation and hiding flags from the
        /// specified creation and interpreter flags.
        /// </summary>
        /// <param name="createFlags">
        /// The creation flags used to initialize the flags.
        /// </param>
        /// <param name="interpreterFlags">
        /// The interpreter flags used to initialize the flags.
        /// </param>
        private void Initialize(
            CreateFlags createFlags,
            InterpreterFlags interpreterFlags
            )
        {
            Interpreter.SetupAddEntityClientData(
                this, createFlags, interpreterFlags);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// When true, the entity is being added to a "safe" interpreter.
        /// </summary>
        private bool createSafe;
        /// <summary>
        /// Gets or sets a value indicating whether the entity is being added to
        /// a "safe" interpreter.
        /// </summary>
        public bool CreateSafe
        {
            get { return createSafe; }
            set { createSafe = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When true, entities that are not safe should be hidden.
        /// </summary>
        private bool hideUnsafe;
        /// <summary>
        /// Gets or sets a value indicating whether entities that are not safe
        /// should be hidden.
        /// </summary>
        public bool HideUnsafe
        {
            get { return hideUnsafe; }
            set { hideUnsafe = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When true, the entity is being added to a "standard" interpreter.
        /// </summary>
        private bool createStandard;
        /// <summary>
        /// Gets or sets a value indicating whether the entity is being added to
        /// a "standard" interpreter.
        /// </summary>
        public bool CreateStandard
        {
            get { return createStandard; }
            set { createStandard = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When true, entities that are not standard should be hidden.
        /// </summary>
        private bool hideNonStandard;
        /// <summary>
        /// Gets or sets a value indicating whether entities that are not
        /// standard should be hidden.
        /// </summary>
        public bool HideNonStandard
        {
            get { return hideNonStandard; }
            set { hideNonStandard = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The string comparison type used when matching operator names.
        /// </summary>
        private StringComparison operatorComparisonType;
        /// <summary>
        /// Gets or sets the string comparison type used when matching operator
        /// names.
        /// </summary>
        public StringComparison OperatorComparisonType
        {
            get { return operatorComparisonType; }
            set { operatorComparisonType = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether the creation flags captured by this
        /// instance match the current safety and standard-compliance state of
        /// the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose state is compared against the captured flags.
        /// If this parameter is null, false is returned.
        /// </param>
        /// <returns>
        /// True if the captured creation flags match the interpreter state;
        /// otherwise, false.
        /// </returns>
        public bool HasMatchingCreateFlags(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if ((createSafe == interpreter.InternalIsSafe()) &&
                    (createStandard == interpreter.InternalIsStandard()))
                {
                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the creation flags captured by this
        /// instance match their corresponding hiding flags.
        /// </summary>
        /// <returns>
        /// True if the creation flags match their corresponding hiding flags;
        /// otherwise, false.
        /// </returns>
        public bool HasMatchingCreateAndHideFlags()
        {
            return (createSafe == hideUnsafe) &&
                (createStandard == hideNonStandard);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any kind of entity hiding is enabled.
        /// </summary>
        /// <returns>
        /// True if unsafe or non-standard entities are being hidden; otherwise,
        /// false.
        /// </returns>
        public bool IsHidingAnything()
        {
            return hideUnsafe || hideNonStandard;
        }
        #endregion
    }
}
