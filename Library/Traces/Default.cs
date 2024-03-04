/*
 * Default.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Traces
{
    [ObjectId("f38c567b-6664-4c00-9e91-1ec81c206be4")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        ITrace
    {
        #region Public Constructors
        public Default(
            ITraceData traceData
            )
        {
            kind = IdentifierKind.Trace;

            if ((traceData == null) ||
                !FlagOps.HasFlags(traceData.TraceFlags,
                    TraceFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            //
            // NOTE: Is the supplied trace data valid?
            //
            if (traceData != null)
            {
                id = traceData.Id;

                EntityOps.MaybeSetupId(this);

                EntityOps.MaybeSetGroup(
                    this, traceData.Group);

                name = traceData.Name;
                description = traceData.Description;
                typeName = traceData.TypeName;
                methodName = traceData.MethodName;
                bindingFlags = traceData.BindingFlags;
                methodFlags = traceData.MethodFlags;
                token = traceData.Token;
                traceFlags = traceData.TraceFlags;
                plugin = traceData.Plugin;
                clientData = traceData.ClientData;
            }

            callback = null;
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return (name != null) ?
                StringList.MakeList(FormatOps.RawTypeName(GetType()), name) :
                base.ToString();
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        private IPlugin plugin;
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        private string typeName;
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private Type type;
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region ITraceData Members
        private string methodName;
        public virtual string MethodName
        {
            get { return methodName; }
            set { methodName = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private BindingFlags bindingFlags;
        public virtual BindingFlags BindingFlags
        {
            get { return bindingFlags; }
            set { bindingFlags = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private MethodFlags methodFlags;
        public virtual MethodFlags MethodFlags
        {
            get { return methodFlags; }
            set { methodFlags = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private TraceFlags traceFlags;
        public virtual TraceFlags TraceFlags
        {
            get { return traceFlags; }
            set { traceFlags = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        private long token;
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteTrace Members
        private TraceCallback callback;
        public virtual TraceCallback Callback
        {
            get { return callback; }
            set { callback = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IExecuteTrace Members
        public virtual ReturnCode Execute(
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region ISetup Members
        public virtual ReturnCode Setup(
            ref Result error
            )
        {
            return ReturnCode.Ok;
        }
        #endregion
    }
}
