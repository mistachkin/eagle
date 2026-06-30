/*
 * TclBuild.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

namespace Eagle._Components.Private.Tcl
{
    /// <summary>
    /// This class represents a discovered, native Tcl library build.  It
    /// captures how the library was located and loaded together with the
    /// descriptive metadata extracted from it (its file name, version and
    /// patch level, release level, operating system, threading and debug
    /// flags, and so on), and provides a convenient string representation
    /// of that information.
    /// </summary>
    [ObjectId("47d48277-9664-457a-bf97-9e4b73f4199d")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class TclBuild
    {
        /// <summary>
        /// Constructs an empty Tcl build instance.  All of its properties are
        /// left at their default values.
        /// </summary>
        public TclBuild()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a Tcl build instance from the supplied discovery, load,
        /// and descriptive metadata.
        /// </summary>
        /// <param name="findFlags">
        /// The flags that controlled how this Tcl build was located.
        /// </param>
        /// <param name="loadFlags">
        /// The flags that controlled how this Tcl build was loaded.
        /// </param>
        /// <param name="findData">
        /// The opaque data associated with locating this Tcl build, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The file name of the native Tcl library for this build.
        /// </param>
        /// <param name="priority">
        /// The priority assigned to this Tcl build relative to others.
        /// </param>
        /// <param name="sequence">
        /// The sequence number used to order this Tcl build relative to
        /// others.
        /// </param>
        /// <param name="operatingSystemId">
        /// The identifier of the operating system this Tcl
        /// build targets.
        /// </param>
        /// <param name="patchLevel">
        /// The full patch level (version) of this Tcl build.
        /// </param>
        /// <param name="releaseLevel">
        /// The release level of this Tcl build (for example, alpha,
        /// beta, or final).
        /// </param>
        /// <param name="magic">
        /// The magic number associated with this Tcl build.
        /// </param>
        /// <param name="threaded">
        /// Non-zero if this Tcl build was compiled with threading
        /// support.
        /// </param>
        /// <param name="debug">
        /// Non-zero if this Tcl build is a debug build.
        /// </param>
        public TclBuild(
            FindFlags findFlags,
            LoadFlags loadFlags,
            object findData,
            string fileName,
            Priority priority,
            Sequence sequence,
            OperatingSystemId operatingSystemId,
            Version patchLevel,
            Tcl_ReleaseLevel releaseLevel,
            ushort magic,
            bool threaded,
            bool debug
            )
        {
            this.findFlags = findFlags;
            this.loadFlags = loadFlags;
            this.findData = findData;
            this.fileName = fileName;
            this.priority = priority;
            this.sequence = sequence;
            this.operatingSystemId = operatingSystemId;
            this.patchLevel = patchLevel;
            this.releaseLevel = releaseLevel;
            this.magic = magic;
            this.threaded = threaded;
            this.debug = debug;

            //
            // NOTE: The version may only contain the major and minor parts.
            //
            this.version = GlobalState.GetTwoPartVersion(patchLevel);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that controlled how this Tcl build was located.
        /// </summary>
        private FindFlags findFlags;
        /// <summary>
        /// Gets or sets the flags that controlled how this Tcl build was
        /// located.
        /// </summary>
        public FindFlags FindFlags
        {
            get { return findFlags; }
            set { findFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that controlled how this Tcl build was loaded.
        /// </summary>
        private LoadFlags loadFlags;
        /// <summary>
        /// Gets or sets the flags that controlled how this Tcl build was
        /// loaded.
        /// </summary>
        public LoadFlags LoadFlags
        {
            get { return loadFlags; }
            set { loadFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The opaque data associated with locating this Tcl build, if any.
        /// </summary>
        private object findData;
        /// <summary>
        /// Gets or sets the opaque data associated with locating this Tcl
        /// build, if any.
        /// </summary>
        public object FindData
        {
            get { return findData; }
            set { findData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the native Tcl library for this build.
        /// </summary>
        private string fileName;
        /// <summary>
        /// Gets or sets the file name of the native Tcl library for this
        /// build.
        /// </summary>
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The priority assigned to this Tcl build relative to others.
        /// </summary>
        private Priority priority;
        /// <summary>
        /// Gets or sets the priority assigned to this Tcl build relative to
        /// others.
        /// </summary>
        public Priority Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The sequence number used to order this Tcl build relative to
        /// others.
        /// </summary>
        private Sequence sequence;
        /// <summary>
        /// Gets or sets the sequence number used to order this Tcl build
        /// relative to others.
        /// </summary>
        public Sequence Sequence
        {
            get { return sequence; }
            set { sequence = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The identifier of the operating system this Tcl build targets.
        /// </summary>
        private OperatingSystemId operatingSystemId;
        /// <summary>
        /// Gets or sets the identifier of the operating system this Tcl
        /// build targets.
        /// </summary>
        public OperatingSystemId OperatingSystemId
        {
            get { return operatingSystemId; }
            set { operatingSystemId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The two-part (major and minor) version of this Tcl build.
        /// </summary>
        private Version version;
        /// <summary>
        /// Gets or sets the two-part (major and minor) version of this Tcl
        /// build.
        /// </summary>
        public Version Version
        {
            get { return version; }
            set { version = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The full patch level (version) of this Tcl build.
        /// </summary>
        private Version patchLevel;
        /// <summary>
        /// Gets or sets the full patch level (version) of this Tcl build.
        /// </summary>
        public Version PatchLevel
        {
            get { return patchLevel; }
            set { patchLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The release level of this Tcl build (for example, alpha, beta, or
        /// final).
        /// </summary>
        private Tcl_ReleaseLevel releaseLevel;
        /// <summary>
        /// Gets or sets the release level of this Tcl build (for example,
        /// alpha, beta, or final).
        /// </summary>
        public Tcl_ReleaseLevel ReleaseLevel
        {
            get { return releaseLevel; }
            set { releaseLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The magic number associated with this Tcl build.
        /// </summary>
        private ushort magic;
        /// <summary>
        /// Gets or sets the magic number associated with this Tcl build.
        /// </summary>
        public ushort Magic
        {
            get { return magic; }
            set { magic = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if this Tcl build was compiled with threading support.
        /// </summary>
        private bool threaded;
        /// <summary>
        /// Gets or sets a value indicating whether this Tcl build was
        /// compiled with threading support.
        /// </summary>
        public bool Threaded
        {
            get { return threaded; }
            set { threaded = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if this Tcl build is a debug build.
        /// </summary>
        private bool debug;
        /// <summary>
        /// Gets or sets a value indicating whether this Tcl build is a debug
        /// build.
        /// </summary>
        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this Tcl build is threaded by
        /// default for its operating system.
        /// </summary>
        public bool DefaultThreaded
        {
            get { return TclWrapper.IsBuildDefaultThreaded(this); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the debug setting of this Tcl
        /// build matches the debug setting of the currently running Eagle
        /// build.
        /// </summary>
        public bool MatchDebug
        {
            get { return debug == Build.Debug; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a list of name and value pairs describing this Tcl build,
        /// with one entry for each of its significant properties.
        /// </summary>
        /// <returns>
        /// A list of name and value pairs describing this Tcl build.
        /// </returns>
        public StringPairList ToList()
        {
            StringPairList list = new StringPairList();

            list.Add("findFlags", findFlags.ToString());
            list.Add("loadFlags", loadFlags.ToString());
            list.Add("findData", StringOps.GetStringsFromObject(findData));
            list.Add("fileName", fileName);
            list.Add("priority", priority.ToString());
            list.Add("sequence", sequence.ToString());
            list.Add("operatingSystem", operatingSystemId.ToString());

            list.Add("version", (version != null) ?
                version.ToString() : null);

            list.Add("patchLevel", (patchLevel != null) ?
                patchLevel.ToString() : null);

            list.Add("releaseLevel", releaseLevel.ToString());
            list.Add("magic", FormatOps.Hexadecimal(magic, true));
            list.Add("threaded", threaded.ToString());
            list.Add("debug", debug.ToString());
            list.Add("defaultThreaded", DefaultThreaded.ToString());
            list.Add("matchDebug", MatchDebug.ToString());

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Returns a string representation of this Tcl build, formatted as a
        /// Tcl-dictionary-style list of its properties.
        /// </summary>
        /// <returns>
        /// A string representation of this Tcl build.
        /// </returns>
        public override string ToString()
        {
            //
            // HACK: This is not a typo.  It flattens the list of string
            //       pairs into a Tcl-dictionary-style list.
            //
            return ToList().ToList().ToString();
        }
        #endregion
    }
}
