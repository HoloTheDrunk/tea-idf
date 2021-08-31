using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshDebug : MonoBehaviour
{
    private Mesh _mesh;
    private Transform _player;
    private MeshCollider _collider;

    // Start is called before the first frame update
    public void Start()
    {
        _mesh = gameObject.GetComponent<MeshFilter>().mesh;
        _player = GameObject.Find("Player").transform;
        _collider = gameObject.GetComponent<MeshCollider>();
    }

    public void Update()
    {
        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
            new Vector2(_player.position.x, _player.position.z)) > 100f)
        {
            _collider.enabled = false;
        }
        else
        {
            _collider.enabled = true;
        }
    }

    public void OnDrawGizmos()
    {
        Debug.Log($"Mesh: {_mesh != null} | VertexCount: {_mesh?.vertexCount ?? 0}");
        if (_mesh != null)
        {
            for (int i = 0; i < _mesh.vertexCount; i++)
            {
                Gizmos.color = Color.Lerp(Color.black, Color.white, i / (_mesh.vertexCount / 2f) % 1f);
                Gizmos.DrawSphere(_mesh.vertices[i], 1f);
            }
        }
    }
}