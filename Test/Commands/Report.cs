/*
 * Report.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the test-only <c>report</c> command, which drives
    /// the graphical test form by sending it alerts, status text, progress
    /// updates, test results, and scripts to evaluate.
    /// </summary>
    [ObjectId("28546edf-0060-401d-9de4-c38b3ac2a285")]
    [CommandFlags(CommandFlags.Unsafe)]
    [ObjectGroup("managedEnvironment")]
    internal sealed class Report : Default
    {
        /// <summary>
        /// Constructs an instance of the <c>report</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its name
        /// and flags.  This parameter may be null.
        /// </param>
        public Report(
            ICommandData commandData
            )
            : base(commandData)
        {
            this.Flags |= Utility.GetCommandFlags(GetType().BaseType) |
                Utility.GetCommandFlags(this);
        }

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        /// <summary>
        /// The collection of sub-commands supported by this command.
        /// </summary>
        private EnsembleDictionary subCommands =
            new EnsembleDictionary(new string[] {
            "alert", "append", "clear", "progress", "result", "send"
        });

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the collection of sub-commands supported by this
        /// command.
        /// </summary>
        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
            set { subCommands = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>report</c> command, dispatching to the
        /// requested sub-command (such as <c>alert</c>, <c>append</c>,
        /// <c>clear</c>, <c>progress</c>, <c>result</c>, or <c>send</c>) to
        /// drive the graphical test form.
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
        /// command name and element one is the sub-command name.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the sub-command.  Upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
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

            if (arguments.Count < 2)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} option ?arg ...?\"",
                    this.Name);

                return ReturnCode.Error;
            }

            ReturnCode code;
            string subCommand = arguments[1];
            bool tried = false;

            code = Utility.TryExecuteSubCommandFromEnsemble(
                interpreter, this, clientData, arguments, true,
                null, ref subCommand, ref tried, ref result);

            if ((code == ReturnCode.Ok) && !tried)
            {
                switch (subCommand)
                {
                    case "alert":
                        {
                            if (arguments.Count == 3)
                            {
                                result = CommonOps.Complain(arguments[2]);
                                code = ReturnCode.Ok;
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1} string\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "append":
                        {
                            if (arguments.Count == 3)
                            {
                                _Forms.TestForm form = _Shell.Test.mainForm;

                                if (form != null)
                                {
                                    /* NO RESULT */
                                    form.AsyncAppendStatusText(arguments[2],
                                        true);

                                    result = String.Empty;
                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    result = "invalid main form";
                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1} string\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "clear":
                        {
                            if (arguments.Count == 2)
                            {
                                _Forms.TestForm form = _Shell.Test.mainForm;

                                if (form != null)
                                {
                                    /* NO RESULT */
                                    form.AsyncClearTestItems();

                                    /* NO RESULT */
                                    form.AsyncClearStatusText();

                                    result = String.Empty;
                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    result = "invalid main form";
                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1}\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "progress":
                        {
                            if (arguments.Count == 3)
                            {
                                int value = 0;

                                code = Value.GetInteger2(
                                    (IGetValue)arguments[2], ValueFlags.AnyInteger,
                                    interpreter.CultureInfo, ref value, ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    _Forms.TestForm form = _Shell.Test.mainForm;

                                    if (form != null)
                                    {
                                        /* NO RESULT */
                                        form.AsyncSetProgressValue(value);

                                        result = String.Empty;
                                        code = ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        result = "invalid main form";
                                        code = ReturnCode.Error;
                                    }
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1} value\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "result":
                        {
                            if (arguments.Count == 4)
                            {
                                object enumValue = Utility.TryParseEnum(
                                    typeof(TestResult), arguments[3],
                                    true, true);

                                if (enumValue is TestResult)
                                {
                                    TestResult testResult = (TestResult)enumValue;
                                    _Forms.TestForm form = _Shell.Test.mainForm;

                                    if (form != null)
                                    {
                                        /* NO RESULT */
                                        form.AsyncAddTestItem(String.Format(
                                            "{0}{1}{2}", arguments[2],
                                            Characters.HorizontalTab,
                                            testResult.ToString().ToUpper()));

                                        result = String.Empty;
                                        code = ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        result = "invalid main form";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    result = Utility.BadValue(
                                        null, "test result", arguments[3],
                                        Enum.GetNames(typeof(TestResult)),
                                        null, null);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1} name result\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "send":
                        {
                            if (arguments.Count >= 3)
                            {
                                OptionDictionary options = new OptionDictionary(
                                    new IOption[] {
                                    new Option(null, OptionFlags.None, 1,
                                        Index.Invalid, "-asynchronous", null),
                                    new Option(null,
                                        OptionFlags.MustHaveIntegerValue,
                                        1, Index.Invalid, "-timeout", null),
                                    Option.CreateEndOfOptions()
                                });

                                int argumentIndex = Index.Invalid;

                                code = interpreter.GetOptions(
                                    options, arguments, 0, 2, Index.Invalid,
                                    true, ref argumentIndex, ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    if ((argumentIndex != Index.Invalid) &&
                                        ((argumentIndex + 1) == arguments.Count))
                                    {
                                        bool asynchronous = false;

                                        if (options.IsPresent("-asynchronous"))
                                            asynchronous = true;

                                        IVariant value = null;
                                        int timeout = _Timeout.Infinite;

                                        if (options.IsPresent("-timeout", ref value))
                                            timeout = (int)value.Value;

                                        _Forms.TestForm form = _Shell.Test.mainForm;

                                        if (form != null)
                                        {
                                            if (asynchronous)
                                            {
                                                /* NO RESULT */
                                                form.AsyncEvaluateScript(
                                                    arguments[argumentIndex], null);
                                            }
                                            else
                                            {
                                                result = Utility.CreateSynchronizedResult(null);

                                                /* NO RESULT */
                                                form.AsyncEvaluateScript(
                                                    arguments[argumentIndex], result);

                                                if (Utility.WaitSynchronizedResult(result, timeout))
                                                {
                                                    if (Utility.GetSynchronizedResult(result, ref code,
                                                            ref result, ref result) != ReturnCode.Ok)
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "timeout, {0} milliseconds",
                                                        timeout);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "invalid main form";
                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        if ((argumentIndex != Index.Invalid) &&
                                            Option.LooksLikeOption(arguments[argumentIndex]))
                                        {
                                            result = OptionDictionary.BadOption(
                                                options, arguments[argumentIndex],
                                                !interpreter.IsSafe());
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?options? script\"",
                                                this.Name, subCommand);
                                        }

                                        code = ReturnCode.Error;
                                    }
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1} ?options? script\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    default:
                        {
                            result = Utility.BadSubCommand(
                                interpreter, null, null, subCommand, this, null, null);

                            code = ReturnCode.Error;
                            break;
                        }
                }
            }

            return code;
        }
        #endregion
    }
}
