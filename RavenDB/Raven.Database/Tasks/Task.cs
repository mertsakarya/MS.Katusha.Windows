//-----------------------------------------------------------------------
// <copyright file="Task.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Raven.Database.Indexing;

namespace Raven.Database.Tasks
{
	public abstract class Task
	{
		public string Index { get; set; }

		public virtual bool SupportsMerging
		{
			get
			{
				return true;
			}
		}

		public abstract bool TryMerge(Task task);
		public abstract void Execute(WorkContext context);

		public byte[] AsBytes()
		{
			var memoryStream = new MemoryStream();
			new JsonSerializer().Serialize(new BsonWriter(memoryStream), this);
			return memoryStream.ToArray();
		}

		public static Task ToTask(string taskType, byte[] task)
		{
			var type = typeof(Task).Assembly.GetType(taskType);
			return (Task) new JsonSerializer().Deserialize(new BsonReader(new MemoryStream(task)), type);
		}

		public abstract Task Clone();
	}
}
