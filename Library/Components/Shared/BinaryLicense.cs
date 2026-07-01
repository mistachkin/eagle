/*
 * BinaryLicense.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if EAGLE
using Eagle._Attributes;
#else
using System.Runtime.InteropServices;
#endif

namespace Eagle._Components.Shared
{
    /// <summary>
    /// This class holds the license summary and full license text that govern
    /// the use and redistribution of binary (compiled) releases of the Eagle
    /// software.
    /// </summary>
#if EAGLE
    [ObjectId("53bd1989-ac87-4dd9-ab44-82df59f380e5")]
#else
    [Guid("53bd1989-ac87-4dd9-ab44-82df59f380e5")]
#endif
    public static class BinaryLicense
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
        /// <summary>
        /// The short summary of the binary license, directing the reader to the
        /// full license terms.
        /// </summary>
        public const string Summary =
@"See the file ""license.terms"" for information on usage and redistribution of
this file, and for a DISCLAIMER OF ALL WARRANTIES.";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The complete text of the binary license agreement.
        /// </summary>
        public const string Text =
@"0. IF YOU DO NOT AGREE TO THE TERMS OF THIS AGREEMENT, PLEASE INDICATE SO
BY CLICKING THE 'Decline' OR 'No' BUTTON AND DO NOT USE THE SOFTWARE.
PLEASE REVIEW THIS AGREEMENT BEFORE EVALUATING OR PURCHASING THE SOFTWARE.
NO REFUNDS WILL BE ISSUED.

1. You, as the User, assume full responsibility for the selection of the
software and its associated data and/or information to achieve his/her
intended results, and for the installation, use and results obtained from
the software.

2. THIS DOCUMENT STATES THE TERMS AND CONDITIONS UPON WHICH MISTACHKIN
SYSTEMS OFFERS TO LICENSE THE SOFTWARE ENCLOSED AND ITS ASSOCIATED DATA
AND/OR INFORMATION. BY USING THIS SOFTWARE OR OPENING THIS ENVELOPE, YOU
ARE AGREEING TO BECOME BOUND BY THE TERMS AND CONDITIONS OF THIS
AGREEMENT. THE SOFTWARE ENCLOSED AND ITS ASSOCIATED DATA AND/OR
INFORMATION AS SUPPLIED BY MISTACHKIN SYSTEMS IS LICENSED, NOT SOLD, TO
YOU FOR USE ONLY UNDER THE TERMS OF THIS LICENSE AND MISTACHKIN SYSTEMS
RESERVES ALL RIGHTS NOT EXPRESSLY GRANTED TO YOU UNDER THIS AGREEMENT.

3. License: The User acknowledges that the software and documents
contained in the package and underlying ideas, algorithms, concepts,
procedures, processes, principals and methods of operation are
confidential and contain trade secrets, and User shall use its best
efforts to maintain the confidentialities thereof.

4. User acknowledges that all intellectual property rights and the
software and documents are owned by Mistachkin Systems. Without limiting
the generality of the preceding sentence, the software, documents, and
user manuals are copyrighted and may not be copied except as specifically
allowed by this license for backup purposes and to load the software on to
the computer as part of executing the software. All other copies of the
software and related user manuals are in violation of this agreement. The
copyright protection claim also includes all forms and matters of
copyrightable materials and information not allowed by statutory or common
law or hereinafter granted including, without limitation, material
generated from the software that are displayed on the screen such as icons
and screen displays. The software and documents contain trade secrets and
are subject to the protection of the Oregon Uniform Trade Secrets Act.

5. The User acknowledges that this licensing agreement unless modified by
Mistachkin Systems also applies to subsequent purchases of software and
data, and any updated versions of the software and materials including any
upgrades and enhancements related thereto. Any receipt of an updated
version and the use thereof is subject to this licensing agreement's
application thereto.

6. User may not sublease, assign or otherwise transfer the software and
documents to any other person or business entity without the prior written
consent of Joseph Mistachkin.

7. This software and its associated data and/or information may contain
cryptographic material. The User acknowledges the export of cryptographic
material from the United States is regulated under ""EI controls"" of the
Export Administration Regulations (EAR, 15 CFR 730-744) of the United
States Commerce Department, Bureau of Export Administration (BXA). An
export license or applicable license exception is required to export
cryptographic software outside the United States or Canada.

8. The User agrees not to directly or indirectly export or re-export any
cryptographic material (or portions thereof) that may be contained in the
software and its associated data and/or information to any country, other
than Canada, or to any person or entity subject to United States export
restrictions without first obtaining a Commerce Department export license
or determining that there is an applicable license exception. You warrant
and represent that neither the BXA nor any other United States federal
agency has suspended, revoked, or denied your export privileges.

9. This license allows you to: (a) Use the software and its associated
data and/or information for your organization only on a single computer,
for which it was designed; (b) Make copies of the software and its
associated data and/or information in machine readable form, solely for
backup purposes; and (c) Physically transfer the software from one
computer to another provided that the software is used on only one
computer at a time.

10. If you have purchased a multiple user version of this software, this
license agreement allows use of the software and its associated data
and/or information for your organization up to the allowed number of seats
inclusive. It may not be copied onto more computers than you have seats
for. If you have a trial version of this software, you are authorized to
use the software and its associated data and/or information on a single
computer for not more than 30 days from the first date of install.

11. Mistachkin Systems may adopt from time to time mechanical or
electronic methods that Mistachkin Systems deems necessary to protect you
and to control unauthorized use or distribution of the software. By using
the software, you consent to such methods of monitoring and reporting.

12. Restrictions: (a) You may not market, distribute or transfer copies of
the software and its associated data and/or information to others outside
your organization without the permission of Mistachkin Systems. The
software and its associated data and/or information contain intellectual
property and in order to protect them you may not decompile, reverse
compile, reverse engineer, reverse translate, disassemble or otherwise
reduce the software or its associated data and/or information to a human
readable form or distribute them to any third party. YOU MAY NOT MODIFY,
ADAPT, TRANSLATE, RENT, SELL, GIVE, LEASE OR LOAN THE SOFTWARE OR ITS
ASSOCIATED DATA AND/OR INFORMATION OR CREATE DERIVATIVE WORKS BASED ON THE
SOFTWARE, ITS ASSOCIATED DATA AND/OR INFORMATION OR THE ACCOMPANYING
WRITTEN MATERIALS EXCEPT AS SPECIFIED IN SECTION 13 OF THIS LICENSE. (b)
The software, its associated data and/or information and accompanying
written materials are copyrighted. Unauthorized copying is expressly
forbidden. You may be held legally responsible for any copyright
infringement that is caused or encouraged by your failure to abide by the
terms of this license. (c) You understand that Mistachkin Systems may
upgrade, enhance, or revise the software and in doing so incurs no
obligation to furnish such upgrades to you. These changes may render the
software obsolete. Consequently, Mistachkin Systems reserves the right to
terminate this agreement as to such prior versions of the software.

13. Derivative Works: You may create, market, and distribute derivative
works based on provided files/components that are designated as
""redistributable"". No royalties or further license is required for such
use. The only additional licensing requirement for using the
""redistributable"" files/components in your applications is that you place
the following notice on your ""about"" screen and/or somewhere conspicuous
in your documentation:

    ""Eagle Enterprise Runtime Copyright (c) 2007-2012 by Joseph
Mistachkin. Used with permission.""

The following files/components are hereby designated as ""redistributable""
(the included binary executables and images may NOT be modified in any way
unless they are recompiled from their corresponding source code and
prominently marked as ""unofficial""):

    Eagle Library Runtime Files

        ""Eagle.dll""
        ""Eagle.Eye.dll""

    Badge Plugin Runtime Files

        ""Badge.dll""
        ""Badge.Basic.dll""
        ""pkgIndex.eagle""
        ""pkgIndex.eagle.harpy""

    Demo Plugin Runtime Files

        ""Demo.dll""
        ""pkgIndex.eagle""
        ""pkgIndex.eagle.harpy""

    Featherlight Plugin Runtime Files

        ""Featherlight.exe""
        ""pkgIndex.eagle""
        ""pkgIndex.eagle.harpy""

    Harpy Plugin Runtime Files

        ""Harpy.dll""
        ""Harpy.Basic.dll""
        ""Harpy.Limited.dll""
        ""Harpy.Sdk.dll""
        ""keyRing.General.demo.eagle""
        ""keyRing.General.demo.eagle.harpy""
        ""keyRing.one.eagle""
        ""keyRing.one.eagle.harpy""
        ""keyRing.zero.eagle""
        ""keyRing.zero.eagle.harpy""
        ""pkgIndex.eagle""
        ""pkgIndex.eagle.harpy""
        ""test.eagle""
        ""test.eagle.harpy""

    HotKey Plugin Runtime Files

        ""HotKey.dll""
        ""Interop.wshom.dll""
        ""complaintForm.eagle""
        ""complaintForm.eagle.harpy""
        ""pkgIndex.eagle""
        ""pkgIndex.eagle.harpy""
        ""hotkey-template-*.eagle""
        ""hotkey-template-*.eagle.harpy""

    Zeus Plugin Runtime Files

        ""Zeus.dll""
        ""pkgIndex.eagle""
        ""pkgIndex.eagle.harpy""

The following files/components originated from ""third-parties"" and may
contain customizations; however, their respective licenses still apply:

    HotKey Plugin Third-Party Files

        ""SciLexer*.*"" -- https://www.scintilla.org/
        ""ScintillaNET.*"" -- https://github.com/jacobslusser/ScintillaNET

14. No distributor, dealer, salesperson, employee or agent of Mistachkin
Systems or any other entity or person is authorized to expand or alter
this warranty or this agreement without the prior written consent of
Joseph Mistachkin.

15. Termination: This license is effective until terminated. Except for
sections 16, 17, 18, 19, and 20 this license shall terminate automatically
upon the earlier of: (a) Breach of your obligations under the license; (b)
Mistachkin Systems written termination of this license; or (c) By your
nonpayment of any outstanding monies owed to Mistachkin Systems. Upon
termination you agree that you will immediately discontinue the use of and
destroy all copies of the software and its associated data and/or
information and return to Mistachkin Systems any and all pieces of
electronic media supplied by Mistachkin Systems.

16. No Warranty: IN NO EVENT SHALL THE AUTHORS OR DISTRIBUTORS BE LIABLE
TO ANY PARTY FOR DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL
DAMAGES ARISING OUT OF THE USE OF THIS SOFTWARE, ITS DOCUMENTATION, OR ANY
DERIVATIVES THEREOF, EVEN IF THE AUTHORS HAVE BEEN ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE. SOME JURISDICTIONS DO NOT ALLOW THE LIMITATION
OF IMPLIED WARRANTIES OR LIABILITY FOR DIRECT, INDIRECT, SPECIAL,
INCIDENTAL, OR CONSEQUENTIAL DAMAGES, SO THE ABOVE LIMITATIONS MAY NOT
ALWAYS APPLY. THE WARRANTIES IN THIS AGREEMENT GIVE YOU SPECIFIC LEGAL
RIGHTS AND YOU MAY ALSO HAVE OTHER RIGHTS WHICH VARY IN ACCORDANCE WITH
LOCAL LAW.

17. Disclaimer: THE AUTHORS AND DISTRIBUTORS SPECIFICALLY DISCLAIM ANY
WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT.
THIS SOFTWARE IS PROVIDED ON AN ""AS IS"" BASIS, AND THE AUTHORS AND
DISTRIBUTORS HAVE NO OBLIGATION TO PROVIDE MAINTENANCE, SUPPORT, UPDATES,
ENHANCEMENTS, OR MODIFICATIONS.

18. Limitation of Remedies and Liability: NEITHER MISTACHKIN SYSTEMS NOR
ANYONE ELSE WHO HAS BEEN INVOLVED IN THE CREATION, PRODUCTION OR DELIVERY
OF THE SOFTWARE AND SERVICES RELATED THERETO, OR EQUIPMENT (INCLUDING
COMPUTERS AND MACHINES) AND SERVICES RELATED THERETO, SHALL BE LIABLE TO
USER OR ANY PARTY CLAIMING THROUGH USER FOR ANY DAMAGES OR EXPENSES OF ANY
TYPE, INCLUDING BUT NOT LIMITED TO ANY LOST PROFITS, LOST SAVINGS, LOST
BUSINESS, LOSS OF ANTICIPATED BENEFITS OR OTHER INCIDENTAL OR
CONSEQUENTIAL DAMAGES, DIRECT OR INDIRECT, SPECIAL OR GENERAL, ARISING OUT
OF THE USE OR INABILITY TO USE SUCH SOFTWARE OR EQUIPMENT, WHETHER ARISING
OUT OF CONTRACT, NEGLIGENCE, TORT OR UNDER ANY WARRANTY OR OTHERWISE AND
WHETHER CAUSED BY DEFECT, NEGLIGENCE, BREACH OF WARRANTY, DELAY IN
DELIVERY OR OTHERWISE, EVEN IF MISTACHKIN SYSTEMS HAS BEEN ADVISED OF THE
POSSIBILITY OF SUCH DAMAGES OR FOR ANY OTHER CLAIM BY ANY OTHER PARTY. NO
OBLIGATION OR LIABILITY SHALL ARISE OR FLOW FROM MISTACHKIN SYSTEMS
RENDERING TECHNICAL OR OTHER ADVICE IN CONNECTION WITH EQUIPMENT,
MISTACHKIN SYSTEMS SOFTWARE OR MISTACHKIN SYSTEMS SERVICES, INCLUDING BUT
NOT LIMITED TO, MISTACHKIN SYSTEMS INSTALLATION AND TRAINING SERVICES AND
ANNUAL SUPPORT AND MAINTENANCE SERVICES. MISTACHKIN SYSTEMS LIABILITY FOR
DAMAGES IN NO EVENT SHALL EXCEED THE LICENSE FEE PAID FOR THE RIGHT TO USE
THE SOFTWARE.

19. General: (a) This license shall be governed and interpreted, except
the federal laws which govern trademarks and copyrights, in accordance
with the laws of the State of Oregon. This agreement has been made
entirely within the State of Oregon. If any suit or action is filed by any
party to enforce this license or otherwise with respect to the subject
matter of this license, venue SHALL be in the federal or state courts
nearest Salem, Oregon. The parties further agree that this provision shall
survive the termination of this agreement and that NO ACTION, regardless
of form arising hereunder, may be instituted by either party more than one
(1) year after the cause of action arose, or, in the case of nonpayment,
more than two (2) years from the date of the last payment, except that the
above limitations shall not apply to the enforcement of any of Mistachkin
Systems intellectual property rights. In any such action, the prevailing
party shall be entitled to its reasonable attorney fees at trial or on
appeal thereof, as awarded by the court. This license shall be construed
in such a fashion as to make each provision enforceable to the maximum
extent possible under law. (b) User acknowledges that User has read this
agreement, which comprises of all the terms and conditions in this
agreement, understands each and every term and condition of it, and agrees
to be bound by its terms and conditions. User agrees that this agreement
is the complete and exclusive statement of the agreement between
Mistachkin Systems and User and that this agreement supersedes all prior
and contemporaneous agreements, proposals, negotiations or discussions,
oral or written, relating to the subject matter herein. No course of
dealing or usage of trade or course of performance shall be relevant to
explain or supplement any terms expressed herein. User further agrees that
no representations or statements of any kind, including but not limited
to, dealer advertising, presentations, oral or written, made by any agent
or representative of Mistachkin Systems which are not stated herein shall
be binding on User or Mistachkin Systems failure or delay in enforcing any
right to a provision of this agreement shall not be deemed as a waiver of
such provisions or right in respect to any subsequent breach or a
continuance of any existing breach. If any provision of this license shall
be held to be unenforceable by a court of jurisdiction, the remaining
provisions will remain in force and effect and be enforced to the maximum
extent permissible. (c) Mistachkin Systems shall not be in default by
reason of any failure of its performance under this agreement if failure
results, directly or indirectly, from, but not limited to, fire,
explosion, strike, freight embargo, act of God, or the public enemy, war,
civil disturbance, act of any government, de jure or de facto, or any
agency or official thereof, terrorism, labor shortage, transportation
contingencies, unusually severe weather, default of manufacturer or
supplier as a subcontractor, quarantine or restriction, epidemic or
catastrophe or any similar event beyond the control of Mistachkin Systems.

20. Government Use: If you are acquiring this software on behalf of the
United States government, the Government shall have only ""Restricted
Rights"" in the software and related documentation as defined in the
Federal Acquisition Regulations (FARs) in Clause 52.227.19 (c) (2). If you
are acquiring the software on behalf of the Department of Defense, the
software shall be classified as ""Commercial Computer Software"" and the
Government shall have only ""Restricted Rights"" as defined in Clause
252.227-7013 (c) (1) of DFARs. Notwithstanding the foregoing, the authors
grant the United States Government and others acting in its behalf
permission to use and distribute the software in accordance with the terms
specified in this license. In addition, this limited grant of rights shall
automatically terminate if, at the sole discretion of Joseph Mistachkin, any
part of the United States government engages in conduct that attempts to
violate the Constitutional rights of any natural person with the last name
""Mistachkin"".";
        #endregion
    }
}
