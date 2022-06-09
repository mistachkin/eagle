/*
 * Class7.cs --
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
    /// <summary>
    /// Declare a static class that implements an example command execution
    /// policy.  The only requirement for such a class is for the policy
    /// callback methods to conform to the required delegate type and to be
    /// marked with the "MethodFlags.CommandPolicy" attribute.
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("bd2eb8ee-cf20-4435-9d89-fa2c7d596ed4")]
    internal static class Class7
    {
        #region Private Data
        /// <summary>
        /// The random number generator used by the example command execution
        /// policy for making "random" policy decisions.
        /// </summary>
        private static Random random;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Example Command Policy Implementation
        /// <summary>
        /// This method is an example command policy.  It will be called by
        /// the evaluation engine before any hidden command is allowed to be
        /// executed.  Determine if the command with the specified arguments
        /// and context should be allowed to execute.  The "Approved" method of
        /// the policy context object should be called if the command should be
        /// allowed to execute; however, this does not guarantee that the
        /// command will execute.  If any other active policies call the
        /// "Denied" method of the policy context object, execution of the
        /// command will be vetoed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The policy context data will be provided by the evaluation engine
        /// using this parameter.  To extract the <see cref="IPolicyContext" />
        /// instance, the <see cref="Utility.ExtractPolicyContextAndCommand" />
        /// method must be used.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments supplied to this command by the script
        /// being evaluated.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter should not be changed.  Upon failure,
        /// this parameter should be modified to contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The value <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Error" />  on failure.  Other return codes
        /// may be used to implement custom control structures.  If the policy
        /// wishes to approve of the command execution, the
        /// <see cref="IPolicyContext.Approved" /> method should be called
        /// prior to returning; otherwise, the
        /// <see cref="IPolicyContext.Denied" /> method should be called.  If
        /// neither of these methods is called, the default command execution
        /// decision will be used instead.
        /// </returns>
        [MethodFlags(MethodFlags.CommandPolicy)]
        private static ReturnCode PolicyCallback( /* POLICY */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            //
            // NOTE: We only want to make policy decisions for our own
            //       command(s); therefore, use the appropriate helper
            //       method provided by the interpreter.
            //
            IPolicyContext policyContext = null;
            bool match = false;

            if (Utility.ExtractPolicyContextAndCommand(
                    interpreter, clientData, typeof(Class2), 0,
                    ref policyContext, ref match, ref result) == ReturnCode.Ok)
            {
                if (match)
                {
                    //
                    // NOTE: If necessary, create a new random policy decision
                    //       maker to allow this policy to easily demonstrate
                    //       the effects of both the "approved" and "denied"
                    //       decisions.
                    //
                    if (random == null)
                        random = new Random();

                    //
                    // NOTE: Get a random integer; even numbers result in a
                    //       policy approval and odd numbers result in policy
                    //       denial.
                    //
                    if ((random.Next() % 2) == 0)
                        policyContext.Approved("random number is even");
                    else
                        policyContext.Denied("random number is odd");
                }

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
