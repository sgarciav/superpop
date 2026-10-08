using UnityEngine;
using System.Collections;
using com.rfilkov.kinect;


namespace com.rfilkov.components
{
    /// <summary>
    /// FollowUserJointPose makes the game object's transform follow the given user's joint pose.
    /// </summary>
    public class FollowUserJointPose : MonoBehaviour
    {
        [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc. -1 means the sensor doesn't matter")]
        private int sensorIndex = -1;

        [Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
        public int playerIndex = 0;

        [Tooltip("The sensor's joint we want to follow.")]
        public KinectInterop.JointType followJoint = KinectInterop.JointType.Head;

        [Tooltip("Whether the joint view is mirrored or not.")]
        public bool mirroredView = false;

        [Tooltip("Whether to move the object's transform.")]
        public bool moveTransform = true;

        [Tooltip("Whether to rotate the object's transform.")]
        public bool rotateTransform = true;

        [Tooltip("Scene object that will be used to represent the sensor's position and rotation in the scene.")]
        public Transform sensorTransform;

        [Tooltip("Offset of the object to the joint's position.")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("Scale factor of the joint position.")]
        public Vector3 motionScale = Vector3.one;

        [Tooltip("Scale factor of the joint rotation.")]
        private Vector3 rotationFactor = Vector3.zero;

        [Tooltip("Smooth factor used for object's position and rotation smoothing.")]
        public float smoothFactor = 10f;


        private JointTrackingManager trackingManager = null;
        private Quaternion initialRotation = Quaternion.identity;

        private Vector3 vPosJoint = Vector3.zero;
        private Quaternion qRotJoint = Quaternion.identity;


        void Start()
        {
            trackingManager = JointTrackingManager.Instance;

            initialRotation = transform.rotation;
            //initialRotation = mirroredView ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        }

        void Update()
        {
            if (trackingManager && trackingManager.IsInitialized())
            {
                // Note: sensorIndex is not used with JointTrackingManager; designed for single sensor setups
                if (trackingManager.IsUserDetected(playerIndex))
                {
                    ulong userId = trackingManager.GetUserIdByIndex(playerIndex);

                    if (trackingManager.IsJointTracked(userId, followJoint))
                    {
                        // Get joint position from the tracking manager
                        vPosJoint = trackingManager.GetJointPosition(userId, followJoint);

                        if (positionOffset != Vector3.zero)
                        {
                            vPosJoint += positionOffset;
                        }

                        if (sensorTransform)
                        {
                            vPosJoint = sensorTransform.TransformPoint(vPosJoint);
                        }

                        if(motionScale != Vector3.one)
                        {
                            vPosJoint = new Vector3(vPosJoint.x * motionScale.x, vPosJoint.y * motionScale.y, vPosJoint.z * motionScale.z);
                        }

                        // Get joint orientation
                        qRotJoint = trackingManager.GetJointOrientation(userId, followJoint);

                        qRotJoint = initialRotation * qRotJoint;

                        if(rotationFactor != Vector3.zero)
                        {
                            qRotJoint = Quaternion.Euler(rotationFactor) * qRotJoint;
                        }

                        if (moveTransform || rotateTransform)
                        {
                            if (smoothFactor != 0f)
                            {
                                if(moveTransform)
                                    transform.position = Vector3.Lerp(transform.position, vPosJoint, smoothFactor * Time.deltaTime);
                                if(rotateTransform)
                                    transform.rotation = Quaternion.Slerp(transform.rotation, qRotJoint, smoothFactor * Time.deltaTime);
                            }
                            else
                            {
                                if(moveTransform)
                                    transform.position = vPosJoint;
                                if(rotateTransform)
                                    transform.rotation = qRotJoint;
                            }

                        }
                    }

                }
            }
        }

    }
}

