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
    [ObjectId("9ddc097b-4635-4504-9493-98c25f0baf83")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("event")]
    internal sealed class Update : Core
    {
        #region Public Constructors
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
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
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
                if ((code == ReturnCode.Ok) &&
                    (FlagOps.HasFlags(updateFlags, UpdateFlags.IdleTasks, true) ||
                    FlagOps.HasFlags(updateFlags, UpdateFlags.PreQueue, true)))
                {
                    code = EventOps.Wait(
                        interpreter, null, 0, null, true, false, false, false,
                        ref result);
                }

                if ((code == ReturnCode.Ok) &&
                    FlagOps.HasFlags(updateFlags, UpdateFlags.Queue, true))
                {
                    int eventCount = 0;

                    code = EventOps.ProcessEvents(
                        interpreter, interpreter.UpdateEventFlags,
                        EventPriority.Update, null, 0, true, false,
                        ref eventCount, ref result);

                    if ((code == ReturnCode.Ok) &&
                        FlagOps.HasFlags(updateFlags, UpdateFlags.Count, true))
                    {
                        result = eventCount;
                    }
                }

                if ((code == ReturnCode.Ok) &&
                    (!FlagOps.HasFlags(updateFlags, UpdateFlags.IdleTasks, true) &&
                    FlagOps.HasFlags(updateFlags, UpdateFlags.PostQueue, true)))
                {
                    code = EventOps.Wait(
                        interpreter, null, 0, null, true, false, false, false,
                        ref result);
                }
            }

            return code;
        }
        #endregion
    }
}
