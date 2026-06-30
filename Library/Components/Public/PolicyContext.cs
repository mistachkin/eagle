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
    /// <summary>
    /// This class represents the context provided to a policy callback while it
    /// decides whether a particular operation should be permitted.  It carries
    /// the details of the operation under consideration (e.g. the assembly,
    /// type, command, arguments, script, file name, bytes, text, encoding,
    /// timeout, and hash information) together with the interpreter and plugin
    /// involved in the current callback.  A policy casts its vote via the
    /// <c>Undecided</c>, <c>Denied</c>, and <c>Approved</c> methods, and the
    /// aggregated outcome is exposed through the <see cref="Decision" />
    /// property.
    /// </summary>
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
        /// <summary>
        /// The default value used to indicate whether the full details of a
        /// policy context should be included when it is traced.
        /// </summary>
        private static bool DefaultTraceFull = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the full details of a policy context are always
        /// included when it is traced, regardless of the value requested by the
        /// caller.
        /// </summary>
        private static bool ForceTraceFull = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The number of "undecided" votes cast against this policy context.
        /// </summary>
        private int undecidedCount; // the number of "undecided" votes.
        /// <summary>
        /// The number of "denied" votes cast against this policy context.
        /// </summary>
        private int deniedCount;    // the number of "denied" votes.
        /// <summary>
        /// The number of "approved" votes cast against this policy context.
        /// </summary>
        private int approvedCount;  // the number of "approved" votes.
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class with all vote counts reset to
        /// zero.
        /// </summary>
        private PolicyContext()
            : base()
        {
            undecidedCount = 0;
            deniedCount = 0;
            approvedCount = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified operation
        /// details, interpreter, plugin, and original policy decision.
        /// </summary>
        /// <param name="flags">
        /// The flags that describe the policy and the operation being checked.
        /// </param>
        /// <param name="assemblyName">
        /// The name of the assembly associated with the operation being
        /// checked.  This parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the type associated with the operation being checked.
        /// This parameter may be null.
        /// </param>
        /// <param name="execute">
        /// The command, sub-command, or other entity being executed.  This
        /// parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="script">
        /// The script associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file associated with the operation being checked.
        /// This parameter may be null.
        /// </param>
        /// <param name="bytes">
        /// The raw bytes associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="text">
        /// The text associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, associated with the operation being
        /// checked.  This parameter may be null.
        /// </param>
        /// <param name="hashValue">
        /// The hash value associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm used to produce the hash value.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that contains the policy currently being invoked.
        /// This parameter may be null.
        /// </param>
        /// <param name="plugin">
        /// The plugin that contains the policy currently being invoked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="originalDecision">
        /// The original policy decision, which is cast as the initial vote.
        /// </param>
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
            int? timeout,
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
            this.timeout = timeout;
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
        /// <summary>
        /// This method creates a new instance of this class using the specified
        /// operation details, interpreter, plugin, and original policy
        /// decision.
        /// </summary>
        /// <param name="flags">
        /// The flags that describe the policy and the operation being checked.
        /// </param>
        /// <param name="assemblyName">
        /// The name of the assembly associated with the operation being
        /// checked.  This parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the type associated with the operation being checked.
        /// This parameter may be null.
        /// </param>
        /// <param name="execute">
        /// The command, sub-command, or other entity being executed.  This
        /// parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="script">
        /// The script associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file associated with the operation being checked.
        /// This parameter may be null.
        /// </param>
        /// <param name="bytes">
        /// The raw bytes associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="text">
        /// The text associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, associated with the operation being
        /// checked.  This parameter may be null.
        /// </param>
        /// <param name="hashValue">
        /// The hash value associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm used to produce the hash value.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that contains the policy currently being invoked.
        /// This parameter may be null.
        /// </param>
        /// <param name="plugin">
        /// The plugin that contains the policy currently being invoked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="originalDecision">
        /// The original policy decision, which is cast as the initial vote.
        /// </param>
        /// <returns>
        /// The newly created policy context.
        /// </returns>
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
            int? timeout,
            byte[] hashValue,
            string hashAlgorithmName,
            IClientData clientData,
            Interpreter interpreter,
            IPlugin plugin,
            PolicyDecision originalDecision
            )
        {
            return new PolicyContext(
                flags, assemblyName, typeName, execute, arguments,
                script, fileName, bytes, text, encoding, timeout,
                hashValue, hashAlgorithmName, clientData, interpreter,
                plugin, originalDecision);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method queries whether the full details of a policy context are
        /// always included when it is traced.
        /// </summary>
        /// <returns>
        /// True if full details are always included; otherwise, false.
        /// </returns>
        internal static bool GetForceTraceFull()
        {
            return ForceTraceFull;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets whether the full details of a policy context are
        /// always included when it is traced.
        /// </summary>
        /// <param name="full">
        /// Non-zero if full details should always be included when tracing;
        /// otherwise, zero.
        /// </param>
        internal static void SetForceTraceFull(
            bool full
            )
        {
            ForceTraceFull = full;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets, to its default value, whether the full details
        /// of a policy context are always included when it is traced.
        /// </summary>
        internal static void ResetForceTraceFull()
        {
            ForceTraceFull = DefaultTraceFull;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Policy Data Helpers
        /// <summary>
        /// This method returns the policy decision that represents the absence
        /// of any decision.
        /// </summary>
        /// <returns>
        /// The <see cref="PolicyDecision.None" /> value.
        /// </returns>
        public static PolicyDecision None()
        {
            return PolicyDecision.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified policy decision
        /// represents the absence of any decision.
        /// </summary>
        /// <param name="decision">
        /// The policy decision to check.
        /// </param>
        /// <returns>
        /// True if the decision is <see cref="PolicyDecision.None" />;
        /// otherwise, false.
        /// </returns>
        public static bool IsNone(
            PolicyDecision decision
            )
        {
            return (decision == PolicyDecision.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the decision of the specified policy
        /// context represents the absence of any decision.
        /// </summary>
        /// <param name="policyContext">
        /// The policy context whose decision is checked.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the policy context is not null and its decision is
        /// <see cref="PolicyDecision.None" />; otherwise, false.
        /// </returns>
        public static bool IsNone(
            IPolicyContext policyContext
            )
        {
            if ((policyContext != null) && IsNone(policyContext.Decision))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified policy decision
        /// represents an undecided outcome.
        /// </summary>
        /// <param name="decision">
        /// The policy decision to check.
        /// </param>
        /// <returns>
        /// True if the decision is <see cref="PolicyDecision.Undecided" />;
        /// otherwise, false.
        /// </returns>
        public static bool IsUndecided(
            PolicyDecision decision
            )
        {
            return (decision == PolicyDecision.Undecided);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the decision of the specified policy
        /// context represents an undecided outcome.
        /// </summary>
        /// <param name="policyContext">
        /// The policy context whose decision is checked.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the policy context is not null and its decision is
        /// <see cref="PolicyDecision.Undecided" />; otherwise, false.
        /// </returns>
        public static bool IsUndecided(
            IPolicyContext policyContext
            )
        {
            if ((policyContext != null) && IsUndecided(policyContext.Decision))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified policy decision
        /// represents a denied outcome.
        /// </summary>
        /// <param name="decision">
        /// The policy decision to check.
        /// </param>
        /// <returns>
        /// True if the decision is <see cref="PolicyDecision.Denied" />;
        /// otherwise, false.
        /// </returns>
        public static bool IsDenied(
            PolicyDecision decision
            )
        {
            return (decision == PolicyDecision.Denied);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified policy decision
        /// represents an approved outcome.
        /// </summary>
        /// <param name="decision">
        /// The policy decision to check.
        /// </param>
        /// <returns>
        /// True if the decision is <see cref="PolicyDecision.Approved" />;
        /// otherwise, false.
        /// </returns>
        public static bool IsApproved(
            PolicyDecision decision
            )
        {
            return (decision == PolicyDecision.Approved);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the decision of the specified policy
        /// context represents an approved outcome.
        /// </summary>
        /// <param name="policyContext">
        /// The policy context whose decision is checked.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the policy context is not null and its decision is
        /// <see cref="PolicyDecision.Approved" />; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method records a single vote for the specified policy decision
        /// by incrementing the associated vote count.
        /// </summary>
        /// <param name="decision">
        /// The policy decision being voted for.
        /// </param>
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

        /// <summary>
        /// This method builds a list of name/value pairs that represents the
        /// current state of this policy context, suitable for display or
        /// tracing.
        /// </summary>
        /// <param name="full">
        /// Non-zero to include the full, untruncated details of the policy
        /// context; otherwise, zero.
        /// </param>
        /// <returns>
        /// The list of name/value pairs representing this policy context.
        /// </returns>
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

            if (timeout != null)
                list.Add("timeout", ((int)timeout).ToString());

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
        /// <summary>
        /// The client data associated with the operation being checked.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets the client data associated with the operation being checked.
        /// </summary>
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
        /// <summary>
        /// The interpreter that contains the policy currently being invoked.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets or sets the interpreter that contains the policy currently being
        /// invoked.
        /// </summary>
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
        /// <summary>
        /// The plugin that contains the policy currently being invoked.
        /// </summary>
        private IPlugin plugin;
        /// <summary>
        /// Gets or sets the plugin that contains the policy currently being
        /// invoked.
        /// </summary>
        public IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// The name of the type associated with the operation being checked.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets the name of the type associated with the operation being
        /// checked.  Setting this property is not supported and always throws an
        /// exception.
        /// </summary>
        public string TypeName
        {
            get { return typeName; }
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the type associated with the operation being checked.
        /// This property is not supported; getting or setting it always throws
        /// an exception.
        /// </summary>
        public Type Type
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPolicyContext Members
        /// <summary>
        /// The flags that describe the policy and the operation being checked.
        /// </summary>
        private PolicyFlags flags;
        /// <summary>
        /// Gets the flags that describe the policy and the operation being
        /// checked.
        /// </summary>
        public PolicyFlags Flags
        {
            get { return flags; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the assembly associated with the operation being
        /// checked.
        /// </summary>
        private AssemblyName assemblyName;
        /// <summary>
        /// Gets the name of the assembly associated with the operation being
        /// checked.
        /// </summary>
        public AssemblyName AssemblyName
        {
            get { return assemblyName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The command, sub-command, or other entity being executed.
        /// </summary>
        private IExecute execute;
        /// <summary>
        /// Gets the command, sub-command, or other entity being executed.
        /// </summary>
        public IExecute Execute
        {
            get { return execute; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The arguments associated with the operation being checked.
        /// </summary>
        private ArgumentList arguments;
        /// <summary>
        /// Gets the arguments associated with the operation being checked.
        /// </summary>
        public ArgumentList Arguments
        {
            get { return arguments; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script associated with the operation being checked.
        /// </summary>
        private IScript script;
        /// <summary>
        /// Gets the script associated with the operation being checked.
        /// </summary>
        public IScript Script
        {
            get { return script; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the file associated with the operation being checked.
        /// </summary>
        private string fileName;
        /// <summary>
        /// Gets the name of the file associated with the operation being
        /// checked.
        /// </summary>
        public string FileName
        {
            get { return fileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The raw bytes associated with the operation being checked.
        /// </summary>
        private byte[] bytes;
        /// <summary>
        /// Gets the raw bytes associated with the operation being checked.
        /// </summary>
        public byte[] Bytes
        {
            get { return bytes; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The text associated with the operation being checked.
        /// </summary>
        private string text;
        /// <summary>
        /// Gets the text associated with the operation being checked.
        /// </summary>
        public string Text
        {
            get { return text; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The encoding associated with the operation being checked.
        /// </summary>
        private Encoding encoding;
        /// <summary>
        /// Gets the encoding associated with the operation being checked.
        /// </summary>
        public Encoding Encoding
        {
            get { return encoding; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The timeout, in milliseconds, associated with the operation being
        /// checked.
        /// </summary>
        private int? timeout;
        /// <summary>
        /// Gets the timeout, in milliseconds, associated with the operation
        /// being checked.
        /// </summary>
        public int? Timeout
        {
            get { return timeout; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The hash value associated with the operation being checked.
        /// </summary>
        private byte[] hashValue;
        /// <summary>
        /// Gets the hash value associated with the operation being checked.
        /// </summary>
        public byte[] HashValue
        {
            get { return hashValue; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the hash algorithm used to produce the hash value.
        /// </summary>
        private string hashAlgorithmName;
        /// <summary>
        /// Gets the name of the hash algorithm used to produce the hash value.
        /// </summary>
        public string HashAlgorithmName
        {
            get { return hashAlgorithmName; }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* For informational purposes only.
        //       Please DO NOT USE to make policy decisions.
        //
        /// <summary>
        /// The result associated with the operation being checked.  This is for
        /// informational purposes only and must not be used to make policy
        /// decisions.
        /// </summary>
        private Result result;
        /// <summary>
        /// Gets or sets the result associated with the operation being checked.
        /// This is for informational purposes only and must not be used to make
        /// policy decisions.
        /// </summary>
        public Result Result
        {
            get { return result; }
            set { result = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The original policy decision, prior to any votes cast against this
        /// policy context.
        /// </summary>
        private PolicyDecision originalDecision;
        /// <summary>
        /// Gets the original policy decision, prior to any votes cast against
        /// this policy context.
        /// </summary>
        public PolicyDecision OriginalDecision
        {
            get { return originalDecision; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the aggregate policy decision computed from the votes cast
        /// against this policy context.  A single "denied" vote yields
        /// <see cref="PolicyDecision.Denied" />; otherwise, a majority of
        /// "approved" votes yields <see cref="PolicyDecision.Approved" />; any
        /// remaining "undecided" votes yield
        /// <see cref="PolicyDecision.Undecided" />; otherwise, the decision is
        /// <see cref="PolicyDecision.None" />.
        /// </summary>
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
        /// <summary>
        /// The reason associated with the most recent vote.  This may be seen
        /// from inside a "safe" interpreter and must not be used for potentially
        /// sensitive information.
        /// </summary>
        private Result reason;
        /// <summary>
        /// Gets the reason associated with the most recent vote.  This may be
        /// seen from inside a "safe" interpreter and must not be used for
        /// potentially sensitive information.
        /// </summary>
        public Result Reason
        {
            get { return reason; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the decision of this policy context
        /// represents an undecided outcome.
        /// </summary>
        /// <returns>
        /// True if this policy context is undecided; otherwise, false.
        /// </returns>
        public bool IsUndecided()
        {
            return IsUndecided(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the decision of this policy context
        /// represents a denied outcome.
        /// </summary>
        /// <returns>
        /// True if this policy context is denied; otherwise, false.
        /// </returns>
        public bool IsDenied()
        {
            return IsDenied(this.Decision);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the decision of this policy context
        /// represents an approved outcome.
        /// </summary>
        /// <returns>
        /// True if this policy context is approved; otherwise, false.
        /// </returns>
        public bool IsApproved()
        {
            return IsApproved(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method casts an "undecided" vote against this policy context.
        /// </summary>
        public void Undecided()
        {
            Vote(PolicyDecision.Undecided);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method casts a "denied" vote against this policy context.
        /// </summary>
        public void Denied()
        {
            Vote(PolicyDecision.Denied);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method casts an "approved" vote against this policy context.
        /// </summary>
        public void Approved()
        {
            Vote(PolicyDecision.Approved);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the specified reason and then casts an
        /// "undecided" vote against this policy context.
        /// </summary>
        /// <param name="reason">
        /// The reason for the vote.  This parameter may be null.
        /// </param>
        public void Undecided(
            Result reason
            )
        {
            this.reason = reason;

            Undecided();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the specified reason and then casts a "denied"
        /// vote against this policy context.
        /// </summary>
        /// <param name="reason">
        /// The reason for the vote.  This parameter may be null.
        /// </param>
        public void Denied(
            Result reason
            )
        {
            this.reason = reason;

            Denied();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the specified reason and then casts an
        /// "approved" vote against this policy context.
        /// </summary>
        /// <param name="reason">
        /// The reason for the vote.  This parameter may be null.
        /// </param>
        public void Approved(
            Result reason
            )
        {
            this.reason = reason;

            Approved();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method traces the current state of this policy context using
        /// the default setting for whether full details are included.
        /// </summary>
        /// <param name="category">
        /// The trace category to use.  This parameter may be null.
        /// </param>
        /// <param name="priority">
        /// The trace priority to use.
        /// </param>
        [Obsolete()]
        public void Trace(
            string category,
            TracePriority priority
            )
        {
            Trace(category, priority, DefaultTraceFull);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method traces the current state of this policy context.
        /// </summary>
        /// <param name="category">
        /// The trace category to use.  This parameter may be null.
        /// </param>
        /// <param name="priority">
        /// The trace priority to use.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the full, untruncated details of the policy
        /// context; otherwise, zero.  Full details are always included when
        /// forced globally.
        /// </param>
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
