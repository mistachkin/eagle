/*
 * EvaluateExpression.cs --
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
    /// This class implements the PowerShell cmdlet that evaluates an Eagle
    /// expression using the embedded interpreter managed by its
    /// <see cref="Script" /> base class and writes the resulting value or
    /// error to the pipeline.
    /// </summary>
    [Cmdlet(
        _Constants.Verb.Evaluate,
        _Constants.Noun.Expression,
        SupportsShouldProcess = true
        )]
    [ObjectId("e697ea72-ac6f-4a9e-b05d-3abbbd673df5")]
    public sealed class EvaluateExpression : Script
    {
        #region System.Management.Automation.Cmdlet Overrides
        /// <summary>
        /// This method is called by the PowerShell runtime to process a single
        /// pipeline record.  It checks the preconditions, evaluates the
        /// configured expression using the embedded interpreter, and writes
        /// either the successful result or an error record to the pipeline
        /// (honoring any request to stop the pipeline).
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
                        typeof(EvaluateExpression), "expression");

                    ReturnCode code;
                    Result result = null;

                    code = EvaluateExpression(ref result);

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
