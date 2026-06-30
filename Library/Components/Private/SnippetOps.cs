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
    /// <summary>
    /// This class provides the internal helper methods used to create and load
    /// script snippets from files, including ordinary script and data files as
    /// well as their associated signature (certificate) files.  Each loaded
    /// snippet is represented by an <see cref="ISnippet" /> instance.
    /// </summary>
    [ObjectId("75ef81e9-5096-4210-80e0-b04e18e4a19a")]
    internal static class SnippetOps
    {
        /// <summary>
        /// This method determines whether the text of a snippet file should be
        /// read using the <see cref="Engine" /> class (which applies line
        /// translations and other script-specific processing) instead of being
        /// read as raw text.
        /// </summary>
        /// <param name="snippetFlags">
        /// The flags that control how the snippet is read and processed.
        /// </param>
        /// <param name="isScript">
        /// Non-zero if the file appears to be a script file.
        /// </param>
        /// <param name="isSignature">
        /// Non-zero if the file appears to be a signature (certificate) file.
        /// </param>
        /// <returns>
        /// True if the <see cref="Engine" /> class should be used to read the
        /// file text; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method creates a new <see cref="ISnippet" /> instance from the
        /// specified file path and data.
        /// </summary>
        /// <param name="path">
        /// The file path associated with the snippet.  This parameter may be
        /// null.
        /// </param>
        /// <param name="bytes">
        /// The raw bytes of the snippet.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The text of the snippet.  This parameter may be null.
        /// </param>
        /// <param name="xml">
        /// The XML (or signature) data of the snippet.  This parameter may be
        /// null.
        /// </param>
        /// <param name="snippetFlags">
        /// The flags that control how the snippet is read and processed.
        /// </param>
        /// <returns>
        /// The newly created <see cref="ISnippet" /> instance.
        /// </returns>
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

        /// <summary>
        /// This method reads the raw bytes and text of the specified file,
        /// optionally using the <see cref="Engine" /> class to read the text.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when reading the file via the
        /// <see cref="Engine" /> class.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to read.
        /// </param>
        /// <param name="viaEngine">
        /// Non-zero if the text should be read using the <see cref="Engine" />
        /// class; otherwise, the text is read directly.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the raw bytes read from the file.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the text read from the file.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method loads a single script or data file and creates an
        /// <see cref="ISnippet" /> instance to represent it, validating that the
        /// file exists and conforms to any script or signature requirements
        /// specified via the snippet flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when loading the file.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to load.
        /// </param>
        /// <param name="snippetFlags">
        /// The flags that control how the snippet is read and processed.
        /// </param>
        /// <param name="snippet">
        /// Upon success, receives the newly created <see cref="ISnippet" />
        /// instance.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

            ScriptFlags scriptFlags = ScriptOps.GetFlags(
                interpreter, ScriptFlags.UserRequiredFile,
                false, false);

            bool isScript = PathOps.IsScriptFile(interpreter,
                fileName, ScriptOps.ViaGetScriptFlag(null,
                ref scriptFlags), false, false);

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

        /// <summary>
        /// This method loads a single signature (certificate) file along with
        /// its associated script file and creates an <see cref="ISnippet" />
        /// instance to represent them.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when loading the file.
        /// </param>
        /// <param name="fileName">
        /// The name of the signature (certificate) file to load.
        /// </param>
        /// <param name="snippetFlags">
        /// The flags that control how the snippet is read and processed.
        /// </param>
        /// <param name="snippet">
        /// Upon success, receives the newly created <see cref="ISnippet" />
        /// instance.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

            ScriptFlags scriptFlags = ScriptOps.GetFlags(
                interpreter, ScriptFlags.UserRequiredFile,
                false, false);

            bool isScript = PathOps.IsScriptFile(interpreter,
                otherFileName, ScriptOps.ViaGetScriptFlag(
                null, ref scriptFlags), false, false);

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

        /// <summary>
        /// This method obtains the text of the specified snippet, deriving it
        /// from the snippet bytes, XML, or text depending on the snippet flags.
        /// </summary>
        /// <param name="snippet">
        /// The snippet whose text is to be obtained.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the text of the snippet.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method loads all signature (certificate) files found within the
        /// specified path and creates an <see cref="ISnippet" /> instance for
        /// each one that loads successfully.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when loading the files.
        /// </param>
        /// <param name="path">
        /// The directory path to search for signature (certificate) files.
        /// </param>
        /// <param name="snippetFlags">
        /// The flags that control how the snippets are read and processed.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the files are located, including whether
        /// the search is recursive.
        /// </param>
        /// <param name="errors">
        /// Upon entry, an optional existing list of errors; upon failure of any
        /// individual file, receives information about the errors encountered.
        /// </param>
        /// <returns>
        /// The collection of loaded snippets, or null if the path could not be
        /// searched or contained no certificates.
        /// </returns>
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

        /// <summary>
        /// This method loads all files matching the specified pattern within the
        /// specified path and creates an <see cref="ISnippet" /> instance for
        /// each one that loads successfully.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when loading the files.
        /// </param>
        /// <param name="path">
        /// The directory path to search for files.
        /// </param>
        /// <param name="pattern">
        /// The search pattern used to match file names within the path.
        /// </param>
        /// <param name="snippetFlags">
        /// The flags that control how the snippets are read and processed.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the files are located, including whether
        /// the search is recursive.
        /// </param>
        /// <param name="errors">
        /// Upon entry, an optional existing list of errors; upon failure of any
        /// individual file, receives information about the errors encountered.
        /// </param>
        /// <returns>
        /// The collection of loaded snippets, or null if the path could not be
        /// searched or contained no matching files.
        /// </returns>
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
