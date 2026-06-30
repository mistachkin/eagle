/*
 * SecretData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface describes a unit of secret data together with the
    /// operation used to process it, exposing the input, auxiliary, output,
    /// and signature values (as either strings or byte lists), the flags
    /// that control processing, and indicators of which values are present.
    /// It extends <see cref="ISynchronizeBase" />.
    /// </summary>
    [ObjectId("767b3968-685a-408b-bf3d-e86590b3dd58")]
    public interface ISecretData : ISynchronizeBase
    {
        /// <summary>
        /// Gets or sets the flags that control how this secret data is
        /// processed.
        /// </summary>
        SecretDataFlags Flags { get; set; }

        /// <summary>
        /// Gets a value indicating whether an input value is present.
        /// </summary>
        bool HaveInput { get; }
        /// <summary>
        /// Gets a value indicating whether an auxiliary value is present.
        /// </summary>
        bool HaveAuxiliary { get; }
        /// <summary>
        /// Gets a value indicating whether an output value is present.
        /// </summary>
        bool HaveOutput { get; }
        /// <summary>
        /// Gets a value indicating whether a signature value is present.
        /// </summary>
        bool HaveSignature { get; }

        /// <summary>
        /// Gets or sets the input value as a string.
        /// </summary>
        string InputString { get; set; }
        /// <summary>
        /// Gets or sets the input value as a list of bytes.
        /// </summary>
        ByteList InputBytes { get; set; }

        /// <summary>
        /// Gets or sets the auxiliary value as a string.
        /// </summary>
        string AuxiliaryString { get; set; }
        /// <summary>
        /// Gets or sets the auxiliary value as a list of bytes.
        /// </summary>
        ByteList AuxiliaryBytes { get; set; }

        /// <summary>
        /// Gets or sets the output value as a string.
        /// </summary>
        string OutputString { get; set; }
        /// <summary>
        /// Gets or sets the output value as a list of bytes.
        /// </summary>
        ByteList OutputBytes { get; set; }

        /// <summary>
        /// Gets or sets the signature value as a string.
        /// </summary>
        string SignatureString { get; set; }
        /// <summary>
        /// Gets or sets the signature value as a list of bytes.
        /// </summary>
        ByteList SignatureBytes { get; set; }

        /// <summary>
        /// Performs the processing operation associated with this secret data,
        /// transforming the input into the output and/or signature values.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Process(ref Result error);
    }
}
