/*
 * Variable.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that represent a Tcl
    /// variable within an interpreter, including its name, flags, value (or
    /// array of element values), link to another variable, and any associated
    /// traces and marks.  It extends <see cref="IIdentifier" /> with the
    /// state and behavior needed to participate in variable resolution,
    /// linking, tracing, and synchronization.
    /// </summary>
    [ObjectId("5336d290-4d0c-45a0-a956-f56f3384e0cb")]
    public interface IVariable : IIdentifier, IThreadLock, IHaveLevels
    {
        /// <summary>
        /// Gets or sets the call frame that this variable belongs to.  This
        /// value may be null.
        /// </summary>
        ICallFrame Frame { get; set; }
        /// <summary>
        /// Gets or sets the flags that control the behavior and describe the
        /// state of this variable.
        /// </summary>
        VariableFlags Flags { get; set; }
        /// <summary>
        /// Gets or sets the dictionary of tags associated with this variable.
        /// This value may be null.
        /// </summary>
        ObjectDictionary Tags { get; set; }
        /// <summary>
        /// Gets or sets the fully qualified name of this variable, including
        /// any namespace qualifiers.  This value may be null.
        /// </summary>
        string QualifiedName { get; set; }
        /// <summary>
        /// Gets or sets the variable that this variable is linked to (e.g. via
        /// the <c>upvar</c> or <c>global</c> commands).  This value may be
        /// null when this variable is not a link.
        /// </summary>
        IVariable Link { get; set; }
        /// <summary>
        /// Gets or sets the array element name within the linked variable that
        /// this variable is linked to, if any.  This value may be null.
        /// </summary>
        string LinkIndex { get; set; }
        /// <summary>
        /// Gets or sets the scalar value of this variable.  This value may be
        /// null.
        /// </summary>
        object Value { get; set; }
        /// <summary>
        /// Gets or sets the dictionary of array element values for this
        /// variable when it is an array.  This value may be null when this
        /// variable is not an array.
        /// </summary>
        ElementDictionary ArrayValue { get; set; }
        /// <summary>
        /// Gets or sets the list of traces associated with this variable.
        /// This value may be null.
        /// </summary>
        TraceList Traces { get; set; }

        /// <summary>
        /// Resets the call frame association of this variable to the specified
        /// call frame.
        /// </summary>
        /// <param name="frame">
        /// The new call frame to associate with this variable.  This parameter
        /// may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context this variable belongs to.  This parameter
        /// may be null.
        /// </param>
        void ResetFrame(
            ICallFrame frame, Interpreter interpreter);

        /// <summary>
        /// Marks this variable as undefined or defined.
        /// </summary>
        /// <param name="undefined">
        /// Non-zero to mark this variable as undefined; otherwise, to mark it
        /// as defined.
        /// </param>
        void MakeUndefined(bool undefined);
        /// <summary>
        /// Marks this variable as global or not global.
        /// </summary>
        /// <param name="global">
        /// Non-zero to mark this variable as global; otherwise, to clear the
        /// global designation.
        /// </param>
        void MakeGlobal(bool global);
        /// <summary>
        /// Marks this variable as local or not local.
        /// </summary>
        /// <param name="local">
        /// Non-zero to mark this variable as local; otherwise, to clear the
        /// local designation.
        /// </param>
        void MakeLocal(bool local);

        /// <summary>
        /// Resets this variable to its initial, empty state, optionally
        /// signaling the specified event when complete.
        /// </summary>
        /// <param name="event">
        /// The event to signal once this variable has been reset, if any.
        /// This parameter may be null.
        /// </param>
        void Reset(EventWaitHandle @event);

        /// <summary>
        /// Copies the value of the specified variable into this variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this variable belongs to.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="variable">
        /// The variable to copy the value from.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is copied.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CopyValueFrom(
            Interpreter interpreter, IVariable variable,
            CloneFlags flags, ref Result error);

        /// <summary>
        /// Creates a copy of this variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this variable belongs to.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how this variable is cloned.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created copy of this variable on success; otherwise,
        /// null with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        IVariable Clone(
            Interpreter interpreter, CloneFlags flags,
            ref Result error);

        /// <summary>
        /// Configures the value of this variable, optionally treating it as an
        /// array and/or merging it with the existing value.
        /// </summary>
        /// <param name="newValue">
        /// The new value to assign to this variable.  This parameter may be
        /// null.
        /// </param>
        /// <param name="union">
        /// Non-zero to merge the new value with the existing value rather than
        /// replacing it.
        /// </param>
        /// <param name="array">
        /// Non-zero to treat this variable as an array.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear any existing value before assigning the new
        /// value.
        /// </param>
        /// <param name="flag">
        /// Non-zero to update the flags of this variable to reflect the new
        /// value.
        /// </param>
        void SetupValue(
            object newValue, bool union, bool array, bool clear,
            bool flag);

        /// <summary>
        /// Determines whether this variable has the specified flags set.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to test for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags are set;
        /// otherwise, any one of them being set is sufficient.
        /// </param>
        /// <returns>
        /// True if the required flags are set; otherwise, false.
        /// </returns>
        bool HasFlags(VariableFlags hasFlags, bool all);
        /// <summary>
        /// Sets or clears the specified flags on this variable.
        /// </summary>
        /// <param name="flags">
        /// The flags to set or clear.
        /// </param>
        /// <param name="set">
        /// Non-zero to set the specified flags; otherwise, to clear them.
        /// </param>
        /// <returns>
        /// The resulting flags of this variable after the change.
        /// </returns>
        VariableFlags SetFlags(VariableFlags flags, bool set);

        /// <summary>
        /// Determines whether this variable has any traces associated with it.
        /// </summary>
        /// <returns>
        /// True if this variable has one or more traces; otherwise, false.
        /// </returns>
        bool HasTraces();
        /// <summary>
        /// Removes all traces associated with this variable.
        /// </summary>
        void ClearTraces();
        /// <summary>
        /// Adds the specified traces to this variable.
        /// </summary>
        /// <param name="traces">
        /// The list of traces to add.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The number of traces that were added.
        /// </returns>
        int AddTraces(TraceList traces);

        /// <summary>
        /// Initializes the collection of marks associated with this variable.
        /// </summary>
        /// <returns>
        /// True if the marks were initialized; otherwise, false.
        /// </returns>
        bool InitializeMarks();
        /// <summary>
        /// Removes all marks associated with this variable.
        /// </summary>
        /// <returns>
        /// True if the marks were cleared; otherwise, false.
        /// </returns>
        bool ClearMarks();
        /// <summary>
        /// Determines whether this variable has the named mark set.
        /// </summary>
        /// <param name="name">
        /// The name of the mark to test for.
        /// </param>
        /// <returns>
        /// True if the named mark is set; otherwise, false.
        /// </returns>
        bool HasMark(string name);
        /// <summary>
        /// Determines whether this variable has the named mark set, returning
        /// its associated namespace value.
        /// </summary>
        /// <param name="name">
        /// The name of the mark to test for.
        /// </param>
        /// <param name="namespace">
        /// Upon success, receives the namespace value associated with the
        /// mark.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the named mark is set; otherwise, false.
        /// </returns>
        bool HasMark(string name, ref INamespace @namespace);
        /// <summary>
        /// Determines whether this variable has the named mark set, returning
        /// its associated call frame value.
        /// </summary>
        /// <param name="name">
        /// The name of the mark to test for.
        /// </param>
        /// <param name="frame">
        /// Upon success, receives the call frame value associated with the
        /// mark.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the named mark is set; otherwise, false.
        /// </returns>
        bool HasMark(string name, ref ICallFrame frame);
        /// <summary>
        /// Determines whether this variable has the named mark set, returning
        /// its associated value.
        /// </summary>
        /// <param name="name">
        /// The name of the mark to test for.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the mark.  This
        /// value may be null.
        /// </param>
        /// <returns>
        /// True if the named mark is set; otherwise, false.
        /// </returns>
        bool HasMark(string name, ref object value);
        /// <summary>
        /// Sets or clears the named mark on this variable, optionally
        /// associating a value with it.
        /// </summary>
        /// <param name="mark">
        /// Non-zero to set the named mark; otherwise, to clear it.
        /// </param>
        /// <param name="name">
        /// The name of the mark to set or clear.
        /// </param>
        /// <param name="value">
        /// The value to associate with the mark when setting it.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the mark was set or cleared; otherwise, false.
        /// </returns>
        bool SetMark(bool mark, string name, object value);

        /// <summary>
        /// Gets the namespace associated with the namespace mark of this
        /// variable.
        /// </summary>
        /// <returns>
        /// The namespace associated with the namespace mark, or null if there
        /// is no namespace mark.
        /// </returns>
        INamespace GetNamespaceMark();
        /// <summary>
        /// Determines whether this variable has its namespace mark set to the
        /// specified namespace.
        /// </summary>
        /// <param name="namespace">
        /// The namespace to test for.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the namespace mark matches the specified namespace;
        /// otherwise, false.
        /// </returns>
        bool HasNamespaceMark(INamespace @namespace);
        /// <summary>
        /// Sets the namespace mark of this variable to the specified
        /// namespace.
        /// </summary>
        /// <param name="namespace">
        /// The namespace to associate with the namespace mark.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the namespace mark was set; otherwise, false.
        /// </returns>
        bool SetNamespaceMark(INamespace @namespace);
        /// <summary>
        /// Clears the namespace mark of this variable.
        /// </summary>
        /// <returns>
        /// True if the namespace mark was cleared; otherwise, false.
        /// </returns>
        bool UnsetNamespaceMark();

        /// <summary>
        /// Gets the call frame associated with the frame mark of this
        /// variable.
        /// </summary>
        /// <returns>
        /// The call frame associated with the frame mark, or null if there is
        /// no frame mark.
        /// </returns>
        ICallFrame GetFrameMark();
        /// <summary>
        /// Determines whether this variable has its frame mark set to the
        /// specified call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to test for.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the frame mark matches the specified call frame; otherwise,
        /// false.
        /// </returns>
        bool HasFrameMark(ICallFrame frame);
        /// <summary>
        /// Sets the frame mark of this variable to the specified call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to associate with the frame mark.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the frame mark was set; otherwise, false.
        /// </returns>
        bool SetFrameMark(ICallFrame frame);
        /// <summary>
        /// Clears the frame mark of this variable.
        /// </summary>
        /// <returns>
        /// True if the frame mark was cleared; otherwise, false.
        /// </returns>
        bool UnsetFrameMark();

        /// <summary>
        /// Fires the traces associated with this variable for the specified
        /// breakpoint type.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of operation (e.g. read, write, or unset) that is causing
        /// the traces to fire.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context this variable belongs to.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The information describing the trace operation being performed.
        /// This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// traces.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        //
        // TODO: Change this to use the IInterpreter type.
        //
        [Throw(true)]
        ReturnCode FireTraces(
            BreakpointType breakpointType, Interpreter interpreter,
            ITraceInfo traceInfo, ref Result result);
    }
}
