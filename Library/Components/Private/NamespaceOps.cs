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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("704cb03e-df30-470f-81be-27fa3f128a88")]
    internal static class NamespaceOps
    {
        #region Private Constants
        private static readonly string[] Delimiters = new string[] {
            TclVars.Namespace.Separator
        };

        ///////////////////////////////////////////////////////////////////////

        private static readonly string[] EmptyName = new string[] {
            String.Empty
        };

        ///////////////////////////////////////////////////////////////////////

        private static readonly string AbsoluteNameFormat = "{0}{1}";
        private static readonly string QualifiedNameFormat = "{0}{1}{2}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Namespace Support Methods
        #region Instance Per-Frame Support Methods
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

        public static bool IsSame(
            INamespace namespace1,
            INamespace namespace2
            )
        {
            return Object.ReferenceEquals(namespace1, namespace2);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static string TrimLeading(
            string name
            )
        {
            bool absolute = false;

            return TrimLeading(name, ref absolute);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static string MakeQualifiedName(
            Interpreter interpreter,
            string name
            )
        {
            return MakeQualifiedName(interpreter, name, false);
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static void NormalizeName(
            ref string qualifiers, /* in, out */
            ref string tail        /* in, out */
            )
        {
            if (qualifiers != null)
            {
                StringBuilder newQualifiers = StringOps.NewStringBuilder();

                foreach (string name in SplitName(qualifiers))
                {
                    if (String.IsNullOrEmpty(name))
                        continue;

                    if (newQualifiers.Length > 0)
                        newQualifiers.Append(TclVars.Namespace.Separator);

                    newQualifiers.Append(name);
                }

                qualifiers = newQualifiers.ToString();
            }

            if (tail != null)
            {
                StringBuilder newTail = StringOps.NewStringBuilder();

                foreach (string name in SplitName(tail))
                {
                    if (String.IsNullOrEmpty(name))
                        continue;

                    if (newTail.Length > 0)
                        newTail.Append(TclVars.Namespace.Separator);

                    newTail.Append(name);
                }

                tail = newTail.ToString();
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

                    if (interpreter.GetIExecuteViaResolvers(
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

                if (interpreter.GetIExecuteViaResolvers(
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

        #region Sub-Command Support Methods
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
            bool useNamespaces = interpreter.AreNamespacesEnabled();

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

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

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

            if (interpreter.GetIExecuteViaResolvers(
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

                    if (interpreter.GetIExecuteViaResolvers(
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
                if (interpreter.DoesIExecuteExistViaResolvers(
                        qualifiedName) == ReturnCode.Ok)
                {
                    result = qualifiedAbsoluteName;
                    return ReturnCode.Ok;
                }
                else
                {
                    if (interpreter.DoesIExecuteExistViaResolvers(
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
