using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Song", menuName = "I Just Got A Smithy/Song")]
    public class Song : ScriptableObject
    {
        [SerializeField] public AudioClip Clip;
        [SerializeField] public float BPM;
        
        [HideInInspector]
        [SerializeField] public List<Note> Notes;
    }
}