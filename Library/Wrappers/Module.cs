/*
 * Module.cs --
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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class implements a wrapper around an <see cref="IModule" />
    /// object, forwarding the module interface to the wrapped instance.  It is
    /// used so a module can participate in the interpreter as an identifiable,
    /// token-bearing entity.
    /// </summary>
    [ObjectId("04409011-7a46-4f9a-a654-31cb14879305")]
    internal sealed class _Module : Default, IModule
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this wrapper class.
        /// </summary>
        public _Module()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped <see cref="IModule" /> object, or null if none has been
        /// set.
        /// </summary>
        internal IModule module;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped module.
        /// </summary>
        public string Name
        {
            get { return (module != null) ? module.Name : null; }
            set { if (module != null) { module.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped module.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (module != null) ? module.Kind : IdentifierKind.None; }
            set { if (module != null) { module.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped module.
        /// </summary>
        public Guid Id
        {
            get { return (module != null) ? module.Id : Guid.Empty; }
            set { if (module != null) { module.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped module.
        /// </summary>
        public IClientData ClientData
        {
            get { return (module != null) ? module.ClientData : null; }
            set { if (module != null) { module.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped module.
        /// </summary>
        public string Group
        {
            get { return (module != null) ? module.Group : null; }
            set { if (module != null) { module.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped module.
        /// </summary>
        public string Description
        {
            get { return (module != null) ? module.Description : null; }
            set { if (module != null) { module.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IModule Members
        /// <summary>
        /// Gets the module flags of the wrapped module.
        /// </summary>
        public ModuleFlags Flags
        {
            get { return (module != null) ? module.Flags : ModuleFlags.None; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the file name of the wrapped module.
        /// </summary>
        public string FileName
        {
            get { return (module != null) ? module.FileName : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the native module handle of the wrapped module.
        /// </summary>
        public IntPtr Module
        {
            get { return (module != null) ? module.Module : IntPtr.Zero; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the reference count of the wrapped module.
        /// </summary>
        public int ReferenceCount
        {
            get { return (module != null) ? module.ReferenceCount : 0; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the load operation to the wrapped module.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Load(
            ref Result error
            )
        {
            if (module == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return module.Load(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the load operation to the wrapped module.
        /// </summary>
        /// <param name="loaded">
        /// Upon success, this is updated with the resulting load count.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Load(
            ref int loaded,
            ref Result error
            )
        {
            if (module == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return module.Load(ref loaded, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the unload operation to the wrapped module.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Unload(
            ref Result error
            )
        {
            if (module == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return module.Unload(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the unload operation to the wrapped module.
        /// </summary>
        /// <param name="loaded">
        /// Upon success, this is updated with the resulting load count.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Unload(
            ref int loaded,
            ref Result error
            )
        {
            if (module == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return module.Unload(ref loaded, ref error);
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
        /// Gets or sets the underlying <see cref="IModule" /> object wrapped by
        /// this instance.
        /// </summary>
        public override object Object
        {
            get { return module; }
            set { module = (IModule)value; } /* throw */
        }
        #endregion
    }
}
