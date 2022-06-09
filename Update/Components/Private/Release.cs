/*
 * Release.cs --
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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Eagle._Components.Shared;
using _BuildType = Eagle._Components.Shared.BuildType;

namespace Eagle._Components.Private
{
    [Guid("4a7afd47-c3ee-4607-b230-81b924814674")]
    internal sealed class Release
    {
        #region Private Constants
        private static readonly string TraceCategory = typeof(Release).Name;

        ///////////////////////////////////////////////////////////////////////

        private static readonly char DigitSeparator = Characters.Underscore;

        ///////////////////////////////////////////////////////////////////////

        [Guid("7b75095c-2046-4a3b-9474-c6f8bd7eb97c")]
        private static class Line
        {
            internal static readonly char[] Separators = {
                Characters.CarriageReturn, Characters.LineFeed
            };

            internal static readonly char[] Comments = {
                Characters.AltComment, Characters.Comment
            };
        }

        ///////////////////////////////////////////////////////////////////////

        [Guid("ee748090-bd67-405a-9bf0-abe774008212")]
        private static class Field
        {
            internal const char Separator = Characters.HorizontalTab;

            ///////////////////////////////////////////////////////////////////

            internal const int Count = 11;

            ///////////////////////////////////////////////////////////////////

            internal const int ProtocolId = 0;
            internal const int PublicKeyToken = 1;
            internal const int Name = 2;
            internal const int Culture = 3;
            internal const int PatchLevel = 4;
            internal const int TimeStamp = 5;
            internal const int BaseUri = 6;
            internal const int Md5Hash = 7;
            internal const int Sha1Hash = 8;
            internal const int Sha512Hash = 9;
            internal const int Notes = 10;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        [Guid("7736fe22-822e-46f2-86d0-c517dcde802d")]
        private static class Protocol
        {
            internal const string Invalid = "0"; /* COMPAT: Eagle beta. */
            internal const string Build = "1";   /* COMPAT: Eagle beta. */
            internal const string Script = "2";
            internal const string Self = "3";
            internal const string Plugin = "4";  /* COMPAT: "update.eagle". */
        }

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static int nextId;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private Release()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Release(
            Configuration configuration,
            int id,
            string protocolId,
            byte[] publicKeyToken,
            string name,
            CultureInfo culture,
            Version patchLevel,
            DateTime? timeStamp,
            BuildType? buildType,
            Uri baseUri,
            string uriFormat,
            byte[] md5Hash,
            byte[] sha1Hash,
            byte[] sha512Hash,
            string notes
            )
            : this()
        {
            this.configuration = configuration;
            this.id = id;
            this.protocolId = protocolId;
            this.publicKeyToken = publicKeyToken;
            this.name = name;
            this.culture = culture;
            this.patchLevel = patchLevel;
            this.timeStamp = timeStamp;
            this.buildType = buildType;
            this.baseUri = baseUri;
            this.uriFormat = uriFormat;
            this.md5Hash = md5Hash;
            this.sha1Hash = sha1Hash;
            this.sha512Hash = sha512Hash;
            this.notes = notes;
        }

        ///////////////////////////////////////////////////////////////////////

        public Release(
            Release release
            )
            : this()
        {
            if (release != null)
            {
                this.configuration = release.configuration;
                this.id = release.id;
                this.protocolId = release.protocolId;
                this.publicKeyToken = release.publicKeyToken;
                this.name = release.name;
                this.culture = release.culture;
                this.patchLevel = release.patchLevel;
                this.timeStamp = release.timeStamp;
                this.buildType = release.buildType;
                this.baseUri = release.baseUri;
                this.uriFormat = release.uriFormat;
                this.md5Hash = release.md5Hash;
                this.sha1Hash = release.sha1Hash;
                this.sha512Hash = release.sha512Hash;
                this.notes = release.notes;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private Configuration configuration;
        public Configuration Configuration
        {
            get { return configuration; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int id;
        public int Id
        {
            get { return id; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string protocolId;
        public string ProtocolId
        {
            get { return protocolId; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] publicKeyToken;
        public byte[] PublicKeyToken
        {
            get { return publicKeyToken; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public string Name
        {
            get { return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        private CultureInfo culture;
        public CultureInfo Culture
        {
            get { return culture; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Version patchLevel;
        public Version PatchLevel
        {
            get { return patchLevel; }
        }

        ///////////////////////////////////////////////////////////////////////

        private DateTime? timeStamp;
        public DateTime? TimeStamp
        {
            get { return timeStamp; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BuildType? buildType;
        public BuildType? BuildType
        {
            get { return buildType; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Uri baseUri;
        public Uri BaseUri
        {
            get { return baseUri; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string uriFormat;
        public string UriFormat
        {
            get { return uriFormat; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] md5Hash;
        public byte[] Md5Hash
        {
            get { return md5Hash; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] sha1Hash;
        public byte[] Sha1Hash
        {
            get { return sha1Hash; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] sha512Hash;
        public byte[] Sha512Hash
        {
            get { return sha512Hash; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string notes;
        public string Notes
        {
            get { return notes; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsValid
        {
            get
            {
                if (id == 0)
                {
                    Trace(configuration, String.Format("Invalid value: {0}",
                        FormatOps.NameAndValue("Id", id)), TraceCategory);

                    return false;
                }

                if (protocolId == null)
                {
                    Trace(configuration, String.Format("Invalid value: {0}",
                        FormatOps.NameAndValue("ProtocolId", protocolId)),
                        TraceCategory);

                    return false;
                }

                if (publicKeyToken == null)
                {
                    Trace(configuration, String.Format("Invalid value: {0}",
                        FormatOps.NameAndValue("PublicKeyToken", publicKeyToken)),
                        TraceCategory);

                    return false;
                }

                if (name == null)
                {
                    Trace(configuration, String.Format("Invalid value: {0}",
                        FormatOps.NameAndValue("Name", name)), TraceCategory);

                    return false;
                }

                if (culture == null)
                {
                    Trace(configuration, String.Format("Invalid value: {0}",
                        FormatOps.NameAndValue("Culture", culture)),
                        TraceCategory);

                    return false;
                }

                //
                // NOTE: The patch level is allowed to be null if this is a
                //       script update.
                //
                if ((patchLevel == null) && !IsScript)
                {
                    Trace(configuration, String.Format("Invalid value: {0}",
                        FormatOps.NameAndValue("PatchLevel", patchLevel)),
                        TraceCategory);

                    return false;
                }

                if (timeStamp == null)
                {
                    Trace(configuration, String.Format("Invalid value: {0}",
                        FormatOps.NameAndValue("TimeStamp", timeStamp)),
                        TraceCategory);

                    return false;
                }

                if (baseUri == null)
                {
                    Trace(configuration, String.Format("Invalid value: {0}",
                        FormatOps.NameAndValue("BaseUri", baseUri)),
                        TraceCategory);

                    return false;
                }

                //if (uriFormat == null) // NOTE: Null allowed.
                //{
                //    Trace(configuration, String.Format("Invalid value: {0}",
                //        FormatOps.NameAndValue("UriFormat", uriFormat)),
                //        TraceCategory);
                //
                //    return false;
                //}

                if (md5Hash == null)
                {
                    Trace(configuration, String.Format("Invalid value: {0}",
                        FormatOps.NameAndValue("Md5Hash", md5Hash)),
                        TraceCategory);

                    return false;
                }

                if (sha1Hash == null)
                {
                    Trace(configuration, String.Format("Invalid value: {0}",
                        FormatOps.NameAndValue("Sha1Hash", sha1Hash)),
                        TraceCategory);

                    return false;
                }

                if (sha512Hash == null)
                {
                    Trace(configuration, String.Format("Invalid value: {0}",
                        FormatOps.NameAndValue("Sha512Hash", sha512Hash)),
                        TraceCategory);

                    return false;
                }

                //if (notes == null) // NOTE: Null allowed.
                //{
                //    Trace(configuration, String.Format("Invalid value: {0}",
                //        FormatOps.NameAndValue("Notes", notes)),
                //        TraceCategory);
                //
                //    return false;
                //}

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsEqual
        {
            get
            {
                if (configuration != null)
                {
                    if (VersionOps.Compare(
                            patchLevel, configuration.PatchLevel) == 0)
                    {
                        Trace(configuration, String.Format(
                            "Release patch level \"{0}\" is equal to " +
                            "configuration patch level \"{1}\".", patchLevel,
                            configuration.PatchLevel), TraceCategory);

                        return true;
                    }

                    if (configuration.Force)
                    {
                        Trace(configuration, String.Format(
                            "Forced to report that release patch level \"{0}\" " +
                            "is equal to configuration patch level \"{1}\" " +
                            "due to configuration.", patchLevel,
                            configuration.PatchLevel), TraceCategory);

                        return true;
                    }
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsGreater
        {
            get
            {
                if (configuration != null)
                {
                    if (VersionOps.Compare(
                            patchLevel, configuration.PatchLevel) > 0)
                    {
                        Trace(configuration, String.Format(
                            "Release patch level \"{0}\" is greater than " +
                            "configuration patch level \"{1}\".", patchLevel,
                            configuration.PatchLevel), TraceCategory);

                        return true;
                    }

                    if (configuration.Force)
                    {
                        Trace(configuration, String.Format(
                            "Forced to report that release patch level \"{0}\" " +
                            "is greater than configuration patch level \"{1}\" " +
                            "due to configuration.", patchLevel,
                            configuration.PatchLevel), TraceCategory);

                        return true;
                    }
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsBuild
        {
            get { return IsBuildProtocol(protocolId); }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsScript
        {
            get { return IsScriptProtocol(protocolId); }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsSelf
        {
            get { return IsSelfProtocol(protocolId); }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsPlugin
        {
            get { return IsPluginProtocol(protocolId); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return String.Format("{0} ({1})",
                FormatOps.ValueToString(patchLevel),
                FormatOps.ValueToString(timeStamp));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void Dump()
        {
            Trace(configuration, FormatOps.NameAndValue("Id", id),
                TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("ProtocolId",
                protocolId), TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("PublicKeyToken",
                publicKeyToken), TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("Name", name),
                TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("Culture", culture),
                TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("PatchLevel",
                patchLevel), TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("TimeStamp",
                timeStamp), TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("BuildType",
                buildType), TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("BaseUri", baseUri),
                TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("UriFormat", uriFormat),
                TraceCategory);

            BuildType localBuildType;

            if (buildType != null)
            {
                localBuildType = (BuildType)buildType;
            }
            else
            {
                localBuildType = (configuration != null) ?
                    configuration.BuildType : _BuildType.Default;
            }

            ReleaseType localReleaseType = (configuration != null) ?
                configuration.ReleaseType : ReleaseType.Default;

            Trace(configuration, FormatOps.NameAndValue("RelativeUri",
                Format(localBuildType, localReleaseType)), TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("Md5Hash", md5Hash),
                TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("Sha1Hash", sha1Hash),
                TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("Sha512Hash",
                sha512Hash), TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("Notes",
                FormatOps.NotesToString(notes)), TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("IsValid", IsValid),
                TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("IsEqual", IsEqual),
                TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("IsGreater",
                IsGreater), TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("IsBuild", IsBuild),
                TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("IsScript", IsScript),
                TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("IsSelf", IsSelf),
                TraceCategory);

            Trace(configuration, FormatOps.NameAndValue("IsPlugin", IsPlugin),
                TraceCategory);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool VerifyFile(
            Configuration configuration,
            string fileName,
            bool strongName
            )
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    Trace(configuration, String.Format(
                        "File \"{0}\" does not exist.", fileName),
                        TraceCategory);

                    return false;
                }

                ///////////////////////////////////////////////////////////////

                string error = null;

                if (strongName)
                {
#if NATIVE && WINDOWS
                    if (VersionOps.IsWindowsOperatingSystem() &&
                        !StrongNameEx.IsStrongNameSigned(
                            configuration, fileName, true, ref error))
                    {
                        Trace(configuration, String.Format(
                            "Assembly in file \"{0}\" is not signed.",
                            fileName), TraceCategory);

                        Trace(configuration, String.Format(
                            "Assembly signature error: {0}", error),
                            TraceCategory);

                        return false;
                    }
#endif

                    ///////////////////////////////////////////////////////////

                    AssemblyName assemblyName =
                        AssemblyName.GetAssemblyName(fileName);

                    if (assemblyName == null)
                    {
                        Trace(configuration, String.Format(
                            "Assembly in file \"{0}\" has no name.", fileName),
                            TraceCategory);

                        return false;
                    }

                    byte[] filePublicKeyToken = assemblyName.GetPublicKeyToken();

                    if (!GenericOps<byte>.Equals(
                            filePublicKeyToken, publicKeyToken))
                    {
                        Trace(configuration, String.Format(
                            "Assembly in file \"{0}\" has incorrect " +
                            "public key token \"{1}\".", fileName,
                            FormatOps.ToHexString(filePublicKeyToken)),
                            TraceCategory);

                        return false;
                    }
                }

                ///////////////////////////////////////////////////////////////

                byte[] hash = null;

                if (FileOps.Hash(
                        configuration, "md5", fileName, ref hash, ref error))
                {
                    if (!GenericOps<byte>.Equals(hash, md5Hash))
                    {
                        Trace(configuration, String.Format(
                            "File \"{0}\" MD5 hash mismatch, got: {1}.",
                            fileName, FormatOps.ToHexString(hash)),
                            TraceCategory);

                        return false;
                    }
                }
                else
                {
                    Trace(configuration, error, TraceCategory);

                    return false;
                }

                ///////////////////////////////////////////////////////////////

                if (FileOps.Hash(
                        configuration, "sha1", fileName, ref hash, ref error))
                {
                    if (!GenericOps<byte>.Equals(hash, sha1Hash))
                    {
                        Trace(configuration, String.Format(
                            "File \"{0}\" SHA1 hash mismatch, got: {1}.",
                            fileName, FormatOps.ToHexString(hash)),
                            TraceCategory);

                        return false;
                    }
                }
                else
                {
                    Trace(configuration, error, TraceCategory);

                    return false;
                }

                ///////////////////////////////////////////////////////////////

                if (FileOps.Hash(
                        configuration, "sha512", fileName, ref hash, ref error))
                {
                    if (!GenericOps<byte>.Equals(hash, sha512Hash))
                    {
                        Trace(configuration, String.Format(
                            "File \"{0}\" SHA512 hash mismatch, got: {1}.",
                            fileName, FormatOps.ToHexString(hash)),
                            TraceCategory);

                        return false;
                    }
                }
                else
                {
                    Trace(configuration, error, TraceCategory);

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Trace(configuration, e, TraceCategory);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static bool TryCreate( /* NOT USED */
            Configuration configuration,
            int releaseId,
            int lineIndex,
            string line,
            out Release release,
            ref string error
            )
        {
            release = ParseLine(
                configuration, releaseId, lineIndex, line, ref error);

            return (release != null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Formatting Methods
        private static string BuildTypeToString(
            BuildType buildType
            )
        {
            string result = (buildType == _BuildType.Default) ?
                String.Empty : buildType.ToString();

            if (!String.IsNullOrEmpty(result) &&
                Char.IsDigit(result[result.Length - 1]))
            {
                result += DigitSeparator;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string ReleaseTypeToString(
            ReleaseType releaseType
            )
        {
            string result = releaseType.ToString();

            if (!String.IsNullOrEmpty(result) &&
                Char.IsDigit(result[result.Length - 1]))
            {
                result += DigitSeparator;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private string Format(
            BuildType buildType,
            ReleaseType releaseType
            )
        {
            string format = uriFormat;

            if ((format == null) && !IsScript)
            {
                format = (configuration != null) ?
                    configuration.UriFormat : Defaults.BuildUriFormat;
            }

            if (format != null)
            {
                return String.Format(
                    format, patchLevel, ReleaseTypeToString(releaseType),
                    BuildTypeToString(buildType));
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Build Type Methods
        public BuildType BuildTypeOrDefault()
        {
            if (buildType != null)
                return (BuildType)buildType;

            return Defaults.BuildType;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public URI Methods
        public bool MaybeUseDownloadBaseUri(
            Assembly assembly
            )
        {
            if (baseUri == null)
            {
                baseUri = AttributeOps.GetAssemblyDownloadBaseUri(assembly);

                if (baseUri != null)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public Uri CreateUri(
            BuildType buildType,
            ReleaseType releaseType
            )
        {
            if (baseUri == null)
                return null;

            try
            {
                string relativeUri = Format(buildType, releaseType);

                if (relativeUri == null)
                    return null;

                Uri uri;

                if (Uri.TryCreate(baseUri, relativeUri, out uri))
                    return uri;
            }
            catch
            {
                // do nothing.
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Miscellaneous Methods
        private static int NextId()
        {
            return Interlocked.Increment(ref nextId);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Tracing Methods
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string Trace(
            Configuration configuration,
            Exception exception,
            string category
            )
        {
            return TraceOps.Trace(configuration, exception, category);
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string Trace(
            Configuration configuration,
            string message,
            string category
            )
        {
            return TraceOps.Trace(configuration, message, category);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Parsing Methods
        private static bool IsBuildProtocol(
            string protocolId
            )
        {
            return StringOps.SystemEquals(protocolId, Protocol.Build);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsScriptProtocol(
            string protocolId
            )
        {
            return StringOps.SystemEquals(protocolId, Protocol.Script);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsSelfProtocol(
            string protocolId
            )
        {
            return StringOps.SystemEquals(protocolId, Protocol.Self);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsPluginProtocol(
            string protocolId
            )
        {
            return StringOps.SystemEquals(protocolId, Protocol.Plugin);
        }

        ///////////////////////////////////////////////////////////////////////

        private static Release ParseLine(
            Configuration configuration,
            int releaseId,
            int lineIndex,
            string line,
            ref string error
            )
        {
            if (configuration == null)
            {
                error = "Invalid configuration.";
                return null;
            }

            if (line == null)
            {
                error = "Invalid release data line.";
                return null;
            }

            line = line.Trim(Characters.Space);

            if (line.Length == 0)
            {
                error = "Empty release data line.";
                return null;
            }

            string[] fields = line.Split(Field.Separator);

            if (fields.Length < Field.Count)
            {
                error = String.Format(
                    "Potential protocol mismatch, release data " +
                    "line #{0} has {1} fields, expected {2} fields.",
                    lineIndex, fields.Length, Field.Count);

                return null;
            }

            //
            // NOTE: First, extract the protocol, because it is used
            //       to determine the processing semantics of several
            //       other fields.
            //
            string protocolId = fields[Field.ProtocolId]; /* string */

            //
            // NOTE: Next, either use the name field verbatim -OR- try
            //       to parse out the name and build type from it.  If
            //       there is no build type, that is perfectly fine.
            //
            string name = null;
            BuildType? buildType = null;

            if (IsBuildProtocol(protocolId))
            {
                if (!ParseOps.NameAndBuildType(
                        fields[Field.Name], false, true, ref name,
                        ref buildType))
                {
                    error = String.Format(
                        "Bad name and/or build type, release data " +
                        "line #{0}.", lineIndex);

                    return null;
                }
            }
            else
            {
                name = fields[Field.Name];
            }

            //
            // NOTE: Using a null UriFormat value here causes it to use
            //       the one associated with the configuration instead.
            //
            string uriFormat = IsSelfProtocol(protocolId) ?
                Defaults.SelfUriFormat : null;

            Release release = new Release(
                configuration, releaseId, protocolId,
                ParseOps.HexString(fields[Field.PublicKeyToken]), name,
                ParseOps.Culture(fields[Field.Culture]),
                ParseOps.Version(fields[Field.PatchLevel]),
                ParseOps.DateTime(fields[Field.TimeStamp]), buildType,
                ParseOps.Uri(fields[Field.BaseUri]), uriFormat,
                ParseOps.HexString(fields[Field.Md5Hash]),
                ParseOps.HexString(fields[Field.Sha1Hash]),
                ParseOps.HexString(fields[Field.Sha512Hash]),
                ParseOps.Notes(fields[Field.Notes]));

            Trace(configuration, String.Format(
                "Release #{0} originated on line #{1}.", releaseId,
                lineIndex), TraceCategory);

            release.Dump();

            return release;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Parsing Methods
        public static bool ParseData(
            Configuration configuration,
            string text,
            ref IEqualityComparer<Configuration> comparer,
            ref IDictionary<Configuration, Release> releases,
            ref int[] protocolCounts,
            ref string error
            )
        {
            if (configuration == null)
            {
                error = "Invalid configuration.";
                return false;
            }

            if (text == null)
            {
                error = "Invalid release data.";
                return false;
            }

            //
            // NOTE: This will contain the counts of the protocols encountered
            //       while parsing the release data (e.g. "1", "2", "3", "4",
            //       or other).
            //
            if (protocolCounts == null)
                protocolCounts = new int[5];

            int parseCount = 0;
            string[] lines = text.Split(Line.Separators);

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];

                if (line == null)
                    continue;

                line = line.Trim(Characters.Space);

                if (line.Length == 0)
                    continue;

                if (GenericOps<char>.Contains(Line.Comments, line[0]))
                    continue;

                Release release = ParseLine(
                    configuration, NextId(), lineIndex, line, ref error);

                if (release != null)
                {
                    /* IGNORED */
                    release.MaybeUseDownloadBaseUri(configuration.Assembly);

                    parseCount++;

                    string protocolId = release.ProtocolId;

                    if (protocolId != null)
                    {
                        if (IsBuildProtocol(protocolId))
                        {
                            //
                            // NOTE: Release build.
                            //
                            protocolCounts[0]++;
                        }
                        else if (IsScriptProtocol(protocolId))
                        {
                            //
                            // NOTE: Update script.
                            //
                            protocolCounts[1]++;
                        }
                        else if (IsSelfProtocol(protocolId))
                        {
                            //
                            // NOTE: Updater itself.
                            //
                            protocolCounts[2]++;
                        }
                        else if (IsPluginProtocol(protocolId))
                        {
                            //
                            // NOTE: Some plugin.
                            //
                            protocolCounts[3]++;
                        }
                        else
                        {
                            //
                            // NOTE: Other and/or unknown.
                            //
                            protocolCounts[4]++;
                        }
                    }

                    if (comparer == null)
                    {
                        comparer = new _Comparers._Configuration(
                            StringOps.GetSystemComparisonType(false),
                            Defaults.Encoding);
                    }

                    if (releases == null)
                    {
                        releases = new Dictionary<Configuration, Release>(
                            comparer);
                    }

                    Configuration releaseConfiguration =
                        Configuration.CreateFrom(release);

                    if (releaseConfiguration == null)
                    {
                        Trace(configuration, String.Format(
                            "Could not create configuration from parsed " +
                            "release {0} on line #{1}, using the " +
                            "pre-existing one...", FormatOps.ForDisplay(
                            release), lineIndex), TraceCategory);

                        releaseConfiguration = configuration;
                    }

                    releases[releaseConfiguration] = release;
                }
            }

            return (parseCount > 0) ? true : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Release Search Methods
        public static Release Find(
            Configuration configuration,
            IDictionary<Configuration, Release> releases,
            bool valid,
            bool equal,
            bool greater
            )
        {
            if (configuration == null)
                return null;

            if (releases == null)
                return null;

            Release release;

            if (releases.TryGetValue(configuration, out release) &&
                (release != null) && (!valid || release.IsValid) &&
                (!equal || release.IsEqual) &&
                (!greater || release.IsGreater))
            {
                return release;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Release FindSelf(
            Configuration configuration,
            IDictionary<Configuration, Release> releases,
            bool valid,
            bool equal,
            bool greater
            )
        {
            return Find(
                Configuration.CreateWithProtocol(configuration,
                Protocol.Self), releases, valid, equal, greater);
        }
        #endregion
    }
}
