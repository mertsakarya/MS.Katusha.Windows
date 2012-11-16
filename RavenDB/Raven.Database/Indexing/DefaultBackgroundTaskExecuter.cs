﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Raven.Database.Config;
using Raven.Abstractions.Extensions;
using Raven.Database.Util;

namespace Raven.Database.Indexing
{
	public class DefaultBackgroundTaskExecuter : IBackgroundTaskExecuter
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public IList<TResult> Apply<T, TResult>(IEnumerable<T> source, Func<T, TResult> func)
			where TResult : class
		{
			return source.AsParallel()
				.Select(func)
				.Where(x => x != null)
				.ToList();
		}

		private readonly ConcurrentDictionary<TimeSpan, Tuple<Timer, ConcurrentSet<IRepeatedAction>>> timers =
			new ConcurrentDictionary<TimeSpan, Tuple<Timer, ConcurrentSet<IRepeatedAction>>>();

		public void Repeat(IRepeatedAction action)
		{
			var tuple = timers.GetOrAddAtomically(action.RepeatDuration,
			                                      span =>
			                                      {
			                                      	var repeatedActions = new ConcurrentSet<IRepeatedAction>
			                                      	{
			                                      		action
			                                      	};
			                                      	var timer = new Timer(ExecuteTimer, action.RepeatDuration,
			                                      	                      action.RepeatDuration,
			                                      	                      action.RepeatDuration);
			                                      	return Tuple.Create(timer, repeatedActions);
			                                      });
			tuple.Item2.TryAdd(action);
		}

		private void ExecuteTimer(object state)
		{
			var span = (TimeSpan) state;
			Tuple<Timer, ConcurrentSet<IRepeatedAction>> tuple;
			if (timers.TryGetValue(span, out tuple) == false)
				return;

			foreach (var repeatedAction in tuple.Item2)
			{
				if (repeatedAction.IsValid == false)
					tuple.Item2.TryRemove(repeatedAction);

				try
				{
					repeatedAction.Execute();
				}
				catch (Exception e)
				{
					logger.ErrorException("Could not execute repeated task", e);
				}
			}

			if (tuple.Item2.Count != 0) 
				return;

			if (timers.TryRemove(span, out tuple) == false)
				return;

			tuple.Item1.Dispose();
		}

		/// <summary>
		/// Note that we assume that source is a relatively small number, expected to be 
		/// the number of indexes, not the number of documents.
		/// </summary>
		public void ExecuteAll<T>(InMemoryRavenConfiguration configuration, TaskScheduler scheduler, IList<T> source, Action<T, long> action)
		{
			if(configuration.MaxNumberOfParallelIndexTasks == 1)
			{
				long i = 0;
				foreach (var item in source)
				{
					action(item, i++);
				}
				return;
			}

			var partitioneds = Partition(source, configuration.MaxNumberOfParallelIndexTasks).ToList();
			int start = 0;
			foreach (var partitioned in partitioneds)
			{
				var currentStart = start;
				Parallel.ForEach(partitioned, new ParallelOptions
				{
					TaskScheduler = scheduler,
					MaxDegreeOfParallelism = configuration.MaxNumberOfParallelIndexTasks
				},(item,_,index)=>action(item, currentStart + index));
				start += partitioned.Count;
			}
		}

		static IEnumerable<IList<T>> Partition<T>(IList<T> source, int size)
		{
			for (int i = 0; i < source.Count; i+=size)
			{
				yield return source.Skip(i).Take(size).ToList();
			}
		}
	}
}