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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("877d1fb8-3e45-4484-855c-c6f7fa8ee676")]
    [ObjectGroup("default")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IFunction
    {
        #region Public Constructors
        public Default(
            IFunctionData functionData
            )
        {
            kind = IdentifierKind.Function;
            id = AttributeOps.GetObjectId(this);
            group = AttributeOps.GetObjectGroups(this);

            if (functionData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, functionData.Group);

                name = functionData.Name;
                description = functionData.Description;
                clientData = functionData.ClientData;
                typeName = functionData.TypeName;
                arguments = functionData.Arguments;
                types = functionData.Types;
                flags = functionData.Flags;
                plugin = functionData.Plugin;
                token = functionData.Token;
            }
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

        #region IHavePlugin Members
        private IPlugin plugin;
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IFunctionData Members
        private string typeName;
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int arguments;
        public virtual int Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private TypeList types;
        public virtual TypeList Types
        {
            get { return types; }
            set { types = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private FunctionFlags flags;
        public virtual FunctionFlags Flags
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

        #region IExecuteArgument Members
        public virtual ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Argument value,
            ref Result error
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
            Interlocked.Increment(ref usageCount);

            microseconds = Interlocked.Add(
                ref usageMicroseconds, microseconds);

            return true;
        }
        #endregion
    }
}
