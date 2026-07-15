using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using Object = UnityEngine.Object;

namespace Editor
{
    public class SongTimelineWindow : EditorWindow
    {
        [SerializeField] private int _selectedIndex = -1;
        
        private VisualElement _timelineView;
        private ListView _songListView;

        private float _zoom = 1f;
        private float _scrollX;

        private float ScrollX
        {
            get => _scrollX;
            set
            {
                _scrollX = value;
                
                if (_scrollX < 0)
                    _scrollX = 0;
                else if (_scrollX + _zoom > 1f)
                    _scrollX = 1 - _zoom;
            }
        }

        private bool _lmbDown;
        private bool _rmbDown;
        private bool _mmbDown;
        private bool _wasMouseDragged;

        private float _normalizedPlayheadPosition;
        private bool _playingMusic;
        
        private Song _song;
        
        private AudioSource _audioSource;

        private Note _heldNote;

        [MenuItem("Tools/Song Timeline")]
        public static void Open()
        {
            EditorWindow window = GetWindow<SongTimelineWindow>();
            window.titleContent = new GUIContent("Song Timeline");
        
            window.minSize = new Vector2(1000, 400);
            window.maxSize = new Vector2(1920, 1080);
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            
            var leftRightSplitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(leftRightSplitView);

            _songListView = new ListView();
            leftRightSplitView.Add(_songListView);
            
            _timelineView = new ListView();
            
            leftRightSplitView.Add(_timelineView);
            
            var allSongs = GetAllSongs();
            
            _songListView.makeItem = () => new Label();
            _songListView.bindItem = (item, index) => { (item as Label)!.text = GetAllSongs()[index].name; };
            _songListView.itemsSource = allSongs;
            
            _songListView.selectedIndex = _selectedIndex;
            _songListView.selectionChanged += OnSongSelectionChange;
            _songListView.selectionChanged += (_) => { _selectedIndex = _songListView.selectedIndex; };
            
            // _timelineView.RegisterCallback<GeometryChangedEvent>(_ =>
            // {
            //     Draw();
            // });
            _timelineView.RegisterCallback<WheelEvent>(HandleScrollInput);
            _timelineView.RegisterCallback<MouseDownEvent>(HandleMouseDownInput);
            _timelineView.RegisterCallback<MouseUpEvent>(HandleMouseUpInput);
            _timelineView.RegisterCallback<MouseMoveEvent>(HandleMouseMoveInput);
            _timelineView.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                _lmbDown = false;
                _rmbDown = false;
                _mmbDown = false;
            });
        }

        private void OnFocus()
        {
            if (_songListView == null)
                return;
            
            _songListView.itemsSource = GetAllSongs();
            _songListView.Rebuild();
        }

        private void OnGUI()
        {
            var rect = _timelineView.localBound;

            var beatTimelineRect = new Rect(rect.x, rect.y, rect.width, 300);
            TimelineGenerator.DrawBeatTimeline(_song, beatTimelineRect, ScrollX, _zoom);
            
            var timelineRect = new Rect(rect.x, beatTimelineRect.yMax, _timelineView.localBound.width, 32);
            TimelineGenerator.DrawTimeline(_song, timelineRect, ScrollX, _zoom);
            
            DrawNotes();
            DrawPlayhead();
            
            Repaint();
        }

        private void DrawPlayhead()
        {
            float songLength = _song.Clip.length;
            
            float startTime = songLength * ScrollX;
            float endTime = startTime + songLength * _zoom;

            float playheadTime = _normalizedPlayheadPosition * songLength;
            
            if (_playingMusic)
            {
                playheadTime = _audioSource.time;

                if (_playingMusic && (playheadTime < startTime ||
                                      (ScrollX + _zoom < 1f && playheadTime > startTime + (endTime - startTime) / 2)))
                {
                    ScrollX = playheadTime / songLength;
                }

                Repaint();
            }
            
            var timelineRect = _timelineView.localBound;
            int x = TimelineUtils.GetXPosition(playheadTime, startTime, endTime, (int) timelineRect.width);
            
            if (x > 0 && x < timelineRect.width)
                EditorGUI.DrawRect(new Rect(timelineRect.x + x, timelineRect.y, 2, timelineRect.height), Color.dodgerBlue);
        }

        private void DrawNotes()
        {
            float songLength = _song.Clip.length;
            
            float startTime = songLength * ScrollX;
            float endTime = startTime + songLength * _zoom;

            var rect = _timelineView.localBound;
            int width = Mathf.FloorToInt(rect.width);

            const int numInputs = TimelineUtils.NUM_INPUTS;

            const int noteSize = 16;
            const int halfNoteSize = noteSize / 2;

            var beatTimelineRect = new Rect(rect.x, rect.y, rect.width, 300);
            
            foreach (var note in _song.Notes.Where(n => n.Time >= startTime && n.Time < endTime))
            {
                int x = TimelineUtils.GetXPosition(note.Time, startTime, endTime, width);
                
                if (x < halfNoteSize || x > width - halfNoteSize)
                    continue;

                int y = note.Lane;
                int yPos = (int) beatTimelineRect.height * (y + 1) / (numInputs + 1) + y / numInputs - halfNoteSize;

                var color = y switch
                {
                    0 => Color.red,
                    1 => Color.green,
                    2 => Color.blue,
                    3 => Color.yellow,
                    _ => Color.saddleBrown
                };

                EditorGUI.DrawRect(new Rect(rect.x + x - halfNoteSize, rect.y + yPos, noteSize, noteSize), color);
            }
        }

        private void OnInspectorUpdate()
        {
            if (!Application.isPlaying)
                _playingMusic = false;
        }

        private List<Song> GetAllSongs()
        {
            var allSongGUIDs = AssetDatabase.FindAssets("t:Song");
            var allSongs = (from GUID in allSongGUIDs
                select AssetDatabase.LoadAssetAtPath<Song>(AssetDatabase.GUIDToAssetPath(GUID))).ToList();

            return allSongs;
        }

        private void HandleScrollInput(WheelEvent e)
        {
            if (e.modifiers == EventModifiers.Shift)
                Pan(e);
            else if (e.modifiers == EventModifiers.None)
                Zoom(e, e.mousePosition);
        }

        private void HandleMouseDownInput(MouseDownEvent e)
        {
            switch (e.button)
            {
                case 0:
                    _lmbDown = true;
                    SetPlayheadPosition(e.mousePosition.x);
                    break;
                case 1:
                    _rmbDown = true;
                    HandleRightClickDown(e);
                    break;
                case 2:
                    _mmbDown = true;
                    break;
                case 3:
                    TogglePlayback();
                    break;
                case 4:
                    ResetPlayback();
                    break;
            }
        }

        private void HandleMouseUpInput(MouseUpEvent e)
        {
            switch (e.button)
            {
                case 0:
                    _lmbDown = false;
                    break;
                case 1:
                    _rmbDown = false;
                    break;
                case 2:
                    _mmbDown = false;
                    break;
            }
        }

        private void HandleMouseMoveInput(MouseMoveEvent e)
        {
            if (_lmbDown)
                SetPlayheadPosition(e.mousePosition.x);
            else if (_rmbDown)
                MoveHeldNote(e);
            else if (_mmbDown)
                Pan(e);
        }

        private void Zoom(WheelEvent e, Vector2 mousePos)
        {
            var rect = _timelineView.localBound;
            
            float scrollDelta = e.delta.y;
            
            float oldZoom = _zoom;
            _zoom = Mathf.Clamp(_zoom * (1f + scrollDelta * 0.05f), 1f / _song.Clip.length, 1f);
            
            float zoomDiff = oldZoom - _zoom;
            
            float normalizedMousePos = (mousePos.x - rect.x) / rect.width;
            
            ScrollX += normalizedMousePos * zoomDiff;
        }

        private void Pan(WheelEvent e)
        {
            float scrollDelta = e.delta.x;

            ScrollX += scrollDelta * 0.1f * _zoom;
        }

        private void Pan(MouseMoveEvent e)
        {
            var rect = _timelineView.localBound;
            
            float mouseXDelta = e.mouseDelta.x;
            float normalizedMouseXDelta = mouseXDelta / rect.width;
            
            ScrollX -= normalizedMouseXDelta * _zoom;
        }
        
        private void SetPlayheadPosition(float mousePosX)
        {
            var rect = _timelineView.worldBound;
            float normalizedMousePos = (mousePosX - rect.x) / rect.width;
            float normalizedMouseTime = ScrollX + normalizedMousePos * _zoom;
            float mouseTime = normalizedMouseTime * _song.Clip.length;

            _normalizedPlayheadPosition = TimelineUtils.SnapToBeat(mouseTime, _song, rect, ScrollX, _zoom) / _song.Clip.length;

            if (_audioSource && _audioSource.clip)
                _audioSource.time = _normalizedPlayheadPosition * _song.Clip.length;
            
            Repaint();
        }

        private void ResetPlayback()
        {
            _normalizedPlayheadPosition = 0;
            _playingMusic = false;
            _audioSource.Stop();
        }
        
        private void TogglePlayback()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("You must enter play mode to enable song playback.");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            _audioSource = scene.GetRootGameObjects().First().GetComponent<AudioSource>();
            _audioSource.clip = _song.Clip;
            
            _playingMusic = !_playingMusic;
            if (_playingMusic) // Start playback
            {
                float startTime = _normalizedPlayheadPosition * _song.Clip.length;
                _audioSource.time = startTime;
                
                _audioSource.Play();
            }
            else // Stop playback
            {
                _audioSource.Stop();
            }
        }
        
        private void HandleRightClickDown(MouseDownEvent e)
        {
            var rect = _timelineView.localBound;
            var beatTimelineRect = new Rect(rect.x, rect.y, rect.width, 300);

            Vector2 pos = e.mousePosition;
            float beatTime;
            int lane;

            if (e.modifiers == EventModifiers.Control)
            {
                float playheadTime = _normalizedPlayheadPosition * _song.Clip.length;
                
                float startTime = ScrollX * _song.Clip.length;
                float endTime = startTime + _song.Clip.length * _zoom;

                if (playheadTime < startTime || playheadTime > endTime)
                    return;
                
                pos = new Vector2(ScrollX + _normalizedPlayheadPosition * _zoom, pos.y);
                GetNotePosition(pos, beatTimelineRect, out _, out lane);
                beatTime = TimelineUtils.SnapToBeat(_normalizedPlayheadPosition * _song.Clip.length, _song, beatTimelineRect, ScrollX, _zoom);
            }
            else if (!beatTimelineRect.Contains(e.mousePosition))
                return;
            else
                GetNotePosition(pos, beatTimelineRect, out beatTime, out lane);
            
            var existingNote = _song.Notes.FirstOrDefault(n => Mathf.Approximately(n.Time, beatTime) && n.Lane == lane);

            if (e.modifiers == EventModifiers.Shift)
            {
                if (existingNote != null)
                    _song.Notes.Remove(existingNote);
            }
            else
            {
                if (existingNote == null)
                {
                    var note = new Note(beatTime, lane);
                    _song.Notes.Add(note);
                    _heldNote = note;
                }
                else
                {
                    _heldNote = existingNote;
                }
            }
        }

        private void MoveHeldNote(MouseMoveEvent e)
        {
            if (e.modifiers != EventModifiers.None)
                return;
            
            var rect = _timelineView.localBound;
            var beatTimelineRect = new Rect(rect.x, rect.y, rect.width, 300);

            if (!beatTimelineRect.Contains(e.mousePosition))
                return;
            
            GetNotePosition(e.mousePosition, beatTimelineRect, out var beatTime, out var lane);
            
            _heldNote.Time = beatTime;
            _heldNote.Lane = lane;
        }

        private void GetNotePosition(Vector2 mousePosition, Rect beatTimelineRect, out float beatTime, out int lane)
        {
            float normalizedMousePosX = (mousePosition.x - beatTimelineRect.x) / beatTimelineRect.width;
            float normalizedMousePosY = (mousePosition.y - beatTimelineRect.y) / beatTimelineRect.height;

            float normalizedMouseTime = ScrollX + normalizedMousePosX * _zoom;
            float mouseTime = normalizedMouseTime * _song.Clip.length;
            beatTime = TimelineUtils.SnapToBeat(mouseTime, _song, beatTimelineRect, ScrollX, _zoom);

            int numInputs = TimelineUtils.NUM_INPUTS;
            lane = Mathf.Clamp(Mathf.RoundToInt((normalizedMousePosY - 3f / ((numInputs + 1) * 2)) * (numInputs + 1)), 0, numInputs - 1);
        }
        
        private void OnSongSelectionChange(IEnumerable<object> selectedItems)
        {
            _zoom = 1;
            ScrollX = 0;
            
            var enumerator = selectedItems.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var selectedSong = enumerator.Current as Song;
                if (selectedSong != null)
                {
                    _song = selectedSong;
                }
            }
            enumerator.Dispose();
        }

        private IEnumerator<Label> GetSongData()
        {
            int clipLength = (int) _song.Clip.length;
            int minutes = clipLength / 60;
            int seconds = clipLength % 60;

            string playtimeString = $"Total Length: {minutes}:{seconds:00}";
            yield return new Label(playtimeString);
            
            float bpm = _song.BPM;
            string bpmString = $"BPM: {bpm:F1}";
            yield return new Label(bpmString);

            int numBeats = Mathf.FloorToInt(bpm * clipLength / 60);
            string numBeatsString = $"Beats: {numBeats}";
            yield return new Label(numBeatsString);
        }
    }
}
