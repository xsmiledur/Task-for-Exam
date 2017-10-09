using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.UI;


public struct eInfo {
	public Vector3 pos;
	public string type;
	public Vector3 move;
	public eInfo(string name, Vector3 a, Vector3 b) {
		type = name;
		pos = a;
		move = b;
	}
}

public struct iInfo {
	public Vector3 pos;
	public float dist; // getNearItemInfo() でアイテムを入れ替えるときに使う
	public int no; // getNearItemInfo() でアイテムを入れ替えるときに使う
	public iInfo(Vector3 a) {
		pos = a;
		dist = 0;
		no = 0;
	}
	public void setItemSortInfo(float _dist, int _no) {
		dist = _dist;
		no = _no;
	}

}

/* updateする際の格納変数 */
public class updtMem {
	public Vector3 pPos;
	public eInfo[] eInfos;
	public bool pFlg;
	public bool[] eFlg;

	public updtMem(Vector3 _pPos, bool _pFlg, eInfo[] _eInfos, bool[] _eFlg) {
		pPos = _pPos;
		eInfos = _eInfos;
		pFlg = _pFlg;
		eFlg = _eFlg;
	}

	public void setUpdtPlayer(Vector3 _pPos, bool _pFlg) {
		pPos = _pPos;
		pFlg = _pFlg;
	}

	public void setUpdtEnemy(int i, eInfo _eInfo, bool _eFlg) {
		eInfos[i] = _eInfo;
		eFlg[i] = _eFlg;
	}

}

public class InitBehaviour : MonoBehaviour {


	/* enemyInfoをクラス化する */

	// fileの名前
	public string fileName = "quiz03.txt";
	// 速さ
	public float timeOut = 1f;
	private float timeElapsed = 0;

	// キーボードプレイを許可するか否か
	public bool KeyPlay = false;
	// 出力するか否か
	public bool outputFlg = false;
	// 敵が動く範囲でできる壁を作るか否か
	public bool setSubWall = true;
	// カウント
	public int count = -1;

	public Vector3 playerPos, goalPos;
	public Vector3[] enemyPos, wallPos, enemyWallPos, wallEdgePos;
	private string text;
	public int time;

	public eInfo[] eInfos, _eInfos;
	public iInfo[] iInfos, _iInfos;

	public nearItemInfo nearItm;

	public updtMem updtMem;
	public string key_pFlg = "";
	public bool[] key_eFlg = new bool[0];


	GameObject updtObj;
	EnemyBehaviour updt;
	private Methods mt;

	GameObject enemyObj, wall, item, playerObj;
	PlayerBehaviour player;

	StreamWriter sw;
	StreamWriter output;

	TextMesh timeText;


	void initSettings() {
		wallPos = new Vector3[0];
		enemyPos = new Vector3[5];
		enemyWallPos = new Vector3[0];
		eInfos = new eInfo[0];
		iInfos = new iInfo[0];
		wallEdgePos = new Vector3[4];

		updtObj = GameObject.Find( "Player" );
		updt = updtObj.GetComponent<EnemyBehaviour>();
		mt = updtObj.GetComponent<Methods> ();

	}

	/* カウントを増やす */

	public void updateCount() {
		count++;
		if (count == 8)
			count = 0;
	}

