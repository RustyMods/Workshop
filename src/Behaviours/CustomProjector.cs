using System;
using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public class CustomProjector : MonoBehaviour
{
    public bool m_square;
    public float m_radius = 5f;
    public int m_nrOfSegments = 20;
    public float m_speed = 0.1f;
    public float m_turns = 1f;
    public float m_start;
    public bool m_sliceLines;
    public float m_calcStart;
    public float m_calcTurns;
    public GameObject m_prefab;
    public LayerMask m_mask;
    public List<GameObject> m_segments = new List<GameObject>();

    public void Awake()
    {
        m_mask = LayerMask.GetMask("terrain");
    }
    public void Start() => CreateSegments();

    public void Update()
    {
        CreateSegments();
        bool isClosedLoop = Mathf.Approximately(m_turns, 1.0f);
        float angle = 6.2831855f * m_turns / m_nrOfSegments - (isClosedLoop ? 0 : 1);
        float offset = !isClosedLoop || m_sliceLines ? 0.0f : Time.time * m_speed;
        
        for (int i = 0; i < m_nrOfSegments; ++i)
        {
            Vector3 pos = m_square
                ? GetSquareSegmentPosition(i, offset)
                : GetCircleSegmentPosition(i, angle, offset);

            GameObject segment = m_segments[i];
            if (Physics.Raycast(
                    pos + Vector3.up * 500f, 
                    Vector3.down, 
                    out RaycastHit hit, 
                    1000f, 
                    m_mask.value))
            {
                pos.y = hit.point.y;
            }
            
            segment.transform.position = pos;
        }

        for (int i = 0; i < m_nrOfSegments; ++i)
        {
            GameObject segment = m_segments[i];
            GameObject previous;
            GameObject next;
            
            if (isClosedLoop)
            {
                previous = i == 0 ? m_segments[m_nrOfSegments - 1] : m_segments[i - 1];
                next = i == m_nrOfSegments - 1 ? m_segments[0] : m_segments[i + 1];
            }
            else
            {
                previous = i == 0 ? segment : m_segments[i - 1];
                next = i == m_nrOfSegments - 1 ? segment : m_segments[i + 1];
            }
            
            Vector3 normalized = (next.transform.position - previous.transform.position).normalized;
            segment.transform.rotation = Quaternion.LookRotation(normalized, Vector3.up);
        }

        for (int i = m_nrOfSegments; i < m_segments.Count; ++i)
        {
            GameObject segment = m_segments[i];
            Vector3 pos = segment.transform.position;
            
            if (Physics.Raycast(
                    pos + Vector3.up * 500f, 
                    Vector3.down, 
                    out RaycastHit hit, 
                    1000f, m_mask.value))
            {
                pos.y = hit.point.y;
            }
            
            segment.transform.position = pos;
        }
    }
    
    public void CreateSegments()
    {
        if (!m_sliceLines && 
            m_segments.Count == m_nrOfSegments ||
            m_sliceLines && 
            Mathf.Approximately(m_calcStart, m_start) && 
            Mathf.Approximately(m_calcTurns, m_turns)) return;

        for (int i = 0; i < m_segments.Count; ++i)
        {
            GameObject segment = m_segments[i];
            Destroy(segment);
        }
        m_segments.Clear();

        for (int i = 0; i < m_nrOfSegments; ++i)
        {
            GameObject instance = Instantiate(m_prefab, transform.position, Quaternion.identity, transform);
            m_segments.Add(instance);
        }

        m_calcStart = m_start;
        m_calcTurns = m_turns;

        if (!m_sliceLines) return;
        
        float start = m_start;
        float angle = m_start + (float)(6.2831854820251465 * m_turns * 57.295780181884766);
        int count = (int)(m_radius / (2.0 * m_radius * Math.PI) * m_turns / m_nrOfSegments) - 2;
        
        PlaceRadiatingSlices(start, count);
        PlaceRadiatingSlices(angle, count);
    }

    public void PlaceRadiatingSlices(float angle, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            GameObject instance = Instantiate(m_prefab, transform.position, Quaternion.Euler(0.0f, angle, 0.0f), transform);
            instance.transform.position += instance.transform.forward * (m_radius * (i + 1)) / (count + 1);
            m_segments.Add(instance);
        }
    }
    
    private Vector3 GetCircleSegmentPosition(int index, float angleStep, float timeOffset)
    {
        float f = (float)(Math.PI / 180.0 * m_start + index * angleStep) + timeOffset;
        return transform.position + new Vector3(
            Mathf.Sin(f) * m_radius,
            0.0f,
            Mathf.Cos(f) * m_radius);
    }

    private Vector3 GetSquareSegmentPosition(int index, float timeOffset)
    {
        float t = ((float)index / m_nrOfSegments + timeOffset / (2f * Mathf.PI)) % 1f;
        if (t < 0f) t += 1f;

        float scaledT = t * 4f;
        int side = (int)scaledT;
        float edgeT = scaledT - side;

        // Corners of the square (clockwise from front-left)
        float half = m_radius;
        Vector3 offset = side switch
        {
            0 => new Vector3(Mathf.Lerp(-half, half, edgeT), 0f,  half), // front
            1 => new Vector3( half, 0f, Mathf.Lerp( half, -half, edgeT)), // right
            2 => new Vector3(Mathf.Lerp( half, -half, edgeT), 0f, -half), // back
            _ => new Vector3(-half, 0f, Mathf.Lerp(-half,  half, edgeT)), // left
        };

        return transform.position + Quaternion.Euler(0f, m_start, 0f) * offset;
    }
}