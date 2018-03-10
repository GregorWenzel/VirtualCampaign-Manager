// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Uri.cs" company="Believe">
//   WhenYouBelieve2014@gmail.com
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;
using System.Text;
using System.Web;

namespace UriCombine
{
    /// <summary>
    ///     The uri extension.
    /// </summary>
    public class Uri : System.Uri
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Uri"/> class.
        /// </summary>
        /// <param name="uriString">
        /// The uri string.
        /// </param>
        public Uri(string uriString)
            : base(uriString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Uri"/> class.
        /// </summary>
        /// <param name="uriString">
        /// The uri string.
        /// </param>
        /// <param name="dontEscape">
        /// The dont escape.
        /// </param>
        /// <remarks>
        /// Uri(string, bool)
        ///     Uri constructor. Assumes that input string is canonically escaped
        /// </remarks>
        [Obsolete(
            "The constructor has been deprecated. Please use new Uri(string). The dontEscape parameter is deprecated and is always false. http://go.microsoft.com/fwlink/?linkid=14202"
            )]

        public Uri(string uriString, bool dontEscape) : base(uriString, dontEscape)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Uri"/> class.
        /// </summary>
        /// <param name="baseUri">
        /// The base uri.
        /// </param>
        /// <param name="relativeUri">
        /// The relative uri.
        /// </param>
        /// <param name="dontEscape">
        /// The dont escape.
        /// </param>
        /// <remarks>
        /// Uri(Uri, string, bool)
        ///     Uri combinatorial constructor. Do not perform character escaping if
        ///     DontEscape is true
        /// </remarks>
        [Obsolete(
            "The constructor has been deprecated. Please new Uri(Uri, string). The dontEscape parameter is deprecated and is always false. http://go.microsoft.com/fwlink/?linkid=14202"
            )]

        public Uri(Uri baseUri, string relativeUri, bool dontEscape) : base(baseUri, relativeUri, dontEscape)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Uri"/> class.
        /// </summary>
        /// <param name="uriString">
        /// The uri string.
        /// </param>
        /// <param name="uriKind">
        /// The uri kind.
        /// </param>
        /// <remarks>
        /// Uri(string, UriKind);
        /// </remarks>
        public Uri(string uriString, UriKind uriKind)
            : base(uriString, uriKind)
        {
// ReSharper restore AssignNullToNotNullAttribute
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Uri"/> class.
        /// </summary>
        /// <param name="baseUri">
        /// The base uri.
        /// </param>
        /// <param name="relativeUri">
        /// The relative uri.
        /// </param>
        /// <remarks>
        /// Uri(Uri, string)
        ///     Construct a new Uri from a base and relative URI. The relative URI may
        ///     also be an absolute URI, in which case the resultant URI is constructed
        ///     entirely from it
        /// </remarks>
        public Uri(Uri baseUri, string relativeUri)

            : base(baseUri, relativeUri)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Uri"/> class.
        /// </summary>
        /// <param name="baseUri">
        /// The base uri.
        /// </param>
        /// <param name="relativeUri">
        /// The relative uri.
        /// </param>
        /// <remarks>
        /// Uri(Uri , Uri )
        ///     Note: a static Create() method should be used by users, not this .ctor
        /// </remarks>
        public Uri(Uri baseUri, Uri relativeUri)
            : base(baseUri, relativeUri)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Uri"/> class.
        /// </summary>
        /// <param name="serializationInfo">
        /// The serialization info.
        /// </param>
        /// <param name="streamingContext">
        /// The streaming context.
        /// </param>
        /// <remarks>
        /// ISerializable constructor
        /// </remarks>
        protected Uri(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        /// <summary>
        /// The combine.
        /// </summary>
        /// <param name="parts">
        /// The parts.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string Combine(params string[] parts)
        {
            if (parts == null || parts.Length == 0) return string.Empty;

            var urlBuilder = new StringBuilder();
            foreach (var part in parts)
            {
                var tempUrl = tryCreateRelativeOrAbsolute(part);
                urlBuilder.Append(tempUrl);
            }
            return VirtualPathUtility.RemoveTrailingSlash(urlBuilder.ToString());
        }

        private static string tryCreateRelativeOrAbsolute(string s)
        {
            System.Uri uri;
            TryCreate(s, UriKind.RelativeOrAbsolute, out uri);
            string tempUrl = VirtualPathUtility.AppendTrailingSlash(uri.ToString());
            return tempUrl;
        }
    }
}