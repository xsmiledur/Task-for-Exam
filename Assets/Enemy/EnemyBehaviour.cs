using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using UnityEngine;
using UnityEditor;

public class nearItemInfo {
	public int no;
	public iInfo info;
	public float dist;
	public bfsPos[] bfsPos;
	public Vector3[] bestWay;
	public nearItemInfo(int _no, iInfo _info, float _dist, bfsPos[] _bfsPos, Vector3[] _bestWay) {
		no = _no;
		info = _info;
		dist = _dist;
		bfsPos = _bfsPos;
		bestWay = _bestWay;
	}
	public void setBestWay(Vector3[] BestWay) {
		bestWay = BestWay;
	}
}

//public class bfsPos {
//	public Vector3 pos;
//	public Vector3[] ch;
//	public Vector3 pr;
//	public int no;
//	public float bfsDist = 0;
//	public bfsPos(Vector3 pos_, Vector3 pr_, Vector3[] ch_, int no_) {
//		pos = pos_;
//		pr = pr_;
//		ch = ch_;
//		no = no_;
//	}
//	public Vector3 getParent() {
//		return pr;
//	}
//	public void set_bfsDist(float _bfsDist) {
//		bfsDist = _bfsDist;
//	}
//}

public class bfsPos {
	public Vector3 pos;
	public int prNo;
	public int[] chNo;
	public int layNo;
	public float bfsDist = 0;
	public bfsPos(Vector3 _pos, int _layNo, int _prNo, int[] _chNo) {
		pos = _pos;
		layNo = _layNo;
		prNo = _prNo;
		chNo = _chNo;
	}
	public int getParent() {
		return prNo;
	}
	public void set_bfsDist(float _bfsDist) {
		bfsDist = _bfsDist;
	}
}

public class EnemyBehaviour : MonoBehaviour {
	
	private GameObject initObj;
	private InitBehaviour init;

	private Methods mt;

	public StreamWriter sw;
	private bool swFlg = false;

	Vector3[] BestWayToGoal = new Vector3[0];

	/* 一時停止 */
	[MenuItem ("Custom/Pause")]
	private static void Pause () {
		EditorApplication.isPaused = true;
	}

	// Use this for initialization
	void Start () {


		initObj = GameObject.Find( "Player" );
		init = initObj.GetComponent<InitBehaviour>();

		mt = initObj.GetComponent<Methods> ();




	}

	/* **************
	 * 
	 * Update
	 * 
	 * **************/

	/* プレイヤーの動きを定義 */
	bool trackGoalFlg = false;
	int goalCnt;

	private void playerMove() {


		// nearItmがリセットされていて、かつ残りのItemがある場合
		if (mt.checkRestItemExist ()) {
			if (!mt.checkNearItemExist (init.nearItm)) {
				mt.getNearItemInfo (); // 一番近いitem情報を出す

				sw.WriteLine ("\nbfsPos");
				bfsPos[] bfsPos =  init.nearItm.bfsPos;
				for (int i = 0; i < bfsPos.Length; i++) {
					sw.Write ("layNo: " + bfsPos [i].layNo.ToString() + " pr: " + bfsPos [i].prNo.ToString() + " pos: " + bfsPos [i].pos + " ch: ");
					for (int j = 0; j < bfsPos [i].chNo.Length; j++) {
						sw.Write (bfsPos [i].chNo [j].ToString() + " ");
					}
					sw.WriteLine ();
				}
				sw.WriteLine ("=====");

				// 最適経路を作成
				Vector3[] BestWay = mt.createBestWay (init.nearItm.info.pos, init.nearItm.bfsPos, (int)init.nearItm.dist);
				// 作成した最適経路はnearItmに格納する
				init.nearItm.setBestWay (BestWay);

				sw.WriteLine ("BestWay len: " + BestWay.Length.ToString());
				for (int i = 0; i < BestWay.Length; i++) {
					sw.WriteLine (BestWay [i]);
				}
			}

			// 格納されているBestWayに沿って動く
			Vector3 pNewPos = new Vector3();
			if (mt.checkBestWayExist (init.nearItm.bestWay)) {
				pNewPos = init.nearItm.bestWay [(int)init.nearItm.dist - 1];
			} else {// BestWayがなかった場合は、アイテムとの距離が2以上ならその方向に進んでみる、1なら停止
				print("noBestWay");
				pNewPos = mt.get_pPos_whenNoBestWay (init.nearItm, init.playerPos, init.eInfos, init.count);
			}

			init.updtMem.setUpdtPlayer( pNewPos, true);
			init.nearItm.dist--;
			sw.WriteLine ("dist: " + init.nearItm.dist + " no: " + init.nearItm.no.ToString());

			// itemとの距離が0の場合、該当するアイテムを消し、nearItmを初期化する
			if (init.nearItm.dist == 0) {
				print ("dist0");
				sw.WriteLine ();
				mt.destroyItem (init.nearItm);
				init.iInfos [init.nearItm.no].pos = new Vector3 ();
				init.nearItm = mt.initNearItm ();
			}
		} else { // goalを追う
			if (!trackGoalFlg) {

				// 最短経路を作成
				BestWayToGoal = mt.trackGoal ();

				// Flgを立てる
				trackGoalFlg = true;
				goalCnt = 1;

			}

			if (BestWayToGoal.Length > goalCnt) {
				init.updtMem.setUpdtPlayer (BestWayToGoal [BestWayToGoal.Length - 1 - goalCnt], true);
			}
			goalCnt = mt.checkGoal (goalCnt, BestWayToGoal);

		}

	}

