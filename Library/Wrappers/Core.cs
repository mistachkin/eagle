/*
 * Core.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class implements the base wrapper used to hold an executable
    /// entity.  It stores the wrapped <see cref="IExecute" /> object and
    /// forwards execution to it, serving as the common base class for the
    /// more specialized wrappers.
    /// </summary>
    [ObjectId("9a1a9764-b475-4f48-99a1-dc61e984a312")]
    internal class Core : Default, IExecute
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with no wrapped object.
        /// </summary>
        public Core()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped executable entity that this wrapper forwards to.
        /// </summary>
        internal IExecute execute;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// Forwards execution to the wrapped executable entity, passing along
        /// the supplied arguments and reporting its outcome.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this entity is executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data supplied when the entity was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// wrapped entity.  Upon failure, this will contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// If there is no wrapped entity, <see cref="ReturnCode.Error" /> is
        /// returned.
        /// </returns>
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (execute == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return execute.Execute(
                interpreter, clientData, arguments, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the wrapped object represents a
        /// disposable resource.  This wrapper never owns a disposable
        /// resource, so this property always returns false.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the wrapped object.  The value being set must be an
        /// <see cref="IExecute" /> instance.
        /// </summary>
        public override object Object
        {
            get { return execute; }
            set { execute = (IExecute)value; } /* throw */
        }
        #endregion
    }
}
