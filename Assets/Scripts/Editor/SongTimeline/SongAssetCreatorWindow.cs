using System;
using ScriptableObjects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.SongTimeline
{
    public class SongAssetCreatorWindow : EditorWindow
    {
        public static void ShowSongAssetCreator()
        {
            EditorWindow window = GetWindow<SongAssetCreatorWindow>();
            window.titleContent = new GUIContent("Create Song Asset");
        
            window.minSize = new Vector2(400, 80);
            window.maxSize = new Vector2(400, 80);
        }

        public void CreateGUI()
        {
            var nameField = new TextField("Song Name");
            nameField.SetValueWithoutNotify("New Song");
            var clipField = new ObjectField("Song Clip") {objectType = typeof(AudioClip)};
            var bpmField = new TextField("BPM");
            bpmField.SetValueWithoutNotify("100");
            
            rootVisualElement.Add(nameField);
            rootVisualElement.Add(clipField);
            rootVisualElement.Add(bpmField);

            var button = new Button(() =>
            {
                var songName = nameField.value;
                var clip = clipField.value as AudioClip;
                var canParseBPM = float.TryParse(bpmField.value, out float bpm);

                if (!canParseBPM)
                {
                    Debug.LogError("BPM must be a number");
                    return;
                }
                
                CreateAsset(songName, clip, bpm);
            })
            {
                text = "Create Song Asset"
            };
            
            rootVisualElement.Add(button);
        }

        private void CreateAsset(string songName, AudioClip clip, float bpm)
        {
            var songInstance = (Song) CreateInstance(typeof(Song));
            songInstance.name = songName;
            songInstance.Clip = clip;
            songInstance.BPM = bpm;
            
            AssetDatabase.CreateAsset(songInstance, $"Assets/Resources/{songName}.asset");
            Close();
            
            var timelineWindow = GetWindow<SongTimelineWindow>();
            timelineWindow.CreateGUI();
            timelineWindow.Focus();
        }
    }
}