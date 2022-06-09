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
    [ObjectId("51eaee80-f84c-438b-b4b5-a5f9e6dc1bca")]
    // [ObjectFlags(ObjectFlags.AutoDispose)]
    public sealed class InterpreterHelper :
            ScriptMarshalByRefObject, IGetInterpreter, IDisposable
    {
        #region Private Constants
        private static readonly AssemblyName assemblyName =
            GlobalState.GetAssemblyName();

        ///////////////////////////////////////////////////////////////////////

        private static readonly string typeName =
            typeof(InterpreterHelper).FullName;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors /* WARNING: FOR EMERGENCY USE ONLY */
        public InterpreterHelper()
        {
            CreateInterpreterOrTrace();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        //
        // NOTE: For use by the associated InterpreterHelper.Create method
        //       overload ONLY.
        //
        private InterpreterHelper(
            InterpreterSettings interpreterSettings,
            bool strict,
            ref Result result
            )
        {
            interpreter = Interpreter.Create(
                interpreterSettings, strict, ref result);

            //
            // HACK: The "ref" result parameter for this constructor
            //       is not honored when invoked using remoting from
            //       another AppDomain; therefore, save the creation
            //       result from Interpreter.Create now.
            //
            SaveResult(result);
        }

        ///////////////////////////////////////////////////////////////////////

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
            PluginFlags pluginFlags,
            string text,
            string libraryPath,
            StringList autoPathList,
            ref Result result
            )
        {
            interpreter = Interpreter.Create(ruleSet,
                args, createFlags, hostCreateFlags, initializeFlags,
                scriptFlags, interpreterFlags, pluginFlags, text,
                libraryPath, autoPathList, ref result);

            //
            // HACK: The "ref" result parameter for this constructor
            //       is not honored when invoked using remoting from
            //       another AppDomain; therefore, save the creation
            //       result from Interpreter.Create now.
            //
            SaveResult(result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void CreateInterpreterOrTrace()
        {
            Result result = null;

            interpreter = Interpreter.Create(
                null, false, ref result);

            if (interpreter == null)
            {
                TraceOps.DebugTrace(String.Format(
                    "CreateInterpreterOrTrace: result = {0}",
                    FormatOps.WrapOrNull(result)),
                    typeof(InterpreterHelper).Name,
                    TracePriority.RemotingError);
            }

            //
            // HACK: The "ref" result parameter for this constructor
            //       is not honored when invoked using remoting from
            //       another AppDomain; therefore, save the creation
            //       result from Interpreter.Create now.
            //
            SaveResult(result);
        }

        ///////////////////////////////////////////////////////////////////////

        private void SaveResult(
            Result result
            )
        {
            this.result = result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
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
        public static InterpreterHelper Create(
            AppDomain appDomain,
            InterpreterSettings interpreterSettings,
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
            PluginFlags pluginFlags,
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
                    pluginFlags, text, libraryPath, autoPathList,
                    result
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
        private Result result;
        public Result Result
        {
            get { CheckDisposed(); return result; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Loop Methods
#if SHELL
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
        private bool disposed;
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~InterpreterHelper()
        {
            Dispose(false);
        }
        #endregion
    }
}
