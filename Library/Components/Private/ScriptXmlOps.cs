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
    [ObjectId("79f7763f-aca9-40d0-ae37-bbd8afb9a4c7")]
    internal static class ScriptXmlOps
    {
        #region Private Constants
        private static readonly string[] CDataEnd = {
            "]]>",               /* XML unescaped end-of-CData marker */
            "&#x5D;&#x5D;&#x3E;" /* XML numeric character references */
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static XmlGetAttributeDictionary attributeGetters;
        private static XmlSetAttributeDictionary attributeSetters;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static bool EscapeCDataEnd = true; /* TODO: Good default? */
        private static bool UnescapeCDataEnd = true; /* TODO: Good default? */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AttributeGetters Helper Class
        [ObjectId("70fd0624-64a1-458d-a47b-fe729a516955")]
        private static class AttributeGetters
        {
            #region Private XmlGetAttributeCallback Methods
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
        [ObjectId("f5caac0d-7e49-432c-b646-f447357e5366")]
        private static class AttributeSetters
        {
            #region Private XmlSetAttributeCallback Methods
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
        private static void ResetAttributeValues(
            out XmlBlockType blockType, /* out */
            out string text             /* out */
            )
        {
            blockType = XmlBlockType.None; /* REQUIRED */
            text = null;                   /* REQUIRED */
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static object GetSyncRoot() /* CANNOT RETURN NULL */
        {
            return syncRoot;
        }

        ///////////////////////////////////////////////////////////////////////

        public static XmlGetAttributeDictionary GetAttributeGetters()
        {
            return attributeGetters;
        }

        ///////////////////////////////////////////////////////////////////////

        public static XmlSetAttributeDictionary GetAttributeSetters()
        {
            return attributeSetters;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Subsystem Initialization Methods
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
