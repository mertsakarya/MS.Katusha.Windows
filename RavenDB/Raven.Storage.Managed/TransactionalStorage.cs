//-----------------------------------------------------------------------
// <copyright file="TransactionalStorage.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NLog;
using Newtonsoft.Json.Linq;
using Raven.Abstractions;
using Raven.Abstractions.Data;
using Raven.Abstractions.MEF;
using Raven.Database;
using Raven.Database.Config;
using Raven.Database.Impl;
using Raven.Database.Plugins;
using Raven.Database.Storage;
using Raven.Munin;
using Raven.Storage.Managed.Backup;
using Raven.Storage.Managed.Impl;

namespace Raven.Storage.Managed
{
	public class TransactionalStorage : ITransactionalStorage
	{
		private readonly ThreadLocal<IStorageActionsAccessor> current = new ThreadLocal<IStorageActionsAccessor>();

		private readonly InMemoryRavenConfiguration configuration;
		private readonly Action onCommit;
		private TableStorage tableStroage;

		public TableStorage TableStroage
		{
			get { return tableStroage; }
		}

		private IPersistentSource persistenceSource;
		private volatile bool disposed;
		private readonly ReaderWriterLockSlim disposerLock = new ReaderWriterLockSlim();
		private Timer idleTimer;
		private long lastUsageTime;
		private IUuidGenerator uuidGenerator;
		private readonly IDocumentCacher documentCacher;

		public IPersistentSource PersistenceSource
		{
			get { return persistenceSource; }
		}

		[ImportMany]
		public OrderedPartCollection<AbstractDocumentCodec> DocumentCodecs { get; set; }

		public TransactionalStorage(InMemoryRavenConfiguration configuration, Action onCommit)
		{
			this.configuration = configuration;
			this.onCommit = onCommit;
			documentCacher = new DocumentCacher(configuration);
		}

		public void Dispose()
		{
			disposerLock.EnterWriteLock();
			try
			{
				if (disposed)
					return;
				disposed = true;
				current.Dispose();
				if (documentCacher != null)
					documentCacher.Dispose();
				if (idleTimer != null)
					idleTimer.Dispose();
				if (persistenceSource != null)
					persistenceSource.Dispose();
				if(tableStroage != null)
					tableStroage.Dispose();
			}
			finally
			{
				disposerLock.ExitWriteLock();
			}
		}

		public Guid Id
		{
			get; private set;
		}

		public void Batch(Action<IStorageActionsAccessor> action)
		{
			if (disposerLock.IsReadLockHeld) // we are currently in a nested Batch call
			{
				if (current.Value != null) // check again, just to be sure
				{
					action(current.Value);
					return;
				}
			}
		    disposerLock.EnterReadLock();
			try
			{
				if (disposed)
				{
					Trace.WriteLine("TransactionalStorage.Batch was called after it was disposed, call was ignored.");
					return; // this may happen if someone is calling us from the finalizer thread, so we can't even throw on that
				}

				ExecuteBatch(action);
			}
			finally
			{
				disposerLock.ExitReadLock();
				if (disposed == false)
					current.Value = null;
			}
			onCommit(); // call user code after we exit the lock
		}

		[DebuggerHidden, DebuggerNonUserCode, DebuggerStepThrough]
		private void ExecuteBatch(Action<IStorageActionsAccessor> action)
		{
			Interlocked.Exchange(ref lastUsageTime, SystemTime.Now.ToBinary());
			using (tableStroage.BeginTransaction())
			{
				var storageActionsAccessor = new StorageActionsAccessor(tableStroage, uuidGenerator, DocumentCodecs, documentCacher);
				current.Value = storageActionsAccessor;
				action(current.Value);
				storageActionsAccessor.SaveAllTasks();
				tableStroage.Commit();
				storageActionsAccessor.InvokeOnCommit();
			}
		}

		public void ExecuteImmediatelyOrRegisterForSyncronization(Action action)
		{
			if (current.Value == null)
			{
				action();
				return;
			}
			current.Value.OnCommit += action;
		}

		public bool Initialize(IUuidGenerator generator)
		{
			uuidGenerator = generator;
			if (configuration.RunInMemory  == false && Directory.Exists(configuration.DataDirectory) == false)
				Directory.CreateDirectory(configuration.DataDirectory);

			persistenceSource = configuration.RunInMemory
						  ? (IPersistentSource)new MemoryPersistentSource()
						  : new FileBasedPersistentSource(configuration.DataDirectory, "Raven", configuration.TransactionMode == TransactionMode.Safe);

			tableStroage = new TableStorage(persistenceSource);

			idleTimer = new Timer(MaybeOnIdle, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

			tableStroage.Initialze();

			if(persistenceSource.CreatedNew)
			{
				Id = Guid.NewGuid();
				Batch(accessor => tableStroage.Details.Put("id", Id.ToByteArray()));
			}
			else
			{
				var readResult = tableStroage.Details.Read("id");
				Id = new Guid(readResult.Data());
			}

			return persistenceSource.CreatedNew;
		}

		public void StartBackupOperation(DocumentDatabase database, string backupDestinationDirectory, bool incrementalBackup)
		{
			var backupOperation = new BackupOperation(database, persistenceSource, database.Configuration.DataDirectory, backupDestinationDirectory);
			ThreadPool.QueueUserWorkItem(backupOperation.Execute);
		
		}

		public void Restore(string backupLocation, string databaseLocation)
		{
			new RestoreOperation(backupLocation, databaseLocation).Execute();
		}

		public long GetDatabaseSizeInBytes()
		{
			return PersistenceSource.Read(stream => stream.Length);
		}

		public string FriendlyName
		{
			get { return "Munin"; }
		}

		public bool HandleException(Exception exception)
		{
			return false;
		}

		private void MaybeOnIdle(object _)
		{
			var ticks = Interlocked.Read(ref lastUsageTime);
			var lastUsage = DateTime.FromBinary(ticks);
			if ((SystemTime.Now - lastUsage).TotalSeconds < 30)
				return;

			tableStroage.PerformIdleTasks();
		}

		public void EnsureCapacity(int value)
		{
			persistenceSource.EnsureCapacity(value);
		}
	}
}