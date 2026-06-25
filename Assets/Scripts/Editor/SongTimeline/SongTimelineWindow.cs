using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.SongTimeline
{
    public class SongTimelineWindow : EditorWindow
    {
        [SerializeField] private int _selectedIndex = -1;

        private ScrollView _timelineView;

        private float _zoom;
        private float _offset;
        
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
            
            if (allSongs.Count != 0)
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

        private void OnInspectorUpdate()
        {
            
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
            var backgroundColor = new Color(0.1568628f, 0.1568628f, 0.1568628f);
            var waveformColor = new Color(0.7372549f, 0.7372549f, 0.7372549f);

            var bounds = _timelineView.localBound;
            var width = Mathf.FloorToInt(bounds.width);
            var height = Mathf.FloorToInt(bounds.height);

            _audioTexture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = backgroundColor;
            
            _audioTexture.SetPixels(pixels);
            
            var clip = song.Clip;
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            
            int samplesPerPixel = Mathf.Max(1, samples.Length / width);
            int centerY = height / 2;

            for (int x = 0; x < width; x++)
            {
                int startSample = x * samplesPerPixel;

                float maxAmplitude = 0f;

                for (int i = 0; i < samplesPerPixel; i++)
                {
                    int sampleIndex = startSample + i;
                    if (sampleIndex >= samples.Length)
                        break;

                    maxAmplitude = Mathf.Max(maxAmplitude, Mathf.Abs(samples[sampleIndex]));
                }

                int waveformHeight = Mathf.RoundToInt(maxAmplitude * centerY);
                
                for (int y = centerY - waveformHeight;
                     y <= centerY + waveformHeight;
                     y++)
                {
                    _audioTexture.SetPixel(x, y, waveformColor);
                }
            }
            _audioTexture.Apply();
            
            var sprite = Sprite.Create(_audioTexture, new Rect(0, 0, _audioTexture.width, _audioTexture.height), Vector2.zero);
            var spriteImage = new Image()
            {
                sprite = sprite,
                scaleMode = ScaleMode.ScaleToFit
            };
            
            _timelineView.Add(spriteImage);
        }

        private void MouseOver()
        {
            
        }
    }
}
