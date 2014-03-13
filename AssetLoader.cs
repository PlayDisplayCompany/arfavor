using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ACCESS.FSM;
using ACCESS.Message;

public class AssetLoader : MonoBehaviour {
	public static GameObject currentCap;
	public static GameObject currentTShirt;
	public static Dictionary<string, DataSet> datasets;
	public static string ServerAddres = "http://10.0.0.182/";
	
	private GameObject menu;
	private GameObject pressedButton;
	
	private Dictionary<string, Asset> assets;
	
	private void clickModelPrepare() {
		pressedButton = (GameObject) Manager.CurrentSender;
		modelLoad(pressedButton.name);
	}
	
	private void modelLoad(string name) {
		Asset.AssetType type = assets[name].type;
	
		bool prevCap = false;
		bool prevTShirt = false;
	
		if (AssetLoader.currentCap != null) {
			prevCap = AssetLoader.currentCap.activeInHierarchy;
			AssetLoader.currentCap.SetActive(false);
		}
		if (AssetLoader.currentTShirt != null) {
			prevTShirt = AssetLoader.currentTShirt.activeInHierarchy;
			AssetLoader.currentTShirt.SetActive(false);
		}
	
		assets[name].enable();
		if (assets[name].type == Asset.AssetType.caps) {
			AssetLoader.currentCap = assets[name].gameObj;
			AssetLoader.currentCap.SetActive(prevCap);
		} else {
			AssetLoader.currentTShirt = assets[name].gameObj;
			AssetLoader.currentTShirt.SetActive(prevTShirt);
		}
	}
	
	void Start () {
		menu = (GameObject) GameObject.Find("Slide_Menu");
		assets = new Dictionary<string, Asset>();
		initFromResources("Button_model_preview_brick", Asset.AssetType.caps);
		initFromResources("Button_model_preview_axe", Asset.AssetType.caps);
		initFromResources("Button_model_preview_helmet", Asset.AssetType.caps);
		initFromResources("Button_model_preview_stars", Asset.AssetType.caps);
		initFromResources("Button_model_preview_alien", Asset.AssetType.t_shirts);
		initFromResources("Button_model_preview_bust", Asset.AssetType.t_shirts);
		initFromResources("Button_model_preview_damages", Asset.AssetType.t_shirts);
		initFromResources("Button_model_preview_heart", Asset.AssetType.t_shirts);
		//initFromServer();
		AssetLoader.datasets = new Dictionary<string, DataSet>();
		LoadDataSet("caps");
		LoadDataSet("t_shirts");
		//InitMenu();
		
		this.MessageRegister("Click_SetModel", clickModelPrepare);
		//this.MessageRegister("Click_LoadModel", clickModelLoad);
		modelLoad("Button_model_preview_alien");
		modelLoad("Button_model_preview_axe");
	}
	
	private void initFromResources(string name, Asset.AssetType typeName) {
		assets[name] = ScriptableObject.CreateInstance<Asset>();
		assets[name].init(name, typeName);
		if (assets[name].isLoaded) {
			assets[name].setButton((GameObject) GameObject.Find(name));
		}
	}
	
	public static WWW askFor(string paramsUrl, int version) {
		string url = ServerAddres+"?unity=true&"+paramsUrl;
		//Debug.Log("ask for cached ("+version+") - "+url);
		return WWW.LoadFromCacheOrDownload(url, version);
	}
	
	public static WWW askFor(string paramsUrl) {
		string url = ServerAddres+"?unity=true&"+paramsUrl;
		//Debug.Log("ask for - "+url);
		return new WWW(url);
	}
	
