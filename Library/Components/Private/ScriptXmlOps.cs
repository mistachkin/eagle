/*
 * ScriptXmlOps.cs --
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
using System.Xml;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using XmlGetAttributeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Private.Delegates.XmlGetAttributeCallback>;

using XmlSetAttributeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Private.Delegates.XmlSetAttributeCallback>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the central support routines used to read and write
    /// the Eagle-specific XML attributes (and inner text) that describe a saved
    /// script block.  It maps between an <see cref="XmlElement" /> and the set
    /// of strongly typed attribute values (such as the block identifier, block
    /// type, name, group, description, time stamp, public key token, and
    /// signature), using per-attribute getter and setter callbacks together
    /// with helpers for escaping CDATA, populating dictionaries, and validating
    /// the configured callbacks.
    /// </summary>
    [ObjectId("79f7763f-aca9-40d0-ae37-bbd8afb9a4c7")]
    internal static class ScriptXmlOps
    {
        #region Private Constants
        /// <summary>
        /// The pair of markers used to escape and unescape the XML end-of-CDATA
        /// sequence.  The first element is the unescaped end-of-CDATA marker;
        /// the second element is its equivalent expressed using XML numeric
        /// character references.
        /// </summary>
        private static readonly string[] CDataEnd = {
            "]]>",               /* XML unescaped end-of-CData marker */
            "&#x5D;&#x5D;&#x3E;" /* XML numeric character references */
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        /// <summary>
        /// The object used to synchronize access to the static data of this
        /// class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The dictionary of per-attribute callbacks used to read XML attribute
        /// values, keyed by attribute name.
        /// </summary>
        private static XmlGetAttributeDictionary attributeGetters;

        /// <summary>
        /// The dictionary of per-attribute callbacks used to write XML attribute
        /// values, keyed by attribute name.
        /// </summary>
        private static XmlSetAttributeDictionary attributeSetters;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the XML end-of-CDATA marker is escaped when CDATA
        /// content is being written.
        /// </summary>
        private static bool EscapeCDataEnd = true; /* TODO: Good default? */

        /// <summary>
        /// When non-zero, the escaped XML end-of-CDATA marker is unescaped when
        /// CDATA content is being read.
        /// </summary>
        private static bool UnescapeCDataEnd = true; /* TODO: Good default? */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AttributeGetters Helper Class
        /// <summary>
        /// This class provides the per-attribute callbacks used to read the
        /// individual Eagle-specific XML attribute values from an
        /// <see cref="XmlElement" />.  Each method matches the
        /// <see cref="XmlGetAttributeCallback" /> delegate signature and is
        /// responsible for a single named attribute.
        /// </summary>
        [ObjectId("70fd0624-64a1-458d-a47b-fe729a516955")]
        private static class AttributeGetters
        {
            #region Private XmlGetAttributeCallback Methods
            /// <summary>
            /// This method gets the value of the identifier XML attribute,
            /// converting it to a <see cref="Guid" />.
            /// </summary>
            /// <param name="element">
            /// The XML element to read the attribute value from.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to read.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be present.
            /// </param>
            /// <param name="attributeValue">
            /// Upon success, receives the converted attribute value; otherwise,
            /// receives null.
            /// </param>
            /// <returns>
            /// True if the attribute value was read successfully (or was absent
            /// and not required); otherwise, false.
            /// </returns>
            private static bool Id(
                XmlElement element,       /* in */
                string attributeName,     /* in */
                bool required,            /* in */
                out object attributeValue /* out */
                )
            {
                if (!TryGetAttributeValue(
                        element, attributeName, out attributeValue))
                {
                    return !required;
                }

                attributeValue = new Guid((string)attributeValue); /* throw */
                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the value of the block type XML attribute,
            /// converting it to an <see cref="XmlBlockType" /> value.
            /// </summary>
            /// <param name="element">
            /// The XML element to read the attribute value from.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to read.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be present.
            /// </param>
            /// <param name="attributeValue">
            /// Upon success, receives the converted attribute value; otherwise,
            /// receives null.
            /// </param>
            /// <returns>
            /// True if the attribute value was read successfully (or was absent
            /// and not required); otherwise, false.
            /// </returns>
            private static bool BlockType(
                XmlElement element,       /* in */
                string attributeName,     /* in */
                bool required,            /* in */
                out object attributeValue /* out */
                )
            {
                if (!TryGetAttributeValue(
                        element, attributeName, out attributeValue))
                {
                    return !required;
                }

                attributeValue = Enum.Parse(
                    typeof(XmlBlockType), (string)attributeValue,
                    true); /* throw */

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the text value associated with the XML element,
            /// which is read from its inner text rather than from a named
            /// attribute.
            /// </summary>
            /// <param name="element">
            /// The XML element to read the inner text from.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to read.  This parameter is
            /// ignored.
            /// </param>
            /// <param name="required">
            /// Non-zero if the value must be present.
            /// </param>
            /// <param name="attributeValue">
            /// Upon success, receives the inner text value; otherwise, receives
            /// null.
            /// </param>
            /// <returns>
            /// True if the value was read successfully (or was absent and not
            /// required); otherwise, false.
            /// </returns>
            private static bool Text(
                XmlElement element,       /* in */
                string attributeName,     /* in: IGNORED */
                bool required,            /* in */
                out object attributeValue /* out */
                )
            {
                if (!TryGetAttributeValue(
                        element, null, out attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the value of the name XML attribute.
            /// </summary>
            /// <param name="element">
            /// The XML element to read the attribute value from.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to read.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be present.
            /// </param>
            /// <param name="attributeValue">
            /// Upon success, receives the attribute value; otherwise, receives
            /// null.
            /// </param>
            /// <returns>
            /// True if the attribute value was read successfully (or was absent
            /// and not required); otherwise, false.
            /// </returns>
            private static bool Name(
                XmlElement element,       /* in */
                string attributeName,     /* in */
                bool required,            /* in */
                out object attributeValue /* out */
                )
            {
                if (!TryGetAttributeValue(
                        element, attributeName, out attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the value of the group XML attribute.
            /// </summary>
            /// <param name="element">
            /// The XML element to read the attribute value from.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to read.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be present.
            /// </param>
            /// <param name="attributeValue">
            /// Upon success, receives the attribute value; otherwise, receives
            /// null.
            /// </param>
            /// <returns>
            /// True if the attribute value was read successfully (or was absent
            /// and not required); otherwise, false.
            /// </returns>
            private static bool Group(
                XmlElement element,       /* in */
                string attributeName,     /* in */
                bool required,            /* in */
                out object attributeValue /* out */
                )
            {
                if (!TryGetAttributeValue(
                        element, attributeName, out attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the value of the description XML attribute.
            /// </summary>
            /// <param name="element">
            /// The XML element to read the attribute value from.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to read.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be present.
            /// </param>
            /// <param name="attributeValue">
            /// Upon success, receives the attribute value; otherwise, receives
            /// null.
            /// </param>
            /// <returns>
            /// True if the attribute value was read successfully (or was absent
            /// and not required); otherwise, false.
            /// </returns>
            private static bool Description(
                XmlElement element,       /* in */
                string attributeName,     /* in */
                bool required,            /* in */
                out object attributeValue /* out */
                )
            {
                if (!TryGetAttributeValue(
                        element, attributeName, out attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the value of the time stamp XML attribute,
            /// parsing it and converting it to a coordinated universal time
            /// (UTC) <see cref="DateTime" /> value.
            /// </summary>
            /// <param name="element">
            /// The XML element to read the attribute value from.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to read.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be present.
            /// </param>
            /// <param name="attributeValue">
            /// Upon success, receives the parsed attribute value; otherwise,
            /// receives null.
            /// </param>
            /// <returns>
            /// True if the attribute value was read and parsed successfully (or
            /// was absent and not required); otherwise, false.
            /// </returns>
            private static bool TimeStamp(
                XmlElement element,       /* in */
                string attributeName,     /* in */
                bool required,            /* in */
                out object attributeValue /* out */
                )
            {
                if (!TryGetAttributeValue(
                        element, attributeName, out attributeValue))
                {
                    return !required;
                }

                DateTime timeStamp;

                if (!DateTime.TryParse(
                        (string)attributeValue, out timeStamp))
                {
                    return false;
                }

                attributeValue = timeStamp.ToUniversalTime();
                return true;

            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the value of the public key token XML
            /// attribute.
            /// </summary>
            /// <param name="element">
            /// The XML element to read the attribute value from.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to read.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be present.
            /// </param>
            /// <param name="attributeValue">
            /// Upon success, receives the attribute value; otherwise, receives
            /// null.
            /// </param>
            /// <returns>
            /// True if the attribute value was read successfully (or was absent
            /// and not required); otherwise, false.
            /// </returns>
            private static bool PublicKeyToken(
                XmlElement element,       /* in */
                string attributeName,     /* in */
                bool required,            /* in */
                out object attributeValue /* out */
                )
            {
                if (!TryGetAttributeValue(
                        element, attributeName, out attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the value of the signature XML attribute,
            /// converting it from its base64 text form to a byte array.
            /// </summary>
            /// <param name="element">
            /// The XML element to read the attribute value from.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to read.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be present.
            /// </param>
            /// <param name="attributeValue">
            /// Upon success, receives the converted attribute value; otherwise,
            /// receives null.
            /// </param>
            /// <returns>
            /// True if the attribute value was read successfully (or was absent
            /// and not required); otherwise, false.
            /// </returns>
            private static bool Signature(
                XmlElement element,       /* in */
                string attributeName,     /* in */
                bool required,            /* in */
                out object attributeValue /* out */
                )
            {
                if (!TryGetAttributeValue(
                        element, attributeName, out attributeValue))
                {
                    return !required;
                }

                attributeValue = Convert.FromBase64String(
                    (string)attributeValue); /* throw */

                return true;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            /// <summary>
            /// This method initializes the array of attribute getter callbacks,
            /// in the canonical attribute order, used to read the
            /// Eagle-specific XML attribute values.
            /// </summary>
            /// <param name="callbacks">
            /// Upon return, receives the newly created array of attribute getter
            /// callbacks.
            /// </param>
            public static void InitializeCallbacksArray(
                out XmlGetAttributeCallback[] callbacks /* out */
                )
            {
                callbacks = new XmlGetAttributeCallback[] {
                    Id,             /* REQUIRED */
                    BlockType,      /* REQUIRED */
                    Text,           /* REQUIRED */
                    Name,           /* OPTIONAL */
                    Group,          /* OPTIONAL */
                    Description,    /* OPTIONAL */
                    TimeStamp,      /* OPTIONAL */
                    PublicKeyToken, /* OPTIONAL */
                    Signature       /* OPTIONAL */
                };
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AttributeSetters Helper Class
        /// <summary>
        /// This class provides the per-attribute callbacks used to write the
        /// individual Eagle-specific XML attribute values to an
        /// <see cref="XmlElement" />.  Each method matches the
        /// <see cref="XmlSetAttributeCallback" /> delegate signature and is
        /// responsible for a single named attribute.
        /// </summary>
        [ObjectId("f5caac0d-7e49-432c-b646-f447357e5366")]
        private static class AttributeSetters
        {
            #region Private XmlSetAttributeCallback Methods
            /// <summary>
            /// This method sets the value of the identifier XML attribute,
            /// which must be a <see cref="Guid" />.
            /// </summary>
            /// <param name="element">
            /// The XML element to write the attribute value to.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to write.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be written successfully.
            /// </param>
            /// <param name="attributeValue">
            /// The attribute value to write.
            /// </param>
            /// <returns>
            /// True if the attribute value was written successfully (or could be
            /// omitted because it was not required); otherwise, false.
            /// </returns>
            private static bool Id(
                XmlElement element,   /* in */
                string attributeName, /* in */
                bool required,        /* in */
                object attributeValue /* in */
                )
            {
                if (!(attributeValue is Guid))
                    return false;

                if (!TrySetAttributeValue(
                        element, attributeName, attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method sets the value of the block type XML attribute,
            /// which must be an <see cref="XmlBlockType" /> value.
            /// </summary>
            /// <param name="element">
            /// The XML element to write the attribute value to.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to write.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be written successfully.
            /// </param>
            /// <param name="attributeValue">
            /// The attribute value to write.
            /// </param>
            /// <returns>
            /// True if the attribute value was written successfully (or could be
            /// omitted because it was not required); otherwise, false.
            /// </returns>
            private static bool BlockType(
                XmlElement element,   /* in */
                string attributeName, /* in */
                bool required,        /* in */
                object attributeValue /* in */
                )
            {
                if (!(attributeValue is XmlBlockType))
                    return false;

                if (!TrySetAttributeValue(
                        element, attributeName, attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method sets the text value associated with the XML element,
            /// which must be a string and is written as the element's inner text
            /// rather than to a named attribute.
            /// </summary>
            /// <param name="element">
            /// The XML element to write the inner text to.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to write.  This parameter is
            /// ignored.
            /// </param>
            /// <param name="required">
            /// Non-zero if the value must be written successfully.
            /// </param>
            /// <param name="attributeValue">
            /// The value to write.
            /// </param>
            /// <returns>
            /// True if the value was written successfully (or could be omitted
            /// because it was not required); otherwise, false.
            /// </returns>
            private static bool Text(
                XmlElement element,   /* in */
                string attributeName, /* in: IGNORED */
                bool required,        /* in */
                object attributeValue /* in */
                )
            {
                if (!(attributeValue is string))
                    return false;

                if (!TrySetAttributeValue(
                        element, null, attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method sets the value of the name XML attribute, which must
            /// be a string.
            /// </summary>
            /// <param name="element">
            /// The XML element to write the attribute value to.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to write.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be written successfully.
            /// </param>
            /// <param name="attributeValue">
            /// The attribute value to write.
            /// </param>
            /// <returns>
            /// True if the attribute value was written successfully (or could be
            /// omitted because it was not required); otherwise, false.
            /// </returns>
            private static bool Name(
                XmlElement element,   /* in */
                string attributeName, /* in */
                bool required,        /* in */
                object attributeValue /* in */
                )
            {
                if (!(attributeValue is string))
                    return false;

                if (!TrySetAttributeValue(
                        element, attributeName, attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method sets the value of the group XML attribute, which
            /// must be a string.
            /// </summary>
            /// <param name="element">
            /// The XML element to write the attribute value to.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to write.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be written successfully.
            /// </param>
            /// <param name="attributeValue">
            /// The attribute value to write.
            /// </param>
            /// <returns>
            /// True if the attribute value was written successfully (or could be
            /// omitted because it was not required); otherwise, false.
            /// </returns>
            private static bool Group(
                XmlElement element,   /* in */
                string attributeName, /* in */
                bool required,        /* in */
                object attributeValue /* in */
                )
            {
                if (!(attributeValue is string))
                    return false;

                if (!TrySetAttributeValue(
                        element, attributeName, attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method sets the value of the description XML attribute,
            /// which must be a string.
            /// </summary>
            /// <param name="element">
            /// The XML element to write the attribute value to.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to write.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be written successfully.
            /// </param>
            /// <param name="attributeValue">
            /// The attribute value to write.
            /// </param>
            /// <returns>
            /// True if the attribute value was written successfully (or could be
            /// omitted because it was not required); otherwise, false.
            /// </returns>
            private static bool Description(
                XmlElement element,   /* in */
                string attributeName, /* in */
                bool required,        /* in */
                object attributeValue /* in */
                )
            {
                if (!(attributeValue is string))
                    return false;

                if (!TrySetAttributeValue(
                        element, attributeName, attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method sets the value of the time stamp XML attribute,
            /// which must be a <see cref="DateTime" /> value.
            /// </summary>
            /// <param name="element">
            /// The XML element to write the attribute value to.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to write.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be written successfully.
            /// </param>
            /// <param name="attributeValue">
            /// The attribute value to write.
            /// </param>
            /// <returns>
            /// True if the attribute value was written successfully (or could be
            /// omitted because it was not required); otherwise, false.
            /// </returns>
            private static bool TimeStamp(
                XmlElement element,   /* in */
                string attributeName, /* in */
                bool required,        /* in */
                object attributeValue /* in */
                )
            {
                if (!(attributeValue is DateTime))
                    return false;

                if (!TrySetAttributeValue(
                        element, attributeName, attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method sets the value of the public key token XML
            /// attribute, which must be a string.
            /// </summary>
            /// <param name="element">
            /// The XML element to write the attribute value to.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to write.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be written successfully.
            /// </param>
            /// <param name="attributeValue">
            /// The attribute value to write.
            /// </param>
            /// <returns>
            /// True if the attribute value was written successfully (or could be
            /// omitted because it was not required); otherwise, false.
            /// </returns>
            private static bool PublicKeyToken(
                XmlElement element,   /* in */
                string attributeName, /* in */
                bool required,        /* in */
                object attributeValue /* in */
                )
            {
                if (!(attributeValue is string))
                    return false;

                if (!TrySetAttributeValue(
                        element, attributeName, attributeValue))
                {
                    return !required;
                }

                return true;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method sets the value of the signature XML attribute, which
            /// must be a byte array and is written in its base64 text form.
            /// </summary>
            /// <param name="element">
            /// The XML element to write the attribute value to.
            /// </param>
            /// <param name="attributeName">
            /// The name of the XML attribute to write.
            /// </param>
            /// <param name="required">
            /// Non-zero if the attribute must be written successfully.
            /// </param>
            /// <param name="attributeValue">
            /// The attribute value to write.
            /// </param>
            /// <returns>
            /// True if the attribute value was written successfully (or could be
            /// omitted because it was not required); otherwise, false.
            /// </returns>
            private static bool Signature(
                XmlElement element,   /* in */
                string attributeName, /* in */
                bool required,        /* in */
                object attributeValue /* in */
                )
            {
                if (!(attributeValue is byte[]))
                    return false;

                if (!TrySetAttributeValue(
                        element, attributeName, attributeValue))
                {
                    return !required;
                }

                return true;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            /// <summary>
            /// This method initializes the array of attribute setter callbacks,
            /// in the canonical attribute order, used to write the
            /// Eagle-specific XML attribute values.
            /// </summary>
            /// <param name="callbacks">
            /// Upon return, receives the newly created array of attribute setter
            /// callbacks.
            /// </param>
            public static void InitializeCallbacksArray(
                out XmlSetAttributeCallback[] callbacks /* out */
                )
            {
                callbacks = new XmlSetAttributeCallback[] {
                    Id,             /* REQUIRED */
                    BlockType,      /* REQUIRED */
                    Text,           /* REQUIRED */
                    Name,           /* OPTIONAL */
                    Group,          /* OPTIONAL */
                    Description,    /* OPTIONAL */
                    TimeStamp,      /* OPTIONAL */
                    PublicKeyToken, /* OPTIONAL */
                    Signature       /* OPTIONAL */
                };
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        #region Array Support Methods
        /// <summary>
        /// This method initializes the array of supported XML attribute names,
        /// in the canonical attribute order.
        /// </summary>
        /// <param name="names">
        /// Upon return, receives the newly created array of attribute names.
        /// </param>
        private static void InitializeNamesArray(
            out string[] names /* out */
            )
        {
            names = new string[] {
                _XmlAttribute.Id,             /* REQUIRED */
                _XmlAttribute.Type,           /* REQUIRED */
                _XmlAttribute.Text,           /* REQUIRED */
                _XmlAttribute.Name,           /* OPTIONAL */
                _XmlAttribute.Group,          /* OPTIONAL */
                _XmlAttribute.Description,    /* OPTIONAL */
                _XmlAttribute.TimeStamp,      /* OPTIONAL */
                _XmlAttribute.PublicKeyToken, /* OPTIONAL */
                _XmlAttribute.Signature       /* OPTIONAL */
            };
        }

        ///////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the array of attribute callbacks by invoking
        /// the specified initialization callback, if any.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the attribute callbacks contained in the array.
        /// </typeparam>
        /// <param name="callback">
        /// The callback used to initialize the array of attribute callbacks.
        /// This parameter may be null.
        /// </param>
        /// <param name="callbacks">
        /// Upon return, receives the array of attribute callbacks, or null if no
        /// initialization callback was supplied.
        /// </param>
        private static void InitializeCallbacksArray<T>(
            XmlInitializeArrayCallback<T> callback, /* in */
            out T[] callbacks                       /* out */
            )
        {
            if (callback != null)
                callback(out callbacks);
            else
                callbacks = null;
        }

        ///////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes both the array of supported XML attribute
        /// names and the corresponding array of attribute callbacks.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the attribute callbacks contained in the array.
        /// </typeparam>
        /// <param name="callback">
        /// The callback used to initialize the array of attribute callbacks.
        /// This parameter may be null.
        /// </param>
        /// <param name="names">
        /// Upon return, receives the newly created array of attribute names.
        /// </param>
        /// <param name="callbacks">
        /// Upon return, receives the array of attribute callbacks, or null if no
        /// initialization callback was supplied.
        /// </param>
        private static void InitializeArrays<T>(
            XmlInitializeArrayCallback<T> callback, /* in */
            out string[] names,                     /* out */
            out T[] callbacks                       /* out */
            )
        {
            InitializeNamesArray(out names);
            InitializeCallbacksArray<T>(callback, out callbacks);
        }

        ///////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies that the array of attribute names and the array
        /// of attribute callbacks are both present and have matching lengths.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the attribute callbacks contained in the array.
        /// </typeparam>
        /// <param name="names">
        /// The array of attribute names to check.
        /// </param>
        /// <param name="callbacks">
        /// The array of attribute callbacks to check.
        /// </param>
        /// <param name="length">
        /// Upon success, receives the common length of both arrays.
        /// </param>
        /// <returns>
        /// True if both arrays are present and have matching lengths; otherwise,
        /// false.
        /// </returns>
        private static bool CheckArrays<T>(
            string[] names, /* in */
            T[] callbacks,  /* in */
            ref int length  /* out */
            )
        {
            if (names == null)
                return false;

            if (callbacks == null)
                return false;

            length = names.Length;

            if (length != callbacks.Length)
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified XML attribute name is
        /// one of the supported attribute names, initializing the array of
        /// supported names on demand.
        /// </summary>
        /// <param name="names">
        /// The array of supported attribute names.  If null, it is initialized
        /// by this method and returned to the caller.
        /// </param>
        /// <param name="name">
        /// The attribute name to check.
        /// </param>
        /// <returns>
        /// True if the specified attribute name is supported; otherwise, false.
        /// </returns>
        private static bool IsSupported(
            ref string[] names, /* in, out */
            string name         /* in */
            )
        {
            if (names == null)
                InitializeNamesArray(out names);

            return (names != null) ?
                Array.IndexOf(names, name) != Index.Invalid : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////

        #region Dictionary Support Methods
        /// <summary>
        /// This method initializes the specified dictionary of attribute
        /// callbacks, keyed by attribute name, creating it if necessary and
        /// populating it from the supported attribute names and callbacks.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the attribute callbacks contained in the dictionary.
        /// </typeparam>
        /// <param name="callback">
        /// The callback used to initialize the array of attribute callbacks.
        /// This parameter may be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary of attribute callbacks to initialize.  If null, it is
        /// created by this method and returned to the caller.
        /// </param>
        /// <param name="force">
        /// Non-zero to (re-)initialize the dictionary even when it has already
        /// been populated.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to overwrite any pre-existing entries with the same
        /// attribute name.
        /// </param>
        private static void InitializeDictionary<T>(
            XmlInitializeArrayCallback<T> callback, /* in */
            ref Dictionary<string, T> dictionary,   /* in, out */
            bool force,                             /* in */
            bool overwrite                          /* in */
            )
        {
            if (!force && (dictionary != null))
                return;

            if (dictionary == null)
                dictionary = new Dictionary<string, T>();

            string[] names;
            T[] callbacks;

            InitializeArrays<T>(callback, out names, out callbacks);

            int length = Length.Invalid;

            if (CheckArrays<T>(names, callbacks, ref length))
            {
                for (int index = 0; index < length; index++)
                {
                    string name = names[index];

                    if (name == null)
                        continue;

                    if (!overwrite && dictionary.ContainsKey(name))
                        continue;

                    dictionary[name] = callbacks[index];
                }
            }
        }

        ///////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        /// <summary>
        /// This method determines whether the two specified attribute callbacks
        /// refer to the same delegate.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the attribute callbacks being compared.
        /// </typeparam>
        /// <param name="callback1">
        /// The first attribute callback to compare.
        /// </param>
        /// <param name="callback2">
        /// The second attribute callback to compare.
        /// </param>
        /// <returns>
        /// True if both callbacks refer to the same delegate; otherwise, false.
        /// </returns>
        private static bool MatchDelegates<T>(
            T callback1, /* in */
            T callback2  /* in */
            )
        {
            Delegate delegate1 = callback1 as Delegate;
            Delegate delegate2 = callback2 as Delegate;

            return delegate1 == delegate2; /* NOTE: Delegate operator. */
        }

        ///////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks the specified dictionary of attribute callbacks
        /// against the expected supported attribute names and callbacks,
        /// reporting any missing, mismatched, or extra entries.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the attribute callbacks contained in the dictionary.
        /// </typeparam>
        /// <param name="callback">
        /// The callback used to initialize the array of expected attribute
        /// callbacks.  This parameter may be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary of attribute callbacks to check.
        /// </param>
        /// <param name="list">
        /// The list of human-readable diagnostic messages to populate.  If null,
        /// it is created by this method and returned to the caller.
        /// </param>
        /// <param name="errors">
        /// The list of errors to populate upon failure.  If null, it is created
        /// by this method and returned to the caller.
        /// </param>
        /// <returns>
        /// True if the dictionary and expected arrays were able to be checked;
        /// otherwise, false.
        /// </returns>
        private static bool CheckDictionary<T>(
            XmlInitializeArrayCallback<T> callback, /* in */
            Dictionary<string, T> dictionary,       /* in */
            ref StringList list,                    /* in, out */
            ref ResultList errors                   /* in, out */
            )
        {
            if (dictionary == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "dictionary {0} not available", typeof(T)));

                return false;
            }

            string[] names;
            T[] callbacks;

            InitializeArrays<T>(callback, out names, out callbacks);

            int length = Length.Invalid;

            if (!CheckArrays<T>(names, callbacks, ref length))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "check of {0} arrays failed", typeof(T)));

                return false;
            }

            bool success = true;

            if (list == null)
                list = new StringList();

            for (int index = 0; index < length; index++)
            {
                string name = names[index];

                if (name == null)
                    continue;

                T localCallback;

                if (!dictionary.TryGetValue(
                        name, out localCallback))
                {
                    list.Add(String.Format("missing {0}",
                        FormatOps.WrapOrNull(name)));

                    success = false;
                }
                else if (!MatchDelegates<T>(
                        localCallback, callbacks[index]))
                {
                    list.Add(String.Format("mismatch {0}: {1}",
                        FormatOps.WrapOrNull(name),
                        FormatOps.DelegateName(
                            localCallback as Delegate)));

                    success = false;
                }
            }

            foreach (KeyValuePair<string, T> pair in dictionary)
            {
                string name = pair.Key;

                if (name == null)
                    continue;

                if (Array.IndexOf(names, name) == Index.Invalid)
                {
                    list.Add(String.Format("extra {0}: {1}",
                        FormatOps.WrapOrNull(name),
                        FormatOps.DelegateName(
                            pair.Value as Delegate)));

                    success = false;
                }
            }

            if (success)
                list.Add("ok");

            return true;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region XmlAttributeListType Support Methods
        /// <summary>
        /// This method gets the human-readable name fragment associated with the
        /// specified attribute list type, for use in diagnostic messages.
        /// </summary>
        /// <param name="listType">
        /// The attribute list type to get the name fragment for.
        /// </param>
        /// <returns>
        /// The name fragment for the specified attribute list type, or null if
        /// the list type is not recognized.
        /// </returns>
        private static string GetAttributeListName(
            XmlAttributeListType listType /* in */
            )
        {
            switch (listType)
            {
                case XmlAttributeListType.Engine:
                    {
                        return "engine ";
                    }
                case XmlAttributeListType.Required:
                    {
                        return "required ";
                    }
                case XmlAttributeListType.All:
                    {
                        return String.Empty;
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the list of XML attribute names associated with the
        /// specified attribute list type.
        /// </summary>
        /// <param name="listType">
        /// The attribute list type to get the attribute names for.
        /// </param>
        /// <returns>
        /// The list of attribute names for the specified attribute list type,
        /// or null if the list type is not recognized.
        /// </returns>
        private static StringList GetAttributeNames(
            XmlAttributeListType listType /* in */
            )
        {
            switch (listType)
            {
                case XmlAttributeListType.Engine:
                    {
                        return _XmlAttribute.EngineList;
                    }
                case XmlAttributeListType.Required:
                    {
                        return _XmlAttribute.RequiredList;
                    }
                case XmlAttributeListType.All:
                    {
                        return _XmlAttribute.AllList;
                    }
                default:
                    {
                        TraceOps.DebugTrace(String.Format(
                            "GetAttributeNames: unknown type {0}",
                            FormatOps.WrapOrNull(listType)),
                            typeof(ScriptXmlOps).Name,
                            TracePriority.ScriptError2);

                        return null;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified XML element has all of
        /// the attributes associated with the specified attribute list type.
        /// </summary>
        /// <param name="element">
        /// The XML element to check.
        /// </param>
        /// <param name="listType">
        /// The attribute list type whose attribute names must be present.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the missing attribute or
        /// other error.
        /// </param>
        /// <returns>
        /// True if the element has all of the required attributes; otherwise,
        /// false.
        /// </returns>
        private static bool HasAttributeNames(
            XmlElement element,            /* in */
            XmlAttributeListType listType, /* in */
            ref Result error               /* out */
            )
        {
            return HasAttributeNames(
                element, GetAttributeNames(listType), listType,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified XML element has all of
        /// the specified attributes, treating an attribute name that maps to the
        /// inner text specially.
        /// </summary>
        /// <param name="element">
        /// The XML element to check.
        /// </param>
        /// <param name="attributeNames">
        /// The list of attribute names that must be present.
        /// </param>
        /// <param name="listType">
        /// The attribute list type, used when formatting diagnostic messages.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the missing attribute or
        /// other error.
        /// </param>
        /// <returns>
        /// True if the element has all of the specified attributes; otherwise,
        /// false.
        /// </returns>
        private static bool HasAttributeNames(
            XmlElement element,            /* in */
            StringList attributeNames,     /* in */
            XmlAttributeListType listType, /* in */
            ref Result error               /* out */
            )
        {
            if (element == null)
            {
                error = "invalid xml element";
                return false;
            }

            if (attributeNames == null)
            {
                error = String.Format(
                    "{0}xml attribute names not available",
                    GetAttributeListName(listType));

                return false;
            }

            foreach (string attributeName in attributeNames)
            {
                if (IsInnerTextAttributeName(attributeName))
                {
                    string innerText = element.InnerText;

                    if (!IsMissingInnerText(innerText))
                        continue;
                }
                else if (attributeName == null)
                {
                    continue;
                }
                else if (element.HasAttribute(attributeName))
                {
                    continue;
                }

                error = String.Format(
                    "missing {0}xml attribute {1}",
                    GetAttributeListName(listType),
                    FormatOps.WrapOrNull(attributeName));

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified attribute name belongs
        /// to the set of attribute names associated with the specified attribute
        /// list type.
        /// </summary>
        /// <param name="attributeName">
        /// The attribute name to check.
        /// </param>
        /// <param name="listType">
        /// The attribute list type whose attribute names are checked.
        /// </param>
        /// <returns>
        /// True if the specified attribute name belongs to the attribute list
        /// type; otherwise, false.
        /// </returns>
        private static bool IsAttributeName(
            string attributeName,         /* in */
            XmlAttributeListType listType /* in */
            )
        {
            StringList attributeNames = GetAttributeNames(listType);

            if (attributeNames == null)
                return false;

            return attributeNames.Contains(attributeName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified attribute name is a
        /// required attribute for the specified attribute list type.  The
        /// <c>All</c> list type is treated as the <c>Required</c> list type for
        /// the purposes of this check.
        /// </summary>
        /// <param name="attributeName">
        /// The attribute name to check.
        /// </param>
        /// <param name="listType">
        /// The attribute list type whose required attribute names are checked.
        /// </param>
        /// <returns>
        /// True if the specified attribute name is required; otherwise, false.
        /// </returns>
        private static bool IsRequiredAttributeName(
            string attributeName,         /* in */
            XmlAttributeListType listType /* in */
            )
        {
            if (listType == XmlAttributeListType.All)
                listType = XmlAttributeListType.Required;

            return IsAttributeName(attributeName, listType);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Attribute Value Helper Methods
        /// <summary>
        /// This method determines whether the specified attribute name refers to
        /// the value stored in the inner text of an XML element rather than in a
        /// named attribute.  A null attribute name is treated as referring to
        /// the inner text.
        /// </summary>
        /// <param name="attributeName">
        /// The attribute name to check.
        /// </param>
        /// <returns>
        /// True if the specified attribute name refers to the inner text;
        /// otherwise, false.
        /// </returns>
        private static bool IsInnerTextAttributeName(
            string attributeName /* in */
            )
        {
            if (attributeName == null)
                return true;

            if (SharedStringOps.Equals(
                    attributeName, _XmlAttribute.Text,
                    StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified inner text value is
        /// considered missing (that is, null or empty).
        /// </summary>
        /// <param name="innerText">
        /// The inner text value to check.
        /// </param>
        /// <returns>
        /// True if the inner text value is missing; otherwise, false.
        /// </returns>
        private static bool IsMissingInnerText(
            string innerText /* in */
            )
        {
            //
            // TODO: Apparently, the InnerText property of an XmlElement
            //       cannot be null.
            //
            return String.IsNullOrEmpty(innerText);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified attribute value to its XML string
        /// representation, using a type-specific format for block type, time
        /// stamp, and byte array values.
        /// </summary>
        /// <param name="attributeValue">
        /// The attribute value to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The string representation of the specified attribute value.
        /// </returns>
        private static string AttributeValueToString(
            object attributeValue /* in */
            )
        {
            string stringValue;

            if (attributeValue is XmlBlockType)
            {
                stringValue = attributeValue.ToString().ToLowerInvariant();
            }
            else if (attributeValue is DateTime)
            {
                stringValue = FormatOps.Iso8601FullDateTime(
                    MarshalOps.ToDateTimeInKind((DateTime)attributeValue,
                    DateTimeKind.Utc, true));
            }
            else if (attributeValue is byte[])
            {
                stringValue = Convert.ToBase64String(
                    (byte[])attributeValue,
                    Base64FormattingOptions.InsertLineBreaks);
            }
            else
            {
                stringValue = StringOps.GetStringFromObject(attributeValue);
            }

            return stringValue;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method escapes or unescapes the XML end-of-CDATA marker within
        /// the specified string value, subject to the configured escape and
        /// unescape settings.
        /// </summary>
        /// <param name="stringValue">
        /// The string value to escape or unescape.  This parameter may be null
        /// or empty.
        /// </param>
        /// <param name="escape">
        /// Non-zero to escape the end-of-CDATA marker; zero to unescape it.
        /// </param>
        /// <returns>
        /// The escaped or unescaped string value, or the original value if no
        /// transformation was applicable.
        /// </returns>
        private static string EscapeOrUnescapeCData(
            string stringValue, /* in */
            bool escape         /* in */
            )
        {
            if (String.IsNullOrEmpty(stringValue))
                return stringValue;

            string oldValue;
            string newValue;

            if (escape)
            {
                if (!EscapeCDataEnd)
                    return stringValue;

                oldValue = CDataEnd[0];
                newValue = CDataEnd[1];
            }
            else
            {
                if (!UnescapeCDataEnd)
                    return stringValue;

                oldValue = CDataEnd[1];
                newValue = CDataEnd[0];
            }

            if (String.IsNullOrEmpty(oldValue))
                return stringValue;

            return stringValue.Replace(oldValue, newValue);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Attribute Value Callback Helper Methods
        /// <summary>
        /// This method gets the value of a single named XML attribute by looking
        /// up and invoking its registered getter callback.
        /// </summary>
        /// <param name="element">
        /// The XML element to read the attribute value from.
        /// </param>
        /// <param name="attributeName">
        /// The name of the XML attribute to read.
        /// </param>
        /// <param name="required">
        /// Non-zero if the attribute must be present.
        /// </param>
        /// <param name="attributeValue">
        /// Upon success, receives the attribute value produced by the callback.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the attribute value was obtained successfully; otherwise,
        /// false.
        /// </returns>
        private static bool TryGetAttributeValueViaCallback(
            XmlElement element,        /* in */
            string attributeName,      /* in */
            bool required,             /* in */
            ref object attributeValue, /* out */
            ref Result error           /* out */
            )
        {
            XmlGetAttributeCallback callback;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (attributeGetters == null)
                {
                    error = "script xml attribute getters not available";
                    return false;
                }

                if ((attributeName == null) || !attributeGetters.TryGetValue(
                        attributeName, out callback))
                {
                    error = String.Format(
                        "unrecognized script xml attribute {0}",
                        FormatOps.WrapOrNull(attributeName));

                    return false;
                }

                if (callback == null)
                {
                    error = String.Format(
                        "forbidden script xml attribute {0}",
                        FormatOps.WrapOrNull(attributeName));

                    return false;
                }
            }

            bool success = false;
            object localAttributeValue = null;

            try
            {
                if (callback(
                        element, attributeName, required,
                        out localAttributeValue)) /* throw */
                {
                    attributeValue = localAttributeValue;
                    success = true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ScriptXmlOps).Name,
                    TracePriority.ScriptError2);
            }
            finally
            {
                if (!success)
                {
                    error = String.Format(
                        "bad script xml {0} attribute value: {1}",
                        FormatOps.WrapOrNull(attributeName),
                        FormatOps.WrapOrNull(localAttributeValue));
                }
            }

            return success;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the value of a single named XML attribute by looking
        /// up and invoking its registered setter callback.
        /// </summary>
        /// <param name="element">
        /// The XML element to write the attribute value to.
        /// </param>
        /// <param name="attributeName">
        /// The name of the XML attribute to write.
        /// </param>
        /// <param name="required">
        /// Non-zero if the attribute must be written successfully.
        /// </param>
        /// <param name="attributeValue">
        /// The attribute value to write.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the attribute value was written successfully; otherwise,
        /// false.
        /// </returns>
        private static bool TrySetAttributeValueViaCallback(
            XmlElement element,    /* in */
            string attributeName,  /* in */
            bool required,         /* in */
            object attributeValue, /* in */
            ref Result error       /* out */
            )
        {
            XmlSetAttributeCallback callback;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (attributeSetters == null)
                {
                    error = "script xml attribute setters not available";
                    return false;
                }

                if ((attributeName == null) || !attributeSetters.TryGetValue(
                        attributeName, out callback))
                {
                    error = String.Format(
                        "unrecognized script xml attribute {0}",
                        FormatOps.WrapOrNull(attributeName));

                    return false;
                }

                if (callback == null)
                {
                    error = String.Format(
                        "forbidden script xml attribute {0}",
                        FormatOps.WrapOrNull(attributeName));

                    return false;
                }
            }

            bool success = false;

            try
            {
                if (callback(
                        element, attributeName, required,
                        attributeValue)) /* throw */
                {
                    success = true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ScriptXmlOps).Name,
                    TracePriority.ScriptError2);
            }
            finally
            {
                if (!success)
                {
                    error = String.Format(
                        "bad script xml {0} attribute value: {1}",
                        FormatOps.WrapOrNull(attributeName),
                        FormatOps.WrapOrNull(attributeValue));
                }
            }

            return success;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Attribute Value Xml Support Methods
        /// <summary>
        /// This method gets the value of the specified XML attribute (or inner
        /// text), using a null default value when the attribute is absent.
        /// </summary>
        /// <param name="element">
        /// The XML element to read the attribute value from.
        /// </param>
        /// <param name="attributeName">
        /// The name of the XML attribute to read.
        /// </param>
        /// <param name="attributeValue">
        /// Upon success, receives the attribute value; otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if the attribute value was read successfully; otherwise, false.
        /// </returns>
        private static bool TryGetAttributeValue(
            XmlElement element,       /* in */
            string attributeName,     /* in */
            out object attributeValue /* out */
            )
        {
            return TryGetAttributeValue(
                element, attributeName, null, out attributeValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends a CDATA section containing the specified string
        /// value, after escaping the end-of-CDATA marker, to the specified XML
        /// element.
        /// </summary>
        /// <param name="element">
        /// The XML element to append the CDATA section to.
        /// </param>
        /// <param name="stringValue">
        /// The string value to place inside the CDATA section.
        /// </param>
        /// <returns>
        /// True if the CDATA section was appended successfully; otherwise, false.
        /// </returns>
        private static bool TryAppendCData(
            XmlElement element, /* in */
            string stringValue  /* in */
            )
        {
            if (element == null)
                return false;

            XmlDocument document = element.OwnerDocument;

            if (document == null)
                return false;

            XmlCDataSection cdata = document.CreateCDataSection(
                EscapeOrUnescapeCData(stringValue, true));

            element.AppendChild(cdata);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates a dictionary of attribute values by reading the
        /// attributes associated with the specified attribute list type from the
        /// specified XML element.
        /// </summary>
        /// <param name="element">
        /// The XML element to read the attribute values from.
        /// </param>
        /// <param name="listType">
        /// The attribute list type whose attribute names are read.
        /// </param>
        /// <param name="attributes">
        /// Upon success, receives the dictionary of attribute names and values.
        /// </param>
        /// <param name="errors">
        /// The list of errors to populate upon failure.  If null, it is created
        /// by this method and returned to the caller.
        /// </param>
        /// <returns>
        /// True if the attribute values were populated successfully; otherwise,
        /// false.
        /// </returns>
        private static bool TryPopulateAttributes(
            XmlElement element,              /* in */
            XmlAttributeListType listType,   /* in */
            ref ObjectDictionary attributes, /* out */
            ref ResultList errors            /* in, out */
            )
        {
            return TryPopulateAttributes(
                element, GetAttributeNames(listType), listType,
                ref attributes, ref errors);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates a dictionary of attribute values by reading the
        /// specified attributes from the specified XML element, skipping null
        /// attribute values.
        /// </summary>
        /// <param name="element">
        /// The XML element to read the attribute values from.
        /// </param>
        /// <param name="attributeNames">
        /// The list of attribute names to read.
        /// </param>
        /// <param name="listType">
        /// The attribute list type, used when determining which attributes are
        /// required and when formatting diagnostic messages.
        /// </param>
        /// <param name="attributes">
        /// Upon success, receives the dictionary of attribute names and values.
        /// </param>
        /// <param name="errors">
        /// The list of errors to populate upon failure.  If null, it is created
        /// by this method and returned to the caller.
        /// </param>
        /// <returns>
        /// True if the attribute values were populated successfully; otherwise,
        /// false.
        /// </returns>
        private static bool TryPopulateAttributes(
            XmlElement element,              /* in */
            StringList attributeNames,       /* in */
            XmlAttributeListType listType,   /* in */
            ref ObjectDictionary attributes, /* out */
            ref ResultList errors            /* in, out */
            )
        {
            if (attributeNames == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "{0}xml attribute names not available",
                    GetAttributeListName(listType)));

                return false;
            }

            bool success = true;
            ObjectDictionary localAttributes = new ObjectDictionary();

            foreach (string attributeName in attributeNames)
            {
                if (attributeName == null)
                    continue;

                bool required = IsRequiredAttributeName(
                    attributeName, listType);

                object attributeValue = null;
                Result error = null;

                if (TryGetAttributeValueViaCallback(
                        element, attributeName, required,
                        ref attributeValue, ref error))
                {
                    //
                    // BUGFIX: Not all attribute types can handle
                    //         null values (e.g. TimeStamp) -AND-
                    //         there is no point in adding a null
                    //         value here because a missing value
                    //         is treated the same way.
                    //
                    if (attributeValue == null)
                        continue;

                    localAttributes[attributeName] = attributeValue;
                }
                else
                {
                    if (error != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(error);
                    }

                    success = false;
                }
            }

            if (success)
                attributes = localAttributes;

            return success;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the specified XML element by writing the
        /// attributes associated with the specified attribute list type from the
        /// specified dictionary of attribute values.
        /// </summary>
        /// <param name="element">
        /// The XML element to write the attribute values to.
        /// </param>
        /// <param name="listType">
        /// The attribute list type whose attribute names are written.
        /// </param>
        /// <param name="attributes">
        /// The dictionary of attribute names and values to write.
        /// </param>
        /// <param name="errors">
        /// The list of errors to populate upon failure.  If null, it is created
        /// by this method and returned to the caller.
        /// </param>
        /// <returns>
        /// True if the element was populated successfully; otherwise, false.
        /// </returns>
        private static bool TryPopulateElement(
            XmlElement element,            /* in */
            XmlAttributeListType listType, /* in */
            ObjectDictionary attributes,   /* in */
            ref ResultList errors          /* in, out */
            )
        {
            return TryPopulateElement(
                element, GetAttributeNames(listType), listType,
                attributes, ref errors);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the specified XML element by writing the
        /// specified attributes from the specified dictionary of attribute
        /// values, reporting an error for any missing required attribute.
        /// </summary>
        /// <param name="element">
        /// The XML element to write the attribute values to.
        /// </param>
        /// <param name="attributeNames">
        /// The list of attribute names to write.
        /// </param>
        /// <param name="listType">
        /// The attribute list type, used when determining which attributes are
        /// required and when formatting diagnostic messages.
        /// </param>
        /// <param name="attributes">
        /// The dictionary of attribute names and values to write.
        /// </param>
        /// <param name="errors">
        /// The list of errors to populate upon failure.  If null, it is created
        /// by this method and returned to the caller.
        /// </param>
        /// <returns>
        /// True if the element was populated successfully; otherwise, false.
        /// </returns>
        private static bool TryPopulateElement(
            XmlElement element,            /* in */
            StringList attributeNames,     /* in */
            XmlAttributeListType listType, /* in */
            ObjectDictionary attributes,   /* in */
            ref ResultList errors          /* in, out */
            )
        {
            if (attributeNames == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "{0}xml attribute names not available",
                    GetAttributeListName(listType)));

                return false;
            }

            if (attributes == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "{0}xml attribute values not available",
                    GetAttributeListName(listType)));

                return false;
            }

            bool success = true;

            foreach (string attributeName in attributeNames)
            {
                if (attributeName == null)
                    continue;

                bool required = IsRequiredAttributeName(
                    attributeName, listType);

                object attributeValue;

                if (!attributes.TryGetValue(
                        attributeName, out attributeValue))
                {
                    if (required)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "missing script xml attribute {0}",
                            FormatOps.WrapOrNull(attributeName)));

                        success = false;
                    }

                    continue;
                }

                Result error = null;

                if (!TrySetAttributeValueViaCallback(
                        element, attributeName, required,
                        attributeValue, ref error))
                {
                    if (error != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(error);
                    }

                    success = false;
                }
            }

            return success;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes from the specified XML element any unsupported
        /// (extra) attributes named in the specified dictionary.
        /// </summary>
        /// <param name="element">
        /// The XML element to remove the attributes from.
        /// </param>
        /// <param name="extra">
        /// The dictionary of extra attribute names and values whose unsupported
        /// names are removed from the element.
        /// </param>
        /// <returns>
        /// True if the operation completed successfully; otherwise, false.
        /// </returns>
        private static bool TryRemoveAttributes(
            XmlElement element,
            ObjectDictionary extra
            )
        {
            if (element == null)
                return false;

            if (extra == null)
                return false;

            string[] attributeNames = null;

            foreach (KeyValuePair<string, object> pair in extra)
            {
                string attributeName = pair.Key;

                if (attributeName == null)
                    continue;

                if (!IsSupported(
                        ref attributeNames, attributeName))
                {
                    element.RemoveAttribute(attributeName);
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes from the specified XML element all of the
        /// attributes associated with the specified attribute list type.
        /// </summary>
        /// <param name="element">
        /// The XML element to remove the attributes from.
        /// </param>
        /// <param name="listType">
        /// The attribute list type whose attribute names are removed.
        /// </param>
        /// <returns>
        /// True if the operation completed successfully; otherwise, false.
        /// </returns>
        private static bool TryRemoveAttributes(
            XmlElement element,           /* in */
            XmlAttributeListType listType /* in */
            )
        {
            if (element == null)
                return false;

            StringList attributeNames = GetAttributeNames(listType);

            if (attributeNames == null)
                return false;

            foreach (string attributeName in attributeNames)
            {
                if (attributeName == null)
                    continue;

                element.RemoveAttribute(attributeName);
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Attribute Value Parameter Support Methods
        /// <summary>
        /// This method resets the block type and text attribute values to their
        /// well-known default values.
        /// </summary>
        /// <param name="blockType">
        /// Upon return, receives the default block type value.
        /// </param>
        /// <param name="text">
        /// Upon return, receives the default text value.
        /// </param>
        private static void ResetAttributeValues(
            out XmlBlockType blockType, /* out */
            out string text             /* out */
            )
        {
            blockType = XmlBlockType.None; /* REQUIRED */
            text = null;                   /* REQUIRED */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the full set of script block attribute values to
        /// their well-known default values.
        /// </summary>
        /// <param name="id">
        /// Upon return, receives the default identifier value.
        /// </param>
        /// <param name="blockType">
        /// Upon return, receives the default block type value.
        /// </param>
        /// <param name="text">
        /// Upon return, receives the default text value.
        /// </param>
        /// <param name="name">
        /// Upon return, receives the default name value.
        /// </param>
        /// <param name="group">
        /// Upon return, receives the default group value.
        /// </param>
        /// <param name="description">
        /// Upon return, receives the default description value.
        /// </param>
        /// <param name="timeStamp">
        /// Upon return, receives the default time stamp value.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon return, receives the default public key token value.
        /// </param>
        /// <param name="signature">
        /// Upon return, receives the default signature value.
        /// </param>
        /// <param name="extra">
        /// Upon return, receives the default extra attributes value.
        /// </param>
        private static void ResetAttributeValues(
            out Guid id,                /* out */
            out XmlBlockType blockType, /* out */
            out string text,            /* out */
            out string name,            /* out */
            out string group,           /* out */
            out string description,     /* out */
            out DateTime timeStamp,     /* out */
            out string publicKeyToken,  /* out */
            out byte[] signature,       /* out */
            out ObjectDictionary extra  /* out */
            )
        {
            id = Guid.Empty;               /* REQUIRED */
            blockType = XmlBlockType.None; /* REQUIRED */
            text = null;                   /* REQUIRED */

            name = null;                   /* OPTIONAL */
            group = null;                  /* OPTIONAL */
            description = null;            /* OPTIONAL */

            timeStamp = DateTime.MinValue; /* OPTIONAL */
            publicKeyToken = null;         /* OPTIONAL */
            signature = null;              /* OPTIONAL */

            extra = null;                  /* OPTIONAL */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unpacks the block type and text attribute values from the
        /// specified dictionary of attribute values into their corresponding
        /// strongly typed output parameters.
        /// </summary>
        /// <param name="attributes">
        /// The dictionary of attribute names and values to unpack.  This
        /// parameter may be null.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to overwrite pre-existing values; this parameter is provided
        /// for symmetry with the related methods.
        /// </param>
        /// <param name="blockType">
        /// Upon return, receives the unpacked block type value.
        /// </param>
        /// <param name="text">
        /// Upon return, receives the unpacked text value.
        /// </param>
        /// <param name="errors">
        /// The list of errors to populate upon failure.  If null, it is created
        /// by this method and returned to the caller.
        /// </param>
        /// <returns>
        /// The number of attribute values that were successfully unpacked, or an
        /// invalid count if the dictionary was null.
        /// </returns>
        private static int UnpackAttributeValues(
            ObjectDictionary attributes, /* in */
            bool overwrite,              /* in */
            out XmlBlockType blockType,  /* out */
            out string text,             /* out */
            ref ResultList errors        /* in, out */
            )
        {
            ResetAttributeValues(out blockType, out text);

            int count = Count.Invalid;

            if (attributes != null)
            {
                count = 0;

                object attributeValue; /* REUSED */

                /* REQUIRED */
                if (attributes.TryGetValue(
                        _XmlAttribute.Type, out attributeValue))
                {
                    try
                    {
                        blockType = (XmlBlockType)attributeValue;
                        count++;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }

                /* REQUIRED */
                if (attributes.TryGetValue(
                        _XmlAttribute.Text, out attributeValue))
                {
                    try
                    {
                        text = (string)attributeValue;
                        count++;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unpacks the full set of script block attribute values
        /// from the specified dictionary of attribute values into their
        /// corresponding strongly typed output parameters, collecting any
        /// unsupported attributes into the extra dictionary.
        /// </summary>
        /// <param name="attributes">
        /// The dictionary of attribute names and values to unpack.  This
        /// parameter may be null.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to count pre-existing extra attribute names as overwritten
        /// rather than skipping them.
        /// </param>
        /// <param name="id">
        /// Upon return, receives the unpacked identifier value.
        /// </param>
        /// <param name="blockType">
        /// Upon return, receives the unpacked block type value.
        /// </param>
        /// <param name="text">
        /// Upon return, receives the unpacked text value.
        /// </param>
        /// <param name="name">
        /// Upon return, receives the unpacked name value.
        /// </param>
        /// <param name="group">
        /// Upon return, receives the unpacked group value.
        /// </param>
        /// <param name="description">
        /// Upon return, receives the unpacked description value.
        /// </param>
        /// <param name="timeStamp">
        /// Upon return, receives the unpacked time stamp value.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon return, receives the unpacked public key token value.
        /// </param>
        /// <param name="signature">
        /// Upon return, receives the unpacked signature value.
        /// </param>
        /// <param name="extra">
        /// Upon return, receives the dictionary of unsupported (extra) attribute
        /// names and values, if any.
        /// </param>
        /// <param name="errors">
        /// The list of errors to populate upon failure.  If null, it is created
        /// by this method and returned to the caller.
        /// </param>
        /// <returns>
        /// The number of attribute values that were successfully unpacked, or an
        /// invalid count if the dictionary was null.
        /// </returns>
        private static int UnpackAttributeValues(
            ObjectDictionary attributes, /* in */
            bool overwrite,              /* in */
            out Guid id,                 /* out */
            out XmlBlockType blockType,  /* out */
            out string text,             /* out */
            out string name,             /* out */
            out string group,            /* out */
            out string description,      /* out */
            out DateTime timeStamp,      /* out */
            out string publicKeyToken,   /* out */
            out byte[] signature,        /* out */
            out ObjectDictionary extra,  /* out */
            ref ResultList errors        /* in, out */
            )
        {
            ResetAttributeValues(
                out id, out blockType, out text, out name,
                out group, out description, out timeStamp,
                out publicKeyToken, out signature, out extra);

            int count = Count.Invalid;

            if (attributes != null)
            {
                count = 0;

                object attributeValue; /* REUSED */

                /* REQUIRED */
                if (attributes.TryGetValue(
                        _XmlAttribute.Id, out attributeValue))
                {
                    try
                    {
                        id = (Guid)attributeValue;
                        count++;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }

                /* REQUIRED */
                if (attributes.TryGetValue(
                        _XmlAttribute.Type, out attributeValue))
                {
                    try
                    {
                        blockType = (XmlBlockType)attributeValue;
                        count++;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }

                /* REQUIRED */
                if (attributes.TryGetValue(
                        _XmlAttribute.Text, out attributeValue))
                {
                    try
                    {
                        text = (string)attributeValue;
                        count++;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }

                /* OPTIONAL */
                if (attributes.TryGetValue(
                        _XmlAttribute.Name, out attributeValue))
                {
                    try
                    {
                        name = (string)attributeValue;
                        count++;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }

                /* OPTIONAL */
                if (attributes.TryGetValue(
                        _XmlAttribute.Group, out attributeValue))
                {
                    try
                    {
                        group = (string)attributeValue;
                        count++;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }

                /* OPTIONAL */
                if (attributes.TryGetValue(
                        _XmlAttribute.Description, out attributeValue))
                {
                    try
                    {
                        description = (string)attributeValue;
                        count++;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }

                /* OPTIONAL */
                if (attributes.TryGetValue(
                        _XmlAttribute.TimeStamp, out attributeValue))
                {
                    try
                    {
                        timeStamp = (DateTime)attributeValue;
                        count++;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }

                /* OPTIONAL */
                if (attributes.TryGetValue(
                        _XmlAttribute.PublicKeyToken, out attributeValue))
                {
                    try
                    {
                        publicKeyToken = (string)attributeValue;
                        count++;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }

                /* OPTIONAL */
                if (attributes.TryGetValue(
                        _XmlAttribute.Signature, out attributeValue))
                {
                    try
                    {
                        signature = (byte[])attributeValue;
                        count++;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }

                /* OPTIONAL */
                string[] attributeNames = null;

                foreach (KeyValuePair<string, object> pair in attributes)
                {
                    //
                    // HACK: Is this XML attribute name one of
                    //       those built into the core library?
                    //       If not, it is considered "extra",
                    //       and it will be added to the extra
                    //       dictionary of attribute names and
                    //       values.  Unless the overwrite flag
                    //       is set, any pre-existing attribute
                    //       names will be skipped.  Currently,
                    //       this should be impossible because
                    //       the extra dictionary is created by
                    //       this method.
                    //
                    string attributeName = pair.Key;

                    if (attributeName == null)
                        continue;

                    if (!IsSupported(
                            ref attributeNames, attributeName))
                    {
                        if (extra == null)
                            extra = new ObjectDictionary();

                        if (extra.ContainsKey(attributeName))
                        {
                            if (overwrite)
                                count++;
                            else
                                continue;
                        }

                        extra[attributeName] = pair.Value;
                        count++;
                    }
                }
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method packs the full set of script block attribute values into
        /// the specified dictionary of attribute values, omitting values that
        /// are at their default and adding any unsupported (extra) attributes.
        /// </summary>
        /// <param name="attributes">
        /// The dictionary of attribute names and values to populate.  This
        /// parameter may be null.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to count pre-existing extra attribute names as overwritten
        /// rather than skipping them.
        /// </param>
        /// <param name="id">
        /// The identifier value to pack.
        /// </param>
        /// <param name="blockType">
        /// The block type value to pack.
        /// </param>
        /// <param name="text">
        /// The text value to pack.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name value to pack.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group value to pack.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description value to pack.  This parameter may be null.
        /// </param>
        /// <param name="timeStamp">
        /// The time stamp value to pack.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token value to pack.  This parameter may be null.
        /// </param>
        /// <param name="signature">
        /// The signature value to pack.  This parameter may be null.
        /// </param>
        /// <param name="extra">
        /// The dictionary of unsupported (extra) attribute names and values to
        /// pack.  This parameter may be null.
        /// </param>
        /// <param name="errors">
        /// The list of errors to populate upon failure.  If null, it is created
        /// by this method and returned to the caller.
        /// </param>
        /// <returns>
        /// The number of attribute values that were successfully packed, or an
        /// invalid count if the dictionary was null.
        /// </returns>
        private static int PackAttributeValues(
            ObjectDictionary attributes, /* in */
            bool overwrite,              /* in */
            Guid id,                     /* in */
            XmlBlockType blockType,      /* in */
            string text,                 /* in */
            string name,                 /* in */
            string group,                /* in */
            string description,          /* in */
            DateTime timeStamp,          /* in */
            string publicKeyToken,       /* in */
            byte[] signature,            /* in */
            ObjectDictionary extra,      /* in */
            ref ResultList errors        /* in, out */
            )
        {
            int count = Count.Invalid;

            if (attributes != null)
            {
                count = 0;

                /* REQUIRED */
                if (!id.Equals(Guid.Empty))
                {
                    attributes[_XmlAttribute.Id] = id;
                    count++;
                }

                /* REQUIRED */
                if (blockType != XmlBlockType.None)
                {
                    attributes[_XmlAttribute.Type] = blockType;
                    count++;
                }

                /* REQUIRED */
                if (text != null)
                {
                    attributes[_XmlAttribute.Text] = text;
                    count++;
                }

                /* OPTIONAL */
                if (name != null)
                {
                    attributes[_XmlAttribute.Name] = name;
                    count++;
                }

                /* OPTIONAL */
                if (group != null)
                {
                    attributes[_XmlAttribute.Group] = group;
                    count++;
                }

                /* OPTIONAL */
                if (description != null)
                {
                    attributes[_XmlAttribute.Description] = description;
                    count++;
                }

                /* OPTIONAL */
                if (timeStamp != DateTime.MinValue)
                {
                    attributes[_XmlAttribute.TimeStamp] = timeStamp;
                    count++;
                }

                /* OPTIONAL */
                if (publicKeyToken != null)
                {
                    attributes[_XmlAttribute.PublicKeyToken] = publicKeyToken;
                    count++;
                }

                /* OPTIONAL */
                if (signature != null)
                {
                    attributes[_XmlAttribute.Signature] = signature;
                    count++;
                }

                if (extra != null)
                {
                    /* OPTIONAL */
                    string[] attributeNames = null;

                    foreach (KeyValuePair<string, object> pair in extra)
                    {
                        //
                        // HACK: Is this XML attribute name one of
                        //       those built into the core library?
                        //       If not, it is considered "extra",
                        //       and it will be added to the extra
                        //       dictionary of attribute names and
                        //       values.  Unless the overwrite flag
                        //       is set, any pre-existing attribute
                        //       names will be skipped.  Currently,
                        //       this should be impossible because
                        //       the extra dictionary is created by
                        //       this method.
                        //
                        string attributeName = pair.Key;

                        if (attributeName == null)
                            continue;

                        if (!IsSupported(
                                ref attributeNames, attributeName))
                        {
                            if (attributes.ContainsKey(attributeName))
                            {
                                if (overwrite)
                                    count++;
                                else
                                    continue;
                            }

                            attributes[attributeName] = pair.Value;
                            count++;
                        }
                    }
                }
            }

            return count;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.WriteEngineInfo method.
        //
        /// <summary>
        /// This method adds diagnostic information about the script XML
        /// subsystem (such as the configured attribute getters and setters and
        /// the CDATA escape settings) to the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to add the diagnostic information to.  If null, this method
        /// does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the verbosity and content of the diagnostic
        /// information.
        /// </param>
        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                bool verbose = HostOps.HasVerboseContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || ((attributeGetters != null) &&
                    (attributeGetters.Count > 0)))
                {
                    localList.Add("AttributeGetters",
                        (attributeGetters != null) ?
                            attributeGetters.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((attributeSetters != null) &&
                    (attributeSetters.Count > 0)))
                {
                    localList.Add("AttributeSetters",
                        (attributeSetters != null) ?
                            attributeSetters.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (verbose)
                {
                    StringList subList1 = null;
                    ResultList errors1 = null;

                    if (CheckDictionary<XmlGetAttributeCallback>(
                            AttributeGetters.InitializeCallbacksArray,
                            attributeGetters, ref subList1, ref errors1))
                    {
                        FormatOps.MaybeAddSubList(
                            localList, subList1, "Script Xml Getters",
                            empty);
                    }
                    else
                    {
                        localList.Add((IPair<string>)null);
                        localList.Add("Script Xml Getters");
                        localList.Add((IPair<string>)null);

                        localList.Add("Errors",
                            FormatOps.WrapOrNull(errors1));
                    }

                    StringList subList2 = null;
                    ResultList errors2 = null;

                    if (CheckDictionary<XmlSetAttributeCallback>(
                            AttributeSetters.InitializeCallbacksArray,
                            attributeSetters, ref subList2, ref errors2))
                    {
                        FormatOps.MaybeAddSubList(
                            localList, subList2, "Script Xml Setters",
                            empty);
                    }
                    else
                    {
                        localList.Add((IPair<string>)null);
                        localList.Add("Script Xml Diagnostics");
                        localList.Add((IPair<string>)null);

                        localList.Add("Errors",
                            FormatOps.WrapOrNull(errors2));
                    }
                }

                if (empty || EscapeCDataEnd)
                {
                    localList.Add("EscapeCDataEnd",
                        EscapeCDataEnd.ToString());
                }

                if (empty || UnescapeCDataEnd)
                {
                    localList.Add("UnescapeCDataEnd",
                        UnescapeCDataEnd.ToString());
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Script Xml");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Attribute Value Helper Methods
        /// <summary>
        /// This method gets the value of the specified XML attribute, or the
        /// inner text when the attribute name refers to the inner text, falling
        /// back to the specified default value when the value is absent.
        /// </summary>
        /// <param name="element">
        /// The XML element to read the attribute value from.
        /// </param>
        /// <param name="attributeName">
        /// The name of the XML attribute to read.
        /// </param>
        /// <param name="defaultAttributeValue">
        /// The default value to use when the attribute value is absent.  This
        /// parameter may be null.
        /// </param>
        /// <param name="attributeValue">
        /// Upon success, receives the attribute value; otherwise, receives the
        /// default value.
        /// </param>
        /// <returns>
        /// True if the attribute value was read successfully; otherwise, false.
        /// </returns>
        public static bool TryGetAttributeValue(
            XmlElement element,           /* in */
            string attributeName,         /* in */
            object defaultAttributeValue, /* in */
            out object attributeValue     /* out */
            )
        {
            if (element == null)
            {
                attributeValue = defaultAttributeValue;
                return false;
            }

            if (IsInnerTextAttributeName(attributeName))
            {
                string innerText = element.InnerText;

                if (IsMissingInnerText(innerText))
                {
                    attributeValue = defaultAttributeValue;
                    return false;
                }

                attributeValue = EscapeOrUnescapeCData(
                    innerText, false);

                return true;
            }

            if (attributeName == null)
            {
                attributeValue = defaultAttributeValue;
                return false;
            }

            if (!element.HasAttribute(attributeName))
            {
                attributeValue = defaultAttributeValue;
                return false;
            }

            attributeValue = element.GetAttribute(attributeName);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the value of the specified XML attribute, or the
        /// inner text (as a CDATA section) when the attribute name refers to the
        /// inner text.
        /// </summary>
        /// <param name="element">
        /// The XML element to write the attribute value to.
        /// </param>
        /// <param name="attributeName">
        /// The name of the XML attribute to write.
        /// </param>
        /// <param name="attributeValue">
        /// The attribute value to write.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the attribute value was written successfully; otherwise,
        /// false.
        /// </returns>
        public static bool TrySetAttributeValue(
            XmlElement element,   /* in */
            string attributeName, /* in */
            object attributeValue /* in */
            )
        {
            if (element == null)
                return false;

            string stringValue = AttributeValueToString(attributeValue);

            if (IsInnerTextAttributeName(attributeName))
            {
                element.InnerText = null;

                if (IsMissingInnerText(stringValue))
                    return true;

                return TryAppendCData(element, stringValue);
            }

            if (attributeName == null)
                return false;

            element.SetAttribute(attributeName, stringValue);
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Testing Support Methods
#if TEST
        /// <summary>
        /// This method gets the object used to synchronize access to the static
        /// data of this class.  It is intended for testing purposes only.
        /// </summary>
        /// <returns>
        /// The synchronization object.  This method cannot return null.
        /// </returns>
        public static object GetSyncRoot() /* CANNOT RETURN NULL */
        {
            return syncRoot;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the dictionary of per-attribute getter callbacks.
        /// It is intended for testing purposes only.
        /// </summary>
        /// <returns>
        /// The dictionary of attribute getter callbacks, or null if it has not
        /// been initialized.
        /// </returns>
        public static XmlGetAttributeDictionary GetAttributeGetters()
        {
            return attributeGetters;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the dictionary of per-attribute setter callbacks.
        /// It is intended for testing purposes only.
        /// </summary>
        /// <returns>
        /// The dictionary of attribute setter callbacks, or null if it has not
        /// been initialized.
        /// </returns>
        public static XmlSetAttributeDictionary GetAttributeSetters()
        {
            return attributeSetters;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Subsystem Initialization Methods
        /// <summary>
        /// This method initializes the dictionary of per-attribute getter
        /// callbacks used to read the Eagle-specific XML attribute values.
        /// </summary>
        /// <param name="force">
        /// Non-zero to (re-)initialize the dictionary even when it has already
        /// been populated.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to overwrite any pre-existing entries with the same
        /// attribute name.
        /// </param>
        public static void InitializeAttributeGetters(
            bool force,    /* in */
            bool overwrite /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                InitializeDictionary<XmlGetAttributeCallback>(
                    AttributeGetters.InitializeCallbacksArray,
                    ref attributeGetters, force, overwrite);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the dictionary of per-attribute setter
        /// callbacks used to write the Eagle-specific XML attribute values.
        /// </summary>
        /// <param name="force">
        /// Non-zero to (re-)initialize the dictionary even when it has already
        /// been populated.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to overwrite any pre-existing entries with the same
        /// attribute name.
        /// </param>
        public static void InitializeAttributeSetters(
            bool force,    /* in */
            bool overwrite /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                InitializeDictionary<XmlSetAttributeCallback>(
                    AttributeSetters.InitializeCallbacksArray,
                    ref attributeSetters, force, overwrite);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Attribute Value Reader / Writer Methods
        /// <summary>
        /// This method reads the engine block type and text values from the
        /// specified XML node.  It is for use by the static Engine class only;
        /// the attribute list type is forced to the engine list type.
        /// </summary>
        /// <param name="node">
        /// The XML node to read the attribute values from.
        /// </param>
        /// <param name="listType">
        /// The attribute list type.  This parameter is ignored; the engine list
        /// type is always used.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to overwrite pre-existing values during unpacking.
        /// </param>
        /// <param name="blockType">
        /// Upon success, receives the block type value.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the text value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the attribute values were read successfully; otherwise, false.
        /// </returns>
        public static bool TryGetAttributeValues(
            XmlNode node,                  /* in */
            XmlAttributeListType listType, /* in: IGNORED */
            bool overwrite,                /* in */
            out XmlBlockType blockType,    /* out */
            out string text,               /* out */
            ref Result error               /* out */
            )
        {
            ResetAttributeValues(out blockType, out text);

            //
            // HACK: Skip checking element against null here as all
            //       the called methods will do that.
            //
            XmlElement element = node as XmlElement;

            try
            {
                //
                // HACK: The list type is hard-coded here because
                //       all the engine attributes are required.
                //
                // WARNING: Any value passed by our caller for this
                //          parameter is IGNORED.
                //
                if (listType != XmlAttributeListType.Engine)
                {
                    TraceOps.DebugTrace(String.Format(
                        "TryGetAttributeValues: IGNORED list " +
                        "type {0}, now forcing list type {1}",
                        FormatOps.WrapOrNull(listType),
                        FormatOps.WrapOrNull(
                            XmlAttributeListType.Engine)),
                        typeof(ScriptXmlOps).Name,
                        TracePriority.ScriptError2);

                    listType = XmlAttributeListType.Engine;
                }

                //
                // HACK: This method only cares about the XML block
                //       type attribute and its inner text.  It is
                //       for use by the static Engine class only.
                //
                StringList attributeNames = GetAttributeNames(
                    listType);

                //
                // TODO: This step is not strictly required; it is
                //       being retained to provide a slightly more
                //       accurate error message to the caller.
                //
                if (!HasAttributeNames(
                        element, attributeNames, listType,
                        ref error))
                {
                    return false;
                }

                //
                // NOTE: Attempt to populate the attributes now,
                //       based on the element.
                //
                ObjectDictionary attributes = null;
                ResultList errors; /* REUSED */

                errors = null;

                if (!TryPopulateAttributes(
                        element, attributeNames, listType,
                        ref attributes, ref errors))
                {
                    error = errors;
                    return false;
                }

                XmlBlockType localBlockType; /* REQUIRED */
                string localText;            /* REQUIRED */

                errors = null;

                UnpackAttributeValues(
                    attributes, overwrite, out localBlockType,
                    out localText, ref errors);

                if (errors != null)
                {
                    error = errors;
                    return false;
                }

                blockType = localBlockType;           /* REQUIRED */
                text = localText;                     /* REQUIRED */

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the full set of script block attribute values from
        /// the specified XML node, validating that all required attributes are
        /// present.
        /// </summary>
        /// <param name="node">
        /// The XML node to read the attribute values from.
        /// </param>
        /// <param name="listType">
        /// The attribute list type whose attribute names are read.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to overwrite pre-existing values during unpacking.
        /// </param>
        /// <param name="id">
        /// Upon success, receives the identifier value.
        /// </param>
        /// <param name="blockType">
        /// Upon success, receives the block type value.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the text value.
        /// </param>
        /// <param name="name">
        /// Upon success, receives the name value.
        /// </param>
        /// <param name="group">
        /// Upon success, receives the group value.
        /// </param>
        /// <param name="description">
        /// Upon success, receives the description value.
        /// </param>
        /// <param name="timeStamp">
        /// Upon success, receives the time stamp value.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon success, receives the public key token value.
        /// </param>
        /// <param name="signature">
        /// Upon success, receives the signature value.
        /// </param>
        /// <param name="extra">
        /// Upon success, receives the dictionary of unsupported (extra)
        /// attribute names and values, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the attribute values were read successfully; otherwise, false.
        /// </returns>
        public static bool TryGetAttributeValues(
            XmlNode node,                  /* in */
            XmlAttributeListType listType, /* in */
            bool overwrite,                /* in */
            out Guid id,                   /* out */
            out XmlBlockType blockType,    /* out */
            out string text,               /* out */
            out string name,               /* out */
            out string group,              /* out */
            out string description,        /* out */
            out DateTime timeStamp,        /* out */
            out string publicKeyToken,     /* out */
            out byte[] signature,          /* out */
            out ObjectDictionary extra,    /* out */
            ref Result error               /* out */
            )
        {
            ResetAttributeValues(
                out id, out blockType, out text, out name,
                out group, out description, out timeStamp,
                out publicKeyToken, out signature, out extra);

            //
            // HACK: Skip checking element against null here as all
            //       the called methods will do that.
            //
            XmlElement element = node as XmlElement;

            try
            {
                //
                // TODO: This step is not strictly required; it is
                //       being retained to provide a slightly more
                //       accurate error message to the caller.
                //
                // HACK: The list type is hard-coded here because
                //       only the required attributes are actually
                //       required.
                //
                if (!HasAttributeNames(
                        element, XmlAttributeListType.Required,
                        ref error))
                {
                    return false;
                }

                //
                // NOTE: Attempt to populate the attributes now,
                //       based on the element.
                //
                ObjectDictionary attributes = null;
                ResultList errors; /* REUSED */

                errors = null;

                if (!TryPopulateAttributes(
                        element, listType, ref attributes,
                        ref errors))
                {
                    error = errors;
                    return false;
                }

                Guid localId;                /* REQUIRED */
                XmlBlockType localBlockType; /* REQUIRED */
                string localText;            /* REQUIRED */
                string localName;            /* OPTIONAL */
                string localGroup;           /* OPTIONAL */
                string localDescription;     /* OPTIONAL */
                DateTime localTimeStamp;     /* OPTIONAL */
                string localPublicKeyToken;  /* OPTIONAL */
                byte[] localSignature;       /* OPTIONAL */
                ObjectDictionary localExtra; /* OPTIONAL */

                errors = null;

                /* IGNORED */
                UnpackAttributeValues(
                    attributes, overwrite, out localId,
                    out localBlockType, out localText,
                    out localName, out localGroup,
                    out localDescription, out localTimeStamp,
                    out localPublicKeyToken, out localSignature,
                    out localExtra, ref errors);

                if (errors != null)
                {
                    error = errors;
                    return false;
                }

                id = localId;                         /* REQUIRED */
                blockType = localBlockType;           /* REQUIRED */
                text = localText;                     /* REQUIRED */

                name = localName;                     /* OPTIONAL */
                group = localGroup;                   /* OPTIONAL */
                description = localDescription;       /* OPTIONAL */

                timeStamp = localTimeStamp;           /* OPTIONAL */
                publicKeyToken = localPublicKeyToken; /* OPTIONAL */
                signature = localSignature;           /* OPTIONAL */

                extra = localExtra;                   /* OPTIONAL */

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the full set of script block attribute values to
        /// the specified XML node, first removing any pre-existing attributes
        /// and then validating that all required attributes were written.
        /// </summary>
        /// <param name="node">
        /// The XML node to write the attribute values to.
        /// </param>
        /// <param name="listType">
        /// The attribute list type whose attribute names are written.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to overwrite pre-existing values during packing.
        /// </param>
        /// <param name="id">
        /// The identifier value to write.
        /// </param>
        /// <param name="blockType">
        /// The block type value to write.
        /// </param>
        /// <param name="text">
        /// The text value to write.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name value to write.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group value to write.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description value to write.  This parameter may be null.
        /// </param>
        /// <param name="timeStamp">
        /// The time stamp value to write.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token value to write.  This parameter may be null.
        /// </param>
        /// <param name="signature">
        /// The signature value to write.  This parameter may be null.
        /// </param>
        /// <param name="extra">
        /// The dictionary of unsupported (extra) attribute names and values to
        /// write.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the attribute values were written successfully; otherwise,
        /// false.
        /// </returns>
        public static bool TrySetAttributeValues(
            XmlNode node,                  /* in */
            XmlAttributeListType listType, /* in */
            bool overwrite,                /* in */
            Guid id,                       /* in */
            XmlBlockType blockType,        /* in */
            string text,                   /* in */
            string name,                   /* in */
            string group,                  /* in */
            string description,            /* in */
            DateTime timeStamp,            /* in */
            string publicKeyToken,         /* in */
            byte[] signature,              /* in */
            ObjectDictionary extra,        /* in */
            ref Result error               /* out */
            )
        {
            //
            // HACK: Skip checking element against null here as all
            //       the called methods will do that.
            //
            XmlElement element = node as XmlElement;

            /* IGNORED */
            TryRemoveAttributes(element, listType);

            /* IGNORED */
            TryRemoveAttributes(element, extra);

            try
            {
                //
                // NOTE: Both of the called methods assume that the
                //       attributes have been created.
                //
                ObjectDictionary attributes = new ObjectDictionary();
                ResultList errors; /* REUSED */

                errors = null;

                /* IGNORED */
                PackAttributeValues(
                    attributes, overwrite, id, blockType, text, name,
                    group, description, timeStamp, publicKeyToken,
                    signature, extra, ref errors);

                if (errors != null)
                {
                    error = errors;
                    return false;
                }

                errors = null;

                if (!TryPopulateElement(
                        element, listType, attributes, ref errors))
                {
                    error = errors;
                    return false;
                }

                //
                // TODO: This step is not strictly required; it is
                //       being retained to provide a slightly more
                //       accurate error message to the caller.
                //
                // HACK: The list type is hard-coded here because
                //       only the required attributes are actually
                //       required.
                //
                if (!HasAttributeNames(
                        element, XmlAttributeListType.Required,
                        ref error))
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
        #endregion
        #endregion
    }
}
