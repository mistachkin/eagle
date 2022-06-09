/*
 * Cmdlet.cs --
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
using System.Management.Automation.Host;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Policies
{
    [ObjectId("adbeaf2d-744d-479f-8e80-eb53b06b5457")]
    internal static class _Cmdlet
    {
        #region Private Data
        private static bool yesToAll = false;
        private static bool noToAll = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static void Reset()
        {
            //
            // NOTE: The cmdlets need to be able to reset these confirmation
            //       variables so that they do not "stick" between interpreters.
            //
            yesToAll = false;
            noToAll = false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static ReturnCode GetCmdlet(
            Interpreter interpreter,
            ref _Cmdlets.Script script,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            IObject @object = null;

            if (interpreter.GetObject(
                    _Cmdlets.Script.CmdletObjectName,
                    LookupFlags.Default, ref @object) == ReturnCode.Ok)
            {
                script = @object.Value as _Cmdlets.Script;

                if (script != null)
                    return ReturnCode.Ok;
                else
                    error = "cmdlet object is not a script";
            }
            else
            {
                //
                // BUGBUG: This is potentially a bad idea since the property
                //         used here (i.e. PolicyObject) could, in theory,
                //         be in-use by another policy; however, this impact
                //         of this is mitigated by the fact that the property
                //         is only relied upon while the interpreter is being
                //         created and initialized.
                //
                script = interpreter.PolicyObject as _Cmdlets.Script;

                if (script != null)
                    return ReturnCode.Ok;
                else
                    error = "policy object is not a script";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldProcess(
            Cmdlet cmdlet,
            string verboseDescription,
            string verboseWarning,
            string caption
            )
        {
            try
            {
                if (cmdlet != null)
                {
                    return cmdlet.ShouldProcess(
                        verboseDescription, verboseWarning, caption);
                }
            }
            catch
            {
                // do nothing.
            }

            //
            // NOTE: We have no idea what happened.  Default to not allowing
            //       the command to continue.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldContinue(
            Cmdlet cmdlet,
            string query,
            string caption,
            ref bool yesToAll,
            ref bool noToAll
            )
        {
            try
            {
                if (cmdlet != null)
                {
                    return cmdlet.ShouldContinue(
                        query, caption, ref yesToAll, ref noToAll);
                }
            }
            catch (HostException)
            {
                //
                // NOTE: There current host is not in interactive mode.
                //       Default to allowing the command to continue.
                //
                return true;
            }
            catch
            {
                // do nothing.
            }

            //
            // NOTE: We have no idea what happened.  Default to not allowing
            //       the command to continue.
            //
            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Policy
        [MethodFlags(MethodFlags.CommandPolicy)]
        public static ReturnCode PolicyCallback( /* POLICY */
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            IPolicyContext policyContext = null;
            bool match = false;

            if (Utility.ExtractPolicyContextAndCommand(
                    interpreter, clientData, null, 0, ref policyContext,
                    ref match, ref result) == ReturnCode.Ok)
            {
                if (match)
                {
                    //
                    // NOTE: Fetch the reference to the Cmdlet itself that we
                    //       smuggled in via the named opaque object handle
                    //       that was prearranged with the base cmdlet itself.
                    //
                    _Cmdlets.Script script = null;

                    if (GetCmdlet(interpreter,
                            ref script, ref result) == ReturnCode.Ok)
                    {
                        //
                        // NOTE: Grab the interpreter host, if any.
                        //
                        IInteractiveHost interactiveHost = interpreter.Host;

                        //
                        // NOTE: If the interpreter host is available, use the
                        //       title as the caption; otherwise, we will use
                        //       the hard-coded default.
                        //
                        string processCaption = (interactiveHost != null) ?
                            interactiveHost.Title : null;

                        //
                        // NOTE: If the caption is null or empty, use the
                        //       hard-coded default.
                        //
                        if (String.IsNullOrEmpty(processCaption))
                            processCaption = _Constants.Policy.ProcessCaption;

                        //
                        // NOTE: Build the description of the operation for
                        //       "What-If" and "Verbose" modes.
                        //
                        string verboseDescription = String.Format(
                            _Constants.Policy.VerboseDescription, arguments);

                        //
                        // NOTE: Grab the command name from the argument list
                        //       because we need to present it to the user in
                        //       the confirmation query.
                        //
                        string commandName = (arguments.Count > 0) ?
                            arguments[0] : null;

                        //
                        // NOTE: Build the confirmation query to present to
                        //       the user.
                        //
                        string verboseWarning = String.Format(
                            _Constants.Policy.VerboseWarning, commandName,
                            arguments);

                        //
                        // TODO: *TEST* Verify that this works correctly and
                        //       has the expected semantics.
                        //
                        if (ShouldProcess(script, verboseDescription,
                                verboseWarning, processCaption))
                        {
                            //
                            // NOTE: If the interpreter host is available, use
                            //       the title as the caption; otherwise, we
                            //       will use the hard-coded default.
                            //
                            string continueCaption = (interactiveHost != null) ?
                                interactiveHost.Title : null;

                            //
                            // NOTE: If the caption is null or empty, use the
                            //       hard-coded default.
                            //
                            if (String.IsNullOrEmpty(continueCaption))
                                continueCaption = _Constants.Policy.ContinueCaption;

                            //
                            // NOTE: Build the re-confirmation query to present
                            //       to the user.
                            //
                            string query = String.Format(
                                _Constants.Policy.Query, verboseWarning);

                            //
                            // NOTE: If we are in "force" mode or the user
                            //       allows us to continue then do so;
                            //       otherwise, do nothing and the command will
                            //       be allowed/denied based on the other
                            //       policies, if any.  In the event that there
                            //       are no other policies present, the command
                            //       will not be allowed to execute.
                            //
                            // BUGFIX: Cannot ask user when not interactive.
                            //
                            if (script.Force ||
                                ShouldContinue(script, query, continueCaption,
                                    ref yesToAll, ref noToAll))
                            {
                                //
                                // NOTE: The user has explicitly approved the
                                //       command execution.
                                //
                                policyContext.Approved();
                            }
                            else if (script.Deny)
                            {
                                //
                                // BUGFIX: Must explicitly deny to override the
                                //         built-in policies (e.g. for [info],
                                //         [object], etc).
                                //
                                policyContext.Denied();
                            }
                        }
                        else if (script.Deny)
                        {
                            //
                            // BUGFIX: Must explicitly deny to override the
                            //         built-in policies (e.g. for [info],
                            //         [object], etc).
                            //
                            policyContext.Denied();
                        }

                        //
                        // NOTE: The policy checking has been successful;
                        //       however, this does not necessarily mean
                        //       that we allow the command to be executed.
                        //
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    result = "policyContext does not contain a command object";
                }
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
