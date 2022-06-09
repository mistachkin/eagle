/*
 * License.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !EAGLE
using System.Runtime.InteropServices;
#endif

using Eagle._Attributes;

#if EAGLE
using Eagle._Interfaces.Public;
#endif

namespace Eagle._Components.Shared
{
#if EAGLE
    [ObjectId("5ba25914-28f6-4218-a05c-d9041da0b14d")]
#else
    [Guid("5ba25914-28f6-4218-a05c-d9041da0b14d")]
#endif
    public static class License
    {
        ///////////////////////////////////////////////////////////////////////
        //*WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
        //
        // Please do not modify or remove the license summary or text in this
        // file.  Doing so would be a violation of the license agreement.
        //
        //*WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        public const string Summary =
@"See the file ""license.terms"" for information on usage and redistribution of
this file, and for a DISCLAIMER OF ALL WARRANTIES.";

        ///////////////////////////////////////////////////////////////////////

        public const string Text =
@"This software is copyrighted by Joe Mistachkin and other parties.  The
following terms apply to all files associated with the software unless
explicitly disclaimed in individual files.

The authors hereby grant permission to use, copy, modify, distribute,
and license this software and its documentation for any purpose, provided
that existing copyright notices are retained in all copies and that this
notice is included verbatim in any distributions.  No written agreement,
license, or royalty fee is required for any of the authorized uses.
Modifications to this software may be copyrighted by their authors
and need not follow the licensing terms described here, provided that
the new terms are clearly indicated on the first page of each file where
they apply.

IN NO EVENT SHALL THE AUTHORS OR DISTRIBUTORS BE LIABLE TO ANY PARTY
FOR DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES
ARISING OUT OF THE USE OF THIS SOFTWARE, ITS DOCUMENTATION, OR ANY
DERIVATIVES THEREOF, EVEN IF THE AUTHORS HAVE BEEN ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.

THE AUTHORS AND DISTRIBUTORS SPECIFICALLY DISCLAIM ANY WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT.  THIS SOFTWARE
IS PROVIDED ON AN ""AS IS"" BASIS, AND THE AUTHORS AND DISTRIBUTORS HAVE
NO OBLIGATION TO PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR
MODIFICATIONS.

GOVERNMENT USE: If you are acquiring this software on behalf of the
U.S. government, the Government shall have only ""Restricted Rights""
in the software and related documentation as defined in the Federal
Acquisition Regulations (FARs) in Clause 52.227.19 (c) (2).  If you
are acquiring the software on behalf of the Department of Defense, the
software shall be classified as ""Commercial Computer Software"" and the
Government shall have only ""Restricted Rights"" as defined in Clause
252.227-7013 (c) (1) of DFARs.  Notwithstanding the foregoing, the
authors grant the U.S. Government and others acting in its behalf
permission to use and distribute the software in accordance with the
terms specified in this license.";

        ///////////////////////////////////////////////////////////////////////

        public static readonly byte[] Hash = {
            246, 184,  71, 237, 139,  17, 164,  39,  40, 213, 192,  18, 184,
             45, 225, 226, 191, 205,  54,  46,  90,   9, 100, 243, 169, 147,
            161, 120, 105,  34,  15,  23, 255, 233, 197, 189,  89,  35,   8,
              7, 246, 164, 179, 249,  93,  64, 198, 148, 157, 150, 217, 207,
             53, 211,  53,  89,  23, 215, 143, 121,  21, 199,  83, 156
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
#if EAGLE
        public static bool WriteSummary(
            IInteractiveHost interactiveHost
            )
        {
            return (interactiveHost != null) ?
                interactiveHost.WriteLine(Summary) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool WriteText(
            IInteractiveHost interactiveHost
            )
        {
            return (interactiveHost != null) ?
                interactiveHost.WriteLine(Text) : false;
        }
#endif
        #endregion
    }
}
