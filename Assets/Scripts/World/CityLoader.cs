using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using World.Parsing;
using IoFile = System.IO.File;

namespace World
{
    public class CityLoader : MonoBehaviour
    {
        public SchoolInfo[] Schools;
        public int schoolToLoad;

        public void Awake()
        {
            Schools = JsonConvert.DeserializeObject<SchoolInfo[]>(IoFile.ReadAllText("Assets/Data/schoolData.json"));
            Debug.Log(Schools[schoolToLoad]);
            LoadSchool();
            // DontDestroyOnLoad(this);
        }

        public int GetSchool(string schoolName)
        {
            foreach (SchoolInfo school in Schools)
                if (school.Name == schoolName)
                    return school.ID;

            return -1;
        }

        private async Task<List<List<Vector3>>> GetGeoData()
        {
            const string url = "https://overpass-api.de/api/interpreter";
            string query = @$"[out:json][timeout:10];
(  
  way(around: 150, {Schools[schoolToLoad].Coordinates.Longitude}, {Schools[schoolToLoad].Coordinates.Latitude})[""building""=""yes""]->._;
  ._>->._; 
); 
._ out body;".Replace(Environment.NewLine, "");

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            // 2D outlines of buildings on the ground
            List<List<Vector3>> buildingOutlines = new List<List<Vector3>>();
            HttpResponseMessage response = client.GetAsync($"?data={query}").Result;
            if (response.IsSuccessStatusCode)
            {
                OsmOutput output = JsonConvert.DeserializeObject<OsmOutput>(await response.Content.ReadAsStringAsync());
                Dictionary<ulong, Vector3> nodes = new Dictionary<ulong, Vector3>();

                int i = 0;

                // Process and scale nodes.
                for (; i < output.Elements.Count && output.Elements[i].Type == "node"; i++)
                {
                    Element element = output.Elements[i];
                    const double scaleFactor = 100_000d;
                    // Converting position from geographical coordinates to usable ones.
                    double trueX = (Schools[schoolToLoad].Coordinates.Longitude - element.Lat) * scaleFactor;
                    double trueZ = (Schools[schoolToLoad].Coordinates.Latitude - element.Lon) * scaleFactor;

                    nodes[element.ID] = new Vector3((float) trueX, 0, (float) trueZ);
                }

                // Add nodes to their respective building outlines.
                for (; i < output.Elements.Count; i++)
                {
                    Element element = output.Elements[i];
                    List<Vector3> buildingOutline = new List<Vector3>();
                    foreach (ulong id in element.Nodes)
                    {
                        buildingOutline.Add(nodes[id]);
                    }

                    buildingOutlines.Add(buildingOutline);
                }
            }
            else
            {
                Debug.LogError($"ERROR {response.StatusCode}: {response.ReasonPhrase}");
            }

            return buildingOutlines;
        }

        public async void LoadSchool()
        {
            Debug.Log("Downloading buildings...");
            List<List<Vector3>> buildingsOutlines = await GetGeoData();

            Debug.Log("Finished download.\nConverting buildings...");
            List<Building> buildings = new List<Building>();
            foreach (List<Vector3> outline in buildingsOutlines)
            {
                buildings.Add(new Building(outline));
            }

            Debug.Log("Finished conversion.\nLoading...");
            Transform parent = GameObject.Find("Environment").transform.GetChild(2);
            // for (int i = 0; i < parent.childCount; i++)
            // {
            //     Destroy(parent.GetChild(0).gameObject);
            // }
            foreach (Building building in buildings)
            {
                Mesh mesh = new Mesh();

                // Vertices
                mesh.vertices = building.Vertices.ToArray();

                // UV
                Vector2[] uv = new Vector2[building.Vertices.Count]; //TODO: find how to calculate proper UVs
                for (int i = 0; i < mesh.uv.Length - 3; i += 3)
                {
                    mesh.uv[i] = Vector2.zero;
                    mesh.uv[i + 1] = Vector2.up;
                    mesh.uv[i + 2] = Vector2.right;
                }

                mesh.uv = uv;

                // Triangles
                mesh.triangles = building.Triangles.ToArray();

                // Mesh processing
                mesh.RecalculateNormals();
                //mesh.RecalculateBounds();

                GameObject newBuilding =
                    Instantiate(original: Resources.Load("Prefabs/Building"), parent: parent) as GameObject;

                // Shouldn't ever be null but I need Rider to stop nagging me with that orange underline
                if (newBuilding != null)
                {
                    MeshFilter meshFilter = newBuilding.GetComponent<MeshFilter>();
                    MeshCollider meshCollider = newBuilding.GetComponent<MeshCollider>();
                    meshFilter.mesh = mesh;
                    meshCollider.sharedMesh = mesh;
                }
            }

            Debug.Log($"Finished loading.\nBuilding count: {buildings.Count}");
        }
    }
}