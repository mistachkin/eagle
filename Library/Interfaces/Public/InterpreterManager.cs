/*
 * InterpreterManager.cs --
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

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that manage the lifetime and
    /// relationships of interpreters, including the stack of active
    /// interpreters and the creation, addition, lookup, and removal of child
    /// interpreters.
    /// </summary>
    [ObjectId("cb781d88-6b9a-4689-82c0-849c230117e8")]
    public interface IInterpreterManager
    {
        /// <summary>
        /// Pushes this interpreter onto the active interpreter stack.
        /// </summary>
        /// <param name="clientData">
        /// The extra data to associate with the active interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter was pushed; otherwise, false.
        /// </returns>
        bool PushActive(IClientData clientData);

        /// <summary>
        /// Examines the active interpreter stack without modifying it.
        /// </summary>
        /// <returns>
        /// True if there is an active interpreter on the stack; otherwise,
        /// false.
        /// </returns>
        bool PeekActive();

        /// <summary>
        /// Pops the most recently pushed interpreter from the active
        /// interpreter stack.
        /// </summary>
        /// <returns>
        /// True if an interpreter was popped; otherwise, false.
        /// </returns>
        bool PopActive();

        /// <summary>
        /// Enables or disables disposal of this interpreter.
        /// </summary>
        /// <param name="noComplain">
        /// Non-zero to suppress the reporting of any errors that may be
        /// encountered.
        /// </param>
        /// <param name="enabled">
        /// The new disposal-enabled value to set, or null to query the current
        /// value without changing it.
        /// </param>
        /// <returns>
        /// The previous disposal-enabled value, or null if it could not be
        /// determined.
        /// </returns>
        bool? SetDisposalEnabled(bool noComplain, bool? enabled);

        /// <summary>
        /// Determines whether this interpreter is an orphan, i.e. one that has
        /// no parent interpreter.
        /// </summary>
        /// <returns>
        /// True if this interpreter is an orphan; otherwise, false.
        /// </returns>
        bool IsOrphanInterpreter();

        /// <summary>
        /// Determines whether this interpreter has any child interpreters.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if this interpreter has one or more child interpreters;
        /// otherwise, false.
        /// </returns>
        bool HasChildInterpreters(ref Result error);

        /// <summary>
        /// Determines whether a child interpreter with the specified path
        /// exists.
        /// </summary>
        /// <param name="path">
        /// The path that identifies the child interpreter to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the child interpreter exists;
        /// otherwise, a non-Ok value.
        /// </returns>
        ReturnCode DoesChildInterpreterExist(string path);

        //
        // TODO: Change these to use the IInterpreter type.
        //
        /// <summary>
        /// Locates the child interpreter identified by the specified path,
        /// optionally creating it if it does not already exist.
        /// </summary>
        /// <param name="path">
        /// The path that identifies the child interpreter to locate.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="nested">
        /// Non-zero if nested child interpreters should be searched.
        /// </param>
        /// <param name="create">
        /// Non-zero if the child interpreter should be created when it does not
        /// already exist.
        /// </param>
        /// <param name="interpreter">
        /// Upon success, this will contain the located child interpreter.
        /// </param>
        /// <param name="name">
        /// Upon success, this will contain the name of the located child
        /// interpreter.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetChildInterpreter(
            string path,
            LookupFlags lookupFlags,
            bool nested,
            bool create,
            ref Interpreter interpreter,
            ref string name,
            ref Result error
            );

        /// <summary>
        /// Creates a new child interpreter identified by the specified path.
        /// </summary>
        /// <param name="path">
        /// The path that identifies the child interpreter to create.
        /// </param>
        /// <param name="clientData">
        /// The extra data to associate with the new child interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="interpreterSettings">
        /// The settings used to configure the new child interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if the new child interpreter should be created in an
        /// isolated application domain.
        /// </param>
        /// <param name="security">
        /// Non-zero if security should be enabled for the new child
        /// interpreter.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result produced while creating
        /// the child interpreter.  Upon failure, this will contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode CreateChildInterpreter(
            string path,
            IClientData clientData,
            IInterpreterSettings interpreterSettings,
            bool isolated,
            bool security,
            ref Result result
            );

        /// <summary>
        /// Adds an existing interpreter as a child of this interpreter using
        /// the specified name.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the child interpreter.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter to add as a child.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to associate with the child interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode AddChildInterpreter(
            string name,
            Interpreter interpreter,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Removes the child interpreter associated with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the child interpreter to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra data to associate with the removal, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode RemoveChildInterpreter(
            string name,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Removes the child interpreter associated with the specified name,
        /// optionally waiting for the removal to complete.
        /// </summary>
        /// <param name="name">
        /// The name of the child interpreter to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra data to associate with the removal, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="synchronous">
        /// Non-zero if the removal should be performed synchronously, waiting
        /// for it to complete before returning.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode RemoveChildInterpreter(
            string name,
            IClientData clientData,
            bool synchronous,
            ref Result error
            );
    }
}
