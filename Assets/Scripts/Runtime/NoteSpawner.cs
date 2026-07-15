using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Splines;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private GameObject notePrefab;
    public List<HitWindow> hitWindows=new List<HitWindow>();
    public List<SplineContainer> splines=new List<SplineContainer>();
    private float songTimer;
    [SerializeField] private Player songPlayer;
    private Song _currentSong;

    private List<Note> _noteList=new List<Note>();

    private Note _nextPossibleNote;

    private int noteIndex=0;

    [SerializeField] private float timeToSpawnNote;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetRandomTime();
        // GameObject newNote = Instantiate(notePrefab);
        // SplineAnimate hitNote = newNote.GetComponent<SplineAnimate>();
        // hitNote.Container = PickRandomLane();
        // hitNote.Play();
        _currentSong=songPlayer.ReturnSong();
        _noteList = _currentSong.Notes;
        Debug.Log(_noteList.Count);
        _noteList.Sort();
        foreach (var note in _noteList)
        {
            Debug.Log(note +"has the time "+note.Time+ " for lane "+note.Lane);
        }
        
        
        // hitNote.splineAnimate.Container = PickRandomLane();
    }
    void Update()
    {
        songTimer += Time.deltaTime;
        if (_noteList[noteIndex].Time-songTimer <timeToSpawnNote)
        {
            CreateNewNote(_noteList[noteIndex]);
            noteIndex++;
            
        }
    }

    private void CreateNewNote(Note note)
    {
        GameObject newNote = Instantiate(notePrefab);
        HitNote hitNote = newNote.GetComponent<HitNote>();
        int laneIndex = note.Lane;
        hitNote.lane = laneIndex;
        hitNote.splineAnimate.Container=splines[laneIndex];
        hitNote.splineAnimate.Play();
    }

    private int PickRandomLane()
    {
        int laneIndex = Random.Range(0, splines.Count);
        return laneIndex;
    }
    
    private void GetRandomTime()
    {
        songTimer = Random.Range(0.1f, 0.3f);
    }
}
