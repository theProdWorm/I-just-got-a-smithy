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
            
            var allSongGUIDs = AssetDatabase.FindAssets("t:Song");
            var allObjects = (from GUID in allSongGUIDs
                select AssetDatabase.LoadAssetAtPath<Song>(AssetDatabase.GUIDToAssetPath(GUID))).ToList();
        
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            var listView = new ListView();
            splitView.Add(listView);
            _timelineView = new ScrollView(ScrollViewMode.Horizontal);
            splitView.Add(_timelineView);
            
            var button = new Button(() =>
                {
                    SongAssetCreatorWindow.ShowSongAssetCreator();
                    CreateGUI();
                })
                { text = "+" };
            listView.hierarchy.Add(button);

            listView.makeItem = () => new Label();
            listView.bindItem = (item, index) => { (item as Label)!.text = allObjects[index].name; };
            listView.itemsSource = allObjects;
            
            listView.selectedIndex = _selectedIndex;
            listView.selectionChanged += OnSongSelectionChange;
            listView.selectionChanged += (_) => { _selectedIndex = listView.selectedIndex; };
            
            // if (allObjects.Count != 0)
            //     GenerateAudioTexture(allObjects[Mathf.Max(_selectedIndex, 0)]);
        }

        private void OnFocus()
        {
            CreateGUI();
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

        private void ShowTimeline()
        {
            
        }
    }
}
