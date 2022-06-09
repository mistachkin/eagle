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
    [ObjectId("6d4116fa-75d8-4d6e-bcbd-79a92a127d01")]
    internal sealed class AddEntityClientData : ClientData
    {
        #region Private Constructors
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
        public AddEntityClientData(
            object data,
            Interpreter interpreter
            )
            : this(data)
        {
            Initialize(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private void Initialize(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            interpreter.SetupAddEntityClientData(this);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private bool createSafe;
        public bool CreateSafe
        {
            get { return createSafe; }
            set { createSafe = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hideUnsafe;
        public bool HideUnsafe
        {
            get { return hideUnsafe; }
            set { hideUnsafe = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool createStandard;
        public bool CreateStandard
        {
            get { return createStandard; }
            set { createStandard = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hideNonStandard;
        public bool HideNonStandard
        {
            get { return hideNonStandard; }
            set { hideNonStandard = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringComparison operatorComparisonType;
        public StringComparison OperatorComparisonType
        {
            get { return operatorComparisonType; }
            set { operatorComparisonType = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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

        public bool HasMatchingCreateAndHideFlags()
        {
            return (createSafe == hideUnsafe) &&
                (createStandard == hideNonStandard);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsHidingAnything()
        {
            return hideUnsafe || hideNonStandard;
        }
        #endregion
    }
}
