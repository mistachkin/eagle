/*
 * VariableManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("4ce3748a-1099-4c93-9fc6-c25f1e543737")]
    public interface IVariableManager
    {
        ///////////////////////////////////////////////////////////////////////
        // VARIABLE CHECKING
        ///////////////////////////////////////////////////////////////////////

        ReturnCode DoesVariableExist(
            VariableFlags flags,
            string name
            );

        ReturnCode DoesVariableExist(
            VariableFlags flags,
            string name,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // VARIABLE PERFORMANCE
        ///////////////////////////////////////////////////////////////////////

        ReturnCode MakeVariableFast(
            string name,
            bool fast,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // VARIABLE MANAGEMENT (SINGLE)
        ///////////////////////////////////////////////////////////////////////

        ReturnCode AddVariable(
            VariableFlags flags,
            string name,
            TraceList traces,
            bool errorOnExist,
            ref Result error
            );

        ReturnCode GetVariableValue(
            string name,
            ref Result value,
            ref Result error
            );

        ReturnCode GetVariableValue(
            VariableFlags flags,
            string name,
            ref Result value,
            ref Result error
            );

        ReturnCode ResetVariable(
            VariableFlags flags,
            string name,
            ref Result error
            );

        ReturnCode SetVariableEnumerable(
            VariableFlags flags,
            string name,
            IEnumerable collection,
            bool autoReset,
            ref Result error
            );

        ReturnCode SetVariableLink(
            VariableFlags flags,
            string name,
            MemberInfo memberInfo,
            object @object,
            ref Result error
            );

        ReturnCode SetVariableSystemArray(
            VariableFlags flags,
            string name,
            Array array,
            ref Result error
            );

        ReturnCode SetVariableValue(
            string name,
            string value,
            ref Result error
            );

        ReturnCode SetVariableValue(
            VariableFlags flags,
            string name,
            string value,
            TraceList traces,
            ref Result error
            );

        ReturnCode UnsetVariable(
            string name,
            ref Result error
            );

        ReturnCode UnsetVariable(
            VariableFlags flags,
            string name,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // VARIABLE SYNCHRONIZATION (SINGLE)
        ///////////////////////////////////////////////////////////////////////

        ReturnCode WaitVariable(
            EventWaitFlags eventWaitFlags,
            VariableFlags variableFlags,
            string name,
            long microseconds,
            long? threadId,
            int limit,
            EventWaitHandle @event,
            ref bool changed,
            ref Result error
            );

        ReturnCode LockVariable(
            EventWaitFlags eventWaitFlags,
            VariableFlags variableFlags,
            string name,
            long microseconds,
            EventWaitHandle @event,
            ref Result error
            );

        ReturnCode UnlockVariable(
            VariableFlags variableFlags,
            string name,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // VARIABLE MANAGEMENT (MULTIPLE)
        ///////////////////////////////////////////////////////////////////////

        ReturnCode ListVariables(
            string pattern,
            bool noCase,
            bool stopOnError,
            ref StringList list,
            ref Result error
            );

        ReturnCode ListVariables(
            VariableFlags flags,
            string pattern,
            bool noCase,
            bool stopOnError,
            ref StringList list,
            ref Result error
            );

        ReturnCode GetVariableValues(
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int getOk,
            ref Result error
            );

        ReturnCode GetVariableValues(
            VariableFlags flags,
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int getOk,
            ref Result error
            );

        ReturnCode SetVariableValues(
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int setOk,
            ref Result error
            );

        ReturnCode SetVariableValues(
            VariableFlags flags,
            TraceList traces,
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int setOk,
            ref Result error
            );

        ReturnCode UnsetVariables(
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int unsetOk,
            ref Result error
            );

        ReturnCode UnsetVariables(
            VariableFlags flags,
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int unsetOk,
            ref Result error
            );
    }
}
