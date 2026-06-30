/*
 * AnyTypeData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines methods for retrieving named values as specific
    /// reference types, such as strings, GUIDs, URIs, versions, interpreters,
    /// plugins, rule sets, objects, encodings, and byte arrays.  Each method
    /// reports its outcome both through its return value and through an error
    /// parameter.
    /// </summary>
    [ObjectId("c05b8893-ac64-44c9-a45f-8740dd41cc9e")]
    public interface IAnyTypeData
    {
        /// <summary>
        /// Attempts to get the value associated with the specified name as
        /// client data.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetClientData(
            string name,
            out IClientData value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// string.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetString(
            string name,
            bool toString,
            out string value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// list of strings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when interpreting the value.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed as a list.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetStringList(
            Interpreter interpreter,
            string name,
            bool toString,
            out StringList value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// globally unique identifier.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetGuid(
            string name,
            bool toString,
            out Guid value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// uniform resource identifier.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="uriKind">
        /// The <see cref="UriKind" /> that the value is required to conform
        /// to.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetUri(
            string name,
            UriKind uriKind,
            bool toString,
            out Uri value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// version.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetVersion(
            string name,
            bool toString,
            out Version value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as an
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when interpreting the value.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is resolved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetInterpreter(
            Interpreter interpreter,
            string name,
            bool toString,
            out Interpreter value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when interpreting the value.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is resolved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetPlugin(
            Interpreter interpreter,
            string name,
            bool toString,
            out IPlugin value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// rule set.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when interpreting the value.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is resolved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetRuleSet(
            Interpreter interpreter,
            string name,
            bool toString,
            out IRuleSet value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as an
        /// opaque object handle.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when interpreting the value.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is resolved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetObject(
            Interpreter interpreter,
            string name,
            bool toString,
            out IObject value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// character encoding.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when interpreting the value.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is resolved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetEncoding(
            Interpreter interpreter,
            string name,
            bool toString,
            out Encoding value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as an
        /// array of bytes.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is converted.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetByteArray(
            string name,
            bool toString,
            out byte[] value,
            ref Result error
            );
    }
}
