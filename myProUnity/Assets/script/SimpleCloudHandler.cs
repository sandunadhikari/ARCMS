using System;
using UnityEngine;
using Vuforia;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;


/// <summary>
/// This MonoBehaviour implements the Cloud Reco Event handling for this sample.
/// It registers itself at the CloudRecoBehaviour and is notified of new search results.
/// </summary>
public class SimpleCloudHandler : MonoBehaviour, ICloudRecoEventHandler
{
	#region PRIVATE_MEMBER_VARIABLES

	// CloudRecoBehaviour reference to avoid lookups
	private CloudRecoBehaviour mCloudRecoBehaviour;
	// ImageTracker reference to avoid lookups
	private ObjectTracker mImageTracker;

	private bool mIsScanning = false;
    public static string mTargetMetadata = null;

	#endregion // PRIVATE_MEMBER_VARIABLES



	#region EXPOSED_PUBLIC_VARIABLES

	/// <summary>
	/// can be set in the Unity inspector to reference a ImageTargetBehaviour that is used for augmentations of new cloud reco results.
	/// </summary>
	public ImageTargetBehaviour ImageTargetTemplate;
    string filename = "manifest.json";
    string path;
    GameData gameData = new GameData();
    public List<LevelInfo> info;
    public float progresspercent;
    public GameObject slider;
    GameObject newImageTarget;
    public List<string> scanImageName;


    #endregion

    #region UNTIY_MONOBEHAVIOUR_METHODS

    /// <summary>
    /// register for events at the CloudRecoBehaviour
    /// </summary>
    void Start()
	{
		// register this event handler at the cloud reco behaviour
		CloudRecoBehaviour cloudRecoBehaviour = GetComponent<CloudRecoBehaviour>();
		if (cloudRecoBehaviour)
		{
			cloudRecoBehaviour.RegisterEventHandler(this);
		}

		// remember cloudRecoBehaviour for later
		mCloudRecoBehaviour = cloudRecoBehaviour;

        try
        {
            path = Application.persistentDataPath + "/" + filename;
            Debug.Log(path);
            if (File.Exists(path))
            {
                string items = File.ReadAllText(path);
                gameData = JsonUtility.FromJson<GameData>(items);

                foreach (Quests q in gameData.contents)
                {
                    LevelInfo lst = new LevelInfo();
                    lst.name = q.name;
                    lst.model = q.model;
                    info.Add(lst);
                }

            }
            else
            {
                Debug.Log("Unable to read the save data,file does not exist");
                gameData = new GameData();
            }

        }
        catch (System.Exception ex)
        {

            Debug.Log(ex.Message);
        }

    }

	#endregion // UNTIY_MONOBEHAVIOUR_METHODS


	#region ICloudRecoEventHandler_IMPLEMENTATION

	/// <summary>
	/// called when TargetFinder has been initialized successfully
	/// </summary>
	public void OnInitialized()
	{
		// get a reference to the Image Tracker, remember it
		mImageTracker = (ObjectTracker)TrackerManager.Instance.GetTracker<ObjectTracker>();
	}

	/// <summary>
	/// visualize initialization errors
	/// </summary>
	public void OnInitError(TargetFinder.InitState initError)
	{
	}

	/// <summary>
	/// visualize update errors
	/// </summary>
	public void OnUpdateError(TargetFinder.UpdateState updateError)
	{
	}

	/// <summary>
	/// when we start scanning, unregister Trackable from the ImageTargetTemplate, then delete all trackables
	/// </summary>
	public void OnStateChanged(bool scanning) {
		mIsScanning = scanning;
		if (scanning) {
			// clear all known trackables
			ObjectTracker tracker = TrackerManager.Instance.GetTracker<ObjectTracker> ();
			tracker.TargetFinder.ClearTrackables (false);
		}
	}

