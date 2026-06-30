/*
 * NamespaceOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using VariablePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.IVariable>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the private helper methods used to implement Eagle
    /// namespace support, including creating, disposing, looking up, matching,
    /// and naming namespaces, as well as managing the current namespace for
    /// call frames and the import/export/scope behavior of the [namespace]
    /// command.
    /// </summary>
    [ObjectId("704cb03e-df30-470f-81be-27fa3f128a88")]
    internal static class NamespaceOps
    {
        #region Private Constants
        /// <summary>
        /// The set of delimiter strings used when splitting a qualified name
        /// into its component parts; it contains only the namespace separator.
        /// </summary>
        private static readonly string[] Delimiters = new string[] {
            TclVars.Namespace.Separator
        };

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The result returned when splitting a null or empty name; it
        /// contains a single empty string element.
        /// </summary>
        private static readonly string[] EmptyName = new string[] {
            String.Empty
        };

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The composite format string used to build an absolute namespace
        /// name from the global namespace prefix and a relative name.
        /// </summary>
        private static readonly string AbsoluteNameFormat = "{0}{1}";

        /// <summary>
        /// The composite format string used to build a qualified namespace
        /// name from a set of qualifiers, the namespace separator, and a tail.
        /// </summary>
        private static readonly string QualifiedNameFormat = "{0}{1}{2}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Namespace Support Methods
        #region Instance Per-Frame Support Methods
        /// <summary>
        /// This method gets the current namespace associated with the
        /// specified call frame, falling back to the interpreter current
        /// frame when no frame is specified.  A disposed namespace is never
        /// returned and any reference to one found on the frame is cleared.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="frame">
        /// The call frame whose current namespace is being queried.  When
        /// null, the current frame of the interpreter is used.
        /// </param>
        /// <returns>
        /// The current namespace for the call frame, or null if there is
        /// none (or it was disposed).
        /// </returns>
        public static INamespace GetCurrent(
            Interpreter interpreter,
            ICallFrame frame
            )
        {
            INamespace @namespace = null;

            if ((frame == null) && (interpreter != null))
                frame = interpreter.CurrentFrame;

            if (frame != null)
            {
                IClientData clientData = frame.ResolveData;

                if (clientData != null)
                {
                    @namespace = clientData.Data as INamespace;

                    //
                    // HACK: Never allow a disposed namespace to be returned
                    //       from this method.  Furthermore, if a call frame
                    //       refers to a disposed namespace, clear it.  For
                    //       the "primary" thread (i.e. in this context, the
                    //       one where the current namespace was disposed),
                    //       this should never be necessary because the
                    //       DeleteNamespace calls ClearCurrentForAll, which
                    //       clears all references to the disposed namespace
                    //       from the entire call stack for that thread.
                    //       Unfortunately, there is no way to do that for
                    //       every thread that may reference the disposed
                    //       namespace; therefore, given the current design,
                    //       this workaround is necessary.
                    //
                    if (IsDisposed(@namespace))
                    {
                        //
                        // HACK: This situation should be extremely rare and
                        //       unusual; therefore, complain.
                        //
                        DebugOps.Complain(
                            interpreter, ReturnCode.Error, String.Format(
                            "forcibly cleared namespace for frame {0}",
                            FormatOps.DisplayCallFrame(frame)));

                        clientData.Data = @namespace = null;
                    }
                }
            }

            return @namespace;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the current namespace associated with the
        /// specified call frame, falling back to the interpreter current
        /// frame when no frame is specified.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="frame">
        /// The call frame whose current namespace is being set.  When null,
        /// the current frame of the interpreter is used.
        /// </param>
        /// <param name="namespace">
        /// The namespace to associate with the call frame.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the current namespace was set; otherwise, false.
        /// </returns>
        public static bool SetCurrent(
            Interpreter interpreter,
            ICallFrame frame,
            INamespace @namespace
            )
        {
            if ((frame == null) && (interpreter != null))
                frame = interpreter.CurrentFrame;

            if (frame != null)
            {
                IClientData clientData = frame.ResolveData;

                if (clientData != null)
                {
                    clientData.Data = @namespace;
                }
                else
                {
                    clientData = new ResolverClientData(@namespace);
                    frame.ResolveData = clientData;
                }

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the current namespace on every call frame in the
        /// chain starting with the specified frame (or the interpreter current
        /// frame when none is specified).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="frame">
        /// The first call frame in the chain to process.  When null, the
        /// current frame of the interpreter is used.
        /// </param>
        /// <param name="namespace">
        /// The namespace to associate with each call frame.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The number of call frames whose current namespace was set.
        /// </returns>
        private static int SetCurrentForAll(
            Interpreter interpreter,
            ICallFrame frame,
            INamespace @namespace
            )
        {
            ICallFrame thisFrame = frame;

            if ((thisFrame == null) && (interpreter != null))
                thisFrame = interpreter.CurrentFrame;

            int result = 0;

            while (thisFrame != null)
            {
                if (SetCurrent(interpreter, thisFrame, @namespace))
                    result++;

                thisFrame = thisFrame.Next;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the current namespace on every call frame in the
        /// specified call stack that currently refers to the specified
        /// namespace.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack whose frames are examined and possibly cleared.
        /// </param>
        /// <param name="namespace">
        /// The namespace whose references are to be cleared from the call
        /// stack.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ClearCurrentForAll(
            Interpreter interpreter,
            CallStack callStack,
            INamespace @namespace,
            ref Result error
            )
        {
            if (callStack == null)
            {
                error = "invalid call stack";
                return ReturnCode.Error;
            }

            int count = callStack.Count;

            for (int index = 0; index < count; index++)
            {
                ICallFrame frame = callStack[index];

                if (frame == null)
                    continue;

                INamespace currentNamespace = GetCurrent(interpreter, frame);

                if (currentNamespace == null)
                    continue;

                if (!IsSame(currentNamespace, @namespace))
                    continue;

                if (!SetCurrent(interpreter, frame, null))
                {
                    error = String.Format(
                        "could not clear current namespace for frame {0}",
                        FormatOps.DisplayCallFrame(frame));

                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Instance Creation & Disposal Support Methods
        /// <summary>
        /// This method obtains the variable call frame to be used for a
        /// namespace, reusing the one previously held by the old namespace
        /// when available, or creating a new namespace call frame otherwise.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the variable call frame.
        /// </param>
        /// <param name="oldNamespace">
        /// The namespace whose stored variable call frame may be reused.
        /// This parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter used to create a new call frame, if necessary.
        /// This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments to assign to the variable call frame.  This
        /// parameter may be null.
        /// </param>
        /// <param name="newFrame">
        /// Non-zero to create a new namespace call frame when an existing one
        /// cannot be reused.
        /// </param>
        /// <returns>
        /// The variable call frame to use, or null if none could be obtained.
        /// </returns>
        private static ICallFrame CreateVariableFrame(
            string name,
            INamespace oldNamespace,
            Interpreter interpreter,
            ArgumentList arguments,
            bool newFrame
            )
        {
            if (oldNamespace != null)
            {
                ICallFrame frame = oldNamespace.GetAndClearVariableFrame();

                if (frame != null)
                {
                    frame.Name = name;
                    frame.Arguments = arguments;

                    return frame;
                }
            }

            if (newFrame && (interpreter != null))
            {
                ICallFrame frame = interpreter.NewNamespaceCallFrame(
                    name, CallFrameFlags.None, arguments);

                if (frame != null)
                    return frame;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates the global namespace for the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that will own the new global namespace.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created global namespace.
        /// </returns>
        public static INamespace CreateGlobal(
            Interpreter interpreter
            )
        {
            return Create(
                TclVars.Namespace.GlobalName, null, interpreter, null,
                null, (interpreter != null) ? interpreter.Unknown : null,
                null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method creates a temporary namespace using the specified
        /// properties.
        /// </summary>
        /// <param name="name">
        /// The name of the namespace to create.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the new namespace.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that will own the new namespace.
        /// </param>
        /// <param name="parent">
        /// The parent namespace of the new namespace, if any.
        /// </param>
        /// <param name="resolve">
        /// The resolver to associate with the new namespace, if any.
        /// </param>
        /// <returns>
        /// The newly created namespace.
        /// </returns>
        public static INamespace CreateTemporary(
            string name,
            IClientData clientData,
            Interpreter interpreter,
            INamespace parent,
            IResolve resolve
            )
        {
            return new Namespace(new NamespaceData(
                name, clientData, interpreter, parent, resolve,
                CreateVariableFrame(name, null, interpreter,
                null, true), null));
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified namespace has been
        /// disposed by casting it to the concrete namespace type and checking
        /// its disposed flag.
        /// </summary>
        /// <param name="namespace">
        /// The namespace to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the namespace has been disposed; otherwise, false.
        /// </returns>
        private static bool IsDisposed(
            INamespace @namespace
            )
        {
            if (@namespace == null)
                return false;

            //
            // HACK: Try to cast to the actual Namespace class here; we
            //       need to use the Disposed property, which is not part
            //       of the formal interface.
            //
            Namespace localNamespace = @namespace as Namespace;

            if (localNamespace == null)
                return false;

            return localNamespace.Disposed;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new namespace, reusing the variable call
        /// frame held by an existing namespace when possible.
        /// </summary>
        /// <param name="name">
        /// The name of the new namespace.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the new namespace.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that will own the new namespace.  This parameter
        /// may be null.
        /// </param>
        /// <param name="parent">
        /// The parent of the new namespace.  This parameter may be null.
        /// </param>
        /// <param name="resolve">
        /// The custom resolver to associate with the new namespace.  This
        /// parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// The existing namespace whose variable call frame may be reused.
        /// This parameter may be null.
        /// </param>
        /// <param name="unknown">
        /// The name of the unknown command handler for the new namespace.
        /// This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments to assign to the variable call frame.  This
        /// parameter may be null.
        /// </param>
        /// <param name="newFrame">
        /// Non-zero to create a new namespace call frame when an existing one
        /// cannot be reused.
        /// </param>
        /// <returns>
        /// The newly created namespace.
        /// </returns>
        public static INamespace CreateFrom(
            string name,
            IClientData clientData,
            Interpreter interpreter,
            INamespace parent,
            IResolve resolve,
            INamespace @namespace,
            string unknown,
            ArgumentList arguments,
            bool newFrame
            )
        {
            ICallFrame frame = CreateVariableFrame(
                name, @namespace, interpreter, arguments, newFrame);

            INamespace newNamespace = new Namespace(new NamespaceData(
                name, clientData, interpreter, parent, resolve, frame,
                unknown));

            if (frame != null)
                frame.ResolveData = new ResolverClientData(newNamespace);

            return newNamespace;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new namespace from the supplied namespace
        /// data.
        /// </summary>
        /// <param name="namespaceData">
        /// The namespace data describing the namespace to create.
        /// </param>
        /// <param name="arguments">
        /// The arguments to assign to the variable call frame.  This
        /// parameter may be null.
        /// </param>
        /// <param name="newFrame">
        /// Non-zero to create a new namespace call frame.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created namespace, or null on failure.
        /// </returns>
        public static INamespace Create(
            INamespaceData namespaceData,
            ArgumentList arguments,
            bool newFrame,
            ref Result error
            )
        {
            if (namespaceData == null)
            {
                error = "invalid namespace data";
                return null;
            }

            return Create(
                namespaceData.Name, namespaceData.ClientData,
                namespaceData.Interpreter, namespaceData.Parent,
                namespaceData.Resolve, namespaceData.Unknown,
                arguments, newFrame);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new namespace using the supplied individual
        /// property values.
        /// </summary>
        /// <param name="name">
        /// The name of the new namespace.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the new namespace.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that will own the new namespace.  This parameter
        /// may be null.
        /// </param>
        /// <param name="parent">
        /// The parent of the new namespace.  This parameter may be null.
        /// </param>
        /// <param name="resolve">
        /// The custom resolver to associate with the new namespace.  This
        /// parameter may be null.
        /// </param>
        /// <param name="unknown">
        /// The name of the unknown command handler for the new namespace.
        /// This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments to assign to the variable call frame.  This
        /// parameter may be null.
        /// </param>
        /// <param name="newFrame">
        /// Non-zero to create a new namespace call frame.
        /// </param>
        /// <returns>
        /// The newly created namespace.
        /// </returns>
        private static INamespace Create(
            string name,
            IClientData clientData,
            Interpreter interpreter,
            INamespace parent,
            IResolve resolve,
            string unknown,
            ArgumentList arguments,
            bool newFrame
            )
        {
            ICallFrame frame = CreateVariableFrame(
                name, null, interpreter, arguments, newFrame);

            INamespace @namespace = new Namespace(new NamespaceData(
                name, clientData, interpreter, parent, resolve, frame,
                unknown));

            if (frame != null)
                frame.ResolveData = new ResolverClientData(@namespace);

            return @namespace;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes the specified namespace, complaining if an
        /// error is encountered.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// The namespace to dispose.  Upon return, this is set to null.
        /// </param>
        public static void Dispose(
            Interpreter interpreter,
            ref INamespace @namespace
            )
        {
            ReturnCode code;
            Result error = null;

            code = Dispose(interpreter, ref @namespace, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes the specified namespace, reporting any failure
        /// via the return code and error message.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// The namespace to dispose.  Upon return, this is set to null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Dispose(
            Interpreter interpreter,
            ref INamespace @namespace,
            ref Result error
            )
        {
            if (@namespace != null)
            {
                IDisposable disposable = @namespace as IDisposable;

                if (disposable != null)
                {
                    GlobalState.PushActiveInterpreter(interpreter);

                    try
                    {
                        disposable.Dispose(); /* throw */
                        disposable = null;

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                        return ReturnCode.Error;
                    }
                    finally
                    {
                        /* IGNORED */
                        GlobalState.PopActiveInterpreter();
                    }
                }

                @namespace = null;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Instance Matching Support Methods
        /// <summary>
        /// This method determines whether the specified executable entity has
        /// a name that refers to the global namespace.
        /// </summary>
        /// <param name="execute">
        /// The executable entity to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the entity has a global namespace name; otherwise, false.
        /// </returns>
        public static bool IsGlobal(
            IExecute execute
            )
        {
            IIdentifierName identifierName = execute as IIdentifierName;

            if (identifierName == null)
                return false;

            return NamespaceOps.IsGlobalName(identifierName.Name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified namespace is the
        /// global namespace of the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose global namespace is compared.  This
        /// parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// The namespace to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the namespace is the global namespace of the interpreter;
        /// otherwise, false.
        /// </returns>
        public static bool IsGlobal(
            Interpreter interpreter,
            INamespace @namespace
            )
        {
            if ((interpreter == null) || (@namespace == null))
                return false;

            return interpreter.IsGlobalNamespace(@namespace);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two namespace references refer to
        /// the same namespace instance.
        /// </summary>
        /// <param name="namespace1">
        /// The first namespace to compare.  This parameter may be null.
        /// </param>
        /// <param name="namespace2">
        /// The second namespace to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if both references refer to the same instance; otherwise,
        /// false.
        /// </returns>
        public static bool IsSame(
            INamespace namespace1,
            INamespace namespace2
            )
        {
            return Object.ReferenceEquals(namespace1, namespace2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the first namespace is the same as,
        /// or a descendant of, the second namespace by walking the parent
        /// chain.
        /// </summary>
        /// <param name="namespace1">
        /// The namespace whose ancestry is walked.  This parameter may be
        /// null.
        /// </param>
        /// <param name="namespace2">
        /// The candidate ancestor namespace.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the first namespace is the same as, or a descendant of, the
        /// second namespace; otherwise, false.
        /// </returns>
        public static bool IsDescendant(
            INamespace namespace1,
            INamespace namespace2
            )
        {
            if ((namespace1 == null) || (namespace2 == null))
                return false;

            INamespace @namespace = namespace1;

            while (@namespace != null)
            {
                if (Object.ReferenceEquals(@namespace, namespace2))
                    return true;

                @namespace = @namespace.Parent;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Entity Matching Support Methods
        /// <summary>
        /// This method counts the number of namespace qualifiers (i.e.
        /// namespace separators) present in the specified name, ignoring any
        /// superfluous extra colons.
        /// </summary>
        /// <param name="name">
        /// The name to examine.  This parameter may be null or empty.
        /// </param>
        /// <returns>
        /// The number of namespace qualifiers found in the name.
        /// </returns>
        private static int CountQualifiers(
            string name
            )
        {
            int count = 0;

            if (String.IsNullOrEmpty(name))
                return count;

            string separator = TclVars.Namespace.Separator;
            int separatorLength = separator.Length;
            int length = name.Length;

            int index = name.IndexOf(
                separator, SharedStringOps.SystemComparisonType);

            while (index != Index.Invalid)
            {
                count++; index += separatorLength;

                //
                // NOTE: Skip superfluous extra colons between the namespace
                //       names.
                //
                while ((index < length) &&
                    (name[index] == Characters.Colon))
                {
                    index++;
                }

                if (index >= length)
                    break;

                index = name.IndexOf(separator, index,
                    SharedStringOps.SystemComparisonType);
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether simple (i.e. non-qualified) pattern
        /// matching should be used for the specified pattern and namespace, by
        /// splitting the pattern and delegating to the qualifier-based
        /// overload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// The target namespace for the match.  This parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The pattern to be matched.  This parameter may be null.
        /// </param>
        /// <param name="default">
        /// The value to return when the pattern cannot be split.
        /// </param>
        /// <returns>
        /// True if simple matching should be used; otherwise, false.
        /// </returns>
        public static bool ShouldUseSimpleMatching(
            Interpreter interpreter,
            INamespace @namespace,
            string pattern,
            bool @default
            )
        {
            string qualifiers = null;
            string tail = null;

            if (SplitName(
                    pattern, ref qualifiers, ref tail) != ReturnCode.Ok)
            {
                return @default;
            }

            return ShouldUseSimpleMatching(
                interpreter, @namespace, qualifiers, tail);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether simple (i.e. non-qualified) pattern
        /// matching should be used, which is the case only when the pattern
        /// has no qualifiers and the target namespace is the global one.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// The target namespace for the match.  This parameter may be null.
        /// </param>
        /// <param name="qualifiers">
        /// The qualifiers portion parsed from the pattern.  This parameter may
        /// be null.
        /// </param>
        /// <param name="tail">
        /// The tail portion parsed from the pattern.  This parameter is not
        /// used.
        /// </param>
        /// <returns>
        /// True if simple matching should be used; otherwise, false.
        /// </returns>
        private static bool ShouldUseSimpleMatching(
            Interpreter interpreter,
            INamespace @namespace,
            string qualifiers,
            string tail /* NOT USED */
            )
        {
            //
            // NOTE: Determine if the pattern is qualified (i.e. it contains
            //       qualifiers) and if the specified namespace is global.  If
            //       both of these conditions are true, we are matching simple
            //       names [in the global namespace only].
            //
            if (((qualifiers == null) || IsGlobalName(qualifiers)) &&
                ((@namespace == null) || IsGlobal(interpreter, @namespace)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method matches the items in the specified collection against
        /// the specified namespace pattern, adding the matching names to the
        /// output list in the requested form (tail-only, absolute, or as-is).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="namespace">
        /// The target namespace for the match.  This parameter may be null.
        /// </param>
        /// <param name="collection">
        /// The collection of candidate names to match.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match against.  When null, all items match.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <param name="useNamespace">
        /// Non-zero to use the namespace provided by the caller instead of the
        /// global namespace when the pattern qualifiers are global.
        /// </param>
        /// <param name="tailOnly">
        /// Non-zero to add only the tail portion of each matching name.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to add each matching name in absolute form.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat a missing target namespace as an error.
        /// </param>
        /// <param name="list">
        /// Upon return, this contains the list of matching names; it is
        /// created if necessary and otherwise appended to.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode MatchItems(
            Interpreter interpreter,
            INamespace @namespace,
            IEnumerable<string> collection,
            string pattern,
            bool noCase,
            bool useNamespace,
            bool tailOnly,
            bool absolute,
            bool strict,
            ref StringList list,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (collection == null)
            {
                error = "invalid collection";
                return ReturnCode.Error;
            }

            string qualifiers = null;
            string tail = null;

            if ((pattern != null) && (SplitName(pattern,
                    ref qualifiers, ref tail, ref error) != ReturnCode.Ok))
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: Create the output list, if necessary.  If the list has
            //       already been created (e.g. either by the caller or via
            //       a previous call to this method), we are simply adding
            //       to it.
            //
            if (list == null)
                list = new StringList();

            //
            // NOTE: Should simple (i.e. non-qualified) pattern matching be
            //       used?  This will only be used if the target namespace
            //       is global and no qualifiers were found in the pattern.
            //
            if (ShouldUseSimpleMatching(
                    interpreter, @namespace, qualifiers, tail))
            {
                //
                // NOTE: Global namespace with a non-qualified pattern, use
                //       the pattern without any leading colons (i.e. since
                //       command/procedure names are always stored relative
                //       to the global namespace).
                //
                string qualifiedPattern = TrimLeading(pattern);

                foreach (string item in collection)
                {
                    //
                    // NOTE: Skip all null items.
                    //
                    if (item == null)
                        continue;

                    //
                    // HACK: Executable entities in the global namespace are
                    //       *NEVER* stored with their namespace name;
                    //       therefore, a qualified name can never match here
                    //       because we want matches in the global namespace.
                    //
                    if (IsQualifiedName(item))
                        continue;

                    //
                    // NOTE: See if the current item matches the pattern.  If
                    //       the pattern is null then all items match.
                    //
                    if ((qualifiedPattern == null) || StringOps.Match(
                            interpreter, MatchMode.Glob, item,
                            qualifiedPattern, noCase))
                    {
                        list.Add(absolute ? MakeAbsoluteName(item) : item);
                    }
                }
            }
            else
            {
                //
                // NOTE: If necessary, lookup the target namespace based on
                //       the qualifiers that were parsed from the pattern.
                //
                if (qualifiers != null)
                {
                    //
                    // BUGFIX: If qualifiers is an empty string, we must
                    //         use the global namespace here.
                    //
                    if (IsGlobalName(qualifiers))
                    {
                        //
                        // BUGFIX: Use the namespace provided by the caller
                        //         instead of the global namespace when the
                        //         flag is set.
                        //
                        if (!useNamespace)
                            @namespace = interpreter.GlobalNamespace;
                    }
                    else
                    {
                        @namespace = Lookup(
                            interpreter, @namespace, qualifiers, false,
                            false, true, ref error);
                    }

                    if (@namespace == null)
                        return strict ? ReturnCode.Error : ReturnCode.Ok;
                }

                //
                // NOTE: Convert the pattern into one that will only match
                //       children of the selected namespace (which may be
                //       the global namespace), without any leading colons
                //       (i.e. since command/procedure names are always
                //       stored relative to the global namespace).
                //
                string qualifiedPattern = MakeQualifiedPattern(
                    interpreter, @namespace, tail, false);

                //
                // NOTE: Count how many nested namespaces are being used by
                //       this pattern.  This count will be compared with the
                //       count for each potential match.
                //
                int qualifierCount = CountQualifiers(qualifiedPattern);

                foreach (string item in collection)
                {
                    //
                    // NOTE: Skip all null items.
                    //
                    if (item == null)
                        continue;

                    //
                    // HACK: This is a quick-and-dirty method of making sure
                    //       that this method only matches items within the
                    //       specified namespace (i.e. and not within nested
                    //       namespaces), even if other items would match
                    //       the pattern.
                    //
                    int itemQualifierCount = CountQualifiers(item);

                    if (itemQualifierCount != qualifierCount)
                        continue;

                    //
                    // NOTE: See if the current item matches the constructed
                    //       qualified pattern.  If the qualified pattern is
                    //       null then all items match.
                    //
                    if ((qualifiedPattern == null) || StringOps.Match(
                            interpreter, MatchMode.Glob, item,
                            qualifiedPattern, noCase))
                    {
                        list.Add(tailOnly ? TailOnly(item) :
                            absolute ? MakeAbsoluteName(item) : item);
                    }
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Name Trimming Support Methods
        /// <summary>
        /// This method removes the leading global namespace separator from the
        /// specified name, if present.
        /// </summary>
        /// <param name="name">
        /// The name to trim.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The name with any leading global namespace separator removed.
        /// </returns>
        public static string TrimLeading(
            string name
            )
        {
            bool absolute = false;

            return TrimLeading(name, ref absolute);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the leading global namespace separator from the
        /// specified name, if present, and reports whether the name was
        /// absolute.
        /// </summary>
        /// <param name="name">
        /// The name to trim.  This parameter may be null.
        /// </param>
        /// <param name="absolute">
        /// Upon return, this is set to non-zero if the name was an absolute
        /// name (i.e. it had a leading global namespace separator).
        /// </param>
        /// <returns>
        /// The name with any leading global namespace separator removed.
        /// </returns>
        public static string TrimLeading(
            string name,
            ref bool absolute
            )
        {
            //
            // WARNING: This function is not totally generic.  It is designed
            //          to remove leading colons if there are at least two of
            //          them at the start of the string to facilitate Tcl
            //          variable name compatiblity.  The use of this API will
            //          be (mostly) banned once full [namespace] support has
            //          been added.
            //
            string result = name;

            //
            // HACK: Support global variables without using the global command
            //       if they are prefixed with "::" (global namespace) for Tcl
            //       source compatibility.
            //
            if (IsAbsoluteName(result))
            {
                if (result != null) // NOTE: Redundant [for now].
                    result = result.TrimStart(Characters.Colon);

                absolute = true;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all leading and trailing namespace separator
        /// (colon) characters from the specified name.
        /// </summary>
        /// <param name="name">
        /// The name to trim.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The name with all leading and trailing colons removed, or null if
        /// the name was null.
        /// </returns>
        public static string TrimAll(
            string name
            )
        {
            if (name == null)
                return null;

            return name.Trim(Characters.Colon);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Name Building Support Methods
        /// <summary>
        /// This method gets the qualified name of the specified namespace,
        /// optionally returning a display placeholder when the namespace is
        /// null or disposed.
        /// </summary>
        /// <param name="namespace">
        /// The namespace whose qualified name is requested.  This parameter
        /// may be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a display placeholder for a null or disposed
        /// namespace instead of null.
        /// </param>
        /// <returns>
        /// The qualified name of the namespace, or null/display placeholder
        /// when it is null or disposed.
        /// </returns>
        public static string MaybeQualifiedName(
            INamespace @namespace,
            bool display
            )
        {
            if (@namespace == null)
                return display ? FormatOps.DisplayNull : null;

            if (IsDisposed(@namespace))
                return display ? FormatOps.DisplayDisposed : null;

            return @namespace.QualifiedName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the fully qualified, absolute name for an entity
        /// by walking the parent chain of the specified namespace and appending
        /// the optional entity name.
        /// </summary>
        /// <param name="namespace">
        /// The namespace that contains the entity.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The command or variable name to append.  This parameter may be null
        /// or empty.
        /// </param>
        /// <returns>
        /// The fully qualified, absolute name.
        /// </returns>
        public static string GetQualifiedName(
            INamespace @namespace,
            string name
            )
        {
            StringList list = new StringList();

            while (@namespace != null)
            {
                string localName = @namespace.Name;

                if (!String.IsNullOrEmpty(localName))
                    list.Add(localName);

                @namespace = @namespace.Parent;
            }

            list.Reverse(); /* O(N) */

            if (!String.IsNullOrEmpty(name))
                list.Add(name); /* NOTE: Command or variable name. */

            return TclVars.Namespace.Global +
                list.ToString(TclVars.Namespace.Separator, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a name from a set of qualifiers and a tail,
        /// optionally making it qualified and/or absolute.
        /// </summary>
        /// <param name="qualifiers">
        /// The qualifiers portion of the name.  This parameter may be null.
        /// </param>
        /// <param name="tail">
        /// The tail portion of the name.  When null or empty, it is returned
        /// unchanged.
        /// </param>
        /// <param name="qualified">
        /// Non-zero to combine the qualifiers and tail into a qualified name.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to make the resulting name absolute.
        /// </param>
        /// <returns>
        /// The constructed name.
        /// </returns>
        public static string MakeName(
            string qualifiers,
            string tail,
            bool qualified,
            bool absolute
            )
        {
            string result = tail;

            if (String.IsNullOrEmpty(result))
                return result;

            if (qualified)
                result = MakeQualifiedName(qualifiers, result, false);

            if (absolute)
                result = MakeAbsoluteName(result);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified name into an absolute name by
        /// prepending the global namespace separator when necessary.
        /// </summary>
        /// <param name="name">
        /// The name to make absolute.  This parameter may be null or empty.
        /// </param>
        /// <returns>
        /// The absolute form of the name, the global namespace name for an
        /// empty name, or null for a null name.
        /// </returns>
        public static string MakeAbsoluteName(
            string name
            )
        {
            if (name == null)
                return null;

            if (name.Length == 0)
                return TclVars.Namespace.GlobalName;

            if (IsAbsoluteName(name))
                return name;

            return String.Format(
                AbsoluteNameFormat, TclVars.Namespace.Global, name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method qualifies the specified name relative to the current
        /// namespace of the interpreter, producing a relative qualified name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose current namespace is used.
        /// </param>
        /// <param name="name">
        /// The name to qualify.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The qualified name.
        /// </returns>
        public static string MakeQualifiedName(
            Interpreter interpreter,
            string name
            )
        {
            return MakeQualifiedName(interpreter, name, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method qualifies the specified name relative to the current
        /// namespace of the interpreter, optionally making the result
        /// absolute.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose current namespace is used.
        /// </param>
        /// <param name="name">
        /// The name to qualify.  This parameter may be null.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to make the resulting qualified name absolute.
        /// </param>
        /// <returns>
        /// The qualified name, made absolute when requested.
        /// </returns>
        public static string MakeQualifiedName(
            Interpreter interpreter,
            string name,
            bool absolute
            )
        {
            INamespace currentNamespace = null;

            if (interpreter.GetCurrentNamespaceViaResolvers(
                    null, LookupFlags.NoVerbose,
                    ref currentNamespace) == ReturnCode.Ok)
            {
                string qualifiedName = MakeQualifiedName(
                    interpreter, currentNamespace, name);

                if (!absolute)
                    return qualifiedName;

                return MakeAbsoluteName(qualifiedName);
            }

            return absolute ? MakeAbsoluteName(name) : name;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the specified qualifiers and tail into a
        /// qualified name using the namespace separator, optionally trimming
        /// all colons from each part.
        /// </summary>
        /// <param name="qualifiers">
        /// The qualifiers portion of the name.  This parameter may be null.
        /// </param>
        /// <param name="tail">
        /// The tail portion of the name.  This parameter may be null.
        /// </param>
        /// <param name="trimAll">
        /// Non-zero to trim all leading and trailing colons from each part
        /// before combining.
        /// </param>
        /// <returns>
        /// The combined qualified name.
        /// </returns>
        public static string MakeQualifiedName(
            string qualifiers,
            string tail,
            bool trimAll
            )
        {
            //
            // HACK: Do not strip the leading colons from the qualifiers
            //       here because the caller may want them.
            //
            return String.Format(
                QualifiedNameFormat, trimAll ? TrimAll(qualifiers) :
                qualifiers, TclVars.Namespace.Separator, trimAll ?
                TrimAll(tail) : tail);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method qualifies the specified name relative to the specified
        /// namespace, returning a relative name for the global namespace.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// The namespace to qualify the name against.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The name to qualify.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The qualified name.
        /// </returns>
        public static string MakeQualifiedName(
            Interpreter interpreter,
            INamespace @namespace,
            string name
            )
        {
            if ((@namespace == null) || IsGlobal(interpreter, @namespace))
                return TrimLeading(name);

            if (name == null)
                return TrimLeading(@namespace.QualifiedName);

            if (IsAbsoluteName(name))
                return TrimLeading(name);

            return MakeQualifiedName(
                TrimLeading(@namespace.QualifiedName), name, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a qualified matching pattern that will match the
        /// children of the specified namespace, defaulting to a wildcard when
        /// no pattern is supplied.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// The namespace whose children the pattern should match.  This
        /// parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The pattern to qualify.  When null, a wildcard is used.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to make the resulting pattern absolute; otherwise, its
        /// leading separator is trimmed.
        /// </param>
        /// <returns>
        /// The qualified matching pattern.
        /// </returns>
        public static string MakeQualifiedPattern(
            Interpreter interpreter,
            INamespace @namespace,
            string pattern,
            bool absolute
            )
        {
            string qualifiedPattern = null;

            if ((@namespace == null) || IsGlobal(interpreter, @namespace))
            {
                if (pattern != null)
                {
                    qualifiedPattern = MakeQualifiedName(
                        TclVars.Namespace.GlobalName, pattern, false);
                }
                else
                {
                    qualifiedPattern = MakeQualifiedName(
                        TclVars.Namespace.GlobalName,
                        Characters.Asterisk.ToString(), false);
                }
            }
            else
            {
                if (pattern != null)
                {
                    qualifiedPattern = MakeQualifiedName(
                        @namespace.QualifiedName, pattern, false);
                }
                else
                {
                    qualifiedPattern = MakeQualifiedName(
                        @namespace.QualifiedName,
                        Characters.Asterisk.ToString(), false);
                }
            }

            if (absolute)
                qualifiedPattern = MakeAbsoluteName(qualifiedPattern);
            else
                qualifiedPattern = TrimLeading(qualifiedPattern);

            return qualifiedPattern;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified absolute name into one that is
        /// relative to the specified namespace, falling back to the current or
        /// global namespace of the interpreter as appropriate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose current namespace may be used as the basis.
        /// This parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// The namespace used as the basis for the relative name.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name to make relative.  This parameter may be null, empty, or
        /// already relative.
        /// </param>
        /// <returns>
        /// The relative form of the name.
        /// </returns>
        private static string MakeRelativeName(
            Interpreter interpreter,
            INamespace @namespace,
            string name
            )
        {
            //
            // NOTE: Is the name either a null/empty string -OR- already
            //       relative?
            //
            if (String.IsNullOrEmpty(name) || !IsAbsoluteName(name))
                return name;

            //
            // NOTE: Are we supposed to use the current namespace for the
            //       interpreter as the basis for the relative name?
            //
            if ((@namespace == null) && (interpreter != null))
            {
                INamespace currentNamespace = null;

                if ((interpreter.GetCurrentNamespaceViaResolvers(
                        null, LookupFlags.NoVerbose,
                        ref currentNamespace) == ReturnCode.Ok) &&
                    (currentNamespace != null) &&
                    !IsGlobal(interpreter, currentNamespace))
                {
                    //
                    // NOTE: Use the current namespace for the interpreter
                    //       as the basis for the relative name.
                    //
                    @namespace = currentNamespace;
                }
                else
                {
                    //
                    // HACK: Upon failure, fallback to using the global
                    //       namespace for the interpreter as the basis
                    //       for the relative name.
                    //
                    @namespace = null;
                }
            }

            //
            // NOTE: If there is no namespace available as the basis for
            //       the relative name, bail out now.
            //
            if (@namespace == null)
                return TrimLeading(name);

            return MakeRelativeName(@namespace.QualifiedName, name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified absolute name into one that is
        /// relative to the current namespace of the specified call frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="frame">
        /// The call frame whose current namespace is used as the basis.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name to make relative.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The relative form of the name.
        /// </returns>
        public static string MakeRelativeName(
            Interpreter interpreter,
            ICallFrame frame,
            string name
            )
        {
            return MakeRelativeName(
                interpreter, GetCurrent(interpreter, frame), name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method makes the second (fully qualified) name relative to the
        /// first (namespace) name, returning the trailing portion that follows
        /// the common prefix.
        /// </summary>
        /// <param name="name1">
        /// The namespace name that serves as the basis.
        /// </param>
        /// <param name="name2">
        /// The fully qualified name to make relative.
        /// </param>
        /// <returns>
        /// The portion of the second name that is relative to the first, or
        /// the trimmed second name when it is not nested beneath the first.
        /// </returns>
        private static string MakeRelativeName(
            string name1, /* this is the namespace name. */
            string name2  /* this is the fully qualified name. */
            )
        {
            //
            // NOTE: The second argument must be made into a name that is
            //       relative to the first argument.
            //
            string[] parts1 = SplitName(name1);
            string[] parts2 = SplitName(name2);

            //
            // NOTE: Therefore, the second part must have more parts than
            //       the first argument.
            //
            if (parts1.Length >= parts2.Length)
                return TrimLeading(name2);

            //
            // NOTE: Furthermore, each part of the first argument must
            //       match the corresponding one in the second argument,
            //       until there are no more parts in the first argument
            //       left to match against.
            //
            for (int index = 0; index < parts1.Length; index++)
            {
                if (!IsSame(parts1[index], parts2[index]))
                    return TrimLeading(name2);
            }

            //
            // NOTE: Since everything matched so far, the only thing that
            //       remains of the second argument is the relative offset
            //       from the first argument.
            //
            return new StringList(parts2, parts1.Length).ToString(
                TclVars.Namespace.Separator, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method normalizes the qualifiers and tail of a name by
        /// removing empty components and rejoining the remaining components
        /// with the namespace separator.
        /// </summary>
        /// <param name="qualifiers">
        /// On input, the qualifiers to normalize; on output, the normalized
        /// qualifiers.  This parameter may be null.
        /// </param>
        /// <param name="tail">
        /// On input, the tail to normalize; on output, the normalized tail.
        /// This parameter may be null.
        /// </param>
        private static void NormalizeName(
            ref string qualifiers, /* in, out */
            ref string tail        /* in, out */
            )
        {
            if (qualifiers != null)
            {
                StringBuilder newQualifiers = StringBuilderFactory.Create();

                foreach (string name in SplitName(qualifiers))
                {
                    if (String.IsNullOrEmpty(name))
                        continue;

                    if (newQualifiers.Length > 0)
                        newQualifiers.Append(TclVars.Namespace.Separator);

                    newQualifiers.Append(name);
                }

                qualifiers = StringBuilderCache.GetStringAndRelease(
                    ref newQualifiers);
            }

            if (tail != null)
            {
                StringBuilder newTail = StringBuilderFactory.Create();

                foreach (string name in SplitName(tail))
                {
                    if (String.IsNullOrEmpty(name))
                        continue;

                    if (newTail.Length > 0)
                        newTail.Append(TclVars.Namespace.Separator);

                    newTail.Append(name);
                }

                tail = StringBuilderCache.GetStringAndRelease(
                    ref newTail);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method normalizes the specified name into its fully qualified,
        /// absolute form relative to the current namespace of the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose current namespace is used.
        /// </param>
        /// <param name="name">
        /// On input, the name to normalize; on output, the normalized name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode NormalizeName(
            Interpreter interpreter,
            ref string name,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (name == null)
            {
                error = "invalid name";
                return ReturnCode.Error;
            }

            if (name.Length == 0)
                return ReturnCode.Ok;

            string qualifiers = null;
            string tail = null;
            NamespaceFlags flags = NamespaceFlags.None;

            if (SplitName(
                    name, ref qualifiers, ref tail, ref flags,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            bool absolute = FlagOps.HasFlags(
                flags, NamespaceFlags.Absolute, true);

            if (!absolute)
            {
                INamespace currentNamespace = null;

                if (interpreter.GetCurrentNamespaceViaResolvers(
                        null, LookupFlags.Default, ref currentNamespace,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                qualifiers = MakeQualifiedName(
                    interpreter, currentNamespace, qualifiers);

                absolute = true;
            }

            NormalizeName(ref qualifiers, ref tail);

            string qualifiedName = MakeQualifiedName(qualifiers, tail, false);

            if (absolute == IsAbsoluteName(qualifiedName))
                name = qualifiedName;
            else
                name = MakeAbsoluteName(qualifiedName);

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Name Parsing Support Methods
        /// <summary>
        /// This method splits the specified name into its qualifiers and tail
        /// portions.
        /// </summary>
        /// <param name="name">
        /// The name to split.
        /// </param>
        /// <param name="qualifiers">
        /// Upon success, this contains the qualifiers portion of the name.
        /// </param>
        /// <param name="tail">
        /// Upon success, this contains the tail portion of the name.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode SplitName(
            string name,
            ref string qualifiers,
            ref string tail
            )
        {
            NamespaceFlags flags = NamespaceFlags.None;
            Result error = null;

            return SplitName(
                name, ref qualifiers, ref tail, ref flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified name into its tail portion and
        /// computed namespace flags, discarding the qualifiers.
        /// </summary>
        /// <param name="name">
        /// The name to split.
        /// </param>
        /// <param name="tail">
        /// Upon success, this contains the tail portion of the name.
        /// </param>
        /// <param name="flags">
        /// Upon success, this contains the namespace flags computed from the
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode SplitName(
            string name,
            ref string tail,
            ref NamespaceFlags flags,
            ref Result error
            )
        {
            string qualifiers = null;

            return SplitName(
                name, ref qualifiers, ref tail, ref flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified name into its qualifiers and tail
        /// portions, reporting any failure via the error message.
        /// </summary>
        /// <param name="name">
        /// The name to split.
        /// </param>
        /// <param name="qualifiers">
        /// Upon success, this contains the qualifiers portion of the name.
        /// </param>
        /// <param name="tail">
        /// Upon success, this contains the tail portion of the name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SplitName(
            string name,
            ref string qualifiers,
            ref string tail,
            ref Result error
            )
        {
            NamespaceFlags flags = NamespaceFlags.None;

            return SplitName(
                name, ref qualifiers, ref tail, ref flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified name into its qualifiers and tail
        /// portions and computes the associated namespace flags (qualified,
        /// absolute, global, and wildcard).
        /// </summary>
        /// <param name="name">
        /// The name to split.
        /// </param>
        /// <param name="qualifiers">
        /// Upon success, this contains the qualifiers portion of the name.
        /// </param>
        /// <param name="tail">
        /// Upon success, this contains the tail portion of the name.
        /// </param>
        /// <param name="flags">
        /// On input, the existing namespace flags (the split-name flags are
        /// cleared); on output, this contains the namespace flags computed
        /// from the name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SplitName(
            string name,
            ref string qualifiers,
            ref string tail,
            ref NamespaceFlags flags,
            ref Result error
            )
        {
            flags &= ~NamespaceFlags.SplitNameMask;

            if (name == null)
            {
                error = "invalid name";
                return ReturnCode.Error;
            }

            string separator = TclVars.Namespace.Separator;

            int index = StringOps.LastIndexOf(
                name, separator, SharedStringOps.SystemComparisonType);

            if (index != Index.Invalid)
            {
                int index2 = index + separator.Length;

                while ((index >= 0) &&
                    (name[index] == Characters.Colon))
                {
                    index--;
                }

                qualifiers = (index >= 0) ?
                    name.Substring(0, index + 1) : String.Empty;

                tail = name.Substring(index2, name.Length - index2);

                if (qualifiers.Length > 0)
                    flags |= NamespaceFlags.Qualified;

                if (IsAbsoluteName(name))
                {
                    flags |= NamespaceFlags.Absolute;

                    if (qualifiers.Length == 0)
                        flags |= NamespaceFlags.Global;
                }

                if (StringOps.HasStringMatchChar(name))
                    flags |= NamespaceFlags.Wildcard;

                return ReturnCode.Ok;
            }

            qualifiers = String.Empty;
            tail = name;

            if (StringOps.HasStringMatchChar(name))
                flags |= NamespaceFlags.Wildcard;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified name into its individual component
        /// names on the namespace separator, removing empty entries and
        /// trimming colons from each component.
        /// </summary>
        /// <param name="name">
        /// The name to split.  This parameter may be null or empty.
        /// </param>
        /// <returns>
        /// An array of the component names, or an array containing a single
        /// empty string when the name is null or empty.
        /// </returns>
        private static string[] SplitName(
            string name
            )
        {
            if (String.IsNullOrEmpty(name))
                return EmptyName;

            string[] names = name.Split(Delimiters,
                StringSplitOptions.RemoveEmptyEntries);

            for (int index = 0; index < names.Length; index++)
                names[index] = TrimAll(names[index]);

            return names;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets only the tail portion of the specified name,
        /// returning the original name when it cannot be split.
        /// </summary>
        /// <param name="name">
        /// The name whose tail is requested.
        /// </param>
        /// <returns>
        /// The tail portion of the name, or the original name on failure.
        /// </returns>
        public static string TailOnly(
            string name
            )
        {
            string qualifiers = null; /* NOT USED */
            string tail = null;

            if (SplitName(name, ref qualifiers, ref tail) == ReturnCode.Ok)
                return tail;
            else
                return name;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Name Matching Support Methods
        /// <summary>
        /// This method determines whether two names are equal using the system
        /// string comparison.
        /// </summary>
        /// <param name="name1">
        /// The first name to compare.  This parameter may be null.
        /// </param>
        /// <param name="name2">
        /// The second name to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the names are equal; otherwise, false.
        /// </returns>
        public static bool IsSame(
            string name1,
            string name2
            )
        {
            return SharedStringOps.Equals(
                name1, name2, SharedStringOps.SystemComparisonType);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Name Typing Support Methods
        /// <summary>
        /// This method determines whether the specified name is an absolute
        /// name (i.e. it begins with the global namespace separator).
        /// </summary>
        /// <param name="name">
        /// The name to check.  This parameter may be null or empty.
        /// </param>
        /// <returns>
        /// True if the name is absolute; otherwise, false.
        /// </returns>
        public static bool IsAbsoluteName(
            string name
            )
        {
            if (String.IsNullOrEmpty(name))
                return false;

            return name.StartsWith(TclVars.Namespace.Global,
                SharedStringOps.SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified name refers to the
        /// global namespace (i.e. it is null/empty or consists only of
        /// colons).
        /// </summary>
        /// <param name="name">
        /// The name to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the name refers to the global namespace; otherwise, false.
        /// </returns>
        public static bool IsGlobalName(
            string name
            )
        {
            if (name == null)
                return false;

            if (name.Length == 0)
                return true;

            name = TrimAll(name);

            if (name.Length == 0)
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified name is qualified
        /// (i.e. it contains at least one namespace separator).
        /// </summary>
        /// <param name="name">
        /// The name to check.  This parameter may be null or empty.
        /// </param>
        /// <returns>
        /// True if the name is qualified; otherwise, false.
        /// </returns>
        public static bool IsQualifiedName(
            string name
            )
        {
            if (String.IsNullOrEmpty(name))
                return false;

            return name.IndexOf(TclVars.Namespace.Separator,
                SharedStringOps.SystemComparisonType) != Index.Invalid;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Name Mapping Support Methods
        /// <summary>
        /// This method creates the default namespace name mappings, which
        /// redirect the package namespace to the global namespace for backward
        /// compatibility with earlier Eagle beta releases.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The dictionary of default namespace name mappings.
        /// </returns>
        public static StringDictionary CreateMappings(
            Interpreter interpreter /* NOT USED */
            )
        {
            StringDictionary result = new StringDictionary();

            //
            // NOTE: By default, redirect the "::Eagle" namespace to the
            //       global one.  This is for backward compatibility with
            //       previous Eagle beta releases which did not have full
            //       namespace support.
            //
            result.Add(MakeAbsoluteName(
                GlobalState.GetPackageName()), TclVars.Namespace.Global);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the mapped namespace name for the specified name,
        /// returning null when there is no mapping.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespace mappings are queried.
        /// </param>
        /// <param name="name">
        /// The name to look up in the namespace mappings.
        /// </param>
        /// <returns>
        /// The mapped name, or null when there is no mapping.
        /// </returns>
        private static string GetMapping(
            Interpreter interpreter,
            string name
            )
        {
            string newName;
            Result error = null;

            newName = GetMapping(interpreter, name, ref error);

#if DEBUG && VERBOSE && false // NOTE: Noisy.
            if (newName == null)
                DebugOps.Complain(interpreter, ReturnCode.Error, error);
#endif

            return newName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the mapped namespace name for the specified name,
        /// reporting any failure via the error message.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespace mappings are queried.
        /// </param>
        /// <param name="name">
        /// The name to look up in the namespace mappings.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The mapped name, or null when there is no mapping or on failure.
        /// </returns>
        private static string GetMapping(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return null;
            }

            if (name == null)
            {
                error = "invalid name";
                return null;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (Interpreter.IsDeletedOrDisposed(
                        interpreter, false, ref error))
                {
                    return null;
                }

                StringDictionary mappings = interpreter.NamespaceMappings;

                if (mappings == null)
                {
                    error = "namespace mappings not available";
                    return null;
                }

                string newName;

                if (mappings.TryGetValue(name, out newName))
                {
                    if (newName == null)
                    {
                        error = String.Format(
                            "namespace mapping {0} is invalid",
                            FormatOps.WrapOrNull(name));
                    }

                    return newName;
                }
            }

            error = String.Format(
                "namespace mapping {0} not found",
                FormatOps.WrapOrNull(name));

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: The "Eagle" namespace redirects to the global namespace for
        //       reasons of backward compatibility with Eagle beta releases.
        //
        /// <summary>
        /// This method maps the specified namespace name through the
        /// interpreter namespace mappings, returning the original name when no
        /// mapping exists.  The package namespace redirects to the global
        /// namespace for backward compatibility.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespace mappings are queried.
        /// </param>
        /// <param name="name">
        /// The name to map.
        /// </param>
        /// <returns>
        /// The mapped name, or the original name when there is no mapping.
        /// </returns>
        public static string MapName(
            Interpreter interpreter,
            string name
            )
        {
            string newName = GetMapping(interpreter, name);

            if (newName != null)
                return newName;

            return name;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Name Lookup Support Methods
        /// <summary>
        /// This method gets the base namespace from which the specified name
        /// should be resolved, which is the global namespace for absolute
        /// names and the current namespace otherwise.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="name">
        /// The name being resolved, used to determine whether it is absolute.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to force use of the global namespace as the base.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The base namespace, or null on failure.
        /// </returns>
        private static INamespace GetBase(
            Interpreter interpreter,
            string name,
            bool absolute,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return null;
            }

            if (absolute || IsAbsoluteName(name))
            {
                INamespace globalNamespace = interpreter.GlobalNamespace;

                if (globalNamespace == null)
                    error = "invalid global namespace";

                return globalNamespace;
            }
            else
            {
                INamespace currentNamespace = null;

                if (interpreter.GetCurrentNamespaceViaResolvers(
                        null, LookupFlags.Default, ref currentNamespace,
                        ref error) == ReturnCode.Ok)
                {
                    if (currentNamespace == null)
                        error = "invalid current namespace";

                    return currentNamespace;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the descendant namespace identified by the
        /// specified name relative to the specified namespace, optionally
        /// creating any missing namespaces along the way.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that will own any namespaces created.  This
        /// parameter may be null.
        /// </param>
        /// <param name="namespace">
        /// The namespace from which the descendant lookup begins.
        /// </param>
        /// <param name="name">
        /// The relative name of the descendant namespace.
        /// </param>
        /// <param name="create">
        /// Non-zero to create any missing namespaces along the path.
        /// </param>
        /// <param name="deleted">
        /// Non-zero to allow namespaces that are pending deletion to be
        /// returned.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The descendant namespace, or null on failure.
        /// </returns>
        private static INamespace GetDescendant(
            Interpreter interpreter,
            INamespace @namespace,
            string name,
            bool create,
            bool deleted,
            ref Result error
            )
        {
            if (@namespace == null)
            {
                error = "cannot get descendant: invalid namespace";
                return null;
            }

#if false
            if (IsDisposed(@namespace))
            {
                error = "cannot get descendant: disposed namespace";
                return null;
            }
#endif

            if (!deleted && @namespace.Deleted)
            {
                error = "cannot get descendant: deleted namespace";
                return null;
            }

            if (String.IsNullOrEmpty(name))
            {
                if (create)
                {
                    error = String.Format(
                        "can't create namespace {0}: only global " +
                        "namespace can have empty name",
                        FormatOps.WrapOrNull(name));
                }
                else
                {
                    error = "cannot get descendant: invalid name";
                }

                return null;
            }

            INamespace childNamespace = @namespace;
            Result localError = null;

            foreach (string localName in SplitName(name))
            {
                if (String.IsNullOrEmpty(localName))
                    continue;

                INamespace parentNamespace = childNamespace;

                childNamespace = parentNamespace.GetChild(
                    localName, ref localError);

                if (childNamespace != null)
                {
#if false
                    if (IsDisposed(childNamespace))
                    {
                        localError = String.Format(
                            "namespace {0} in {1} is disposed",
                            FormatOps.WrapOrNull(localName),
                            FormatOps.WrapOrNull(
                                parentNamespace.QualifiedName));

                        childNamespace = null;
                        break;
                    }
#endif

                    if (!deleted && childNamespace.Deleted)
                    {
                        localError = String.Format(
                            "namespace {0} in {1} is deleted",
                            FormatOps.WrapOrNull(localName),
                            FormatOps.WrapOrNull(
                                parentNamespace.QualifiedName));

                        childNamespace = null;
                        break;
                    }
                }
                else
                {
                    if (create)
                    {
                        if ((interpreter != null) &&
                            !interpreter.CanAddNamespace(ref localError))
                        {
                            break;
                        }

                        bool success = false;
                        INamespace newNamespace = null;

                        try
                        {
                            newNamespace = Create(
                                localName, null, interpreter,
                                parentNamespace, null, null,
                                null, true);

                            if (newNamespace == null)
                            {
                                localError = String.Format(
                                    "creation of namespace {0} in {1} failed",
                                    FormatOps.WrapOrNull(localName),
                                    FormatOps.WrapOrNull(
                                        parentNamespace.QualifiedName));

                                break;
                            }

                            if (parentNamespace.AddChild(newNamespace,
                                    ref localError) != ReturnCode.Ok)
                            {
                                break;
                            }

                            if (interpreter != null)
                            {
                                /* IGNORED */
                                interpreter.TrackNamespaceAdded();
                            }

                            childNamespace = newNamespace;
                            success = true;
                        }
                        finally
                        {
                            if (!success && (newNamespace != null))
                                Dispose(interpreter, ref newNamespace);
                        }
                    }
                    else
                    {
                        if (IsAbsoluteName(name))
                        {
                            localError = String.Format(
                                "namespace {0} not found",
                                FormatOps.WrapOrNull(name));
                        }
                        else
                        {
                            localError = String.Format(
                                "namespace {0} not found in {1}",
                                FormatOps.WrapOrNull(name),
                                FormatOps.WrapOrNull(
                                    @namespace.QualifiedName));
                        }
                        break;
                    }
                }
            }

            if (childNamespace == null)
                error = localError;

            return childNamespace;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up the parent namespace of the namespace
        /// identified by the specified name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="name">
        /// The name whose parent namespace is requested.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat a request for the parent of the global namespace
        /// as an error.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to resolve the parent relative to the global namespace.
        /// </param>
        /// <param name="create">
        /// Non-zero to create any missing namespaces along the path.
        /// </param>
        /// <returns>
        /// The parent namespace, or null on failure.
        /// </returns>
        public static INamespace LookupParent(
            Interpreter interpreter,
            string name,
            bool strict,
            bool absolute,
            bool create
            )
        {
            Result error = null;

            return LookupParent(
                interpreter, name, strict, absolute, create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up the parent namespace of the namespace
        /// identified by the specified name, reporting any failure via the
        /// error message.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="name">
        /// The name whose parent namespace is requested.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat a request for the parent of the global namespace
        /// as an error.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to resolve the parent relative to the global namespace.
        /// </param>
        /// <param name="create">
        /// Non-zero to create any missing namespaces along the path.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The parent namespace, or null on failure.
        /// </returns>
        public static INamespace LookupParent(
            Interpreter interpreter,
            string name,
            bool strict,
            bool absolute,
            bool create,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(name))
            {
                error = "invalid name";
                return null;
            }

            string qualifiers = null;
            string tail = null;

            if (SplitName(
                    name, ref qualifiers, ref tail,
                    ref error) != ReturnCode.Ok)
            {
                return null;
            }

            if (strict && IsGlobalName(qualifiers))
            {
                error = "global namespace has no parent";
                return null;
            }

            if (absolute && (qualifiers != null))
            {
                qualifiers = TclVars.Namespace.Global + TrimLeading(
                    qualifiers);
            }

            return Lookup(
                interpreter, null, qualifiers, absolute, create, true,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up the namespace identified by the specified
        /// name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="name">
        /// The name of the namespace to look up.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to resolve the name relative to the global namespace.
        /// </param>
        /// <param name="create">
        /// Non-zero to create any missing namespaces along the path.
        /// </param>
        /// <returns>
        /// The namespace, or null on failure.
        /// </returns>
        public static INamespace Lookup(
            Interpreter interpreter,
            string name,
            bool absolute,
            bool create
            )
        {
            Result error = null;

            return Lookup(
                interpreter, null, name, absolute, create, true,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up the namespace identified by the specified
        /// name, reporting any failure via the error message.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="name">
        /// The name of the namespace to look up.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to resolve the name relative to the global namespace.
        /// </param>
        /// <param name="create">
        /// Non-zero to create any missing namespaces along the path.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The namespace, or null on failure.
        /// </returns>
        public static INamespace Lookup(
            Interpreter interpreter,
            string name,
            bool absolute,
            bool create,
            ref Result error
            )
        {
            return Lookup(
                interpreter, null, name, absolute, create, true,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up the namespace identified by the specified name
        /// relative to the specified namespace, determining the base namespace
        /// when none is supplied.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="namespace">
        /// The namespace from which the lookup begins.  When null, the base
        /// namespace is determined automatically.
        /// </param>
        /// <param name="name">
        /// The name of the namespace to look up.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to resolve the name relative to the global namespace.
        /// </param>
        /// <param name="create">
        /// Non-zero to create any missing namespaces along the path.
        /// </param>
        /// <param name="deleted">
        /// Non-zero to allow namespaces that are pending deletion to be
        /// returned.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The namespace, or null on failure.
        /// </returns>
        private static INamespace Lookup(
            Interpreter interpreter,
            INamespace @namespace,
            string name,
            bool absolute,
            bool create,
            bool deleted,
            ref Result error
            )
        {
            if (@namespace == null)
            {
                @namespace = GetBase(
                    interpreter, name, absolute, ref error);

                if (@namespace == null)
                    return null;
            }
            else if ((interpreter != null) &&
                (absolute || IsAbsoluteName(name)))
            {
                @namespace = interpreter.GlobalNamespace;

                if (@namespace == null)
                {
                    error = "invalid global namespace";
                    return null;
                }
            }

            if (IsGlobalName(name) && IsGlobal(interpreter, @namespace))
                return @namespace;

            return GetDescendant(
                interpreter, @namespace, name, create, deleted,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Variable Lookup Support Methods
        /// <summary>
        /// This method determines the call frame and adjusted variable name to
        /// use when accessing the specified variable, taking into account
        /// global, qualified, and namespace-scoped variable lookups.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces and call frames are queried.
        /// </param>
        /// <param name="frame">
        /// On input, the call frame supplied by the caller; on output, the
        /// call frame that should be used for the variable.
        /// </param>
        /// <param name="varName">
        /// On input, the variable name to resolve; on output, the adjusted
        /// variable name relative to the resolved call frame.
        /// </param>
        /// <param name="flags">
        /// The variable flags that influence how the call frame is selected.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetVariableFrame(
            Interpreter interpreter,
            ref ICallFrame frame,
            ref string varName,
            ref VariableFlags flags,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (FlagOps.HasFlags(flags, VariableFlags.GlobalOnly, true))
            {
                frame = interpreter.CurrentGlobalFrame;
                varName = TrimLeading(varName);

                return ReturnCode.Ok;
            }

            string qualifiers = null;
            string tail = null;
            NamespaceFlags namespaceFlags = NamespaceFlags.None;

            if ((varName != null) && (SplitName(varName,
                    ref qualifiers, ref tail, ref namespaceFlags,
                    ref error) != ReturnCode.Ok))
            {
                return ReturnCode.Error;
            }

            bool qualified = FlagOps.HasFlags(
                namespaceFlags, NamespaceFlags.Qualified, true);

            bool absolute = FlagOps.HasFlags(
                namespaceFlags, NamespaceFlags.Absolute, true);

            if (absolute)
            {
                if (qualified)
                {
                    INamespace @namespace = Lookup(
                        interpreter, qualifiers, false, false, ref error);

                    if (@namespace == null)
                        return ReturnCode.Error;

                    frame = @namespace.VariableFrame;
                    varName = tail;

                    return ReturnCode.Ok;
                }
                else
                {
                    frame = interpreter.CurrentGlobalFrame;
                    varName = tail;

                    return ReturnCode.Ok;
                }
            }
            else
            {
                INamespace currentNamespace = null;

                if (interpreter.GetCurrentNamespaceViaResolvers(
                        frame, LookupFlags.Default, ref currentNamespace,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (currentNamespace == null)
                {
                    error = "current namespace is invalid";
                    return ReturnCode.Error;
                }

                //
                // NOTE: If the variable name is qualified, then perform a
                //       relative lookup (i.e. because we know it is not
                //       an absolute name at this point) of its containing
                //       namespace and then use the call frame associated
                //       with it.
                //
                if (qualified)
                {
                    INamespace @namespace = GetDescendant(
                        interpreter, currentNamespace, qualifiers, false,
                        true, ref error);

                    if (@namespace == null)
                        return ReturnCode.Error;

                    frame = @namespace.VariableFrame;
                    varName = tail;

                    return ReturnCode.Ok;
                }

                //
                // NOTE: Check to see if the call frame specified by the
                //       caller supports variables.  If so, use it.
                //
                if (CallFrameOps.IsVariable(frame))
                {
                    //
                    // NOTE: The provided call frame already supports
                    //       variables, just use it.  Also, since we know
                    //       the variable name is not qualified by this
                    //       point, leave it unchanged.
                    //
                    return ReturnCode.Ok;
                }

                //
                // NOTE: At this point, grab the "legacy" variable call
                //       frame for the interpreter.
                //
                ICallFrame variableFrame = interpreter.GetVariableFrame(
                    frame, flags);

                //
                // NOTE: If the call frame is specially marked, use its
                //       current namespace to obtain the real call frame
                //       that should contain the variables.  This is the
                //       mechanism used by the [namespace eval] and
                //       [namespace inscope] sub-commands.
                //
                if (CallFrameOps.IsUseNamespace(variableFrame))
                {
                    INamespace @namespace = GetCurrent(interpreter,
                        variableFrame);

                    if ((@namespace != null) &&
                        !IsGlobal(interpreter, @namespace))
                    {
                        variableFrame = @namespace.VariableFrame;

                        if (CallFrameOps.IsVariable(variableFrame))
                        {
                            //
                            // NOTE: Ok, the call frame associated with
                            //       the namespace has variables, use it.
                            //
                            frame = variableFrame;
                            return ReturnCode.Ok;
                        }
                    }

                    //
                    // NOTE: Fallback to using the global call frame.
                    //
                    frame = interpreter.CurrentGlobalFrame;
                    return ReturnCode.Ok;
                }

                //
                // NOTE: Check if the variable call frame is within a
                //       procedure or scope call frame.  In that case,
                //       with a simple variable (i.e. non-qualified),
                //       we should use that call frame.
                //
                if (CallFrameOps.IsVariable(variableFrame) &&
                    !CallFrameOps.IsGlobal(variableFrame))
                {
                    frame = variableFrame;
                    return ReturnCode.Ok;
                }

                //
                // NOTE: If the current namespace is not the global one,
                //       make sure to use its variable call frame for
                //       non-qualified variable names.
                //
                if (!IsGlobal(interpreter, currentNamespace))
                {
                    frame = currentNamespace.VariableFrame;
                    return ReturnCode.Ok;
                }

                //
                // NOTE: If this point is reached, either the global call
                //       frame or a namespace call frame [that contains
                //       variables] will probably be used.
                //
                frame = variableFrame;
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For variable resolver use only.
        //
        /// <summary>
        /// This method looks up the namespace that contains the specified
        /// qualified variable name, returning the variable tail.  It is for
        /// variable resolver use only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="varName">
        /// The variable name to resolve.
        /// </param>
        /// <param name="tail">
        /// Upon success, this contains the tail (i.e. simple variable name)
        /// portion of the variable name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The containing namespace when the variable name is qualified;
        /// otherwise, null.
        /// </returns>
        public static INamespace Lookup(
            Interpreter interpreter,
            string varName,
            ref string tail,
            ref Result error
            )
        {
            string qualifiers = null;
            NamespaceFlags flags = NamespaceFlags.None;

            if (SplitName(
                    varName, ref qualifiers, ref tail, ref flags,
                    ref error) != ReturnCode.Ok)
            {
                return null;
            }

            INamespace @namespace = null;

            if (FlagOps.HasFlags(flags, NamespaceFlags.Qualified, true))
            {
                @namespace = Lookup(
                    interpreter, qualifiers, false, false, ref error);
            }

            return @namespace;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method is ONLY used by the _Resolvers.Namespace class
        //       in order to support variable lookup for when per-namespace
        //       resolvers are present.  This design may need changes at
        //       some point.
        //
        /// <summary>
        /// This method gets the namespace and custom resolver to use for the
        /// specified variable name.  It is used only by the namespace variable
        /// resolver to support per-namespace resolvers.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="frame">
        /// The call frame context.  This parameter is not used.
        /// </param>
        /// <param name="varName">
        /// The variable name to resolve.
        /// </param>
        /// <param name="resolve">
        /// Upon success, when the resolved namespace has a custom resolver,
        /// this is set to that resolver.
        /// </param>
        /// <param name="tail">
        /// Upon success, this contains the tail (i.e. simple variable name)
        /// portion of the variable name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The containing namespace, or null on failure.
        /// </returns>
        public static INamespace GetForVariable(
            Interpreter interpreter,
            ICallFrame frame, /* NOT USED */
            string varName,
            ref IResolve resolve,
            ref string tail,
            ref Result error
            )
        {
            INamespace @namespace;

            @namespace = Lookup(
                interpreter, varName, ref tail, ref error);

            if (@namespace != null)
            {
                IResolve localResolve = @namespace.Resolve;

                if (localResolve != null)
                    resolve = localResolve;
            }

            return @namespace;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute & IAlias Support Methods
        #region IExecute Support Methods
        /// <summary>
        /// This method sets the name to the absolute name of the specified
        /// executable entity, using the procedure or command name when
        /// available and otherwise using the supplied name.
        /// </summary>
        /// <param name="execute">
        /// The executable entity whose name is preferred.  This parameter may
        /// be null.
        /// </param>
        /// <param name="name">
        /// On input, the fallback name; on output, the absolute name of the
        /// executable entity.
        /// </param>
        private static void MaybeGetIExecuteName(
            IExecute execute,
            ref string name
            )
        {
            IProcedure procedure = execute as IProcedure;

            if (procedure != null)
            {
                name = MakeAbsoluteName(procedure.Name);
                return;
            }

            ICommand command = execute as ICommand;

            if (command != null)
            {
                name = MakeAbsoluteName(command.Name);
                return;
            }

            name = MakeAbsoluteName(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the specified name to an executable entity and
        /// returns its normalized, fully qualified name, optionally searching
        /// the hidden commands.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose executable entities are queried.
        /// </param>
        /// <param name="hidden">
        /// Non-zero to also search the hidden executable entities.
        /// </param>
        /// <param name="hiddenOnly">
        /// Non-zero to search only the hidden executable entities.
        /// </param>
        /// <param name="name">
        /// On input, the name to resolve; on output, the normalized, fully
        /// qualified name of the resolved entity.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetIExecuteName(
            Interpreter interpreter,
            bool hidden,
            bool hiddenOnly,
            ref string name,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (name == null)
            {
                error = "invalid name";
                return ReturnCode.Error;
            }

            ResultList errors = null;
            StringList names = new StringList(name);

            if (!IsAbsoluteName(name))
                names.Add(MakeAbsoluteName(name));

            foreach (string localName in names)
            {
                IExecute execute; /* REUSED */
                Result localError; /* REUSED */

                if (hidden)
                {
                    execute = null;
                    localError = null;

                    if (interpreter.InternalGetIExecuteViaResolvers(
                            interpreter.GetResolveEngineFlagsNoLock(true) |
                            EngineFlags.UseHidden, localName,
                            null, LookupFlags.Default, ref execute,
                            ref localError) == ReturnCode.Ok)
                    {
                        /* NO RESULT */
                        MaybeGetIExecuteName(execute, ref name);

                        return NormalizeName(
                            interpreter, ref name, ref error);
                    }
                    else if (localError != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }

                    if (hiddenOnly)
                        break;
                }

                execute = null;
                localError = null;

                if (interpreter.InternalGetIExecuteViaResolvers(
                        interpreter.GetResolveEngineFlagsNoLock(true),
                        localName, null, LookupFlags.Default,
                        ref execute, ref localError) == ReturnCode.Ok)
                {
                    /* NO RESULT */
                    MaybeGetIExecuteName(execute, ref name);

                    return NormalizeName(
                        interpreter, ref name, ref error);
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            if (errors != null)
                error = errors;

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method is ONLY used by the _Resolvers.Namespace class
        //       in order to support command lookup for when per-namespace
        //       resolvers are present.  This design may need changes at
        //       some point.
        //
        /// <summary>
        /// This method gets the namespace and custom resolver to use for the
        /// specified command name.  It is used only by the namespace resolver
        /// to support per-namespace command resolvers.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="frame">
        /// The call frame whose current namespace is used when the name is not
        /// qualified.
        /// </param>
        /// <param name="name">
        /// The command name to resolve.
        /// </param>
        /// <param name="resolve">
        /// Upon success, when the resolved namespace has a custom resolver,
        /// this is set to that resolver.
        /// </param>
        /// <param name="tail">
        /// Upon success, this contains the tail (i.e. simple command name)
        /// portion of the command name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The namespace to use for the command, or null when none is found.
        /// </returns>
        public static INamespace GetForIExecute(
            Interpreter interpreter,
            ICallFrame frame,
            string name,
            ref IResolve resolve,
            ref string tail,
            ref Result error
            )
        {
            INamespace @namespace;

            if (IsQualifiedName(name))
            {
                string qualifiers = null;
                string localTail = null;

                if (SplitName(
                        name, ref qualifiers, ref localTail,
                        ref error) == ReturnCode.Ok)
                {
                    @namespace = Lookup(
                        interpreter, qualifiers, false, false, ref error);

                    if (@namespace != null)
                    {
                        IResolve localResolve = @namespace.Resolve;

                        if (localResolve != null)
                        {
                            resolve = localResolve;
                            tail = localTail;

                            return @namespace;
                        }
                    }
                }
            }

            @namespace = GetCurrent(interpreter, frame);

            if (@namespace != null)
            {
                IResolve localResolve = @namespace.Resolve;

                if (localResolve != null)
                {
                    resolve = localResolve;
                    tail = name;
                }
            }

            return @namespace;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAlias Support Methods
        /// <summary>
        /// This method gets the target name (i.e. the first argument) of the
        /// specified alias.
        /// </summary>
        /// <param name="alias">
        /// The alias whose target name is requested.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The alias target name, or null when it is unavailable.
        /// </returns>
        public static string GetAliasName(
            IAlias alias
            )
        {
            if (alias == null)
                return null;

            ArgumentList arguments = alias.Arguments;

            if ((arguments == null) || (arguments.Count == 0))
                return null;

            return arguments[0];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the target name (i.e. the first argument) of the
        /// specified alias, creating the argument list when necessary.
        /// </summary>
        /// <param name="alias">
        /// The alias whose target name is set.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The target name to assign to the alias.
        /// </param>
        /// <returns>
        /// True if the target name was set; otherwise, false.
        /// </returns>
        public static bool SetAliasName(
            IAlias alias,
            string name
            )
        {
            if (alias == null)
                return false;

            ArgumentList arguments = alias.Arguments;

            if (arguments != null)
            {
                if (arguments.Count == 0)
                    arguments.Add(name);
                else
                    arguments[0] = name;
            }
            else
            {
                arguments = new ArgumentList(name);
                alias.Arguments = arguments;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified executable entity is
        /// an alias whose target name matches the specified name.
        /// </summary>
        /// <param name="execute">
        /// The executable entity to check.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name to compare against the alias target name.
        /// </param>
        /// <returns>
        /// True if the entity is an alias whose target name matches the
        /// specified name; otherwise, false.
        /// </returns>
        private static bool IsSameAsAliasName(
            IExecute execute,
            string name
            )
        {
            IAlias alias = GetAliasFromIExecute(execute);

            if (alias == null)
                return false;

            return SharedStringOps.SystemEquals(GetAliasName(alias), name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the alias from the specified executable
        /// entity, unwrapping it when the entity is a wrapper around an alias.
        /// </summary>
        /// <param name="execute">
        /// The executable entity from which to extract the alias.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The alias, or null when the entity is not (and does not wrap) an
        /// alias.
        /// </returns>
        private static IAlias GetAliasFromIExecute(
            IExecute execute
            )
        {
            if (execute is IAlias)
                return (IAlias)execute;

            if (execute is IWrapper)
            {
                object @object = ((IWrapper)execute).Object;

                if (@object is IAlias)
                    return (IAlias)@object;
            }

            return null;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Enable & Disable Support Methods
        /// <summary>
        /// This method determines whether the commands required to enable or
        /// disable namespace support are available in the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose commands are queried.
        /// </param>
        /// <param name="enable">
        /// Non-zero to check for the commands required to enable namespace
        /// support; zero to check for those required to disable it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the required commands are available; otherwise, false.
        /// </returns>
        public static bool HaveRequiredCommands(
            Interpreter interpreter,
            bool enable,
            ref Result error
            )
        {
            IPluginData pluginData = null;
            ICommandData commandData = null;
            Type oldType = null;
            Type newType = null;

            if (GetEntityData(
                    interpreter, enable, ref pluginData, ref commandData,
                    ref oldType, ref newType, ref error) == ReturnCode.Ok)
            {
                return (commandData != null);
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the core plugin data, command data, and command
        /// types needed to switch between the enabled and disabled
        /// implementations of the [namespace] command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose core plugin and commands are queried.
        /// </param>
        /// <param name="enable">
        /// Non-zero to obtain the data for enabling namespace support; zero for
        /// disabling it.
        /// </param>
        /// <param name="pluginData">
        /// Upon success, this contains the core plugin data.
        /// </param>
        /// <param name="commandData">
        /// Upon success, this contains the command data for the new command
        /// implementation.
        /// </param>
        /// <param name="oldType">
        /// Upon success, this contains the type of the old command
        /// implementation.
        /// </param>
        /// <param name="newType">
        /// Upon success, this contains the type of the new command
        /// implementation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetEntityData(
            Interpreter interpreter,      /* in */
            bool enable,                  /* in */
            ref IPluginData pluginData,   /* out */
            ref ICommandData commandData, /* out */
            ref Type oldType,             /* out */
            ref Type newType,             /* out */
            ref Result error              /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            pluginData = interpreter.GetCorePlugin(ref error);

            if (pluginData == null)
                return ReturnCode.Error;

            oldType = enable ?
                typeof(_Commands.Namespace1) : typeof(_Commands.Namespace2);

            newType = enable ?
                typeof(_Commands.Namespace2) : typeof(_Commands.Namespace1);

            commandData = RuntimeOps.FindCommandData(pluginData, newType);

            if (commandData == null)
            {
                error = "could not find new command data";
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method changes the [namespace] command implementation in the
        /// specified interpreter, removing the old command and adding the new
        /// one appropriate for the requested enable state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose [namespace] command is changed.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the command operations.  This
        /// parameter may be null.
        /// </param>
        /// <param name="enable">
        /// Non-zero to install the enabled implementation; zero to install the
        /// disabled implementation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode ChangeCommand(
            Interpreter interpreter,
            IClientData clientData,
            bool enable,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            IPluginData pluginData = null;
            ICommandData newCommandData = null;
            Type oldType = null;
            Type newType = null;

            if (GetEntityData(
                    interpreter, enable, ref pluginData, ref newCommandData,
                    ref oldType, ref newType, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // HACK: We know that the [namespace] commands are not within
            //       an isolated plugin because the core plugin is never
            //       isolated.
            //
            ICommand command = enable ?
                (ICommand)new _Commands.Namespace2(newCommandData) :
                (ICommand)new _Commands.Namespace1(newCommandData);

            string oldName = AttributeOps.GetObjectName(oldType);

            if (oldName == null)
                oldName = ScriptOps.TypeNameToEntityName(oldType);

            string newName = AttributeOps.GetObjectName(newType);

            if (newName == null)
                newName = ScriptOps.TypeNameToEntityName(newType);

            ReturnCode removeCode;
            Result removeError = null;

            removeCode = interpreter.RemoveCommand(
                oldName, clientData, ref removeError);

            if (removeCode == ReturnCode.Ok)
            {
                ICommandData oldCommandData = RuntimeOps.FindCommandData(
                    pluginData, oldType);

                if (oldCommandData != null)
                {
                    EntityOps.SetToken(oldCommandData, 0 /* REMOVED */);
                }
                else
                {
                    DebugOps.Complain(interpreter,
                        ReturnCode.Error, "could not find old command data");
                }
            }
            else
            {
                DebugOps.Complain(interpreter, removeCode, removeError);
            }

            long newToken = 0;

            if (interpreter.AddCommand(
                    command, clientData, ref newToken,
                    ref error) == ReturnCode.Ok)
            {
                EntityOps.SetToken(newCommandData, newToken);

                return ReturnCode.Ok;
            }
            else
            {
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Scope Support Methods
        //
        // WARNING: This method assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method attaches the unmarked variables of the specified scope
        /// call frame to the specified namespace, marking them and moving them
        /// into the namespace call frame.  The interpreter lock must already be
        /// held by the caller.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns the namespace and scope.
        /// </param>
        /// <param name="namespace">
        /// The namespace to which the scope variables are attached.
        /// </param>
        /// <param name="scopeFrame">
        /// The scope call frame whose variables are attached.
        /// </param>
        /// <param name="list">
        /// Upon return, this contains the names of the variables that were
        /// attached; it is created if necessary.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode AttachScope(
            Interpreter interpreter, /* in */
            INamespace @namespace,   /* in */
            ICallFrame scopeFrame,   /* in */
            ref StringList list,     /* in, out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (@namespace == null)
            {
                error = "invalid namespace";
                return ReturnCode.Error;
            }

            ICallFrame namespaceFrame;

            if (IsGlobal(interpreter, @namespace))
                namespaceFrame = interpreter.CurrentGlobalFrame;
            else
                namespaceFrame = @namespace.VariableFrame;

            if (namespaceFrame == null)
            {
                error = "namespace has invalid call frame";
                return ReturnCode.Error;
            }

            VariableDictionary namespaceVariables = namespaceFrame.Variables;

            if (namespaceVariables == null)
            {
                error = "namespace call frame does not support variables";
                return ReturnCode.Error;
            }

            if (scopeFrame == null)
            {
                error = "invalid scope call frame";
                return ReturnCode.Error;
            }

            if (!CallFrameOps.IsScope(scopeFrame))
            {
                error = "scope call frame must be scope";
                return ReturnCode.Error;
            }

            VariableDictionary scopeVariables = scopeFrame.Variables;

            if (scopeVariables == null)
            {
                error = "scope call frame does not support variables";
                return ReturnCode.Error;
            }

            foreach (VariablePair pair in scopeVariables)
            {
                IVariable scopeVariable = pair.Value;

                if (scopeVariable == null)
                    continue;

                if (scopeVariable.HasNamespaceMark(null))
                    continue;

                string varName = pair.Key;

                if (varName == null)
                    continue;

                if (namespaceVariables.ContainsKey(varName))
                    continue;

                if (list == null)
                    list = new StringList();

                /* NO RESULT */
                list.Add(varName);

                /* IGNORED */
                scopeVariable.SetNamespaceMark(@namespace);

                /* IGNORED */
                scopeVariable.SetFrameMark(scopeFrame);

                /* NO RESULT */
                namespaceVariables.Add(varName, scopeVariable);

                /* NO RESULT */
                scopeVariable.ResetFrame(namespaceFrame, interpreter);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method detaches the variables of the specified scope call frame
        /// that are marked for the specified namespace, removing the marks and
        /// removing them from the namespace call frame.  The interpreter lock
        /// must already be held by the caller.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns the namespace and scope.
        /// </param>
        /// <param name="namespace">
        /// The namespace from which the scope variables are detached.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="scopeFrame">
        /// The scope call frame whose variables are detached.
        /// </param>
        /// <param name="list">
        /// Upon return, this contains the names of the variables that were
        /// detached; it is created if necessary.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode DetachScope(
            Interpreter interpreter, /* in */
            INamespace @namespace,   /* in: OPTIONAL */
            ICallFrame scopeFrame,   /* in */
            ref StringList list,     /* in, out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            VariableDictionary namespaceVariables = null;

            if (@namespace != null)
            {
                ICallFrame namespaceFrame;

                if (IsGlobal(interpreter, @namespace))
                    namespaceFrame = interpreter.CurrentGlobalFrame;
                else
                    namespaceFrame = @namespace.VariableFrame;

                if (namespaceFrame == null)
                {
                    error = "namespace has invalid call frame";
                    return ReturnCode.Error;
                }

                namespaceVariables = namespaceFrame.Variables;

                if (namespaceVariables == null)
                {
                    error = "namespace call frame does not support variables";
                    return ReturnCode.Error;
                }
            }

            if (scopeFrame == null)
            {
                error = "invalid scope call frame";
                return ReturnCode.Error;
            }

            if (!CallFrameOps.IsScope(scopeFrame))
            {
                error = "scope call frame must be scope";
                return ReturnCode.Error;
            }

            VariableDictionary scopeVariables = scopeFrame.Variables;

            if (scopeVariables == null)
            {
                error = "scope call frame does not support variables";
                return ReturnCode.Error;
            }

            foreach (VariablePair pair in scopeVariables)
            {
                IVariable scopeVariable = pair.Value;

                if (scopeVariable == null)
                    continue;

                if (!scopeVariable.HasNamespaceMark(@namespace))
                    continue;

                string varName = pair.Key;

                if (varName == null)
                    continue;

                if (namespaceVariables != null)
                {
                    IVariable namespaceVariable;

                    if (!namespaceVariables.TryGetValue(
                            varName, out namespaceVariable))
                    {
                        continue;
                    }

                    if (!Object.ReferenceEquals(
                            scopeVariable, namespaceVariable))
                    {
                        continue;
                    }
                }

                if (list == null)
                    list = new StringList();

                /* NO RESULT */
                list.Add(varName);

                /* IGNORED */
                scopeVariable.UnsetFrameMark();

                /* IGNORED */
                scopeVariable.UnsetNamespaceMark();

                if (namespaceVariables != null)
                {
                    /* IGNORED */
                    namespaceVariables.Remove(varName);

                    /* NO RESULT */
                    scopeVariable.ResetFrame(scopeFrame, interpreter);
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method exports eligible variables from the specified scope call
        /// frame into the specified namespace, moving them from the scope to the
        /// namespace call frame.  The interpreter lock must already be held by
        /// the caller.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns the namespace and scope.
        /// </param>
        /// <param name="namespace">
        /// The namespace into which the scope variables are exported.
        /// </param>
        /// <param name="scopeFrame">
        /// The scope call frame whose variables are exported.
        /// </param>
        /// <param name="system">
        /// Non-zero to also export variables marked as system variables.
        /// </param>
        /// <param name="list">
        /// Upon return, this contains the names of the variables that were
        /// exported; it is created if necessary.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ExportScope(
            Interpreter interpreter, /* in */
            INamespace @namespace,   /* in */
            ICallFrame scopeFrame,   /* in */
            bool system,             /* in */
            ref StringList list,     /* in, out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (@namespace == null)
            {
                error = "invalid namespace";
                return ReturnCode.Error;
            }

            ICallFrame namespaceFrame;

            if (IsGlobal(interpreter, @namespace))
                namespaceFrame = interpreter.CurrentGlobalFrame;
            else
                namespaceFrame = @namespace.VariableFrame;

            if (namespaceFrame == null)
            {
                error = "namespace has invalid call frame";
                return ReturnCode.Error;
            }

            VariableDictionary namespaceVariables = namespaceFrame.Variables;

            if (namespaceVariables == null)
            {
                error = "namespace call frame does not support variables";
                return ReturnCode.Error;
            }

            if (scopeFrame == null)
            {
                error = "invalid scope call frame";
                return ReturnCode.Error;
            }

            if (!CallFrameOps.IsScope(scopeFrame))
            {
                error = "scope call frame must be scope";
                return ReturnCode.Error;
            }

            VariableDictionary scopeVariables = scopeFrame.Variables;

            if (scopeVariables == null)
            {
                error = "scope call frame does not support variables";
                return ReturnCode.Error;
            }

            VariableDictionary localVariables = new VariableDictionary(
                scopeVariables);

            foreach (VariablePair pair in localVariables)
            {
                IVariable scopeVariable = pair.Value;

                if ((scopeVariable == null) ||
                    EntityOps.IsReadOnlyOrInvariant(scopeVariable))
                {
                    continue;
                }

                if (!system && EntityOps.IsSystem(scopeVariable))
                    continue;

                string varName = pair.Key;

                if (varName == null)
                    continue;

                IVariable namespaceVariable;

                if (namespaceVariables.TryGetValue(
                        varName, out namespaceVariable) &&
                    ((namespaceVariable == null) ||
                    !EntityOps.IsUndefined(namespaceVariable)))
                {
                    continue;
                }

                if (list == null)
                    list = new StringList();

                /* NO RESULT */
                list.Add(varName);

                /* IGNORED */
                scopeVariable.UnsetNamespaceMark();

                /* IGNORED */
                scopeVariable.UnsetFrameMark();

                /* NO RESULT */
                namespaceVariables[varName] = scopeVariable;

                /* IGNORED */
                scopeVariables.Remove(varName);

                /* NO RESULT */
                scopeVariable.ResetFrame(namespaceFrame, interpreter);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method imports eligible variables from the specified namespace
        /// into the specified scope call frame, moving them from the namespace
        /// to the scope call frame.  The interpreter lock must already be held
        /// by the caller.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns the namespace and scope.
        /// </param>
        /// <param name="namespace">
        /// The namespace from which the variables are imported.
        /// </param>
        /// <param name="scopeFrame">
        /// The scope call frame into which the variables are imported.
        /// </param>
        /// <param name="system">
        /// Non-zero to also import variables marked as system variables.
        /// </param>
        /// <param name="list">
        /// Upon return, this contains the names of the variables that were
        /// imported; it is created if necessary.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ImportScope(
            Interpreter interpreter, /* in */
            INamespace @namespace,   /* in */
            ICallFrame scopeFrame,   /* in */
            bool system,             /* in */
            ref StringList list,     /* in, out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (@namespace == null)
            {
                error = "invalid namespace";
                return ReturnCode.Error;
            }

            ICallFrame namespaceFrame;

            if (IsGlobal(interpreter, @namespace))
                namespaceFrame = interpreter.CurrentGlobalFrame;
            else
                namespaceFrame = @namespace.VariableFrame;

            if (namespaceFrame == null)
            {
                error = "namespace has invalid call frame";
                return ReturnCode.Error;
            }

            VariableDictionary namespaceVariables = namespaceFrame.Variables;

            if (namespaceVariables == null)
            {
                error = "namespace call frame does not support variables";
                return ReturnCode.Error;
            }

            if (scopeFrame == null)
            {
                error = "invalid scope call frame";
                return ReturnCode.Error;
            }

            if (!CallFrameOps.IsScope(scopeFrame))
            {
                error = "scope call frame must be scope";
                return ReturnCode.Error;
            }

            VariableDictionary scopeVariables = scopeFrame.Variables;

            if (scopeVariables == null)
            {
                error = "scope call frame does not support variables";
                return ReturnCode.Error;
            }

            VariableDictionary localVariables = new VariableDictionary(
                namespaceVariables);

            foreach (VariablePair pair in localVariables)
            {
                IVariable namespaceVariable = pair.Value;

                if ((namespaceVariable == null) ||
                    EntityOps.IsReadOnlyOrInvariant(namespaceVariable))
                {
                    continue;
                }

                if (!system && EntityOps.IsSystem(namespaceVariable))
                    continue;

                string varName = pair.Key;

                if (varName == null)
                    continue;

                IVariable scopeVariable;

                if (scopeVariables.TryGetValue(
                        varName, out scopeVariable) &&
                    ((scopeVariable == null) ||
                    !EntityOps.IsUndefined(scopeVariable)))
                {
                    continue;
                }

                if (list == null)
                    list = new StringList();

                /* NO RESULT */
                list.Add(varName);

                /* IGNORED */
                namespaceVariable.UnsetNamespaceMark();

                /* IGNORED */
                namespaceVariable.UnsetFrameMark();

                /* NO RESULT */
                scopeVariables[varName] = namespaceVariable;

                /* IGNORED */
                namespaceVariables.Remove(varName);

                /* NO RESULT */
                namespaceVariable.ResetFrame(scopeFrame, interpreter);
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Sub-Command Support Methods
        /// <summary>
        /// This method gets the immediate child namespaces of the namespace
        /// identified by the specified name (or the current namespace when no
        /// name is given) that match the specified pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="name">
        /// The name of the parent namespace.  When null, the current namespace
        /// is used.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter the child namespaces.  This parameter may
        /// be null.
        /// </param>
        /// <param name="deleted">
        /// Non-zero to include namespaces that are pending deletion.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The matching child namespaces, or null on failure.
        /// </returns>
        public static IEnumerable<INamespace> Children(
            Interpreter interpreter,
            string name,
            string pattern,
            bool deleted,
            ref Result error
            )
        {
            if (name == null)
            {
                INamespace currentNamespace = null;

                if ((interpreter.GetCurrentNamespaceViaResolvers(
                        null, LookupFlags.Default, ref currentNamespace,
                        ref error) == ReturnCode.Ok))
                {
                    if (currentNamespace == null)
                    {
                        error = "current namespace is invalid";
                        return null;
                    }

                    name = currentNamespace.QualifiedName;
                }
                else
                {
                    return null;
                }
            }

            INamespace @namespace = Lookup(
                interpreter, name, false, false, ref error);

            if (@namespace == null)
                return null;

            return @namespace.GetChildren(pattern, deleted);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets all descendant namespaces of the namespace
        /// identified by the specified name (or the current namespace when no
        /// name is given) that match the specified pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="name">
        /// The name of the ancestor namespace.  When null, the current
        /// namespace is used.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter the descendant namespaces.  This
        /// parameter may be null.
        /// </param>
        /// <param name="deleted">
        /// Non-zero to include namespaces that are pending deletion.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The matching descendant namespaces, or null on failure.
        /// </returns>
        public static IEnumerable<INamespace> Descendants(
            Interpreter interpreter,
            string name,
            string pattern,
            bool deleted,
            ref Result error
            )
        {
            if (name == null)
            {
                INamespace currentNamespace = null;

                if ((interpreter.GetCurrentNamespaceViaResolvers(
                        null, LookupFlags.Default, ref currentNamespace,
                        ref error) == ReturnCode.Ok))
                {
                    if (currentNamespace == null)
                    {
                        error = "current namespace is invalid";
                        return null;
                    }

                    name = currentNamespace.QualifiedName;
                }
                else
                {
                    return null;
                }
            }

            INamespace @namespace = Lookup(
                interpreter, name, false, false, ref error);

            if (@namespace == null)
                return null;

            return @namespace.GetDescendants(pattern, deleted);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables namespace support in the specified
        /// interpreter, switching the [namespace] command implementation and
        /// resetting the related namespace and resolver state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespace support is changed.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the command change.  This
        /// parameter may be null.
        /// </param>
        /// <param name="enable">
        /// Non-zero to enable namespace support; zero to disable it.
        /// </param>
        /// <param name="force">
        /// Non-zero to reset the script-visible namespace state even when the
        /// enable state is unchanged.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Enable(
            Interpreter interpreter,
            IClientData clientData,
            bool enable,
            bool force,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: Before doing anything else, determine if namespaces are
            //       currently enabled or disabled.
            //
            bool useNamespaces = interpreter.InternalAreNamespacesEnabled();

            //
            // NOTE: Attempt to set the [namespace] command to point to either
            //       the [namespace1] command implementation (when disabled)
            //       or [namespace2] command implementation (when enabled).
            //
            if (ChangeCommand(interpreter,
                    clientData, enable, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: Reset the internal namespace-related state in the target
            //       interpreter.  None of this state is directly visible to
            //       the script level, nor is it extensible by third parties.
            //       Furthermore, in the (common?) case, no changes will be
            //       made except for the interpreter creation flags (i.e. due
            //       to the global namespace and namespace pending deletion
            //       list already having been created at some point earlier,
            //       such as interpreter creation).
            //
            /* NO RESULT */
            interpreter.PreSetupNamespaces(enable, false, true);

            //
            // NOTE: When resetting things that are visible from the script
            //       level, only do so when necessary unless forced by the
            //       caller.
            //
            if (force || (useNamespaces != enable))
            {
                /* NO RESULT */
                interpreter.InternalResetResolvers(ref error);

                //
                // NOTE: Figure out which namespace to use.  When namespaces
                //       are being enabled, always use the global namespace;
                //       otherwise, null out the namespace, which will also
                //       end up using the global namespace (which is a very
                //       subtle, but nonetheless important distinction).
                //
                INamespace @namespace = enable ?
                    interpreter.GlobalNamespace : null;

                /* IGNORED */
                SetCurrent(
                    interpreter, interpreter.CurrentGlobalFrame, @namespace);

                /* IGNORED */
                SetCurrentForAll(interpreter, null, @namespace);
            }

#if EXECUTE_CACHE
            /* IGNORED */
            interpreter.ClearExecuteCache();
#endif

#if ARGUMENT_CACHE
            /* IGNORED */
            interpreter.MaybeClearArgumentCache(null);
#endif

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method updates the export name patterns of the specified
        /// namespace (or the current namespace), optionally clearing the
        /// existing patterns first, and returns the current patterns when none
        /// are supplied.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespace is queried.
        /// </param>
        /// <param name="namespace">
        /// The namespace whose export patterns are updated.  When null, the
        /// current namespace is used.
        /// </param>
        /// <param name="patterns">
        /// The simple export patterns to add.  When empty, the current export
        /// patterns are returned.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the existing export patterns before adding.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the current export patterns (when none
        /// were supplied) or an empty string; upon failure, this contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Export(
            Interpreter interpreter, /* NOTE: Namespace queried here. */
            INamespace @namespace,   /* NOTE: Used for simple patterns. */
            StringList patterns,     /* NOTE: Simple patterns only. */
            bool clear,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (@namespace == null)
            {
                INamespace currentNamespace = null;

                if (interpreter.GetCurrentNamespaceViaResolvers(
                        null, LookupFlags.Default, ref currentNamespace,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (currentNamespace == null)
                {
                    result = "current namespace is invalid";
                    return ReturnCode.Error;
                }

                @namespace = currentNamespace;
            }

            if (patterns == null)
            {
                result = "invalid export pattern list";
                return ReturnCode.Error;
            }

            StringDictionary exportNames = @namespace.ExportNames;

            if (exportNames == null)
            {
                result = "invalid namespace export list";
                return ReturnCode.Error;
            }

            if (clear)
                exportNames.Clear();

            if (patterns.Count == 0)
            {
                result = new StringList(exportNames.Keys);
                return ReturnCode.Ok;
            }

            foreach (string pattern in patterns)
            {
                if (pattern == null)
                    continue;

                string tail = null;
                NamespaceFlags flags = NamespaceFlags.None;

                if (SplitName(
                        pattern, ref tail, ref flags,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (FlagOps.HasFlags(flags, NamespaceFlags.Absolute, true) ||
                    FlagOps.HasFlags(flags, NamespaceFlags.Qualified, true))
                {
                    result = String.Format(
                        "invalid export pattern {0}: pattern " +
                        "can't specify a namespace",
                        FormatOps.WrapOrNull(pattern));

                    return ReturnCode.Error;
                }

                exportNames[tail] = null;
            }

            result = String.Empty;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes (forgets) the imported commands in the current
        /// namespace that match the specified patterns.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose imported command aliases are removed.
        /// </param>
        /// <param name="patterns">
        /// The simple or qualified patterns identifying the imports to forget.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Forget(
            Interpreter interpreter, /* NOTE: Aliases removed here. */
            StringList patterns,     /* NOTE: Simple/qualified patterns. */
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (patterns == null)
            {
                error = "invalid forget pattern list";
                return ReturnCode.Error;
            }

            INamespace currentNamespace = null;

            if (interpreter.GetCurrentNamespaceViaResolvers(
                    null, LookupFlags.Default, ref currentNamespace,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (currentNamespace == null)
            {
                error = "current namespace is invalid";
                return ReturnCode.Error;
            }

            foreach (string pattern in patterns)
            {
                string qualifiers = null;
                string tail = null;
                NamespaceFlags flags = NamespaceFlags.None;

                if (SplitName(
                        pattern, ref qualifiers, ref tail, ref flags,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                string qualifiedPattern;

                if (FlagOps.HasFlags(flags, NamespaceFlags.Qualified, true))
                {
                    INamespace @namespace = Lookup(
                        interpreter, qualifiers, false, false);

                    if (@namespace == null)
                    {
                        error = String.Format(
                            "unknown namespace in namespace forget pattern " +
                            "{0}", FormatOps.WrapOrNull(pattern));

                        return ReturnCode.Error;
                    }

                    qualifiedPattern = MakeQualifiedPattern(
                        interpreter, @namespace, tail, false);
                }
                else
                {
                    qualifiedPattern = MakeQualifiedName(
                        Characters.Asterisk.ToString(), tail, false);
                }

                if (currentNamespace.RemoveImports(
                        qualifiedPattern, false, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method imports the exported commands matching the specified
        /// qualified patterns into the current namespace as command aliases,
        /// returning the current imports when no patterns are supplied.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter into which the command aliases are added.
        /// </param>
        /// <param name="patterns">
        /// The qualified patterns identifying the commands to import.  When
        /// empty, the current import names are returned.
        /// </param>
        /// <param name="force">
        /// Non-zero to overwrite any existing commands that conflict with the
        /// imported names.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the current import names (when none were
        /// supplied) or an empty string; upon failure, this contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Import(
            Interpreter interpreter, /* NOTE: Aliases added here. */
            StringList patterns,     /* NOTE: Qualified patterns only. */
            bool force,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (patterns == null)
            {
                result = "invalid export pattern list";
                return ReturnCode.Error;
            }

            INamespace currentNamespace = null;

            if (interpreter.GetCurrentNamespaceViaResolvers(
                    null, LookupFlags.Default, ref currentNamespace,
                    ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (currentNamespace == null)
            {
                result = "current namespace is invalid";
                return ReturnCode.Error;
            }

            if (patterns.Count == 0)
            {
                result = currentNamespace.GetImportNames(null, true, true);
                return ReturnCode.Ok;
            }

            foreach (string pattern in patterns)
            {
                if (pattern == null)
                    continue;

                if (pattern.Length == 0)
                {
                    result = "empty import pattern";
                    return ReturnCode.Error;
                }

                string qualifiers = null;
                string tail = null;
                NamespaceFlags flags = NamespaceFlags.None;

                if (SplitName(
                        pattern, ref qualifiers, ref tail,
                        ref flags, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (!FlagOps.HasFlags(flags, NamespaceFlags.Qualified, true))
                {
                    result = "import patterns must be qualified";
                    return ReturnCode.Error;
                }

                INamespace @namespace = Lookup(
                    interpreter, qualifiers, false, false);

                if (@namespace == null)
                {
                    result = String.Format(
                        "unknown namespace in import pattern {0}",
                        FormatOps.WrapOrNull(pattern));

                    return ReturnCode.Error;
                }

                if (IsSame(@namespace, currentNamespace))
                {
                    result = String.Format(
                        "import pattern {0} tries to import " +
                        "from namespace {1} into itself",
                        FormatOps.WrapOrNull(pattern),
                        FormatOps.WrapOrNull(@namespace));

                    return ReturnCode.Error;
                }

                StringList exportNames = @namespace.GetExportNames(tail);

                if (exportNames == null)
                {
                    result = "invalid namespace export names list";
                    return ReturnCode.Error;
                }

                foreach (string exportName in exportNames)
                {
                    string qualifiedPattern = MakeQualifiedPattern(
                        interpreter, @namespace, exportName, false);

                    ObjectDictionary dictionary = null;

                    if (interpreter.ListAnyIExecute(
                            qualifiedPattern, false, false, false,
                            ref dictionary, ref result) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    if (dictionary == null)
                        continue;

                    foreach (KeyValuePair<string, object> pair in dictionary)
                    {
                        IExecute execute = pair.Value as IExecute;

                        if (execute == null)
                            continue;

                        string nameTailOnly = TailOnly(pair.Key);

                        string qualifiedExportName = MakeQualifiedName(
                            interpreter, @namespace, nameTailOnly);

                        string qualifiedImportName = MakeQualifiedName(
                            interpreter, currentNamespace, nameTailOnly);

                        if (interpreter.DoesIExecuteExist(
                                qualifiedImportName) == ReturnCode.Ok)
                        {
                            if (!force)
                            {
                                result = String.Format(
                                    "can't import command {0}: already exists",
                                    FormatOps.WrapOrNull(nameTailOnly));

                                return ReturnCode.Error;
                            }

                            if (currentNamespace.RemoveImport(
                                    qualifiedImportName, false,
                                    ref result) == ReturnCode.Ok)
                            {
                                if (interpreter.RemoveIExecute(
                                        qualifiedImportName, null,
                                        ref result) != ReturnCode.Ok)
                                {
                                    return ReturnCode.Error;
                                }
                            }
                            else
                            {
                                return ReturnCode.Error;
                            }
                        }

                        if (interpreter.DoesProcedureExist(
                                qualifiedImportName) == ReturnCode.Ok)
                        {
                            if (!force)
                            {
                                result = String.Format(
                                    "can't import command {0}: already exists",
                                    FormatOps.WrapOrNull(nameTailOnly));

                                return ReturnCode.Error;
                            }

                            if (currentNamespace.RemoveImport(
                                    qualifiedImportName, false,
                                    ref result) == ReturnCode.Ok)
                            {
                                if (interpreter.RemoveProcedure(
                                        qualifiedImportName, null,
                                        ref result) != ReturnCode.Ok)
                                {
                                    return ReturnCode.Error;
                                }
                            }
                            else
                            {
                                return ReturnCode.Error;
                            }
                        }

                        ICommand command = null;

                        if (interpreter.GetCommand(
                                qualifiedImportName, LookupFlags.Exists,
                                ref command) == ReturnCode.Ok)
                        {
                            //
                            // NOTE: If the command we just found is the same
                            //       one being imported, just skip it.
                            //
                            if (IsSameAsAliasName(command, qualifiedExportName))
                                continue;

                            if (!force)
                            {
                                result = String.Format(
                                    "can't import command {0}: already exists",
                                    FormatOps.WrapOrNull(nameTailOnly));

                                return ReturnCode.Error;
                            }

                            if (currentNamespace.RemoveImport(
                                    qualifiedImportName, false,
                                    ref result) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: Add a command alias to the list of imports for
                        //       the target namespace.
                        //
                        if (currentNamespace.AddImport(
                                @namespace, qualifiedImportName,
                                qualifiedExportName,
                                ref result) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }
                    }
                }
            }

            result = String.Empty;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method collects detailed diagnostic information about how the
        /// specified name is resolved (as a namespace, command, and variable)
        /// and returns it as a name/value list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces, commands, and variables are
        /// queried.
        /// </param>
        /// <param name="name">
        /// The name to gather resolution information for.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the diagnostic name/value list; upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode InfoSubCommand(
            Interpreter interpreter,
            string name,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            StringList list = new StringList();
            Result error; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            string normalized = name;

            list.Add("NormalizeName");

            error = null;

            if (NormalizeName(
                    interpreter, ref normalized, ref error) == ReturnCode.Ok)
            {
                list.Add(ReturnCode.Ok.ToString());
                list.Add("Normalized");
                list.Add(normalized);
            }
            else
            {
                list.Add(ReturnCode.Error.ToString());
                list.Add("Result");
                list.Add(error);
            }

            ///////////////////////////////////////////////////////////////////

            string qualifiers = null;
            string tail = null;
            NamespaceFlags namespaceFlags = NamespaceFlags.None;

            list.Add("SplitName");

            error = null;

            if (SplitName(
                    name, ref qualifiers, ref tail, ref namespaceFlags,
                    ref error) == ReturnCode.Ok)
            {
                list.Add(ReturnCode.Ok.ToString());
                list.Add("Qualifiers");
                list.Add(qualifiers);
                list.Add("Tail");
                list.Add(tail);
                list.Add("Flags");
                list.Add(namespaceFlags.ToString());
            }
            else
            {
                list.Add(ReturnCode.Error.ToString());
                list.Add("Result");
                list.Add(error);
            }

            ///////////////////////////////////////////////////////////////////

            INamespace @namespace = null;

            list.Add("GetCurrentNamespaceViaResolvers");

            error = null;

            if (interpreter.GetCurrentNamespaceViaResolvers(
                    null, LookupFlags.Default,
                    ref @namespace, ref error) == ReturnCode.Ok)
            {
                list.Add(ReturnCode.Ok.ToString());
            }
            else
            {
                list.Add(ReturnCode.Error.ToString());
                list.Add("Result");
                list.Add(error);
            }

            list.Add("CurrentINamespace");

            if (@namespace != null)
                list.Add(@namespace.ToString());
            else
                list.Add(FormatOps.DisplayNull);

            ///////////////////////////////////////////////////////////////////

            list.Add("INamespace");

            error = null;

            @namespace = Lookup(
                interpreter, name, false, false, ref error);

            if (@namespace != null)
            {
                list.Add(@namespace.ToString());
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
                list.Add("Result");
                list.Add(error);
            }

            ///////////////////////////////////////////////////////////////////

            list.Add("ParentINamespace");

            error = null;

            @namespace = LookupParent(
                interpreter, name, true, false, false, ref error);

            if (@namespace != null)
            {
                list.Add(@namespace.ToString());
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
                list.Add("Result");
                list.Add(error);
            }

            ///////////////////////////////////////////////////////////////////

            IExecute execute = null;

            list.Add("GetIExecuteViaResolvers");

            error = null;

            if (interpreter.InternalGetIExecuteViaResolvers(
                    interpreter.GetResolveEngineFlagsNoLock(true),
                    name, null, LookupFlags.Default,
                    ref execute, ref error) == ReturnCode.Ok)
            {
                list.Add(ReturnCode.Ok.ToString());
            }
            else
            {
                list.Add(ReturnCode.Error.ToString());
                list.Add("Result");
                list.Add(error);
            }

            list.Add("IExecute");

            if (execute != null)
                list.Add(execute.ToString());
            else
                list.Add(FormatOps.DisplayNull);

            ///////////////////////////////////////////////////////////////////

            ICallFrame frame = null;
            string varName = name;
            VariableFlags variableFlags = VariableFlags.None;

            list.Add("GetVariableFrameViaResolvers");

            error = null;

            if (interpreter.GetVariableFrameViaResolvers(
                    LookupFlags.Default, ref frame, ref varName,
                    ref variableFlags, ref error) == ReturnCode.Ok)
            {
                list.Add(ReturnCode.Ok.ToString());
            }
            else
            {
                list.Add(ReturnCode.Error.ToString());
                list.Add("Result");
                list.Add(error);
            }

            list.Add("ICallFrame");

            if (frame != null)
                list.Add(frame.ToString());
            else
                list.Add(FormatOps.DisplayNull);

            list.Add("FrameVariableName");
            list.Add(varName);
            list.Add("FrameVariableFlags");
            list.Add(variableFlags.ToString());

            ///////////////////////////////////////////////////////////////////

            IVariable variable = null;

            list.Add("GetVariableViaResolversWithSplit");

            variableFlags = VariableFlags.None;
            error = null;

            if (interpreter.GetVariableViaResolversWithSplit(
                    name, ref variableFlags,
                    ref variable, ref error) == ReturnCode.Ok)
            {
                list.Add(ReturnCode.Ok.ToString());
            }
            else
            {
                list.Add(ReturnCode.Error.ToString());
                list.Add("Result");
                list.Add(error);
            }

            list.Add("ResolverVariableFlags");
            list.Add(variableFlags.ToString());
            list.Add("IVariable");

            if (variable != null)
            {
                list.Add(StringList.MakeList(
                    "flags", variable.Flags, "frame", variable.Frame,
                    "name", variable.Name, "value", variable.Value));
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }

            ///////////////////////////////////////////////////////////////////

            result = list;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text is a [namespace]
        /// command invocation, optionally matching a specific sub-command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to split the text into a list.
        /// </param>
        /// <param name="text">
        /// The text to examine.  This parameter may be null or empty.
        /// </param>
        /// <param name="subCommand">
        /// The sub-command name to require, or null to match any [namespace]
        /// invocation.
        /// </param>
        /// <returns>
        /// True if the text is a matching [namespace] command invocation;
        /// otherwise, false.
        /// </returns>
        public static bool IsSubCommand(
            Interpreter interpreter,
            string text,
            string subCommand
            )
        {
            if (String.IsNullOrEmpty(text))
                return false;

            StringList list = null;

            if (ParserOps<string>.SplitList(
                    interpreter, text, 0, Length.Invalid,
                    true, ref list) != ReturnCode.Ok)
            {
                return false;
            }

            if (list.Count < 1)
                return false;

            if (!SharedStringOps.SystemEquals(
                    list[0], MakeAbsoluteName("namespace")))
            {
                return false;
            }

            if (subCommand != null)
            {
                if (list.Count < 2)
                    return false;

                if (!SharedStringOps.SystemEquals(list[1], subCommand))
                    return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the origin of the specified command by
        /// following the chain of namespace imports and command aliases back to
        /// the original command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose command aliases are queried.
        /// </param>
        /// <param name="namespace">
        /// The namespace used for a simple (non-qualified) name.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The simple or qualified command name whose origin is requested.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the absolute name of the originating
        /// command; upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Origin(
            Interpreter interpreter, /* NOTE: Aliases queried here. */
            INamespace @namespace,   /* NOTE: Used for simple name. */
            string name,             /* NOTE: Simple/qualified name. */
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            INamespace currentNamespace = null;

            if (interpreter.GetCurrentNamespaceViaResolvers(
                    null, LookupFlags.Default, ref currentNamespace,
                    ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (currentNamespace == null)
            {
                result = "current namespace is invalid";
                return ReturnCode.Error;
            }

            int count = 0;

            do
            {
                string qualifiers = null;
                string tail = null;
                NamespaceFlags flags = NamespaceFlags.None;

                if (SplitName(
                        name, ref qualifiers, ref tail,
                        ref flags, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                INamespace importNamespace = @namespace;

                if (FlagOps.HasFlags(flags, NamespaceFlags.Qualified, true))
                {
                    importNamespace = Lookup(
                        interpreter, qualifiers, false, false, ref result);

                    if (importNamespace == null)
                        return ReturnCode.Error;
                }
                else if (importNamespace == null)
                {
                    importNamespace = currentNamespace;
                }

                string qualifiedName = null;
                Result localError = null;

                //
                // HACK: This call to GetImport is special.  Since this method
                //       is called, during namespace deletion, indirectly, via
                //       Namespace.RemoveImports --> Namespace.GetOriginName,
                //       the imported, possibly global, namespace may have been
                //       disposed.  In that case, calling the GetImport method
                //       on it is totally meaningless.
                //
                if (!IsDisposed(importNamespace) &&
                    importNamespace.GetImport(
                        TrimLeading(name), ref qualifiedName,
                        ref localError) == ReturnCode.Ok)
                {
                    name = MakeAbsoluteName(qualifiedName);
                    count++; /* NOTE: An import was resolved. */
                }
                else
                {
                    IExecute execute = null;

                    if (interpreter.InternalGetIExecuteViaResolvers(
                            interpreter.GetResolveEngineFlagsNoLock(true),
                            TrimLeading(name), null, LookupFlags.Default,
                            ref execute, ref localError) == ReturnCode.Ok)
                    {
                        IAlias alias = GetAliasFromIExecute(execute);

                        if (alias != null)
                        {
                            name = MakeAbsoluteName(GetAliasName(alias));
                            count++; /* NOTE: A command alias was resolved. */

                            continue;
                        }
                        else
                        {
                            count++; /* NOTE: A command was resolved. */
                        }
                    }

                    if (count == 0)
                    {
                        result = localError;
                        return ReturnCode.Error;
                    }

                    result = MakeAbsoluteName(name);
                    return ReturnCode.Ok;
                }
            }
            while (true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the qualified name of the parent of the namespace
        /// identified by the specified name (or the current namespace), always
        /// returning an empty string for the global namespace.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose namespaces are queried.
        /// </param>
        /// <param name="name">
        /// The name of the namespace whose parent is requested.  When null, the
        /// current namespace is used.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the qualified name of the parent
        /// namespace (or an empty string for the global namespace); upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Parent(
            Interpreter interpreter,
            string name,
            ref Result result
            )
        {
            INamespace @namespace;

            if (name != null)
            {
                if (IsGlobalName(name))
                {
                    //
                    // NOTE: *SPECIAL* Native Tcl always returns an empty
                    //       string for the parent of the global namespace.
                    //
                    result = String.Empty;
                    return ReturnCode.Ok;
                }
                else
                {
                    @namespace = Lookup(
                        interpreter, name, false, false, ref result);

                    if (@namespace == null)
                        return ReturnCode.Error;
                }
            }
            else /* NOTE: The null value means "use current namespace". */
            {
                INamespace currentNamespace = null;

                if (interpreter.GetCurrentNamespaceViaResolvers(
                        null, LookupFlags.Default, ref currentNamespace,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (currentNamespace == null)
                {
                    result = "current namespace is invalid";
                    return ReturnCode.Error;
                }

                if (IsGlobal(interpreter, currentNamespace))
                {
                    //
                    // NOTE: *SPECIAL* Native Tcl always returns an empty
                    //       string for the parent of the global namespace.
                    //
                    result = String.Empty;
                    return ReturnCode.Ok;
                }

                @namespace = currentNamespace;
            }

            string qualifiedName = @namespace.QualifiedName;

            @namespace = @namespace.Parent;

            if (@namespace != null)
            {
                result = @namespace.QualifiedName;
                return ReturnCode.Ok;
            }
            else
            {
                result = String.Format(
                    "namespace {0} has no parent",
                    FormatOps.WrapOrNull(qualifiedName));

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the fully qualified, absolute name of the
        /// command or variable identified by the specified name, returning an
        /// empty string when no matching entity is found.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose commands and variables are queried.
        /// </param>
        /// <param name="namespace">
        /// The namespace used for a simple (non-qualified) name.  When null,
        /// the current namespace is used.
        /// </param>
        /// <param name="name">
        /// The simple or qualified name to resolve.
        /// </param>
        /// <param name="flags">
        /// The namespace flags indicating whether to resolve a command or a
        /// variable.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the resolved name (or an empty string
        /// when not found); upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Which(
            Interpreter interpreter, /* NOTE: Command/variable queried here. */
            INamespace @namespace,   /* NOTE: Used for simple name. */
            string name,             /* NOTE: Simple/qualified name. */
            NamespaceFlags flags,    /* NOTE: Command or variable? */
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (name == null)
            {
                result = "invalid name";
                return ReturnCode.Error;
            }

            if (@namespace == null)
            {
                INamespace currentNamespace = null;

                if (interpreter.GetCurrentNamespaceViaResolvers(
                        null, LookupFlags.Default, ref currentNamespace,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                @namespace = currentNamespace;
            }

            string absoluteName = MakeAbsoluteName(name);

            string qualifiedName = !IsQualifiedName(name) ?
                MakeQualifiedName(interpreter, @namespace, name) : name;

            string qualifiedAbsoluteName = MakeAbsoluteName(qualifiedName);

            if (FlagOps.HasFlags(flags, NamespaceFlags.Command, true))
            {
                if (interpreter.InternalDoesIExecuteExistViaResolvers(
                        qualifiedName) == ReturnCode.Ok)
                {
                    result = qualifiedAbsoluteName;
                    return ReturnCode.Ok;
                }
                else
                {
                    if (interpreter.InternalDoesIExecuteExistViaResolvers(
                            name) == ReturnCode.Ok)
                    {
                        result = absoluteName;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        //
                        // NOTE: No command found, return empty.
                        //
                        result = String.Empty;
                        return ReturnCode.Ok;
                    }
                }
            }
            else if (FlagOps.HasFlags(flags, NamespaceFlags.Variable, true))
            {
                if (interpreter.DoesVariableExist(
                        VariableFlags.NamespaceWhichMask,
                        qualifiedAbsoluteName) == ReturnCode.Ok)
                {
                    result = qualifiedAbsoluteName;
                    return ReturnCode.Ok;
                }
                else
                {
                    if (interpreter.DoesVariableExist(
                            VariableFlags.GlobalNamespaceWhichMask,
                            name) == ReturnCode.Ok)
                    {
                        result = absoluteName;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        //
                        // NOTE: No variable found, return empty.
                        //
                        result = String.Empty;
                        return ReturnCode.Ok;
                    }
                }
            }
            else
            {
                //
                // NOTE: No flag set, do nothing.
                //
                result = String.Empty;
                return ReturnCode.Ok;
            }
        }
        #endregion
        #endregion
    }
}
