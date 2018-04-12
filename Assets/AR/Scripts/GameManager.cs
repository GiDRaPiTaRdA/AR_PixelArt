using System;
using AR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {

    public static void Log(object message)
    {
        Debug.Log($"Unity {message}");
    }

    public void TogglePlanes(Boolean value)
    {


        ARController controller = GameObject.FindObjectOfType<ARController>();
        controller.planes.ForEach(p=>p.SetActive(value));

        PointcloudVisualizer pointCloud = GameObject.FindObjectOfType<PointcloudVisualizer>();
        pointCloud.gameObject.SetActive(value);

        controller.PlanesSearch = value;
    }

    public void AddBlock()
    {
        FindObjectOfType<ARController>().AddBlockFunc();
    }

    public void RemoveBlock()
    {
        FindObjectOfType<ARController>().RemoveBlockFunc();
    }


    public void Reset()
    {
        SceneManager.LoadScene(0);
    }


    private static Dictionary<Vector3, GameObject> blocks;

    public static Dictionary<Vector3, GameObject> Blocks => blocks ?? (blocks = new Dictionary<Vector3, GameObject>());

    public static GameObject InitiateBlockObject(GameObject prefab, Vector3 origin)
    {
        GameObject gameObj = null;

        if (!Blocks.ContainsKey(origin))
        {
            gameObj = Instantiate(prefab, origin, Quaternion.identity);

            GameManager.Blocks.Add(origin, gameObj);

        }

        return gameObj;
    }

    public static Vector3 GetCenterOfScreenPosition()
    {
        var cameraPos = Camera.current.transform.position;
        cameraPos.x += Screen.width / 2;
        cameraPos.y += Screen.height / 2;

        return cameraPos;
    }

    public static void DestroyBlockObject(Vector3 origin)
    {
        if (Blocks.ContainsKey(origin))
        {
            GameObject gm = Blocks[origin];

            Blocks.Remove(origin);

            Destroy(gm);
        }
    }
    public static void DestroyBlockObject(GameObject gameObject) =>
        DestroyBlockObject(gameObject.transform.position);
}
