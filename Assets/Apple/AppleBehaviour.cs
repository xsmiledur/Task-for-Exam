using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct mouseData {
	public Vector3 pos;
	public float gain, dist;

	public mouseData(Vector3 a, float b, float c) {
		pos = a;
		gain = b;
		dist = c;
	}
}

[RequireComponent (typeof(MeshRenderer))]
[RequireComponent (typeof(MeshFilter))]
public class AppleBehaviour : MonoBehaviour {

	Vector3[] vertices;
	int[] triangles, _triangles;
	int n = 16, m = 16;
	int i, j, N;
	float radius = 0.4f;
	Mesh mesh;

	private Vector3 mouse, mouse_, pos;

	private mouseData mouseData;

	[SerializeField]
	private Material _mat;

	void setInitialVertices() {
		vertices = new Vector3[n * m + 2];
		for (i = 0; i < n; i++) {
			for (j=0; j < m; j++) {
				vertices [m * i + j].x = radius * (1 + Mathf.Cos (Mathf.PI * (j + 1) / (m + 1))) * Mathf.Sin (Mathf.PI * (j + 1) / (m + 1)) * Mathf.Cos (2 * i * Mathf.PI / n);
				vertices [m * i + j].y = -radius * ((1 + Mathf.Cos (Mathf.PI * (j + 1) / (m + 1))) * Mathf.Cos (Mathf.PI * (j + 1) / (m + 1)) - 1);
				vertices [m * i + j].z = radius * (1 + Mathf.Cos (Mathf.PI * (j + 1) / (m + 1))) * Mathf.Sin (Mathf.PI * (j + 1) / (m + 1)) * Mathf.Sin (2 * i * Mathf.PI / n);
			}
		}
		vertices [n * m] = new Vector3 (0, -radius);
		vertices [n * m + 1] = new Vector3 (0, 0);
	}

	void setInitialTriangles() {
		triangles = new int[0];
		for (i = 0; i < n; i++) {
			N = triangles.Length;
			_triangles = new int[N + 3];
			System.Array.Copy (triangles, _triangles, triangles.Length);
			_triangles [N] = n * m;
			_triangles [N + 1] = i * m;
			if (i == n - 1) {
				_triangles [N + 2] = 0;
			} else {
				_triangles [N + 2] = (i + 1) * m;
			}
			triangles = _triangles;


			for (j = 0; j < m - 1; j++) {
				
				N = triangles.Length;
				_triangles = new int[N + 6];
				System.Array.Copy (triangles, _triangles, triangles.Length);
				_triangles [N] = m * i + j;
				_triangles [N + 1] = m * i + j + 1;

				if (i == n - 1) {
					_triangles [N + 2] = j;
					_triangles [N + 3] = j;
				} else {
					_triangles [N + 2] = m * (i + 1) + j;
					_triangles [N + 3] = m * (i + 1) + j;
				}


				_triangles [N + 4] = m * i + j + 1;
				if (i == n - 1) {
					_triangles [N + 5] = j + 1;
				} else {
					_triangles [N + 5] = m * (i + 1) + j + 1;
				}

				triangles = _triangles;
			}



			N = triangles.Length;
			_triangles = new int[N + 3];
			System.Array.Copy (triangles, _triangles, triangles.Length);
			_triangles [N] = n * m + 1;
			if (i == n - 1) {
				_triangles [N + 1] = m - 1;
			} else {
				_triangles [N + 1] = (i + 2) * m - 1;
			}
			_triangles [N + 2] = (i + 1) * m - 1;

			triangles = _triangles;
		}
	}


	// マウスのリンゴに対する座標をとる
	public Vector3 getMousePos() {

		mouse_ = Input.mousePosition; //とりあえずスクリーン座標を取る
		mouse_.z = -2f; // Z軸修正
		mouse = Camera.main.ScreenToWorldPoint (mouse_);// マウス位置座標をスクリーン座標からワールド座標に変換する
		pos = transform.position; // 現在のリンゴの中心座標の画面内の位置を取得
		mouse.x = mouse.x - pos.x; mouse.y = mouse.y - pos.y; // マウス座標を中心座標に対する相対的な距離とする

		return mouse;
	}

	// マウスのリンゴに対する
	float getMouseDist(Vector3 p1, Vector3 mouse) {
		return Mathf.Sqrt(Mathf.Pow (p1.x - mouse.x, 2) + Mathf.Pow (p1.y - mouse.y, 2));
	}

	// Use this for initialization
	void Start () {
		mesh = new Mesh ();

		setInitialVertices ();
		mesh.vertices = vertices;

		setInitialTriangles ();
		mesh.triangles = triangles;

		/* メッシュとレンダラーとコリダーのおまじない */
		mesh.RecalculateNormals ();
		var meshfilter = GetComponent<MeshFilter> ();
		meshfilter.sharedMesh = mesh;

		var renderer = GetComponent<MeshRenderer> ();
		renderer.material = _mat;

		var collider = GetComponent<MeshCollider> ();
		collider.sharedMesh = mesh;

	}
	
	// Update is called once per frame
	void Update () {
		// 左クリックを取得
		if (Input.GetMouseButtonDown (0)) {
			// クリックしたスクリーン座標をrayに変換
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			// Rayの当たったオブジェクトの情報を格納する
			RaycastHit hit = new RaycastHit ();
			// オブジェクトにrayが当たった時

			//if (Physics.Raycast(ray, out hit, distance)) {
			if (Physics.Raycast (ray, out hit)) {
				// rayが当たったオブジェクトの名前を取得
				string objectName = hit.collider.gameObject.name;
				if (objectName == "Apple") {
					mouse = getMousePos ();
				}
				Debug.Log (objectName);
			}
		} else if (Input.GetMouseButton (0)) {
			mouse = getMousePos ();
			transform.position = new Vector3 (-mouse.x, -mouse.y);
		}
	}

}
