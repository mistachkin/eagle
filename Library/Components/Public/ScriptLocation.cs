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
        private ScriptLocation()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static IScriptLocation Create(
            IScriptLocation location
            )
        {
            return new ScriptLocation(location);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Static Methods
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
#if SERIALIZATION
        [NonSerialized()]
#endif
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IScriptLocation Members
        private string fileName;
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int startLine;
        public int StartLine
        {
            get { return startLine; }
            set { startLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int endLine;
        public int EndLine
        {
            get { return endLine; }
            set { endLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool viaSource;
        public bool ViaSource
        {
            get { return viaSource; }
            set { viaSource = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringPairList ToList()
        {
            return ToList(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public override int GetHashCode()
        {
            return CommonOps.HashCodes.Combine(
                PathOps.GetHashCode(fileName),
                startLine.GetHashCode(),
                endLine.GetHashCode());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            return StringList.MakeList(fileName, startLine, endLine);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new ScriptLocation(
                interpreter, fileName, startLine, endLine, viaSource);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IComparer<IScriptLocation> Members
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
        public bool Equals(
            IScriptLocation x,
            IScriptLocation y
            )
        {
            return GenericOps<IScriptLocation>.EqualityComparerEquals(this, x, y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int GetHashCode(
            IScriptLocation obj
            )
        {
            return GenericOps<IScriptLocation>.EqualityComparerGetHashCode(this, obj);
        }
        #endregion
    }
}
