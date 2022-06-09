/*
 * ContextManager.cs --
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

namespace Eagle._Interfaces.Private
{
    //
    // NOTE: This interface is currently private; however, it may be "promoted"
    //       to public at some point.
    //
    [ObjectId("740b2349-fc1c-45f7-9549-9e96f20e8221")]
    internal interface IContextManager
    {
        int GetInterpreterContextCount();

        bool ReleaseEngineContext(bool global);
        bool ReleaseEngineContext(bool global, ref Result error);

        IEngineContext GetEngineContext(bool create);
        IEngineContext GetEngineContext(bool create, ref Result error);

        int GetEngineContextCount();

        int PurgeEngineContexts(
            Interpreter interpreter, bool nonPrimary, bool global);

        bool ReleaseInteractiveContext(bool global);
        bool ReleaseInteractiveContext(bool global, ref Result error);

        IInteractiveContext GetInteractiveContext(bool create);
        IInteractiveContext GetInteractiveContext(bool create, ref Result error);

        int GetInteractiveContextCount();

        int PurgeInteractiveContexts(
            Interpreter interpreter, bool nonPrimary, bool global);

        bool ReleaseTestContext(bool global);
        bool ReleaseTestContext(bool global, ref Result error);

        ITestContext GetTestContext(bool create);
        ITestContext GetTestContext(bool create, ref Result error);

        int GetTestContextCount();

        int PurgeTestContexts(
            Interpreter interpreter, bool nonPrimary, bool global);

        bool ReleaseVariableContext(bool global);
        bool ReleaseVariableContext(bool global, ref Result error);

        IVariableContext GetVariableContext(bool create);
        IVariableContext GetVariableContext(bool create, ref Result error);

        int GetVariableContextCount();

        int PurgeVariableContexts(
            Interpreter interpreter, bool nonPrimary, bool global);

        void Free(bool global);
    }
}
