using TreeEditor;
using UnityEngine;
using UnityEngine.Splines;

public class HitNote : MonoBehaviour
{
    // private ScriptableObject.Song test;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private float travelSpeed;
    public TreeSpline spline;
    public int lane;
    public SplineAnimate splineAnimate;
    // public
    void Start()
    {
        Player player=FindAnyObjectByType<Player>();
        travelSpeed = player.TimelineSpeed;
        // Debug.Log(travelSpeed);
        splineAnimate=GetComponent<SplineAnimate>();

        splineAnimate.Completed += WhenDone;
    }

    private void WhenDone()
    {
        splineAnimate.Completed -= WhenDone;
        Destroy(this.gameObject);
    }

    public void OnHit()
    {
        splineAnimate.Completed -= WhenDone;
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        // float moveAmount = 5 * Time.deltaTime;
        // Vector3 startPos = nodes[0].point;
        // Vector3 endPos = nodes[1].point;
        // transform.position=Vector3.Lerp(startPos,endPos,moveAmount);
        
        
    }
}
