/*
 * ScriptManager.cs --
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

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by the component responsible for
    /// managing the library and host scripts used by an Eagle interpreter,
    /// including locating and loading the core script library, tracking the
    /// auto-path, and performing interpreter initialization.
    /// </summary>
    [ObjectId("f7cba501-c759-46d9-9e6b-e42e0ed8a044")]
    public interface IScriptManager
    {
        ///////////////////////////////////////////////////////////////////////
        // LIBRARY & HOST SCRIPT MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the path to the core script library used by this
        /// interpreter.
        /// </summary>
        string LibraryPath { get; set; }
        /// <summary>
        /// Gets or sets the list of directories searched when automatically
        /// loading packages and scripts.
        /// </summary>
        StringList AutoPathList { get; set; }

        /// <summary>
        /// Gets the location of the script currently being evaluated, if any.
        /// </summary>
        IScriptLocation ScriptLocation { get; }

        /// <summary>
        /// Performs any preliminary setup required before this interpreter is
        /// initialized.
        /// </summary>
        /// <param name="force">
        /// True to force pre-initialization even if it has already been
        /// performed; otherwise, false.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode PreInitialize(bool force, ref Result error);
        /// <summary>
        /// Initializes this interpreter, loading the core script library as
        /// necessary.
        /// </summary>
        /// <param name="force">
        /// True to force initialization even if the interpreter has
        /// already been initialized; otherwise, false.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Initialize(bool force, ref Result error);

#if SHELL
        /// <summary>
        /// Performs the additional initialization required when this interpreter
        /// is used to host an interactive shell.
        /// </summary>
        /// <param name="force">
        /// True to force shell initialization even if it has already been
        /// performed; otherwise, false.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode InitializeShell(bool force, ref Result error);
#endif

        /// <summary>
        /// Locates and returns the script with the specified name, honoring the
        /// requested script flags.
        /// </summary>
        /// <param name="name">
        /// The name of the script to locate.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="scriptFlags">
        /// On input, the flags that control how the script is
        /// located and loaded; on output, the flags that describe how it was
        /// actually handled.
        /// </param>
        /// <param name="clientData">
        /// On input, the extra data to associate with the request, if
        /// any; on output, this may receive extra data associated with the
        /// located script.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the script text or other located
        /// value; upon failure, this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode GetScript(
            string name,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result result
            );
    }
}
