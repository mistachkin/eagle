/*
 * CallFrame.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents a single call frame (i.e. an activation
    /// record) on the Eagle interpreter call stack.  It composes a unique
    /// identity (<see cref="IIdentifier" />), optional disposal tracking
    /// (<see cref="IMaybeDisposed" />), and thread locking
    /// (<see cref="IThreadLock" />), and exposes the frame identity, flags,
    /// the entity being executed, its arguments, the variables owned by the
    /// frame, the links to neighboring frames, and the methods used to
    /// inspect, mark, save, and restore the frame.
    /// </summary>
    [ObjectId("5c1396df-5c74-4ffa-bafa-13f2b795fab1")]
    public interface ICallFrame : IIdentifier, IMaybeDisposed, IThreadLock
    {
        /// <summary>
        /// Gets or sets the unique identifier of this call frame.
        /// </summary>
        long FrameId { get; set; }
        /// <summary>
        /// Gets or sets the absolute stack level of this call frame.
        /// </summary>
        long FrameLevel { get; set; }
        /// <summary>
        /// Gets or sets the flags that describe the kind and behavior of this
        /// call frame.
        /// </summary>
        CallFrameFlags Flags { get; set; }
        /// <summary>
        /// Gets or sets the dictionary of arbitrary tags associated with this
        /// call frame.
        /// </summary>
        ObjectDictionary Tags { get; set; }
        /// <summary>
        /// Gets or sets the index of this call frame.
        /// </summary>
        long Index { get; set; }
        /// <summary>
        /// Gets or sets the logical level of this call frame.
        /// </summary>
        long Level { get; set; }
        /// <summary>
        /// Gets or sets the entity being executed within this call frame.
        /// </summary>
        IExecute Execute { get; set; }
        /// <summary>
        /// Gets or sets the list of arguments associated with this call
        /// frame.
        /// </summary>
        ArgumentList Arguments { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this call frame owns its
        /// arguments.
        /// </summary>
        bool OwnArguments { get; set; }
        /// <summary>
        /// Gets or sets the list of procedure arguments associated with this
        /// call frame.
        /// </summary>
        ArgumentList ProcedureArguments { get; set; }
        /// <summary>
        /// Gets or sets the dictionary of variables owned by this call frame.
        /// </summary>
        VariableDictionary Variables { get; set; }
        /// <summary>
        /// Gets or sets the other call frame associated with this call frame,
        /// if any (e.g. a linked or aliased frame).
        /// </summary>
        ICallFrame Other { get; set; }
        /// <summary>
        /// Gets or sets the previous call frame in the call stack.
        /// </summary>
        ICallFrame Previous { get; set; }
        /// <summary>
        /// Gets or sets the next call frame in the call stack.
        /// </summary>
        ICallFrame Next { get; set; }

        //
        // NOTE: *RESERVED* For future use by the core library only.
        //
        /// <summary>
        /// Gets or sets data reserved for future use by the core library
        /// only.
        /// </summary>
        IClientData EngineData { get; set; }

        //
        // NOTE: *RESERVED* For future use by the core library only.
        //
        /// <summary>
        /// Gets or sets data reserved for future use by the core library
        /// only.
        /// </summary>
        IClientData AuxiliaryData { get; set; }

        //
        // NOTE: *RESERVED* For use by custom resolvers only.
        //
        /// <summary>
        /// Gets or sets data reserved for use by custom resolvers only.
        /// </summary>
        IClientData ResolveData { get; set; }

        //
        // NOTE: *RESERVED* For use by third-party applications and
        //       plugins.  The core library will never use this.
        //
        /// <summary>
        /// Gets or sets data reserved for use by third-party applications and
        /// plugins.  The core library will never use this.
        /// </summary>
        IClientData ExtraData { get; set; }

        //
        // NOTE: Non-zero if the call frame actually owns variables.
        //
        /// <summary>
        /// Gets a value indicating whether this call frame actually owns
        /// variables.
        /// </summary>
        bool IsVariable { get; }

        /// <summary>
        /// Produces a list of name and value pairs that describe this call
        /// frame.
        /// </summary>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included.
        /// </param>
        /// <returns>
        /// A list of name and value pairs describing this call frame.
        /// </returns>
        StringPairList ToList(DetailFlags detailFlags);
        /// <summary>
        /// Produces a string that describes this call frame.
        /// </summary>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included.
        /// </param>
        /// <returns>
        /// A string describing this call frame.
        /// </returns>
        string ToString(DetailFlags detailFlags);

        /// <summary>
        /// Determines whether this call frame has the specified flags set.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags are set; zero
        /// to require that any of them are set.
        /// </param>
        /// <returns>
        /// True if the requested flags are set; otherwise, false.
        /// </returns>
        bool HasFlags(CallFrameFlags hasFlags, bool all);
        /// <summary>
        /// Sets or clears the specified flags on this call frame.
        /// </summary>
        /// <param name="flags">
        /// The flags to set or clear.
        /// </param>
        /// <param name="set">
        /// Non-zero to set the specified flags; zero to clear them.
        /// </param>
        /// <returns>
        /// The resulting flags for this call frame.
        /// </returns>
        CallFrameFlags SetFlags(CallFrameFlags flags, bool set);

        /// <summary>
        /// Initializes the collection of marks for this call frame.
        /// </summary>
        /// <returns>
        /// True if the marks were initialized; otherwise, false.
        /// </returns>
        bool InitializeMarks();
        /// <summary>
        /// Removes all marks from this call frame.
        /// </summary>
        /// <returns>
        /// True if the marks were cleared; otherwise, false.
        /// </returns>
        bool ClearMarks();
        /// <summary>
        /// Determines whether this call frame has the named mark.
        /// </summary>
        /// <param name="name">
        /// The name of the mark to check for.
        /// </param>
        /// <returns>
        /// True if the named mark is present; otherwise, false.
        /// </returns>
        bool HasMark(string name);
        /// <summary>
        /// Determines whether this call frame has the named mark, also
        /// returning the associated call frame.
        /// </summary>
        /// <param name="name">
        /// The name of the mark to check for.
        /// </param>
        /// <param name="frame">
        /// Upon success, receives the call frame associated with the mark.
        /// </param>
        /// <returns>
        /// True if the named mark is present; otherwise, false.
        /// </returns>
        bool HasMark(string name, ref ICallFrame frame);
        /// <summary>
        /// Determines whether this call frame has the named mark, also
        /// returning the associated value.
        /// </summary>
        /// <param name="name">
        /// The name of the mark to check for.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the mark.
        /// </param>
        /// <returns>
        /// True if the named mark is present; otherwise, false.
        /// </returns>
        bool HasMark(string name, ref object value);
        /// <summary>
        /// Sets or clears the named mark on this call frame, associating the
        /// specified value with it.
        /// </summary>
        /// <param name="mark">
        /// Non-zero to set the mark; zero to clear it.
        /// </param>
        /// <param name="name">
        /// The name of the mark to set or clear.
        /// </param>
        /// <param name="value">
        /// The value to associate with the mark.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the mark was set or cleared; otherwise, false.
        /// </returns>
        bool SetMark(bool mark, string name, object value);
        /// <summary>
        /// Sets or clears the named mark on this call frame, associating the
        /// specified flags and value with it.
        /// </summary>
        /// <param name="mark">
        /// Non-zero to set the mark; zero to clear it.
        /// </param>
        /// <param name="flags">
        /// The flags to associate with the mark.
        /// </param>
        /// <param name="name">
        /// The name of the mark to set or clear.
        /// </param>
        /// <param name="value">
        /// The value to associate with the mark.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the mark was set or cleared; otherwise, false.
        /// </returns>
        bool SetMark(bool mark, CallFrameFlags flags, string name, object value);

        /// <summary>
        /// Saves the variables of this call frame, optionally limited to the
        /// specified arguments.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this call frame belongs to.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="arguments">
        /// The optional list of arguments that selects which variables to
        /// save.  This parameter may be null.
        /// </param>
        /// <param name="savedVariables">
        /// Upon success, receives the dictionary of saved variables.
        /// </param>
        /// <param name="count">
        /// On input, the running count of saved variables; on output, the
        /// updated count.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Save(
            Interpreter interpreter,               /* in */
            ArgumentList arguments,                /* in: OPTIONAL */
            ref VariableDictionary savedVariables, /* out */
            ref int count,                         /* in, out */
            ref Result error                       /* out */
        );

        /// <summary>
        /// Saves the variables of this call frame, optionally limited to the
        /// specified arguments.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this call frame belongs to.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="arguments">
        /// The optional dictionary of arguments that selects which variables
        /// to save.  This parameter may be null.
        /// </param>
        /// <param name="savedVariables">
        /// Upon success, receives the dictionary of saved variables.
        /// </param>
        /// <param name="count">
        /// On input, the running count of saved variables; on output, the
        /// updated count.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Save(
            Interpreter interpreter,               /* in */
            ArgumentDictionary arguments,          /* in: OPTIONAL */
            ref VariableDictionary savedVariables, /* out */
            ref int count,                         /* in, out */
            ref Result error                       /* out */
        );

        /// <summary>
        /// Restores the variables of this call frame from a previously saved
        /// dictionary, optionally limited to the specified arguments.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this call frame belongs to.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="arguments">
        /// The optional list of arguments that selects which variables to
        /// restore.  This parameter may be null.
        /// </param>
        /// <param name="savedVariables">
        /// On input, the dictionary of previously saved variables; on output,
        /// the updated dictionary.
        /// </param>
        /// <param name="count">
        /// On input, the running count of restored variables; on output, the
        /// updated count.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Restore(
            Interpreter interpreter,               /* in */
            ArgumentList arguments,                /* in: OPTIONAL */
            ref VariableDictionary savedVariables, /* in, out */
            ref int count,                         /* in, out */
            ref Result error                       /* out */
        );

        /// <summary>
        /// Restores the variables of this call frame from a previously saved
        /// dictionary, optionally limited to the specified arguments.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this call frame belongs to.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="arguments">
        /// The optional dictionary of arguments that selects which variables
        /// to restore.  This parameter may be null.
        /// </param>
        /// <param name="savedVariables">
        /// On input, the dictionary of previously saved variables; on output,
        /// the updated dictionary.
        /// </param>
        /// <param name="count">
        /// On input, the running count of restored variables; on output, the
        /// updated count.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Restore(
            Interpreter interpreter,               /* in */
            ArgumentDictionary arguments,          /* in: OPTIONAL */
            ref VariableDictionary savedVariables, /* in, out */
            ref int count,                         /* in, out */
            ref Result error                       /* out */
        );

        /// <summary>
        /// Frees the resources associated with this call frame.
        /// </summary>
        /// <param name="global">
        /// Non-zero if this is the global call frame.
        /// </param>
        void Free(bool global);
    }
}
