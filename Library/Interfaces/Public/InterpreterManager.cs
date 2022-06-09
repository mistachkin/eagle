/*
 * InterpreterManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("cb781d88-6b9a-4689-82c0-849c230117e8")]
    public interface IInterpreterManager
    {
        bool PushActive(IClientData clientData);
        bool PopActive();

        bool? SetDisposalEnabled(bool noComplain, bool? enabled);

        bool IsOrphanInterpreter();
        bool HasChildInterpreters(ref Result error);
        ReturnCode DoesChildInterpreterExist(string path);

        //
        // TODO: Change these to use the IInterpreter type.
        //
        ReturnCode GetChildInterpreter(
            string path,
            LookupFlags lookupFlags,
            bool nested,
            bool create,
            ref Interpreter interpreter,
            ref string name,
            ref Result error
            );

        ReturnCode CreateChildInterpreter(
            string path,
            IClientData clientData,
            InterpreterSettings interpreterSettings,
            bool isolated,
            bool security,
            ref Result result
            );

        ReturnCode AddChildInterpreter(
            string name,
            Interpreter interpreter,
            IClientData clientData,
            ref Result error
            );

        ReturnCode RemoveChildInterpreter(
            string name,
            IClientData clientData,
            ref Result error
            );

        ReturnCode RemoveChildInterpreter(
            string name,
            IClientData clientData,
            bool synchronous,
            ref Result error
            );
    }
}
