/*
 * EvaluateScript.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Management.Automation;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Cmdlets
{
    /// <summary>
    /// This class implements the PowerShell cmdlet used to evaluate an Eagle
    /// script and write its result (or error) to the pipeline.
    /// </summary>
    [Cmdlet(
        _Constants.Verb.Evaluate,
        _Constants.Noun.Script,
        SupportsShouldProcess = true
        )]
    [ObjectId("f98b9465-975e-4487-be89-e06c3f75beb2")]
    public sealed class EvaluateScript : Script
    {
        #region System.Management.Automation.Cmdlet Overrides
        /// <summary>
        /// This method processes a single record for this cmdlet.  It
        /// evaluates the configured script and writes the resulting value,
        /// error, or pipeline-stopping notification as appropriate.
        /// </summary>
        protected override void ProcessRecord()
        {
            //
            // NOTE: Report that we are entering this method.
            //
            WriteVerbose(String.Format(
                _Constants.Verbose.Entered,
                MethodBase.GetCurrentMethod().Name));

            try
            {
                ErrorRecord errorRecord = null;

                if (CanProcessRecord(ref errorRecord))
                {
                    WriteParameters();

                    WriteVerboseRecord(
                        typeof(EvaluateScript), "script");

                    ReturnCode code;
                    Result result = null;

                    code = EvaluateScript(ref result);

                    if (!Stopping)
                    {
                        if (IsSuccess(code))
                            WriteObjectRecord(code, result);
                        else
                            WriteErrorRecord(code, result);
                    }
                    else
                    {
                        WriteVerbosePipelineStopping(code, result);
                    }
                }
                else
                {
                    ThrowTerminatingError(errorRecord);
                }
            }
            finally
            {
                //
                // NOTE: Report that we are exiting this method.
                //
                WriteVerbose(String.Format(
                    _Constants.Verbose.Exited,
                    MethodBase.GetCurrentMethod().Name));
            }
        }
        #endregion
    }
}
