//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]

public class Outline : MonoBehaviour {
  private static HashSet<Mesh> _registeredMeshes = new HashSet<Mesh>();

  public enum Mode {
    OutlineAll,
    OutlineVisible,
    OutlineHidden,
    OutlineAndSilhouette,
    SilhouetteOnly
  }

  public Mode OutlineMode {
    get { return outlineMode; }
    set {
      outlineMode = value;
      _needsUpdate = true;
    }
  }

  public Color OutlineColor {
    get { return outlineColor; }
    set {
      outlineColor = value;
      _needsUpdate = true;
    }
  }

  public float OutlineWidth {
    get { return outlineWidth; }
    set {
      outlineWidth = value;
      _needsUpdate = true;
    }
  }

  [Serializable]
  private class ListVector3 {
    public List<Vector3> data;
  }

  [SerializeField]
  private Mode outlineMode;

  [SerializeField]
  private Color outlineColor = Color.white;

  [SerializeField, Range(0f, 10f)]
  private float outlineWidth = 2f;

  [Header("Optional")]

  [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
  + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
  private bool precomputeOutline = false;

  [SerializeField, HideInInspector]
  private List<Mesh> bakeKeys = new List<Mesh>();

  [SerializeField, HideInInspector]
  private List<ListVector3> bakeValues = new List<ListVector3>();

  private Renderer[] _renderers;
  private Material _outlineMaskMaterial;
  private Material _outlineFillMaterial;

  private bool _needsUpdate;

  void Awake() {

    // Cache renderers
    _renderers = GetComponentsInChildren<Renderer>();

    // Instantiate outline materials
    _outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
    _outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

    _outlineMaskMaterial.name = "OutlineMask (Instance)";
    _outlineFillMaterial.name = "OutlineFill (Instance)";

    // Retrieve or generate smooth normals
    LoadSmoothNormals();

    // Apply material properties immediately
    _needsUpdate = true;
  }

  void OnEnable() {
    foreach (Renderer renderer in _renderers) {

      // Append outline shaders
      List<Material> materials = renderer.sharedMaterials.ToList();

      materials.Add(_outlineMaskMaterial);
      materials.Add(_outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
  }

  void OnValidate() {

    // Update material properties
    _needsUpdate = true;

    // Clear cache when baking is disabled or corrupted
    if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count) {
      bakeKeys.Clear();
      bakeValues.Clear();
    }

    // Generate smooth normals when baking is enabled
    if (precomputeOutline && bakeKeys.Count == 0) {
      Bake();
    }
  }

  void Update() {
    if (_needsUpdate) {
      _needsUpdate = false;

      UpdateMaterialProperties();
    }
  }

  void OnDisable() {
    foreach (Renderer renderer in _renderers) {

      // Remove outline shaders
      List<Material> materials = renderer.sharedMaterials.ToList();

      materials.Remove(_outlineMaskMaterial);
      materials.Remove(_outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
  }

  void OnDestroy() {

    // Destroy material instances
    Destroy(_outlineMaskMaterial);
    Destroy(_outlineFillMaterial);
  }

  void Bake() {

    // Generate smooth normals for each mesh
    HashSet<Mesh> bakedMeshes = new HashSet<Mesh>();

    foreach (MeshFilter meshFilter in GetComponentsInChildren<MeshFilter>()) {

      // Skip duplicates
      if (!bakedMeshes.Add(meshFilter.sharedMesh)) {
        continue;
      }

      // Serialize smooth normals
      List<Vector3> smoothNormals = SmoothNormals(meshFilter.sharedMesh);

      bakeKeys.Add(meshFilter.sharedMesh);
      bakeValues.Add(new ListVector3() { data = smoothNormals });
    }
  }

  void LoadSmoothNormals() {

    // Retrieve or generate smooth normals
    foreach (MeshFilter meshFilter in GetComponentsInChildren<MeshFilter>()) {

      // Skip if smooth normals have already been adopted
      if (!_registeredMeshes.Add(meshFilter.sharedMesh)) {
        continue;
      }

      // Retrieve or generate smooth normals
      int index = bakeKeys.IndexOf(meshFilter.sharedMesh);
      List<Vector3> smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);

      // Store smooth normals in UV3
      meshFilter.sharedMesh.SetUVs(3, smoothNormals);
    }

    // Clear UV3 on skinned mesh renderers
    foreach (SkinnedMeshRenderer skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>()) {
      if (_registeredMeshes.Add(skinnedMeshRenderer.sharedMesh)) {
        skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];
      }
    }
  }

  List<Vector3> SmoothNormals(Mesh mesh) {

    // Group vertices by location
    IEnumerable<IGrouping<Vector3, KeyValuePair<Vector3, int>>> groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);

    // Copy normals to a new list
    List<Vector3> smoothNormals = new List<Vector3>(mesh.normals);

    // Average normals for grouped vertices
    foreach (IGrouping<Vector3, KeyValuePair<Vector3, int>> group in groups) {

      // Skip single vertices
      if (group.Count() == 1) {
        continue;
      }

      // Calculate the average normal
      Vector3 smoothNormal = Vector3.zero;

      foreach (KeyValuePair<Vector3, int> pair in group) {
        smoothNormal += mesh.normals[pair.Value];
      }

      smoothNormal.Normalize();

      // Assign smooth normal to each vertex
      foreach (KeyValuePair<Vector3, int> pair in group) {
        smoothNormals[pair.Value] = smoothNormal;
      }
    }

    return smoothNormals;
  }

  void UpdateMaterialProperties() {

    // Apply properties according to mode
    _outlineFillMaterial.SetColor("_OutlineColor", outlineColor);

    switch (outlineMode) {
      case Mode.OutlineAll:
        _outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        _outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        _outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineVisible:
        _outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        _outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        _outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineHidden:
        _outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        _outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        _outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineAndSilhouette:
        _outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        _outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        _outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.SilhouetteOnly:
        _outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        _outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        _outlineFillMaterial.SetFloat("_OutlineWidth", 0);
        break;
    }
  }
}
