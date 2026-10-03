using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARWallSpawner : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;

    public static Pose LastWallPose { get; private set; }
    public static bool HasWallPose { get; private set; } = false;
    public static bool IsWaitingForWallScan { get; private set; }

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private static int scanStartedFrame = -1;

    public static void BeginWallScan()
    {
        HasWallPose = false;
        IsWaitingForWallScan = true;
        scanStartedFrame = Time.frameCount;
    }

    public static void CancelWallScan()
    {
        IsWaitingForWallScan = false;
    }

    private void Update()
    {
        if (!IsWaitingForWallScan || Time.frameCount == scanStartedFrame) return;

        Vector2 touchPos = Vector2.zero;
        bool hasInput = false;
        int pointerId = -1;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            touchPos = Input.GetTouch(0).position;
            pointerId = Input.GetTouch(0).fingerId;
            hasInput = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            touchPos = Input.mousePosition;
            hasInput = true;
        }

        if (hasInput)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId)) return;

            if (raycastManager != null && raycastManager.Raycast(touchPos, hits, TrackableType.Planes))
            {
                ARRaycastHit hit = hits[0];
                Pose hitPose = hit.pose;

                Vector3 wallNormal = hitPose.up;
                Camera camera = Camera.main;
                if (camera != null && Vector3.Dot(wallNormal, camera.transform.position - hitPose.position) < 0f)
                {
                    wallNormal = -wallNormal;
                }

                Quaternion targetRot = wallNormal != Vector3.zero
                    ? Quaternion.LookRotation(-wallNormal, Vector3.up)
                    : hitPose.rotation;

                Vector3 wallPosition = hitPose.position;
                if (camera != null)
                {
                    Ray centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                    Plane wallPlane = new Plane(wallNormal, hitPose.position);
                    if (wallPlane.Raycast(centerRay, out float distance) && distance >= 0f)
                    {
                        Vector3 centerPoint = centerRay.GetPoint(distance);
                        Vector3 stageRight = targetRot * Vector3.right;
                        wallPosition += stageRight * Vector3.Dot(centerPoint - wallPosition, stageRight);
                    }
                }

                SetWallPose(new Pose(wallPosition, targetRot));
                Debug.Log("Pared AR detectada. Pulsa Iniciar combate para continuar.");
            }

#if UNITY_EDITOR
            if (!HasWallPose)
            {
                Camera camera = Camera.main;
                if (camera != null)
                {
                    Vector3 testPos = camera.transform.position + camera.transform.forward * 1.8f;
                    Quaternion testRot = Quaternion.LookRotation(camera.transform.forward, Vector3.up);
                    SetWallPose(new Pose(testPos, testRot));
                    Debug.Log("Modo Editor PC: pared de prueba registrada frente a la cámara.");
                }
            }
#endif
        }
    }

    private static void SetWallPose(Pose pose)
    {
        LastWallPose = pose;
        HasWallPose = true;
        IsWaitingForWallScan = false;
    }
}