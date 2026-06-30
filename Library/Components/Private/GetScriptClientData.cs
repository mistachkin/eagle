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
    /// <summary>
    /// This class holds the client data describing a script that was obtained
    /// via a resource lookup, extending <see cref="ReadScriptClientData" />
    /// with the resource origin metadata (e.g. the plugin, resource file,
    /// resource manager, assembly, method, and resource names) used to locate
    /// the script, along with whether the lookup was isolated.
    /// </summary>
    [ObjectId("654c7fc2-999e-4a1f-83bb-52139265f0a5")]
    internal sealed class GetScriptClientData : ReadScriptClientData
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class from the fully specified set of
        /// script and resource lookup parameters.  This is the most general
        /// constructor; the other constructor overloads delegate to it.
        /// </summary>
        /// <param name="data">
        /// The optional client data associated with the script.
        /// </param>
        /// <param name="scriptFileName">
        /// The name of the file the script was read from, if any.
        /// </param>
        /// <param name="originalText">
        /// The original, unmodified text of the script, if any.
        /// </param>
        /// <param name="text">
        /// The (possibly modified) text of the script, if any.
        /// </param>
        /// <param name="bytes">
        /// The raw bytes of the script, if any.
        /// </param>
        /// <param name="silent">
        /// Non-zero if errors encountered while obtaining the script should be
        /// suppressed.
        /// </param>
        /// <param name="resourceMethodName">
        /// The name of the method used to obtain the script resource, if any.
        /// </param>
        /// <param name="resourceName">
        /// The name of the script resource, if any.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if the script resource was obtained in an isolated manner.
        /// </param>
        private GetScriptClientData(
            object data,               /* in */
            string scriptFileName,     /* in */
            string originalText,       /* in */
            string text,               /* in */
            ByteList bytes,            /* in */
            bool silent,               /* in */
            string resourceMethodName, /* in */
            string resourceName,       /* in */
            bool isolated              /* in */
            )
            : base(data, scriptFileName, originalText, text, bytes, silent)
        {
            this.resourceMethodName = resourceMethodName;
            this.resourceName = resourceName;
            this.isolated = isolated;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
#if DATA
        /// <summary>
        /// Constructs an instance of this class using the specified bundle
        /// manager.  This constructor delegates to the private constructor.
        /// </summary>
        /// <param name="data">
        /// The optional client data associated with the script.
        /// </param>
        /// <param name="scriptFileName">
        /// The name of the file the script was read from, if any.
        /// </param>
        /// <param name="originalText">
        /// The original, unmodified text of the script, if any.
        /// </param>
        /// <param name="text">
        /// The (possibly modified) text of the script, if any.
        /// </param>
        /// <param name="bytes">
        /// The raw bytes of the script, if any.
        /// </param>
        /// <param name="silent">
        /// Non-zero if errors encountered while obtaining the script should be
        /// suppressed.
        /// </param>
        /// <param name="bundleManager">
        /// The bundle manager associated with the script resource, if any.
        /// </param>
        /// <param name="resourceMethodName">
        /// The name of the method used to obtain the script resource, if any.
        /// </param>
        /// <param name="resourceName">
        /// The name of the script resource, if any.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if the script resource was obtained in an isolated manner.
        /// </param>
        public GetScriptClientData(
            object data,                  /* in */
            string scriptFileName,        /* in */
            string originalText,          /* in */
            string text,                  /* in */
            ByteList bytes,               /* in */
            bool silent,                  /* in */
            IBundleManager bundleManager, /* in */
            string resourceMethodName,    /* in */
            string resourceName,          /* in */
            bool isolated                 /* in */
            )
            : this(data, scriptFileName, originalText, text, bytes,
                   silent, resourceMethodName, resourceName, isolated)
        {
            this.bundleManager = bundleManager;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified resource
        /// plugin data.  This constructor delegates to the private
        /// constructor.
        /// </summary>
        /// <param name="data">
        /// The optional client data associated with the script.
        /// </param>
        /// <param name="scriptFileName">
        /// The name of the file the script was read from, if any.
        /// </param>
        /// <param name="originalText">
        /// The original, unmodified text of the script, if any.
        /// </param>
        /// <param name="text">
        /// The (possibly modified) text of the script, if any.
        /// </param>
        /// <param name="bytes">
        /// The raw bytes of the script, if any.
        /// </param>
        /// <param name="silent">
        /// Non-zero if errors encountered while obtaining the script should be
        /// suppressed.
        /// </param>
        /// <param name="resourcePluginData">
        /// The plugin data for the plugin that contains the script resource,
        /// if any.
        /// </param>
        /// <param name="resourceMethodName">
        /// The name of the method used to obtain the script resource, if any.
        /// </param>
        /// <param name="resourceName">
        /// The name of the script resource, if any.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if the script resource was obtained in an isolated manner.
        /// </param>
        public GetScriptClientData(
            object data,                    /* in */
            string scriptFileName,          /* in */
            string originalText,            /* in */
            string text,                    /* in */
            ByteList bytes,                 /* in */
            bool silent,                    /* in */
            IPluginData resourcePluginData, /* in */
            string resourceMethodName,      /* in */
            string resourceName,            /* in */
            bool isolated                   /* in */
            )
            : this(data, scriptFileName, originalText, text, bytes,
                   silent, resourceMethodName, resourceName, isolated)
        {
            this.resourcePluginData = resourcePluginData;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified resource
        /// file name and resource manager.  This constructor delegates to the
        /// private constructor.
        /// </summary>
        /// <param name="data">
        /// The optional client data associated with the script.
        /// </param>
        /// <param name="scriptFileName">
        /// The name of the file the script was read from, if any.
        /// </param>
        /// <param name="originalText">
        /// The original, unmodified text of the script, if any.
        /// </param>
        /// <param name="text">
        /// The (possibly modified) text of the script, if any.
        /// </param>
        /// <param name="bytes">
        /// The raw bytes of the script, if any.
        /// </param>
        /// <param name="silent">
        /// Non-zero if errors encountered while obtaining the script should be
        /// suppressed.
        /// </param>
        /// <param name="resourceFileName">
        /// The name of the file that contains the script resource, if any.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager used to obtain the script resource, if any.
        /// </param>
        /// <param name="resourceMethodName">
        /// The name of the method used to obtain the script resource, if any.
        /// </param>
        /// <param name="resourceName">
        /// The name of the script resource, if any.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if the script resource was obtained in an isolated manner.
        /// </param>
        public GetScriptClientData(
            object data,                     /* in */
            string scriptFileName,           /* in */
            string originalText,             /* in */
            string text,                     /* in */
            ByteList bytes,                  /* in */
            bool silent,                     /* in */
            string resourceFileName,         /* in */
            ResourceManager resourceManager, /* in */
            string resourceMethodName,       /* in */
            string resourceName,             /* in */
            bool isolated                    /* in */
            )
            : this(data, scriptFileName, originalText, text, bytes,
                   silent, resourceMethodName, resourceName, isolated)
        {
            this.resourceFileName = resourceFileName;
            this.resourceManager = resourceManager;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified resource
        /// assembly.  This constructor delegates to the private constructor.
        /// </summary>
        /// <param name="data">
        /// The optional client data associated with the script.
        /// </param>
        /// <param name="scriptFileName">
        /// The name of the file the script was read from, if any.
        /// </param>
        /// <param name="originalText">
        /// The original, unmodified text of the script, if any.
        /// </param>
        /// <param name="text">
        /// The (possibly modified) text of the script, if any.
        /// </param>
        /// <param name="bytes">
        /// The raw bytes of the script, if any.
        /// </param>
        /// <param name="silent">
        /// Non-zero if errors encountered while obtaining the script should be
        /// suppressed.
        /// </param>
        /// <param name="resourceAssembly">
        /// The assembly that contains the script resource, if any.
        /// </param>
        /// <param name="resourceMethodName">
        /// The name of the method used to obtain the script resource, if any.
        /// </param>
        /// <param name="resourceName">
        /// The name of the script resource, if any.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if the script resource was obtained in an isolated manner.
        /// </param>
        public GetScriptClientData(
            object data,               /* in */
            string scriptFileName,     /* in */
            string originalText,       /* in */
            string text,               /* in */
            ByteList bytes,            /* in */
            bool silent,               /* in */
            Assembly resourceAssembly, /* in */
            string resourceMethodName, /* in */
            string resourceName,       /* in */
            bool isolated              /* in */
            )
            : this(data, scriptFileName, originalText, text, bytes,
                   silent, resourceMethodName, resourceName, isolated)
        {
            this.resourceAssembly = resourceAssembly;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
#if DATA
        /// <summary>
        /// Stores the bundle manager associated with the script resource.
        /// </summary>
        private IBundleManager bundleManager;
        /// <summary>
        /// Gets or sets the bundle manager associated with the script
        /// resource.
        /// </summary>
        public IBundleManager BundleManager
        {
            get { return bundleManager; }
            set { bundleManager = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the plugin data for the plugin that contains the script
        /// resource.
        /// </summary>
        private IPluginData resourcePluginData;
        /// <summary>
        /// Gets or sets the plugin data for the plugin that contains the
        /// script resource.
        /// </summary>
        public IPluginData ResourcePluginData
        {
            get { return resourcePluginData; }
            set { resourcePluginData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the file that contains the script resource.
        /// </summary>
        private string resourceFileName;
        /// <summary>
        /// Gets or sets the name of the file that contains the script
        /// resource.
        /// </summary>
        public string ResourceFileName
        {
            get { return resourceFileName; }
            set { resourceFileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the resource manager used to obtain the script resource.
        /// </summary>
        private ResourceManager resourceManager;
        /// <summary>
        /// Gets or sets the resource manager used to obtain the script
        /// resource.
        /// </summary>
        public ResourceManager ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the assembly that contains the script resource.
        /// </summary>
        private Assembly resourceAssembly;
        /// <summary>
        /// Gets or sets the assembly that contains the script resource.
        /// </summary>
        public Assembly ResourceAssembly
        {
            get { return resourceAssembly; }
            set { resourceAssembly = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the method used to obtain the script resource.
        /// </summary>
        private string resourceMethodName;
        /// <summary>
        /// Gets or sets the name of the method used to obtain the script
        /// resource.
        /// </summary>
        public string ResourceMethodName
        {
            get { return resourceMethodName; }
            set { resourceMethodName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the script resource.
        /// </summary>
        private string resourceName;
        /// <summary>
        /// Gets or sets the name of the script resource.
        /// </summary>
        public string ResourceName
        {
            get { return resourceName; }
            set { resourceName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores whether the script resource was obtained in an isolated
        /// manner.
        /// </summary>
        private bool isolated;
        /// <summary>
        /// Gets or sets a value indicating whether the script resource was
        /// obtained in an isolated manner.
        /// </summary>
        public bool Isolated
        {
            get { return isolated; }
            set { isolated = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method builds a list of name/value pairs describing this
        /// object, including the resource origin metadata maintained by this
        /// class in addition to those provided by the base class.
        /// </summary>
        /// <returns>
        /// A list of name/value pairs describing this object.
        /// </returns>
        public override IStringList ToList()
        {
            IStringList list = base.ToList();

#if DATA
            if (bundleManager != null)
            {
                list.Add("BundleManager",
                    bundleManager.ToString());
            }
#endif

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
