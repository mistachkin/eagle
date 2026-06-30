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
    /// <summary>
    /// This class represents a single available release parsed from the update
    /// server's release data, which may describe a build, an update script, the
    /// updater itself, or a plugin.  It carries the metadata describing the
    /// release -- its protocol, public key token, name, culture, patch level,
    /// time stamp, build type, base URI, and content hashes -- and provides
    /// methods to parse the release data, verify a downloaded file against the
    /// recorded hashes, locate matching releases, and build the download URI.
    /// </summary>
    [Guid("4a7afd47-c3ee-4607-b230-81b924814674")]
    internal sealed class Release
    {
        #region Private Constants
        /// <summary>
        /// The trace category used when emitting diagnostic messages from this
        /// class.
        /// </summary>
        private static readonly string TraceCategory = typeof(Release).Name;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The character inserted after a build or release type name that ends
        /// in a digit, to separate it from any following digits.
        /// </summary>
        private static readonly char DigitSeparator = Characters.Underscore;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This class contains the constants used when splitting the raw release
        /// data text into individual lines.
        /// </summary>
        [Guid("7b75095c-2046-4a3b-9474-c6f8bd7eb97c")]
        private static class Line
        {
            /// <summary>
            /// The characters that separate one line of release data from the
            /// next.
            /// </summary>
            internal static readonly char[] Separators = {
                Characters.CarriageReturn, Characters.LineFeed
            };

            /// <summary>
            /// The characters that, when found at the start of a line, mark that
            /// line as a comment to be skipped.
            /// </summary>
            internal static readonly char[] Comments = {
                Characters.AltComment, Characters.Comment
            };
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This class contains the constants describing the layout of a single
        /// line of release data: the field separator, the expected field count,
        /// and the zero-based index of each field.
        /// </summary>
        [Guid("ee748090-bd67-405a-9bf0-abe774008212")]
        private static class Field
        {
            /// <summary>
            /// The character that separates one field from the next within a
            /// line of release data.
            /// </summary>
            internal const char Separator = Characters.HorizontalTab;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The number of fields expected in a well-formed line of release
            /// data.
            /// </summary>
            internal const int Count = 11;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The field index of the protocol identifier.
            /// </summary>
            internal const int ProtocolId = 0;

            /// <summary>
            /// The field index of the public key token.
            /// </summary>
            internal const int PublicKeyToken = 1;

            /// <summary>
            /// The field index of the name.
            /// </summary>
            internal const int Name = 2;

            /// <summary>
            /// The field index of the culture.
            /// </summary>
            internal const int Culture = 3;

            /// <summary>
            /// The field index of the patch level.
            /// </summary>
            internal const int PatchLevel = 4;

            /// <summary>
            /// The field index of the time stamp.
            /// </summary>
            internal const int TimeStamp = 5;

            /// <summary>
            /// The field index of the base URI.
            /// </summary>
            internal const int BaseUri = 6;

            /// <summary>
            /// The field index of the MD5 hash.
            /// </summary>
            internal const int Md5Hash = 7;

            /// <summary>
            /// The field index of the SHA1 hash.
            /// </summary>
            internal const int Sha1Hash = 8;

            /// <summary>
            /// The field index of the SHA512 hash.
            /// </summary>
            internal const int Sha512Hash = 9;

            /// <summary>
            /// The field index of the notes.
            /// </summary>
            internal const int Notes = 10;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This class contains the protocol identifier strings that classify
        /// the kind of each release found in the release data.
        /// </summary>
        [Guid("7736fe22-822e-46f2-86d0-c517dcde802d")]
        private static class Protocol
        {
            /// <summary>
            /// The protocol identifier representing an invalid release.
            /// </summary>
            internal const string Invalid = "0"; /* COMPAT: Eagle beta. */

            /// <summary>
            /// The protocol identifier representing a release build.
            /// </summary>
            internal const string Build = "1";   /* COMPAT: Eagle beta. */

            /// <summary>
            /// The protocol identifier representing an update script.
            /// </summary>
            internal const string Script = "2";

            /// <summary>
            /// The protocol identifier representing the updater itself.
            /// </summary>
            internal const string Self = "3";

            /// <summary>
            /// The protocol identifier representing a plugin.
            /// </summary>
            internal const string Plugin = "4";  /* COMPAT: "update.eagle". */
        }

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The counter used to assign a unique identifier to each release
        /// created from the release data.
        /// </summary>
        private static int nextId;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty release instance.  This constructor is used
        /// internally as the common base for the public constructors.
        /// </summary>
        private Release()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a release instance from the specified field values.
        /// </summary>
        /// <param name="configuration">
        /// The configuration associated with this release.
        /// </param>
        /// <param name="id">
        /// The unique identifier of this release.
        /// </param>
        /// <param name="protocolId">
        /// The protocol identifier classifying this release.
        /// </param>
        /// <param name="publicKeyToken">
        /// The expected public key token of the release assembly.
        /// </param>
        /// <param name="name">
        /// The name of this release.
        /// </param>
        /// <param name="culture">
        /// The culture associated with this release.
        /// </param>
        /// <param name="patchLevel">
        /// The patch level (version) of this release.
        /// </param>
        /// <param name="timeStamp">
        /// The time stamp of this release.
        /// </param>
        /// <param name="buildType">
        /// The build type of this release, or null to use the one associated
        /// with the configuration.
        /// </param>
        /// <param name="baseUri">
        /// The base URI used to download this release.
        /// </param>
        /// <param name="uriFormat">
        /// The format string used to build the relative download URI, or null
        /// to use the one associated with the configuration.
        /// </param>
        /// <param name="md5Hash">
        /// The expected MD5 hash of the release file.
        /// </param>
        /// <param name="sha1Hash">
        /// The expected SHA1 hash of the release file.
        /// </param>
        /// <param name="sha512Hash">
        /// The expected SHA512 hash of the release file.
        /// </param>
        /// <param name="notes">
        /// The notes associated with this release.
        /// </param>
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

        /// <summary>
        /// Constructs a release instance that is a copy of the specified
        /// release.
        /// </summary>
        /// <param name="release">
        /// The release to copy.  If this parameter is null, the resulting
        /// instance is left in its default state.
        /// </param>
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
        /// <summary>
        /// Stores the configuration associated with this release.
        /// </summary>
        private Configuration configuration;
        /// <summary>
        /// Gets the configuration associated with this release.
        /// </summary>
        public Configuration Configuration
        {
            get { return configuration; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the unique identifier of this release.
        /// </summary>
        private int id;
        /// <summary>
        /// Gets the unique identifier of this release.
        /// </summary>
        public int Id
        {
            get { return id; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the protocol identifier classifying this release.
        /// </summary>
        private string protocolId;
        /// <summary>
        /// Gets the protocol identifier classifying this release.
        /// </summary>
        public string ProtocolId
        {
            get { return protocolId; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the expected public key token of the release assembly.
        /// </summary>
        private byte[] publicKeyToken;
        /// <summary>
        /// Gets the expected public key token of the release assembly.
        /// </summary>
        public byte[] PublicKeyToken
        {
            get { return publicKeyToken; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of this release.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets the name of this release.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the culture associated with this release.
        /// </summary>
        private CultureInfo culture;
        /// <summary>
        /// Gets the culture associated with this release.
        /// </summary>
        public CultureInfo Culture
        {
            get { return culture; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the patch level (version) of this release.
        /// </summary>
        private Version patchLevel;
        /// <summary>
        /// Gets the patch level (version) of this release.
        /// </summary>
        public Version PatchLevel
        {
            get { return patchLevel; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the time stamp of this release.
        /// </summary>
        private DateTime? timeStamp;
        /// <summary>
        /// Gets the time stamp of this release.
        /// </summary>
        public DateTime? TimeStamp
        {
            get { return timeStamp; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the build type of this release, if any.
        /// </summary>
        private BuildType? buildType;
        /// <summary>
        /// Gets the build type of this release, if any.
        /// </summary>
        public BuildType? BuildType
        {
            get { return buildType; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the base URI used to download this release.
        /// </summary>
        private Uri baseUri;
        /// <summary>
        /// Gets the base URI used to download this release.
        /// </summary>
        public Uri BaseUri
        {
            get { return baseUri; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the format string used to build the relative download URI.
        /// </summary>
        private string uriFormat;
        /// <summary>
        /// Gets the format string used to build the relative download URI.
        /// </summary>
        public string UriFormat
        {
            get { return uriFormat; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the expected MD5 hash of the release file.
        /// </summary>
        private byte[] md5Hash;
        /// <summary>
        /// Gets the expected MD5 hash of the release file.
        /// </summary>
        public byte[] Md5Hash
        {
            get { return md5Hash; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the expected SHA1 hash of the release file.
        /// </summary>
        private byte[] sha1Hash;
        /// <summary>
        /// Gets the expected SHA1 hash of the release file.
        /// </summary>
        public byte[] Sha1Hash
        {
            get { return sha1Hash; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the expected SHA512 hash of the release file.
        /// </summary>
        private byte[] sha512Hash;
        /// <summary>
        /// Gets the expected SHA512 hash of the release file.
        /// </summary>
        public byte[] Sha512Hash
        {
            get { return sha512Hash; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the notes associated with this release.
        /// </summary>
        private string notes;
        /// <summary>
        /// Gets the notes associated with this release.
        /// </summary>
        public string Notes
        {
            get { return notes; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this release has valid required
        /// field values.  Diagnostic messages are emitted for any field that is
        /// found to be invalid.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether this release's patch level is equal
        /// to the configuration's patch level.  This property also reports true
        /// when the configuration is forced.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether this release's patch level is
        /// greater than the configuration's patch level.  This property also
        /// reports true when the configuration is forced.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether this release uses the build
        /// protocol.
        /// </summary>
        public bool IsBuild
        {
            get { return IsBuildProtocol(protocolId); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this release uses the script
        /// protocol.
        /// </summary>
        public bool IsScript
        {
            get { return IsScriptProtocol(protocolId); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this release uses the self protocol
        /// (i.e. it describes the updater itself).
        /// </summary>
        public bool IsSelf
        {
            get { return IsSelfProtocol(protocolId); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this release uses the plugin
        /// protocol.
        /// </summary>
        public bool IsPlugin
        {
            get { return IsPluginProtocol(protocolId); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Returns a string representation of this release, consisting of its
        /// patch level and time stamp.
        /// </summary>
        /// <returns>
        /// The string representation of this release.
        /// </returns>
        public override string ToString()
        {
            return String.Format("{0} ({1})",
                FormatOps.ValueToString(patchLevel),
                FormatOps.ValueToString(timeStamp));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Emits a diagnostic trace of every field and computed property of
        /// this release, for debugging purposes.
        /// </summary>
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

        /// <summary>
        /// Verifies that the specified file matches this release, optionally
        /// checking its strong name and public key token, and always checking
        /// its MD5, SHA1, and SHA512 hashes against the recorded values.
        /// </summary>
        /// <param name="configuration">
        /// The configuration to use while verifying the file and emitting
        /// diagnostic messages.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to verify.
        /// </param>
        /// <param name="strongName">
        /// Non-zero to verify the strong name signature and public key token of
        /// the file in addition to its hashes.
        /// </param>
        /// <returns>
        /// True if the file exists and matches this release; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Attempts to create a release by parsing a single line of release
        /// data.
        /// </summary>
        /// <param name="configuration">
        /// The configuration to associate with the created release.
        /// </param>
        /// <param name="releaseId">
        /// The unique identifier to assign to the created release.
        /// </param>
        /// <param name="lineIndex">
        /// The zero-based index of the line being parsed, used in diagnostic
        /// messages.
        /// </param>
        /// <param name="line">
        /// The line of release data to parse.
        /// </param>
        /// <param name="release">
        /// Upon success, receives the created release; upon failure, receives
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// True if the release was created successfully; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Converts the specified build type to its string form for use in a
        /// download URI, appending a digit separator when the result ends in a
        /// digit.
        /// </summary>
        /// <param name="buildType">
        /// The build type to convert.
        /// </param>
        /// <returns>
        /// The string form of the build type, or the empty string when the
        /// build type is the default.
        /// </returns>
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

        /// <summary>
        /// Converts the specified release type to its string form for use in a
        /// download URI, appending a digit separator when the result ends in a
        /// digit.
        /// </summary>
        /// <param name="releaseType">
        /// The release type to convert.
        /// </param>
        /// <returns>
        /// The string form of the release type.
        /// </returns>
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

        /// <summary>
        /// Builds the relative download URI for this release using its URI
        /// format (falling back to the configuration's or the default build URI
        /// format) and the specified build and release types.
        /// </summary>
        /// <param name="buildType">
        /// The build type to incorporate into the relative URI.
        /// </param>
        /// <param name="releaseType">
        /// The release type to incorporate into the relative URI.
        /// </param>
        /// <returns>
        /// The formatted relative URI, or null when no URI format is available.
        /// </returns>
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
        /// <summary>
        /// Gets the build type of this release, falling back to the default
        /// build type when none is set.
        /// </summary>
        /// <returns>
        /// The build type of this release, or the default build type.
        /// </returns>
        public BuildType BuildTypeOrDefault()
        {
            if (buildType != null)
                return (BuildType)buildType;

            return Defaults.BuildType;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public URI Methods
        /// <summary>
        /// When this release has no base URI, attempts to use the download base
        /// URI declared by the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose download base URI attribute is consulted.
        /// </param>
        /// <returns>
        /// True if a base URI was obtained from the assembly and applied;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Creates the absolute download URI for this release by combining its
        /// base URI with the relative URI built from the specified build and
        /// release types.
        /// </summary>
        /// <param name="buildType">
        /// The build type to incorporate into the relative URI.
        /// </param>
        /// <param name="releaseType">
        /// The release type to incorporate into the relative URI.
        /// </param>
        /// <returns>
        /// The absolute download URI, or null when no base URI is set, no
        /// relative URI could be built, or the combination failed.
        /// </returns>
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
        /// <summary>
        /// Atomically generates the next unique release identifier.
        /// </summary>
        /// <returns>
        /// The next unique release identifier.
        /// </returns>
        private static int NextId()
        {
            return Interlocked.Increment(ref nextId);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Tracing Methods
        /// <summary>
        /// Emits a diagnostic trace message describing the specified exception.
        /// </summary>
        /// <param name="configuration">
        /// The configuration controlling tracing behavior.
        /// </param>
        /// <param name="exception">
        /// The exception to trace.
        /// </param>
        /// <param name="category">
        /// The trace category to use.
        /// </param>
        /// <returns>
        /// The formatted trace message that was emitted.
        /// </returns>
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

        /// <summary>
        /// Emits the specified diagnostic trace message.
        /// </summary>
        /// <param name="configuration">
        /// The configuration controlling tracing behavior.
        /// </param>
        /// <param name="message">
        /// The trace message to emit.
        /// </param>
        /// <param name="category">
        /// The trace category to use.
        /// </param>
        /// <returns>
        /// The trace message that was emitted.
        /// </returns>
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
        /// <summary>
        /// Determines whether the specified protocol identifier represents the
        /// build protocol.
        /// </summary>
        /// <param name="protocolId">
        /// The protocol identifier to test.
        /// </param>
        /// <returns>
        /// True if the protocol identifier represents the build protocol;
        /// otherwise, false.
        /// </returns>
        private static bool IsBuildProtocol(
            string protocolId
            )
        {
            return StringOps.SystemEquals(protocolId, Protocol.Build);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified protocol identifier represents the
        /// script protocol.
        /// </summary>
        /// <param name="protocolId">
        /// The protocol identifier to test.
        /// </param>
        /// <returns>
        /// True if the protocol identifier represents the script protocol;
        /// otherwise, false.
        /// </returns>
        private static bool IsScriptProtocol(
            string protocolId
            )
        {
            return StringOps.SystemEquals(protocolId, Protocol.Script);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified protocol identifier represents the
        /// self protocol.
        /// </summary>
        /// <param name="protocolId">
        /// The protocol identifier to test.
        /// </param>
        /// <returns>
        /// True if the protocol identifier represents the self protocol;
        /// otherwise, false.
        /// </returns>
        private static bool IsSelfProtocol(
            string protocolId
            )
        {
            return StringOps.SystemEquals(protocolId, Protocol.Self);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified protocol identifier represents the
        /// plugin protocol.
        /// </summary>
        /// <param name="protocolId">
        /// The protocol identifier to test.
        /// </param>
        /// <returns>
        /// True if the protocol identifier represents the plugin protocol;
        /// otherwise, false.
        /// </returns>
        private static bool IsPluginProtocol(
            string protocolId
            )
        {
            return StringOps.SystemEquals(protocolId, Protocol.Plugin);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Parses a single line of release data into a release instance.
        /// </summary>
        /// <param name="configuration">
        /// The configuration to associate with the created release.
        /// </param>
        /// <param name="releaseId">
        /// The unique identifier to assign to the created release.
        /// </param>
        /// <param name="lineIndex">
        /// The zero-based index of the line being parsed, used in diagnostic
        /// messages.
        /// </param>
        /// <param name="line">
        /// The line of release data to parse.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// The created release, or null when the line could not be parsed.
        /// </returns>
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
        /// <summary>
        /// Parses the raw release data text into a dictionary of releases keyed
        /// by configuration, tallying the number of releases encountered for
        /// each protocol.
        /// </summary>
        /// <param name="configuration">
        /// The configuration to associate with the parsed releases.
        /// </param>
        /// <param name="text">
        /// The raw release data text to parse.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer used to key the releases dictionary.  When
        /// null on entry, a default comparer is created and returned.
        /// </param>
        /// <param name="releases">
        /// The dictionary that receives the parsed releases keyed by
        /// configuration.  When null on entry, a new dictionary is created and
        /// returned.
        /// </param>
        /// <param name="protocolCounts">
        /// The array that receives the counts of releases encountered for each
        /// protocol (build, script, self, plugin, and other).  When null on
        /// entry, a new array is created and returned.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// True if at least one release was parsed; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Finds the release matching the specified configuration, optionally
        /// requiring that it be valid, equal to, and/or greater than the
        /// configuration's patch level.
        /// </summary>
        /// <param name="configuration">
        /// The configuration to look up.
        /// </param>
        /// <param name="releases">
        /// The dictionary of releases to search.
        /// </param>
        /// <param name="valid">
        /// Non-zero to require that the matching release be valid.
        /// </param>
        /// <param name="equal">
        /// Non-zero to require that the matching release be equal to the
        /// configuration's patch level.
        /// </param>
        /// <param name="greater">
        /// Non-zero to require that the matching release be greater than the
        /// configuration's patch level.
        /// </param>
        /// <returns>
        /// The matching release, or null when none is found.
        /// </returns>
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

        /// <summary>
        /// Finds the release describing the updater itself that matches the
        /// specified configuration, optionally requiring that it be valid,
        /// equal to, and/or greater than the configuration's patch level.
        /// </summary>
        /// <param name="configuration">
        /// The configuration to look up, which is reconstituted with the self
        /// protocol before searching.
        /// </param>
        /// <param name="releases">
        /// The dictionary of releases to search.
        /// </param>
        /// <param name="valid">
        /// Non-zero to require that the matching release be valid.
        /// </param>
        /// <param name="equal">
        /// Non-zero to require that the matching release be equal to the
        /// configuration's patch level.
        /// </param>
        /// <param name="greater">
        /// Non-zero to require that the matching release be greater than the
        /// configuration's patch level.
        /// </param>
        /// <returns>
        /// The matching self release, or null when none is found.
        /// </returns>
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
