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
    /// <summary>
    /// This class stores metadata about a single argument used during method
    /// binding, including its position, type, and name, the per-slot usage
    /// counts, and whether it is used for input and/or output.
    /// </summary>
    [ObjectId("df085969-cedc-4984-b73f-ac79af50da08")]
    internal sealed class ArgumentInfo
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        private ArgumentInfo()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified argument
        /// metadata.
        /// </summary>
        /// <param name="index">
        /// The zero-based position of the argument.
        /// </param>
        /// <param name="type">
        /// The type of the argument.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This parameter may be null.
        /// </param>
        /// <param name="counts">
        /// The array of per-slot usage counts associated with the argument.
        /// This parameter may be null.
        /// </param>
        /// <param name="input">
        /// Non-zero if the argument is used for input.
        /// </param>
        /// <param name="output">
        /// Non-zero if the argument is used for output.
        /// </param>
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
        /// <summary>
        /// This method creates a new instance of this class using the
        /// specified argument metadata, allocating a single per-slot usage
        /// count.
        /// </summary>
        /// <param name="index">
        /// The zero-based position of the argument.
        /// </param>
        /// <param name="type">
        /// The type of the argument.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This parameter may be null.
        /// </param>
        /// <param name="input">
        /// Non-zero if the argument is used for input.
        /// </param>
        /// <param name="output">
        /// Non-zero if the argument is used for output.
        /// </param>
        /// <returns>
        /// The newly created instance of this class.
        /// </returns>
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
        /// <summary>
        /// This method queries the per-slot usage count at the specified index
        /// for the specified argument metadata.
        /// </summary>
        /// <param name="argumentInfo">
        /// The argument metadata to query.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the usage count slot to query.
        /// </param>
        /// <returns>
        /// The usage count at the specified index, or
        /// <see cref="Count.Invalid" /> if it cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method resets the per-slot usage count at the specified index
        /// to zero for the specified argument metadata.
        /// </summary>
        /// <param name="argumentInfo">
        /// The argument metadata to modify.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the usage count slot to reset.
        /// </param>
        /// <returns>
        /// True if the usage count was reset; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method increments the per-slot usage count at the specified
        /// index for the specified argument metadata.
        /// </summary>
        /// <param name="argumentInfo">
        /// The argument metadata to modify.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the usage count slot to increment.
        /// </param>
        /// <returns>
        /// True if the usage count was incremented; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Stores the zero-based position of the argument.
        /// </summary>
        private int index;
        /// <summary>
        /// Gets the zero-based position of the argument.
        /// </summary>
        public int Index
        {
            get { return index; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the type of the argument.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets the type of the argument.
        /// </summary>
        public Type Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the argument.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets the name of the argument.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the array of per-slot usage counts associated with the
        /// argument.
        /// </summary>
        private int[] counts;
        /// <summary>
        /// Gets the array of per-slot usage counts associated with the
        /// argument.
        /// </summary>
        public int[] Counts
        {
            get { return counts; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the argument is used for input.
        /// </summary>
        private bool input;
        /// <summary>
        /// Gets a value indicating whether the argument is used for input.
        /// </summary>
        public bool Input
        {
            get { return input; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the argument is used for output.
        /// </summary>
        private bool output;
        /// <summary>
        /// Gets a value indicating whether the argument is used for output.
        /// </summary>
        public bool Output
        {
            get { return output; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method sets the name of the argument.
        /// </summary>
        /// <param name="name">
        /// The new name of the argument.  This parameter may be null.
        /// </param>
        public void SetName(
            string name
            )
        {
            this.name = name;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs representing the
        /// metadata stored by this instance.
        /// </summary>
        /// <returns>
        /// A list of name/value pairs representing the metadata stored by this
        /// instance.
        /// </returns>
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
        /// <summary>
        /// This method returns the string representation of the metadata
        /// stored by this instance.
        /// </summary>
        /// <returns>
        /// The string representation of the metadata stored by this instance.
        /// </returns>
        public override string ToString()
        {
            return ToList().ToString();
        }
        #endregion
    }
}
