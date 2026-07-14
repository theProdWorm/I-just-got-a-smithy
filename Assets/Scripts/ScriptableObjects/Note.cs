using System;

namespace ScriptableObjects
{
    [Serializable]
    public class Note
    {
        public float Time;
        public int Lane;
        
        public Note(float time, int lane)
        {
            Time = time;
            Lane = lane;
        }
    }
}