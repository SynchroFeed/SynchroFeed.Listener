#region header
// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Robert Vandehey" file="FeedEvent.cs">
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
namespace SynchroFeed.Listener.Model
{
    /// <summary>
    /// The types of events that get raised by the webhook.
    /// </summary>
    public class EventType
    {
        /// <summary>
        /// The package was added to the feed.
        /// </summary>
        public const string Added = "added";

        /// <summary>
        /// The package was deleted from the feed.
        /// </summary>
        public const string Deleted = "deleted";

        /// <summary>
        /// The package was deployed to an environment.
        /// </summary>
        public const string Deployed = "deployed";

        /// <summary>
        /// The package was promoted to the feed.
        /// </summary>
        public const string Promoted = "promoted";

        /// <summary>
        /// The package was purged from the feed through automated deletion rules
        /// </summary>
        public const string Purged = "purged";

        /// <summary>
        /// The package is being processed
        /// </summary>
        public const string Process = "process";
    }

    /// <summary>
    /// The FeedEvent class contains the properties related to an event raised by a feed.
    /// </summary>
    public class FeedEvent
    {
        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        /// <value>The type of event.</value>
        public string Event { get; set; }

        /// <summary>
        /// Gets or sets the feed that raised this event.
        /// </summary>
        /// <value>The feed that raised the event.</value>
        public string Feed { get; set; }

        /// <summary>
        /// Gets or sets the hash of the bytes of this package.
        /// </summary>
        /// <value>The hash of the bytes of this package.</value>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets the package that is associated with this event.
        /// </summary>
        /// <value>The package that is associated with this event.</value>
        public string Package { get; set; }

        /// <summary>
        /// Gets or sets the type of the package.
        /// </summary>
        /// <value>The type of the package.</value>
        public string PackageType { get; set; }

        /// <summary>
        /// Gets or sets the package URL.
        /// </summary>
        /// <value>The package URL.</value>
        public string PackageUrl { get; set; }

        /// <summary>
        /// Gets or sets the user that generated this event.
        /// </summary>
        /// <value>The user that generated this event.</value>
        public string User { get; set; }

        /// <summary>
        /// Gets or sets the version associated with the package that raised this event.
        /// </summary>
        /// <value>The version associated with the package that raised this event.</value>
        public string Version { get; set; }
    }
}