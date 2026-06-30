/*
 * InterpreterHelper.cs --
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
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class wraps an interpreter and provides convenience support for
    /// creating one, optionally inside another application domain.  It derives
    /// from <see cref="ScriptMarshalByRefObject" /> and implements
    /// <see cref="IGetInterpreter" /> and <see cref="IDisposable" />.
    /// </summary>
    [ObjectId("51eaee80-f84c-438b-b4b5-a5f9e6dc1bca")]
    // [ObjectFlags(ObjectFlags.AutoDispose)]
    public sealed class InterpreterHelper :
            ScriptMarshalByRefObject, IGetInterpreter, IDisposable
    {
        #region Private Constants
        /// <summary>
        /// The cached assembly name of the assembly containing this class.
        /// </summary>
        private static readonly AssemblyName assemblyName =
            GlobalState.GetAssemblyName();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached full name of this class.
        /// </summary>
        private static readonly string typeName =
            typeof(InterpreterHelper).FullName;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors /* WARNING: FOR EMERGENCY USE ONLY */
        /// <summary>
        /// Constructs an interpreter helper, creating a default interpreter.
        /// </summary>
        public InterpreterHelper()
        {
            CreateInterpreterOrTrace();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an interpreter helper, creating an interpreter from the
        /// specified interpreter settings.
        /// </summary>
        /// <param name="interpreterSettings">
        /// The settings used to create the interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require all interpreter settings to be valid.
        /// </param>
        /// <param name="result">
        /// Upon success, the result of creating the interpreter.  Upon failure,
        /// an error message describing why the interpreter could not be
        /// created.
        /// </param>
        //
        // NOTE: For use by the associated InterpreterHelper.Create method
        //       overload ONLY.
        //
        private InterpreterHelper(
            IInterpreterSettings interpreterSettings,
            bool strict,
            ref Result result
            )
        {
            CreateInterpreterOrTrace(
                interpreterSettings, strict, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an interpreter helper, creating an interpreter from the
        /// fully specified set of rule set, argument, flag, and path
        /// parameters.
        /// </summary>
        /// <param name="ruleSet">
        /// The rule set used to create the interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="args">
        /// The command-line arguments for the interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="createFlags">
        /// The flags that control how the interpreter is created.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The flags that control how the interpreter host is created.
        /// </param>
        /// <param name="initializeFlags">
        /// The flags that control how the interpreter is initialized.
        /// </param>
        /// <param name="scriptFlags">
        /// The flags that control how scripts are located and evaluated.
        /// </param>
        /// <param name="interpreterFlags">
        /// The flags that control miscellaneous interpreter behavior.
        /// </param>
        /// <param name="interpreterTestFlags">
        /// The flags that control interpreter testing behavior.
        /// </param>
        /// <param name="pluginFlags">
        /// The flags that control how plugins are loaded.
        /// </param>
        /// <param name="findFlags">
        /// The flags that control how native Tcl is located.
        /// </param>
        /// <param name="loadFlags">
        /// The flags that control how native Tcl is loaded.
        /// </param>
        /// <param name="text">
        /// The startup script text to evaluate, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="libraryPath">
        /// The path to the script library, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="autoPathList">
        /// The list of directories to search for packages, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, the result of creating the interpreter.  Upon failure,
        /// an error message describing why the interpreter could not be
        /// created.
        /// </param>
        //
        // NOTE: For use by the associated InterpreterHelper.Create method
        //       overload ONLY.
        //
        private InterpreterHelper(
            IRuleSet ruleSet,
            IEnumerable<string> args,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            InterpreterTestFlags interpreterTestFlags,
            PluginFlags pluginFlags,
#if NATIVE && TCL
            FindFlags findFlags,
            LoadFlags loadFlags,
#endif
            string text,
            string libraryPath,
            StringList autoPathList,
            ref Result result
            )
        {
            CreateInterpreterOrTrace(
                ruleSet, args, createFlags, hostCreateFlags,
                initializeFlags, scriptFlags, interpreterFlags,
                interpreterTestFlags, pluginFlags,
#if NATIVE && TCL
                findFlags, loadFlags,
#endif
                text, libraryPath, autoPathList, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method emits a trace message when interpreter creation fails
        /// (that is, when there is no resulting interpreter).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that was created, if any.  This parameter may be
        /// null, in which case a trace message is emitted.
        /// </param>
        /// <param name="result">
        /// The result of attempting to create the interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        private void MaybeTraceCreationError(
            Interpreter interpreter, /* in: OPTIONAL */
            Result result            /* in: OPTIONAL */
            )
        {
            if (interpreter != null)
                return;

            TraceOps.DebugTrace(String.Format(
                "MaybeTraceCreationError: result = {0}",
                FormatOps.WrapOrNull(result)),
                typeof(InterpreterHelper).Name,
                TracePriority.RemotingError);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a default interpreter, emitting a trace message
        /// and saving the creation result if it fails.
        /// </summary>
        private void CreateInterpreterOrTrace()
        {
            Result result = null;

            interpreter = Interpreter.Create(
                null, false, ref result);

            MaybeTraceCreationError(interpreter, result);

            //
            // HACK: The "ref" result parameter for this constructor
            //       is not honored when invoked using remoting from
            //       another AppDomain; therefore, save the creation
            //       result from Interpreter.Create now.
            //
            SaveResult(result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an interpreter from the specified interpreter
        /// settings, emitting a trace message and saving the creation result
        /// if it fails.
        /// </summary>
        /// <param name="interpreterSettings">
        /// The settings used to create the interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require all interpreter settings to be valid.
        /// </param>
        /// <param name="result">
        /// Upon success, the result of creating the interpreter.  Upon failure,
        /// an error message describing why the interpreter could not be
        /// created.
        /// </param>
        private void CreateInterpreterOrTrace(
            IInterpreterSettings interpreterSettings,
            bool strict,
            ref Result result
            )
        {
            interpreter = Interpreter.Create(
                interpreterSettings, strict, ref result);

            MaybeTraceCreationError(interpreter, result);

            //
            // HACK: The "ref" result parameter for this constructor
            //       is not honored when invoked using remoting from
            //       another AppDomain; therefore, save the creation
            //       result from Interpreter.Create now.
            //
            SaveResult(result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an interpreter from the fully specified set of
        /// rule set, argument, flag, and path parameters, emitting a trace
        /// message and saving the creation result if it fails.
        /// </summary>
        /// <param name="ruleSet">
        /// The rule set used to create the interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="args">
        /// The command-line arguments for the interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="createFlags">
        /// The flags that control how the interpreter is created.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The flags that control how the interpreter host is created.
        /// </param>
        /// <param name="initializeFlags">
        /// The flags that control how the interpreter is initialized.
        /// </param>
        /// <param name="scriptFlags">
        /// The flags that control how scripts are located and evaluated.
        /// </param>
        /// <param name="interpreterFlags">
        /// The flags that control miscellaneous interpreter behavior.
        /// </param>
        /// <param name="interpreterTestFlags">
        /// The flags that control interpreter testing behavior.
        /// </param>
        /// <param name="pluginFlags">
        /// The flags that control how plugins are loaded.
        /// </param>
        /// <param name="findFlags">
        /// The flags that control how native Tcl is located.
        /// </param>
        /// <param name="loadFlags">
        /// The flags that control how native Tcl is loaded.
        /// </param>
        /// <param name="text">
        /// The startup script text to evaluate, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="libraryPath">
        /// The path to the script library, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="autoPathList">
        /// The list of directories to search for packages, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, the result of creating the interpreter.  Upon failure,
        /// an error message describing why the interpreter could not be
        /// created.
        /// </param>
        private void CreateInterpreterOrTrace(
            IRuleSet ruleSet,
            IEnumerable<string> args,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            InterpreterTestFlags interpreterTestFlags,
            PluginFlags pluginFlags,
#if NATIVE && TCL
            FindFlags findFlags,
            LoadFlags loadFlags,
#endif
            string text,
            string libraryPath,
            StringList autoPathList,
            ref Result result
            )
        {
            interpreter = Interpreter.Create(
                ruleSet, args, createFlags, hostCreateFlags,
                initializeFlags, scriptFlags, interpreterFlags,
                interpreterTestFlags, pluginFlags,
#if NATIVE && TCL
                findFlags, loadFlags,
#endif
                text, libraryPath, autoPathList, ref result);

            MaybeTraceCreationError(interpreter, result);

            //
            // HACK: The "ref" result parameter for this constructor
            //       is not honored when invoked using remoting from
            //       another AppDomain; therefore, save the creation
            //       result from Interpreter.Create now.
            //
            SaveResult(result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves the specified interpreter creation result so it
        /// can be retrieved later, even when this instance was created via
        /// remoting from another application domain.
        /// </summary>
        /// <param name="result">
        /// The interpreter creation result to save.
        /// </param>
        private void SaveResult(
            Result result
            )
        {
            this.result = result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method extracts the result from the last element of the
        /// specified constructor argument array.
        /// </summary>
        /// <param name="ctorArgs">
        /// The constructor argument array whose last element is the result.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The extracted result, or null if it could not be extracted.
        /// </returns>
        private static Result ExtractResult(
            object[] ctorArgs
            )
        {
            if (ctorArgs == null)
                return null;

            int length = ctorArgs.Length;

            if (length == 0)
                return null;

            return ctorArgs[length - 1] as Result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates an interpreter helper inside the specified
        /// application domain, using the specified interpreter settings.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain in which to create the interpreter helper.
        /// </param>
        /// <param name="interpreterSettings">
        /// The settings used to create the interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require all interpreter settings to be valid.
        /// </param>
        /// <param name="result">
        /// Upon success, the result of creating the interpreter.  Upon failure,
        /// an error message describing why the interpreter helper could not be
        /// created.
        /// </param>
        /// <returns>
        /// The newly created interpreter helper, or null if it could not be
        /// created.
        /// </returns>
        public static InterpreterHelper Create(
            AppDomain appDomain,
            IInterpreterSettings interpreterSettings,
            bool strict,
            ref Result result
            )
        {
            if (appDomain == null)
            {
                result = "invalid application domain";
                return null;
            }

            if (assemblyName == null)
            {
                result = "invalid assembly name";
                return null;
            }

            if (typeName == null)
            {
                result = "invalid type name";
                return null;
            }

            try
            {
                object[] ctorArgs = { interpreterSettings, strict, result };

                InterpreterHelper interpreterHelper =
                    appDomain.CreateInstanceAndUnwrap(
                        assemblyName.ToString(), typeName, false,
                        ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateCreateInstance, true),
                        null, ctorArgs, null, null,
                        null) as InterpreterHelper;

                if (interpreterHelper != null)
                {
                    //
                    // NOTE: Grab the result as it may have been modified.
                    //
                    Result localResult = ExtractResult(ctorArgs);

                    //
                    // HACK: Otherwise, since "ref" parameters do not seem
                    //       to work for any class constructors invoked via
                    //       CreateInstanceAndUnwrap (?), fallback to using
                    //       the Result property of the instance.
                    //
                    if (localResult != null)
                        result = localResult;
                    else
                        result = interpreterHelper.Result;

                    return interpreterHelper;
                }
                else
                {
                    result = String.Format(
                        "could not create interpreter helper {0}",
                        FormatOps.WrapOrNull(typeName));
                }
            }
            catch (Exception e)
            {
                result = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an interpreter helper inside the specified
        /// application domain, using the fully specified set of rule set,
        /// argument, flag, and path parameters.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain in which to create the interpreter helper.
        /// </param>
        /// <param name="ruleSet">
        /// The rule set used to create the interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="args">
        /// The command-line arguments for the interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="createFlags">
        /// The flags that control how the interpreter is created.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The flags that control how the interpreter host is created.
        /// </param>
        /// <param name="initializeFlags">
        /// The flags that control how the interpreter is initialized.
        /// </param>
        /// <param name="scriptFlags">
        /// The flags that control how scripts are located and evaluated.
        /// </param>
        /// <param name="interpreterFlags">
        /// The flags that control miscellaneous interpreter behavior.
        /// </param>
        /// <param name="interpreterTestFlags">
        /// The flags that control interpreter testing behavior.
        /// </param>
        /// <param name="pluginFlags">
        /// The flags that control how plugins are loaded.
        /// </param>
        /// <param name="findFlags">
        /// The flags that control how native Tcl is located.
        /// </param>
        /// <param name="loadFlags">
        /// The flags that control how native Tcl is loaded.
        /// </param>
        /// <param name="text">
        /// The startup script text to evaluate, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="libraryPath">
        /// The path to the script library, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="autoPathList">
        /// The list of directories to search for packages, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, the result of creating the interpreter.  Upon failure,
        /// an error message describing why the interpreter helper could not be
        /// created.
        /// </param>
        /// <returns>
        /// The newly created interpreter helper, or null if it could not be
        /// created.
        /// </returns>
        /* NOTE: For use by [test2] and CreateChildInterpreter ONLY. */
        internal static InterpreterHelper Create(
            AppDomain appDomain,
            IRuleSet ruleSet,
            IEnumerable<string> args,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            InterpreterTestFlags interpreterTestFlags,
            PluginFlags pluginFlags,
#if NATIVE && TCL
            FindFlags findFlags,
            LoadFlags loadFlags,
#endif
            string text,
            string libraryPath,
            StringList autoPathList,
            ref Result result
            )
        {
            if (appDomain == null)
            {
                result = "invalid application domain";
                return null;
            }

            if (assemblyName == null)
            {
                result = "invalid assembly name";
                return null;
            }

            if (typeName == null)
            {
                result = "invalid type name";
                return null;
            }

            try
            {
                object[] ctorArgs = {
                    ruleSet, args, createFlags, hostCreateFlags,
                    initializeFlags, scriptFlags, interpreterFlags,
                    interpreterTestFlags, pluginFlags,
#if NATIVE && TCL
                    findFlags, loadFlags,
#endif
                    text, libraryPath, autoPathList, result
                };

                InterpreterHelper interpreterHelper =
                    appDomain.CreateInstanceAndUnwrap(
                        assemblyName.ToString(), typeName, false,
                        ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateCreateInstance, true),
                        null, ctorArgs, null, null,
                        null) as InterpreterHelper;

                if (interpreterHelper != null)
                {
                    //
                    // NOTE: Grab the result as it may have been modified.
                    //
                    Result localResult = ExtractResult(ctorArgs);

                    //
                    // HACK: Otherwise, since "ref" parameters do not seem
                    //       to work for any class constructors invoked via
                    //       CreateInstanceAndUnwrap (?), fallback to using
                    //       the Result property of the instance.
                    //
                    if (localResult != null)
                        result = localResult;
                    else
                        result = interpreterHelper.Result;

                    return interpreterHelper;
                }
                else
                {
                    result = String.Format(
                        "could not create interpreter helper {0}",
                        FormatOps.WrapOrNull(typeName));
                }
            }
            catch (Exception e)
            {
                result = e;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// The saved result of creating the interpreter.
        /// </summary>
        private Result result;
        /// <summary>
        /// Gets the saved result of creating the interpreter.
        /// </summary>
        public Result Result
        {
            get { CheckDisposed(); return result; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method removes the interpreter from this helper without
        /// disposing it, so that the caller assumes ownership of it.
        /// </summary>
        /// <returns>
        /// True if there was an interpreter to remove; otherwise, false.
        /// </returns>
        public bool RemoveInterpreter()
        {
            CheckDisposed();

            if (interpreter != null)
            {
                interpreter = null;
                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// The interpreter wrapped by this helper, if any.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter wrapped by this helper, if any.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Loop Methods
#if SHELL
        /// <summary>
        /// This method runs the interactive loop using the interpreter wrapped
        /// by this helper.
        /// </summary>
        /// <param name="loopData">
        /// The data describing the state of the interactive loop, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, the result produced by the interactive loop.  Upon
        /// failure, an error message describing what went wrong.
        /// </param>
        /// <returns>
        /// The return code produced by the interactive loop.
        /// </returns>
        public ReturnCode InteractiveLoop(
            IInteractiveLoopData loopData,
            ref Result result
            )
        {
            CheckDisposed();

            return Interpreter.InteractiveLoop(
                interpreter, loopData, ref result);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this interpreter helper has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this interpreter helper has been
        /// disposed and the associated interpreter is configured to throw on
        /// disposed access.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
            {
                throw new ObjectDisposedException(
                    typeof(InterpreterHelper).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this interpreter helper,
        /// including the wrapped interpreter.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method (instead of from the finalizer).
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    result = null;

                    if (interpreter != null)
                    {
                        interpreter.Dispose();
                        interpreter = null;
                    }
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources used by this interpreter helper.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this interpreter helper, releasing any resources that
        /// were not released explicitly.
        /// </summary>
        ~InterpreterHelper()
        {
            Dispose(false);
        }
        #endregion
    }
}
