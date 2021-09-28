﻿using Backend;
using Backend.Entities;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SpotifySongTagger.ViewModels
{
    public class TagEditorViewModel : BaseViewModel
    {
        #region track
        private bool isLoadingTracks;
        public bool IsLoadingTracks
        {
            get => isLoadingTracks;
            set => SetProperty(ref isLoadingTracks, value, nameof(IsLoadingTracks));
        }

        private TrackViewModel selectedTrackVM;
        public TrackViewModel SelectedTrackVM
        {
            get => selectedTrackVM;
            set => SetProperty(ref selectedTrackVM, value, nameof(SelectedTrackVM));
        }
        public ObservableCollection<TrackViewModel> TrackVMs { get; } = new ObservableCollection<TrackViewModel>();
        private async Task LoadTracks(string playlistId, ListBox sender, Func<string, Task<List<Track>>> getTracksFunc)
        {
            IsLoadingTracks = true;
            TrackVMs.Clear();
            var tracks = await getTracksFunc(playlistId);

            // check if the playlist is still selected
            var selectedPlaylist = sender.SelectedItem as Playlist;
            if (selectedPlaylist.Id == playlistId)
            {
                foreach (var track in tracks)
                    TrackVMs.Add(new TrackViewModel(track));
                IsLoadingTracks = false;
            }
        }
        public async Task LoadPlaylistTracks(string playlistId, ListBox sender) => await LoadTracks(playlistId, sender, id => DatabaseOperations.PlaylistTracks(id));
        public async Task LoadGeneratedTracks(string playlistId, ListBox sender) => await LoadTracks(playlistId, sender, id => DatabaseOperations.GeneratedPlaylistTracks(id));
        public void UpdatePlayingTrack(string newId)
        {
            foreach (var trackVM in TrackVMs)
                trackVM.IsPlaying = trackVM.Track.Id == newId;
        }
        #endregion

        private string newTagName;
        public string NewTagName
        {
            get => newTagName;
            set
            {
                SetProperty(ref newTagName, value, nameof(NewTagName));
                NotifyPropertyChanged(nameof(CanAddTag));
                NotifyPropertyChanged(nameof(CanEditTag));
            }
        }
        public Tag ClickedTag { get; set; }



        public static void AssignTag(Track track, string tag) => DatabaseOperations.AssignTag(track, tag);
        public void RemoveAssignment(Tag tag) => DatabaseOperations.RemoveAssignment(SelectedTrackVM.Track, tag);
        public bool CanAddTag => DatabaseOperations.CanAddTag(NewTagName);
        public void AddTag()
        {
            var tag = NewTagName;
            if (string.IsNullOrEmpty(tag)) return;
            DatabaseOperations.AddTag(tag);
        }
        public bool CanEditTag => DatabaseOperations.CanEditTag(ClickedTag, NewTagName);
        public void EditTag()
        {
            if (ClickedTag == null) return;
            DatabaseOperations.EditTag(ClickedTag, NewTagName);
        }
        public void DeleteTag()
        {
            if (ClickedTag == null) return;
            DatabaseOperations.DeleteTag(ClickedTag);
        }

        private bool isTagEditMode;
        public bool IsTagEditMode
        {
            get => isTagEditMode;
            set
            {
                SetProperty(ref isTagEditMode, value, nameof(IsTagEditMode));
                NotifyPropertyChanged(nameof(TagEditIcon));
                NotifyPropertyChanged(nameof(TagDeleteOrEditIcon));
                NotifyPropertyChanged(nameof(IsTagEditOrDeleteMode));
            }
        }
        public PackIconKind TagEditIcon => IsTagEditMode ? PackIconKind.Close : PackIconKind.Edit;
        private bool isTagDeleteMode;
        public bool IsTagDeleteMode
        {
            get => isTagDeleteMode;
            set
            {
                SetProperty(ref isTagDeleteMode, value, nameof(IsTagDeleteMode));
                NotifyPropertyChanged(nameof(TagDeleteIcon));
                NotifyPropertyChanged(nameof(TagDeleteOrEditIcon));
                NotifyPropertyChanged(nameof(IsTagEditOrDeleteMode));
            }
        }
        public PackIconKind TagDeleteIcon => IsTagDeleteMode ? PackIconKind.Close : PackIconKind.Delete;
        public PackIconKind TagDeleteOrEditIcon => IsTagEditMode ? PackIconKind.Edit : PackIconKind.Delete;
        public bool IsTagEditOrDeleteMode => IsTagDeleteMode || IsTagEditMode;

        public bool DisableVolumeUpdates { get; set; }
        public bool DisableSpotifyProgressUpdates { get; set; }
        public enum ProgressUpdateSource
        {
            Spotify,
            User,
        }
        public void SetProgressSpotify(int newProgress) => SetProgress(newProgress, ProgressUpdateSource.Spotify);

        public void SetProgress(int newProgress, ProgressUpdateSource source)
        {
            if (DisableSpotifyProgressUpdates && source == ProgressUpdateSource.Spotify)
                return;

            progress = newProgress;
            ProgressSource = source;
            NotifyPropertyChanged(nameof(Progress));
        }
        public ProgressUpdateSource ProgressSource { get; private set; }
        private int progress;
        public int Progress
        {
            get => progress;
            set => SetProgress(value, ProgressUpdateSource.User);
        }

    }
}
