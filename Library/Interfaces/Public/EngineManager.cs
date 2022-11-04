/*
 * EngineManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.IO;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("6d41d61d-1173-4034-b5c3-3e929e5c7f24")]
    public interface IEngineManager
    {
        ///////////////////////////////////////////////////////////////////////
        // SCRIPT CANCELLATION
        ///////////////////////////////////////////////////////////////////////

        ReturnCode IsCanceled(
            CancelFlags cancelFlags,
            ref Result result
            );

#if THREADING
        ReturnCode IsCanceled(
            object engineContext,
            CancelFlags cancelFlags,
            ref Result result
            );
#endif

        ReturnCode CancelEvaluate(
            Result result,
            CancelFlags cancelFlags,
            ref Result error
            );

        ReturnCode CancelAnyEvaluate(
            Result result,
            CancelFlags cancelFlags,
            ref Result error
            );

#if THREADING
        ReturnCode CancelAnyEvaluate(
            object engineContext,
            Result result,
            CancelFlags cancelFlags,
            ref Result error
            );
#endif

        ReturnCode ResetCancel(
            CancelFlags cancelFlags,
            ref Result error
            );

#if THREADING
        ReturnCode ResetCancel(
            object engineContext,
            CancelFlags cancelFlags,
            ref Result error
            );
#endif

        ///////////////////////////////////////////////////////////////////////
        // SCRIPT EVALUATION
        ///////////////////////////////////////////////////////////////////////

        ReturnCode EvaluateScript(
            string text,
            ref Result result
            );

        ReturnCode EvaluateScript(
            string text,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateScript(
            string text,
            EngineFlags engineFlags,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateGlobalScript(
            string text,
            ref Result result
            );

        ReturnCode EvaluateGlobalScript(
            string text,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateScript(
            IScript script,
            ref Result result
            );

        ReturnCode EvaluateScript(
            IScript script,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateScriptWithScopeFrame(
            string text,
            ref ICallFrame frame,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateFile(
            string fileName,
            ref Result result
            );

        ReturnCode EvaluateFile(
            string fileName,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateFile(
            Encoding encoding,
            string fileName,
            ref Result result
            );

        ReturnCode EvaluateFile(
            Encoding encoding,
            string fileName,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateFile(
            Encoding encoding,
            string fileName,
            EngineFlags engineFlags,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateGlobalFile(
            string fileName,
            ref Result result
            );

        ReturnCode EvaluateGlobalFile(
            string fileName,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateGlobalFile(
            Encoding encoding,
            string fileName,
            ref Result result
            );

        ReturnCode EvaluateGlobalFile(
            Encoding encoding,
            string fileName,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateFileWithScopeFrame(
            Encoding encoding,
            string fileName,
            ref ICallFrame frame,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            EngineFlags engineFlags,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateGlobalStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateStreamWithScopeFrame(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            ref ICallFrame frame,
            ref Result result,
            ref int errorLine
            );

        ///////////////////////////////////////////////////////////////////////
        // TRUSTED SCRIPT EVALUATION
        ///////////////////////////////////////////////////////////////////////

        ReturnCode EvaluateTrustedScript(
            string text,
            TrustFlags trustFlags,
            ref Result result
            );

        ReturnCode EvaluateTrustedScript(
            string fileName,
            string text,
            TrustFlags trustFlags,
            ref Result result
            );

        ReturnCode EvaluateTrustedScript(
            string text,
            TrustFlags trustFlags,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateTrustedScript(
            string fileName,
            string text,
            TrustFlags trustFlags,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateTrustedFile(
            Encoding encoding,
            string fileName,
            TrustFlags trustFlags,
            ref Result result
            );

        ReturnCode EvaluateTrustedFile(
            Encoding encoding,
            string fileName,
            TrustFlags trustFlags,
            ref Result result,
            ref int errorLine
            );

        ReturnCode EvaluateTrustedStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            TrustFlags trustFlags,
            ref Result result
            );

        ReturnCode EvaluateTrustedStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            TrustFlags trustFlags,
            ref Result result,
            ref int errorLine
            );

        ///////////////////////////////////////////////////////////////////////
        // EXPRESSION EVALUATION
        ///////////////////////////////////////////////////////////////////////

        ReturnCode EvaluateExpression(
            string text,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // SUBSTITUTION PROCESSING
        ///////////////////////////////////////////////////////////////////////

        ReturnCode SubstituteString(
            string text,
            ref Result result
            );

        ReturnCode SubstituteString(
            string text,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        ReturnCode SubstituteGlobalString(
            string text,
            ref Result result
            );

        ReturnCode SubstituteGlobalString(
            string text,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        ReturnCode SubstituteFile(
            string fileName,
            ref Result result
            );

        ReturnCode SubstituteFile(
            string fileName,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        ReturnCode SubstituteGlobalFile(
            string fileName,
            ref Result result
            );

        ReturnCode SubstituteGlobalFile(
            string fileName,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        ReturnCode SubstituteStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            ref Result result
            );

        ReturnCode SubstituteStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        ReturnCode SubstituteGlobalStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            ref Result result
            );

        ReturnCode SubstituteGlobalStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS SCRIPT EVALUATION
        ///////////////////////////////////////////////////////////////////////

        ReturnCode EvaluateScript(
            string text,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ReturnCode EvaluateGlobalScript(
            string text,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ReturnCode EvaluateFile(
            string fileName,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ReturnCode EvaluateGlobalFile(
            string fileName,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS SUBSTITUTION PROCESSING
        ///////////////////////////////////////////////////////////////////////

        ReturnCode SubstituteString(
            string text,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ReturnCode SubstituteString(
            string text,
            SubstitutionFlags substitutionFlags,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error);

        ReturnCode SubstituteGlobalString(
            string text,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ReturnCode SubstituteGlobalString(
            string text,
            SubstitutionFlags substitutionFlags,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error);

        ReturnCode SubstituteFile(
            string fileName,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ReturnCode SubstituteFile(
            string fileName,
            SubstitutionFlags substitutionFlags,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ReturnCode SubstituteGlobalFile(
            string fileName,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ReturnCode SubstituteGlobalFile(
            string fileName,
            SubstitutionFlags substitutionFlags,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // COMMAND EXECUTION
        ///////////////////////////////////////////////////////////////////////

        ReturnCode Invoke(
            string name,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            );
    }
}