	/// <summary>
	/// Handles new search results
	/// </summary>
	/// <param name="targetSearchResult"></param>
	public void OnNewSearchResult(TargetFinder.TargetSearchResult targetSearchResult)
	{
		// duplicate the referenced image target
		newImageTarget = Instantiate(ImageTargetTemplate.gameObject) as GameObject;


		GameObject augmentation = null;

		string model_name = targetSearchResult.MetaData;


		if (augmentation != null) {
			augmentation.transform.parent = newImageTarget.transform;
		}
		// enable the new result with the same ImageTargetBehaviour:
		ImageTargetBehaviour imageTargetBehaviour = mImageTracker.TargetFinder.EnableTracking(targetSearchResult, newImageTarget);
		Debug.Log("Metadata value is " + model_name );
		mTargetMetadata = model_name;

        if (scanImageName.Contains(targetSearchResult.TargetName))
        {
            foreach (var x in info)
            {
                if (x.name == targetSearchResult.TargetName)
                {
                    string nam = Path.GetFileName(x.model);
                    try {
                        LoadModel(nam);
                    }catch (Exception e)
                    {
                        Debug.Log(e.Message);
                    }

                }
            }
        }
        else
        {
            foreach (var x in info)
            {
                //Debug.Log("x.name---" + x.name);
                if (x.name == targetSearchResult.TargetName)
                {
                    Debug.Log(x.model);
                    scanImageName.Add(targetSearchResult.TargetName);
                    StartCoroutine(LoadAssets(x.model));
                }
            }
        }

        


        /*switch( model_name ){

		case "Cube" :

			Destroy( imageTargetBehaviour.gameObject.transform.Find("Capsule").gameObject );

			break;

		case "Capsule" :

			Destroy( imageTargetBehaviour.gameObject.transform.Find("Cube").gameObject );

			break;

		}*/

        if (!mIsScanning)
		{
			// stop the target finder
			mCloudRecoBehaviour.CloudRecoEnabled = true;
		}
	}
    /*IEnumerator WaitForReq(string x){
		WWW www = WWW.LoadFromCacheOrDownload ("file:///C:/Users/USER/Desktop/AssetBundles/"+x,1);
		yield return www;
		AssetBundle bundle = www.assetBundle;
		AssetBundleRequest request = bundle.LoadAssetAsync<GameObject> (x);
		yield return request;
		GameObject wheel=request.asset as GameObject;
		Instantiate<GameObject>(wheel);
		
	}*/

    IEnumerator LoadAssets(String url)
    {
        slider.SetActive(true);
        float progress = 0f;
        using (WWW www = new WWW(url))
        {
            float lastprogress = progress;
            while (!www.isDone)
            {
                yield return www.progress;
                progress = lastprogress + www.progress;
                progresspercent = progress;
                //Debug.Log("progress-----"+ progress);
                slider.GetComponentInChildren<loadingtext>().LoadBar(progress);
                //g.GetComponentInChildren<loadingtext>().LoadBar(progress);

            }

            yield return www;
            try
            {
                byte[] WriteBytes = www.bytes;
                string nam = Path.GetFileName(url);
                //Debug.Log("777777777777777777777" + nam);
                File.WriteAllBytes(Application.persistentDataPath + "/" + nam, WriteBytes);
                if (progresspercent == 1f)
                {
                    Debug.Log("download complete");
                    LoadModel(nam);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }


        }
    }

    private void LoadModel(string nam2)
    {
        try
        {
            var myLoadedAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.persistentDataPath + "/", nam2));
            if (myLoadedAssetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                return;
            }
            string[] assets = myLoadedAssetBundle.GetAllAssetNames();
            /*foreach(var t in assets)
            {
                Debug.Log("prefab-------" + t);
            }*/
            slider.SetActive(false);
            var prefab = myLoadedAssetBundle.LoadAsset<GameObject>(assets[0].ToString());
            //Debug.Log("prefab-------"+ prefab);
            var x = Instantiate(prefab);
            x.transform.parent = newImageTarget.transform;
            //x.transform.parent = gameObject.transform;
            x.transform.localPosition = new Vector3(0F, 0F, 0F);
            x.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            //BoxCollider bc = x.AddComponent<BoxCollider>();
            //bc.center = new Vector3(0.1F, 1F, 0F);
            //bc.size = new Vector3(1F, 1.7F, 1F);
            myLoadedAssetBundle.Unload(false);

        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }




    #endregion // ICloudRecoEventHandler_IMPLEMENTATION

    /*void OnGUI() {
		GUI.Box (new Rect(100,200,200,50), "Metadata: " + mTargetMetadata);
	}*/



}

[System.Serializable]
public class LevelInfo
{
    public string name = "";
    public string model = "";
}