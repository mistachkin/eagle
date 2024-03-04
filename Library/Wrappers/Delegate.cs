/*
 * Delegate.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    [ObjectId("2f3e94eb-861d-4f2f-af84-f877e0fe9406")]
    internal sealed class Delegate : Default, IDelegate
    {
        #region Public Constructors
        public Delegate(
            long token,
            IDelegate @delegate
            )
            : base(token)
        {
            this.@delegate = @delegate;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IDelegate @delegate;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (@delegate != null) ? @delegate.Name : null; }
            set { if (@delegate != null) { @delegate.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (@delegate != null) ? @delegate.Kind : IdentifierKind.None; }
            set { if (@delegate != null) { @delegate.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (@delegate != null) ? @delegate.Id : Guid.Empty; }
            set { if (@delegate != null) { @delegate.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (@delegate != null) ? @delegate.ClientData : null; }
            set { if (@delegate != null) { @delegate.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (@delegate != null) ? @delegate.Group : null; }
            set { if (@delegate != null) { @delegate.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (@delegate != null) ? @delegate.Description : null; }
            set { if (@delegate != null) { @delegate.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDelegate Members
        public CallingConvention CallingConvention
        {
            get { return (@delegate != null) ? @delegate.CallingConvention : (CallingConvention)0; }
        }

        ///////////////////////////////////////////////////////////////////////

        public Type ReturnType
        {
            get { return (@delegate != null) ? @delegate.ReturnType : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        public TypeList ParameterTypes
        {
            get { return (@delegate != null) ? @delegate.ParameterTypes : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        public Type Type
        {
            get { return (@delegate != null) ? @delegate.Type : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        public IModule Module
        {
            get { return (@delegate != null) ? @delegate.Module : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string FunctionName
        {
            get { return (@delegate != null) ? @delegate.FunctionName : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        public IntPtr Address
        {
            get { return (@delegate != null) ? @delegate.Address : IntPtr.Zero; }
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodInfo MethodInfo
        {
            get { return (@delegate != null) ? @delegate.MethodInfo : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Resolve(
            IModule module,
            string functionName,
            ref Result error
            )
        {
            if (@delegate != null)
                return @delegate.Resolve(module, functionName, ref error);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Resolve(
            IModule module,
            string functionName,
            ref Result error,
            ref Exception exception
            )
        {
            if (@delegate != null)
                return @delegate.Resolve(
                    module, functionName, ref error, ref exception);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Unresolve(
            ref Result error
            )
        {
            if (@delegate != null)
                return @delegate.Unresolve(ref error);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Unresolve(
            ref Result error,
            ref Exception exception
            )
        {
            if (@delegate != null)
                return @delegate.Unresolve(ref error, ref exception);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Invoke(
            object[] arguments,
            ref object returnValue,
            ref Result error
            )
        {
            if (@delegate != null)
                return @delegate.Invoke(arguments, ref returnValue, ref error);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Invoke(
            object[] arguments,
            ref object returnValue,
            ref Result error,
            ref Exception exception
            )
        {
            if (@delegate != null)
                return @delegate.Invoke(
                    arguments, ref returnValue, ref error, ref exception);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        public override bool IsDisposable
        {
            get { return true; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override object Object
        {
            get { return @delegate; }
        }
        #endregion
    }
}
