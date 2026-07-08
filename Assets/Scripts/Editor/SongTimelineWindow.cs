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
        private const int BEAT_RESOLUTION = 16;
        
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
            
            _timelineView.RegisterCallback<GeometryChangedEvent>(_ => Draw());
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
            float songLength = _song.Clip.length;
            float playheadTime = _normalizedPlayheadPosition * songLength;
            
            float startTime = songLength * ScrollX;
            float endTime = startTime + songLength * _zoom;
            if (_playingMusic)
            {
                playheadTime = _audioSource.time;

                if (_playingMusic && (playheadTime < startTime ||
                                      (ScrollX + _zoom < 1f && playheadTime > startTime + (endTime - startTime) / 2)))
                {
                    ScrollX = playheadTime / songLength;
                    Draw();
                }

                Repaint();
            }
            
            var timelineRect = _timelineView.localBound;
            int x = TimelineUtils.GetXPosition(playheadTime, startTime, endTime, (int) timelineRect.width);
            
            if (x > 0 && x < timelineRect.width)
                EditorGUI.DrawRect(new Rect(timelineRect.x + x, timelineRect.y, 2, timelineRect.height), Color.dodgerBlue);
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
            {}
            else if (_mmbDown)
                Pan(e);
        }

        private void Zoom(WheelEvent e, Vector2 mousePos)
        {
            var rect = _timelineView.localBound;
            
            float scrollDelta = e.delta.y;
            
            float oldZoom = _zoom;
            _zoom = Mathf.Clamp(_zoom * (1f + scrollDelta * 0.05f), 0.02f, 1f);
            
            float zoomDiff = oldZoom - _zoom;
            
            float centerX = rect.center.x;
            float centerDistanceNormalized = (mousePos.x - centerX) / rect.width * 2f;

            ScrollX += centerDistanceNormalized * zoomDiff;
            
            Draw();
        }

        private void Pan(WheelEvent e)
        {
            float scrollDelta = e.delta.x;

            ScrollX += scrollDelta * 0.1f * _zoom;
            
            Draw();
        }

        private void Pan(MouseMoveEvent e)
        {
            var rect = _timelineView.localBound;
            
            float mouseXDelta = e.mouseDelta.x;
            float normalizedMouseXDelta = mouseXDelta / rect.width;
            
            ScrollX -= normalizedMouseXDelta * _zoom;
            
            Draw();
        }
        
        private void SetPlayheadPosition(float mousePosX)
        {
            var rect = _timelineView.worldBound;
            
            float normalizedMousePos = (mousePosX - rect.x) / rect.width;
            
            _normalizedPlayheadPosition = SnapToBeat(ScrollX + normalizedMousePos * _zoom);

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
        
        private void HandleRightClick()
        {
            
        }

        private float SnapToBeat(float value)
        {
            int totalBeats = Mathf.FloorToInt(BEAT_RESOLUTION * _song.BPM / 60f * _song.Clip.length);
            int beatSnap = Mathf.RoundToInt(value * totalBeats);
            
            return beatSnap / (float) totalBeats;
        }
        
        private void OnSongSelectionChange(IEnumerable<object> selectedItems)
        {
            _timelineView.hierarchy.Clear();
            
            var enumerator = selectedItems.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var selectedSong = enumerator.Current as Song;
                if (selectedSong != null)
                {
                    _song = selectedSong;
                    Draw();
                }
            }
            enumerator.Dispose();
        }

        private void Draw()
        {
            if (_timelineView.localBound.width <= 0)
                return;
            
            _timelineView.hierarchy.Clear();
            
            // Generate beat timeline
            var beatTimelineTexture = TimelineGenerator.GenerateBeatTimeline(_song, _timelineView.localBound, ScrollX, _zoom);
            var beatTimelineSprite = Sprite.Create(beatTimelineTexture,
                new Rect(0, 0, beatTimelineTexture.width, beatTimelineTexture.height), Vector2.zero);
            var beatTimelineImage = new Image()
            {
                sprite = beatTimelineSprite,
                scaleMode = ScaleMode.ScaleToFit
            };
            _timelineView.hierarchy.Add(beatTimelineImage);
            
            // Generate timeline
            var timelineTexture = TimelineGenerator.GenerateTimeline(_song, _timelineView.localBound, ScrollX, _zoom,
                out var timestampsTexture);

            var timelineSprite = Sprite.Create(timelineTexture,
                new Rect(0, 0, timelineTexture.width, timelineTexture.height), Vector2.zero);
            var timelineImage = new Image()
            {
                sprite = timelineSprite,
                scaleMode = ScaleMode.ScaleToFit
            };
            _timelineView.hierarchy.Add(timelineImage);

            // Generate timestamps
            var timestampsSprite = Sprite.Create(timestampsTexture,
                new Rect(0, 0, timestampsTexture.width, timestampsTexture.height), Vector2.zero);

            var timestampsImage = new Image()
            {
                sprite = timestampsSprite,
                scaleMode = ScaleMode.ScaleToFit
            };
            _timelineView.hierarchy.Add(timestampsImage);
            
            var songInfo = GetSongData();
            while (songInfo.MoveNext())
            {
                var label = songInfo.Current;
                _timelineView.hierarchy.Add(label);
            }
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
