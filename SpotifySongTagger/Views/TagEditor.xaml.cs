﻿using Backend;
using Backend.Entities;
using MaterialDesignThemes.Wpf;
using Serilog;
using SpotifySongTagger.Utils;
using SpotifySongTagger.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Linq;
using static SpotifySongTagger.ViewModels.TagEditorViewModel;

namespace SpotifySongTagger.Views
{
    /// <summary>
    /// Interaction logic for TagEditor.xaml
    /// </summary>
    public partial class TagEditor : UserControl
    {
        private TagEditorViewModel ViewModel { get; }

        public TagEditor(ISnackbarMessageQueue messageQueue)
        {
            InitializeComponent();
            ViewModel = new TagEditorViewModel(messageQueue);
            DataContext = ViewModel;

            BaseViewModel.PlayerManager.OnTrackChanged += UpdatePlayingTrack;
            BaseViewModel.PlayerManager.OnNewTrack += UpdateNewTrack;
        }

        private void UpdatePlayingTrack(string newId)
        {
            SetPlayerTagsForCurrentlyPlayingTrack();
        }

        private void UpdateNewTrack(string newId)
        {
            SetArtisGenresForCurrentlyPlayingTrack();
        }

        #region load/unload
        private void UserControl_Loaded(object sender, RoutedEventArgs e) => ViewModel.OnLoaded();
        private void UserControl_Unloaded(object sender, RoutedEventArgs e) => ViewModel.OnUnloaded();
        #endregion


        #region tag drag & drop
        private void Tag_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var chip = sender as Chip;
            // DragDrop.DoDragDrop leads to no further events (e.g. on the delete button of the tag)
            if (TagEditOrDeleteIsHovered && e.LeftButton == MouseButtonState.Pressed)
            {

            }
            else if (chip != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var tag = chip.DataContext as Tag;
                DragDrop.DoDragDrop(chip, tag.Name, DragDropEffects.Link);
            }
        }
        private void Tracks_Drop(object sender, DragEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid.Items.Count == 0) return;

            var index = UIHelper.GetDataGridRowIndex(dataGrid, e);

