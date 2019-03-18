#region header
// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Robert Vandehey" file="AwsCredentials.cs">
// MIT License
// 
// Copyright(c) 2018 Robert Vandehey
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion
using System;

namespace SynchroFeed.Listener.Settings
{
    /// <summary>
    /// An enumeration for the Amazon CredentialsType
    /// </summary>
    public enum AwsCredentialsType
    {
        /// <summary>
        /// The credential is an Amazon Basic credential
        /// </summary>
        Basic
    }

    /// <summary>
    /// The AwsCredentials class encapsulates the AWS credentials.
    /// </summary>
    public class AwsCredentials
    {
        /// <summary>
        /// Gets or sets the type of the credentials.
        /// </summary>
        /// <value>The type of the credentials.</value>
        public AwsCredentialsType CredentialsType { get; set; } = AwsCredentialsType.Basic;

        /// <summary>
        /// Gets or sets the AWS credentials access key.
        /// </summary>
        /// <value>The AWS credentials access key.</value>
        public string AccessKey { get; set; } = "";

        /// <summary>
        /// Gets or sets the AWS credentials secret key.
        /// </summary>
        /// <value>The AWS credentials secret key.</value>
        public string SecretKey { get; set; } = "";
    }
}
