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
    /// <summary>
    /// This class provides the default implementation of the
    /// <see cref="ITrace" /> interface, which represents a variable trace
    /// within the Eagle engine.  A trace is invoked when a variable it is
    /// attached to is read, written, or unset.  This class supplies the common
    /// identity, plugin, type-and-name, and callback storage shared by all
    /// traces; its <see cref="Execute" /> and <see cref="Setup" /> methods are
    /// no-op placeholders that derived classes override.
    /// </summary>
    [ObjectId("f38c567b-6664-4c00-9e91-1ec81c206be4")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        ITrace
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the default trace, optionally
        /// initializing it from the supplied trace data.
        /// </summary>
        /// <param name="traceData">
        /// The data used to create and identify this trace, such as its name
        /// and the plugin type and method that supply its callback.  This
        /// parameter may be null, in which case the trace is left with default
        /// property values.
        /// </param>
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
        /// <summary>
        /// Returns a string representation of this trace.
        /// </summary>
        /// <returns>
        /// A list-formatted string containing the type name and the name of
        /// this trace; if this trace has no name, the default string
        /// representation from the base class is returned instead.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ?
                StringList.MakeList(FormatOps.RawTypeName(GetType()), name) :
                base.ToString();
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of this trace.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this trace.
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The kind of identifier represented by this trace.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this trace.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier for this trace.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier for this trace.
        /// </summary>
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The extra, caller-specific data associated with this trace, if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the extra, caller-specific data associated with this
        /// trace.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The name of the group this trace belongs to, if any.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the name of the group this trace belongs to.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of this trace, if any.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this trace.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// The plugin that provides this trace, if any.
        /// </summary>
        private IPlugin plugin;
        /// <summary>
        /// Gets or sets the plugin that provides this trace.
        /// </summary>
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// The name of the type that contains the method supplying this
        /// trace's callback.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the name of the type that contains the method
        /// supplying this trace's callback.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The type that contains the method supplying this trace's callback.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the type that contains the method supplying this
        /// trace's callback.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region ITraceData Members
        /// <summary>
        /// The name of the method that supplies this trace's callback.
        /// </summary>
        private string methodName;
        /// <summary>
        /// Gets or sets the name of the method that supplies this trace's
        /// callback.
        /// </summary>
        public virtual string MethodName
        {
            get { return methodName; }
            set { methodName = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The reflection binding flags used to locate the method that
        /// supplies this trace's callback.
        /// </summary>
        private BindingFlags bindingFlags;
        /// <summary>
        /// Gets or sets the reflection binding flags used to locate the method
        /// that supplies this trace's callback.
        /// </summary>
        public virtual BindingFlags BindingFlags
        {
            get { return bindingFlags; }
            set { bindingFlags = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that describe the method that supplies this trace's
        /// callback.
        /// </summary>
        private MethodFlags methodFlags;
        /// <summary>
        /// Gets or sets the flags that describe the method that supplies this
        /// trace's callback.
        /// </summary>
        public virtual MethodFlags MethodFlags
        {
            get { return methodFlags; }
            set { methodFlags = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that control the behavior of this trace.
        /// </summary>
        private TraceFlags traceFlags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of this trace.
        /// </summary>
        public virtual TraceFlags TraceFlags
        {
            get { return traceFlags; }
            set { traceFlags = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The interpreter token that uniquely identifies this trace within
        /// its containing collection.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the interpreter token that uniquely identifies this
        /// trace within its containing collection.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteTrace Members
        /// <summary>
        /// The delegate, if any, invoked to execute this trace.
        /// </summary>
        private TraceCallback callback;
        /// <summary>
        /// Gets or sets the delegate invoked to execute this trace.
        /// </summary>
        public virtual TraceCallback Callback
        {
            get { return callback; }
            set { callback = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IExecuteTrace Members
        /// <summary>
        /// Executes this trace.  This default implementation performs no work
        /// and always succeeds; derived classes override it to perform the
        /// actual trace handling.
        /// </summary>
        /// <param name="breakpointType">
        /// The kind of variable operation (e.g. read, write, or unset) that
        /// triggered this trace.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context this trace is executing in.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The details of the variable operation that triggered this trace.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// trace.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
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
        /// <summary>
        /// Prepares this trace for use.  This default implementation performs
        /// no work and always succeeds; derived classes override it to resolve
        /// the trace callback and perform any other required initialization.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode Setup(
            ref Result error
            )
        {
            return ReturnCode.Ok;
        }
        #endregion
    }
}
