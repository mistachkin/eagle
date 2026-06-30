/*
 * Object.cs --
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

#if DEBUGGER && DEBUGGER_ARGUMENTS
using Eagle._Containers.Public;
#endif

using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class implements a wrapper around an <see cref="IObject" />
    /// object, forwarding the object interface to the wrapped instance.  It is
    /// used so a managed object can participate in the interpreter as an
    /// identifiable, token-bearing entity.
    /// </summary>
    [ObjectId("95ed2ec8-3753-4cb1-b4c2-26e5b8d1671f")]
    internal sealed class _Object : Default, IObject
    {
        #region Private Data
        /// <summary>
        /// Non-zero if this instance has been disposed; used only when there is
        /// no wrapped object.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// Non-zero if this instance is being disposed; used only when there is
        /// no wrapped object.
        /// </summary>
        private bool disposing;
        /// <summary>
        /// The wrapped <see cref="IObject" /> object, or null if none has been
        /// set.
        /// </summary>
        internal IObject @object;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this wrapper class.
        /// </summary>
        public _Object() : base()
        {
            this.disposed = false;
            this.disposing = false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped object.
        /// </summary>
        public string Name
        {
            get { return (@object != null) ? @object.Name : null; }
            set { if (@object != null) { @object.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped object.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (@object != null) ? @object.Kind : IdentifierKind.None; }
            set { if (@object != null) { @object.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped object.
        /// </summary>
        public Guid Id
        {
            get { return (@object != null) ? @object.Id : Guid.Empty; }
            set { if (@object != null) { @object.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped object.
        /// </summary>
        public IClientData ClientData
        {
            get { return (@object != null) ? @object.ClientData : null; }
            set { if (@object != null) { @object.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped object.
        /// </summary>
        public string Group
        {
            get { return (@object != null) ? @object.Group : null; }
            set { if (@object != null) { @object.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped object.
        /// </summary>
        public string Description
        {
            get { return (@object != null) ? @object.Description : null; }
            set { if (@object != null) { @object.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IValueData Members
        /// <summary>
        /// Gets or sets the value data of the wrapped object.
        /// </summary>
        public IClientData ValueData
        {
            get { return (@object != null) ? @object.ValueData : null; }
            set { if (@object != null) { @object.ValueData = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the extra data of the wrapped object.
        /// </summary>
        public IClientData ExtraData
        {
            get { return (@object != null) ? @object.ExtraData : null; }
            set { if (@object != null) { @object.ExtraData = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the call frame of the wrapped object.
        /// </summary>
        public ICallFrame CallFrame
        {
            get { return (@object != null) ? @object.CallFrame : null; }
            set { if (@object != null) { @object.CallFrame = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue / ISetValue Members
        /// <summary>
        /// Gets or sets the value of the wrapped object.
        /// </summary>
        public object Value
        {
            get { return (@object != null) ? @object.Value : 0; }
            set { if (@object != null) { @object.Value = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the string representation of the wrapped object.
        /// </summary>
        public string String
        {
            get { return (@object != null) ? @object.String : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the string length of the wrapped object.
        /// </summary>
        public int Length
        {
            get { return (@object != null) ? @object.Length : 0; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveObjectFlags Members
        /// <summary>
        /// Gets or sets the object flags of the wrapped object.
        /// </summary>
        public ObjectFlags ObjectFlags
        {
            get { return (@object != null) ? @object.ObjectFlags : ObjectFlags.None; }
            set { if (@object != null) { @object.ObjectFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectData Members
        /// <summary>
        /// Gets or sets the type of the wrapped object.
        /// </summary>
        public Type Type
        {
            get { return (@object != null) ? @object.Type : null; }
            set { if (@object != null) { @object.Type = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the alias of the wrapped object.
        /// </summary>
        public IAlias Alias
        {
            get { return (@object != null) ? @object.Alias : null; }
            set { if (@object != null) { @object.Alias = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the reference count of the wrapped object.
        /// </summary>
        public int ReferenceCount
        {
            get { return (@object != null) ? @object.ReferenceCount : 0; }
            set { if (@object != null) { @object.ReferenceCount = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the temporary reference count of the wrapped object.
        /// </summary>
        public int TemporaryReferenceCount
        {
            get { return (@object != null) ? @object.TemporaryReferenceCount : 0; }
            set { if (@object != null) { @object.TemporaryReferenceCount = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        /// <summary>
        /// Gets or sets the associated Tcl interpreter name of the wrapped
        /// object.
        /// </summary>
        public string InterpName
        {
            get { return (@object != null) ? @object.InterpName : null; }
            set { if (@object != null) { @object.InterpName = value; } }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER && DEBUGGER_ARGUMENTS
        /// <summary>
        /// Gets or sets the execute arguments of the wrapped object.
        /// </summary>
        public ArgumentList ExecuteArguments
        {
            get { return (@object != null) ? @object.ExecuteArguments : null; }
            set { if (@object != null) { @object.ExecuteArguments = value; } }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObject Members
        /// <summary>
        /// This method adds a reference to the wrapped object.
        /// </summary>
        /// <returns>
        /// The resulting reference count, or zero when there is no wrapped
        /// object.
        /// </returns>
        public int AddReference()
        {
            return (@object != null) ? @object.AddReference() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a reference from the wrapped object.
        /// </summary>
        /// <returns>
        /// The resulting reference count, or zero when there is no wrapped
        /// object.
        /// </returns>
        public int RemoveReference()
        {
            return (@object != null) ? @object.RemoveReference() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds a temporary reference to the wrapped object.
        /// </summary>
        /// <returns>
        /// The resulting reference count, or zero when there is no wrapped
        /// object.
        /// </returns>
        public int AddTemporaryReference()
        {
            return (@object != null) ? @object.AddTemporaryReference() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a temporary reference from the wrapped object.
        /// </summary>
        /// <returns>
        /// The resulting reference count, or zero when there is no wrapped
        /// object.
        /// </returns>
        public int RemoveTemporaryReference()
        {
            return (@object != null) ? @object.RemoveTemporaryReference() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the temporary references from the wrapped object.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this object is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the object whose temporary references are being removed.
        /// </param>
        /// <param name="finalCount">
        /// Upon success, this is set to the resulting reference count.
        /// </param>
        /// <returns>
        /// True if the temporary references were removed; otherwise, false.
        /// </returns>
        public bool RemoveTemporaryReferences(
            Interpreter interpreter,
            string name,
            ref int finalCount
            )
        {
            if (@object == null)
                return false;

            return @object.RemoveTemporaryReferences(
                interpreter, name, ref finalCount);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets or sets the disposed state of the wrapped object.
        /// </summary>
        public bool Disposed
        {
            get { return (@object != null) ? @object.Disposed : disposed; }
            set
            {
                if (@object != null)
                    @object.Disposed = value;
                else
                    disposed = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the disposing state of the wrapped object.
        /// </summary>
        public bool Disposing
        {
            get { return (@object != null) ? @object.Disposing : disposing; }
            set
            {
                if (@object != null)
                    @object.Disposing = value;
                else
                    disposing = value;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the object wrapped by this instance
        /// represents a resource that requires disposal.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the underlying <see cref="IObject" /> object wrapped by
        /// this instance.
        /// </summary>
        public override object Object
        {
            get { return @object; }
            set { @object = (IObject)value; } /* throw */
        }
        #endregion
    }
}
