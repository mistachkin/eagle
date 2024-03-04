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
    [ObjectId("04409011-7a46-4f9a-a654-31cb14879305")]
    internal sealed class _Module : Default, IModule
    {
        #region Public Constructors
        public _Module(
            long token,
            IModule module
            )
            : base(token)
        {
            this.module = module;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IModule module;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (module != null) ? module.Name : null; }
            set { if (module != null) { module.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (module != null) ? module.Kind : IdentifierKind.None; }
            set { if (module != null) { module.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (module != null) ? module.Id : Guid.Empty; }
            set { if (module != null) { module.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (module != null) ? module.ClientData : null; }
            set { if (module != null) { module.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (module != null) ? module.Group : null; }
            set { if (module != null) { module.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (module != null) ? module.Description : null; }
            set { if (module != null) { module.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IModule Members
        public ModuleFlags Flags
        {
            get { return (module != null) ? module.Flags : ModuleFlags.None; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string FileName
        {
            get { return (module != null) ? module.FileName : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        public IntPtr Module
        {
            get { return (module != null) ? module.Module : IntPtr.Zero; }
        }

        ///////////////////////////////////////////////////////////////////////

        public int ReferenceCount
        {
            get { return (module != null) ? module.ReferenceCount : 0; }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Load(
            ref Result error
            )
        {
            if (module != null)
                return module.Load(ref error);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Load(
            ref int loaded,
            ref Result error
            )
        {
            if (module != null)
                return module.Load(ref loaded, ref error);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Unload(
            ref Result error
            )
        {
            if (module != null)
                return module.Unload(ref error);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Unload(
            ref int loaded,
            ref Result error
            )
        {
            if (module != null)
                return module.Unload(ref loaded, ref error);
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
            get { return module; }
        }
        #endregion
    }
}
