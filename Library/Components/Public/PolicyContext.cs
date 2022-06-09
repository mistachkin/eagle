/*
 * PolicyContext.cs --
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
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("00e38589-5457-4aa1-a0f6-b4c0ca0e9a01")]
    public sealed class PolicyContext :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IPolicyContext
    {
        #region Private Constants
        //
        // HACK: This is purposely not read-only.
        //
        private static bool DefaultTraceFull = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static bool ForceTraceFull = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private int undecidedCount; // the number of "undecided" votes.
        private int deniedCount;    // the number of "denied" votes.
        private int approvedCount;  // the number of "approved" votes.
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private PolicyContext()
            : base()
        {
            undecidedCount = 0;
            deniedCount = 0;
            approvedCount = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private PolicyContext(
            PolicyFlags flags,
            AssemblyName assemblyName,
            string typeName,
            IExecute execute,
            ArgumentList arguments,
            IScript script,
            string fileName,
            byte[] bytes,
            string text,
            Encoding encoding,
            byte[] hashValue,
            string hashAlgorithmName,
            IClientData clientData,
            Interpreter interpreter,
            IPlugin plugin,
            PolicyDecision originalDecision
            )
            : this()
        {
            this.flags = flags;
            this.assemblyName = assemblyName;
            this.typeName = typeName;
            this.execute = execute;
            this.arguments = arguments;
            this.script = script;
            this.fileName = fileName;
            this.bytes = bytes;
            this.text = text;
            this.encoding = encoding;
            this.hashValue = hashValue;
            this.hashAlgorithmName = hashAlgorithmName;
            this.clientData = clientData;
            this.interpreter = interpreter;
            this.plugin = plugin;
            this.originalDecision = originalDecision;

            //
            // NOTE: *WARNING* Take the original decision into account.  With
            //       the current logic, if the original decision is "denied"
            //       then any later votes do not matter.
            //
            Vote(this.originalDecision);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        public static PolicyContext Create(
            PolicyFlags flags,
            AssemblyName assemblyName,
            string typeName,
            IExecute execute,
            ArgumentList arguments,
            IScript script,
            string fileName,
            byte[] bytes,
            string text,
            Encoding encoding,
            byte[] hashValue,
            string hashAlgorithmName,
            IClientData clientData,
            Interpreter interpreter,
            IPlugin plugin,
            PolicyDecision originalDecision
            )
        {
            return new PolicyContext(
                flags, assemblyName, typeName, execute, arguments, script,
                fileName, bytes, text, encoding, hashValue, hashAlgorithmName,
                clientData, interpreter, plugin, originalDecision);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        internal static bool GetForceTraceFull()
        {
            return ForceTraceFull;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void SetForceTraceFull(
            bool full
            )
        {
            ForceTraceFull = full;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void ResetForceTraceFull()
        {
            ForceTraceFull = DefaultTraceFull;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Policy Data Helpers
        public static PolicyDecision None()
        {
            return PolicyDecision.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNone(
            PolicyDecision decision
            )
        {
            return (decision == PolicyDecision.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNone(
            IPolicyContext policyContext
            )
        {
            if ((policyContext != null) && IsNone(policyContext.Decision))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUndecided(
            PolicyDecision decision
            )
        {
            return (decision == PolicyDecision.Undecided);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUndecided(
            IPolicyContext policyContext
            )
        {
            if ((policyContext != null) && IsUndecided(policyContext.Decision))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDenied(
            PolicyDecision decision
            )
        {
            return (decision == PolicyDecision.Denied);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsApproved(
            PolicyDecision decision
            )
        {
            return (decision == PolicyDecision.Approved);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsApproved(
            IPolicyContext policyContext
            )
        {
            if ((policyContext != null) && IsApproved(policyContext.Decision))
                return true;

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Members
        private void Vote(
            PolicyDecision decision
            )
        {
            switch (decision)
            {
                case PolicyDecision.Undecided:
                    {
                        Interlocked.Increment(ref undecidedCount);
                        break;
                    }
                case PolicyDecision.Denied:
                    {
                        Interlocked.Increment(ref deniedCount);
                        break;
                    }
                case PolicyDecision.Approved:
                    {
                        Interlocked.Increment(ref approvedCount);
                        break;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringPairList ToList(
            bool full
            )
        {
            StringPairList list = new StringPairList();

            list.Add("full", full.ToString());

            if (clientData != null)
                list.Add("clientData", clientData.ToString(), !full, !full);

            if (interpreter != null)
            {
                list.Add("interpreter",
                    FormatOps.InterpreterNoThrow(interpreter));
            }

            if (plugin != null)
                list.Add("plugin", plugin.ToString());

            list.Add("flags", flags.ToString());

            if (assemblyName != null)
                list.Add("assemblyName", assemblyName.ToString());

            if (typeName != null)
                list.Add("typeName", typeName);

            if (execute != null)
                list.Add("execute", execute.ToString());

            if (arguments != null)
                list.Add("arguments", arguments.ToString(), !full, !full);

            if (script != null)
                list.Add("script", script.ToString(), !full, !full);

            if (fileName != null)
                list.Add("fileName", fileName, !full, !full);

            if (bytes != null)
            {
                list.Add("bytes",
                    ArrayOps.ToHexadecimalString(bytes), !full, !full);
            }

            if (text != null)
                list.Add("text", text, !full, !full);

            if (encoding != null)
                list.Add("encoding", encoding.ToString());

            if (hashValue != null)
                list.Add("hashValue", ArrayOps.ToHexadecimalString(hashValue));

            if (hashAlgorithmName != null)
                list.Add("hashAlgorithmName", hashAlgorithmName);

            if (result != null)
                list.Add("result", result, !full, !full);

            list.Add("originalDecision", originalDecision.ToString());
            list.Add("decision", Decision.ToString()); /* PROPERTY */

            if (reason != null)
                list.Add("reason", reason, !full, !full);

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { return clientData; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        //
        // NOTE: *WARNING* This is the interpreter that contains
        //       the policy currently being invoked (i.e. it can
        //       change with each callback).
        //
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        //
        // NOTE: *WARNING* This is the plugin that contains
        //       the policy currently being invoked (i.e. it
        //       can change with each callback).
        //
        private IPlugin plugin;
        public IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPolicyContext Members
        private PolicyFlags flags;
        public PolicyFlags Flags
        {
            get { return flags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private AssemblyName assemblyName;
        public AssemblyName AssemblyName
        {
            get { return assemblyName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string typeName;
        public string TypeName
        {
            get { return typeName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IExecute execute;
        public IExecute Execute
        {
            get { return execute; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentList arguments;
        public ArgumentList Arguments
        {
            get { return arguments; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IScript script;
        public IScript Script
        {
            get { return script; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string fileName;
        public string FileName
        {
            get { return fileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] bytes;
        public byte[] Bytes
        {
            get { return bytes; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string text;
        public string Text
        {
            get { return text; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Encoding encoding;
        public Encoding Encoding
        {
            get { return encoding; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] hashValue;
        public byte[] HashValue
        {
            get { return hashValue; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string hashAlgorithmName;
        public string HashAlgorithmName
        {
            get { return hashAlgorithmName; }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* For informational purposes only.
        //       Please DO NOT USE to make policy decisions.
        //
        private Result result;
        public Result Result
        {
            get { return result; }
            set { result = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PolicyDecision originalDecision;
        public PolicyDecision OriginalDecision
        {
            get { return originalDecision; }
        }

        ///////////////////////////////////////////////////////////////////////

        public PolicyDecision Decision
        {
            get
            {
                //
                // NOTE: The logic here is fairly simple:
                //
                // 1. If there are any votes to deny, the decision is "denied".
                //
                // 2. Otherwise, if the majority of votes are to approve, the
                //    decision is "approved".
                //
                // 3. Otherwise, if there are any undecided votes, the decision
                //    is "undecided".
                //
                // 4. Otherwise, the decision is "none".
                //
                if (deniedCount > 0)
                    return PolicyDecision.Denied;
                else if (approvedCount > undecidedCount)
                    return PolicyDecision.Approved;
                else if (undecidedCount > 0)
                    return PolicyDecision.Undecided;
                else
                    return PolicyDecision.None;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Will be seen from inside the "safe"
        //       interpreter.  Please DO NOT USE for potentially
        //       sensitive information.
        //
        private Result reason;
        public Result Reason
        {
            get { return reason; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsUndecided()
        {
            return IsUndecided(this);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsDenied()
        {
            return IsDenied(this.Decision);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsApproved()
        {
            return IsApproved(this);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Undecided()
        {
            Vote(PolicyDecision.Undecided);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Denied()
        {
            Vote(PolicyDecision.Denied);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Approved()
        {
            Vote(PolicyDecision.Approved);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Undecided(
            Result reason
            )
        {
            this.reason = reason;

            Undecided();
        }

        ///////////////////////////////////////////////////////////////////////

        public void Denied(
            Result reason
            )
        {
            this.reason = reason;

            Denied();
        }

        ///////////////////////////////////////////////////////////////////////

        public void Approved(
            Result reason
            )
        {
            this.reason = reason;

            Approved();
        }

        ///////////////////////////////////////////////////////////////////////

        [Obsolete()]
        public void Trace(
            string category,
            TracePriority priority
            )
        {
            Trace(category, priority, DefaultTraceFull);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Trace(
            string category,
            TracePriority priority,
            bool full
            )
        {
            bool localFull = full || ForceTraceFull;

            TraceOps.DebugTrace(String.Format(
                "PolicyContext: {0}", ToList(localFull)), category,
                priority);
        }
        #endregion
    }
}
