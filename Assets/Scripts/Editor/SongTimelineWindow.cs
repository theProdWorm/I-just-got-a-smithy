using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;

namespace Editor
{
    public class SongTimelineWindow : EditorWindow
    {
        [SerializeField] private int _selectedIndex = -1;
        
        private VisualElement _timelineView;

        private float _zoom = 1f;
        private float _scrollX;

        private bool _lmbDown;
        private bool _rmbDown;
        private Vector2 _lastMousePos;
        
        private Song _song;

        private ListView _listView;

        [MenuItem("Tools/Song Timeline")]
        public static void Open()
        {
            EditorWindow window = GetWindow<SongTimelineWindow>();
            window.titleContent = new GUIContent("Song Timeline");
        
            window.minSize = new Vector2(1000, 400);
            window.maxSize = new Vector2(1920, 1080);
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            
            var leftRightSplitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(leftRightSplitView);

            _listView = new ListView();
            leftRightSplitView.Add(_listView);
            
            _timelineView = new ListView();
            leftRightSplitView.Add(_timelineView);
            
            var allSongs = GetAllSongs();
            
            _listView.makeItem = () => new Label();
            _listView.bindItem = (item, index) => { (item as Label)!.text = GetAllSongs()[index].name; };
            _listView.itemsSource = allSongs;
            
            _listView.selectedIndex = _selectedIndex;
            _listView.selectionChanged += OnSongSelectionChange;
            _listView.selectionChanged += (_) => { _selectedIndex = _listView.selectedIndex; };
            
            if (allSongs.Count != 0 && _selectedIndex >= 0)
                GenerateAudioTexture(allSongs[_selectedIndex]);
        }

        private List<Song> GetAllSongs()
        {
            var allSongGUIDs = AssetDatabase.FindAssets("t:Song");
            var allSongs = (from GUID in allSongGUIDs
                select AssetDatabase.LoadAssetAtPath<Song>(AssetDatabase.GUIDToAssetPath(GUID))).ToList();

            return allSongs;
        }
        
        private void OnFocus()
        {
            if (_listView == null)
                return;
            
            _listView.itemsSource = GetAllSongs();
            _listView.Rebuild();
        }

        private void OnGUI()
        {
            HandleInput(_timelineView.localBound);
            
            Repaint();
        }
        
        private void HandleInput(Rect rect)
        {
            var e = Event.current;

            if (!rect.Contains(e.mousePosition))
                return;
            
            Vector2 mousePos = e.mousePosition - new Vector2(rect.x, rect.y);
            
            switch (e.type)
            {
                case EventType.MouseDrag:
                    if (_lmbDown)
                    {
                        // slide waveform
                    }
                    
                    break;
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Space)
                    {
                        // Play audio
                        
                    }
                    break;
            }
        }

        private void Zoom(Rect rect, WheelEvent e, Vector2 mousePos)
        {
            Debug.Log("zooming");
            
            float scrollDelta = e.delta.y;
                    
            float oldZoom = _zoom;
            _zoom = Mathf.Clamp(_zoom * (1f - scrollDelta * 0.05f), 0.05f, 1f);
            
            float zoomDiff = oldZoom - _zoom;
            
            float centerX = rect.center.x;
            float centerDistanceNormalized = (mousePos.x - centerX) / centerX;

            _scrollX += centerDistanceNormalized * zoomDiff;
        }

        private void Pan(Rect rect, WheelEvent e)
        {
            
        }

        private void HandleRightClick(Rect rect)
        {
            _rmbDown = true;
            
            
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
                    GenerateAudioTexture(selectedSong);
                }
            }
            enumerator.Dispose();
        }

        private void GenerateAudioTexture(Song song)
        {
            var beatTimelineTexture = TimelineGenerator.GenerateBeatTimeline(song, _timelineView.localBound, _scrollX, _zoom);
            var beatTimelineSprite = Sprite.Create(beatTimelineTexture,
                new Rect(0, 0, beatTimelineTexture.width, beatTimelineTexture.height), Vector2.zero);
            var beatTimelineImage = new Image()
            {
                sprite = beatTimelineSprite,
                scaleMode = ScaleMode.ScaleToFit
            };
            _timelineView.hierarchy.Add(beatTimelineImage);
            
            var timelineTexture = TimelineGenerator.GenerateTimeline(song, _timelineView.localBound, _scrollX, _zoom,
                out var timestampsTexture);

            var timelineSprite = Sprite.Create(timelineTexture,
                new Rect(0, 0, timelineTexture.width, timelineTexture.height), Vector2.zero);
            var timelineImage = new Image()
            {
                sprite = timelineSprite,
                scaleMode = ScaleMode.ScaleToFit
            };
            _timelineView.hierarchy.Add(timelineImage);

            var timestampsSprite = Sprite.Create(timestampsTexture,
                new Rect(0, 0, timestampsTexture.width, timestampsTexture.height), Vector2.zero);

            var timestampsImage = new Image()
            {
                sprite = timestampsSprite,
                scaleMode = ScaleMode.ScaleToFit
            };
            _timelineView.hierarchy.Add(timestampsImage);
            
            // var waveformTexture = WaveformGenerator.GenerateAudioTexture(song, _timelineView.localBound, _scrollX, _zoom);
            // var waveformSprite = Sprite.Create(waveformTexture,
            //     new Rect(0, 0, waveformTexture.width, waveformTexture.height), Vector2.zero);
            // var waveformImage = new Image()
            // {
            //     sprite = waveformSprite,
            //     scaleMode = ScaleMode.ScaleToFit
            // };
            // _timelineView.hierarchy.Add(waveformImage);

            // spriteImage.RegisterCallback<PointerDownEvent>(e =>
            // {
            //     Debug.Log("mouse down");
            //     
            //     if (e.button == 0)
            //         _lmbDown = true;
            //     else if (e.button == 1)
            //         HandleRightClick(spriteImage.sourceRect); // TODO: change if necessary
            // });
            // spriteImage.RegisterCallback<PointerUpEvent>(e =>
            // {
            //     Debug.Log("mouse up");
            //     
            //     if (e.button == 0)
            //         _lmbDown = false;
            //     else if (e.button == 1)
            //         _rmbDown = false;
            // });
            // spriteImage.RegisterCallback<WheelEvent>(e =>
            // {
            //     Debug.Log("mouse wheel");
            //     
            //     Vector2 mousePos = e.mousePosition - spriteImage.sourceRect.position;
            //     
            //     if (e.modifiers == EventModifiers.Shift)
            //         Pan(spriteImage.sourceRect, e);
            //     else if (e.modifiers == EventModifiers.None)
            //         Zoom(spriteImage.sourceRect, e, mousePos);
            // });
        }
    }
}
