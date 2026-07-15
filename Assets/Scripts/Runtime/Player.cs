using System;
using ScriptableObjects;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private Song _currentSong;

    private float _timelineSpeed;
    private float bpm;
    private float quarterNote;
    private float songPos;
    public float TimelineSpeed=>_timelineSpeed;
    private AudioSource _audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Debug.Log(_currentSong.BPM);
        quarterNote=60/bpm;
        // songPos=AudioSettings.dspTime-dsp
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        bpm = _currentSong.BPM;
        _audioSource.clip = _currentSong.Clip;
        _audioSource.Play();
    }

    public Song ReturnSong()
    {
        return _currentSong;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
