using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

public class HitWindow : MonoBehaviour
{
    [SerializeField] private CircleCollider2D circleCollider2D;

    
    [SerializeField] private int lane;
    // Start is called once before the first execution of Update after the MonoBehaviour is created



    public void OnPressHitWindow()
    {
        HitNote[] possibleNote=FindObjectsByType<HitNote>(FindObjectsSortMode.None);
        HitNote nextNote = possibleNote[0];
        for (int i = possibleNote.Length-1; i >=0; i--)
        {
            if(possibleNote[i].lane!=lane)
            {
                continue;
            }

            if (Vector2.Distance(possibleNote[i].transform.position, transform.position) <=
                Vector2.Distance(nextNote.transform.position, transform.position))
            {
                nextNote = possibleNote[i];
            }
        }
        float distance = Vector2.Distance(transform.position, nextNote.transform.position);
        if (distance < 0.5f)
        {
            //Should be a perfect hit or something
            Debug.Log("Hit");
            nextNote.OnHit();
        }
        //Make bigger rooms for error for less points, but not much

    }

    public void SetLane(int lane)
    {
        this.lane =lane;
        Debug.Log(lane +" for lane name: "+gameObject.name);
    }
}
