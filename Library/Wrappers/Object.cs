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
    [ObjectId("95ed2ec8-3753-4cb1-b4c2-26e5b8d1671f")]
    internal sealed class _Object : Default, IObject
    {
        #region Public Constructors
        public _Object(
            long token,
            IObject @object
            )
            : base(token)
        {
            this.@object = @object;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IObject @object;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (@object != null) ? @object.Name : null; }
            set { if (@object != null) { @object.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (@object != null) ? @object.Kind : IdentifierKind.None; }
            set { if (@object != null) { @object.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (@object != null) ? @object.Id : Guid.Empty; }
            set { if (@object != null) { @object.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (@object != null) ? @object.ClientData : null; }
            set { if (@object != null) { @object.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (@object != null) ? @object.Group : null; }
            set { if (@object != null) { @object.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (@object != null) ? @object.Description : null; }
            set { if (@object != null) { @object.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IValueData Members
        public IClientData ValueData
        {
            get { return (@object != null) ? @object.ValueData : null; }
            set { if (@object != null) { @object.ValueData = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public IClientData ExtraData
        {
            get { return (@object != null) ? @object.ExtraData : null; }
            set { if (@object != null) { @object.ExtraData = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ICallFrame CallFrame
        {
            get { return (@object != null) ? @object.CallFrame : null; }
            set { if (@object != null) { @object.CallFrame = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue / ISetValue Members
        public object Value
        {
            get { return (@object != null) ? @object.Value : 0; }
            set { if (@object != null) { @object.Value = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string String
        {
            get { return (@object != null) ? @object.String : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        public int Length
        {
            get { return (@object != null) ? @object.Length : 0; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveObjectFlags Members
        public ObjectFlags ObjectFlags
        {
            get { return (@object != null) ? @object.ObjectFlags : ObjectFlags.None; }
            set { if (@object != null) { @object.ObjectFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectData Members
        public Type Type
        {
            get { return (@object != null) ? @object.Type : null; }
            set { if (@object != null) { @object.Type = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public IAlias Alias
        {
            get { return (@object != null) ? @object.Alias : null; }
            set { if (@object != null) { @object.Alias = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public int ReferenceCount
        {
            get { return (@object != null) ? @object.ReferenceCount : 0; }
            set { if (@object != null) { @object.ReferenceCount = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public int TemporaryReferenceCount
        {
            get { return (@object != null) ? @object.TemporaryReferenceCount : 0; }
            set { if (@object != null) { @object.TemporaryReferenceCount = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public string InterpName
        {
            get { return (@object != null) ? @object.InterpName : null; }
            set { if (@object != null) { @object.InterpName = value; } }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER && DEBUGGER_ARGUMENTS
        public ArgumentList ExecuteArguments
        {
            get { return (@object != null) ? @object.ExecuteArguments : null; }
            set { if (@object != null) { @object.ExecuteArguments = value; } }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObject Members
        public int AddReference()
        {
            return (@object != null) ? @object.AddReference() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public int RemoveReference()
        {
            return (@object != null) ? @object.RemoveReference() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public int AddTemporaryReference()
        {
            return (@object != null) ? @object.AddTemporaryReference() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public int RemoveTemporaryReference()
        {
            return (@object != null) ? @object.RemoveTemporaryReference() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool RemoveTemporaryReferences(
            Interpreter interpreter,
            string name,
            ref int finalCount
            )
        {
            return (@object != null) ?
                @object.RemoveTemporaryReferences(
                    interpreter, name, ref finalCount) :
                false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override object Object
        {
            get { return @object; }
        }
        #endregion
    }
}
