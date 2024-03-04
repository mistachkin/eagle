/*
 * SnippetManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("b4a2f0a6-2d23-47be-8bec-9c2d89a82969")]
    public interface ISnippetManager
    {
        ReturnCode HaveSnippet(
            string name,               /* in */
            SnippetFlags snippetFlags, /* in: NOT USED */
            LookupFlags lookupFlags,   /* in */
            ref Result error           /* out */
            );

        ReturnCode GetSnippet(
            string name,               /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref ISnippet snippet,      /* out */
            ref Result error           /* out */
            );

        ReturnCode ListSnippets(
            string pattern,                /* in: OPTIONAL */
            bool noCase,                   /* in */
            SnippetFlags snippetFlags,     /* in */
            LookupFlags lookupFlags,       /* in: NOT USED */
            ref IEnumerable<string> names, /* in, out */
            ref Result error               /* out */
            );

        ReturnCode EvaluateSnippet(
            string name,               /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref Result result          /* out */
            );

        ReturnCode EvaluateSnippet(
            string name,               /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref Result result,         /* out */
            ref int errorLine          /* out */
            );

        ReturnCode ClearSnippets(
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in: NOT USED */
            ref int count,             /* in, out */
            ref Result error           /* out */
            );

        ReturnCode AddSnippet(
            string text,               /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref string name,           /* out */
            ref Result error           /* out */
            );

        ReturnCode RemoveSnippet(
            string name,               /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref Result error           /* out */
            );

        ReturnCode AddSnippets(
            string path,                   /* in */
            string pattern,                /* in */
            SnippetFlags snippetFlags,     /* in */
            LookupFlags lookupFlags,       /* in */
            ref IEnumerable<string> names, /* in, out */
            ref ResultList errors          /* in, out */
            );

        ReturnCode AddSnippetsForCertificates(
            string path,                   /* in */
            SnippetFlags snippetFlags,     /* in */
            LookupFlags lookupFlags,       /* in */
            ref IEnumerable<string> names, /* in, out */
            ref ResultList errors          /* in, out */
            );

        ReturnCode RemoveSnippets(
            SnippetFlags snippetFlags,     /* in */
            LookupFlags lookupFlags,       /* in */
            ref IEnumerable<string> names, /* in, out */
            ref ResultList errors          /* in, out */
            );
    }
}
