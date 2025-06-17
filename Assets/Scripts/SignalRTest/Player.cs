using System;
using UnityEngine;

namespace Test
{
    public class Player : MonoBehaviour
    {
        
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        
        [Header("sync delay")]
        public float syncDelay = 0.5f;

        private float curTime = 0f;

        public long suid;
        
        [Header("Smooth Movement Settings")]
        public float lerpSpeed = 10f;           // 보간 속도
        public float snapDistance = 5f;         // 즉시 이동할 거리 임계값
    
        private Vector3 targetPosition;         // 목표 위치
        private Vector3 lastPosition;           // 이전 위치
        private bool isMoving = false;
        
        private void Update()
        {
            if (UserData.Instance.Suid == suid)
            {
                // 방향키 입력 감지
                float horizontal = Input.GetAxis("Horizontal"); // A/D 키 또는 왼쪽/오른쪽 화살표
                float vertical = Input.GetAxis("Vertical");     // W/S 키 또는 위/아래 화살표
        
                // 이동 벡터 계산
                Vector3 movement = new Vector3(horizontal, 0f, vertical);

                if (movement.magnitude >= 0.1f)
                {
                    // 시간에 따른 이동 (프레임 독립적)
                    transform.Translate(movement * (moveSpeed * Time.deltaTime));
            
                    curTime += Time.deltaTime;
                    if (curTime >= syncDelay)
                    {
                        curTime = 0f;
                        _ = RoomManager.Instance.SyncPlayerPositionAsync();
                    }     
                }
            }
            else
            {
                if (isMoving && Vector3.Distance(transform.position, targetPosition) > 0.01f)
                {
                    transform.position = Vector3.Lerp(transform.position, targetPosition, 
                        lerpSpeed * Time.deltaTime);
                }
                else if (isMoving)
                {
                    transform.position = targetPosition;
                    isMoving = false;
                }
            }
        }
        
        public void SetTargetPosition(float x, float y, float z)
        {
            Vector3 newPosition = new Vector3(x, y, z);
        
            // 거리가 너무 멀면 즉시 이동 (텔레포트, 스폰 등)
            if (Vector3.Distance(transform.position, newPosition) > snapDistance)
            {
                transform.position = newPosition;
                targetPosition = newPosition;
                isMoving = false;
            }
            else
            {
                targetPosition = newPosition;
                isMoving = true;
            }
        }
    }
}