	/* 敵の動きを定義 */
	int iA, iB, iC, iD, iE;
	public Vector3 enemyMove(eInfo eInfo, int index, Vector3 pPos) {
		Vector3 rPos = pPos - eInfo.pos;

		if (eInfo.type == "A") {
			if (rPos.y != 0) {
				Vector3 eNextPos = eInfo.pos + new Vector3 (0, rPos.y / Mathf.Abs (rPos.y));
				if (checkEnemyCollider (eNextPos, index)) {
					return eNextPos;
				}
			}
			if (rPos.x != 0) {
				Vector3 eNextPos = eInfo.pos + new Vector3 (rPos.x / Mathf.Abs(rPos.x), 0);
				if (checkEnemyCollider (eNextPos, index)) {
					return eNextPos;
				}
			}
			Vector3[] moveA= new Vector3[4] { new Vector3(0,-1), new Vector3(-1,0), new Vector3(0,+1), new Vector3(+1,0) };
			for (iA = 0; iA < moveA.Length; iA++) {
				Vector3 eNextPos = eInfo.pos + moveA[iA];
				if (checkEnemyCollider (eNextPos, index)) {
					return eNextPos;
				}
			}
		} else if (eInfo.type == "B") {
			if (rPos.x != 0) {
				Vector3 eNextPos = eInfo.pos + new Vector3 (rPos.x / Mathf.Abs (rPos.x), 0);
				if (checkEnemyCollider (eNextPos, index)) {
					return eNextPos;
				}
			} if (rPos.y != 0) {
				Vector3 eNextPos = eInfo.pos + new Vector3 (0, rPos.y / Mathf.Abs (rPos.y));
				if (checkEnemyCollider (eNextPos, index)) {
					return eNextPos;
				}
			}
			Vector3[] moveB = new Vector3[4] { new Vector3(0,+1), new Vector3(-1,0), new Vector3(0,-1), new Vector3(+1,0) };
			for (iB = 0; iB < moveB.Length; iB++) {
				Vector3 eNextPos = eInfo.pos + moveB [iB];
				if (checkEnemyCollider (eNextPos, index)) {
					return eNextPos;
				}
			}
		} else if (eInfo.type == "C" || eInfo.type == "D" || eInfo.type == "E") {
			return mt.enemyMoveCDE (index, eInfo, pPos);
		}
		return new Vector3 ();
	}



	/* 敵の動きに対するCollider判定 新しい位置が衝突位置でないかどうか確認 */
	public bool checkEnemyCollider(Vector3 NextPos, int eIndex) {
		int m;
		for (m = 0; m < init.wallPos.Length; m++) {
//			if (init.wallPos [m].x == NextPos.x) {
//				//print (" nextPos: " + NextPos + " " + mt.checkVecEqual (init.wallPos [m], NextPos));
//			}
			if (mt.checkVecEqual(init.wallPos [m], NextPos)) {
				return false;
			}
		}

		for (m = 0; m < init.iInfos.Length; m++) {
			Vector3 iPos = init.iInfos [m].pos;
			if (iPos.x != 0 || iPos.y != 0) {
				if (mt.checkVecEqual (iPos, NextPos)) {
					return false;
				}
			}
		}

		if (eIndex >= 0) {
			for (int i = 0; i < init.eInfos.Length; i++) {
				if (i != eIndex) {
					Vector3 ePos_ = init.eInfos [i].pos;
					float dist = Mathf.Abs (NextPos.x - ePos_.x) + Mathf.Abs (NextPos.y - ePos_.y);

//					if (this.name == "Player") {
//						sw.WriteLine ("dist: " + dist.ToString());
//					}
					// 距離が0になってしまうの場合
					if (dist == 0) {
						return false;

					} 
					// 距離が1の場合
					else if (dist == 1) {
						// eIndex番号の小さい方が優先的に進めるという設定で。
						if (mt.checkRankOfEnemy(eIndex, i)) {
							// 何もしない(番号iの方が遠慮して進むことにする)
						} else {
							// enemy_ の次の動きと被った場合false
							Vector3 e_NextPos = enemyMove (init.eInfos[i], i, init.playerPos);
							if (e_NextPos == NextPos) {
								return false;
							}

						}
					}
				}
			}
		}
		return true;
	}

