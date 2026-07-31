/*
 * ArraySearch.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class represents an in-progress search over the element names of an
    /// Eagle array variable, as used by the <c>array startsearch</c>,
    /// <c>array nextelement</c>, and <c>array anymore</c> sub-commands.  It
    /// wraps an underlying enumerator over the array's element keys and tracks
    /// whether enumeration has started and whether the end has been reached.
    /// It transparently handles the special global <c>env</c> array, the test
    /// information array, system arrays, and thread, database, network, and
    /// registry backed variables, in addition to ordinary array variables.  It
    /// implements <see cref="IEnumerable" /> (to support C# foreach) and
    /// <see cref="IGetInterpreter" />.
    /// </summary>
    [ObjectId("6c48d23d-35fd-48ee-91a3-c34c6411a2c3")]
    internal sealed class ArraySearch : IEnumerable /* NOTE: Support C# foreach. */, IGetInterpreter
    {
        #region Private Data
        /// <summary>
        /// The underlying ("real") enumerator over the element names of the
        /// array variable being searched.
        /// </summary>
        private IEnumerator enumerator; /* NOTE: The "real" enumerator for this array variable. */

        /// <summary>
        /// Non-zero if <c>MoveNext</c> has been called at least once on the
        /// underlying enumerator.
        /// </summary>
        private bool didMoveNext;       /* NOTE: Non-zero if we have called MoveNext at least once on
                                         *       the "real" enumerator. */

        /// <summary>
        /// Non-zero if the underlying enumerator has reached the end of its
        /// elements.
        /// </summary>
        private bool noMoreElements;    /* NOTE: Non-zero if the "real" enumerator has hit the end. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new array search over the element names of the specified
        /// array variable, capturing the parent interpreter and variable and
        /// setting up the initial enumeration state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns the array variable being searched.
        /// </param>
        /// <param name="variable">
        /// The array variable whose element names are to be enumerated.
        /// </param>
        public ArraySearch(
            Interpreter interpreter,
            IVariable variable
            )
        {
            //
            // NOTE: Set the parent interpreter and variable for this array search operation.
            //
            this.interpreter = interpreter;
            this.variable = variable;

            //
            // NOTE: Setup the initial internal state.
            //
            this.enumerator = GetEnumerator();
            this.noMoreElements = false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// The interpreter that owns the array variable being searched.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter that owns the array variable being searched.
        /// </summary>
        public Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// The array variable whose element names are being enumerated.
        /// </summary>
        private IVariable variable;
        /// <summary>
        /// Gets the array variable whose element names are being enumerated.
        /// </summary>
        public IVariable Variable
        {
            get { return variable; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The boolean flag indicating whether or not this instance has
        /// been invalidated, i.e. by a modification to the associated
        /// array while a search is pending.
        /// </summary>
        private bool invalidated;
        /// <summary>
        /// Gets or sets the boolean flag indicating whether or not this
        /// instance has been invalidated, i.e. by a modification to the
        /// associated array while a search is pending.
        /// </summary>
        public bool Invalidated
        {
            get { return invalidated; }
            set { invalidated = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnumerable Members
        /// <summary>
        /// This method returns a fresh enumerator over the element names of the
        /// array variable being searched.  It handles the special global
        /// <c>env</c> array, the test information array, system arrays, and
        /// thread, database, network, and registry backed variables, falling
        /// back to the variable's ordinary array element keys.  When no suitable
        /// enumerator can be obtained, an enumerator that yields no elements is
        /// returned rather than null.
        /// </summary>
        /// <returns>
        /// An enumerator over the array's element names; never null.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            //
            // NOTE: Make sure that the variable they supplied is valid before we try to
            //       use it as the basis of our enumerator.
            //
            if (interpreter != null)
            {
                //
                // HACK: Handle the global "env" array specially.  We must do this because
                //       our global "env" array has no backing storage (unlike Tcl's) and
                //       we do not have a trace operation for "get names" or "get names
                //       and values".
                //
                if (interpreter.IsEnvironmentVariable(variable))
                {
                    IDictionary environment =
                        Environment.GetEnvironmentVariables();

                    if (environment != null)
                        return environment.Keys.GetEnumerator();
                    else
                        DebugOps.Complain(interpreter, ReturnCode.Error,
                            "environment variables unavailable");
                }
                else if (interpreter.IsTestsVariable(variable))
                {
                    Result error = null;
                    StringDictionary tests = interpreter.GetAllTestInformation(
                        false, ref error);

                    if (tests != null)
                        return (IEnumerator)tests.Keys.GetEnumerator();
                    else
                        DebugOps.Complain(interpreter, ReturnCode.Error, error);
                }
                else if (interpreter.IsSystemArrayVariable(variable))
                {
                    ReturnCode code;
                    StringList keys = null;
                    Result error = null;

                    code = MarshalOps.GetArrayElementKeys(
                        interpreter, EntityOps.GetSystemArray(variable),
                        StringOps.DefaultMatchMode, null, false, ref keys,
                        ref error);

                    if (code == ReturnCode.Ok)
                        return keys.GetEnumerator();
                    else
                        DebugOps.Complain(interpreter, code, error);
                }
                else
                {
                    ThreadVariable threadVariable = null;

                    if (interpreter.IsThreadVariable(variable, ref threadVariable))
                    {
                        Result error = null;

                        ObjectDictionary thread = threadVariable.GetList(
                            interpreter, true, false, ref error);

                        if (thread != null)
                            return (IEnumerator)thread.Keys.GetEnumerator();
                        else
                            DebugOps.Complain(interpreter, ReturnCode.Error, error);
                    }
                    else
                    {
#if DATA
                        DatabaseVariable databaseVariable = null;

                        if (interpreter.IsDatabaseVariable(variable, ref databaseVariable))
                        {
                            Result error = null;

                            ObjectDictionary database = databaseVariable.GetList(
                                interpreter, true, false, ref error);

                            if (database != null)
                                return (IEnumerator)database.Keys.GetEnumerator();
                            else
                                DebugOps.Complain(interpreter, ReturnCode.Error, error);
                        }
                        else
#endif
                        {
#if NETWORK && WEB
                            NetworkVariable networkVariable = null;

                            if (interpreter.IsNetworkVariable(variable, ref networkVariable))
                            {
                                Result error = null;

                                ObjectDictionary network = networkVariable.GetList(
                                    interpreter, null, false, true, false, ref error);

                                if (network != null)
                                    return (IEnumerator)network.Keys.GetEnumerator();
                                else
                                    DebugOps.Complain(interpreter, ReturnCode.Error, error);
                            }
                            else
#endif
                            {
#if !NET_STANDARD_20 && WINDOWS
                                RegistryVariable registryVariable = null;

                                if (interpreter.IsRegistryVariable(variable, ref registryVariable))
                                {
                                    Result error = null;

                                    ObjectDictionary registry = registryVariable.GetList(
                                        interpreter, true, false, ref error);

                                    if (registry != null)
                                        return (IEnumerator)registry.Keys.GetEnumerator();
                                    else
                                        DebugOps.Complain(interpreter, ReturnCode.Error, error);
                                }
                                else
#endif
                                {
                                    if (variable != null)
                                    {
                                        ElementDictionary arrayValue = variable.ArrayValue;

                                        if ((arrayValue != null) && (arrayValue.Keys != null))
                                            return arrayValue.Keys.GetEnumerator();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //
            // NOTE: While the MSDN documentation does not seem to prohibit returning
            //       null here, there may be components and/or applications that would
            //       consider it "bad form"; therefore, we simply return an enumerator
            //       that does nothing.
            //
            return new NullEnumerator<object>();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Array Enumerator Members
        /// <summary>
        /// Gets a value indicating whether at least one more element exists
        /// beyond the current position of the underlying enumerator, without
        /// changing the state of that enumerator.  This is determined by
        /// creating a fresh enumerator and advancing it until it reaches the
        /// current element, which can be expensive in the worst case.  This
        /// property returns true if another element exists; otherwise, false.
        /// </summary>
        public bool AnyMore
        {
            get
            {
                //
                // HACK: This algorithm seems to run contrary to how the .NET Framework treats
                //       enumerators.  Basically, we need to know if the next call to MoveNext
                //       on the "real" enumerator is going to return true; however, we do not
                //       actually want to change the state of the "real" enumerator in the
                //       process of finding out this information.  Unfortunately, there is no
                //       "peek" functionality available for enumerators in the .NET Framework.
                //       We work around this by creating a brand new enumerator and then
                //       advancing it until we hit the current element of the "real" enumerator.
                //       At that point, if we have not hit the end of the new enumerator, we can
                //       advance to the next element by calling MoveNext one more time.  If that
                //       call returns true then we know that there WAS at least one more element
                //       beyond the current element one in the "real" enumerator.  The primary
                //       problem with this approach is that it has the potential to be extremely
                //       inefficient in the worst case (i.e. if the "real" enumerator is
                //       positioned on the last element), which would perform O(N) operations
                //       each time.  Also, this algorithm absolutely relies upon two enumerators
                //       that are operating on the same underlying data returning elements in the
                //       exact same order.
                //
                if (noMoreElements)
                    return false;

                IEnumerator newEnumerator = GetEnumerator();

                if (newEnumerator != null)
                {
                    //
                    // NOTE: If we have not fetched the first value from the "real" enumerator,
                    //       we can skip a lot of code here and just return the result of the
                    //       first call to the MoveNext method on the new enumerator.
                    //
                    if (!didMoveNext)
                        return newEnumerator.MoveNext();

                    try
                    {
                        bool found = false;
                        string currentString = enumerator.Current as string;

                        while (newEnumerator.MoveNext())
                        {
                            if (String.CompareOrdinal(
                                    newEnumerator.Current as string,
                                    currentString) == 0) /* throw */
                            {
                                found = true;
                                break;
                            }
                        }

                        if (found)
                            return newEnumerator.MoveNext();
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method advances the underlying enumerator to the next array
        /// element and returns that element's name.  Once the end of the
        /// elements has been reached, subsequent calls return null.
        /// </summary>
        /// <returns>
        /// The name of the next array element, or null if there are no more
        /// elements.
        /// </returns>
        public string GetNextElement()
        {
            string result = null;

            //
            // NOTE: Make sure we managed to get some kind of enumerator.  Attempt to
            //       move to the next (or first?) element.  If we succeed, return the
            //       new current element.
            //
            if (enumerator != null)
            {
                //
                // NOTE: Did we already hit the last element?
                //
                if (!noMoreElements)
                {
                    if (enumerator.MoveNext())
                    {
                        //
                        // NOTE: We just performed a successful call to MoveNext.
                        //
                        didMoveNext = true;

                        try
                        {
                            //
                            // NOTE: Return the new current element.  This is always a
                            //       string because we are returning element names, not
                            //       values.
                            //
                            result = enumerator.Current as string; /* throw */
                        }
                        catch
                        {
                            // do nothing.
                        }
                    }
                    else
                    {
                        //
                        // NOTE: There are no more elements to enumerate over.
                        //
                        noMoreElements = true;
                    }
                }
            }

            return result;
        }
        #endregion
    }
}
