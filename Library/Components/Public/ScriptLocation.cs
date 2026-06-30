/*
 * ScriptLocation.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a location within a script -- a file name together
    /// with a starting and ending line number -- and tracks whether that
    /// location was reached via the <c>source</c> command.  It is used by the
    /// engine to associate scripts and commands with their originating file and
    /// line range, and it provides helpers for normalizing and matching file
    /// names as well as comparing locations.  It implements
    /// <see cref="IScriptLocation" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("6b581ceb-8520-4185-8775-be85e19350f3")]
    public sealed class ScriptLocation :
        IHaveInterpreter,
        IScriptLocation,
        ICloneable,
        IComparer<IScriptLocation>,
        IEqualityComparer<IScriptLocation>
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an empty script location.  This constructor is used by the
        /// other constructor overloads.
        /// </summary>
        private ScriptLocation()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a script location from the specified interpreter, file
        /// name, line range, and source flag.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with this script location.  This parameter
        /// may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file for this script location.  This parameter may be
        /// null.
        /// </param>
        /// <param name="startLine">
        /// The starting line number for this script location.
        /// </param>
        /// <param name="endLine">
        /// The ending line number for this script location.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if this script location was reached via the <c>source</c>
        /// command.
        /// </param>
        private ScriptLocation(
            Interpreter interpreter,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource
            )
            : this()
        {
            this.interpreter = interpreter;
            this.fileName = fileName;
            this.startLine = startLine;
            this.endLine = endLine;
            this.viaSource = viaSource;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a script location by copying the property values from the
        /// specified script location.
        /// </summary>
        /// <param name="location">
        /// The script location to copy.  This parameter may be null, in which
        /// case all properties are left at their default values.
        /// </param>
        private ScriptLocation(
            IScriptLocation location
            )
            : this()
        {
            if (location != null)
            {
                IGetInterpreter getInterpreter = location as IGetInterpreter;

                if (getInterpreter != null)
                    this.interpreter = getInterpreter.Interpreter;

                this.fileName = location.FileName;
                this.startLine = location.StartLine;
                this.endLine = location.EndLine;
                this.viaSource = location.ViaSource;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// Creates a new script location with an unknown line range using the
        /// specified interpreter, file name, and source flag.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the new script location.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file for the new script location.  This parameter may
        /// be null.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if the new script location was reached via the <c>source</c>
        /// command.
        /// </param>
        /// <returns>
        /// The newly created script location.
        /// </returns>
        public static IScriptLocation Create(
            Interpreter interpreter,
            string fileName,
            bool viaSource
            )
        {
            return Create(
                interpreter, fileName, Parser.UnknownLine, viaSource);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new script location with an unknown ending line using the
        /// specified interpreter, file name, starting line, and source flag.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the new script location.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file for the new script location.  This parameter may
        /// be null.
        /// </param>
        /// <param name="startLine">
        /// The starting line number for the new script location.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if the new script location was reached via the <c>source</c>
        /// command.
        /// </param>
        /// <returns>
        /// The newly created script location.
        /// </returns>
        public static IScriptLocation Create(
            Interpreter interpreter,
            string fileName,
            int startLine,
            bool viaSource
            )
        {
            return Create(
                interpreter, fileName, startLine, Parser.UnknownLine,
                viaSource);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new script location using the specified interpreter, file
        /// name, line range, and source flag.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the new script location.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file for the new script location.  This parameter may
        /// be null.
        /// </param>
        /// <param name="startLine">
        /// The starting line number for the new script location.
        /// </param>
        /// <param name="endLine">
        /// The ending line number for the new script location.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if the new script location was reached via the <c>source</c>
        /// command.
        /// </param>
        /// <returns>
        /// The newly created script location.
        /// </returns>
        public static IScriptLocation Create(
            Interpreter interpreter,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource
            )
        {
            return new ScriptLocation(
                interpreter, fileName, startLine, endLine, viaSource);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new script location by copying the property values from the
        /// specified script location.
        /// </summary>
        /// <param name="location">
        /// The script location to copy.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created script location.
        /// </returns>
        public static IScriptLocation Create(
            IScriptLocation location
            )
        {
            return new ScriptLocation(location);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method checks whether the specified script location and pattern
        /// are both valid candidates for matching, optionally ignoring their
        /// file names.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when comparing file names.  This parameter may
        /// be null.
        /// </param>
        /// <param name="location">
        /// The script location to check.  This parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The script location pattern to check.  This parameter may be null.
        /// </param>
        /// <param name="noFile">
        /// Non-zero to skip the file name comparison.
        /// </param>
        /// <returns>
        /// True if both script locations are valid candidates for matching;
        /// otherwise, false.
        /// </returns>
        private static bool Check(
            Interpreter interpreter,
            IScriptLocation location,
            IScriptLocation pattern,
            bool noFile
            )
        {
            if ((location == null) || (pattern == null))
                return false;

            if (!noFile &&
                !MatchFileName(interpreter, location.FileName, pattern.FileName))
            {
                return false; // NOTE: Different file name...
            }

            if ((location.StartLine == Parser.NoLine) ||
                (location.EndLine == Parser.NoLine) ||
                (pattern.StartLine == Parser.NoLine) ||
                (pattern.EndLine == Parser.NoLine))
            {
                return false; // NOTE: Cannot match location with "no line".
            }

            if ((location.StartLine != Parser.AnyLine) &&
                (location.EndLine != Parser.AnyLine) &&
                (location.StartLine > location.EndLine))
            {
                return false; // NOTE: Invalid, start after end?
            }

            if ((pattern.StartLine != Parser.AnyLine) &&
                (pattern.EndLine != Parser.AnyLine) &&
                (pattern.StartLine > pattern.EndLine))
            {
                return false; // NOTE: Invalid, start after end?
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /// <summary>
        /// This method normalizes the specified file name, resolving it to a
        /// full Unix-style path when it contains a directory and no path
        /// wildcards.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when resolving the full path.  This parameter
        /// may be null.
        /// </param>
        /// <param name="fileName">
        /// The file name to normalize.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The normalized file name, or the original file name when it cannot be
        /// normalized.
        /// </returns>
        public static string NormalizeFileName(
            Interpreter interpreter,
            string fileName
            )
        {
            if (!PathOps.HasPathWildcard(fileName) &&
                PathOps.HasDirectory(fileName))
            {
                return PathOps.GetUnixPath(PathOps.ResolveFullPath(
                    interpreter, fileName));
            }

            return fileName;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the file names of the two specified
        /// script locations match, optionally requiring an exact match that
        /// disallows path wildcards.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when comparing the file names.  This parameter
        /// may be null.
        /// </param>
        /// <param name="location1">
        /// The first script location.  This parameter may be null.
        /// </param>
        /// <param name="location2">
        /// The second script location.  This parameter may be null.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require an exact match, disallowing path wildcards in
        /// either file name.
        /// </param>
        /// <returns>
        /// True if the file names match; otherwise, false.
        /// </returns>
        public static bool MatchFileName(
            Interpreter interpreter,
            IScriptLocation location1,
            IScriptLocation location2,
            bool exact
            )
        {
            if ((location1 == null) && (location2 == null))
            {
                return true;
            }
            else if ((location1 == null) || (location2 == null))
            {
                return false;
            }
            else if (exact)
            {
                if (PathOps.HasPathWildcard(location1.FileName) ||
                    PathOps.HasPathWildcard(location2.FileName))
                {
                    return false;
                }
            }

            return MatchFileName(interpreter, location1.FileName, location2.FileName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the two specified paths match, using
        /// pattern matching when the second path contains a path wildcard and
        /// same-file comparison otherwise.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when matching or comparing the paths.  This
        /// parameter may be null.
        /// </param>
        /// <param name="path1">
        /// The first path.  This parameter may be null.
        /// </param>
        /// <param name="path2">
        /// The second path, which may contain a path wildcard.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the paths match; otherwise, false.
        /// </returns>
        public static bool MatchFileName(
            Interpreter interpreter,
            string path1,
            string path2
            )
        {
            //
            // BUGBUG: This might be too slow?
            //
            if (PathOps.HasPathWildcard(path2))
            {
                return StringOps.Match(
                    interpreter, StringOps.DefaultMatchMode,
                    path1, path2, PathOps.NoCase);
            }
            else
            {
                return PathOps.IsSameFile(interpreter, path1, path2);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether the specified script location is valid,
        /// optionally ignoring its file name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the check.  This parameter may be
        /// null.
        /// </param>
        /// <param name="location">
        /// The script location to check.  This parameter may be null.
        /// </param>
        /// <param name="noFile">
        /// Non-zero to skip the file name validation.
        /// </param>
        /// <returns>
        /// True if the script location is valid; otherwise, false.
        /// </returns>
        public static bool Check(
            Interpreter interpreter,
            IScriptLocation location,
            bool noFile
            )
        {
            if (location == null)
                return false;

            //
            // NOTE: *WARNING: Empty file names are allowed here, please do
            //       not change this to String.IsNullOrEmpty.
            //
            if (!noFile && (location.FileName == null))
                return false; // NOTE: Invalid file name...

            if ((location.StartLine == Parser.NoLine) ||
                (location.EndLine == Parser.NoLine))
            {
                return false; // NOTE: Cannot match location with "no line".
            }

            if ((location.StartLine != Parser.AnyLine) &&
                (location.EndLine != Parser.AnyLine) &&
                (location.StartLine > location.EndLine))
            {
                return false; // NOTE: Invalid, start after end?
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified script location matches
        /// the specified script location pattern, comparing their file names and
        /// overlapping line ranges.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when comparing file names.  This parameter may
        /// be null.
        /// </param>
        /// <param name="location">
        /// The script location to test.  This parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The script location pattern to match against.  This parameter may be
        /// null.
        /// </param>
        /// <param name="noFile">
        /// Non-zero to skip the file name comparison.
        /// </param>
        /// <returns>
        /// True if the script location matches the pattern; otherwise, false.
        /// </returns>
        public static bool Match(
            Interpreter interpreter,
            IScriptLocation location,
            IScriptLocation pattern,
            bool noFile
            )
        {
            if (!Check(interpreter, location, pattern, noFile))
                return false;

            if (((location.StartLine != Parser.AnyLine) &&
                (pattern.EndLine != Parser.AnyLine) &&
                (location.StartLine > pattern.EndLine)) ||
                ((pattern.StartLine != Parser.AnyLine) &&
                (location.EndLine != Parser.AnyLine) &&
                (pattern.StartLine > location.EndLine)))
            {
                return false;
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        /// <summary>
        /// The interpreter associated with this script location.
        /// </summary>
#if SERIALIZATION
        [NonSerialized()]
#endif
        private Interpreter interpreter;

        /// <summary>
        /// Gets or sets the interpreter associated with this script location.
        /// </summary>
        public Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IScriptLocation Members
        /// <summary>
        /// The name of the file for this script location.
        /// </summary>
        private string fileName;

        /// <summary>
        /// Gets or sets the name of the file for this script location.
        /// </summary>
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The starting line number for this script location.
        /// </summary>
        private int startLine;

        /// <summary>
        /// Gets or sets the starting line number for this script location.
        /// </summary>
        public int StartLine
        {
            get { return startLine; }
            set { startLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The ending line number for this script location.
        /// </summary>
        private int endLine;

        /// <summary>
        /// Gets or sets the ending line number for this script location.
        /// </summary>
        public int EndLine
        {
            get { return endLine; }
            set { endLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if this script location was reached via the <c>source</c>
        /// command.
        /// </summary>
        private bool viaSource;

        /// <summary>
        /// Gets or sets a value indicating whether this script location was
        /// reached via the <c>source</c> command.
        /// </summary>
        public bool ViaSource
        {
            get { return viaSource; }
            set { viaSource = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs representing the
        /// properties of this script location.
        /// </summary>
        /// <returns>
        /// The list of name/value pairs representing this script location.
        /// </returns>
        public StringPairList ToList()
        {
            return ToList(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs representing the
        /// properties of this script location, optionally scrubbing the file
        /// name of its base path.
        /// </summary>
        /// <param name="scrub">
        /// Non-zero to scrub the base path from the file name.
        /// </param>
        /// <returns>
        /// The list of name/value pairs representing this script location.
        /// </returns>
        public StringPairList ToList(bool scrub)
        {
            StringPairList list = new StringPairList();

            list.Add("FileName", scrub ? PathOps.ScrubPath(
                GlobalState.GetBasePath(), fileName) : fileName);

            list.Add("StartLine", startLine.ToString());
            list.Add("EndLine", endLine.ToString());
            list.Add("ViaSource", viaSource.ToString());

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method determines whether the specified object is equal to this
        /// script location, comparing the file name and line range.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with this script location.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// True if the specified object is equal to this script location;
        /// otherwise, false.
        /// </returns>
        public override bool Equals(
            object obj
            )
        {
            if (obj == null)
                return false;

            if (Object.ReferenceEquals(obj, this))
                return true;

            IScriptLocation location = obj as IScriptLocation;

            if (location == null)
                return false;

            if (!MatchFileName(interpreter, fileName, location.FileName))
                return false;

            if (startLine != location.StartLine)
                return false;

            if (endLine != location.EndLine)
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for this script location, combining
        /// the hash codes of the file name and line range.
        /// </summary>
        /// <returns>
        /// The hash code for this script location.
        /// </returns>
        public override int GetHashCode()
        {
            return CommonOps.HashCodes.Combine(
                PathOps.GetHashCode(fileName),
                startLine.GetHashCode(),
                endLine.GetHashCode());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of this script location,
        /// consisting of the file name and line range formatted as a list.
        /// </summary>
        /// <returns>
        /// The string representation of this script location.
        /// </returns>
        public override string ToString()
        {
            return StringList.MakeList(fileName, startLine, endLine);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new script location that is a copy of this
        /// script location.
        /// </summary>
        /// <returns>
        /// The newly created copy of this script location.
        /// </returns>
        public object Clone()
        {
            return new ScriptLocation(
                interpreter, fileName, startLine, endLine, viaSource);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IComparer<IScriptLocation> Members
        /// <summary>
        /// This method compares two script locations, ordering them by file
        /// name, then by starting line, then by ending line.
        /// </summary>
        /// <param name="x">
        /// The first script location to compare.  This parameter may be null.
        /// </param>
        /// <param name="y">
        /// The second script location to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the script locations are equal, a negative number if the
        /// first sorts before the second, or a positive number if the first
        /// sorts after the second.
        /// </returns>
        public int Compare(
            IScriptLocation x,
            IScriptLocation y
            )
        {
            if ((x == null) && (y == null))
            {
                return 0;
            }
            else if (x == null)
            {
                return -1;
            }
            else if (y == null)
            {
                return 1;
            }
            else
            {
                int result = PathOps.CompareFileNames(x.FileName,
                    y.FileName);

                if (result != 0)
                    return result;

                result = LogicOps.Compare(x.StartLine, y.StartLine);

                if (result != 0)
                    return result;

                return LogicOps.Compare(x.EndLine, y.EndLine);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<IScriptLocation> Members
        /// <summary>
        /// This method determines whether two script locations are equal.
        /// </summary>
        /// <param name="x">
        /// The first script location to compare.  This parameter may be null.
        /// </param>
        /// <param name="y">
        /// The second script location to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two script locations are equal; otherwise, false.
        /// </returns>
        public bool Equals(
            IScriptLocation x,
            IScriptLocation y
            )
        {
            return GenericOps<IScriptLocation>.EqualityComparerEquals(this, x, y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for the specified script location.
        /// </summary>
        /// <param name="obj">
        /// The script location for which to compute a hash code.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The hash code for the specified script location.
        /// </returns>
        public int GetHashCode(
            IScriptLocation obj
            )
        {
            return GenericOps<IScriptLocation>.EqualityComparerGetHashCode(this, obj);
        }
        #endregion
    }
}
