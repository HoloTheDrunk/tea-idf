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

        public void Start()
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
  way(around: 100, {Schools[schoolToLoad].Coordinates.Longitude}, {Schools[schoolToLoad].Coordinates.Latitude})[""building""=""yes""]->._;
  ._>->._; 
); 
._ out body;".Replace(Environment.NewLine, "");

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            List<List<Vector3>> buildingOutlines = new List<List<Vector3>>();
            HttpResponseMessage response = client.GetAsync($"?data={query}").Result;
            if (response.IsSuccessStatusCode)
            {
                OsmOutput output = JsonConvert.DeserializeObject<OsmOutput>(await response.Content.ReadAsStringAsync());
                Dictionary<ulong, Vector3> nodes = new Dictionary<ulong, Vector3>();

                int i = 0;

                // Get scaled nodes.
                for(; i < output.Elements.Count && output.Elements[i].Type == "node"; i++)
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

                // Legacy
                // foreach (Element element in output.Elements)
                // {
                //     if (element.Type == "node")
                //     {
                //         const double scaleFactor = 100_000d;
                //         // Converting position from geographical coordinates to usable ones
                //         double trueX = (Schools[schoolToLoad].Coordinates.Longitude - element.Lat) * scaleFactor;
                //         double trueZ = (Schools[schoolToLoad].Coordinates.Latitude - element.Lon) * scaleFactor;
                //
                //         nodes[element.ID] = new Vector3((float) trueX, 0, (float) trueZ);
                //     }
                //     else if (element.Type == "way")
                //     {
                //         List<Vector3> buildingOutline = new List<Vector3>();
                //         foreach (ulong id in element.Nodes)
                //         {
                //             buildingOutline.Add(nodes[id]);
                //         }
                //
                //         buildingOutlines.Add(buildingOutline);
                //     }
                // }
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
            foreach (Building building in buildings)
            {
                GameObject newBuilding =
                    Instantiate(original: Resources.Load("Prefabs/Building"), parent: parent) as GameObject;

                Mesh mesh = new Mesh();

                mesh.vertices = building.Vertices.ToArray();
                mesh.uv = new Vector2[building.Vertices.Count]; //TODO: find how to calculate proper UVs
                mesh.triangles = building.Triangles.ToArray();

                // Should never be null but I need Rider to stop nagging me with that orange underline
                if (newBuilding != null)
                {
                    newBuilding.GetComponent<MeshFilter>().mesh = mesh;
                    newBuilding.GetComponent<MeshCollider>().sharedMesh = mesh;
                }
            }

            Debug.Log($"Finished loading.\nBuilding count: {buildings.Count}");
        }
    }
}