/*
 * Plugin.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.IO;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by every plugin that can be loaded into
    /// an Eagle interpreter; it is the primary extension point for
    /// third-party functionality.  It composes the plugin identity and
    /// metadata (<see cref="IPluginData" />), mutable per-plugin state
    /// (<see cref="IState" />), and the request-based execution entry point
    /// (<see cref="IExecuteRequest" />), and adds methods for post-load
    /// initialization, embedded resource access (streams, strings, and URIs),
    /// certificate and key material lookup, and the standard informational
    /// hooks (banner, about, options, and status).  When the
    /// <c>NOTIFY</c>/<c>NOTIFY_OBJECT</c> features are compiled in, it also
    /// composes <see cref="INotify" /> so the plugin can receive interpreter
    /// notifications.  Most plugin authors derive from the default plugin base
    /// class rather than implementing this interface directly.
    /// </summary>
    [ObjectId("3b770696-81da-481f-b9ef-aba55f81d004")]
    public interface IPlugin : IPluginData, IState, IExecuteRequest
#if NOTIFY || NOTIFY_OBJECT
        , INotify
#endif
    {
        /// <summary>
        /// This method is called after the plugin has been loaded and its
        /// initial setup has completed, allowing the plugin to perform any
        /// additional initialization that must happen at that point.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this plugin is being loaded into.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this plugin was created, if any.
        /// This parameter may be null.
        /// </param>
        [Throw(true)]
        void PostInitialize(
            Interpreter interpreter, /* in */
            IClientData clientData   /* in */
            );

        /// <summary>
        /// This method queries framework information associated with the
        /// plugin, selected by an optional identifier and a set of flags.
        /// </summary>
        /// <param name="id">
        /// The optional unique identifier of the framework entry to query.
        /// This parameter may be null to use the default selection.
        /// </param>
        /// <param name="flags">
        /// The flags that control which framework information is returned and
        /// how it is formatted.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the requested framework information.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="result" />.
        /// </returns>
        ReturnCode GetFramework(
            Guid? id,             /* in */
            FrameworkFlags flags, /* in */
            ref Result result     /* out */
            );

        /// <summary>
        /// This method opens a stream over a named resource embedded in, or
        /// otherwise associated with, the plugin (for example, a manifest
        /// resource).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context requesting the resource.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the resource to open.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to resolve a localized variant of the resource,
        /// if applicable.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The opened stream -OR- null if the named resource cannot be found
        /// or opened.
        /// </returns>
        Stream GetStream(
            Interpreter interpreter, /* in */
            string name,             /* in */
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            );

        /// <summary>
        /// This method retrieves the textual content of a named resource
        /// embedded in, or otherwise associated with, the plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context requesting the resource.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the resource to retrieve.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to resolve a localized variant of the resource,
        /// if applicable.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The resource content -OR- null if the named resource cannot be
        /// found or read.
        /// </returns>
        string GetString(
            Interpreter interpreter, /* in */
            string name,             /* in */
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            );

        /// <summary>
        /// This method resolves a named resource into a uniform resource
        /// identifier (for example, the location of a documentation or update
        /// resource provided by the plugin).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context requesting the resource.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the resource to resolve.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to resolve a localized variant of the resource,
        /// if applicable.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The resolved <see cref="Uri" /> -OR- null if the named resource
        /// cannot be resolved.
        /// </returns>
        Uri GetUri(
            Interpreter interpreter, /* in */
            string name,             /* in */
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            );

        /// <summary>
        /// This method returns the file name of the certificate associated
        /// with the named resource, if the plugin exposes one.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context requesting the certificate.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the resource whose certificate file name is wanted.
        /// This parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The certificate file name -OR- null if none is available.
        /// </returns>
        string GetCertificateFileName(
            Interpreter interpreter, /* in */
            string name,             /* in */
            ref Result error         /* out */
            );

        /// <summary>
        /// This method returns the certificate associated with the named
        /// resource, if the plugin exposes one.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context requesting the certificate.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the resource whose certificate is wanted.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The certificate, as an <see cref="IIdentifier" /> -OR- null if
        /// none is available.
        /// </returns>
        IIdentifier GetCertificate(
            Interpreter interpreter, /* in */
            string name,             /* in */
            ref Result error         /* out */
            );

        /// <summary>
        /// This method returns the key pair associated with the named
        /// resource, if the plugin exposes one.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context requesting the key pair.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the resource whose key pair is wanted.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The key pair, as an <see cref="IIdentifier" /> -OR- null if none
        /// is available.
        /// </returns>
        IIdentifier GetKeyPair(
            Interpreter interpreter, /* in */
            string name,             /* in */
            ref Result error         /* out */
            );

        /// <summary>
        /// This method returns the key ring associated with the named
        /// resource, if the plugin exposes one.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context requesting the key ring.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the resource whose key ring is wanted.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The key ring, as an <see cref="IIdentifier" /> -OR- null if none
        /// is available.
        /// </returns>
        IIdentifier GetKeyRing(
            Interpreter interpreter, /* in */
            string name,             /* in */
            ref Result error         /* out */
            );

        /// <summary>
        /// This method produces the plugin's banner text, typically a short
        /// identifying line shown when the plugin is loaded.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context requesting the banner.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the banner text.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="result" />.
        /// </returns>
        ReturnCode Banner(
            Interpreter interpreter, /* in */
            ref Result result        /* out */
            );

        /// <summary>
        /// This method produces the plugin's "about" text, typically a longer
        /// description of the plugin and its authorship.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context requesting the information.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the "about" text.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="result" />.
        /// </returns>
        ReturnCode About(
            Interpreter interpreter, /* in */
            ref Result result        /* out */
            );

        /// <summary>
        /// This method produces a description of the plugin's configurable
        /// options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context requesting the information.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the options description.  Upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="result" />.
        /// </returns>
        ReturnCode Options(
            Interpreter interpreter, /* in */
            ref Result result        /* out */
            );

        /// <summary>
        /// This method produces a description of the plugin's current runtime
        /// status.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context requesting the information.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the status description.  Upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="result" />.
        /// </returns>
        ReturnCode Status(
            Interpreter interpreter, /* in */
            ref Result result        /* out */
            );
    }
}
