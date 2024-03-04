/*
 * SnippetOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using SnippetList = System.Collections.Generic.IEnumerable<
    Eagle._Interfaces.Public.ISnippet>;

namespace Eagle._Components.Private
{
    [ObjectId("75ef81e9-5096-4210-80e0-b04e18e4a19a")]
    internal static class SnippetOps
    {
        private static bool ShouldReadViaEngine(
            SnippetFlags snippetFlags, /* in */
            bool isScript,             /* in */
            bool isSignature           /* in */
            )
        {
            if (isSignature || FlagOps.HasFlags(
                    snippetFlags, SnippetFlags.ReadAsRawText, true))
            {
                //
                // NOTE: Use of the Engine class to read the text
                //       has been manually disabled by the caller
                //       -OR- the specified file name appears to
                //       point to a signature file; therefore, do
                //       not use the Engine class to read it.
                //
                return false;
            }
            else if (isScript)
            {
                //
                // NOTE: The specified file name appears to point
                //       to a script file; therefore, use Engine
                //       class to properly read it (i.e. with all
                //       line translations, etc).
                //
                return true;
            }
            else
            {
                //
                // NOTE: Otherwise, this may be just an arbitrary
                //       data file, not a script file; therefore,
                //       do not use the Engine class to read the
                //       text.
                //
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ISnippet Create(
            string path,              /* in: OPTIONAL */
            byte[] bytes,             /* in: OPTIONAL */
            string text,              /* in: OPTIONAL */
            string xml,               /* in: OPTIONAL */
            SnippetFlags snippetFlags /* in */
            )
        {
            return new Snippet(
                null, null, null, path, bytes, text, xml, null,
                snippetFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode LoadFileData(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            bool viaEngine,          /* in */
            ref byte[] bytes,        /* out */
            ref string text,         /* out */
            ref Result error         /* out */
            )
        {
            byte[] localBytes = null;

            try
            {
                localBytes = File.ReadAllBytes(fileName);
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }

            string localText = null;

            if (viaEngine)
            {
                if (Engine.ReadScriptFile(
                        interpreter, fileName, ref localText,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }
            else
            {
                Encoding encoding = StringOps.GetEncoding(
                    EncodingType.Snippet);

                try
                {
                    localText = (encoding != null) ?
                        File.ReadAllText(fileName, encoding) :
                        File.ReadAllText(fileName);
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }

            bytes = localBytes;
            text = localText;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode LoadOneFile(
            Interpreter interpreter,   /* in */
            string fileName,           /* in */
            SnippetFlags snippetFlags, /* in */
            ref ISnippet snippet,      /* out */
            ref Result error           /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = String.Format(
                    "invalid primary file name {0}",
                    FormatOps.DisplayString(fileName));

                return ReturnCode.Error;
            }

            if (!File.Exists(fileName))
            {
                error = String.Format(
                    "primary file {0} does not exist",
                    FormatOps.DisplayString(fileName));

                return ReturnCode.Error;
            }

            bool isScript = PathOps.IsScriptFile(
                fileName, false, false);

            if (!isScript && FlagOps.HasFlags(snippetFlags,
                    SnippetFlags.MustBeScript, true))
            {
                error = String.Format(
                    "primary file {0} is not script",
                    FormatOps.DisplayString(fileName));

                return ReturnCode.Error;
            }

            bool isSignature = PathOps.IsSignatureFile(
                fileName);

            if (!isSignature && FlagOps.HasFlags(snippetFlags,
                    SnippetFlags.MustBeSignature, true))
            {
                error = String.Format(
                    "primary file {0} is not signature",
                    FormatOps.DisplayString(fileName));

                return ReturnCode.Error;
            }

            byte[] localBytes = null;
            string localText = null;

            if (LoadFileData(interpreter,
                    fileName, ShouldReadViaEngine(snippetFlags,
                    isScript, isSignature), ref localBytes,
                    ref localText, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            string localXml = null;

            if (isSignature)
            {
                //
                // HACK: Signature (certificate) data is always
                //       returned via the Xml property, not the
                //       Text property, from the GetData method
                //       of the _Hosts.File class.
                //
                localXml = localText;
                localText = null;
            }

            //
            // HACK: Since this is a signature file that is being
            //       loaded without the text from its associated
            //       script file (if any?), set the snippet flag
            //       so that the snippet name is based on a hash
            //       of the associated script file and not of the
            //       signature file itself.
            //
            if (isSignature && !FlagOps.HasFlags(snippetFlags,
                    SnippetFlags.NoHashOtherPath, true))
            {
                snippetFlags |= SnippetFlags.HashOtherPath;
            }

            snippetFlags = Snippet.MaskFlags(snippetFlags,
                isScript, isSignature, isSignature, true);

            snippet = Create(
                fileName, localBytes, localText, localXml,
                snippetFlags);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode LoadOneCertificateFile(
            Interpreter interpreter,   /* in */
            string fileName,           /* in */
            SnippetFlags snippetFlags, /* in */
            ref ISnippet snippet,      /* out */
            ref Result error           /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = String.Format(
                    "invalid primary file name {0}",
                    FormatOps.DisplayString(fileName));

                return ReturnCode.Error;
            }

            if (!File.Exists(fileName))
            {
                error = String.Format(
                    "primary file {0} does not exist",
                    FormatOps.DisplayString(fileName));

                return ReturnCode.Error;
            }

            string otherFileName = PathOps.RemoveExtension(
                fileName);

            if (String.IsNullOrEmpty(otherFileName))
            {
                error = String.Format(
                    "invalid other file name {0} for {1}",
                    FormatOps.DisplayString(otherFileName),
                    FormatOps.DisplayString(fileName));

                return ReturnCode.Error;
            }

            bool isScript = PathOps.IsScriptFile(
                otherFileName, false, false);

            if (!isScript && FlagOps.HasFlags(snippetFlags,
                    SnippetFlags.MustBeScript, true))
            {
                error = String.Format(
                    "other file {0} for {1} is not script",
                    FormatOps.DisplayString(otherFileName),
                    FormatOps.DisplayString(fileName));

                return ReturnCode.Error;
            }

            if (!File.Exists(otherFileName))
            {
                error = String.Format(
                    "other file {0} for {1} does not exist",
                    FormatOps.DisplayString(otherFileName),
                    FormatOps.DisplayString(fileName));

                return ReturnCode.Error;
            }

            byte[] localBytes = null;
            string localText = null;

            if (LoadFileData(
                    interpreter, otherFileName, ShouldReadViaEngine(
                    snippetFlags, isScript, false), ref localBytes,
                    ref localText, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            string localXml = null;

#if XML
            if (XmlOps.CanLoadFile(fileName,
                    ref localXml, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }
#endif

            snippetFlags = Snippet.MaskFlags(snippetFlags,
                isScript, true, true, true);

            snippet = Create(
                fileName, localBytes, localText, localXml,
                snippetFlags);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetText(
            ISnippet snippet, /* in */
            ref string text,  /* out */
            ref Result error  /* out */
            )
        {
            if (snippet == null)
            {
                error = "invalid snippet";
                return ReturnCode.Error;
            }

            string localText;
            SnippetFlags snippetFlags = snippet.SnippetFlags;

            if (FlagOps.HasFlags(
                    snippetFlags, SnippetFlags.UseBytes, true))
            {
                byte[] bytes = snippet.Bytes;

                if (bytes == null)
                {
                    error = "invalid snippet bytes";
                    return ReturnCode.Error;
                }

                Encoding encoding = StringOps.GetEncoding(
                    EncodingType.Snippet);

                if (encoding == null)
                {
                    error = "invalid encoding";
                    return ReturnCode.Error;
                }

                localText = encoding.GetString(bytes);
            }
#if XML
            else if (FlagOps.HasFlags(
                    snippetFlags, SnippetFlags.UseXml, true))
            {
                string xml = snippet.Xml;

                if (xml == null)
                {
                    error = "invalid snippet xml";
                    return ReturnCode.Error;
                }

                if (XmlOps.ValidateScriptString(
                        xml, true, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                localText = xml;
            }
#endif
            else
            {
                localText = snippet.Text;

                if (localText == null)
                {
                    error = "invalid snippet text";
                    return ReturnCode.Error;
                }
            }

            text = localText;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static SnippetList LoadAllCertificateFiles(
            Interpreter interpreter,   /* in */
            string path,               /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref ResultList errors      /* in, out */
            )
        {
            string[] fileNames;

            try
            {
                fileNames = Directory.GetFiles(
                    path, String.Format("{0}{1}", Characters.Asterisk,
                    FileExtension.Signature), FileOps.GetSearchOption(
                    FlagOps.HasFlags(lookupFlags, LookupFlags.Recursive,
                    true)));
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Interpreter).Name,
                    TracePriority.FileSystemError);

                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
                return null;
            }

            if ((fileNames == null) || (fileNames.Length == 0))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "path {0} contains no certificates",
                    Utility.FormatWrapOrNull(path)));

                return null;
            }

            Array.Sort(fileNames); /* O(N) */

            IList<ISnippet> result = new List<ISnippet>();

            foreach (string fileName in fileNames)
            {
                if (String.IsNullOrEmpty(fileName))
                    continue;

                ISnippet snippet = null;
                Result error = null; /* REUSED */

                if (LoadOneCertificateFile(interpreter,
                        fileName, snippetFlags, ref snippet,
                        ref error) != ReturnCode.Ok)
                {
                    if (error != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(error);
                    }

                    continue;
                }

                string name = null;

                error = null;

                if (interpreter.InternalSnippetName(
                        snippet, snippetFlags, lookupFlags,
                        ref name, ref error) != ReturnCode.Ok)
                {
                    if (error != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(error);
                    }

                    continue;
                }

                snippet.SetName(name);
                result.Add(snippet);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static SnippetList LoadAllFiles(
            Interpreter interpreter,   /* in */
            string path,               /* in */
            string pattern,            /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref ResultList errors      /* in, out */
            )
        {
            string[] fileNames;

            try
            {
                fileNames = Directory.GetFiles(
                    path, pattern, FileOps.GetSearchOption(
                    FlagOps.HasFlags(lookupFlags,
                    LookupFlags.Recursive, true)));
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Interpreter).Name,
                    TracePriority.FileSystemError);

                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
                return null;
            }

            if ((fileNames == null) || (fileNames.Length == 0))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "path {0} contains no certificates",
                    Utility.FormatWrapOrNull(path)));

                return null;
            }

            Array.Sort(fileNames); /* O(N) */

            IList<ISnippet> result = new List<ISnippet>();

            foreach (string fileName in fileNames)
            {
                if (String.IsNullOrEmpty(fileName))
                    continue;

                ISnippet snippet = null;
                Result error = null; /* REUSED */

                if (LoadOneFile(interpreter,
                        fileName, snippetFlags, ref snippet,
                        ref error) != ReturnCode.Ok)
                {
                    if (error != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(error);
                    }

                    continue;
                }

                string name = null;

                error = null;

                if (interpreter.InternalSnippetName(
                        snippet, snippetFlags, lookupFlags,
                        ref name, ref error) != ReturnCode.Ok)
                {
                    if (error != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(error);
                    }

                    continue;
                }

                snippet.SetName(name);
                result.Add(snippet);
            }

            return result;
        }
    }
}
