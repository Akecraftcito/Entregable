using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARWallSpawner : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject worldSpaceMenuPrefab;

    private GameObject spawnedMenuInstance;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        if (spawnedMenuInstance != null) return;

        Vector2 touchPos = Vector2.zero;
        bool hasInput = false;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            touchPos = Input.GetTouch(0).position;
            hasInput = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            touchPos = Input.mousePosition;
            hasInput = true;
        }

        if (hasInput && raycastManager != null)
        {
            if (raycastManager.Raycast(touchPos, hits, TrackableType.Planes))
            {
                ARRaycastHit hit = hits[0];
                Pose hitPose = hit.pose;

                // 1. Instanciar en la posición del impacto
                spawnedMenuInstance = Instantiate(worldSpaceMenuPrefab, hitPose.position, Quaternion.identity);

                // 2. Orientar plano hacia la normal de la pared (Alineación 2D plana)
                Vector3 wallNormal = hitPose.up; // La normal en los planos de AR Foundation es el eje UP (+Y)
                if (wallNormal != Vector3.zero)
                {
                    spawnedMenuInstance.transform.rotation = Quaternion.LookRotation(-wallNormal, Vector3.up);
                }

                Debug.Log("¡Canvas pegado perfectamente plano contra la pared!");
            }
        }
    }
}