/*==============================================================================
Copyright (c) 2010-2013 QUALCOMM Austria Research Center GmbH.
All Rights Reserved.
Confidential and Proprietary - QUALCOMM Austria Research Center GmbH.
==============================================================================*/

using UnityEngine;
using ACCESS.FSM;
using System.Collections.Generic;

/// <summary>
/// A custom handler that implements the ITrackableEventHandler interface.
/// </summary>
public class DefaultTrackableEventHandler : MonoBehaviour,
                                            ITrackableEventHandler
{
    #region PRIVATE_MEMBER_VARIABLES
 
    private TrackableBehaviour mTrackableBehaviour;
    
    #endregion // PRIVATE_MEMBER_VARIABLES

	private bool found;
	private static float lastTrackedTime;

    #region UNTIY_MONOBEHAVIOUR_METHODS
    
	private ImageTargetBehaviour mImgTarget;
	
	public static List<DefaultTrackableEventHandler> activeCaps = new List<DefaultTrackableEventHandler>();
	public static List<DefaultTrackableEventHandler> activeThirts = new List<DefaultTrackableEventHandler>();
	
    void Start()
    {
        mTrackableBehaviour = GetComponent<TrackableBehaviour>();
        if (mTrackableBehaviour)
        {
            mTrackableBehaviour.RegisterTrackableEventHandler(this);
        }
		mImgTarget = GetComponent<ImageTargetBehaviour>();
		//activeCaps ;
		//activeThirts = new List<DefaultTrackableEventHandler>();
    }

    #endregion // UNTIY_MONOBEHAVIOUR_METHODS
	
    #region PUBLIC_METHODS

    /// <summary>
    /// Implementation of the ITrackableEventHandler function called when the
    /// tracking state changes.
    /// </summary>
    public void OnTrackableStateChanged(
                                    TrackableBehaviour.Status previousStatus,
                                    TrackableBehaviour.Status newStatus)
    {
        if (newStatus == TrackableBehaviour.Status.DETECTED ||
            newStatus == TrackableBehaviour.Status.TRACKED)
        {
            OnTrackingFound();
        }
        else
        {
            OnTrackingLost();
        }
    }

    #endregion // PUBLIC_METHODS
	public string markerType;
	
	public string TrackableName {
		get {
			return mTrackableBehaviour.TrackableName;
		}
	}
	
	public void setMarkerType(string type) {
		markerType = type;
	}
	
	bool isHeart(DefaultTrackableEventHandler track) {
		return ((track.TrackableName == "heart_L") || (track.TrackableName == "heart_L_mirrored"));
	}
	
	static Vector3 baseHeartScale;
	static Vector3 baseHeartScaleBig;
	private bool heartAvailible = false;
	
	private void heartsUpdate() {
		if (AssetLoader.currentTShirt.name != "Button_model_preview_damages")
			return;
		Transform child = AssetLoader.currentTShirt.transform.GetChild(0);
		if (baseHeartScale.x == 0) {
			baseHeartScale = child.localScale;
			baseHeartScaleBig = baseHeartScale * 4;
		}
		if (activeThirts.Count == 1) {
			if (!isHeart(activeThirts[0]))
				return;
			child.localScale = baseHeartScale;
			Debug.Log("trackableName = "+activeThirts[0].TrackableName);
			if (activeThirts[0].TrackableName == "heart_L")
				child.localPosition = new Vector3(2f, 0f, 6f);
			if (activeThirts[0].TrackableName == "heart_L_mirrored")
				child.localPosition = new Vector3(8f, 0f, 14f);
			
			if (heartAvailible) {
				child.GetComponent<AudioSource>().Stop();
				heartAvailible = false;
			}
			
		} else if ((activeThirts.Count == 2) && isHeart(activeThirts[0]) && isHeart(activeThirts[1])
			&& AssetLoader.currentTShirt.name == "Button_model_preview_damages") {
			AssetLoader.currentTShirt.transform.Translate(-2f, 0f, -7f);
			child.localPosition=new Vector3(2f,child.localPosition.y,6f);
			
			child.localScale = baseHeartScaleBig;
			
			if (!heartAvailible) {
				child.GetComponent<AudioSource>().Play();
				heartAvailible = true;
			}
		}
		
	}
	
	
	
	void Update() {
		DefaultTrackableEventHandler.lastTrackedTime = Time.timeSinceLevelLoad;
		if (markerType == "caps") {
			if (AssetLoader.currentCap != null) {
				if (activeCaps.Count > 0) {
					AssetLoader.currentCap.transform.localPosition = activeCaps[0].transform.position;
					AssetLoader.currentCap.transform.localRotation = activeCaps[0].transform.rotation;
				}
			}
		} else {
			if (AssetLoader.currentTShirt != null) {
				if (activeThirts.Count == 1) {
					AssetLoader.currentTShirt.transform.localPosition = activeThirts[0].transform.position;
					AssetLoader.currentTShirt.transform.localRotation = activeThirts[0].transform.rotation;
				} if (activeThirts.Count == 2) {
					AssetLoader.currentTShirt.transform.localPosition = (activeThirts[0].transform.position + activeThirts[1].transform.position)/2;
					AssetLoader.currentTShirt.transform.forward = (activeThirts[0].transform.forward + activeThirts[1].transform.forward) / 2;
				}
				heartsUpdate();
			}
		}
		if (Time.timeSinceLevelLoad - DefaultTrackableEventHandler.lastTrackedTime > 6.0f)
			FSMCore.SetActive(FSMStates.Tracking_Lost, this);
	}

	private void setObjActive(bool active) {
		Debug.Log("change state for - "+mTrackableBehaviour.Trackable.Name+" to "+active);
		if (markerType == "caps") {
			if (AssetLoader.currentCap != null) {
				Debug.Log(AssetLoader.currentCap.name+" enabled="+active);
				AssetLoader.currentCap.SetActive(active);
			}
		} else {
			if (!active) {
				if (activeThirts.Count == 0) {
					Debug.Log(AssetLoader.currentTShirt.name+" enabled="+active);
					AssetLoader.currentTShirt.SetActive(active);
				}
			} else {
				if (AssetLoader.currentTShirt != null) {
					Debug.Log(AssetLoader.currentTShirt.name+" enabled="+active);
					AssetLoader.currentTShirt.SetActive(active);
				}
			}
		}
	}
	#region PRIVATE_METHODS
    private void OnTrackingFound()
    {
		
		DefaultTrackableEventHandler.lastTrackedTime = Time.timeSinceLevelLoad;
		FSMCore.SetActive(FSMStates.Tracking_Found, this);
		if (markerType == "caps") {
			activeCaps.Add(this);
		} else {
			activeThirts.Add(this);
		}
		setObjActive(true);
        Debug.Log("Trackable " + mTrackableBehaviour.TrackableName + " found");
    }


    private void OnTrackingLost()
    {
		
		if (AssetLoader.datasets["caps"].Contains(mTrackableBehaviour.Trackable)) {
			activeCaps.Remove(this);
		} else {
			activeThirts.Remove(this);
		}
		setObjActive(false);
        Debug.Log("Trackable " + mTrackableBehaviour.TrackableName + " lost");
    }

    #endregion // PRIVATE_METHODS
}
