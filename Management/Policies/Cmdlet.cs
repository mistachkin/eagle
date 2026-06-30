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
    /// <summary>
    /// This class implements the command policy used by the Eagle PowerShell
    /// cmdlets, allowing command execution to be confirmed (or denied) via the
    /// standard PowerShell "ShouldProcess" and "ShouldContinue" confirmation
    /// mechanisms.
    /// </summary>
    [ObjectId("adbeaf2d-744d-479f-8e80-eb53b06b5457")]
    internal static class _Cmdlet
    {
        #region Private Data
        /// <summary>
        /// When non-zero, the user has chosen to confirm all subsequent
        /// commands without further prompting.
        /// </summary>
        private static bool yesToAll = false;

        /// <summary>
        /// When non-zero, the user has chosen to deny all subsequent commands
        /// without further prompting.
        /// </summary>
        private static bool noToAll = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method resets the cached "yes to all" and "no to all"
        /// confirmation choices so that they do not persist between
        /// interpreters.
        /// </summary>
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
        /// <summary>
        /// This method retrieves the cmdlet script object associated with the
        /// specified interpreter, either via the prearranged opaque object
        /// handle or via the interpreter policy object.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to retrieve the cmdlet script object from.
        /// This parameter may be null.
        /// </param>
        /// <param name="script">
        /// Upon success, this contains the cmdlet script object.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method confirms, via the specified cmdlet, whether the
        /// operation it describes should be performed, catching and suppressing
        /// any exceptions.
        /// </summary>
        /// <param name="cmdlet">
        /// The cmdlet used to prompt for confirmation.  This parameter may be
        /// null.
        /// </param>
        /// <param name="verboseDescription">
        /// The textual description of the operation, used in "What-If" and
        /// "Verbose" modes.
        /// </param>
        /// <param name="verboseWarning">
        /// The warning message presented to the user as part of the
        /// confirmation prompt.
        /// </param>
        /// <param name="caption">
        /// The caption displayed with the confirmation prompt.
        /// </param>
        /// <returns>
        /// True if the operation should be performed; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method asks the user, via the specified cmdlet, whether the
        /// operation should continue, defaulting to allowing the operation when
        /// the host is not interactive and catching and suppressing any other
        /// exceptions.
        /// </summary>
        /// <param name="cmdlet">
        /// The cmdlet used to prompt the user.  This parameter may be null.
        /// </param>
        /// <param name="query">
        /// The query presented to the user.
        /// </param>
        /// <param name="caption">
        /// The caption displayed with the query.
        /// </param>
        /// <param name="yesToAll">
        /// On input and output, whether the user has chosen to confirm all
        /// subsequent operations without further prompting.
        /// </param>
        /// <param name="noToAll">
        /// On input and output, whether the user has chosen to deny all
        /// subsequent operations without further prompting.
        /// </param>
        /// <returns>
        /// True if the operation should continue; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method implements the command policy callback used by the
        /// cmdlets.  It confirms command execution with the user (via the
        /// "ShouldProcess" and "ShouldContinue" mechanisms) and approves or
        /// denies the command accordingly.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this policy is executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra, policy-specific data supplied to this callback, if any.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for the command being checked; element zero is
        /// the command name.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the policy was checked successfully
        /// (which does not necessarily mean the command is allowed to execute);
        /// otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
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
