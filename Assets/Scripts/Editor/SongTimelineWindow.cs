using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public class SongTimelineWindow : EditorWindow
    {
        [SerializeField] private int _selectedIndex = -1;

        private ScrollView _timelineView;

        private float _zoom;
        private float _scrollX;
        
        private AudioClip _clip;

        private Texture2D _audioTexture;
        private ListView _listView;

        [MenuItem("Tools/Song Timeline")]
        public static void ShowSongTimeline()
        {
            EditorWindow window = GetWindow<SongTimelineWindow>();
            window.titleContent = new GUIContent("Song Timeline");
        
            window.minSize = new Vector2(400, 300);
            window.maxSize = new Vector2(1920, 1080);
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            _listView = new ListView();
            splitView.Add(_listView);
            _timelineView = new ScrollView(ScrollViewMode.Horizontal);
            splitView.Add(_timelineView);

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
            HandleInput(_timelineView.worldBound);
        }
        
        private void HandleInput(Rect rect)
        {
            var e = Event.current;

            if (!rect.Contains(e.mousePosition))
                return;
            
            Vector2 mousePos = e.mousePosition - new Vector2(rect.x, rect.y);
            
            switch (e.type)
            {
                case EventType.ScrollWheel:
                    Zoom(rect, e, mousePos);
                    break;
            }
        }

        private void Zoom(Rect rect, Event e, Vector2 mousePos)
        {
            float scrollDelta = e.delta.y;
                    
            float oldZoom = _zoom;
            _zoom = Mathf.Clamp(_zoom * (1f - scrollDelta * 0.05f), 0.05f, 1f);
            
            float zoomDiff = Mathf.Abs(oldZoom - _zoom);
            
            float centerX = rect.center.x;
            float centerDistanceNormalized = (mousePos.x - centerX) / centerX;
            
            
        }
        
        private void OnSongSelectionChange(IEnumerable<object> selectedItems)
        {
            _timelineView.Clear();
            
            var enumerator = selectedItems.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var selectedSong = enumerator.Current as Song;
                if (selectedSong != null)
                    GenerateAudioTexture(selectedSong);
            }
            enumerator.Dispose();
        }

        private void GenerateAudioTexture(Song song)
        {
            _audioTexture = WaveformGenerator.GenerateAudioTexture(song, _timelineView.localBound, _zoom, _scrollX);
            
            var sprite = Sprite.Create(_audioTexture, new Rect(0, 0, _audioTexture.width, _audioTexture.height), Vector2.zero);
            var spriteImage = new Image()
            {
                sprite = sprite,
                scaleMode = ScaleMode.ScaleToFit
            };

            _timelineView.Add(spriteImage);
        }
    }
}
