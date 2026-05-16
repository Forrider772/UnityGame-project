using UnityEngine;
using System.Collections.Generic;

public class TestPathHover : MonoBehaviour
{
    public PathManager path;
    public float threshold = 1f;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        path.SetVisible(true);
    }

    void Update()
    {
        Vector2 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        float dist = Math2DHelper.MinDistancePointToPolyline(mouse, path.GetWaypoints2D());
        path.SetHighlight(dist < threshold);
    }
}