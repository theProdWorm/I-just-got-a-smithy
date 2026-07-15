using System;

namespace ScriptableObjects
{
    [Serializable]
    public class Note: IComparable<Note>
    {
        public float Time;
        public int Lane;
        
        public Note(float time, int lane)
        {
            Time = time;
            Lane = lane;
        }
        

        public int CompareTo(Note other)
        {
            return this.Time.CompareTo(other.Time);
        }
    }
}