	private void getAssetList() {
		WWW www = askFor("getUpdates=true");
		float progress = -1;
		while (!www.isDone) {
			if (progress != www.progress) {
				progress = www.progress;
				//Debug.Log(progress);
			}
		}
		StringReader stream = new StringReader(www.text);
		int version = int.Parse(stream.ReadLine());
		int assetsCount = int.Parse(stream.ReadLine());
		
		assets = new Dictionary<string, Asset>();
		string line = stream.ReadLine();
		while (line != null) {
			string[] buf = line.Split(' ');
			string assetName = buf[0];
			int assetVersion = System.Convert.ToInt32(buf[1]);
			
			//assets[assetName] = new Asset(assetName, loadPicture(assetName), assetVersion);
			line = stream.ReadLine();
		}
	}
	/*
	private void InitMenu() {
		GameObject list = (GameObject) Instantiate((GameObject)Resources.Load("CAP_List"));
		list.transform.parent = menu.transform;
		list.transform.localPosition = new Vector3(0.45f, 0.1f, 0.0f);
		
		float cy = 0.71f;
		int i = 0;
		Shader shader = Shader.Find("Custom/GrayScale");
		GameObject cap_model = (GameObject)Resources.Load("CAP_Model");
		foreach (KeyValuePair<string, Asset> pair in assets) {
			float dy = 0.45f;
			
			GameObject btn = (GameObject) Instantiate(cap_model);
			btn.name = pair.Key;
			btn.transform.parent = list.transform;
			btn.transform.localPosition = new Vector3(0, cy, 0.2f);
			cy -= dy;
			MeshRenderer renderer = (MeshRenderer) btn.GetComponent("MeshRenderer");
			renderer.material.SetTexture("_MainTex", pair.Value.pic);
			renderer.material.shader = shader;
			i++;
		}
	}
	*/
	private bool LoadDataSet(string name)
	{
		Debug.Log("Activate dataSet - "+name);
	    if (!DataSet.Exists(name))
	    {
	        Debug.LogError("Data set " + name + " does not exist.");
	        return false;
	    }
		//FIX исправление для версии вуфории 2.6.7
		if (!QCARRuntimeUtilities.IsQCAREnabled())
        {
			Debug.Log("return");
            return false;
        }

        if (QCARRuntimeUtilities.IsPlayMode())
        {
            // initialize QCAR 
            QCARUnity.CheckInitializationError();
        }
		
		if (TrackerManager.Instance.GetTracker(Tracker.Type.IMAGE_TRACKER) == null)
        {
            TrackerManager.Instance.InitTracker(Tracker.Type.IMAGE_TRACKER);
        }
		//FIX конец исправления, странный баг
		
	    ImageTracker imageTracker =
	        (ImageTracker) TrackerManager.Instance.GetTracker(Tracker.Type.IMAGE_TRACKER);
		
		Debug.Log(imageTracker);
	    
		DataSet dataSet = imageTracker.CreateDataSet();
		AssetLoader.datasets[name] = dataSet;
	    if (!dataSet.Load(name))
	    {
	        Debug.LogError("Failed to load data set " + name + ".");
	        return false;
	    }
	    imageTracker.ActivateDataSet(dataSet);
		
		IEnumerable<TrackableBehaviour> trackableBehaviours = TrackerManager.Instance.GetStateManager().GetTrackableBehaviours();
		foreach (TrackableBehaviour trackable in trackableBehaviours)
		{
			if (dataSet.Contains(trackable.Trackable)) {
				GameObject go = trackable.gameObject;
				DefaultTrackableEventHandler handler = go.AddComponent<DefaultTrackableEventHandler>();
				handler.setMarkerType(name);
				go.name = "trackable_"+name+"_"+trackable.TrackableName;
			}
		}
		
	    return true;
	}
	/*
	private bool LoadDataSet(string dataSetPath, string name)
	{
		//Debug.Log("Activate dataSet - "+name);
	    if (!DataSet.Exists(dataSetPath, DataSet.StorageType.STORAGE_ABSOLUTE))
	    {
	        //Debug.LogError("Data set " + dataSetPath + " does not exist.");
	        return false;
	    }
	    ImageTracker imageTracker =
	        (ImageTracker)TrackerManager.Instance.GetTracker(Tracker.Type.IMAGE_TRACKER);
		
	    DataSet dataSet = imageTracker.CreateDataSet();
		AssetLoader.datasets[name] = dataSet;
	    if (!dataSet.Load(dataSetPath, DataSet.StorageType.STORAGE_ABSOLUTE))
	    {
	        //Debug.LogError("Failed to load data set " + dataSetPath + ".");
	        return false;
	    }
	    imageTracker.ActivateDataSet(dataSet);
		
		IEnumerable<TrackableBehaviour> trackableBehaviours = TrackerManager.Instance.GetStateManager().GetTrackableBehaviours();
	     foreach (TrackableBehaviour trackable in trackableBehaviours)
	     {
			if (dataSet.Contains(trackable.Trackable)) {
				GameObject go = trackable.gameObject;
				go.AddComponent<DefaultTrackableEventHandler>();
				go.name = "trackable_"+name+"_"+trackable.TrackableName;
				//Debug.Log ("add - "+go.name);
			}
	     }
		
	    return true;
	}
	*/
	private Texture2D loadPicture(string assetName) {
		WWW  www = askFor("getIcon="+assetName);
		
		float progress = -1;
		while (!www.isDone) {
			if (progress != www.progress) {
				progress = www.progress;
				//Debug.Log(progress);
			}
		}
		return www.texture;
	}
	/*
	private void loadMarkers(string assetName) {
		WWW  xml = askFor("getMarkersXml="+assetName);
		WWW  data = askFor("getMarkersXml="+assetName);
		float progress = -1;
		while (!data.isDone && !xml.isDone) {
			if (progress != data.progress) {
				progress = data.progress;
				//Debug.Log(progress);
			}
		}
		//Debug.Log("succesful download - "+assetName);
		string markersPath = Application.persistentDataPath+"/"+assetName+".xml";
		string datPath = Application.persistentDataPath+"/"+assetName+".dat";
		File.WriteAllBytes(markersPath, xml.bytes);
		File.WriteAllBytes(datPath, data.bytes);
		
		if  (LoadDataSet(markersPath, assetName)) {
			//Debug.Log("success load markers - "+assetName);
		} else {
			//Debug.Log("fail load markers - "+assetName);
		}
	}
	private IEnumerator loadAsset(string assetName) {
		//WWW www = askFor("getAsset="+assetName, assets[assetName].version);
		WWW www = askFor("getAsset="+assetName);
		yield return www;
		//while (!www.isDone) {
			////Debug.Log(www.progress);
		//}
		assets[assetName].isLoaded = true;
		GameObject model = (GameObject) Instantiate(www.assetBundle.mainAsset);
		model.name = www.assetBundle.mainAsset.name;
		string name = "trackable_"+assetName;
		//Debug.Log("find - "+name);
		GameObject trackable = GameObject.Find(name);
		
		if (trackable != null) {
			//Debug.Log("found "+trackable.name);
			model.transform.parent = trackable.transform;
			model.transform.localPosition = new Vector3(0, 0, 0);
			model.transform.Rotate(270, 0, 0);
			model.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
			
			pressedButton.renderer.material.shader = Shader.Find("ACCESS/GUI/VertexColorTransparent");
			pressedButton.renderer.material.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
			pressedButton = null;
			FSMCore.SetActive(FSMStates.SelectedObject_IsLoaded, this);
		} else {
			//Debug.Log("fail");
		}
	}*/
}

