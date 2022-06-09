/*
 * Class15.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Sample
{
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("5b039357-f5d6-483e-8cfb-166a609ff6f3")]
    internal sealed class Class15
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        : ScriptMarshalByRefObject, IUnknownCallback
#endif
    {
        #region IExecute Sample Class
        //
        // FIXME: Always change this GUID.
        //
        [ObjectId("65305139-bbc6-4974-b04e-d5c9fabb685b")]
        private sealed class Class15Execute :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
            ScriptMarshalByRefObject,
#endif
            IExecute
        {
            #region Private Data
            /// <summary>
            /// The name of this dynamic command object.
            /// </summary>
            private string name;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Constructors
            /// <summary>
            /// Creates an instance of a simple dynamic command.
            /// </summary>
            /// <param name="name">
            /// The new name for this dynamic command object.
            /// </param>
            public Class15Execute(
                string name /* in */
                )
            {
                this.name = name;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Properties
            /// <summary>
            /// Returns the name of this dynamic command object.
            /// </summary>
            public string Name
            {
                get { return name; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IExecute Members
            /// <summary>
            /// Execute the command and return the appropriate result and
            /// return code.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context we are executing in.
            /// </param>
            /// <param name="clientData">
            /// The extra data supplied when this command was initially
            /// created, if any.
            /// </param>
            /// <param name="arguments">
            /// The list of arguments supplied to this command by the script
            /// being evaluated.
            /// </param>
            /// <param name="result">
            /// Upon success, a list with two elements.  The first element
            /// will contain the name of this dynamic command.  The second
            /// element will contain a list of the arguments supplied.  Upon
            /// failure, an appropriate error message.
            /// </param>
            /// <returns>
            /// ReturnCode.Ok on success, ReturnCode.Error on failure.
            /// </returns>
            public ReturnCode Execute(
                Interpreter interpreter, /* in */
                IClientData clientData,  /* in */
                ArgumentList arguments,  /* in */
                ref Result result        /* out */
                )
            {
                if (arguments == null)
                {
                    result = "invalid argument list";
                    return ReturnCode.Error;
                }

                result = StringList.MakeList(name, arguments);
                return ReturnCode.Ok;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the configured plugin instance.  It will be used to
        //       query for an embedded resource string containing the sample
        //       package script.
        //
        private IPlugin plugin;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The cached dynamic command to use when a name is specified in
        //       the constructor.  This will be created (only) when the public
        //       constructor receives a valid name.
        //
        private Class15Execute class15Execute;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="plugin">
        /// The plugin context we are executing in.
        /// </param>
        /// <param name="name">
        /// The name of the new dynamic command.
        /// </param>
        public Class15(
            IPlugin plugin, /* in */
            string name     /* in */
            )
        {
            this.plugin = plugin;

            if (name != null)
                class15Execute = new Class15Execute(name);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUnknownCallback Members
        /// <summary>
        /// Attempts to resolve unknown command names provided by the script
        /// engine.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="engineFlags">
        /// The flags that should be used when calling back into the script
        /// engine while attempting to resolve the command.
        /// </param>
        /// <param name="name">
        /// The name of the command that could not be resolved by the script
        /// engine.
        /// </param>
        /// <param name="arguments">
        /// The arguments to the command that could not be resolved by the
        /// script engine.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that should be used when looking up associated entities
        /// within the interpreter while attempting to resolve the command.
        /// </param>
        /// <param name="ambiguous">
        /// Upon entry, will be non-zero if the script engine found more than
        /// one command (e.g. based on prefix matching).  Upon exit, this can
        /// be used to indicate that command resolution resulted in more than
        /// one command being matched (e.g. based on prefix matching).
        /// </param>
        /// <param name="execute">
        /// Upon success, this parameter will be set to an instance of the
        /// <see cref="IExecute" /> interface that should be used to handle
        /// the previously unknown command.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok to indicate the previously unknown command was
        /// resolved.  ReturnCode.Error to indicate the (previously) unknown
        /// command was not resolved.  ReturnCode.Break to indicate the
        /// (previously) unknown command was not resolved, further command
        /// resolution should be skipped, and the script engine should ignore
        /// the command.  ReturnCode.Continue to indicate the (previously)
        /// unknown command was not resolved, further command resolution
        /// should be skipped, and the returned error message should be used
        /// verbatim.
        /// </returns>
        public ReturnCode Unknown(
            Interpreter interpreter, /* in */
            EngineFlags engineFlags, /* in */
            string name,             /* in */
            ArgumentList arguments,  /* in */
            LookupFlags lookupFlags, /* in */
            ref bool ambiguous,      /* in, out */
            ref IExecute execute,    /* out */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            if ((class15Execute != null) &&
                Utility.SystemStringEquals(name, class15Execute.Name))
            {
                execute = class15Execute;
                return ReturnCode.Ok;
            }
            else if (Utility.SystemStringEquals(name, "class15_new"))
            {
                execute = new Class15Execute(name);
                return ReturnCode.Ok;
            }
            else if (Utility.SystemStringEquals(name, "class15_error"))
            {
                error = String.Format(
                    "returning error for {0}",
                    Utility.FormatWrapOrNull(name));

                return ReturnCode.Error;
            }
            else if (Utility.SystemStringEquals(name, "class15_break"))
            {
                error = String.Format(
                    "returning break for {0}",
                    Utility.FormatWrapOrNull(name));

                return ReturnCode.Break;
            }
            else if (Utility.SystemStringEquals(name, "class15_continue"))
            {
                error = String.Format(
                    "returning continue for {0}",
                    Utility.FormatWrapOrNull(name));

                return ReturnCode.Continue;
            }

            error = "unknown class15 dynamic command";
            return ReturnCode.CustomError;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Class15));
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

                    plugin = null; /* NOT OWNED: DO NOT DISPOSE */
                    class15Execute = null;
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
        ~Class15()
        {
            Dispose(false);
        }
        #endregion
    }
}
