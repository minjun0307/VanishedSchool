// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class DownStair : MonoBehaviour
// {
//     public string m_TransferMapName; // 로드할 맵 이름 (도착지)
//     public string m_CurrentMapName;  // 언로드할 맵 이름 (현재 맵)
//     public bool m_IsActive = false;
//     public bool m_IsActive2 = false;
//     public bool m_ChecktheScene = false;
//     void Start()
//     {

//     }
//     void WhatSCENEAREWEAT()
//     {
//         int SceneIdx = SceneManager.GetActiveScene().buildIndex;
//         if (SceneIdx == 1)
//         {
//             Debug.Log("cur scene is 1");
//             m_IsActive = true;

//         }
//         if (SceneIdx == 2)
//         {
//             Debug.Log("cur scene is 2");
//             m_IsActive2 = true;
//         }


//     }
//     void Update()
//     {

//     }

//     // 맵을 불러오거나 다시 켜는 기능 (요청하신 '다시 여는 스크립트')
//     public void OpenScene(string sceneName)
//     {
//         if (string.IsNullOrEmpty(sceneName)) return;

//         Scene targetScene = SceneManager.GetSceneByName(sceneName);

//         // 1. 씬이 아예 안 열려있으면 새로 열기 (Additive)
//         if (!targetScene.IsValid())
//         {
//             SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
//         }
//         else
//         {
//             // 2. 이미 열려 있다면 비활성화되어 있던 것을 다시 켜기 (카메라/오디오리스너 켜짐)
//             ToggleSceneState(sceneName, true);
//         }
//     }

//     // 맵을 끄는 기능 (요청하신 '사용하지 않는 씬 비활성화 스크립   트')
//     public void CloseScene(string sceneName)
//     {
//         if (string.IsNullOrEmpty(sceneName)) return;

//         // 씬을 메모리에서 지우지(Unload) 않고, 잠재우기
//         ToggleSceneState(sceneName, false);
//     }

//     // 씬 안의 모든 오브젝트(카메라/오디오리스너 등)를 일괄적으로 끄거나 켜는 함수
//     private void ToggleSceneState(string sceneName, bool isActive)
//     {
//         Scene targetScene = SceneManager.GetSceneByName(sceneName);
//         if (!targetScene.IsValid() || !targetScene.isLoaded) return;

//         // 해당 씬의 최상위 게임 오브젝트들을 모두 가져옵니다.
//         GameObject[] rootObjects = targetScene.GetRootGameObjects();
//         foreach (GameObject go in rootObjects)
//         {
//             // 끄면 안 되는 매니저나 플레이어가 같은 씬에 있다면 무시
//             if (go.CompareTag("Player") || go.name.Contains("Mgr") || go.name.Contains("Manager"))
//                 continue;

//             go.SetActive(isActive);
//         }
//     }

//     private void OnCollisionEnter2D(Collision2D collision)
//     {
//         StairSystem(collision);
//     }
//     public void StairSystem(Collision2D collision)
//     {
//         if (m_IsActive != false)
//         {
//             if (collision.collider.tag == "Player")
//             {
//                 Debug.Log($"이동 포탈 작동! 현재 맵: {m_CurrentMapName} -> 다음 맵: {m_TransferMapName}");

//                 // 1. 기존 씬을 먼저 '비활성화' (이전 카메라 & 오디오리스너가 즉시 꺼집니다)
//                 CloseScene(m_CurrentMapName);

//                 // 2. 새로운 씬을 엽니다 (카메라 & 오디오리스너 켜짐 - 중첩 경고 해결!)
//                 OpenScene(m_TransferMapName);

//                 if (GameMgr.Inst() != null)
//                 {
//                     GameMgr.Inst().m_GameScene.m_GameUI.m_Player.gameObject.SetActive(false);
//                 }
//             }
//         }

//     }

// }
