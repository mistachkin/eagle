/*
 * TraceInfo.cs --
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
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class carries the contextual information associated with a single
    /// variable trace callback within an Eagle interpreter -- the trace being
    /// invoked, the breakpoint type, the call frame and variable involved, the
    /// variable name and array index, the variable flags, the old and new
    /// values (both scalar and array element forms), an associated list, and
    /// control fields governing cancellation, post-processing, and the
    /// resulting return code.  It implements <see cref="ITraceInfo" /> and
    /// <see cref="ICloneable" />.
    /// </summary>
    [ObjectId("208b3c14-c266-4611-a541-733a2b6f4d9b")]
    public sealed class TraceInfo :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        ITraceInfo, ICloneable
    {
        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of this class using the field values
        /// copied from an existing trace information object.
        /// </summary>
        /// <param name="traceInfo">
        /// The existing trace information object whose field values are used to
        /// initialize this instance.  This parameter may be null.
        /// </param>
        internal TraceInfo(
            ITraceInfo traceInfo
            )
        {
            Update(traceInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class using the fully specified
        /// set of trace, variable, value, and control field values.
        /// </summary>
        /// <param name="trace">
        /// The trace being invoked.  This parameter may be null.
        /// </param>
        /// <param name="breakpointType">
        /// The breakpoint type associated with this trace.
        /// </param>
        /// <param name="frame">
        /// The call frame associated with the variable being traced.  This
        /// parameter may be null.
        /// </param>
        /// <param name="variable">
        /// The variable being traced.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the variable being traced.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The array element index of the variable being traced, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The variable flags associated with this trace.
        /// </param>
        /// <param name="oldValue">
        /// The old scalar value of the variable being traced.  This parameter
        /// may be null.
        /// </param>
        /// <param name="newValue">
        /// The new scalar value of the variable being traced.  This parameter
        /// may be null.
        /// </param>
        /// <param name="oldValues">
        /// The old array element values of the variable being traced.  This
        /// parameter may be null.
        /// </param>
        /// <param name="newValues">
        /// The new array element values of the variable being traced.  This
        /// parameter may be null.
        /// </param>
        /// <param name="list">
        /// The list associated with this trace, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="cancel">
        /// Non-zero if the operation being traced should be canceled.
        /// </param>
        /// <param name="postProcess">
        /// Non-zero if the trace should be post-processed.
        /// </param>
        /// <param name="returnCode">
        /// The return code associated with this trace.
        /// </param>
        internal TraceInfo(
            ITrace trace,
            BreakpointType breakpointType,
            ICallFrame frame,
            IVariable variable,
            string name,
            string index,
            VariableFlags flags,
            object oldValue,
            object newValue,
            ElementDictionary oldValues,
            ElementDictionary newValues,
            StringList list,
            bool cancel,
            bool postProcess,
            ReturnCode returnCode
            )
        {
            Update(
                trace, breakpointType, frame, variable, name, index, flags,
                oldValue, newValue, oldValues, newValues, list, cancel,
                postProcess, returnCode);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this trace
        /// information object.
        /// </summary>
        /// <returns>
        /// The string representation of this trace information object.
        /// </returns>
        public override string ToString()
        {
            StringPairList result = ToStringPairList();
            
            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITraceInfo Members
        /// <summary>
        /// Stores the trace being invoked.
        /// </summary>
        private ITrace trace;
        /// <summary>
        /// Gets or sets the trace being invoked.
        /// </summary>
        public ITrace Trace
        {
            get { return trace; }
            set { trace = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the breakpoint type associated with this trace.
        /// </summary>
        private BreakpointType breakpointType;
        /// <summary>
        /// Gets or sets the breakpoint type associated with this trace.
        /// </summary>
        public BreakpointType BreakpointType
        {
            get { return breakpointType; }
            set { breakpointType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the call frame associated with the variable being traced.
        /// </summary>
        private ICallFrame frame;
        /// <summary>
        /// Gets or sets the call frame associated with the variable being
        /// traced.
        /// </summary>
        public ICallFrame Frame
        {
            get { return frame; }
            set { frame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the variable being traced.
        /// </summary>
        private IVariable variable;
        /// <summary>
        /// Gets or sets the variable being traced.
        /// </summary>
        public IVariable Variable
        {
            get { return variable; }
            set { variable = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the variable being traced.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of the variable being traced.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the array element index of the variable being traced.
        /// </summary>
        private string index;
        /// <summary>
        /// Gets or sets the array element index of the variable being traced.
        /// </summary>
        public string Index
        {
            get { return index; }
            set { index = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the variable flags associated with this trace.
        /// </summary>
        private VariableFlags flags;
        /// <summary>
        /// Gets or sets the variable flags associated with this trace.
        /// </summary>
        public VariableFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the old scalar value of the variable being traced.
        /// </summary>
        private object oldValue;
        /// <summary>
        /// Gets or sets the old scalar value of the variable being traced.
        /// </summary>
        public object OldValue
        {
            get { return oldValue; }
            set { oldValue = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the new scalar value of the variable being traced.
        /// </summary>
        private object newValue;
        /// <summary>
        /// Gets or sets the new scalar value of the variable being traced.
        /// </summary>
        public object NewValue
        {
            get { return newValue; }
            set { newValue = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the old array element values of the variable being traced.
        /// </summary>
        private ElementDictionary oldValues;
        /// <summary>
        /// Gets or sets the old array element values of the variable being
        /// traced.
        /// </summary>
        public ElementDictionary OldValues
        {
            get { return oldValues; }
            set { oldValues = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the new array element values of the variable being traced.
        /// </summary>
        private ElementDictionary newValues;
        /// <summary>
        /// Gets or sets the new array element values of the variable being
        /// traced.
        /// </summary>
        public ElementDictionary NewValues
        {
            get { return newValues; }
            set { newValues = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list associated with this trace.
        /// </summary>
        private StringList list;
        /// <summary>
        /// Gets or sets the list associated with this trace.
        /// </summary>
        public StringList List
        {
            get { return list; }
            set { list = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the operation being traced should
        /// be canceled.
        /// </summary>
        private bool cancel;
        /// <summary>
        /// Gets or sets a value indicating whether the operation being traced
        /// should be canceled.
        /// </summary>
        public bool Cancel
        {
            get { return cancel; }
            set { cancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the trace should be
        /// post-processed.
        /// </summary>
        private bool postProcess;
        /// <summary>
        /// Gets or sets a value indicating whether the trace should be
        /// post-processed.
        /// </summary>
        public bool PostProcess
        {
            get { return postProcess; }
            set { postProcess = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the return code associated with this trace.
        /// </summary>
        private ReturnCode returnCode;
        /// <summary>
        /// Gets or sets the return code associated with this trace.
        /// </summary>
        public ReturnCode ReturnCode
        {
            get { return returnCode; }
            set { returnCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new trace information object that is a copy of
        /// this instance.
        /// </summary>
        /// <returns>
        /// The newly created copy of this trace information object.
        /// </returns>
        public ITraceInfo Copy()
        {
            return new TraceInfo(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method updates the field values of this instance using the
        /// field values copied from an existing trace information object.
        /// </summary>
        /// <param name="traceInfo">
        /// The existing trace information object whose field values are used to
        /// update this instance.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The trace information object passed via
        /// <paramref name="traceInfo" />.
        /// </returns>
        public ITraceInfo Update(
            ITraceInfo traceInfo
            )
        {
            if (traceInfo != null)
            {
                Update(
                    traceInfo.Trace, traceInfo.BreakpointType,
                    traceInfo.Frame, traceInfo.Variable,
                    traceInfo.Name, traceInfo.Index,
                    traceInfo.Flags, traceInfo.OldValue,
                    traceInfo.NewValue, traceInfo.OldValues,
                    traceInfo.NewValues, traceInfo.List,
                    traceInfo.Cancel, traceInfo.PostProcess,
                    traceInfo.ReturnCode);
            }

            return traceInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method updates the field values of this instance using the
        /// fully specified set of trace, variable, value, and control field
        /// values.
        /// </summary>
        /// <param name="trace">
        /// The trace being invoked.  This parameter may be null.
        /// </param>
        /// <param name="breakpointType">
        /// The breakpoint type associated with this trace.
        /// </param>
        /// <param name="frame">
        /// The call frame associated with the variable being traced.  This
        /// parameter may be null.
        /// </param>
        /// <param name="variable">
        /// The variable being traced.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the variable being traced.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The array element index of the variable being traced, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The variable flags associated with this trace.
        /// </param>
        /// <param name="oldValue">
        /// The old scalar value of the variable being traced.  This parameter
        /// may be null.
        /// </param>
        /// <param name="newValue">
        /// The new scalar value of the variable being traced.  This parameter
        /// may be null.
        /// </param>
        /// <param name="oldValues">
        /// The old array element values of the variable being traced.  This
        /// parameter may be null.
        /// </param>
        /// <param name="newValues">
        /// The new array element values of the variable being traced.  This
        /// parameter may be null.
        /// </param>
        /// <param name="list">
        /// The list associated with this trace, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="cancel">
        /// Non-zero if the operation being traced should be canceled.
        /// </param>
        /// <param name="postProcess">
        /// Non-zero if the trace should be post-processed.
        /// </param>
        /// <param name="returnCode">
        /// The return code associated with this trace.
        /// </param>
        /// <returns>
        /// This trace information object.
        /// </returns>
        public ITraceInfo Update(
            ITrace trace,
            BreakpointType breakpointType,
            ICallFrame frame,
            IVariable variable,
            string name,
            string index,
            VariableFlags flags,
            object oldValue,
            object newValue,
            ElementDictionary oldValues,
            ElementDictionary newValues,
            StringList list,
            bool cancel,
            bool postProcess,
            ReturnCode returnCode
            )
        {
            this.trace = trace;
            this.breakpointType = breakpointType;
            this.frame = frame;
            this.variable = variable;
            this.name = name;
            this.index = index;
            this.flags = flags;
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.oldValues = oldValues;
            this.newValues = newValues;
            this.list = list;
            this.cancel = cancel;
            this.postProcess = postProcess;
            this.returnCode = returnCode;

            return this;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs representing the field
        /// values of this trace information object, suitable for display.
        /// </summary>
        /// <returns>
        /// The list of name/value pairs representing this trace information
        /// object.
        /// </returns>
        public StringPairList ToStringPairList()
        {
            StringPairList result = new StringPairList();

            if (variable != null)
            {
                result.Add(variable.Kind.ToString());
                result.Add((IPair<string>)null);

                if (variable.Name != null)
                    result.Add("name", variable.Name);
                else
                    result.Add("name", String.Empty);

                if (EntityOps.IsArray2(variable))
                {
                    ElementDictionary arrayValue = variable.ArrayValue;

                    if (arrayValue != null)
                    {
                        result.Add("<array>");

                        if (index != null)
                        {
                            object value;

                            if (arrayValue.TryGetValue(index, out value))
                            {
                                if (value != null)
                                {
                                    result.Add("value",
                                        StringOps.GetStringFromObject(
                                            value, null, !(value is TraceInfo)));
                                }
                                else
                                {
                                    result.Add("value", FormatOps.DisplayNull);
                                }
                            }
                            else
                            {
                                result.Add("value", "<noValue>");
                            }
                        }
                        else
                        {
                            result.Add("value", "<noIndex>");
                        }
                    }
                    else
                    {
                        result.Add("<noArray>");
                    }
                }
                else
                {
                    object value = variable.Value;

                    if (value != null)
                        result.Add("value",
                            StringOps.GetStringFromObject(value));
                    else
                        result.Add("value", "<noValue>");
                }

                result.Add("flags", variable.Flags.ToString());
                result.Add((IPair<string>)null);
            }

            result.Add("TraceInfo");
            result.Add((IPair<string>)null);

            if (trace != null)
                result.Add("trace", trace.ToString());
            else
                result.Add("trace", "<noTrace>");

            result.Add("breakpointType", breakpointType.ToString());

            if (frame != null)
                result.Add("frame", (frame.Name != null) ?
                    frame.Name : "<noFrameName>");
            else
                result.Add("frame", "<noFrame>");

            if (name != null)
                result.Add("name", name);
            else
                result.Add("name", "<noName>");

            if (index != null)
                result.Add("index", index);
            else
                result.Add("index", "<noIndex>");

            result.Add("flags", flags.ToString());

            if (oldValue != null)
                result.Add("oldValue",
                    StringOps.GetStringFromObject(oldValue));
            else
                result.Add("oldValue", "<noOldValue>");

            if (newValue != null)
                result.Add("newValue",
                    StringOps.GetStringFromObject(newValue));
            else
                result.Add("newValue", "<noNewValue>");

            if (oldValues != null)
                result.Add("oldValues", oldValues.ToString());
            else
                result.Add("oldValues", "<noOldValues>");

            if (newValues != null)
                result.Add("newValues", newValues.ToString());
            else
                result.Add("newValues", "<noNewValues>");

            if (list != null)
                result.Add("list", list.ToString());
            else
                result.Add("list", "<noList>");

            result.Add("cancel", cancel.ToString());
            result.Add("postProcess", postProcess.ToString());
            result.Add("returnCode", returnCode.ToString());

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new object that is a shallow copy of this
        /// trace information instance.
        /// </summary>
        /// <returns>
        /// The newly created shallow copy of this trace information instance.
        /// </returns>
        public object Clone()
        {
            return MemberwiseClone();
        }
        #endregion
    }
}
