using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalCameraManager : MonoBehaviour
{
    private Camera camera;

    // Start is called before the first frame update
    void Start()
    {
        camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DisableCamera(string message)
    {
        GameObject[] objects = (FindObjectsOfType<GameObject>() as GameObject[]);
        foreach (GameObject gameObject in objects)
        {
            if (gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
            }
        }
    }

    public void EnableCamera(string message)
    {
        GameObject newGameObject = Instantiate(Resources.Load("AR_Prefab")) as GameObject;
    }

}
