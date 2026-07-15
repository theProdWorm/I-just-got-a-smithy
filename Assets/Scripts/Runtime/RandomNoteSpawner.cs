using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

public class RandomNoteSpawner : MonoBehaviour
{
    [SerializeField] private GameObject notePrefab;
    public List<HitWindow> hitWindows=new List<HitWindow>();
    public List<SplineContainer> splines=new List<SplineContainer>();
    private float timer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetRandomTime();
        // GameObject newNote = Instantiate(notePrefab);
        // SplineAnimate hitNote = newNote.GetComponent<SplineAnimate>();
        // hitNote.Container = PickRandomLane();
        // hitNote.Play();
        
        // hitNote.splineAnimate.Container = PickRandomLane();
    }
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            CreateNewNote();
            GetRandomTime();
        }
    }

    private void CreateNewNote()
    {
        GameObject newNote = Instantiate(notePrefab);
        HitNote hitNote = newNote.GetComponent<HitNote>();
        int laneIndex = PickRandomLane();
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
        timer = Random.Range(0.1f, 0.3f);
    }


    
}
