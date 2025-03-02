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
    [ObjectId("282223e2-a56a-4847-9963-63f0b14ad25d")]
    public interface IBundleManager
    {
        string FileName { get; }

        IDictionary<string, byte[]> FileNames { get; }

        void BeginEvaluation(
            Interpreter interpreter,
            string fileName,
            out string savedFileName
        );

        void EndEvaluation(
            Interpreter interpreter,
            ref string savedFileName
        );

        ReturnCode ListMounts(
            Interpreter interpreter,
            string pattern,
            bool noCase,
            ref Result error
        );

        ReturnCode Mount(
            Interpreter interpreter,
            string fileName,
            byte[] password,
            bool errorOnMounted,
            ref Result error
        );

        ReturnCode GetData(
            Interpreter interpreter,
            CultureInfo cultureInfo,
            Encoding encoding,
            string path,
            ref byte[] data,
            ref Result error
        );

        ReturnCode Unmount(
            Interpreter interpreter,
            string fileName,
            bool errorOnNotMounted,
            ref Result error
        );
    }
}
