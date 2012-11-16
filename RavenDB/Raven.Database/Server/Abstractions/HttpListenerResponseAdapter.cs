//-----------------------------------------------------------------------
// <copyright file="HttpListenerResponseAdapter.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace Raven.Database.Server.Abstractions
{
	public class HttpListenerResponseAdapter : IHttpResponse
	{
		private readonly HttpListenerResponse response;

		public HttpListenerResponseAdapter(HttpListenerResponse response)
		{
			this.response = response;
			OutputStream = response.OutputStream;
		}

		public string RedirectionPrefix
		{
			get;
			set;
		}

		public void AddHeader(string name, string value)
		{
			if (name == "ETag" && string.IsNullOrEmpty(response.Headers["Cache-Control"]))
				response.AddHeader("Expires", "Sat, 01 Jan 2000 00:00:00 GMT");
			response.AddHeader(name, value);
		}

		public Stream OutputStream { get; set; }

		public long ContentLength64
		{
			get { return response.ContentLength64; }
			set { response.ContentLength64 = value; }
		}

		public int StatusCode
		{
			get { return response.StatusCode; }
			set { response.StatusCode = value; }
		}

		public string StatusDescription
		{
			get { return response.StatusDescription; }
			set { response.StatusDescription = value; }
		}

		public string ContentType
		{
			get { return response.ContentType; }
			set { response.ContentType = value; }
		}

		public void Redirect(string url)
		{
			response.Redirect(RedirectionPrefix + url);
		}

		public void Close()
		{
			OutputStream.Dispose();
			response.Close();
		}

		public void WriteFile(string path)
		{
			using(var file = File.OpenRead(path))
			{
				file.CopyTo(OutputStream);
			}
		}

		public NameValueCollection GetHeaders()
		{
			return response.Headers;
		}

		public void SetPublicCachability()
		{
			response.Headers["Cache-Control"] = "Public";
		}
	}
}
