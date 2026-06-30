/*
 * SubstituteString.cs --
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
    /// This class implements a Windows PowerShell cmdlet that performs Eagle
    /// command substitution on a string of text and writes the resulting value
    /// (or error) to the pipeline.
    /// </summary>
    [Cmdlet(
        _Constants.Verb.Substitute,
        _Constants.Noun.Text,
        SupportsShouldProcess = true
        )]
    [ObjectId("2b70344b-b6b5-404a-a04f-7256a9e07cbc")]
    public sealed class SubstituteString : Script
    {
        #region System.Management.Automation.Cmdlet Overrides
        /// <summary>
        /// This method is called by the PowerShell runtime to process a single
        /// pipeline record.  It performs command substitution on the configured
        /// text and writes the result, an error, or a pipeline-stopping notice
        /// as appropriate.
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
                        typeof(SubstituteString), "text");

                    ReturnCode code;
                    Result result = null;

                    code = SubstituteString(ref result);

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
