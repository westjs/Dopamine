﻿using Dopamine.Core.Database.Entities;
using Dopamine.Core.Database.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace Dopamine.Common.Services.Indexing
{
    public class FolderWatcherManager
    {
        #region Variables
        private IFolderRepository folderRepository;
        private List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private Timer folderWatcherTimer;
        #endregion

        #region Events
        public event EventHandler FoldersChanged = delegate { };
        #endregion

        #region Construction
        public FolderWatcherManager(IFolderRepository folderRepository)
        {
            this.folderRepository = folderRepository;
            folderWatcherTimer = new Timer(2000);
            folderWatcherTimer.Elapsed += FolderWatcherTimer_Elapsed;
        }
        #endregion

        #region Private
        private void FolderWatcherTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.StopFolderWatcherTimer();
            this.FoldersChanged(this, new EventArgs());
        }

        private void ResetFolderWatcherTimer()
        {
            if (this.folderWatcherTimer.Enabled)
            {
                this.folderWatcherTimer.Stop();
            }

            this.folderWatcherTimer.Start();
        }

        private void StopFolderWatcherTimer()
        {
            if (this.folderWatcherTimer.Enabled)
            {
                this.folderWatcherTimer.Stop();
            }
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            this.ResetFolderWatcherTimer();
        }
        #endregion

        #region Public
        public async Task StartWatchingAsync()
        {
            await this.StopWatchingAsync();

            List<Folder> folders = await this.folderRepository.GetFoldersAsync();

            foreach (Folder fol in folders)
            {
                var watcher = new FileSystemWatcher(fol.Path) { EnableRaisingEvents = true, IncludeSubdirectories = true };

                // Changed event should be raised when files are created/renamed/deleted, including for sub folders.
                watcher.Changed += Watcher_Changed;
                this.watchers.Add(watcher);
            }

            this.ResetFolderWatcherTimer();
        }

        public async Task StopWatchingAsync()
        {
            this.StopFolderWatcherTimer();

            if(this.watchers.Count == 0)
            {
                return;
            }

            await Task.Run(() =>
            {
                for (int i = this.watchers.Count - 1; i >= 0; i--)
                {
                    this.watchers[i].Changed -= Watcher_Changed;
                    this.watchers[i].Dispose();
                    this.watchers.RemoveAt(i);
                }
            });
        }
        #endregion
    }
}