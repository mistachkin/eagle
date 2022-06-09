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
    [ObjectId("47d48277-9664-457a-bf97-9e4b73f4199d")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class TclBuild
    {
        public TclBuild()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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

        private FindFlags findFlags;
        public FindFlags FindFlags
        {
            get { return findFlags; }
            set { findFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private LoadFlags loadFlags;
        public LoadFlags LoadFlags
        {
            get { return loadFlags; }
            set { loadFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private object findData;
        public object FindData
        {
            get { return findData; }
            set { findData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string fileName;
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Priority priority;
        public Priority Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Sequence sequence;
        public Sequence Sequence
        {
            get { return sequence; }
            set { sequence = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private OperatingSystemId operatingSystemId;
        public OperatingSystemId OperatingSystemId
        {
            get { return operatingSystemId; }
            set { operatingSystemId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Version version;
        public Version Version
        {
            get { return version; }
            set { version = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Version patchLevel;
        public Version PatchLevel
        {
            get { return patchLevel; }
            set { patchLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Tcl_ReleaseLevel releaseLevel;
        public Tcl_ReleaseLevel ReleaseLevel
        {
            get { return releaseLevel; }
            set { releaseLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ushort magic;
        public ushort Magic
        {
            get { return magic; }
            set { magic = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool threaded;
        public bool Threaded
        {
            get { return threaded; }
            set { threaded = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool debug;
        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool DefaultThreaded
        {
            get { return TclWrapper.IsBuildDefaultThreaded(this); }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool MatchDebug
        {
            get { return debug == Build.Debug; }
        }

        ///////////////////////////////////////////////////////////////////////

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
