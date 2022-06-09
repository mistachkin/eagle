/*
 * SubstituteFile.cs --
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
    [Cmdlet(
        _Constants.Verb.Substitute,
        _Constants.Noun.TextFile,
        SupportsShouldProcess = true
        )]
    [ObjectId("15e76641-d775-4313-bc57-bc1c986f821a")]
    public sealed class SubstituteFile : Script
    {
        #region System.Management.Automation.Cmdlet Overrides
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
                        typeof(SubstituteFile), "text file");

                    ReturnCode code;
                    Result result = null;

                    code = SubstituteFile(ref result);

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

