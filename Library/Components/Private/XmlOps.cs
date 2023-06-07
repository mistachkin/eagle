/*
 * XmlOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;

#if false
using System.Xml.Xsl;
#endif

#if SERIALIZATION
using System.Xml.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using NamespacePair = System.Collections.Generic.KeyValuePair<string, string>;

namespace Eagle._Components.Private
{
    [ObjectId("dc088aa2-7481-4313-94dc-4809fb31eb1d")]
    internal static class XmlOps
    {
        #region Private Constants
        //
        // NOTE: This string is used to detect if a given string looks like
        //       the start of an XML document.  This is not designed to be a
        //       "perfect" detection mechanism; however, it will work well
        //       enough for our purposes.
        //
        private static readonly string DocumentStart = "<?xml ";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This should be the XPath query that will return all nodes
        //       within the XML document.
        //
        // HACK: This is purposely not read-only.
        //
        private static string AllXPath = "/descendant-or-self::node()";

        ///////////////////////////////////////////////////////////////////////

        #region Relaxed Schema Support Constants
        //
        // NOTE: This token can be used in the XSD schema to indicate where
        //       the "<xsd:anyAttribute>" XML element should be inserted if
        //       the "relaxed" XML schema validation mode is enabled; note
        //       that the token is wrapped in an XML comment so that it a
        //       NO-OP when left alone.
        //
        private static readonly string RelaxedToken = "<!-- {RelaxedXml} -->";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the "<xsd:anyAttribute>" XML element that will be
        //       inserted in place of the token if the "relaxed" XML schema
        //       validation mode is enabled.  Any additional XSD attributes
        //       that may (eventually?) be necessary should be added here.
        //
        private static readonly string RelaxedElement =
            "<xsd:anyAttribute processContents=\"lax\" />";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Generic Xml Handling Methods
        public static bool CouldBeDocument(
            string path /* in: OPTIONAL */
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            string extension = PathOps.GetExtension(path);

            if (String.IsNullOrEmpty(extension))
                return false;

            if (SharedStringOps.Equals(extension,
                    FileExtension.Markup, PathOps.ComparisonType))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LooksLikeDocument(
            string text /* in: OPTIONAL */
            )
        {
            if (text == null)
                return false;

            string prefix = DocumentStart;

            if (prefix == null)
                return false;

            return text.StartsWith(
                prefix, SharedStringOps.SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool FileLooksLikeDocument(
            string fileName /* in: OPTIONAL */
            )
        {
            try
            {
                if (String.IsNullOrEmpty(fileName))
                    return false;

                if (PathOps.IsRemoteUri(fileName) ||
                    !File.Exists(fileName))
                {
                    return false;
                }

                long size = Size.Invalid;

                if (FileOps.GetFileSize(
                        fileName, ref size) != ReturnCode.Ok)
                {
                    return false;
                }

                string prefix = DocumentStart;

                if (prefix == null)
                    return false;

                int prefixLength = prefix.Length;

                if (size < prefixLength)
                    return false;

                Encoding encoding;
                int preambleSize = 0;

                encoding = Engine.GetEncoding(
                    fileName, EncodingType.Xml, null, ref preambleSize);

                if (encoding == null)
                    return false;

                using (FileStream stream = File.OpenRead(fileName))
                {
                    using (BinaryReader binaryReader = new BinaryReader(
                            stream))
                    {
                        //
                        // NOTE: If there is a preamble in the file
                        //       (e.g. a UTF-8 BOM), skip over it.
                        //
                        if (preambleSize > 0)
                        {
                            stream.Seek(
                                preambleSize, SeekOrigin.Begin);
                        }

                        return LooksLikeDocument(encoding.GetString(
                            binaryReader.ReadBytes(prefixLength)));
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldRetryError(
            Result error,             /* in: OPTIONAL */
            XmlErrorTypes errorType,  /* in */
            XmlErrorTypes retryTypes, /* in */
            bool @default             /* in */
            )
        {
            if ((errorType != XmlErrorTypes.None) &&
                FlagOps.HasFlags(retryTypes, errorType, true))
            {
                return true;
            }

            if (error == null)
            {
                if (FlagOps.HasFlags(
                        retryTypes, XmlErrorTypes.NoError, true))
                {
                    return true;
                }
                else
                {
                    return @default;
                }
            }

            Exception exception = error.Exception;

            if (exception == null)
            {
                if (FlagOps.HasFlags(
                        retryTypes, XmlErrorTypes.NoException, true))
                {
                    return true;
                }
                else
                {
                    return @default;
                }
            }

#if false
            if (exception is XsltException)
            {
                return FlagOps.HasFlags(
                    retryTypes, XmlErrorTypes.Xslt, true);
            }
#endif

            if (exception is XPathException)
            {
                return FlagOps.HasFlags(
                    retryTypes, XmlErrorTypes.Xpath, true);
            }

            if (exception is XmlSchemaException)
            {
                if (exception is XmlSchemaValidationException)
                {
                    return FlagOps.HasFlags(
                        retryTypes, XmlErrorTypes.Validate, true);
                }
                else
                {
                    return FlagOps.HasFlags(
                        retryTypes, XmlErrorTypes.Schema, true);
                }
            }

            if (exception is XmlException)
            {
                return FlagOps.HasFlags(
                    retryTypes, XmlErrorTypes.Generic, true);
            }

            return FlagOps.HasFlags(
                retryTypes, XmlErrorTypes.Unknown, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetEncoding(
            string fileName,      /* in */
            Assembly assembly,    /* in: OPTIONAL */
            string resourceName,  /* in: OPTIONAL */
            bool validate,        /* in */
            bool strict,          /* in */
            ref Encoding encoding /* out */
            )
        {
            Result error = null;

            return GetEncoding(
                fileName, assembly, resourceName, validate, strict,
                ref encoding, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetEncoding(
            string fileName,       /* in */
            Assembly assembly,     /* in: OPTIONAL */
            string resourceName,   /* in: OPTIONAL */
            bool validate,         /* in */
            bool strict,           /* in */
            ref Encoding encoding, /* out */
            ref Result error       /* out */
            )
        {
            XmlDocument document = null;

            if (LoadFile(
                    fileName, ref document,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (validate)
            {
                if (Validate(
                        assembly, resourceName, document,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            return GetEncoding(
                document, strict, ref encoding, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetEncoding(
            XmlDocument document,  /* in */
            bool strict,           /* in */
            ref Encoding encoding, /* out */
            ref Result error       /* out */
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            try
            {
                XmlDeclaration declaration =
                    document.FirstChild as XmlDeclaration;

                if (declaration == null)
                {
                    error = "invalid xml declaration";
                    return ReturnCode.Error;
                }

                string encodingName = declaration.Encoding;
                Encoding localEncoding;

                if (!String.IsNullOrEmpty(encodingName))
                {
                    localEncoding = StringOps.GetEncoding(
                        encodingName, ref error);

                    if (localEncoding != null)
                    {
                        encoding = localEncoding;
                        return ReturnCode.Ok;
                    }
                }
                else if (strict)
                {
                    error = "invalid encoding name";
                }
                else
                {
                    localEncoding = StringOps.GetEncoding(
                        EncodingType.Xml);

                    if (localEncoding != null)
                    {
                        encoding = localEncoding;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "invalid built-in encoding {0}",
                            FormatOps.WrapOrNull(EncodingType.Xml));
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadString(
            string xml,              /* in */
            ref XmlDocument document /* out */
            )
        {
            Result error = null;

            return LoadString(xml, ref document, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadString(
            string xml,               /* in */
            ref XmlDocument document, /* out */
            ref Result error          /* out */
            )
        {
            if (xml == null)
            {
                error = "invalid xml";
                return ReturnCode.Error;
            }

            try
            {
                document = new XmlDocument();
                document.LoadXml(xml); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadFile(
            string fileName,          /* in */
            ref XmlDocument document, /* out */
            ref Result error          /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            bool remoteUri = PathOps.IsRemoteUri(fileName);

            if (!remoteUri && !File.Exists(fileName))
            {
                error = String.Format(
                    "couldn't read file \"{0}\": no such file or directory",
                    fileName);

                return ReturnCode.Error;
            }

            try
            {
                document = new XmlDocument();
                document.Load(fileName); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SaveFile(
            string fileName,      /* in */
            XmlDocument document, /* in */
            ref Result error      /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            bool remoteUri = PathOps.IsRemoteUri(fileName);

            if (!remoteUri && File.Exists(fileName))
            {
                error = String.Format(
                    "couldn't write file \"{0}\": file already exists",
                    fileName);

                return ReturnCode.Error;
            }

            try
            {
                document.Save(fileName); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetNamespaceArrays(
            StringDictionary dictionary, /* in */
            CultureInfo cultureInfo,     /* in: OPTIONAL */
            out string[] namespaceNames, /* out */
            out Uri[] namespaceUris,     /* out */
            ref Result error             /* out */
            )
        {
            namespaceNames = null;
            namespaceUris = null;

            if (dictionary == null)
            {
                error = "invalid namespace dictionary";
                return ReturnCode.Error;
            }

            int count = dictionary.Count;

            namespaceNames = new string[count];
            namespaceUris = new Uri[count];

            int index = 0;

            foreach (NamespacePair pair in dictionary)
            {
                Uri uri = null;
                string uriString = pair.Value;

                if ((uriString != null) && (Value.GetUri(
                        uriString, UriKind.Absolute, cultureInfo,
                        ref uri, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                namespaceNames[index] = pair.Key;
                namespaceUris[index] = uri;

                index++;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetNamespaceManager(
            string namespaceName,                     /* in */
            Uri namespaceUri,                         /* in */
            XmlNameTable nameTable,                   /* in */
            ref XmlNamespaceManager namespaceManager, /* out */
            ref Result error                          /* out */
            )
        {
            string[] namespaceNames = { namespaceName };
            Uri[] namespaceUris = { namespaceUri };

            return GetNamespaceManager(
                namespaceNames, namespaceUris, nameTable,
                ref namespaceManager, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNamespaceManager(
            string[] namespaceNames,                  /* in */
            Uri[] namespaceUris,                      /* in */
            XmlNameTable nameTable,                   /* in */
            ref XmlNamespaceManager namespaceManager, /* out */
            ref Result error                          /* out */
            )
        {
            if (namespaceNames == null)
            {
                error = "invalid xml namespace names";
                return ReturnCode.Error;
            }

            if (namespaceUris == null)
            {
                error = "invalid xml namespace uris";
                return ReturnCode.Error;
            }

            if (nameTable == null)
            {
                error = "invalid xml name table";
                return ReturnCode.Error;
            }

            int length = namespaceNames.Length;

            if (length != namespaceUris.Length)
            {
                error = String.Format(
                    "namespace mismatch, have {0} names and {1} uris",
                    length, namespaceUris.Length);

                return ReturnCode.Error;
            }

            try
            {
                namespaceManager = new XmlNamespaceManager(nameTable);

                for (int index = 0; index < length; index++)
                {
                    string namespaceName = namespaceNames[index];

                    if (namespaceName == null)
                    {
                        error = String.Format(
                            "invalid namespace name at index {0}",
                            index);

                        return ReturnCode.Error;
                    }

                    Uri namespaceUri = namespaceUris[index];

                    if (namespaceUri == null)
                    {
                        error = String.Format(
                            "invalid namespace uri at index {0}",
                            index);

                        return ReturnCode.Error;
                    }

                    namespaceManager.AddNamespace(
                        namespaceName, namespaceUri.ToString());
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSchemaStream(
            string schemaXml,  /* in */
            bool relaxed,      /* in */
            ref Stream stream, /* out */
            ref Result error   /* out */
            )
        {
            try
            {
                if (schemaXml == null)
                {
                    error = "invalid schema xml";
                    return ReturnCode.Error;
                }

                Encoding encoding = StringOps.GetEncoding(
                    EncodingType.Xml);

                if (encoding == null)
                {
                    error = "invalid encoding";
                    return ReturnCode.Error;
                }

                if (relaxed)
                    EnableRelaxedSchema(ref schemaXml);

                stream = new MemoryStream(
                    encoding.GetBytes(schemaXml));

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Validate(
            string schemaXml,     /* in */
            XmlDocument document, /* in */
            bool relaxed,         /* in */
            ref Result error      /* out */
            )
        {
            if (schemaXml == null)
            {
                error = "invalid schema xml";
                return ReturnCode.Error;
            }

            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            Stream stream = null;

            try
            {
                if (GetSchemaStream(
                        schemaXml, relaxed, ref stream,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                try
                {
                    return Validate(
                        XmlSchema.Read(stream, null),
                        document, ref error);
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Validate(
            Assembly assembly,    /* in */
            string resourceName,  /* in */
            XmlDocument document, /* in */
            ref Result error      /* out */
            )
        {
            return Validate(
                assembly, resourceName, document,
                false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Validate(
            Assembly assembly,    /* in */
            string resourceName,  /* in */
            XmlDocument document, /* in */
            bool relaxed,         /* in */
            ref Result error      /* out */
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            Stream stream = null;

            try
            {
                if (GetSchemaStream(
                        assembly, resourceName,
                        relaxed, ref stream,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                try
                {
                    return Validate(
                        XmlSchema.Read(stream, null),
                        document, ref error); /* throw */
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode Validate(
            XmlSchema schema,     /* in */
            XmlDocument document, /* in */
            ref Result error      /* out */
            )
        {
            if (schema == null)
            {
                error = "invalid xml schema";
                return ReturnCode.Error;
            }

            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            try
            {
                document.Schemas.Add(schema); /* throw */
                document.Validate(null); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetNodeList(
            XmlDocument document,     /* in */
            string namespaceName,     /* in: OPTIONAL */
            Uri namespaceUri,         /* in: OPTIONAL */
            StringList xpaths,        /* in: OPTIONAL */
            ref XmlNodeList nodeList, /* out */
            ref Result error          /* out */
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            XmlNamespaceManager namespaceManager = null;

            if ((namespaceName != null) || (namespaceUri != null))
            {
                if (GetNamespaceManager(
                        namespaceName, namespaceUri, document.NameTable,
                        ref namespaceManager, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            return GetNodeList(
                document, namespaceManager, xpaths, ref nodeList, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetNodeList(
            XmlDocument document,     /* in */
            string[] namespaceNames,  /* in: OPTIONAL */
            Uri[] namespaceUris,      /* in: OPTIONAL */
            StringList xpaths,        /* in: OPTIONAL */
            ref XmlNodeList nodeList, /* out */
            ref Result error          /* out */
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            XmlNamespaceManager namespaceManager = null;

            if ((namespaceNames != null) || (namespaceUris != null))
            {
                if (GetNamespaceManager(
                        namespaceNames, namespaceUris, document.NameTable,
                        ref namespaceManager, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            return GetNodeList(
                document, namespaceManager, xpaths, ref nodeList, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNodeList(
            XmlDocument document,                 /* in */
            XmlNamespaceManager namespaceManager, /* in: OPTIONAL */
            StringList xpaths,                    /* in: OPTIONAL */
            ref XmlNodeList nodeList,             /* out */
            ref Result error                      /* out */
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            if (xpaths != null)
            {
                try
                {
                    foreach (string xpath in xpaths)
                    {
                        if (xpath == null)
                            continue;

                        if (namespaceManager != null)
                        {
                            nodeList = document.SelectNodes(
                                xpath, namespaceManager);
                        }
                        else
                        {
                            nodeList = document.SelectNodes(
                                xpath);
                        }

                        if ((nodeList == null) ||
                            (nodeList.Count == 0))
                        {
                            continue;
                        }

                        return ReturnCode.Ok;
                    }

                    error = "xml nodes not found";
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                try
                {
                    nodeList = document.SelectNodes(AllXPath);
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Semi-Generic Xml Handling Methods
        private static void EnableRelaxedSchema(
            ref string schemaXml /* in, out */
            )
        {
            StringBuilder builder = StringBuilderFactory.Create(
                schemaXml);

            builder.Replace(RelaxedToken, RelaxedElement);

            schemaXml = StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by the test "xml-1.3", please do not remove.
        //
        private static ReturnCode GetSchemaStream(
            Assembly assembly,   /* in: OPTIONAL */
            string resourceName, /* in: OPTIONAL */
            ref Stream stream,   /* out */
            ref Result error     /* out */
            )
        {
            return GetSchemaStream(
                assembly, resourceName, false, ref stream,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSchemaStream(
            Assembly assembly,   /* in: OPTIONAL */
            string resourceName, /* in: OPTIONAL */
            bool relaxed,        /* in */
            ref Stream stream,   /* out */
            ref Result error     /* out */
            )
        {
            try
            {
                if (assembly == null)
                    assembly = GlobalState.GetAssembly();

                if (resourceName == null)
                    resourceName = Xml.SchemaResourceName;

                Stream localStream = AssemblyOps.GetResourceStream(
                    assembly, resourceName, ref error);

                if (localStream == null)
                    return ReturnCode.Error;

                if (!relaxed)
                {
                    stream = localStream;
                    return ReturnCode.Ok;
                }

                using (StreamReader streamReader = new StreamReader(
                        localStream))
                {
                    return GetSchemaStream(
                        streamReader.ReadToEnd(), relaxed, ref stream,
                        ref error);
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Specific Xml Handling Methods
        public static ReturnCode GetAssemblyNamespaceManager(
            XmlNameTable nameTable,                   /* in */
            ref XmlNamespaceManager namespaceManager, /* out */
            ref Result error                          /* out */
            )
        {
            return GetNamespaceManager(
                Xml.NamespaceName, Xml.ScriptNamespaceUri,
                nameTable, ref namespaceManager,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetScriptBlockNodeList(
            XmlDocument document,     /* in */
            ref XmlNodeList nodeList, /* out */
            ref Result error          /* out */
            )
        {
            return GetNodeList(
                document, Xml.NamespaceName,
                Xml.ScriptNamespaceUri, Xml.XPathList,
                ref nodeList, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Serialization Methods
#if SERIALIZATION
        public static ReturnCode Serialize(
            object @object,                               /* in */
            Type type,                                    /* in */
            XmlSerializerNamespaces serializerNamespaces, /* in */
            ref byte[] bytes,                             /* out */
            ref Result error                              /* out */
            )
        {
            if (@object == null)
            {
                error = "invalid object";
                return ReturnCode.Error;
            }

            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (bytes != null)
            {
                error = "cannot overwrite valid byte array";
                return ReturnCode.Error;
            }

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    XmlSerializer serializer = new XmlSerializer(
                        type);

                    serializer.Serialize(
                        stream, @object,
                        serializerNamespaces);

                    serializer = null;

                    bytes = stream.ToArray();
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Serialize(
            object @object,                               /* in */
            Type type,                                    /* in */
            XmlWriter writer,                             /* in */
            XmlSerializerNamespaces serializerNamespaces, /* in */
            ref Result error                              /* out */
            )
        {
            if (@object == null)
            {
                error = "invalid object";
                return ReturnCode.Error;
            }

            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (writer == null)
            {
                error = "invalid xml writer";
                return ReturnCode.Error;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(
                    type);

                serializer.Serialize(
                    writer, @object, 
                    serializerNamespaces);

                serializer = null;

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Deserialize(
            Type type,          /* in */
            byte[] bytes,       /* in */
            ref object @object, /* out */
            ref Result error    /* out */
            )
        {
            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (bytes == null)
            {
                error = "invalid byte array";
                return ReturnCode.Error;
            }

            if (@object != null)
            {
                error = "cannot overwrite valid object";
                return ReturnCode.Error;
            }

            try
            {
                using (MemoryStream stream = new MemoryStream(
                        bytes))
                {
                    XmlSerializer serializer = new XmlSerializer(
                        type);

                    @object = serializer.Deserialize(stream);
                    serializer = null;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Deserialize(
            Type type,          /* in */
            XmlReader reader,   /* in */
            ref object @object, /* out */
            ref Result error    /* out */
            )
        {
            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (reader == null)
            {
                error = "invalid xml reader";
                return ReturnCode.Error;
            }

            if (@object != null)
            {
                error = "cannot overwrite valid object";
                return ReturnCode.Error;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(
                    type);

                @object = serializer.Deserialize(reader);
                serializer = null;

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif
        #endregion
    }
}
