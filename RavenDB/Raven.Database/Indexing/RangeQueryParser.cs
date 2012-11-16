//-----------------------------------------------------------------------
// <copyright file="RangeQueryParser.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Raven.Abstractions.Indexing;
using SpellChecker.Net.Search.Spell;
using Version = Lucene.Net.Util.Version;

namespace Raven.Database.Indexing
{
	
	public class RangeQueryParser : QueryParser
	{
		static readonly Regex rangeValue = new Regex(@"^[\w\d]x[-\w\d.]+$", RegexOptions.Compiled);

		private Dictionary<string,HashSet<string>> untokenized = new Dictionary<string, HashSet<string>>();

		public RangeQueryParser(Version matchVersion, string f, Analyzer a)
			: base(matchVersion, f, a)
		{
		}

		public override Query GetFieldQuery(string field, string queryText)
		{
			HashSet<string> set;
			if(untokenized.TryGetValue(field, out set))
			{
				if (set.Contains(queryText))
					return new TermQuery(new Term(field, queryText));
			}

			var fieldQuery = base.GetFieldQuery(field, queryText);
			if (fieldQuery is TermQuery
				&& queryText.EndsWith("*")
				&& !queryText.EndsWith(@"\*")
				&& queryText.Contains(" "))
			{ 
				var analyzer = GetAnalyzer();
				var tokenStream = analyzer.ReusableTokenStream(field, new StringReader(queryText.Substring(0, queryText.Length-1)));
				var sb = new StringBuilder();
				while (tokenStream.IncrementToken())
				{
					var attribute = (TermAttribute)tokenStream.GetAttribute(typeof(TermAttribute));
					if (sb.Length != 0)
						sb.Append(' ');
					sb.Append(attribute.Term());
				}
				var prefix = new Term(field, sb.ToString());
				return new PrefixQuery(prefix);
			}
			return fieldQuery;
		}

		/// <summary>
		/// Detects numeric range terms and expands range expressions accordingly
		/// </summary>
		/// <param name="field"></param>
		/// <param name="lower"></param>
		/// <param name="upper"></param>
		/// <param name="inclusive"></param>
		/// <returns></returns>
		protected override Query GetRangeQuery(string field, string lower, string upper, bool inclusive)
		{
			if (lower == "NULL" || lower == "*")
				lower = null;
			if (upper == "NULL" || upper == "*")
				upper = null;

			if ( (lower == null || !rangeValue.IsMatch(lower)) && (upper == null || !rangeValue.IsMatch(upper)))
			{
				return NewRangeQuery(field, lower, upper, inclusive);
			}

			var from = NumberUtil.StringToNumber(lower);
			var to = NumberUtil.StringToNumber(upper);

			TypeCode numericType;

			if (from != null)
				numericType = Type.GetTypeCode(from.GetType());
			else if (to != null)
				numericType = Type.GetTypeCode(to.GetType());
			else
				numericType = TypeCode.Empty;

			switch (numericType)
			{
				case TypeCode.Int32:
				{
					return NumericRangeQuery.NewIntRange(field, (int)(from ?? Int32.MinValue), (int)(to ?? Int32.MaxValue), inclusive, inclusive);
				}
				case TypeCode.Int64:
				{
					return NumericRangeQuery.NewLongRange(field, (long)(from ?? Int64.MinValue), (long)(to ?? Int64.MaxValue), inclusive, inclusive);
				}
				case TypeCode.Double:
				{
					return NumericRangeQuery.NewDoubleRange(field, (double)(from ?? Double.MinValue), (double)(to ?? Double.MaxValue), inclusive, inclusive);
				}
				case TypeCode.Single:
				{
					return NumericRangeQuery.NewFloatRange(field, (float)(from ?? Single.MinValue), (float)(to ?? Single.MaxValue), inclusive, inclusive);
				}
				default:
				{
					return NewRangeQuery(field, lower, upper, inclusive);
				}
			}
		}

		public void SetUntokenized(string field, string value)
		{
			HashSet<string> set;
			if(untokenized.TryGetValue(field,out set) == false)
			{
				untokenized[field] = set = new HashSet<string>();
			}
			set.Add(value);
		}
	}
}
