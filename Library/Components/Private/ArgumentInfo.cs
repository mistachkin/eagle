/*
 * ArgumentInfo.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("df085969-cedc-4984-b73f-ac79af50da08")]
    internal sealed class ArgumentInfo
    {
        #region Private Constructors
        private ArgumentInfo()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentInfo(
            int index,
            Type type,
            string name,
            int[] counts,
            bool input,
            bool output
            )
        {
            this.index = index;
            this.type = type;
            this.name = name;
            this.counts = counts;
            this.input = input;
            this.output = output;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static ArgumentInfo Create(
            int index,
            Type type,
            string name,
            bool input,
            bool output
            )
        {
            return new ArgumentInfo(
                index, type, name, new int[1], input, output);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Helper" Methods
        public static int QueryCount(
            ArgumentInfo argumentInfo,
            int index
            )
        {
            if (argumentInfo != null)
            {
                int[] counts = argumentInfo.Counts;

                if ((counts != null) &&
                    (index >= 0) && (index < counts.Length))
                {
                    return counts[index];
                }
            }

            return Count.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ResetCount(
            ArgumentInfo argumentInfo,
            int index
            )
        {
            if (argumentInfo != null)
            {
                int[] counts = argumentInfo.Counts;

                if ((counts != null) &&
                    (index >= 0) && (index < counts.Length))
                {
                    counts[index] = 0;

                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IncrementCount(
            ArgumentInfo argumentInfo,
            int index
            )
        {
            if (argumentInfo != null)
            {
                int[] counts = argumentInfo.Counts;

                if ((counts != null) &&
                    (index >= 0) && (index < counts.Length))
                {
                    counts[index]++;

                    return true;
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private int index;
        public int Index
        {
            get { return index; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Type type;
        public Type Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public string Name
        {
            get { return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int[] counts;
        public int[] Counts
        {
            get { return counts; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool input;
        public bool Input
        {
            get { return input; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool output;
        public bool Output
        {
            get { return output; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void SetName(
            string name
            )
        {
            this.name = name;
        }

        ///////////////////////////////////////////////////////////////////////

        public IStringList ToList()
        {
            IStringList list = new StringPairList();

            list.Add("Index", index.ToString());
            list.Add("Type", (type != null) ? type.ToString() : null);
            list.Add("Name", name);

            list.Add("Counts", (counts != null) ?
                new IntList(counts).ToString() : null);

            list.Add("Input", input.ToString());
            list.Add("Output", output.ToString());

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToList().ToString();
        }
        #endregion
    }
}
