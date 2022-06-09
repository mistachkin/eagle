/*
 * ScriptBinder.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("1df31d07-3746-4e9e-93e2-cbd63c22f1d4")]
    public interface IScriptBinder : IBinder
    {
        #region IScriptBinder Members
        //
        // NOTE: What is the default binder to use if the current script
        //       binder could not handle the type conversion and the
        //       fallback binder is null.  This may end up returning an
        //       object instance that simply uses Type.DefaultBinder.
        //
        IBinder DefaultBinder { get; set; }

        //
        // NOTE: What binder do we fallback upon if we cannot handle the
        //       type conversion, if any.  If this is null, we will use
        //       the value of DefaultBinder.
        //
        IBinder FallbackBinder { get; set; }

        //
        // NOTE: What script binder do we fallback upon if we cannot handle
        //       the type conversion, if any.  If this is null, there is no
        //       fallback script binder.  The default implementation of the
        //       IScriptBinder interface does not make use of this value.
        //
        IScriptBinder ParentBinder { get; set; }

        //
        // NOTE: What binding flags should be used when they cannot be given
        //       to us by a direct caller?
        //
        BindingFlags DefaultBindingFlags { get; set; }

        //
        // NOTE: Is this binder operating in "debug" mode?  Setting this
        //       to non-zero may or may not cause additional diagnostic
        //       messages to be emitted at runtime.
        //
        bool Debug { get; set; }

        //
        // NOTE: Is the specified method callable via reflection?  If not,
        //       it is not allowed for use with the core library.
        //
        bool IsAllowed(MethodBase method);

        //
        // NOTE: This allows resolution of a type with an optional
        //       object instance to be intercepted by custom binders.
        //
        ReturnCode GetObject(string text, TypeList types,
            AppDomain appDomain, BindingFlags bindingFlags, Type objectType,
            Type proxyType, ValueFlags valueFlags, CultureInfo cultureInfo,
            ref ITypedInstance value, ref Result error);

        //
        // NOTE: This allows resolution of a member with an optional
        //       object instance to be intercepted by custom binders.
        //
        ReturnCode GetMember(string text, ITypedInstance typedInstance,
            MemberTypes memberTypes, BindingFlags bindingFlags,
            ValueFlags valueFlags, CultureInfo cultureInfo,
            ref ITypedMember value, ref Result error);

        //
        // NOTE: Does the type of the object match the target type (at
        //       least as far as the binder is concerned)?
        //
        bool DoesMatchType(object value, Type type, MarshalFlags marshalFlags);

        //
        // NOTE: Is the ChangeType or ToString callback implemented by
        //       one of our internal methods?
        //
        bool IsCoreCallback(Delegate callback);

        //
        // NOTE: Is the ChangeType or ToString callback implemented by
        //       one of our internal methods that deals with the StringList
        //       type?
        //
        bool IsCoreStringListToStringCallback(ToStringCallback callback);
        bool IsCoreStringListChangeTypeCallback(ChangeTypeCallback callback);

        //
        // NOTE: Are types with custom ToString handling available?
        //
        bool HasToStringTypes();

        //
        // NOTE: Return the list of types with custom ToString
        //       handling that we know about.
        //
        ReturnCode ListToStrings(ref TypeList types,
            ref Result error);

        //
        // NOTE: Is the ToString callback implemented by one of our
        //       internal methods?
        //
        bool IsCoreToStringCallback(ToStringCallback callback);

        //
        // NOTE: Is a ToString callback registered for the specified
        //       type?
        //
        bool HasToStringCallback(Type type, bool primitive);
        bool HasToStringCallback(Type type, bool primitive,
            ref ToStringCallback callback);

        //
        // NOTE: Add a ToString callback for the specified type.
        //
        ReturnCode AddToStringCallback(Type type,
            ToStringCallback callback, ref Result error);

        //
        // NOTE: Remove a ToString callback for the specified type.
        //
        ReturnCode RemoveToStringCallback(Type type,
            ref Result error);

        //
        // NOTE: Invoke a ToString callback for the specified type.
        //
        ReturnCode InvokeToStringCallback(
            ToStringCallback callback, Type type, object value,
            OptionDictionary options, CultureInfo cultureInfo,
            IClientData clientData, ref MarshalFlags marshalFlags,
            ref string text, ref Result error);

        //
        // NOTE: Perform the necessary ToString callback(s) to convert the
        //       object value to a round-trip capable string representation.
        //
        ReturnCode ToString(IChangeTypeData changeTypeData, ref Result error);

        //
        // NOTE: Are types with custom ChangeType handling available?
        //
        bool HasChangeTypes();

        //
        // NOTE: Return the list of types with custom ChangeType
        //       handling that we know about.
        //
        ReturnCode ListChangeTypes(ref TypeList types,
            ref Result error);

        //
        // NOTE: Is the ChangeType callback implemented by one of our
        //       internal methods?
        //
        bool IsCoreChangeTypeCallback(ChangeTypeCallback callback);

        //
        // NOTE: Is a ChangeType callback registered for the specified
        //       type?
        //
        bool HasChangeTypeCallback(Type type, bool primitive);
        bool HasChangeTypeCallback(Type type, bool primitive,
            ref ChangeTypeCallback callback);

        //
        // NOTE: Add a ChangeType callback for the specified type.
        //
        ReturnCode AddChangeTypeCallback(Type type,
            ChangeTypeCallback callback, ref Result error);

        //
        // NOTE: Remove a ChangeType callback for the specified type.
        //
        ReturnCode RemoveChangeTypeCallback(Type type,
            ref Result error);

        //
        // NOTE: Invoke a ChangeType callback for the specified type.
        //
        ReturnCode InvokeChangeTypeCallback(
            ChangeTypeCallback callback, Type type, string text,
            OptionDictionary options, CultureInfo cultureInfo,
            IClientData clientData, ref MarshalFlags marshalFlags,
            ref object value, ref Result error);

        //
        // NOTE: Perform the necessary ChangeType callback(s) to convert
        //       the object value to the specified type.
        //
        ReturnCode ChangeType(IChangeTypeData changeTypeData,
            ref Result error);

        //
        // NOTE: *EXPERIMENTAL* Reorder the index for "best" method overloads.
        //       A return value of "Continue" means "use built-in semantics".
        //
        ReturnCode ReorderMethodIndexes(Type type, CultureInfo cultureInfo,
            MethodBase[] methods, ReorderFlags reorderFlags,
            ref IntList methodIndexList, ref ObjectArrayList argsList,
            ref Result error);

        //
        // NOTE: *EXPERIMENTAL* Select the index for "best" method overload.
        //       A return value of "Continue" means "use built-in semantics".
        //
        ReturnCode SelectMethodIndex(Type type, CultureInfo cultureInfo,
            TypeList parameterTypes, MarshalFlagsList parameterMarshalFlags,
            MethodBase[] methods, object[] args, IntList methodIndexList,
            ObjectArrayList argsList, ref int index, ref int methodIndex,
            ref Result error);

        //
        // NOTE: *EXPERIMENTAL* Select the "best" type to use when handling
        //       (e.g. invoking) the object.  A return value of "Continue"
        //       means "use built-in semantics".
        //
        ReturnCode SelectType(Interpreter interpreter,
            object oldValue, object newValue, TypeList types,
            CultureInfo cultureInfo, ObjectFlags objectFlags,
            ref Type type, ref Result error);
        #endregion
    }
}