	void textToPosition() {
		Vector3 pos = new Vector3 (-0.5f, 0.5f);
		wallEdgePos [0] = new Vector3 (0.5f, 0.5f); // 壁の左上の座標
		int flg = 0; int timeFlg = 0; string timeSr = "";

		for (int i = 0; i < text.Length; i++) {
			if (flg == 0) {
				if (text [i] == ':') {
					timeFlg = 1;
				} else if (text [i] == '\n') {
					flg = 1;
					time = Int32.Parse (timeSr);
				} else if (timeFlg == 1) {
					timeSr = timeSr + Convert.ToString (text [i]);
				}
			} else {
				pos = new Vector3 (pos.x + 1, pos.y);
				if (text [i] == '#') { // 壁を作る
					Vector3[] prePos = new Vector3[wallPos.Length + 1];
					System.Array.Copy (wallPos, prePos, wallPos.Length);
					prePos [wallPos.Length] = pos;
					wallPos = prePos;
				} else if (text [i] == 'o') { // アイテムを作る
					_iInfos = new iInfo[iInfos.Length + 1];
					System.Array.Copy (iInfos, _iInfos, iInfos.Length);
					_iInfos [iInfos.Length] = new iInfo(pos);
					iInfos = _iInfos;
				} else if (text [i] == 'A' || text [i] == 'B' || text [i] == 'C' || text [i] == 'D' || text [i] == 'E') { // 敵を作る
					string str = Convert.ToString(text[i]);
					_eInfos = new eInfo[eInfos.Length + 1];
					System.Array.Copy (eInfos, _eInfos, eInfos.Length);
					_eInfos [eInfos.Length] = new eInfo (str, pos, new Vector3(0,1));
					eInfos = _eInfos;

				} else if (text [i] == 'S') { // プレイヤーの初期位置を作る
					playerPos = pos;
				} else if (text [i] == 'G') { // プレイヤーのゴール位置を作る
					goalPos = pos;
				} else if (text [i] == '\n') { // 改行コードを読み取る
					if (pos.y == 0.5f) wallEdgePos[1] = pos; // 壁の右上の座標
					if (i != text.Length - 1) {
						pos = new Vector3 (-0.5f, pos.y - 1);
					}
				}
			}
		}
		wallEdgePos [3] = pos; // 壁の右下の座標
		wallEdgePos [2] = new Vector3 (0.5f, wallEdgePos [3].y); // 壁の左下の座標

	}


	private GameObject gameObj;
	/* 壁情報を取得 */
	void getWallPos() {
		wall = GameObject.Find ("Wall");
		for (int i = 0; i < wallPos.Length; i++) {
			gameObj = Instantiate (wall) as GameObject;
			gameObj.transform.position = wallPos [i];
		}
		wall.SetActive (false);


		wall = GameObject.Find ("SubWall");
		wall.SetActive (false);

	}
	/* アイテム情報を取得 */
	void getItemPos() {
		item = GameObject.Find ("Item");
		for (int i = 0; i < iInfos.Length; i++) {
			gameObj = Instantiate (item) as GameObject;
			gameObj.transform.position = iInfos [i].pos;
			gameObj.name = "item" + i.ToString();
		}
		item.SetActive (false);
	}

	/* ゴール情報を取得 */
	void setGoalPos() {
		GameObject goal = GameObject.Find ("Goal");
		goalPos.z = 5;
		goal.transform.position = goalPos;


	}

	/* 敵情報を取得 */
	void getEnemyPos() {
		enemyObj = GameObject.Find ("Enemy");

		for (int i=0; i<eInfos.Length; i++) {
			gameObj = Instantiate (enemyObj) as GameObject;
			gameObj.transform.position = eInfos[i].pos;
			gameObj.name = "Enemy" + i.ToString ();
			gameObj.tag = "Enemy" + eInfos [i].type;
		}

		enemyObj.SetActive (false);
	}

	/* カメラ情報を設定 */
	void setCameraPos() {
		GameObject camera = GameObject.Find ("Main Camera");
		Vector3 pos = new Vector3 ((wallEdgePos [0].x + wallEdgePos [1].x) / 2 + 0.5f, (wallEdgePos [0].y + wallEdgePos [2].y) / 2 + 0.5f);
		pos.z = -10f;
		camera.transform.position = pos;


		Camera _camera = camera.GetComponent<Camera> ();

		float width  = Mathf.Abs (wallEdgePos [0].x + wallEdgePos [1].x) / 2 + 1;

		_camera.orthographicSize = width;

	}

	/* 残り時間情報を設定 */
	void setTimeInfo() {
		GameObject timeObj = GameObject.Find ("Time");
		timeObj.transform.position = new Vector3 (wallEdgePos [0].x - 2, wallEdgePos [0].y + 2);
		timeText = timeObj.GetComponent<TextMesh> ();
		timeText.text = "Time: " + time.ToString();
	}


