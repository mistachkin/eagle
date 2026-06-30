/*
 * Update.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>update</c> command, which processes
    /// pending events and idle tasks for the interpreter, optionally limited to
    /// a caller-supplied mask of <see cref="UpdateFlags" /> values.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("9ddc097b-4635-4504-9493-98c25f0baf83")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("event")]
    internal sealed class Update : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>update</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Update(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>update</c> command.  It optionally
        /// parses a mask argument into <see cref="UpdateFlags" /> (defaulting
        /// to the interpreter's current <see cref="Interpreter.UpdateFlags" />)
        /// and then waits on idle tasks and/or processes queued events at the
        /// <see cref="EventPriority.Update" /> priority as directed by those
        /// flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name; an optional element one supplies the mask of
        /// <see cref="UpdateFlags" /> values that selects which events and
        /// idle tasks are processed.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this is empty, or contains the number of processed
        /// events when the <see cref="UpdateFlags.Count" /> flag is set.  Upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, the argument list is null,
        /// the mask cannot be parsed, or event processing fails, with details
        /// placed in <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if ((arguments.Count < 1) || (arguments.Count > 2))
            {
                result = "wrong # args: should be \"update ?mask?\"";
                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;
            UpdateFlags updateFlags = interpreter.UpdateFlags;

            if (arguments.Count == 2)
            {
                object enumValue = EnumOps.TryParseFlags(
                    interpreter, typeof(UpdateFlags),
                    updateFlags.ToString(), arguments[1],
                    interpreter.InternalCultureInfo, true,
                    true, true, ref result);

                if (enumValue is UpdateFlags)
                    updateFlags = (UpdateFlags)enumValue;
                else
                    code = ReturnCode.Error;
            }

            if (code == ReturnCode.Ok)
            {
                bool idleTasks = FlagOps.HasFlags(
                    updateFlags, UpdateFlags.IdleTasks, true);

                bool preQueue = FlagOps.HasFlags(
                    updateFlags, UpdateFlags.PreQueue, true);

                bool queue = FlagOps.HasFlags(
                    updateFlags, UpdateFlags.Queue, true);

                bool postQueue = FlagOps.HasFlags(
                    updateFlags, UpdateFlags.PostQueue, true);

                bool count = FlagOps.HasFlags(
                    updateFlags, UpdateFlags.Count, true);

                bool trace = FlagOps.HasFlags(
                    updateFlags, UpdateFlags.Trace, true);

                if ((code == ReturnCode.Ok) && (idleTasks || preQueue))
                {
                    code = EventOps.Wait(
                        interpreter, null, 0, null, true, false, false,
                        false, trace, ref result);
                }

                if ((code == ReturnCode.Ok) && queue)
                {
                    int eventCount = 0;

                    code = EventOps.ProcessEvents(
                        interpreter, interpreter.UpdateEventFlags,
                        EventPriority.Update, null, 0, true, false,
                        ref eventCount, ref result);

                    if ((code == ReturnCode.Ok) && count)
                        result = eventCount;
                }

                if ((code == ReturnCode.Ok) && (!idleTasks && postQueue))
                {
                    code = EventOps.Wait(
                        interpreter, null, 0, null, true, false, false,
                        false, trace, ref result);
                }
            }

            return code;
        }
        #endregion
    }
}
