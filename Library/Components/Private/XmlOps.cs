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
            string path
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
            string text
            )
        {
            return (text != null) && text.StartsWith(
                DocumentStart, SharedStringOps.SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldRetryError(
            Result error,
            XmlErrorTypes errorType,
            XmlErrorTypes retryTypes,
            bool @default
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
            string fileName,
            Assembly assembly,
            string resourceName,
            bool validate,
            bool strict,
            ref Encoding encoding
            )
        {
            Result error = null;

            return GetEncoding(
                fileName, assembly, resourceName, validate, strict,
                ref encoding, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetEncoding(
            string fileName,
            Assembly assembly,
            string resourceName,
            bool validate,
            bool strict,
            ref Encoding encoding,
            ref Result error
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
            XmlDocument document,
            bool strict,
            ref Encoding encoding,
            ref Result error
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
            string xml,
            ref XmlDocument document,
            ref Result error
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
            string fileName,
            ref XmlDocument document,
            ref Result error
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
            string fileName,
            XmlDocument document,
            ref Result error
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

        public static ReturnCode GetNamespaceManager(
            string namespaceName,
            Uri namespaceUri,
            XmlNameTable nameTable,
            ref XmlNamespaceManager namespaceManager,
            ref Result error
            )
        {
            if (nameTable == null)
            {
                error = "invalid xml name table";
                return ReturnCode.Error;
            }

            if (namespaceName == null)
            {
                error = "invalid xml namespace name";
                return ReturnCode.Error;
            }

            if (namespaceUri == null)
            {
                error = "invalid xml namespace uri";
                return ReturnCode.Error;
            }

            try
            {
                namespaceManager = new XmlNamespaceManager(nameTable);

                namespaceManager.AddNamespace(
                    namespaceName, namespaceUri.ToString());

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSchemaStream(
            string schemaXml,
            bool relaxed,
            ref Stream stream,
            ref Result error
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
            string schemaXml,
            XmlDocument document,
            bool relaxed,
            ref Result error
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
            Assembly assembly,
            string resourceName,
            XmlDocument document,
            ref Result error
            )
        {
            return Validate(
                assembly, resourceName, document,
                false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Validate(
            Assembly assembly,
            string resourceName,
            XmlDocument document,
            bool relaxed,
            ref Result error
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
            XmlSchema schema,
            XmlDocument document,
            ref Result error
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
            XmlDocument document,
            string namespaceName,
            Uri namespaceUri,
            StringList xpaths,
            ref XmlNodeList nodeList,
            ref Result error
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            if (xpaths == null)
            {
                error = "invalid xpath list";
                return ReturnCode.Error;
            }

            try
            {
                XmlNamespaceManager namespaceManager = null;

                if (GetNamespaceManager(
                        namespaceName, namespaceUri,
                        document.NameTable,
                        ref namespaceManager,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                foreach (string xpath in xpaths)
                {
                    if (xpath == null)
                        continue;

                    nodeList = document.SelectNodes(
                        xpath, namespaceManager);

                    if ((nodeList == null) ||
                        (nodeList.Count == 0))
                    {
                        continue;
                    }

                    return ReturnCode.Ok;
                }

                error = String.Format(
                    "{0} xml nodes not found",
                    namespaceName).TrimStart();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Semi-Generic Xml Handling Methods
        private static void EnableRelaxedSchema(
            ref string schemaXml
            )
        {
            StringBuilder builder = StringOps.NewStringBuilder(
                schemaXml);

            builder.Replace(RelaxedToken, RelaxedElement);

            schemaXml = builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by the test "xml-1.3", please do not remove.
        //
        private static ReturnCode GetSchemaStream(
            Assembly assembly,
            string resourceName,
            ref Stream stream,
            ref Result error
            )
        {
            return GetSchemaStream(
                assembly, resourceName, false, ref stream,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSchemaStream(
            Assembly assembly,
            string resourceName,
            bool relaxed,
            ref Stream stream,
            ref Result error
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
            XmlNameTable nameTable,
            ref XmlNamespaceManager namespaceManager,
            ref Result error
            )
        {
            return GetNamespaceManager(
                Xml.NamespaceName, Xml.ScriptNamespaceUri,
                nameTable, ref namespaceManager,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetScriptBlockNodeList(
            XmlDocument document,
            ref XmlNodeList nodeList,
            ref Result error
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
            object @object,
            Type type,
            XmlSerializerNamespaces serializerNamespaces,
            ref byte[] bytes,
            ref Result error
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
            object @object,
            Type type,
            XmlWriter writer,
            XmlSerializerNamespaces serializerNamespaces,
            ref Result error
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
            Type type,
            byte[] bytes,
            ref object @object,
            ref Result error
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
            Type type,
            XmlReader reader,
            ref object @object,
            ref Result error
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
