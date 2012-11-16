//-----------------------------------------------------------------------
// <copyright file="SimpleQueryParser.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Raven.Abstractions.Data;

namespace Raven.Database.Indexing
{
	public class SimpleQueryParser
	{
		static readonly Regex QueryTerms = new Regex(@"([^\s\(\+\-][\w._,]+)\:", RegexOptions.Compiled);

		static readonly Regex DynamicQueryTerms = new Regex(@"[-+]?([^\(\)\s]*[^\\\s])\:", RegexOptions.Compiled);

		public static HashSet<string> GetFields(IndexQuery query)
		{
			return GetFieldsInternal(query, QueryTerms);
		}

		public static HashSet<Tuple<string,string>> GetFieldsForDynamicQuery(IndexQuery query)
		{
			var results = new HashSet<Tuple<string,string>>();
			foreach (var result in GetFieldsInternal(query, DynamicQueryTerms))
			{
				if(result == "*")
					continue;
				results.Add(Tuple.Create(TranslateField(result), result));
			}
			
			return results;
		}
		private static HashSet<string> GetFieldsInternal(IndexQuery query, Regex queryTerms)
		{
			var fields = new HashSet<string>();
			if (string.IsNullOrEmpty(query.DefaultField) == false)
			{
				fields.Add(query.DefaultField);
			}
			if(query.Query == null)
				return fields;
			var queryTermMatches = queryTerms.Matches(query.Query);
			for (int x = 0; x < queryTermMatches.Count; x++)
			{
				Match match = queryTermMatches[x];
				String field = match.Groups[1].Value;

				fields.Add(field);
			}
			return fields;
		}

		private static string TranslateField(string field)
		{
			var fieldParts = field.Split(new[]{"."}, StringSplitOptions.RemoveEmptyEntries);

			var result = new StringBuilder();
			foreach (var fieldPart in fieldParts)
			{
				if ((char.IsLetter(fieldPart[0]) == false && fieldPart[0] != '_') || 
					fieldPart.Any(c => char.IsLetterOrDigit(c) == false && c != '_' 
						&& c != ',' /* we allow the comma operator for collections */))
				{
					result.Append("[\"").Append(fieldPart).Append("\"]");
				}
				else
				{
					if (result.Length > 0)
						result.Append('.');

					result
						.Append(fieldPart);
				}
			}
			return result.ToString();
		}
	}
}