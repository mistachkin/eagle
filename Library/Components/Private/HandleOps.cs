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
    /// <summary>
    /// This class provides helper methods that support the "identity" style
    /// of object handling used by the <c>[object invoke]</c> family of
    /// commands, including wrapping a raw object value as an opaque object
    /// handle and the trivial identity transforms used to route arbitrary
    /// values and types through the return-value fixup machinery.
    /// </summary>
    [ObjectId("9099d3af-8b6d-404a-95f5-f8621f5884c2")]
    internal static class HandleOps
    {
        #region Private Constants
        #region Member (Method) Names
        /// <summary>
        /// The name of the <see cref="Identity" /> method, used when reflecting
        /// over this class to obtain its member information.
        /// </summary>
        internal static readonly string IdentityMemberName = "Identity";

        /// <summary>
        /// The name of the <see cref="TypeIdentity" /> method, used when
        /// reflecting over this class to obtain its member information.
        /// </summary>
        internal static readonly string TypeIdentityMemberName = "TypeIdentity";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Member (Method) Infos
        /// <summary>
        /// The cached reflected member information for the
        /// <see cref="Identity" /> method.
        /// </summary>
        internal static readonly MemberInfo[] IdentityMemberInfo =
            typeof(HandleOps).GetMember(
                IdentityMemberName, ObjectOps.GetBindingFlags(
                    MetaBindingFlags.PublicStaticMethod, true));

        /// <summary>
        /// The cached reflected member information for the
        /// <see cref="TypeIdentity" /> method.
        /// </summary>
        internal static readonly MemberInfo[] TypeIdentityMemberInfo =
            typeof(HandleOps).GetMember(
                TypeIdentityMemberName, ObjectOps.GetBindingFlags(
                    MetaBindingFlags.PublicStaticMethod, true));
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /// <summary>
        /// This method wraps a raw object value as an opaque object handle,
        /// using the return-value fixup machinery so that the resulting handle
        /// is registered with the specified interpreter.  This works around the
        /// inability to pass a string that happens to represent an existing
        /// opaque object handle to a managed method via <c>[object invoke]</c>.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to look up and create the opaque object
        /// handle.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The object value to wrap as an opaque object handle.
        /// </param>
        /// <returns>
        /// The string name of the opaque object handle for the specified value,
        /// the value itself when it is already a string, or null if the value
        /// could not be wrapped.
        /// </returns>
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
                            ObjectFlags.None, null, null,
                            ObjectOptionType.None, null, null, name,
                            true, ObjectOps.GetDefaultDispose(),
                            false, false, false,
                            ref result) == ReturnCode.Ok)
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

        /// <summary>
        /// This method returns its argument unchanged.  It is used by
        /// <c>[object invoke -identity]</c> so that the return-value fixup
        /// handling can be applied to any object.
        /// </summary>
        /// <param name="arg">
        /// The object to return unchanged.
        /// </param>
        /// <returns>
        /// The <paramref name="arg" /> value, exactly as supplied.
        /// </returns>
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

        /// <summary>
        /// This method returns its <see cref="Type" /> argument unchanged.  It
        /// is used by <c>[object invoke -typeidentity]</c> so that the
        /// return-value fixup handling can be applied to any
        /// <see cref="Type" /> object.
        /// </summary>
        /// <param name="arg">
        /// The <see cref="Type" /> object to return unchanged.
        /// </param>
        /// <returns>
        /// The <paramref name="arg" /> value, exactly as supplied.
        /// </returns>
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