	// Use this for initialization
	void Start () {


		/* 初期設定 */
		initSettings ();

		/* テキストファイルを読み込み */
		StreamReader sr = new StreamReader (
			"Assets/quiz/" + fileName, Encoding.GetEncoding ("Shift_JIS"));

		text = sr.ReadToEnd ();
		sr.Close ();

		textToPosition ();

		/* プレイヤーに関する情報を取得 */
		playerObj = GameObject.Find ("Player");
		player = playerObj.GetComponent<PlayerBehaviour> ();
		player.transform.position = playerPos;

		/* 敵情報を取得 */
		getEnemyPos ();

		/* アイテム情報を取得 */
		getItemPos ();

		/* 壁情報を取得 */
		getWallPos ();

		/* 一番近いアイテムは初期化しておく */
		nearItm = mt.initNearItm ();

		/* カメラの位置を変更 */
		setCameraPos ();

		/* ゴール位置を変更 */
		setGoalPos ();

		/* 時間情報を設定 */
		setTimeInfo ();

		/* 敵C,D,Eの動きを決定するのに必要な定数を作っておく */
		setEnemyCDEdrct ();

		/* update用に格納された変数を入れておく */
		updtMem = new updtMem (playerPos, false, eInfos, createUpdtInitBool(eInfos.Length));
	
		// 全てのtimeFlgもfalseにする
		createInitTimeFlg (eInfos.Length);

		// 全てのkeyFlgもfalseにする
		createInitKeyFlg(eInfos.Length);

		/* 結果を入れる */
		if (outputFlg) {
			output = new StreamWriter ("Assets/Log/" + fileName + "_out.txt", false); //true=追記 false=上書き
			output.WriteLine (fileName);
			output.Flush ();
			output.Close ();
		}
	}

	public Vector3[] drct = new Vector3[4] { new Vector3 (0, 1), new Vector3 (-1, 0), new Vector3 (0, -1), new Vector3 (1, 0) };
	public Vector3[,] mvC, mvD, mvE;
	// 敵C,D,Eの動きを決定するのに必要な定数を作っておく
	void setEnemyCDEdrct() {
		mvC = new Vector3[4, 4] {
			{ new Vector3 (-1, 0), new Vector3 (0, 1), new Vector3 (1, 0), new Vector3 (0, -1) },
			{ new Vector3(), new Vector3(), new Vector3(), new Vector3() },
			{ new Vector3(), new Vector3(), new Vector3(), new Vector3() },
			{ new Vector3(), new Vector3(), new Vector3(), new Vector3() }
		};
		mvD = new Vector3[4, 4] {
			{ new Vector3 (1, 0), new Vector3 (0, 1), new Vector3 (-1, 0), new Vector3 (0, -1) },
			{ new Vector3(), new Vector3(), new Vector3(), new Vector3() },
			{ new Vector3(), new Vector3(), new Vector3(), new Vector3() },
			{ new Vector3(), new Vector3(), new Vector3(), new Vector3() }
		};
		mvE = new Vector3[4,8];

		for (int i = 1; i < 4; i++) {
			//mvCを90*i回転させる
			for (int j = 0; j < 4; j++) {
				mvC [i, j] = rotate (mvC [0, j], 90 * i);
				mvD [i, j] = rotate (mvD [0, j], 90 * i);
			}
		}

		for (int i = 0; i < 4; i++) {
			for (int j = 0; j < 4; j++) {
				mvE [i, 2 * j] = mvC [i, j];
				mvE [i, 2 * j + 1] = mvD [i, j];
			}
		}


	}

	// th度回転
	private Vector3 rotate(Vector3 vec, int th) {
		if (th == 90) {
			return new Vector3 (-vec.y, vec.x);
		} else if (th == 180) {
			return -vec;
		} else if (th == 270) {
			return new Vector3 (vec.y, -vec.x);
		} else {
			return new Vector3 ();
		}

	}

	// updateする際のboolean初期化
	bool[] createUpdtInitBool(int length) {
		bool[] updtInitBool = new bool[length];
		for (int i = 0; i < length; i++) {
			updtInitBool[i] = false;
		}
		return updtInitBool;
	}

	public bool time_pFlg = false;
	public bool[] time_eFlg;
	// 全てのtimeFlgをfalseにする
	void createInitTimeFlg(int length) {
		time_pFlg = false;
		time_eFlg = new bool[length];
		for (int i = 0; i < length; i++) {
			time_eFlg[i] = false;
		}
	}

	void createInitKeyFlg(int length) {
		key_pFlg = "";
		key_eFlg = new bool[length];
		for (int i = 0; i < length; i++) {
			key_eFlg[i] = false;
		}
	}

	void makeTimeFlgTrue(int length) {
		time_pFlg = true;
		time_eFlg = new bool[length];
		for (int i = 0; i < length; i++) {
			time_eFlg[i] = true;
		}
	}

	void makeKeyFlgTrue(int length, string keyName) {
		key_pFlg = keyName;
		key_eFlg = new bool[length];
		for (int i = 0; i < length; i++) {
			key_eFlg[i] = true;
		}
	}




