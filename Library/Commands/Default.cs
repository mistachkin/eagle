/*
 * Default.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("f8fbc42f-5b6d-4a34-be0a-c328220eb40a")]
    [ObjectGroup("default")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        ICommand
    {
        #region Public Constructors
        public Default(
            ICommandData commandData
            )
        {
            kind = IdentifierKind.Command;

            if ((commandData == null) ||
                !FlagOps.HasFlags(commandData.Flags,
                    CommandFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            if (commandData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, commandData.Group);

                name = commandData.Name;
                description = commandData.Description;
                flags = commandData.Flags;
                plugin = commandData.Plugin;
                clientData = commandData.ClientData;
                token = commandData.Token;
            }

            callback = null;
            subCommands = null;
            syntax = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return (name != null) ?
                StringList.MakeList(FormatOps.RawTypeName(GetType()), name) :
                base.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        private int initializeCount;
        public virtual bool Initialized
        {
            get
            {
                return Interlocked.CompareExchange(
                    ref initializeCount, 0, 0) > 0;
            }
            set
            {
                if (value)
                    Interlocked.Increment(ref initializeCount);
                else
                    Interlocked.Decrement(ref initializeCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            Interlocked.Increment(ref initializeCount);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            Interlocked.Decrement(ref initializeCount);
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        private ExecuteCallback callback;
        public virtual ExecuteCallback Callback
        {
            get { return callback; }
            set { callback = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private EnsembleDictionary subCommands;
        public virtual EnsembleDictionary SubCommands
        {
            get { return subCommands; }
            set { subCommands = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        private EnsembleDictionary allowedSubCommands;
        public virtual EnsembleDictionary AllowedSubCommands
        {
            get { return allowedSubCommands; }
            set { allowedSubCommands = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EnsembleDictionary disallowedSubCommands;
        public virtual EnsembleDictionary DisallowedSubCommands
        {
            get { return disallowedSubCommands; }
            set { disallowedSubCommands = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public virtual ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        private long usageCount;
        private long usageMicroseconds;

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ResetUsage(
            UsageType type,
            ref long value
            )
        {
            switch (type)
            {
                case UsageType.Count:
                    {
                        value = Interlocked.Exchange(
                            ref usageCount, 0);

                        return true;
                    }
                case UsageType.Microseconds:
                    {
                        value = Interlocked.Exchange(
                            ref usageMicroseconds, 0);

                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            switch (type)
            {
                case UsageType.Count:
                    {
                        value = Interlocked.CompareExchange(
                            ref usageCount, 0, 0);

                        return true;
                    }
                case UsageType.Microseconds:
                    {
                        value = Interlocked.CompareExchange(
                            ref usageMicroseconds, 0, 0);

                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            switch (type)
            {
                case UsageType.Count:
                    {
                        value = Interlocked.Exchange(
                            ref usageCount, value);

                        return true;
                    }
                case UsageType.Microseconds:
                    {
                        value = Interlocked.Exchange(
                            ref usageMicroseconds, value);

                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool AddUsage(
            UsageType type,
            ref long value
            )
        {
            switch (type)
            {
                case UsageType.Count:
                    {
                        value = Interlocked.Add(
                            ref usageCount, value);

                        return true;
                    }
                case UsageType.Microseconds:
                    {
                        value = Interlocked.Add(
                            ref usageMicroseconds, value);

                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool CountUsage(
            ref long count
            )
        {
            count = Interlocked.Increment(ref usageCount);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ProfileUsage(
            ref long microseconds
            )
        {
            /* IGNORED */
            Interlocked.Increment(ref usageCount);

            microseconds = Interlocked.Add(
                ref usageMicroseconds, microseconds);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        private string typeName;
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Type type;
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandBaseData Members
        public virtual CommandFlags CommandFlags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        private IPlugin plugin;
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandData Members
        private CommandFlags flags;
        public virtual CommandFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        private long token;
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISyntax Members
        private string syntax;
        public virtual string Syntax
        {
            get { return syntax; }
            set { syntax = value; }
        }
        #endregion
    }
}
