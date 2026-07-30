/*
 * Dict.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using ObjectPair = System.Collections.Generic.KeyValuePair<string, object>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the <c>dict</c> command, which creates, queries,
    /// and manipulates dictionary values and dictionary-valued variables.  It
    /// is an ensemble whose sub-commands cover creation, lookup, mutation,
    /// iteration, filtering, and related operations on dictionaries.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("dc87a0be-2552-4244-8fcb-70f581ae0b70")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard
    )]
    [ObjectGroup("list")]
    internal sealed class Dict : _Commands.Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>dict</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Dict(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        /// <summary>
        /// The collection of sub-command names supported by this ensemble
        /// command, used to dispatch each invocation to the appropriate
        /// sub-command handler.
        /// </summary>
        private readonly EnsembleDictionary subCommands =
            new EnsembleDictionary(new string[] {
            "append", "create", "exists", "filter", "foreach", "get",
            "incr", "info", "keys", "lappend", "map", "merge", "remove",
            "replace", "set", "size", "unset", "update", "values", "with"
        });

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the collection of sub-command names supported by this ensemble
        /// command.
        /// </summary>
        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>dict</c> command.  It dispatches to the
        /// requested ensemble sub-command (for example <c>create</c>,
        /// <c>get</c>, <c>set</c>, <c>append</c>, <c>incr</c>, <c>filter</c>,
        /// <c>foreach</c>, <c>map</c>, <c>merge</c>, <c>update</c>, or
        /// <c>with</c>) in order to create, query, or modify dictionary values
        /// and dictionary-valued variables, honoring the recognized options for
        /// each sub-command.
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
        /// command name and element one is the sub-command name, followed by
        /// any sub-command-specific arguments.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result produced by the dispatched
        /// sub-command.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, the argument list is null, or
        /// the dispatched sub-command fails, with details placed in
        /// <paramref name="result" />.
        /// </returns>
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

            int argumentCount = arguments.Count;

            if (argumentCount < 2)
            {
                result = "wrong # args: should be \"dict option ?arg ...?\"";
                return ReturnCode.Error;
            }

            ReturnCode code;
            string subCommand = arguments[1];
            bool tried = false;

            code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                interpreter, this, clientData, arguments, true,
                null, ref subCommand, ref tried, ref result);

            if ((code != ReturnCode.Ok) || tried)
                goto done;

            int argumentIndex; /* REUSED */
            VariableFlags variableFlags; /* REUSED */
            string variableName; /* REUSED */
            string keyName; /* REUSED */
            object value; /* REUSED */
            string keyVarName; /* REUSED */
            string valueVarName; /* REUSED */
            string body; /* REUSED */
            IScriptLocation location; /* REUSED */
            long longValue; /* REUSED */
            bool boolValue; /* REUSED */
            StringList keyNames; /* REUSED */
            StringList list; /* REUSED */
            IVariable variable; /* REUSED */
            ObjectDictionary dictionary; /* REUSED */
            ObjectDictionary localDictionary; /* REUSED */
            ObjectDictionary otherDictionary; /* REUSED */
            int changeCount; /* REUSED */
            bool stopOnNotFound; /* REUSED */
            Result localResult; /* REUSED */

            switch (subCommand)
            {
                case "append":
                    {
                        if (argumentCount >= 4)
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                variableFlags = VariableFlags.ArrayCommandMask;
                                variableName = arguments[2];
                                variable = null;
                                dictionary = null;

                                code = interpreter.GetDictionaryVariableViaResolversWithSplit(
                                    variableName, ref variableFlags, ref variable, ref dictionary,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;

                                StringBuilder builder;

                                keyName = arguments[3];

                                if (keyName == null)
                                {
                                    result = "invalid dictionary key name";
                                    code = ReturnCode.Error;

                                    goto done;
                                }

                                if (dictionary.TryGetValue(keyName, out value))
                                {
                                    builder = (value is StringBuilder) ?
                                        (StringBuilder)value : StringBuilderFactory.Create(
                                            StringOps.GetStringFromObject(value));
                                }
                                else
                                {
                                    builder = StringBuilderFactory.Create();
                                }

                                if (!dictionary.InternalAddOrChange(
                                        interpreter, keyName, builder, ref result))
                                {
                                    code = ReturnCode.Error;
                                    goto done;
                                }

                                for (argumentIndex = 4;
                                        argumentIndex < argumentCount; argumentIndex++)
                                {
                                    builder.Append(arguments[argumentIndex]);
                                }

                                if (!FlagOps.HasFlags(
                                        variable.Flags, VariableFlags.NonTrace, false))
                                {
                                    code = interpreter.FireTraces(
                                        BreakpointType.BeforeVariableSet, variableFlags,
                                        null, variableName, null, dictionary, null, null,
                                        variable, ref result);

                                    if (code != ReturnCode.Ok)
                                        goto done;
                                }

                                variable.Value = dictionary;

                                EntityOps.SignalDirty(variable, null);

                                result = dictionary;
                            }
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict append dictionaryVariable key ?string ...?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "create":
                    {
                        if ((argumentCount >= 2) &&
                            (((argumentCount - 2) % 2) == 0))
                        {
                            dictionary = new ObjectDictionary(true);

                            for (argumentIndex = 2;
                                    argumentIndex < argumentCount;
                                    argumentIndex += 2)
                            {
                                if (!dictionary.InternalAddOrChange(
                                        interpreter, arguments[argumentIndex],
                                        arguments[argumentIndex + 1], ref result))
                                {
                                    code = ReturnCode.Error;
                                    goto done;
                                }
                            }

                            result = dictionary;
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict create ?key value ...?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "exists":
                    {
                        if (argumentCount >= 4)
                        {
                            dictionary = ObjectDictionary.FromValue(
                                interpreter, arguments[2], true, false, ref result);

                            if (dictionary == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            result = interpreter.BooleanToResult(
                                dictionary.CanTraverse(interpreter, arguments.GetRange(
                                3, argumentCount - 3), true));
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict exists dictionaryValue key ?key ...?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "filter":
                    {
                        if (argumentCount >= 4)
                        {
                            dictionary = ObjectDictionary.FromValue(
                                interpreter, arguments[2], true, false, ref result);

                            if (dictionary == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            object enumValue = EnumOps.TryParse(
                                typeof(DictionaryFilterType), arguments[3],
                                true, true, ref result);

                            if (!(enumValue is DictionaryFilterType))
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            switch ((DictionaryFilterType)enumValue)
                            {
                                case DictionaryFilterType.Key:
                                    {
                                        if (argumentCount >= 4)
                                        {
                                            localDictionary = new ObjectDictionary(true);

                                            for (argumentIndex = 4;
                                                    argumentIndex < argumentCount;
                                                    argumentIndex++)
                                            {
                                                string pattern = arguments[argumentIndex];

                                                foreach (ObjectPair pair in dictionary)
                                                {
                                                    if ((pattern == null) || StringOps.Match(
                                                            interpreter, MatchMode.Glob,
                                                            pair.Key, pattern, false))
                                                    {
                                                        if (!localDictionary.InternalAddOrChange(
                                                                interpreter, pair.Key, pair.Value,
                                                                ref result))
                                                        {
                                                            code = ReturnCode.Error;
                                                            goto done;
                                                        }
                                                    }
                                                }
                                            }

                                            result = localDictionary;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"dict filter dictionaryValue key ?pattern ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case DictionaryFilterType.Value:
                                    {
                                        if (argumentCount >= 4)
                                        {
                                            localDictionary = new ObjectDictionary(true);

                                            for (argumentIndex = 4;
                                                    argumentIndex < argumentCount;
                                                    argumentIndex++)
                                            {
                                                string pattern = arguments[argumentIndex];

                                                foreach (ObjectPair pair in dictionary)
                                                {
                                                    if ((pattern == null) || StringOps.Match(
                                                            interpreter, MatchMode.Glob,
                                                            StringOps.GetStringFromObject(
                                                                pair.Value), pattern, false))
                                                    {
                                                        if (!localDictionary.InternalAddOrChange(
                                                                interpreter, pair.Key, pair.Value,
                                                                ref result))
                                                        {
                                                            code = ReturnCode.Error;
                                                            goto done;
                                                        }
                                                    }
                                                }
                                            }

                                            result = localDictionary;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"dict filter dictionaryValue value ?pattern ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case DictionaryFilterType.Script:
                                    {
                                        if (argumentCount == 6)
                                        {
                                            list = null;

                                            code = ListOps.GetOrCopyOrSplitList(
                                                interpreter, arguments[4], true, ref list,
                                                ref result);

                                            if (code != ReturnCode.Ok)
                                                goto done;

                                            if (list.Count != 2)
                                            {
                                                result = "must have exactly two variable names";
                                                code = ReturnCode.Error;

                                                goto done;
                                            }

                                            keyVarName = list[0];
                                            valueVarName = list[1];

                                            body = arguments[5];
                                            location = arguments[5];

                                            localDictionary = new ObjectDictionary(true);

                                            foreach (ObjectPair pair in dictionary)
                                            {
                                                code = interpreter.SetVariableValue(
                                                    VariableFlags.None, keyVarName,
                                                    pair.Key, null, ref result);

                                                if (code != ReturnCode.Ok)
                                                    break;

                                                code = interpreter.SetVariableValue(
                                                    VariableFlags.None, valueVarName,
                                                    StringOps.GetStringFromObject(pair.Value),
                                                    null, ref result);

                                                if (code != ReturnCode.Ok)
                                                    break;

                                                localResult = null;

                                                code = interpreter.EvaluateScript(
                                                    body, location, ref localResult);

                                                if (code != ReturnCode.Ok)
                                                {
                                                    result = localResult;
                                                    break;
                                                }

                                                boolValue = false;

                                                code = Engine.ToBoolean(
                                                    localResult, interpreter.InternalCultureInfo,
                                                    ref boolValue, ref localResult);

                                                if (code != ReturnCode.Ok)
                                                    break;

                                                if (boolValue)
                                                {
                                                    if (!localDictionary.InternalAddOrChange(
                                                            interpreter, pair.Key, pair.Value,
                                                            ref result))
                                                    {
                                                        code = ReturnCode.Error;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = localDictionary;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"dict filter dictionaryValue script {keyVariable valueVariable} body\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        result = ScriptOps.BadValue(
                                            null, "filter type", enumValue.ToString(),
                                            Enum.GetNames(typeof(DictionaryFilterType)),
                                            null, null);

                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict filter dictionaryValue filterType ...\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "foreach":
                    {
                        if (argumentCount == 5)
                        {
                            list = null;

                            code = ListOps.GetOrCopyOrSplitList(
                                interpreter, arguments[2], true, ref list,
                                ref result);

                            if (code != ReturnCode.Ok)
                                goto done;

                            if (list.Count != 2)
                            {
                                result = "must have exactly two variable names";
                                code = ReturnCode.Error;

                                goto done;
                            }

                            dictionary = ObjectDictionary.FromValue(
                                interpreter, arguments[3], true, false, ref result);

                            if (dictionary == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            keyVarName = list[0];
                            valueVarName = list[1];

                            body = arguments[4];
                            location = arguments[4];

                            list = new StringList(dictionary.Keys);
                            list.Sort(); /* O(N log N) */

                            foreach (string element in list)
                            {
                                if (element == null)
                                    continue;

                                code = interpreter.SetVariableValue(
                                    VariableFlags.None, keyVarName,
                                    element, null, ref result);

                                if (code != ReturnCode.Ok)
                                {
                                    Engine.AddErrorInformation(
                                        interpreter, result, String.Format(
                                            "{0}    (setting dict foreach loop variable \"{1}\")",
                                            Environment.NewLine,
                                            FormatOps.Ellipsis(keyVarName)));

                                    break;
                                }

                                value = dictionary[element];

                                code = interpreter.SetVariableValue(
                                    VariableFlags.None, valueVarName,
                                    StringOps.GetStringFromObject(value),
                                    null, ref result);

                                if (code != ReturnCode.Ok)
                                {
                                    Engine.AddErrorInformation(
                                        interpreter, result, String.Format(
                                            "{0}    (setting dict foreach loop variable \"{1}\")",
                                            Environment.NewLine,
                                            FormatOps.Ellipsis(valueVarName)));

                                    break;
                                }

                                localResult = null;

                                code = interpreter.EvaluateScript(
                                    body, location, ref localResult);

                                if (code == ReturnCode.Ok)
                                {
                                    if (interpreter.ExitNoThrow)
                                        break;
                                }
                                else if (code == ReturnCode.Continue)
                                {
                                    code = ReturnCode.Ok;
                                }
                                else if (code == ReturnCode.Break)
                                {
                                    code = ReturnCode.Ok;
                                    break;
                                }
                                else if (code == ReturnCode.Error)
                                {
                                    Engine.AddErrorInformation(
                                        interpreter, localResult, String.Format(
                                            "{0}    (\"dict foreach\" body line {1})",
                                            Environment.NewLine,
                                            Interpreter.GetErrorLine(interpreter)));

                                    result = localResult;
                                    break;
                                }
                                else
                                {
                                    result = localResult;
                                    break;
                                }
                            }

                            if (code == ReturnCode.Ok)
                                Engine.ResetResult(interpreter, ref result);
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict foreach {keyVar valueVar} dictionaryValue body\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "get":
                    {
                        if (argumentCount >= 3)
                        {
                            dictionary = ObjectDictionary.FromValue(
                                interpreter, arguments[2], true, false, ref result);

                            if (dictionary == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            if (argumentCount >= 4)
                            {
                                value = null;

                                if (!dictionary.TryTraverse(
                                        interpreter, arguments.GetRange(
                                            3, argumentCount - 3),
                                        true, ref value, ref result))
                                {
                                    code = ReturnCode.Error;
                                    goto done;
                                }

                                result = StringOps.GetStringFromObject(value);
                            }
                            else
                            {
                                result = dictionary.KeysAndValuesToString(null, false);
                            }
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict get dictionaryValue ?key ...?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "incr":
                    {
                        if ((argumentCount == 4) || (argumentCount == 5))
                        {
                            long increment = 1;

                            if (argumentCount == 5)
                            {
                                code = Value.GetWideInteger2(
                                    (IGetValue)arguments[4], ValueFlags.AnyWideInteger,
                                    interpreter.InternalCultureInfo, ref increment,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;
                            }

                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                variableFlags = VariableFlags.ArrayCommandMask;
                                variableName = arguments[2];
                                variable = null;
                                dictionary = null;

                                code = interpreter.GetDictionaryVariableViaResolversWithSplit(
                                    variableName, ref variableFlags, ref variable, ref dictionary,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;

                                keyName = arguments[3];

                                if (keyName == null)
                                {
                                    result = "invalid dictionary key name";
                                    code = ReturnCode.Error;

                                    goto done;
                                }

                                longValue = 0;

                                if (dictionary.TryGetValue(keyName, out value))
                                {
                                    code = Value.GetWideInteger2(
                                        StringOps.GetStringFromObject(value),
                                        ValueFlags.AnyWideInteger,
                                        interpreter.InternalCultureInfo,
                                        ref longValue, ref result);

                                    if (code != ReturnCode.Ok)
                                        goto done;
                                }

                                longValue += increment;

                                if (!dictionary.InternalAddOrChange(
                                        interpreter, keyName, longValue, ref result))
                                {
                                    code = ReturnCode.Error;
                                    goto done;
                                }

                                if (!FlagOps.HasFlags(
                                        variable.Flags, VariableFlags.NonTrace, false))
                                {
                                    code = interpreter.FireTraces(
                                        BreakpointType.BeforeVariableSet, variableFlags,
                                        null, variableName, null, dictionary, null,
                                        null, variable, ref result);

                                    if (code != ReturnCode.Ok)
                                        goto done;
                                }

                                variable.Value = dictionary;

                                EntityOps.SignalDirty(variable, null);

                                result = longValue;
                            }
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict incr dictionaryVariable key ?increment?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "info":
                    {
                        if (argumentCount == 3)
                        {
                            dictionary = ObjectDictionary.FromValue(
                                interpreter, arguments[2], true, false, ref result);

                            if (dictionary == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            result = String.Format(
                                "{0} root entries in table, {1} nested entries " +
                                "in table, hash code 0x{2:X}", dictionary.Count,
                                dictionary.TraverseAndCount(
                                    interpreter, null, false, false, true, true),
                                dictionary.GetHashCode());
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict info dictionaryValue\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "keys":
                    {
                        if ((argumentCount >= 3) && (argumentCount <= 4))
                        {
                            dictionary = ObjectDictionary.FromValue(
                                interpreter, arguments[2], true, false, ref result);

                            if (dictionary == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            string pattern = null;

                            if (argumentCount == 4)
                                pattern = arguments[3];

                            result = dictionary.KeysToString(pattern, false);
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict keys dictionaryValue ?pattern?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "lappend":
                    {
                        if (argumentCount >= 4)
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                variableFlags = VariableFlags.ArrayCommandMask;
                                variableName = arguments[2];
                                variable = null;
                                dictionary = null;

                                code = interpreter.GetDictionaryVariableViaResolversWithSplit(
                                    variableName, ref variableFlags, ref variable, ref dictionary,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;

                                keyName = arguments[3];

                                if (keyName == null)
                                {
                                    result = "invalid dictionary key name";
                                    code = ReturnCode.Error;

                                    goto done;
                                }

                                if (dictionary.TryGetValue(keyName, out value))
                                {
                                    list = null;

                                    code = ParserOps<string>.SplitList(
                                        interpreter, StringOps.GetStringFromObject(
                                        value), 0, Length.Invalid, true, ref list,
                                        ref result);

                                    if (code != ReturnCode.Ok)
                                        goto done;
                                }
                                else
                                {
                                    list = new StringList();
                                }

                                for (argumentIndex = 4;
                                        argumentIndex < argumentCount;
                                        argumentIndex++)
                                {
                                    list.Add(arguments[argumentIndex]);
                                }

                                if (!dictionary.InternalAddOrChange(
                                        interpreter, keyName, list, ref result))
                                {
                                    code = ReturnCode.Error;
                                    goto done;
                                }

                                if (!FlagOps.HasFlags(
                                        variable.Flags, VariableFlags.NonTrace, false))
                                {
                                    code = interpreter.FireTraces(
                                        BreakpointType.BeforeVariableSet, variableFlags,
                                        null, variableName, null, dictionary, null,
                                        null, variable, ref result);

                                    if (code != ReturnCode.Ok)
                                        goto done;
                                }

                                variable.Value = dictionary;

                                EntityOps.SignalDirty(variable, null);

                                result = dictionary;
                            }
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict lappend dictionaryVariable key ?value ...?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "map":
                    {
                        if (argumentCount == 5)
                        {
                            list = null;

                            code = ListOps.GetOrCopyOrSplitList(
                                interpreter, arguments[2], true, ref list,
                                ref result);

                            if (code != ReturnCode.Ok)
                                goto done;

                            if (list.Count != 2)
                            {
                                result = "must have exactly two variable names";
                                code = ReturnCode.Error;

                                goto done;
                            }

                            dictionary = ObjectDictionary.FromValue(
                                interpreter, arguments[3], true, false, ref result);

                            if (dictionary == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            keyVarName = list[0];
                            valueVarName = list[1];

                            body = arguments[4];
                            location = arguments[4];

                            localDictionary = new ObjectDictionary(true);

                            foreach (ObjectPair pair in dictionary)
                            {
                                code = interpreter.SetVariableValue(
                                    VariableFlags.None, keyVarName,
                                    pair.Key, null, ref result);

                                if (code != ReturnCode.Ok)
                                {
                                    Engine.AddErrorInformation(
                                        interpreter, result, String.Format(
                                            "{0}    (setting dict map loop variable \"{1}\")",
                                            Environment.NewLine,
                                            FormatOps.Ellipsis(keyVarName)));

                                    break;
                                }

                                code = interpreter.SetVariableValue(
                                    VariableFlags.None, valueVarName,
                                    StringOps.GetStringFromObject(pair.Value),
                                    null, ref result);

                                if (code != ReturnCode.Ok)
                                {
                                    Engine.AddErrorInformation(
                                        interpreter, result, String.Format(
                                            "{0}    (setting dict map loop variable \"{1}\")",
                                            Environment.NewLine,
                                            FormatOps.Ellipsis(valueVarName)));

                                    break;
                                }

                                localResult = null;

                                code = interpreter.EvaluateScript(
                                    body, location, ref localResult);

                                if (code == ReturnCode.Ok)
                                {
                                    if (interpreter.ExitNoThrow)
                                        break;

                                    if (!String.IsNullOrEmpty(localResult))
                                    {
                                        if (!localDictionary.InternalAddOrChange(
                                                interpreter, pair.Key, localResult,
                                                ref result))
                                        {
                                            code = ReturnCode.Error;
                                            break;
                                        }
                                    }
                                }
                                else if (code == ReturnCode.Continue)
                                {
                                    code = ReturnCode.Ok;
                                }
                                else if (code == ReturnCode.Break)
                                {
                                    code = ReturnCode.Ok;
                                    break;
                                }
                                else if (code == ReturnCode.Error)
                                {
                                    Engine.AddErrorInformation(
                                        interpreter, localResult, String.Format(
                                            "{0}    (\"dict map\" body line {1})",
                                            Environment.NewLine,
                                            Interpreter.GetErrorLine(interpreter)));

                                    result = localResult;
                                    break;
                                }
                                else
                                {
                                    result = localResult;
                                    break;
                                }
                            }

                            if (code == ReturnCode.Ok)
                                result = localDictionary;
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict map {keyVar valueVar} dictionaryValue body\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "merge":
                    {
                        if (argumentCount >= 2)
                        {
                            dictionary = new ObjectDictionary(true);

                            for (argumentIndex = 2;
                                    argumentIndex < argumentCount;
                                    argumentIndex++)
                            {
                                otherDictionary = ObjectDictionary.FromValue(
                                    interpreter, arguments[argumentIndex], true,
                                    false, ref result);

                                if (otherDictionary == null)
                                {
                                    code = ReturnCode.Error;
                                    goto done;
                                }

                                foreach (ObjectPair pair in otherDictionary)
                                {
                                    if (!dictionary.InternalAddOrChange(
                                            interpreter, pair.Key, pair.Value,
                                            ref result))
                                    {
                                        code = ReturnCode.Error;
                                        goto done;
                                    }
                                }
                            }

                            result = dictionary;
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict merge ?dictionaryValue ...?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "remove":
                    {
                        if (argumentCount >= 3)
                        {
                            dictionary = ObjectDictionary.FromValue(
                                interpreter, arguments[2], true, false, ref result);

                            if (dictionary == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            dictionary = new ObjectDictionary(
                                (IDictionary<string, object>)dictionary, true);

                            for (argumentIndex = 3;
                                    argumentIndex < argumentCount;
                                    argumentIndex++)
                            {
                                /* IGNORED */
                                dictionary.InternalRemove(
                                    interpreter, arguments[argumentIndex]);
                            }

                            result = dictionary;
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict remove dictionaryValue ?key ...?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "replace":
                    {
                        if ((argumentCount >= 3) &&
                            (((argumentCount - 3) % 2) == 0))
                        {
                            dictionary = ObjectDictionary.FromValue(
                                interpreter, arguments[2], true, false, ref result);

                            if (dictionary == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            dictionary = new ObjectDictionary(
                                (IDictionary<string, object>)dictionary, true);

                            for (argumentIndex = 3;
                                    argumentIndex < argumentCount;
                                    argumentIndex += 2)
                            {
                                if (!dictionary.InternalAddOrChange(
                                        interpreter, arguments[argumentIndex],
                                        arguments[argumentIndex + 1], ref result))
                                {
                                    code = ReturnCode.Error;
                                    goto done;
                                }
                            }

                            result = dictionary;
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict replace dictionaryValue ?key value ...?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "set":
                    {
                        if (argumentCount >= 5)
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                variableFlags = VariableFlags.ArrayCommandMask;
                                variableName = arguments[2];
                                variable = null;
                                dictionary = null;

                                code = interpreter.GetDictionaryVariableViaResolversWithSplit(
                                    variableName, ref variableFlags, ref variable, ref dictionary,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;

                                if (argumentCount > 5)
                                {
                                    changeCount = 0;
                                    stopOnNotFound = false;

                                    localDictionary = dictionary.TraverseAndCreate(
                                        interpreter,
                                        arguments.GetRange(3, argumentCount - 5), 0,
                                        Index.Invalid, true, ref changeCount,
                                        ref stopOnNotFound, ref result);

                                    if (localDictionary == null)
                                    {
                                        code = ReturnCode.Error;
                                        goto done;
                                    }
                                }
                                else
                                {
                                    localDictionary = dictionary;
                                }

                                if (!localDictionary.InternalAddOrChange(
                                        interpreter, arguments[argumentCount - 2],
                                        arguments[argumentCount - 1], ref result))
                                {
                                    code = ReturnCode.Error;
                                    goto done;
                                }

                                if (!FlagOps.HasFlags(
                                        variable.Flags, VariableFlags.NonTrace, false))
                                {
                                    code = interpreter.FireTraces(
                                        BreakpointType.BeforeVariableSet, variableFlags,
                                        null, variableName, null, dictionary, null,
                                        null, variable, ref result);

                                    if (code != ReturnCode.Ok)
                                        goto done;
                                }

                                variable.Value = dictionary;

                                EntityOps.SignalDirty(variable, null);

                                result = dictionary;
                            }
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict set dictionaryVariable key ?key ...? value\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "size":
                    {
                        if (argumentCount == 3)
                        {
                            dictionary = ObjectDictionary.FromValue(
                                interpreter, arguments[2], true, false, ref result);

                            if (dictionary == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            result = dictionary.Count;
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict size dictionaryValue\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "unset":
                    {
                        if (argumentCount >= 4)
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                variableFlags = VariableFlags.ArrayCommandMask;
                                variableName = arguments[2];
                                variable = null;
                                dictionary = null;

                                code = interpreter.GetDictionaryVariableViaResolversWithSplit(
                                    variableName, ref variableFlags, ref variable, ref dictionary,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;

                                changeCount = 0;

                                if (argumentCount > 4)
                                {
                                    stopOnNotFound = true;

                                    localDictionary = dictionary.TraverseAndCreate(
                                        interpreter,
                                        arguments.GetRange(3, argumentCount - 4), 0,
                                        Index.Invalid, true, ref changeCount,
                                        ref stopOnNotFound, ref result);

                                    if (localDictionary == null)
                                    {
                                        if (stopOnNotFound)
                                        {
                                            code = ReturnCode.Error;
                                            goto done;
                                        }
                                        else
                                        {
                                            //
                                            // NOTE: Per Tcl, [dict unset] does not
                                            //       raise a script error when any
                                            //       intermediate keys do not exist.
                                            //
                                            goto skipUnset;
                                        }
                                    }
                                }
                                else
                                {
                                    localDictionary = dictionary;
                                }

                                if (localDictionary.InternalRemove(
                                        interpreter, arguments[argumentCount - 1]))
                                {
                                    changeCount++;
                                }

                            skipUnset:

                                //
                                // HACK: This is a bit sub-optimal as we may fire these
                                //       variable traces even when a dictionary appears
                                //       to be unchanged.  This is necessary because an
                                //       the internal variable value may have just been
                                //       mutated (i.e. to create nested dictionaries).
                                //
                                if (changeCount > 0)
                                {
                                    if (!FlagOps.HasFlags(
                                            variable.Flags, VariableFlags.NonTrace, false))
                                    {
                                        code = interpreter.FireTraces(
                                            BreakpointType.BeforeVariableSet, variableFlags,
                                            null, variableName, null, dictionary, null,
                                            null, variable, ref result);

                                        if (code != ReturnCode.Ok)
                                            goto done;
                                    }

                                    variable.Value = dictionary;

                                    EntityOps.SignalDirty(variable, null);
                                }

                                result = dictionary;
                            }
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict unset dictionaryVariable key ?key ...?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "update":
                    {
                        if ((argumentCount >= 6) &&
                            (((argumentCount - 4) % 2) == 0))
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                variableFlags = VariableFlags.ArrayCommandMask;
                                variableName = arguments[2];
                                variable = null;
                                dictionary = null;

                                code = interpreter.GetDictionaryVariableViaResolversWithSplit(
                                    variableName, ref variableFlags, ref variable, ref dictionary,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;

                                for (argumentIndex = 3;
                                        argumentIndex < argumentCount - 1;
                                        argumentIndex += 2)
                                {
                                    keyName = arguments[argumentIndex];

                                    if (keyName == null)
                                    {
                                        result = "invalid dictionary key name";
                                        code = ReturnCode.Error;

                                        goto done;
                                    }

                                    if (dictionary.TryGetValue(keyName, out value))
                                    {
                                        code = interpreter.SetVariableValue(
                                            VariableFlags.None,
                                            arguments[argumentIndex + 1],
                                            StringOps.GetStringFromObject(value),
                                            null, ref result);
                                    }
                                    else
                                    {
                                        code = interpreter.UnsetVariable(
                                            VariableFlags.NoComplain,
                                            arguments[argumentIndex + 1],
                                            ref result);
                                    }

                                    if (code != ReturnCode.Ok)
                                        goto done;
                                }
                            }

                            body = arguments[argumentCount - 1];
                            location = arguments[argumentCount - 1];
                            localResult = null;

                            code = interpreter.EvaluateScript(
                                body, location, ref localResult);

                            if (code == ReturnCode.Ok)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    variableFlags = VariableFlags.ArrayCommandMask;
                                    variableName = arguments[2];
                                    variable = null;
                                    dictionary = null;

                                    code = interpreter.GetDictionaryVariableViaResolversWithSplit(
                                        variableName, ref variableFlags, ref variable, ref dictionary,
                                        ref result);

                                    if (code != ReturnCode.Ok)
                                        goto done;

                                    for (argumentIndex = 3;
                                            argumentIndex < (argumentCount - 1);
                                            argumentIndex += 2)
                                    {
                                        keyName = arguments[argumentIndex];
                                        localResult = null;

                                        if (interpreter.GetVariableValue(
                                                VariableFlags.None,
                                                arguments[argumentIndex + 1],
                                                ref localResult) == ReturnCode.Ok)
                                        {
                                            if (!dictionary.InternalAddOrChange(
                                                    interpreter, keyName, localResult,
                                                    ref result))
                                            {
                                                code = ReturnCode.Error;
                                                goto done;
                                            }
                                        }
                                        else
                                        {
                                            /* IGNORED */
                                            dictionary.InternalRemove(
                                                interpreter, keyName);
                                        }
                                    }

                                    if (!FlagOps.HasFlags(
                                            variable.Flags, VariableFlags.NonTrace, false))
                                    {
                                        code = interpreter.FireTraces(
                                            BreakpointType.BeforeVariableSet, variableFlags,
                                            null, variableName, null, dictionary, null,
                                            null, variable, ref result);

                                        if (code != ReturnCode.Ok)
                                            goto done;
                                    }

                                    variable.Value = dictionary;

                                    EntityOps.SignalDirty(variable, null);
                                }

                                Engine.ResetResult(interpreter, ref result);
                            }
                            else if (code == ReturnCode.Error)
                            {
                                Engine.AddErrorInformation(
                                    interpreter, localResult, String.Format(
                                        "{0}    (\"dict update\" body line {1})",
                                        Environment.NewLine,
                                        Interpreter.GetErrorLine(interpreter)));

                                result = localResult;
                            }
                            else
                            {
                                result = localResult;
                            }
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict update dictionaryVariable key varName ?key varName ...? body\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "values":
                    {
                        if ((argumentCount >= 3) && (argumentCount <= 4))
                        {
                            dictionary = ObjectDictionary.FromValue(
                                interpreter, arguments[2], true, false, ref result);

                            if (dictionary == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            string pattern = null;

                            if (argumentCount == 4)
                                pattern = arguments[3];

                            result = dictionary.ValuesToString(pattern, false);
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict values dictionaryValue ?pattern?\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "with":
                    {
                        if (argumentCount >= 4)
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                variableFlags = VariableFlags.ArrayCommandMask;
                                variableName = arguments[2];
                                variable = null;
                                dictionary = null;

                                code = interpreter.GetDictionaryVariableViaResolversWithSplit(
                                    variableName, ref variableFlags, ref variable, ref dictionary,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;

                                if (argumentCount > 4)
                                {
                                    changeCount = 0;
                                    stopOnNotFound = false;

                                    localDictionary = dictionary.TraverseAndCreate(
                                        interpreter,
                                        arguments.GetRange(3, argumentCount - 4), 0,
                                        Index.Invalid, true, ref changeCount,
                                        ref stopOnNotFound, ref result);

                                    if (localDictionary == null)
                                    {
                                        code = ReturnCode.Error;
                                        goto done;
                                    }
                                }
                                else
                                {
                                    localDictionary = dictionary;
                                }

                                keyNames = new StringList(localDictionary.Keys);

                                foreach (string keyName2 in keyNames)
                                {
                                    if (localDictionary.TryGetValue(keyName2, out value))
                                    {
                                        code = interpreter.SetVariableValue(
                                            VariableFlags.None, keyName2,
                                            StringOps.GetStringFromObject(value),
                                            null, ref result);
                                    }
                                    else
                                    {
                                        code = interpreter.SetVariableValue(
                                            VariableFlags.None, keyName2, null,
                                            null, ref result);
                                    }

                                    if (code != ReturnCode.Ok)
                                        goto done;
                                }
                            }

                            body = arguments[argumentCount - 1];
                            location = arguments[argumentCount - 1];
                            localResult = null;

                            code = interpreter.EvaluateScript(
                                body, location, ref localResult);

                            if (code == ReturnCode.Ok)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    variableFlags = VariableFlags.ArrayCommandMask;
                                    variableName = arguments[2];
                                    variable = null;
                                    dictionary = null;

                                    code = interpreter.GetDictionaryVariableViaResolversWithSplit(
                                        variableName, ref variableFlags, ref variable, ref dictionary,
                                        ref result);

                                    if (code != ReturnCode.Ok)
                                        goto done;

                                    if (argumentCount > 4)
                                    {
                                        changeCount = 0;
                                        stopOnNotFound = false;

                                        otherDictionary = dictionary.TraverseAndCreate(
                                            interpreter,
                                            arguments.GetRange(3, argumentCount - 4), 0,
                                            Index.Invalid, true, ref changeCount,
                                            ref stopOnNotFound, ref result);

                                        if (otherDictionary == null)
                                        {
                                            code = ReturnCode.Error;
                                            goto done;
                                        }
                                    }
                                    else
                                    {
                                        otherDictionary = dictionary;
                                    }

                                    keyNames = new StringList(otherDictionary.Keys);

                                    foreach (string keyName2 in keyNames)
                                    {
                                        localResult = null;

                                        if (interpreter.GetVariableValue(
                                                VariableFlags.None, keyName2,
                                                ref localResult) == ReturnCode.Ok)
                                        {
                                            if (!otherDictionary.InternalAddOrChange(
                                                    interpreter, keyName2, localResult,
                                                    ref result))
                                            {
                                                code = ReturnCode.Error;
                                                goto done;
                                            }
                                        }
                                        else
                                        {
                                            /* IGNORED */
                                            otherDictionary.InternalRemove(
                                                interpreter, keyName2);
                                        }
                                    }

                                    if (!FlagOps.HasFlags(
                                            variable.Flags, VariableFlags.NonTrace, false))
                                    {
                                        code = interpreter.FireTraces(
                                            BreakpointType.BeforeVariableSet,
                                            variableFlags, null, variableName,
                                            null, dictionary, null, null,
                                            variable, ref result);

                                        if (code != ReturnCode.Ok)
                                            goto done;
                                    }

                                    variable.Value = dictionary;

                                    EntityOps.SignalDirty(variable, null);
                                }

                                if (code == ReturnCode.Ok)
                                    Engine.ResetResult(interpreter, ref result);
                            }
                            else if (code == ReturnCode.Error)
                            {
                                Engine.AddErrorInformation(
                                    interpreter, localResult, String.Format(
                                        "{0}    (\"dict with\" body line {1})",
                                        Environment.NewLine,
                                        Interpreter.GetErrorLine(interpreter)));

                                result = localResult;
                            }
                            else
                            {
                                result = localResult;
                            }
                        }
                        else
                        {
                            result = "wrong # args: should be \"dict with dictionaryVariable ?key ...? body\"";
                            code = ReturnCode.Error;
                        }
                        break;
                    }
                default:
                    {
                        result = ScriptOps.BadSubCommand(
                            interpreter, null, null, subCommand, this,
                            null, null);

                        code = ReturnCode.Error;
                        break;
                    }
            }

        done:

            return code;
        }
        #endregion
    }
}