	public Vector3 bfs_enemyMove(eInfo[] eInfos, int index, Vector3 pPos) {
		Vector3 rPos = pPos - eInfos[index].pos;

		if (eInfos[index].type == "A") {
			if (rPos.y != 0) {
				Vector3 eNextPos = eInfos[index].pos + new Vector3 (0, rPos.y / Mathf.Abs (rPos.y));
				if (check_bfsEnemyCollider (eNextPos, index, eInfos)) {
					return eNextPos;
				}
			}
			if (rPos.x != 0) {
				Vector3 eNextPos = eInfos[index].pos + new Vector3 (rPos.x / Mathf.Abs(rPos.x), 0);
				if (check_bfsEnemyCollider (eNextPos, index, eInfos)) {
					return eNextPos;
				}
			}
			Vector3[] moveA= new Vector3[4] { new Vector3(0,-1), new Vector3(-1,0), new Vector3(0,+1), new Vector3(+1,0) };
			for (iA = 0; iA < moveA.Length; iA++) {
				Vector3 eNextPos = eInfos[index].pos + moveA[iA];
				if (check_bfsEnemyCollider (eNextPos, index, eInfos)) {
					return eNextPos;
				}
			}
		} else if (eInfos[index].type == "B") {
			if (rPos.x != 0) {
				Vector3 eNextPos = eInfos[index].pos + new Vector3 (rPos.x / Mathf.Abs (rPos.x), 0);
				if (check_bfsEnemyCollider (eNextPos, index, eInfos)) {
					return eNextPos;
				}
			} if (rPos.y != 0) {
				Vector3 eNextPos = eInfos[index].pos + new Vector3 (0, rPos.y / Mathf.Abs (rPos.y));
				if (check_bfsEnemyCollider (eNextPos, index, eInfos)) {
					return eNextPos;
				}
			}
			Vector3[] moveB = new Vector3[4] { new Vector3(0,+1), new Vector3(-1,0), new Vector3(0,-1), new Vector3(+1,0) };
			for (iB = 0; iB < moveB.Length; iB++) {
				Vector3 eNextPos = eInfos[index].pos + moveB [iB];
				if (check_bfsEnemyCollider (eNextPos, index, eInfos)) {
					return eNextPos;
				}
			}
		} else if (eInfos[index].type == "C" || eInfos[index].type == "D" || eInfos[index].type == "E") {
			return mt.enemyMoveCDE (index, eInfos[index], pPos);
		}
		return new Vector3 ();
	}


	public bool check_bfsEnemyCollider(Vector3 NextPos, int eIndex, eInfo[] eInfos) {
		int m;
		for (m = 0; m < init.wallPos.Length; m++) {
			//			if (init.wallPos [m].x == NextPos.x) {
			//				//print (" nextPos: " + NextPos + " " + mt.checkVecEqual (init.wallPos [m], NextPos));
			//			}
			if (mt.checkVecEqual(init.wallPos [m], NextPos)) {
				return false;
			}
		}

		for (m = 0; m < init.iInfos.Length; m++) {
			Vector3 iPos = init.iInfos [m].pos;
			if (iPos.x != 0 || iPos.y != 0) {
				if (mt.checkVecEqual (iPos, NextPos)) {
					return false;
				}
			}
		}

		if (eIndex >= 0) {
			for (int i = 0; i < eInfos.Length; i++) {
				if (i != eIndex) {
					Vector3 ePos_ = eInfos [i].pos;
					float dist = Mathf.Abs (NextPos.x - ePos_.x) + Mathf.Abs (NextPos.y - ePos_.y);

					if (this.name == "Player") {
						sw.WriteLine ("dist: " + dist.ToString());
					}
					// 距離が0になってしまうの場合
					if (dist == 0) {
						return false;

					} 
					// 距離が1の場合
					else if (dist == 1) {
						// eIndex番号の小さい方が優先的に進めるという設定で。
						if (mt.checkRankOfEnemy(eIndex, i)) {
							// 何もしない(番号iの方が遠慮して進むことにする)
						} else {
							// enemy_ の次の動きと被った場合false
							Vector3 e_NextPos = bfs_enemyMove (eInfos, i, NextPos);
							sw.WriteLine ("相手のenemyのpos" + ePos_ +  " nextPos: " + e_NextPos);
							if (e_NextPos == NextPos) {
								return false;
							}

						}
					}
				}
			}
		}
		return true;
	}

