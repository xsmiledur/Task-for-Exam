using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* ****************
 * 
 * りんごの葉っぱ部分を作成
 * 
 * ****************/

public class AppleLeefBehaviour : MonoBehaviour {

	// Use this for initialization

	Vector3[] vertices;
	int[] triangles, _triangles;
	int n = 40;
	int h = 25;
	int i, j, N;
	float mgf = 0.39f, m = 20;
	float num, NUM;
	Mesh mesh;
	private MeshFilter mf;


	[SerializeField]
	private Material _mat;

	void setInitialVertices() {
		vertices = new Vector3[5 * n];
		//vertices = new Vector3[2 * n];
		for (i = 0; i < n; i++) {
			vertices [i] = new Vector3 (mgf * i / m, mgf * (Mathf.Exp (-Mathf.Pow ((i - h) / m, 2)) - Mathf.Exp (-Mathf.Pow (h / m, 2))));
			vertices [i].z = 0.1f + 0.1f * Mathf.Cos (Mathf.PI * i / m);

			vertices [i + n] = new Vector3 (mgf * i / m, 0.5f * mgf * (Mathf.Exp (-Mathf.Pow ((i - h) / m, 2)) - Mathf.Exp (-Mathf.Pow (h / m, 2))));
			vertices [i + n].z = -0.3f + 0.1f * Mathf.Cos (Mathf.PI * i / m);

			vertices [i + 2 * n] = new Vector3 (mgf * i / m, 0);
			vertices [i + 2 * n].z = +0.1f * Mathf.Cos (Mathf.PI * i / m);

			vertices [i + 3 * n] = new Vector3 (mgf * i / m, -0.5f * mgf * (Mathf.Exp (-Mathf.Pow ((i - h) / m, 2)) - Mathf.Exp (-Mathf.Pow (h / m, 2))));
			vertices [i + 3 * n].z = -0.3f + 0.1f * Mathf.Cos (Mathf.PI * i / m);

			vertices [i + 4 * n] = new Vector3 (mgf * i / m, -mgf * (Mathf.Exp (-Mathf.Pow ((i - h) / m, 2)) - Mathf.Exp (-Mathf.Pow (h / m, 2))));
			vertices [i + 4 * n].z = 0.1f + 0.1f * Mathf.Cos (Mathf.PI * i / m);
		}



	}

	void setInitialTriangles() {
		triangles = new int[0];
		for (i = 0; i < n - 1; i++) {
			for (j = 0; j < 4; j++) {
				N = triangles.Length;
				_triangles = new int[N + 6];
				System.Array.Copy (triangles, _triangles, triangles.Length);
				_triangles [N    ] = (j + 1) * n + i;
				_triangles [N + 1] = j * n + i;
				_triangles [N + 2] = (j + 1) * n + i + 1; //j = 1, i = n-2の時、3n-1

				_triangles [N + 3] = (j + 1) * n + i + 1;
				_triangles [N + 4] = j * n + i;
				_triangles [N + 5] = j * n + i + 1;
			
				triangles = _triangles;
			}
		}
	}

	// Use this for initialization
	void Start () {
		mesh = new Mesh ();

		setInitialVertices ();
		mesh.vertices = vertices;

		setInitialTriangles ();
		mesh.triangles = triangles;

		/* メッシュとレンダラーのおまじない */
		mesh.RecalculateNormals ();
		mf = GetComponent<MeshFilter> ();
		mf.sharedMesh = mesh;

		var renderer = GetComponent<MeshRenderer> ();
		renderer.material = _mat;

	}

	
	// Update is called once per frame
	void Update () {
		
	}
}
