using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Image = UnityEngine.UI.Image;

public class CubeColorBehaviour : MonoBehaviour
{

    public static Color CubeColor { get; private set; } = Color.white;

    private float hitLength = 1000f;


    // Use this for initialization
    void Start()
    {

    }

    public void GetColorAndSetCube()
    {
        var cameraPos = GameManager.GetCenterOfScreenPosition();

        Ray ray = Camera.current.ScreenPointToRay(cameraPos);

        RaycastHit hHit;

        if (Physics.Raycast(ray, out hHit, this.hitLength))
        {
            CubeColor = hHit.transform.gameObject.GetComponent<MeshRenderer>().material.color;
        }
        else
        {
            var rect = Camera.current.pixelRect;
            Texture2D tex = new Texture2D((int) rect.width, (int) rect.height, TextureFormat.RGB24, false);
            tex.ReadPixels(rect, 0, 0, false);
            tex.Apply();

            CubeColor = tex.GetPixel((int)rect.center.x + 5, (int)rect.center.y + 5);
        }

        this.gameObject.GetComponent<Image>().color = CubeColor;
    }
}