	/* playerの動きに対するCollier判定 */
	public bool checkPlayerCollider(Vector3 nowPos, Vector3 nextPos, eInfo[] dnme, eInfo[] dnmeNext, Vector3 target) {
		int m;
		for (m = 0; m < init.wallPos.Length; m++) {
			if (mt.checkVecEqual(init.wallPos [m], nextPos)) {

				return false;
			}
		}

//		if (nowPos == target) {
//			return false;
//		}

		int len = dnme.Length;
		if (dnme.Length > 0) {
			// 次移動した時の位置が現在のenemyの位置と一致していた場合、そこも避けなければならない
			for (int i = 0; i < len; i++) {
				if (dnmeNext [i].pos == nextPos) {
					//sw.WriteLine ("次の移動位置が現在のenemyと一致");
					return false;
				}
			}
			for (int i = 0; i < len; i++) {
				if (dnmeNext [i].pos == nowPos && dnme [i].pos == nextPos) {
					//sw.WriteLine ("入れ替わり交差");
					return false;
				}
			}

			// 次の位置がターゲットとも、敵の次の位置とも同じである場合
			for (int i = 0; i < len; i++) {
				if (nextPos == target && nextPos == dnmeNext [i].pos) {
					//sw.WriteLine ("次の位置がターゲットとも、敵の次の位置とも同じ");
					return false;
				}
			}
		}
			
		return true;
	}

	// Update is called once per frame



	void Update () {

		if (!init.KeyPlay) {
			if (mt.checkFlg ("time", this.name)) {
				// timeFlg==trueの検知を一瞬にしたいので、即座にfalseにする必要がある
				mt.makeFlgFalse ("time", this.name);

				if (this.tag == "player") {
					sw = new StreamWriter ("Assets/Log/LogData.txt", swFlg); //true=追記 false=上書き
					swFlg = true;
				}

				if (this.tag == "player") {

					/* プレイヤーの動きを決定 */
					playerMove ();


//					/* 残り時間を更新 */
//					mt.updateTime ();


				} else if (this.name.Substring (0, 5) == "Enemy") {

					/* 敵の動きを決定 */
					int index = Int32.Parse (this.name.Substring (5));

					Vector3 ePos = enemyMove (init.eInfos [index], index, init.playerPos);
					Vector3 e_rPos = ePos - init.eInfos [index].pos;
					eInfo eInfo = new eInfo (init.eInfos [index].type, ePos, e_rPos);

					init.updtMem.setUpdtEnemy (index, eInfo, true);

				}


				if (this.tag == "player") {
					sw.Flush ();
					sw.Close ();
				}


			}

		} else {
			if (mt.checkFlg("key", this.name)) {
				if (mt.checkFlg ("time", this.name)) {
					// timeFlg==trueの検知を一瞬にしたいので、即座にfalseにする必要がある
					mt.makeFlgFalse ("time", this.name);
			
					if (this.tag == "player") {
						
						Vector3 pNewPos = mt.get_pPosByKey ();

						for (int i = 0; i < init.iInfos.Length; i++) {
							if (mt.checkVecEqual (init.iInfos [i].pos, pNewPos)) {
								GameObject itemObj = GameObject.Find ("item" + i.ToString ());
								Destroy (itemObj);
								init.iInfos [i].pos = new Vector3 ();
								break;
							}
						}
						init.updtMem.setUpdtPlayer (pNewPos, true);

					} else if (this.name.Substring (0, 5) == "Enemy") {
						int index = Int32.Parse (this.name.Substring (5));
			
						Vector3 ePos = enemyMove (init.eInfos [index], index, init.playerPos);
						Vector3 e_rPos = ePos - init.eInfos [index].pos;
						eInfo eInfo = new eInfo (init.eInfos [index].type, ePos, e_rPos);

						init.updtMem.setUpdtEnemy (index, eInfo, true);
				
					}
					mt.makeFlgFalse ("key", this.name);
				}

			}


		}

	}
}
