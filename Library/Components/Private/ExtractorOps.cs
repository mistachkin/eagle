/*
 * ExtractorOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("a19ca4af-0ae2-48a5-bf68-f54c106056f3")]
    internal static class ExtractorOps
    {
        #region Single Value Extractors
        #region IAnyPair Extractors
        #region Strongly Typed Extractors
        public static bool TryExtractX<T>(
            IAnyPair anyPair,
            out T x
            )
        {
            if (anyPair == null)
            {
                x = default(T);
                return false;
            }

            object lx = anyPair.X;

            if (!MarshalOps.DoesValueMatchType(typeof(T), lx))
            {
                x = default(T);
                return false;
            }

            x = (T)lx;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractY<T>(
            IAnyPair anyPair,
            out T y
            )
        {
            if (anyPair == null)
            {
                y = default(T);
                return false;
            }

            object ly = anyPair.Y;

            if (!MarshalOps.DoesValueMatchType(typeof(T), ly))
            {
                y = default(T);
                return false;
            }

            y = (T)ly;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractZ<T>(
            IAnyPair anyPair,
            out T z
            )
        {
            z = default(T);
            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Weakly Typed Extractors
        public static bool TryExtractX(
            IAnyPair anyPair,
            out object x
            )
        {
            if (anyPair == null)
            {
                x = null;
                return false;
            }

            x = anyPair.X;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractY(
            IAnyPair anyPair,
            out object y
            )
        {
            if (anyPair == null)
            {
                y = null;
                return false;
            }

            y = anyPair.Y;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractZ(
            IAnyPair anyPair,
            out object z
            )
        {
            z = null;
            return false;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyTriplet Extractors
        #region Strongly Typed Extractors
        public static bool TryExtractX<T>(
            IAnyTriplet anyTriplet,
            out T x
            )
        {
            if (anyTriplet == null)
            {
                x = default(T);
                return false;
            }

            object lx = anyTriplet.X;

            if (!MarshalOps.DoesValueMatchType(typeof(T), lx))
            {
                x = default(T);
                return false;
            }

            x = (T)lx;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractY<T>(
            IAnyTriplet anyTriplet,
            out T y
            )
        {
            if (anyTriplet == null)
            {
                y = default(T);
                return false;
            }

            object ly = anyTriplet.Y;

            if (!MarshalOps.DoesValueMatchType(typeof(T), ly))
            {
                y = default(T);
                return false;
            }

            y = (T)ly;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractZ<T>(
            IAnyTriplet anyTriplet,
            out T z
            )
        {
            if (anyTriplet == null)
            {
                z = default(T);
                return false;
            }

            object lz = anyTriplet.Z;

            if (!MarshalOps.DoesValueMatchType(typeof(T), lz))
            {
                z = default(T);
                return false;
            }

            z = (T)lz;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Weakly Typed Extractors
        public static bool TryExtractX(
            IAnyTriplet anyTriplet,
            out object x
            )
        {
            if (anyTriplet == null)
            {
                x = null;
                return false;
            }

            x = anyTriplet.X;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractY(
            IAnyTriplet anyTriplet,
            out object y
            )
        {
            if (anyTriplet == null)
            {
                y = null;
                return false;
            }

            y = anyTriplet.Y;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractZ(
            IAnyTriplet anyTriplet,
            out object z
            )
        {
            if (anyTriplet == null)
            {
                z = null;
                return false;
            }

            z = anyTriplet.Z;
            return true;
        }
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Double Value Extractors
        #region IAnyPair Extractors
        #region Strongly Typed Extractors
        public static bool TryExtractXY<T1, T2>(
            IAnyPair anyPair,
            out T1 x,
            out T2 y 
            )
        {
            if (anyPair == null)
            {
                x = default(T1);
                y = default(T2);
                return false;
            }

            object lx = anyPair.X;

            if (!MarshalOps.DoesValueMatchType(typeof(T1), lx))
            {
                x = default(T1);
                y = default(T2);
                return false;
            }

            object ly = anyPair.Y;

            if (!MarshalOps.DoesValueMatchType(typeof(T2), ly))
            {
                x = default(T1);
                y = default(T2);
                return false;
            }

            x = (T1)lx;
            y = (T2)ly;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractXZ<T1, T2>(
            IAnyPair anyPair,
            out T1 x,
            out T2 z
            )
        {
            x = default(T1);
            z = default(T2);
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractYZ<T1, T2>(
            IAnyPair anyPair,
            out T1 y,
            out T2 z
            )
        {
            y = default(T1);
            z = default(T2);
            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Weakly Typed Extractors
        public static bool TryExtractXY(
            IAnyPair anyPair,
            out object x,
            out object y
            )
        {
            if (anyPair == null)
            {
                x = null;
                y = null;
                return false;
            }

            x = anyPair.X;
            y = anyPair.Y;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractXZ(
            IAnyPair anyPair,
            out object x,
            out object z
            )
        {
            x = null;
            z = null;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractYZ(
            IAnyPair anyPair,
            out object y,
            out object z
            )
        {
            y = null;
            z = null;
            return false;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyTriplet Extractors
        #region Strongly Typed Extractors
        public static bool TryExtractXY<T1, T2>(
            IAnyTriplet anyTriplet,
            out T1 x,
            out T2 y
            )
        {
            if (anyTriplet == null)
            {
                x = default(T1);
                y = default(T2);
                return false;
            }

            object lx = anyTriplet.X;

            if (!MarshalOps.DoesValueMatchType(typeof(T1), lx))
            {
                x = default(T1);
                y = default(T2);
                return false;
            }

            object ly = anyTriplet.Y;

            if (!MarshalOps.DoesValueMatchType(typeof(T2), ly))
            {
                x = default(T1);
                y = default(T2);
                return false;
            }

            x = (T1)lx;
            y = (T2)ly;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractXZ<T1, T2>(
            IAnyTriplet anyTriplet,
            out T1 x,
            out T2 z
            )
        {
            if (anyTriplet == null)
            {
                x = default(T1);
                z = default(T2);
                return false;
            }

            object lx = anyTriplet.X;

            if (!MarshalOps.DoesValueMatchType(typeof(T1), lx))
            {
                x = default(T1);
                z = default(T2);
                return false;
            }

            object lz = anyTriplet.Z;

            if (!MarshalOps.DoesValueMatchType(typeof(T2), lz))
            {
                x = default(T1);
                z = default(T2);
                return false;
            }

            x = (T1)lx;
            z = (T2)lz;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractYZ<T1, T2>(
            IAnyTriplet anyTriplet,
            out T1 y,
            out T2 z
            )
        {
            if (anyTriplet == null)
            {
                y = default(T1);
                z = default(T2);
                return false;
            }

            object ly = anyTriplet.Y;

            if (!MarshalOps.DoesValueMatchType(typeof(T1), ly))
            {
                y = default(T1);
                z = default(T2);
                return false;
            }

            object lz = anyTriplet.Z;

            if (!MarshalOps.DoesValueMatchType(typeof(T2), lz))
            {
                y = default(T1);
                z = default(T2);
                return false;
            }

            y = (T1)ly;
            z = (T2)lz;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Weakly Typed Extractors
        public static bool TryExtractXY(
            IAnyTriplet anyTriplet,
            out object x,
            out object y
            )
        {
            if (anyTriplet == null)
            {
                x = null;
                y = null;
                return false;
            }

            x = anyTriplet.X;
            y = anyTriplet.Y;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractXZ(
            IAnyTriplet anyTriplet,
            out object x,
            out object z
            )
        {
            if (anyTriplet == null)
            {
                x = null;
                z = null;
                return false;
            }

            x = anyTriplet.X;
            z = anyTriplet.Z;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtractYZ(
            IAnyTriplet anyTriplet,
            out object y,
            out object z
            )
        {
            if (anyTriplet == null)
            {
                y = null;
                z = null;
                return false;
            }

            y = anyTriplet.Y;
            z = anyTriplet.Z;
            return true;
        }
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Triple Value Extractors
        #region IAnyPair Extractors
        #region Strongly Typed Extractors
        public static bool TryExtractXYZ<T1, T2, T3>(
            IAnyPair anyPair,
            out T1 x,
            out T2 y,
            out T3 z
            )
        {
            x = default(T1);
            y = default(T2);
            z = default(T3);
            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Weakly Typed Extractors
        public static bool TryExtractXYZ(
            IAnyPair anyPair,
            out object x,
            out object y,
            out object z
            )
        {
            x = null;
            y = null;
            z = null;
            return false;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyTriplet Extractors
        #region Strongly Typed Extractors
        public static bool TryExtractXYZ<T1, T2, T3>(
            IAnyTriplet anyTriplet,
            out T1 x,
            out T2 y,
            out T3 z
            )
        {
            if (anyTriplet == null)
            {
                x = default(T1);
                y = default(T2);
                z = default(T3);
                return false;
            }

            object lx = anyTriplet.X;

            if (!MarshalOps.DoesValueMatchType(typeof(T1), lx))
            {
                x = default(T1);
                y = default(T2);
                z = default(T3);
                return false;
            }

            object ly = anyTriplet.Y;

            if (!MarshalOps.DoesValueMatchType(typeof(T2), ly))
            {
                x = default(T1);
                y = default(T2);
                z = default(T3);
                return false;
            }

            object lz = anyTriplet.Z;

            if (!MarshalOps.DoesValueMatchType(typeof(T3), lz))
            {
                x = default(T1);
                y = default(T2);
                z = default(T3);
                return false;
            }

            x = (T1)lx;
            y = (T2)ly;
            z = (T3)lz;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Weakly Typed Extractors
        public static bool TryExtractXYZ(
            IAnyTriplet anyTriplet,
            out object x,
            out object y,
            out object z
            )
        {
            if (anyTriplet == null)
            {
                x = null;
                y = null;
                z = null;
                return false;
            }

            x = anyTriplet.X;
            y = anyTriplet.Y;
            z = anyTriplet.Z;
            return true;
        }
        #endregion
        #endregion
        #endregion
    }
}
