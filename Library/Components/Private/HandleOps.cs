/*
 * HandleOps.cs --
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
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    [ObjectId("9099d3af-8b6d-404a-95f5-f8621f5884c2")]
    internal static class HandleOps
    {
        #region Private Constants
        #region Member (Method) Names
        internal static readonly string IdentityMemberName = "Identity";
        internal static readonly string TypeIdentityMemberName = "TypeIdentity";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Member (Method) Infos
        internal static readonly MemberInfo[] IdentityMemberInfo =
            typeof(HandleOps).GetMember(
                IdentityMemberName, ObjectOps.GetBindingFlags(
                    MetaBindingFlags.PublicStaticMethod, true));

        internal static readonly MemberInfo[] TypeIdentityMemberInfo =
            typeof(HandleOps).GetMember(
                TypeIdentityMemberName, ObjectOps.GetBindingFlags(
                    MetaBindingFlags.PublicStaticMethod, true));
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static string Wrap(
            Interpreter interpreter,
            object value
            )
        {
            //
            // HACK: Currently, it is impossible to pass a string that
            //       happens to represent an existing opaque object
            //       handle to any managed method via [object invoke],
            //       et al.  This is because there must be internal
            //       calls (inside the binder and method overload
            //       resolution engine) that automatically convert any
            //       such string to the underlying raw object value for
            //       [object invoke] to be truly useful.  This method
            //       is an extremely nasty hack that works around this
            //       issue.
            //
            if (interpreter != null)
            {
                string name = null;

                if (interpreter.GetObject(
                        value, LookupFlags.MarshalNoVerbose,
                        ref name) == ReturnCode.Ok)
                {
                    Result result = null;

                    if (MarshalOps.FixupReturnValue(
                            interpreter, interpreter.InternalBinder,
                            interpreter.InternalCultureInfo, null,
                            ObjectFlags.None, null, ObjectOptionType.None, null,
                            null, name, true, ObjectOps.GetDefaultDispose(),
                            false, false, false, ref result) == ReturnCode.Ok)
                    {
                        return result;
                    }
                }
                else if (value is string)
                {
                    return (string)value;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static object Identity(
            object arg
            )
        {
            //
            // NOTE: Used by [object invoke -identity] to allow the
            //       FixupReturnValue handling to be used on any
            //       object.
            //
            return arg;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Type TypeIdentity(
            Type arg
            )
        {
            //
            // NOTE: Used by [object invoke -typeidentity] to allow the
            //       FixupReturnValue handling to be used on any Type
            //       object.
            //
            return arg;
        }
        #endregion
    }
}
