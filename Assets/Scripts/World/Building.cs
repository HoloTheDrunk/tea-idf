using System;
using System.Collections.Generic;
using UnityEngine;

namespace World
{
    public class Building
    {
        private int _outlineSize;
        public List<Vector3> Vertices;
        public List<int> Triangles;

        public Building(List<Vector3> vertices)
        {
            _outlineSize = vertices.Count;
            Vertices = vertices;
            Triangles = new List<int>();

            Extrude();
            Triangulate();
        }

        private void Extrude()
        {
            for (int i = 0; i < _outlineSize; i++)
            {
                //TODO: switch API for https://data.osmbuildings.org/0.2/anonymous/tile/?zoomLevel=15?/?x?/?y?.json
                //All buildings have the same constant height right now.
                Vertices.Add(Vertices[i] + Vector3.up * 10f);
            }
        }

        private void Triangulate()
        {
            for (int i = 0; i < _outlineSize; i++)
            {
                /* Adding two triangles for every rectangle:
                 * 3---4
                 * | / |
                 * 1---2
                 * => 143 and 124
                 * Going CCW because CW results in normals facing the wrong way.
                 */
                Triangles.AddRange(new List<int> {i, (i + 1) % _outlineSize + _outlineSize, i + _outlineSize});
                Triangles.AddRange(new List<int> {i, i + 1, (i + 1) % _outlineSize + _outlineSize});
            }
        }
    }
}