            // assign tag to track
            var tag = e.Data.GetData(DataFormats.StringFormat) as string;
            var trackVM = (TrackViewModel)dataGrid.Items.GetItemAt(index);
            AssignTag(trackVM.Track, tag);
            SetPlayerTagsForCurrentlyPlayingTrack();
            //Log.Information($"Assigned {tag} to {trackVM.Track.Name}");
            e.Handled = true;
        }

        private void SongInfo_Drop(object sender, DragEventArgs e)
        {
            // assign tag to currently playing track
            var tag = e.Data.GetData(DataFormats.StringFormat) as string;
            ViewModel.AssignTagToCurrentlyPlayingTrack(tag);
            SetPlayerTagsForCurrentlyPlayingTrack();
            e.Handled = true;
        }
        #endregion

        #region tag double click
        private void Tag_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var chip = sender as MaterialDesignThemes.Wpf.Chip;
            var tag = chip.DataContext as Backend.Entities.Tag;
            ViewModel.AssignTagToCurrentlyPlayingTrack(tag.Name);
            SetPlayerTagsForCurrentlyPlayingTrack();
            e.Handled = true;   
        }
        #endregion

        private void AssignedTag_DeleteClick(object sender, RoutedEventArgs e)
        {
            var chip = sender as Chip;
            var tag = chip.DataContext as Tag;
            ViewModel.RemoveAssignment(tag);
            SetPlayerTagsForCurrentlyPlayingTrack();
        }


        private async void Playlists_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // clear tracks
            var treeView = sender as TreeView;
            PlaylistOrTag playlistOrTag = null;
            if (treeView.SelectedItem is Playlist playlist)
                playlistOrTag = new PlaylistOrTag(playlist);
            else if (treeView.SelectedItem is Tag tag)
                playlistOrTag = new PlaylistOrTag(tag);
            
            if (treeView.SelectedItem == null || playlistOrTag == null)
            {
                ViewModel.SelectedPlaylistOrTag = null;
                ViewModel.TrackVMs = null;
                return;
            }

            // load new tracks
            ViewModel.SelectedPlaylistOrTag = playlistOrTag;
            await ViewModel.LoadTracks(playlistOrTag);
        }


        #region add/edit/delete tag dialog
        public void AddTagDialog_Cancel(object sender, RoutedEventArgs e)
        {
            ViewModel.NewTagName = null;
            NewTagNameTextBox.Text = null; // this bugs sometimes and does not adapt the value of ViewModel.NewTagName even though it is set to null
        }
        private void AddTagDialog_Add(object sender, RoutedEventArgs e)
        {
            ViewModel.AddTag();
            ViewModel.NewTagName = null;
            NewTagNameTextBox.Text = null; // this bugs sometimes and does not adapt the value of ViewModel.NewTagName even though it is set to null
        }
        public void EditTagDialog_Cancel(object sender, RoutedEventArgs e)
        {
            ViewModel.NewTagName = null;
            ViewModel.ClickedTag = null;
        }
        private void EditTagDialog_Save(object sender, RoutedEventArgs e)
        {
            ViewModel.EditTag();
            ViewModel.NewTagName = null;
            ViewModel.ClickedTag = null;
        }
        public void DeleteTagDialog_Cancel(object sender, RoutedEventArgs e)
        {
            ViewModel.NewTagName = null;
            ViewModel.ClickedTag = null;
        }
        private void DeleteTagDialog_Delete(object sender, RoutedEventArgs e)
        {
            ViewModel.DeleteTag();
            ViewModel.NewTagName = null;
            ViewModel.ClickedTag = null;
        }


        private void NewTagName_TextChanged(object sender, TextChangedEventArgs e)
        {
            // if validation gives an error for NewTagName, it is not updated in the ViewModel
            var textBox = sender as TextBox;
            ViewModel.NewTagName = textBox.Text;
            //Log.Information("TextChanged");
            // binding would sometimes bug and not bind properly
            var textBinding = NewTagNameTextBox.GetBindingExpression(TextBox.TextProperty);
            var validationRule = textBinding.ParentBinding.ValidationRules[0];
            var validationError = new ValidationError(validationRule, textBox.GetBindingExpression(TextBox.TextProperty));
            var validationResult = validationRule.Validate(ViewModel.NewTagName, null);
            if (!validationResult.IsValid)
            {
                validationError.ErrorContent = validationResult.ErrorContent;
                Validation.MarkInvalid(textBinding, validationError);
            }
            else
                Validation.ClearInvalid(textBinding);
        }
        #endregion

        #region add/edit/delete tag group dialog
        public void AddTagGroupDialog_Cancel(object sender, RoutedEventArgs e)
        {
            ViewModel.NewTagGroupName = null;
            NewTagGroupNameTextBox.Text = null; // this bugs sometimes and does not adapt the value of ViewModel.NewTagName even though it is set to null
        }
        private void AddTagGroupDialog_Add(object sender, RoutedEventArgs e)
        {
            ViewModel.AddTagGroup();
            ViewModel.NewTagGroupName = null;
            NewTagGroupNameTextBox.Text = null; // this bugs sometimes and does not adapt the value of ViewModel.NewTagName even though it is set to null
        }
        private void NewTagGroupName_TextChanged(object sender, TextChangedEventArgs e)
        {
            // if validation gives an error for NewTagName, it is not updated in the ViewModel
            var textBox = sender as TextBox;
            ViewModel.NewTagGroupName = textBox.Text;
            //Log.Information("TextChanged");
            // binding would sometimes bug and not bind properly
            var textBinding = textBox.GetBindingExpression(TextBox.TextProperty);
            var validationRule = textBinding.ParentBinding.ValidationRules[0];
            var validationError = new ValidationError(validationRule, textBox.GetBindingExpression(TextBox.TextProperty));
            var validationResult = validationRule.Validate(ViewModel.NewTagGroupName, null);
            if (!validationResult.IsValid)
            {
                validationError.ErrorContent = validationResult.ErrorContent;
                Validation.MarkInvalid(textBinding, validationError);
            }
            else
                Validation.ClearInvalid(textBinding);
        }
        public void EditTagGroupDialog_Cancel(object sender, RoutedEventArgs e)
        {
            ViewModel.NewTagGroupName = null;
            ViewModel.ClickedTagGroup = null;
        }
        private void EditTagGroupDialog_Save(object sender, RoutedEventArgs e)
        {
            ViewModel.EditTagGroup();
            ViewModel.NewTagGroupName = null;
            ViewModel.ClickedTagGroup = null;
        }
        public void DeleteTagGroupDialog_Cancel(object sender, RoutedEventArgs e)
        {
            ViewModel.NewTagGroupName = null;
            ViewModel.ClickedTagGroup = null;
        }
        private void DeleteTagGroupDialog_Delete(object sender, RoutedEventArgs e)
        {
            ViewModel.DeleteTagGroup();
            ViewModel.NewTagGroupName = null;
            ViewModel.ClickedTagGroup = null;
        }
        #endregion

        #region tag edit/delete button behaviour
        private void ToggleDeleteMode(object sender, RoutedEventArgs e)
        {

            ViewModel.IsTagEditMode = false;
            ViewModel.IsTagDeleteMode = !ViewModel.IsTagDeleteMode;
        }
        private void ToggleEditMode(object sender, RoutedEventArgs e)
        {
            ViewModel.IsTagDeleteMode = false;
            ViewModel.IsTagEditMode = !ViewModel.IsTagEditMode;
        }

        private bool TagEditOrDeleteIsHovered { get; set; }
        private void TagEditOrDelete_MouseEnter(object sender, MouseEventArgs e) => TagEditOrDeleteIsHovered = true;
        private void TagEditOrDelete_MouseLeave(object sender, MouseEventArgs e) => TagEditOrDeleteIsHovered = false;

        private void EditOrDeleteTagButton_Click(object sender, RoutedEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            ViewModel.ClickedTag = frameworkElement.DataContext as Tag;
            ViewModel.NewTagName = ViewModel.ClickedTag.Name;
        }
        #endregion

        #region TagGroups
        private void AddTagGroup(object sender, RoutedEventArgs e)
        {
            ViewModel.AddTagGroup();
        }
        private void TagGroupHeader_Drop(object sender, DragEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            var tagGroup = frameworkElement.DataContext as TagGroup;
            var tagName = e.Data.GetData(DataFormats.StringFormat) as string;

            ViewModel.ChangeTagGroup(tagName, tagGroup);
            Log.Information($"Changed TagGroup of Tag {tagName} to {tagGroup.Name}");
            e.Handled = true;
        }
        private void EditOrDeleteTagGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            ViewModel.ClickedTagGroup = frameworkElement.DataContext as TagGroup;
            ViewModel.NewTagGroupName = ViewModel.ClickedTagGroup.Name;
        }

        private void MoveTagGroupUpButton_Click(object sender, RoutedEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            var tagGroup = frameworkElement.DataContext as TagGroup;
            ViewModel.MoveUp(tagGroup);
            Log.Information($"moved {tagGroup.Name} up");
        }
        private void MoveTagGroupDownButton_Click(object sender, RoutedEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            var tagGroup = frameworkElement.DataContext as TagGroup;
            ViewModel.MoveDown(tagGroup);
            Log.Information($"moved {tagGroup.Name} down");
        }
        #endregion

        #region play/pause
        private async void Play_Click(object sender, RoutedEventArgs e) => await PlayerManager.Instance.Play();
        private async void Pause_Click(object sender, RoutedEventArgs e) => await PlayerManager.Instance.Pause();

        private async void PlayTrack(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedTrackVM == null) return;
            await BaseViewModel.PlayerManager.SetTrack(ViewModel.SelectedTrackVM.Track);
        }
        #endregion

        #region volume
        private void SetVolume_DragStarted(object sender, DragStartedEventArgs e)
        {
            //Log.Information("volume drag start");
            ViewModel.IsDraggingVolume = true;
        }
        private async void SetVolume_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            //Log.Information("volume drag completed");
            await SetVolume(sender);
            ViewModel.IsDraggingVolume = false;
        }
        private async void SetVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // avoid firing an event when the progress bar is updated from spotify
            if (ViewModel.VolumeSource == UpdateSource.Spotify)
            {
                //Log.Information("avoided volume value changed (updated by spotify)");
                return;
            }
            // avoid firing when volume bar is currently dragged
            if (ViewModel.IsDraggingVolume)
            {
                //Log.Information("avoided volume value changed (is dragging slider)");
                return;
            }

            //Log.Information("volume value changed");
            await SetVolume(sender);
        }
        private static async Task SetVolume(object sender)
        {
            var slider = sender as Slider;
            var newVolume = (int)slider.Value;
            await BaseViewModel.PlayerManager.SetVolume(newVolume);
        }
        #endregion

        #region progress
        private void SetProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            //Log.Information("progress drag start");
            ViewModel.IsDraggingProgress = true;
        }
        private async void SetProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            //Log.Information("progress drag completed");
            await SetProgress(sender);
            ViewModel.IsDraggingProgress = false;
        }
        private async void SetProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // avoid firing an event when the progress bar is updated from spotify
            if (ViewModel.ProgressSource == UpdateSource.Spotify)
            {
                //Log.Information("avoided volume value changed (updated by spotify)");
                return;
            }
            // avoid firing when progress bar is currently dragged
            if (ViewModel.IsDraggingProgress)
            {
                //Log.Information("avoided volume value changed (is dragging slider)");
                return;
            }

            // this event is fired if the progress bar is set by just clicking somewhere (not dragging it there)
            //Log.Information("progress value changed");
            await SetProgress(sender);
        }
        private static async Task SetProgress(object sender)
        {
            var slider = sender as Slider;
            var newProgress = (int)slider.Value;
            await BaseViewModel.PlayerManager.SetProgress(newProgress);
        }
        #endregion

        private void SetPlayerTagsForCurrentlyPlayingTrack()
        {

            if (BaseViewModel.PlayerManager.Track == null) return;
            var trackId = BaseViewModel.PlayerManager.TrackId;

            // get track directly from database
            var track = DatabaseOperations.GetTrack(trackId);
            if (track != null) {
                PlayerManager.Instance.Tags = track.Tags;
                PlayerManager.Instance.TagString = String.Join(", ", (track.Tags as IEnumerable<Tag>).Select(x => x.Name).ToArray());
            } else {
                PlayerManager.Instance.TagString = "";
            }

        }

        private async void SetArtisGenresForCurrentlyPlayingTrack()
        {

            if (BaseViewModel.PlayerManager.Track == null) return;
            var trackId = BaseViewModel.PlayerManager.TrackId;

            var artistIds = BaseViewModel.PlayerManager.Track.Artists.Select(x => x.Id).ToList();
            var spotifyArtists = await SpotifyOperations.GetArtists(artistIds);
            var spGenres = String.Join(" / ", spotifyArtists.Select(x => String.Join(", ", x.Genres)).ToList());

            PlayerManager.Instance.ArtistsGenreString = spGenres;

        }
    }
}
