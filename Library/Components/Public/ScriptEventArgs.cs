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
    /// <summary>
    /// This class contains the event data associated with a script-related
    /// notification raised by an Eagle interpreter.  It carries the notification
    /// type and flags, the interpreter and client data involved, the arguments,
    /// result, and exception (if any), the interrupt type, and the resource
    /// manager used to format any associated message.  It implements
    /// <see cref="IScriptEventArgs" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("98df66a4-8f97-4305-bedc-598474f947d0")]
    public class ScriptEventArgs : MessageEventArgs, IScriptEventArgs
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class using the fully specified set of
        /// script notification event data.
        /// </summary>
        /// <param name="id">
        /// The unique identifier associated with this event.
        /// </param>
        /// <param name="types">
        /// The notification types associated with this event.
        /// </param>
        /// <param name="flags">
        /// The notification flags associated with this event.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with this event.  This parameter may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this event.  This parameter may be
        /// null.
        /// </param>
        /// <param name="arguments">
        /// The argument list associated with this event.  This parameter may be
        /// null.
        /// </param>
        /// <param name="result">
        /// The result associated with this event.  This parameter may be null.
        /// </param>
        /// <param name="exception">
        /// The script exception associated with this event, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interruptType">
        /// The interrupt type associated with this event.
        /// </param>
        /// <param name="resourceName">
        /// The name of the resource used to format the message associated with
        /// this event.  This parameter may be null.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager used to format the message associated with this
        /// event.  This parameter may be null.
        /// </param>
        /// <param name="messageArgs">
        /// The arguments used to format the message associated with this event.
        /// This parameter may be null.
        /// </param>
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
        /// <summary>
        /// The interpreter associated with this event.  This field may be null.
        /// </summary>
#if SERIALIZATION && !ISOLATED_INTERPRETERS && !ISOLATED_PLUGINS
        [NonSerialized()]
#endif
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter associated with this event.
        /// </summary>
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData Members
        /// <summary>
        /// The client data associated with this event.  This field may be null.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets the client data associated with this event.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptEventArgs Members
        /// <summary>
        /// The notification types associated with this event.
        /// </summary>
        private NotifyType notifyTypes;
        /// <summary>
        /// Gets the notification types associated with this event.
        /// </summary>
        public virtual NotifyType NotifyTypes
        {
            get { return notifyTypes; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The notification flags associated with this event.
        /// </summary>
        private NotifyFlags notifyFlags;
        /// <summary>
        /// Gets the notification flags associated with this event.
        /// </summary>
        public virtual NotifyFlags NotifyFlags
        {
            get { return notifyFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The argument list associated with this event.  This field may be
        /// null.
        /// </summary>
        private ArgumentList arguments;
        /// <summary>
        /// Gets the argument list associated with this event.
        /// </summary>
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The result associated with this event.  This field may be null.
        /// </summary>
        private Result result;
        /// <summary>
        /// Gets the result associated with this event.
        /// </summary>
        public virtual Result Result
        {
            get { return result; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script exception associated with this event, if any.  This field
        /// may be null.
        /// </summary>
        private ScriptException exception;
        /// <summary>
        /// Gets the script exception associated with this event, if any.
        /// </summary>
        public virtual ScriptException Exception
        {
            get { return exception; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The interrupt type associated with this event.
        /// </summary>
        private InterruptType interruptType;
        /// <summary>
        /// Gets the interrupt type associated with this event.
        /// </summary>
        public virtual InterruptType InterruptType
        {
            get { return interruptType; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Eagle._Components.Public.MessageEventArgs Overrides
        /// <summary>
        /// The resource manager used to format the message associated with this
        /// event.  This field may be null.
        /// </summary>
        private ResourceManager resourceManager;
        /// <summary>
        /// Gets the resource manager used to format the message associated with
        /// this event.
        /// </summary>
        public override ResourceManager ResourceManager
        {
            get { return resourceManager; }
        }
        #endregion
    }
}
