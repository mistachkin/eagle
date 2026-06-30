/*
 * BundleManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the contract for managing script bundles, i.e.
    /// archive files that can be mounted into the interpreter and have their
    /// contained scripts and data accessed.  It provides members to query the
    /// currently mounted bundles, to bracket the evaluation of a bundle file,
    /// and to mount, unmount, list, and read data from bundles.
    /// </summary>
    [ObjectId("282223e2-a56a-4847-9963-63f0b14ad25d")]
    public interface IBundleManager
    {
        /// <summary>
        /// Gets the file name of the bundle currently being evaluated, if any.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Gets the collection of mounted bundle file names mapped to their
        /// raw bytes.
        /// </summary>
        IDictionary<string, byte[]> FileNames { get; }

        /// <summary>
        /// Begins the evaluation of the specified bundle file, recording the
        /// previously active bundle file name so that it can be restored later.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the evaluation is taking place.
        /// </param>
        /// <param name="fileName">
        /// The file name of the bundle whose evaluation is beginning.
        /// </param>
        /// <param name="savedFileName">
        /// Upon return, receives the previously active bundle file name, for
        /// later restoration by <see cref="EndEvaluation" />.
        /// </param>
        void BeginEvaluation(
            Interpreter interpreter,
            string fileName,
            out string savedFileName
        );

        /// <summary>
        /// Ends the evaluation of a bundle file, restoring the previously
        /// active bundle file name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the evaluation is taking place.
        /// </param>
        /// <param name="savedFileName">
        /// The previously active bundle file name to restore, as produced by
        /// <see cref="BeginEvaluation" />.
        /// </param>
        void EndEvaluation(
            Interpreter interpreter,
            ref string savedFileName
        );

        /// <summary>
        /// Lists the bundles currently mounted, optionally filtered by a
        /// pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the operation is taking place.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter the mounted bundles.  This parameter may
        /// be null to list all mounted bundles.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive pattern match; otherwise, the
        /// match is case-sensitive.
        /// </param>
        /// <param name="error">
        /// On success, receives the list of matching mounted bundles; upon
        /// failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListMounts(
            Interpreter interpreter,
            string pattern,
            bool noCase,
            ref Result error
        );

        /// <summary>
        /// Mounts the specified bundle file, making its contents available to
        /// the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the operation is taking place.
        /// </param>
        /// <param name="fileName">
        /// The file name of the bundle to mount.
        /// </param>
        /// <param name="password">
        /// The password used to decrypt the bundle, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="errorOnMounted">
        /// Non-zero to return an error when the bundle is already mounted.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Mount(
            Interpreter interpreter,
            string fileName,
            byte[] password,
            bool errorOnMounted,
            ref Result error
        );

        /// <summary>
        /// Reads the data at the specified path from the mounted bundles.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the operation is taking place.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used while reading the data.  This parameter may be
        /// null.
        /// </param>
        /// <param name="encoding">
        /// The encoding used while reading the data.  This parameter may be
        /// null.
        /// </param>
        /// <param name="path">
        /// The path, within the mounted bundles, of the data to read.
        /// </param>
        /// <param name="data">
        /// Upon success, receives the raw bytes of the requested data.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetData(
            Interpreter interpreter,
            CultureInfo cultureInfo,
            Encoding encoding,
            string path,
            ref byte[] data,
            ref Result error
        );

        /// <summary>
        /// Unmounts the specified bundle file, removing its contents from the
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the operation is taking place.
        /// </param>
        /// <param name="fileName">
        /// The file name of the bundle to unmount.
        /// </param>
        /// <param name="errorOnNotMounted">
        /// Non-zero to return an error when the bundle is not currently
        /// mounted.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Unmount(
            Interpreter interpreter,
            string fileName,
            bool errorOnNotMounted,
            ref Result error
        );
    }
}
