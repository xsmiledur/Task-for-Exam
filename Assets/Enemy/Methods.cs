using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;

public class Methods : MonoBehaviour {

	EnemyBehaviour eb;
	InitBehaviour init;

	/* 一時停止 */
	[MenuItem ("Custom/Pause")]
	private static void Pause () {
		EditorApplication.isPaused = true;
	}

	// Use this for initialization
	void Start () {
		GameObject enemyObj = GameObject.Find( "Player" );
		eb = enemyObj.GetComponent<EnemyBehaviour>();
		init = enemyObj.GetComponent<InitBehaviour>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	/* flgをチェックする */
	public bool checkFlg( string type, string name) {
		if (type == "time") {
			if (name == "Player") {
				return init.time_pFlg;
			} else if (name.Substring (0, 5) == "Enemy") {
				int index = Int32.Parse (name.Substring (5));
				return init.time_eFlg [index];
			}
		} else if (type == "key") {
			if (name == "Player") {
				if (init.key_pFlg == "up" || init.key_pFlg == "down" || init.key_pFlg == "right" || init.key_pFlg == "left" || init.key_pFlg == "space") {
					return true;
				} else {
					return false;
				}
			} else if (name.Substring (0, 5) == "Enemy") {
				int index = Int32.Parse (name.Substring (5));
				return init.key_eFlg [index];
			}
		}
		return false;
	}

	/* flgを全てfalse(初期化)にする */
	public void makeFlgFalse(string type, string name) {
		if (type == "time") {
			if (name == "Player") {
				init.time_pFlg = false;
			} else if (name.Substring (0, 5) == "Enemy") {
				int index = Int32.Parse (name.Substring (5));
				init.time_eFlg [index] = false;
			}
		} else if (type == "key") {
			if (name == "Player") {
				init.key_pFlg = "";
			} else if (name.Substring (0, 5) == "Enemy") {
				int index = Int32.Parse (name.Substring (5));
				init.key_eFlg [index] = false;
			}
		}
	}

	/* 2つのベクトルが等しいかどうか */
	public bool checkVecEqual(Vector3 a, Vector3 b) {
		if (a.x == b.x && a.y == b.y)
			return true;
		else
			return false;
	}

	/* 敵C,D,Eの動きを決定 */
	public Vector3 enemyMoveCDE(int eIndex, eInfo eInfo, bool playerFlg, Vector3 pNowPos, Vector3 target) {
		string type = eInfo.type;
		Vector3 nowPos = eInfo.pos;
		Vector3 move = eInfo.move;

		Vector3[] mvC = new Vector3[4] { new Vector3 (-1, 0), new Vector3 (0, 1), new Vector3 (1, 0), new Vector3 (0, -1) };
		Vector3[] mvD = new Vector3[4] { new Vector3 (1, 0), new Vector3 (0, 1), new Vector3 (-1, 0), new Vector3 (0, -1) };
		Vector3[] mvE = new Vector3[8];
		for (int i = 0; i < 4; i++) {
			mvE [2 * i] = mvC [i];
			mvE [2 * i + 1] = mvD [i];
		}

		Vector3[] eMove = new Vector3[0];
		int N = 4;
		if (type == "C") {
			eMove = mvC;
		} else if (type == "D") {
			eMove = mvD;
		} else if (type == "E") {
			eMove = mvE;
			N = 8;
		}

		Vector3[] drct = new Vector3[4] { new Vector3 (0, 1), new Vector3 (1, 0), new Vector3 (0, -1), new Vector3 (-1, 0) };
		Vector3 newVec;
		for (int i = 0; i < drct.Length; i++) {
			if (checkVecEqual (move, drct [i])) {
				for (int j = 0; j < eMove.Length; j++) {
					newVec = nowPos + eMove [(i + j) % N];
					if (eb.checkCollider (pNowPos, newVec, playerFlg, eIndex, target)) {
						return newVec;
					}
				}
			}
		}
		return new Vector3 ();
	}


	/* 最適経路を作成する */
	public Vector3[] createBestWay(Vector3 targetPos, bfsPos[] bfsPos, int no) {
		Vector3[] BestWay = new Vector3[1] { targetPos };
//		print (no);

		int start = bfsPos.Length - 1;
		int cnt = 0;

		while (no > 0) {
			for (int i = start; i >= 0; i--) {

				if (bfsPos [i].no == no && checkVecEqual (bfsPos [i].pos, BestWay [BestWay.Length - 1])) {
					Vector3[] _BestWay = new Vector3[BestWay.Length + 1];
					System.Array.Copy (BestWay, _BestWay, BestWay.Length);
					_BestWay [BestWay.Length] = bfsPos [i].pr;
					BestWay = _BestWay;

					no = no - 1;
					start = i - 1;
					break;
				}
			}
			cnt++;
			if (cnt > 1000) {
				break;
			}
		}

		return BestWay;
	}

	/* 一番近いitemとその距離を取得する 幅優先探索を用いて */
	public void getNearItemInfo() {

		// nearItemの初期化

		// キュー, bfsPos
		iInfo[] iInfos = init.iInfos;

		for (int l = 0; l < iInfos.Length; l++) {

			if (iInfos [l].pos.x != 0 || iInfos [l].pos.y != 0) { // 消されて値がリセットされていなければ
				// 訪問済みの位置にフラグを立てるarrayを初期化
				bool[,] posHist = createVisitedArray (init.wallEdgePos [3], init.wallEdgePos [0]);
				// bfsPosに根ノードを入れる
				bfsPos[] bfsPos = bfsPosPrepare();

				// 根ノードを空のキューに加える
				Vector3[] cue = bfs_cuePrepare();

				// dynamicaEnemyのキューを作る

				eInfo[,] dnmeCue = bfs_allDnmeCuePrepare (init.eInfos);

				// countのキューを作る
				int[] cntcue = bfs_cntcuePrepare (true);

				// 幅優先探索を行う
				bfsPos = bfs(bfsPos, cue, iInfos[l].pos, posHist, dnmeCue, cntcue, init.nearItm.dist);
				eb.sw.WriteLine ("Item" + l.ToString ());
				// 道のりの長さを求める
				float bfsDist = bfsPos [0].bfsDist;

				// 距離最小の場合、最小itemとして記録
				if (bfsDist < init.nearItm.dist) {
					init.nearItm = new nearItemInfo (l, iInfos [l], bfsDist, bfsPos, new Vector3[0]);
				}

			}	
		}
		eb.sw.WriteLine ("nearItm: Item" + init.nearItm.no + " dist: " + init.nearItm.dist + " Pos: " + init.nearItm.info.pos);

	}


	/* くっついた敵A,Bと、player間の距離を調べる 2以下でtrue, 3以上でfalse */
	public bool checkDistOfWEnemyAndPlayer() {
		Vector3 rltPos = init.eInfos [0].pos - init.playerPos;
		float dist = Mathf.Abs (rltPos.x) + Mathf.Abs (rltPos.y);
		if (dist >= 3) {
			return false;
		} else {
			return true;
		}
	}

	/* ゴールしてるかどうかチェック */
	public int checkGoal(int goalCnt, Vector3[] BestWay) {
		if (goalCnt == BestWay.Length) {
			print("end");
			Pause ();
		}
		goalCnt++;
		return goalCnt;
	}

	/* updateした時のplayerの位置を取る */
	public Vector3 get_updt_pPos_nearItmExist(Vector3[] BestWay) {
		if (checkBestWayExist (BestWay)) { // 最適経路が存在した場合は使う
			return get_pPos_byBestWay (BestWay);
		} else { // BestWayがなかった場合は、停止する
			print("noBestWay");
			return init.playerPos;
		}
	}

	/* ゴールに向かう */
	public Vector3[] trackGoal() {
		bool[,] posHist = createVisitedArray (init.wallEdgePos [3], init.wallEdgePos [0]);

		// プレイヤーの現在の位置から幅優先探索
		// 距離を初期化
		float bfsDist = 1000;
		// 根ノードを空のキューに加える。
		Vector3[] cue = new Vector3[1]{ init.playerPos }; 
		// bfsPosに根ノードを入れる
		bfsPos[] bfsPos = new bfsPos[1]{ new global::bfsPos (init.playerPos, new Vector3 (), new Vector3[0], 0) };
		// 幅優先探索を始める
		//bfsPos = bfs (bfsPos,  cue, init.goalPos, posHist, bfsDist , false, new eInfo[0], new int[0]);

		eInfo[,] dnmeCue = bfs_allDnmeCuePrepare (init.eInfos);
		int[] cntcue = bfs_cntcuePrepare (true);
		bfsPos = bfs(bfsPos, cue, init.goalPos, posHist, dnmeCue, cntcue, 1000);

		// 道のりの長さも出す
		bfsDist = bfsPos [0].bfsDist;

		return createBestWay(init.goalPos, bfsPos, (int)bfsDist); // 最適経路を返す
	}

	/* bfs(幅優先探索)の準備 */

	// bfsPosに根ノードを入れる
	public bfsPos[] bfsPosPrepare() {
		return new bfsPos[1]{ new global::bfsPos (init.playerPos, new Vector3 (), new Vector3[0], 0) };
	}

	// 根ノードを空のキューに加える
	public Vector3[] bfs_cuePrepare() {
		return new Vector3[1]{ init.playerPos };
	}

	// dynamicaEnemyのキューを作る
	public eInfo[] bfs_dnmeCuePrepare (bool dynamicFlg, eInfo dnmcEnemy) {
		eInfo[] dnmeCue;
		if (dynamicFlg) {
			dnmeCue = new eInfo[1] { dnmcEnemy };
		} else {
			dnmeCue = new eInfo[0];
		}
		return dnmeCue;
	}
	// dynamicaEnemyのキュー(2次元)を作る
	public eInfo[,] bfs_allDnmeCuePrepare (eInfo[] dnmcEnemy) {
		eInfo[,] dnmeCue;

		dnmeCue = new eInfo[1, dnmcEnemy.Length];
		for (int i = 0; i < dnmcEnemy.Length; i++) {
			dnmeCue [0, i] = dnmcEnemy [i];
		}

		return dnmeCue;
	}
		
	// countのキューを作る
	public int[] bfs_cntcuePrepare(bool dynamicFlg) {
		if (dynamicFlg) {
			return new int[1]{ init.count };
		} else {
			return new int[0];
		}
	}


	// キューを詰める
	public Vector3[] packCue(Vector3[] cue) {
		// キューを詰める
		Vector3[] _cue = new Vector3[cue.Length - 1];
		for (int i = 0; i < _cue.Length; i++) {
			_cue [i] = cue [i + 1];

		}
		return _cue;
	}

	// dynamicEnemyの入ったキューを詰める
	public eInfo[] packDnmeCue(eInfo[] dnmeCue) {
		// キューを詰める
		eInfo[] _dnmeCue = new eInfo[dnmeCue.Length - 1];
		for (int i = 0; i < _dnmeCue.Length; i++) {
			_dnmeCue [i] = dnmeCue [i + 1];
		}
		return _dnmeCue;
	}

	// dynamicEnemy(2次元)の入ったキューを詰める
	public eInfo[,] packAllDnmeCue(eInfo[,] dnmeCue) {
		// キューを詰める
		eInfo[,] _dnmeCue = new eInfo[dnmeCue.GetLength(0) - 1, dnmeCue.GetLength(1)];
		for (int i = 0; i < _dnmeCue.GetLength(0); i++) {
			for (int j=0; j<_dnmeCue.GetLength(1); j++) {
				_dnmeCue [i, j] = dnmeCue [i + 1, j];
			}
		}
		return _dnmeCue;
	}

	// カウントの入ったキューを詰める
	public int[] packCntCue(int[] cntCue) {
		// キューを詰める
		int[] _cntCue = new int[cntCue.Length - 1];
		for (int i = 0; i < _cntCue.Length; i++) {
			_cntCue [i] = cntCue [i + 1];
		}
		return _cntCue;
	}

	// 現在の親ノードの番号を記録。bfsPosの最後尾にいることを利用
	public int getParentNo(Vector3 chVec, bfsPos[] bfsPos) {
		int index = -1;
		for (int i = 0; i < bfsPos.Length; i++) {
			if (checkVecEqual (bfsPos [i].pos, chVec)) {
				index = i;
				break;
			}
		}
		return index;
	}

	// キューに新しいものを入れる
	public Vector3[] insertToCue(Vector3[] cue, Vector3 newVec) {
		Vector3[] _cue = new Vector3[cue.Length + 1];
		System.Array.Copy (cue, _cue, cue.Length);
		_cue [cue.Length] = newVec;
		return _cue;
	}
	public eInfo[] insertToDnmeCue(eInfo[] dnmeCue, eInfo dnmeNew) {
		eInfo[] _dnmeCue = new eInfo[dnmeCue.Length + 1];
		System.Array.Copy (dnmeCue, _dnmeCue, dnmeCue.Length);
		_dnmeCue [dnmeCue.Length] = dnmeNew;
		return  _dnmeCue;
	}
	public eInfo[,] insertToAllDnmeCue(eInfo[,] dnmeAllCue, eInfo[] dnmeNext) {
		eInfo[,] _dnmeCue = new eInfo[dnmeAllCue.GetLength(0) + 1, dnmeAllCue.GetLength(1)];
		System.Array.Copy (dnmeAllCue, _dnmeCue, dnmeAllCue.Length);
		for (int i=0; i<dnmeNext.Length; i++) {
			_dnmeCue [dnmeAllCue.GetLength (0), i] = dnmeNext [i];
		}
		return  _dnmeCue;
	}
	public int[] insertToCntCue(int[] cntCue, int cnt) {
		int[] _cntCue = new int[cntCue.Length + 1];
		System.Array.Copy (cntCue, _cntCue, cntCue.Length);
		_cntCue [cntCue.Length] = cnt;
		return  _cntCue;
	}

	// bfsPosにノードの情報を追加
	public bfsPos[] insertTo_bfsPos(bfsPos[] bfsPos, Vector3 vec, Vector3 prVec, int prNo) {
		bfsPos[] _bfsPos = new bfsPos[bfsPos.Length + 1];
		System.Array.Copy (bfsPos, _bfsPos, bfsPos.Length);

		_bfsPos [bfsPos.Length] = new bfsPos (vec, prVec, new Vector3[0], bfsPos[prNo].no + 1);
		return _bfsPos;
	}

	// bfsPosの親ノード情報に対し、この子ノード情報を付け加える
	public bfsPos[] insertChildTo_bfsPos(bfsPos[] bfsPos, int prNo, Vector3 chVec) {
		Vector3[] _ch = new Vector3[bfsPos[prNo].ch.Length + 1];
		System.Array.Copy (bfsPos [prNo].ch, _ch, bfsPos [prNo].ch.Length);
		_ch [bfsPos [prNo].ch.Length] = chVec;
		bfsPos [prNo].ch = _ch;
		return bfsPos;
	}

	/* bfsにより得られた目的地までの道のりの長さをえる */
	public int getBfsDist(bfsPos[] bfsPos, int prNo) {
		return bfsPos [prNo].no;
	}

	/* 訪問済みの印をつける */
	public void setVisited(Vector3 _pos, bool[,] posHist) {
		Vector3 rltWPos = new Vector3(_pos.x - init.wallEdgePos[0].x, init.wallEdgePos[0].y - _pos.y);
		posHist [(int)rltWPos.x, (int)rltWPos.y] = true;
	}


	/* 訪れたことのある点かどうか確認  true: 訪れたことがある false: 訪れたことがない */
	public bool checkVisited(Vector3 _pos, bool[,] posHist) {
		Vector3 rltWPos = new Vector3(_pos.x - init.wallEdgePos[0].x, init.wallEdgePos[0].y - _pos.y);
		return posHist [(int)rltWPos.x, (int)rltWPos.y];
	}


	/* 幅優先探索 
	    1.（前処理：根ノードを空のキューに加える。）
		2.ノードをキューの先頭から取り出し、以下の処理を行う。
		ノードが探索対象であれば、探索をやめ結果を返す。
		そうでない場合、ノードの子で未探索のものを全てキューに追加する。
		もしキューが空ならば、グラフ内の全てのノードに対して処理が行われたので、探索をやめ"not found"と結果を返す。
		2に戻る。
		ノードの展開により得られる子ノードはキューに追加される。訪問済みの管理は配列やセットなどでも行える。
	*/
	// 旧バージョン
//	public bfsPos[] _bfs(bfsPos[] bfsPos, Vector3[] cue, Vector3 target, bool[,] posHist, bool dynamicFlg, eInfo[] dnmeCue, int[] cntCue) {
//
//		Vector3[] move = getMoveDirect (false);
//
//		// キューの先頭のvecを出す(player)
//		Vector3 prVec = cue [0]; 
//		// 訪問済みの印をつける
//		setVisited (prVec, posHist);
//		// キューを詰める
//		cue = packCue (cue);
//
//		eInfo dnmcEnemy = new eInfo("", new Vector3());
//		int Cnt = 0, _Cnt = 0; //_Cntは子ノードのキューに入れる方のcount
//
//		if (dynamicFlg) {
//			// キューの先頭のenemyを出す
//			dnmcEnemy = dnmeCue [0];
//			dnmeCue = packDnmeCue (dnmeCue);
//
//			// キューの先頭のcountを出す
//			Cnt = cntCue [0];
//			cntCue = packCntCue (cntCue);
//			_Cnt = Cnt + 1;
//		}
//
//		// 現在の親ノードの番号を記録。bfsPosの最後尾にいることを利用
//		int prNo = getParentNo(prVec, bfsPos);
//
//		Vector3 newVec;
//		eInfo dnmeNext = new eInfo("", new Vector3());
//
//		if (dynamicFlg) {
//			dnmeNext = new eInfo (dnmcEnemy.type, eb.enemyMove (dnmcEnemy, prVec, Cnt));
//		}
//		for (int i = 0; i < 4; i++) {
//			newVec = prVec + move [i]; // 子を定義
//			//eb.sw.WriteLine ("prNo: " + prNo.ToString() + "newChildVec: " + newVec + " target: " + target + "cue Length: " + cue.Length.ToString());
//			bool res;
//
//			// 動く敵に対して
//			//sw.WriteLine("dynamicFlg: " + dynamicFlg);
//			if (dynamicFlg == true) {
//				res = checkColliderWallAndDynamicEnemy (prVec, newVec, target, dnmcEnemy, dnmeNext, true);
//			} else {
//				res = checkColliderWallAndEnemyWall (newVec, true);
//			}
//
//			if (res && !checkVisited(newVec, posHist)) { // 壁や敵にぶつからず、かつ訪れたことがない場合
//
//				// このnewVecは子ノードであるので、キューに追加
//				cue = insertToCue (cue, newVec);
//
//				// 動く敵がいる場合、その敵のキューも追加
//				if (dynamicFlg) {
//					dnmeCue = insertToDnmeCue (dnmeCue, dnmeNext);
//					cntCue = insertToCntCue (cntCue, _Cnt);
//				}
//
//				// bfsPosにこの子ノードの情報を追加
//				bfsPos = insertTo_bfsPos(bfsPos, newVec, prVec, prNo);
//
//				// bfsPosの親ノード情報に対し、この子ノード情報を付け加える
//				bfsPos = insertChildTo_bfsPos(bfsPos, prNo, newVec);
//
//
//			}
//		}
////		eb.sw.WriteLine (" prVec: " + prVec + " target: " + target + " equal: " + checkVecEqual(prVec, target) + " cue.len: " + cue.Length.ToString());
//		if (checkVecEqual(prVec, target) || cue.Length == 0) { // 目的地である場合 or cueの長さが0の場合
//			//sw.WriteLine ("=======");
//			//sw.Write("成功 ");
//			// 道のりの長さを求める
//			float bfsDist = getBfsDist(bfsPos, prNo);
//			//eb.sw.WriteLine ("prNo: " + prNo.ToString() + " prVec: " + prVec + " target: " + target + " bfsDist: " + bfsDist.ToString ());
//			// bfsPosのindex=0のところに格納しておく
//			bfsPos[0].set_bfsDist(bfsDist);
//
//			return bfsPos; // 終了
//
//		} else {
//
//			if (dynamicFlg) {
//				Cnt++;
//			}
//
//			return bfs (bfsPos, cue, target, posHist, dynamicFlg, dnmeCue, cntCue);
//
//		}
//
//	}


	public bfsPos[] bfs(bfsPos[] bfsPos, Vector3[] cue, Vector3 target, bool[,] posHist, eInfo[,] dnmeAllCue, int[] cntCue, float nowLeastDist) {

		Vector3[] move = getMoveDirect (false);

		// キューの先頭のvecを出す(player)
		Vector3 prVec = cue [0]; 
		// 訪問済みの印をつける
		setVisited (prVec, posHist);
		// キューを詰める
		cue = packCue (cue);

		eInfo[] dnme = new eInfo[dnmeAllCue.GetLength(1)];
		int Cnt = 0, _Cnt = 0; //_Cntは子ノードのキューに入れる方のcount


		// キューの先頭のenemyを出す
		for (int i = 0; i < dnme.Length; i++) {
			dnme[i] = dnmeAllCue [0,i];
		}
		dnmeAllCue = packAllDnmeCue (dnmeAllCue);

		// キューの先頭のcountを出す
		Cnt = cntCue [0];
		cntCue = packCntCue (cntCue);
		_Cnt = Cnt + 1;

		// 現在の親ノードの番号を記録。bfsPosの最後尾にいることを利用
		int prNo = getParentNo(prVec, bfsPos);

		Vector3 newVec;
		//eInfo dnmeNext = new eInfo("", new Vector3());
		eb.sw.WriteLine ("\n no: " + prNo.ToString());
		eInfo[] dnmeNext = new eInfo[dnme.Length];
		for (int i=0; i<dnmeNext.Length; i++) {
			Vector3 newEpos = eb.enemyMove (dnme [i], i, prVec, true, target);
			Vector3 e_rPos = newEpos - dnme [i].pos;
			dnmeNext[i] = new eInfo(dnme[i].type, newEpos, e_rPos);
			eb.sw.WriteLine ("Enemy" + i.ToString()  + " " + dnme[i].pos + " EnemyNext " + dnmeNext [i].pos);
		}
		eb.sw.WriteLine ();

		for (int i = 0; i < 4; i++) {
			newVec = prVec + move [i]; // 子を定義

			bool res = true;

			// 動く敵に対して
			//sw.WriteLine("dynamicFlg: " + dynamicFlg);

			//res = checkColliderWallAndAllDynamicEnemy (prVec, newVec, target, dnme, dnmeNext, true);
			res = eb.checkPlayerCollider(prVec, newVec);
			for (int j = 0; j < dnme.Length; j++) {
				res = eb.checkPlayerCollider (prVec, newVec, dnme, dnmeNext, target);
				if (!res)
					break;
			}
			eb.sw.WriteLine ("prVec: " + prVec + " newChVec: " + newVec + " res: " + res);

			if (res && !checkVisited(newVec, posHist)) { // 壁や敵にぶつからず、かつ訪れたことがない場合

				// このnewVecは子ノードであるので、キューに追加
				cue = insertToCue (cue, newVec);

				// 動く敵がいる場合、その敵のキューも追加
				dnmeAllCue = insertToAllDnmeCue (dnmeAllCue, dnmeNext);
				cntCue = insertToCntCue (cntCue, _Cnt);


				// bfsPosにこの子ノードの情報を追加
				bfsPos = insertTo_bfsPos(bfsPos, newVec, prVec, prNo);

				// bfsPosの親ノード情報に対し、この子ノード情報を付け加える
				bfsPos = insertChildTo_bfsPos(bfsPos, prNo, newVec);


			}
		}



		float bfsDist = getBfsDist(bfsPos, prNo);

		if (checkVecEqual (prVec, target) || cue.Length == 0) { // 目的地である場合 or cueの長さが0の場合
			bfsPos [0].set_bfsDist (bfsDist); // bfsPosのindex=0のところに格納しておく
			eb.sw.Write ("prNo" + prNo.ToString() + " bfsDist: " +  bfsDist.ToString());
			return bfsPos; // 終了

		} else if (bfsDist > nowLeastDist) { // 現在の最短距離よりも大きな距離となってしまっても、終了
			bfsPos [0].set_bfsDist (bfsDist);
			return bfsPos; // 終了
		} else {
			Cnt++;

			return bfs (bfsPos, cue, target, posHist, dnmeAllCue, cntCue, nowLeastDist);

		}

	}

//
//
//	/* 対象の全周囲方向のベクトルを返す */
//	public Vector3[] getAllDirect() {
//		Vector3[] direct = new Vector3[9] {
//			new Vector3 (0, 0),
//			new Vector3 (1, 0),
//			new Vector3 (-1, 0),
//			new Vector3 (0, 1),
//			new Vector3 (0, -1),
//			new Vector3 (1, 1),
//			new Vector3 (1, -1),
//			new Vector3 (-1, 1),
//			new Vector3 (-1, -1) // +2, -2も追加したい
//		};
//		return direct;
//	}

	/* 操作物体の動く方向を取得 */
	public Vector3[] getMoveDirect(bool zeroNeed) {
		Vector3[] move;

		if (zeroNeed) {
			move = new Vector3[5] {
				new Vector3 (0, 0),
				new Vector3 (1, 0),
				new Vector3 (-1, 0),
				new Vector3 (0, 1),
				new Vector3 (0, -1),
			};
		} else {
			move = new Vector3[4] {
				new Vector3 (0, 1),
				new Vector3 (1, 0),
				new Vector3 (0, -1),
				new Vector3 (-1, 0),
			};
		}

		return move;
	}

//	/* 敵の位置から最も離れられる/近くような方向に進めるよう、moveの順序を変化させて出力 */
//	public Vector3[] getMoveDirectWithEnemy(bool zeroNeed, bool upFlg){
//		// 相対距離でいいので、一番近い敵を見出す
//		float dist, leastDist = 1000; int leastNo = -1; Vector3 rPos;
//		for (int i=0; i < init.eInfos.Length; i++) {
//			
//			rPos = init.playerPos - init.eInfos [i].pos;
//			dist = Mathf.Abs (rPos.x) + Mathf.Abs (rPos.y);
//			if (dist < leastDist) {
//				leastDist = dist; leastNo = i;
//			}
//		}
//
//		// 一番近い敵の次の位置から最も離れられる方向を出す
//		Vector3 eNextPos = eb.enemyMove (init.eInfos [leastNo], init.playerPos, init.count);
//		Vector3[] drct = getMoveDirect (zeroNeed); 
//		Vector3[] move = new Vector3[0]; Vector3[] _move;
//		Vector3 pNextPos;
//		int[] allDist = new int[0]; int[] _allDist;
//		for (int i = 0; i < drct.Length; i++) {
//			pNextPos = init.playerPos + drct [i];
//			if (eb.checkCollider(pNextPos, true)) {
//			//if (eb.checkCollider(pNextPos, true, "")) {
//				rPos = pNextPos - eNextPos;
//
//				_allDist = new int[allDist.Length + 1];
//				System.Array.Copy(allDist, _allDist, allDist.Length);
//				_allDist[allDist.Length] = (int)(Mathf.Abs (rPos.x) + Mathf.Abs (rPos.y));
//				allDist = _allDist;
//
//				_move = new Vector3[move.Length + 1];
//				System.Array.Copy(move, _move, move.Length);
//				_move[move.Length] = drct[i];
//				move = _move;
//			}
//		}
//
//		// allDistの中身を基準に昇順にソート
//		bubbleSort (allDist, move, upFlg);
//
//		return move;
//
//	}
		
	void bubbleSort(int[] crt, Vector3[] vec, bool upFlg){
		int i,j;
		int len = crt.Length;
		int tmpcrt; Vector3 tmpvec;
		for(i=0; i<len; i++){
			for(j=len-1; j>i; j--){
				if(upFlg && (crt[j] > crt[j-1]) || !upFlg && (crt[j] < crt[j-1])){
					tmpcrt = crt[j];
					crt[j] = crt[j-1];
					crt[j-1] = tmpcrt;

					tmpvec = vec[j];
					vec[j] = vec[j-1];
					vec[j-1] = tmpvec;
				}
			}
		}
	}

//	void bubbleSort2(float[] crt, int[] index) {
//		int i,j;
//		int len = crt.Length;
//		float tmpcrt; int tmpIndex;
//		for(i=0; i<len; i++){
//			for(j=len-1; j>i; j--){
//				if(crt[j] < crt[j-1]){
//					tmpcrt = crt[j];
//					crt[j] = crt[j-1];
//					crt[j-1] = tmpcrt;
//
//					tmpIndex = index[j];
//					index[j] = index[j-1];
//					index[j-1] = tmpIndex;
//				}
//			}
//		}
//	}

	/* BestWayがあるか否かの判定 */
	public bool checkBestWayExist(Vector3[] BestWay) {
		if (BestWay.Length >= 2)
			return true;
		else
			return false;
	}

	/* BestWayがあった場合の、update後のplayerの動きを取得 */
	public Vector3 get_pPos_byBestWay(Vector3[] BestWay) {
		return BestWay [BestWay.Length - 2];
	}

	/* BestWayがなかった場合の、update後のplayerの動きを取得 */
	public Vector3 get_pPos_whenNoBestWay(nearItemInfo nearItm, Vector3 pPos, eInfo[] eInfos, int cnt) {
//		eInfo dnme = new eInfo("", new Vector3());
//		eInfo nextDnme = new eInfo("", new Vector3());
//
		Vector3[] move = getMoveDirect (true);

		float leastDist = 1000;

		Vector3 resPos;
		Vector3 _resPos = new Vector3 ();
	
		if (nearItm.dist == 1) {
			resPos = pPos;
		} else {
			for (int i = 0; i < move.Length; i++) {
				// 敵の次進む位置を確認
				Vector3 _nextPos = pPos + move [i];

				// 敵や壁とぶつかるか判定
				bool res;
//				if (nearItm.info.inWall) {
//					res = checkColliderWallAndDynamicEnemy (pPos, _nextPos, nearItm.info.pos, dnme, nextDnme, true);
//				} else {
					res = checkColliderWallAndEnemyWall (_nextPos, true);
//				}

				if (res) { // 敵や壁とぶつからなければ
					// 距離が最小なら適用
					Vector3 rPos = nearItm.info.pos - _nextPos;
					float dist = Mathf.Abs (rPos.x) + Mathf.Abs (rPos.y);
					if (dist < leastDist) {
						leastDist = dist;
						_resPos = _nextPos;
					}
				}
			}

			if (leastDist < 1000) {
				resPos = _resPos;
			} else {
				resPos = pPos;
			}
		}

		return resPos;
	}


	/* 訪れたことのある点を記録する箱をつくる */
	public bool[,] createVisitedArray(Vector3 a, Vector3 b) {
		Vector3 rPos = new Vector3 (Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

		bool[,] posHist = new bool[(int)rPos.x, (int)rPos.y]; 
		for (int i = 0; i < posHist.GetLength (0); i++) {
			for (int j = 0; j < posHist.GetLength (1); j++) {
				posHist [i, j] = false; // 初期化
			}
		}

		return posHist;
	}

	/* itemとの距離が1の場合、該当するアイテムを消す */
	public void destroyItem(nearItemInfo itm) {

		GameObject itemObj = GameObject.Find ("item" + itm.no.ToString ());
		Destroy (itemObj);
		init.iInfos [itm.no].pos = new Vector3 ();

	}

	/* itemがあるか否かチェック */
	public bool checkNearItemExist(nearItemInfo itm) {
		if (itm.no >= 0) {
			return true;
		} else {
			return false;
		}
	}

	/* Itemがまだ残っているかチェック */
	public bool checkRestItemExist() {
		for (int i = 0; i < init.iInfos.Length; i++) {
			if (!checkVecEqual (init.iInfos [i].pos, new Vector3 ())) {
				return true;
			}
		}
		return false;
	}

	/* nearItmの内容を初期化 */
	public nearItemInfo initNearItm() {
		return new nearItemInfo (-1, new iInfo (), 500, new bfsPos[0], new Vector3[0]);
	}


	/* 新しい位置が壁とも敵とも衝突しないかどうか確認 */
	public bool checkColliderWallAndEnemyWall(Vector3 NextPos, bool playerFlg) {
		for (int k = 0; k < init.wallPos.Length; k++) {
			if (NextPos.x == init.wallPos [k].x && NextPos.y == init.wallPos [k].y) {
				return false;
			}
		}
		if (!playerFlg) {
			for (int m = 0; m < init.iInfos.Length; m++) {
				Vector3 iPos = init.iInfos [m].pos;
				if (iPos.x != 0 || iPos.y != 0) {
					if (checkVecEqual (iPos, NextPos)) {
						return false;
					}
				}
			}	
		}
//		for (int k = 0; k < init.enemyWallPos.Length; k++) {
//			if (NextPos.x == init.enemyWallPos [k].x && NextPos.y == init.enemyWallPos [k].y) {
//				return false;
//			}
//		}
		return true;
	}


	/* 新しい位置が壁とも動的な敵とも衝突しないかどうか確認 */
	public bool checkColliderWallAndDynamicEnemy(Vector3 nowPos, Vector3 NextPos, Vector3 target, eInfo DNMEnemy, eInfo nextDNMEnemy, bool playerFlg) {

		//eb.sw.WriteLine ("pNow: " + nowPos + " pNext: " + NextPos + " eNow: " + DNMEnemy.pos + " eNext: " + nextDNMEnemy + " target: " + target);
		for (int k = 0; k < init.wallPos.Length; k++) {
			if (NextPos.x == init.wallPos [k].x && NextPos.y == init.wallPos [k].y) {
				return false;
			}
		}
		if (!playerFlg) {
			for (int m = 0; m < init.iInfos.Length; m++) {
				Vector3 iPos = init.iInfos [m].pos;
				if (iPos.x != 0 || iPos.y != 0) {
					if (checkVecEqual (iPos, NextPos)) {
						return false;
					}
				}
			}
		}

		if (nowPos == target) {
			return false;
		}

		// 次移動した時の位置が現在のenemyの位置と一致していた場合、そこも避けなければならない

		if (nextDNMEnemy.pos == NextPos) {
			//sw.WriteLine ("次の移動位置が現在のenemyと一致");
			return false;
		}
		if (nextDNMEnemy.pos == nowPos && DNMEnemy.pos == NextPos) {
			//sw.WriteLine ("入れ替わり交差");
			return false;
		}

		// 次の位置がターゲットとも、敵の次の位置とも同じである場合
		//if (NextPos == target && nowPos == nextDNMEnemy.pos) {
		if (NextPos == target && NextPos == nextDNMEnemy.pos) {
			//sw.WriteLine ("次の位置がターゲットとも、敵の次の位置とも同じ");
			return false;
		}


		return true;
	}

	/* 新しい位置が壁とも動的な敵とも衝突しないかどうか確認 */
	public bool checkColliderWallAndAllDynamicEnemy(Vector3 nowPos, Vector3 NextPos, Vector3 target, eInfo[] DNMEnemy, eInfo[] nextDNMEnemy, bool playerFlg) {

		//eb.sw.WriteLine ("pNow: " + nowPos + " pNext: " + NextPos + " eNow: " + DNMEnemy.pos + " eNext: " + nextDNMEnemy + " target: " + target);
		for (int k = 0; k < init.wallPos.Length; k++) {
			if (NextPos.x == init.wallPos [k].x && NextPos.y == init.wallPos [k].y) {
				return false;
			}
		}
		if (!playerFlg) {
			for (int m = 0; m < init.iInfos.Length; m++) {
				Vector3 iPos = init.iInfos [m].pos;
				if (iPos.x != 0 || iPos.y != 0) {
					if (checkVecEqual (iPos, NextPos)) {
						return false;
					}
				}
			}
		}

		if (nowPos == target) {
			return false;
		}

		int len = nextDNMEnemy.Length;
		// 次移動した時の位置が現在のenemyの位置と一致していた場合、そこも避けなければならない
		for (int i=0; i<len; i++) {
			if (nextDNMEnemy[i].pos == NextPos) {
				//sw.WriteLine ("次の移動位置が現在のenemyと一致");
				return false;
			}
		}
		for (int i=0; i<len; i++) {
			if (nextDNMEnemy[i].pos == nowPos && DNMEnemy[i].pos == NextPos) {
				//sw.WriteLine ("入れ替わり交差");
				return false;
			}
		}

		// 次の位置がターゲットとも、敵の次の位置とも同じである場合
		for (int i=0; i<len; i++) {
			//if (NextPos == target && nowPos == nextDNMEnemy.pos) {
			if (NextPos == target && NextPos == nextDNMEnemy[i].pos) {
				//sw.WriteLine ("次の位置がターゲットとも、敵の次の位置とも同じ");
				return false;
			}
		}


		return true;
	}


	/* enemy(敵)同士がぶつかった時の、道を譲らない優先度を出す */
	public bool checkRankOfEnemy(int i, int j) {
		string type1 = init.eInfos [i].type;
		string type2 = init.eInfos [j].type;
		string[] rank = new string[5] { "B", "A", "C", "D", "E" };
		int rank1 = -1, rank2 = -1;
		for (int n = 0; n < rank.Length; n++) {
			if (rank [n] == type1)
				rank1 = n;
			if (rank [n] == type2)
				rank2 = n;
		}

		if (rank1 >= rank2) {
			return true;
		} else {
			return false;
		}
	}

	/* キーボードの入力に合わせてplayerの動きを返す */
	public Vector3 get_pPosByKey() {
		Vector3 pNewPos = new Vector3 ();
		if (init.key_pFlg == "up") {
			pNewPos = new Vector3 (init.playerPos.x, init.playerPos.y + 1);
		} else if (init.key_pFlg == "down") {
			pNewPos = new Vector3 (init.playerPos.x, init.playerPos.y - 1);
		} else if (init.key_pFlg == "right") {
			pNewPos = new Vector3 (init.playerPos.x + 1, init.playerPos.y);
		} else if (init.key_pFlg == "left") {
			pNewPos = new Vector3 (init.playerPos.x - 1, init.playerPos.y);
		} else if (init.key_pFlg == "space") {
			pNewPos = init.playerPos;
		}
		return pNewPos;
	}


}
