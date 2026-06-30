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
    /// <summary>
    /// This interface is implemented by the script binder used by an Eagle
    /// interpreter to customize how the core library resolves types and
    /// members, converts values to and from their string representations,
    /// and selects among candidate method overloads.  It extends
    /// <see cref="IBinder" /> with Eagle-specific hooks for object and
    /// member lookup, ToString and ChangeType callbacks, and experimental
    /// method overload selection.
    /// </summary>
    [ObjectId("1df31d07-3746-4e9e-93e2-cbd63c22f1d4")]
    public interface IScriptBinder : IBinder
    {
        #region IScriptBinder Members
        /// <summary>
        /// Gets or sets the default binder to use when the current script binder
        /// cannot handle a type conversion and no fallback binder is available.
        /// The resulting object instance may simply defer to the default binder
        /// provided by the runtime.
        /// </summary>
        //
        // NOTE: What is the default binder to use if the current script
        //       binder could not handle the type conversion and the
        //       fallback binder is null.  This may end up returning an
        //       object instance that simply uses Type.DefaultBinder.
        //
        IBinder DefaultBinder { get; set; }

        /// <summary>
        /// Gets or sets the binder to fall back upon when this binder cannot
        /// handle a type conversion, if any.  If this is null, the value of
        /// <see cref="DefaultBinder" /> is used instead.
        /// </summary>
        //
        // NOTE: What binder do we fallback upon if we cannot handle the
        //       type conversion, if any.  If this is null, we will use
        //       the value of DefaultBinder.
        //
        IBinder FallbackBinder { get; set; }

        /// <summary>
        /// Gets or sets the script binder to fall back upon when this binder
        /// cannot handle a type conversion, if any.  If this is null, there is
        /// no fallback script binder.  The default implementation of this
        /// interface does not make use of this value.
        /// </summary>
        //
        // NOTE: What script binder do we fallback upon if we cannot handle
        //       the type conversion, if any.  If this is null, there is no
        //       fallback script binder.  The default implementation of the
        //       IScriptBinder interface does not make use of this value.
        //
        IScriptBinder ParentBinder { get; set; }

        /// <summary>
        /// Gets or sets the binding flags to use when none are supplied by a
        /// direct caller.
        /// </summary>
        //
        // NOTE: What binding flags should be used when they cannot be given
        //       to us by a direct caller?
        //
        BindingFlags DefaultBindingFlags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this binder is operating in
        /// "debug" mode.  Setting this to true may cause additional diagnostic
        /// messages to be emitted at runtime.
        /// </summary>
        //
        // NOTE: Is this binder operating in "debug" mode?  Setting this
        //       to non-zero may or may not cause additional diagnostic
        //       messages to be emitted at runtime.
        //
        bool Debug { get; set; }

        /// <summary>
        /// Determines whether the specified method may be called via reflection.
        /// Methods that are not allowed cannot be used with the core library.
        /// </summary>
        /// <param name="method">
        /// The method to check.  This parameter should not be null.
        /// </param>
        /// <returns>
        /// True if the specified method is allowed for use via reflection;
        /// otherwise, false.
        /// </returns>
        //
        // NOTE: Is the specified method callable via reflection?  If not,
        //       it is not allowed for use with the core library.
        //
        bool IsAllowed(MethodBase method);

        /// <summary>
        /// Resolves a type, together with an optional object instance, allowing
        /// the resolution to be intercepted by custom binders.
        /// </summary>
        /// <param name="text">
        /// The input string to resolve into a type and optional object
        /// instance.  This parameter may be null.
        /// </param>
        /// <param name="types">
        /// The list of candidate types to consider during resolution, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to search for matching types, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags to use during resolution.
        /// </param>
        /// <param name="objectType">
        /// The required type of the resolved object instance, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="proxyType">
        /// The type to use when the resolved object instance is a
        /// transparent proxy, if any.  This parameter may be null.
        /// </param>
        /// <param name="valueFlags">
        /// The flags used to control how the input string is
        /// interpreted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for value conversion, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this receives the resolved type and optional
        /// object instance.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: This allows resolution of a type with an optional
        //       object instance to be intercepted by custom binders.
        //
        ReturnCode GetObject(string text, TypeList types,
            AppDomain appDomain, BindingFlags bindingFlags, Type objectType,
            Type proxyType, ValueFlags valueFlags, CultureInfo cultureInfo,
            ref ITypedInstance value, ref Result error);

        /// <summary>
        /// Resolves a member of the specified typed instance, allowing the
        /// resolution to be intercepted by custom binders.
        /// </summary>
        /// <param name="text">
        /// The name to resolve into a member.  This parameter may be null.
        /// </param>
        /// <param name="typedInstance">
        /// The type and optional object instance to search for the
        /// member.  This parameter should not be null.
        /// </param>
        /// <param name="memberTypes">
        /// The kinds of members to consider during resolution.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags to use during resolution.
        /// </param>
        /// <param name="valueFlags">
        /// The flags used to control how the input string is
        /// interpreted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for value conversion, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this receives the resolved member.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: This allows resolution of a member with an optional
        //       object instance to be intercepted by custom binders.
        //
        ReturnCode GetMember(string text, ITypedInstance typedInstance,
            MemberTypes memberTypes, BindingFlags bindingFlags,
            ValueFlags valueFlags, CultureInfo cultureInfo,
            ref ITypedMember value, ref Result error);

        /// <summary>
        /// Determines whether the type of the specified value matches the target
        /// type, at least as far as this binder is concerned.
        /// </summary>
        /// <param name="value">
        /// The object value whose type is to be checked.  This parameter
        /// may be null.
        /// </param>
        /// <param name="type">
        /// The target type to compare against.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags used to control type matching.
        /// </param>
        /// <returns>
        /// True if the value is considered to match the target type;
        /// otherwise, false.
        /// </returns>
        //
        // NOTE: Does the type of the object match the target type (at
        //       least as far as the binder is concerned)?
        //
        bool DoesMatchType(object value, Type type, MarshalFlags marshalFlags);

        /// <summary>
        /// Determines whether the specified ChangeType or ToString callback is
        /// implemented by one of the internal methods of the core library.
        /// </summary>
        /// <param name="callback">
        /// The callback delegate to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the callback is implemented by the core library;
        /// otherwise, false.
        /// </returns>
        //
        // NOTE: Is the ChangeType or ToString callback implemented by
        //       one of our internal methods?
        //
        bool IsCoreCallback(Delegate callback);

        /// <summary>
        /// Determines whether the specified ToString callback is implemented by
        /// one of the internal methods of the core library that handles the
        /// <see cref="StringList" /> type.
        /// </summary>
        /// <param name="callback">
        /// The ToString callback to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the callback is implemented by the core library;
        /// otherwise, false.
        /// </returns>
        //
        // NOTE: Is the ChangeType or ToString callback implemented by
        //       one of our internal methods that deals with the StringList
        //       type?
        //
        bool IsCoreStringListToStringCallback(ToStringCallback callback);
        /// <summary>
        /// Determines whether the specified ChangeType callback is implemented by
        /// one of the internal methods of the core library that handles the
        /// <see cref="StringList" /> type.
        /// </summary>
        /// <param name="callback">
        /// The ChangeType callback to check.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the callback is implemented by the core library;
        /// otherwise, false.
        /// </returns>
        bool IsCoreStringListChangeTypeCallback(ChangeTypeCallback callback);

        /// <summary>
        /// Determines whether any types with custom ToString handling are
        /// available.
        /// </summary>
        /// <returns>
        /// True if at least one type with custom ToString handling is
        /// available; otherwise, false.
        /// </returns>
        //
        // NOTE: Are types with custom ToString handling available?
        //
        bool HasToStringTypes();

        /// <summary>
        /// Returns the list of types with custom ToString handling that are known
        /// to this binder.
        /// </summary>
        /// <param name="types">
        /// Upon success, this receives the list of types with custom
        /// ToString handling.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Return the list of types with custom ToString
        //       handling that we know about.
        //
        ReturnCode ListToStrings(ref TypeList types,
            ref Result error);

        /// <summary>
        /// Determines whether the specified ToString callback is implemented by
        /// one of the internal methods of the core library.
        /// </summary>
        /// <param name="callback">
        /// The ToString callback to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the callback is implemented by the core library;
        /// otherwise, false.
        /// </returns>
        //
        // NOTE: Is the ToString callback implemented by one of our
        //       internal methods?
        //
        bool IsCoreToStringCallback(ToStringCallback callback);

        /// <summary>
        /// Determines whether a ToString callback is registered for the specified
        /// type.
        /// </summary>
        /// <param name="type">
        /// The type to check.  This parameter should not be null.
        /// </param>
        /// <param name="primitive">
        /// True if the type should be treated as a primitive type for
        /// the purposes of this check; otherwise, false.
        /// </param>
        /// <returns>
        /// True if a ToString callback is registered for the specified
        /// type; otherwise, false.
        /// </returns>
        //
        // NOTE: Is a ToString callback registered for the specified
        //       type?
        //
        bool HasToStringCallback(Type type, bool primitive);
        /// <summary>
        /// Determines whether a ToString callback is registered for the specified
        /// type, returning the registered callback when one is present.
        /// </summary>
        /// <param name="type">
        /// The type to check.  This parameter should not be null.
        /// </param>
        /// <param name="primitive">
        /// True if the type should be treated as a primitive type for
        /// the purposes of this check; otherwise, false.
        /// </param>
        /// <param name="callback">
        /// Upon success, this receives the ToString callback registered
        /// for the specified type.
        /// </param>
        /// <returns>
        /// True if a ToString callback is registered for the specified
        /// type; otherwise, false.
        /// </returns>
        bool HasToStringCallback(Type type, bool primitive,
            ref ToStringCallback callback);

        /// <summary>
        /// Adds a ToString callback for the specified type.
        /// </summary>
        /// <param name="type">
        /// The type to associate the callback with.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="callback">
        /// The ToString callback to add.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Add a ToString callback for the specified type.
        //
        ReturnCode AddToStringCallback(Type type,
            ToStringCallback callback, ref Result error);

        /// <summary>
        /// Removes the ToString callback for the specified type.
        /// </summary>
        /// <param name="type">
        /// The type whose ToString callback is to be removed.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Remove a ToString callback for the specified type.
        //
        ReturnCode RemoveToStringCallback(Type type,
            ref Result error);

        /// <summary>
        /// Invokes the specified ToString callback for the specified type and
        /// value.
        /// </summary>
        /// <param name="callback">
        /// The ToString callback to invoke.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="type">
        /// The type of the value being converted.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="value">
        /// The object value to convert to its string representation.  This
        /// parameter may be null.
        /// </param>
        /// <param name="options">
        /// The options that control the conversion, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for the conversion, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags used to control marshalling; these may be
        /// modified by the callback.
        /// </param>
        /// <param name="text">
        /// Upon success, this receives the string representation of the
        /// value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Invoke a ToString callback for the specified type.
        //
        ReturnCode InvokeToStringCallback(
            ToStringCallback callback, Type type, object value,
            OptionDictionary options, CultureInfo cultureInfo,
            IClientData clientData, ref MarshalFlags marshalFlags,
            ref string text, ref Result error);

        /// <summary>
        /// Performs the necessary ToString callbacks to convert an object value
        /// to a round-trip capable string representation.
        /// </summary>
        /// <param name="changeTypeData">
        /// The data describing the value to convert and receiving
        /// the conversion result.  This parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Perform the necessary ToString callback(s) to convert the
        //       object value to a round-trip capable string representation.
        //
        ReturnCode ToString(IChangeTypeData changeTypeData, ref Result error);

        /// <summary>
        /// Determines whether any types with custom ChangeType handling are
        /// available.
        /// </summary>
        /// <returns>
        /// True if at least one type with custom ChangeType handling is
        /// available; otherwise, false.
        /// </returns>
        //
        // NOTE: Are types with custom ChangeType handling available?
        //
        bool HasChangeTypes();

        /// <summary>
        /// Returns the list of types with custom ChangeType handling that are
        /// known to this binder.
        /// </summary>
        /// <param name="types">
        /// Upon success, this receives the list of types with custom
        /// ChangeType handling.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Return the list of types with custom ChangeType
        //       handling that we know about.
        //
        ReturnCode ListChangeTypes(ref TypeList types,
            ref Result error);

        /// <summary>
        /// Determines whether the specified ChangeType callback is implemented by
        /// one of the internal methods of the core library.
        /// </summary>
        /// <param name="callback">
        /// The ChangeType callback to check.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the callback is implemented by the core library;
        /// otherwise, false.
        /// </returns>
        //
        // NOTE: Is the ChangeType callback implemented by one of our
        //       internal methods?
        //
        bool IsCoreChangeTypeCallback(ChangeTypeCallback callback);

        /// <summary>
        /// Determines whether a ChangeType callback is registered for the
        /// specified type.
        /// </summary>
        /// <param name="type">
        /// The type to check.  This parameter should not be null.
        /// </param>
        /// <param name="primitive">
        /// True if the type should be treated as a primitive type for
        /// the purposes of this check; otherwise, false.
        /// </param>
        /// <returns>
        /// True if a ChangeType callback is registered for the specified
        /// type; otherwise, false.
        /// </returns>
        //
        // NOTE: Is a ChangeType callback registered for the specified
        //       type?
        //
        bool HasChangeTypeCallback(Type type, bool primitive);
        /// <summary>
        /// Determines whether a ChangeType callback is registered for the
        /// specified type, returning the registered callback when one is
        /// present.
        /// </summary>
        /// <param name="type">
        /// The type to check.  This parameter should not be null.
        /// </param>
        /// <param name="primitive">
        /// True if the type should be treated as a primitive type for
        /// the purposes of this check; otherwise, false.
        /// </param>
        /// <param name="callback">
        /// Upon success, this receives the ChangeType callback
        /// registered for the specified type.
        /// </param>
        /// <returns>
        /// True if a ChangeType callback is registered for the specified
        /// type; otherwise, false.
        /// </returns>
        bool HasChangeTypeCallback(Type type, bool primitive,
            ref ChangeTypeCallback callback);

        /// <summary>
        /// Adds a ChangeType callback for the specified type.
        /// </summary>
        /// <param name="type">
        /// The type to associate the callback with.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="callback">
        /// The ChangeType callback to add.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Add a ChangeType callback for the specified type.
        //
        ReturnCode AddChangeTypeCallback(Type type,
            ChangeTypeCallback callback, ref Result error);

        /// <summary>
        /// Removes the ChangeType callback for the specified type.
        /// </summary>
        /// <param name="type">
        /// The type whose ChangeType callback is to be removed.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Remove a ChangeType callback for the specified type.
        //
        ReturnCode RemoveChangeTypeCallback(Type type,
            ref Result error);

        /// <summary>
        /// Invokes the specified ChangeType callback for the specified type and
        /// input string.
        /// </summary>
        /// <param name="callback">
        /// The ChangeType callback to invoke.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="type">
        /// The target type for the conversion.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="text">
        /// The input string to convert.  This parameter may be null.
        /// </param>
        /// <param name="options">
        /// The options that control the conversion, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for the conversion, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags used to control marshalling; these may be
        /// modified by the callback.
        /// </param>
        /// <param name="value">
        /// Upon success, this receives the converted object value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Invoke a ChangeType callback for the specified type.
        //
        ReturnCode InvokeChangeTypeCallback(
            ChangeTypeCallback callback, Type type, string text,
            OptionDictionary options, CultureInfo cultureInfo,
            IClientData clientData, ref MarshalFlags marshalFlags,
            ref object value, ref Result error);

        /// <summary>
        /// Performs the necessary ChangeType callbacks to convert an object value
        /// to the specified type.
        /// </summary>
        /// <param name="changeTypeData">
        /// The data describing the value to convert and receiving
        /// the conversion result.  This parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Perform the necessary ChangeType callback(s) to convert
        //       the object value to the specified type.
        //
        ReturnCode ChangeType(IChangeTypeData changeTypeData,
            ref Result error);

        /// <summary>
        /// Reorders the indexes of the "best" method overloads for the specified
        /// type.  This member is experimental.
        /// </summary>
        /// <param name="type">
        /// The type that declares the candidate methods.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="methods">
        /// The array of candidate methods.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="reorderFlags">
        /// The flags used to control reordering.
        /// </param>
        /// <param name="methodIndexList">
        /// The list of candidate method indexes to reorder; this
        /// receives the reordered indexes.
        /// </param>
        /// <param name="argsList">
        /// The list of argument arrays corresponding to each candidate
        /// method; this receives the reordered arrays.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Continue" /> to use the built-in semantics;
        /// otherwise, a non-Ok value with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: *EXPERIMENTAL* Reorder the index for "best" method overloads.
        //       A return value of "Continue" means "use built-in semantics".
        //
        ReturnCode ReorderMethodIndexes(Type type, CultureInfo cultureInfo,
            MethodBase[] methods, ReorderFlags reorderFlags,
            ref IntList methodIndexList, ref ObjectArrayList argsList,
            ref Result error);

        /// <summary>
        /// Selects the index of the "best" method overload for the specified
        /// type.  This member is experimental.
        /// </summary>
        /// <param name="type">
        /// The type that declares the candidate methods.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of argument types being passed, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// The per-parameter marshalling flags, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="methods">
        /// The array of candidate methods.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="args">
        /// The argument values being passed, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="methodIndexList">
        /// The list of candidate method indexes to choose from.
        /// This parameter should not be null.
        /// </param>
        /// <param name="argsList">
        /// The list of argument arrays corresponding to each candidate
        /// method.  This parameter should not be null.
        /// </param>
        /// <param name="index">
        /// Upon success, this receives the index into the candidate list
        /// of the selected method.
        /// </param>
        /// <param name="methodIndex">
        /// Upon success, this receives the index of the selected
        /// method.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Continue" /> to use the built-in semantics;
        /// otherwise, a non-Ok value with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: *EXPERIMENTAL* Select the index for "best" method overload.
        //       A return value of "Continue" means "use built-in semantics".
        //
        ReturnCode SelectMethodIndex(Type type, CultureInfo cultureInfo,
            TypeList parameterTypes, MarshalFlagsList parameterMarshalFlags,
            MethodBase[] methods, object[] args, IntList methodIndexList,
            ObjectArrayList argsList, ref int index, ref int methodIndex,
            ref Result error);

        /// <summary>
        /// Selects the "best" type to use when handling (e.g. invoking) the
        /// specified object.  This member is experimental.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operation is being performed
        /// in.  This parameter should not be null.
        /// </param>
        /// <param name="oldValue">
        /// The current object value, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newValue">
        /// The proposed new object value, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="types">
        /// The list of candidate types to choose from, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="objectFlags">
        /// The flags associated with the object.
        /// </param>
        /// <param name="type">
        /// Upon success, this receives the selected type.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Continue" /> to use the built-in semantics;
        /// otherwise, a non-Ok value with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
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
