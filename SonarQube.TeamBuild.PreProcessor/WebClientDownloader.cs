﻿//-----------------------------------------------------------------------
// <copyright file="WebClientDownloader.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;
using System.Net;

namespace SonarQube.TeamBuild.PreProcessor
{
    public class WebClientDownloader : IDownloader
    {
        private readonly WebClient client;

        public WebClientDownloader(string username, string password)
        {
            // SONARMSBRU-169 Support TLS versions 1.0, 1.1 and 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            this.client = new WebClient();
            if (username != null && password != null)
            {
                if (username.Contains(':'))
                {
                    throw new ArgumentException("username cannot contain the ':' character due to basic authentication limitations");
                }
                if (!IsAscii(username) || !IsAscii(password))
                {
                    throw new ArgumentException("username and password should contain only ASCII characters due to basic authentication limitations");
                }

                var credentials = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}", username, password);
                credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
                client.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
            }
        }

        public string GetHeader(HttpRequestHeader header)
        {
            return this.client.Headers[header];
        }
        
        private static bool IsAscii(string s)
        {
            return !s.Any(c => c > sbyte.MaxValue);
        }

        public bool TryDownloadIfExists(string url, out string contents)
        {
            try
            {
                contents = client.DownloadString(url);
                return true;
            }
            catch (WebException e)
            {
                var response = e.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    contents = null;
                    return false;
                }

                throw;
            }
        }

        public string Download(string url)
        {
            return client.DownloadString(url);
        }

        #region IDisposable implementation

        private bool disposed;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing)
            {
                if (this.client != null)
                {
                    this.client.Dispose();
                }
            }

            this.disposed = true;
        }

        #endregion
    }
}
