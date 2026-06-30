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
    /// <summary>
    /// This class implements a wrapper around an <see cref="IDelegate" />
    /// object, forwarding the delegate interface to the wrapped instance.  It
    /// is used so a delegate can participate in the interpreter as an
    /// identifiable, token-bearing entity.
    /// </summary>
    [ObjectId("2f3e94eb-861d-4f2f-af84-f877e0fe9406")]
    internal sealed class Delegate : Default, IDelegate
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this wrapper class.
        /// </summary>
        public Delegate()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped <see cref="IDelegate" /> object, or null if none has
        /// been set.
        /// </summary>
        internal IDelegate @delegate;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped delegate.
        /// </summary>
        public string Name
        {
            get { return (@delegate != null) ? @delegate.Name : null; }
            set { if (@delegate != null) { @delegate.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped delegate.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (@delegate != null) ? @delegate.Kind : IdentifierKind.None; }
            set { if (@delegate != null) { @delegate.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped delegate.
        /// </summary>
        public Guid Id
        {
            get { return (@delegate != null) ? @delegate.Id : Guid.Empty; }
            set { if (@delegate != null) { @delegate.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped delegate.
        /// </summary>
        public IClientData ClientData
        {
            get { return (@delegate != null) ? @delegate.ClientData : null; }
            set { if (@delegate != null) { @delegate.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped delegate.
        /// </summary>
        public string Group
        {
            get { return (@delegate != null) ? @delegate.Group : null; }
            set { if (@delegate != null) { @delegate.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped delegate.
        /// </summary>
        public string Description
        {
            get { return (@delegate != null) ? @delegate.Description : null; }
            set { if (@delegate != null) { @delegate.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDelegate Members
        /// <summary>
        /// Gets the calling convention of the wrapped delegate.
        /// </summary>
        public CallingConvention CallingConvention
        {
            get { return (@delegate != null) ? @delegate.CallingConvention : (CallingConvention)0; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the return type of the wrapped delegate.
        /// </summary>
        public Type ReturnType
        {
            get { return (@delegate != null) ? @delegate.ReturnType : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the list of parameter types of the wrapped delegate.
        /// </summary>
        public TypeList ParameterTypes
        {
            get { return (@delegate != null) ? @delegate.ParameterTypes : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the delegate type of the wrapped delegate.
        /// </summary>
        public Type Type
        {
            get { return (@delegate != null) ? @delegate.Type : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the module that contains the wrapped delegate.
        /// </summary>
        public IModule Module
        {
            get { return (@delegate != null) ? @delegate.Module : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the native function name associated with the wrapped delegate.
        /// </summary>
        public string FunctionName
        {
            get { return (@delegate != null) ? @delegate.FunctionName : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the native function address of the wrapped delegate.
        /// </summary>
        public IntPtr Address
        {
            get { return (@delegate != null) ? @delegate.Address : IntPtr.Zero; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the method information for the wrapped delegate.
        /// </summary>
        public MethodInfo MethodInfo
        {
            get { return (@delegate != null) ? @delegate.MethodInfo : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the wrapped delegate against the specified
        /// module and function name.
        /// </summary>
        /// <param name="module">
        /// The module that contains the function to resolve.  This parameter
        /// may be null.
        /// </param>
        /// <param name="functionName">
        /// The name of the function to resolve within the module.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Resolve(
            IModule module,
            string functionName,
            ref Result error
            )
        {
            if (@delegate == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return @delegate.Resolve(module, functionName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the wrapped delegate against the specified
        /// module and function name, also returning any exception that was
        /// caught.
        /// </summary>
        /// <param name="module">
        /// The module that contains the function to resolve.  This parameter
        /// may be null.
        /// </param>
        /// <param name="functionName">
        /// The name of the function to resolve within the module.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <param name="exception">
        /// Upon failure, this contains the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Resolve(
            IModule module,
            string functionName,
            ref Result error,
            ref Exception exception
            )
        {
            if (@delegate == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return @delegate.Resolve(
                module, functionName, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resolution of the wrapped delegate.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Unresolve(
            ref Result error
            )
        {
            if (@delegate == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return @delegate.Unresolve(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resolution of the wrapped delegate, also
        /// returning any exception that was caught.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <param name="exception">
        /// Upon failure, this contains the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Unresolve(
            ref Result error,
            ref Exception exception
            )
        {
            if (@delegate == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return @delegate.Unresolve(ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the wrapped delegate with the specified
        /// arguments.
        /// </summary>
        /// <param name="arguments">
        /// The arguments to pass to the wrapped delegate.  This parameter may
        /// be null.
        /// </param>
        /// <param name="returnValue">
        /// Upon success, this is set to the value returned by the wrapped
        /// delegate.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Invoke(
            object[] arguments,
            ref object returnValue,
            ref Result error
            )
        {
            if (@delegate == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return @delegate.Invoke(arguments, ref returnValue, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the wrapped delegate with the specified
        /// arguments, also returning any exception that was caught.
        /// </summary>
        /// <param name="arguments">
        /// The arguments to pass to the wrapped delegate.  This parameter may
        /// be null.
        /// </param>
        /// <param name="returnValue">
        /// Upon success, this is set to the value returned by the wrapped
        /// delegate.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <param name="exception">
        /// Upon failure, this contains the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Invoke(
            object[] arguments,
            ref object returnValue,
            ref Result error,
            ref Exception exception
            )
        {
            if (@delegate == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return @delegate.Invoke(
                arguments, ref returnValue, ref error, ref exception);
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
            get { return true; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the underlying <see cref="IDelegate" /> object wrapped
        /// by this instance.
        /// </summary>
        public override object Object
        {
            get { return @delegate; }
            set { @delegate = (IDelegate)value; } /* throw */
        }
        #endregion
    }
}