	// Update is called once per frame
	void Update () {
		timeElapsed += Time.deltaTime;
		if (KeyPlay && (Input.GetKeyDown (KeyCode.UpArrow) || Input.GetKeyDown (KeyCode.DownArrow) || Input.GetKeyDown (KeyCode.LeftArrow) || Input.GetKeyDown (KeyCode.RightArrow) || Input.GetKeyDown (KeyCode.Space))|| !KeyPlay && (timeElapsed >= timeOut) ) {
			if (KeyPlay) {
				makeKeyFlgTrue (eInfos.Length, checkKeyCord());
			}
			timeElapsed = 0.0f;
			/* 位置の情報を同期させておく */
			getObjectPosition ();
			/* カウント数を上げておく */
			updateCount ();
			/* 同期が終わったら、データをEnemyBehaviour.csに渡す許可としてtimeFlgをあげる */
			makeTimeFlgTrue(eInfos.Length);

		} else {
			/* 位置情報のupdateが可能なら */
			if (checkUpdateEnable ()) {
				updateTime ();
				fileOut (); // ファイルにまず出力する
				updateObjectPosition ();
			}
		}


	}


	/* 表示時間を更新する */
	public void updateTime() {
		time = time - 1;
		timeText.text = "Time: " + time.ToString();
	}


	/* ファイルに動きを出力 */
	private void fileOut() {
		if (outputFlg) {
			output = new StreamWriter ("Assets/Log/" + fileName.Substring (0, 6) + "_out.txt", true); //true=追記 false=上書き
			Vector3 resPos = updtMem.pPos - playerPos;
			output.Write (getDirectName (resPos));
			output.Flush ();
			output.Close ();
		}
	}

	private string getDirectName(Vector3 pos) {
		if (pos.x == 0 && pos.y == 1) {
			return "u";
		} else if (pos.x == 0 && pos.y == -1) {
			return "d";
		} else if (pos.x == 1 && pos.y == 0) {
			return "r";
		} else if (pos.x == 0 && pos.y == 1) {
			return "l";
		} else if (pos.x == 0 && pos.y == 0) {
			return "w";
		} else {
			return "";
		}
	}

	/* 今押されているキーを返す */
	private string checkKeyCord() {
		if (Input.GetKey (KeyCode.UpArrow)) {
			return "up";
		} else if (Input.GetKey (KeyCode.DownArrow)) {
			return "down";
		} else if (Input.GetKey (KeyCode.RightArrow)) {
			return "right";
		} else if (Input.GetKey (KeyCode.LeftArrow)) {
			return "left";
		} else if (Input.GetKey (KeyCode.Space)) {
			return "space";
		} else {
			return "";
		}
	} 


	/* player, 各enemyの位置情報のupdateが可能かどうかチェック */
	bool checkUpdateEnable() {

		if (!updtMem.pFlg)
			return false;
		for (int i = 0; i < eInfos.Length; i++) {
			if (!updtMem.eFlg [i]) 
				return false;
		}
		return true;
	}
		
	/* 各動体の位置情報をupdateする */
	void updateObjectPosition() {
		
		// プレイヤー
		playerObj = GameObject.Find ("Player");
		playerObj.transform.position = updtMem.pPos;

		// 敵
		for (int i = 0; i < updtMem.eInfos.Length; i++) {
			enemyObj = GameObject.Find( "Enemy" + i.ToString() );
			enemyObj.transform.position = updtMem.eInfos [i].pos;
		}

		// updtMemのflgを下げる
		setUpdateFlgFalse();

	}
	void setUpdateFlgFalse() {
		updtMem.pFlg = false;
		updtMem.eFlg = createUpdtInitBool (eInfos.Length);
	}

	/* 各動体の位置情報を取得し保存 */
	void getObjectPosition() {
		//プレイヤー
		playerObj = GameObject.Find ("Player");
		playerPos = playerObj.transform.position;

		//敵
		for (int i = 0; i < eInfos.Length; i++) {
			enemyObj = GameObject.Find( "Enemy" + i.ToString() );
			eInfos [i].pos = enemyObj.transform.position;
			eInfos [i].move = updtMem.eInfos [i].move;
		}
		// updtMemを初期化
		updtMem = new updtMem (new Vector3 (), false, new eInfo[eInfos.Length], createUpdtInitBool(eInfos.Length));
	}
		
}
