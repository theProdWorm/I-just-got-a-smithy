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
            var timelineView = new ScrollView(ScrollViewMode.Horizontal);
            splitView.Add(timelineView);
            
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
            
        }

        private void ShowTimeline()
        {
            
        }
    }
}
