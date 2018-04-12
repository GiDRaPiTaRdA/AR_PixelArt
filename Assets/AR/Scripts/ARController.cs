//-----------------------------------------------------------------------
// <copyright file="ARController.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using InstantPreviewInput = GoogleARCore.InstantPreviewInput;

namespace AR
{
    using System.Collections.Generic;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.Rendering;

#if UNITY_EDITOR
    using Input = InstantPreviewInput;
#endif

    public class ARController : MonoBehaviour
    {
        #region Constants

        private float hitLength = 1000f;
        private static float initialShift = 0;
        #endregion

        #region Variables
        /// <summary>
        /// The first-person camera being used to render the passthrough camera image (i.e. AR background).
        /// </summary>
        public Camera FirstPersonCamera;

        /// <summary>
        /// A prefab for tracking and visualizing detected planes.
        /// </summary>
        public GameObject TrackedPlanePrefab;

        /// <summary>
        /// A model to place when a raycast from a user touch hits a plane.
        /// </summary>
        public GameObject CubePrefab;

        /// <summary>
        /// A gameobject parenting UI for displaying the "searching for planes" snackbar.
        /// </summary>
        public GameObject SearchingForPlaneUI;

        /// <summary>
        /// A list to hold new planes ARCore began tracking in the current frame. This object is used across
        /// the application to avoid per-frame allocations.
        /// </summary>
        private List<TrackedPlane> m_NewPlanes = new List<TrackedPlane>();

        /// <summary>
        /// A list to hold all planes ARCore is tracking in the current frame. This object is used across
        /// the application to avoid per-frame allocations.
        /// </summary>
        private List<TrackedPlane> m_AllPlanes = new List<TrackedPlane>();

        /// <summary>
        /// True if the app is in the process of quitting due to an ARCore connection error, otherwise false.
        /// </summary>
        private bool m_IsQuitting = false;

        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon |
                                          TrackableHitFlags.FeaturePointWithSurfaceNormal |
                                          TrackableHitFlags.FeaturePoint |
                                          TrackableHitFlags.PlaneWithinPolygon |
                                          TrackableHitFlags.None |
                                          TrackableHitFlags.PlaneWithinInfinity;

        #endregion

        public bool NotTraking => this.m_AllPlanes.ToList().All(p => p.TrackingState != TrackingState.Tracking);
        public bool PlanesSearch { get; set; } = true;

        public List<GameObject> planes = new List<GameObject>();

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            this._QuitOnConnectionErrors();

            // Check that motion tracking is tracking.
            if (Session.Status != SessionStatus.Tracking)
            {
                const int lostTrackingSleepTimeout = 15;
                Screen.sleepTimeout = lostTrackingSleepTimeout;
                if (!this.m_IsQuitting && Session.Status.IsValid())
                {
                    this.SearchingForPlaneUI.SetActive(this.PlanesSearch);
                }

                return;
            }

            // Screen actiive
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Update new planes
            Session.GetTrackables<TrackedPlane>(this.m_NewPlanes, TrackableQueryFilter.New);

            // Update traked planes
            Session.GetTrackables<TrackedPlane>(this.m_AllPlanes);

            if (this.PlanesSearch)
            {
                // Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
                for (int i = 0; i < this.m_NewPlanes.Count; i++)
                {
                    GameObject planeObject = Instantiate(this.TrackedPlanePrefab, Vector3.zero, Quaternion.identity,
                        this.transform);
                    this.planes.Add(planeObject);
                    planeObject.GetComponent<TrackedPlaneVisualizer>().Initialize(this.m_NewPlanes[i]);
                }
            }


            this.SearchingForPlaneUI.SetActive(this.NotTraking);
        }

   
        public void AddBlockFunc()
        {          
            // get position of center of the sreen
            var cameraPos = GameManager.GetCenterOfScreenPosition();

            Vector3 pos =Vector3.zero;

            // Physics //////////////////////////////////////////
            TrackableHit hit;
            RaycastHit hHit;
            Ray ray = Camera.current.ScreenPointToRay(cameraPos);

            if (Physics.Raycast(ray, out hHit, this.hitLength))
            {
                pos = hHit.point + hHit.normal* this.CubePrefab.transform.localScale.y / 2;
            }
            /////////////////////////////////////////////////////

            // ARCore ///////////////////////////////////////////

            else if (Frame.Raycast(cameraPos.x, cameraPos.y,raycastFilter , out hit))
            {
                pos = new Vector3(hit.Pose.position.x, hit.Pose.position.y+ this.CubePrefab.transform.localScale.y/2, hit.Pose.position.z);
            }
            /////////////////////////////////////////////////////

            // Spawn cube
            if (pos != Vector3.zero)
            {
                var cube = GameManager.InitiateBlockObject(this.CubePrefab,this.RoundPosition(pos));
                cube.GetComponent<MeshRenderer>().material.color = CubeColorBehaviour.CubeColor;
            }
        }

        public void RemoveBlockFunc()
        {
            Vector3 pos =Vector3.zero;

            // Physics //////////////////////////////////////////
            RaycastHit hHit;
            Ray ray = Camera.current.ScreenPointToRay(GameManager.GetCenterOfScreenPosition());

            if (Physics.Raycast(ray, out hHit, this.hitLength))
            {
                pos = hHit.point - hHit.normal* this.CubePrefab.transform.localScale.y / 2;
            }

            // Remove cube
            if (pos != Vector3.zero)
            {
                GameManager.DestroyBlockObject(this.RoundPosition(pos));
            }
        }


        private Vector3 RoundPosition(Vector3 pos)
        {
            pos /= this.CubePrefab.transform.localScale.y;

            if (initialShift == 0)
            {
                initialShift = pos.y - (float) Math.Round(pos.y, MidpointRounding.AwayFromZero);
            }

            pos.x = (float)Math.Round(pos.x, MidpointRounding.AwayFromZero);
            pos.y = (float)Math.Round(pos.y, MidpointRounding.AwayFromZero) + initialShift;
            pos.z = (float)Math.Round(pos.z, MidpointRounding.AwayFromZero);

            pos *= this.CubePrefab.transform.localScale.y;

            return pos;
        }

        
        private Vector3 RoundPosition1(Vector3 pos)
        {
            pos /= this.CubePrefab.transform.localScale.y;

            pos.x = (float)Math.Round(pos.x, MidpointRounding.AwayFromZero);
            pos.y = (float) Math.Round(pos.y, MidpointRounding.AwayFromZero);
            pos.z = (float)Math.Round(pos.z, MidpointRounding.AwayFromZero);

            pos *= this.CubePrefab.transform.localScale.y;

            return pos;
        }

        #region Other

        /// <summary>
        /// Quit the application if there was a connection error for the ARCore session.
        /// </summary>
        private void _QuitOnConnectionErrors()
        {
            if (this.m_IsQuitting)
            {
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
            {
                this._ShowAndroidToastMessage("Camera permission is needed to run this application.");
                this.m_IsQuitting = true;
                this.Invoke("_DoQuit", 0.5f);
            }
            else if (Session.Status.IsError())
            {
                this._ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                this.m_IsQuitting = true;
                this.Invoke("_DoQuit", 0.5f);
            }
        }

        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void _DoQuit()
        {
            Application.Quit();
        }

        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        private void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }));
            }
        }
        #endregion
    }
}
