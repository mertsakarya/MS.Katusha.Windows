using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Raven.Abstractions.Extensions;
using Raven.Abstractions.MEF;
using Raven.Database.Extensions;
using Raven.Database.Impl;
using Raven.Database.Plugins;
using Raven.Database.Server.Abstractions;

namespace Raven.Database.Server.Responders
{
	public class SilverlightEnsuresStartup : RequestResponder
	{
		[ImportMany]
		public OrderedPartCollection<ISilverlightRequestedAware> SilverlightRequestedAware { get; set; }


		public override string UrlPattern
		{
			get { return @"^/silverlight/ensureStartup$"; }
		}

		public override string[] SupportedVerbs
		{
			get { return new[]{"GET"}; }
		}

		public override void Respond(IHttpContext context)
		{
			ResourceStore.ExtensionsState.GetOrAddAtomically("SilverlightUI.NotifiedAboutSilverlightBeingRequested", s =>
			{
				var skipCreatingStudioIndexes = ResourceStore.Configuration.Settings["Raven/SkipCreatingStudioIndexes"];
				if (string.IsNullOrEmpty(skipCreatingStudioIndexes) == false && 
					"true".Equals(skipCreatingStudioIndexes, StringComparison.InvariantCultureIgnoreCase))
					return true;

				foreach (var silverlightRequestedAware in SilverlightRequestedAware)
				{
					silverlightRequestedAware.Value.SilverlightWasRequested(ResourceStore);
				}
				return true;
			});

			context.WriteJson(new {ok = true});
		}
	}
}