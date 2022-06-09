/*
 * ScriptEventArgs.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Resources;
using Eagle._Attributes;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("98df66a4-8f97-4305-bedc-598474f947d0")]
    public class ScriptEventArgs : MessageEventArgs, IScriptEventArgs
    {
        #region Public Constructors
        public ScriptEventArgs(
            long id,
            NotifyType types,
            NotifyFlags flags,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            Result result,
            ScriptException exception,
            InterruptType interruptType,
            string resourceName,
            ResourceManager resourceManager,
            params object[] messageArgs
            )
            : base(null, id, resourceName, messageArgs)
        {
            this.notifyTypes = types;
            this.notifyFlags = flags;
            this.interpreter = interpreter;
            this.clientData = clientData;
            this.arguments = arguments;
            this.result = result;
            this.exception = exception;
            this.interruptType = interruptType;
            this.resourceManager = resourceManager;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
#if SERIALIZATION && !ISOLATED_INTERPRETERS && !ISOLATED_PLUGINS
        [NonSerialized()]
#endif
        private Interpreter interpreter;
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptEventArgs Members
        private NotifyType notifyTypes;
        public virtual NotifyType NotifyTypes
        {
            get { return notifyTypes; }
        }

        ///////////////////////////////////////////////////////////////////////

        private NotifyFlags notifyFlags;
        public virtual NotifyFlags NotifyFlags
        {
            get { return notifyFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentList arguments;
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Result result;
        public virtual Result Result
        {
            get { return result; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ScriptException exception;
        public virtual ScriptException Exception
        {
            get { return exception; }
        }

        ///////////////////////////////////////////////////////////////////////

        private InterruptType interruptType;
        public virtual InterruptType InterruptType
        {
            get { return interruptType; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Eagle._Components.Public.MessageEventArgs Overrides
        private ResourceManager resourceManager;
        public override ResourceManager ResourceManager
        {
            get { return resourceManager; }
        }
        #endregion
    }
}
