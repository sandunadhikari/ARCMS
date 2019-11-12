using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using LitJson;


public class LoadManifest : MonoBehaviour
{
    public string url;
    string filename = "manifest.json";
    string path;
    //GameData gameData=new GameData();
    //MyClass myclass = new MyClass ();
    public Button startButton;
    // Use this for initialization
    IEnumerator Start()
    {

        using (var www = new WWW("http://127.0.0.1:8000/api/manifest"))
        {
            yield return www;
            creature walid = JsonUtility.FromJson<creature>(www.text);
            url = walid.manifest;
            Debug.Log(url);
            StartCoroutine(SetPath());
        }

    }
    public class creature
    {
        public string manifest;

    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator SetPath()
    {
        path = Application.persistentDataPath + "/" + filename;

        using (WWW www = new WWW(url))
        {
            yield return www;
            byte[] WriteBytes = www.bytes;
            File.WriteAllBytes(path, WriteBytes);
            ReadallAssets();
        }


    }
    void ReadallAssets()
    {
        try
        {
            if (File.Exists(path))
            {
                startButton.gameObject.SetActive(true);
            }
            else
            {
                Debug.Log("File does not exist");

            }

        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

}

