//-----------------------------------------------------------------------
// <copyright file="AbstractRequestResponder.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Raven.Abstractions.Data;
using Raven.Database.Config;
using Raven.Database.Server.Abstractions;

namespace Raven.Database.Server
{
	[InheritedExport]
	public abstract class AbstractRequestResponder
	{
		private readonly string[] supportedVerbsCached;
		protected readonly Regex urlMatcher;
		private Func<InMemoryRavenConfiguration> settings;
		private Func<DocumentDatabase> database;
		protected HttpServer server;
		private Func<string> tenantId;

		protected AbstractRequestResponder()
		{
			urlMatcher = new Regex(UrlPattern);
			supportedVerbsCached = SupportedVerbs;
		}

		public abstract string UrlPattern { get; }
		public abstract string[] SupportedVerbs { get; }

		public DocumentDatabase DefaultResourceStore { get { return server.DefaultResourceStore; } }
		public DocumentDatabase ResourceStore { get { return database(); } }
		public InMemoryRavenConfiguration Settings { get { return settings(); } }
		public string TenantId { get { return tenantId(); } }

		public virtual bool IsUserInterfaceRequest { get { return false; } }

		public void Initialize(Func<DocumentDatabase> databaseGetter, Func<InMemoryRavenConfiguration> settingsGetter, Func<string> tenantIdGetter, HttpServer theServer)
		{
			server = theServer;
			database = databaseGetter;
			settings = settingsGetter;
			tenantId = tenantIdGetter;
		}

		public bool WillRespond(IHttpContext context)
		{
			var requestUrl = context.GetRequestUrl();
			var match = urlMatcher.Match(requestUrl);
			return match.Success && supportedVerbsCached.Contains(context.Request.HttpMethod);
		}

		public abstract void Respond(IHttpContext context);

		protected TransactionInformation GetRequestTransaction(IHttpContext context)
		{
			var txInfo = context.Request.Headers["Raven-Transaction-Information"];
			if (string.IsNullOrEmpty(txInfo))
				return null;
			var parts = txInfo.Split(new[]{", "}, StringSplitOptions.RemoveEmptyEntries);
			if(parts.Length != 2)
				throw new ArgumentException("'Raven-Transaction-Information' is in invalid format, expected format is: 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx, hh:mm:ss'");
			return new TransactionInformation
			{
				Id = new Guid(parts[0]),
				Timeout = TimeSpan.ParseExact(parts[1], "c", CultureInfo.InvariantCulture)
			};
		}
	}
}
