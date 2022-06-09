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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Lambdas
{
    [ObjectId("e55df68b-77d6-4d25-8a9c-8932bf375941")]
    internal class Default : ILambda
    {
        #region Public Constructors
        public Default(
            ILambdaData lambdaData
            )
        {
            kind = IdentifierKind.Lambda;
            id = AttributeOps.GetObjectId(this);
            group = AttributeOps.GetObjectGroups(this);

            if (lambdaData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, lambdaData.Group);

                name = lambdaData.Name;
                description = lambdaData.Description;
                flags = lambdaData.Flags;
                clientData = lambdaData.ClientData;
                arguments = lambdaData.Arguments;
                namedArguments = lambdaData.NamedArguments;
                body = lambdaData.Body;
                location = lambdaData.Location;
                token = lambdaData.Token;
            }

            callback = null;
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

        ///////////////////////////////////////////////////////////////////////

        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
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

        ///////////////////////////////////////////////////////////////////////

        #region ILevels Members
        private int levels;
        public virtual int Levels
        {
            get { return Interlocked.CompareExchange(ref levels, 0, 0); }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int EnterLevel()
        {
            return Interlocked.Increment(ref levels);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int ExitLevel()
        {
            return Interlocked.Decrement(ref levels);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcedureData Members
        private ProcedureFlags flags;
        public virtual ProcedureFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentList arguments;
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentDictionary namedArguments;
        public virtual ArgumentDictionary NamedArguments
        {
            get { return namedArguments; }
            set { namedArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string body;
        public virtual string Body
        {
            get { return body; }
            set { body = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IScriptLocation location;
        public virtual IScriptLocation Location
        {
            get { return location; }
            set { location = value; }
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

        #region IDynamicExecuteCallback Members
        private ExecuteCallback callback;
        public virtual ExecuteCallback Callback
        {
            get { return callback; }
            set { callback = value; }
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
    }
}
