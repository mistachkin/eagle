/*
 * DelegateMapper.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

using DelegateList = System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>;

namespace Eagle._Interfaces.Public
{
    [ObjectId("0e9836bb-d62a-48f4-b1c8-fb0a639d9cf6")]
    public interface IDelegateMapper : IDisposable
    {
        ReturnCode Count(
            bool delegatesOnly,
            ref int count,
            ref Result error
        );

        ReturnCode Clear(
            bool delegatesOnly,
            ref int count,
            ref Result error
        );

        ReturnCode Load(
            Type objectType,
            BindingFlags? bindingFlags,
            MarshalFlags? marshalFlags,
            DelegateFlags? delegateFlags,
            bool clear,
            ref int count,
            ref Result error
        );

        EnsembleDictionary CreateEnsemble(
            Type objectType,
            int parameterCount
        );

        ReturnCode Lookup(
            Type objectType,
            string methodName,
            int parameterCount,
            int? limit,
            int? index,
            ref DelegateList delegates,
            ref Result error
        );

        ReturnCode ToList(
            Interpreter interpreter,
            Type objectType,
            string methodName,
            int? parameterCount,
            MatchMode mode,
            MarshalFlags marshalFlags,
            bool noCase,
            bool? safe,
            ref EnsembleDictionary subCommands,
            ref Result error
        );
    }
}
