/*
 * GetScriptClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Reflection;
using System.Resources;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("654c7fc2-999e-4a1f-83bb-52139265f0a5")]
    internal sealed class GetScriptClientData : ReadScriptClientData
    {
        #region Private Constructors
        private GetScriptClientData(
            object data,               /* in */
            string scriptFileName,     /* in */
            string originalText,       /* in */
            string text,               /* in */
            ByteList bytes,            /* in */
            string resourceMethodName, /* in */
            string resourceName,       /* in */
            bool isolated              /* in */
            )
            : base(data, scriptFileName, originalText, text, bytes)
        {
            this.resourceMethodName = resourceMethodName;
            this.resourceName = resourceName;
            this.isolated = isolated;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public GetScriptClientData(
            object data,                    /* in */
            string scriptFileName,          /* in */
            string originalText,            /* in */
            string text,                    /* in */
            ByteList bytes,                 /* in */
            IPluginData resourcePluginData, /* in */
            string resourceMethodName,      /* in */
            string resourceName,            /* in */
            bool isolated                   /* in */
            )
            : this(data, scriptFileName, originalText, text, bytes,
                   resourceMethodName, resourceName, isolated)
        {
            this.resourcePluginData = resourcePluginData;
        }

        ///////////////////////////////////////////////////////////////////////

        public GetScriptClientData(
            object data,                     /* in */
            string scriptFileName,           /* in */
            string originalText,             /* in */
            string text,                     /* in */
            ByteList bytes,                  /* in */
            string resourceFileName,         /* in */
            ResourceManager resourceManager, /* in */
            string resourceMethodName,       /* in */
            string resourceName,             /* in */
            bool isolated                    /* in */
            )
            : this(data, scriptFileName, originalText, text, bytes,
                   resourceMethodName, resourceName, isolated)
        {
            this.resourceFileName = resourceFileName;
            this.resourceManager = resourceManager;
        }

        ///////////////////////////////////////////////////////////////////////

        public GetScriptClientData(
            object data,               /* in */
            string scriptFileName,     /* in */
            string originalText,       /* in */
            string text,               /* in */
            ByteList bytes,            /* in */
            Assembly resourceAssembly, /* in */
            string resourceMethodName, /* in */
            string resourceName,       /* in */
            bool isolated              /* in */
            )
            : this(data, scriptFileName, originalText, text, bytes,
                   resourceMethodName, resourceName, isolated)
        {
            this.resourceAssembly = resourceAssembly;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private IPluginData resourcePluginData;
        public IPluginData ResourcePluginData
        {
            get { return resourcePluginData; }
            set { resourcePluginData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string resourceFileName;
        public string ResourceFileName
        {
            get { return resourceFileName; }
            set { resourceFileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ResourceManager resourceManager;
        public ResourceManager ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Assembly resourceAssembly;
        public Assembly ResourceAssembly
        {
            get { return resourceAssembly; }
            set { resourceAssembly = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string resourceMethodName;
        public string ResourceMethodName
        {
            get { return resourceMethodName; }
            set { resourceMethodName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string resourceName;
        public string ResourceName
        {
            get { return resourceName; }
            set { resourceName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool isolated;
        public bool Isolated
        {
            get { return isolated; }
            set { isolated = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public override IStringList ToList()
        {
            IStringList list = base.ToList();

            if (resourcePluginData != null)
            {
                list.Add("ResourcePluginData",
                    resourcePluginData.ToString());
            }

            if (resourceFileName != null)
                list.Add("ResourceFileName", resourceFileName);

            if (resourceManager != null)
            {
                list.Add("ResourceManager",
                    resourceManager.ToString());
            }

            if (resourceAssembly != null)
            {
                list.Add("ResourceAssembly",
                    resourceAssembly.ToString());
            }

            if (resourceMethodName != null)
                list.Add("ResourceMethodName", resourceMethodName);

            if (resourceName != null)
                list.Add("ResourceName", resourceName);

            list.Add("Isolated", isolated.ToString());

            return list;
        }
        #endregion
    }
}
