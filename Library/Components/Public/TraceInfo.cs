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
    [ObjectId("208b3c14-c266-4611-a541-733a2b6f4d9b")]
    public sealed class TraceInfo :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        ITraceInfo, ICloneable
    {
        #region Private Constructors
        internal TraceInfo(
            ITraceInfo traceInfo
            )
        {
            Update(traceInfo);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override string ToString()
        {
            StringPairList result = ToStringPairList();
            
            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITraceInfo Members
        private ITrace trace;
        public ITrace Trace
        {
            get { return trace; }
            set { trace = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BreakpointType breakpointType;
        public BreakpointType BreakpointType
        {
            get { return breakpointType; }
            set { breakpointType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame frame;
        public ICallFrame Frame
        {
            get { return frame; }
            set { frame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IVariable variable;
        public IVariable Variable
        {
            get { return variable; }
            set { variable = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string index;
        public string Index
        {
            get { return index; }
            set { index = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private VariableFlags flags;
        public VariableFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private object oldValue;
        public object OldValue
        {
            get { return oldValue; }
            set { oldValue = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private object newValue;
        public object NewValue
        {
            get { return newValue; }
            set { newValue = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ElementDictionary oldValues;
        public ElementDictionary OldValues
        {
            get { return oldValues; }
            set { oldValues = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ElementDictionary newValues;
        public ElementDictionary NewValues
        {
            get { return newValues; }
            set { newValues = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringList list;
        public StringList List
        {
            get { return list; }
            set { list = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool cancel;
        public bool Cancel
        {
            get { return cancel; }
            set { cancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool postProcess;
        public bool PostProcess
        {
            get { return postProcess; }
            set { postProcess = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode returnCode;
        public ReturnCode ReturnCode
        {
            get { return returnCode; }
            set { returnCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public ITraceInfo Copy()
        {
            return new TraceInfo(this);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public object Clone()
        {
            return MemberwiseClone();
        }
        #endregion
    }
}
