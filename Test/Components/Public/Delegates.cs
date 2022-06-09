/*
 * Delegates.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Components.Public.Delegates
{
    [ObjectId("75d7cd08-be91-41a6-8f47-8c323b4dd5c2")]
    public delegate void SimpleDelegate();

    [ObjectId("bdea0daf-f0e8-43cd-b775-c53972cea292")]
    public delegate void ClearTestItemsDelegate();

    [ObjectId("ae7191eb-e2e0-4522-b0b9-d6e662d7652c")]
    public delegate int AddTestItemDelegate(string item);

    [ObjectId("16510f6b-badc-44cb-8e95-b725d83e7180")]
    public delegate void ClearStatusTextDelegate();

    [ObjectId("f0ff74e7-6aa4-4a03-a243-e5e743de2e36")]
    public delegate void AppendStatusTextDelegate(
        string text, bool newLine);

    [ObjectId("d90b0d10-c066-4a73-897e-488d926452cb")]
    public delegate void SetProgressValueDelegate(int value);

    [ObjectId("1488d074-7afa-4f1e-b747-2b1d8c82b7e2")]
    public delegate void EvaluateScriptDelegate(
        string text, Result synchronizedResult);

    [ObjectId("89b4865d-34c6-42e0-aaf6-cbe84713ed57")]
    public delegate void ScriptCompletedDelegate(
        string text, Result result);

    [ObjectId("55ecf211-a903-4ed8-aa90-2742bd523caa")]
    public delegate void ScriptCanceledDelegate();
}
